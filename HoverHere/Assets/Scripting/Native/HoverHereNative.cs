// ############################################################################
// Free RC helicopter Simulator
// P/Invoke wrapper of the C++ physics library hoverhere.dll / libhoverhere.so (HoverHerePhysics/c_api).
//
// - The structs are the blittable mirrors of sim_types.h in HoverHereNative.Structs.g.cs (generated, layout checked by the interop check).
// - The library is RIGHT handed (x forward, y up, z right in the body frame, y up in the world); Unity is left handed. Every conversion
//   happens in HoverHereCoordinates (z is negated, quaternions are mirrored, triangles change their winding), never inside the library.
// - Language level C# 9 (Unity 6); the file compiles without UnityEngine (the Unity part is at the end, #if UNITY_5_3_OR_NEWER).
// ############################################################################

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HoverHere.Native
{
    public enum HoverHereResult
    {
        Ok = 0,
        InvalidArgument = 1,
        OutOfMemory = 2,
        Internal = 3,
        InvalidState = 4,
        Io = 5,
        Parse = 6,
        Model = 7
    }

    /// <summary>Kind of a parameter file (same numbers as hh_xml_kind).</summary>
    public enum HoverHereXmlKind
    {
        Auto = -1,
        Helicopter = 0,
        Scenery = 1,
        MainrotorMechanics = 3,
        TailrotorMechanics = 4
    }

    public enum HoverHereRotorModel
    {
        Classic = 0,
        Bemt = 1
    }

    [Flags]
    public enum HoverHereFlags : uint
    {
        None = 0,
        Freewheeling = 1u << 0,
        Crashed = 1u << 1,
        GovernorSaturated = 1u << 2,
        MotorEnabled = 1u << 3,
        AnyContact = 1u << 4,
        MainrotorBemt = 1u << 5,
        MechanicsMainrotor = 1u << 6,
        MechanicsTailrotor = 1u << 7,
        Diverged = 1u << 8
    }

    [Flags]
    public enum HoverHereBemtFlags : uint
    {
        None = 0,
        NoVortexRing = 1u << 0,
        NoGroundEffect = 1u << 1,
        NoTurbulence = 1u << 2,
        NoDynamicInflow = 1u << 3,
        QuasiStatic = 1u << 4
    }

    public sealed class HoverHereException : Exception
    {
        public HoverHereResult Result { get; }

        public HoverHereException(HoverHereResult result, string function, string message)
            : base(function + " failed with " + result + (string.IsNullOrEmpty(message) ? "" : ": " + message))
        {
            Result = result;
        }
    }

    internal static class NativeMethods
    {
        public const string Library = "hoverhere";

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint hh_get_abi_version();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hh_sim_options_init(ref NativeSimOptions options);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hh_reset_params_init(ref NativeResetParams parameters);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hh_sim_inputs_init(ref NativeSimInputs inputs);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_create(out IntPtr sim);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hh_sim_destroy(IntPtr sim);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int hh_sim_get_message(IntPtr sim, byte[] buffer, int capacity);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_clear_setup(IntPtr sim);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_load_xml(IntPtr sim, byte[] utf8Path, int kind);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_load_xml_text(IntPtr sim, byte[] utf8Xml, byte[] utf8SourceName, int kind);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_set_options(IntPtr sim, ref NativeSimOptions options);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_update_parameters(IntPtr sim);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe HoverHereResult hh_sim_set_ground_mesh(IntPtr sim, float* vertices, uint vertexCount, uint* indices, uint indexCount, uint flags);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_clear_ground_mesh(IntPtr sim);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_reset(IntPtr sim, ref NativeResetParams parameters);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_step(IntPtr sim, double frameDt, ref NativeSimInputs inputs, ref NativeSimOutputs outputs, out int steps);

        // the same function without the (large) output copy: for the physics steps of a frame whose result is not displayed
        [DllImport(Library, EntryPoint = "hh_sim_step", CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_step_no_outputs(IntPtr sim, double frameDt, ref NativeSimInputs inputs, IntPtr outputs, out int steps);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_step_fixed(IntPtr sim, ref NativeSimInputs inputs, ref NativeSimOutputs outputs);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_get_outputs(IntPtr sim, ref NativeSimOutputs outputs);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_get_bemt_telemetry(IntPtr sim, ref NativeBemtTelemetry telemetry);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_thread_start(IntPtr sim);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_thread_stop(IntPtr sim);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_post_inputs(IntPtr sim, ref NativeSimInputs inputs);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_sim_read_outputs(IntPtr sim, ref NativeSimOutputs outputs);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int hh_get_layout_count();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern HoverHereResult hh_get_layout_entry(int index, out IntPtr name, out ulong value);
    }

    /// <summary>
    /// One simulation handle of the native library. All methods throw HoverHereException on errors, except the ones that are named Try....
    /// A handle is used by one thread at a time (PostInputs and ReadOutputs may be called from the game thread while the physics thread runs).
    /// </summary>
    public sealed class HoverHereSim : IDisposable
    {
        public const uint ExpectedAbiVersion = 2;

        private IntPtr handle;

        public HoverHereSim()
        {
            uint abi = NativeMethods.hh_get_abi_version();
            if (abi != ExpectedAbiVersion)
            {
                throw new InvalidOperationException("hoverhere library has ABI version " + abi + ", the C# wrapper expects " + ExpectedAbiVersion);
            }
            Check(NativeMethods.hh_sim_create(out handle), nameof(NativeMethods.hh_sim_create));
        }

        ~HoverHereSim()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (handle != IntPtr.Zero)
            {
                NativeMethods.hh_sim_thread_stop(handle);
                NativeMethods.hh_sim_destroy(handle);
                handle = IntPtr.Zero;
            }
        }

        public bool IsDisposed
        {
            get { return handle == IntPtr.Zero; }
        }

        private IntPtr Handle
        {
            get
            {
                if (handle == IntPtr.Zero)
                {
                    throw new ObjectDisposedException(nameof(HoverHereSim));
                }
                return handle;
            }
        }

        /// <summary>Text of the last error or warning (diagnostics of the XML loaders).</summary>
        public string Message
        {
            get
            {
                int length = NativeMethods.hh_sim_get_message(Handle, null, 0);
                if (length <= 0)
                {
                    return string.Empty;
                }
                byte[] buffer = new byte[length + 1];
                NativeMethods.hh_sim_get_message(Handle, buffer, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, length);
            }
        }

        private void Check(HoverHereResult result, string function)
        {
            if (result != HoverHereResult.Ok)
            {
                string message = string.Empty;
                if (handle != IntPtr.Zero)
                {
                    message = Message;
                }
                throw new HoverHereException(result, function, message);
            }
        }

        private static byte[] Utf8(string text)
        {
            return Encoding.UTF8.GetBytes(text + "\0");
        }

        // ------------------------------------------------------------------------------------------------------------------------------
        // setup
        // ------------------------------------------------------------------------------------------------------------------------------
        public static NativeSimOptions DefaultOptions()
        {
            NativeSimOptions options = default;
            NativeMethods.hh_sim_options_init(ref options);
            return options;
        }

        public static NativeResetParams DefaultResetParams()
        {
            NativeResetParams parameters = default;
            NativeMethods.hh_reset_params_init(ref parameters);
            return parameters;
        }

        public static NativeSimInputs DefaultInputs()
        {
            NativeSimInputs inputs = default;
            NativeMethods.hh_sim_inputs_init(ref inputs);
            return inputs;
        }

        public static NativeSimOutputs NewOutputs()
        {
            NativeSimOutputs outputs = default;
            outputs.struct_size = (uint)Marshal.SizeOf<NativeSimOutputs>();
            return outputs;
        }

        public void ClearSetup()
        {
            Check(NativeMethods.hh_sim_clear_setup(Handle), nameof(NativeMethods.hh_sim_clear_setup));
        }

        /// <summary>Loads a parameter file (helicopter, scenery, main or tail rotor mechanics) as an overlay on the files loaded before.</summary>
        public void LoadXml(string path, HoverHereXmlKind kind = HoverHereXmlKind.Auto)
        {
            Check(NativeMethods.hh_sim_load_xml(Handle, Utf8(path), (int)kind), nameof(NativeMethods.hh_sim_load_xml));
        }

        public void LoadXmlText(string xml, string sourceName, HoverHereXmlKind kind = HoverHereXmlKind.Auto)
        {
            Check(NativeMethods.hh_sim_load_xml_text(Handle, Utf8(xml), Utf8(sourceName), (int)kind), nameof(NativeMethods.hh_sim_load_xml_text));
        }

        public void SetOptions(NativeSimOptions options)
        {
            Check(NativeMethods.hh_sim_set_options(Handle, ref options), nameof(NativeMethods.hh_sim_set_options));
        }

        /// <summary>
        /// Live tuning: applies the parameter files loaded with LoadXml since the last Reset to the RUNNING simulation (nothing is reset).
        /// Throws HoverHereException(InvalidState) when the rotor system changed (tandem / BEMT / rotor exists): that needs a rebuild with Reset.
        /// </summary>
        public void UpdateParameters()
        {
            Check(NativeMethods.hh_sim_update_parameters(Handle), nameof(NativeMethods.hh_sim_update_parameters));
        }

        /// <summary>
        /// Ground mesh in the RIGHT handed world frame (see HoverHereCoordinates.MeshToPhysics for a Unity mesh): xyz per vertex, three indices per
        /// triangle, counter clockwise seen from the side of the normal.
        /// </summary>
        public unsafe void SetGroundMesh(float[] verticesXyz, uint[] indices, bool flipWinding = false)
        {
            if (verticesXyz == null || indices == null)
            {
                throw new ArgumentNullException();
            }
            fixed (float* v = verticesXyz)
            fixed (uint* i = indices)
            {
                Check(NativeMethods.hh_sim_set_ground_mesh(Handle, v, (uint)(verticesXyz.Length / 3), i, (uint)indices.Length, flipWinding ? 1u : 0u),
                      nameof(NativeMethods.hh_sim_set_ground_mesh));
            }
        }

        public void ClearGroundMesh()
        {
            Check(NativeMethods.hh_sim_clear_ground_mesh(Handle), nameof(NativeMethods.hh_sim_clear_ground_mesh));
        }

        /// <summary>Builds the model from the loaded files and places the helicopter.</summary>
        public void Reset(NativeResetParams parameters)
        {
            Check(NativeMethods.hh_sim_reset(Handle, ref parameters), nameof(NativeMethods.hh_sim_reset));
        }

        // ------------------------------------------------------------------------------------------------------------------------------
        // synchronous stepping
        // ------------------------------------------------------------------------------------------------------------------------------
        /// <summary>Advances by a frame time (fixed physics steps with the frame cut and substep clamp). Returns the number of steps.</summary>
        public int Step(double frameDt, ref NativeSimInputs inputs, ref NativeSimOutputs outputs)
        {
            outputs.struct_size = (uint)Marshal.SizeOf<NativeSimOutputs>();
            Check(NativeMethods.hh_sim_step(Handle, frameDt, ref inputs, ref outputs, out int steps), nameof(NativeMethods.hh_sim_step));
            return steps;
        }

        /// <summary>Advances like Step but does not copy the outputs (cheaper); GetOutputs reads the state afterwards.</summary>
        public int StepNoOutputs(double frameDt, ref NativeSimInputs inputs)
        {
            Check(NativeMethods.hh_sim_step_no_outputs(Handle, frameDt, ref inputs, IntPtr.Zero, out int steps), nameof(NativeMethods.hh_sim_step));
            return steps;
        }

        public void StepFixed(ref NativeSimInputs inputs, ref NativeSimOutputs outputs)
        {
            outputs.struct_size = (uint)Marshal.SizeOf<NativeSimOutputs>();
            Check(NativeMethods.hh_sim_step_fixed(Handle, ref inputs, ref outputs), nameof(NativeMethods.hh_sim_step_fixed));
        }

        public void GetOutputs(ref NativeSimOutputs outputs)
        {
            outputs.struct_size = (uint)Marshal.SizeOf<NativeSimOutputs>();
            Check(NativeMethods.hh_sim_get_outputs(Handle, ref outputs), nameof(NativeMethods.hh_sim_get_outputs));
        }

        /// <summary>The 4 x 10 BEMT grid of the last step; false when the main rotor is not a BEMT rotor.</summary>
        public bool TryGetBemtTelemetry(ref NativeBemtTelemetry telemetry)
        {
            telemetry.struct_size = (uint)Marshal.SizeOf<NativeBemtTelemetry>();
            HoverHereResult result = NativeMethods.hh_sim_get_bemt_telemetry(Handle, ref telemetry);
            if (result == HoverHereResult.InvalidState)
            {
                return false;
            }
            Check(result, nameof(NativeMethods.hh_sim_get_bemt_telemetry));
            return true;
        }

        // ------------------------------------------------------------------------------------------------------------------------------
        // physics thread
        // ------------------------------------------------------------------------------------------------------------------------------
        public void StartThread()
        {
            Check(NativeMethods.hh_sim_thread_start(Handle), nameof(NativeMethods.hh_sim_thread_start));
        }

        public void StopThread()
        {
            Check(NativeMethods.hh_sim_thread_stop(Handle), nameof(NativeMethods.hh_sim_thread_stop));
        }

        /// <summary>Lock free: hands the sticks to the physics thread.</summary>
        public void PostInputs(ref NativeSimInputs inputs)
        {
            Check(NativeMethods.hh_sim_post_inputs(Handle, ref inputs), nameof(NativeMethods.hh_sim_post_inputs));
        }

        /// <summary>Lock free: copies the newest state published by the physics thread; false while nothing has been published yet.</summary>
        public bool TryReadOutputs(ref NativeSimOutputs outputs)
        {
            outputs.struct_size = (uint)Marshal.SizeOf<NativeSimOutputs>();
            HoverHereResult result = NativeMethods.hh_sim_read_outputs(Handle, ref outputs);
            if (result == HoverHereResult.InvalidState)
            {
                return false;
            }
            Check(result, nameof(NativeMethods.hh_sim_read_outputs));
            return true;
        }
    }

    /// <summary>
    /// Conversion between the right handed physics frame and the left handed Unity frame (mirror at the x-y plane: z is negated).
    /// Pure functions on doubles; the Unity types are handled by HoverHereUnityConversion below.
    /// </summary>
    public static class HoverHereCoordinates
    {
        /// <summary>Vector (position, velocity, force, wind, ...) between the two frames; the conversion is its own inverse.</summary>
        public static void Vector(double x, double y, double z, out double ox, out double oy, out double oz)
        {
            ox = x;
            oy = y;
            oz = -z;
        }

        /// <summary>
        /// Rotation quaternion (w, x, y, z): the mirror M = diag(1, 1, -1) maps a rotation R to M R M, the axis of a rotation is a pseudo vector,
        /// so (w, x, y, z) becomes (w, -x, -y, z). Its own inverse.
        /// </summary>
        public static void Quaternion(double w, double x, double y, double z, out double ow, out double ox, out double oy, out double oz)
        {
            ow = w;
            ox = -x;
            oy = -y;
            oz = z;
        }

        /// <summary>Angular velocity (a pseudo vector, in the world frame or in the body frame of a mirrored model): (x, y, z) becomes (-x, -y, z).</summary>
        public static void AngularVelocity(double x, double y, double z, out double ox, out double oy, out double oz)
        {
            ox = -x;
            oy = -y;
            oz = z;
        }

        /// <summary>
        /// Triangle mesh from the (left handed) world space of Unity to the physics frame: z is negated and the winding of every triangle is
        /// reversed, so that the normals computed from the winding stay on the same side of the surface (Unity: front faces are clockwise).
        /// Returns the converted vertex array (xyz per vertex) and writes the reordered indices.
        /// </summary>
        public static float[] MeshToPhysics(float[] unityVerticesXyz, int[] unityTriangles, out uint[] physicsIndices)
        {
            return MeshToPhysics(unityVerticesXyz, unityTriangles, null, out physicsIndices);
        }

        /// <summary>
        /// Same, but when `unityNormalsXyz` (one normal per vertex in the same space, e.g. mesh.normals of the world space mesh) is given the winding
        /// of every triangle is made to agree with the mean normal of its vertices. That makes the result independent of how a mesh file was wound.
        /// </summary>
        public static float[] MeshToPhysics(float[] unityVerticesXyz, int[] unityTriangles, float[] unityNormalsXyz, out uint[] physicsIndices)
        {
            float[] vertices = new float[unityVerticesXyz.Length];
            for (int i = 0; i + 2 < unityVerticesXyz.Length; i += 3)
            {
                vertices[i] = unityVerticesXyz[i];
                vertices[i + 1] = unityVerticesXyz[i + 1];
                vertices[i + 2] = -unityVerticesXyz[i + 2];
            }
            physicsIndices = new uint[unityTriangles.Length - unityTriangles.Length % 3];
            for (int t = 0; t + 2 < unityTriangles.Length; t += 3)
            {
                int a = unityTriangles[t];
                int b = unityTriangles[t + 2];  // reversed winding
                int c = unityTriangles[t + 1];
                if (unityNormalsXyz != null)
                {
                    double ex = vertices[3 * b] - vertices[3 * a];
                    double ey = vertices[3 * b + 1] - vertices[3 * a + 1];
                    double ez = vertices[3 * b + 2] - vertices[3 * a + 2];
                    double fx = vertices[3 * c] - vertices[3 * a];
                    double fy = vertices[3 * c + 1] - vertices[3 * a + 1];
                    double fz = vertices[3 * c + 2] - vertices[3 * a + 2];
                    double nx = ey * fz - ez * fy;
                    double ny = ez * fx - ex * fz;
                    double nz = ex * fy - ey * fx;
                    double mx = unityNormalsXyz[3 * a] + unityNormalsXyz[3 * b] + unityNormalsXyz[3 * c];
                    double my = unityNormalsXyz[3 * a + 1] + unityNormalsXyz[3 * b + 1] + unityNormalsXyz[3 * c + 1];
                    double mz = -(unityNormalsXyz[3 * a + 2] + unityNormalsXyz[3 * b + 2] + unityNormalsXyz[3 * c + 2]);  // the normal is a vector: z negated
                    if (nx * mx + ny * my + nz * mz < 0)
                    {
                        int swap = b;
                        b = c;
                        c = swap;
                    }
                }
                physicsIndices[t] = (uint)a;
                physicsIndices[t + 1] = (uint)b;
                physicsIndices[t + 2] = (uint)c;
            }
            return vertices;
        }
    }

    /// <summary>Convenient access to the fixed size array members of the native structs.</summary>
    public static unsafe class NativeAccess
    {
        public static void SetVector(double* destination, double x, double y, double z)
        {
            destination[0] = x;
            destination[1] = y;
            destination[2] = z;
        }
    }
}

#if UNITY_5_3_OR_NEWER
namespace HoverHere.Native
{
    using UnityEngine;

    /// <summary>The conversions of HoverHereCoordinates with the Unity types.</summary>
    public static class HoverHereUnityConversion
    {
        public static unsafe Vector3 ToUnityVector(double* v)
        {
            return new Vector3((float)v[0], (float)v[1], (float)-v[2]);
        }

        public static unsafe void FromUnityVector(Vector3 u, double* destination)
        {
            destination[0] = u.x;
            destination[1] = u.y;
            destination[2] = -u.z;
        }

        /// <summary>Unity quaternion from the physics quaternion stored as (w, x, y, z).</summary>
        public static unsafe Quaternion ToUnityQuaternion(double* q)
        {
            return new Quaternion((float)-q[1], (float)-q[2], (float)q[3], (float)q[0]);
        }

        public static unsafe void FromUnityQuaternion(Quaternion u, double* destination)
        {
            destination[0] = u.w;
            destination[1] = -u.x;
            destination[2] = -u.y;
            destination[3] = u.z;
        }

        /// <summary>Ground mesh of a Unity mesh in world space (apply the transform of the collision object before).</summary>
        public static void SetGroundMesh(HoverHereSim sim, Vector3[] worldVertices, int[] triangles, Vector3[] worldNormals = null)
        {
            float[] xyz = new float[worldVertices.Length * 3];
            for (int i = 0; i < worldVertices.Length; i++)
            {
                xyz[3 * i] = worldVertices[i].x;
                xyz[3 * i + 1] = worldVertices[i].y;
                xyz[3 * i + 2] = worldVertices[i].z;
            }
            float[] normals = null;
            if (worldNormals != null && worldNormals.Length == worldVertices.Length)
            {
                normals = new float[worldNormals.Length * 3];
                for (int i = 0; i < worldNormals.Length; i++)
                {
                    normals[3 * i] = worldNormals[i].x;
                    normals[3 * i + 1] = worldNormals[i].y;
                    normals[3 * i + 2] = worldNormals[i].z;
                }
            }
            float[] converted = HoverHereCoordinates.MeshToPhysics(xyz, triangles, normals, out uint[] indices);
            sim.SetGroundMesh(converted, indices, false);
        }
    }
}
#endif
