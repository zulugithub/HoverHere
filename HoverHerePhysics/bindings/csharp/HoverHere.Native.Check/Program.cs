using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using HoverHere.Native;

namespace HoverHere.Native.Check
{
    // usage: HoverHere.Native.Check [default_params.bin]
    //   default_params.bin: raw bytes of make_default_params() + update_calculated() written by
    //   `hoverhere_params_check --write-default-blob`; when given, the C# view of the bytes is compared with the known defaults.
    public static class Program
    {
        private static int failures;

        private static void Check(bool ok, string message)
        {
            if (!ok)
            {
                Console.Error.WriteLine("FAIL: " + message);
                failures++;
            }
        }

        private static void CheckNear(double actual, double expected, string name)
        {
            Check(Math.Abs(actual - expected) <= 1e-9 * Math.Max(1.0, Math.Abs(expected)), name + ": " + actual + " instead of " + expected);
        }

        public static int Main(string[] args)
        {
            CheckLayout();
            CheckBlittable();
            if (args.Length > 0)
            {
                CheckBlob(File.ReadAllBytes(args[0]));
            }
            if (failures == 0)
            {
                Console.WriteLine("C# struct layout ok: " + ModelParamsLayout.Leaves.Length + " parameters, " + ModelParamsLayout.Size + " bytes"
                                  + (args.Length > 0 ? ", values of the C++ blob match" : ""));
            }
            return failures == 0 ? 0 : 1;
        }

        // offsets and sizes of every leaf as computed by the generator
        private static void CheckLayout()
        {
            Check(Marshal.SizeOf<ModelParams>() == ModelParamsLayout.Size, "size of ModelParams is " + Marshal.SizeOf<ModelParams>() + " but the schema says " + ModelParamsLayout.Size);
            foreach ((string path, int offset, int size) in ModelParamsLayout.Leaves)
            {
                Type type = typeof(ModelParams);
                int sum = 0;
                bool found = true;
                foreach (string name in path.Split('/'))
                {
                    FieldInfo field = type.GetField(name);
                    if (field == null)
                    {
                        Check(false, "no field '" + name + "' for " + path);
                        found = false;
                        break;
                    }
                    sum += (int)Marshal.OffsetOf(type, name);
                    type = field.FieldType;
                }
                if (!found)
                {
                    continue;
                }
                Check(sum == offset, path + ": offset " + sum + " instead of " + offset);
                Check(Marshal.SizeOf(type) == size, path + ": size " + Marshal.SizeOf(type) + " instead of " + size);
            }
        }

        // pinning fails for structs that contain managed references (not blittable)
        private static void CheckBlittable()
        {
            object boxed = new ModelParams();
            try
            {
                GCHandle handle = GCHandle.Alloc(boxed, GCHandleType.Pinned);
                handle.Free();
            }
            catch (ArgumentException e)
            {
                Check(false, "ModelParams is not blittable: " + e.Message);
            }
        }

        private static void CheckBlob(byte[] bytes)
        {
            Check(bytes.Length == ModelParamsLayout.Size, "blob has " + bytes.Length + " bytes, ModelParams has " + ModelParamsLayout.Size);
            if (bytes.Length != ModelParamsLayout.Size)
            {
                return;
            }
            ModelParams p = MemoryMarshal.Read<ModelParams>(bytes);

            CheckNear(p.helicopter.mass_total, 3.6, "helicopter.mass_total");
            CheckNear(p.helicopter.mainrotor.R, 0.775, "helicopter.mainrotor.R");
            CheckNear(p.helicopter.mainrotor.posLH.y, 0.182387, "helicopter.mainrotor.posLH.y");
            CheckNear(p.helicopter.tailrotor.R, 0.138, "helicopter.tailrotor.R");
            CheckNear(p.helicopter.tailrotor.oriLH.x, 90, "helicopter.tailrotor.oriLH.x");
            CheckNear(p.helicopter.governor.target_rpm.bank1, 1160, "helicopter.governor.target_rpm.bank1");
            CheckNear(p.helicopter.governor.target_rpm.bank3, 1550, "helicopter.governor.target_rpm.bank3");
            CheckNear(p.helicopter.flybarless.K_a.bank2, 270, "helicopter.flybarless.K_a.bank2");
            Check(p.helicopter.accumulator.cell_count == 6, "helicopter.accumulator.cell_count");
            Check(p.helicopter.transmission.invert_motor2maingear_transmission == 1, "helicopter.transmission.invert_motor2maingear_transmission");
            Check(p.helicopter.transmission.invert_mainrotor2tailrotor_transmission == 0, "helicopter.transmission.invert_mainrotor2tailrotor_transmission");
            Check(p.helicopter.tailrotor.rotor_exists == 1 && p.helicopter.propeller.rotor_exists == 0, "rotor_exists flags");
            Check(p.helicopter.collision.positions_left_type == 0, "helicopter.collision.positions_left_type");
            CheckNear(p.helicopter.collision.positions_left_rised_offset.z, 0.095, "helicopter.collision.positions_left_rised_offset.z");
            Check(p.helicopter.collision.positions_usual.count == 0, "helicopter.collision.positions_usual.count");
            CheckNear(p.helicopter.brushless.Ke, 1.0 / (560 * Math.PI / 30), "helicopter.brushless.Ke (calculated)");
            CheckNear(p.helicopter.transmission.n_mo2mr, 106.0 / 15.0, "helicopter.transmission.n_mo2mr (calculated)");
            CheckNear(p.scenery.gravity, 9.81, "scenery.gravity");
            CheckNear(p.scenery.weather.rho_air, 1.27, "scenery.weather.rho_air");
            CheckNear(p.tuning.rotor_tuning.mainrotor_tuning.inflow_factor, 1.2, "tuning.rotor_tuning.mainrotor_tuning.inflow_factor");
            CheckNear(p.tuning.rotor_tuning.tailrotor_tuning.inflow_factor, 1.0, "tuning.rotor_tuning.tailrotor_tuning.inflow_factor");
            CheckNear(p.simulation.various.stiction_factor, 1.0, "simulation.various.stiction_factor");
            CheckNear(p.simulation.various.V_h, 0.09, "simulation.various.V_h");
        }
    }
}
