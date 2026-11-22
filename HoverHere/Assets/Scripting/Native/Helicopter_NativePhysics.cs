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
//   - writes the native results back into x_states[] and the public output fields of the ODE object, so that the animation, the audio, the
//     gear compression, the wheel rolling and the UI keep working unchanged,
//   - moves the servos, linkages, swashplate, rotating drivers and blades of the 3D rotor mechanics with the kinematics of the C++ library,
//   - fills the debug display of the ODE object (ODEDebug: lines of the forces and of the BEMT grid, debug text) when it is switched on,
//   - and applies parameter changes of the in-game GUI (par_temp -> par) to the running native simulation.
// Values of the ODE object stay in the RIGHT handed physics convention (like in the ODE object); ODEDebug holds Unity's left handed values.
// ############################################################################

using System;
using System.Collections.Generic;
using System.IO;
using Common;
using HoverHere.Native;
using UnityEngine;
using MainrotorMechanics = Helicopter_Mainrotor_Mechanics.Helicopter_Mainrotor_Mechanics;
using TailrotorMechanics = Helicopter_Tailrotor_Mechanics.Helicopter_Tailrotor_Mechanics;
using Helper = Common.Helper;

namespace Helisimulator
{
    public sealed class Helicopter_NativePhysics : IDisposable
    {
        private readonly object sync = new object();
        private HoverHereSim sim;
        private NativeSimInputs inputs;
        private NativeSimOutputs outputs = HoverHereSim.NewOutputs();
        private NativeBemtTelemetry telemetry;
        private bool ready;
        private double lastTime;
        private object uploadedGroundMesh;
        private object lastParameterObject;   // ode.par of the last parameters that were handed to the library (the GUI replaces the object at every change)
        private string temporaryFolder = "";
        private MainrotorMechanics mainMechanics;
        private TailrotorMechanics tailMechanics;

        /// <summary>The native model is built and the physics can be stepped.</summary>
        public bool IsReady
        {
            get { return ready; }
        }

