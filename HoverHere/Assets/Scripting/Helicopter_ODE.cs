// ##################################################################################
// Free RC helicopter Simulator
// 20.01.2020 
// Copyright (c) zulu
//
// Unity c# code
// ##################################################################################
//
// Documents
//  Helicopter
//      115€: Cai, Guowei, Chen, Ben M., Lee, Tong Heng  (https://www.springer.com/de/book/9780857296344) 2011_Book_UnmannedRotorcraftSystems.pdf
//      FREE: Vladislav Gavrilets (https://core.ac.uk/download/pdf/4385472.pdf)  Dynamic Model for aMiniature Aerobatic Helicopter
//      FREE: Simon Lindblom & Adam Lundmark (http://liu.diva-portal.org/smash/get/diva2:821251/FULLTEXT01.pdf) Modelling and control of a hexarotor UAV
//      FREE: Martin Insulander (https://www.researchgate.net/publication/305723278_Development_of_a_Helicopter_Simulation_for_Operator_Interface_Research) Development of a Helicopter Simulation for Operator Interface Research
//      FREE: Heffley, Robert K. (https://ntrs.nasa.gov/search.jsp?R=19870015897) STUDY OF HELICOPTER ROLL CONTROL EFFECTIVENESS CRITERIA
// Brushless
//      FREE: Prof. Yon-Ping Chen (http://ocw.nctu.edu.tw/course/dssi032/DSSI_2.pdf) Modeling of DC Motor
//      FREE: http://www.drivecalc.de/
//            https://www.ecalc.ch/
//
// Solving ODEs in Unity:
//      FREE: https://joinerda.github.io/Solving-ODEs-in-Unity/ main program structure based on his examlpe
//      FREE: https://joinerda.github.io/Threading-In-Unity/ main program structure based on his examlpe
//
//
// https://www.researchgate.net/publication/220806807_Aggressive_Maneuvering_Flight_Tests_of_a_Miniature_Robotic_Helicopter ???
// http://www.ampere-lyon.fr/IMG/pdf/phd_book_skandertaamallah_final.pdf ???
//
// Ascii text generator
// http://patorjk.com/software/taag/#p=display&f=Doh&t=ODE
//
// 
// ##################################################################################
//  Collsion does not work in build -> check http://docs.unity3d.com/Manual/LogFiles.html
// ##################################################################################
// XML https://gram.gs/gramlog/xml-serialization-and-deserialization-in-unity/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Common;
using System.Xml;
using System.Xml.Serialization;
using Parameter;
//using Newtonsoft.Json;
using Rotor;
//using MathNet.Numerics.LinearAlgebra;


namespace Helisimulator
{

    // ##################################################################################
    //                                            bbbbbbbb                                                                                                                                      
    //    DDDDDDDDDDDDD                           b::::::b                                                                           lllllll                                                    
    //    D::::::::::::DDD                        b::::::b                                                                           l:::::l                                                    
    //    D:::::::::::::::DD                      b::::::b                                                                           l:::::l                                                    
    //    DDD:::::DDDDD:::::D                      b:::::b                                                                           l:::::l                                                    
    //      D:::::D    D:::::D     eeeeeeeeeeee    b:::::bbbbbbbbb    uuuuuu    uuuuuu     ggggggggg   ggggg         cccccccccccccccc l::::l   aaaaaaaaaaaaa      ssssssssss       ssssssssss   
    //      D:::::D     D:::::D  ee::::::::::::ee  b::::::::::::::bb  u::::u    u::::u    g:::::::::ggg::::g       cc:::::::::::::::c l::::l   a::::::::::::a   ss::::::::::s    ss::::::::::s  
    //      D:::::D     D:::::D e::::::eeeee:::::eeb::::::::::::::::b u::::u    u::::u   g:::::::::::::::::g      c:::::::::::::::::c l::::l   aaaaaaaaa:::::ass:::::::::::::s ss:::::::::::::s 
    //      D:::::D     D:::::De::::::e     e:::::eb:::::bbbbb:::::::bu::::u    u::::u  g::::::ggggg::::::gg     c:::::::cccccc:::::c l::::l            a::::as::::::ssss:::::ss::::::ssss:::::s
    //      D:::::D     D:::::De:::::::eeeee::::::eb:::::b    b::::::bu::::u    u::::u  g:::::g     g:::::g      c::::::c     ccccccc l::::l     aaaaaaa:::::a s:::::s  ssssss  s:::::s  ssssss 
    //      D:::::D     D:::::De:::::::::::::::::e b:::::b     b:::::bu::::u    u::::u  g:::::g     g:::::g      c:::::c              l::::l   aa::::::::::::a   s::::::s         s::::::s      
    //      D:::::D     D:::::De::::::eeeeeeeeeee  b:::::b     b:::::bu::::u    u::::u  g:::::g     g:::::g      c:::::c              l::::l  a::::aaaa::::::a      s::::::s         s::::::s   
    //      D:::::D    D:::::D e:::::::e           b:::::b     b:::::bu:::::uuuu:::::u  g::::::g    g:::::g      c::::::c     ccccccc l::::l a::::a    a:::::assssss   s:::::s ssssss   s:::::s 
    //    DDD:::::DDDDD:::::D  e::::::::e          b:::::bbbbbb::::::bu:::::::::::::::uug:::::::ggggg:::::g      c:::::::cccccc:::::cl::::::la::::a    a:::::as:::::ssss::::::ss:::::ssss::::::s
    //    D:::::::::::::::DD    e::::::::eeeeeeee  b::::::::::::::::b  u:::::::::::::::u g::::::::::::::::g       c:::::::::::::::::cl::::::la:::::aaaa::::::as::::::::::::::s s::::::::::::::s 
    //    D::::::::::::DDD       ee:::::::::::::e  b:::::::::::::::b    uu::::::::uu:::u  gg::::::::::::::g        cc:::::::::::::::cl::::::l a::::::::::aa:::as:::::::::::ss   s:::::::::::ss  
    //    DDDDDDDDDDDDD            eeeeeeeeeeeeee  bbbbbbbbbbbbbbbb       uuuuuuuu  uuuu    gggggggg::::::g          ccccccccccccccccllllllll  aaaaaaaaaa  aaaa sssssssssss      sssssssssss    
    //                                                                                              g:::::g                                                                                     
    //                                                                                  gggggg      g:::::g                                                                                     
    //                                                                                  g:::::gg   gg:::::g                                                                                     
    //                                                                                   g::::::ggg:::::::g                                                                                     
    //                                                                                    gg:::::::::::::g                                                                                      
    // ##################################################################################   ggg::::::ggg                                                                                        
    #region debug_class
    // ##################################################################################
    // debug structure
    // ##################################################################################
    public class ODEDebugClass
    {
        public Vector3 veloLH; // in local frame

        public List<Vector3> contact_positionR { get; set; } // in reference frame
        public List<Vector3> contact_forceR1 { get; set; } // in reference frame
        public List<Vector3> contact_forceR2 { get; set; } // in reference frame
        public List<GameObject> line_object_contact_forceR1 { get; set; }
        public List<GameObject> line_object_contact_forceR2 { get; set; }

        public float mainrotor_v_i;// [m/s]
        public float Theta_col_mr; // [rad]
        public Vector3 mainrotor_forceLH;  // in local frame
        public Vector3 mainrotor_torqueLH; // in local frame
        public Vector3 mainrotor_positionO; // in reference frame
        public Vector3 mainrotor_forceO; // in reference frame
        public GameObject line_object_mainrotor_forceO { get; set; }
        public Vector3 mainrotor_torqueO;// in reference frame
        public GameObject line_object_mainrotor_torqueO { get; set; }
        public Vector3 mainrotor_flapping_stiffness_torqueO; // in reference frame
        public GameObject line_object_mainrotor_flapping_stiffness_torqueO { get; set; }

        public float tailrotor_v_i; // [m/s]
        public float Theta_col_tr; // [rad]
        public Vector3 tailrotor_forceLH; // in local frame
        public Vector3 tailrotor_torqueLH; // in local frame
        public Vector3 tailrotor_positionO; // in reference frame
        public Vector3 tailrotor_forceO; // in reference frame
        public GameObject line_object_tailrotor_forceO { get; set; }
        public Vector3 tailrotor_torqueO; // in reference frame
        public GameObject line_object_tailrotor_torqueO { get; set; }
        public Vector3 tailrotor_flapping_stiffness_torqueO; // in reference frame
        public GameObject line_object_tailrotor_flapping_stiffness_torqueO { get; set; }

        public float propeller_v_i; // [m/s]
        public float Theta_col_pr; // [rad]
        public Vector3 propeller_forceLH; // in local frame
        public Vector3 propeller_torqueLH; // in local frame
        public Vector3 propeller_positionO; // in reference frame
        public Vector3 propeller_forceO; // in reference frame
        public GameObject line_object_propeller_forceO { get; set; }
        public Vector3 propeller_torqueO; // in reference frame
        public GameObject line_object_propeller_torqueO { get; set; }

        public List<Vector3> booster_forceLH { get; set; } // in local frame
        public List<Vector3> booster_positionO { get; set; } // in reference frame
        public List<Vector3> booster_forceO { get; set; } // in reference frame
        public List<GameObject> line_object_booster_forceO { get; set; }

        public Vector3 drag_on_fuselage_positionO { get; set; } // in reference frame
        public Vector3 drag_on_fuselage_drag_on_fuselage_forceO { get; set; } // in reference frame
        public GameObject line_object_drag_on_fuselage_drag_on_fuselage_forceO { get; set; }

        public Vector3 force_on_horizontal_fin_positionO { get; set; } // in reference frame
        public Vector3 force_on_horizontal_fin_forceO { get; set; } // in reference frame
        public GameObject line_object_force_on_horizontal_fin_forceO { get; set; }

        public Vector3 force_on_vertical_fin_positionO { get; set; } // in reference frame
        public Vector3 force_on_vertical_fin_forceO { get; set; } // in reference frame
        public GameObject line_object_force_on_vertical_fin_forceO { get; set; }

        public Vector3 force_on_horizontal_wing_left_positionO { get; set; } // in reference frame
        public Vector3 force_on_horizontal_wing_left_forceO { get; set; } // in reference frame
        public GameObject line_object_force_on_horizontal_wing_left_forceO { get; set; }

        public Vector3 force_on_horizontal_wing_right_positionO { get; set; } // in reference frame
        public Vector3 force_on_horizontal_wing_right_forceO { get; set; } // in reference frame
        public GameObject line_object_force_on_horizontal_wing_right_forceO { get; set; }


        public string debug_text = "";
        public float P_mr_pr, P_mr_i, P_mr_pa, P_mr_c; // mainrotor power calculation results
        public float P_tr_pr, P_tr_i, P_tr_pa, P_tr_c; // tailrotor power calculation results
        public float P_pr_pr, P_pr_i, P_pr_pa, P_pr_c; // propeller power calculation results

        public float turbulence_mr;
        public float vortex_ring_state_mr;
        public float ground_effect_mr;
        public float flap_up_mr;
        //public float stall_mr;

        public float turbulence_tr;
        public float vortex_ring_state_tr;
        public float ground_effect_tr;
        public float flap_up_tr;
        //public float stall_tr;

        public float turbulence_pr;
        public float vortex_ring_state_pr;
        public float ground_effect_pr;
        public float flap_up_pr;
        //public float stall_pr;


        public List<List<GameObject>> line_object_BEMT_blade_segment_velocity { get; set; }
        public List<List<GameObject>> line_object_BEMT_blade_segment_thrust { get; set; }
        public List<List<GameObject>> line_object_BEMT_blade_segment_torque { get; set; }
        public List<List<Vector3>> BEMT_blade_segment_position { get; set; } 
        public List<List<Vector3>> BEMT_blade_segment_velocity { get; set; }
        public List<List<Vector3>> BEMT_blade_segment_thrust { get; set; }
        public List<List<Vector3>> BEMT_blade_segment_torque { get; set; }
    }
    // ##################################################################################
    #endregion





    // ##################################################################################
    //    MMMMMMMM               MMMMMMMM                    iiii                                                lllllll                                                    
    //    M:::::::M             M:::::::M                   i::::i                                               l:::::l                                                    
    //    M::::::::M           M::::::::M                    iiii                                                l:::::l                                                    
    //    M:::::::::M         M:::::::::M                                                                        l:::::l                                                    
    //    M::::::::::M       M::::::::::M  aaaaaaaaaaaaa   iiiiiiinnnn  nnnnnnnn                 cccccccccccccccc l::::l   aaaaaaaaaaaaa      ssssssssss       ssssssssss   
    //    M:::::::::::M     M:::::::::::M  a::::::::::::a  i:::::in:::nn::::::::nn             cc:::::::::::::::c l::::l   a::::::::::::a   ss::::::::::s    ss::::::::::s  
    //    M:::::::M::::M   M::::M:::::::M  aaaaaaaaa:::::a  i::::in::::::::::::::nn           c:::::::::::::::::c l::::l   aaaaaaaaa:::::ass:::::::::::::s ss:::::::::::::s 
    //    M::::::M M::::M M::::M M::::::M           a::::a  i::::inn:::::::::::::::n         c:::::::cccccc:::::c l::::l            a::::as::::::ssss:::::ss::::::ssss:::::s
    //    M::::::M  M::::M::::M  M::::::M    aaaaaaa:::::a  i::::i  n:::::nnnn:::::n         c::::::c     ccccccc l::::l     aaaaaaa:::::a s:::::s  ssssss  s:::::s  ssssss 
    //    M::::::M   M:::::::M   M::::::M  aa::::::::::::a  i::::i  n::::n    n::::n         c:::::c              l::::l   aa::::::::::::a   s::::::s         s::::::s      
    //    M::::::M    M:::::M    M::::::M a::::aaaa::::::a  i::::i  n::::n    n::::n         c:::::c              l::::l  a::::aaaa::::::a      s::::::s         s::::::s   
    //    M::::::M     MMMMM     M::::::Ma::::a    a:::::a  i::::i  n::::n    n::::n         c::::::c     ccccccc l::::l a::::a    a:::::assssss   s:::::s ssssss   s:::::s 
    //    M::::::M               M::::::Ma::::a    a:::::a i::::::i n::::n    n::::n         c:::::::cccccc:::::cl::::::la::::a    a:::::as:::::ssss::::::ss:::::ssss::::::s
    //    M::::::M               M::::::Ma:::::aaaa::::::a i::::::i n::::n    n::::n          c:::::::::::::::::cl::::::la:::::aaaa::::::as::::::::::::::s s::::::::::::::s 
    //    M::::::M               M::::::M a::::::::::aa:::ai::::::i n::::n    n::::n           cc:::::::::::::::cl::::::l a::::::::::aa:::as:::::::::::ss   s:::::::::::ss  
    //    MMMMMMMM               MMMMMMMM  aaaaaaaaaa  aaaaiiiiiiii nnnnnn    nnnnnn             ccccccccccccccccllllllll  aaaaaaaaaa  aaaa sssssssssss      sssssssssss    
    // ##################################################################################
    public class Helicopter_ODE : Helicopter_Integrator
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
        // Fields
        // ##################################################################################
        #region parameter
        private Helicopter_Main Helicopter_Main = GameObject.Find("Game_Controller").GetComponent<Helicopter_Main>();

        public bool flag_load_new_parameter_in_ODE_thread = false;
        public Parameter.Parameter par_temp = new Parameter.Parameter();
        public Parameter.Parameter par = new Parameter.Parameter();

        public double[] x_states;
        public double[] x_states_old;

        //static int log_output_frequency = 0;

        bool flag_motor_controller_saturation;
        float omega_mo_target_with_soft_start;

        public ODEDebugClass ODEDebug = new ODEDebugClass();
        public float thrust_mr_for_rotordisc_conical_deformation; // [N]  
        public float sound_volume_motor; // [-] 
        public float sound_volume_mainrotor; // [-] mainrotor
        public float sound_volume_tailrotor; // [-] tailrotor
        public float sound_volume_mainrotor_stall; // [-] mainrotor
        public float sound_volume_tailrotor_stall; // [-] tailrotor
        public bool flag_motor_enabled = true; // [-]
        public int flight_bank = 0; // [-]
        public float engine_restart_time; // [sec]
        public float engine_restart_time_stopped_time; // [sec]
        public enum flag_motor_start_speed { slow, fast };
        public flag_motor_start_speed flag_motor_start_slow_or_fast = flag_motor_start_speed.slow; // [-]

        double v_i_mr; // [m/sec] mainrotor induced velocity 
        double v_i_tr; // [m/sec] tailrotor induced velocity 
        double v_i_pr; // [m/sec] propeller induced velocity 

        Common.Helper.GroundMesh ground_mesh;

        List<Common.Helper.contact_info> contact_informations;

        public float collision_positions_landing_gear_left_rising_offset; //[m]
        public float collision_positions_landing_gear_right_rising_offset; //[m]
        public float collision_positions_landing_gear_steering_center_rising_offset; //[m]
        public float collision_positions_landing_gear_steering_left_rising_offset; //[m]
        public float collision_positions_landing_gear_steering_right_rising_offset; //[m]

        public bool collision_force_too_high_flag = false; 

        // debug variables
        public void Update_ODE_Debug_Variables()
        {
            ODEDebug.veloLH = new Vector3();

            int count = par_temp.transmitter_and_helicopter.helicopter.collision.positions_usual.vect3.Count +
                        par_temp.transmitter_and_helicopter.helicopter.collision.positions_left.vect3.Count + 
                        par_temp.transmitter_and_helicopter.helicopter.collision.positions_right.vect3.Count +
                        par_temp.transmitter_and_helicopter.helicopter.collision.positions_steering_center.vect3.Count +
                        par_temp.transmitter_and_helicopter.helicopter.collision.positions_steering_left.vect3.Count +
                        par_temp.transmitter_and_helicopter.helicopter.collision.positions_steering_right.vect3.Count;

            ODEDebug.contact_positionR = new List<Vector3>(new Vector3[count]);
            ODEDebug.contact_forceR1 = new List<Vector3>(new Vector3[count]);
            ODEDebug.contact_forceR2 = new List<Vector3>(new Vector3[count]);
            ODEDebug.line_object_contact_forceR1 = new List<GameObject>(new GameObject[count]);
            ODEDebug.line_object_contact_forceR2 = new List<GameObject>(new GameObject[count]);

            ODEDebug.mainrotor_forceLH = new Vector3();
            ODEDebug.mainrotor_torqueLH = new Vector3();
            ODEDebug.mainrotor_positionO = new Vector3();
            ODEDebug.mainrotor_forceO = new Vector3();
            ODEDebug.mainrotor_torqueO = new Vector3();
            ODEDebug.mainrotor_flapping_stiffness_torqueO = new Vector3();

            ODEDebug.tailrotor_forceLH = new Vector3();
            ODEDebug.tailrotor_torqueLH = new Vector3();
            ODEDebug.tailrotor_positionO = new Vector3();
            ODEDebug.tailrotor_forceO = new Vector3();
            ODEDebug.tailrotor_torqueO = new Vector3();
            ODEDebug.tailrotor_flapping_stiffness_torqueO = new Vector3();

            ODEDebug.propeller_positionO = new Vector3();
            ODEDebug.propeller_forceO = new Vector3();
            ODEDebug.propeller_torqueO = new Vector3();

            if(par_temp.transmitter_and_helicopter.helicopter.booster.booster_symmetric.val == false)
                count = 1;
            else
                count = 2;
            ODEDebug.booster_forceLH = new List<Vector3>(new Vector3[count]);
            ODEDebug.booster_positionO = new List<Vector3>(new Vector3[count]);
            ODEDebug.booster_forceO = new List<Vector3>(new Vector3[count]);
            ODEDebug.line_object_booster_forceO = new List<GameObject>(new GameObject[count]);

            ODEDebug.drag_on_fuselage_positionO = new Vector3();
            ODEDebug.drag_on_fuselage_drag_on_fuselage_forceO = new Vector3();

            ODEDebug.force_on_horizontal_fin_positionO = new Vector3();
            ODEDebug.force_on_horizontal_fin_forceO = new Vector3();

            ODEDebug.force_on_vertical_fin_positionO = new Vector3();
            ODEDebug.force_on_vertical_fin_forceO = new Vector3();

            ODEDebug.force_on_horizontal_wing_left_positionO = new Vector3();
            ODEDebug.force_on_horizontal_wing_left_forceO = new Vector3();

            ODEDebug.force_on_horizontal_wing_right_positionO = new Vector3();
            ODEDebug.force_on_horizontal_wing_right_forceO = new Vector3();



            ODEDebug.line_object_BEMT_blade_segment_velocity = new List<List<GameObject>>();
            ODEDebug.line_object_BEMT_blade_segment_thrust = new List<List<GameObject>>();
            ODEDebug.line_object_BEMT_blade_segment_torque = new List<List<GameObject>>();
            ODEDebug.BEMT_blade_segment_position = new List<List<Vector3>>(); 
            ODEDebug.BEMT_blade_segment_velocity = new List<List<Vector3>>();
            ODEDebug.BEMT_blade_segment_thrust = new List<List<Vector3>>();
            ODEDebug.BEMT_blade_segment_torque = new List<List<Vector3>>();

            int r_n = 4;  // radial steps - (polar coordiantes)
            int c_n = 10; // circumferencial steps - (polar coordiantes) - number of virtual blades
            for (int r = 0; r < r_n; r++)
            {
                ODEDebug.line_object_BEMT_blade_segment_velocity.Add(new List<GameObject>());
                ODEDebug.line_object_BEMT_blade_segment_thrust.Add(new List<GameObject>());
                ODEDebug.line_object_BEMT_blade_segment_torque.Add(new List<GameObject>());
                ODEDebug.BEMT_blade_segment_position.Add(new List<Vector3>()); 
                ODEDebug.BEMT_blade_segment_velocity.Add(new List<Vector3>());
                ODEDebug.BEMT_blade_segment_thrust.Add(new List<Vector3>());
                ODEDebug.BEMT_blade_segment_torque.Add(new List<Vector3>());


                for (int c = 0; c < c_n; c++)
                {
                    ODEDebug.line_object_BEMT_blade_segment_velocity[r].Add(new GameObject());
                    ODEDebug.line_object_BEMT_blade_segment_thrust[r].Add(new GameObject());
                    ODEDebug.line_object_BEMT_blade_segment_torque[r].Add(new GameObject());
                    ODEDebug.BEMT_blade_segment_position[r].Add(new Vector3()); 
                    ODEDebug.BEMT_blade_segment_velocity[r].Add(new Vector3());
                    ODEDebug.BEMT_blade_segment_thrust[r].Add(new Vector3());
                    ODEDebug.BEMT_blade_segment_torque[r].Add(new Vector3());

                }
            }
        }
        // ##################################################################################   




        // ##################################################################################
        // variables declaration 
        // ##################################################################################
        private double force_gravityO = 0; // [N] gravity in inertial coordinate system
        private Vector3 forcesO = Vector3.zero; // [N] force acting on fuselage in inertial coordinate system
        private Vector3 torquesLH = Vector3.zero; // [Nm] torque acting on fuselage in helicopter's local coordinate system 
        public double thrust_mr = 0; // [N] mainrotor thrust
        private double torque_mr = 0; // [Nm] mainrotor torque 
        private double omega_mr; // [rad/sec] mainrotor rotation velocity 
        private double Omega_mr; // [rad] mainrotor rotation angle
        private double thrust_tr = 0; // [N] tailrotor thrust
        private double torque_tr = 0; // [Nm] tailrotor torque 
        private double omega_tr; // [rad/sec] tailrotor rotation velocity 
        private double thrust_pr = 0; // [N] propeller thrust
        private double torque_pr = 0; // [Nm] propeller torque 
        private double thrust_bo = 0; // [N] booster thrust
        //private double torque_bo = 0; // [Nm] booster torque 
        private double omega_pr; // [rad/sec] propeller rotation velocity 
        private Quaternion q = new Quaternion(0, 0, 0, 1);  // unity: x, y, z, w --> [0], [1], [2], [3]
        private Vector3 vectO = Vector3.zero; // [m]
        //public Vector3 veloLH { get; protected set; } // = Vector3.zero; // [m/sec]
        private Vector3 veloLH = Vector3.zero; // [m/sec]
        private Vector3 veloO = Vector3.zero; // [m/sec]
        private Vector3 omegaLH = Vector3.zero; // [rad/sec] 
        //private Vector3 omegaO = Vector3.zero; // [rad/sec]
        private float velo_u_aLH; // [m/sec] longitudinal front-back direction local velocity
        private float velo_v_aLH; // [m/sec] vertical top-bottom direction local velocity    (y is w! in 2011_Book_UnmannedRotorcraftSystems.pdf)
        private float velo_w_aLH; // [m/sec] lateral left-rigth direction local velocity
        private Vector3 force_fuselageLH = Vector3.zero; // [N]
        private Vector3 force_fuselageO = Vector3.zero; // [N]
        private Vector3 torque_contactO = Vector3.zero; // [Nm]
        private Vector3 torque_contactLH = Vector3.zero; // [Nm]
        private Vector3 torque_contactLH_sum = Vector3.zero; // [Nm]
        private Vector3 torque_frictionO = Vector3.zero; // [Nm]
        private Vector3 torque_frictionLH = Vector3.zero; // [Nm]
        private Vector3 torque_frictionLH_sum = Vector3.zero; // [Nm]
        private Vector3 velo_wind_LH = Vector3.zero; // [m/sec]
        private Vector3 velo_wind_O = Vector3.zero; // [m/sec]
        private double dDELTA_x_roll__int_dt = 0;    // [rad/sec] flybarless error value integral
        private double dDELTA_y_yaw__int_dt = 0;     // [rad/sec] gyro error value integral
        private double dDELTA_z_pitch__int_dt = 0;   // [rad/sec] flybarless error value integral
        private double DELTA_x_roll__diff = 0;    // [rad/sec] flybarless error value differential
        private double DELTA_y_yaw__diff = 0;     // [rad/sec] gyro error value differential
        private double DELTA_z_pitch__diff = 0;   // [rad/sec] flybarless error value differential
        private double DELTA_x_roll__diff_old = 0;    // [rad/sec] flybarless error value differential _old
        private double DELTA_y_yaw__diff_old = 0;     // [rad/sec] gyro error value differential _old
        private double DELTA_z_pitch__diff_old = 0;   // [rad/sec] flybarless error value differential _old
        private double dservo_col_mr_damped_dt = 0;  // [-1...1] damping of mainrotor collective movement - Collective
        private double dservo_lat_mr_damped_dt = 0;  // [-1...1] damping of mainrotor lateral movement - Roll
        private double dservo_lon_mr_damped_dt = 0;  // [-1...1] damping of mainrotor longitudial movement - Pitch
        private double dservo_col_tr_damped_dt = 0;  // [-1...1] damping of tailrotor collective movement - Yaw
        private double dservo_lat_tr_damped_dt = 0;  // [-1...1] damping of tailrotor collective movement 
        private double dservo_lon_tr_damped_dt = 0;  // [-1...1] damping of tailrotor collective movement 
        private double Theta_cyc_b_s_mr = 0; // [rad]
        private double Theta_cyc_a_s_mr = 0; // [rad]
        public double Theta_col_mr { get; protected set; } // [rad]
        public double Theta_col_tr { get; protected set; } // [rad]
        private double Theta_cyc_b_s_tr = 0; // [rad]
        private double Theta_cyc_a_s_tr = 0; // [rad]
        public double Theta_col_pr { get; protected set; } // [rad]
        private float reduce_flapping_effect_at_low_rpm;
        // landing gear or skids on the left side, on the right side and "usual" collision points in reference coordinate system
        readonly private List<Vector3> point_positionL = new List<Vector3>(); // local coordinate system
        readonly private List<Vector3> point_positionR = new List<Vector3>(); // reference/world coordinate system
        readonly private List<Collision_Point_Type> point_type = new List<Collision_Point_Type>();
        enum Collision_Point_Type
        {
            gear_or_skid_left,
            gear_or_skid_right,
            gear_or_support_steering_center,
            gear_or_support_steering_left,
            gear_or_support_steering_right,
            usual,
            mainrotor_for_groundeffect,
            tailrotor_for_groundeffect
        }

        public Helper.stru_AABB helicopters_aabb;

        public double I_mo; // [A]
        double V_mo; // [V]
        double P_mo_el; // [W]
        double P_mo_mech; // [W]
        double eta_mo; // [W]

        double V_s_out; // [Volt] Governor_output voltage

        public bool flag_freewheeling { get; protected set; } // Brushless freewheeling 

        private double dflapping_a_s_mr_LR__int_dt = 0;
        private double dflapping_b_s_mr_LR__int_dt = 0;
        private double dflapping_a_s_tr_LR__int_dt = 0;
        private double dflapping_b_s_tr_LR__int_dt = 0;
        //float flapping_torque_x_mr_LR; // [Nm] around x axis  -- flapping_b_s_L
        //float flapping_torque_z_mr_LR; // [Nm] around z axis  -- flapping_a_s_L
        //float flapping_torque_x_tr_LR; // [Nm] around x axis  -- flapping_b_s_L
        //float flapping_torque_z_tr_LR; // [Nm] around z axis  -- flapping_a_s_L
        float debug_tau_mr; // mainrotor flapping time constant

        private Quaternion q_DO = new Quaternion(0, 0, 0, 1);  // unity: x, y, z, w --> [0], [1], [2], [3]



        // ##################################################################################
        // read state variables
        // ##################################################################################
        private double x_R; // [m] right handed system, position x in reference frame
        private double y_R; // [m] right handed system, position y in reference frame
        private double z_R; // [m] right handed system, position z in reference frame
        private double q0; // [-] w quaternion orientation real
        private double q1; // [-] x quaternion orientation imag i
        private double q2; // [-] y quaternion orientation imag j
        private double q3; // [-] z quaternion orientation imag k
        private double dxdt_R; // [m/sec] right handed system, velocity x in reference frame
        private double dydt_R; // [m/sec] right handed system, velocity y in reference frame
        private double dzdt_R; // [m/sec] right handed system, velocity z in reference frame
        private double wx_LH; // [rad/sec] local rotational velocity vector x   around longitudial x-axis 
        private double wy_LH; // [rad/sec] local rotational velocity vector y   around vertical y-axis  
        private double wz_LH; // [rad/sec] local rotational velocity vector z   around lateral z-axis 

