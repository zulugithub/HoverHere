// ############################################################################
// Free RC helicopter Simulator
// Adapter between the existing Unity scripts (Helicopter_Main / Helicopter_ODE) and the C++ physics library hoverhere.dll.
//
// The C# physics (Helicopter_ODE.RK4Step) can stay as it is or be replaced by the native one at run time: Helicopter_Main.TakeStep calls
// Helicopter_NativePhysics.Step instead of RK4Step when `use_native_physics` is on (default: off). The adapter
//   - builds the native model from the live parameters of the ODE object (they are written as XML with the same serializer that saves the
//     parameter sets) and the mechanics files of the helicopter,
//   - hands over the ground mesh that the ODE object uses for its own collision detection,
//   - maps the sticks and switches of the ODE object to the native inputs,
//   - and writes the native results back into x_states[] and the public output fields of the ODE object, so that every other script
//     (animation, audio, gear compression, wheel rolling, UI) keeps working unchanged.
// All values stay in the RIGHT handed physics convention here, exactly like in the ODE object; Helicopter_Main converts to Unity's frame.
// ############################################################################

using System;
using System.IO;
using HoverHere.Native;
using UnityEngine;

namespace Helisimulator
{
    public sealed class Helicopter_NativePhysics : IDisposable
    {
        private readonly object sync = new object();
        private HoverHereSim sim;
        private NativeSimInputs inputs;
        private NativeSimOutputs outputs = HoverHereSim.NewOutputs();
        private bool ready;
        private double lastTime;
        private object uploadedGroundMesh;

        /// <summary>The native model is built and the physics can be stepped.</summary>
        public bool IsReady
        {
            get { return ready; }
        }

        /// <summary>Simulated time of the native physics [s].</summary>
        public double SimulationTime
        {
            get { return lastTime; }
        }

        public string LastError { get; private set; } = string.Empty;

        /// <summary>True when hoverhere.dll can be loaded and has the ABI version of the wrapper.</summary>
        public static bool IsLibraryAvailable(out string error)
        {
            try
            {
                if (NativeMethods.hh_get_abi_version() != HoverHereSim.ExpectedAbiVersion)
                {
                    error = "hoverhere.dll has ABI version " + NativeMethods.hh_get_abi_version() + ", expected " + HoverHereSim.ExpectedAbiVersion;
                    return false;
                }
                error = string.Empty;
                return true;
            }
            catch (DllNotFoundException e)
            {
                error = "hoverhere.dll not found (Assets/Plugins/x86_64): " + e.Message;
            }
            catch (EntryPointNotFoundException e)
            {
                error = "hoverhere.dll has the wrong version: " + e.Message;
            }
            catch (BadImageFormatException e)
            {
                error = "hoverhere.dll has the wrong architecture: " + e.Message;
            }
            return false;
        }

        /// <summary>
        /// Builds the native model of the helicopter that is currently loaded in `ode` and places it at its initial position.
        /// `mainMechanicsXml` / `tailMechanicsXml`: contents of the *_mainrotor_mechanics.xml / *_tailrotor_mechanics.xml of the helicopter (may be
        /// null: ideal CCPM mixer and analytical tail pitch). `temporaryFolder` receives the two parameter files written for the library.
        /// Must be called from the main thread; the physics thread may keep calling Step meanwhile (it waits).
        /// </summary>
        public bool Initialize(Helicopter_ODE ode, string temporaryFolder, string mainMechanicsXml, string tailMechanicsXml, bool useBemt)
        {
            if (!IsLibraryAvailable(out string error))
            {
                LastError = error;
                return false;
            }
            try
            {
                Directory.CreateDirectory(temporaryFolder);
                string heliFile = Path.Combine(temporaryFolder, "native_helicopter_parameter.xml");
                string sceneryFile = Path.Combine(temporaryFolder, "native_scenery_parameter.xml");
                Common.Helper.IO_XML_Serialize(ode.par.transmitter_and_helicopter, heliFile, true);
                Common.Helper.IO_XML_Serialize(ode.par.scenery, sceneryFile, true);

                HoverHereSim fresh = new HoverHereSim();
                fresh.LoadXml(heliFile, HoverHereXmlKind.Helicopter);
                fresh.LoadXml(sceneryFile, HoverHereXmlKind.Scenery);
                if (!string.IsNullOrEmpty(mainMechanicsXml))
                {
                    fresh.LoadXmlText(mainMechanicsXml, "mainrotor_mechanics", HoverHereXmlKind.MainrotorMechanics);
                }
                if (!string.IsNullOrEmpty(tailMechanicsXml))
                {
                    fresh.LoadXmlText(tailMechanicsXml, "tailrotor_mechanics", HoverHereXmlKind.TailrotorMechanics);
                }
                NativeSimOptions options = HoverHereSim.DefaultOptions();
                options.rotor_model = (int)(useBemt ? HoverHereRotorModel.Bemt : HoverHereRotorModel.Classic);
                fresh.SetOptions(options);

                object mesh = UploadGroundMesh(fresh, ode);

                lock (sync)
                {
                    HoverHereSim old = sim;
                    sim = fresh;
                    uploadedGroundMesh = mesh;
                    ready = false;
                    if (old != null)
                    {
                        old.Dispose();
                    }
                    ResetLocked(ode);
                }
                LastError = fresh.Message;
                return ready;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                ready = false;
                return false;
            }
        }

