// Minimal stand-ins for the parts of UnityEngine / Common.Helper that Helicopter_Parameter.cs uses.
// The behaviour of Vector3 (public fields x, y, z => <x>,<y>,<z> in XML) and of the rotation helpers mirrors
// Unity and Helicopter_Common.cs; nothing here is used by the physics library itself.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [Serializable]
    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    default: throw new IndexOutOfRangeException("Invalid Vector3 index!");
                }
            }
            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    default: throw new IndexOutOfRangeException("Invalid Vector3 index!");
                }
            }
        }

        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator *(float d, Vector3 a) { return new Vector3(a.x * d, a.y * d, a.z * d); }
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public const float Deg2Rad = PI * 2F / 360F;
        public const float Rad2Deg = 1F / Deg2Rad;

        public static float Sin(float f) { return (float)Math.Sin(f); }
        public static float Cos(float f) { return (float)Math.Cos(f); }
        public static float Pow(float f, float p) { return (float)Math.Pow(f, p); }
        public static float Sqrt(float f) { return (float)Math.Sqrt(f); }
        public static float Abs(float f) { return Math.Abs(f); }
    }

    public struct Resolution
    {
        public int width;
        public int height;
        public int refreshRate;
    }

    public static class Screen
    {
        // The list is only used to fill a drop-down of the UI (not physics relevant).
        public static Resolution[] resolutions
        {
            get { return new Resolution[] { new Resolution { width = 1920, height = 1080, refreshRate = 60 } }; }
        }
    }

    public static class PlayerPrefs
    {
        public static float GetFloat(string key, float defaultValue) { return defaultValue; }
        public static int GetInt(string key, int defaultValue) { return defaultValue; }
    }
}

namespace Common
{
    using UnityEngine;

    // Subset of Common.Helper (Helicopter_Common.cs) needed by Parameter.Update_Calculated_Parameter().
    public static class Helper
    {
        public const float RadPerSec_to_Rpm = 9.5492965964254F;
        public const float Rpm_to_RadPerSec = 0.104719755F;
        public const float Deg_to_Rad = 0.0174532925199433F;
        public const float Rad_to_Deg = 57.2957795130823F;

        // right-hand rule rotation over x-axis, local to reference frame (Helicopter_Common.cs L397)
        public static Vector3 Ax_RL(float phi, Vector3 v3L)
        {
            return new Vector3(
                v3L.x,
                Mathf.Cos(phi) * v3L.y - Mathf.Sin(phi) * v3L.z,
                Mathf.Sin(phi) * v3L.y + Mathf.Cos(phi) * v3L.z);
        }

        // right-hand rule rotation over y-axis (Helicopter_Common.cs L417)
        public static Vector3 Ay_RL(float theta, Vector3 v3L)
        {
            return new Vector3(
                Mathf.Cos(theta) * v3L.x + Mathf.Sin(theta) * v3L.z,
                v3L.y,
                -Mathf.Sin(theta) * v3L.x + Mathf.Cos(theta) * v3L.z);
        }

        // right-hand rule rotation over z-axis (Helicopter_Common.cs L437)
        public static Vector3 Az_RL(float psi, Vector3 v3L)
        {
            return new Vector3(
                Mathf.Cos(psi) * v3L.x - Mathf.Sin(psi) * v3L.y,
                Mathf.Sin(psi) * v3L.x + Mathf.Cos(psi) * v3L.y,
                v3L.z);
        }

        // Helicopter_Common.cs L693 (same as A_RL_B321)
        public static Vector3 A_RL_S123(Vector3 phi_theta_psi, Vector3 v3L)
        {
            return Az_RL(phi_theta_psi[2], Ay_RL(phi_theta_psi[1], Ax_RL(phi_theta_psi[0], v3L)));
        }
    }
}