        /// <summary>A parameter change needs a new model (rotor system changed): the caller should call Initialize again.</summary>
        public bool NeedsRebuild { get; private set; }

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
        /// The 3D rotor mechanics objects of Helicopter_Main. While they are attached the servos, linkages, swashplate, drivers and blades are
        /// moved with the kinematics of the native library (the C# Calculate() of these objects is not called in native mode).
        /// </summary>
        public void AttachMechanics(MainrotorMechanics main, TailrotorMechanics tail)
        {
            mainMechanics = main;
            tailMechanics = tail;
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
                this.temporaryFolder = temporaryFolder;
                NeedsRebuild = false;
                HoverHereSim fresh = new HoverHereSim();
                LoadLiveParameters(fresh, ode);
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
                    lastParameterObject = ode.par;
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

        // writes the live parameters of the ODE object (helicopter and transmitter, scenery) as XML and loads them into the native handle
        private void LoadLiveParameters(HoverHereSim target, Helicopter_ODE ode)
        {
            Directory.CreateDirectory(temporaryFolder);
            string heliFile = Path.Combine(temporaryFolder, "native_helicopter_parameter.xml");
            string sceneryFile = Path.Combine(temporaryFolder, "native_scenery_parameter.xml");
            Helper.IO_XML_Serialize(ode.par.transmitter_and_helicopter, heliFile, true);
            Helper.IO_XML_Serialize(ode.par.scenery, sceneryFile, true);
            target.LoadXml(heliFile, HoverHereXmlKind.Helicopter);
            target.LoadXml(sceneryFile, HoverHereXmlKind.Scenery);
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
            Helper.GroundMesh mesh = ode.ground_mesh;
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
            ApplyMechanics();
        }

        /// <summary>
        /// One physics step with the fixed step of the Unity physics thread (dt already multiplied with the time scale). Writes x_states[] and the
        /// output fields of the ODE object, moves the 3D rotor mechanics and fills the debug display. Called from the physics thread.
        /// `debugLevel`: 0 nothing, 1 lines of the forces, 2 also the debug text (ui_debug_panel_state of Helicopter_Main).
        /// </summary>
        public void Step(Helicopter_ODE ode, double[] u_inputs, double dt, bool lastTakestepInFrame, int debugLevel)
        {
            lock (sync)
            {
                if (!ready || sim == null)
                {
                    return;
                }
                // Parameter changes of the GUI. While the game runs the GUI sets the flag and the physics thread copies par_temp to par at the
                // end of a frame (the ODE does it in RK4Step, here it is done by this adapter). While the game is paused (menu open) the GUI
                // replaces `par` itself and sets no flag: the new object is recognized here at the first step after the pause.
                if (lastTakestepInFrame && ode.flag_load_new_parameter_in_ODE_thread)
                {
                    ode.par_temp.transmitter_and_helicopter.Update_Calculated_Parameter();
                    ode.par = ode.par_temp.Deep_Clone();
                    ode.par.transmitter_and_helicopter.Update_Calculated_Parameter();
                    ode.Update_ODE_Debug_Variables();
                    ode.flag_load_new_parameter_in_ODE_thread = false;
                }
                if (!ReferenceEquals(ode.par, lastParameterObject))
                {
                    PushParameters(ode);
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
                if (lastTakestepInFrame)
                {
                    // only the state at the end of the frame is displayed: the large output copy, the mechanism, the ODE fields and the
                    // debug display are done once per frame (the rest of the frame time interpolates the displayed state inside the library)
                    sim.Step(dt, ref inputs, ref outputs);
                    lastTime = outputs.time;
                    CopyToOde(ode);
                    ApplyMechanics();
                    FillDebug(ode, u_inputs, debugLevel);
                }
                else
                {
                    sim.StepNoOutputs(dt, ref inputs);
                    lastTime += dt;
                }
            }
        }

        // The new parameters of ode.par are handed to the running native simulation: no reset, the state stays.
        private void PushParameters(Helicopter_ODE ode)
        {
            lastParameterObject = ode.par;
            try
            {
                LoadLiveParameters(sim, ode);
                sim.UpdateParameters();
                LastError = sim.Message;
            }
            catch (HoverHereException e)
            {
                LastError = e.Message;
                if (e.Result == HoverHereResult.InvalidState)
                {
                    NeedsRebuild = true; // rotor system changed: Helicopter_Main builds a new model
                }
            }
            catch (Exception e)
            {
                LastError = e.Message;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------
        // native outputs -> x_states[] (layout of Helicopter_ODE) and the public fields the other scripts read
        // ------------------------------------------------------------------------------------------------------------------------------
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
                x[22] = o->controller_integral[0]; // roll
                x[23] = o->controller_integral[1]; // yaw
                x[24] = o->controller_integral[2]; // pitch
                // x[25..30] (servo states of the C# model): the normalized controller outputs are the closest equivalent
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

        // ------------------------------------------------------------------------------------------------------------------------------
        // 3D rotor mechanics: servos, rods, swashplate, drivers, blades (positions in the right handed rotor frame, like the C# Calculate())
        // ------------------------------------------------------------------------------------------------------------------------------
        private static unsafe Vector3 V(double* a, int index)
        {
            return new Vector3((float)a[3 * index], (float)a[3 * index + 1], (float)a[3 * index + 2]);
        }

        private unsafe void ApplyMechanics()
        {
            fixed (NativeSimOutputs* o = &outputs)
            {
                if (mainMechanics != null && o->mainrotor_mechanics.valid == 1)
                {
                    NativeMainrotorMechanics* m = &o->mainrotor_mechanics;
                    Vector3[] servoB = new Vector3[3];
                    Vector3[] rodC = new Vector3[3];
                    float[] sigma = new float[3];
                    for (int i = 0; i < 3; i++)
                    {
                        servoB[i] = V(m->servo_arm_B, i);
                        rodC[i] = V(m->rod_C, i);
                        sigma[i] = (float)o->servo_actual[i];
                    }
                    Vector3[] D = new Vector3[2], E = new Vector3[2], axis = new Vector3[2], F = new Vector3[2], G = new Vector3[2], H = new Vector3[2];
                    float[] beta = new float[2];
                    for (int i = 0; i < 2; i++)
                    {
                        D[i] = V(m->swashplate_D, i);
                        E[i] = V(m->blade_E, i);
                        axis[i] = V(m->blade_axis, i);
                        F[i] = V(m->driver_F, i);
                        G[i] = V(m->driver_G, i);
                        H[i] = V(m->driver_H, i);
                        beta[i] = (float)m->blade_beta[i];
                    }
                    // normal of the swashplate: the y-axis of its frame A_OLK (row major, column 1)
                    Vector3 normal = new Vector3((float)m->swashplate_A_OLK[1], (float)m->swashplate_A_OLK[4], (float)m->swashplate_A_OLK[7]);
                    mainMechanics.Apply_Native_Kinematics(sigma, servoB, rodC, V(m->swashplate_K, 0), normal, D, E, axis, beta, F, G, H, (float)o->mainrotor_angle);
                }
                if (tailMechanics != null && o->tailrotor_mechanics.valid == 1)
                {
                    NativeTailrotorMechanics* m = &o->tailrotor_mechanics;
                    Vector3[] G = new Vector3[2], H = new Vector3[2], hingeAxis = new Vector3[2], bladeAxis = new Vector3[2];
                    for (int i = 0; i < 2; i++)
                    {
                        G[i] = V(m->hinge_G, i);
                        H[i] = V(m->pitch_link_H, i);
                        hingeAxis[i] = V(m->hinge_axis, i);
                        bladeAxis[i] = V(m->blade_axis, i);
                    }
                    tailMechanics.Apply_Native_Kinematics((float)o->servo_actual[3], V(m->servo_arm_B, 0), V(m->rod_C, 0), V(m->sleeve_K, 0), G, H, hingeAxis, bladeAxis,
                                                          (float)o->tailrotor_angle);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------
        // debug display (ODEDebug of the ODE object): left handed Unity values, like the ODE fills them
        // ------------------------------------------------------------------------------------------------------------------------------
        private static Vector3 Lh(Vector3 rightHanded)
        {
            return new Vector3(rightHanded.x, rightHanded.y, -rightHanded.z);
        }

        private static unsafe Vector3 Rh(double* a)
        {
            return new Vector3((float)a[0], (float)a[1], (float)a[2]);
        }

        private static string F(double value, string format)
        {
            return Helper.FormatNumber(value, format);
        }

        private unsafe void FillDebug(Helicopter_ODE ode, double[] u, int debugLevel)
        {
            ODEDebugClass d = ode.ODEDebug;
            if (d == null)
            {
                return;
            }
            fixed (NativeSimOutputs* o = &outputs)
            {
                // body orientation as a plain rotation (the formulas of a quaternion do not care about the handedness of the coordinates)
                Quaternion q = new Quaternion((float)o->orientation[1], (float)o->orientation[2], (float)o->orientation[3], (float)o->orientation[0]);
                Quaternion qInverse = Quaternion.Inverse(q);

                d.veloLH = Rh(o->velocity_L);
                d.mainrotor_v_i = (float)o->mainrotor.induced_velocity;
                d.tailrotor_v_i = (float)o->tailrotor.induced_velocity;
                d.propeller_v_i = (float)o->propeller.induced_velocity;
                d.Theta_col_mr = (float)o->mainrotor_debug.collective_pitch;
                d.Theta_col_tr = (float)o->tailrotor_debug.collective_pitch;
                d.Theta_col_pr = (float)o->propeller_debug.collective_pitch;

                // rotors: force / torque lines, local values for the graphs
                FillRotorDebug(o->mainrotor, o->mainrotor_debug, q, qInverse, out d.mainrotor_forceLH, out d.mainrotor_torqueLH, out d.mainrotor_positionO,
                               out d.mainrotor_forceO, out d.mainrotor_torqueO, out d.mainrotor_flapping_stiffness_torqueO);
                FillRotorDebug(o->tailrotor, o->tailrotor_debug, q, qInverse, out d.tailrotor_forceLH, out d.tailrotor_torqueLH, out d.tailrotor_positionO,
                               out d.tailrotor_forceO, out d.tailrotor_torqueO, out d.tailrotor_flapping_stiffness_torqueO);
                FillRotorDebug(o->propeller, o->propeller_debug, q, qInverse, out d.propeller_forceLH, out d.propeller_torqueLH, out d.propeller_positionO,
                               out d.propeller_forceO, out d.propeller_torqueO, out Vector3 unusedStiffness);

                d.P_mr_pr = (float)o->mainrotor_debug.power_profile; d.P_mr_i = (float)o->mainrotor_debug.power_induced;
                d.P_mr_pa = (float)o->mainrotor_debug.power_parasite; d.P_mr_c = (float)o->mainrotor_debug.power_climb;
                d.P_tr_pr = (float)o->tailrotor_debug.power_profile; d.P_tr_i = (float)o->tailrotor_debug.power_induced;
                d.P_tr_pa = (float)o->tailrotor_debug.power_parasite; d.P_tr_c = (float)o->tailrotor_debug.power_climb;
                d.P_pr_pr = (float)o->propeller_debug.power_profile; d.P_pr_i = (float)o->propeller_debug.power_induced;
                d.P_pr_pa = (float)o->propeller_debug.power_parasite; d.P_pr_c = (float)o->propeller_debug.power_climb;
                d.turbulence_mr = (float)o->mainrotor_debug.turbulence; d.turbulence_tr = (float)o->tailrotor_debug.turbulence; d.turbulence_pr = (float)o->propeller_debug.turbulence;
                d.vortex_ring_use_mr = (float)o->mainrotor_debug.vortex_ring_use; d.vortex_ring_state_mr = (float)o->mainrotor_debug.vortex_ring_strength;
                d.vortex_ring_Vz_scaled_mr = (float)o->mainrotor_debug.vz_scaled; d.vortex_ring_Vx_scaled_mr = (float)o->mainrotor_debug.vx_scaled;
                d.vortex_ring_use_tr = (float)o->tailrotor_debug.vortex_ring_use; d.vortex_ring_state_tr = (float)o->tailrotor_debug.vortex_ring_strength;
                d.vortex_ring_Vz_scaled_tr = (float)o->tailrotor_debug.vz_scaled; d.vortex_ring_Vx_scaled_tr = (float)o->tailrotor_debug.vx_scaled;
                d.vortex_ring_use_pr = (float)o->propeller_debug.vortex_ring_use; d.vortex_ring_state_pr = (float)o->propeller_debug.vortex_ring_strength;
                d.vortex_ring_Vz_scaled_pr = (float)o->propeller_debug.vz_scaled; d.vortex_ring_Vx_scaled_pr = (float)o->propeller_debug.vx_scaled;
                // the display wants the strength of the ground effect in [0, 1]: (factor - 1) * 4 like the C# rotor code (factor 1 = no effect)
                d.ground_effect_mr = (float)((o->mainrotor.ground_effect_factor - 1.0) * 4.0);
                d.ground_effect_tr = (float)((o->tailrotor.ground_effect_factor - 1.0) * 4.0);
                d.ground_effect_pr = (float)((o->propeller.ground_effect_factor - 1.0) * 4.0);
                // flap up: air speed in the rotor plane relative to 100 km/h, as in the C# rotor code
                const double normalizeSpeed = 100.0 / 3.6;
                d.flap_up_mr = (float)(o->mainrotor_debug.horizontal_speed / normalizeSpeed);
                d.flap_up_tr = (float)(o->tailrotor_debug.horizontal_speed / normalizeSpeed);
                d.flap_up_pr = (float)(o->propeller_debug.horizontal_speed / normalizeSpeed);

                if (debugLevel > 0)
                {
                    FillLineDebug(ode, d, o);
                }
                if (debugLevel > 1)
                {
                    d.debug_text = BuildDebugText(ode, d, o, u);
                }
            }
        }

        private static unsafe void FillRotorDebug(NativeRotorOutput rotor, NativeRotorDebug debug, Quaternion q, Quaternion qInverse, out Vector3 forceLH,
                                                  out Vector3 torqueLH, out Vector3 positionO, out Vector3 forceO, out Vector3 torqueO, out Vector3 stiffnessTorqueO)
        {
            Vector3 forceWorld = Rh(rotor.force_O);
            Vector3 torqueBody = Rh(rotor.torque_L);
            forceLH = Lh(qInverse * forceWorld);
            torqueLH = Lh(torqueBody);
            positionO = Lh(Rh(debug.position_O));
            forceO = Lh(forceWorld);
            torqueO = Lh(q * torqueBody);
            stiffnessTorqueO = Lh(q * Rh(debug.hub_torque_L));
        }

        private unsafe void FillLineDebug(Helicopter_ODE ode, ODEDebugClass d, NativeSimOutputs* o)
        {
            Quaternion q = new Quaternion((float)o->orientation[1], (float)o->orientation[2], (float)o->orientation[3], (float)o->orientation[0]);
            Vector3 position = Rh(o->position_O);
            var helicopter = ode.par.transmitter_and_helicopter.helicopter;
            bool arrows = helicopter.visual_effects.debug_force_arrow_scale.val > 0;

            // contacts (the first entries of the contact list are the points of the collision lists, the last two are the rotor hubs)
            int forcePoints = Math.Max(0, o->contact_count - 2);
            for (int i = 0; i < d.contact_forceR1.Count && i < forcePoints; i++)
            {
                NativeContact c = o->Get_contacts(i);
                if (c.in_contact != 0)
                {
                    Vector3 normal = new Vector3((float)c.normal_force_O[0], (float)c.normal_force_O[1], (float)c.normal_force_O[2]);
                    Vector3 friction = new Vector3((float)c.friction_force_O[0], (float)c.friction_force_O[1], (float)c.friction_force_O[2]);
                    d.contact_positionR[i] = Lh(new Vector3((float)c.position_O[0], (float)c.position_O[1], (float)c.position_O[2]));
                    d.contact_forceR1[i] = Lh(arrows ? normal + friction : friction);
                    d.contact_forceR2[i] = arrows ? Vector3.zero : Lh(normal);
                }
                else
                {
                    d.contact_positionR[i] = Vector3.zero;
                    d.contact_forceR1[i] = Vector3.zero;
                    d.contact_forceR2[i] = Vector3.zero;
                }
            }

            // fuselage and the fins / wings
            d.drag_on_fuselage_positionO = Lh(position);
            d.drag_on_fuselage_drag_on_fuselage_forceO = Lh(Rh(o->fuselage_force_O));
            d.force_on_horizontal_fin_positionO = Lh(position + q * helicopter.horizontal_fin.posLH.vect3);
            d.force_on_horizontal_fin_forceO = Lh(SurfaceForce(o, 0));
            d.force_on_vertical_fin_positionO = Lh(position + q * helicopter.vertical_fin.posLH.vect3);
            d.force_on_vertical_fin_forceO = Lh(SurfaceForce(o, 1));
            d.force_on_horizontal_wing_left_positionO = Lh(position + q * helicopter.horizontal_wing_left.posLH.vect3);
            d.force_on_horizontal_wing_left_forceO = Lh(SurfaceForce(o, 2));
            d.force_on_horizontal_wing_right_positionO = Lh(position + q * helicopter.horizontal_wing_right.posLH.vect3);
            d.force_on_horizontal_wing_right_forceO = Lh(SurfaceForce(o, 3));

            // booster
            for (int i = 0; i < d.booster_forceO.Count && i < o->booster_count; i++)
            {
                d.booster_positionO[i] = Lh(new Vector3((float)o->booster_position_O[3 * i], (float)o->booster_position_O[3 * i + 1], (float)o->booster_position_O[3 * i + 2]));
                d.booster_forceO[i] = Lh(new Vector3((float)o->booster_force_O[3 * i], (float)o->booster_force_O[3 * i + 1], (float)o->booster_force_O[3 * i + 2]));
            }

            // BEMT grid: position, velocity, thrust and tangential force of the 4 x 10 blade elements
            if ((o->flags & (uint)HoverHereFlags.MainrotorBemt) != 0 && sim.TryGetBemtTelemetry(ref telemetry))
            {
                fixed (NativeBemtTelemetry* t = &telemetry)
                {
                    for (int r = 0; r < NativeConstants.HH_BEMT_RADIAL && r < d.BEMT_blade_segment_position.Count; r++)
                    {
                        for (int c = 0; c < NativeConstants.HH_BEMT_AZIMUTH && c < d.BEMT_blade_segment_position[r].Count; c++)
                        {
                            int k = r * NativeConstants.HH_BEMT_AZIMUTH + c;
                            d.BEMT_blade_segment_position[r][c] = Lh(V(t->position_O, k));
                            // the green line is the induced velocity Vi along the rotor axis, scaled by 0.1 like the C# debug display
                            d.BEMT_blade_segment_velocity[r][c] = Lh(V(t->induced_velocity_O, k) * 0.1f);
                            d.BEMT_blade_segment_thrust[r][c] = Lh(V(t->force_thrust_O, k) * 0.1f);
                            d.BEMT_blade_segment_torque[r][c] = Lh(V(t->force_torque_O, k) * 1.0f);
                        }
                    }
                }
            }
        }

        private static unsafe Vector3 SurfaceForce(NativeSimOutputs* o, int index)
        {
            return new Vector3((float)o->surface_force_O[3 * index], (float)o->surface_force_O[3 * index + 1], (float)o->surface_force_O[3 * index + 2]);
        }

        // the text of the in-game debug window: the same lines as Helicopter_ODE writes
        private unsafe string BuildDebugText(Helicopter_ODE ode, ODEDebugClass d, NativeSimOutputs* o, double[] u)
        {
            const double rad2deg = 180.0 / Math.PI;
            const double rad2rpm = 30.0 / Math.PI;
            var par = ode.par.transmitter_and_helicopter;
            var transmission = par.helicopter.transmission;
            double n_tr = transmission.n_mr2tr.val;
            double n_pr = transmission.n_mr2pr.val;
            double n_mo2mr = transmission.n_mo2mr.val;
            bool freewheel = (o->flags & (uint)HoverHereFlags.Freewheeling) != 0;
            bool motorOn = (o->flags & (uint)HoverHereFlags.MotorEnabled) != 0;
            double Omega_mg = o->motor_angle / n_mo2mr;
            double dOmegaFreewheel = (Omega_mg - o->mainrotor_angle) * (transmission.invert_mainrotor_rotation_direction.val ? -1.0 : 1.0) * rad2deg / 360;
            var sb = new System.Text.StringBuilder();
            sb.Append(" INFO:       veloLHx=").Append(F(o->velocity_L[0], "0.00")).Append("m/s   veloLHy=").Append(F(o->velocity_L[1], "0.00")).Append("m/s   veloLHz=").Append(F(o->velocity_L[2], "0.00"))
              .Append("m/s   omegaLHx=").Append(F(o->omega_L[0] * rad2deg, "0.0")).Append("deg/s   omegaLHy=").Append(F(o->omega_L[1] * rad2deg, "0.0")).Append("deg/s   omegaLHz = ").Append(F(o->omega_L[2] * rad2deg, "0.0")).Append("deg/s\n");
            sb.Append(" WIND:       vx=").Append(F(o->wind_L[0], "0.00")).Append("m/sec   vy= ").Append(F(o->wind_L[1], "0.00")).Append("m/sec   vz=").Append(F(o->wind_L[2], "0.0"))
              .Append("m/sec                  delta_lat_mr = ").Append(F(o->commands[1], "0.0000")).Append("    delta_lon_mr = ").Append(F(o->commands[2], "0.0000"))
              .Append("       x_states[22] = ").Append(F(o->controller_integral[0], "0.0000")).Append("    x_states[24] = ").Append(F(o->controller_integral[2], "0.0000")).Append("\n");
            sb.Append(" INPUT:      u0=").Append(F(u[0], "0.000")).Append("   u1=").Append(F(u[1], "0.000")).Append("   u2 = ").Append(F(u[2], "0.000")).Append("   u3= ").Append(F(u[3], "0.000"))
              .Append("   u4= ").Append(F(u[4], "0.000")).Append("   u5= ").Append(F(u[5], "0.000")).Append("   u6= ").Append(F(u[6], "0.000")).Append("   u7= ").Append(F(u[7], "0.000"))
              .Append("    Wheel Brake= ").Append(F(o->wheel_brake_strength * 100, "0")).Append(" %\n");
            sb.Append(" BLDC:       omega_mo = ").Append(F(o->motor_speed * rad2rpm, "####.0")).Append("rpm   V_mo= ").Append(F(o->motor_voltage_terminal, "0.00")).Append("V   I_mo=").Append(F(o->audio.motor_current, "0.0"))
              .Append("A   P_mo_el=").Append(F(o->motor_power_electric, "0")).Append("W   P_mo_mech=").Append(F(o->motor_power_mechanical, "0")).Append("W   eta_mo= ").Append(F(o->motor_efficiency, "0.00"))
              .Append("   ").Append(motorOn ? "ON" : "OFF").Append("\n");
            sb.Append(" POWER:      P_mr_pr=").Append(F(d.P_mr_pr, "####.0")).Append("W   P_mr_i=").Append(F(d.P_mr_i, "####.0")).Append("W   P_mr_pa=").Append(F(d.P_mr_pa, "####.0")).Append("W   P_mr_c= ").Append(F(d.P_mr_c, "####.0")).Append("W\n");
            sb.Append(" POWER:      P_tr_pr=").Append(F(d.P_tr_pr, "####.0")).Append("W   P_tr_i=").Append(F(d.P_tr_i, "####.0")).Append("W   P_tr_pa=").Append(F(d.P_tr_pa, "####.0")).Append("W   P_tr_c= ").Append(F(d.P_tr_c, "####.0")).Append("W\n");
            sb.Append(" POWER:      P_pr_pr=").Append(F(d.P_pr_pr, "####.0")).Append("W   P_pr_i=").Append(F(d.P_pr_i, "####.0")).Append("W   P_pr_pa=").Append(F(d.P_pr_pa, "####.0")).Append("W   P_pr_c= ").Append(F(d.P_pr_c, "####.0")).Append("W\n");
            sb.Append(" DRAG:       force_fuselageO.x=").Append(F(o->fuselage_force_O[0], "0.0")).Append("N   force_fuselageO.y=").Append(F(o->fuselage_force_O[1], "0.0")).Append("N   force_fuselageO.z=").Append(F(o->fuselage_force_O[2], "0.0")).Append("N\n");
            sb.Append(" MAINROTOR:  omega_mr=").Append(F(o->mainrotor_speed * rad2rpm, "####")).Append("rpm   thrust_mr=").Append(F(o->mainrotor.thrust, "###.00")).Append("N   torque_mr=").Append(F(o->mainrotor.torque, "###.00"))
              .Append("Nm   Theta_col_mr=").Append(F(o->mainrotor_debug.collective_pitch * rad2deg, "##.00")).Append("deg   v_i_mr=").Append(F(o->mainrotor.induced_velocity, "##.00")).Append("m/s     tau_mr=")
              .Append(F(o->mainrotor_debug.flapping_tau, "0.000")).Append("s     flag_freewheeling=").Append(freewheel ? "YES" : "NO").Append("\n");
            sb.Append(" VRS_MAIN:   use_VRS=").Append(F(o->mainrotor_debug.vortex_ring_use, "0.00")).Append(" strength_VRS=").Append(F(o->mainrotor_debug.vortex_ring_strength, "0.00")).Append(" Vz_scaled=")
              .Append(F(o->mainrotor_debug.vz_scaled, "0.00")).Append(" Vx_scaled=").Append(F(o->mainrotor_debug.vx_scaled, "0.00")).Append("  Vi_blended=").Append(F(o->mainrotor.induced_velocity, "##.00")).Append("m/s\n");
            sb.Append(" TAILROTOR:  omega_tr=").Append(F(o->tailrotor_speed * rad2rpm, "####")).Append("rpm   thrust_tr=").Append(F(o->tailrotor.thrust, "###.00")).Append("N   torque_tr=").Append(F(o->tailrotor.torque, "###.00"))
              .Append("Nm   Theta_col_tr=").Append(F(o->tailrotor_debug.collective_pitch * rad2deg, "##.00")).Append("deg   delta_col_tr=").Append(F(o->commands[3], "##.00")).Append("   v_i_tr=")
              .Append(F(o->tailrotor.induced_velocity, "##.00")).Append("m/s\n");
            sb.Append(" PROPELLER:  omega_pr=").Append(F(o->propeller_speed * rad2rpm, "####")).Append("rpm   thrust_pr=").Append(F(o->propeller.thrust, "###.00")).Append("N   torque_pr=").Append(F(o->propeller.torque, "###.00"))
              .Append("Nm   delta_col_pr=").Append(F(o->commands[6], "##.00")).Append("   v_i_pr=").Append(F(o->propeller.induced_velocity, "##.00")).Append("m/s\n");
            sb.Append(" FLAPPING:   flapping_a_s_L= ").Append(F(o->mainrotor.flapping_a_s * rad2deg, "0.00")).Append("deg     flapping_b_s_L=").Append(F(o->mainrotor.flapping_b_s * rad2deg, "0.00")).Append("deg\n");
            sb.Append(" TORQUES:    torquesLH.x = ").Append(F(o->nonground_torque_L[0], "0.000")).Append("Nm     torquesLH.y=").Append(F(o->nonground_torque_L[1], "0.000")).Append("Nm     torquesLH.z=")
              .Append(F(o->nonground_torque_L[2], "0.000")).Append("Nm   \n");
            sb.Append(" GOVERNOR:   Lipo = ").Append(F(par.helicopter.accumulator.voltage.val, "0.00")).Append("V    V_s_out = ").Append(F(o->motor_voltage_applied, "0.00")).Append("Volt    omega_mr=")
              .Append(F(o->mainrotor_speed * rad2rpm, "####.0")).Append("rpm(").Append(F((1 / n_mo2mr) * o->governor_target * rad2rpm, "0")).Append("rpm) DELTA_omega_mo___int=")
              .Append(F(o->governor_integral * rad2deg, "####.0")).Append("deg\n");
            sb.Append(" Omega_mo: ").Append(F(o->motor_angle * rad2deg / 360, "####")).Append("rev Omega_mr: ").Append(F(o->mainrotor_angle * rad2deg / 360, "####")).Append("rev Omega_mg: ")
              .Append(F(Omega_mg * rad2deg / 360, "####")).Append("rev DOmega_Freewheel: ").Append(F(dOmegaFreewheel, "0.00")).Append("rev  [native physics, ").Append(F(o->step_count, "0")).Append(" steps]\n");
            return sb.ToString();
        }

        /// <summary>The newest outputs of the native physics.</summary>
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