        /// <summary>Sends the ground mesh of the ODE object to the library when it has changed (new scenery). Call from the main thread.</summary>
        public void UpdateGroundMeshIfChanged(Helicopter_ODE ode)
        {
            if (!ready || ode.ground_mesh == null || ReferenceEquals(ode.ground_mesh, uploadedGroundMesh))
            {
                return;
            }
            try
            {
                lock (sync)
                {
                    uploadedGroundMesh = UploadGroundMesh(sim, ode);
                }
            }
            catch (Exception e)
            {
                LastError = e.Message;
            }
        }

        // The ground mesh of the ODE object is already in the right handed physics frame (the ODE mirrors the Unity mesh), three vertices per
        // triangle and one normal per triangle. The winding is made to agree with the normals, so it does not matter how the mesh file was wound.
        private static object UploadGroundMesh(HoverHereSim target, Helicopter_ODE ode)
        {
            Common.Helper.GroundMesh mesh = ode.ground_mesh;
            if (mesh == null || mesh.vertices == null || mesh.normals == null)
            {
                return null;
            }
            int triangles = mesh.vertices.Length / 3;
            float[] vertices = new float[mesh.vertices.Length * 3];
            uint[] indices = new uint[triangles * 3];
            for (int i = 0; i < triangles; i++)
            {
                Vector3 a = mesh.vertices[3 * i];
                Vector3 b = mesh.vertices[3 * i + 1];
                Vector3 c = mesh.vertices[3 * i + 2];
                uint ib = (uint)(3 * i + 1);
                uint ic = (uint)(3 * i + 2);
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), mesh.normals[i]) < 0)
                {
                    uint swap = ib;
                    ib = ic;
                    ic = swap;
                }
                for (int k = 0; k < 3; k++)
                {
                    Vector3 v = mesh.vertices[3 * i + k];
                    vertices[9 * i + 3 * k] = v.x;
                    vertices[9 * i + 3 * k + 1] = v.y;
                    vertices[9 * i + 3 * k + 2] = v.z;
                }
                indices[3 * i] = (uint)(3 * i);
                indices[3 * i + 1] = ib;
                indices[3 * i + 2] = ic;
            }
            target.SetGroundMesh(vertices, indices, false);
            return mesh;
        }

        /// <summary>Places the helicopter at the initial position of the scenery (same call as Helicopter_ODE.Set_Initial_Conditions).</summary>
        public void Reset(Helicopter_ODE ode)
        {
            lock (sync)
            {
                ResetLocked(ode);
            }
        }

        private void ResetLocked(Helicopter_ODE ode)
        {
            if (sim == null)
            {
                return;
            }
            ode.Initial_Position_And_Orientation(out Vector3 position, out Quaternion orientation);  // right handed
            NativeResetParams reset = HoverHereSim.DefaultResetParams();
            unsafe
            {
                reset.position_O[0] = position.x;
                reset.position_O[1] = position.y;
                reset.position_O[2] = position.z;
                reset.orientation[0] = orientation.w;
                reset.orientation[1] = orientation.x;
                reset.orientation[2] = orientation.y;
                reset.orientation[3] = orientation.z;
            }
            reset.rotor_spooled_up = ode.flag_motor_enabled ? 1 : 0;
            sim.Reset(reset);
            inputs = HoverHereSim.DefaultInputs();
            sim.GetOutputs(ref outputs);
            lastTime = 0;
            ready = true;
            CopyToOde(ode);
        }

        /// <summary>
        /// One physics step with the fixed step of the Unity physics thread (dt already multiplied with the time scale). Writes x_states[] and the
        /// output fields of the ODE object. Called from the physics thread.
        /// </summary>
        public void Step(Helicopter_ODE ode, double[] u_inputs, double dt)
        {
            lock (sync)
            {
                if (!ready || sim == null)
                {
                    return;
                }
                inputs.collective = u_inputs[0];
                inputs.yaw = u_inputs[1];
                inputs.pitch = u_inputs[2];
                inputs.roll = u_inputs[3];
                inputs.propeller = u_inputs[4];
                inputs.booster = u_inputs[5];
                inputs.flight_bank = ode.flight_bank;
                inputs.motor_enabled = ode.flag_motor_enabled ? 1 : 0;
                inputs.wheels_raised = ode.wheel_status == Helicopter_ODE.Wheels_Status_Variants.raised ? 1 : 0;
                inputs.wheel_brake = ode.wheel_brake_target;
                unsafe
                {
                    fixed (NativeSimInputs* pinned = &inputs)
                    {
                        pinned->gear_transition[0] = ode.collision_positions_landing_gear_left_rising_offset;
                        pinned->gear_transition[1] = ode.collision_positions_landing_gear_right_rising_offset;
                        pinned->gear_transition[2] = ode.collision_positions_landing_gear_steering_center_rising_offset;
                        pinned->gear_transition[3] = ode.collision_positions_landing_gear_steering_left_rising_offset;
                        pinned->gear_transition[4] = ode.collision_positions_landing_gear_steering_right_rising_offset;
                    }
                }
                sim.Step(dt, ref inputs, ref outputs);
                lastTime = outputs.time;
                CopyToOde(ode);
            }
        }

        // native outputs -> x_states[] (layout of Helicopter_ODE) and the public fields the other scripts read
        private unsafe void CopyToOde(Helicopter_ODE ode)
        {
            double[] x = ode.x_states;
            fixed (NativeSimOutputs* o = &outputs)
            {
                x[0] = o->position_O[0];
                x[1] = o->position_O[1];
                x[2] = o->position_O[2];
                x[3] = o->orientation[0];
                x[4] = o->orientation[1];
                x[5] = o->orientation[2];
                x[6] = o->orientation[3];
                x[7] = o->velocity_O[0];
                x[8] = o->velocity_O[1];
                x[9] = o->velocity_O[2];
                x[10] = o->omega_L[0];
                x[11] = o->omega_L[1];
                x[12] = o->omega_L[2];
                x[13] = o->mainrotor.flapping_a_s;
                x[14] = o->mainrotor.flapping_b_s;
                x[15] = o->tailrotor.flapping_a_s;
                x[16] = o->tailrotor.flapping_b_s;
                x[17] = o->motor_speed;
                x[18] = o->motor_angle;
                x[19] = o->mainrotor_speed;
                x[20] = o->mainrotor_angle;
                x[21] = o->governor_integral;
                // x[22..24] (controller integrals) and x[25..30] (servo states) only exist inside the controller of the library;
                // the normalized controller outputs are the closest equivalent of the servo states
                x[25] = o->commands[0];
                x[26] = o->commands[1];
                x[27] = o->commands[2];
                x[28] = o->commands[3];
                x[29] = o->commands[4];
                x[30] = o->commands[5];
                x[31] = o->rotor_disc_orientation[0];
                x[32] = o->rotor_disc_orientation[1];
                x[33] = o->rotor_disc_orientation[2];
                x[34] = o->rotor_disc_orientation[3];
                x[35] = o->rotor_disc_rate_x;
                x[36] = o->rotor_disc_rate_z;

                ode.I_mo = o->audio.motor_current;
                ode.sound_volume_motor = (float)o->audio.motor_load;
                ode.sound_volume_mainrotor = (float)o->audio.mainrotor_noise;
                ode.sound_volume_mainrotor_stall = (float)o->audio.mainrotor_stall;
                ode.sound_volume_tailrotor = (float)o->audio.tailrotor_pitch_volume;
                ode.sound_volume_tailrotor_stall = 0;
                ode.flag_freewheeling = (o->flags & (uint)HoverHereFlags.Freewheeling) != 0;
                ode.collision_force_too_high_flag = (o->flags & (uint)HoverHereFlags.Crashed) != 0;
                ode.wheel_brake_strength = (float)o->wheel_brake_strength;

                // gear compression and wheel animation
                ode.force_y_gear_or_skid_leftLH = (float)o->gear_force_y[0];
                ode.force_y_gear_or_skid_rightLH = (float)o->gear_force_y[1];
                ode.force_y_gear_or_support_steering_centerLH = (float)o->gear_force_y[2];
                ode.force_y_gear_or_support_steering_leftLH = (float)o->gear_force_y[3];
                ode.force_y_gear_or_support_steering_rightLH = (float)o->gear_force_y[4];
                ode.wheel_rolling_distance_left = (float)o->wheel_rolling_distance[0];
                ode.wheel_rolling_distance_right = (float)o->wheel_rolling_distance[1];
                ode.wheel_rolling_distance_steering_center = (float)o->wheel_rolling_distance[2];
                ode.wheel_rolling_distance_steering_left = (float)o->wheel_rolling_distance[3];
                ode.wheel_rolling_distance_steering_right = (float)o->wheel_rolling_distance[4];
                ode.wheel_steering_center = (float)o->wheel_steering[0];
                ode.wheel_steering_left = (float)o->wheel_steering[1];
                ode.wheel_steering_right = (float)o->wheel_steering[2];
            }
        }

        /// <summary>The newest outputs of the native physics (kinematics of the mechanism, contacts, BEMT telemetry source).</summary>
        public NativeSimOutputs LatestOutputs
        {
            get
            {
                lock (sync)
                {
                    return outputs;
                }
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                ready = false;
                if (sim != null)
                {
                    sim.Dispose();
                    sim = null;
                }
            }
        }
    }
}
