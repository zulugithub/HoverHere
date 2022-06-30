// ############################################################################
// Free RC helicopter Simulator
// 20.01.2020 
// Copyright (c) zulu
//
// Unity c# code
// ############################################################################
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Parameter;
//using System.Collections;
using System.Runtime.InteropServices;
using System.Drawing;


namespace Common
{
    enum Rotor_Type { mainrotor, tailrotor, propeller };

    // ############################################################################
    //    HHHHHHHHH     HHHHHHHHH                   lllllll                                                            
    //    H:::::::H     H:::::::H                   l:::::l                                                            
    //    H:::::::H     H:::::::H                   l:::::l                                                            
    //    HH::::::H     H::::::HH                   l:::::l                                                            
    //      H:::::H     H:::::H      eeeeeeeeeeee    l::::lppppp   ppppppppp       eeeeeeeeeeee    rrrrr   rrrrrrrrr   
    //      H:::::H     H:::::H    ee::::::::::::ee  l::::lp::::ppp:::::::::p    ee::::::::::::ee  r::::rrr:::::::::r  
    //      H::::::HHHHH::::::H   e::::::eeeee:::::eel::::lp:::::::::::::::::p  e::::::eeeee:::::eer:::::::::::::::::r 
    //      H:::::::::::::::::H  e::::::e     e:::::el::::lpp::::::ppppp::::::pe::::::e     e:::::err::::::rrrrr::::::r
    //      H:::::::::::::::::H  e:::::::eeeee::::::el::::l p:::::p     p:::::pe:::::::eeeee::::::e r:::::r     r:::::r
    //      H::::::HHHHH::::::H  e:::::::::::::::::e l::::l p:::::p     p:::::pe:::::::::::::::::e  r:::::r     rrrrrrr
    //      H:::::H     H:::::H  e::::::eeeeeeeeeee  l::::l p:::::p     p:::::pe::::::eeeeeeeeeee   r:::::r            
    //      H:::::H     H:::::H  e:::::::e           l::::l p:::::p    p::::::pe:::::::e            r:::::r            
    //    HH::::::H     H::::::HHe::::::::e         l::::::lp:::::ppppp:::::::pe::::::::e           r:::::r            
    //    H:::::::H     H:::::::H e::::::::eeeeeeee l::::::lp::::::::::::::::p  e::::::::eeeeeeee   r:::::r            
    //    H:::::::H     H:::::::H  ee:::::::::::::e l::::::lp::::::::::::::pp    ee:::::::::::::e   r:::::r            
    //    HHHHHHHHH     HHHHHHHHH    eeeeeeeeeeeeee llllllllp::::::pppppppp        eeeeeeeeeeeeee   rrrrrrr            
    //                                                      p:::::p                                                    
    //                                                      p:::::p                                                    
    //                                                     p:::::::p                                                   
    //                                                     p:::::::p                                                   
    //                                                     p:::::::p                                                   
    //                                                     ppppppppp
    // ############################################################################
    public static class Helper
    {
        // ############################################################################
        // const values
        // ############################################################################
        public const float RadPerSec_to_Rpm = 9.5492965964254F;
        public const float Rpm_to_RadPerSec = 0.104719755F;
        public const float Deg_to_Rad = 0.0174532925199433F;
        public const float Rad_to_Deg = 57.2957795130823F;
        // ############################################################################




        // ############################################################################
        // clamp
        // ############################################################################
        public static int Clamp(stru_int stru_int)
        {
            if (stru_int.val.CompareTo(stru_int.max) > 0)
                return stru_int.max;
            if (stru_int.val.CompareTo(stru_int.min) < 0)
                return stru_int.min;
            return stru_int.val;
        }
        public static float Clamp(stru_float stru_float)
        {
            if (stru_float.val.CompareTo(stru_float.max) > 0)
                return stru_float.max;
            if (stru_float.val.CompareTo(stru_float.min) < 0)
                return stru_float.min;
            return stru_float.val;
        }
        public static int Clamp(stru_list stru_list)
        {
            if (stru_list.val.CompareTo(stru_list.str.Count) >= 0)
                return stru_list.str.Count - 1;
            if (stru_list.val.CompareTo(0) < 0)
                return 0;
            return stru_list.val;
        }
        // ############################################################################




        // ############################################################################
        // conversion from here used math's right handed coordinates system to Unity's left handed coordinates system definition
        // ############################################################################
        public static Quaternion ConvertRightHandedToLeftHandedQuaternion(Quaternion rightHandedQuaternion)
        {
            // works only for this here-used special coordinate definition that only the z axis is flipped
            return new Quaternion(-rightHandedQuaternion.x,
                                  -rightHandedQuaternion.y,
                                   rightHandedQuaternion.z,
                                   rightHandedQuaternion.w);
        }
        public static Quaternion ConvertLeftHandedToRightHandedQuaternion(Quaternion leftHandedQuaternion)
        {
            // works only for this here-used special coordinate definition that only the z axis is flipped
            return new Quaternion(leftHandedQuaternion.x,
                                   leftHandedQuaternion.y,
                                  -leftHandedQuaternion.z,
                                  -leftHandedQuaternion.w);
        }

        public static Vector3 ConvertRightHandedToLeftHandedVector(Vector3 rightHandedVector)
        {
            // works only for this here used special coordinate definition that only the z axis is flipped
            return new Vector3(rightHandedVector.x, rightHandedVector.y, -rightHandedVector.z);
        }
        public static Vector3 ConvertLeftHandedToRightHandedVector(Vector3 leftHandedVector)
        {
            // works only for this here used special coordinate definition that only the z axis is flipped
            return new Vector3(leftHandedVector.x, leftHandedVector.y, -leftHandedVector.z);
        }

        public static Vector3 ConvertRightHandedToLeftHandedEuler123(Vector3 rightHandedVector)
        {
            // works only for this here used special coordinate definition that only the z axis is flipped
            return new Vector3(rightHandedVector.x, rightHandedVector.y, -rightHandedVector.z);
        }
        public static Vector3 ConvertLeftHandedToRightHandedEuler123(Vector3 leftHandedVector)
        {
            // works only for this here used special coordinate definition that only the z axis is flipped
            return new Vector3(leftHandedVector.x, leftHandedVector.y, -leftHandedVector.z);
        }
        // ############################################################################




        // ############################################################################
        // for printing numbers with desired format but keep place for a + or - sign
        // ############################################################################
        public static string FormatNumber(float number, string format)
        {
            string sign = number < 0 ? "-" : " "; // decide the sign
            return sign + Mathf.Abs(number).ToString(format);
        }
        public static string FormatNumber(double number, string format)
        {
            string sign = number < 0 ? "-" : " "; // decide the sign
            return sign + Math.Abs(number).ToString(format);
        }
        // ############################################################################




