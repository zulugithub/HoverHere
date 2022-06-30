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


namespace Helicopter_Mainrotor_Mechanics
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
    public class Helicopter_Mainrotor_Mechanics 
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
        private stru_mainrotor_mechanics mainrotor_mechanics;

        public bool rotor_3d_mechanics_geometry_available = true;
        private stru_gameobject[] blade;
        private stru_gameobject mechanics_fixed;
        private stru_gameobject mechanics_rotating;
        private stru_gameobject[] rod_CB;
        private stru_gameobject[] rod_ED;
        private stru_gameobject[] servoarm_BA;
        private stru_gameobject swashpalte_rotating;
        private stru_gameobject swashpalte_stationary;
        private stru_gameobject ball;
        private stru_gameobject[] rotation_driver_FG;
        private stru_gameobject[] rotation_driver_GH;

        private Vector3 r_KO_O;
        private Vector3 sp_normal_actual;
        private Matrix4x4 Ay_LR_Omega;

        int ab = 0; // to avoid flickering while reading the values from this thread by the main routine two memory blocks are reserved for the result. While one is filled, the other can be read.

        //public readonly float[] sound_volume_servo = new float[4];
        //public readonly float[] sound_pitch_servo = new float[4];
        public readonly float[] servo_sigma = new float[4];
        //public readonly float[] servo_sigma_old = new float[4];
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
        /*public Helicopter_Mainrotor_Mechanics()
        {

        }*/


        public void Initialize(string helicopter_name, string rotor_name, Transform transform)
        {
            mainrotor_mechanics = new stru_mainrotor_mechanics();

            // load parameter, that are exported from blender
            TextAsset txtAsset = (TextAsset)Resources.Load(helicopter_name + "_mainrotor_mechanics", typeof(TextAsset));
            // if this helicopter do not have a rotor-mechanics.xml file use the LOGO600SE-V3 as default mechanics.
            if (txtAsset == null)
                txtAsset = (TextAsset)Resources.Load("Logo600SE_V3_mainrotor_mechanics", typeof(TextAsset));
            // get data
            if (txtAsset != null)
                mainrotor_mechanics = Helper.IO_XML_Deserialize<stru_mainrotor_mechanics>(txtAsset);


            // if 3D mechanics-geometry is available, then get the parts
            if (rotor_3d_mechanics_geometry_available == true)
            { 
                blade = new stru_gameobject[2]; for (int i = 0; i < 2; i++) blade[i] = new stru_gameobject();
                mechanics_fixed = new stru_gameobject();
                mechanics_rotating = new stru_gameobject();
                rod_CB = new stru_gameobject[3]; for (int i = 0; i < 3; i++) rod_CB[i] = new stru_gameobject();
                rod_ED = new stru_gameobject[2]; for (int i = 0; i < 2; i++) rod_ED[i] = new stru_gameobject();
                servoarm_BA = new stru_gameobject[3]; for (int i = 0; i < 3; i++) servoarm_BA[i] = new stru_gameobject();
                swashpalte_rotating = new stru_gameobject();
                swashpalte_stationary = new stru_gameobject();
                ball = new stru_gameobject();
                rotation_driver_FG = new stru_gameobject[2]; for (int i = 0; i < 2; i++) rotation_driver_FG[i] = new stru_gameobject();
                rotation_driver_GH = new stru_gameobject[2]; for (int i = 0; i < 2; i++) rotation_driver_GH[i] = new stru_gameobject();

                // assign geometies, that are exported from blender (and allready are added manually to the game)
                foreach (Transform eachChild in transform.gameObject.transform)
                {
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Blade0") blade[0].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Blade1") blade[1].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Mechanics_Fixed") mechanics_fixed.go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Mechanics_Rotating") mechanics_rotating.go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Rod_C0B0") rod_CB[0].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Rod_C1B1") rod_CB[1].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Rod_C2B2") rod_CB[2].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Rod_E0D0") rod_ED[0].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Rod_E1D1") rod_ED[1].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Servoarm_B0A0") servoarm_BA[0].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Servoarm_B1A1") servoarm_BA[1].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Servoarm_B2A2") servoarm_BA[2].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Swashpalte_Rotating") swashpalte_rotating.go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Swashpalte_Stationary") swashpalte_stationary.go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Ball") ball.go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Swashplate_Driver_F0G0") rotation_driver_FG[0].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Swashplate_Driver_G0H0") rotation_driver_GH[0].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Swashplate_Driver_F1G1") rotation_driver_FG[1].go = eachChild.gameObject;
                    if (eachChild.name == helicopter_name + "_" + rotor_name + "_" + "Swashplate_Driver_G1H1") rotation_driver_GH[1].go = eachChild.gameObject;
                }
            }
        }




        // [rad], [rad], [m], [rad], [rad], [sec], [-]
        public void Calculate(float roll, float pitch, float collective, float Omega, ref float[] beta, float dtime, int integrator_function_call_number)
        {

            //blade_object = Helicopter_Selected.transform.Find(rotor_name + "_Model").gameObject.transform.GetChild(1).gameObject;
            //Vector3 forward = Helper.ConvertRightHandedToLeftHandedVector(new Vector3(1, 0, 0));
            //Vector3 upward = Helper.ConvertRightHandedToLeftHandedVector(new Vector3(0, 0.3f, 1));
            //blade_object.transform.localRotation = Quaternion.LookRotation(forward, upward);


            //mainrotor_mechanics.state.roll = 5 * Mathf.PI / 180; // [rad] roll - x
            //mainrotor_mechanics.state.pitch = -15 * Mathf.PI / 180; // [rad] pitch - z
            //mainrotor_mechanics.state.collective = -0.003f; // [m]  
            //mainrotor_mechanics.state.Omega = 20 * Mathf.PI / 180; // [rad] shaft rotation angle(around rotor system's y-axis)

            mainrotor_mechanics.state_in.roll = roll; // [rad] roll - x
            mainrotor_mechanics.state_in.pitch = pitch; // [rad] pitch - z
            mainrotor_mechanics.state_in.collective = collective; // [m]  
            mainrotor_mechanics.state_in.Omega = Omega; // [rad] shaft rotation angle(around rotor system's y-axis)





            // ##################################################################################
            // pre - calculations
            // ##################################################################################
            for (int i = 0; i < 3; i++) // three servos
            {
                mainrotor_mechanics.limb_stationary[i].calculated.normal_servo_arm = Helper.QuaternionFromMatrix9x1(mainrotor_mechanics.limb_stationary[i].parameter.A_OLAi) * mainrotor_mechanics.limb_stationary[i].parameter.r_BiAi_LAi;
                mainrotor_mechanics.limb_stationary[i].calculated.normal_servo_axis = Helper.QuaternionFromMatrix9x1(mainrotor_mechanics.limb_stationary[i].parameter.A_OLAi) * new Vector3(0, 0, 1);
                mainrotor_mechanics.limb_stationary[i].calculated.normal_servo_top = Helper.QuaternionFromMatrix9x1(mainrotor_mechanics.limb_stationary[i].parameter.A_OLAi) * new Vector3(1, 0, 0);
            }
            // ##################################################################################



            // ##################################################################################
            // 1.) define servo arm positions from tilting angles (roll, pitch) and axial position (collective) of swashplate  
            // ##################################################################################

            // swapshplate tilting, A_LR = Ax * Az (Body31),     -->  1.) pitch around z   2.) roll around x'   
            Matrix4x4 A_com_LKO = Helper.Ax_LR(mainrotor_mechanics.state_in.roll) * Helper.Az_LR(mainrotor_mechanics.state_in.pitch);
            Vector3 r_KOcom_O = new Vector3(0, mainrotor_mechanics.r_KO_O.y + mainrotor_mechanics.state_in.collective, 0); // [m] midpoint "M" of swashplate 

            // calculate Ci - joint positions on stationary part of swashpalte
            Vector3 sp_normal_commanded = A_com_LKO.transpose * new Vector3(0, 1, 0); // swashpalte normal

            // build transformation matrix from normal "n" and the fact, that the z-component of e_xL_R of the transforamtion matrix must be zero (due to the joint
            // at Cs, C0's z-component must be zero). --> "e_xL_R"'s z-component is zero, if cross(n0,[0 0 -1]) is used.
            Vector3 forward = sp_normal_commanded;      // swashpalte normal
            Vector3 upward = new Vector3(0, 0, -1);
            Vector3 e_yLK_O = forward;                  // Building A_RL from e_xL_R, e_yL_R, e_yL_R, see Hubert Hahn 2.48d...)
            Vector3 e_xLK_O = Helper.Cross(upward, forward);   // "e_xL_R"'s z-component is zero, if cross(n0,[0 0 -1]) is used
            Vector3 e_zLK_O = Helper.Cross(e_xLK_O, e_yLK_O);
            Matrix4x4 A_com_OLK = Helper.A_RL(e_xLK_O, e_yLK_O, e_zLK_O);

            // find itarative the three servo arm angles necessary to achieve the position and orientation of the swashplate
            for (int i = 0; i < 3; i++)
            {
                mainrotor_mechanics.limb_stationary[i].calculated.r_CiO_O = r_KOcom_O + (Vector3)(A_com_OLK * mainrotor_mechanics.limb_stationary[i].parameter.r_CiK_LK);

                // servo arm start position: r_BiO_O
                mainrotor_mechanics.limb_stationary[i].calculated.sigma_commanded = 0; // start at servo angles sigma_i == 0°
                mainrotor_mechanics.limb_stationary[i].calculated.r_BiO_O = mainrotor_mechanics.limb_stationary[i].parameter.r_AiO_O + // <-- this line: add servo position
                    (Vector3)(Matrix4x4.Rotate(Helper.QuaternionFromMatrix9x1(mainrotor_mechanics.limb_stationary[i].parameter.A_OLAi)) * // <-- this line: transform arm- to world-coodriante system ( world-coodriante O is defined at rotorhead)
                    Helper.Az_LR(mainrotor_mechanics.limb_stationary[i].calculated.sigma_commanded) * mainrotor_mechanics.limb_stationary[i].parameter.r_BiAi_LAi); // <-- this line: rotate servo arm in servo-coordinate-system (around local z - axis)

                // do several iteration loops
                for (int j = 0; j < 10; j++)
                {
                    // 1.) first step: to define direction from Ci to Bi as "intermediate r_BiCi_O"
                    Vector3 r_BiCi_O = mainrotor_mechanics.limb_stationary[i].calculated.r_BiO_O - mainrotor_mechanics.limb_stationary[i].calculated.r_CiO_O;
                    // 2.) second step: the "intermediate r_BiCi_O" is normalized to reperesent a direction and multiplied with the known length of this vector, so we get with r_CiO_O a new r_BiO_O_ with the correct rod length
                    Vector3 r_BiO_O_ = mainrotor_mechanics.limb_stationary[i].calculated.r_CiO_O + r_BiCi_O.normalized * mainrotor_mechanics.limb_stationary[i].parameter.l_CiBi;
                    // 3.) third step: project point Bi onto a plane in 3D, plane is definde by servo-rotation axis and Ai  (https://stackoverflow.com/questions/9605556/how-to-project-a-point-onto-a-plane-in-3d) 
                    Vector3 Bi_proj = (r_BiO_O_ - mainrotor_mechanics.limb_stationary[i].calculated.normal_servo_axis * Vector3.Dot(r_BiO_O_ - mainrotor_mechanics.limb_stationary[i].parameter.r_AiO_O, mainrotor_mechanics.limb_stationary[i].calculated.normal_servo_axis));
                    // 4.) fourth step: 
                    Vector3 r_BjAi_O = Bi_proj - mainrotor_mechanics.limb_stationary[i].parameter.r_AiO_O;
                    // 5.) fifth step: update point Bi position by correcting rod length
                    mainrotor_mechanics.limb_stationary[i].calculated.r_BiO_O = mainrotor_mechanics.limb_stationary[i].parameter.r_AiO_O + r_BjAi_O.normalized * mainrotor_mechanics.limb_stationary[i].parameter.l_BiAi;
                }

                // calculate servo arm angle
                Vector3 r_BiAi_O = mainrotor_mechanics.limb_stationary[i].calculated.r_BiO_O - mainrotor_mechanics.limb_stationary[i].parameter.r_AiO_O; // servo arm vector
                mainrotor_mechanics.limb_stationary[i].calculated.sigma_commanded = Mathf.Acos(Mathf.Clamp(Vector3.Dot(mainrotor_mechanics.limb_stationary[i].calculated.normal_servo_arm, r_BiAi_O) / (mainrotor_mechanics.limb_stationary[i].calculated.normal_servo_arm.magnitude * r_BiAi_O.magnitude), -1.0f, 1.0f)); // angle between °0 position of servo arm and resulting servo arm vector

                // angle between two vector returns only servo angle, but we need also the sign
                if (Vector3.Dot(r_BiAi_O, mainrotor_mechanics.limb_stationary[i].calculated.normal_servo_top) > 0) mainrotor_mechanics.limb_stationary[i].calculated.sigma_commanded *= -1;
            }
            // ##################################################################################




            // ##################################################################################
            // here would servo dynamics be simulated // TODO
            // ##################################################################################
            // ... just to show, where the ODE - simulation would be implemented in the game/ simulation 
            //float smoothTime = 0.100f;
            for (int i = 0; i < 3; i++) 
            {
                //mainrotor_mechanics.limb_stationary[i].calculated.sigma_actual = Mathf.SmoothDamp(mainrotor_mechanics.limb_stationary[i].calculated.sigma_actual, mainrotor_mechanics.limb_stationary[i].calculated.sigma_commanded, ref xServoVelocity[i], smoothTime);
                mainrotor_mechanics.limb_stationary[i].calculated.sigma_actual = mainrotor_mechanics.limb_stationary[i].calculated.sigma_commanded;  // sigma_actual differs to sigma_commanded by possible slower motion, if implemented

                // sound
                if (integrator_function_call_number == 0)
                {
                    //servo_sigma[i] = mainrotor_mechanics.limb_stationary[i].calculated.sigma_actual; // [rad]
                    //float servo_speed = (servo_sigma_old[i] - servo_sigma[i]) / dtime; // [rad/sec]
                    //servo_sigma_old[i] = servo_sigma[i];
                    //sound_pitch_servo[i] = 1; // Mathf.Clamp(Mathf.Abs(servo_speed) * 1.0000000f, 0, 1);  
                    //sound_volume_servo[i] = Helper.Step(Mathf.Abs(servo_speed) * 500.0000000f, 0.5f, 0, 1.5f, 1);
                    servo_sigma[i] = mainrotor_mechanics.limb_stationary[i].calculated.sigma_actual; // [rad] // for servo sound
                }
            }
            // ##################################################################################





            // ##################################################################################
            // 2.) calculate swashplate orientation and position from servo angles
            // ##################################################################################
            // rotate servo arms
            for (int i = 0; i < 3; i++)
            {
                mainrotor_mechanics.limb_stationary[i].calculated.r_BiO_O = mainrotor_mechanics.limb_stationary[i].parameter.r_AiO_O + // <-- this line: add servo position
                  (Vector3)(Matrix4x4.Rotate(Helper.QuaternionFromMatrix9x1(mainrotor_mechanics.limb_stationary[i].parameter.A_OLAi)) * // <-- this line: transform arm to world coodriante system (defined at rotorhead)
                  Helper.Az_LR(mainrotor_mechanics.limb_stationary[i].calculated.sigma_actual) * mainrotor_mechanics.limb_stationary[i].parameter.r_BiAi_LAi); // <-- this line: rotate servo arm in servo-coordinate-system (around local z - axis)
            }

            // find swashplate orientation and center position(intersection with global y - axis) by iteration --> set initial rod direction
            for (int i = 0; i < 3; i++)
                mainrotor_mechanics.limb_stationary[i].calculated.rod_normal = new Vector3(0, 1, 0); // set initial direction

            // do several iteration loops
            sp_normal_actual = new Vector3(0, 1, 0);
            r_KO_O = new Vector3(0, 1, 0);
            for (int j = 0; j < 10; j++)
            {
                // plane throug C0, C1, C2 --> swashplate normal 
                Vector3 r_C0O_O = (mainrotor_mechanics.limb_stationary[0].calculated.r_BiO_O + mainrotor_mechanics.limb_stationary[0].calculated.rod_normal * mainrotor_mechanics.limb_stationary[0].parameter.l_CiBi);  // support vector for plane is limb0's point B0 + rod0_normal*l_C0B0
                Vector3 r_C1C0_O = (mainrotor_mechanics.limb_stationary[1].calculated.r_BiO_O + mainrotor_mechanics.limb_stationary[1].calculated.rod_normal * mainrotor_mechanics.limb_stationary[1].parameter.l_CiBi) - r_C0O_O;
                Vector3 r_C2C0_O = (mainrotor_mechanics.limb_stationary[2].calculated.r_BiO_O + mainrotor_mechanics.limb_stationary[2].calculated.rod_normal * mainrotor_mechanics.limb_stationary[2].parameter.l_CiBi) - r_C0O_O;
                sp_normal_actual = Helper.Cross(r_C1C0_O, r_C2C0_O).normalized; // swashplate normal e_yLK_O

                // find intersection point of plane with global-y - axis(point M).
                r_KO_O = new Vector3(0, Vector3.Dot(sp_normal_actual, r_C0O_O) / sp_normal_actual[1], 0);           // See syms solution (res ======> y = (n1*p1 + n2*p2 + n3*p3)/n2)

                // build transformation matrix from normal n0 and the fact, that the z-component of e_xL_R of the transforamtion matrix must be zero(due to the joint
                // at Cs, C0's z-component must be zero). "e_xL_R"'s z - component is zero, if cross(n0,[0 0 - 1]) is used.
                forward = sp_normal_actual;             // this will be the y-axis of the new system
                upward = new Vector3(0, 0, -1);         // upward defines with cross(upward, forward) => e_xL_R, so upward is not an axis of the new system (only, if is perprendicular to forward)
                e_yLK_O = forward;                      // Building A_RL from e_xL_R, e_yL_R, e_yL_R, see Hubert Hahn 2.48d...)
                e_xLK_O = Helper.Cross(upward, forward);       // "e_xL_R"'s z-component is zero, if cross(n0,[0 0 -1]) is used
                e_zLK_O = Helper.Cross(e_xLK_O, e_yLK_O);
                Matrix4x4 A_OLK = Helper.A_RL(e_xLK_O, e_yLK_O, e_zLK_O);

                for (int i = 0; i < 3; i++)
                {
                    mainrotor_mechanics.limb_stationary[i].calculated.r_CiO_O = r_KO_O + (Vector3)(A_OLK * mainrotor_mechanics.limb_stationary[i].parameter.r_CiK_LK);

                    //calculate new rod- normal for next iteration loop
                    Vector3 r_CiBi_O = (mainrotor_mechanics.limb_stationary[i].calculated.r_CiO_O - mainrotor_mechanics.limb_stationary[i].calculated.r_BiO_O);
                    mainrotor_mechanics.limb_stationary[i].calculated.rod_normal = r_CiBi_O.normalized;
                }
            }
            // ##################################################################################






            // ##################################################################################
            // 3.) calculate blade angle from swashplate orientation and position 
            // ##################################################################################
            Ay_LR_Omega = Helper.Ay_LR(mainrotor_mechanics.state_in.Omega); // shaft rotation
            //Vector3 n_rot_x = Ay_LR_Omega.transpose * new Vector3(1, 0, 0);  // AyLR' * [1 0 0]
            Vector3 n_rot_z = Ay_LR_Omega.transpose * new Vector3(0, 0, 1);  // AyLR' * [0 0 1]
            Vector3 n_rot_z_neg = Ay_LR_Omega.transpose * new Vector3(0, 0, -1);  // AyLR' * [0 0 1]

            // The roation_driver 0 (point H0) defines the rotation of the swashplate
            // the calcuation can be imagined as the intersection of two planes, described by two plane-normals. The plane-plane intersection creates a vector (H0 lies on this vector).
            // This vector forms together with the swashplate normal, by calculating the cross product of both, a third vector. All three vector are perpendicular to each other and form
            // the transformation matrix for the rotating swashplate part A_{L_JO}^{O}. With A_{L_JO}^{O} all rotating-swashplate local points can be transformed into the global reference frame (as a function of Omega and swashplate-normal).
            forward = sp_normal_actual;               // this will be the y-axis of the new system
            upward = n_rot_z_neg;                     // upward defines with cross(upward, forward) => e_xL_R, so upward is not an axis of the new system (only, if is perprendicular to forward)
            Vector3 e_yLJ_O = forward;                // Building A_RL from e_xL_R, e_yL_R, e_yL_R, see Hubert Hahn 2.48d...)
            Vector3 e_xLJ_O = Helper.Cross(upward, forward); // "e_xL_R"'s z-component is zero, if cross(n0,[0 0 -1]) is used
            Vector3 e_zLJ_O = Helper.Cross(e_xLJ_O, e_yLJ_O);
            Matrix4x4 A_OLJ = Helper.A_RL(e_xLJ_O, e_yLJ_O, e_zLJ_O); // A_RL also normalizes vetors

            for (int i = 0; i < 2; i++)
            {
                float Omega_offset = ((360f / mainrotor_mechanics.blades_count) * i) * Mathf.Deg2Rad; // [rad]
                mainrotor_mechanics.limb_rotating[i].calculated.r_DiO_O = r_KO_O + (Vector3)(A_OLJ * mainrotor_mechanics.limb_rotating[i].parameter.r_DiJ_LJ);
                mainrotor_mechanics.limb_rotating[i].calculated.n_blade_O = Helper.Ay_LR(mainrotor_mechanics.state_in.Omega + Omega_offset).transpose * new Vector3(1, 0, 0); // n_blade_O is the blade axis

                // first point E at 0° blade beta
                mainrotor_mechanics.limb_rotating[i].calculated.r_EiO_O = Ay_LR_Omega.transpose * mainrotor_mechanics.limb_rotating[i].parameter.r_EiO_O;

                // do several iteration loops
                for (int j = 0; j < 10; j++)
                {
                    // 1.) first step: to define direction from Di to Ei as "intermediate r_EiO_O"
                    Vector3 r_EiDi_O = mainrotor_mechanics.limb_rotating[i].calculated.r_EiO_O - mainrotor_mechanics.limb_rotating[i].calculated.r_DiO_O; // r_EiO_O is not the correct value so far, so this r_EiDi_O is an intermediate variable only
                    // 2.) second step: the "intermediate r_EiO_O" is normalized to reperesent a direction and multiplied with the known length of this vector, so we get with r_DiO_O a new r_EiO_O_temp with the correct rod length
                    Vector3 r_EiO_O_temp = mainrotor_mechanics.limb_rotating[i].calculated.r_DiO_O + r_EiDi_O.normalized * mainrotor_mechanics.limb_rotating[i].parameter.l_EiDi;
                    // 3.) third step: project Ei point onto a plane in 3D, plane is definde by blade-rotation axis and O (https://stackoverflow.com/questions/9605556/how-to-project-a-point-onto-a-plane-in-3d)
                    Vector3 Ei_proj = r_EiO_O_temp - mainrotor_mechanics.limb_rotating[i].calculated.n_blade_O * Vector3.Dot(r_EiO_O_temp, mainrotor_mechanics.limb_rotating[i].calculated.n_blade_O);
                    // 4.) fourth step:
                    Vector3 r_Ei0_O = Ei_proj; // - new Vector3(0,0,0) because the origin is already at (0,0,0) this step is not necessary
                    // 5.) fifth step: update point Ei position by correcting rod length
                    mainrotor_mechanics.limb_rotating[i].calculated.r_EiO_O = r_Ei0_O.normalized * mainrotor_mechanics.limb_rotating[i].parameter.l_EiO;
                }

                // calculate blade angle (beta) 
                Vector3 r_Ei0O_O = Helper.Cross(mainrotor_mechanics.limb_rotating[i].calculated.n_blade_O, new Vector3(0, 1, 0)).normalized * mainrotor_mechanics.limb_rotating[i].parameter.l_EiO;  // blade direction vector at beta_i = °0
                Vector3 r_EiO_O = mainrotor_mechanics.limb_rotating[i].calculated.r_EiO_O; // blade arm vector
                mainrotor_mechanics.limb_rotating[i].calculated.beta = Mathf.Acos(Mathf.Clamp(Vector3.Dot(r_Ei0O_O, r_EiO_O) / (r_Ei0O_O.magnitude * r_EiO_O.magnitude), -1.0f, 1.0f)); // [rad] angle between °0 position of blade arm and resulting blade arm vector. clamp() needed because of rounding errors

                // angle between vector returns only servo angle, but we need also the sign
                if (Vector3.Dot(r_EiO_O, new Vector3(0, 1, 0)) > 0) mainrotor_mechanics.limb_rotating[i].calculated.beta *= -1; // [rad]  
            }
            // ##################################################################################





            // ##################################################################################
            // 3x.) calculate virtual blade angle from swashplate orientation and position 
            // ##################################################################################
            int blades_count_virtual = 10; // c_n

            for (int c = 0; c < blades_count_virtual; c++)
            {
                float Omega_virtual = (c * (360f / blades_count_virtual)) * (Mathf.Deg2Rad); // [rad] Psi
                Matrix4x4 Ay_LR_Omega_virtual = Helper.Ay_LR(Omega_virtual); // shaft rotation
                //Vector3 n_rot_x = Ay_LR_Omega.transpose * new Vector3(1, 0, 0);  // AyLR' * [1 0 0]
                Vector3 n_rot_z_neg_virtual = Ay_LR_Omega_virtual.transpose * new Vector3(0, 0, -1);  // AyLR' * [0 0 1]

                // The roation_driver 0 (point H0) defines the rotation of the swashplate
                // the calcuation can be imagined as the intersection of two planes, described by two plane-normals. The plane-plane intersection creates a vector (H0 lies on this vector).
                // This vector forms together with the swashplate normal, by calculating the cross product of both, a third vector. All three vector are perpendicular to each other and form
                // the transformation matrix for the rotating swashplate part A_{L_JO}^{O}. With A_{L_JO}^{O} all rotating-swashplate local points can be transformed into the global reference frame (as a function of Omega and swashplate-normal).
                forward = sp_normal_actual;               // this will be the y-axis of the new system
                upward = n_rot_z_neg_virtual;                     // upward defines with cross(upward, forward) => e_xL_R, so upward is not an axis of the new system (only, if is perprendicular to forward)
                Vector3 e_yLJ_O_virtual = forward;                // Building A_RL from e_xL_R, e_yL_R, e_yL_R, see Hubert Hahn 2.48d...)
                Vector3 e_xLJ_O_virtual = Helper.Cross(upward, forward); // "e_xL_R"'s z-component is zero, if cross(n0,[0 0 -1]) is used
                Vector3 e_zLJ_O_virtual = Helper.Cross(e_xLJ_O_virtual, e_yLJ_O_virtual);
                Matrix4x4 A_OLJ_virtual = Helper.A_RL(e_xLJ_O_virtual, e_yLJ_O_virtual, e_zLJ_O_virtual); // A_RL also normalizes vetors

                for (int i = 0; i < 1; i++)
                {
                    float Omega_offset = 0; //  ((360f / mainrotor_mechanics.blades_count) * i) * Mathf.Deg2Rad; // [rad]
                    Vector3 r_DiO_O_virtual = r_KO_O + (Vector3)(A_OLJ_virtual * mainrotor_mechanics.limb_rotating[i].parameter.r_DiJ_LJ);
                    Vector3 n_blade_O_virtual = Helper.Ay_LR(Omega_virtual + Omega_offset).transpose * new Vector3(1, 0, 0); // n_blade_O is the blade axis

                    // first point E at 0° blade beta
                    Vector3 r_EiO_O_virtual = Ay_LR_Omega_virtual.transpose * mainrotor_mechanics.limb_rotating[i].parameter.r_EiO_O;

                    // do several iteration loops
                    for (int j = 0; j < 10; j++)
                    {
                        // 1.) first step: to define direction from Di to Ei as "intermediate r_EiO_O"
                        Vector3 r_EiDi_O = r_EiO_O_virtual - r_DiO_O_virtual; // r_EiO_O is not the correct value so far, so this r_EiDi_O is an intermediate variable only
                        // 2.) second step: the "intermediate r_EiO_O" is normalized to reperesent a direction and multiplied with the known length of this vector, so we get with r_DiO_O a new r_EiO_O_temp with the correct rod length
                        Vector3 r_EiO_O_temp = r_DiO_O_virtual + r_EiDi_O.normalized * mainrotor_mechanics.limb_rotating[i].parameter.l_EiDi;
                        // 3.) third step: project Ei point onto a plane in 3D, plane is definde by blade-rotation axis and O (https://stackoverflow.com/questions/9605556/how-to-project-a-point-onto-a-plane-in-3d)
                        Vector3 Ei_proj = r_EiO_O_temp - n_blade_O_virtual * Vector3.Dot(r_EiO_O_temp, n_blade_O_virtual);
                        // 4.) fourth step:
                        Vector3 r_Ei0_O = Ei_proj; // - new Vector3(0,0,0) because the origin is already at (0,0,0) this step is not necessary
                        // 5.) fifth step: update point Ei position by correcting rod length
                        r_EiO_O_virtual = r_Ei0_O.normalized * mainrotor_mechanics.limb_rotating[i].parameter.l_EiO;
                    }

                    // calculate blade angle (beta) 
                    Vector3 r_Ei0O_O_virtual = Helper.Cross(n_blade_O_virtual, new Vector3(0, 1, 0)).normalized * mainrotor_mechanics.limb_rotating[i].parameter.l_EiO;  // blade direction vector at beta_i = °0
                    //r_EiO_O_virtual = r_EiO_O_virtual; // blade arm vector
                    mainrotor_mechanics.state_out.beta[c] = -Mathf.Acos(Mathf.Clamp(Vector3.Dot(r_Ei0O_O_virtual, r_EiO_O_virtual) / (r_Ei0O_O_virtual.magnitude * r_EiO_O_virtual.magnitude), -1.0f, 1.0f)); // [rad] angle between °0 position of blade arm and resulting blade arm vector. clamp() needed because of rounding errors

                    // angle between vector returns only servo angle, but we need also the sign
                    if (Vector3.Dot(r_EiO_O_virtual, new Vector3(0, 1, 0)) > 0) mainrotor_mechanics.state_out.beta[c] *= -1; // [rad] 

                    beta[c] = mainrotor_mechanics.state_out.beta[c];
                }
            }

            /*string str= "Beta: ";
            for (int c = 0; c < blades_count_virtual; c++)
                str = str +  (mainrotor_mechanics.state_out.beta[c]*(Mathf.Rad2Deg)).ToString("000.#")  + "  " ;
            UnityEngine.Debug.Log(str);*/
            // ##################################################################################






            // ##################################################################################
            // 4.) calculate position of rotation-driver arms - not mathematically neccessary, only for visualizaton
            // ##################################################################################
            float signum;
            for (int i = 0; i < 2; i++)
            {
                signum = i == 0 ? +1 : -1;
                // find r_H1J_O on rotating part of swashplate
                mainrotor_mechanics.driver_rotating[i].calculated.r_HiO_O = r_KO_O + (Vector3)(A_OLJ * new Vector3(signum * mainrotor_mechanics.driver_rotating[i].parameter.l_HiJ, 0, 0));

                // rotate r_HiO_O back into global xy-plane to reduce the 3D-problem to a 2D-problem
                mainrotor_mechanics.driver_rotating[i].calculated.r_HiO_O = Ay_LR_Omega * mainrotor_mechanics.driver_rotating[i].calculated.r_HiO_O; // AyLR * r_HiO_O

                // 2D problem in xy-plane: How can I find the points at which two circles intersect ?
                // (https://math.stackexchange.com/questions/256100/how-can-i-find-the-points-at-which-two-circles-intersect)
                float r1 = mainrotor_mechanics.driver_rotating[i].parameter.l_GiFi * 1000; // [m]->[mm] because of numeric stability, othervise R^4 reaches float's limits 
                float r2 = mainrotor_mechanics.driver_rotating[i].parameter.l_HiGi * 1000;
                float x1 = mainrotor_mechanics.driver_rotating[i].parameter.r_FiO_O.x * 1000;
                float y1 = mainrotor_mechanics.driver_rotating[i].parameter.r_FiO_O.y * 1000;
                float x2 = mainrotor_mechanics.driver_rotating[i].calculated.r_HiO_O.x * 1000;
                float y2 = mainrotor_mechanics.driver_rotating[i].calculated.r_HiO_O.y * 1000;
                // find intersection points of two circles in 2D space
                float R = Mathf.Sqrt(Mathf.Pow(x2 - x1, 2) + Mathf.Pow(y2 - y1, 2));
                Vector2 xy = 0.5f * new Vector2(x1 + x2, y1 + y2) + ((r1 * r1 - r2 * r2) / (2 * R * R)) * new Vector2(x2 - x1, y2 - y1) - signum * 0.5f * Mathf.Sqrt((2 * (r1 * r1 + r2 * r2) / (R * R)) - (Mathf.Pow(r1 * r1 - r2 * r2, 2) / (R * R * R * R)) - 1f) * new Vector2(y2 - y1, x1 - x2);
                // resort results
                mainrotor_mechanics.driver_rotating[i].calculated.r_GiO_O = new Vector3(xy.x, xy.y, 0) / 1000;

                // rotate all three points(from 2D to 3D)
                mainrotor_mechanics.driver_rotating[i].calculated.r_FiO_O = Ay_LR_Omega.transpose * mainrotor_mechanics.driver_rotating[i].parameter.r_FiO_O; // AyLR' * r_FiO_O 
                mainrotor_mechanics.driver_rotating[i].calculated.r_GiO_O = Ay_LR_Omega.transpose * mainrotor_mechanics.driver_rotating[i].calculated.r_GiO_O; // AyLR' * r_GiO_O  
                mainrotor_mechanics.driver_rotating[i].calculated.r_HiO_O = Ay_LR_Omega.transpose * mainrotor_mechanics.driver_rotating[i].calculated.r_HiO_O; // AyLR' * r_HiO_O
            }
            // ##################################################################################







            // ##################################################################################
            // calculate position and orientation of 3D-objects
            // ##################################################################################
            if (rotor_3d_mechanics_geometry_available == true)
            {
                if (ab == 0) ab = 1; else ab = 0; // a => memory block 0,  b => memory block 1 

                // stationary objects (nonrotating)
                swashpalte_stationary.localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(r_KO_O);
                swashpalte_stationary.localRotation[ab] = Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(sp_normal_actual),
                                            Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.limb_stationary[0].calculated.r_CiO_O - r_KO_O));

                for (int i = 0; i < 3; i++)
                {
                    servoarm_BA[i].localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.limb_stationary[i].parameter.r_AiO_O);
                    servoarm_BA[i].localRotation[ab] =
                    Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(-mainrotor_mechanics.limb_stationary[i].calculated.normal_servo_axis),
                                            Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.limb_stationary[i].calculated.r_BiO_O - mainrotor_mechanics.limb_stationary[i].parameter.r_AiO_O));

                    rod_CB[i].localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.limb_stationary[i].calculated.r_BiO_O);
                    rod_CB[i].localRotation[ab] =
                    Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.limb_stationary[i].calculated.r_CiO_O - mainrotor_mechanics.limb_stationary[i].calculated.r_BiO_O),
                                            Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.limb_stationary[i].calculated.r_BiO_O - r_KO_O));
                }

                // rotating objects
                mechanics_rotating.localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(new Vector3(0, 0, 0));
                mechanics_rotating.localRotation[ab] = Helper.ConvertRightHandedToLeftHandedQuaternion(Ay_LR_Omega.transpose.rotation);

                ball.localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(r_KO_O);
                ball.localRotation[ab] = Helper.ConvertRightHandedToLeftHandedQuaternion(Ay_LR_Omega.transpose.rotation);

                swashpalte_rotating.localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(r_KO_O);
                swashpalte_rotating.localRotation[ab] =
                    Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(sp_normal_actual),
                                            Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.limb_rotating[0].calculated.r_DiO_O - r_KO_O));

                for (int i = 0; i < 2; i++)
                {
                    rod_ED[i].localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.limb_rotating[i].calculated.r_DiO_O);
                    rod_ED[i].localRotation[ab] =
                    Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.limb_rotating[i].calculated.r_EiO_O - mainrotor_mechanics.limb_rotating[i].calculated.r_DiO_O),
                                            Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.limb_rotating[i].calculated.r_DiO_O - r_KO_O));

                    blade[i].localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(new Vector3(0, 0, 0));
                    blade[i].localRotation[ab] =
                    Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.limb_rotating[i].calculated.n_blade_O),
                                            Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.limb_rotating[i].calculated.r_EiO_O));