        private double flapping_a_s_mr_LR; // [rad] mainrotor pitch flapping angle a_s in rotor's local frame (longitudial direction)
        private double flapping_b_s_mr_LR; // [rad] mainrotor roll flapping angle b_s in rotor's local frame (lateral direction) 
        private double flapping_a_s_tr_LR; // [rad] tailrotor pitch flapping angle a_s in rotor's local frame (longitudial direction)
        private double flapping_b_s_tr_LR; // [rad] tailrotor roll flapping angle b_s in rotor's local frame (lateral direction) 

        private double omega_mo; // [rad/sec] brushless motor rotational speed
        private double Omega_mo; // [rad] brushless motor rotational angle
        private double DELTA_omega_mo___int; // [rad] // PI Controller's integral part

        private double DELTA_x_roll__int;   // [rad] flybarless error value integral
        private double DELTA_y_yaw__int;    // [rad] gyro error value integral
        private double DELTA_z_pitch__int;  // [rad] flybarless error value integral

        private double servo_col_mr_damped;  // [-1...1] damping of mainrotor collective movement - Collective
        private double servo_lat_mr_damped;  // [-1...1] damping of mainrotor lateral movement - Roll
        private double servo_lon_mr_damped;  // [-1...1] damping of mainrotor longitudial movement - Pitch
        private double servo_col_tr_damped;  // [-1...1] damping of tailrotor collective movement - Yaw
        private double servo_lat_tr_damped;  // [-1...1] damping of tailrotor lateral movement 
        private double servo_lon_tr_damped;  // [-1...1] damping of tailrotor longitudial movement 

        private double q0_DO; // [-] w quaternion orientation real - rotor-disc
        private double q1_DO; // [-] x quaternion orientation imag i - rotor-disc
        private double q2_DO; // [-] y quaternion orientation imag j - rotor-disc
        private double q3_DO; // [-] z quaternion orientation imag k - rotor-disc
        private double wx_DO_LD; // [rad/sec] local rotational velocity vector x   around longitudial x-axis  - rotor-disc
        private double wy_DO_LD; // [rad/sec] local rotational velocity vector y   around vertical y-axis   - rotor-disc
        private double wz_DO_LD; // [rad/sec] local rotational velocity vector z   around lateral z-axis  - rotor-disc
        // ##################################################################################


        // ##################################################################################
        // user input
        // ##################################################################################
        private float delta_col_mr; // [-1 ... 1] Collective
        private float delta_lat_mr; // [-1 ... 1] Roll  
        private float delta_lon_mr; // [-1 ... 1] Pitch 
        private float delta_col_tr; // [-1 ... 1] Yaw
        private float delta_lon_tr; // [-1 ... 1] 
        private float delta_lat_tr; // [-1 ... 1] 
        private float delta_col_pr; // [-1 ... 1] Pusher propeller 

        private float input_y_col; // [-1 ... 1] Normalized Collective pitch servo input  TODO double
        private float input_x_roll; // [-1 ... 1] Roll 
        private float input_y_yaw; // [-1 ... 1] Yaw
        private float input_z_pitch; // [-1 ... 1] Pitchs
        private float input_x_propeller; // [-1 ... 1] Pusher propeller 
        private float input_x_booster; // [-1 ... 1] Booster
        // ##################################################################################


        // ##################################################################################
        // collect the resulting forces in y-direction (upwards) for the animation of the deformation 
        // of the landing gears or skids on left and right side
        // ##################################################################################
        private float force_y_gear_or_skid_leftLH_temp; // local coordiante system - right handed
        private float force_y_gear_or_skid_rightLH_temp; // local coordiante system - right handed
        private float force_y_gear_or_support_steering_centerLH_temp; // local coordiante system - right handed
        private float force_y_gear_or_support_steering_leftLH_temp; // local coordiante system - right handed
        private float force_y_gear_or_support_steering_rightLH_temp; // local coordiante system - right handed

        public float force_y_gear_or_skid_leftLH; // local coordiante system - right handed
        public float force_y_gear_or_skid_rightLH; // local coordiante system - right handed
        public float force_y_gear_or_support_steering_centerLH; // local coordiante system - right handed
        public float force_y_gear_or_support_steering_leftLH; // local coordiante system - right handed
        public float force_y_gear_or_support_steering_rightLH; // local coordiante system - right handed
        // ##################################################################################


        // ##################################################################################
        // wheel/gear rotation distance to visualize rotation, while rolling on ground
        // ##################################################################################
        private float wheel_rolling_velocity_left_temp = 0; // wheel rotation velocity for rolling
        private float wheel_rolling_velocity_right_temp = 0; // wheel rotation velocity for rolling
        private float wheel_rolling_velocity_steering_center_temp = 0; // wheel rotation velocity for rolling
        private float wheel_rolling_velocity_steering_left_temp = 0; // wheel rotation velocity for rolling
        private float wheel_rolling_velocity_steering_right_temp = 0; // wheel rotation velocity for rolling

        private float wheel_rolling_distance_left_temp = 0; // wheel rotation distance (integral/summed value) for rolling
        private float wheel_rolling_distance_right_temp = 0; // wheel rotation distance (integral/summed value) for rolling
        private float wheel_rolling_distance_steering_center_temp = 0; // wheel rotation distance (integral/summed value) for rolling
        private float wheel_rolling_distance_steering_left_temp = 0; // wheel rotation distance (integral/summed value) for rolling
        private float wheel_rolling_distance_steering_right_temp = 0; // wheel rotation distance (integral/summed value) for rolling

        public float wheel_rolling_distance_left; // wheel rotation distance (integral/summed value) for rolling
        public float wheel_rolling_distance_right; // wheel rotation distance (integral/summed value) for rolling 
        public float wheel_rolling_distance_steering_center; // wheel rotation distance (integral/summed value) for rolling
        public float wheel_rolling_distance_steering_left; // wheel rotation distance (integral/summed value) for rolling
        public float wheel_rolling_distance_steering_right; // wheel rotation distance (integral/summed value) for rolling
        // ################################################################################## 


        // ##################################################################################
        // center,left,right wheel/gear steering direction
        // ##################################################################################
        public float wheel_steering_center_temp = 0; //
        private float wheel_steering_left_temp = 0; //
        private float wheel_steering_right_temp = 0; //

        public float wheel_steering_center; //
        public float wheel_steering_left; //
        public float wheel_steering_right; //

        public bool wheel_steering_center_lock_to_initial_direction = false; // if wheel gets raised it should rotate to inital position, thus it fits into housing
        public bool wheel_steering_left_lock_to_initial_direction = false; // if wheel gets raised it should rotate to inital position, thus it fits into housing
        public bool wheel_steering_right_lock_to_initial_direction = false; // if wheel gets raised it should rotate to inital position, thus it fits into housing

        private int wheel_steering_center_steer_to_center_if_no_contact_delay;
        private int wheel_steering_left_steer_to_center_if_no_contact_delay;
        private int wheel_steering_right_steer_to_center_if_no_contact_delay;
        // ################################################################################## 



        // ##################################################################################
        // stiction model - not implemented yet in V2.1 (only friction)         TODO
        // ##################################################################################
        private Vector3[] stiction_ancor_point_memoryR = new Vector3[30]; // TOTO: here only max 30 contact points are availabe -> better set size to needed points numbers
        private float beta_forward = 1;
        private float beta_sideward = 1;

        public enum Wheels_Status_Variants
        {
            lowered,
            raised
        }
        public Wheels_Status_Variants wheel_status;

        private float wheel_brake_target = 0; // [0, 1]
        public float wheel_brake_strength { get; protected set; } // [0...1]
        private float wheel_brake_transition_velocity = 0; // [1/sec]
        private const float wheel_brake_transition_duration = 1.0f; // [sec]
        // ##################################################################################



        // ##################################################################################
        // rotating unbalance of mainrotor during startup
        // ##################################################################################
        private float rotating_unbalance; // [N]
        private double omega_mr_old; // [rad/sec]
        // ##################################################################################



        // ##################################################################################
        // rotor's ground effect
        // ##################################################################################
        private float ground_effect_mainrotor_hub_distance_to_ground; // [m]
        private float ground_effect_tailrotor_hub_distance_to_ground; // [m]
        private float ground_effect_propeller_hub_distance_to_ground; // [m]
        private Vector3 ground_effect_mainrotor_triangle_normalR; // [-]
        private Vector3 ground_effect_tailrotor_triangle_normalR; // [-]
        private Vector3 ground_effect_propeller_triangle_normalR; // [-]
        // ##################################################################################

        // ##################################################################################
        // mainrotor-disc BEMT  
        // ##################################################################################
        private Vector3 T_stiffLR_CH = new Vector3(0, 0, 0); // [Nm]
        private Vector3 T_stiffLR_LD = new Vector3(0, 0, 0); // [Nm]
        private Vector3 T_dampLR_CH = new Vector3(0, 0, 0); // [Nm]
        private Vector3 T_dampLR_LD = new Vector3(0, 0, 0); // [Nm]
        public Matrix4x4 A_OLDnorot;
        public Vector3[,] r_LBO_O = new Vector3[4, 10]; // [m]
        public Vector3[,] dr_LBO_O_dt = new Vector3[4, 10]; // [m/s]
        public Vector3[,] dr_LBO_LB_dt = new Vector3[4, 10]; // [m/s]
        public Vector3[,] F_LB_O_thrust = new Vector3[4, 10]; // [N]
        public Vector3[,] F_LB_O_torque = new Vector3[4, 10]; // [Nm]
        public Vector3 F_thrustsumLD_O = new Vector3(); // [N]
        public Vector3 F_torquesumLD_LD = new Vector3(); // [Nm]
        public Vector3 F_thrustsumLD_LD = new Vector3(); // [N]
        public float[,] Vi_LD = new float[4, 10]; // [m/sec]
        public float[,] Vi_LD_smoothdamp = new float[4, 10]; // [m/sec]
        public float[,] Vi_LD_smoothdamp_diff = new float[4, 10]; // [m/sec]
        public float[,] Vi_LD_smoothdamp_velocity = new float[4, 10]; // [m/sec]
        public float Vi_mean; // [m/sec]
        private float[] beta = new float[10];
        // ##################################################################################



        // ##################################################################################
        // turbulence
        // ##################################################################################
        System.Random random = new System.Random();
        // ##################################################################################


        //#if UNITY_EDITOR
        //        StreamWriter writer = new StreamWriter("c:\\Temp\\unity_debug_textfile.txt", false);
        //#endif
        #endregion







        // ##################################################################################
        //            CCCCCCCCCCCCC                                                             tttt                                                                            tttt                                               
        //         CCC::::::::::::C                                                          ttt:::t                                                                         ttt:::t                                               
        //       CC:::::::::::::::C                                                          t:::::t                                                                         t:::::t                                               
        //      C:::::CCCCCCCC::::C                                                          t:::::t                                                                         t:::::t                                               
        //     C:::::C       CCCCCC   ooooooooooo   nnnn  nnnnnnnn        ssssssssss   ttttttt:::::ttttttt   rrrrr   rrrrrrrrr   uuuuuu    uuuuuu      ccccccccccccccccttttttt:::::ttttttt       ooooooooooo   rrrrr   rrrrrrrrr   
        //    C:::::C               oo:::::::::::oo n:::nn::::::::nn    ss::::::::::s  t:::::::::::::::::t   r::::rrr:::::::::r  u::::u    u::::u    cc:::::::::::::::ct:::::::::::::::::t     oo:::::::::::oo r::::rrr:::::::::r  
        //    C:::::C              o:::::::::::::::on::::::::::::::nn ss:::::::::::::s t:::::::::::::::::t   r:::::::::::::::::r u::::u    u::::u   c:::::::::::::::::ct:::::::::::::::::t    o:::::::::::::::or:::::::::::::::::r 
        //    C:::::C              o:::::ooooo:::::onn:::::::::::::::ns::::::ssss:::::stttttt:::::::tttttt   rr::::::rrrrr::::::ru::::u    u::::u  c:::::::cccccc:::::ctttttt:::::::tttttt    o:::::ooooo:::::orr::::::rrrrr::::::r
        //    C:::::C              o::::o     o::::o  n:::::nnnn:::::n s:::::s  ssssss       t:::::t          r:::::r     r:::::ru::::u    u::::u  c::::::c     ccccccc      t:::::t          o::::o     o::::o r:::::r     r:::::r
        //    C:::::C              o::::o     o::::o  n::::n    n::::n   s::::::s            t:::::t          r:::::r     rrrrrrru::::u    u::::u  c:::::c                   t:::::t          o::::o     o::::o r:::::r     rrrrrrr
        //    C:::::C              o::::o     o::::o  n::::n    n::::n      s::::::s         t:::::t          r:::::r            u::::u    u::::u  c:::::c                   t:::::t          o::::o     o::::o r:::::r            
        //     C:::::C       CCCCCCo::::o     o::::o  n::::n    n::::nssssss   s:::::s       t:::::t    ttttttr:::::r            u:::::uuuu:::::u  c::::::c     ccccccc      t:::::t    tttttto::::o     o::::o r:::::r            
        //      C:::::CCCCCCCC::::Co:::::ooooo:::::o  n::::n    n::::ns:::::ssss::::::s      t::::::tttt:::::tr:::::r            u:::::::::::::::uuc:::::::cccccc:::::c      t::::::tttt:::::to:::::ooooo:::::o r:::::r            
        //       CC:::::::::::::::Co:::::::::::::::o  n::::n    n::::ns::::::::::::::s       tt::::::::::::::tr:::::r             u:::::::::::::::u c:::::::::::::::::c      tt::::::::::::::to:::::::::::::::o r:::::r            
        //         CCC::::::::::::C oo:::::::::::oo   n::::n    n::::n s:::::::::::ss          tt:::::::::::ttr:::::r              uu::::::::uu:::u  cc:::::::::::::::c        tt:::::::::::tt oo:::::::::::oo  r:::::r            
        //            CCCCCCCCCCCCC   ooooooooooo     nnnnnn    nnnnnn  sssssssssss              ttttttttttt  rrrrrrr                uuuuuuuu  uuuu    cccccccccccccccc          ttttttttttt     ooooooooooo    rrrrrrr            
        // ##################################################################################
        #region constructor
        // ##################################################################################
        // create a class constructor
        // ##################################################################################
        public Helicopter_ODE()
        {
            // state variables
            x_states = new double[37]; // ODE state variables
            x_states_old = new double[37]; // for NaN workaround, TODO better
            Init(37); // allocates memory for Helicopter_Integrator variables

            par_temp.transmitter_and_helicopter.Update_Calculated_Parameter();
            par.transmitter_and_helicopter.Update_Calculated_Parameter();
        }
        // ##################################################################################
        #endregion





        // ##################################################################################
        //     IIIIIIIIII        CCCCCCCCCCCCC
        //     I::::::::I     CCC::::::::::::C
        //     I::::::::I   CC:::::::::::::::C
        //     II::::::II  C:::::CCCCCCCC::::C
        //       I::::I   C:::::C       CCCCCC
        //       I::::I  C:::::C              
        //       I::::I  C:::::C              
        //       I::::I  C:::::C              
        //       I::::I  C:::::C              
        //       I::::I  C:::::C              
        //       I::::I  C:::::C              
        //       I::::I   C:::::C       CCCCCC
        //     II::::::II  C:::::CCCCCCCC::::C
        //     I::::::::I   CC:::::::::::::::C
        //     I::::::::I     CCC::::::::::::C
        //     IIIIIIIIII        CCCCCCCCCCCCC initial conditions
        // ##################################################################################
        #region initial_conditions
        public void Set_Initial_Conditions()
        {
            bool last_takestep_in_frame = false;


            Initial_Position_And_Orientation(out Vector3 r_LHO_O, out Quaternion q_LHO);

            x_states[0] = r_LHO_O.x; // [m]
            x_states[1] = r_LHO_O.y; // [m]
            x_states[2] = r_LHO_O.z; // [m] 
            x_states[3] = q_LHO.w;
            x_states[4] = q_LHO.x;
            x_states[5] = q_LHO.y;
            x_states[6] = q_LHO.z; // unity: x, y, z, w --> [0], [1], [2], [3], ode: w, x, y, z --> [0], [1], [2], [3]
            x_states[7] = 0; // [m/sec] right handed system, velocity x in reference frame
            x_states[8] = 0; // [m/sec] right handed system, velocity y in reference frame
            x_states[9] = 0; // [m/sec] right handed system, velocity z in reference frame
            x_states[10] = 0; // [rad/sec] local rotational velocity vector x   around longitudial x-axis 
            x_states[11] = 0; // [rad/sec] local rotational velocity vector y   around vertical y-axis  
            x_states[12] = 0; // [rad/sec] local rotational velocity vector z   around lateral z-axis  

            // mainrotor flapping dynamics
            x_states[13] = 0; // [rad] mainrotor pitch flapping angle a_s (longitudial direction)
            x_states[14] = 0; // [rad] mainrotor roll flapping angle b_s (lateral direction)

            // tailrotor flapping dynamics
            x_states[15] = 0; // [rad] mainrotor pitch flapping angle a_s (longitudial direction)
            x_states[16] = 0; // [rad] mainrotor roll flapping angle b_s (lateral direction)

            // brushless governor
            float omega_mo_target = (par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor_rotation_direction.val ? -1.0f : 1.0f) *
                                    (par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val) *
                                    (par.transmitter_and_helicopter.helicopter.governor.target_rpm.vect3[flight_bank] / (60.0f / (2.0f * Mathf.PI))); // [rad/sec] "governor_target_rpm" is mainrotor speed 
            if (flag_motor_enabled == false)
            { 
                omega_mo_target = 0;
                engine_restart_time_stopped_time = 0;
                flag_motor_start_slow_or_fast = flag_motor_start_speed.slow;
            }

            omega_mo_target_with_soft_start = omega_mo_target;
            //x_states[17] = omega_mo_target;  // [rad/sec] brushless motor rotational speed
            //x_states[18] = 0; // [rad] mainrotorspeed controller's int part (from PI)     TODO during reset




            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////////////////////////////////////////
            // we need the brushless motors's speed governor's initial value for the I-part. Othervise if the I-part is initailized with zero, then the main 
            // rotor speed after selecting a new heli or reseting the acual one always goes down very much. To avoid this the drivetain and the governor 
            // differential equations are solved here for 5 seconds by a separate solver to reach a steady operation point with the proper I-part value.

            // for the brushless motor or drivetrain the load has to be calculated here at first:
            veloLH = Vector3.zero;
            omegaLH = Vector3.zero;
            omega_mo = omega_mo_target * 1; // [rad/sec]  reson for factor "2": At init the speed controller input speed are equal thus the controller commands 0 Volt that brakres the motor. Therefore the motor speed falls ver fast to a low rpm, that initializes mainrotor oscialltions.
            Omega_mo = 0; // [rad]
            omega_mr = omega_mo_target * (1 / par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val); // [rad/sec]
            Omega_mr = 0; // [rad]
            //omega_mo = 0; // [rad/sec]
            //Omega_mo = 0; // [rad]
            //omega_mr = 0; // [rad/sec]
            //Omega_mr = 0; // [rad]
            DELTA_omega_mo___int = 0; // [rad]
            Theta_col_mr = 0; // [rad]
            Theta_cyc_a_s_mr = 0; // [rad]
            Theta_cyc_b_s_mr = 0; // [rad]
            Theta_col_tr = 0; // [rad]
            Theta_cyc_a_s_tr = 0; // [rad]
            Theta_cyc_b_s_tr = 0; // [rad]
            Theta_col_pr = 0; // [rad]
            flapping_a_s_mr_LR = 0; // [rad]
            flapping_b_s_mr_LR = 0; // [rad]
            flapping_a_s_tr_LR = 0; // [rad]
            flapping_b_s_tr_LR = 0; // [rad]
            forcesO = Vector3.zero; // [N]
            torquesLH = Vector3.zero; // [Nm]

            DELTA_x_roll__int = 0;   // [rad] flybarless error value integral
            DELTA_y_yaw__int = 0;    // [rad] gyro error value integral
            DELTA_z_pitch__int = 0;  // [rad] flybarless error value integral

            dflapping_a_s_mr_LR__int_dt = 0;   // [rad/sec] mainrotor pitch flapping velocity a_s (longitudial direction)
            dflapping_b_s_mr_LR__int_dt = 0;   // [rad/sec] mainrotor roll flapping velocity b_s (lateral direction)

            dflapping_a_s_tr_LR__int_dt = 0;   // [rad/sec] tailrotor pitch flapping velocity a_s (longitudial direction)
            dflapping_b_s_tr_LR__int_dt = 0;   // [rad/sec] tailrotor roll flapping velocity b_s (lateral direction)

            force_fuselageLH = Vector3.zero; // [N]
            force_fuselageO = Vector3.zero; // [N]
            torque_contactO = Vector3.zero; // [Nm]
            torque_contactLH = Vector3.zero; // [Nm]
            torque_contactLH_sum = Vector3.zero; // [Nm]
            torque_frictionO = Vector3.zero; // [Nm]
            torque_frictionLH = Vector3.zero; // [Nm]
            torque_frictionLH_sum = Vector3.zero; // [Nm]
            velo_wind_LH = Vector3.zero; // [m/sec]
            velo_wind_O = Vector3.zero; // [m/sec]
            dDELTA_x_roll__int_dt = 0;    // [rad/sec] flybarless error value integral
            dDELTA_y_yaw__int_dt = 0;     // [rad/sec] gyro error value integral
            dDELTA_z_pitch__int_dt = 0;   // [rad/sec] flybarless error value integral
            DELTA_x_roll__diff = 0;    // [rad/sec] flybarless error value differential
            DELTA_y_yaw__diff = 0;     // [rad/sec] gyro error value differential
            DELTA_z_pitch__diff = 0;   // [rad/sec] flybarless error value differential
            DELTA_x_roll__diff_old = 0;    // [rad/sec] flybarless error value differential _old
            DELTA_y_yaw__diff_old = 0;     // [rad/sec] gyro error value differential _old
            DELTA_z_pitch__diff_old = 0;   // [rad/sec] flybarless error value differential _old
            dservo_col_mr_damped_dt = 0;  // [-1...1] damping of mainrotor collective movement - Collective
            dservo_lat_mr_damped_dt = 0;  // [-1...1] damping of mainrotor lateral movement - Roll
            dservo_lon_mr_damped_dt = 0;  // [-1...1] damping of mainrotor longitudial movement - Pitch
            dservo_col_tr_damped_dt = 0;  // [-1...1] damping of tailrotor collective movement - Yaw
            dservo_lat_tr_damped_dt = 0;  // [-1...1] damping of tailrotor collective movement 
            dservo_lon_tr_damped_dt = 0;  // [-1...1] damping of tailrotor collective movement 

            Helicopter_Rotor_Physics.Rotor_Reset_Variables();


            if (flag_motor_enabled == true)
            {
                Wind_Model(out velo_wind_LH, out velo_wind_O);
                Fuselage_Model(last_takestep_in_frame, out force_fuselageLH, out force_fuselageO);
                Mainrotor_Thrust_and_Torque_with_Precalculations(0, (float)0.001, 0, force_fuselageLH, out thrust_mr, out torque_mr, out v_i_mr, out dflapping_a_s_mr_LR__int_dt, out dflapping_b_s_mr_LR__int_dt);
                Tailrotor_Thrust_and_Torque_with_Precalculations(0, (float)0.001, 0, force_fuselageLH, out thrust_tr, out torque_tr, out v_i_tr, out dflapping_a_s_tr_LR__int_dt, out dflapping_b_s_tr_LR__int_dt);
                if (par.transmitter_and_helicopter.helicopter.propeller.rotor_exists.val)
                    Propeller_Thrust_and_Torque_with_Precalculations(0, (float)0.001, 0, force_fuselageLH, out thrust_pr, out torque_pr, out v_i_pr);
                else
                {
                    thrust_pr = 0; torque_pr = 0; v_i_pr = 0;
                }

//#if UNITY_EDITOR
//                UnityEngine.Debug.Log("###############################################xxxx");
//                UnityEngine.Debug.Log("n_mo2mr: " + par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val + "    n_mr2tr: " + par.transmitter_and_helicopter.helicopter.transmission.n_mr2tr.val + "    invert_V: " + par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor_rotation_direction.val +
//                    "    omega_mo_target: " + omega_mo_target + "    governor_target_rpm: " + governor_target_rpm);
//                UnityEngine.Debug.Log("torque_mr: " + torque_mr + "    torque_tr: " + torque_tr + "    torque_pr: " + torque_pr +
//                                   "   thrust_mr: " + thrust_mr + "    thrust_tr: " + thrust_tr + "    thrust_pr: " + thrust_pr);
//                UnityEngine.Debug.Log("omega_mo: " + omega_mo + "    Omega_mo: " + Omega_mo + "    omega_mr: " + omega_mr + "    Omega_mr: " + Omega_mr + "    DELTA_omega_mo___int: " + DELTA_omega_mo___int);
//                UnityEngine.Debug.Log("P_mr_pr: " + ODEDebug.P_mr_pr + "   P_mr_i: " + ODEDebug.P_mr_i + "   P_mr_pa: " + ODEDebug.P_mr_pa + "   P_mr_c: " + ODEDebug.P_mr_c);
//#endif

                // simple euler method for solving ODE of drivetrain (brushless motor, mainrotor) and speed governor (PI controller)
                const float h = 0.002f; // [sec] stepsize
                for (int t = 0; t < 5 / h; t++)
                {
                    Drivetrain_With_Governor(omega_mo_target, out double domega_mo_dt, out double dOmega_mo_dt, out double domega_mr_dt, out double dOmega_mr_dt, out double dDELTA_omega_mo___int_dt);
                    omega_mo += domega_mo_dt * h;
                    Omega_mo += dOmega_mo_dt * h;
                    omega_mr += domega_mr_dt * h;
                    Omega_mr += dOmega_mr_dt * h;
                    DELTA_omega_mo___int += dDELTA_omega_mo___int_dt * h;
                }
            }

//#if UNITY_EDITOR
//            UnityEngine.Debug.Log("###############################################");
//            UnityEngine.Debug.Log("n_mo2mr: " + par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val + "    n_mr2tr: " + par.transmitter_and_helicopter.helicopter.transmission.n_mr2tr.val + "    invert_V: " + par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor_rotation_direction.val +
//                "    omega_mo_target: " + omega_mo_target + "    governor_target_rpm: " + governor_target_rpm);
//            UnityEngine.Debug.Log("torque_mr: " + torque_mr + "    torque_tr: " + torque_tr + "    torque_pr: " + torque_pr +
//                               "   thrust_mr: " + thrust_mr + "    thrust_tr: " + thrust_tr + "    thrust_pr: " + thrust_pr);
//            UnityEngine.Debug.Log("omega_mo: " + omega_mo + "    Omega_mo: " + Omega_mo + "    omega_mr: " + omega_mr + "    Omega_mr: " + Omega_mr + "    DELTA_omega_mo___int: " + DELTA_omega_mo___int);
//            UnityEngine.Debug.Log("P_mr_pr: " + ODEDebug.P_mr_pr + "   P_mr_i: " + ODEDebug.P_mr_i + "   P_mr_pa: " + ODEDebug.P_mr_pa + "   P_mr_c: " + ODEDebug.P_mr_c);
//            //writer.Close();
//#endif
            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////////////////////////////////////////


            x_states[17] = omega_mo; // [rad/sec] brushless motor rotational speed
            x_states[18] = Omega_mo; // [rad] brushless motor rotational angle
            x_states[19] = omega_mr; // [rad/sec] mainrotor motor rotational speed
            x_states[20] = Omega_mr; // [rad] mainrotor motor rotational angle

            if (flag_motor_enabled == false) DELTA_omega_mo___int = 0; // [deg]
            x_states[21] = DELTA_omega_mo___int; // [rad] mainrotorspeed controller's int part (from PI)  

            // flybarless controller 
            x_states[22] = par.transmitter_and_helicopter.helicopter.flybarless.initial_roll_angle.val * Helper.Deg_to_Rad;// -0.065; // [rad/sec] flybarless error value integral
            x_states[23] = 0; // [rad/sec] gyro error value integral
            x_states[24] = par.transmitter_and_helicopter.helicopter.flybarless.initial_pitch_angle.val * Helper.Deg_to_Rad;// 0.075; // [rad/sec] flybarless error value integral

            // servo movement damping
            x_states[25] = 0;  // [rad] damping of mainrotor collective movement - Collective
            x_states[26] = 0;  // [rad] damping of mainrotor lateral movement - Roll
            x_states[27] = 0;  // [rad] damping of mainrotor longitudial movement - Pitch
            x_states[28] = 0;  // [rad] damping of tailrotor collective movement - Yaw
            x_states[29] = 0;  // [rad] damping of tailrotor lateral movement 
            x_states[30] = 0;  // [rad] damping of tailrotor longitudial movement


            // correct orientation according to heli's initial position and rotor-hub local orientation  (oriLH: B321)
            Quaternion q__DO =
                Quaternion.Euler(par.transmitter_and_helicopter.helicopter.mainrotor.oriLH.vect3.x, 0, 0) *
                Quaternion.Euler(0, par.transmitter_and_helicopter.helicopter.mainrotor.oriLH.vect3.y, 0) *
                Quaternion.Euler(0, 0, par.transmitter_and_helicopter.helicopter.mainrotor.oriLH.vect3.z) *
                q_LHO;

            if (Helicopter_Main.mainrotor_simplified0_or_BEMT1 == 0)
            { 
                x_states[31] = 1; // [-] w quaternion orientation real - rotor disc
                x_states[32] = 0; // [-] x quaternion orientation imag i - rotor disc
                x_states[33] = 0; // [-] y quaternion orientation imag j - rotor disc
                x_states[34] = 0; // [-] z quaternion orientation imag k - rotor disc
                x_states[35] = 0; // [rad/sec] local rotational velocity vector x   around longitudial x-axis - rotor disc
                //x_states[36] = omega_mr*0.50990000000000000000; // [rad/sec] local rotational velocity vector y   around vertical y-axis - rotor disc  
                x_states[36] = 0; // [rad/sec] local rotational velocity vector z   around lateral z-axis - rotor disc
            }
            else
            {
                x_states[31] = q__DO.w; // [-] w quaternion orientation real - rotor disc
                x_states[32] = q__DO.x; // [-] x quaternion orientation imag i - rotor disc
                x_states[33] = q__DO.y; // [-] y quaternion orientation imag j - rotor disc
                x_states[34] = q__DO.z; // [-] z quaternion orientation imag k - rotor disc
                x_states[35] = 0; // [rad/sec] local rotational velocity vector x   around longitudial x-axis - rotor disc
                //x_states[36] = omega_mr*0.50990000000000000000; // [rad/sec] local rotational velocity vector y   around vertical y-axis - rotor disc  
                x_states[36] = 0; // [rad/sec] local rotational velocity vector z   around lateral z-axis - rotor disc
            }



            // reset stiction position
            for (int i = 0; i < stiction_ancor_point_memoryR.Length; i++)
                stiction_ancor_point_memoryR[i] = Vector3.zero;
        }
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