        // ############################################################################
        // right handed cross product for vectors 
        // ############################################################################
        public static Vector3 Cross(Vector3 vector1, Vector3 vector2)
        {
            return new Vector3(
                vector1.y * vector2.z - vector1.z * vector2.y,
                vector1.z * vector2.x - vector1.x * vector2.z,
                vector1.x * vector2.y - vector1.y * vector2.x);
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// right handed cross product for vector components
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="z1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="z2"></param>
        /// <returns></returns>
        // ############################################################################
        public static Vector3 Cross(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            return new Vector3(
                y1 * z2 - z1 * y2,
                z1 * x2 - x1 * z2,
                x1 * y2 - y1 * x2);
        }
        // ############################################################################



        #region orientation
        // ############################################################################
        /// <summary>
        /// transformation from the local body reference frame to the earth reference frame (hint: read A_RL from right to left)
        /// </summary>
        /// <param name="q"></param>
        /// <param name="v3"></param>
        /// <returns></returns> 
        /// Quaternion Index [0] [1] [2] [3]
        ///            Unity  x   y   z   w
        ///            ODE    w   x   y   z   !!!   
        ///                   q0  q1  q2  q3  !!! http://liu.diva-portal.org/smash/get/diva2:821251/FULLTEXT01.pdf (2.30)
        // ############################################################################
        public static Vector3 A_RL(Quaternion q, Vector3 v3)
        {
            return new Vector3(
                (2 * q.w * q.w + 2 * q.x * q.x - 1) * v3.x +
                (2 * q.x * q.y - 2 * q.w * q.z) * v3.y +
                (2 * q.x * q.z + 2 * q.w * q.y) * v3.z,
                (2 * q.x * q.y + 2 * q.w * q.z) * v3.x +
                (2 * q.w * q.w + 2 * q.y * q.y - 1) * v3.y +
                (2 * q.y * q.z - 2 * q.w * q.x) * v3.z,
                (2 * q.x * q.z - 2 * q.w * q.y) * v3.x +
                (2 * q.y * q.z + 2 * q.w * q.x) * v3.y +
                (2 * q.w * q.w + 2 * q.z * q.z - 1) * v3.z
               );
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// transformation from earth reference frame to local body reference frame (hint: read A_LR from right to left)
        /// </summary>
        /// <param name="q"></param>
        /// <param name="v3"></param>
        /// <returns></returns>
        /// Quaternion Index [0] [1] [2] [3]
        ///            Unity  x   y   z   w
        ///            ODE    w   x   y   z   !!!   
        ///                   q0  q1  q2  q3  !!! http://liu.diva-portal.org/smash/get/diva2:821251/FULLTEXT01.pdf (2.30) 
        // ############################################################################
        public static Vector3 A_LR(Quaternion q, Vector3 v3)
        {
            return new Vector3(
                (2 * q.w * q.w + 2 * q.x * q.x - 1) * v3.x +
                (2 * q.x * q.y + 2 * q.w * q.z) * v3.y +
                (2 * q.x * q.z - 2 * q.w * q.y) * v3.z,
                (2 * q.x * q.y - 2 * q.w * q.z) * v3.x +
                (2 * q.w * q.w + 2 * q.y * q.y - 1) * v3.y +
                (2 * q.y * q.z + 2 * q.w * q.x) * v3.z,
                (2 * q.x * q.z + 2 * q.w * q.y) * v3.x +
                (2 * q.y * q.z - 2 * q.w * q.x) * v3.y +
                (2 * q.w * q.w + 2 * q.z * q.z - 1) * v3.z
               );
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// short version of A_RL() where in the vector v3 only y is !=0
        /// </summary>
        /// <param name="q"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        /// Quaternion Index [0] [1] [2] [3]
        ///            Unity  x   y   z   w
        ///            ODE    w   x   y   z   !!!   
        ///                   q0  q1  q2  q3  !!! http://liu.diva-portal.org/smash/get/diva2:821251/FULLTEXT01.pdf (2.30)
        // ############################################################################
        public static Vector3 Ay_RL(Quaternion q, float y)
        {
            return new Vector3(
                 (2 * q.x * q.y - 2 * q.w * q.z) * y,
                 (2 * q.w * q.w + 2 * q.y * q.y - 1) * y,
                 (2 * q.y * q.z + 2 * q.w * q.x) * y
               );
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// short version of A_LR() where in the vector v3 only y is !=0
        /// </summary>
        /// <param name="q"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        /// Quaternion Index [0] [1] [2] [3]
        ///            Unity  x   y   z   w
        ///            ODE    w   x   y   z   !!!   
        ///                   q0  q1  q2  q3  !!! http://liu.diva-portal.org/smash/get/diva2:821251/FULLTEXT01.pdf (2.30)
        // ############################################################################
        public static Vector3 Ay_LR(Quaternion q, float y)
        {
            return new Vector3(
                (2 * q.x * q.y + 2 * q.w * q.z) * y,
                (2 * q.w * q.w + 2 * q.y * q.y - 1) * y,
                (2 * q.y * q.z - 2 * q.w * q.x) * y
               );
        }
        // ############################################################################





        // ############################################################################
        /// <summary>
        /// short version of A_LR() where in the vector v3 only y is !=0 and only the y component is returned
        /// </summary>
        /// <param name="q"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        // ############################################################################
        public static float Ayy_LR(Quaternion q, float y)
        {
            return (2 * q.w * q.w + 2 * q.y * q.y - 1) * y;
        }
        // ############################################################################





        // ############################################################################
        /// <summary>
        /// short version of A_RL() where in the vector v3 only z is !=0
        /// </summary>
        /// <param name="q"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        /// Quaternion Index [0] [1] [2] [3]
        ///            Unity  x   y   z   w
        ///            ODE    w   x   y   z   !!!   
        ///                   q0  q1  q2  q3  !!! http://liu.diva-portal.org/smash/get/diva2:821251/FULLTEXT01.pdf (2.30)
        // ############################################################################
        public static Vector3 Az_RL(Quaternion q, float z)
        {
            return new Vector3(
                       (2 * q.x * q.z + 2 * q.w * q.y) * z,
                       (2 * q.y * q.z - 2 * q.w * q.x) * z,
                       (2 * q.w * q.w + 2 * q.z * q.z - 1) * z
                      );
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// short version of A_LR() where in the vector v3 only z is !=0
        /// </summary>
        /// <param name="q"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        /// Quaternion Index [0] [1] [2] [3]
        ///            Unity  x   y   z   w
        ///            ODE    w   x   y   z   !!!   
        ///                   q0  q1  q2  q3  !!! http://liu.diva-portal.org/smash/get/diva2:821251/FULLTEXT01.pdf (2.30)
        // ############################################################################
        public static Vector3 Az_LR(Quaternion q, float z)
        {
            return new Vector3(
             (2 * q.x * q.z - 2 * q.w * q.y) * z,
             (2 * q.y * q.z + 2 * q.w * q.x) * z,
             (2 * q.w * q.w + 2 * q.z * q.z - 1) * z
            );
        }
        // ############################################################################










        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix over x-axis, local to reference frame transformation
        /// </summary>
        /// <param name="phi"></param>
        /// <param name="v3L"></param>
        /// <returns></returns>
        /// https://link.springer.com/book/10.1007/978-3-662-04831-3 (Eq 2.55a - inverse of)
        // ############################################################################
        public static Vector3 Ax_RL(float phi, Vector3 v3L)
        {
            return new Vector3(
            v3L.x,
            Mathf.Cos(phi) * v3L.y - Mathf.Sin(phi) * v3L.z,
            Mathf.Sin(phi) * v3L.y + Mathf.Cos(phi) * v3L.z
            );
        }
        // ############################################################################


        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix over y-axis, local to reference frame transformation
        /// </summary>
        /// <param name="theta"></param>
        /// <param name="v3L"></param>
        /// <returns></returns>
        /// https://link.springer.com/book/10.1007/978-3-662-04831-3 (Eq 2.55b - inverse of)
        // ############################################################################
        public static Vector3 Ay_RL(float theta, Vector3 v3L)
        {
            return new Vector3(
            Mathf.Cos(theta) * v3L.x + Mathf.Sin(theta) * v3L.z,
            v3L.y,
            -Mathf.Sin(theta) * v3L.x + Mathf.Cos(theta) * v3L.z
            );
        }
        // ############################################################################


        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix over z-axis, local to reference frame transformation
        /// </summary>
        /// <param name="psi"></param>
        /// <param name="v3L"></param>
        /// <returns></returns>
        /// https://link.springer.com/book/10.1007/978-3-662-04831-3 (Eq 2.55c - inverse of)
        // ############################################################################
        public static Vector3 Az_RL(float psi, Vector3 v3L)
        {
            return new Vector3(
            Mathf.Cos(psi) * v3L.x - Mathf.Sin(psi) * v3L.y,
            Mathf.Sin(psi) * v3L.x + Mathf.Cos(psi) * v3L.y,
            v3L.z
            );
        }
        // ############################################################################








        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix over x-axis, reference to local frame transformation
        /// </summary>
        /// <param name="phi"></param>
        /// <param name="v3R"></param>
        /// <returns></returns>
        /// https://link.springer.com/book/10.1007/978-3-662-04831-3 (Eq 2.55a)
        // ############################################################################
        public static Vector3 Ax_LR(float phi, Vector3 v3R) // [rad]
        {
            return new Vector3(
            v3R.x,
             Mathf.Cos(phi) * v3R.y + Mathf.Sin(phi) * v3R.z,
            -Mathf.Sin(phi) * v3R.y + Mathf.Cos(phi) * v3R.z
            );
        }
        // ############################################################################


        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix over y-axis, reference to local frame transformation
        /// </summary>
        /// <param name="theta"></param>
        /// <param name="v3R"></param>
        /// <returns></returns>
        /// https://link.springer.com/book/10.1007/978-3-662-04831-3 (Eq 2.55b)
        // ############################################################################
        public static Vector3 Ay_LR(float theta, Vector3 v3R) // [rad]
        {
            return new Vector3(
            Mathf.Cos(theta) * v3R.x - Mathf.Sin(theta) * v3R.z,
            v3R.y,
            Mathf.Sin(theta) * v3R.x + Mathf.Cos(theta) * v3R.z
            );
        }
        // ############################################################################


        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix over z-axis, reference to local frame transformation
        /// </summary>
        /// <param name="psi"></param>
        /// <param name="v3R"></param>
        /// <returns></returns>
        /// https://link.springer.com/book/10.1007/978-3-662-04831-3 (Eq 2.55c)
        // ############################################################################
        public static Vector3 Az_LR(float psi, Vector3 v3R) // [rad]
        {
            return new Vector3(
             Mathf.Cos(psi) * v3R.x + Mathf.Sin(psi) * v3R.y,
            -Mathf.Sin(psi) * v3R.x + Mathf.Cos(psi) * v3R.y,
            v3R.z
            );
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix "body fixed 123(B123) == intrinsic rotation 123 == extrinsic rotation 321 == space fixed 321(S321)": Reference frame to local frame.
        /// </summary>
        /// <param name="phi"></param>      1.) rotation around body fixed x
        /// <param name="theta"></param>    2.) rotation around body fixed y'
        /// <param name="psi"></param>      3.) rotation around body fixed z''
        /// <param name="v3R"></param>
        /// <returns></returns>
        /// https://link.springer.com/book/10.1007/978-3-662-04831-3 (Eq 2.56 (inverse of) )
        // ############################################################################
        public static Vector3 A_LR(float phi, float theta, float psi, Vector3 v3R) // [rad]
        {
            float c1 = Mathf.Cos(phi);
            float c2 = Mathf.Cos(theta);
            float c3 = Mathf.Cos(psi);

            float s1 = Mathf.Sin(phi);
            float s2 = Mathf.Sin(theta);
            float s3 = Mathf.Sin(psi);

            return new Vector3(
                 (c2 * c3) * v3R.x   +   (s3 * c1 + c3 * s2 * s1) * v3R.y   +   (s3 * s1 - c3 * s2 * c1) * v3R.z,
                (-c2 * s3) * v3R.x   +   (c3 * c1 - s3 * s2 * s1) * v3R.y   +   (c3 * s1 + s3 * s2 * c1) * v3R.z,
                (s2)       * v3R.x   +   (-c2 * s1)               * v3R.y   +   (c2 * c1) * v3R.z);
        }
        public static Vector3 A_LR(Vector3 phi_theta_psi, Vector3 v3R)
        {
            return A_LR(phi_theta_psi[0], phi_theta_psi[1], phi_theta_psi[2], v3R);
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix "body fixed 123(B123) == intrinsic rotation 123 == extrinsic rotation 321 == space fixed 321(S321)": Local frame to reference frame.
        /// </summary>
        /// <param name="phi"></param>      1.) rotation around body fixed x
        /// <param name="theta"></param>    2.) rotation around body fixed y'
        /// <param name="psi"></param>      3.) rotation around body fixed z''
        /// <param name="v3L"></param>
        /// <returns></returns>
        /// https://link.springer.com/book/10.1007/978-3-662-04831-3 (Eq 2.56)
        // ############################################################################
        public static Vector3 A_RL(float phi, float theta, float psi, Vector3 v3L) // [rad]
        {
            float c1 = Mathf.Cos(phi);
            float c2 = Mathf.Cos(theta);
            float c3 = Mathf.Cos(psi);

            float s1 = Mathf.Sin(phi);
            float s2 = Mathf.Sin(theta);
            float s3 = Mathf.Sin(psi);

            return new Vector3(
                (c2 * c3)                * v3L.x   +   (-c2 * s3)               * v3L.y   +   (s2) * v3L.z,
                (s3 * c1 + c3 * s2 * s1) * v3L.x   +   (c3 * c1 - s3 * s2 * s1) * v3L.y   +   (-c2 * s1) * v3L.z,
                (s3 * s1 - c3 * s2 * c1) * v3L.x   +   (c3 * s1 + s3 * s2 * c1) * v3L.y   +   (c2 * c1) * v3L.z);
        }
        public static Vector3 A_RL(Vector3 phi_theta_psi, Vector3 v3L)
        {
            return A_RL(phi_theta_psi[0], phi_theta_psi[1], phi_theta_psi[2], v3L);
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix "body fixed 123(B123) == intrinsic rotation 123 == extrinsic rotation 321 == space fixed 321(S321)": Reference frame to local frame.
        /// </summary>
        /// <param name="phi"></param>      1.) rotation around body fixed y
        /// <param name="theta"></param>    2.) rotation around body fixed x'
        /// <param name="psi"></param>      3.) rotation around body fixed z''
        /// <param name="v3R"></param>
        /// <returns></returns>
        // ############################################################################
        public static Vector3 A_LR_B213(float phi, float theta, float psi, Vector3 v3R) // [rad]
        {
            float c1 = Mathf.Cos(phi);
            float c2 = Mathf.Cos(theta);
            float c3 = Mathf.Cos(psi);

            float s1 = Mathf.Sin(phi);
            float s2 = Mathf.Sin(theta);
            float s3 = Mathf.Sin(psi);

            return new Vector3(
                (c3*c2 + s1 * s3 * s2) * v3R.x   +  (c1 * s3) * v3R.y   +   (c2 * s1 * s3 - c3 * s2) * v3R.z,
                (c3*s1 * s2 - c2 * s3) * v3R.x   +  (c1 * c3) * v3R.y   +   (s3 * s2 + c3 * c2 * s1) * v3R.z,
                (c1*s2)                * v3R.x   +  (-s1)     * v3R.y   +   (c1 * c2)                * v3R.z);

        }
        public static Vector3 A_LR_B213(Vector3 phi_theta_psi, Vector3 v3R)
        {
            return A_LR_B213(phi_theta_psi[0], phi_theta_psi[1], phi_theta_psi[2], v3R);
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix "body fixed 123(B123) == intrinsic rotation 123 == extrinsic rotation 321 == space fixed 321(S321)": Local frame to reference frame.
        /// </summary>
        /// <param name="phi"></param>      1.) rotation around body fixed y
        /// <param name="theta"></param>    2.) rotation around body fixed x'
        /// <param name="psi"></param>      3.) rotation around body fixed z''
        /// <param name="v3L"></param>
        /// <returns></returns>
        // ############################################################################
        public static Vector3 A_RL_B213(float phi, float theta, float psi, Vector3 v3L) // [rad]
        {
            float c1 = Mathf.Cos(phi);
            float c2 = Mathf.Cos(theta);
            float c3 = Mathf.Cos(psi);

            float s1 = Mathf.Sin(phi);
            float s2 = Mathf.Sin(theta);
            float s3 = Mathf.Sin(psi);

            return new Vector3(
                (c3*c2 + s1 * s3 * s2)   * v3L.x   +  (c3 * s1 * s2 - c2 * s3) * v3L.y   +   (c1 * s2) * v3L.z,
                (c1 * s3)                * v3L.x   +  (c1 * c3)                * v3L.y   +   (-s1)     * v3L.z,
                (c2 * s1 * s3 - c3 * s2) * v3L.x   +  (s3 * s2 + c3 * c2 * s1) * v3L.y   +   (c1 * c2) * v3L.z);

        }
        public static Vector3 A_RL_B213(Vector3 phi_theta_psi, Vector3 v3L)
        {
            return A_RL_B213(phi_theta_psi[0], phi_theta_psi[1], phi_theta_psi[2], v3L);
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix "space fixed 123 (S123) == extrinsic rotation 123 == intrinsic 321 == body fixed 321 (B321)", reference to local frame
        /// </summary>
        /// <param name="phi"></param>      1.) rotation around space fixed x
        /// <param name="theta"></param>    2.) rotation around space fixed y
        /// <param name="psi"></param>      3.) rotation around space fixed z
        /// <param name="v3R"></param>
        /// <returns></returns>
        // ############################################################################
        public static Vector3 A_LR_S123(float phi, float theta, float psi, Vector3 v3R) // [rad]
        {
            return Az_LR(psi, Ay_LR(theta, Ax_LR(phi, v3R))); // (comment: A_LR_S123 the same as A_LR_B321)
        }
        public static Vector3 A_LR_S123(Vector3 phi_theta_psi, Vector3 v3R) // [rad]
        {
            return Az_LR(phi_theta_psi[2], Ay_LR(phi_theta_psi[1], Ax_LR(phi_theta_psi[0], v3R))); // (comment: A_LR_S123 the same as A_LR_B321)
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix "space fixed 123 (S123) == extrinsic rotation 123 == intrinsic 321 == body fixed 321 (B321)", local to reference frame
        /// </summary>
        /// <param name="phi"></param>      1.) rotation around space fixed x
        /// <param name="theta"></param>    2.) rotation around space fixed y
        /// <param name="psi"></param>      3.) rotation around space fixed z
        /// <param name="v3L"></param>
        /// <returns></returns>
        // ############################################################################
        public static Vector3 A_RL_S123(float phi, float theta, float psi, Vector3 v3L) // [rad]
        {
            return Az_RL(psi, Ay_RL(theta, Ax_RL(phi, v3L))); // (comment: A_RL_S123 the same as A_RL_B321)
        }
        public static Vector3 A_RL_S123(Vector3 phi_theta_psi, Vector3 v3L) // [rad]
        {
            return Az_RL(phi_theta_psi[2], Ay_RL(phi_theta_psi[1], Ax_RL(phi_theta_psi[0], v3L))); // (comment: A_RL_S123 the same as A_RL_B321)
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// Euler's "body fixed 321 (B321) == intrinsic rotation 321 == extrinsic 123 == space fixed 123 (S123)" rotation conversion to quaternions - right handed
        /// Local to Reference --> A_B321_RL
        /// </summary>
        /// <param name="psi-deg"></param>
        /// <param name="theta-deg"></param>
        /// <param name="phi-deg"></param>
        /// <returns></returns>
        // ############################################################################
        public static Quaternion B321toQuat(float phi, float theta, float psi) // [deg]
        {
            float c1 = Mathf.Cos(phi * Mathf.Deg2Rad / 2);
            float c2 = Mathf.Cos(theta * Mathf.Deg2Rad / 2);
            float c3 = Mathf.Cos(psi * Mathf.Deg2Rad / 2);

            float s1 = Mathf.Sin(phi * Mathf.Deg2Rad / 2);
            float s2 = Mathf.Sin(theta * Mathf.Deg2Rad / 2);
            float s3 = Mathf.Sin(psi * Mathf.Deg2Rad / 2);

            return new Quaternion(
                s1 * c2 * c3 - c1 * s2 * s3,
                c1 * s2 * c3 + s1 * c2 * s3,
                c1 * c2 * s3 - s1 * s2 * c3,
                c1 * c2 * c3 + s1 * s2 * s3
                ); // [x y z w]

        }
        /// <summary>
        /// Euler's "body fixed 321 (B321) == intrinsic rotation 321 == extrinsic 123 == space fixed 123 (S123)" rotation conversion to quaternions - right handed
        /// Local to Reference --> A_B321_RL
        /// </summary>
        /// <param name="angles"></param>
        /// <returns></returns>
        public static Quaternion B321toQuat(Vector3 angles) // [deg]
        {
            return B321toQuat(angles.x, angles.y, angles.z);
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// quaternion from right handed euler "space fixed 123 (S123) == extrinsic rotation 123 == intrinsic rotation 321 == body fixed 321 (B321)" 
        /// (untiy's standard is (with Quaternion.Euler()) S312 == B213, left handed, space fixed) 
        /// Local to Reference --> A_B321_RL
        /// </summary>
        /// <param name="angles"></param>
        /// <returns></returns>
        // ############################################################################
        /// https://www.astro.rug.nl/software/kapteyn-beta/_downloads/attitude.pdf (Eq. 297, but he uses wxyz not xyzw to store quaternion)
        public static Quaternion S123toQuat(Vector3 angles) // [deg]
        {
            return B321toQuat(angles.x, angles.y, angles.z);
        }
        // ############################################################################



        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix over z-axis, reference to local frame transformation
        /// </summary>
        /// <param name="psi"></param>
        /// <param name="v3R"></param>
        /// <returns></returns>
        /// https://link.springer.com/book/10.1007/978-3-662-04831-3 (Eq 2.55b)
        // ############################################################################
        public static Matrix4x4 Az_LR(float psi) // [rad]
        {
            float c3 = Mathf.Cos(psi);
            float s3 = Mathf.Sin(psi);
            Matrix4x4 m = new Matrix4x4();

            // m = Matrix4x4.Rotate(Quaternion.Euler(0, 0, psi)).transpose
            // [cos(psi) sin(psi) 0;    [00 10 20 30]    [00 01 02 03]
            // -sin(psi) cos(psi) 0;    [01 11 21 31]    [10 11 12 13]
            // 0         0        1]    [02 12 22 32]    [20 21 22 23] 
            //                          [03 13 23 33]    [30 31 32 33]
            m.m00 = c3; m.m10 = -s3; m.m01 = s3; m.m11 = c3; m.m22 = 1; m.m33 = 1;
            return m;
        }
        // ############################################################################


        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix over y-axis, reference to local frame transformation
        /// </summary>
        /// <param name="theta"></param>
        /// <param name="v3R"></param>
        /// <returns></returns>
        /// https://link.springer.com/book/10.1007/978-3-662-04831-3 (Eq 2.55b)
        // ############################################################################
        public static Matrix4x4 Ay_LR(float theta) // [rad]
        {
            float c2 = Mathf.Cos(theta);
            float s2 = Mathf.Sin(theta);
            Matrix4x4 m = new Matrix4x4();

            // m = Matrix4x4.Rotate(Quaternion.Euler(0, theta, 0)).transpose
            // [cos(theta) 0 -sin(theta);    [00 10 20 30]    [00 01 02 03]
            // 0           1           0;    [01 11 21 31]    [10 11 12 13]
            // sin(theta)  0  cos(theta)]    [02 12 22 32]    [20 21 22 23] 
            //                               [03 13 23 33]    [30 31 32 33]
            m.m00 = c2; m.m20 = s2; m.m11 = 1; m.m02 = -s2; m.m22 = c2; m.m33 = 1;
            return m;
        }
        // ############################################################################


        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix over z-axis, reference to local frame transformation
        /// </summary>
        /// <param name="psi"></param>
        /// <param name="v3R"></param>
        /// <returns></returns>
        /// https://link.springer.com/book/10.1007/978-3-662-04831-3 (Eq 2.55c)
        // ############################################################################
        public static Matrix4x4 Ax_LR(float phi) // [rad]
        {
            float c1 = Mathf.Cos(phi);
            float s1 = Mathf.Sin(phi);
            Matrix4x4 m = new Matrix4x4();

            // m = Matrix4x4.Rotate(Quaternion.Euler(phi, 0, 0)).transpose
            // [1         0        0;    [00 10 20 30]    [00 01 02 03]
            //  0  cos(phi) sin(phi);    [01 11 21 31]    [10 11 12 13]
            //  0 -sin(phi) cos(phi)]    [02 12 22 32]    [20 21 22 23] 
            //                           [03 13 23 33]    [30 31 32 33]
            m.m00 = 1; m.m11 = c1; m.m21 = -s1; m.m12 = s1; m.m22 = c1; m.m33 = 1;
            return m;
        }
        // ############################################################################



        // ############################################################################
        /// <summary>
        /// right-hand rule rotation matrix from three orthogonal nomal vectors
        /// transforms vector from local to reference frame
        /// </summary>
        /// <param name="psi"></param>
        /// <returns></returns>
        // ############################################################################
        public static Matrix4x4 A_RL(Vector3 e_xL_R, Vector3 e_yL_R, Vector3 e_zL_R)
        {
            Matrix4x4 m = new Matrix4x4();

            e_xL_R = e_xL_R.normalized;
            e_yL_R = e_yL_R.normalized;
            e_zL_R = e_zL_R.normalized;

            // m = Matrix4x4.Rotate(Quaternion.Euler(0, 0, psi)).transpose
            // [e_xL_R(x) e_yL_R(x) e_zL_R(x);    [00 10 20 30]    [00 01 02 03]
            //  e_xL_R(y) e_yL_R(y) e_zL_R(y);    [01 11 21 31]    [10 11 12 13]
            //  e_xL_R(z) e_yL_R(z) e_zL_R(z)]    [02 12 22 32]    [20 21 22 23] 
            //                                    [03 13 23 33]    [30 31 32 33]
            m.m00 = e_xL_R.x; m.m01 = e_yL_R.x; m.m02 = e_zL_R.x;
            m.m10 = e_xL_R.y; m.m11 = e_yL_R.y; m.m12 = e_zL_R.y;
            m.m20 = e_xL_R.z; m.m21 = e_yL_R.z; m.m22 = e_zL_R.z;
            m.m33 = 1;
            return m;
        }
        // ############################################################################



        // ############################################################################
        // create quaternon from 3x3 transformation matrix, that is stored as 9x1 array in row-major order
        // ############################################################################
        public static Quaternion QuaternionFromMatrix9x1(float[] m)
        {
            // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0] + m[4] + m[8])) / 2; // row-major order (9x1)
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0] - m[4] - m[8])) / 2;
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0] + m[4] - m[8])) / 2;
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0] - m[4] + m[8])) / 2;
            q.x *= Mathf.Sign(q.x * (m[7] - m[5]));
            q.y *= Mathf.Sign(q.y * (m[2] - m[6]));
            q.z *= Mathf.Sign(q.z * (m[3] - m[1]));
            return q;
        }
        // ############################################################################




        // ############################################################################
        // converting Matrix4x4 to Quaternion 
        // ############################################################################
        // https://answers.unity.com/questions/11363/converting-matrix4x4-to-quaternion-vector3.html
        public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
        {
            // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
            q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
            q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
            q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
            return q;
        }
        // ############################################################################


        // ############################################################################
        // skew-symmetric matrix 
        // ############################################################################
        public static Matrix4x4 tilde_omega(Vector3 w)
        {
            //  member variables |      indices         https://answers.unity.com/questions/1359718/what-do-the-values-in-the-matrix4x4-for-cameraproj.html?childToView=1359877#answer-1359877
            // ------------------|-----------------
            // m00 m01 m02 m03   |   00  04  08  12      0  -wz  wy   0
            // m10 m11 m12 m13   |   01  05  09  13      wz   0  -wx  0
            // m20 m21 m22 m23   |   02  06  10  14     -wy  wx  0    0
            // m30 m31 m32 m33   |   03  07  11  15      0    0   0   1 
            Matrix4x4 m = new Matrix4x4();
            m.m01 = -w.z;
            m.m02 = w.y;
            m.m10 = w.z;
            m.m12 = -w.x;
            m.m20 = -w.y;
            m.m21 = w.x;
            m.m33 = 1;
            return m;
        }

        // ############################################################################



        // ############################################################################
        /// Normalize Quaternion (double)
        /// <summary>
        /// unity:   
        ///     x,    [0] quaternion orientation imag i
        ///     y,    [1] quaternion orientation imag j
        ///     z,    [2] quaternion orientation imag k
        ///     w,    [3] quaternion orientation real
        /// phyiscs: 
        ///     q0;    w  quaternion orientation real
        ///     q1;    x  quaternion orientation imag i
        ///     q2;    y  quaternion orientation imag j
        ///     q3;    z  quaternion orientation imag k
        /// </summary>
        /// <param name="q0"></param>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <param name="q3"></param>
        // ############################################################################
        public static void QuaternionNormalize(ref double q0, ref double q1, ref double q2, ref double q3)
        {
            double d = Math.Sqrt(q0*q0 + q1*q1 + q2*q2 + q3*q3);
            q0 /= d;
            q1 /= d;
            q2 /= d;
            q3 /= d;
        }
        // ############################################################################






        /*
        // ############################################################################
        /// <summary>
        /// special function to gerenrate transformation matrix of "non rotating" rotor disk - needed to calculate local velocities in each blade element (meshed disk)
        /// </summary>
        /// <returns></returns>
        // ############################################################################
        public static Vector3 A_RT_BEMT(Quaternion qCO, Quaternion qTO, Vector3 anglesRC, Vector3 v3, out float tilting_angle, out Vector3 axis_vector)
        {
            // rotating rotor disk (3 degree of freedom - TTP) y-axis expressed in world origin frame
            Vector3 ey_TO__O = Helper.Ay_RL(qTO, 1);   

            //Vector3 ey_TO__R  =  A_RC_B321(anglesRC, A_CO_q( qCO, ey_TO__O )); 
            Vector3 ey_TO__R = A_LR_S123(anglesRC, A_LR(qCO, ey_TO__O)); // expressed in rotor frame
            
            
            // Construct a transformation matrix, that does NOT rotate with the rotor-TTP, but tilts.
            //
            //    Given is the normalized basis vector ey_L__R of a local frame L, expressed in reference frame R.
            //    Requirement: The ex_L__R component of the frame L should align with x_R, if viewing at the coordinate system along the y_R axis as shown in figure below.
            //    Goal: Form the transformation matrix of A_RL
            //    Thus given conditions are: 
            //      1.) ex_L__R(z) == 0
            //      2.) ex_L__R perpendicular to ey_L__R ==> ex_L__R . ey_L__R = 0
            //      3.) ex_L__R / norm(ex_L__R) = 1
            //
            //    solve([bz == 0, ax*bx + ay*by + az*bz == 0, sqrt(bx^2 + by^2) == 1],[bx by,bz])
            //    ==>
            //      ex_L__R(x) = +ey_L__R(y) / (ey_L__R(x)^2 + ey_L__R(y)^2)^(1/2)
            //      ex_L__R(y) = -ey_L__R(x) / (ey_L__R(x)^2 + ey_L__R(y)^2)^(1/2)
            //      ex_L__R(z) = 0
            //
            //
            //           ez_L__R
            //    ez_R  <----___
            //    <---------------O  ey_R
            //                   ||\
            //                   || \
            //                   ||  \ 
            //                   ||   v  ey_L__R
            //         ex_L__R   v|
            //                    |
            //                    v ey_R
            Vector3 ex_TO__R;
            float denominator = Mathf.Sqrt(ey_TO__R.x * ey_TO__R.x + ey_TO__R.y * ey_TO__R.y); // can't become zero
            ex_TO__R.x = +ey_TO__R.y / denominator;
            ex_TO__R.y = -ey_TO__R.x / denominator;
            ex_TO__R.z = 0;

            // Add here angle-axis of tilting for torque calculation (due to stiffenss / O-ring)
            // https://www.euclideanspace.com/maths/algebra/vectors/angleBetween/
            // - The angle is given by acos of the dot product of the two(normalised) vectors: v1•v2 = | v1 || v2 | cos(angle)
            // - The axis is given by the cross product of the two vectors, the length of this axis is given by | v1 x v2| = | v1 || v2 | sin(angle).
            tilting_angle = Mathf.Acos(Helper.Dot(0f, 1f, 0f, ey_TO__R.x, ey_TO__R.y, ey_TO__R.z)); // "R" is here the rotor-frame, y-axis is the rotation axis of the rotor shaft
            axis_vector = Helper.Cross(new Vector3(0f, 1f, 0f), ey_TO__R.normalized); // TODO: catch if both vector are parallel

            // transformation matrix's third vector
            Vector3 ez_TO__R = Helper.Cross(ex_TO__R, ey_TO__R);
            // transformation matrix "A_RT"
            return new Vector3(ex_TO__R.x * v3.x + ey_TO__R.x * v3.y + ez_TO__R.x * v3.z, 
                               ex_TO__R.y * v3.x + ey_TO__R.y * v3.y + ez_TO__R.y * v3.z,
                               ex_TO__R.z * v3.x + ey_TO__R.z * v3.y + ez_TO__R.z * v3.z );
        }
        // ############################################################################
        */
        #endregion



        // ############################################################################ 
        /// <summary>
        /// translational velocity of a local point expressed in world coordinates (right handed)
        /// </summary>
        /// <param name="translational_velocity_R"></param>
        /// <param name="rotational_velocity_omega_L"></param>
        /// <param name="point_position_L"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        // ############################################################################
        public static Vector3 Translational_Velocity_At_Local_Point_Expressed_In_Global_Frame(Vector3 translational_velocity_R,
                                                                      Vector3 rotational_velocity_omega_L,
                                                                      Vector3 point_position_L,
                                                                      Quaternion q)

        {
            return translational_velocity_R + Helper.A_RL(q, Helper.Cross(rotational_velocity_omega_L, point_position_L));
        }
        // ############################################################################



        // ############################################################################
        /// <summary>
        /// rotational velocity of a any rigid body point expressed in world coordinates (right handed)
        /// Hint: All points on a rigid body experience the same angular velocity at all times.
        /// </summary>
        /// <param name="rotational_velocity_omega_L"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        // ############################################################################
        public static Vector3 Rotational_Velocity_Expressed_In_Global_Frame(Vector3 rotational_velocity_omega_L,
                                                                             Quaternion q)

        {
            return Helper.A_RL(q, rotational_velocity_omega_L);
        }
        // ############################################################################






        // ############################################################################
        // dot product with doubles
        // ############################################################################
        public static double Dot(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return x1 * x2 + y1 * y2 + z1 * z2;
        }
        // ############################################################################
        // ############################################################################
        // dot product with float
        // ############################################################################
        public static float Dot(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            return x1 * x2 + y1 * y2 + z1 * z2;
        }
        // ############################################################################





        // ############################################################################ 
        /// <summary>
        /// projection of a vector onto a plane (right handed)
        /// </summary>
        // ############################################################################
        public static Vector3 Projection_Of_A_Vector_Onto_A_Plane(Vector3 vector_to_be_projeced,
                                                                  Vector3 plane_normal_vector)
        {
            // https://www.maplesoft.com/support/help/Maple/view.aspx?path=MathApps/ProjectionOfVectorOntoPlane
            // proj_plane(u) = u - proj_n(u) = u - ( dot(u,n) / ||n||^2 ) * n 
            // ||n||^2 = 1, if normal is already normalizied to lenght 1 --> proj_plane(u) = u - ( dot(u,n) ) * n 
            Vector3 n = plane_normal_vector.normalized;
            return vector_to_be_projeced - Vector3.Dot(vector_to_be_projeced, n) * n;
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// Two point form of a line.
        ///  y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1;
        ///  https://mathworld.wolfram.com/Two-PointForm.html
        /// </summary>
        /// <param name="x"></param>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        // ############################################################################
        public static double Two_Point_Form_Of_Line(double x, double x1, double x2, double y1, double y2)
        {
            return ((y2 - y1) / (x2 - x1)) * (x - x1) + y1;
        }
        public static float Two_Point_Form_Of_Line(float x, float x1, float x2, float y1, float y2)
        {
            return ((y2 - y1) / (x2 - x1)) * (x - x1) + y1;
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// limiting function for a value similar to Mathf.Clamp but with a smooth transition
        /// limits input value "x" to +-"limit". A smooth transition range can be defined with "limit_transition". limit_transition is the measured distance from limit value.
        /// example 1: Limit_Symetric(time, 50, 0.001) gives a sharp corner
        /// example 2: Limit_Symetric(time, 50, 20) gives a smooth corner similiar to a tanh-function
        /// </summary>
        /// <param name="x"></param>
        /// <param name="limit"></param>
        /// <param name="limit_transition"></param>
        /// <returns></returns>
        // ############################################################################
        public static double Limit_Symetric(double x, double limit, double limit_transition)
        {
            double y = 0;
            if (x < (-limit) - limit_transition)
            {
                y = -limit;
            }
            else
            {
                if (x < ((-limit) + limit_transition))
                {
                    double m = 1;
                    double x1 = (-limit) - limit_transition; double y1 = (-limit); double x2 = (-limit) + limit_transition;
                    double a = -m / (2.0 * (x1 - x2)); double b = (m * x1) / (x1 - x2); double c = -(m * x1 * x1 - 2.0 * y1 * x1 + 2.0 * x2 * y1) / (2.0 * (x1 - x2));
                    y = a * x * x + b * x + c;
                }
                else
                {
                    if (x <= (limit - limit_transition))
                    {
                        y = x; // A_linear
                    }
                    else
                    {
                        if (x < limit + limit_transition)
                        {
                            double m = 1;
                            double x1 = limit + limit_transition; double y1 = limit; double x2 = limit - limit_transition;
                            double a = -m / (2.0f * (x1 - x2)); double b = (m * x1) / (x1 - x2); double c = -(m * x1 * x1 - 2.0 * y1 * x1 + 2.0 * x2 * y1) / (2 * (x1 - x2));
                            y = a * x * x + b * x + c;
                        }
                        else
                        {
                            if (x >= limit)
                                y = limit;
                        }
                    }
                }
            }
            return y;
        }
        // ############################################################################




        // ############################################################################ 
        /// <summary>
        /// Step-method approximates a step function with a cubic polynominal
        /// ^ h          x1,h1   
        /// |            +________
        /// |           /        
        /// |          /          
        /// |---------+ 
        /// |          x0,h0
        /// 0-------------------------------------> x 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="x0"></param>
        /// <param name="h0"></param>
        /// <param name="x1"></param>
        /// <param name="h1"></param>
        /// <returns></returns>
        // ############################################################################
        public static float Step(float x, float x0, float h0, float x1, float h1)
        {
            // x specifies the independen variable
            // x0 specifies the x value at which the step function begins
            // h0 specifies the value of the function before the step
            // x1 specifies the x value at which the step function ends
            // h1 specifies the value of the function after the step
            if (x <= x0)
            {
                return h0;
            }
            else
            {
                if (x0 < x && x < x1)
                {
                    float a = (x - x0) / (x1 - x0);
                    return h0 + (h1 - h0) * a * a * (3 - (2 * a));
                }
                else
                {
                    return h1;
                }
            }
        }
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// Two steps-method approximates two steps function with cubic polynominal
        /// ^ h          x1,h1    x2,(h1)
        /// |            +-------+
        /// |           /         \
        /// |          /           +-------
        /// |---------+             x3,h3
        /// |          x0,h0       
        /// 0-------------------------------------> x
        /// </summary>
        /// <param name="x"></param>
        /// <param name="x0"></param>
        /// <param name="h0"></param>
        /// <param name="x1"></param>
        /// <param name="h1"></param>
        /// <param name="x2"></param>
        /// <param name="x3"></param>
        /// <param name="h3"></param>
        /// <returns></returns>
        // ############################################################################
        public static float Step(float x, float x0, float h0, float x1, float h1, float x2, float x3, float h3)
        {
            // x specifies the independen variable
            // x0 specifies the x value at which the first step function begins
            // h0 specifies the value of the function before the first step
            // x1 specifies the x value at which the first step function ends
            // h1 specifies the value of the function after the first step 
            // x2 specifies the x value at which the second step function begins 
            // h2 == h1
            // x3 specifies the x value at which the second function ends
            // h3 specifies the value of the function after the second step 

            // x0 < x1 < x2 < x3 
            return Step(x, x0, h0, x1, h1) * Step(x, x2, 1.0f, x3, h3 / h1);
        }
        // ############################################################################



        // ############################################################################
        // Lerp in 2d-array 
        // ############################################################################
        public static float Interpolate(ref float[,] data, float x)
        {
            // data organized as culumn major order
            // [m00 m01 m02 m03 m04]     [00 02 04 06 08]
            // [m10 m11 m12 m13 m14]     [01 03 05 07 09] 
            for (var i = 1; i < data.GetLength(1); i++)
            {  
                if(!(x >= data[0, i]))
                {
                    return Two_Point_Form_Of_Line(x, data[0, i - 1], data[0, i + 0], data[1, i - 1], data[1, i + 0]);
                }
            }
            return 0;
        } 




        // ############################################################################
        // draws a line during simulation (similar to Unity's built in debug lines, but these aren't displayed in finish build)
        // ############################################################################
        #region draw_line
        // ############################################################################
        //// draws a line during simulation (similar to Unity's built in debug lines, but these aren't displayed in finish build)
        //// https://answers.unity.com/questions/8338/how-to-draw-a-line-using-script.html?_ga=2.112274628.118596743.1580121995-2133810121.1564613509
        //public static void Draw_Line(Vector3 start, Vector3 end, Color color, float duration = 0.01f)
        //{
        //    GameObject myLine = new GameObject();
        //    myLine.transform.position = start;
        //    myLine.AddComponent<LineRenderer>();
        //    LineRenderer lr = myLine.GetComponent<LineRenderer>();
        //    lr.material = new Material(Shader.Find("Sprites/Default"));
        //    lr.startColor = color;
        //    lr.endColor = color;
        //    lr.startWidth = 0.01f;
        //    lr.endWidth = 0.01f;
        //    lr.SetPosition(0, start);
        //    lr.SetPosition(1, end);
        //    GameObject.Destroy(myLine, duration);
        //}
        // ############################################################################




        // ############################################################################
        /// <summary>
        /// draws a line during simulation (similar to Unity's built in debug lines, but these aren't displayed in finish build)
        /// </summary>
        /// <param name="ui_debug_lines"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        // ############################################################################
        public static GameObject Create_Line(Transform ui_debug_lines, Color color)
        {
            GameObject myLine = new GameObject();
            myLine.transform.parent = ui_debug_lines.transform;
            myLine.name = "ODEDebug line_object";
            myLine.transform.position = Vector3.zero;
            myLine.AddComponent<LineRenderer>();
            LineRenderer lr = myLine.GetComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = 0.005f;
            lr.endWidth = 0.005f;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, Vector3.zero);
            return myLine;
        }

        // update line 
        public static void Update_Line(GameObject myLine, Vector3 start, Vector3 end)
        {
            myLine.GetComponent<LineRenderer>().SetPosition(0, start);
            myLine.GetComponent<LineRenderer>().SetPosition(1, end);
        }

        // deleta all lines that are instantiated under "Canvas"/"UI Debug Panel"/"Debug_Lines"
        public static void Destroy_Lines(Transform ui_debug_lines)
        {
            // remove all children form Scroll View Content UI
            foreach (Transform child in ui_debug_lines.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        // ############################################################################
        #endregion



        // ############################################################################
        // resize a list
        // ############################################################################
        #region resize_list
        // https://stackoverflow.com/questions/12231569/is-there-in-c-sharp-a-method-for-listt-like-resize-in-c-for-vectort (puradox)
        //    list: List<T> to resize
        //    size: desired new size
        // element: default value to insert
        public static void Resize_List<T>(this List<T> list, int size, T element = default(T))
        {
            int count = list.Count;

            if (size < count)
            {
                list.RemoveRange(size, count - size);
            }
            else if (size > count)
            {
                if (size > list.Capacity)   // Optimization
                    list.Capacity = size;

                list.AddRange(Enumerable.Repeat(element, size - count));
            }
        }
        // ############################################################################
        #endregion



        // ############################################################################
        // copy the content of a directory
        // ############################################################################
        #region copy_directory
        // https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        public static void Directory_Copy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    Directory_Copy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
        #endregion



        // ############################################################################
        // write XML 
        // ############################################################################
        #region xml_io
        public static void IO_XML_Serialize(object item, string path, bool override_flag = false)
        {
            XmlSerializer serializer;

            //if (override_flag)
            //{
            //    var attributes = new XmlAttributes { XmlIgnore = true };
            //    var overrides = new XmlAttributeOverrides();
            //    overrides.Add(item.GetType(), "transmitter/countdown_minutes", attributes);  // 
            //    overrides.Add(item.GetType(), "helicopter", attributes);

            //    serializer = new XmlSerializer(item.GetType(), overrides);
            //}
            //else
            //  serializer = new XmlSerializer(item.GetType());

            serializer = new XmlSerializer(item.GetType());
            StreamWriter writer = new StreamWriter(path);
            serializer.Serialize(writer.BaseStream, item);
            writer.Close();
        }

        // read XML from folder
        public static T IO_XML_Deserialize<T>(string path)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(path);

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StringReader reader = new StringReader(xmlDocument.OuterXml);// reader.BaseStream);
            T deserialized = (T)serializer.Deserialize(reader);
            reader.Close();
            return deserialized;
        }

        // read XML from "Resources"
        public static T IO_XML_Deserialize<T>(TextAsset textAsset)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StringReader reader = new StringReader(textAsset.text);
            T deserialized = (T)serializer.Deserialize(reader);
            reader.Close();
            return deserialized;
        }
        // ############################################################################
        #endregion



        // ############################################################################
        // merge XML - CODE by Wizbit
        //  https://stackoverflow.com/questions/1892336/merge-two-xelements
        // ############################################################################
        #region XmlMerging
        /// <summary>
        /// Provides facilities to merge 2 XElement or XML files. 
        /// <para>
        /// Where the LHS holds an element with non-element content and the RHS holds 
        /// a tree, the LHS non-element content will be applied as text and the RHS 
        /// tree ignored. 
        /// </para>
        /// <para>
        /// This does not handle anything other than element and text nodes (infact 
        /// anything other than element is treated as text). Thus comments in the 
        /// source XML are likely to be lost.
        /// </para>
        /// <remarks>You can pass <see cref="XDocument.Root"/> if it you have XDocs 
        /// to work with:
        /// <code>
        /// XDocument mergedDoc = new XDocument(MergeElements(lhsDoc.Root, rhsDoc.Root);
        /// </code></remarks>
        /// </summary>
        public class XmlMerging
        {
            /// <summary>
            /// Produce an XML file that is made up of the unique data from both
            /// the LHS file and the RHS file. Where there are duplicates the LHS will 
            /// be treated as master
            /// </summary>
            /// <param name="lhsPath">XML file to base the merge off. This will override 
            /// the RHS where there are clashes</param>
            /// <param name="rhsPath">XML file to enrich the merge with</param>
            /// <param name="resultPath">The fully qualified file name in which to 
            /// write the resulting merged XML</param>
            /// <param name="options"> Specifies the options to apply when saving. 
            /// Default is <see cref="SaveOptions.OmitDuplicateNamespaces"/></param>
            public static bool TryMergeXmlFiles(string lhsPath, string rhsPath,
                string resultPath, SaveOptions options = SaveOptions.OmitDuplicateNamespaces)
            {
                try
                {
                    MergeXmlFiles(lhsPath, rhsPath, resultPath);
                }
                catch (Exception)
                {
                    // could integrate your logging here
                    return false;
                }
                return true;
            }

            /// <summary>
            /// Produce an XML file that is made up of the unique data from both the LHS
            /// file and the RHS file. Where there are duplicates the LHS will be treated 
            /// as master
            /// </summary>
            /// <param name="lhsPath">XML file to base the merge off. This will override 
            /// the RHS where there are clashes</param>
            /// <param name="rhsPath">XML file to enrich the merge with</param>
            /// <param name="resultPath">The fully qualified file name in which to write 
            /// the resulting merged XML</param>
            /// <param name="options"> Specifies the options to apply when saving. 
            /// Default is <see cref="SaveOptions.OmitDuplicateNamespaces"/></param>
            public static void MergeXmlFiles(string lhsPath, string rhsPath,
                string resultPath, SaveOptions options = SaveOptions.OmitDuplicateNamespaces)
            {
                XElement result =
                    MergeElements(XElement.Load(lhsPath), XElement.Load(rhsPath));
                result.Save(resultPath, options);
            }

            /// <summary>
            /// Produce a resulting <see cref="XElement"/> that is made up of the unique 
            /// data from both the LHS element and the RHS element. Where there are 
            /// duplicates the LHS will be treated as master
            /// </summary>
            /// <param name="lhs">XML Element tree to base the merge off. This will 
            /// override the RHS where there are clashes</param>
            /// <param name="rhs">XML element tree to enrich the merge with</param>
            /// <returns>A merge of the left hand side and right hand side element 
            /// trees treating the LHS as master in conflicts</returns>
            public static XElement MergeElements(XElement lhs, XElement rhs)
            {
                // if either of the sides of the merge are empty then return the other... 
                // if they both are then we return null
                if (rhs == null) return lhs;
                if (lhs == null) return rhs;

                // Otherwise build a new result based on the root of the lhs (again lhs 
                // is taken as master)
                XElement result = new XElement(lhs.Name);

                MergeAttributes(result, lhs.Attributes(), rhs.Attributes());

                // now add the lhs child elements merged to the RHS elements if there are any
                MergeSubElements(result, lhs, rhs);
                return result;
            }

            /// <summary>
            /// Enrich the passed in <see cref="XElement"/> with the contents of both 
            /// attribute collections.
            /// Again where the RHS conflicts with the LHS, the LHS is deemed the master
            /// </summary>
            /// <param name="elementToUpdate">The element to take the merged attribute 
            /// collection</param>
            /// <param name="lhs">The master set of attributes</param>
            /// <param name="rhs">The attributes to enrich the merge</param>
            private static void MergeAttributes(XElement elementToUpdate,
                IEnumerable<XAttribute> lhs, IEnumerable<XAttribute> rhs)
            {
                // Add in the attribs of the lhs... we will only add new attribs from 
                // the rhs duplicates will be ignored as lhs is master
                elementToUpdate.Add(lhs);

                // collapse the element names to save multiple evaluations... also why 
                // we ain't putting this in as a sub-query
                List<XName> lhsAttributeNames =
                    lhs.Select(attribute => attribute.Name).ToList();
                // so add in any missing attributes
                elementToUpdate.Add(rhs.Where(attribute =>
                    !lhsAttributeNames.Contains(attribute.Name)));
            }

            /// <summary>
            /// Enrich the passed in <see cref="XElement"/> with the contents of both 
            /// <see cref="XElement.Elements()"/> subtrees.
            /// Again where the RHS conflicts with the LHS, the LHS is deemed the master.
            /// Where the passed elements do not have element subtrees, but do have text 
            /// content that will be used. Again the LHS will dominate
            /// </summary>
            /// <remarks>Where the LHS has text content and no subtree, but the RHS has 
            /// a subtree; the LHS text content will be used and the RHS tree ignored. 
            /// This may be unexpected but is consistent with other .NET XML 
            /// operations</remarks>
            /// <param name="elementToUpdate">The element to take the merged element 
            /// collection</param>
            /// <param name="lhs">The element from which to extract the master 
            /// subtree</param>
            /// <param name="rhs">The element from which to extract the subtree to 
            /// enrich the merge</param>
            private static void MergeSubElements(XElement elementToUpdate,
                XElement lhs, XElement rhs)
            {
                // see below for the special case where there are no children on the LHS
                if (lhs.Elements().Count() > 0)
                {
                    // collapse the element names to a list to save multiple evaluations...
                    // also why we ain't putting this in as a sub-query later
                    List<XName> lhsElementNames =
                        lhs.Elements().Select(element => element.Name).ToList();

                    // Add in the elements of the lhs and merge in any elements of the 
                    //same name on the RHS
                    elementToUpdate.Add(
                        lhs.Elements().Select(
                            lhsElement =>
                                MergeElements(lhsElement, rhs.Element(lhsElement.Name))));

                    // so add in any missing elements from the rhs
                    elementToUpdate.Add(rhs.Elements().Where(element =>
                        !lhsElementNames.Contains(element.Name)));
                }
                else
                {
                    // special case for elements where they have no element children 
                    // but still have content:
                    // use the lhs text value if it is there
                    if (!string.IsNullOrEmpty(lhs.Value))
                    {
                        elementToUpdate.Value = lhs.Value;
                    }
                    // if it isn't then see if we have any children on the right
                    else if (rhs.Elements().Count() > 0)
                    {
                        // we do so shove them in the result unaltered
                        elementToUpdate.Add(rhs.Elements());
                    }
                    else
                    {
                        // nope then use the text value (doen't matter if it is empty 
                        //as we have nothing better elsewhere)
                        elementToUpdate.Value = rhs.Value;
                    }
                }
            }
        }
        // ############################################################################
        #endregion





        // ############################################################################
        // contact search for point to triangle contacts
        // ############################################################################
        #region point_to_triangle_contact

        public struct stru_AABB
        {
            public Vector3 c; // center point of AABB
            public Vector3 r; // radius of halfwidth extents (rx,ry,rz)
        }

        public static int TestAABBAABB(stru_AABB a, stru_AABB b)
        {
            if (Mathf.Abs(a.c[0] - b.c[0]) > (a.r[0] + b.r[0])) return 0;
            if (Mathf.Abs(a.c[1] - b.c[1]) > (a.r[1] + b.r[1])) return 0;
            if (Mathf.Abs(a.c[2] - b.c[2]) > (a.r[2] + b.r[2])) return 0;
            return 1;
        }

        public class GroundMesh
        {
            public Vector3[] vertices { set; get; } // every triangle has its own verticies! 
            public Vector3[] normals { set; get; }
            public stru_AABB[] aabb { set; get; }
        }

        public class contact_info
        {
            public bool flag_has_contact { get; set; }
            public Vector3 normalR { get; set; }
            public float penetration { get; set; }

            public contact_info()
            {
                flag_has_contact = false;
                normalR = new Vector3(0f, 1f, 0f); // in world space
                penetration = 1000f; //[m]
            }
        }
        // ############################################################################
        #endregion





        // ############################################################################
        // Axis-aligned Bounding Boxes 
        // this box is centered square at the helicotpers center of mass
        // http://www.r-5.org/files/books/computers/algo-list/realtime-3d/Christer_Ericson-Real-Time_Collision_Detection-EN.pdf
        // list_pointsLH: collison points, expressed in helicotper's local coordinate system
        // ############################################################################
        #region AABB
        public static stru_AABB Set_AABB_Helicopter(List<Vector3> list_pointsLH)
        {
            float min_x = 1000, max_x = -1000, min_y = 1000, max_y = -1000, min_z = 1000, max_z = -1000;

            // find smallest and largest distances
            for (int i = 0; i < list_pointsLH.Count; i++)
            {
                min_x = Mathf.Min(min_x, list_pointsLH[i].x); // [m]
                max_x = Mathf.Max(max_x, list_pointsLH[i].x); // [m]
                min_y = Mathf.Min(min_y, list_pointsLH[i].y); // [m]
                max_y = Mathf.Max(max_y, list_pointsLH[i].y); // [m]
                min_z = Mathf.Min(min_z, list_pointsLH[i].z); // [m]
                max_z = Mathf.Max(max_z, list_pointsLH[i].z); // [m]
            }

            // which point has the biggest distance to center?
            float[] extrema = new float[6];
            extrema[0] = Mathf.Abs(min_x);
            extrema[1] = Mathf.Abs(max_x);
            extrema[2] = Mathf.Abs(min_y);
            extrema[3] = Mathf.Abs(max_y);
            extrema[4] = Mathf.Abs(min_z);
            extrema[5] = Mathf.Abs(max_z);

            const float additional_size = 0.10f; // [m] 
            float r = extrema.Max() + additional_size;
            stru_AABB aabb = new stru_AABB();
            aabb.r[0] = r;
            aabb.r[1] = r;
            aabb.r[2] = r;
            aabb.c = new Vector3(0, 0, 0);

            return aabb;
        }
        // ############################################################################
        #endregion





        // ############################################################################
        // loop through every triangle and contact point to find collisions
        // ############################################################################
        #region Collision_Point_To_Mesh
        public static List<contact_info> Collision_Point_To_Mesh(Vector3 vect_r_CHO_O, stru_AABB helicopters_aabb, GroundMesh mesh, List<Vector3> list_points)
        {
            List<contact_info> contact_informations = new List<contact_info>();
            List<int> available_points = new List<int>();
            for (int j = 0; j < list_points.Count; j++)
            { 
                contact_informations.Add(new contact_info());
                available_points.Add(j);
            }

            Vector3d v0, v1, v2, n;
            int faces_count = mesh.vertices.Count() / 3;

            stru_AABB helicopters_aabb_translated = new stru_AABB();
            helicopters_aabb_translated.r[0] = helicopters_aabb.r[0];
            helicopters_aabb_translated.r[1] = helicopters_aabb.r[1];
            helicopters_aabb_translated.r[2] = helicopters_aabb.r[2];
            helicopters_aabb_translated.c = helicopters_aabb.c + vect_r_CHO_O;   // vect_r_CHO_O is the helicopters center of mass position CH from inertial frame O, expressed in inertial frame O


            //Debug.Log(triangles_count);
            for (int i = 0; i < faces_count; i++)
            {
                if (TestAABBAABB(helicopters_aabb_translated, mesh.aabb[i]) != 0)
                {
                    v0.x = (double)mesh.vertices[i * 3 + 0].x;
                    v0.y = (double)mesh.vertices[i * 3 + 0].y;
                    v0.z = (double)mesh.vertices[i * 3 + 0].z;
                    v1.x = (double)mesh.vertices[i * 3 + 1].x;
                    v1.y = (double)mesh.vertices[i * 3 + 1].y;
                    v1.z = (double)mesh.vertices[i * 3 + 1].z;
                    v2.x = (double)mesh.vertices[i * 3 + 2].x;
                    v2.y = (double)mesh.vertices[i * 3 + 2].y;
                    v2.z = (double)mesh.vertices[i * 3 + 2].z;
                    n.x = (double)mesh.normals[i].x;
                    n.y = (double)mesh.normals[i].y;
                    n.z = (double)mesh.normals[i].z;

                    for (int p = 0; p < available_points.Count; p++)
                    {
                        int j = available_points[p];
                        float distance = Point_To_Triangle_Distance(v0, v1, v2, n, list_points[j]);

                        if (distance < 0)
                        {
                            contact_informations[j].flag_has_contact = true;
                            contact_informations[j].normalR = (Vector3)n.normalized;
                            contact_informations[j].penetration = distance;
                            available_points.RemoveAt(p--); // remove point because we found a collision. (we only allow one collision)
                        }
                        else
                        {
                            // test a second point, close to the orignal, to avoid falling throug "gaps" in non-convex ground mesh  
                            distance = Point_To_Triangle_Distance(v0, v1, v2, n, list_points[j] + new Vector3(0.02f, 0, 0)); // TODO: this vector should be defined and added in heli's local coordiante system, not in inertial frame
                            if (distance < 0)
                            {
                                contact_informations[j].flag_has_contact = true;
                                contact_informations[j].normalR = (Vector3)n.normalized;
                                contact_informations[j].penetration = distance;
                                available_points.RemoveAt(p--); // remove point because we found a collision. (we only allow one collision)
                            }
                            else
                            {
                                // if we search a point above the ground, we search for the smallest distanced
                                if (contact_informations[j].penetration > distance)
                                {
                                    contact_informations[j].flag_has_contact = false;
                                    contact_informations[j].normalR = (Vector3)n.normalized; 
                                    contact_informations[j].penetration = distance;
                                }     
                            }
                        }

                    }
                }
            }

            return contact_informations;
        }
        // ############################################################################




        // ############################################################################
        // find distance between point and triangle
        // ############################################################################
        // https://www.geometrictools.com/Documentation/DistancePoint3Triangle3.pdf 
        // Only "Region 0" is used here (see Figure 1.)
        public static float Point_To_Triangle_Distance(Vector3d vertex0, Vector3d vertex1, Vector3d vertex2, Vector3d normal, Vector3 point)
        {
            Vector3d E0 = vertex1 - vertex0;
            Vector3d E1 = vertex2 - vertex0;
            Vector3d D;
            D.x = vertex0.x - (double)point.x; // D = B - P
            D.y = vertex0.y - (double)point.y; // D = B - P
            D.z = vertex0.z - (double)point.z; // D = B - P
            double a = E0[0] * E0[0] + E0[1] * E0[1] + E0[2] * E0[2];   // Vector3.Dot(E0, E0); 
            double b = E0[0] * E1[0] + E0[1] * E1[1] + E0[2] * E1[2];   // Vector3.Dot(E0, E1);
            double c = E1[0] * E1[0] + E1[1] * E1[1] + E1[2] * E1[2];   // Vector3.Dot(E1, E1);
            double d = E0[0] * D[0] + E0[1] * D[1] + E0[2] * D[2];      // Vector3.Dot(E0, D);
            double e = E1[0] * D[0] + E1[1] * D[1] + E1[2] * D[2];      // Vector3.Dot(E1, D);
            double f = D[0] * D[0] + D[1] * D[1] + D[2] * D[2];         // Vector3.Dot(D, D);
            double det = a * c - b * b;
            double s = b * e - c * d;
            double t = b * d - a * e;
            double sqr_distance; // [m]
            double inv_det = 0.0000001;

            if ((s + t) <= det)
            {
                if (s >= 0.0 && t >= 0.0)
                {
                    // "region 0"
                    if (det != 0.0)
                        inv_det = 1.0 / det;

                    s *= inv_det;
                    t *= inv_det;
                    sqr_distance = s * (a * s + b * t + 2.0 * d) + t * (b * s + c * t + 2.0 * e) + f;

                    // closest point on triangle
                    // P = point - (vertex0 + s * E0 + t * E1);
                    double Px = point[0] - (vertex0[0] + s * E0[0] + t * E1[0]);
                    double Py = point[1] - (vertex0[1] + s * E0[1] + t * E1[1]);
                    double Pz = point[2] - (vertex0[2] + s * E0[2] + t * E1[2]);

                    // sqr(distance^2) from point to triangle surface
                    return (float)(Math.Sqrt(sqr_distance) * Math.Sign(Dot(Px, Py, Pz, normal[0], normal[1], normal[2])));
                    //return (Math.Sqrt(sqr_distance) * Math.Sign(Dot(Px, Py, Pz, normal[0], normal[1], normal[2])));
                }
            }

            return 1000;
        }
        // ############################################################################
        #endregion




        // ############################################################################
        // sprite to texture2d
        // ############################################################################
        #region Texture_From_Sprite
        // https://answers.unity.com/questions/651984/convert-sprite-image-to-texture.html?page=2&pageSize=5&sort=votes
        public static Texture2D Texture_From_Sprite(Sprite sprite)
        {
            if (sprite.rect.width != sprite.texture.width)
            {
                int texWid = (int)sprite.rect.width;
                int texHei = (int)sprite.rect.height;
                Texture2D newTex = new Texture2D(texWid, texHei);
                Color[] defaultPixels = Enumerable.Repeat<Color>(new Color(0, 0, 0, 0), texWid * texHei).ToArray();
                Color[] pixels = sprite.texture.GetPixels((int)sprite.textureRect.x
                                                        , (int)sprite.textureRect.y
                                                        , (int)sprite.textureRect.width
                                                        , (int)sprite.textureRect.height);
                newTex.SetPixels(defaultPixels);
                newTex.SetPixels((int)sprite.textureRectOffset.x, (int)sprite.textureRectOffset.y, (int)sprite.textureRect.width, (int)sprite.textureRect.height, pixels);
                newTex.Apply();
                return newTex;
            }
            else
            {
                return sprite.texture;
            }
        }
        #endregion




        // ############################################################################
        // moving average filter 
        // ############################################################################
        #region MovingAverage
        public class Exponential_Moving_Average_Filter
        {
            //public int windowSize = 16;
            private double average = 0;

            public void Init_Mean_Value(double init_mean_value)
            {
                average = init_mean_value;
            }

            public double Calculate(int windowSize, double sample)
            {
                double alpha = 2.0 / (windowSize + 1);
                average = alpha * sample + (1.0 - alpha) * average;
                return average;
            }
        }

        // moving average filter for rotations ( whit discontinuity ) [deg, 0 ... 360]
        public class Exponential_Moving_Average_Filter_For_Rotations
        {
            private double average = 0;
            private double sample_old = 0; // [deg, 0 ... 360]

            public void Init_Mean_Value(double init_mean_value)
            {
                average = init_mean_value;
            }

            public double Calculate(int windowSize, double sample) // [deg, 0 ... 360]
            {
                double alpha = 2.0 / (windowSize + 1);

                if (sample < 90 && sample_old > 270)
                    average -= 360;
                if (sample_old < 90 && sample > 270)
                    average += 360;

                average = alpha * sample + (1.0 - alpha) * average;

                sample_old = sample;
                if (average < 0)
                {
                    return average + 360;
                }
                else
                {
                    if (average > 360)
                    {
                        return average - 360;
                    }
                    else
                    {
                        return average;
                    }
                }
            }
        }
        #endregion



        // ############################################################################
        // bitmap pixel manipulation 
        // ############################################################################
        #region BitmapPixelManipulation 
        // https://docs.unity3d.com/ScriptReference/Texture2D.GetRawTextureData.html
        public static void SetTexturePixel(ref Texture2D texture, List<int> yy)
        {
            //GetComponent<Renderer>().material.mainTexture = texture;
            var data = texture.GetRawTextureData<Color32>();

            // fill texture data with a simple pattern
            Color32 transparent = new Color32(0, 0, 0, 0);
            Color32 white = new Color32(255, 255, 255, 255);
            int index = 0;
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {

                    //data[index++] = ((x & y) == 0 ? transparent : white);
                    if(yy.Count>x)
                    { 
                        if (y < yy[x])
                            data[index++] = white;
                        else
                            data[index++] = transparent;
                    }
                    else
                        data[index++] = transparent;
                }
            }
            // upload to the GPU
            texture.Apply();
        }



        /*
        // - requires System.Drawing.dll 
        // https://stackoverflow.com/questions/24701703/c-sharp-faster-alternatives-to-setpixel-and-getpixel-for-bitmaps-for-windows-f
        // https://forum.unity.com/threads/the-type-or-namespace-name-bitmap-could-not-be-found.829026/
        public class DirectBitmap : IDisposable
        {
            public Bitmap Bitmap { get; private set; }
            public Int32[] Bits { get; private set; }
            public bool Disposed { get; private set; }
            public int Height { get; private set; }
            public int Width { get; private set; }

            protected GCHandle BitsHandle { get; private set; }

            public DirectBitmap(int width, int height)
            {
                Width = width;
                Height = height;
                Bits = new Int32[width * height];
                BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
                Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
            }

            public void SetPixel(int x, int y, Color colour)
            {
                int index = x + (y * Width);
                int col = colour.ToArgb();

                Bits[index] = col;
            }

            public Color GetPixel(int x, int y)
            {
                int index = x + (y * Width);
                int col = Bits[index];
                Color result = Color.FromArgb(col);

                return result;
            }

            public void Dispose()
            {
                if (Disposed) return;
                Disposed = true;
                Bitmap.Dispose();
                BitsHandle.Free();
            }
        }*/
        #endregion




        public static bool HasComponent<T>(this GameObject obj) where T : Component
        {
            return obj.GetComponent(typeof(T)) != null;
        }


    }








    // ############################################################################
    //
    //    EEEEEEEEEEEEEEEEEEEEEE                             tttt                                                                   iiii                                                      
    //    E::::::::::::::::::::E                          ttt:::t                                                                  i::::i                                                     
    //    E::::::::::::::::::::E                          t:::::t                                                                   iiii                                                      
    //    EE::::::EEEEEEEEE::::E                          t:::::t                                                                                                                             
    //      E:::::E       EEEEEExxxxxxx      xxxxxxxttttttt:::::ttttttt        eeeeeeeeeeee    nnnn  nnnnnnnn        ssssssssss   iiiiiii    ooooooooooo   nnnn  nnnnnnnn        ssssssssss   
    //      E:::::E              x:::::x    x:::::x t:::::::::::::::::t      ee::::::::::::ee  n:::nn::::::::nn    ss::::::::::s  i:::::i  oo:::::::::::oo n:::nn::::::::nn    ss::::::::::s  
    //      E::::::EEEEEEEEEE     x:::::x  x:::::x  t:::::::::::::::::t     e::::::eeeee:::::een::::::::::::::nn ss:::::::::::::s  i::::i o:::::::::::::::on::::::::::::::nn ss:::::::::::::s 
    //      E:::::::::::::::E      x:::::xx:::::x   tttttt:::::::tttttt    e::::::e     e:::::enn:::::::::::::::ns::::::ssss:::::s i::::i o:::::ooooo:::::onn:::::::::::::::ns::::::ssss:::::s
    //      E:::::::::::::::E       x::::::::::x          t:::::t          e:::::::eeeee::::::e  n:::::nnnn:::::n s:::::s  ssssss  i::::i o::::o     o::::o  n:::::nnnn:::::n s:::::s  ssssss 
    //      E::::::EEEEEEEEEE        x::::::::x           t:::::t          e:::::::::::::::::e   n::::n    n::::n   s::::::s       i::::i o::::o     o::::o  n::::n    n::::n   s::::::s      
    //      E:::::E                  x::::::::x           t:::::t          e::::::eeeeeeeeeee    n::::n    n::::n      s::::::s    i::::i o::::o     o::::o  n::::n    n::::n      s::::::s   
    //      E:::::E       EEEEEE    x::::::::::x          t:::::t    tttttte:::::::e             n::::n    n::::nssssss   s:::::s  i::::i o::::o     o::::o  n::::n    n::::nssssss   s:::::s 
    //    EE::::::EEEEEEEE:::::E   x:::::xx:::::x         t::::::tttt:::::te::::::::e            n::::n    n::::ns:::::ssss::::::si::::::io:::::ooooo:::::o  n::::n    n::::ns:::::ssss::::::s
    //    E::::::::::::::::::::E  x:::::x  x:::::x        tt::::::::::::::t e::::::::eeeeeeee    n::::n    n::::ns::::::::::::::s i::::::io:::::::::::::::o  n::::n    n::::ns::::::::::::::s 
    //    E::::::::::::::::::::E x:::::x    x:::::x         tt:::::::::::tt  ee:::::::::::::e    n::::n    n::::n s:::::::::::ss  i::::::i oo:::::::::::oo   n::::n    n::::n s:::::::::::ss  
    //    EEEEEEEEEEEEEEEEEEEEEExxxxxxx      xxxxxxx          ttttttttttt      eeeeeeeeeeeeee    nnnnnn    nnnnnn  sssssssssss    iiiiiiii   ooooooooooo     nnnnnn    nnnnnn  sssssssssss  
    // ############################################################################
    // extensions
    // ############################################################################
    public static class Extensions
    {


        // ############################################################################
        // deep copy 
        // ############################################################################
        //  https://www.techiedelight.com/copy-object-csharp/
        public static T Deep_Clone<T>(this T obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(stream, obj);
                stream.Position = 0;

                return (T)serializer.Deserialize(stream);
            }
        }



        // ############################################################################
        // deep compare 
        // ############################################################################
        // https://stackoverflow.com/questions/10454519/best-way-to-compare-two-complex-objects
        public static string Serialize_Object<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }
        public static bool Deep_Compare(this object obj, object toCompare)
        {
            if (obj.Serialize_Object() == toCompare.Serialize_Object())
                return true;
            else
                return false;
        }

    }



    // ############################################################################
    // change unity's material type
    // ############################################################################
    // https://forum.unity.com/threads/change-rendering-mode-via-script.476437/
    public static class MaterialExtensions
    {
        public static void ToOpaqueMode(this Material material)
        {
            material.SetOverrideTag("RenderType", "");
            material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
        }
   
        public static void ToFadeMode(this Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Transparent;
        }


        public static void ChangeTransparency(this Material material, float transparency)
        {
            var temp = material.color; temp.a = Mathf.Clamp(transparency, 0, 1); // alpha setting
            material.color = temp;
        }
    }




    // ############################################################################
    /// <summary>
    /// Rotate texture
    /// </summary>
    // ############################################################################
    //https://answers.unity.com/questions/490365/rotate-a-color-at-intervals-of-90-degrees.html?_ga=2.112649665.412277658.1594574603-2133810121.1564613509#answer-490545
    public static class Texture2DExtensions
    {
        // ############################################################################
        public enum Rotation { Left, Right, HalfCircle }
        public static void Rotate(this Texture2D texture, Rotation rotation)
        {
            Color32[] originalPixels = texture.GetPixels32();
            IEnumerable<Color32> rotatedPixels;

            if (rotation == Rotation.HalfCircle)
                rotatedPixels = originalPixels.Reverse();
            else
            {
                // Rotate left:
                var firstRowPixelIndeces = Enumerable.Range(0, texture.height).Select(i => i * texture.width).Reverse().ToArray();
                rotatedPixels = Enumerable.Repeat(firstRowPixelIndeces, texture.width).SelectMany(
                    (frpi, rowIndex) => frpi.Select(i => originalPixels[i + rowIndex])
                );

                if (rotation == Rotation.Right)
                    rotatedPixels = rotatedPixels.Reverse();
            }

            texture.SetPixels32(rotatedPixels.ToArray());
            texture.Apply();
        }
        // ############################################################################




        // ############################################################################
        // https://answers.unity.com/questions/238922/png-transparency-has-white-borderhalo.html#answer-949217
        // Copy the values of adjacent pixels to transparent pixels color info, to
        // remove the white border artifact when importing transparent .PNGs.
        public static void FixTransparency(this Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            int w = texture.width;
            int h = texture.height;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    Color32 pixel = pixels[idx];
                    if (pixel.a == 0)
                    {
                        bool done = false;
                        if (!done && x > 0) done = TryAdjacent(ref pixel, pixels[idx - 1]);        // Left   pixel
                        if (!done && x < w - 1) done = TryAdjacent(ref pixel, pixels[idx + 1]);        // Right  pixel
                        if (!done && y > 0) done = TryAdjacent(ref pixel, pixels[idx - w]);        // Top    pixel
                        if (!done && y < h - 1) done = TryAdjacent(ref pixel, pixels[idx + w]);        // Bottom pixel
                        pixels[idx] = pixel;
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

        }

        private static bool TryAdjacent(ref Color32 pixel, Color32 adjacent)
        {
            if (adjacent.a == 0) return false;

            pixel.r = adjacent.r;
            pixel.g = adjacent.g;
            pixel.b = adjacent.b;
            return true;
        }
        // ############################################################################



    }
    // ############################################################################






}



// ############################################################################
/// A unility class with functions to scale Texture2D Data.
///
/// Scale is performed on the GPU using RTT, so it's blazing fast.
/// Setting up and Getting back the texture data is the bottleneck. 
/// But Scaling itself costs only 1 draw call and 1 RTT State setup!
/// WARNING: This script override the RTT Setup! (It sets a RTT!)	 
///
/// Note: This scaler does NOT support aspect ratio based scaling. You will have to do it yourself!
/// It supports Alpha, but you will have to divide by alpha in your shaders, 
/// because of premultiplied alpha effect. Or you should use blend modes.
public class TextureScaler
{

    /// <summary>
    ///	Returns a scaled copy of given texture. 
    /// </summary>
    /// <param name="tex">Source texure to scale</param>
    /// <param name="width">Destination texture width</param>
    /// <param name="height">Destination texture height</param>
    /// <param name="mode">Filtering mode</param>
    public static Texture2D scaled(Texture2D src, int width, int height, FilterMode mode = FilterMode.Trilinear)
    {
        Rect texR = new Rect(0, 0, width, height);
        _gpu_scale(src, width, height, mode);

        //Get rendered data back to a new texture
        Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, true);
        result.Resize(width, height);
        result.ReadPixels(texR, 0, 0, true);
        return result;
    }

    /// <summary>
    /// Scales the texture data of the given texture.
    /// </summary>
    /// <param name="tex">Texure to scale</param>
    /// <param name="width">New width</param>
    /// <param name="height">New height</param>
    /// <param name="mode">Filtering mode</param>
    public static void scale(Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear)
    {
        Rect texR = new Rect(0, 0, width, height);
        _gpu_scale(tex, width, height, mode);

        // Update new texture
        tex.Resize(width, height);
        tex.ReadPixels(texR, 0, 0, true);
        tex.Apply(true);    //Remove this if you hate us applying textures for you :)
    }

    // Internal unility that renders the source texture into the RTT - the scaling method itself.
    static void _gpu_scale(Texture2D src, int width, int height, FilterMode fmode)
    {
        //We need the source texture in VRAM because we render with it
        src.filterMode = fmode;
        src.Apply(true);

        //Using RTT for best quality and performance. Thanks, Unity 5
        RenderTexture rtt = new RenderTexture(width, height, 32);

        //Set the RTT in order to render to it
        Graphics.SetRenderTarget(rtt);

        //Setup 2D matrix in range 0..1, so nobody needs to care about sized
        GL.LoadPixelMatrix(0, 1, 1, 0);

        //Then clear & draw the texture to fill the entire RTT.
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
    }
}
// ############################################################################