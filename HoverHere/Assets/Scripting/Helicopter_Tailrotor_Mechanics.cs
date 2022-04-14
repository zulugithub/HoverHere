using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Common;
using System.Xml;
using System.Xml.Serialization;
using Parameter;
using System.Linq;


namespace Helicopter_Tailrotor_Mechanics
{


    // ##################################################################################                                                                                                                                                                                                                   
    //    VVVVVVVV           VVVVVVVVIIIIIIIIII   SSSSSSSSSSSSSSS UUUUUUUU     UUUUUUUU           AAA               LLLLLLLLLLL                                     lllllll                                                    
    //    V::::::V           V::::::VI::::::::I SS:::::::::::::::SU::::::U     U::::::U          A:::A              L:::::::::L                                     l:::::l                                                    
    //    V::::::V           V::::::VI::::::::IS:::::SSSSSS::::::SU::::::U     U::::::U         A:::::A             L:::::::::L                                     l:::::l                                                    
    //    V::::::V           V::::::VII::::::IIS:::::S     SSSSSSSUU:::::U     U:::::UU        A:::::::A            LL:::::::LL                                     l:::::l                                                    
    //     V:::::V           V:::::V   I::::I  S:::::S             U:::::U     U:::::U        A:::::::::A             L:::::L                       cccccccccccccccc l::::l   aaaaaaaaaaaaa      ssssssssss       ssssssssss   
    //      V:::::V         V:::::V    I::::I  S:::::S             U:::::D     D:::::U       A:::::A:::::A            L:::::L                     cc:::::::::::::::c l::::l   a::::::::::::a   ss::::::::::s    ss::::::::::s  
    //       V:::::V       V:::::V     I::::I   S::::SSSS          U:::::D     D:::::U      A:::::A A:::::A           L:::::L                    c:::::::::::::::::c l::::l   aaaaaaaaa:::::ass:::::::::::::s ss:::::::::::::s 
    //        V:::::V     V:::::V      I::::I    SS::::::SSSSS     U:::::D     D:::::U     A:::::A   A:::::A          L:::::L                   c:::::::cccccc:::::c l::::l            a::::as::::::ssss:::::ss::::::ssss:::::s
    //         V:::::V   V:::::V       I::::I      SSS::::::::SS   U:::::D     D:::::U    A:::::A     A:::::A         L:::::L                   c::::::c     ccccccc l::::l     aaaaaaa:::::a s:::::s  ssssss  s:::::s  ssssss 
    //          V:::::V V:::::V        I::::I         SSSSSS::::S  U:::::D     D:::::U   A:::::AAAAAAAAA:::::A        L:::::L                   c:::::c              l::::l   aa::::::::::::a   s::::::s         s::::::s      
    //           V:::::V:::::V         I::::I              S:::::S U:::::D     D:::::U  A:::::::::::::::::::::A       L:::::L                   c:::::c              l::::l  a::::aaaa::::::a      s::::::s         s::::::s   
    //            V:::::::::V          I::::I              S:::::S U::::::U   U::::::U A:::::AAAAAAAAAAAAA:::::A      L:::::L         LLLLLL    c::::::c     ccccccc l::::l a::::a    a:::::assssss   s:::::s ssssss   s:::::s 
    //             V:::::::V         II::::::IISSSSSSS     S:::::S U:::::::UUU:::::::UA:::::A             A:::::A   LL:::::::LLLLLLLLL:::::L    c:::::::cccccc:::::cl::::::la::::a    a:::::as:::::ssss::::::ss:::::ssss::::::s
    //              V:::::V          I::::::::IS::::::SSSSSS:::::S  UU:::::::::::::UUA:::::A               A:::::A  L::::::::::::::::::::::L     c:::::::::::::::::cl::::::la:::::aaaa::::::as::::::::::::::s s::::::::::::::s 
    //               V:::V           I::::::::IS:::::::::::::::SS     UU:::::::::UU A:::::A                 A:::::A L::::::::::::::::::::::L      cc:::::::::::::::cl::::::l a::::::::::aa:::as:::::::::::ss   s:::::::::::ss  
    //                VVV            IIIIIIIIII SSSSSSSSSSSSSSS         UUUUUUUUU  AAAAAAA                   AAAAAAALLLLLLLLLLLLLLLLLLLLLLLL        ccccccccccccccccllllllll  aaaaaaaaaa  aaaa sssssssssss      sssssssssss    
    // ##################################################################################
    public class Helicopter_Tailrotor_Mechanics : MonoBehaviour
    {