//#if UNITY_EDITOR
//                    if(i==0)
//                        UnityEngine.Debug.Log("blade alpha: " + mainrotor_mechanics.limb_rotating[i].calculated.beta * Mathf.Rad2Deg);
//#endif
                }

                for (int i = 0; i < 2; i++)
                {
                    signum = i == 0 ? -1 : +1;
                    rotation_driver_FG[i].localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.driver_rotating[i].calculated.r_FiO_O);
                    rotation_driver_FG[i].localRotation[ab] =
                    Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(Ay_LR_Omega.transpose * new Vector3(0, 0, signum)),
                                            Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.driver_rotating[i].calculated.r_GiO_O - mainrotor_mechanics.driver_rotating[i].calculated.r_FiO_O));

                    rotation_driver_GH[i].localPosition[ab] = Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.driver_rotating[i].calculated.r_GiO_O);
                    rotation_driver_GH[i].localRotation[ab] =
                    Quaternion.LookRotation(Helper.ConvertRightHandedToLeftHandedVector(Ay_LR_Omega.transpose * new Vector3(0, 0, signum)),
                                            Helper.ConvertRightHandedToLeftHandedVector(mainrotor_mechanics.driver_rotating[i].calculated.r_HiO_O - mainrotor_mechanics.driver_rotating[i].calculated.r_GiO_O));

                }
            }

        }
        // ##################################################################################







        // ##################################################################################
        // to avoid flickering while reading the values from this thread by the main routine two memory blocks are reserved for the result. While one is filled, the other can be read.
        // ##################################################################################
        public void Update_3D_Objects()
        {
            // check at first if 3D geometry is available
            if(rotor_3d_mechanics_geometry_available == true)
            { 
                // ##################################################################################
                // apply position and orientation to 3D-objects
                // ##################################################################################
                // stationary objects (nonrotating)
                int ab_inverted = ab == 0 ? 1 : 0; // a => memory block 0,  b => memory block 1 

                swashpalte_stationary.copy(ab_inverted);

                for (int i = 0; i < 3; i++)
                {
                    servoarm_BA[i].copy(ab_inverted);
                    rod_CB[i].copy(ab_inverted);
                }

                // rotating objects
                mechanics_rotating.copy(ab_inverted);
                ball.copy(ab_inverted);
                swashpalte_rotating.copy(ab_inverted);

                for (int i = 0; i < 2; i++)
                {
                    rod_ED[i].copy(ab_inverted);
                    blade[i].copy(ab_inverted);
                }

                for (int i = 0; i < 2; i++)
                {
                    rotation_driver_FG[i].copy(ab_inverted);
                    rotation_driver_GH[i].copy(ab_inverted);
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
        public class stru_mainrotor_mechanics
        {
            public stru_state_in state_in { get; set; }
            public stru_state_out state_out { get; set; }
            public int blades_count { get; set; } // [-]
            public Vector3 r_KO_O { get; set; } // [m]
            public stru_limb_stationary[] limb_stationary { get; set; }
            public stru_limb_rotating[] limb_rotating { get; set; }
            public stru_driver_rotating[] driver_rotating { get; set; }

            public stru_mainrotor_mechanics()
            {
                state_in = new stru_state_in();
                state_out = new stru_state_out();
                blades_count = new int();
                r_KO_O = new Vector3();
                limb_stationary = new stru_limb_stationary[3];
                limb_rotating = new stru_limb_rotating[2];
                driver_rotating = new stru_driver_rotating[2];
                for (int i = 0; i < limb_stationary.Count(); i++) limb_stationary[i] = new stru_limb_stationary(); // Initialize array's elements
                for (int i = 0; i < limb_rotating.Count(); i++) limb_rotating[i] = new stru_limb_rotating();  // Initialize array's elements
                for (int i = 0; i < driver_rotating.Count(); i++) driver_rotating[i] = new stru_driver_rotating();  // Initialize array's elements
            }
        }

        [Serializable]
        public class stru_state_in
        {
            public float roll { get; set; } // [rad]    1.) roll --> z --> psi
            public float pitch { get; set; } // [rad]   2.) pitch --> x --> phi
            public float collective { get; set; } // [m]
            public float Omega { get; set; } // [rad]

            public stru_state_in()
            {
                roll = new float();
                pitch = new float();
                collective = new float();
                Omega = new float();
            }
        }

        [Serializable]
        public class stru_state_out
        {
            public float[] beta { get; set; } // [rad]  virtual blades angle

            public stru_state_out()
            {
                beta = new float[10]; // n_c
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
        public class stru_driver_rotating
        {
            public stru_driver_rotating_parameter parameter { get; set; }
            public stru_driver_rotating_calculated calculated { get; set; }

            public stru_driver_rotating()
            {
                parameter = new stru_driver_rotating_parameter();
                calculated = new stru_driver_rotating_calculated();
            }
        }

        [Serializable]
        public class stru_limb_stationary_calculated
        {
            public float sigma_commanded { get; set; } // [rad] servo arm angle (processvalue)
            public float sigma_actual { get; set; }  // [rad] servo arm angle (set-point), differs to sigma_commanded by possible slower motion 
            public Vector3 normal_servo_arm { get; set; } // [m] servo arm at inital position (sigma==0°)
            public Vector3 normal_servo_axis { get; set; } // [m] servo arm roation axis
            public Vector3 normal_servo_top { get; set; } // [m] servo arm normal at inital position (sigma==0°)
            public Vector3 r_CiO_O { get; set; } // [m]
            public Vector3 r_BiO_O { get; set; } // [m]
            public Vector3 rod_normal { get; set; } // [m]

            public stru_limb_stationary_calculated()
            {
                sigma_commanded = new float();
                sigma_actual = new float();
                normal_servo_arm = new Vector3();
                normal_servo_axis = new Vector3();
                normal_servo_top = new Vector3();
                r_CiO_O = new Vector3();
                r_BiO_O = new Vector3();
                rod_normal = new Vector3();

            }
        }

        [Serializable]
        public class stru_limb_rotating_calculated
        {
            public float beta { get; set; } // [rad] blade angle
            public Vector3 n_blade_O { get; set; } // [m] normal vector in blade direction
            public Vector3 r_EiO_O { get; set; } // [m]
            public Vector3 r_DiO_O { get; set; } // [m]

            public stru_limb_rotating_calculated()
            {
                beta = new float();
                n_blade_O = new Vector3();
                r_EiO_O = new Vector3();
                r_DiO_O = new Vector3();
            }
        }

        [Serializable]
        public class stru_driver_rotating_calculated
        {
            public Vector3 r_FiO_O { get; set; } // [m]
            public Vector3 r_GiO_O { get; set; } // [m]
            public Vector3 r_HiO_O { get; set; } // [m]

            public stru_driver_rotating_calculated()
            {
                r_FiO_O = new Vector3();
                r_GiO_O = new Vector3();
                r_HiO_O = new Vector3();
            }
        }





        [Serializable]
        public class stru_limb_stationary_parameter
        {
            public float[] A_OLAi { get; set; } // [] transformation matrix A_OLA of servo axis (z_L is servo-shaft-rotation axis, y_L is servo arm direction)
            public Vector3 r_AiO_O { get; set; } // [m]
            public Vector3 r_BiAi_LAi { get; set; } // [m]
            public Vector3 r_CiK_LK { get; set; } // [m]
            public float l_BiAi { get; set; } // [m]
            public float l_CiBi { get; set; } // [m]

            public stru_limb_stationary_parameter()
            {
                A_OLAi = new float[9];
                r_AiO_O = new Vector3();
                r_BiAi_LAi = new Vector3();
                r_CiK_LK = new Vector3();
                l_BiAi = new float();
                l_CiBi = new float();
            }
        }

        [Serializable]
        public class stru_limb_rotating_parameter
        {
            public Vector3 r_DiJ_LJ { get; set; } // [m]
            public Vector3 r_EiO_O { get; set; } // [m]
            public float l_DiJ { get; set; } // [m]
            public float l_EiDi { get; set; } // [m]
            public float l_EiO { get; set; } // [m]

            public stru_limb_rotating_parameter()
            {
                r_DiJ_LJ = new Vector3();
                r_EiO_O = new Vector3();
                l_DiJ = new float();
                l_EiDi = new float();
                l_EiO = new float();
            }
        }

        [Serializable]
        public class stru_driver_rotating_parameter
        {
            public Vector3 r_FiO_O { get; set; } // [m]
            public float l_GiFi { get; set; } // [m]
            public float l_HiGi { get; set; } // [m]
            public float l_HiJ { get; set; } // [m]

            public stru_driver_rotating_parameter()
            {
                r_FiO_O = new Vector3();
                l_GiFi = new float();
                l_HiGi = new float();
                l_HiJ = new float();
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