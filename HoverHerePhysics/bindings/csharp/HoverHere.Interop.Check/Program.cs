using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using HoverHere.Native;

namespace HoverHere.Interop.Check
{
    // usage: HoverHere.Interop.Check <path to hoverhere.dll|libhoverhere.so> [directory with Logo600SE_V3_parameter_file.xml and the mechanics files]
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

        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("usage: HoverHere.Interop.Check <hoverhere library> [directory with the Logo600SE_V3 files]");
                return 2;
            }
            string library = Path.GetFullPath(args[0]);
            NativeLibrary.SetDllImportResolver(typeof(HoverHereSim).Assembly, (name, assembly, path) =>
                name == NativeMethods.Library ? NativeLibrary.Load(library) : IntPtr.Zero);

            CheckAbi();
            CheckLayout();
            CheckConversions();
            if (args.Length > 1 && Directory.Exists(args[1]))
            {
                CheckSimulation(args[1]);
            }
            else
            {
                Console.WriteLine("(no Logo600SE_V3 directory given: simulation part skipped)");
            }

            if (failures == 0)
            {
                Console.WriteLine("C# interop ok");
            }
            return failures == 0 ? 0 : 1;
        }

        private static void CheckAbi()
        {
            Check(NativeMethods.hh_get_abi_version() == HoverHereSim.ExpectedAbiVersion, "ABI version of the library differs from the wrapper");
        }

        // "hh_sim_options" -> NativeSimOptions, the same rule as csharp_name() of tools/gen_layout_table.py
        private static string CSharpName(string cName)
        {
            string name = cName.StartsWith("hh_", StringComparison.Ordinal) ? cName.Substring(3) : cName;
            string result = "Native";
            foreach (string part in name.Split('_'))
            {
                result += char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant();
            }
            return result;
        }

        private static void CheckLayout()
        {
            Dictionary<string, ulong> table = new Dictionary<string, ulong>();
            int count = NativeMethods.hh_get_layout_count();
            Check(count > 100, "layout table of the library is too small: " + count);
            for (int i = 0; i < count; i++)
            {
                Check(NativeMethods.hh_get_layout_entry(i, out IntPtr namePointer, out ulong value) == HoverHereResult.Ok, "layout entry " + i);
                table[Marshal.PtrToStringAnsi(namePointer)] = value;
            }

            // every struct of the table has a C# mirror with the same size, and every member the same offset
            HashSet<string> cStructs = new HashSet<string>();
            foreach (string key in table.Keys)
            {
                if (key.StartsWith("sizeof:", StringComparison.Ordinal))
                {
                    cStructs.Add(key.Substring("sizeof:".Length));
                }
            }
            int members = 0;
            foreach (string cStruct in cStructs)
            {
                Type type = typeof(HoverHereSim).Assembly.GetType("HoverHere.Native." + CSharpName(cStruct));
                if (type == null)
                {
                    Check(false, "no C# struct for " + cStruct);
                    continue;
                }
                Check((ulong)Marshal.SizeOf(type) == table["sizeof:" + cStruct], "size of " + type.Name + " is " + Marshal.SizeOf(type) + " but the C struct has " + table["sizeof:" + cStruct]);
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    string cMember = field.Name.EndsWith("__raw", StringComparison.Ordinal) ? field.Name.Substring(0, field.Name.Length - 5) : field.Name;
                    string key = "offsetof:" + cStruct + "." + cMember;
                    if (!table.TryGetValue(key, out ulong offset))
                    {
                        Check(false, "the C struct " + cStruct + " has no member " + cMember + " (C# field " + field.Name + ")");
                        continue;
                    }
                    Check((ulong)Marshal.OffsetOf(type, field.Name) == offset, "offset of " + type.Name + "." + field.Name + " is " + Marshal.OffsetOf(type, field.Name) + " but the C member is at " + offset);
                    members++;
                }
                foreach (string key in table.Keys)
                {
                    if (key.StartsWith("offsetof:" + cStruct + ".", StringComparison.Ordinal))
                    {
                        string cMember = key.Substring(("offsetof:" + cStruct + ".").Length);
                        bool found = type.GetField(cMember) != null || type.GetField(cMember + "__raw") != null;
                        Check(found, "the C member " + cStruct + "." + cMember + " is missing in " + type.Name);
                    }
                }
            }
            Console.WriteLine("layout: " + cStructs.Count + " structs, " + members + " members match the C compiler");
        }

        private static void Near(double actual, double expected, double tolerance, string name)
        {
            Check(Math.Abs(actual - expected) <= tolerance, name + ": " + actual + " instead of " + expected);
        }

        // quaternion (w, x, y, z) rotates a vector
        private static void Rotate(double w, double x, double y, double z, double vx, double vy, double vz, out double rx, out double ry, out double rz)
        {
            // v' = v + 2 w (q x v) + 2 q x (q x v)
            double cx = y * vz - z * vy;
            double cy = z * vx - x * vz;
            double cz = x * vy - y * vx;
            double dx = y * cz - z * cy;
            double dy = z * cx - x * cz;
            double dz = x * cy - y * cx;
            rx = vx + 2 * (w * cx + dx);
            ry = vy + 2 * (w * cy + dy);
            rz = vz + 2 * (w * cz + dz);
        }

        private static void CheckConversions()
        {
            // vectors: its own inverse
            HoverHereCoordinates.Vector(1, 2, 3, out double a, out double b, out double c);
            HoverHereCoordinates.Vector(a, b, c, out double d, out double e, out double f);
            Check(a == 1 && b == 2 && c == -3 && d == 1 && e == 2 && f == 3, "vector conversion");

            // rotating a vector and converting the result is the same as converting both and rotating with the converted quaternion
            double angle = 0.7;
            double s = Math.Sin(angle / 2);
            double qw = Math.Cos(angle / 2);
            double qx = s * 0.36;
            double qy = s * 0.48;
            double qz = s * 0.8;
            Rotate(qw, qx, qy, qz, 0.3, -1.2, 2.0, out double rx, out double ry, out double rz);
            HoverHereCoordinates.Vector(rx, ry, rz, out double crx, out double cry, out double crz);
            HoverHereCoordinates.Quaternion(qw, qx, qy, qz, out double cw, out double cqx, out double cqy, out double cqz);
            HoverHereCoordinates.Vector(0.3, -1.2, 2.0, out double cvx, out double cvy, out double cvz);
            Rotate(cw, cqx, cqy, cqz, cvx, cvy, cvz, out double lx, out double ly, out double lz);
            Near(lx, crx, 1e-12, "rotated vector x");
            Near(ly, cry, 1e-12, "rotated vector y");
            Near(lz, crz, 1e-12, "rotated vector z");

            // mesh: the geometric normal of every triangle is the mirrored normal
            float[] vertices = { 0, 0, 0, 1, 0, 0, 0, 0, 1 };
            int[] triangles = { 0, 2, 1 };  // Unity (clockwise seen from above): normal points up
            float[] converted = HoverHereCoordinates.MeshToPhysics(vertices, triangles, out uint[] indices);
            double[] n = Normal(converted, indices);
            Near(n[1], 1.0, 1e-12, "normal of the converted mesh points up");

            // with the normals of the mesh the winding is corrected where it disagrees (a mesh whose triangles are not wound like its normals)
            float[] normals = { 0, 1, 0, 0, 1, 0, 0, 1, 0 };
            int[] wrongWinding = { 0, 1, 2 };  // clockwise seen from below
            HoverHereCoordinates.MeshToPhysics(vertices, wrongWinding, normals, out uint[] fixedIndices);
            double[] fixedNormal = Normal(HoverHereCoordinates.MeshToPhysics(vertices, wrongWinding, normals, out fixedIndices), fixedIndices);
            Near(fixedNormal[1], 1.0, 1e-12, "the normals of the mesh decide the winding");
            Check(converted[8] == -1f && converted[5] == 0f, "z of the vertices is negated");
        }

        private static double[] Normal(float[] v, uint[] i)
        {
            double[] e0 = { v[3 * i[1]] - v[3 * i[0]], v[3 * i[1] + 1] - v[3 * i[0] + 1], v[3 * i[1] + 2] - v[3 * i[0] + 2] };
            double[] e1 = { v[3 * i[2]] - v[3 * i[0]], v[3 * i[2] + 1] - v[3 * i[0] + 1], v[3 * i[2] + 2] - v[3 * i[0] + 2] };
            double[] n = { e0[1] * e1[2] - e0[2] * e1[1], e0[2] * e1[0] - e0[0] * e1[2], e0[0] * e1[1] - e0[1] * e1[0] };
            double length = Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]);
            return new[] { n[0] / length, n[1] / length, n[2] / length };
        }

        private static unsafe void CheckSimulation(string directory)
        {
            string heli = Path.Combine(directory, "Logo600SE_V3_parameter_file.xml");
            if (!File.Exists(heli))
            {
                Console.WriteLine("(" + heli + " not found: simulation part skipped)");
                return;
            }
            using (HoverHereSim sim = new HoverHereSim())
            {
                sim.LoadXml(heli, HoverHereXmlKind.Helicopter);
                sim.LoadXml(Path.Combine(directory, "Logo600SE_V3_mainrotor_mechanics.xml"), HoverHereXmlKind.MainrotorMechanics);
                sim.LoadXml(Path.Combine(directory, "Logo600SE_V3_tailrotor_mechanics.xml"));  // auto detected

                // a bad file is reported, not thrown away silently
                bool threw = false;
                try { sim.LoadXml(Path.Combine(directory, "does_not_exist.xml")); }
                catch (HoverHereException exception) { threw = exception.Result == HoverHereResult.Io; }
                Check(threw, "loading a missing file throws HoverHereException(Io)");

                NativeSimOptions options = HoverHereSim.DefaultOptions();
                options.rotor_model = (int)HoverHereRotorModel.Classic;
                sim.SetOptions(options);

                // ground plane in Unity coordinates (clockwise seen from above in the left handed system)
                float[] quad = { -50, 0, -50, 50, 0, -50, 50, 0, 50, -50, 0, 50 };
                int[] unityTriangles = { 0, 3, 2, 0, 2, 1 };  // clockwise seen from above: the visible side points up
                float[] converted = HoverHereCoordinates.MeshToPhysics(quad, unityTriangles, out uint[] indices);
                sim.SetGroundMesh(converted, indices);

                NativeResetParams reset = HoverHereSim.DefaultResetParams();
                reset.position_O[1] = 0.25;
                sim.Reset(reset);

                NativeSimInputs inputs = HoverHereSim.DefaultInputs();
                inputs.collective = -0.5;
                NativeSimOutputs outputs = HoverHereSim.NewOutputs();
                int steps = sim.Step(0.1005, ref inputs, ref outputs);
                Check(steps == 100, "0.1005 s are 100 steps of 1 ms, got " + steps);
                Check(outputs.step_count == 100, "step_count " + outputs.step_count);
                Check(!double.IsNaN(outputs.position_O[1]) && outputs.position_O[1] > 0 && outputs.position_O[1] < 1, "height after 0.1 s: " + outputs.position_O[1]);
                Check(outputs.mainrotor_speed != 0, "main rotor turns");
                Check((outputs.flags & (uint)HoverHereFlags.Diverged) == 0, "no divergence");
                Check(outputs.contact_count > 0 && outputs.contact_count <= NativeConstants.HH_MAX_CONTACTS, "contact count " + outputs.contact_count);
                NativeContact contact = outputs.Get_contacts(0);
                Check(contact.type >= 0 && contact.type <= 7, "contact type " + contact.type);
                Check((outputs.flags & (uint)HoverHereFlags.MainrotorBemt) == 0, "classic rotor model selected");

                // the BEMT telemetry exists for the BEMT model only
                NativeBemtTelemetry telemetry = default;
                Check(!sim.TryGetBemtTelemetry(ref telemetry), "no BEMT telemetry for the classic rotor");
                options.rotor_model = (int)HoverHereRotorModel.Bemt;
                sim.SetOptions(options);
                sim.Reset(reset);
                sim.StepFixed(ref inputs, ref outputs);
                Check(sim.TryGetBemtTelemetry(ref telemetry), "BEMT telemetry");
                Check(telemetry.valid == 1 && telemetry.radius[NativeConstants.HH_BEMT_ELEMENTS - 1] > telemetry.radius[0], "BEMT grid radii");
                Check((outputs.flags & (uint)HoverHereFlags.MechanicsMainrotor) != 0 && outputs.mainrotor_mechanics.valid == 1, "mechanics visuals");

                // debug data of the BEMT grid and of the rotors for the debug display of Unity
                double hubDistance = Math.Sqrt(Math.Pow(telemetry.position_O[0] - outputs.mainrotor_debug.position_O[0], 2) +
                                               Math.Pow(telemetry.position_O[1] - outputs.mainrotor_debug.position_O[1], 2) +
                                               Math.Pow(telemetry.position_O[2] - outputs.mainrotor_debug.position_O[2], 2));
                Near(hubDistance, telemetry.radius[0], 1e-3, "BEMT element 0 lies on its radius around the hub");
                Check(outputs.mainrotor_debug.power_profile != 0 || outputs.mainrotor_speed != 0, "rotor debug values");

                // live tuning: new parameters for the running simulation, nothing is reset
                ulong stepsBefore = outputs.step_count;
                sim.LoadXml(heli, HoverHereXmlKind.Helicopter);
                sim.UpdateParameters();
                sim.StepFixed(ref inputs, ref outputs);
                Check(outputs.step_count == stepsBefore + 1, "UpdateParameters keeps the simulation running (step count " + outputs.step_count + ")");
                Check(!double.IsNaN(outputs.mainrotor_speed) && (outputs.flags & (uint)HoverHereFlags.Diverged) == 0, "no divergence after UpdateParameters");

                // steps without the output copy, the state is read afterwards
                int quiet = sim.StepNoOutputs(0.0025, ref inputs);
                Check(quiet == 2, "StepNoOutputs: 2.5 ms are two steps, got " + quiet);
                sim.GetOutputs(ref outputs);
                Check(outputs.step_count == stepsBefore + 3, "state after StepNoOutputs (step count " + outputs.step_count + ")");

                // physics thread: lock free hand over of inputs and outputs
                sim.Reset(reset);
                sim.PostInputs(ref inputs);
                sim.StartThread();
                NativeSimOutputs live = HoverHereSim.NewOutputs();
                bool got = false;
                for (int i = 0; i < 300 && !got; i++)
                {
                    got = sim.TryReadOutputs(ref live) && live.step_count > 5;
                    System.Threading.Thread.Sleep(10);
                }
                sim.StopThread();
                Check(got, "the physics thread publishes states");
            }
        }
    }
}