        // ##################################################################################
        //  PPPPPPPPPPPPPPPPP                                                                                                           tttt                                                      
        //  P::::::::::::::::P                                                                                                       ttt:::t                                                      
        //  P::::::PPPPPP:::::P                                                                                                      t:::::t                                                      
        //  PP:::::P     P:::::P                                                                                                     t:::::t                                                      
        //    P::::P     P:::::Paaaaaaaaaaaaa  rrrrr   rrrrrrrrr   aaaaaaaaaaaaa      mmmmmmm    mmmmmmm       eeeeeeeeeeee    ttttttt:::::ttttttt        eeeeeeeeeeee    rrrrr   rrrrrrrrr       
        //    P::::P     P:::::Pa::::::::::::a r::::rrr:::::::::r  a::::::::::::a   mm:::::::m  m:::::::mm   ee::::::::::::ee  t:::::::::::::::::t      ee::::::::::::ee  r::::rrr:::::::::r      
        //    P::::PPPPPP:::::P aaaaaaaaa:::::ar:::::::::::::::::r aaaaaaaaa:::::a m::::::::::mm::::::::::m e::::::eeeee:::::eet:::::::::::::::::t     e::::::eeeee:::::eer:::::::::::::::::r     
        //    P:::::::::::::PP           a::::arr::::::rrrrr::::::r         a::::a m::::::::::::::::::::::me::::::e     e:::::etttttt:::::::tttttt    e::::::e     e:::::err::::::rrrrr::::::r    
        //    P::::PPPPPPPPP      aaaaaaa:::::a r:::::r     r:::::r  aaaaaaa:::::a m:::::mmm::::::mmm:::::me:::::::eeeee::::::e      t:::::t          e:::::::eeeee::::::e r:::::r     r:::::r    
        //    P::::P            aa::::::::::::a r:::::r     rrrrrrraa::::::::::::a m::::m   m::::m   m::::me:::::::::::::::::e       t:::::t          e:::::::::::::::::e  r:::::r     rrrrrrr    
        //    P::::P           a::::aaaa::::::a r:::::r           a::::aaaa::::::a m::::m   m::::m   m::::me::::::eeeeeeeeeee        t:::::t          e::::::eeeeeeeeeee   r:::::r                
        //    P::::P          a::::a    a:::::a r:::::r          a::::a    a:::::a m::::m   m::::m   m::::me:::::::e                 t:::::t    tttttte:::::::e            r:::::r                
        //  PP::::::PP        a::::a    a:::::a r:::::r          a::::a    a:::::a m::::m   m::::m   m::::me::::::::e                t::::::tttt:::::te::::::::e           r:::::r                
        //  P::::::::P        a:::::aaaa::::::a r:::::r          a:::::aaaa::::::a m::::m   m::::m   m::::m e::::::::eeeeeeee        tt::::::::::::::t e::::::::eeeeeeee   r:::::r                
        //  P::::::::P         a::::::::::aa:::ar:::::r           a::::::::::aa:::am::::m   m::::m   m::::m  ee:::::::::::::e          tt:::::::::::tt  ee:::::::::::::e   r:::::r                
        //  PPPPPPPPPP          aaaaaaaaaa  aaaarrrrrrr            aaaaaaaaaa  aaaammmmmm   mmmmmm   mmmmmm    eeeeeeeeeeeeee            ttttttttttt      eeeeeeeeeeeeee   rrrrrrr                  
        // ##################################################################################
        #region parameter
        // ##################################################################################
        private stru_tailrotor_mechanics tailrotor_mechanics;

        public bool rotor_3d_mechanics_geometry_available = true;
        private stru_gameobject mechanics_fixed;
        private stru_gameobject mechanics_rotating;
        private stru_gameobject servoarm_BA;
        private stru_gameobject rod_CB;
        private stru_gameobject lever_D;
        private stru_gameobject sleeve_K;
        private stru_gameobject pitch_plate_J; // rotating
        private stru_gameobject[] hinge_HG; // rotating
        private stru_gameobject[] blade; // rotating

        private Matrix4x4 Ay_LR_Omega;

        int ab = 0; // to avoid flickering while reading the values from this thread by the main routine two memory blocks are reserved for the result. While one is filled, the other can be read.

        public readonly float[] servo_sigma = new float[1]; // [rad] // for servo sound
        #endregion
        // ##################################################################################






        // ##################################################################################
        //                                                                                                                              dddddddd                 
        //    MMMMMMMM               MMMMMMMM                             tttt         hhhhhhh                                          d::::::d                 
        //    M:::::::M             M:::::::M                          ttt:::t         h:::::h                                          d::::::d                 
        //    M::::::::M           M::::::::M                          t:::::t         h:::::h                                          d::::::d                 
        //    M:::::::::M         M:::::::::M                          t:::::t         h:::::h                                          d:::::d                  
        //    M::::::::::M       M::::::::::M    eeeeeeeeeeee    ttttttt:::::ttttttt    h::::h hhhhh          ooooooooooo       ddddddddd:::::d     ssssssssss   
        //    M:::::::::::M     M:::::::::::M  ee::::::::::::ee  t:::::::::::::::::t    h::::hh:::::hhh     oo:::::::::::oo   dd::::::::::::::d   ss::::::::::s  
        //    M:::::::M::::M   M::::M:::::::M e::::::eeeee:::::eet:::::::::::::::::t    h::::::::::::::hh  o:::::::::::::::o d::::::::::::::::d ss:::::::::::::s 
        //    M::::::M M::::M M::::M M::::::Me::::::e     e:::::etttttt:::::::tttttt    h:::::::hhh::::::h o:::::ooooo:::::od:::::::ddddd:::::d s::::::ssss:::::s
        //    M::::::M  M::::M::::M  M::::::Me:::::::eeeee::::::e      t:::::t          h::::::h   h::::::ho::::o     o::::od::::::d    d:::::d  s:::::s  ssssss 
        //    M::::::M   M:::::::M   M::::::Me:::::::::::::::::e       t:::::t          h:::::h     h:::::ho::::o     o::::od:::::d     d:::::d    s::::::s      
        //    M::::::M    M:::::M    M::::::Me::::::eeeeeeeeeee        t:::::t          h:::::h     h:::::ho::::o     o::::od:::::d     d:::::d       s::::::s   
        //    M::::::M     MMMMM     M::::::Me:::::::e                 t:::::t    tttttth:::::h     h:::::ho::::o     o::::od:::::d     d:::::d ssssss   s:::::s 
        //    M::::::M               M::::::Me::::::::e                t::::::tttt:::::th:::::h     h:::::ho:::::ooooo:::::od::::::ddddd::::::dds:::::ssss::::::s
        //    M::::::M               M::::::M e::::::::eeeeeeee        tt::::::::::::::th:::::h     h:::::ho:::::::::::::::o d:::::::::::::::::ds::::::::::::::s 
        //    M::::::M               M::::::M  ee:::::::::::::e          tt:::::::::::tth:::::h     h:::::h oo:::::::::::oo   d:::::::::ddd::::d s:::::::::::ss  
        //    MMMMMMMM               MMMMMMMM    eeeeeeeeeeeeee            ttttttttttt  hhhhhhh     hhhhhhh   ooooooooooo      ddddddddd   ddddd  sssssssssss   
        // ##################################################################################
        #region methods
        public Helicopter_Tailrotor_Mechanics()
        {

        }