        // ##################################################################################
        /// <summary>
        /// calcualte mass centrum point from scenery start position and from helicopter's reference to center of mass position 
        /// also calculate orientations given in scenery and in model's inital orientation (inital frame LI)
        /// </summary>
        /// <param name="r_CO_O"></param>
        /// <param name="q_OC"></param>
        // ##################################################################################
        public void Initial_Position_And_Orientation(out Vector3 r_LHO_O, out Quaternion q_LHO)
        {
            // r_MO_O = r_IO_O + A__OI(q_OI) * r_MI_I
            // A__OM = A__MI(q_CI)  * A__IO(q_IO) = A__OM(q_MI*q_IO)
            Vector3 r_IO_O = Helper.ConvertLeftHandedToRightHandedVector(par.scenery.initial_position.position.vect3); // in world coordinate system, lefthanded converted to righthanded
            Quaternion q__OI = Helper.ConvertLeftHandedToRightHandedQuaternion(Quaternion.Euler(par.scenery.initial_position.orientation.vect3));  // input: Left handed orientation, deg, S312==B213. Then converted to righthanded.  

            Vector3 r_LHI_I = par.transmitter_and_helicopter.helicopter.reference_to_masscentrum.position.vect3; // in local coordinate system, righthanded
            Quaternion q__ILH = Helper.B321toQuat(par.transmitter_and_helicopter.helicopter.reference_to_masscentrum.orientation.vect3); // right handed and deg, B321     (B321 is the same as S123)                           

            // r_MO_O = r_IO_O + A__OI(q_OI) * r_MI_I
            r_LHO_O = r_IO_O + Helper.A_LR(q__OI, r_LHI_I); // right handed and [m]

            // A__OM = A_RL( q__MI * q__IO );
            q_LHO = q__OI * q__ILH; // right handed and [rad]   q = q_2.) * q_1.)
        }
        // ##################################################################################



        // ##################################################################################
        // public methods to control simulation
        // ##################################################################################
        public void Toggle_Start_Motor()
        {
            // toggle state
            if (flag_motor_enabled == false)
                Start_Motor();
            else
                Stop_Motor(); 
        }
        public void Start_Motor()
        {
            flag_motor_enabled = true;

            if((Time.time - engine_restart_time_stopped_time) > par.transmitter_and_helicopter.helicopter.governor.engine_restart_time.val)
                flag_motor_start_slow_or_fast = flag_motor_start_speed.slow;
            else
                flag_motor_start_slow_or_fast = flag_motor_start_speed.fast;

            engine_restart_time_stopped_time = 0;
        }
        public void Stop_Motor()
        {
            flag_motor_enabled = false;
            engine_restart_time_stopped_time = Time.time;
        }
        public void Wheel_Brake_Enable()
        {
            wheel_brake_target = 1;
        }
        public void Wheel_Brake_Disable()
        {
            wheel_brake_target = 0;
        }
        // ##################################################################################



        // ##################################################################################
        // to speedup collision detection use AABB (Axis-aligned Bounding Boxes).
        // ##################################################################################
        public void Set_AABB_Skybox_Collision_Landing_Mesh(Mesh mesh)
        {
            //if (ground_mesh != null)
            //    ground_mesh.Clear();
            ground_mesh = new Common.Helper.GroundMesh
            {
                vertices = new Vector3[mesh.vertices.Length],
                normals = new Vector3[mesh.normals.Length / 3], // reduce size to number of triangles
                aabb = new Helper.stru_AABB[mesh.normals.Length / 3] // reduce size to number of triangles
            };          
            Vector3[] temp_all_normals = new Vector3[mesh.normals.Length]; // mesh.normals.Length==mesh.vertices.Length
            //ground_mesh.triangles = new int[mesh.triangles.Length];

            //for (int i = 0; i < mesh.vertices.Length; i++)
            //    Debug.Log("mesh.vertices: x=" + mesh.vertices[i].x + " y=" + mesh.vertices[i].y + " z=" + mesh.vertices[i].z + "\n");
            //for (int i = 0; i < mesh.normals.Length; i++)
            //    Debug.Log("mesh.normals: x=" + mesh.normals[i].x + " y=" + mesh.normals[i].y + " z=" + mesh.normals[i].z + "\n");
            //for (int i = 0; i < mesh.triangles.Length; i++)
            //    Debug.Log("mesh.triangles: x=" + mesh.triangles[i] + "\n");

            //System.Array.Copy(mesh.vertices, ground_mesh.vertices, mesh.vertices.Length);
            //System.Array.Copy(mesh.normals, temp_all_normals, mesh.normals.Length);
            ////System.Array.Copy(mesh.triangles, ground_mesh.triangles, mesh.triangles.Length);
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                ground_mesh.vertices[i] = mesh.vertices[i];
            }
            for (int i = 0; i < mesh.normals.Length; i++)
            {
                temp_all_normals[i] = mesh.normals[i];
            }


            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                ground_mesh.vertices[i].x = -ground_mesh.vertices[i].x;  // mirrored over x, see also collisionobject scale --> -1,1,1
                ground_mesh.vertices[i].z = -ground_mesh.vertices[i].z;  // change from left handed to right handed
            }
            for (int i = 0; i < mesh.normals.Length / 3; i++)
            {
                ground_mesh.normals[i] = temp_all_normals[i * 3]; // reduce size
                ground_mesh.normals[i].x = -ground_mesh.normals[i].x;  // mirrored over x, see also collisionobject scale --> -1,1,1
                ground_mesh.normals[i].z = -ground_mesh.normals[i].z;  // change from left handed to right handed
            }

            // find AABB box for each triangle
            int faces_count = mesh.vertices.Length / 3;
            float min_x, max_x, min_y, max_y, min_z, max_z;
            for (int i = 0; i < faces_count; i++)
            {
                min_x = Mathf.Min(Mathf.Min(ground_mesh.vertices[i * 3 + 0].x, ground_mesh.vertices[i * 3 + 1].x), ground_mesh.vertices[i * 3 + 2].x);
                max_x = Mathf.Max(Mathf.Max(ground_mesh.vertices[i * 3 + 0].x, ground_mesh.vertices[i * 3 + 1].x), ground_mesh.vertices[i * 3 + 2].x);
                min_y = Mathf.Min(Mathf.Min(ground_mesh.vertices[i * 3 + 0].y, ground_mesh.vertices[i * 3 + 1].y), ground_mesh.vertices[i * 3 + 2].y);
                max_y = Mathf.Max(Mathf.Max(ground_mesh.vertices[i * 3 + 0].y, ground_mesh.vertices[i * 3 + 1].y), ground_mesh.vertices[i * 3 + 2].y);
                min_z = Mathf.Min(Mathf.Min(ground_mesh.vertices[i * 3 + 0].z, ground_mesh.vertices[i * 3 + 1].z), ground_mesh.vertices[i * 3 + 2].z);
                max_z = Mathf.Max(Mathf.Max(ground_mesh.vertices[i * 3 + 0].z, ground_mesh.vertices[i * 3 + 1].z), ground_mesh.vertices[i * 3 + 2].z);

                const float additional_size = 0.25f; // [m]
                ground_mesh.aabb[i].r[0] = ((max_x - min_x) / 2) + additional_size; // [m] radius of halfwidth extents (rx,ry,rz)
                ground_mesh.aabb[i].r[1] = ((max_y - min_y) / 2) + additional_size; // [m] 
                ground_mesh.aabb[i].r[2] = ((max_z - min_z) / 2) + additional_size; // [m]
                ground_mesh.aabb[i].c = new Vector3(min_x + ground_mesh.aabb[i].r[0], min_y + ground_mesh.aabb[i].r[1], min_z + ground_mesh.aabb[i].r[2]); // [m] 
            }

            //Debug.Log("ground_mesh.vertices.Length: " + ground_mesh.vertices.Length);
            //Debug.Log("ground_mesh.normals.Length: " + ground_mesh.normals.Length);
            //////Debug.Log("ground_mesh.triangles.Length: " + ground_mesh.triangles.Length);
        }
        // ##################################################################################



        // ##################################################################################
        // to speedup collision detection use AABB (Axis-aligned Bounding Boxes).
        // ##################################################################################
        public void Set_AABB_Helicopter_Collision_Points()
        {
            // to speedup collision detection use AABB (Axis-aligned Bounding Boxes). One singel AABB is definde for the whole helicopter with sikds or gear collsion points:
            point_positionL.Clear(); // local coordinate system
            for (var i = 0; i < par.transmitter_and_helicopter.helicopter.collision.positions_usual.vect3.Count; i++)
                point_positionL.Add(par.transmitter_and_helicopter.helicopter.collision.positions_usual.vect3[i]); // [m]

            for (var i = 0; i < par.transmitter_and_helicopter.helicopter.collision.positions_left.vect3.Count; i++)
                point_positionL.Add(par.transmitter_and_helicopter.helicopter.collision.positions_left.vect3[i]); // [m]

            for (var i = 0; i < par.transmitter_and_helicopter.helicopter.collision.positions_right.vect3.Count; i++)
                point_positionL.Add(par.transmitter_and_helicopter.helicopter.collision.positions_right.vect3[i]); // [m]

            for (var i = 0; i < par.transmitter_and_helicopter.helicopter.collision.positions_steering_center.vect3.Count; i++)
                point_positionL.Add(par.transmitter_and_helicopter.helicopter.collision.positions_steering_center.vect3[i]); // [m]

            for (var i = 0; i < par.transmitter_and_helicopter.helicopter.collision.positions_steering_left.vect3.Count; i++)
                point_positionL.Add(par.transmitter_and_helicopter.helicopter.collision.positions_steering_left.vect3[i]); // [m]

            for (var i = 0; i < par.transmitter_and_helicopter.helicopter.collision.positions_steering_right.vect3.Count; i++)
                point_positionL.Add(par.transmitter_and_helicopter.helicopter.collision.positions_steering_right.vect3[i]); // [m]

            helicopters_aabb = Helper.Set_AABB_Helicopter(point_positionL);
        }
        // ##################################################################################



        // ##################################################################################
        // how fast is a helicopter's local point's translational velocity, expressed in inertial frame (launching missiles need the initial velocity at spawning)
        // ##################################################################################
        public Vector3 Translational_Velocity_At_Local_Point_Expressed_In_Global_Frame_RightHanded(Vector3 point_position_LH)
        {
            // all vectors in right handed system
            Vector3 translational_velocity_O = new Vector3((float)x_states[7], (float)x_states[8], (float)x_states[9]); // [m/sec] right handed system, velocity x in reference frame
            Vector3 rotational_velocity_omega_LH = new Vector3((float)x_states[10], (float)x_states[11], (float)x_states[12]); // [rad/sec] local rotational velocity vector 
            Quaternion q = new Quaternion((float)x_states[4], (float)x_states[5], (float)x_states[6], (float)x_states[3]);  // // [-] w quaternion orientation, unity: x, y, z, w --> [0], [1], [2], [3], ode: w, x, y, z --> [0], [1], [2], [3]

            return Helper.Translational_Velocity_At_Local_Point_Expressed_In_Global_Frame(translational_velocity_O, rotational_velocity_omega_LH, point_position_LH, q);
        }
        // ##################################################################################



        // ##################################################################################
        // how fast is a helicopter's local point's rotational velocity, expressed in inertial frame (launching missiles need the initial velocity at spawning) 
        // ##################################################################################
        public Vector3 Rotational_Velocity_Expressed_In_Global_Frame_RightHanded()
        {
            // all vectors in right handed system
            Vector3 rotational_velocity_omega_LH = new Vector3((float)x_states[10], (float)x_states[11], (float)x_states[12]); // [rad/sec] local rotational velocity vector 
            Quaternion q = new Quaternion((float)x_states[4], (float)x_states[5], (float)x_states[6], (float)x_states[3]);  // // [-] w quaternion orientation, unity: x, y, z, w --> [0], [1], [2], [3], ode: w, x, y, z --> [0], [1], [2], [3]

            return Helper.Rotational_Velocity_Expressed_In_Global_Frame(rotational_velocity_omega_LH, q); // [rad/sec]
        }
        // ##################################################################################



        // ##################################################################################
        // simple wind physics-model
        // ##################################################################################
        private void Wind_Model(out Vector3 velo_wind_LH, out Vector3 velo_wind_O)
        {
            velo_wind_O = Helper.Ay_RL(par.scenery.weather.wind_direction.val * Mathf.Deg2Rad, new Vector3(0, 0, par.scenery.weather.wind_speed.val)); // [m/sec] wind velocity in world coordinate frame - R
            velo_wind_O += new Vector3(0, par.scenery.weather.wind_speed_vertical.val, 0); // [m/sec]
            velo_wind_LH = Helper.A_LR(q, velo_wind_O); // [m/sec] wind velocity in local coordinate frame - L
        }
        // ##################################################################################



        // ##################################################################################
        // simple fuselage air-drag physics model
        // ##################################################################################
        private void Fuselage_Model(bool last_takestep_in_frame, out Vector3 force_fuselageLH, out Vector3 force_fuselageO)
        {
            velo_u_aLH = (veloLH.x - velo_wind_LH.x); // [m/sec] longitudinal front-back direction local velocity
            velo_v_aLH = (veloLH.y - velo_wind_LH.y); // [m/sec] vertical top-bottom direction local velocity    (y is w! in 2011_Book_UnmannedRotorcraftSystems.pdf)
            velo_w_aLH = (veloLH.z - velo_wind_LH.z); // [m/sec] lateral left-rigth direction local velocity

            // CW value == 1 ?????
            float v_i_mr_factor = par.transmitter_and_helicopter.helicopter.fuselage.downwash_factor_mainrotor.val;     
            float v_i_tr_factor = par.transmitter_and_helicopter.helicopter.fuselage.downwash_factor_tailrotor.val; // used by tandem-rotor helicopters 
            force_fuselageLH.x = (float)(-(par.scenery.weather.rho_air.val / 2) * par.transmitter_and_helicopter.helicopter.fuselage.S_fxyz.vect3.x * 
                velo_u_aLH * Mathf.Abs(velo_u_aLH)); // [N] front  
            float v_i_mr_inverted_flight_factor = Helper.Step(-(float)v_i_mr, -0.5f, 0.3333f, +0.5f, 1.0f);  // inverted flight should have reduced downwash effect on fuselage
            float v_i_tr_inverted_flight_factor = Helper.Step(-(float)v_i_tr, -0.5f, 0.3333f, +0.5f, 1.0f);  // inverted flight should have reduced downwash effect on fuselage
            float velo_temp = velo_v_aLH - (float)v_i_mr * v_i_mr_factor * v_i_mr_inverted_flight_factor - (float)v_i_tr * v_i_tr_factor * v_i_tr_inverted_flight_factor; // [m/s]
            force_fuselageLH.y = (float)(-(par.scenery.weather.rho_air.val / 2) * par.transmitter_and_helicopter.helicopter.fuselage.S_fxyz.vect3.y *
                velo_temp * Mathf.Abs(velo_temp)); // [N] top with mainrotor downwash    
            force_fuselageLH.z = (float)(-(par.scenery.weather.rho_air.val / 2) * par.transmitter_and_helicopter.helicopter.fuselage.S_fxyz.vect3.z * 
                velo_w_aLH * Mathf.Abs(velo_w_aLH)); // [N]  side

            force_fuselageO = Helper.A_RL(q, force_fuselageLH); // [N]

            forcesO += force_fuselageO; // [N]
            torquesLH += Vector3.zero; // [Nm]

            if(last_takestep_in_frame)
            { 
                ODEDebug.drag_on_fuselage_positionO = Helper.ConvertRightHandedToLeftHandedVector(new Vector3((float)x_R, (float)y_R, (float)z_R) + Helper.A_RL(q, new Vector3(0, 0, 0))); // [m]
                ODEDebug.drag_on_fuselage_drag_on_fuselage_forceO = Helper.ConvertRightHandedToLeftHandedVector(force_fuselageO); // [N] also threat right to lefthandside conv.
            }
        }
        // ##################################################################################



        // ##################################################################################
        // simple wing lift physics model 
        // ##################################################################################
        private void Wing_Model(stru_wing wing,
            out Vector3 wing_forceO,
            out Vector3 wing_torque_wrp_cgLH,
            out Vector3 debug_wing_positionO, // left handed system
            out Vector3 debug_wing_forceO) // left handed system
        {
            float velo_u_aLH; // [m/sec] longitudinal front-back direction local velocity
            float velo_v_aLH; // [m/sec] vertical top-bottom direction local velocity    (y is w! in 2011_Book_UnmannedRotorcraftSystems.pdf)
            float velo_w_aLH; // [m/sec] lateral left-rigth direction local velocity


            Vector3 velo_at_wingL = veloLH + Helper.Cross(omegaLH, wing.posLH.vect3); // [m/sec] absolute velocity due to wing movement in local coordinate system
            Vector3 velo_at_wing_with_windL = velo_at_wingL - velo_wind_LH; // [m/sec] relative velocity taking wind in account in local coordinate system
            // [m/sec] relative velocity taking rotor downwash in account    
            // negative induced velocity direction should not create force
            velo_u_aLH = velo_at_wing_with_windL.x + (v_i_pr < 0 ? (float)-v_i_pr * wing.downwash_factor_propeller.val : 0.0f); // [m/sec] longitudial front-back direction local velocity
            velo_v_aLH = velo_at_wing_with_windL.y + (v_i_mr < 0 ? (float)-v_i_mr * wing.downwash_factor_mainrotor.val : 0.0f); // [m/sec] vertical top-bottom direction local velocity         TODO inverted flight should not have downwash
            velo_w_aLH = velo_at_wing_with_windL.z + (v_i_tr < 0 ? (float)-v_i_tr * wing.downwash_factor_tailrotor.val : 0.0f); // [m/sec] lateral left-rigth direction local velocity


            //Vector3 velo_vertical_finR = Helper.A_RL(q, velo_vertical_finL); // [m/sec]
            //Vector3 temp_velo_vertical_fin = Helper.A_RL(q, par.transmitter_and_helicopter.helicopter.tailrotor.dirL.vect3); // [m]  
            //double alpha_st = Vector3.Angle(velo_at_wingL, new Vector3(0,1,0)); // [rad] angle of attack AOA at fin surface      arccos ( (a dot product b) / ( |a| * |b| ) )


            float angle_of_attack = Mathf.PI / 2, denominator;
            float FxL = 0, FyL = 0, FzL = 0;
            if (wing.horizontal0_or_vertical1.val == false)
            {
                denominator = velo_u_aLH * velo_u_aLH + velo_w_aLH * velo_w_aLH; // [m/sec]
                if (denominator > 0.00001)
                    angle_of_attack = Mathf.Atan(Mathf.Abs(velo_v_aLH / Mathf.Sqrt(denominator))); // [rad] angle of attack (AOA) at fin surface

                if (angle_of_attack <= (wing.alpha_stall.val * Mathf.Deg2Rad)) // [rad] par.transmitter_and_helicopter.helicopter.horizontal_fin.alpha_stall.val
                {
                    FyL = -(par.scenery.weather.rho_air.val / 2) * wing.C_l_alpha.val * angle_of_attack * wing.area.val * velo_v_aLH * Math.Abs(Mathf.Sqrt(velo_u_aLH * velo_u_aLH + velo_w_aLH * velo_w_aLH)); //  front  
                    FxL = 0; // TODO drag
                }
                else
                {
                    // surface stalled: In stall, we assume that the sideforce is caused only by the dynamic pressure perpendicular to the vertical fin.
                    FyL = -(par.scenery.weather.rho_air.val / 2) * wing.drag_coeff.val * wing.area.val * velo_v_aLH * Math.Abs(velo_v_aLH); //  front 
                    FxL = 0; // TODO drag
                }
            }
            else
            {
                denominator = velo_u_aLH * velo_u_aLH + velo_v_aLH * velo_v_aLH; // [m/sec]
                if (denominator > 0.00001)
                    angle_of_attack = Mathf.Atan(Mathf.Abs(velo_w_aLH / Mathf.Sqrt(denominator))); // [rad] angle of attack (AOA) at fin surface

                if (angle_of_attack <= (wing.alpha_stall.val * Mathf.Deg2Rad)) // [rad]  par.transmitter_and_helicopter.helicopter.vertical_fin.alpha_stall.val
                {
                    FzL = -(par.scenery.weather.rho_air.val / 2) * wing.C_l_alpha.val * angle_of_attack * par.transmitter_and_helicopter.helicopter.vertical_fin.area.val * velo_w_aLH * Math.Abs(Mathf.Sqrt(velo_u_aLH * velo_u_aLH + velo_v_aLH * velo_v_aLH)); //  front  
                    FxL = 0; // TODO drag
                }
                else
                {
                    // surface stalled: In stall, we assume that the sideforce is caused only by the dynamic pressure perpendicular to the vertical fin.
                    FzL = -(par.scenery.weather.rho_air.val / 2) * wing.drag_coeff.val * wing.area.val * velo_w_aLH * Math.Abs(velo_w_aLH); //  front 
                    FxL = 0; // TODO drag
                }
            }

            wing_forceO = Helper.A_RL(q, new Vector3(FxL, FyL, FzL)); // Local to Reference frame
            wing_torque_wrp_cgLH = Helper.Cross(wing.posLH.vect3, wing_forceO); // [Nm]


            debug_wing_positionO = Helper.ConvertRightHandedToLeftHandedVector(new Vector3((float)x_R, (float)y_R, (float)z_R) + Helper.A_RL(q, wing.posLH.vect3)); // [m]
            debug_wing_forceO = Helper.ConvertRightHandedToLeftHandedVector(wing_forceO); // [N] also threat right to lefthandside conv.
        }
        // ##################################################################################



        // ##################################################################################
        // mainrotor thrust, torque and induced velocity
        // ##################################################################################
        private void Mainrotor_Thrust_and_Torque_with_Precalculations(float time, float dtime, int integrator_function_call_number, Vector3 force_fuselageLH, out double thrust_mr, out double torque_mr, out double v_i_mr, out double dflapping_a_s_mr_LR__int_dt, out double dflapping_b_s_mr_LR__int_dt)
        {
            const bool calculate_flapping = true;
            const bool calculate_vortex_ring_state = true;
            const bool calculate_turbulence = true;
            const bool calculate_ground_effect = true;

            float mass_total = 0; // [kg]

            switch (par.transmitter_and_helicopter.helicopter.rotor_systems_configuration.val)
            {
                case 0: // 0: Single Main Rotor (with optional pusher propeller)
                    {
                        mass_total = par.transmitter_and_helicopter.helicopter.mass_total.val;
                        break;
                    }
                case 1: // 1: tandem rotor
                    {
                        mass_total = par.transmitter_and_helicopter.helicopter.mass_total.val / 2;
                        break;
                    }
            }

            Helicopter_Rotor_Physics.Rotor_Thrust_and_Torque_with_Precalculations(
                time, 
                dtime,
                integrator_function_call_number,
                Rotor_Type.mainrotor,
                flag_freewheeling,
                par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor_rotation_direction.val,
                calculate_flapping, // bool calculate_flapping,
                calculate_vortex_ring_state,
                calculate_turbulence,
                calculate_ground_effect,
                par.transmitter_and_helicopter.helicopter.mainrotor,
                par.transmitter_and_helicopter.helicopter.flapping,
                par.transmitter_and_helicopter.tuning,
                par.transmitter_and_helicopter.tuning.rotor_tuning.mainrotor_tuning,
                par.scenery.weather.rho_air.val,
                mass_total, // [kg]
                random,
                vectO, // [m]  state: helicopter's position, expressed in inertial frame
                q, // [-] state: helicopter's orientation 
                veloLH, // [m/sec]  state: helicopter's translational velocity vector, expressed in helicopter's local coordinate system
                omegaLH, // [rad/sec]  state: helicopter's rotational velocity vector, expressed in helicopter's local coordinate system
                flapping_a_s_mr_LR, // [rad]  state: flapping around rotor's local z-axis
                flapping_b_s_mr_LR, // [rad]  state: flapping around rotor's local x-axis
                omega_mr, // [rad/sec] state: rotor's shaft speed
                Theta_col_mr, // [rad] 
                Theta_cyc_a_s_mr, // [rad] 
                Theta_cyc_b_s_mr, // [rad]
                velo_wind_LH, // [m/sec] 
                force_fuselageLH, // [N]
                ground_effect_mainrotor_hub_distance_to_ground,
                ground_effect_mainrotor_triangle_normalR,
                out thrust_mr, // [N]
                out torque_mr, // [Nm]
                out v_i_mr,  // [m/sec]
                out Vector3 force_at_heli_CH_O, // [N]
                out Vector3 torque_at_heli_CH_LH, // [Nm]
                out dflapping_a_s_mr_LR__int_dt, // state derivative
                out dflapping_b_s_mr_LR__int_dt, // state derivative
                out debug_tau_mr, // flapping time constant
                out ODEDebug.mainrotor_forceLH, // left handed system
                out ODEDebug.mainrotor_torqueLH, // left handed system
                out ODEDebug.mainrotor_positionO, // left handed system
                out ODEDebug.mainrotor_forceO, // left handed system
                out ODEDebug.mainrotor_torqueO, // left handed system
                out Vector3 _, // left handed system
                out ODEDebug.mainrotor_flapping_stiffness_torqueO, // left handed system
                out ODEDebug.P_mr_pr,
                out ODEDebug.P_mr_i,
                out ODEDebug.P_mr_pa,
                out ODEDebug.P_mr_c,
                out ODEDebug.turbulence_mr,
                out ODEDebug.vortex_ring_state_mr,
                out ODEDebug.ground_effect_mr,
                out ODEDebug.flap_up_mr
                );

            // for low main rotor rotational speeds reduce the effect of flapping
            if (calculate_flapping)
            {
                dflapping_a_s_mr_LR__int_dt *= reduce_flapping_effect_at_low_rpm;  // TODO better?
                dflapping_b_s_mr_LR__int_dt *= reduce_flapping_effect_at_low_rpm;  // TODO better?
            }

            if (Math.Abs(omega_mr) > 0.1)
            {
                forcesO += force_at_heli_CH_O; // [N]
                torquesLH += torque_at_heli_CH_LH; // [Nm]
            }
            else
            {
                thrust_mr = 0;
                torque_mr = 0;
                v_i_mr = 0;
                dflapping_a_s_mr_LR__int_dt = 0;
                dflapping_b_s_mr_LR__int_dt = 0;
                flapping_a_s_mr_LR = 0;
                flapping_b_s_mr_LR = 0;
            }

            // debug
            ODEDebug.mainrotor_v_i = (float)v_i_mr; // [m/s]

            // audio
            sound_volume_mainrotor = 1; // (float)thrust_mr; // 

            // deformation
            thrust_mr_for_rotordisc_conical_deformation = (float)thrust_mr; // 
        }
        // ##################################################################################



        // ##################################################################################
        // tailrotor thrust, torque and induced velcoity
        // ##################################################################################
        private void Tailrotor_Thrust_and_Torque_with_Precalculations(float time, float dtime, int integrator_function_call_number, Vector3 force_fuselageLH, out double thrust_tr, out double torque_tr, out double v_i_tr, out double dflapping_a_s_tr_LR__int_dt, out double dflapping_b_s_tr_LR__int_dt)
        {
            bool calculate_flapping = false;
            bool calculate_vortex_ring_state = false;
            bool calculate_turbulence = false;
            bool calculate_ground_effect = false;
            float mass_total = 0; // [kg]

            switch (par.transmitter_and_helicopter.helicopter.rotor_systems_configuration.val)
            {
                case 0: // 0: Single Main Rotor (with optional pusher propeller)
                    {
                        calculate_flapping = false;
                        calculate_vortex_ring_state = false;
                        calculate_turbulence = false;
                        calculate_ground_effect = false;
                        mass_total = 0;
                        break;
                    }
                case 1: // 1: tandem rotor
                    {
                        calculate_flapping = true;
                        calculate_vortex_ring_state = true;
                        calculate_turbulence = true;
                        calculate_ground_effect = true;
                        mass_total = par.transmitter_and_helicopter.helicopter.mass_total.val/2;
                        break;
                    }
            }

            omega_tr = omega_mr / par.transmitter_and_helicopter.helicopter.transmission.n_mr2tr.val; // [rad/sec]

            Helicopter_Rotor_Physics.Rotor_Thrust_and_Torque_with_Precalculations(
                time,
                dtime,
                integrator_function_call_number,
                Rotor_Type.tailrotor,
                flag_freewheeling,
                false, // par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor2tailrotor_transmission.val,
                calculate_flapping,
                calculate_vortex_ring_state,
                calculate_turbulence,
                calculate_ground_effect,
                par.transmitter_and_helicopter.helicopter.tailrotor,
                par.transmitter_and_helicopter.helicopter.flapping, // just as dummy, not used for tailrotor
                par.transmitter_and_helicopter.tuning,
                par.transmitter_and_helicopter.tuning.rotor_tuning.tailrotor_tuning,
                par.scenery.weather.rho_air.val,
                mass_total, // [kg]
                random,
                vectO, // [m]  state: helicopter's position, expressed in inertial frame
                q, // [-] state: helicopter's orientation 
                veloLH, // [m/sec]  state: helicopter's translational velocity vector, expressed in helicopter's local coordinate system
                omegaLH, // [rad/sec]  state: helicopter's rotational velocity vector, expressed in helicopter's local coordinate system
                flapping_a_s_tr_LR * (calculate_flapping ? 1f : 0f), // [rad]  state: flapping around rotor's local z-axis   flapping_a_s_LR
                flapping_b_s_tr_LR * (calculate_flapping ? 1f : 0f), // [rad]  state: flapping around rotor's local x-axis   flapping_b_s_LR
                omega_tr, // [rad/sec] state: rotor's shaft speed
                Theta_col_tr, // [rad] 
                Theta_cyc_a_s_tr, // [rad] 
                Theta_cyc_b_s_tr, // [rad]
                velo_wind_LH, // [m/sec] 
                Vector3.zero, // [N] force_fuselageLH
                ground_effect_tailrotor_hub_distance_to_ground,
                ground_effect_tailrotor_triangle_normalR,
                out thrust_tr, // [N]
                out torque_tr, // [Nm]
                out v_i_tr, // [m/sec]
                out Vector3 force_at_heli_CH_O, // [N]
                out Vector3 torque_at_heli_CH_LH, // [Nm]
                out dflapping_a_s_tr_LR__int_dt, // state derivative
                out dflapping_b_s_tr_LR__int_dt, // state derivative
                out float _, // flapping time constant
                out ODEDebug.tailrotor_forceLH, // left handed system
                out ODEDebug.tailrotor_torqueLH, // left handed system
                out ODEDebug.tailrotor_positionO, // left handed system
                out ODEDebug.tailrotor_forceO, // left handed system
                out ODEDebug.tailrotor_torqueO, // left handed system
                out _, // left handed system
                out ODEDebug.tailrotor_flapping_stiffness_torqueO, // left handed system
                out ODEDebug.P_tr_pr,
                out ODEDebug.P_tr_i,
                out ODEDebug.P_tr_pa,
                out ODEDebug.P_tr_c,
                out ODEDebug.turbulence_tr,
                out ODEDebug.vortex_ring_state_tr,
                out ODEDebug.ground_effect_tr,
                out ODEDebug.flap_up_tr
                );

            // for low main rotor rotational speeds reduce the effect of flapping
            if (calculate_flapping)
            {
                dflapping_a_s_tr_LR__int_dt *= reduce_flapping_effect_at_low_rpm;  // TODO
                dflapping_b_s_tr_LR__int_dt *= reduce_flapping_effect_at_low_rpm;  // TODO
            }

            if (Math.Abs(omega_tr) > 0.1)
            {
                forcesO += force_at_heli_CH_O; // [N]
                torquesLH += torque_at_heli_CH_LH; // [Nm]
            }
            else
            {
                thrust_tr = 0;
                torque_tr = 0;
                v_i_tr = 0;
                dflapping_a_s_tr_LR__int_dt = 0;
                dflapping_b_s_tr_LR__int_dt = 0;
                flapping_a_s_tr_LR = 0;
                flapping_b_s_tr_LR = 0;
            }
//torque_tr = 0; // TODO
            // debug
            ODEDebug.tailrotor_v_i = (float)v_i_tr; // [m/s]
            ODEDebug.Theta_col_tr = (float)Theta_col_tr; // [rad]

            // audio
            sound_volume_tailrotor = Helper.Step(Mathf.Abs((float)Theta_col_tr), 10.000000000000f * Mathf.Deg2Rad, 0.5000000f, 25.000000000f * Mathf.Deg2Rad, 1.0000000000000f ) ; // 

            // 
            v_i_tr *= (par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor2tailrotor_transmission.val ? -1.0f : 1.0f); // TODO correct here??????
        }
        // ##################################################################################



        // ##################################################################################
        // propller thrust, torque and induced velcoity
        // ##################################################################################
        private void Propeller_Thrust_and_Torque_with_Precalculations(float time, float dtime, int integrator_function_call_number, Vector3 force_fuselageLH,  out double thrust_pr, out double torque_pr, out double v_i_pr)
        {
            omega_pr = omega_mr / par.transmitter_and_helicopter.helicopter.transmission.n_mr2pr.val; // [rad/sec]

            const bool calculate_flapping = false;
            const bool calculate_vortex_ring_state = false;
            const bool calculate_turbulence = false;
            const bool calculate_ground_effect = false;

            Helicopter_Rotor_Physics.Rotor_Thrust_and_Torque_with_Precalculations(
                time,
                dtime,
                integrator_function_call_number,
                Rotor_Type.propeller,
                flag_freewheeling,
                false, // par.transmitter_and_helicopter.helicopter.transmission.invert_propeller_rotation_direction.val,
                calculate_flapping, // 
                calculate_vortex_ring_state, // 
                calculate_turbulence, // 
                calculate_ground_effect, // 
                par.transmitter_and_helicopter.helicopter.propeller,
                par.transmitter_and_helicopter.helicopter.flapping, // just as dummy, not used for propeller
                par.transmitter_and_helicopter.tuning,
                par.transmitter_and_helicopter.tuning.rotor_tuning.propeller_tuning,
                par.scenery.weather.rho_air.val,
                0, // mass_total=0[kg]
                random,
                vectO, // [m]  state: helicopter's position, expressed in inertial frame
                q, // [-] state: helicopter's orientation 
                veloLH, // [m/sec]  state: helicopter's translational velocity vector, expressed in helicopter's local coordinate system
                omegaLH, // [rad/sec]  state: helicopter's rotational velocity vector, expressed in helicopter's local coordinate system
                0, // [rad]  state: flapping around rotor's local z-axis   flapping_a_s_LR
                0, // [rad]  state: flapping around rotor's local x-axis   flapping_b_s_LR
                omega_pr, // [rad/sec] state: rotor's shaft speed
                Theta_col_pr, // [rad] 
                0, // [rad]  Theta_cyc_a_s_pr
                0, // [rad]  Theta_cyc_b_s_pr
                velo_wind_LH, // [m/sec] 
                Vector3.zero, // [N] force_fuselageLH
                ground_effect_propeller_hub_distance_to_ground,
                ground_effect_propeller_triangle_normalR,
                out thrust_pr, // [N]
                out torque_pr, // [Nm]
                out v_i_pr, // [m/sec]
                out Vector3 force_at_heli_CH_O, // [N]
                out Vector3 torque_at_heli_CH_LH, // [Nm]
                out _,
                out _,
                out _, // flapping time constant
                out ODEDebug.propeller_forceLH, // left handed system
                out ODEDebug.propeller_torqueLH, // left handed system
                out ODEDebug.propeller_positionO, // left handed system
                out ODEDebug.propeller_forceO, // left handed system
                out ODEDebug.propeller_torqueO, // left handed system
                out _, // left handed system
                out _, // left handed system
                out ODEDebug.P_pr_pr,
                out ODEDebug.P_pr_i,
                out ODEDebug.P_pr_pa,
                out ODEDebug.P_pr_c,
                out ODEDebug.turbulence_pr,
                out ODEDebug.vortex_ring_state_pr,
                out ODEDebug.ground_effect_pr,
                out ODEDebug.flap_up_pr
                );

            if (Math.Abs(omega_pr) > 0.1)
            {
                forcesO += force_at_heli_CH_O; // [N]
                torquesLH += torque_at_heli_CH_LH; // [Nm]
            }
            else
            {
                thrust_pr = 0;
                torque_pr = 0;
                v_i_pr = 0;
            }

            // debug
            ODEDebug.propeller_v_i = (float)v_i_pr; // [m/s]
            ODEDebug.Theta_col_pr = (float)Theta_col_pr; // [rad]

            //v_i_pr *= (par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor2propeller_transmission.val ? -1.0f : 1.0f);
        }
        // ##################################################################################




        // ##################################################################################
        // booster thrust
        // ##################################################################################
        //private void Booster_Thrust(float time, float dtime, out double thrust_bo, out double torque_bo)
        private void Booster_Thrust()
        {
            int n_booster = 1;

            thrust_bo = par.transmitter_and_helicopter.helicopter.booster.thrust.val * input_x_booster; // [N]
            //torque_bo = 0; // [Nm]

            if(par.transmitter_and_helicopter.helicopter.booster.booster_symmetric.val == true)
                n_booster = 2;

            for (int i = 0; i < n_booster; i++)
            {
                Vector3 scale; // for mirroring booster
                if(i==0)
                    scale = new Vector3(1, 1, +1); 
                else
                    scale = new Vector3(1, 1, -1); // symmetric (mirror) along z-axis

                Vector3 force_at_heli_CH_LH = Vector3.Scale(par.transmitter_and_helicopter.helicopter.booster.dirLH.vect3, scale) * (float)thrust_bo; // [N] 
                Vector3 torque_at_heli_CH_LH = Helper.Cross(Vector3.Scale(par.transmitter_and_helicopter.helicopter.booster.posLH.vect3, scale), force_at_heli_CH_LH); // [Nm] 
                Vector3 force_at_heli_CH_O = Helper.A_RL(q, force_at_heli_CH_LH); // [N] 

                forcesO += force_at_heli_CH_O; // [N]
                torquesLH += torque_at_heli_CH_LH; // [Nm]

                // debug
                ODEDebug.booster_positionO[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(vectO + Helper.A_RL(q, Vector3.Scale(par.transmitter_and_helicopter.helicopter.booster.posLH.vect3, scale)) ); // [m]
                ODEDebug.booster_forceO[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(force_at_heli_CH_O); // [N]
            }
        }
        // ##################################################################################





        // ##################################################################################
        // drivetrain with governor
        // ##################################################################################
        private void Drivetrain_With_Governor(float omega_mo_target, out double domega_mo_dt, out double dOmega_mo_dt, out double domega_mr_dt, out double dOmega_mr_dt, out double dDELTA_omega_mo___int_dt)
        {

            // ##################################################################################
            // PI controller (Governor)
            // ##################################################################################
            double delta_omega_mo = omega_mo_target - omega_mo; // [rad/sec] error "e" 
            double V_s = par.transmitter_and_helicopter.helicopter.governor.Kp.val * delta_omega_mo + par.transmitter_and_helicopter.helicopter.governor.Ki.val * DELTA_omega_mo___int; // [Volt]
            V_s_out = V_s; // [Volt]
            //V_s *= (par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor_rotation_direction.val ? -1.0f : 1.0f); // [V]

            // output voltage saturation
            flag_motor_controller_saturation = false;
            if (!par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor_rotation_direction.val)
            {
                if (V_s > par.transmitter_and_helicopter.helicopter.governor.saturation_max.val)
                {
                    V_s_out = par.transmitter_and_helicopter.helicopter.governor.saturation_max.val; flag_motor_controller_saturation = true;
                }
                else if (V_s <= par.transmitter_and_helicopter.helicopter.governor.saturation_min.val)
                {
                    V_s_out = par.transmitter_and_helicopter.helicopter.governor.saturation_min.val; flag_motor_controller_saturation = true;
                }
            }
            else
            {
                if (-V_s > par.transmitter_and_helicopter.helicopter.governor.saturation_max.val)
                {
                    V_s_out = -par.transmitter_and_helicopter.helicopter.governor.saturation_max.val; flag_motor_controller_saturation = true;
                }
                else if (-V_s <= par.transmitter_and_helicopter.helicopter.governor.saturation_min.val)
                {
                    V_s_out = -par.transmitter_and_helicopter.helicopter.governor.saturation_min.val; flag_motor_controller_saturation = true;
                }
            }

            //V_s_out *= (par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor_rotation_direction.val ? -1.0f : 1.0f); // [V]
            // controller's integrator diff. equation
            dDELTA_omega_mo___int_dt = delta_omega_mo;

            // controller's integral anti-windup
            if (flag_motor_controller_saturation) dDELTA_omega_mo___int_dt = 0;
            // ##################################################################################





            // ##################################################################################
            // drivetrain
            // ##################################################################################
            // torques of mainrotor, tailrotor and propeller w.r.t. mainrotor shaft
const double eta = 0.80f;
            double T_mainrotor_mr = torque_mr; // [Nm]
            double T_tailrotor_mr = torque_tr * (1 / par.transmitter_and_helicopter.helicopter.transmission.n_mr2tr.val) * (1 / eta); // [Nm]
            double T_propeller_mr = torque_pr * (1 / par.transmitter_and_helicopter.helicopter.transmission.n_mr2pr.val) * (1 / eta); // [Nm]
            float friction_torque_low_rpm = 0.1f * par.transmitter_and_helicopter.helicopter.mass_total.val; // only for very low velocities, see Helper.Step(...)   TOTO parameter in list 
            double T_friction_mr = -Math.Sign(omega_mr) * friction_torque_low_rpm * Helper.Step(Mathf.Abs((float)omega_mr), 0.0f, 0, 0.01f * Helper.Rpm_to_RadPerSec, 1, 300.0f * Helper.Rpm_to_RadPerSec, 400.0f * Helper.Rpm_to_RadPerSec, 0.0f); // TODO put value into parameter-list
            double T_load_mr = T_mainrotor_mr + T_tailrotor_mr + T_propeller_mr + T_friction_mr; // [Nm] positiv, if rotor under load

            // freewheel torsional stiffness and damping
            double Omega_mg = Omega_mo / par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val; // [rad]
            double omega_mg = omega_mo / par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val; // [rad/sec]
            double T_freewheel_mr = par.transmitter_and_helicopter.helicopter.transmission.k_freewheel.val * (Omega_mg - Omega_mr) +
                                    par.transmitter_and_helicopter.helicopter.transmission.d_freewheel.val * (omega_mg - omega_mr); // [Nm]

            if ((Omega_mg - Omega_mr) * (par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor_rotation_direction.val ? -1.0f : 1.0f) < 0)
            //if ((omega_mg - omega_mr) * (par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor_rotation_direction.val ? -1.0f : 1.0f) < 0)
            {
                flag_freewheeling = true;
                T_freewheel_mr = 0;
                // reset the states, because when the freewheel recouples the angles must be the same
                // omega_mo = x_states[17]; // [rad/sec] brushless motor rotational speed
                // Omega_mo = x_states[18]; // [rad] brushless motor rotational angle
                // omega_mr = x_states[19]; // [rad/sec] mainrotor rotational speed
                // Omega_mr = x_states[20]; // [rad] mainrotor rotational angle
                x_states[18] = x_states[20] * par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val;
                //x_states[17] = x_states[19] * par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val;
            }
            else flag_freewheeling = false;

            double T_freewheel_mg = -T_freewheel_mr; // [Nm] actio = reactio
            double T_freewheel_mo = T_freewheel_mg * (1 / par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val) * (1 / eta); // [Nm]


            double R_all = par.transmitter_and_helicopter.helicopter.brushless.R_a.val + par.transmitter_and_helicopter.helicopter.accumulator.Ri.val; // [ohm]

            if(flag_motor_enabled)
            { 
                // differential equtions for drivetrain system 1
                // motor's diff. equation http://ocw.nctu.edu.tw/course/dssi032/DSSI_2.pdf (2-14)
                domega_mo_dt = (T_freewheel_mo / par.transmitter_and_helicopter.helicopter.transmission.J_momg_mo.val)
                                + par.transmitter_and_helicopter.helicopter.brushless.k.val / (par.transmitter_and_helicopter.helicopter.transmission.J_momg_mo.val * R_all) * V_s_out 
                                - (Helper.Step((float)Math.Abs(omega_mo), -1f, -1f, 1f, 1f) * par.transmitter_and_helicopter.helicopter.brushless.B_M.val / par.transmitter_and_helicopter.helicopter.transmission.J_momg_mo.val
                                + (par.transmitter_and_helicopter.helicopter.brushless.k.val * par.transmitter_and_helicopter.helicopter.brushless.k.val) / (par.transmitter_and_helicopter.helicopter.transmission.J_momg_mo.val * R_all)) * omega_mo
                                - 0.001 * omega_mo; // TODO last term is only test (for additional friction ) [(rad/sec) / sec] == [rad]

                // for postprocessing only  
                I_mo = (1.0 / R_all) * V_s_out - (par.transmitter_and_helicopter.helicopter.brushless.k.val / R_all) * omega_mo; // [A]
                V_mo = V_s_out - par.transmitter_and_helicopter.helicopter.accumulator.Ri.val * I_mo; // V_s_out is here for simplicity the inner voltage of the lipo
                P_mo_el = I_mo * V_mo; // [W] input el. power into motor
                P_mo_mech = omega_mo * T_freewheel_mo;// + (par.transmitter_and_helicopter.helicopter.transmission.j.val * domega_mo_dt) * omega_mo; // [W] output mech. power from motor
                eta_mo = 0; if (P_mo_el > 0.000001) eta_mo = P_mo_mech / P_mo_el; // [0..1] efficiency
            }
            else
            {
                domega_mo_dt = (T_freewheel_mo / (par.transmitter_and_helicopter.helicopter.transmission.J_momg_mo.val)) 
                                 - Math.Sign(omega_mo) * (Helper.Step((float)Math.Abs(omega_mo), -1f, -1f, 1f, 1f) * par.transmitter_and_helicopter.helicopter.brushless.B_M.val / par.transmitter_and_helicopter.helicopter.transmission.J_momg_mo.val) 
                                 - 0.9000000000f * omega_mo 
                                 - Math.Sign(omega_mo) * Helper.Step((float)Math.Abs(omega_mo), 0, 0f, 0.5f, 10f,20f,40f,0);

                I_mo = 0;
                V_mo = 0;
                P_mo_el = 0;
                P_mo_mech = 0;
                eta_mo = 0;
            }
            dOmega_mo_dt = omega_mo; // integral for get rotational angle





            // differential equation for drivetrain system 2
            domega_mr_dt = (1 / (par.transmitter_and_helicopter.helicopter.transmission.J_mrtrpr_mr.val)) * (T_freewheel_mr + T_load_mr); // [(rad / sec) / sec] == [rad]
            dOmega_mr_dt = omega_mr; // integral for get rotational angle
            // ##################################################################################
           

            // ##################################################################################
            // test inertial counter torque http://liu.diva-portal.org/smash/get/diva2:821251/FULLTEXT01.pdf 4.4.4
            // ##################################################################################
            //    
            if(!flag_freewheeling && (Math.Abs(omega_pr) > 0.1))
            {
                double domega_tr_dt = domega_mr_dt / par.transmitter_and_helicopter.helicopter.transmission.n_mr2tr.val;
                double domega_pr_dt = domega_mr_dt / par.transmitter_and_helicopter.helicopter.transmission.n_mr2pr.val;

                torquesLH -= par.transmitter_and_helicopter.helicopter.mainrotor.dirLH.vect3 * (float)domega_mr_dt * par.transmitter_and_helicopter.helicopter.mainrotor.J.val;
                torquesLH -= par.transmitter_and_helicopter.helicopter.tailrotor.dirLH.vect3 * (float)domega_tr_dt * par.transmitter_and_helicopter.helicopter.tailrotor.J.val;
                if(par.transmitter_and_helicopter.helicopter.propeller.rotor_exists.val)
                    torquesLH -= par.transmitter_and_helicopter.helicopter.propeller.dirLH.vect3 * (float)domega_pr_dt * par.transmitter_and_helicopter.helicopter.propeller.J.val;
                torquesLH -= new Vector3(0, 1, 0) * (float)domega_mo_dt * par.transmitter_and_helicopter.helicopter.transmission.J_momg_mo.val;
            }
            // ##################################################################################


            //#if UNITY_EDITOR
            //           // if (integrator_function_call_number == 0)
            //            //{
            //                writer.WriteLine(omega_mo + ", " + Omega_mo + ", " + omega_mr + ", " + Omega_mr + ", " + omega_mg + ", " + Omega_mg + ", " +
            //                                T_freewheel_mr + ", " + T_load_mr + ", " + V_s + ", " + V_s_out + ", " +
            //                                delta_omega_mo + ", " + DELTA_omega_mo___int + ", " +
            //                                (T_freewheel_mo / par.transmitter_and_helicopter.helicopter.transmission.J_momg_mo.val) + ", " +
            //                                par.transmitter_and_helicopter.helicopter.brushless.k.val / (par.transmitter_and_helicopter.helicopter.transmission.J_momg_mo.val * R_all) * V_s_out + ", " +
            //                                (Helper.Step((float)Math.Abs(omega_mo), -1f, -1f, 1f, 1f) * par.transmitter_and_helicopter.helicopter.brushless.B_M.val / par.transmitter_and_helicopter.helicopter.transmission.J_momg_mo.val) * omega_mo + ", " +
            //                                (par.transmitter_and_helicopter.helicopter.brushless.k.val * par.transmitter_and_helicopter.helicopter.brushless.k.val) / (par.transmitter_and_helicopter.helicopter.transmission.J_momg_mo.val * R_all) * omega_mo + ", " +
            //                                par.transmitter_and_helicopter.helicopter.transmission.k_freewheel.val * (Omega_mg - Omega_mr) + ", " +
            //                                par.transmitter_and_helicopter.helicopter.transmission.d_freewheel.val * (omega_mg - omega_mr) + ", " +
            //                                omega_mo_target + ", " +
            //                                "\n");
            //            //}
            //#endif




            //// freewheel (almost the same as above differential equation)
            ////if (domega_mo_dt <= 0 || ((omega_mo < 500f * Helper.Rpm_to_RadPerSec) && (flag_motor_enabled == false))) // TODO put value into parameter-list
            //if ((torque_mo < 0) || (flag_motor_enabled == false))
            //{
            //    float friction_factor = 5; // only for very low velocities, see Helper.Step(...)      TODO units? and put into parameter-list
            //                               //-(Math.Sign(omega_mo) * par.transmitter_and_helicopter.helicopter.brushless.B_M.val / par.transmitter_and_helicopter.helicopter.transmission.J_all.val) * omega_mo
            //    domega_mo_dt = -(torque_mo / par.transmitter_and_helicopter.helicopter.transmission.J_all.val)
            //                - Mathf.Sign((float)omega_mr) * friction_factor * Helper.Step(Mathf.Abs((float)omega_mr), 0.0f, 0, 0.1f * Helper.Rpm_to_RadPerSec, 1, 300.0f * Helper.Rpm_to_RadPerSec, 400.0f * Helper.Rpm_to_RadPerSec, 0.0f); // TODO put value into parameter-list
            //    //domega_mo_dt = -(torque_mr / par.transmitter_and_helicopter.helicopter.transmission.J_mrtrpr_mr.val)
            //    //            - Mathf.Sign((float)omega_mr) * friction_factor * Helper.Step(Mathf.Abs((float)omega_mr), 0.0f, 0, 0.1f * Helper.Rpm_to_RadPerSec, 1, 300.0f * Helper.Rpm_to_RadPerSec, 400.0f * Helper.Rpm_to_RadPerSec, 0.0f); // TODO put value into parameter-list

            //    // main rotor rotational torque at fuselage is almost zero (only friction)???????? during freewheeling
            //    torque_mrLH = new Vector3(0, Helper.Step((float)omega_mo, -1f, -1f, 1f, 1f) * par.transmitter_and_helicopter.helicopter.brushless.B_M.val / par.transmitter_and_helicopter.helicopter.transmission.J_all.val, 0);// Vector3.zero;

            //    dDELTA_omega_mo___int_dt = 0;

            //    flag_freewheeling = true;
            //}
            //else
            //{ 
            //    flag_freewheeling = false;
            //}
        }
        // ##################################################################################



        // ##################################################################################
        // landing gears collision
        // ##################################################################################
        // if the landing gears are rised / lowered the collision points have to be rised or lowered too. This function calulates the rising in two steps. 1.) rising/lowering gear following a cosinus curve. 2.) if gear is rised the bay doors are closed- then here actually nothing happens - we wait only for the animation
        //private float Landing_Gear_Collision_Point_Transition(float landing_gear_main_transition_time_gear, float landing_gear_main_transition_time_bay, float landing_gear_mechnism_tilted_forward, float positions_rised_offset, float transition)
        private float Landing_Gear_Collision_Point_Transition(float landing_gear_main_transition_time_gear, float landing_gear_main_transition_time_bay, float landing_gear_mechnism_tilted_forward, float transition)
        {
            const float positions_rised_offset = 1.0f;
            float t_total = landing_gear_main_transition_time_gear + landing_gear_main_transition_time_bay; // [sec]

            if (transition < (landing_gear_main_transition_time_gear / t_total))
            {
                // rising/lowering gear as sinus curve
                float tranistion_status = (transition / (landing_gear_main_transition_time_gear / t_total)); // [0...1]
                // map linear transition to cos-curve like movement: y = -cos(x * pi/2) - 1 
                //return -(Mathf.Cos(tranistion_status * (Mathf.PI / 2.0f)) - 1) * positions_rised_offset; // [m]
                // test: movement more than a quarter cosinus (if gear is not 90° lowered (vertical) but > 90°)
                // y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1; // two point form of a line
                // y = (((Mathf.PI / 2.0f) - (-0.2f)) / (1 - 0)) * (x - 0) + (-0.2f);
                return -((Mathf.Cos((((Mathf.PI / 2.0f) - (-landing_gear_mechnism_tilted_forward * Mathf.Deg2Rad)) / (1 - 0)) * (tranistion_status - 0) + (-landing_gear_mechnism_tilted_forward * Mathf.Deg2Rad))) - 1) * positions_rised_offset; // [m]
            }
            else
            {
                // time where bay-doors are closed / opened
                return positions_rised_offset; // [m]
            }
            //float gear_rising_offset = -((Mathf.Cos((((Mathf.PI / 2.0f) - (-0.2f)) / (1 - 0)) * (collision_positions_landing_gear_left_rising_offset - 0) + (-0.2f))) - 1) * par.transmitter_and_helicopter.helicopter.collision.positions_left_rised_offset.val; // [m]
        }
        // ##################################################################################
        #endregion










        // ##################################################################################
        //        OOOOOOOOO     DDDDDDDDDDDDD      EEEEEEEEEEEEEEEEEEEEEE
        //      OO:::::::::OO   D::::::::::::DDD   E::::::::::::::::::::E
        //    OO:::::::::::::OO D:::::::::::::::DD E::::::::::::::::::::E
        //   O:::::::OOO:::::::ODDD:::::DDDDD:::::DEE::::::EEEEEEEEE::::E
        //   O::::::O   O::::::O  D:::::D    D:::::D E:::::E       EEEEEE
        //   O:::::O     O:::::O  D:::::D     D:::::DE:::::E             
        //   O:::::O     O:::::O  D:::::D     D:::::DE::::::EEEEEEEEEE   
        //   O:::::O     O:::::O  D:::::D     D:::::DE:::::::::::::::E   
        //   O:::::O     O:::::O  D:::::D     D:::::DE:::::::::::::::E   
        //   O:::::O     O:::::O  D:::::D     D:::::DE::::::EEEEEEEEEE   
        //   O:::::O     O:::::O  D:::::D     D:::::DE:::::E             
        //   O::::::O   O::::::O  D:::::D    D:::::D E:::::E       EEEEEE
        //   O:::::::OOO:::::::ODDD:::::DDDDD:::::DEE::::::EEEEEEEE:::::E
        //    OO:::::::::::::OO D:::::::::::::::DD E::::::::::::::::::::E
        //      OO:::::::::OO   D::::::::::::DDD   E::::::::::::::::::::E
        //        OOOOOOOOO     DDDDDDDDDDDDD      EEEEEEEEEEEEEEEEEEEEEE 
        // ##################################################################################
        #region ordinary_differential_equations
        public override void ODE(
            bool last_takestep_in_frame,                // [IN] mark the last TakeStep calculation per frame, so i.e. debbugging can only made in this last TakeStep calculation
            int integrator_function_call_number,        // [IN]  
            ref double[] x_states,                      // [IN] states 
            double[] u_inputs,                          // [IN] inputs
            double[] dxdt,                              // [OUT] derivatives
            double time,                                // [IN] time 
            double dtime)                               // [IN] timestep    
        {

            // ##################################################################################
            // update debugging info only in the last Runge-Kutta solver call of ODE (once per frame)
            // ##################################################################################
            last_takestep_in_frame = (integrator_function_call_number == 3 && last_takestep_in_frame);




            // ##################################################################################
            // variables declaration 
            // ##################################################################################
            force_gravityO = 0; // [N] gravitry in reference coordinate system
            forcesO = Vector3.zero; // [N] force acting on fuselage in inertial coordinate system
            torquesLH = Vector3.zero; // [Nm] torque acting on fuselage in helicopter's local coordinate system 
            thrust_mr = 0; // [N] mainrotor thrust
            torque_mr = 0; // [Nm] mainrotor torque  
            omega_mr = 0; // [rad/sec] mainrotor rotation velocity 
            Omega_mr = 0; // [rad] mainrotor rotation angle 
            thrust_tr = 0; // [N] tailrotor thrust
            torque_tr = 0; // [Nm] tailrotor torque 
            omega_tr = 0; // [rad/sec] tailrotor rotation velocity 
            thrust_pr = 0; // [N] propeller thrust
            torque_pr = 0; // [Nm] propeller torque 
            thrust_bo = 0; // [N] booster thrust
            //torque_bo = 0; // [Nm] booster torque 
            omega_pr = 0; // [rad/sec] propeller rotation velocity 
            q = new Quaternion(0, 0, 0, 1);  // unity: x, y, z, w --> [0], [1], [2], [3]
            vectO = Vector3.zero; // [m]
            veloLH = Vector3.zero; // [m/sec]
            veloO = Vector3.zero; // [m/sec]
            omegaLH = Vector3.zero; // [rad/sec]
            //omegaO = Vector3.zero; // [rad/sec]
            torque_contactO = Vector3.zero; // [Nm]
            torque_contactLH = Vector3.zero; // [Nm]
            torque_contactLH_sum = Vector3.zero; // [Nm]
            torque_frictionO = Vector3.zero; // [Nm]
            torque_frictionLH = Vector3.zero; // [Nm]
            torque_frictionLH_sum = Vector3.zero; // [Nm]
            velo_wind_LH = Vector3.zero; // [m/sec]
            velo_wind_O = Vector3.zero; // [m/sec]
            //dDELTA_x_roll__int_dt = 0;    // [rad/sec] flybarless error value integral
            //dDELTA_y_yaw__int_dt = 0;     // [rad/sec] gyro error value integral
            //dDELTA_z_pitch__int_dt = 0;   // [rad/sec] flybarless error value integral
            DELTA_x_roll__diff = 0;    // [rad/sec] flybarless error value differential
            DELTA_y_yaw__diff = 0;     // [rad/sec] gyro error value differential
            DELTA_z_pitch__diff = 0;   // [rad/sec] flybarless error value differential
            DELTA_x_roll__diff_old = 0;    // [rad/sec] flybarless error value differential_old
            DELTA_y_yaw__diff_old = 0;     // [rad/sec] gyro error value differential_old
            DELTA_z_pitch__diff_old = 0;   // [rad/sec] flybarless error value differential_old
            dservo_col_mr_damped_dt = 0;  // [-1...1] damping of mainrotor collective movement - Collective
            dservo_lat_mr_damped_dt = 0;  // [-1...1] damping of mainrotor lateral movement - Roll
            dservo_col_tr_damped_dt = 0;  // [-1...1] damping of tailrotor collective movement - Yaw
            dservo_lon_mr_damped_dt = 0;  // [-1...1] damping of mainrotor longitudial movement - Pitch
            // ##################################################################################




            // ##################################################################################
            // read state variables
            // ##################################################################################
            x_R = x_states[0]; // [m] right handed system, position x in reference frame
            y_R = x_states[1]; // [m] right handed system, position y in reference frame
            z_R = x_states[2]; // [m] right handed system, position z in reference frame
            // because RungeKutta4 doesn't preserve quaternion-norm normalize quaternion: from mathematical point of view not a clean solution  
            Helper.QuaternionNormalize(ref x_states[3], ref x_states[4], ref x_states[5], ref x_states[6]); // double
            q0 = x_states[3]; // [-] w quaternion orientation real
            q1 = x_states[4]; // [-] x quaternion orientation imag i
            q2 = x_states[5]; // [-] y quaternion orientation imag j
            q3 = x_states[6]; // [-] z quaternion orientation imag k
            dxdt_R = x_states[7]; // [m/sec] right handed system, velocity x in reference frame
            dydt_R = x_states[8]; // [m/sec] right handed system, velocity y in reference frame
            dzdt_R = x_states[9]; // [m/sec] right handed system, velocity z in reference frame
            wx_LH = x_states[10]; // [rad/sec] local rotational velocity vector x   around longitudial x-axis 
            wy_LH = x_states[11]; // [rad/sec] local rotational velocity vector y   around vertical y-axis  
            wz_LH = x_states[12]; // [rad/sec] local rotational velocity vector z   around lateral z-axis 

            flapping_a_s_mr_LR = x_states[13]; // [rad] mainrotor pitch flapping angle a_s in local frame (longitudial direction)
            flapping_b_s_mr_LR = x_states[14]; // [rad] mainrotor roll flapping angle b_s in local frame (lateral direction) 
            flapping_a_s_tr_LR = x_states[15]; // [rad] tailrotor pitch flapping angle a_s in local frame (longitudial direction)
            flapping_b_s_tr_LR = x_states[16]; // [rad] tailrotor roll flapping angle b_s in local frame (lateral direction) 

            omega_mo = x_states[17]; // [rad/sec] brushless motor rotational speed
            Omega_mo = x_states[18]; // [rad] brushless motor rotational angle
            omega_mr = x_states[19]; // [rad/sec] mainrotor rotational speed
            Omega_mr = x_states[20]; // [rad] mainrotor rotational angle

            DELTA_omega_mo___int = x_states[21]; // [rad] // PI Controller's integral part

            DELTA_x_roll__int = x_states[22];   // [rad] flybarless error value integral
            DELTA_y_yaw__int = x_states[23];    // [rad] gyro error value integral
            DELTA_z_pitch__int = x_states[24];  // [rad] flybarless error value integral

            servo_col_mr_damped = x_states[25];  // [-1...1] damping of mainrotor collective movement - Collective
            servo_lat_mr_damped = x_states[26];  // [-1...1] damping of mainrotor lateral movement - Roll
            servo_lon_mr_damped = x_states[27];  // [-1...1] damping of mainrotor longitudial movement - Pitch
            servo_col_tr_damped = x_states[28];  // [-1...1] damping of tailrotor collective movement - Yaw
            servo_lat_tr_damped = x_states[29];  // [-1...1] damping of tailrotor lateral movement 
            servo_lon_tr_damped = x_states[30];  // [-1...1] damping of tailrotor longitudial movement

            if (Helicopter_Main.mainrotor_simplified0_or_BEMT1 == 0)
            {
                q0_DO = 1; // [-] w quaternion orientation real - rotor disc
                q1_DO = 0; // [-] x quaternion orientation imag i - rotor disc
                q2_DO = 0; // [-] y quaternion orientation imag j - rotor disc
                q3_DO = 0; // [-] z quaternion orientation imag k - rotor disc
                wx_DO_LD = 0; // [rad/sec] local rotational velocity vector x   around longitudial x-axis - rotor disc
                wy_DO_LD = 0; //     x_states[36]; // [rad/sec] local rotational velocity vector y   around vertical y-axis - rotor disc    ////// TODO reomve it. not a extra state --> use omega_mr
                wz_DO_LD = 0; // [rad/sec] local rotational velocity vector z   around lateral z-axis - rotor disc
            }
            else
            {
                // because RungeKutta4 doesn't preserve quaternion-norm normalize quaternion: from mathematical point of view not a clean solution  
                Helper.QuaternionNormalize(ref x_states[31], ref x_states[32], ref x_states[33], ref x_states[34]); // double
                q0_DO = x_states[31]; // [-] w quaternion orientation real - rotor disc
                q1_DO = x_states[32]; // [-] x quaternion orientation imag i - rotor disc
                q2_DO = x_states[33]; // [-] y quaternion orientation imag j - rotor disc
                q3_DO = x_states[34]; // [-] z quaternion orientation imag k - rotor disc
                wx_DO_LD = x_states[35]; // [rad/sec] local rotational velocity vector x   around longitudial x-axis - rotor disc
                wy_DO_LD = omega_mr; //     x_states[36]; // [rad/sec] local rotational velocity vector y   around vertical y-axis - rotor disc    ////// TODO reomve it. not a extra state --> use omega_mr
                wz_DO_LD = x_states[36]; // [rad/sec] local rotational velocity vector z   around lateral z-axis - rotor disc
            }
            // ##################################################################################





            // ##################################################################################
            // map state variables
            // ##################################################################################
            q.w = (float)q0; // unity: x, y, z, w --> [0], [1], [2], [3]
            q.x = (float)q1;
            q.y = (float)q2;
            q.z = (float)q3;

            vectO.x = (float)x_R; //[m]  TODO double
            vectO.y = (float)y_R; //[m]
            vectO.z = (float)z_R; //[m]

            veloO.x = (float)dxdt_R; //[m/sec]  TODO double
            veloO.y = (float)dydt_R; //[m/sec]
            veloO.z = (float)dzdt_R; //[m/sec]
            veloLH = Helper.A_LR(q, veloO); //[m/sec] TODO double

            omegaLH.x = (float)wx_LH; // [rad/sec] TODO double
            omegaLH.y = (float)wy_LH; // [rad/sec]
            omegaLH.z = (float)wz_LH; // [rad/sec]

            q_DO.w = (float)q0_DO; // unity: x, y, z, w --> [0], [1], [2], [3]
            q_DO.x = (float)q1_DO;
            q_DO.y = (float)q2_DO;
            q_DO.z = (float)q3_DO;
            // ##################################################################################





            // ##################################################################################
            // user input
            // ##################################################################################
            input_y_col = (float)u_inputs[0]; // [-1 ... 1] Normalized Collective pitch servo input  TODO double
            input_x_roll = (float)u_inputs[3]; // [-1 ... 1] Roll 
            input_y_yaw = (float)u_inputs[1]; // [-1 ... 1] Yaw
            input_z_pitch = (float)u_inputs[2]; // [-1 ... 1] Pitch

            input_x_propeller = (float)u_inputs[4]; // [-1 ... 1] pusher propeller collective
            input_x_booster = (float)u_inputs[5]; // [-1 ... 1] booster 
            // ##################################################################################





            // ##################################################################################
            // after resetting the simulation the user input it turned off for "x/2"-sec and is fully active after "x"-sec
            // ##################################################################################   
            float set_controll_to_zero_after_simulation_reset = 1;
            if (time < Mathf.Abs(par.simulation.gameplay.delay_after_reset.val))
            {
                set_controll_to_zero_after_simulation_reset = Helper.Step((float)time, Mathf.Abs(par.simulation.gameplay.delay_after_reset.val / 2), 0, Mathf.Abs(par.simulation.gameplay.delay_after_reset.val), 1);
                input_y_col *= set_controll_to_zero_after_simulation_reset;
                input_x_roll *= set_controll_to_zero_after_simulation_reset;
                input_y_yaw *= set_controll_to_zero_after_simulation_reset;
                input_z_pitch *= set_controll_to_zero_after_simulation_reset;

                input_x_propeller *= set_controll_to_zero_after_simulation_reset;
                input_x_booster *= set_controll_to_zero_after_simulation_reset;
            }
            // ##################################################################################




            // ##################################################################################
            // flybarless controller (with gyro)
            // ##################################################################################
            /*delta_col_mr = 0; // [-1 ... 1] Collective
            delta_lat_mr = 0; // [-1 ... 1] Roll  
            delta_lon_mr = 0; // [-1 ... 1] Pitch 
            delta_col_tr = 0; // [-1 ... 1] Yaw
            delta_lat_tr = 0; // [-1 ... 1] 
            delta_lon_tr = 0; // [-1 ... 1] 
            delta_col_pr = 0; // [-1 ... 1] Pusher propeller*/

            float delta_error; // PI controller
            float delta_error_debug = 0;
            switch (par.transmitter_and_helicopter.helicopter.rotor_systems_configuration.val)
            {
                case 0: // 0: Single Main Rotor (with optional pusher propeller)
                    {
                        // PI controller                  
                        delta_error = (par.transmitter_and_helicopter.helicopter.flybarless.K_a.vect3[flight_bank] * Mathf.Deg2Rad * input_x_roll - (float)wx_LH); // [rad/sec] error value in PI controller
                        //delta_error = (par.transmitter_and_helicopter.helicopter.flybarless.K_a.val * Mathf.Deg2Rad * input_x_roll - (float)wx_LH) / (par.transmitter_and_helicopter.helicopter.flybarless.K_a.val * Mathf.Deg2Rad); // [rad/sec] error value in PI controller
                        DELTA_x_roll__diff = (delta_error - DELTA_x_roll__diff_old) / dtime; // differential part
                        if (integrator_function_call_number == 3)
                            DELTA_x_roll__diff_old = delta_error;
                        delta_lat_mr = par.transmitter_and_helicopter.helicopter.flybarless.K_p.vect3[flight_bank] * delta_error +
                                       par.transmitter_and_helicopter.helicopter.flybarless.K_I.vect3[flight_bank] * (float)DELTA_x_roll__int +
                                       par.transmitter_and_helicopter.helicopter.flybarless.K_d.vect3[flight_bank] * 0.001f * (float)DELTA_x_roll__diff; // [rad/sec] roll
                        dDELTA_x_roll__int_dt = delta_error; // [rad/sec] error value to be integrated

                        delta_error = (par.transmitter_and_helicopter.helicopter.gyro.K_a.vect3[flight_bank] * Mathf.Deg2Rad * input_y_yaw - (float)wy_LH); // [rad/sec] error value in PI controller
                        //delta_error = (par.transmitter_and_helicopter.helicopter.gyro.K_a.val * Mathf.Deg2Rad * input_y_yaw - (float)wy_LH) / (par.transmitter_and_helicopter.helicopter.gyro.K_a.val * Mathf.Deg2Rad); // [rad/sec] error value in PI controller
                        DELTA_y_yaw__diff = (delta_error - DELTA_y_yaw__diff_old) / dtime; // differential part
                        if (integrator_function_call_number == 3)
                            DELTA_y_yaw__diff_old = delta_error;
                        delta_col_tr = par.transmitter_and_helicopter.helicopter.gyro.K_p.vect3[flight_bank] * delta_error +
                                       par.transmitter_and_helicopter.helicopter.gyro.K_I.vect3[flight_bank] * (float)DELTA_y_yaw__int +
                                       par.transmitter_and_helicopter.helicopter.gyro.K_d.vect3[flight_bank] * 0.001f * (float)DELTA_y_yaw__diff; // [rad/sec] yaw
                        dDELTA_y_yaw__int_dt = delta_error; // [rad/sec] error value to be integrated

                        delta_error = (par.transmitter_and_helicopter.helicopter.flybarless.K_a.vect3[flight_bank] * Mathf.Deg2Rad * input_z_pitch - (float)wz_LH); // [rad/sec] error value in PI controller
                        //delta_error = (par.transmitter_and_helicopter.helicopter.flybarless.K_a.val * Mathf.Deg2Rad * input_z_pitch - (float)wz_LH) / (par.transmitter_and_helicopter.helicopter.flybarless.K_a.val * Mathf.Deg2Rad); // [rad/sec] error value in PI controller
                        DELTA_z_pitch__diff = (delta_error - DELTA_z_pitch__diff_old) / dtime; // differential part
                        if (integrator_function_call_number == 3)
                            DELTA_z_pitch__diff_old = delta_error;
                        delta_lon_mr = par.transmitter_and_helicopter.helicopter.flybarless.K_p.vect3[flight_bank] * delta_error +
                                       par.transmitter_and_helicopter.helicopter.flybarless.K_I.vect3[flight_bank] * (float)DELTA_z_pitch__int +
                                       par.transmitter_and_helicopter.helicopter.flybarless.K_d.vect3[flight_bank] * 0.001f * (float)DELTA_z_pitch__diff; // [rad/sec] pitch
                        dDELTA_z_pitch__int_dt = delta_error; // [rad/sec] error value to be integrated
                        delta_error_debug = delta_error;

                        // feedtrough of controller input (controller override)
                        if (par.transmitter_and_helicopter.helicopter.flybarless.direct_feadtrough_flybarless.val == true)
                        {
                            delta_lat_mr = input_x_roll;
                            delta_lon_mr = input_z_pitch;
                        }
                        if (par.transmitter_and_helicopter.helicopter.gyro.direct_feadtrough_gyroscope.val == true)
                        {
                            delta_col_tr = input_y_yaw;
                        }

                        // limit output and anti-windup for integrator
                        if (delta_lat_mr > 1) { delta_lat_mr = 1; dDELTA_x_roll__int_dt = 0; };
                        if (delta_lat_mr < -1) { delta_lat_mr = -1; dDELTA_x_roll__int_dt = 0; };
                        if (delta_col_tr > 1) { delta_col_tr = 1; dDELTA_y_yaw__int_dt = 0; };
                        if (delta_col_tr < -1) { delta_col_tr = -1; dDELTA_y_yaw__int_dt = 0; };
                        if (delta_lon_mr > 1) { delta_lon_mr = 1; dDELTA_z_pitch__int_dt = 0; };
                        if (delta_lon_mr < -1) { delta_lon_mr = -1; dDELTA_z_pitch__int_dt = 0; };

                        delta_col_mr = input_y_col; // mainrotor collective
                        delta_col_pr = input_x_propeller; // pusher propeller collective

                        break;
                    }
                case 1: // 1: Tandem rotor
                    {
                        delta_lon_mr = 0;
                        delta_lon_tr = 0;


                        // PI controller                  
                        delta_error = par.transmitter_and_helicopter.helicopter.flybarless.K_a.vect3[flight_bank] * Mathf.Deg2Rad * input_x_roll - (float)wx_LH; // [rad/sec] error value in PI controller
                        float controller_x_roll = par.transmitter_and_helicopter.helicopter.flybarless.K_p.vect3[flight_bank] * delta_error + par.transmitter_and_helicopter.helicopter.flybarless.K_I.vect3[flight_bank] * (float)DELTA_x_roll__int; // [rad/sec] yaw
                        dDELTA_x_roll__int_dt = delta_error; // [rad/sec] error value to be integrated

                        delta_error = par.transmitter_and_helicopter.helicopter.gyro.K_a.vect3[flight_bank] * Mathf.Deg2Rad * input_y_yaw - (float)wy_LH; // [rad/sec] error value in PI controller
                        float controller_y_yaw = par.transmitter_and_helicopter.helicopter.gyro.K_p.vect3[flight_bank] * delta_error + par.transmitter_and_helicopter.helicopter.gyro.K_I.vect3[flight_bank] * (float)DELTA_y_yaw__int; // [rad/sec] yaw
                        dDELTA_y_yaw__int_dt = delta_error; // [rad/sec] error value to be integrated

                        delta_error = par.transmitter_and_helicopter.helicopter.flybarless.K_a.vect3[flight_bank] * Mathf.Deg2Rad * input_z_pitch - (float)wz_LH; // [rad/sec] error value in PI controller
                        float controller_z_pitch = par.transmitter_and_helicopter.helicopter.flybarless.K_p.vect3[flight_bank] * delta_error + par.transmitter_and_helicopter.helicopter.flybarless.K_I.vect3[flight_bank] * (float)DELTA_z_pitch__int; // [rad/sec] yaw
                        dDELTA_z_pitch__int_dt = delta_error; // [rad/sec] error value to be integrated


                        // limit output and anti-windup for integrator
                        if (controller_x_roll > 1) { controller_x_roll = 1; dDELTA_x_roll__int_dt = 0; };
                        if (controller_x_roll < -1) { controller_x_roll = -1; dDELTA_x_roll__int_dt = 0; };
                        if (controller_y_yaw > 1) { controller_y_yaw = 1; dDELTA_y_yaw__int_dt = 0; };
                        if (controller_y_yaw < -1) { controller_y_yaw = -1; dDELTA_y_yaw__int_dt = 0; };
                        if (controller_z_pitch > 1) { controller_z_pitch = 1; dDELTA_z_pitch__int_dt = 0; };
                        if (controller_z_pitch < -1) { controller_z_pitch = -1; dDELTA_z_pitch__int_dt = 0; };

                        // weight of different control signals   TODO better: let each signal use the complete range, if other signal is small 
                        delta_lat_mr = controller_x_roll * 0.60f - controller_y_yaw * 0.40f;
                        delta_lat_tr = controller_x_roll * 0.60f + controller_y_yaw * 0.40f;

                        delta_col_mr = +controller_z_pitch * 0.20f + input_y_col * 0.80f;
                        delta_col_tr = -controller_z_pitch * 0.20f + input_y_col * 0.80f;

                        // handle deactivation of controller
                        if (par.transmitter_and_helicopter.helicopter.flybarless.direct_feadtrough_flybarless.val == true)
                        {
                            delta_lat_mr = input_x_roll * 0.50f - controller_y_yaw * 0.50f;
                            delta_lat_tr = input_x_roll * 0.50f + controller_y_yaw * 0.50f;

                            delta_col_mr = +input_z_pitch * 0.25f + input_y_col * 0.75f;
                            delta_col_tr = -input_z_pitch * 0.25f + input_y_col * 0.75f;
                        }
                        if (par.transmitter_and_helicopter.helicopter.gyro.direct_feadtrough_gyroscope.val == true)
                        {
                            delta_lat_mr = controller_x_roll * 0.50f - input_y_yaw * 0.50f;
                            delta_lat_tr = controller_x_roll * 0.50f + input_y_yaw * 0.50f;
                        }
                        if (par.transmitter_and_helicopter.helicopter.flybarless.direct_feadtrough_flybarless.val == true &&
                            par.transmitter_and_helicopter.helicopter.gyro.direct_feadtrough_gyroscope.val == true)
                        {
                            delta_lat_mr = input_x_roll * 0.50f - input_y_yaw * 0.50f;
                            delta_lat_tr = input_x_roll * 0.50f + input_y_yaw * 0.50f;

                            delta_col_mr = +input_z_pitch * 0.25f + input_y_col * 0.75f;
                            delta_col_tr = -input_z_pitch * 0.25f + input_y_col * 0.75f;
                        }


                        // limit output and anti-windup for integrator
                        if (delta_lat_mr > 1) { delta_lat_mr = 1; };
                        if (delta_lat_mr < -1) { delta_lat_mr = -1; };
                        if (delta_col_mr > 1) { delta_col_mr = 1; };
                        if (delta_col_mr < -1) { delta_col_mr = -1; };
                        if (delta_lat_tr > 1) { delta_lat_tr = 1; };
                        if (delta_lat_tr < -1) { delta_lat_tr = -1; };
                        if (delta_col_tr > 1) { delta_col_tr = 1; };
                        if (delta_col_tr < -1) { delta_col_tr = -1; };
                        break;
                    }
            }
            // ##################################################################################




            // ##################################################################################
            // turn off mainrotor flapping and flybarelles control when low rpm ( after landing )
            // ##################################################################################
            float turn_off_flapping_below_this_rotational_speed = par.transmitter_and_helicopter.helicopter.governor.target_rpm.vect3[0] * 0.7f * Helper.Rpm_to_RadPerSec;
            float turn_off_flapping_completely_rotational_speed = par.transmitter_and_helicopter.helicopter.governor.target_rpm.vect3[0] * 0.1f * Helper.Rpm_to_RadPerSec;

            reduce_flapping_effect_at_low_rpm = Helper.Step((float)Math.Abs(omega_mr), turn_off_flapping_completely_rotational_speed, 0, turn_off_flapping_below_this_rotational_speed, 1);

            if (integrator_function_call_number == 0)  // TODO
            {
                if (Math.Abs(omega_mr) < turn_off_flapping_below_this_rotational_speed) // TODO
                {
                    //delta_lat_mr *= reduce_flapping_effect_at_low_rpm;
                    //delta_lon_mr *= reduce_flapping_effect_at_low_rpm;

                    float factor = Helper.Step(Mathf.Abs((float)omega_mr), turn_off_flapping_completely_rotational_speed, 0.003f, turn_off_flapping_below_this_rotational_speed, 0);

                    float factor1 = factor;  // TODO
                    // mainrotor flapping dynamics  - reduce state value to zero
                    x_states[13] -= Math.Sign(x_states[13]) * factor1; // [rad] mainrotor pitch flapping angle a_s (longitudial direction)  // TODO
                    x_states[14] -= Math.Sign(x_states[14]) * factor1; // [rad] mainrotor roll flapping angle b_s (lateral direction)  // TODO
                    x_states[15] -= Math.Sign(x_states[15]) * factor1; // [rad] tailrotor pitch flapping angle a_s (longitudial direction)  // TODO
                    x_states[16] -= Math.Sign(x_states[16]) * factor1; // [rad] tailrotor roll flapping angle b_s (lateral direction)  // TODO

                    const float factor2 = 0.01f;
                    // flybarless controller  - reduce state value to zero
                    x_states[22] -= Math.Sign(x_states[22]) * factor2; // [rad/sec] flybarless error value integral  // TODO
                    x_states[23] -= Math.Sign(x_states[23]) * factor2 * 0.01f; // [rad/sec] gyro error value integral  // TODO
                    x_states[24] -= Math.Sign(x_states[24]) * factor2; // [rad/sec] flybarless error value integral  // TODO

                    //// servo movement damping - reduce state value to zero
                    //x_states[25] -= Math.Sign(x_states[25]) * factor2;  // [rad] damping of mainrotor collective movement - Collective
                    //x_states[26] -= Math.Sign(x_states[26]) * factor2;  // [rad] damping of mainrotor lateral movement - Roll
                    //x_states[27] -= Math.Sign(x_states[27]) * factor2;  // [rad] damping of mainrotor longitudial movement - Pitch
                    //x_states[28] -= Math.Sign(x_states[28]) * factor2;  // [rad] damping of tailrotor collective movement - Yaw
                    //x_states[29] -= Math.Sign(x_states[29]) * factor2;  // [rad] damping of tailrotor lateral movement - Yaw
                    //x_states[30] -= Math.Sign(x_states[30]) * factor2;  // [rad] damping of tailrotor longitudial movement - Yaw

                }
                if (Math.Abs(omega_mr) < (60f * Helper.Rpm_to_RadPerSec))
                {
                    x_states[13] = 0;
                    x_states[14] = 0;
                    x_states[15] = 0;
                    x_states[16] = 0;

                    x_states[22] = 0;
                    x_states[23] = 0;
                    x_states[24] = 0;
                }
            }
            // ##################################################################################








            // ##################################################################################
            // servo movement damping and input signal scaling 
            // ##################################################################################
            // damping of servo movement as pt1-element (servos are not direct modelled, just their effect at the separate inputs)
            // diff. equation of pt1: dydt = (K * u - y) / T
            const double K = 1;
            const float min_servo_time_constant = 0.005f; // [sec]
            if (par.transmitter_and_helicopter.helicopter.servo_damping.servo_col_mr_time_constant.val > min_servo_time_constant)
                dservo_col_mr_damped_dt = (K * delta_col_mr - servo_col_mr_damped) / par.transmitter_and_helicopter.helicopter.servo_damping.servo_col_mr_time_constant.val;  // [-1...1] damping of mainrotor collective movement - Collective
            else
                servo_col_mr_damped = delta_col_mr;

            if (par.transmitter_and_helicopter.helicopter.servo_damping.servo_lat_mr_time_constant.val > min_servo_time_constant)
                dservo_lat_mr_damped_dt = (K * delta_lat_mr - servo_lat_mr_damped) / par.transmitter_and_helicopter.helicopter.servo_damping.servo_lat_mr_time_constant.val;  // [-1...1] damping of mainrotor lateral movement - Roll
            else
                servo_lat_mr_damped = delta_lat_mr;

            if (par.transmitter_and_helicopter.helicopter.servo_damping.servo_lon_mr_time_constant.val > min_servo_time_constant)
                dservo_lon_mr_damped_dt = (K * delta_lon_mr - servo_lon_mr_damped) / par.transmitter_and_helicopter.helicopter.servo_damping.servo_lon_mr_time_constant.val;  // [-1...1] damping of mainrotor longitudial movement - Pitch
            else
                servo_lon_mr_damped = delta_lon_mr;


            if (par.transmitter_and_helicopter.helicopter.servo_damping.servo_col_tr_time_constant.val > min_servo_time_constant)
                dservo_col_tr_damped_dt = (K * delta_col_tr - servo_col_tr_damped) / par.transmitter_and_helicopter.helicopter.servo_damping.servo_col_tr_time_constant.val;  // [-1...1] damping of tailrotor collective movement - Yaw
            else
                servo_col_tr_damped = delta_col_tr;

            if (par.transmitter_and_helicopter.helicopter.servo_damping.servo_lat_tr_time_constant.val > min_servo_time_constant)
                dservo_lat_tr_damped_dt = (K * delta_lat_tr - servo_lat_tr_damped) / par.transmitter_and_helicopter.helicopter.servo_damping.servo_lat_tr_time_constant.val;  // [-1...1] damping of tailrotor lateral movement 
            else
                servo_lat_tr_damped = delta_lat_tr;

            if (par.transmitter_and_helicopter.helicopter.servo_damping.servo_lon_tr_time_constant.val > min_servo_time_constant)
                dservo_lon_tr_damped_dt = (K * delta_lon_tr - servo_lon_tr_damped) / par.transmitter_and_helicopter.helicopter.servo_damping.servo_lon_tr_time_constant.val;  // [-1...1] damping of tailrotor longitudial movement 
            else
                servo_lon_tr_damped = delta_lon_tr;



            switch (par.transmitter_and_helicopter.helicopter.rotor_systems_configuration.val)
            {
                case 0: // 0: Single Main Rotor
                    {
                        // convert normalized controller output to angle
                        Theta_cyc_b_s_mr = par.transmitter_and_helicopter.helicopter.flapping.B_lat.val * Mathf.Deg2Rad * (float)servo_lat_mr_damped; // [rad]
                        Theta_cyc_a_s_mr = par.transmitter_and_helicopter.helicopter.flapping.A_lon.val * Mathf.Deg2Rad * (float)servo_lon_mr_damped; // [rad]    
                        //  y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1
                        //Theta_col_mr = (double)Helper.Two_Point_Form_Of_Line(servo_col_mr_damped,-1,1, par.transmitter_and_helicopter.helicopter.mainrotor.Theta_col_min.val, par.transmitter_and_helicopter.helicopter.mainrotor.Theta_col_max.val) * Mathf.Deg2Rad; // [rad] Theta_ped, K_ped 
                        //Theta_col_tr = (double)Helper.Two_Point_Form_Of_Line(servo_col_tr_damped, -1,1, par.transmitter_and_helicopter.helicopter.tailrotor.Theta_col_min.val, par.transmitter_and_helicopter.helicopter.tailrotor.Theta_col_max.val) * Mathf.Deg2Rad; // [rad] Theta_ped, K_ped 
                        //Theta_col_pr = (double)Helper.Two_Point_Form_Of_Line(delta_col_pr, -1,1, par.transmitter_and_helicopter.helicopter.propeller.Theta_col_min.val, par.transmitter_and_helicopter.helicopter.propeller.Theta_col_max.val) * Mathf.Deg2Rad; // [rad] Theta_ped, K_ped 

                        Theta_col_mr = (par.transmitter_and_helicopter.helicopter.mainrotor.K_col.val * (float)servo_col_mr_damped + par.transmitter_and_helicopter.helicopter.mainrotor.Theta_col_0.val) * Mathf.Deg2Rad;
                        Theta_col_tr = (par.transmitter_and_helicopter.helicopter.tailrotor.K_col.val * (float)servo_col_tr_damped + par.transmitter_and_helicopter.helicopter.tailrotor.Theta_col_0.val) * Mathf.Deg2Rad; // [rad] Theta_ped, K_ped    
                        Theta_col_pr = (par.transmitter_and_helicopter.helicopter.propeller.K_col.val * (float)delta_col_pr + par.transmitter_and_helicopter.helicopter.propeller.Theta_col_0.val) * Mathf.Deg2Rad; // [rad] Theta_ped, K_ped 
                        break;
                    }
                case 1: // 1: Tandem Rotor
                    {
                        Theta_cyc_b_s_mr = par.transmitter_and_helicopter.helicopter.flapping.B_lat.val * Mathf.Deg2Rad * (float)servo_lat_mr_damped; // [rad]
                        Theta_cyc_a_s_mr = 0; // [rad]        
                        Theta_cyc_b_s_tr = par.transmitter_and_helicopter.helicopter.flapping.B_lat.val * Mathf.Deg2Rad * (float)servo_lat_tr_damped; // [rad]
                        Theta_cyc_a_s_tr = 0; // [rad]   
                        //  y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1
                        //Theta_col_mr = (double)Helper.Two_Point_Form_Of_Line(servo_col_mr_damped, -1, 1, par.transmitter_and_helicopter.helicopter.mainrotor.Theta_col_min.val, par.transmitter_and_helicopter.helicopter.mainrotor.Theta_col_max.val) * Mathf.Deg2Rad; // [rad] Theta_ped, K_ped 
                        //Theta_col_mr = (double)Helper.Two_Point_Form_Of_Line(servo_col_tr_damped, -1, 1, par.transmitter_and_helicopter.helicopter.tailrotor.Theta_col_min.val, par.transmitter_and_helicopter.helicopter.tailrotor.Theta_col_max.val) * Mathf.Deg2Rad; // [rad] Theta_ped, K_ped 
                        //Theta_col_mr = (double)Helper.Two_Point_Form_Of_Line(delta_col_pr, -1, 1, par.transmitter_and_helicopter.helicopter.propeller.Theta_col_min.val, par.transmitter_and_helicopter.helicopter.propeller.Theta_col_max.val) * Mathf.Deg2Rad; // [rad] Theta_ped, K_ped 

                        Theta_col_mr = (par.transmitter_and_helicopter.helicopter.mainrotor.K_col.val * (float)servo_col_mr_damped + par.transmitter_and_helicopter.helicopter.mainrotor.Theta_col_0.val) * Mathf.Deg2Rad;
                        Theta_col_tr = (par.transmitter_and_helicopter.helicopter.tailrotor.K_col.val * (float)servo_col_tr_damped + par.transmitter_and_helicopter.helicopter.tailrotor.Theta_col_0.val) * Mathf.Deg2Rad; // [rad] Theta_ped, K_ped 
                        Theta_col_pr = (par.transmitter_and_helicopter.helicopter.propeller.K_col.val * (float)delta_col_pr + par.transmitter_and_helicopter.helicopter.propeller.Theta_col_0.val) * Mathf.Deg2Rad; // [rad] Theta_ped, K_ped 
                        break;
                    }
            }
            // ##################################################################################




            // ##################################################################################
            // after resetting the simulation the user input is turned off for 1 sec and is fully active after 2sec
            // ##################################################################################            
            if (time < Mathf.Abs(par.simulation.gameplay.delay_after_reset.val))
            {
                Theta_cyc_b_s_mr *= set_controll_to_zero_after_simulation_reset;
                Theta_cyc_a_s_mr *= set_controll_to_zero_after_simulation_reset;
                Theta_cyc_b_s_tr *= set_controll_to_zero_after_simulation_reset;
                Theta_cyc_a_s_tr *= set_controll_to_zero_after_simulation_reset;
                Theta_col_mr *= set_controll_to_zero_after_simulation_reset;
                //if (par.transmitter_and_helicopter.helicopter.rotor_systems_configuration.val == 0) // single main rotor
                // -
                if (par.transmitter_and_helicopter.helicopter.rotor_systems_configuration.val == 1)   // tandem rotor
                    Theta_col_tr *= set_controll_to_zero_after_simulation_reset;

                Theta_col_pr *= set_controll_to_zero_after_simulation_reset;
            }
            // ##################################################################################








            // ##################################################################################
            // calcualte the rotor mechanics states --> results in blade angle beta (and 3d part positions)
            // ################################################################################## 
            if (integrator_function_call_number == 0)
            {
                if (Helicopter_Main.mainrotor_simplified0_or_BEMT1 == 1 || Helicopter_Main.Helicopter_Mainrotor_Mechanics.rotor_3d_mechanics_geometry_available == true)
                {
                    //delta_lat_mr = 0; // [-1 ... 1] Roll  
                    //delta_lon_mr = 0; // [-1 ... 1] Pitch 
                    float roll = Mathf.Clamp(delta_lat_mr, -1, 1) * -15.0000000f * Mathf.Deg2Rad;
                    float pitch = Mathf.Clamp(delta_lon_mr, -1, 1) * -15.0000000f * Mathf.Deg2Rad;
                    //float collective = Mathf.Clamp(input_y_col, -1, 1) * -0.005f;
                    float collective =   (float)Theta_col_mr * Mathf.Rad2Deg / par.transmitter_and_helicopter.helicopter.mainrotor.K_col.val * -0.00650000f; // [m] meter and not rad!   TODO what is the relationship between collective-stroke [m] and blade angle [deg]????
                    //float Omega_mr_ = (float)Omega_mr;

                    Helicopter_Main.Helicopter_Mainrotor_Mechanics.Calculate(roll, pitch, collective, (float)Omega_mr, ref beta, (float)dtime, integrator_function_call_number);

                    float beta_sum = 0; // [rad]
                    for (int i = 0; i < beta.Length; i++) beta_sum += beta[i];
                    ODEDebug.Theta_col_mr = beta_sum / beta.Length; // [rad]
                }
                else
                {
                   ODEDebug.Theta_col_mr = (float)Theta_col_mr; // [rad]
                }


                //switch (par.transmitter_and_helicopter.helicopter.rotor_systems_configuration.val)
                if (Helicopter_Main.Helicopter_Tailrotor_Mechanics.rotor_3d_mechanics_geometry_available == true)
                {
                    float yaw = Mathf.Clamp(delta_col_tr, -1, 1) * -30.0000000f * Mathf.Deg2Rad;
                    float Omega_tr = (float)Omega_mr / par.transmitter_and_helicopter.helicopter.transmission.n_mr2tr.val; //  (float)time; // [rad]
float beta_tr = 0; // calculated blade angle not used yet

                    Helicopter_Main.Helicopter_Tailrotor_Mechanics.Calculate(yaw, (float)Omega_tr, ref beta_tr, (float)dtime, integrator_function_call_number);
                }
            }

            // ##################################################################################






            //###################################################################
            // collision detection with ground mesh (reduced komplexity)
            //###################################################################
            //point_positionL.Clear(); // local coordinate system
            point_positionR.Clear(); // reference coordinate system
            point_type.Clear();

            for (var i = 0; i < par.transmitter_and_helicopter.helicopter.collision.positions_usual.vect3.Count; i++)
            {
                //point_positionL.Add(par.transmitter_and_helicopter.helicopter.collision.positions_usual.vect3[i]); // [m]
                point_positionR.Add(vectO + Helper.A_RL(q, par.transmitter_and_helicopter.helicopter.collision.positions_usual.vect3[i])); // [m] expressed in inertial frame
                point_type.Add(Collision_Point_Type.usual);
            }
            for (var i = 0; i < par.transmitter_and_helicopter.helicopter.collision.positions_left.vect3.Count; i++)
            {
                //point_positionL.Add(par.transmitter_and_helicopter.helicopter.collision.positions_left.vect3[i]); // [m]

                float gear_rising_offset = Landing_Gear_Collision_Point_Transition(
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_gear.val,
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_bay.val,
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_mechanism_tilted_forward.val,
                    collision_positions_landing_gear_left_rising_offset);

                point_positionR.Add(vectO + Helper.A_RL(q, par.transmitter_and_helicopter.helicopter.collision.positions_left.vect3[i] +
                    par.transmitter_and_helicopter.helicopter.collision.positions_left_rised_offset.vect3 * gear_rising_offset)); // [m] expressed in inertial frame
                point_type.Add(Collision_Point_Type.gear_or_skid_left);
            }
            for (var i = 0; i < par.transmitter_and_helicopter.helicopter.collision.positions_right.vect3.Count; i++)
            {
                //point_positionL.Add(par.transmitter_and_helicopter.helicopter.collision.positions_right.vect3[i]); // [m]

                float gear_rising_offset = Landing_Gear_Collision_Point_Transition(
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_gear.val,
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_bay.val,
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_mechanism_tilted_forward.val,
                    collision_positions_landing_gear_right_rising_offset); // [0...1]

                point_positionR.Add(vectO + Helper.A_RL(q, par.transmitter_and_helicopter.helicopter.collision.positions_right.vect3[i] +
                    par.transmitter_and_helicopter.helicopter.collision.positions_right_rised_offset.vect3 * gear_rising_offset)); // [m] expressed in inertial frame
                point_type.Add(Collision_Point_Type.gear_or_skid_right);
            }
            for (var i = 0; i < par.transmitter_and_helicopter.helicopter.collision.positions_steering_center.vect3.Count; i++)
            {
                //point_positionL.Add(par.transmitter_and_helicopter.helicopter.collision.positions_steering_center.vect3[i]); // [m]

                float gear_rising_offset = Landing_Gear_Collision_Point_Transition(
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_gear.val,
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_bay.val,
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_mechanism_tilted_forward.val,
                    collision_positions_landing_gear_steering_center_rising_offset); // [0...1]

                point_positionR.Add(vectO + Helper.A_RL(q, par.transmitter_and_helicopter.helicopter.collision.positions_steering_center.vect3[i] +
                    par.transmitter_and_helicopter.helicopter.collision.positions_steering_center_rised_offset.vect3 * gear_rising_offset)); // [m] expressed in inertial frame
                point_type.Add(Collision_Point_Type.gear_or_support_steering_center);
            }
            for (var i = 0; i < par.transmitter_and_helicopter.helicopter.collision.positions_steering_left.vect3.Count; i++)
            {
                //point_positionL.Add(par.transmitter_and_helicopter.helicopter.collision.positions_steering_left.vect3[i]); // [m]

                float gear_rising_offset = Landing_Gear_Collision_Point_Transition(
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_gear.val,
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_bay.val,
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_mechanism_tilted_forward.val,
                    collision_positions_landing_gear_steering_left_rising_offset); // [0...1]

                point_positionR.Add(vectO + Helper.A_RL(q, par.transmitter_and_helicopter.helicopter.collision.positions_steering_left.vect3[i] +
                    par.transmitter_and_helicopter.helicopter.collision.positions_steering_left_rised_offset.vect3 * gear_rising_offset)); // [m] expressed in inertial frame
                point_type.Add(Collision_Point_Type.gear_or_support_steering_left);
            }
            for (var i = 0; i < par.transmitter_and_helicopter.helicopter.collision.positions_steering_right.vect3.Count; i++)
            {
                //point_positionL.Add(par.transmitter_and_helicopter.helicopter.collision.positions_steering_right.vect3[i]); // [m]

                float gear_rising_offset = Landing_Gear_Collision_Point_Transition(
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_gear.val,
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_bay.val,
                    par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_mechanism_tilted_forward.val,
                    collision_positions_landing_gear_steering_right_rising_offset); // [0...1]

                point_positionR.Add(vectO + Helper.A_RL(q, par.transmitter_and_helicopter.helicopter.collision.positions_steering_right.vect3[i] +
                    par.transmitter_and_helicopter.helicopter.collision.positions_steering_right_rised_offset.vect3 * gear_rising_offset)); // [m] expressed in inertial frame
                point_type.Add(Collision_Point_Type.gear_or_support_steering_right);
            }
            point_positionR.Add(vectO + Helper.A_RL(q, par.transmitter_and_helicopter.helicopter.mainrotor.posLH.vect3)); // [m] expressed in inertial frame
            point_type.Add(Collision_Point_Type.mainrotor_for_groundeffect);
            point_positionR.Add(vectO + Helper.A_RL(q, par.transmitter_and_helicopter.helicopter.tailrotor.posLH.vect3)); // [m] expressed in inertial frame
            point_type.Add(Collision_Point_Type.tailrotor_for_groundeffect);



            //###################################################################
            // main collision detection routine
            //###################################################################
            //if (integrator_function_call_number == 0)
            //   contact_informations = Common.Helper.Collision_Point_To_Mesh(vectO, helicopters_aabb, ground_mesh, point_positionR);
            contact_informations = Common.Helper.Collision_Point_To_Mesh(vectO, helicopters_aabb, ground_mesh, point_positionR);


            //// debug
            //if (integrator_function_call_number == 0)
            //{ 
            //    string ss = "C: ";
            //    for (int i = 0; i < contact_informations.Count; i++)
            //        ss += contact_informations[i].penetration.ToString() + ", ";
            //    UnityEngine.Debug.Log(ss);
            //}

            // simplified contact with simple plane
            //if (integrator_function_call_number == 0)
            //{
            //    contact_informations = new List<Common.Helper.contact_info>();
            //    for (int j = 0; j < point_positionR.Count; j++)
            //    {
            //        contact_informations.Add(new Common.Helper.contact_info());
            //        if (point_positionR[j].y < 0)
            //        {
            //            contact_informations[j].flag_has_contact = true;
            //            contact_informations[j].normalR = new Vector3(0, 1, 0);
            //            contact_informations[j].penetration = point_positionR[j].y;
            //        }
            //        else
            //        {
            //            contact_informations[j].flag_has_contact = false;
            //        }
            //    }
            //}




            //###################################################################
            // wheel brake transition factor from on to off ( 1 ... 0 )
            //###################################################################
            if (integrator_function_call_number == 0)
            {
                // overwrite wheel_brake_strength if wheels are raised and set them to full applied
                if (wheel_status == Wheels_Status_Variants.raised)
                {
                    wheel_brake_strength = Mathf.SmoothDamp(wheel_brake_strength, 1.000000000000000f, ref wheel_brake_transition_velocity, wheel_brake_transition_duration, 1000, (float)dtime);
                }
                else
                {
                    wheel_brake_strength = Mathf.SmoothDamp(wheel_brake_strength, wheel_brake_target, ref wheel_brake_transition_velocity, wheel_brake_transition_duration, 1000, (float)dtime);
                }
            }

            //###################################################################
            // calculation of collision forces with ground mesh
            //###################################################################
            //const float V_h = 0.02f; // [m/s] friction to stiction transition velocity 
            //const float U_max = 0.02f; // [m] stiction model's maximal distance to ancor point
            //const float stiction_factor = 1.5f; // defines the stiction factor (static friction) as x-times the sliding factor
            float V_h = par.simulation.various.V_h.val; // [m/s] friction to stiction transition velocity 
            float U_max = par.simulation.various.U_max.val; // [m] stiction model's maximal distance to ancor point                                            
            float stiction_factor = par.simulation.various.stiction_factor.val; // defines the stiction factor (static friction) as x-times the sliding factor

            // local frame to reference frame
            //omegaO = Helper.A_RL(q, omegaLH);  // [rad/sec]

            // reset values
            Vector3 contact_forceR, friction_forceR = new Vector3(0, 0, 0), friction_forceL;
            if (integrator_function_call_number == 0)
            {
                force_y_gear_or_skid_leftLH_temp = 0; force_y_gear_or_skid_rightLH_temp = 0; force_y_gear_or_support_steering_centerLH_temp = 0;
                force_y_gear_or_support_steering_leftLH_temp = 0; force_y_gear_or_support_steering_rightLH_temp = 0;
            }
            // check collision for all defined collision points
            for (var i = 0; i < point_positionR.Count - 2; i++) // -2 because the last two points are the main- and tailrotor hub distances to ground for ground-effect calculation ( not for collision detection) 
            {
                // bumping on ground
                if (contact_informations[i].flag_has_contact == true)
                {
                    float bump_height = par.scenery.ground.bump_height.val; // [m]
                    float bump_density_scale = par.scenery.ground.bump_density_scale.val; // [1/m]
                    contact_informations[i].penetration += Mathf.Clamp(bump_height * Mathf.PerlinNoise(point_positionR[i].x * bump_density_scale, point_positionR[i].z * bump_density_scale), 0.0f, bump_height);
                    contact_informations[i].penetration = Mathf.Clamp(contact_informations[i].penetration, -1.0f, 0);
                }

                // calculate velocity in point: vR = A_RL(q) * (vL + cross(omegaLH,positionL))
                Vector3 point_velocityR = Helper.A_RL(q, veloLH + Helper.Cross(omegaLH, point_positionL[i])); // [m/sec]

                // normal velocity component (for damping calulation, expressed in trinagles normal direction)
                float point_velocity_scalar_normal_to_triangle = Vector3.Dot(point_velocityR, contact_informations[i].normalR) / contact_informations[i].normalR.magnitude;
                
                // velocity vector parallel to triangle surface (expressed in reference frame)
                Vector3 point_velocity_vector_parallel_to_triangleR = point_velocityR - contact_informations[i].normalR * point_velocity_scalar_normal_to_triangle;
                //float point_velocity_scalar_parallel_to_triangleR = point_velocity_vector_parallel_to_triangleR.magnitude;

                // helicopter's forward direction velocity component (+x), projected onto triangle 
                Vector3 helicopter_forward_directionR = Helper.A_RL(q, new Vector3(1, 0, 0));
                Vector3 helicopter_forward_direction_projected_to_triangleR = Helper.Projection_Of_A_Vector_Onto_A_Plane(helicopter_forward_directionR, contact_informations[i].normalR);
                //helicopter_forward_direction_projected_to_triangleR.Normalize();
                float points_velocity_component_in_helicopters_forward_direction_at_triangle = Vector3.Dot(point_velocity_vector_parallel_to_triangleR, helicopter_forward_direction_projected_to_triangleR) / helicopter_forward_direction_projected_to_triangleR.magnitude;

                // helicopter's sidewards direction velocity component (+z), projected onto triangle 
                Vector3 helicopter_sidewards_directionR = Helper.A_RL(q, new Vector3(0, 0, 1));
                Vector3 helicopter_sidewards_direction_projected_to_triangleR = Helper.Projection_Of_A_Vector_Onto_A_Plane(helicopter_sidewards_directionR, contact_informations[i].normalR);
                //helicopter_right_direction_projected_to_triangleR.Normalize();
                float points_velocity_component_in_helicopters_sideward_direction_at_triangle = Vector3.Dot(point_velocity_vector_parallel_to_triangleR, helicopter_sidewards_direction_projected_to_triangleR) / helicopter_sidewards_direction_projected_to_triangleR.magnitude;



                // store contact point position as ancor point for stiction model
                if (Mathf.Abs(points_velocity_component_in_helicopters_forward_direction_at_triangle) >= V_h ||
                    Mathf.Abs(points_velocity_component_in_helicopters_sideward_direction_at_triangle) >= V_h || point_velocityR.magnitude >= V_h )
                {
                    if (integrator_function_call_number == 0) // update the position only at Runghe Kutta's first call
                        stiction_ancor_point_memoryR[i] = point_positionR[i];
                }



                if (contact_informations[i].flag_has_contact == true)
                {

                    // normal contact force and damping
                    float v_dampingR = point_velocity_scalar_normal_to_triangle;
                    if (point_velocity_scalar_normal_to_triangle > 0)
                        v_dampingR = point_velocity_scalar_normal_to_triangle * 0.01f;

                    contact_forceR = -par.transmitter_and_helicopter.helicopter.collision.stiffness_factor.val * contact_informations[i].normalR * contact_informations[i].penetration
                                     -par.transmitter_and_helicopter.helicopter.collision.damping_factor.val * contact_informations[i].normalR * v_dampingR;
                    torque_contactO = Helper.Cross(point_positionR[i] - vectO, contact_forceR);
                    torque_contactLH = Helper.A_LR(q, torque_contactO);   //[Nm] // reference frame to local frame




                    // friction force coefficient setup
                    float mu_forward = 0.1f;
                    float mu_sideward = 0.1f;

                    if (point_type[i] == Collision_Point_Type.gear_or_skid_left)
                    {
                        if (par.transmitter_and_helicopter.helicopter.collision.positions_left_type.val == 0) // 0:skids
                        {
                            mu_forward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_forward.val;
                        }
                        if (par.transmitter_and_helicopter.helicopter.collision.positions_left_type.val == 1) // 1:gear
                        {
                            float mu_rolling = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_forward.val; // rolling
                            float mu_braking = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_sideward.val; // braking
                            //  y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1;
                            mu_forward = ((mu_braking - mu_rolling) / (1.0f - 0.0f)) * (wheel_brake_strength - 0.0f) + mu_rolling;
                        }
                        mu_sideward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_sideward.val;
                    }
                    if (point_type[i] == Collision_Point_Type.gear_or_skid_right)
                    {
                        if (par.transmitter_and_helicopter.helicopter.collision.positions_right_type.val == 0) // 0:skids
                        {
                            mu_forward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_forward.val;
                        }
                        if (par.transmitter_and_helicopter.helicopter.collision.positions_right_type.val == 1) // 1:gear
                        {
                            float mu_rolling = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_forward.val; // rolling
                            float mu_braking = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_sideward.val; // braking
                            //  y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1;
                            mu_forward = ((mu_braking - mu_rolling) / (1.0f - 0.0f)) * (wheel_brake_strength - 0.0f) + mu_rolling;
                        }
                        mu_sideward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_sideward.val;
                    }
                    if (point_type[i] == Collision_Point_Type.gear_or_support_steering_center)
                    {
                        if (par.transmitter_and_helicopter.helicopter.collision.positions_steering_center_type.val == 0) // 0:support
                        {
                            mu_forward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_forward.val;
                            mu_sideward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_sideward.val;
                        }
                        if (par.transmitter_and_helicopter.helicopter.collision.positions_steering_center_type.val == 1) // 1:gear
                        {
                            float mu_rolling = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_steering.val; // rolling
                            float mu_braking = par.transmitter_and_helicopter.helicopter.collision.friction_coeff.val; // braking
                            //  y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1;
                            mu_forward = ((mu_braking - mu_rolling) / (1.0f - 0.0f)) * (wheel_brake_strength - 0.0f) + mu_rolling;
                            // center steering wheel can turn, therefore also the sidewards (relative to helicopter coordinate system) direction 
                            // friction factor is changed:
                            mu_sideward = ((mu_braking - mu_rolling) / (1.0f - 0.0f)) * (wheel_brake_strength - 0.0f) + mu_rolling;
                        }
                    }
                    if (point_type[i] == Collision_Point_Type.gear_or_support_steering_left)
                    {
                        if (par.transmitter_and_helicopter.helicopter.collision.positions_steering_left_type.val == 0) // 0:support
                        {
                            mu_forward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_forward.val;
                            mu_sideward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_sideward.val;
                        }
                        if (par.transmitter_and_helicopter.helicopter.collision.positions_steering_left_type.val == 1) // 1:gear
                        {
                            float mu_rolling = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_steering.val; // rolling
                            float mu_braking = par.transmitter_and_helicopter.helicopter.collision.friction_coeff.val; // braking
                            //  y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1;
                            mu_forward = ((mu_braking - mu_rolling) / (1.0f - 0.0f)) * (wheel_brake_strength - 0.0f) + mu_rolling;
                            // left steering wheel can turn, therefore also the sidewards (relative to helicopter coordinate system) direction 
                            // friction factor is changed:
                            mu_sideward = ((mu_braking - mu_rolling) / (1.0f - 0.0f)) * (wheel_brake_strength - 0.0f) + mu_rolling;
                        }
                    }
                    if (point_type[i] == Collision_Point_Type.gear_or_support_steering_right)
                    {
                        if (par.transmitter_and_helicopter.helicopter.collision.positions_steering_right_type.val == 0) // 0:support
                        {
                            mu_forward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_forward.val;
                            mu_sideward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_sideward.val;
                        }
                        if (par.transmitter_and_helicopter.helicopter.collision.positions_steering_right_type.val == 1) // 1:gear
                        {
                            float mu_rolling = par.transmitter_and_helicopter.helicopter.collision.friction_coeff_steering.val; // rolling
                            float mu_braking = par.transmitter_and_helicopter.helicopter.collision.friction_coeff.val; // braking
                            //  y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1;
                            mu_forward = ((mu_braking - mu_rolling) / (1.0f - 0.0f)) * (wheel_brake_strength - 0.0f) + mu_rolling;
                            // right steering wheel can turn, therefore also the sidewards (relative to helicopter coordinate system) direction 
                            // friction factor is changed:
                            mu_sideward = ((mu_braking - mu_rolling) / (1.0f - 0.0f)) * (wheel_brake_strength - 0.0f) + mu_rolling;
                        }
                    }
                    if (point_type[i] == Collision_Point_Type.usual)
                    {
                        mu_forward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff.val;
                        mu_sideward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff.val;
                    }




                    // friction force 
                    friction_forceR.x = 0;
                    friction_forceR.y = 0;
                    friction_forceR.z = 0;

                    // friction
                    //float V_rel = point_velocity_scalar_parallel_to_triangleR;
                    float mu_v_forward = 0f; // friction coefficient
                    float mu_v_sideward = 0f; // friction coefficient 
                    float mu_u_forward = 0f; // stiction coefficient
                    float mu_u_sideward = 0f; // stiction coefficient 
                    Vector3 vect_U_rel = Vector3.zero; // [m]

                    //mu_v_forward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff.val * 0.3f; // friction coefficient
                    //mu_v_sideward = par.transmitter_and_helicopter.helicopter.collision.friction_coeff.val * 1.0f; // friction coefficient 

                    beta_forward = 1;
                    beta_sideward = 1;
                    if (Mathf.Abs(points_velocity_component_in_helicopters_forward_direction_at_triangle) < V_h)
                        beta_forward = Mathf.Sin((Mathf.PI / 2f) * (points_velocity_component_in_helicopters_forward_direction_at_triangle / V_h)); // transition 

                    if (Mathf.Abs(points_velocity_component_in_helicopters_sideward_direction_at_triangle) < V_h)
                        beta_sideward = Mathf.Sin((Mathf.PI / 2f) * (points_velocity_component_in_helicopters_sideward_direction_at_triangle / V_h)); // transition 

                    mu_v_forward = Mathf.Abs(beta_forward * mu_forward); // friction coefficient
                    mu_v_sideward = Mathf.Abs(beta_sideward * mu_sideward); // friction coefficient
                    
                    // increase sliding-friction value close to V_h
                    mu_v_forward *= Helper.Step(Mathf.Abs(points_velocity_component_in_helicopters_forward_direction_at_triangle), V_h, stiction_factor, V_h * 2f, 1.0f);
                    mu_v_sideward *= Helper.Step(Mathf.Abs(points_velocity_component_in_helicopters_sideward_direction_at_triangle), V_h, stiction_factor, V_h * 2f, 1.0f);

                    // // https://functionbay.com/documentation/onlinehelp/default.htm#!Documents/slidingandstictionfriction.htm
                    //if (V_rel >= V_h)
                    if (Mathf.Abs(points_velocity_component_in_helicopters_forward_direction_at_triangle) >= V_h ||
                        Mathf.Abs(points_velocity_component_in_helicopters_sideward_direction_at_triangle) >= V_h)
                    {
                        //if (Mathf.Abs(points_velocity_component_in_helicopters_forward_direction_at_triangle) > 0.00001)
                        //{
                        //    float mu = (float)Math.Tanh((double)points_velocity_component_in_helicopters_forward_direction_at_triangle * 20f) * par.transmitter_and_helicopter.helicopter.collision.friction_coeff.val; // mu = f(velo) at v==0 friction coefficient mu must be zero, othervise singulartiy makes sim unstable
                        //    friction_forceR -= helicopter_forward_direction_projected_to_triangleR.normalized * mu_forward * contact_forceR.magnitude;
                        //}
                        //if (Mathf.Abs(points_velocity_component_in_helicopters_sideward_direction_at_triangle) > 0.00001)
                        //{
                        //    float mu = (float)Math.Tanh((double)points_velocity_component_in_helicopters_sideward_direction_at_triangle * 20f) * par.transmitter_and_helicopter.helicopter.collision.friction_coeff.val; // mu = f(velo) at v==0 friction coefficient mu must be zero, othervise singulartiy makes sim unstable
                        //    friction_forceR -= helicopter_sidewards_direction_projected_to_triangleR.normalized * mu_sideward * contact_forceR.magnitude;
                        //}
                        //friction_forceR -= helicopter_forward_direction_projected_to_triangleR.normalized * mu_v_forward * contact_forceR.magnitude;
                        //friction_forceR -= helicopter_sidewards_direction_projected_to_triangleR.normalized * mu_v_sideward * contact_forceR.magnitude;
                    }
                    else // stiction
                    {
                        vect_U_rel = stiction_ancor_point_memoryR[i] - point_positionR[i];
                        float vect_U_rel_forward = Vector3.Dot(vect_U_rel, helicopter_forward_direction_projected_to_triangleR) / helicopter_forward_direction_projected_to_triangleR.magnitude;
                        float vect_U_rel_sideward = Vector3.Dot(vect_U_rel, helicopter_sidewards_direction_projected_to_triangleR) / helicopter_sidewards_direction_projected_to_triangleR.magnitude;
   
                        if (Mathf.Abs(vect_U_rel_forward) >= U_max) vect_U_rel_forward = U_max * Mathf.Sign(vect_U_rel_forward);
                        if (Mathf.Abs(vect_U_rel_sideward) >= U_max) vect_U_rel_sideward = U_max * Mathf.Sign(vect_U_rel_sideward);

                        float mu_1_forward = Mathf.Sin((Mathf.PI / 2f) * ((vect_U_rel_forward) / U_max)); // [-1...+1]
                        float mu_1_sideward = Mathf.Sin((Mathf.PI / 2f) * ((vect_U_rel_sideward) / U_max)); // [-1...+1]

                        mu_u_forward = ((1.0f - Mathf.Abs((beta_forward))) * (mu_forward * stiction_factor) * mu_1_forward);
                        mu_u_sideward = ((1.0f - Mathf.Abs((beta_sideward))) * (mu_sideward * stiction_factor) * mu_1_sideward);

                    }

                    friction_forceR -=
                        -helicopter_forward_direction_projected_to_triangleR.normalized * mu_u_forward * contact_forceR.magnitude +
                        Mathf.Sign(points_velocity_component_in_helicopters_forward_direction_at_triangle) *
                        helicopter_forward_direction_projected_to_triangleR.normalized * mu_v_forward * contact_forceR.magnitude;

                    friction_forceR -=
                        -helicopter_sidewards_direction_projected_to_triangleR.normalized * mu_u_sideward * contact_forceR.magnitude +
                        Mathf.Sign(points_velocity_component_in_helicopters_sideward_direction_at_triangle) *
                        helicopter_sidewards_direction_projected_to_triangleR.normalized * mu_v_sideward * contact_forceR.magnitude;

                    friction_forceL = Helper.A_LR(q, friction_forceR);
                    torque_frictionLH = Helper.Cross(point_positionL[i], friction_forceL);





                    // all forces and torques
                    forcesO += contact_forceR + friction_forceR; // [N]
                    torque_contactLH_sum.x += torque_contactLH.x; // [Nm]
                    torque_contactLH_sum.y += torque_contactLH.y; // [Nm]
                    torque_contactLH_sum.z += torque_contactLH.z; // [Nm]
                    torque_frictionLH_sum.x += torque_frictionLH.x; // [Nm]
                    torque_frictionLH_sum.y += torque_frictionLH.y; // [Nm]
                    torque_frictionLH_sum.z += torque_frictionLH.z; // [Nm]



                    // if contact force is to high, then heli crashes and is reset to inital position
                    if (integrator_function_call_number == 3)
                    {
                        if (contact_forceR.magnitude > par.transmitter_and_helicopter.helicopter.collision.collision_force_max.val) // [N]
                        {
                            //Set_Initial_Conditions();
                            collision_force_too_high_flag = true;
                        }
                    }

                    // 
                    if (integrator_function_call_number == 0)
                    {
                        // collect the resulting forces in y-direction (upwards) for the animation of the deformation 
                        // of the landing gears or skids on left/right side and center.
                        if (point_type[i] == Collision_Point_Type.gear_or_skid_left)
                            force_y_gear_or_skid_leftLH_temp += Helper.Ayy_LR(q, contact_forceR.y + friction_forceR.y); // [N]
                        if (point_type[i] == Collision_Point_Type.gear_or_skid_right)
                            force_y_gear_or_skid_rightLH_temp += Helper.Ayy_LR(q, contact_forceR.y + friction_forceR.y); // [N]
                        if (point_type[i] == Collision_Point_Type.gear_or_support_steering_center)
                            force_y_gear_or_support_steering_centerLH_temp += Helper.Ayy_LR(q, contact_forceR.y + friction_forceR.y); // [N]
                        if (point_type[i] == Collision_Point_Type.gear_or_support_steering_left)
                            force_y_gear_or_support_steering_leftLH_temp += Helper.Ayy_LR(q, contact_forceR.y + friction_forceR.y); // [N]
                        if (point_type[i] == Collision_Point_Type.gear_or_support_steering_right)
                            force_y_gear_or_support_steering_rightLH_temp += Helper.Ayy_LR(q, contact_forceR.y + friction_forceR.y); // [N]

                        // wheel turning/steering
                        Vector3 point_velocity_vector_parallel_to_triangleL = Helper.A_LR(q, point_velocity_vector_parallel_to_triangleR);
                        if (point_type[i] == Collision_Point_Type.gear_or_support_steering_center)
                        {
                            if (point_velocity_vector_parallel_to_triangleR.sqrMagnitude > 0.005f) // [m/s]
                            {
                                wheel_steering_center_temp = ((Mathf.Atan2(point_velocity_vector_parallel_to_triangleL.x, point_velocity_vector_parallel_to_triangleL.z) + (2.0f * Mathf.PI)) % (2.0f * Mathf.PI)) / (2.0f * Mathf.PI); // [0...1]
                            }
                        }
                        if (point_type[i] == Collision_Point_Type.gear_or_support_steering_left)
                        {
                            if (point_velocity_vector_parallel_to_triangleR.sqrMagnitude > 0.005f) // [m/s]
                            {
                                wheel_steering_left_temp = ((Mathf.Atan2(point_velocity_vector_parallel_to_triangleL.x, point_velocity_vector_parallel_to_triangleL.z) + (2.0f * Mathf.PI)) % (2.0f * Mathf.PI)) / (2.0f * Mathf.PI); // [0...1]
                            }
                        }
                        if (point_type[i] == Collision_Point_Type.gear_or_support_steering_right)
                        {
                            if (point_velocity_vector_parallel_to_triangleR.sqrMagnitude > 0.005f) // [m/s]
                            {
                                wheel_steering_right_temp = ((Mathf.Atan2(point_velocity_vector_parallel_to_triangleL.x, point_velocity_vector_parallel_to_triangleL.z) + (2.0f * Mathf.PI)) % (2.0f * Mathf.PI)) / (2.0f * Mathf.PI); // [0...1]
                            }
                        }

                        // wheel rotation/rolling (integration of velocity to get traveled distance)
                        if (point_type[i] == Collision_Point_Type.gear_or_skid_left)
                        {
                            wheel_rolling_velocity_left_temp = points_velocity_component_in_helicopters_forward_direction_at_triangle;
                            wheel_rolling_distance_left_temp += wheel_rolling_velocity_left_temp * (float)dtime;
                        }
                            
                        if (point_type[i] == Collision_Point_Type.gear_or_skid_right)
                        {
                            wheel_rolling_velocity_right_temp = points_velocity_component_in_helicopters_forward_direction_at_triangle;
                            wheel_rolling_distance_right_temp += wheel_rolling_velocity_right_temp * (float)dtime;
                        }

                        if (point_type[i] == Collision_Point_Type.gear_or_support_steering_center)
                        {
                            wheel_rolling_velocity_steering_center_temp = point_velocity_vector_parallel_to_triangleR.magnitude;
                            wheel_rolling_distance_steering_center_temp += wheel_rolling_velocity_steering_center_temp * (float)dtime;
                        }
                            
                        if (point_type[i] == Collision_Point_Type.gear_or_support_steering_left)
                        {
                            wheel_rolling_velocity_steering_left_temp = point_velocity_vector_parallel_to_triangleR.magnitude;
                            wheel_rolling_distance_steering_left_temp += wheel_rolling_velocity_steering_left_temp * (float)dtime;
                        }
                           
                        if (point_type[i] == Collision_Point_Type.gear_or_support_steering_right)
                        {
                            wheel_rolling_velocity_steering_right_temp = point_velocity_vector_parallel_to_triangleR.magnitude;
                            wheel_rolling_distance_steering_right_temp += wheel_rolling_velocity_steering_right_temp * (float)dtime;
                        }
                    }

                    if (last_takestep_in_frame)
                    {
                        // in-game visual debug lines 
                        if (i < ODEDebug.contact_positionR.Count)
                        {
                            //ODEDebug.contact_positionR[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(point_positionR[i]);
                            //ODEDebug.contact_forceR1[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(contact_forceR + friction_forceR);

                            //ODEDebug.contact_positionR[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(point_positionR[i] + new Vector3(0,0.2f,0));
                            ODEDebug.contact_positionR[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(point_positionR[i] + new Vector3(0.0f, 0.0f, 0.0f));
                            if (par.transmitter_and_helicopter.helicopter.visual_effects.debug_force_arrow_scale.val > 0)
                            {
                                ODEDebug.contact_forceR1[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(contact_forceR + friction_forceR);
                                //ODEDebug.contact_forceR1[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(vect_U_rel);
                            }
                            else
                            {
                                //ODEDebug.contact_forceR1[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(
                                //    helicopter_forward_direction_projected_to_triangleR.normalized * mu_u_forward * contact_forceR.magnitude
                                //    +helicopter_sidewards_direction_projected_to_triangleR.normalized * mu_u_sideward * contact_forceR.magnitude);

                                Vector3 test = Vector3.zero;
                                test = -Mathf.Sign(points_velocity_component_in_helicopters_forward_direction_at_triangle) *
                                        helicopter_forward_direction_projected_to_triangleR.normalized * mu_v_forward * contact_forceR.magnitude;
                                ODEDebug.contact_forceR1[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(test);
                                test = -Mathf.Sign(points_velocity_component_in_helicopters_sideward_direction_at_triangle) *
                                        helicopter_sidewards_direction_projected_to_triangleR.normalized * mu_v_sideward * contact_forceR.magnitude;
                                ODEDebug.contact_forceR2[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(test);

                                //Vector3 test = Vector3.zero;
                                //test = -Mathf.Sign(points_velocity_component_in_helicopters_forward_direction_at_triangle) *
                                //        helicopter_forward_direction_projected_to_triangleR.normalized;
                                //ODEDebug.contact_forceR1[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(test);
                                //test = -Mathf.Sign(points_velocity_component_in_helicopters_sideward_direction_at_triangle) *
                                //        helicopter_sidewards_direction_projected_to_triangleR.normalized;
                                //ODEDebug.contact_forceR2[i] = Common.Helper.ConvertRightHandedToLeftHandedVector(test);

                            }
                        }
                    }

                }
                else
                {

                    // wheel rotation/rolling due to (faked) inertia (after gear loses contact to ground it still rotates)
                    // if brake enabled wheel should stop faster to freely rotate
                    // y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1; // two point form of a line
                    // y = ((y2 - y1)/(1 - 0)) * (wheel_brake_strength - 0) + y1;
                    float y1 = 0.0006f; // if brake disabled wheel rotates long time 
                    float y2 = 0.03f; // if brake enabled wheel rotates short period 
                    float wheel_friction_factor_for_free_rolling = ((y2 - y1) / (1 - 0)) * (wheel_brake_strength - 0) + y1;

                    if (point_type[i] == Collision_Point_Type.gear_or_skid_left)
                    {
                        if (wheel_rolling_velocity_left_temp > wheel_friction_factor_for_free_rolling) wheel_rolling_velocity_left_temp -= wheel_friction_factor_for_free_rolling;
                        if (wheel_rolling_velocity_left_temp < -wheel_friction_factor_for_free_rolling) wheel_rolling_velocity_left_temp += wheel_friction_factor_for_free_rolling;
                        wheel_rolling_distance_left_temp += wheel_rolling_velocity_left_temp * (float)dtime / point_positionR.Count;
                    }
                    if (point_type[i] == Collision_Point_Type.gear_or_skid_right)
                    {                       
                        if (wheel_rolling_velocity_right_temp > wheel_friction_factor_for_free_rolling) wheel_rolling_velocity_right_temp -= wheel_friction_factor_for_free_rolling;
                        if (wheel_rolling_velocity_right_temp < -wheel_friction_factor_for_free_rolling) wheel_rolling_velocity_right_temp += wheel_friction_factor_for_free_rolling;
                        wheel_rolling_distance_right_temp += wheel_rolling_velocity_right_temp * (float)dtime / point_positionR.Count;
                    }
                    if (point_type[i] == Collision_Point_Type.gear_or_support_steering_center)
                    {
                        if (wheel_rolling_velocity_steering_center_temp > wheel_friction_factor_for_free_rolling) wheel_rolling_velocity_steering_center_temp -= wheel_friction_factor_for_free_rolling;
                        if (wheel_rolling_velocity_steering_center_temp < -wheel_friction_factor_for_free_rolling) wheel_rolling_velocity_steering_center_temp += wheel_friction_factor_for_free_rolling;
                        wheel_rolling_distance_steering_center_temp += wheel_rolling_velocity_steering_center_temp * (float)dtime / point_positionR.Count;
                    }
                    if (point_type[i] == Collision_Point_Type.gear_or_support_steering_left)
                    {
                        if (wheel_rolling_velocity_steering_left_temp > wheel_friction_factor_for_free_rolling) wheel_rolling_velocity_steering_left_temp -= wheel_friction_factor_for_free_rolling;
                        if (wheel_rolling_velocity_steering_left_temp < -wheel_friction_factor_for_free_rolling) wheel_rolling_velocity_steering_left_temp += wheel_friction_factor_for_free_rolling;
                        wheel_rolling_distance_steering_left_temp += wheel_rolling_velocity_steering_left_temp * (float)dtime / point_positionR.Count;
                    }
                    if (point_type[i] == Collision_Point_Type.gear_or_support_steering_right)
                    {
                        if (wheel_rolling_velocity_steering_right_temp > wheel_friction_factor_for_free_rolling) wheel_rolling_velocity_steering_right_temp -= wheel_friction_factor_for_free_rolling;
                        if (wheel_rolling_velocity_steering_right_temp < -wheel_friction_factor_for_free_rolling) wheel_rolling_velocity_steering_right_temp += wheel_friction_factor_for_free_rolling;
                        wheel_rolling_distance_steering_right_temp += wheel_rolling_velocity_steering_right_temp * (float)dtime / point_positionR.Count;
                    }


                    if (last_takestep_in_frame)
                    {
                        if (i < ODEDebug.contact_positionR.Count)
                        {
                            ODEDebug.contact_positionR[i] = Vector3.zero;
                            ODEDebug.contact_forceR1[i] = Vector3.zero;
                            ODEDebug.contact_forceR2[i] = Vector3.zero;
                        }
                    }
                }

            }

            // this values are read by the main-thread at unpredictable times. Because force_y_gear_or_skid_leftLH,... are set to zero,
            // and then summed up in the last for loop, reading the unfinished sum leads to jittering in the force. This three lines avoid this problem:
            force_y_gear_or_skid_leftLH = force_y_gear_or_skid_leftLH_temp; // [N] summ of forces in local coordinate system
            force_y_gear_or_skid_rightLH = force_y_gear_or_skid_rightLH_temp; // [N] summ of forces in local coordinate system
            force_y_gear_or_support_steering_centerLH = force_y_gear_or_support_steering_centerLH_temp; // [N] summ of forces in local coordinate system
            force_y_gear_or_support_steering_leftLH = force_y_gear_or_support_steering_leftLH_temp; // [N] summ of forces in local coordinate system
            force_y_gear_or_support_steering_rightLH = force_y_gear_or_support_steering_rightLH_temp; // [N] summ of forces in local coordinate system

            //const bool steering_left_gameobject_is_mirror_of_steering_right = true; // TODO put into parameter list, also does not work well for the left and right steering wheel: wheel steers in wrong direction

            wheel_rolling_distance_left = wheel_rolling_distance_left_temp; // [m]
            wheel_rolling_distance_right = wheel_rolling_distance_right_temp; // [m]
            wheel_rolling_distance_steering_center = wheel_rolling_distance_steering_center_temp; // [m]
            wheel_rolling_distance_steering_left = wheel_rolling_distance_steering_left_temp; // [m]
            wheel_rolling_distance_steering_right = wheel_rolling_distance_steering_right_temp; // [m]

            wheel_steering_center = wheel_steering_center_temp; // [0...1] for one rotation
            //if (!steering_left_gameobject_is_mirror_of_steering_right)
            //    wheel_steering_left = (wheel_steering_left_temp); // [0...1] for one rotation
            //else
            //    wheel_steering_left = (0.5f - wheel_steering_left_temp) % 1.0f; // [0...1] for one rotation
            wheel_steering_left = (0.5f - wheel_steering_left_temp) % 1.0f; // [0...1] for one rotation
            wheel_steering_right = wheel_steering_right_temp; // [0...1] for one rotation


            // steer the wheel to initial position and overwite all other steering values
            if (wheel_steering_center_lock_to_initial_direction == true)
            {
                wheel_steering_center = 0.25f;
                wheel_steering_center_temp = 0.25f;
            }
            if (wheel_steering_left_lock_to_initial_direction == true)
            {
                wheel_steering_left = 0.25f;
                wheel_steering_left_temp = 0.25f;
            }
            if (wheel_steering_right_lock_to_initial_direction == true)
            {
                wheel_steering_right = 0.25f;
                wheel_steering_right_temp = 0.25f;
            }


            // steer the wheel to initial position after a while if no contact force
            if (Mathf.Abs(force_y_gear_or_support_steering_centerLH) < 0.1f)
            {
                if (wheel_steering_center_steer_to_center_if_no_contact_delay > 1)
                {
                    wheel_steering_center_steer_to_center_if_no_contact_delay -= 1;
                }
                else
                {
                    wheel_steering_center = 0.25f;
                    wheel_steering_center_temp = 0.25f;
                }
            }
            else
            {
                wheel_steering_center_steer_to_center_if_no_contact_delay = 2000;
            }





            // prepare factors for ground-effect calculation
            ground_effect_mainrotor_hub_distance_to_ground = contact_informations[(point_positionR.Count - 2)].penetration; // [m] distance to ground
            ground_effect_tailrotor_hub_distance_to_ground = contact_informations[(point_positionR.Count - 1)].penetration; // [m] distance to ground
            ground_effect_propeller_hub_distance_to_ground = 1000; // [m] distance to ground
            ground_effect_mainrotor_triangle_normalR = contact_informations[(point_positionR.Count - 2)].normalR;
            ground_effect_tailrotor_triangle_normalR = contact_informations[(point_positionR.Count - 1)].normalR;
            ground_effect_propeller_triangle_normalR = Vector3.up;

            // ##################################################################################







            // ##################################################################################
            // gravity
            // ##################################################################################
            force_gravityO = -par.transmitter_and_helicopter.helicopter.mass_total.val * par.scenery.gravity.val; // [N]
            forcesO.y += (float)force_gravityO; // [N]
            // ##################################################################################





            // ##################################################################################
            // wind
            // ##################################################################################
            Wind_Model(out velo_wind_LH, out velo_wind_O);
            // ##################################################################################




            // ##################################################################################
            // drag on fuselage
            // ##################################################################################
            Fuselage_Model(last_takestep_in_frame, out force_fuselageLH, out force_fuselageO);
            // ##################################################################################




            // ##################################################################################
            // horizontal fin force and moments (normal towards local y-direction)
            // ##################################################################################
            if (par.transmitter_and_helicopter.helicopter.horizontal_fin.wing_exists.val)
            {
                Wing_Model(par.transmitter_and_helicopter.helicopter.horizontal_fin,
                        out Vector3 wing_forceO,
                        out Vector3 wing_torque_wrp_cgLH,
                        out Vector3 debug_wing_positionO,  // left handed system
                        out Vector3 debug_wing_forceO);  // left handed system

                forcesO += wing_forceO; // [N]
                torquesLH += wing_torque_wrp_cgLH; // [Nm]

                if (last_takestep_in_frame)
                {
                    ODEDebug.force_on_horizontal_fin_positionO = debug_wing_positionO;
                    ODEDebug.force_on_horizontal_fin_forceO = debug_wing_forceO;
                }
            }
            // ##################################################################################



            // ##################################################################################
            // Vertical fin force and moment (normal towards local z-direction)
            // ##################################################################################
            if (par.transmitter_and_helicopter.helicopter.vertical_fin.wing_exists.val)
            {
                Wing_Model(par.transmitter_and_helicopter.helicopter.vertical_fin,
                        out Vector3 wing_forceO,
                        out Vector3 wing_torque_wrp_cgLH,
                        out Vector3 debug_wing_positionO,  // left handed system
                        out Vector3 debug_wing_forceO);  // left handed system

                forcesO += wing_forceO; // [N]
                torquesLH += wing_torque_wrp_cgLH; // [Nm]

                if (last_takestep_in_frame)
                {
                    ODEDebug.force_on_vertical_fin_positionO = debug_wing_positionO;
                    ODEDebug.force_on_vertical_fin_forceO = debug_wing_forceO;
                }
            }
            // ##################################################################################



            // ##################################################################################
            // horizontal wing left -  force and moments (normal towards local y-direction)
            // ##################################################################################
            if (par.transmitter_and_helicopter.helicopter.horizontal_wing_left.wing_exists.val)
            {
                Wing_Model(par.transmitter_and_helicopter.helicopter.horizontal_wing_left,
                            out Vector3 wing_forceO,
                            out Vector3 wing_torque_wrp_cgLH,
                            out Vector3 debug_wing_positionO, // left handed system
                            out Vector3 debug_wing_forceO); // left handed system

                forcesO += wing_forceO; // [N]
                torquesLH += wing_torque_wrp_cgLH; // [Nm]

                if (last_takestep_in_frame)
                {
                    ODEDebug.force_on_horizontal_wing_left_positionO = debug_wing_positionO;
                    ODEDebug.force_on_horizontal_wing_left_forceO = debug_wing_forceO;
                }
            }
            // ##################################################################################



            // ##################################################################################
            // horizontal wing right -  force and moments (normal towards local y-direction)
            // ##################################################################################
            if (par.transmitter_and_helicopter.helicopter.horizontal_wing_right.wing_exists.val)
            {
                Wing_Model(par.transmitter_and_helicopter.helicopter.horizontal_wing_right,
                            out Vector3 wing_forceO,
                            out Vector3 wing_torque_wrp_cgLH,
                            out Vector3 debug_wing_positionO,  // left handed system
                            out Vector3 debug_wing_forceO);  // left handed systemf

                forcesO += wing_forceO; // [N]
                torquesLH += wing_torque_wrp_cgLH; // [Nm]

                if (last_takestep_in_frame)
                {
                    ODEDebug.force_on_horizontal_wing_right_positionO = debug_wing_positionO;
                    ODEDebug.force_on_horizontal_wing_right_forceO = debug_wing_forceO;
                }
            }
            // ##################################################################################












            // ##################################################################################
            // mainrotor force and torque calculation
            // ##################################################################################
            if (Helicopter_Main.mainrotor_simplified0_or_BEMT1 == 0)
            {
                Mainrotor_Thrust_and_Torque_with_Precalculations((float)time, (float)dtime, integrator_function_call_number, force_fuselageLH, out thrust_mr, out torque_mr, out v_i_mr, out dflapping_a_s_mr_LR__int_dt, out dflapping_b_s_mr_LR__int_dt);
            }


            // ##################################################################################
            // rotating unbalance of mainrotor blades during startup
            // ##################################################################################
            float unbalance_turn_off_rpm = par.transmitter_and_helicopter.tuning.startup_rotating_unbalance.turn_off_rpm.val; // [rpm]  turn off effect, if shaft rotational speed rises above this value
            float rotating_unbalance_mass = par.transmitter_and_helicopter.tuning.startup_rotating_unbalance.mass.val; // [kg] unbalance mass (at r=1.0m)

            if (integrator_function_call_number == 0)
            {
                //float rotating_unbalance_mass = (1 + par.transmitter_and_helicopter.helicopter.mass_total.val) / 25.00000000f; // [kg]   25: just some scaling value
                rotating_unbalance = rotating_unbalance_mass * 1.0f * ((float)omega_mr * (float)omega_mr); // [N] rotating unbalance   F_U = m * e * omega² = [kg] * [m] * [rad/sec]² = [N]

                if (Math.Abs(omega_mr) < omega_mr_old) // if velocity is falling, then reduce the effect (unbalance only at rising velocity)
                    rotating_unbalance *= 0.1f;
                omega_mr_old = Math.Abs(omega_mr);
            }
            rotating_unbalance *= Helper.Step(Math.Abs((float)omega_mr), unbalance_turn_off_rpm * 0.9f * Helper.Rpm_to_RadPerSec, 1, unbalance_turn_off_rpm * Helper.Rpm_to_RadPerSec, 0); // [N] turn off effect if speed rises above xxx rpm

            if (Math.Abs((float)omega_mr) < unbalance_turn_off_rpm * Helper.Rpm_to_RadPerSec && Math.Abs((float)omega_mr) > 0.1)
            {
                Vector3 force_unbalance_LR = new Vector3(Mathf.Cos((float)Omega_mr), 0, Mathf.Sin((float)Omega_mr)) * rotating_unbalance; // [N] rotating unbalance direction, expressed in rotor's local coorindate system
                Vector3 force_unbalance_LH = Helper.A_RL_S123(par.transmitter_and_helicopter.helicopter.mainrotor.oriLH.vect3 * Helper.Deg_to_Rad, force_unbalance_LR); // [N] rotating unbalance direction, expressed in helicopters's local coorindate system
                Vector3 force_unbalance_O = Helper.A_RL(q, force_unbalance_LH); // [N] rotating unbalance direction, expressed in inertial coorindate system

                Vector3 torque_unbalance_LH = Helper.Cross(par.transmitter_and_helicopter.helicopter.mainrotor.posLH.vect3, force_unbalance_LH); // [Nm] rotating unbalance torque at helicopter's center of gravity, 

                forcesO += force_unbalance_O; // [N]
                torquesLH += torque_unbalance_LH; // [Nm]
            }
            // ##################################################################################//




            // ##################################################################################
            // tailrotor force and torque calculation
            // ##################################################################################
            // flapping state output is only used in tandem 
            Tailrotor_Thrust_and_Torque_with_Precalculations((float)time, (float)dtime, integrator_function_call_number, force_fuselageLH, out thrust_tr, out torque_tr, out v_i_tr, out dflapping_a_s_tr_LR__int_dt, out dflapping_b_s_tr_LR__int_dt);
            // ##################################################################################




            // ##################################################################################
            // propeller force and torque calculation (e.g. AH56-Cheyenne)
            // ##################################################################################
            if (par.transmitter_and_helicopter.helicopter.propeller.rotor_exists.val)
                Propeller_Thrust_and_Torque_with_Precalculations((float)time, (float)dtime, integrator_function_call_number, force_fuselageLH, out thrust_pr, out torque_pr, out v_i_pr);
            // ##################################################################################




            // ##################################################################################
            // booster force (e.g. Airwolf)
            // ##################################################################################
            if (par.transmitter_and_helicopter.helicopter.booster.booster_exists.val)
                Booster_Thrust();
                //Booster_Thrust((float)time, (float)dtime, out thrust_bo, out torque_bo);
            // ##################################################################################




            // ##################################################################################
            // mainrotor-disc BEMT  
            // ##################################################################################
            if (Helicopter_Main.mainrotor_simplified0_or_BEMT1 == 1)
            {
                Helicopter_Rotor_Physics.Rotor_Disc_BEMT_Calculations(false,
                    ref x_states, (stru_rotor)par.transmitter_and_helicopter.helicopter.mainrotor, ref beta, velo_wind_O,
                    ref T_stiffLR_CH, ref T_stiffLR_LD, ref T_dampLR_CH, ref T_dampLR_LD,
                    ref A_OLDnorot,
                    ref r_LBO_O, ref dr_LBO_O_dt, ref dr_LBO_LB_dt,
                    ref F_LB_O_thrust, ref F_LB_O_torque,
                    ref F_thrustsumLD_O, ref F_torquesumLD_LD, ref F_thrustsumLD_LD, ref Vi_LD, ref Vi_LD_smoothdamp, ref Vi_LD_smoothdamp_diff, ref Vi_LD_smoothdamp_velocity, ref Vi_mean, (float)dtime,
                    ground_effect_mainrotor_hub_distance_to_ground,
                    ground_effect_mainrotor_triangle_normalR,
                    out sound_volume_mainrotor,
                    out sound_volume_mainrotor_stall,
                    out ODEDebug.mainrotor_forceLH, // left handed system
                    out ODEDebug.mainrotor_torqueLH, // left handed system
                    out ODEDebug.mainrotor_positionO, // left handed system
                    out ODEDebug.mainrotor_forceO, // left handed system
                    out ODEDebug.mainrotor_torqueO, // left handed system
                    out ODEDebug.turbulence_mr,
                    out ODEDebug.vortex_ring_state_mr,
                    out ODEDebug.ground_effect_mr
                    );

                //F_thrustsumLD_O = Vector3.zero;
                //F_torquesumLD_LD = Vector3.zero;

                thrust_mr = F_thrustsumLD_O.magnitude;
                torque_mr = -F_torquesumLD_LD.y; // [Nm]

                forcesO += F_thrustsumLD_O; // [N]
                torquesLH += Helper.A_RL_S123(par.transmitter_and_helicopter.helicopter.mainrotor.oriLH.vect3 * Helper.Deg_to_Rad, new Vector3(0, -F_torquesumLD_LD.y, 0)); // [Nm]
                

                v_i_mr = Vi_mean;

                
                thrust_mr_for_rotordisc_conical_deformation = (float)F_thrustsumLD_LD.y; // [N] 

                if (last_takestep_in_frame)
                {
                    int r_n = 4;  // radial steps - (polar coordiantes)
                    int c_n = 10; // circumferencial steps - (polar coordiantes) - number of virtual blades
                    for (int r = 0; r < r_n; r++)
                    {
                        for (int c = 0; c < c_n; c++)
                        {
                            ODEDebug.BEMT_blade_segment_position[r][c] = Helper.ConvertRightHandedToLeftHandedVector(r_LBO_O[r, c]);
                            ODEDebug.BEMT_blade_segment_velocity[r][c] = Helper.ConvertRightHandedToLeftHandedVector(dr_LBO_O_dt[r, c]);
                            ODEDebug.BEMT_blade_segment_thrust[r][c] = Helper.ConvertRightHandedToLeftHandedVector(F_LB_O_thrust[r, c]);
                            ODEDebug.BEMT_blade_segment_torque[r][c] = Helper.ConvertRightHandedToLeftHandedVector(F_LB_O_torque[r, c]);
                        }
                    }
                    ODEDebug.P_mr_pr = (float)(torque_mr * omega_mr); // [W]
                }
            }
            else
            {
                T_stiffLR_CH = Vector3.zero;
                T_dampLR_CH = Vector3.zero;

                if (last_takestep_in_frame)
                {
                    int r_n = 4;  // radial steps - (polar coordiantes)
                    int c_n = 10; // circumferencial steps - (polar coordiantes) - number of virtual blades
                    for (int r = 0; r < r_n; r++)
                    {
                        for (int c = 0; c < c_n; c++)
                        {
                            ODEDebug.BEMT_blade_segment_position[r][c] = Vector3.zero;
                            ODEDebug.BEMT_blade_segment_velocity[r][c] = Vector3.zero;
                            ODEDebug.BEMT_blade_segment_thrust[r][c] = Vector3.zero;
                            ODEDebug.BEMT_blade_segment_torque[r][c] = Vector3.zero;
                        }
                    }
                }
            }
            // ##################################################################################




            // ##################################################################################
            // brushless motor with speed controller (governor)
            // ##################################################################################
            double domega_mo_dt, dOmega_mo_dt, domega_mr_dt, dOmega_mr_dt, dDELTA_omega_mo___int_dt;

            //float torque_mo = (float)torque_mr / (par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val) + Math.Abs((float)torque_tr / (par.transmitter_and_helicopter.helicopter.transmission.n_mo2tr.val));

            // set target speed
            float omega_mo_target = (par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor_rotation_direction.val ? -1.0f : 1.0f) *
                                     par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val *
                                    (par.transmitter_and_helicopter.helicopter.governor.target_rpm.vect3[flight_bank] / (60.0f / (2.0f * Mathf.PI))); // [rad/sec] desired speed   rpm/(60/(2*pi))-> rad/sec

            // motor soft start speed (if motor is restarted inside a short time period, it accelerates faster)
            float start_speed_factor;
            if (flag_motor_start_slow_or_fast == flag_motor_start_speed.slow)
                start_speed_factor = par.transmitter_and_helicopter.helicopter.governor.soft_start_factor.val;
            else
                start_speed_factor = par.transmitter_and_helicopter.helicopter.governor.engine_restart_factor.val;
            
            // motor soft start
            if (flag_motor_enabled)
            {
                if (omega_mo_target > 0)
                {
                    if (omega_mo_target_with_soft_start <= omega_mo_target)
                    {
                        if (integrator_function_call_number == 0)
                            omega_mo_target_with_soft_start += omega_mo_target * (float)dtime * start_speed_factor;
                    }
                    else
                        omega_mo_target_with_soft_start = omega_mo_target;
                }
                else
                {
                    if (omega_mo_target_with_soft_start >= omega_mo_target)
                    {
                        if (integrator_function_call_number == 0)
                            omega_mo_target_with_soft_start += omega_mo_target * (float)dtime * start_speed_factor;
                    }
                    else
                        omega_mo_target_with_soft_start = omega_mo_target;
                }
            }
            else
                omega_mo_target_with_soft_start = 0;


            Drivetrain_With_Governor(omega_mo_target_with_soft_start, out domega_mo_dt, out dOmega_mo_dt, out domega_mr_dt, out dOmega_mr_dt, out dDELTA_omega_mo___int_dt);
            // ##################################################################################

            



            // ##################################################################################
            // rigid body dynamics as 13 first order ordinary differential equations + other states
            // ##################################################################################
            dxdt[0] = dxdt_R; // after integration: [m]
            dxdt[1] = dydt_R; // after integration: [m]
            dxdt[2] = dzdt_R; // after integration: [m]
            dxdt[3] = (-q1 * wx_LH - q2 * wy_LH - q3 * wz_LH) / 2.0; // w https://pdfs.semanticscholar.org/8031/8e902df9ae42dd59ee5c9ebaf210920a7f11.pdf (Page 29, Eq. 4.38)
            dxdt[4] = (+q0 * wx_LH + q2 * wz_LH - q3 * wy_LH) / 2.0; // x
            dxdt[5] = (+q0 * wy_LH - q1 * wz_LH + q3 * wx_LH) / 2.0; // y
            dxdt[6] = (+q0 * wz_LH + q1 * wy_LH - q2 * wx_LH) / 2.0; // z
            dxdt[7] = forcesO.x / par.transmitter_and_helicopter.helicopter.mass_total.val;
            dxdt[8] = forcesO.y / par.transmitter_and_helicopter.helicopter.mass_total.val;
            dxdt[9] = forcesO.z / par.transmitter_and_helicopter.helicopter.mass_total.val;
            //dxdt[10] = (torque_frictionLH_sum.x + torque_contactLH_sum.x + torquesLH.x + wy_LH * wz_LH * (par.transmitter_and_helicopter.helicopter.Jxx_Jyy_Jzz.vect3.y - par.transmitter_and_helicopter.helicopter.Jxx_Jyy_Jzz.vect3.z)) / par.transmitter_and_helicopter.helicopter.Jxx_Jyy_Jzz.vect3.x;
            //dxdt[11] = (torque_frictionLH_sum.y + torque_contactLH_sum.y + torquesLH.y - wx_LH * wz_LH * (par.transmitter_and_helicopter.helicopter.Jxx_Jyy_Jzz.vect3.x - par.transmitter_and_helicopter.helicopter.Jxx_Jyy_Jzz.vect3.z)) / par.transmitter_and_helicopter.helicopter.Jxx_Jyy_Jzz.vect3.y;
            //dxdt[12] = (torque_frictionLH_sum.z + torque_contactLH_sum.z + torquesLH.z + wx_LH * wy_LH * (par.transmitter_and_helicopter.helicopter.Jxx_Jyy_Jzz.vect3.x - par.transmitter_and_helicopter.helicopter.Jxx_Jyy_Jzz.vect3.y)) / par.transmitter_and_helicopter.helicopter.Jxx_Jyy_Jzz.vect3.z;

            double MLx = torque_frictionLH_sum.x + torque_contactLH_sum.x + torquesLH.x + T_stiffLR_CH.x + T_dampLR_CH.x;
            double MLy = torque_frictionLH_sum.y + torque_contactLH_sum.y + torquesLH.y + T_stiffLR_CH.y + T_dampLR_CH.y;
            double MLz = torque_frictionLH_sum.z + torque_contactLH_sum.z + torquesLH.z + T_stiffLR_CH.z + T_dampLR_CH.z;

            double Jx = par.transmitter_and_helicopter.helicopter.Jxx_Jyy_Jzz.vect3.x;
            double Jy = par.transmitter_and_helicopter.helicopter.Jxx_Jyy_Jzz.vect3.y;
            double Jz = par.transmitter_and_helicopter.helicopter.Jxx_Jyy_Jzz.vect3.z;
            double Jxy = par.transmitter_and_helicopter.helicopter.Jxy_Jxz_Jyz.vect3.x;
            double Jxz = par.transmitter_and_helicopter.helicopter.Jxy_Jxz_Jyz.vect3.y;
            double Jyz = par.transmitter_and_helicopter.helicopter.Jxy_Jxz_Jyz.vect3.z;
            double Jyx = Jxy;
            double Jzx = Jxz;
            double Jzy = Jyz;

            double wLx = wx_LH;
            double wLy = wy_LH;
            double wLz = wz_LH;

            dxdt[10] = -((Jxz * Jy + Jxy * Jyz) * (MLz + wLx * (Jx * wLy + Jxy * wLx) - wLy * (Jxy * wLy + Jy * wLx) - wLz * (Jxz * wLy - Jyz * wLx))) / (Jxy * Jyx * Jz - Jx * Jy * Jz + Jxz * Jy * Jzx + Jx * Jyz * Jzy + Jxy * Jyz * Jzx + Jxz * Jyx * Jzy) - ((Jxy * Jz + Jxz * Jzy) * (MLy - wLx * (Jx * wLz + Jxz * wLx) + wLy * (Jxy * wLz - Jyz * wLx) + wLz * (Jxz * wLz + Jz * wLx))) / (Jxy * Jyx * Jz - Jx * Jy * Jz + Jxz * Jy * Jzx + Jx * Jyz * Jzy + Jxy * Jyz * Jzx + Jxz * Jyx * Jzy) - ((Jy * Jz - Jyz * Jzy) * (MLx - wLx * (Jxy * wLz - Jxz * wLy) + wLy * (Jy * wLz + Jyz * wLy) - wLz * (Jyz * wLz + Jz * wLy))) / (Jxy * Jyx * Jz - Jx * Jy * Jz + Jxz * Jy * Jzx + Jx * Jyz * Jzy + Jxy * Jyz * Jzx + Jxz * Jyx * Jzy);
            dxdt[11] = -((Jx * Jyz + Jxz * Jyx) * (MLz + wLx * (Jx * wLy + Jxy * wLx) - wLy * (Jxy * wLy + Jy * wLx) - wLz * (Jxz * wLy - Jyz * wLx))) / (Jxy * Jyx * Jz - Jx * Jy * Jz + Jxz * Jy * Jzx + Jx * Jyz * Jzy + Jxy * Jyz * Jzx + Jxz * Jyx * Jzy) - ((Jx * Jz - Jxz * Jzx) * (MLy - wLx * (Jx * wLz + Jxz * wLx) + wLy * (Jxy * wLz - Jyz * wLx) + wLz * (Jxz * wLz + Jz * wLx))) / (Jxy * Jyx * Jz - Jx * Jy * Jz + Jxz * Jy * Jzx + Jx * Jyz * Jzy + Jxy * Jyz * Jzx + Jxz * Jyx * Jzy) - ((Jyx * Jz + Jyz * Jzx) * (MLx - wLx * (Jxy * wLz - Jxz * wLy) + wLy * (Jy * wLz + Jyz * wLy) - wLz * (Jyz * wLz + Jz * wLy))) / (Jxy * Jyx * Jz - Jx * Jy * Jz + Jxz * Jy * Jzx + Jx * Jyz * Jzy + Jxy * Jyz * Jzx + Jxz * Jyx * Jzy);
            dxdt[12] = -((Jx * Jy - Jxy * Jyx) * (MLz + wLx * (Jx * wLy + Jxy * wLx) - wLy * (Jxy * wLy + Jy * wLx) - wLz * (Jxz * wLy - Jyz * wLx))) / (Jxy * Jyx * Jz - Jx * Jy * Jz + Jxz * Jy * Jzx + Jx * Jyz * Jzy + Jxy * Jyz * Jzx + Jxz * Jyx * Jzy) - ((Jx * Jzy + Jxy * Jzx) * (MLy - wLx * (Jx * wLz + Jxz * wLx) + wLy * (Jxy * wLz - Jyz * wLx) + wLz * (Jxz * wLz + Jz * wLx))) / (Jxy * Jyx * Jz - Jx * Jy * Jz + Jxz * Jy * Jzx + Jx * Jyz * Jzy + Jxy * Jyz * Jzx + Jxz * Jyx * Jzy) - ((Jy * Jzx + Jyx * Jzy) * (MLx - wLx * (Jxy * wLz - Jxz * wLy) + wLy * (Jy * wLz + Jyz * wLy) - wLz * (Jyz * wLz + Jz * wLy))) / (Jxy * Jyx * Jz - Jx * Jy * Jz + Jxz * Jy * Jzx + Jx * Jyz * Jzy + Jxy * Jyz * Jzx + Jxz * Jyx * Jzy);
           
            // mainrotor flapping
            dxdt[13] = dflapping_a_s_mr_LR__int_dt;   // [rad/sec] mainrotor pitch flapping velocity a_s (longitudial direction)
            dxdt[14] = dflapping_b_s_mr_LR__int_dt;   // [rad/sec] mainrotor roll flapping velocity b_s (lateral direction)

            // tailrotor flapping (tandem rotor helicopter)
            dxdt[15] = dflapping_a_s_tr_LR__int_dt;   // [rad/sec] tailrotor pitch flapping velocity a_s (longitudial direction)
            dxdt[16] = dflapping_b_s_tr_LR__int_dt;   // [rad/sec] tailrotor roll flapping velocity b_s (lateral direction)

            // drivetrain
            dxdt[17] = domega_mo_dt; // [rad/sec^2] brushless motor rotational acceleration   
            dxdt[18] = dOmega_mo_dt; // [rad/sec] brushless motor rotational speed       
            dxdt[19] = domega_mr_dt; // [rad/sec^2] mainrotor motor rotational acceleration   
            dxdt[20] = dOmega_mr_dt; // [rad/sec] mainrotor motor rotational speed       

            // governor
            dxdt[21] = dDELTA_omega_mo___int_dt; // [rad] governor PI-controller's integrator 

            // controller
            dxdt[22] = dDELTA_x_roll__int_dt;    // [rad/sec] flybarless error value integral
            dxdt[23] = dDELTA_y_yaw__int_dt;     // [rad/sec] gyro error value integral
            dxdt[24] = dDELTA_z_pitch__int_dt;   // [rad/sec] flybarless error value integral

            // servo damping
            dxdt[25] = dservo_col_mr_damped_dt;  // [-1...1] damping of mainrotor collective movement - Collective
            dxdt[26] = dservo_lat_mr_damped_dt;  // [-1...1] damping of mainrotor lateral movement - Roll
            dxdt[27] = dservo_lon_mr_damped_dt;  // [-1...1] damping of mainrotor longitudial movement - Pitch
            dxdt[28] = dservo_col_tr_damped_dt;  // [-1...1] damping of tailrotor collective movement - Yaw
            dxdt[29] = dservo_lat_tr_damped_dt;  // [-1...1] damping of tailrotor lateral movement
            dxdt[30] = dservo_lon_tr_damped_dt;  // [-1...1] damping of tailrotor longitudial movement

            // rotor disc
            if (Helicopter_Main.mainrotor_simplified0_or_BEMT1 == 0) 
            {
                dxdt[31] = 0; // dwdt
                dxdt[32] = 0; // dxdt
                dxdt[33] = 0; // dydt
                dxdt[34] = 0; // dzdt
                dxdt[35] = 0; // 
                dxdt[36] = 0; // 
            }
            else
            { 
                wLx = wx_DO_LD;
                wLy = wy_DO_LD;
                wLz = wz_DO_LD;
            
                dxdt[31] = (-q1_DO * wx_DO_LD - q2_DO * wy_DO_LD - q3_DO * wz_DO_LD) / 2.0; // w https://pdfs.semanticscholar.org/8031/8e902df9ae42dd59ee5c9ebaf210920a7f11.pdf (Page 29, Eq. 4.38)
                dxdt[32] = (+q0_DO * wx_DO_LD + q2_DO * wz_DO_LD - q3_DO * wy_DO_LD) / 2.0; // x
                dxdt[33] = (+q0_DO * wy_DO_LD - q1_DO * wz_DO_LD + q3_DO * wx_DO_LD) / 2.0; // y
                dxdt[34] = (+q0_DO * wz_DO_LD + q1_DO * wy_DO_LD - q2_DO * wx_DO_LD) / 2.0; // z
                Jx = par.transmitter_and_helicopter.helicopter.flapping.I_flapping.val; // TODO times number of blades??
                Jy = par.transmitter_and_helicopter.helicopter.mainrotor.J.val;
                Jz = par.transmitter_and_helicopter.helicopter.flapping.I_flapping.val; // TODO times number of blades??


                MLx = T_stiffLR_LD.x + T_dampLR_LD.x + F_torquesumLD_LD.x;
                //MLy = T_stiffLR_LD.y + T_dampLR_LD.y;
                MLz = T_stiffLR_LD.z + T_dampLR_LD.z + F_torquesumLD_LD.z;
    //MLx = 0;
    //MLz = 0;
                dxdt[35] = (MLx + wLy * wLz * (Jy - Jz)) / Jx;
                //dxdt[36] = (MLy - wLx * wLz * (Jx - Jz)) / Jy;
                //dxdt[36] = domega_mr_dt; // [rad/sec^2] mainrotor motor rotational acceleration   
                dxdt[36] = (MLz + wLx * wLy * (Jx - Jy)) / Jz;
            }

            if (last_takestep_in_frame)
            {
                ODEDebug.veloLH = veloLH;
            }
            // ##################################################################################




            //// ##################################################################################
            //// if not a number, then reset
            //// ##################################################################################
            //for (int i = 0; i < dxdt.Length; i++)
            // {
            //     if (double.IsNaN(dxdt[i]))
            //         Set_Initial_Conditions();
            // }
            // // ##################################################################################





            // ##################################################################################
            if (last_takestep_in_frame)        
            {                
                ODEDebug.debug_text = "";
                ODEDebug.debug_text += " INFO:       veloLHx=" + Helper.FormatNumber(veloLH.x, "0.00") + "m/s" + "   veloLHy=" + Helper.FormatNumber(veloLH.y, "0.00") + "m/s" + "   veloLHz=" + Helper.FormatNumber(veloLH.z, "0.00") + "m/s" +
                                                    "   omegaLHx=" + Helper.FormatNumber(omegaLH.x * Mathf.Rad2Deg, "0.0") + "deg/s" + "   omegaLHy=" + Helper.FormatNumber(omegaLH.y * Mathf.Rad2Deg, "0.0") + "deg/s" + "   omegaLHz = " + Helper.FormatNumber(omegaLH.z * Mathf.Rad2Deg, "0.0") + "deg/s" + "\n";
                ODEDebug.debug_text += " WIND:       vx=" + Helper.FormatNumber(velo_wind_LH.x, "0.00") + "m/sec   vy= " + Helper.FormatNumber(velo_wind_LH.y, "0.00") + "m/sec   vz=" + Helper.FormatNumber(velo_wind_LH.z, "0.0") + "m/sec" + "                  delta_lat_mr = " + Helper.FormatNumber(delta_lat_mr, "0.0000") + "" + "    delta_lon_mr = " + Helper.FormatNumber(delta_lon_mr, "0.0000") + "" + "       x_states[22] = " + Helper.FormatNumber(x_states[22], "0.0000") + "" + "    x_states[24] = " + Helper.FormatNumber(x_states[24], "0.0000") + "" +"\n"; 
                ODEDebug.debug_text += " INPUT:      u0=" + Helper.FormatNumber(u_inputs[0], "0.000") + "   u1=" + Helper.FormatNumber(u_inputs[1], "0.000") + "   u2 = " + Helper.FormatNumber(u_inputs[2], "0.000") + "   u3= " + Helper.FormatNumber(u_inputs[3], "0.000") + "   u4= " + Helper.FormatNumber(u_inputs[4], "0.000") + "   u5= " + Helper.FormatNumber(u_inputs[5], "0.000") + "   u6= " + Helper.FormatNumber(u_inputs[6], "0.000") + "   u7= " + Helper.FormatNumber(u_inputs[7], "0.000") + "    Wheel Brake= " + Helper.FormatNumber(wheel_brake_strength * 100, "0") + " %" + "\n";
                ODEDebug.debug_text += " BLDC:       omega_mo = " + Helper.FormatNumber(omega_mo * Helper.RadPerSec_to_Rpm, "####.0") + "rpm   V_mo= " + Helper.FormatNumber(V_mo, "0.00") + "V   I_mo=" + Helper.FormatNumber(I_mo, "0.0") + "A   P_mo_el=" + Helper.FormatNumber(P_mo_el, "0") + "W   P_mo_mech=" + Helper.FormatNumber(P_mo_mech, "0") + "W   eta_mo= " + Helper.FormatNumber(eta_mo, "0.00") + "   " + (flag_motor_enabled ? "ON" : "OFF") + "\n";
                ODEDebug.debug_text += " POWER:      P_mr_pr=" + Helper.FormatNumber(ODEDebug.P_mr_pr, "####.0") + "W   P_mr_i=" + Helper.FormatNumber(ODEDebug.P_mr_i, "####.0") + "W   P_mr_pa=" + Helper.FormatNumber(ODEDebug.P_mr_pa, "####.0") + "W   P_mr_c= " + Helper.FormatNumber(ODEDebug.P_mr_c, "####.0") + "W" + "\n";
                ODEDebug.debug_text += " POWER:      P_tr_pr=" + Helper.FormatNumber(ODEDebug.P_tr_pr, "####.0") + "W   P_tr_i=" + Helper.FormatNumber(ODEDebug.P_tr_i, "####.0") + "W   P_tr_pa=" + Helper.FormatNumber(ODEDebug.P_tr_pa, "####.0") + "W   P_tr_c= " + Helper.FormatNumber(ODEDebug.P_tr_c, "####.0") + "W" + "\n";
                ODEDebug.debug_text += " POWER:      P_pr_pr=" + Helper.FormatNumber(ODEDebug.P_pr_pr, "####.0") + "W   P_pr_i=" + Helper.FormatNumber(ODEDebug.P_pr_i, "####.0") + "W   P_pr_pa=" + Helper.FormatNumber(ODEDebug.P_pr_pa, "####.0") + "W   P_pr_c= " + Helper.FormatNumber(ODEDebug.P_pr_c, "####.0") + "W" + "\n";
                ODEDebug.debug_text += " DRAG:       force_fuselageO.x=" + Helper.FormatNumber(force_fuselageO.x, "0.0") + "N" + "   force_fuselageO.y=" + Helper.FormatNumber(force_fuselageO.y, "0.0") + "N" + "   force_fuselageO.z=" + Helper.FormatNumber(force_fuselageO.z, "0.0") + "N" + "\n";
                ODEDebug.debug_text += " MAINROTOR:  omega_mr=" + Helper.FormatNumber(omega_mr * Helper.RadPerSec_to_Rpm, "####") + "rpm" + "   thrust_mr=" + Helper.FormatNumber(thrust_mr, "###.00") + "N" + "   torque_mr=" + Helper.FormatNumber(torque_mr, "###.00") + "Nm" + "   Theta_col_mr=" + Helper.FormatNumber(Theta_col_mr * Helper.Rad_to_Deg, "##.00") + "deg   v_i_mr=" + Helper.FormatNumber(v_i_mr, "##.00") + "m/s" + "     tau_mr=" + Helper.FormatNumber(debug_tau_mr, "0.000") + "s" + "     flag_freewheeling=" + (flag_freewheeling ? "YES" : "NO") + "\n";
                ODEDebug.debug_text += " TAILROTOR:  omega_tr=" + Helper.FormatNumber(omega_tr * Helper.RadPerSec_to_Rpm, "####") + "rpm" + "   thrust_tr=" + Helper.FormatNumber(thrust_tr, "###.00") + "N" + "   torque_tr=" + Helper.FormatNumber(torque_tr, "###.00") + "Nm" + "   Theta_col_tr=" + Helper.FormatNumber(Theta_col_tr * Helper.Rad_to_Deg, "##.00") + "deg" + "   delta_col_tr=" + Helper.FormatNumber(delta_col_tr, "##.00") + "   v_i_tr=" + Helper.FormatNumber(v_i_tr, "##.00") + "m/s" + "\n";
                ODEDebug.debug_text += " PROPELLER:  omega_pr=" + Helper.FormatNumber(omega_pr * Helper.RadPerSec_to_Rpm, "####") + "rpm" + "   thrust_pr=" + Helper.FormatNumber(thrust_pr, "###.00") + "N" + "   torque_pr=" + Helper.FormatNumber(torque_pr, "###.00") + "Nm" + "   delta_col_pr=" + Helper.FormatNumber(delta_col_pr, "##.00") + "   v_i_pr=" + Helper.FormatNumber(v_i_pr, "##.00") + "m/s" + "\n";
                ODEDebug.debug_text += " FLAPPING:   flapping_a_s_L= " + Helper.FormatNumber(flapping_a_s_mr_LR * Mathf.Rad2Deg, "0.00") + "deg     flapping_b_s_L=" + Helper.FormatNumber(flapping_b_s_mr_LR * Mathf.Rad2Deg, "0.00") + "deg" + "\n";
                ODEDebug.debug_text += " TORQUES:    torquesLH.x = " + Helper.FormatNumber(torquesLH.x, "0.000") + "Nm     torquesLH.y=" + Helper.FormatNumber(torquesLH.y, "0.000") + "Nm     torquesLH.z=" + Helper.FormatNumber(torquesLH.z, "0.000") + "Nm   " + "\n";
                ODEDebug.debug_text += " GOVERNOR:   Lipo = " + Helper.FormatNumber(par.transmitter_and_helicopter.helicopter.accumulator.voltage.val, "0.00") + "V    V_s_out = " + Helper.FormatNumber(V_s_out, "0.00") + "Volt " +
                                        "   omega_mr=" + Helper.FormatNumber(omega_mr * Helper.RadPerSec_to_Rpm, "####.0") + "rpm" +
                                        "(" + Helper.FormatNumber(((1 / par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val) * omega_mo_target_with_soft_start * (60 / (2 * Mathf.PI))), "0") + "rpm)" +
                                        " DELTA_omega_mo___int=" + Helper.FormatNumber(DELTA_omega_mo___int * Mathf.Rad2Deg, "####.0") + "deg" + "\n";
                ODEDebug.debug_text += " Omega_mo: " + Helper.FormatNumber(Omega_mo * Helper.Rad_to_Deg / 360, "####") + "rev" +
                                        " Omega_mr: " + Helper.FormatNumber(Omega_mr * Helper.Rad_to_Deg / 360, "####") + "rev" +
                                        " Omega_mg: " + Helper.FormatNumber(Omega_mo / par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val * Helper.Rad_to_Deg / 360, "####") + "rev" +
                                        " DOmega_Freewheel: " + Helper.FormatNumber((Omega_mo / par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val - Omega_mr) * (par.transmitter_and_helicopter.helicopter.transmission.invert_mainrotor_rotation_direction.val ? -1.0f : 1.0f) * Helper.Rad_to_Deg / 360, "0.00") + "rev"
                    + "\n";
                
                /*
                ODEDebug.debug_text = "";
                ODEDebug.debug_text += " INFO:       veloLHx=" + Helper.FormatNumber(veloLH.x, "0.00") + "m/s" + "   veloLHy=" + Helper.FormatNumber(veloLH.y, "0.00") + "m/s" + "   veloLHz=" + Helper.FormatNumber(veloLH.z, "0.00") + "m/s" +
                                                    "   omegaLHx=" + Helper.FormatNumber(omegaLH.x * Mathf.Rad2Deg, "0.0") + "deg/s" + "   omegaLHy=" + Helper.FormatNumber(omegaLH.y * Mathf.Rad2Deg, "0.0") + "deg/s" + "   omegaLHz = " + Helper.FormatNumber(omegaLH.z * Mathf.Rad2Deg, "0.0") + "deg/s" + "\n";
                ODEDebug.debug_text += " WIND:       vx=" + Helper.FormatNumber(velo_wind_LH.x, "0.00") + "m/sec   vy= " + Helper.FormatNumber(velo_wind_LH.y, "0.00") + "m/sec   vz=" + Helper.FormatNumber(velo_wind_LH.z, "0.0") + "m/sec" + "                  delta_lat_mr = " + Helper.FormatNumber(delta_lat_mr, "0.00") + "" + "    delta_lon_mr = " + Helper.FormatNumber(delta_lon_mr, "0.00") + "" + "\n";
                ODEDebug.debug_text += " INPUT:      u0=" + Helper.FormatNumber(u_inputs[0], "0.000") + "   u1=" + Helper.FormatNumber(u_inputs[1], "0.000") + "   u2 = " + Helper.FormatNumber(u_inputs[2], "0.000") + "   u3= " + Helper.FormatNumber(u_inputs[3], "0.000") + "   u4= " + Helper.FormatNumber(u_inputs[4], "0.000") + "   u5= " + Helper.FormatNumber(u_inputs[5], "0.000") + "   u6= " + Helper.FormatNumber(u_inputs[6], "0.000") + "   u7= " + Helper.FormatNumber(u_inputs[7], "0.000") + "    Wheel Brake= " + Helper.FormatNumber(wheel_brake_strength * 100, "0") + " %" + "\n";
                ODEDebug.debug_text += " BLDC:       omega_mo = " + Helper.FormatNumber(omega_mo * Helper.RadPerSec_to_Rpm, "####.0") + "rpm   V_mo= " + Helper.FormatNumber(V_mo, "0.00") + "V   I_mo=" + Helper.FormatNumber(I_mo, "0.0") + "A   P_mo_el=" + Helper.FormatNumber(P_mo_el, "0") + "W   P_mo_mech=" + Helper.FormatNumber(P_mo_mech, "0") + "W   eta_mo= " + Helper.FormatNumber(eta_mo, "0.00") + "   " + (flag_motor_enabled ? "ON" : "OFF") + "\n";
                ODEDebug.debug_text += " turbulence: turbulence = " + Helper.FormatNumber(ODEDebug.turbulence, "####.00") + "\n";
                ODEDebug.debug_text += " VRS:        vortex_ring_state = " + Helper.FormatNumber(ODEDebug.vortex_ring_state, "####.00") + "\n"
                + "\n";*/


                //ODEDebug.debug_text += " beta_forward:" + Helper.FormatNumber(beta_forward, "0.000") + "\n";
                //ODEDebug.debug_text += " beta_sideward:" + Helper.FormatNumber(beta_sideward, "0.000") + "\n";

               // log_output_frequency = 0;
            }
            // ##################################################################################




            // ##################################################################################
            // thread safe setting of parameter between threads (if a new helicopter oder new parameter sets are loaded in the main thread)
            // ##################################################################################
            if (last_takestep_in_frame == true && flag_load_new_parameter_in_ODE_thread == true)
            {
                //UnityEngine.Debug.Log("flag_load_new_parameter_in_ODE_thread");

                //par_temp.simulation.get_stru_simulation_settings_from_player_prefs();
                par_temp.transmitter_and_helicopter.Update_Calculated_Parameter();

                // copy new values from par_temp-object to par-object
                par = par_temp.Deep_Clone();

                par.transmitter_and_helicopter.Update_Calculated_Parameter();

                Update_ODE_Debug_Variables();

                flag_load_new_parameter_in_ODE_thread = false;
            }
            // ##################################################################################    


        }


        /*
        // ##################################################################################
        // disc has to be integrated with higher frequency 
        // ##################################################################################
        public override void ODE_DISC(
                  int integrator_function_call_number,        // [IN]  
                  ref double[] x_states,                          // [IN] states
                  double[] u_inputs,                          // [IN] inputs
                  double[] dxdt,                              // [OUT] derivatives
                  double time,                                // [IN] time 
                  double dtime)                               // [IN] timestep    
        {

             q0_DO = x_states[31]; // [-] w quaternion orientation real - rotor disc
             q1_DO = x_states[32]; // [-] x quaternion orientation imag i - rotor disc
             q2_DO = x_states[33]; // [-] y quaternion orientation imag j - rotor disc
             q3_DO = x_states[34]; // [-] z quaternion orientation imag k - rotor disc
             wx_DO_LD = x_states[35]; // [rad/sec] local rotational velocity vector x   around longitudial x-axis - rotor disc
             wy_DO_LD = omega_mr; //omega_mr; //     x_states[36]; // [rad/sec] local rotational velocity vector y   around vertical y-axis - rotor disc    ////// TODO reomve it. not a extra state --> use omega_mr
             wz_DO_LD = x_states[36]; // [rad/sec] local rotational velocity vector z   around lateral z-axis - rotor disc

             double wLx = wx_DO_LD;
             double wLy = wy_DO_LD;
             double wLz = wz_DO_LD;

             dxdt[31] = (-q1_DO* wx_DO_LD - q2_DO* wy_DO_LD - q3_DO* wz_DO_LD) / 2.0; // w https://pdfs.semanticscholar.org/8031/8e902df9ae42dd59ee5c9ebaf210920a7f11.pdf (Page 29, Eq. 4.38)
             dxdt[32] = (+q0_DO* wx_DO_LD + q2_DO* wz_DO_LD - q3_DO* wy_DO_LD) / 2.0; // x
             dxdt[33] = (+q0_DO* wy_DO_LD - q1_DO* wz_DO_LD + q3_DO* wx_DO_LD) / 2.0; // y
             dxdt[34] = (+q0_DO* wz_DO_LD + q1_DO* wy_DO_LD - q2_DO* wx_DO_LD) / 2.0; // z
             double Jx = par.transmitter_and_helicopter.helicopter.flapping.I_flapping.val; // TODO times number of blades??
             double Jy = par.transmitter_and_helicopter.helicopter.mainrotor.J.val;
             double Jz = par.transmitter_and_helicopter.helicopter.flapping.I_flapping.val; // TODO times number of blades??

            
             Helicopter_Rotor_Physics.Rotor_Disc_BEMT_Calculations(true, ref x_states, (stru_rotor)par.transmitter_and_helicopter.helicopter.mainrotor, ref beta,
                 ref T_stiffLR_CH, ref T_stiffLR_LD, ref T_dampLR_CH, ref T_dampLR_LD,
                 ref A_OLDnorot,
                 ref r_LBO_O, ref dr_LBO_O_dt, ref dr_LBO_LB_dt, 
                 ref F_LB_O_thrust, ref F_LB_O_torque,
                 ref F_thrustsumLD_O, ref F_torquesumLD_LD);
            
             //forcesO += F_thrustsumLD_O/4; // [N]

             //int r_n = 4;  // radial steps - (polar coordiantes)
             //int c_n = 10; // circumferencial steps - (polar coordiantes) - number of virtual blades
             //for (int r = 0; r < r_n; r++)
             //{
             //    for (int c = 0; c < c_n; c++)
             //    {
             //        ODEDebug.BEMT_blade_segment_position[r][c] = Helper.ConvertRightHandedToLeftHandedVector(r_LBO_O[r, c]);
             //        ODEDebug.BEMT_blade_segment_velocity[r][c] = Helper.ConvertRightHandedToLeftHandedVector(dr_LBO_O_dt[r, c]);
             //        ODEDebug.BEMT_blade_segment_thrust[r][c] = Helper.ConvertRightHandedToLeftHandedVector(dr_LBO_O_dt[r, c]);
             //        ODEDebug.BEMT_blade_segment_torque[r][c] = Helper.ConvertRightHandedToLeftHandedVector(dr_LBO_O_dt[r, c]);
             //    }
             //}


             //wy_DO_LD = 0;
             double MLx = T_stiffLR_LD.x + T_dampLR_LD.x + F_torquesumLD_LD.x;
             //MLy = T_stiffLR_LD.y + T_dampLR_LD.y;
             double MLz = T_stiffLR_LD.z + T_dampLR_LD.z + F_torquesumLD_LD.y;

             dxdt[35] = (MLx + wLy* wLz * (Jy - Jz)) / Jx;
             //dxdt[36] = (MLy - wLx * wLz * (Jx - Jz)) / Jy; 
             dxdt[36] = (MLz + wLx* wLy * (Jx - Jy)) / Jz;

        }
        */
        #endregion
    }


}