        public void Initialize(string helicopter_name, string rotor_name, Transform transform)
        {
            tailrotor_mechanics = new stru_tailrotor_mechanics();

            // load parameter, that are exported from blender
            TextAsset txtAsset = (TextAsset)Resources.Load(helicopter_name + "_tailrotor_mechanics", typeof(TextAsset));
            // if this helicopter do not have a rotor-mechanics.xml file use the LOGO600SE-V3 as default mechanics.
            if (txtAsset == null)
                txtAsset = (TextAsset)Resources.Load("Logo600SE_V3_tailrotor_mechanics", typeof(TextAsset));
            // get data
            if (txtAsset != null)
                tailrotor_mechanics = Helper.IO_XML_Deserialize<stru_tailrotor_mechanics>(txtAsset);

            //Helper.IO_XML_Serialize(tailrotor_mechanics, "test.xml", true);


            // if 3D mechanics-geometry is available, then get the parts
            if (rotor_3d_mechanics_geometry_available == true)
            {
                mechanics_fixed = new stru_gameobject();
                mechanics_rotating = new stru_gameobject();
                servoarm_BA = new stru_gameobject();
                rod_CB = new stru_gameobject();
                lever_D = new stru_gameobject();
                sleeve_K = new stru_gameobject();
                pitch_plate_J = new stru_gameobject();
                hinge_HG = new stru_gameobject[2]; for (int i = 0; i < 2; i++) hinge_HG[i] = new stru_gameobject();
                blade = new stru_gameobject[2]; for (int i = 0; i < 2; i++) blade[i] = new stru_gameobject();

                // assign geometies, that are exported from blender (and allready are added manually to the game)
                foreach (Transform eachChild in transform.gameObject.transform)
                {
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Mechanics_Fixed") mechanics_fixed.go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Mechanics_Rotating") mechanics_rotating.go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Servoarm_BA") servoarm_BA.go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Rod_CB") rod_CB.go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Lever_D") lever_D.go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Sleeve_K") sleeve_K.go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Pitch_Plate_J") pitch_plate_J.go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Hinge_H0G0") hinge_HG[0].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Hinge_H1G1") hinge_HG[1].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Blade0") blade[0].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Blade1") blade[1].go = eachChild.gameObject;
                }
            }
        }




        // [rad], [rad], [rad], [sec], [-]
        public void Calculate(float yaw, float Omega, ref float beta, float dtime, int integrator_function_call_number)
        {

            //blade_object = Helicopter_Selected.transform.Find(rotor_name + "_Model").gameObject.transform.GetChild(1).gameObject;
            //Vector3 forward = Helper.ConvertRightHandedToLeftHandedVector(new Vector3(1, 0, 0));
            //Vector3 upward = Helper.ConvertRightHandedToLeftHandedVector(new Vector3(0, 0.3f, 1));
            //blade_object.transform.localRotation = Quaternion.LookRotation(forward, upward);


            //tailrotor_mechanics.state.yaw = 5 * Mathf.PI / 180; // [rad] yaw
            //tailrotor_mechanics.state.Omega = 20 * Mathf.PI / 180; // [rad] shaft rotation angle(around rotor system's y-axis)

            tailrotor_mechanics.state_in.yaw = yaw; // [rad] yaw
            tailrotor_mechanics.state_in.Omega = Omega; // [rad] shaft rotation angle(around rotor system's y-axis)



            // ##################################################################################
            // Pre - calculations
            // ##################################################################################
            tailrotor_mechanics.limb_stationary.calculated.normal_servo_axis = Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_stationary.parameter.A_OLA) * new Vector3(0, 0, 1);
            // ##################################################################################





            // ##################################################################################
            // Here would servo dynamics be simulated // TODO
            // ##################################################################################
            // ... just to show, where the ODE - simulation would be implemented in the game/ simulation 
            //float smoothTime = 0.100f;

            //tailrotor_mechanics.limb_stationary[i].calculated.sigma_actual = Mathf.SmoothDamp(tailrotor_mechanics.limb_stationary[i].calculated.sigma_actual, tailrotor_mechanics.limb_stationary[i].calculated.sigma_commanded, ref xServoVelocity[i], smoothTime);
            tailrotor_mechanics.limb_stationary.calculated.sigma_commanded = tailrotor_mechanics.state_in.yaw;
            tailrotor_mechanics.limb_stationary.calculated.sigma_actual = tailrotor_mechanics.limb_stationary.calculated.sigma_commanded;  // sigma_actual differs to sigma_commanded by possible slower motion, if implemented

            // only for sound
            if (integrator_function_call_number == 0)
            {
                servo_sigma[0] = tailrotor_mechanics.limb_stationary.calculated.sigma_actual; // [rad] // [rad]
            }
            // ##################################################################################





            // ##################################################################################
            // 1.) Calculate servo angles and rod
            // ##################################################################################
            // rotate servo arm
            tailrotor_mechanics.limb_stationary.calculated.r_BO_O = tailrotor_mechanics.limb_stationary.parameter.r_AO_O + // <-- this line: add servo position
                  (Vector3)(Matrix4x4.Rotate(Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_stationary.parameter.A_OLA)) * // <-- this line: transform arm to world coodriante system (defined at rotorhead)
                  Helper.Az_LR(tailrotor_mechanics.limb_stationary.calculated.sigma_actual) * tailrotor_mechanics.limb_stationary.parameter.r_BA_LA); // <-- this line: rotate servo arm in servo-coordinate-system (around local z - axis)


            // find rod end position (point C)
            // https://gamedev.stackexchange.com/questions/75756/sphere-sphere-intersection-and-circle-sphere-intersection
            float r = tailrotor_mechanics.limb_stationary.parameter.l_CD; //[m]
            float R = tailrotor_mechanics.limb_stationary.parameter.l_CB; //[m]
            Vector3 B = tailrotor_mechanics.limb_stationary.calculated.r_BO_O; //[m]
            Vector3 D = tailrotor_mechanics.limb_stationary.parameter.r_DO_O; //[m]
            Vector3 n_D = Matrix4x4.Rotate(Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_stationary.parameter.A_OLD)) * new Vector3(0, 0, 1);  //[m]

            Vector3 n_D_hat = n_D.normalized; // "hat" means normailzed
            float d = Vector3.Dot(n_D_hat, D - B );
            Vector3 P = B + n_D_hat * d;
            float u = Mathf.Sqrt(R * R - d * d);   // u_sqr = (R ^ 2 - d ^ 2);

            float a = (D - P).magnitude;
            float h = (a * a + u * u - r * r) / (2 * a);  // u_sqr
            float l = Mathf.Sqrt(u * u - h * h);  // u_sqr
            Vector3 Q = P + ((D - P).normalized) * h;

            Vector3 t_hat = Helper.Cross(D - P, n_D).normalized; // "hat" means normailzed

            // Vector3 C_0 = Q - l * t_hat;
            Vector3 C_1 = Q + l * t_hat;

            tailrotor_mechanics.limb_stationary.calculated.r_CO_O = C_1;
            // ##################################################################################



            // ##################################################################################
            // 2. Lever rotation 
            // ##################################################################################
            // lever's arm vector in world coordiantes  
            tailrotor_mechanics.limb_stationary.calculated.normal_lever_arm = Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_stationary.parameter.A_OLD) * tailrotor_mechanics.limb_stationary.parameter.r_CD_LD;

            // lever's rotation angle
            Vector3 r_CD_O = tailrotor_mechanics.limb_stationary.calculated.r_CO_O - tailrotor_mechanics.limb_stationary.parameter.r_DO_O; // lever arm vector
            tailrotor_mechanics.limb_stationary.calculated.lever_rotation_angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(tailrotor_mechanics.limb_stationary.calculated.normal_lever_arm, r_CD_O) / (tailrotor_mechanics.limb_stationary.calculated.normal_lever_arm.magnitude * r_CD_O.magnitude),-1,+1)); // angle between °0 position of lever arm and resulting lever arm vector

            // sign of lever's rotation angle
            if (Vector3.Dot(Quaternion.Inverse(Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_stationary.parameter.A_OLD)) * r_CD_O,   new Vector3(1,0,0)) > 0) tailrotor_mechanics.limb_stationary.calculated.lever_rotation_angle = -tailrotor_mechanics.limb_stationary.calculated.lever_rotation_angle;

            // transformation matrix around z - axis
            float lambda = tailrotor_mechanics.limb_stationary.calculated.lever_rotation_angle; // [rad]
            Matrix4x4 Az_LR_lambda = Helper.Az_LR(lambda); // [cos(lambda) sin(lambda) 0; -sin(lambda) cos(lambda) 0; 0 0 1]; // lever local rotation matrix

            // calculate point E
            tailrotor_mechanics.limb_stationary.calculated.r_EO_O = (Vector3)(Matrix4x4.Rotate(Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_stationary.parameter.A_OLD)) * Az_LR_lambda.inverse * tailrotor_mechanics.limb_stationary.parameter.r_ED_LD) + tailrotor_mechanics.limb_stationary.parameter.r_DO_O;
            // ##################################################################################




            // ##################################################################################
            // 3. Position K according to point E(shaft axis is the reference system's y-axis)
            // ##################################################################################
            // position K according to point E(shaft axis is the reference system's y-axis)
            tailrotor_mechanics.limb_stationary.calculated.r_KO_O = new Vector3(0, tailrotor_mechanics.limb_stationary.calculated.r_EO_O.y, 0);

            for (int i = 0; i < tailrotor_mechanics.blades_count; i++)
            {
                tailrotor_mechanics.limb_rotating[i].calculated.r_JO_O = new Vector3(0, tailrotor_mechanics.limb_stationary.calculated.r_EO_O.y, 0);
            }

            // linear motion along Oy of point K
            float stroke = tailrotor_mechanics.limb_stationary.calculated.r_KO_O.y - tailrotor_mechanics.limb_stationary.parameter.r_KO_O.y; // [m]

            // point F and thus rotation of "sleeve_K" is not calculated yet, because rotation is very small
            // ##################################################################################




            // ##################################################################################
            // 4. Find blade angle beta and the pitch-plate's small rotation alpha. Shaft rotation is here 0°.
            // ##################################################################################
            Matrix4x4 Ay_OLJ__alpha = new Matrix4x4();

            // first blade is calcualted only, beacuse all tailrotor blades have the same angle beta
            { 
                int i=0;
                // precalculations 
                Vector3 vector_Jy = Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_rotating[i].parameter.A_OLJ) * new Vector3(0, 1, 0);
                Vector3 vector_Biy = Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_rotating[i].parameter.A_OLBi) * new Vector3(0, 1, 0); //* new Vector3(1, 0, 1);
                vector_Biy.y = 0;

                // angle between two vectors
                float angle_between_Jy_and_Biy_theta = Mathf.Acos(Mathf.Clamp(Vector3.Dot(vector_Jy, vector_Biy) / (vector_Jy.magnitude * vector_Biy.magnitude), -1, +1)); //[rad] valid at initial position
                                                                                                                                                                           
                // https://de.wikipedia.org/wiki/Skalarprodukt -->  Orthogonalität und orthogonale Projektion --> ba = (b*a)a
                // l_I0O = norm(    dot(limb_rotating(i).parameter.r_HiO_O, limb_rotating(i).parameter.A_OLBi * [0 0 1]') * (limb_rotating(i).parameter.A_OLBi * [0 0 1]')    );
                Vector3 r_IiO_O = Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_rotating[i].parameter.A_OLBi) * tailrotor_mechanics.limb_rotating[i].parameter.r_IiO_Bi; // r_IiO_Bladei
                float l_I0O = Mathf.Abs(r_IiO_O.x); // [m]
                
                float alpha = 0; //[rad]  pitch plate rotation difference relative to blade - has to be defined iteratively
                for (int j = 0; j < 5; j++) // number of ietartion 5...10 
                {
                    // use postion of pitch-plate and blade0 to find iteratively the small pitch-plate rotation (alpha) and the hinge's spatial position/orientation (Hi)
    
                    // pitch-plate and blade have a difference angle. Additionaly to the axial movement of the pitch plate this angle varies a small amount (alpha) 
                    // intersection the plane throug I0 with normal Blade0_z and the plane through J with normal J_x an intersction line can be defined. On this line lies r_HyiO_O. 
                    // The length l_HyiO can then be calculated with:
                    float l_Hy0O = l_I0O / Mathf.Sin(angle_between_Jy_and_Biy_theta + alpha); // positive sign (+alpha) is here a definition t

                    // using l_HyiO we can calulate in the plane though J and with the normal J_x  the point Hi
                    float a_ = l_Hy0O - Mathf.Abs(tailrotor_mechanics.limb_rotating[i].parameter.r_GiJ_LJ.y); // [m]
                    float c = tailrotor_mechanics.limb_rotating[i].parameter.l_HiGi; // [m] lenght of hinge
                    float b = Mathf.Sqrt(c*c-a_*a_); // [m]

                    // end point Hi of hinge-i, expressed in pitch-plate local frame J
                    Vector3 r_HiJ_LJ = tailrotor_mechanics.limb_rotating[i].parameter.r_GiJ_LJ + new Vector3(0, a_, b); // [m]
                    // end point Hi of hinge-i, expressed in global frame O
                    Ay_OLJ__alpha = Helper.Ay_LR(-alpha).inverse; //[cos(-alpha) 0 sin(-alpha); 0 1 0; -sin(-alpha) 0 cos(-alpha)]; // RL <-- local to reference but use negative sign (-alpha) due to mathematical positive rotation around J_z
                    Vector3 r_HiJ_O = Ay_OLJ__alpha * Matrix4x4.Rotate(Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_rotating[i].parameter.A_OLJ)) * r_HiJ_LJ;
    
    
                    tailrotor_mechanics.limb_rotating[i].calculated.r_HiO_O = r_HiJ_O + tailrotor_mechanics.limb_rotating[i].calculated.r_JO_O;
                    //tailrotor_mechanics.limb_rotating[i].calculated.r_HiO_O;
                    //tailrotor_mechanics.limb_rotating[i].parameter.r_HiO_O;

                    tailrotor_mechanics.limb_rotating[i].calculated.r_HiIi_O = tailrotor_mechanics.limb_rotating[i].calculated.r_HiO_O - r_IiO_O; // Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_rotating[i].parameter.A_OLBi) * tailrotor_mechanics.limb_rotating[i].parameter.r_IiO_Bi;
                    //tailrotor_mechanics.limb_rotating[i].calculated.l_HiIi = tailrotor_mechanics.limb_rotating[i].calculated.r_HiIi_O.magnitude;
                    //tailrotor_mechanics.limb_rotating[i].calculated.l_HiIi
                    //tailrotor_mechanics.limb_rotating[i].parameter.l_HiIi

                    // iteation loop target update
                    // find new alpha: (small rotation of pitch plate, relative to blade due to hinge kinematics)
                    // 1. create a new vector xxx with known l_HiLi along calculated vector r_HiIi_O. 
                    // 2. calculate angle alpha of projected vector (on yz-global-plane) xxx
                    Vector3 r_HiIi_O_projected = tailrotor_mechanics.limb_rotating[i].calculated.r_HiIi_O.normalized  *  tailrotor_mechanics.limb_rotating[i].parameter.l_HiIi;
                    r_HiIi_O_projected.y = 0;
                    alpha = (Mathf.Abs(Mathf.Atan(l_I0O / r_HiIi_O_projected.z)) - angle_between_Jy_and_Biy_theta);
                }

                // calculate blade angle (beta) 
                tailrotor_mechanics.limb_rotating[i].calculated.beta = Mathf.Acos(Mathf.Clamp(Vector3.Dot(new Vector3(0, 0, -1), tailrotor_mechanics.limb_rotating[i].calculated.r_HiIi_O) / (1 * tailrotor_mechanics.limb_rotating[i].calculated.r_HiIi_O.magnitude), -1, +1)); //[rad] 
                // angle between vector returns only servo angle, but we need also the sign
                if (Vector3.Dot(tailrotor_mechanics.limb_rotating[i].calculated.r_HiIi_O, new Vector3(0, 1, 0)) > 0) tailrotor_mechanics.limb_rotating[i].calculated.beta = -tailrotor_mechanics.limb_rotating[i].calculated.beta;  // Todo > or < ?

            }

            // calculate the other blades 
            for (int i = 1; i < 2; i++) // tailrotor_mechanics.blades_count
            {
                tailrotor_mechanics.limb_rotating[i].calculated.beta = tailrotor_mechanics.limb_rotating[0].calculated.beta;
                tailrotor_mechanics.limb_rotating[i].calculated.r_HiO_O = Helper.Ay_RL((float)i * Helper.Deg_to_Rad * (360f / tailrotor_mechanics.blades_count), tailrotor_mechanics.limb_rotating[0].calculated.r_HiO_O);
            }
            // ##################################################################################



            // ##################################################################################
            // 5. When balde angle beta is found in 4.) calculate additionally rotation of shaft 
            // ##################################################################################
            // shaft rotation
            Matrix4x4 Ay_RL_Omega = Helper.Ay_LR(tailrotor_mechanics.state_in.Omega).inverse; // [cos(state.Omega) 0 sin(state.Omega); 0 1 0; -sin(state.Omega) 0 cos(state.Omega)]; // shaft rotation  RL <-- local to reference   (think about as rotate local system with vector on it and then express result in reference system)
            Ay_LR_Omega = Ay_RL_Omega.inverse;

            for (int i = 0; i < 2; i++) // tailrotor_mechanics.blades_count
            {
                tailrotor_mechanics.limb_rotating[i].calculated.r_HiO_O = Ay_RL_Omega * tailrotor_mechanics.limb_rotating[i].calculated.r_HiO_O; 
                tailrotor_mechanics.limb_rotating[i].calculated.r_IiO_O = Ay_RL_Omega * Matrix4x4.Rotate(Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_rotating[i].parameter.A_OLBi)) * tailrotor_mechanics.limb_rotating[i].parameter.r_IiO_Bi;
                tailrotor_mechanics.limb_rotating[i].calculated.r_GiO_O = (Vector3)(Ay_RL_Omega * (Ay_OLJ__alpha * tailrotor_mechanics.limb_rotating[i].parameter.r_GiO_O)) + new Vector3(0, stroke, 0);
                tailrotor_mechanics.limb_rotating[i].calculated.normal_hinge_axis = Vector3.Cross(tailrotor_mechanics.limb_rotating[i].calculated.r_GiO_O - tailrotor_mechanics.limb_rotating[i].calculated.r_JO_O, tailrotor_mechanics.limb_rotating[i].calculated.r_GiO_O - tailrotor_mechanics.limb_rotating[i].calculated.r_HiO_O); 
                tailrotor_mechanics.limb_rotating[i].calculated.n_blade_O = Ay_RL_Omega * Matrix4x4.Rotate(Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_rotating[i].parameter.A_OLBi)) * new Vector3(0, 0, -1); // n_blade_O is the blade axis
            }
            // ##################################################################################



            // ##################################################################################
            // 6. Calculate position and orientation of 3D-objects
            // ##################################################################################
            if (rotor_3d_mechanics_geometry_available == true)
            {
                if (ab == 0) ab = 1; else ab = 0; // a => memory block 0,  b => memory block 1 

                servoarm_BA.localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_stationary.parameter.r_AO_O);
                servoarm_BA.localRotation[ab] =
                Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(-tailrotor_mechanics.limb_stationary.calculated.normal_servo_axis),
                                        Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_stationary.calculated.r_BO_O - tailrotor_mechanics.limb_stationary.parameter.r_AO_O));

                rod_CB.localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_stationary.calculated.r_BO_O); 
                rod_CB.localRotation[ab] =
                Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_stationary.calculated.r_CO_O - tailrotor_mechanics.limb_stationary.calculated.r_BO_O),
                                        Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_stationary.calculated.r_KO_O - tailrotor_mechanics.limb_stationary.calculated.r_BO_O));

                lever_D.localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_stationary.parameter.r_DO_O);
                lever_D.localRotation[ab] = 
                    Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(new Vector3(0, 0, 1)),
                            Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_stationary.calculated.r_CO_O - tailrotor_mechanics.limb_stationary.parameter.r_DO_O));

                sleeve_K.localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_stationary.calculated.r_KO_O);
                sleeve_K.localRotation[ab] = Helper.ConvertRightHandedToLeftHandedQuaternion(Helper.QuaternionFromMatrix9x1(tailrotor_mechanics.limb_stationary.parameter.A_OLK));

                // rotating objects
                mechanics_rotating.localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(new Vector3(0, 0, 0));
                mechanics_rotating.localRotation[ab] = Helper.ConvertRightHandedToLeftHandedQuaternion(Ay_LR_Omega.transpose.rotation);

                pitch_plate_J.localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_stationary.calculated.r_KO_O); // r_KO_O == r_JO_O
                pitch_plate_J.localRotation[ab] =
                    Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(new Vector3(0,-1, 0)),
                                            Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_rotating[0].calculated.r_GiO_O - tailrotor_mechanics.limb_stationary.calculated.r_KO_O));

                for (int i = 0; i < 2; i++)
                {
                    hinge_HG[i].localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_rotating[i].calculated.r_GiO_O);
                    hinge_HG[i].localRotation[ab] =
                    Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_rotating[i].calculated.normal_hinge_axis),
                                            Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_rotating[i].calculated.r_HiO_O - tailrotor_mechanics.limb_rotating[i].calculated.r_GiO_O));

                    blade[i].localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(new Vector3(0, 0, 0));
                    blade[i].localRotation[ab] =
                    Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_rotating[i].calculated.n_blade_O),
                                            Helper.ConvertRightHandedToLeftHandedVector(tailrotor_mechanics.limb_rotating[i].calculated.r_HiO_O));
                    //#if UNITY_EDITOR
                    //  if(i==0)
                    //  UnityEngine.Debug.Log("blade alpha: " + tailrotor_mechanics.limb_rotating[i].calculated.beta * Mathf.Rad2Deg);
                    //#endif
                }

            }
             
        }
        // ##################################################################################







        // ##################################################################################
        // To avoid flickering while reading the values from this thread by the main routine two memory blocks are reserved for the result. While one is filled, the other can be read.
        // ##################################################################################
        public void Update_3D_Objects()
        {
            // check at first if 3D geometry is available
            if (rotor_3d_mechanics_geometry_available == true)
            {
                // ##################################################################################
                // apply position and orientation to 3D-objects
                // ##################################################################################
                // stationary objects (nonrotating)
                int ab_inverted = ab == 0 ? 1 : 0; // a => memory block 0,  b => memory block 1 

                servoarm_BA.copy(ab_inverted);
                rod_CB.copy(ab_inverted);
                lever_D.copy(ab_inverted);
                sleeve_K.copy(ab_inverted);

                // rotating objects
                mechanics_rotating.copy(ab_inverted);
                pitch_plate_J.copy(ab_inverted);

                for (int i = 0; i < 2; i++)
                {
                    hinge_HG[i].copy(ab_inverted);
                    blade[i].copy(ab_inverted);
                }
            }
        }
        // ##################################################################################


        #endregion
        // ##################################################################################








        // ############################################################################
        //
        // ############################################################################
        [Serializable]
        public class stru_tailrotor_mechanics
        {
            public stru_state_in state_in { get; set; }
            public stru_state_out state_out { get; set; }
            public int blades_count { get; set; } // [-]
            //public Vector3 r_KO_O { get; set; } // [m]
            public stru_limb_stationary limb_stationary { get; set; }
            public stru_limb_rotating[] limb_rotating { get; set; }

            public stru_tailrotor_mechanics()
            {
                state_in = new stru_state_in();
                state_out = new stru_state_out();
                blades_count = new int();
                //r_KO_O = new Vector3();
                limb_stationary = new stru_limb_stationary();
                limb_rotating = new stru_limb_rotating[2];
                for (int i = 0; i < limb_rotating.Count(); i++) limb_rotating[i] = new stru_limb_rotating();  // Initialize array's elements
            }
        }

        [Serializable]
        public class stru_state_in
        {
            public float yaw { get; set; } // [rad] servo angle sigma    
            public float Omega { get; set; } // [rad] rotor rotation

            public stru_state_in()
            {
                yaw = new float();
                Omega = new float();
            }
        }

        [Serializable]
        public class stru_state_out
        {
            public float beta { get; set; } // [rad]  virtual blade angle

            public stru_state_out()
            {
                beta = new float(); // 
            }
        }

        [Serializable]
        public class stru_limb_stationary
        {
            public stru_limb_stationary_parameter parameter { get; set; }
            public stru_limb_stationary_calculated calculated { get; set; }

            public stru_limb_stationary()
            {
                parameter = new stru_limb_stationary_parameter();
                calculated = new stru_limb_stationary_calculated();
            }
        }

        [Serializable]
        public class stru_limb_rotating
        {
            public stru_limb_rotating_parameter parameter { get; set; }
            public stru_limb_rotating_calculated calculated { get; set; }

            public stru_limb_rotating()
            {
                parameter = new stru_limb_rotating_parameter();
                calculated = new stru_limb_rotating_calculated();
            }
        }

        [Serializable]
        public class stru_limb_stationary_calculated
        {
            public float sigma_commanded { get; set; } // [rad] servo arm angle (processvalue)
            public float sigma_actual { get; set; }  // [rad] servo arm angle (set-point), differs to sigma_commanded by possible slower motion 
            public Vector3 normal_servo_axis { get; set; } // [m] servo arm at inital position (sigma==0°)
            public Vector3 normal_lever_arm { get; set; } // [m] lever arm at inital position (sigma==0°)
            public float lever_rotation_angle { get; set; }  // [rad] 
            //public Vector3 normal_servo_axis { get; set; } // [m] servo arm roation axis
            //public Vector3 normal_servo_top { get; set; } // [m] servo arm normal at inital position (sigma==0°)
            //public Vector3 rod_normal { get; set; } // [m]
            public Vector3 r_BO_O { get; set; } // [m]
            public Vector3 r_CO_O { get; set; } // [m]
            public Vector3 r_EO_O { get; set; } // [m]
            public Vector3 r_FO_O { get; set; } // [m]
            public Vector3 r_KO_O { get; set; } // [m]

            public stru_limb_stationary_calculated()
            {
                sigma_commanded = new float();
                sigma_actual = new float();
                normal_servo_axis = new Vector3();
                normal_lever_arm = new Vector3();
                lever_rotation_angle = new float();
                //normal_servo_axis = new Vector3();
                //normal_servo_top = new Vector3();
                //rod_normal = new Vector3();
                r_BO_O = new Vector3();
                r_CO_O = new Vector3();
                r_EO_O = new Vector3();
                r_EO_O = new Vector3();
                r_KO_O = new Vector3();
            }
        }

        [Serializable]
        public class stru_limb_rotating_calculated
        {
            public Vector3 r_JO_O { get; set; } // [m]
            public Vector3 n_blade_O { get; set; } // [m] normal vector in blade direction
            public Vector3 normal_hinge_axis { get; set; } // [m] normal vector 
            public Vector3 r_GiO_O { get; set; } // [m]
            public Vector3 r_HiO_O { get; set; } // [m]
            public Vector3 r_HiIi_O { get; set; } // [m]
            public Vector3 r_IiO_O { get; set; } // [m]
            public float l_HiIi { get; set; } // [rad] blade angle
            public float beta { get; set; } // [rad] blade angle

            public stru_limb_rotating_calculated()
            {
                r_JO_O = new Vector3(); 
                n_blade_O = new Vector3();
                normal_hinge_axis = new Vector3();
                r_GiO_O = new Vector3();
                r_HiO_O = new Vector3();
                r_HiIi_O = new Vector3();
                r_IiO_O = new Vector3();
                l_HiIi = new float();
                beta = new float();
            }
        }






        [Serializable]
        public class stru_limb_stationary_parameter
        {
            public float[] A_OLA { get; set; } // [] transformation matrix A_OLA of servo axis (z_L is servo-shaft-rotation axis, y_L is servo arm direction)
            public Vector3 r_AO_O { get; set; } // [m]
            public Vector3 r_BA_LA { get; set; } // [m]
            public float[] A_OLD { get; set; } // [] transformation matrix A_OLD of lever axis (z_D is servo-shaft-rotation axis)
            public Vector3 r_DO_O { get; set; } // [m]
            public Vector3 r_CD_LD { get; set; } // [m]
            public Vector3 r_ED_LD { get; set; } // [m]
            public float[] A_OLK { get; set; } // [] transformation matrix A_OLK of sleeve axis (z_K is shaft-rotation axis)
            public Vector3 r_KO_O { get; set; } // [m]
            public float l_BA { get; set; } // [m]
            public float l_CB { get; set; } // [m]
            public float l_CD { get; set; } // [m]
            public float l_ED { get; set; } // [m]

            public stru_limb_stationary_parameter()
            {
                A_OLA = new float[9];
                r_AO_O = new Vector3();
                r_BA_LA = new Vector3();
                A_OLD = new float[9];
                r_DO_O = new Vector3();
                r_CD_LD = new Vector3();
                r_ED_LD = new Vector3();
                A_OLK = new float[9];
                r_KO_O = new Vector3();
                l_BA = new float();
                l_CB = new float();
                l_CD = new float();
                l_ED = new float();
            }
        }

        [Serializable]
        public class stru_limb_rotating_parameter
        {
            public float[] A_OLJ { get; set; } // [] transformation matrix A_OLJ of pitch-plate 
            public Vector3 r_JO_O { get; set; } // [m]
            public Vector3 r_GiJ_LJ { get; set; } // [m] pitch plate
            public float[] A_OLGi { get; set; } // [] transformation matrix A_OLG of hinge axis
            public Vector3 r_GiO_O { get; set; } // [m] hinge
            public Vector3 r_HiO_O { get; set; } // [m] hinge
            //public Vector3 r_HiGi_LGi { get; set; } // [m] hinge
            public float[] A_OLBi { get; set; } // [] transformation matrix A_OLBi of blade-i
            public Vector3 r_IiO_Bi { get; set; } // [m] 
            public float l_GiJ { get; set; } // [m]
            public float l_HiGi { get; set; } // [m]
            public float l_HiIi { get; set; } // [m]
            
            public stru_limb_rotating_parameter()
            {
                A_OLJ = new float[9];
                r_JO_O = new Vector3();
                r_GiJ_LJ = new Vector3();
                A_OLGi = new float[9];
                r_GiO_O = new Vector3();
                r_HiO_O = new Vector3();
                //r_HiGi_LGi = new Vector3();
                A_OLBi = new float[9];
                r_IiO_Bi = new Vector3();
                l_GiJ = new float();
                l_HiGi = new float();
                l_HiIi = new float(); 
            }
        }
        // ############################################################################








        [Serializable]
        public class stru_gameobject
        {
            public GameObject go { get; set; }
            public Vector3[] localPosition { get; set; } // [m]
            public Quaternion[] localRotation { get; set; }

            public stru_gameobject()
            {
                go = new GameObject();
                localPosition = new Vector3[2];
                localRotation = new Quaternion[2];
            }
            public void copy(int select_bank)
            {
                go.transform.localPosition = localPosition[select_bank];
                go.transform.localRotation = localRotation[select_bank];
            }
        }

    }
}
// ############################################################################