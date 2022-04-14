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
// Unity Logfiles
// https://docs.unity3d.com/Manual/LogFiles.html
// 
// https://www.reddit.com/r/Unity3D/comments/9vmhf5/anyone_getting_invalid_langversion_in_visual/
// ##################################################################################
//  Collsion does not work in build -> check http://docs.unity3d.com/Manual/LogFiles.html
//
//
//
// https://github.com/ValveSoftware/unity-xr-plugin/releases/tag/installer
// ##################################################################################
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine.Profiling;
using UnityEngine.UI;
using Common;
using UnityEngine.Rendering.PostProcessing;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;
using System.Threading;
using Rotor;
using Parameter;

using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem;

// ##################################################################################
//    MMMMMMMM               MMMMMMMM                    iiii                   
//    M:::::::M             M:::::::M                   i::::i                  
//    M::::::::M           M::::::::M                    iiii                   
//    M:::::::::M         M:::::::::M                                           
//    M::::::::::M       M::::::::::M  aaaaaaaaaaaaa   iiiiiiinnnn  nnnnnnnn    
//    M:::::::::::M     M:::::::::::M  a::::::::::::a  i:::::in:::nn::::::::nn  
//    M:::::::M::::M   M::::M:::::::M  aaaaaaaaa:::::a  i::::in::::::::::::::nn 
//    M::::::M M::::M M::::M M::::::M           a::::a  i::::inn:::::::::::::::n
//    M::::::M  M::::M::::M  M::::::M    aaaaaaa:::::a  i::::i  n:::::nnnn:::::n
//    M::::::M   M:::::::M   M::::::M  aa::::::::::::a  i::::i  n::::n    n::::n
//    M::::::M    M:::::M    M::::::M a::::aaaa::::::a  i::::i  n::::n    n::::n
//    M::::::M     MMMMM     M::::::Ma::::a    a:::::a  i::::i  n::::n    n::::n
//    M::::::M               M::::::Ma::::a    a:::::a i::::::i n::::n    n::::n
//    M::::::M               M::::::Ma:::::aaaa::::::a i::::::i n::::n    n::::n
//    M::::::M               M::::::M a::::::::::aa:::ai::::::i n::::n    n::::n
//    MMMMMMMM               MMMMMMMM  aaaaaaaaaa  aaaaiiiiiiii nnnnnn    nnnnnn
// ##################################################################################
public partial class Helicopter_Main : Helicopter_TimestepModel
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
    // variables, objects
    // ##################################################################################


    // ##################################################################################
    // XR
    // ##################################################################################
    //const bool virtual_reality_used_flag = true;
    // sub_camera (for collsion object texturing) orientation, see https://forum.unity.com/threads/sync-multiple-cameras-with-arfoundation.718844/
    // for black screen,rer see https://github.com/ValveSoftware/steamvr_unity_plugin/issues/713
    // single pass ( in Assets -> XR -> Settings ) would be more efficiet , but shder must be edited https://docs.unity3d.com/Manual/SinglePassStereoRendering.html TODO
    // ##################################################################################



    [Header("Simulation Objects")]
    public GameObject helicopters_available;
    public UI_Informations_Overlay ui_informations_overlay;
    private bool ui_informations_overlay_visibility_state = false;

    // main object where all the equations are calculated
    public Helisimulator.Helicopter_ODE helicopter_ODE;
    double time = 0.0; // [sec] time
    readonly double[] u_inputs = new double[8]; // transmitter/controller input signal vector; // input for system of ordenary equations
    readonly Stopwatch stopwatch = new Stopwatch();
    int counter = 0;
    int error_counter = 0;
    float msec_per_thread_call_filtered=0;

    // animation: pilot is rising his arms with transmitter
    Animator animator_pilot_with_transmitter;

    // animation: rising and lowering wheels
    Animator animator_wheels_left;
    Animator animator_wheels_right;
    Animator animator_wheels_steering_center;
    Animator animator_wheels_steering_left;
    Animator animator_wheels_steering_right;
    enum Wheels_Status_Variants
    {
        lowered,
        raised
    }
    Wheels_Status_Variants wheels_status;
    Wheels_Status_Variants wheels_status_old; // for detect flank
    private float collision_positions_landing_gear_left_rising_offset_target = 0; // [0...1]
    private float collision_positions_landing_gear_right_rising_offset_target = 0; // [0...1]
    private float collision_positions_landing_gear_steering_center_rising_offset_target = 0; // [0...1]
    private float collision_positions_landing_gear_steering_left_rising_offset_target = 0; // [0...1]
    private float collision_positions_landing_gear_steering_right_rising_offset_target = 0; // [0...1]
    private float collision_positions_landing_gear_left_rising_offset_velocity = 0.0f;  // [0...1/s]
    private float collision_positions_landing_gear_right_rising_offset_velocity = 0.0f;  // [0...1/s]
    private float collision_positions_landing_gear_steering_center_rising_offset_velocity = 0.0f;  // [0...1/s]
    private float collision_positions_landing_gear_steering_left_rising_offset_velocity = 0.0f;  // [0...1/s]
    private float collision_positions_landing_gear_steering_right_rising_offset_velocity = 0.0f;  // [0...1/s]

    // animation: pilot sitting in scale helicopter
    Animator animator_pilot;

    // animation: opening and closing doors
    Animator animator_doors;
    enum Doors_Status_Variants
    {
        closed,
        opened
    }
    Doors_Status_Variants doors_status;
    Doors_Status_Variants doors_status_old; // for detect flank
    Doors_Status_Variants doors_status_002;
    Doors_Status_Variants doors_status_002_old; // for detect flank

    enum Wheel_Brake_Status_Variants
    {
        enabled,
        disabled
    }
    Wheel_Brake_Status_Variants wheel_brake_status = Wheel_Brake_Status_Variants.disabled;


    // animation: air brake / speed brake (e.g. Sikorsky S-67)
    Animator animator_speed_brake;
    enum Animator_Speed_Brake_Status_Variants
    {
        closed,
        opened
    }
    Animator_Speed_Brake_Status_Variants speed_brake_status = Animator_Speed_Brake_Status_Variants.closed;

    // special animation
    Animator animator_special_animation;

    Vector3 position = Vector3.zero;
    Quaternion rotation = Quaternion.identity;
    Vector3 velocity = Vector3.zero;

    string scenery_name = "none";
    string helicopter_name = "none";

    enum State_Load_Skymap
    {
        not_running,
        prepare_starting,
        starting,
        running,
        finising
    }
    State_Load_Skymap load_skymap_state = State_Load_Skymap.not_running;

    string version_number;
    int first_start_flag = 1;
    //int loading_flag = 0;
    int active_helicopter_id = 0;
    int active_scenery_id = 0;
    GameObject helicopter_object;

    readonly Helicopter_Rotor_Visualization_And_Audio mainrotor_object = new Helicopter_Rotor_Visualization_And_Audio();
    readonly Helicopter_Rotor_Visualization_And_Audio tailrotor_object = new Helicopter_Rotor_Visualization_And_Audio();
    readonly Helicopter_Rotor_Visualization_And_Audio propeller_object = new Helicopter_Rotor_Visualization_And_Audio();

    GameObject helicopter_setup_instance;
    GameObject helicopter_setup_gameobject;

    GameObject helicopter_setup_doors_animation_instance;
    GameObject helicopter_setup_doors_animation_gameobject;
    
    GameObject helicopter_setup_gear_or_skids_left_instance;
    GameObject helicopter_setup_gear_or_skids_left_gameobject;
    GameObject helicopter_setup_gear_or_skids_right_instance;
    GameObject helicopter_setup_gear_or_skids_right_gameobject;
    GameObject helicopter_setup_gear_or_skids_steering_center_instance;
    GameObject helicopter_setup_gear_or_skids_steering_center_gameobject;

    GameObject helicopter_setup_special_animation_instance;
    GameObject helicopter_setup_special_animation_gameobject;

    //GameObject helicopter_setup_missile_instance;
    GameObject helicopter_setup_missile_gameobject;
    readonly List<Transform> list_helicopter_setup_missile_pylon_localposition = new List<Transform>();
    int list_helicopter_setup_missile_pylon_localposition_current_active;

    GameObject helicopter_vortex_ring_state_particles_gameobject;

    float current_mo = 0; // [A]
    float omega_mo = 0;  // [rad/sec] brushless motor rotational speed
    float omega_mr = 0;  // [rad/sec] mainrotor 
    float omega_tr = 0;  // [rad/sec] tailrotor
    float omega_pr = 0;  // [rad/sec] propeller

    float Omega_mo_ = 0;  // [rad] brushless motor rotational angle (calcualted in Helicopter_Main only for visualization purposes)
    float Omega_mr = 0;  // [rad] mainrotor 
    float Omega_tr = 0;  // [rad] tailrotor
    float Omega_pr = 0;  // [rad] propeller

    float flapping_a_s_mr_L = 0; // [rad] mainrotor pitch flapping angle a_s (longitudial direction)
    float flapping_b_s_mr_L = 0; // [rad] mainrotor roll flapping angle b_s (lateral direction)

    float flapping_a_s_tr_L = 0; // [rad] tailrotor pitch flapping angle a_s (longitudial direction) -  used for tandem rotor setup
    float flapping_b_s_tr_L = 0; // [rad] tailrotor roll flapping angle b_s (lateral direction) -  used for tandem rotor setup

    float flapping_a_s_pr_L = 0; // [rad] propeller pitch flapping angle a_s (longitudial direction) -  not used
    float flapping_b_s_pr_L = 0; // [rad] propeller roll flapping angle b_s (lateral direction) -  not used

    readonly List<Material> helicopter_canopy_material = new List<Material>(); int helicopter_canopy_material_ID = 0;
    AudioSource ambient_audio_source;
    AudioSource transmitter_audio_source;
    AudioSource commentator_audio_source;
    AudioSource crash_audio_source;
    AudioSource booster_audio_source; // Airwolf
    AudioSource booster_audio_source_continous; // Airwolf
    AudioSource[] audio_source_servo = new AudioSource[4];
    float[] servo_sigma = new float[4];
    float[] servo_sigma_old = new float[4];
    AudioSource audio_source_motor;
    float audio_source_motor_smooth_transition = 0.5f; // [0...1]

    Texture[] all_textures;

    // Debug 2D plot figures
    private Rect[] plot2D_graph_rect = new Rect[6];

    // game logic flag
    [HideInInspector]
    public bool gl_pause_flag = true;
    bool gl_controller_connected_flag = false;

    const float RIGHT2LEFT_HANDED = -1.0f;

    private Coroutine co_hide_cursor;
    float mouse_fov;
    float mouse_camera_yaw;
    float mouse_camera_pitch;
    const float mouse_fov_limit = 30.0f;
    const float mouse_scroll_fov_increment = 0.015f;
    const float mouse_camera_speed_horizontaly = 0.20f;
    const float mouse_camera_speed_verticaly = 0.20f;

    //private Coroutine co_load_skymap;
    List<stru_skymap_paths> list_skymap_paths = new List<stru_skymap_paths>();

    Light directional_light;

    Camera main_camera;
    float fieldOfView;
    float fieldOfView_min = 5;
    float fieldOfView_max = 60;
    float fieldOfView_velocity; // smoothdamp
    Quaternion main_camera_rotation;
    float main_camera_rotation_offset_during_debuging; // [rad]
    float main_camera_rotation_offset_to_keep_ground_visible; // [rad]
    GameObject camera_offset;
    float xr_camera_vertical_position_offset = 0f; // [m] offset value for xr-camera: at loading of the scenery this offset ensures, that the xr-camera is at the same height, as the panoramic photo was taken
    //Camera sub_camera; // needed for projecting skymap to mesh
    Bloom bloom_layer = null;
    float bloom_layer_intensity_old;

    // graphic settings
    MotionBlur motion_blur_layer = null;
    bool motion_blur_old;

    bool bloom_old;

    int quality_setting_old = 0;
    int resolution_setting_old = 0;

    //int target_frame_rate_old;

    GameObject pilot;

    public ReflectionProbe reflextion_probe;

    // transmitter countdown 
    float transmitter_countdown_minutes_timer = 0;
    int display_seconds_spent_old;
    Texture2D[] transmitter_display_numbers_texture2D;
    Material[] transmitter_display_digits_material;

    //int v_sync_old;
    long update_called_cntr = 0;


    float scenery_selection_key_delay_timer;
    bool scenery_selection_key_delay_flag;
    float helicopter_selection_key_delay_timer;
    bool helicopter_selection_key_delay_flag;
    float animation_selection_key_delay_timer;
    bool animation_selection_key_delay_flag;


    //Helper.Exponential_Moving_Average_Filter exponential_moving_average_filter_for_camera_position_x = new Helper.Exponential_Moving_Average_Filter();
    //Helper.Exponential_Moving_Average_Filter exponential_moving_average_filter_for_camera_position_y = new Helper.Exponential_Moving_Average_Filter();
    //Helper.Exponential_Moving_Average_Filter exponential_moving_average_filter_for_camera_position_z = new Helper.Exponential_Moving_Average_Filter();

    // XR
    // https://forum.unity.com/threads/deprecation-nightmare.812688/
    readonly List<XRDisplaySubsystemDescriptor> displaysDescs = new List<XRDisplaySubsystemDescriptor>();
    readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
    private Coroutine co_StartXR;
    bool xr_mode_flag = false;

    // steering wheel
    readonly Helper.Exponential_Moving_Average_Filter_For_Rotations exponential_moving_average_filter_for_rotations_for_steering_wheel_center = new Helper.Exponential_Moving_Average_Filter_For_Rotations();
    readonly Helper.Exponential_Moving_Average_Filter_For_Rotations exponential_moving_average_filter_for_rotations_for_steering_wheel_left = new Helper.Exponential_Moving_Average_Filter_For_Rotations();
    readonly Helper.Exponential_Moving_Average_Filter_For_Rotations exponential_moving_average_filter_for_rotations_for_steering_wheel_right = new Helper.Exponential_Moving_Average_Filter_For_Rotations();
   
    // wheel deflection
    readonly Helper.Exponential_Moving_Average_Filter_For_Rotations exponential_moving_average_filter_for_wheels_left_deflection = new Helper.Exponential_Moving_Average_Filter_For_Rotations();
    readonly Helper.Exponential_Moving_Average_Filter_For_Rotations exponential_moving_average_filter_for_wheels_right_deflection = new Helper.Exponential_Moving_Average_Filter_For_Rotations();
    readonly Helper.Exponential_Moving_Average_Filter_For_Rotations exponential_moving_average_filter_for_wheels_steering_center_deflection = new Helper.Exponential_Moving_Average_Filter_For_Rotations();
    readonly Helper.Exponential_Moving_Average_Filter_For_Rotations exponential_moving_average_filter_for_wheels_steering_left_deflection = new Helper.Exponential_Moving_Average_Filter_For_Rotations();
    readonly Helper.Exponential_Moving_Average_Filter_For_Rotations exponential_moving_average_filter_for_wheels_steering_right_deflection = new Helper.Exponential_Moving_Average_Filter_For_Rotations();

    // Brushless motor rotation
    GameObject motor_to_rotate;
    GameObject maingear_to_rotate;

    // position lights 
    public enum Position_Lights_Status_Variants
    {
        off,
        blinking,
        on
    }
    public Position_Lights_Status_Variants position_lights_state;

    // mainrotor kinematics
    public int mainrotor_simplified0_or_BEMT1 = 0;
    public Helicopter_Mainrotor_Mechanics.Helicopter_Mainrotor_Mechanics Helicopter_Mainrotor_Mechanics;
    // tailrotor kinematics
    public Helicopter_Tailrotor_Mechanics.Helicopter_Tailrotor_Mechanics Helicopter_Tailrotor_Mechanics;
    // ##################################################################################
    #endregion





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
    // Collsion detection with 3d geometry ------> see Helicopter_CollisionDetection.cs  TODO why not here? maybe script must be on trigger object?
    // ##################################################################################
    //void OnTriggerEnter(Collider other) //Collision collision
    //{
    //    //Debug.Log("Collision!!!");

    //    Reset_Simulation_States();
    //}

    // ##################################################################################
    // XR device check https://docs.unity3d.com/ScriptReference/XR.XRDevice-isPresent.html
    // ##################################################################################
    public static bool XRDeviceIsPresent()
    {
        var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
        foreach (var xrDisplay in xrDisplaySubsystems)
        {
            UnityEngine.Debug.Log("xrDisplay: " + xrDisplay);
            if (xrDisplay.running)
            {
                return true;
            }
        }
        return false;
    }


    // https://forum.unity.com/threads/deprecation-nightmare.812688/
    bool IsActive()
    {
        displaysDescs.Clear();
        SubsystemManager.GetSubsystemDescriptors(displaysDescs);

        // If there are registered display descriptors that is a good indication that VR is most likely "enabled"
        return displaysDescs.Count > 0;
    }

    bool IsVrRunning()
    {
        bool vrIsRunning = false;
        displays.Clear();
        SubsystemManager.GetInstances(displays);
        foreach (var displaySubsystem in displays)
        {
            if (displaySubsystem.running)
            {
                vrIsRunning = true;
                break;
            }
        }

        return vrIsRunning;
    }
    /* 
    // https://forum.unity.com/threads/deprecation-nightmare.812688/
   bool IsVrRunning_()
    {
        var inputs = new List<XRInputSubsystem>();
        SubsystemManager.GetInstances(inputs);
        bool canTest = false;
        bool result = false;
        if (inputs.Count > 0)
        {
            List<InputDevice> devices = new List<InputDevice>();
            foreach (var input in inputs)
            {
                if (input.TryGetInputDevices(devices))
                {
                    foreach (var d in devices)
                    {
                      //  if ((d.characteristics & (InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Left)) == (InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Left))
                    //        leftHandInput = d;
                     //   if ((d.characteristics & (InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Right)) == (InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Right))
                     //       rightHandInput = d;
                        if ((d.characteristics & (InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice)) == (InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice))
                            headSetInput = d;

                        if (d.TryGetFeatureValue(CommonUsages.userPresence, out bool userPresent))
                        {
                            canTest = true;
                            if (userPresent)
                                result = true;
                        }
                    }
                }
            }
        }
    }*/

    // ##################################################################################


    /// <summary>
    /// TODO: lock necceessry, better way ??????
    /// </summary>
    static readonly object _locker = new object();

    // ##################################################################################
    // This function is running in a thread with constant low stepsize (high frquency)
    // Inside of it the explicit ODE solver is called gets the ODE with all its calculations updated
    // The 4th order Runge Kutta needs four calls to the ODE to get the states for next timestep approximated
    // ##################################################################################
    public override void TakeStep(float dt)
    {
        lock (_locker)
        {
            IO_AntiStutter__Get_ODE_Transform_Before_Calculation(dt);

            //dt = helicopter_ODE.par.simulation.delta_t.val;//  thread_ODE_deltat    override with const-value

            //UnityEngine.Debug.Log("TakeStep Called");
            stopwatch.Start();

            if (helicopter_ODE.RK4Step(ref helicopter_ODE.x_states, u_inputs, ref time, dt * helicopter_ODE.par.simulation.physics.timescale.val, 0, 37))
            //if (helicopter_ODE.RK4Step(helicopter_ODE.x_states, u_inputs, ref time, dt * helicopter_ODE.par.simulation.physics.timescale.val, 0, 31 ))
            { 
                // catch NaN
                error_counter++;
                //helicopter_ODE.Set_Initial_Conditions(); 
                time = 0;
                for (int i = 0; i < helicopter_ODE.x_states.Count(); i++)
                    helicopter_ODE.x_states[i] = helicopter_ODE.x_states_old[i];
            }
            else
            {
                // save successfull result, for the case, if the next results are not valid or NaN, then use these instead
                for(int i=0; i<helicopter_ODE.x_states.Count(); i++)
                    helicopter_ODE.x_states_old[i] = helicopter_ODE.x_states[i];
            }

            //int ODE_repeat_factor = 4; // times x
            //double time_temp = time;
            //for (int i = 0; i < ODE_repeat_factor; i++)
            //    helicopter_ODE.RK4Step_DISC(ref helicopter_ODE.x_states, u_inputs, ref time_temp, dt / (float)ODE_repeat_factor * helicopter_ODE.par.simulation.physics.timescale.val, 31, 37);


            stopwatch.Stop();
            counter++;

            IO_AntiStutter__Get_ODE_Transform_After_Calculation();
        }
    }
    // ##################################################################################




    // ##################################################################################
    // find the exact refreshrate
    // ##################################################################################
    public void Find_Exact_Monitor_Refreshrate()
    {
        float refreshRate;

        if (!XRSettings.enabled)
            refreshRate = (float)Screen.currentResolution.refreshRate;
        else
        {
            refreshRate = XRDevice.refreshRate;
            //UnityEngine.Debug.Log("XRDevice.refreshRate " + XRDevice.refreshRate); 
            if (refreshRate == 0) refreshRate = 90; // todo see https://docs.unity3d.com/ScriptReference/XR.XRDevice-refreshRate.html
        }

        if (refresh_rate_sec_found_flag == false)
        {
            const float range_dT = 0.05f; // 5 %
            if (Time.deltaTime < (refreshRate * (1.0f + range_dT)) ||
                Time.deltaTime > (refreshRate * (1.0f - range_dT)))
            {
                // do the filtering
                float refresh_rate_sec_filtered =
                    (float)exponential_moving_average_filter_for_refresh_rate_sec.Calculate(200, (double)Time.deltaTime);

                // clamp the refresh-rate close to the theoretical value
                float refresh_rate_sec_rounded = 1.0f / refreshRate;
                const float range = 0.05f; // 5 %
                if (refresh_rate_sec_filtered > (refresh_rate_sec_rounded * (1.0f + range)) ||
                    refresh_rate_sec_filtered < (refresh_rate_sec_rounded * (1.0f - range)))
                    refresh_rate_sec = refresh_rate_sec_rounded;
                else
                {
                    refresh_rate_sec = refresh_rate_sec_filtered;


                    if (Mathf.Abs(refresh_rate_sec_old - refresh_rate_sec) < 0.0001f)
                    {
                        refresh_rate_sec_found_flag_cntr++;
                        if (refresh_rate_sec_found_flag_cntr > 100) // if 100 times the value hasn't changed, then we found the refresh_rate_sec
                            refresh_rate_sec_found_flag = true;
                    }
                    else
                    {
                        refresh_rate_sec_found_flag_cntr = 0;
                    }
                    refresh_rate_sec_old = refresh_rate_sec;

                }

                refresh_rate_hz = 1.0f / refresh_rate_sec;
            }
        }
    }
    // ##################################################################################



    // ##################################################################################
    // Resets the model to the initial conditions states
    // ##################################################################################
    public void Reset_Simulation_States()
    {
        time = 0; // [sec]

        // reset to inintial state in ordinay differetial quations thread
        helicopter_ODE.Set_Initial_Conditions();

        // anti stutter - extrapolation of last two ODE-thread results
        IO_AntiStutter__Preset_ODE_Thread_Transforms();

        // reset transmitter countdown timer
        transmitter_countdown_minutes_timer = helicopter_ODE.par.transmitter_and_helicopter.transmitter.countdown_minutes.val * 60.0f;
        // play transmitter sound
        Transmitter_Play_Audio(Application.streamingAssetsPath + "/Audio/Futaba_T18SZ_Sounds/Futaba_T18SZ_Click.wav");

        // reset wheel animation to lowered wheels
        Reset_Animation_Wheels(Wheels_Status_Variants.lowered);

        // reset pusher propeller at Chayenne tail or Airwolf's rocket booster
        if (helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.propeller.rotor_exists.val)
            u_inputs[4] = 0;

        if (helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.booster.booster_exists.val)
        {
            u_inputs[5] = 0;

            Transform go_;
            GameObject Helicopter_Selected_ = helicopters_available.transform.Find(helicopter_name).gameObject;
            go_ = Helicopter_Selected_.transform.Find("Booster").gameObject.transform.Find("Afterburner_Left");
            if (go_ != null) go_.gameObject.SetActive(false);
            go_ = Helicopter_Selected_.transform.Find("Booster").gameObject.transform.Find("Afterburner_Right");
            if (go_ != null) go_.gameObject.SetActive(false);

            booster_audio_source.Stop();
            booster_audio_source_continous.Stop();

            transmitter_audio_source.volume = (helicopter_ODE.par.transmitter_and_helicopter.transmitter.countdown_volume.val / 100f) * (helicopter_ODE.par.simulation.audio.master_sound_volume.val / 100f);
        }


        // reset xr-camera height correction value
        if (XRSettings.enabled)
            xr_camera_vertical_position_offset = helicopter_ODE.par.scenery.camera_height.val - main_camera.transform.localPosition.y + 0.0f; // [m]
    }
    // ##################################################################################




    // ##################################################################################
    // Load the setup (textures and and special objects) of the helicopter model
    // ##################################################################################
    void Load_Helicopter_Different_Setups()
    {
        // textures
        string setup_foldername = helicopter_name + "_Setup_00" + helicopter_canopy_material_ID + "/";

        // loop through A...E textures, and update the materials
        int i = 0;
        for (char c = 'A'; c <= 'F'; c++)
        {
            if (helicopter_canopy_material.Count == i)
                break;
            if (helicopter_canopy_material[i] == null)
                break;

            Texture canopy_texture = Resources.Load<Texture>(setup_foldername + "Textures/" + helicopter_name + "_Canopy_00" + helicopter_canopy_material_ID + "_" + c);
            if (canopy_texture != null)
            {
                helicopter_canopy_material[i].SetTexture("_MainTex", canopy_texture); // Diffuse 

                if (c == 'A')
                {
                    // for EC135 mainrotor cover, that uses the same map, as the main hull -- TODO better
                    mainrotor_object.Update_Rotor_Material(ref canopy_texture);

                    // for AH56 Cheyenne tailrotor hub, that uses the same map, as the main hull -- TODO better
                    tailrotor_object.Update_Rotor_Material(ref canopy_texture);

                    // for AH56 Cheyenne propller hub, that uses the same map, as the main hull-- TODO better
                    propeller_object.Update_Rotor_Material(ref canopy_texture);
                }
            }

            Texture canopy_rougness_texture = Resources.Load<Texture>(setup_foldername + "Textures/" + helicopter_name + "_Canopy_Roughness_00" + helicopter_canopy_material_ID + "_" + c);
            if (canopy_rougness_texture != null)
                helicopter_canopy_material[i].SetTexture("_SpecGlossMap", canopy_rougness_texture); // Specular/Roughness

            Texture canopy_metallic_texture = Resources.Load<Texture>(setup_foldername + "Textures/" + helicopter_name + "_Canopy_Metallic_00" + helicopter_canopy_material_ID + "_" + c);
            if (canopy_metallic_texture != null)
                helicopter_canopy_material[i].SetTexture("_MetallicGlossMap", canopy_metallic_texture); // Metallic
            else
            {
                helicopter_canopy_material[i].SetTexture("_MetallicGlossMap", null); // Metallic
                //  helicopter_canopy_material[i].SetFloat("_Metallic", .25f);
            }

            Texture canopy_AO_texture = Resources.Load<Texture>(setup_foldername + "Textures/" + helicopter_name + "_Canopy_AO_00" + helicopter_canopy_material_ID + "_" + c);
            if (canopy_AO_texture != null)
                helicopter_canopy_material[i].SetTexture("_OcclusionMap", canopy_AO_texture); // Specular/Roughness

            i++;
        }

        // load specific heilcopter setup's 3d-objects
        Destroy(helicopter_setup_instance);
        helicopter_setup_gameobject = Resources.Load(setup_foldername + "Prefabs/" + helicopter_name + "_setup_00" + helicopter_canopy_material_ID + " Variant", typeof(GameObject)) as GameObject;
        if (helicopter_setup_gameobject != null)
        {
            helicopter_setup_instance = Instantiate(helicopter_setup_gameobject, helicopters_available.transform.Find(helicopter_name).gameObject.transform);

            // search for max 10 missile launch pylons positioning objects
            list_helicopter_setup_missile_pylon_localposition.Clear();
            for (int j = 0; j < 10; j++)
            {
                Transform tr = helicopter_setup_instance.transform.Find("missile_pylon_00" + j);
                if (tr != null)
                    list_helicopter_setup_missile_pylon_localposition.Add(tr.gameObject.GetComponent<Transform>());
            }
        }

        // load specific heilcopter setup's doors
        GameObject temp = GameObject.Find("Helicopters_Available/" + helicopter_name + "/Animation_Doors/" + helicopter_name + "_Doors_Animation");
        if (temp == null)
        {
            Destroy(helicopter_setup_doors_animation_instance);

            GameObject helicopter_setup_door_animation_gameobject_temp = Resources.Load(setup_foldername + "Prefabs/" + helicopter_name + "_setup_00" + helicopter_canopy_material_ID + "_Doors_Animation Variant", typeof(GameObject)) as GameObject;
            if (helicopter_setup_door_animation_gameobject_temp != null)
            { 
                helicopter_setup_doors_animation_gameobject = helicopter_setup_door_animation_gameobject_temp;

                helicopter_setup_doors_animation_instance = Instantiate(helicopter_setup_doors_animation_gameobject, helicopters_available.transform.Find(helicopter_name).gameObject.transform);

                animator_doors = helicopter_setup_doors_animation_instance.gameObject.GetComponent<Animator>();

                if (doors_status == Doors_Status_Variants.opened)
                {
                    animator_doors.SetTrigger("DoorsStatus");
                    animator_doors.Play("Doors.Animation_Doors|Doors_Opened", 0, 0.0f); 
                }
                else
                {
                    animator_doors.ResetTrigger("DoorsStatus");
                    animator_doors.Play("Doors.Animation_Doors|Doors_Closed", 0, 0.0f);
                }

                if (doors_status_002 == Doors_Status_Variants.opened)
                {
                    animator_doors.SetTrigger("DoorsRearStatus");
                    animator_doors.Play("DoorsRear.Animation_DoorsRear|Doors_Transition_Opening", 1, 1.000000f); 
                }
                else
                {
                    animator_doors.ResetTrigger("DoorsRearStatus");
                    animator_doors.Play("DoorsRear.Animation_DoorsRear|Doors_Closed", 1, 0.0f);
                }
            }

        }

        // load specific heilcopter setup's animated left-landing-gears 
        // setup wheel animation (animator can be owerwritten in Load_Helicopter_Different_Setups()-function, if setup specific gear or skids are available)
        // ..._Gear_Or_Skid_Animation_Left and ..._Gear_Or_Skid_Animation_Right are used for skids and rotating wheels. Other wheels are added or removed in Load_Helicopter(). If they should become setup specific, then add them here (so far was not necessary)
        GameObject helicopter_setup_gear_or_skids_left_gameobject_temp = Resources.Load(setup_foldername + "Prefabs/" + helicopter_name + "_setup_00" + helicopter_canopy_material_ID + "_Gear_Or_Skid_Animation_Left Variant", typeof(GameObject)) as GameObject;
        if (helicopter_setup_gear_or_skids_left_gameobject_temp != null)
        {
            Destroy(helicopter_setup_gear_or_skids_left_instance);
            helicopter_setup_gear_or_skids_left_gameobject = helicopter_setup_gear_or_skids_left_gameobject_temp;

            helicopter_setup_gear_or_skids_left_instance = Instantiate(helicopter_setup_gear_or_skids_left_gameobject, helicopters_available.transform.Find(helicopter_name).gameObject.transform);

            animator_wheels_left = helicopter_setup_gear_or_skids_left_instance.gameObject.GetComponent<Animator>();

            if (System.Array.Exists(animator_wheels_left.parameters, p => p.name == "Wheel_Only_Allowed_Lowered"))
            {
                wheels_status = Wheels_Status_Variants.lowered;
                helicopter_ODE.wheel_status = (Helisimulator.Helicopter_ODE.Wheels_Status_Variants)wheels_status;
                wheels_status_old = Wheels_Status_Variants.lowered;
                collision_positions_landing_gear_right_rising_offset_target = 0;
            }

            if (collision_positions_landing_gear_right_rising_offset_target==0)
                Reset_Animation_Wheels(Wheels_Status_Variants.lowered);
            else
                Reset_Animation_Wheels(Wheels_Status_Variants.raised);
        }

        // load specific heilcopter setup's animated right-landing-gears 
        GameObject helicopter_setup_gear_or_skids_right_gameobject_temp = Resources.Load(setup_foldername + "Prefabs/" + helicopter_name + "_setup_00" + helicopter_canopy_material_ID + "_Gear_Or_Skid_Animation_Right Variant", typeof(GameObject)) as GameObject;
        if (helicopter_setup_gear_or_skids_right_gameobject_temp != null)
        {
            Destroy(helicopter_setup_gear_or_skids_right_instance);
            helicopter_setup_gear_or_skids_right_gameobject = helicopter_setup_gear_or_skids_right_gameobject_temp;

            helicopter_setup_gear_or_skids_right_instance = Instantiate(helicopter_setup_gear_or_skids_right_gameobject, helicopters_available.transform.Find(helicopter_name).gameObject.transform);

            animator_wheels_right = helicopter_setup_gear_or_skids_right_instance.gameObject.GetComponent<Animator>();

            if (System.Array.Exists(animator_wheels_right.parameters, p => p.name == "Wheel_Only_Allowed_Lowered"))
            {
                wheels_status = Wheels_Status_Variants.lowered;
                helicopter_ODE.wheel_status = (Helisimulator.Helicopter_ODE.Wheels_Status_Variants)wheels_status;
                wheels_status_old = Wheels_Status_Variants.lowered;
                collision_positions_landing_gear_right_rising_offset_target = 0;
            }

            if (collision_positions_landing_gear_right_rising_offset_target == 0)
                Reset_Animation_Wheels(Wheels_Status_Variants.lowered);
            else
                Reset_Animation_Wheels(Wheels_Status_Variants.raised);
        }

        // load specific heilcopter setup's animated center-landing-gears 
        GameObject helicopter_setup_gear_or_skids_steering_center_gameobject_temp = Resources.Load(setup_foldername + "Prefabs/" + helicopter_name + "_setup_00" + helicopter_canopy_material_ID + "_Gear_Or_Support_Animation_Steering_Center Variant", typeof(GameObject)) as GameObject;
        if (helicopter_setup_gear_or_skids_steering_center_gameobject_temp != null)
        {
            Destroy(helicopter_setup_gear_or_skids_steering_center_instance);
            helicopter_setup_gear_or_skids_steering_center_gameobject = helicopter_setup_gear_or_skids_steering_center_gameobject_temp;

            helicopter_setup_gear_or_skids_steering_center_instance = Instantiate(helicopter_setup_gear_or_skids_steering_center_gameobject, helicopters_available.transform.Find(helicopter_name).gameObject.transform);

            animator_wheels_steering_center = helicopter_setup_gear_or_skids_steering_center_instance.gameObject.GetComponent<Animator>();

            if (System.Array.Exists(animator_wheels_steering_center.parameters, p => p.name == "Wheel_Only_Allowed_Lowered"))
            {
                wheels_status = Wheels_Status_Variants.lowered;
                helicopter_ODE.wheel_status = (Helisimulator.Helicopter_ODE.Wheels_Status_Variants)wheels_status;
                wheels_status_old = Wheels_Status_Variants.lowered;
                collision_positions_landing_gear_right_rising_offset_target = 0;
            }

            if (collision_positions_landing_gear_right_rising_offset_target == 0)
                Reset_Animation_Wheels(Wheels_Status_Variants.lowered);
            else
                Reset_Animation_Wheels(Wheels_Status_Variants.raised);
        }

        // load specific heilcopter setup's animated special-animation
        GameObject helicopter_setup_special_animation_gameobject_temp = Resources.Load(setup_foldername + "Prefabs/" + helicopter_name + "_setup_00" + helicopter_canopy_material_ID + "_Special_Animation Variant", typeof(GameObject)) as GameObject;
        if (helicopter_setup_special_animation_gameobject_temp != null)
        {
            Destroy(helicopter_setup_special_animation_instance);
            helicopter_setup_special_animation_gameobject = helicopter_setup_special_animation_gameobject_temp;

            helicopter_setup_special_animation_instance = Instantiate(helicopter_setup_special_animation_gameobject, helicopters_available.transform.Find(helicopter_name).gameObject.transform);

            animator_special_animation = helicopter_setup_special_animation_instance.gameObject.GetComponent<Animator>();

            if (doors_status_002 == Doors_Status_Variants.opened) // special animation is also controlled by "doors_status_002"
            {
                animator_special_animation.SetTrigger("Status");
                animator_special_animation.Play("Base Layer.Animation_Special|Opened", -1, 0.0f);
            }
            else
            { 
                animator_special_animation.ResetTrigger("Status");
                animator_special_animation.Play("Base Layer.Animation_Special|Closed", -1, 0.0f);
            }
        }
        else
        {
            Destroy(helicopter_setup_special_animation_instance);
            animator_special_animation = null;
        }

        // load specific helicopters setup's missile
        helicopter_setup_missile_gameobject = Resources.Load(setup_foldername + "Prefabs/" + helicopter_name + "_setup_00" + helicopter_canopy_material_ID + "_Missile Variant", typeof(GameObject)) as GameObject;

        u_inputs[5] = 0;
        if (helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.booster.booster_exists.val)
        {
            Transform go_;
            GameObject Helicopter_Selected_ = helicopters_available.transform.Find(helicopter_name).gameObject;
            go_ = Helicopter_Selected_.transform.Find("Booster").gameObject.transform.Find("Afterburner_Left");
            if (go_ != null) go_.gameObject.SetActive(false);
            go_ = Helicopter_Selected_.transform.Find("Booster").gameObject.transform.Find("Afterburner_Right");
            if (go_ != null) go_.gameObject.SetActive(false);
        }
    }
    // ##################################################################################




    // ##################################################################################
    // Destroy all debug lines gameobjects 
    // ##################################################################################
    // destroy all debug linesr
    public void Destroy_Debug_Line_GameObjects()
    {
        Helper.Destroy_Lines(ui_debug_lines);
    }
    // ##################################################################################




    // ##################################################################################
    // Create new line gameobjects
    // ##################################################################################
    public void Create_Debug_Line_GameObjects()
    {
        // create new one
        //UnityEngine.Debug.Log(" helicopter_ODE.ODEDebug.contact_forceR.Count " + helicopter_ODE.ODEDebug.contact_forceR.Count);
        for (var i = 0; i < helicopter_ODE.ODEDebug.contact_forceR.Count; i++)
        {
            helicopter_ODE.ODEDebug.line_object_contact_forceR[i] = Helper.Create_Line(ui_debug_lines, Color.green);
        }

        helicopter_ODE.ODEDebug.line_object_mainrotor_forceO = Helper.Create_Line(ui_debug_lines, Color.yellow);
        helicopter_ODE.ODEDebug.line_object_mainrotor_torqueO = Helper.Create_Line(ui_debug_lines, Color.blue);
        helicopter_ODE.ODEDebug.line_object_mainrotor_flapping_stiffness_torqueO = Helper.Create_Line(ui_debug_lines, Color.blue);

        helicopter_ODE.ODEDebug.line_object_tailrotor_forceO = Helper.Create_Line(ui_debug_lines, Color.yellow);
        helicopter_ODE.ODEDebug.line_object_tailrotor_torqueO = Helper.Create_Line(ui_debug_lines, Color.blue);
        helicopter_ODE.ODEDebug.line_object_tailrotor_flapping_stiffness_torqueO = Helper.Create_Line(ui_debug_lines, Color.blue);

        helicopter_ODE.ODEDebug.line_object_propeller_forceO = Helper.Create_Line(ui_debug_lines, Color.yellow);
        helicopter_ODE.ODEDebug.line_object_propeller_torqueO = Helper.Create_Line(ui_debug_lines, Color.blue);

        for (var i = 0; i < helicopter_ODE.ODEDebug.line_object_booster_forceO.Count; i++)
        {
            helicopter_ODE.ODEDebug.line_object_booster_forceO[i] = Helper.Create_Line(ui_debug_lines, Color.yellow);
        }

        helicopter_ODE.ODEDebug.line_object_drag_on_fuselage_drag_on_fuselage_forceO = Helper.Create_Line(ui_debug_lines, Color.red);
        helicopter_ODE.ODEDebug.line_object_force_on_horizontal_fin_forceO = Helper.Create_Line(ui_debug_lines, Color.red);
        helicopter_ODE.ODEDebug.line_object_force_on_vertical_fin_forceO = Helper.Create_Line(ui_debug_lines, Color.red);
        helicopter_ODE.ODEDebug.line_object_force_on_horizontal_wing_left_forceO = Helper.Create_Line(ui_debug_lines, Color.red);
        helicopter_ODE.ODEDebug.line_object_force_on_horizontal_wing_right_forceO = Helper.Create_Line(ui_debug_lines, Color.red);


        int r_n = 4;  // radial steps - (polar coordiantes)
        int c_n = 10; // circumferencial steps - (polar coordiantes) - number of virtual blades
        for (int r = 0; r < r_n; r++)
        {
            for (int c = 0; c < c_n; c++)
            {
                helicopter_ODE.ODEDebug.line_object_BEMT_blade_segment_velocity[r][c] = Helper.Create_Line(ui_debug_lines, Color.green);
                helicopter_ODE.ODEDebug.line_object_BEMT_blade_segment_thrust[r][c] = Helper.Create_Line(ui_debug_lines, Color.blue);
                helicopter_ODE.ODEDebug.line_object_BEMT_blade_segment_torque[r][c] = Helper.Create_Line(ui_debug_lines, Color.red);
            }
        }
    }
    // ##################################################################################




    // ##################################################################################
    // In options menu parameter can be saved with unique names, given by user. While user enters text, disable the other key actions.
    // ##################################################################################
    public static bool Is_Text_Input_Field_Focused()
    {
        GameObject obj = EventSystem.current.currentSelectedGameObject;
        return (obj != null && obj.GetComponent<InputField>() != null);
    }
    // ##################################################################################




    // ##################################################################################
    // setup pilot scale to match camera with pilot's head/eyes position
    // ##################################################################################
    void Scale_Pilot_To_Match_Camera_Height()
    {
        var scale = helicopter_ODE.par.scenery.camera_height.val / 1.7000f; // todo not scale but move vertically
        pilot.transform.localScale = new Vector3(scale, scale, scale);
    }
    // ##################################################################################




    // ############################################################################
    // play transmitter countdown and speach audio files  
    // ############################################################################
    private void Transmitter_Play_Audio(string fullpath_transmitter_audio_file)
    {
        transmitter_audio_source.volume = (helicopter_ODE.par.transmitter_and_helicopter.transmitter.countdown_volume.val / 100f) * (helicopter_ODE.par.simulation.audio.master_sound_volume.val / 100f);

        Play_Audio(transmitter_audio_source, fullpath_transmitter_audio_file);
    }
    // ############################################################################




    // ############################################################################
    // play commentator's audio files  https://spik.ai/
    // ############################################################################
    private void Commentator_Play_Audio(string fullpath_commentator_audio_file)
    {
        commentator_audio_source.volume = (helicopter_ODE.par.simulation.audio.commentator_audio_source_volume.val / 100f) * (helicopter_ODE.par.simulation.audio.master_sound_volume.val / 100f);

        Play_Audio(commentator_audio_source, fullpath_commentator_audio_file);
    }
    // ############################################################################




    // ############################################################################
    // play commentator's audio files 
    // ############################################################################
    public void Crash_Play_Audio(string fullpath_audio_file)
    {
        // set crash sound volume at distance, where crash happened. 
        crash_audio_source.volume = (1.0f / (Mathf.Pow(Vector3.Distance(Vector3.zero, helicopters_available.transform.position), 0.7f))) *
            (helicopter_ODE.par.simulation.audio.crash_audio_source_volume.val / 100f) *
            (helicopter_ODE.par.simulation.audio.master_sound_volume.val / 100f);

        Play_Audio(crash_audio_source, fullpath_audio_file);
    }
    // ############################################################################




    // ############################################################################
    // play audio files 
    // ############################################################################
    private void Play_Audio(AudioSource audio_source, string fullpath_transmitter_audio_file)
    {
        if (audio_source.clip != null)
        {
            audio_source.Stop();
            AudioClip clip = audio_source.clip;
            audio_source.clip = null;
            clip.UnloadAudioData();
            DestroyImmediate(clip, false); // This is important to avoid memory leak
        }

        // import audio file
        // https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequestMultimedia.GetAudioClip.html?_ga=2.120261704.1701772512.1582723368-2133810121.1564613509
        //UnityEngine.Debug.Log(fullpath_transmitter_audio_file); 
        if (File.Exists(fullpath_transmitter_audio_file))
        {
            if (Path.GetExtension(fullpath_transmitter_audio_file).Equals(".wav"))
                StartCoroutine(GetAudioClip(audio_source, fullpath_transmitter_audio_file, AudioType.WAV));
            if (Path.GetExtension(fullpath_transmitter_audio_file).Equals(".ogg"))
                StartCoroutine(GetAudioClip(audio_source, fullpath_transmitter_audio_file, AudioType.OGGVORBIS));
        }
    }
    // ############################################################################




    // ############################################################################
    // update transmitter time counter numbers on display
    // ############################################################################
    void Transmitter_Update_Time_Digits_On_Display(float time_seconds)
    {
        string str = TimeSpan.FromSeconds(time_seconds).ToString(@"mm\:ss\:f"); // 00:00:0

        transmitter_display_digits_material[0].SetTexture("_MainTex", transmitter_display_numbers_texture2D[str[0] - '0']); // Digit1 - minutes
        transmitter_display_digits_material[1].SetTexture("_MainTex", transmitter_display_numbers_texture2D[str[1] - '0']); // Digit2 - minutes
        transmitter_display_digits_material[2].SetTexture("_MainTex", transmitter_display_numbers_texture2D[str[3] - '0']); // Digit3 - seconds
        transmitter_display_digits_material[3].SetTexture("_MainTex", transmitter_display_numbers_texture2D[str[4] - '0']); // Digit4 - seconds
        transmitter_display_digits_material[4].SetTexture("_MainTex", transmitter_display_numbers_texture2D[str[6] - '0']); // Digit5 - milliseconds
    }
    // ############################################################################




    // ##################################################################################
    // wheel animation
    // ##################################################################################
    void Reset_Animation_Wheels(Wheels_Status_Variants wheel_status)
    {
        // reset wheel animation to lowered wheels
        if (animator_wheels_left != null && animator_wheels_right != null)
        {
            //wheels_status = Wheels_Status_Variants.lowered;
            //wheels_status_old = Wheels_Status_Variants.lowered;
            wheels_status = wheel_status;
            helicopter_ODE.wheel_status = (Helisimulator.Helicopter_ODE.Wheels_Status_Variants)wheels_status;
            wheels_status_old = wheel_status;

            // non-stearable wheels
            if (System.Array.Exists(animator_wheels_left.parameters, p => p.name == "Wheel_Status") && wheel_status==Wheels_Status_Variants.lowered)
            {
                animator_wheels_left.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                animator_wheels_left.Play("Transition.wheels_lowered", -1, 0.0f);
                collision_positions_landing_gear_left_rising_offset_target = 0; // [0...1]
                helicopter_ODE.collision_positions_landing_gear_left_rising_offset = 0; // [0...1]
            }
            else
            {
                animator_wheels_left.ResetTrigger("Wheel_Status"); // 0 triggers transition down -> raised
                animator_wheels_left.Play("Transition.wheels_rised", -1, 0.0f);
                collision_positions_landing_gear_left_rising_offset_target = 1; // [0...1]
                helicopter_ODE.collision_positions_landing_gear_left_rising_offset = 1; // [0...1]
            }

            if (System.Array.Exists(animator_wheels_right.parameters, p => p.name == "Wheel_Status") && wheel_status == Wheels_Status_Variants.lowered)
            {
                animator_wheels_right.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                animator_wheels_right.Play("Transition.wheels_lowered", -1, 0.0f);
                collision_positions_landing_gear_right_rising_offset_target = 0; // [0...1]
                helicopter_ODE.collision_positions_landing_gear_right_rising_offset = 0; // [0...1]
            }
            else
            {
                animator_wheels_right.ResetTrigger("Wheel_Status"); // 0 triggers transition down -> raised
                animator_wheels_right.Play("Transition.wheels_rised", -1, 0.0f);
                collision_positions_landing_gear_right_rising_offset_target = 1; // [0...1]
                helicopter_ODE.collision_positions_landing_gear_right_rising_offset = 1; // [0...1]
            }

            // steerable wheels
            if (animator_wheels_steering_center != null)
            {
                if (System.Array.Exists(animator_wheels_steering_center.parameters, p => p.name == "Wheel_Status"))
                {
                    if (wheel_status == Wheels_Status_Variants.lowered)
                    {
                        animator_wheels_steering_center.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                        animator_wheels_steering_center.Play("Transition.wheels_lowered", -1, 0.0f);
                        collision_positions_landing_gear_steering_center_rising_offset_target = 0; // [0...1]
                        helicopter_ODE.collision_positions_landing_gear_steering_center_rising_offset = 0; // [0...1]
                        helicopter_ODE.wheel_steering_center_lock_to_initial_direction = false;
                    }
                    else
                    {
                        animator_wheels_steering_center.ResetTrigger("Wheel_Status"); // 0 triggers transition down -> raised
                        animator_wheels_steering_center.Play("Transition.wheels_rised", -1, 0.0f);
                        collision_positions_landing_gear_steering_center_rising_offset_target = 1; // [0...1]
                        helicopter_ODE.collision_positions_landing_gear_steering_center_rising_offset = 1; // [0...1]
                        helicopter_ODE.wheel_steering_center_lock_to_initial_direction = true;
                    }
                }
                else
                {
                    animator_wheels_steering_center.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                    animator_wheels_steering_center.Play("Transition.wheels_lowered", -1, 0.0f);
                    collision_positions_landing_gear_steering_center_rising_offset_target = 0; // [0...1]
                    helicopter_ODE.collision_positions_landing_gear_steering_center_rising_offset = 0; // [0...1]
                    helicopter_ODE.wheel_steering_center_lock_to_initial_direction = false;
                }
            }

            if (animator_wheels_steering_left != null)
            {
                if (System.Array.Exists(animator_wheels_steering_left.parameters, p => p.name == "Wheel_Status")) // wheel is rectractable
                {
                    if (wheel_status == Wheels_Status_Variants.lowered)
                    {
                        animator_wheels_steering_left.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                        animator_wheels_steering_left.Play("Transition.wheels_lowered", -1, 0.0f);
                        collision_positions_landing_gear_steering_left_rising_offset_target = 0; // [0...1]
                        helicopter_ODE.collision_positions_landing_gear_steering_left_rising_offset = 0; // [0...1]
                        helicopter_ODE.wheel_steering_left_lock_to_initial_direction = false;
                    }
                    else
                    {
                        animator_wheels_steering_left.ResetTrigger("Wheel_Status"); // 0 triggers transition down -> raised
                        animator_wheels_steering_left.Play("Transition.wheels_rised", -1, 0.0f);
                        collision_positions_landing_gear_steering_left_rising_offset_target = 1; // [0...1]
                        helicopter_ODE.collision_positions_landing_gear_steering_left_rising_offset = 1; // [0...1]
                        helicopter_ODE.wheel_steering_left_lock_to_initial_direction = true;
                    }
                }
                else
                {
                    animator_wheels_steering_left.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                    animator_wheels_steering_left.Play("Transition.wheels_lowered", -1, 0.0f);
                    collision_positions_landing_gear_steering_left_rising_offset_target = 0; // [0...1]
                    helicopter_ODE.collision_positions_landing_gear_steering_left_rising_offset = 0; // [0...1]
                    helicopter_ODE.wheel_steering_left_lock_to_initial_direction = false;
                }
            }

            if (animator_wheels_steering_right != null)
            {
                if (System.Array.Exists(animator_wheels_steering_right.parameters, p => p.name == "Wheel_Status"))
                {
                    if (wheel_status == Wheels_Status_Variants.lowered)
                    {
                        animator_wheels_steering_right.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                        animator_wheels_steering_right.Play("Transition.wheels_lowered", -1, 0.0f);
                        collision_positions_landing_gear_steering_right_rising_offset_target = 0; // [0...1]
                        helicopter_ODE.collision_positions_landing_gear_steering_right_rising_offset = 0; // [0...1]
                        helicopter_ODE.wheel_steering_right_lock_to_initial_direction = false;
                    }
                    else
                    {
                        animator_wheels_steering_right.ResetTrigger("Wheel_Status"); // 0 triggers transition down -> raised
                        animator_wheels_steering_right.Play("Transition.wheels_rised", -1, 0.0f);
                        collision_positions_landing_gear_steering_right_rising_offset_target = 1; // [0...1]
                        helicopter_ODE.collision_positions_landing_gear_steering_right_rising_offset = 1; // [0...1]
                        helicopter_ODE.wheel_steering_right_lock_to_initial_direction = true;
                    }
                }
                else 
                {
                    animator_wheels_steering_right.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                    animator_wheels_steering_right.Play("Transition.wheels_lowered", -1, 0.0f);
                    collision_positions_landing_gear_steering_right_rising_offset_target = 0; // [0...1]
                    helicopter_ODE.collision_positions_landing_gear_steering_right_rising_offset = 0; // [0...1]
                    helicopter_ODE.wheel_steering_right_lock_to_initial_direction = false;
                }
            }
            collision_positions_landing_gear_left_rising_offset_velocity = 0.0f;  // [0...1/s]
            collision_positions_landing_gear_right_rising_offset_velocity = 0.0f;  // [0...1/s]
            collision_positions_landing_gear_steering_center_rising_offset_velocity = 0.0f;  // [0...1/s]
        }
    }
    // ##################################################################################


    // ##################################################################################
    // limit the vertical position (height) of the camera, even if the headset moves up or down
    // add also inital offset value "xr_camera_vertical_position_offset" to "camera_offset-object" 
    // ##################################################################################
    void Correct_And_Limit_XR_Camera_Vertical_Position()
    {
        // limit the vertical position (height) of the camera, even if the headset moves
        Vector3 main_camera_localposition = main_camera.transform.localPosition;
        Vector3 camera_limit = new Vector3(0, xr_camera_vertical_position_offset, 0); // [m]
        const float camera_position_range_y = 0.10f; // [m]

        if (main_camera_localposition.y > (helicopter_ODE.par.scenery.camera_height.val + camera_position_range_y - xr_camera_vertical_position_offset))
            camera_limit.y = (helicopter_ODE.par.scenery.camera_height.val + camera_position_range_y) - main_camera_localposition.y;
        if (main_camera_localposition.y < (helicopter_ODE.par.scenery.camera_height.val - camera_position_range_y - xr_camera_vertical_position_offset))
            camera_limit.y = (helicopter_ODE.par.scenery.camera_height.val - camera_position_range_y) - main_camera_localposition.y;

        // UnityEngine.Debug.Log("main_camera_position: " + main_camera_position + "   camera_limit: " + camera_limit);
        camera_offset.transform.localPosition = camera_limit;
    }
    // ##################################################################################


    // ##################################################################################
    // menu logic: pause menu
    // ##################################################################################
    void ui_menu_logic_pause()
    {
        if (ui_pause_flag ^= true)
        {
            ui_welcome_panel_flag = false;
            ui_info_panel_flag = false;
            //ui_debug_panel_state = 0;
            ui_exit_panel_flag = false;
            ui_parameter_panel_flag = false;
            ui_helicopter_selection_menu_flag = false;
            ui_scenery_selection_menu_flag = false;
            ui_pie_menu_flag = false;

            gl_pause_flag = true;
        }
        else
        {
            gl_pause_flag = false;
        }
    }
    // ##################################################################################


    // ##################################################################################
    // menu logic: parameter menu
    // ##################################################################################
    void ui_menu_logic_parameter()
    {
        if (ui_parameter_panel_flag ^= true)
        {
            ui_welcome_panel_flag = false;
            ui_info_panel_flag = false;
            //ui_debug_panel_state = 0;
            ui_exit_panel_flag = false;
            ui_pause_flag = false;
            ui_helicopter_selection_menu_flag = false;
            ui_scenery_selection_menu_flag = false;
            ui_pie_menu_flag = false;

            gl_pause_flag = true;
        }
        else
        {
            gl_pause_flag = false;
        }
    }
    // ##################################################################################


    // ##################################################################################
    // menu logic: info menu
    // ##################################################################################
    void ui_menu_logic_info()
    {
        if (ui_info_panel_flag ^= true)
        {
            ui_welcome_panel_flag = false;
            //ui_debug_panel_state = 0;
            ui_exit_panel_flag = false;
            ui_pause_flag = false;
            ui_parameter_panel_flag = false;
            ui_helicopter_selection_menu_flag = false;
            ui_scenery_selection_menu_flag = false;
            ui_pie_menu_flag = false;

            gl_pause_flag = true;
        }
        else
        {
            gl_pause_flag = false;
        }
    }
    // ##################################################################################


    // ##################################################################################
    // menu logic: select helicopter menu
    // ##################################################################################
    void ui_menu_logic_select_helicopter()
    {
        ui_welcome_panel_flag = false;
        //ui_debug_panel_state = 0;
        ui_exit_panel_flag = false;
        ui_pause_flag = false;
        ui_parameter_panel_flag = false;
        ui_helicopter_selection_menu_flag = true; // <==
        ui_scenery_selection_menu_flag = false;
        ui_pie_menu_flag = false;

        helicopter_selection_key_delay_flag = false;

        UI_Update_Helicopter_Or_Scenery_Selection_Panel_UI(Ui_Selection_Type.helicopter, ui_helicopter_selection);

        gl_pause_flag = true;
    }
    // ################################################################################## 


    // ##################################################################################
    // menu logic: select scenery menu
    // ##################################################################################
    void ui_menu_logic_select_scenery()
    {
        ui_welcome_panel_flag = false;
        //ui_debug_panel_state = 0;
        ui_exit_panel_flag = false;
        ui_pause_flag = false;
        ui_parameter_panel_flag = false;
        ui_helicopter_selection_menu_flag = false;
        ui_scenery_selection_menu_flag = true; // <==
        ui_pie_menu_flag = false;

        scenery_selection_key_delay_flag = false;

        UI_Update_Helicopter_Or_Scenery_Selection_Panel_UI(Ui_Selection_Type.scenery, ui_scenery_selection);

        gl_pause_flag = true;
    }
    // ################################################################################## 


    // ##################################################################################
    // menu logic: welcome menu
    // ##################################################################################
    void ui_menu_logic_welcome_menu()
    {
        if (ui_welcome_panel_flag ^= true)
        {
            ui_info_panel_flag = false;
            //ui_debug_panel_state = 0;
            ui_exit_panel_flag = false;
            ui_pause_flag = false;
            ui_parameter_panel_flag = false;
            ui_helicopter_selection_menu_flag = false;
            ui_scenery_selection_menu_flag = false;
            ui_pie_menu_flag = false; 

            gl_pause_flag = true;
        }
        else
        {
            gl_pause_flag = false;
        }
    }
    // ##################################################################################  


    // ##################################################################################
    // menu logic: controller calibration menu
    // ##################################################################################
    void ui_menu_logic_controller_calibration_menu()
    {
        if (calibration_state == State_Calibration.not_running && !ui_parameter_panel_flag && first_start_flag == 0)
        {
            calibration_state = State_Calibration.starting;
            calibration_abortable = true; // if started manually, calibration can be abborted
        }

        ui_pie_menu_flag = false;
    }
    // ##################################################################################  


    // ##################################################################################  
    // menu logic: pie menu
    // ##################################################################################  
    void ui_menu_logic_pie_menu()
    {
        ui_welcome_panel_flag = false;
        ui_info_panel_flag = false;
        ui_exit_panel_flag = false;
        ui_pause_flag = false;
        ui_parameter_panel_flag = false;
        ui_helicopter_selection_menu_flag = false;
        ui_scenery_selection_menu_flag = false;
        ui_pie_menu_flag = true; // <==

        gl_pause_flag = true;
    }
    // ##################################################################################  


#if DEBUG_LOG
    // ##################################################################################
    // debug
    // ##################################################################################
    void Debug_Collect_Time_Ticks(enum_ID type, long value)
    {
        // debug time ticks to file
        if (my_debug_time_array_ID_cntr < range)
        {
            my_debug_time_array_VALUE[my_debug_time_array_ID_cntr] = value; //  stopwatch_debug.Elapsed.Ticks;
            my_debug_time_array_ID[my_debug_time_array_ID_cntr++] = type;
        }
    }

    void Debug_Save_Time_Ticks()
    {
        using (StreamWriter sr = new StreamWriter(Application.dataPath + "/../debug_file.txt")) // https://answers.unity.com/questions/13072/how-do-i-get-the-application-path.html
        {
            for (int i = 0; i < my_debug_time_array_VALUE.Length; i++)
            {
                sr.WriteLine((int)my_debug_time_array_ID[i] + " " + my_debug_time_array_VALUE[i]);
            }
        }
    }
    // ##################################################################################
#endif
    #endregion










    // ##################################################################################
    //                   AAA                                                             kkkkkkkk                            
    //                  A:::A                                                            k::::::k                            
    //                 A:::::A                                                           k::::::k                            
    //                A:::::::A                                                          k::::::k                            
    //               A:::::::::Awwwwwww           wwwww           wwwwwwwaaaaaaaaaaaaa    k:::::k    kkkkkkk eeeeeeeeeeee    
    //              A:::::A:::::Aw:::::w         w:::::w         w:::::w a::::::::::::a   k:::::k   k:::::kee::::::::::::ee  
    //             A:::::A A:::::Aw:::::w       w:::::::w       w:::::w  aaaaaaaaa:::::a  k:::::k  k:::::ke::::::eeeee:::::ee
    //            A:::::A   A:::::Aw:::::w     w:::::::::w     w:::::w            a::::a  k:::::k k:::::ke::::::e     e:::::e
    //           A:::::A     A:::::Aw:::::w   w:::::w:::::w   w:::::w      aaaaaaa:::::a  k::::::k:::::k e:::::::eeeee::::::e
    //          A:::::AAAAAAAAA:::::Aw:::::w w:::::w w:::::w w:::::w     aa::::::::::::a  k:::::::::::k  e:::::::::::::::::e 
    //         A:::::::::::::::::::::Aw:::::w:::::w   w:::::w:::::w     a::::aaaa::::::a  k:::::::::::k  e::::::eeeeeeeeeee  
    //        A:::::AAAAAAAAAAAAA:::::Aw:::::::::w     w:::::::::w     a::::a    a:::::a  k::::::k:::::k e:::::::e           
    //       A:::::A             A:::::Aw:::::::w       w:::::::w      a::::a    a:::::a k::::::k k:::::ke::::::::e          
    //      A:::::A               A:::::Aw:::::w         w:::::w       a:::::aaaa::::::a k::::::k  k:::::ke::::::::eeeeeeee  
    //     A:::::A                 A:::::Aw:::w           w:::w         a::::::::::aa:::ak::::::k   k:::::kee:::::::::::::e  
    //    AAAAAAA                   AAAAAAAwww             www           aaaaaaaaaa  aaaakkkkkkkk    kkkkkkk eeeeeeeeeeeeee                                               
    // ##################################################################################
    //  Here you setup the component you are on right now (the "this" object)
    #region awake
    void Awake()
    {

        // ##################################################################################
        // Init: clear UNITY console  
        // ##################################################################################
#if UNITY_EDITOR
        var assembly = Assembly.GetAssembly(typeof(SceneView));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
#endif
        // ##################################################################################







        // #############################################################################2#####
        // PlayerPrefs
        // ##################################################################################
        //UnitySelectMonitor = PlayerPrefs.GetInt("UnitySelectMonitor", 0);

        version_number = PlayerPrefs.GetString("version_number", "0");  // triggers first_start_flag
        active_helicopter_id = PlayerPrefs.GetInt("active_helicopter_id", 2);
        active_scenery_id = PlayerPrefs.GetInt("active_scenery_id", 4);
        ui_informations_overlay_visibility_state = PlayerPrefs.GetInt("ui_informations_overlay_visibility_state", 0)!=0;
        ui_informations_overlay.transform.gameObject.SetActive(ui_informations_overlay_visibility_state);
        selected_input_device_id = PlayerPrefs.GetInt("CC___selected_input_device_i", 0);

        //ui_dropdown_actual_selected_scenery_xml_filename = PlayerPrefs.GetString("ui_dropdown_actual_selected_scenery_xml_filename", null);
        //ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename = PlayerPrefs.GetString("ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename", null);

        //ui_dropdown_actual_selected_scenery_xml_filename = PlayerPrefs.GetString("SavedSetting____" + scenery_name + "____actual_selected_xml_filename", null);
        //ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename = PlayerPrefs.GetString("SavedSetting____" + helicopter_name + "____actual_selected_xml_filename", null);

        // reset some of the PlayerPrefs after version changes
        if (version_number != Application.version)
        {
            // deleting the key leads later to set to default values
            //PlayerPrefs.DeleteKey("active_helicopter_id");
            //PlayerPrefs.DeleteKey("active_scenery_id");
            PlayerPrefs.DeleteKey("__simulation_" + "delta_t");
            PlayerPrefs.DeleteKey("__simulation_" + "show_fps");
            PlayerPrefs.DeleteKey("__simulation_" + "v_sync");
            PlayerPrefs.DeleteKey("__simulation_" + "xr_zoom_factor");

            //mainrotor_simplified0_or_BEMT1 = PlayerPrefs.GetInt("SavedSetting____" + "Logo600SE_V3" + "____mainrotor_simplified0_or_BEMT1", 1);
        }


        
        // ##################################################################################





        // ##################################################################################
        // declaring the main object of type Helicopter_ODE().
        // ##################################################################################
        helicopter_ODE = new Helisimulator.Helicopter_ODE();

        // call the IO_Initialize() from the partial class file UI_Scipts.cs
        IO_Initialize();

        // call the UI_Initialize() from the partial class file UI_Scipts.cs
        UI_Initialize();

        // call the initilaization of the new Input System
        IO_InputSystem_Initialize();

        // initialize game controller memory
        Init_Controller();

        // get connected controller
        Get_Connected_Controller();

        all_textures = Resources.LoadAll("", typeof(Texture)).Cast<Texture>().ToArray(); // TODO: It is not good to load EVERY texture!!!!
        // ##################################################################################



        // ##################################################################################
        // init filter for find the exact monitor refreshrate
        // ##################################################################################
        exponential_moving_average_filter_for_refresh_rate_sec.Init_Mean_Value(1.0f / Screen.currentResolution.refreshRate);
        // ##################################################################################



        // ##################################################################################
        /// <summary> 
        /// Prepare default parameter
        /// Merge scenery's (optionaly)reduced-parameter set with scenery default-full-paremeter set and copy all merged parameter files into "folder_saved_parameter_for_sceneries" folder 
        /// </summary> 
        // ##################################################################################
        // Save helicopter_ODE.par.transmitter_and_helicopter to c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedHelicopterParametersets\
        IO_Save_Parameter(helicopter_ODE.par.scenery, folder_saved_parameter_for_sceneries, "default_parameter.xml"); // save to folder "folder_saved_parameter_for_transmitter_and_helicopter"
        string fullpath_default_full_scenery_xml_file = System.IO.Path.Combine(folder_saved_parameter_for_sceneries, "default_parameter.xml");

        XmlDocument xmldoc = new XmlDocument();

        Check_Skymaps(ref active_scenery_id, ref list_skymap_paths);
        // UnityEngine.Debug.Log("reduced_skymap_xml_file " + list_skymap_paths.Count());

        foreach (stru_skymap_paths each_skymap in list_skymap_paths)
        {
            //UnityEngine.Debug.Log("each_skymap " + each_skymap.name);

            // get the current skymap's reduced parameterset from  
            string reduced_skymap_xml_file = each_skymap.name + "_parameter_file";
            // get the current skymap's reduced parameterset from 
            string fullpath_reduced_skymap_xml_file_copy = System.IO.Path.Combine(folder_saved_parameter_for_sceneries, each_skymap.name + "_parameter_file_temporary_copy.xml"); // to c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedSceneriesParametersets\
            // the reduced parameterset has to be merged with the full .._default_parameter.xml to get a full xml file
            string fullpath_full_skymap_xml_file = System.IO.Path.Combine(folder_saved_parameter_for_sceneries, each_skymap.name + "_default_parameter.xml"); // to c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedSceneriesParametersets\

            // get the current skymap's reduces parameterset from i.e. ...\Free-RC-Helicopter-Simulator\Assets\StreamingAssets\Skymaps\MFC-Ulm_Neu-Ulm_001\
            // and save a copy to "folder_saved_parameter_for_sceneries" c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedSceneriesParametersets\
            xmldoc.Load(each_skymap.fullpath_skymap_folder + "/" + reduced_skymap_xml_file + ".xml");
            xmldoc.Save(fullpath_reduced_skymap_xml_file_copy);

            // merge xml files, merge individual reduced-parameters of selected scenery over default-full-parameter set   
            Helper.XmlMerging.TryMergeXmlFiles(fullpath_reduced_skymap_xml_file_copy, fullpath_default_full_scenery_xml_file, fullpath_full_skymap_xml_file);

            // set path to be loaded initially
            each_skymap.fullpath_parameter_file_used = fullpath_full_skymap_xml_file;

            // clean up
            File.Delete(fullpath_reduced_skymap_xml_file_copy);
        }
        // clean up
        File.Delete(fullpath_default_full_scenery_xml_file);
        // ##################################################################################



        // ##################################################################################
        // load skybox
        // ##################################################################################
        Load_Skymap(list_skymap_paths, active_scenery_id);
        // ##################################################################################




        // ##################################################################################
        /// <summary> 
        /// Prepare default parameter
        /// Merge helicoper- and transmitters's (optionaly)reduced-parameter set with default-full-paremeter set and copy all merged parameter files into "folder_saved_parameter_for_transmitter_and_helicopter" folder 
        /// </summary> 
        // ##################################################################################
        // save helicopter_ODE.par.transmitter_and_helicopter to c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedHelicopterAndTransmitterParametersets\
        IO_Save_Parameter(helicopter_ODE.par.transmitter_and_helicopter, folder_saved_parameter_for_transmitter_and_helicopter, "default_parameter.xml"); // save to folder "folder_saved_parameter_for_transmitter_and_helicopter"
        string fullpath_default_full_parameter_xml_file = System.IO.Path.Combine(folder_saved_parameter_for_transmitter_and_helicopter, "default_parameter.xml");

        //XmlDocument xmldoc = new XmlDocument();

        // We want to load and prepare several additional pre-setup files for each helicopter. We need the names of additional files. These all are stored (if available) in "text_assets" and are filtered later.
        UnityEngine.Object[] text_assets = Resources.LoadAll("", typeof(TextAsset));

        foreach (Transform each_helicopter in helicopters_available.transform)
        {
            // get the current helicoper- and transmitters's reduces parameterset name from e.g. Asset/Helicopters_Available/Logo600SE_V3/Resources/ 
            string reduced_parameter_xml_file = each_helicopter.name + "_parameter_file"; // from e.g. Asset/Helicopters_Available/Logo600SE_V3/Resources/
            // get the current helicoper- and transmitters's reduces parameterset fullpath from e.g. Asset/Helicopters_Available/Logo600SE_V3/Resources/ 
            string fullpath_reduced_parameter_xml_file_copy = System.IO.Path.Combine(folder_saved_parameter_for_transmitter_and_helicopter, each_helicopter.name + "_parameter_file_temporary_copy.xml"); // to c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedHelicopterAndTransmitterParametersets\
            // the reduced parameterset has to be merged with the full default_parameter.xml to get a full xml file
            string fullpath_full_parameter_xml_file = System.IO.Path.Combine(folder_saved_parameter_for_transmitter_and_helicopter, each_helicopter.name + "_default_parameter.xml"); // to c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedHelicopterAndTransmitterParametersets\

            // get the current helicoper- and transmitters's reduces parameterset from Asset/Helicopters_Available/Logo600SE_V3/Resources/ 
            // and save a copy to "folder_saved_parameter_for_transmitter_and_helicopter" c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedHelicopterAndTransmitterParametersets\
            xmldoc.LoadXml(Resources.Load<TextAsset>(reduced_parameter_xml_file).text);
            xmldoc.Save(fullpath_reduced_parameter_xml_file_copy);

            // merge xml files, merge individual reduced-parameters of selected helicoper and transmitter over default-full-parameter set   
            Helper.XmlMerging.TryMergeXmlFiles(fullpath_reduced_parameter_xml_file_copy, fullpath_default_full_parameter_xml_file, fullpath_full_parameter_xml_file);

            // clean up
            File.Delete(fullpath_reduced_parameter_xml_file_copy);

            // We want to load and prepare several additional pre-setup files for each helicopter. We need the names of additional files. These all are stored (if available) in "text_assets" and are filtered later.
            foreach (var t in text_assets)
            {
                if (t.name.Contains(reduced_parameter_xml_file + "_"))
                {  
                    xmldoc.LoadXml(Resources.Load<TextAsset>(t.name).text);
                    xmldoc.Save(fullpath_reduced_parameter_xml_file_copy);

                    string fullpath_full_parameter_xml_file_additional = System.IO.Path.Combine(folder_saved_parameter_for_transmitter_and_helicopter, t.name.Replace("_parameter_file", "_default_parameter")); // to c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedHelicopterAndTransmitterParametersets\

                    // merge xml files, merge individual reduced-parameters of selected helicoper and transmitter over default-full-parameter set   
                    Helper.XmlMerging.TryMergeXmlFiles(fullpath_reduced_parameter_xml_file_copy, fullpath_full_parameter_xml_file, fullpath_full_parameter_xml_file_additional);

                    // clean up
                    File.Delete(fullpath_reduced_parameter_xml_file_copy);
                }
            }

        }
        // clean up
        //File.Delete(fullpath_default_full_parameter_xml_file);
        // ##################################################################################



        // ##################################################################################
        /// <summary> start simulation thread </summary> 
        // ##################################################################################
        // Simulation_Thread_Start();
        // ##################################################################################



        // ##################################################################################
        /// <summary> get main camera </summary> 
        // ##################################################################################
        main_camera = GameObject.Find("Main Camera").gameObject.GetComponent<Camera>();
        camera_offset = GameObject.Find("Camera Offset");
        //sub_camera = main_camera.transform.Find("Sub Camera").gameObject.GetComponent<Camera>();
        //var test = main_camera.GetComponent<PostProcessVolume>(); // access bloom... http://synersteel.com/blog/2019/1/28/unity3d-bloom-animation-script
        //UnityEngine.XR.InputTracking.disablePositionalTracking = true;
        // ##################################################################################



        // ##################################################################################
        /// <summary> disable post_processing_layer if in vr mode (because disorts view if enabled) </summary> 
        // ##################################################################################
        if (XRSettings.enabled)
        {
            if (main_camera.GetComponent<PostProcessLayer>() != null) main_camera.GetComponent<PostProcessLayer>().enabled = false;
        }
        // ##################################################################################



        // ##################################################################################
        // load helicopter
        // ##################################################################################
        // get objects in Helicopters_Available object (get active Helicopter model)
        Load_Helicopter(ref active_helicopter_id);

        // reset model to initial position
        Reset_Simulation_States();

        // needed if no joystick is connected and then give the camera a target // TODO maybe better 
        helicopter_ODE.Initial_Position_And_Orientation(out position, out rotation); // righthanded, [m], []
        position = Helper.ConvertRightHandedToLeftHandedVector(position); // left handed, [m]
        rotation = Helper.ConvertRightHandedToLeftHandedQuaternion(rotation); // left handed, []
        velocity = Vector3.zero;

        Pause_ODE(gl_pause_flag = true);
        //ui_pause_flag = true;
        // ##################################################################################



        // ##################################################################################
        /// <summary> Init: UI Tabs </summary> 
        // ##################################################################################
        Manage_Tab_Button_Logic(Available_Tabs.simulation);
        // ##################################################################################



        // ##################################################################################
        // sun / eye adaption
        // ##################################################################################
        // get sun's instance
        directional_light = GameObject.Find("Sun").gameObject.GetComponent<Light>();
        // get camerea's postprocess volume
        UnityEngine.Rendering.PostProcessing.PostProcessVolume post_processing_volume = main_camera.GetComponent<PostProcessVolume>();
        // get bloom
        if (post_processing_volume != null)
            post_processing_volume.profile.TryGetSettings(out bloom_layer);
        // ##################################################################################



        // ##################################################################################
        // Camera post-processing effects
        // ##################################################################################
        // motion blur
        if (post_processing_volume != null)
            post_processing_volume.profile.TryGetSettings(out motion_blur_layer);
        // ##################################################################################



        // ##################################################################################
        // pilot
        // ##################################################################################
        // get pilot
        pilot = GameObject.Find("Pilot/Pilot").gameObject;
        // setup pilot scale to match camera with pilot's head/eyes position
        Scale_Pilot_To_Match_Camera_Height();
        // ##################################################################################




        // ##################################################################################
        // pilots' transmitter timer digits
        // ##################################################################################
        Sprite[] transmitter_display_numbers_sprite = Resources.LoadAll<Sprite>("Transmitter_Futaba_T18SZ_numbers");
        transmitter_display_numbers_texture2D = new Texture2D[transmitter_display_numbers_sprite.Count()];
        transmitter_display_digits_material = new Material[5]; // material in 3d modell - each digit has its own amterial Digit1, Digit2,... Digit5

        Material[] materialsArray = pilot.transform.Find("Armature/ribs.001/shoulder.R/upper_arm.R/forearm.R/hand.R/hand.R.002/Transmitter_Antenna").GetComponent<MeshRenderer>().materials;
        foreach (Material material in materialsArray)
        {
            //UnityEngine.Debug.Log("material.name " + material.name);
            for (int i = 0; i < 5; i++)
            {
                if (material.name == "Digit" + (i + 1).ToString() + " (Instance)")
                {
                    // get material of digit
                    transmitter_display_digits_material[i] = material;

                    // change material to "Fade" - to be able to use alpha
                    transmitter_display_digits_material[i].SetFloat("_Mode", 2); // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/StandardShaderGUI.cs
                    transmitter_display_digits_material[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    transmitter_display_digits_material[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    transmitter_display_digits_material[i].SetInt("_ZWrite", 0);
                    transmitter_display_digits_material[i].DisableKeyword("_ALPHATEST_ON");
                    transmitter_display_digits_material[i].EnableKeyword("_ALPHABLEND_ON");
                    transmitter_display_digits_material[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    transmitter_display_digits_material[i].renderQueue = 3000;

                    // change material color to white
                    transmitter_display_digits_material[i].SetColor("_Color", Color.white);
                }
            }
        }

        // convert sprite to texture2D
        for (int i = 0; i < transmitter_display_numbers_sprite.Count(); i++)
            transmitter_display_numbers_texture2D[i] = Common.Helper.Texture_From_Sprite(transmitter_display_numbers_sprite[i]);

        // set default time 
        transmitter_display_digits_material[0].SetTexture("_MainTex", transmitter_display_numbers_texture2D[0]);
        transmitter_display_digits_material[1].SetTexture("_MainTex", transmitter_display_numbers_texture2D[7]);
        transmitter_display_digits_material[2].SetTexture("_MainTex", transmitter_display_numbers_texture2D[3]);
        transmitter_display_digits_material[3].SetTexture("_MainTex", transmitter_display_numbers_texture2D[1]);
        transmitter_display_digits_material[4].SetTexture("_MainTex", transmitter_display_numbers_texture2D[2]);
        // ##################################################################################



        // ##################################################################################
        /// <summary> / UI debug panel's GraphManager position setup </summary> 
        // ##################################################################################
        plot2D_graph_rect[0] = new Rect(0.01f * Screen.width, 0.3500f * Screen.height, 0.24f * Screen.width, 0.2f * Screen.height); // xPos, yPos, xSize, ySize
        plot2D_graph_rect[1] = new Rect(0.01f * Screen.width, 0.5600f * Screen.height, 0.24f * Screen.width, 0.2f * Screen.height); // xPos, yPos, xSize, ySize
        plot2D_graph_rect[2] = new Rect(0.01f * Screen.width, 0.7700f * Screen.height, 0.24f * Screen.width, 0.2f * Screen.height); // xPos, yPos, xSize, ySize
        plot2D_graph_rect[3] = new Rect(0.26f * Screen.width, 0.3500f * Screen.height, 0.24f * Screen.width, 0.2f * Screen.height); // xPos, yPos, xSize, ySize
        plot2D_graph_rect[4] = new Rect(0.26f * Screen.width, 0.5600f * Screen.height, 0.24f * Screen.width, 0.2f * Screen.height); // xPos, yPos, xSize, ySize
        plot2D_graph_rect[5] = new Rect(0.26f * Screen.width, 0.7700f * Screen.height, 0.24f * Screen.width, 0.2f * Screen.height); // xPos, yPos, xSize, ySize
        // ##################################################################################



        // ##################################################################################
        // set connected_input_devices_count_old value
        // ##################################################################################
        IO_Get_Connected_Gamepads_And_Joysticks();
        connected_input_devices_count_old = connected_input_devices_count;
        // ##################################################################################


    }
    // ##################################################################################
    #endregion












    // ##################################################################################
    //      SSSSSSSSSSSSSSS      tttt                                                        tttt          
    //    SS:::::::::::::::S  ttt:::t                                                     ttt:::t          
    //   S:::::SSSSSS::::::S  t:::::t                                                     t:::::t          
    //   S:::::S     SSSSSSS  t:::::t                                                     t:::::t          
    //   S:::::S        ttttttt:::::ttttttt      aaaaaaaaaaaaa  rrrrr   rrrrrrrrr   ttttttt:::::ttttttt    
    //   S:::::S        t:::::::::::::::::t      a::::::::::::a r::::rrr:::::::::r  t:::::::::::::::::t    
    //    S::::SSSS     t:::::::::::::::::t      aaaaaaaaa:::::ar:::::::::::::::::r t:::::::::::::::::t    
    //     SS::::::SSSSStttttt:::::::tttttt               a::::arr::::::rrrrr::::::rtttttt:::::::tttttt    
    //       SSS::::::::SS    t:::::t              aaaaaaa:::::a r:::::r     r:::::r      t:::::t          
    //          SSSSSS::::S   t:::::t            aa::::::::::::a r:::::r     rrrrrrr      t:::::t          
    //               S:::::S  t:::::t           a::::aaaa::::::a r:::::r                  t:::::t          
    //               S:::::S  t:::::t    tttttta::::a    a:::::a r:::::r                  t:::::t    tttttt
    //   SSSSSSS     S:::::S  t::::::tttt:::::ta::::a    a:::::a r:::::r                  t::::::tttt:::::t
    //   S::::::SSSSSS:::::S  tt::::::::::::::ta:::::aaaa::::::a r:::::r                  tt::::::::::::::t
    //   S:::::::::::::::SS     tt:::::::::::tt a::::::::::aa:::ar:::::r                    tt:::::::::::tt
    //    SSSSSSSSSSSSSSS         ttttttttttt    aaaaaaaaaa  aaaarrrrrrr                      ttttttttttt  
    //                                                                                                                                         
    // ##################################################################################
    // ##################################################################################
    /// <summary> Start is called on the frame when a script is enabled just before any of the Update methods are called the first time. </summary>
    // ##################################################################################
    #region start
    void Start()
    {
     
        // during first start of the game a welcome message is shown by using "first_start_flag"
        if (version_number == Application.version)
        {
            first_start_flag = 0;
        }
        else
        {
            PlayerPrefs.SetString("version_number", Application.version);
            Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_callibration_welcome.wav");
        }


        // get monitor timing
        //Resolution[] resolutions = Screen.resolutions;
        if (!XRSettings.enabled)
        {
            refresh_rate_hz = Screen.currentResolution.refreshRate; // [Hz]
            refresh_rate_sec = 1.0f / refresh_rate_hz; // [sec]
                                                       //refresh_rate_sec = Time.smoothDeltaTime; 
        }
        else
        {
            refresh_rate_hz = XRDevice.refreshRate; // [Hz]
            if (refresh_rate_hz == 0) refresh_rate_hz = 90; // todo see https://docs.unity3d.com/ScriptReference/XR.XRDevice-refreshRate.html
            refresh_rate_sec = 1.0f / refresh_rate_hz; // [sec]
        }


        // movement anti-stutter handling initialization
        Start_IO_AntiStutter();

        // reset stopwatch
        stopwatch.Reset();

        // 
        if (first_start_flag == 1)
            gl_pause_flag = true;

        // show which controller is selected
        if (first_start_flag != 1)
            UI_show_new_controller_name_flag = true;



        //// ##################################################################################
        ///// <summary> start simulation thread </summary> 
        //// ##################################################################################
        Simulation_Thread_Start();
        //// ##################################################################################

        //gl_pause_flag = false;
        //Pause_ODE(gl_pause_flag);
        //ui_pause_flag = false;



        //#if DEBUG_LOG  
        //        // debug time ticks to file
        //        //stopwatch_debug.Start();
        //#endif
    }
    #endregion








    // ##################################################################################
    //
    //                                                                          dddddddd                                                               
    //    LLLLLLLLLLL                                                           d::::::d     HHHHHHHHH     HHHHHHHHH                   lllllll   iiii  
    //    L:::::::::L                                                           d::::::d     H:::::::H     H:::::::H                   l:::::l  i::::i 
    //    L:::::::::L                                                           d::::::d     H:::::::H     H:::::::H                   l:::::l   iiii  
    //    LL:::::::LL                                                           d:::::d      HH::::::H     H::::::HH                   l:::::l         
    //      L:::::L                  ooooooooooo     aaaaaaaaaaaaa      ddddddddd:::::d        H:::::H     H:::::H      eeeeeeeeeeee    l::::l iiiiiii 
    //      L:::::L                oo:::::::::::oo   a::::::::::::a   dd::::::::::::::d        H:::::H     H:::::H    ee::::::::::::ee  l::::l i:::::i 
    //      L:::::L               o:::::::::::::::o  aaaaaaaaa:::::a d::::::::::::::::d        H::::::HHHHH::::::H   e::::::eeeee:::::eel::::l  i::::i 
    //      L:::::L               o:::::ooooo:::::o           a::::ad:::::::ddddd:::::d        H:::::::::::::::::H  e::::::e     e:::::el::::l  i::::i 
    //      L:::::L               o::::o     o::::o    aaaaaaa:::::ad::::::d    d:::::d        H:::::::::::::::::H  e:::::::eeeee::::::el::::l  i::::i 
    //      L:::::L               o::::o     o::::o  aa::::::::::::ad:::::d     d:::::d        H::::::HHHHH::::::H  e:::::::::::::::::e l::::l  i::::i 
    //      L:::::L               o::::o     o::::o a::::aaaa::::::ad:::::d     d:::::d        H:::::H     H:::::H  e::::::eeeeeeeeeee  l::::l  i::::i 
    //      L:::::L         LLLLLLo::::o     o::::oa::::a    a:::::ad:::::d     d:::::d        H:::::H     H:::::H  e:::::::e           l::::l  i::::i 
    //    LL:::::::LLLLLLLLL:::::Lo:::::ooooo:::::oa::::a    a:::::ad::::::ddddd::::::dd     HH::::::H     H::::::HHe::::::::e         l::::::li::::::i
    //    L::::::::::::::::::::::Lo:::::::::::::::oa:::::aaaa::::::a d:::::::::::::::::d     H:::::::H     H:::::::H e::::::::eeeeeeee l::::::li::::::i
    //    L::::::::::::::::::::::L oo:::::::::::oo  a::::::::::aa:::a d:::::::::ddd::::d     H:::::::H     H:::::::H  ee:::::::::::::e l::::::li::::::i
    //    LLLLLLLLLLLLLLLLLLLLLLLL   ooooooooooo     aaaaaaaaaa  aaaa  ddddddddd   ddddd     HHHHHHHHH     HHHHHHHHH    eeeeeeeeeeeeee lllllllliiiiiiii
    //
    // ##################################################################################
    /// <summary> Init: get objects in Helicopter Object (get active Helicopter model)  </summary>
    // ##################################################################################
    #region Load_Helicopter
    void Load_Helicopter(ref int helicopter_id)
    {
        // ##################################################################################
        // clamp helicopter id to number of availabe helicopters 
        // ##################################################################################
        active_helicopter_id = (active_helicopter_id < 0) ? helicopters_available.transform.childCount - 1 : (active_helicopter_id > helicopters_available.transform.childCount - 1) ? 0 : active_helicopter_id;


        // ##################################################################################
        // find and activate selected helicopter and read its parameter
        // ##################################################################################
        int helicopter_count = 0;
        foreach (Transform each_helicopter in helicopters_available.transform)
        {
            if (helicopter_count == helicopter_id)
            {
                // activate the selected helicopter
                each_helicopter.gameObject.SetActive(true);

                helicopter_name = each_helicopter.name;

                // get parameter xml-file name
                ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename = PlayerPrefs.GetString("SavedSetting____" + helicopter_name + "____actual_selected_xml_filename", null);
                if (string.IsNullOrEmpty(ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename) ||
                    !File.Exists(folder_saved_parameter_for_transmitter_and_helicopter + ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename))
                {
                    ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename = helicopter_name + "_default_parameter.xml";
                    //PlayerPrefs.SetString("ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename", ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);
                    PlayerPrefs.SetString("SavedSetting____" + helicopter_name + "____actual_selected_xml_filename", ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);
                }

                // load its parameter
                string filename = folder_saved_parameter_for_transmitter_and_helicopter + ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename;
                IO_Load_Transmitter_And_Helicopter_Parameter(filename);

                // load governor rpm setting and change governor target rpm variable
                int selected_bank_id = PlayerPrefs.GetInt("SavedSetting____" + helicopter_name + "____selected_bank_id", 2);
                if (selected_bank_id == 1)
                {
                    helicopter_ODE.flight_bank = 0;
                }
                if (selected_bank_id == 2)
                {
                    helicopter_ODE.flight_bank = 1;
                }
                if (selected_bank_id == 3)
                {
                    helicopter_ODE.flight_bank = 2;
                }
                // setup pilot with transmitter animation
                var temp = GameObject.Find("Pilot/Pilot");
                if (temp != null) animator_pilot_with_transmitter = temp.gameObject.GetComponent<Animator>(); else animator_pilot_with_transmitter = null;

                // setup wheel animation (animator can be owerwritten in Load_Helicopter_Different_Setups()-function, if setup specific gear or skids are available)
                temp = GameObject.Find("Helicopters_Available/" + helicopter_name + "/Animation_Wheels/" + helicopter_name + "_Gear_Or_Skid_Animation_Left");
                if (temp != null) animator_wheels_left = temp.gameObject.GetComponent<Animator>(); else animator_wheels_left = null;
                temp = GameObject.Find("Helicopters_Available/" + helicopter_name + "/Animation_Wheels/" + helicopter_name + "_Gear_Or_Skid_Animation_Right");
                if (temp != null) animator_wheels_right = temp.gameObject.GetComponent<Animator>(); else animator_wheels_right = null;
                temp = GameObject.Find("Helicopters_Available/" + helicopter_name + "/Animation_Wheels/" + helicopter_name + "_Gear_Or_Support_Animation_Steering_Center");
                if (temp != null) animator_wheels_steering_center = temp.gameObject.GetComponent<Animator>(); else animator_wheels_steering_center = null;
                temp = GameObject.Find("Helicopters_Available/" + helicopter_name + "/Animation_Wheels/" + helicopter_name + "_Gear_Or_Support_Animation_Steering_Left");
                if (temp != null) animator_wheels_steering_left = temp.gameObject.GetComponent<Animator>(); else animator_wheels_steering_left = null;
                temp = GameObject.Find("Helicopters_Available/" + helicopter_name + "/Animation_Wheels/" + helicopter_name + "_Gear_Or_Support_Animation_Steering_Right");
                if (temp != null) animator_wheels_steering_right = temp.gameObject.GetComponent<Animator>(); else animator_wheels_steering_right = null;

                // setup pilot in helicopter animation
                temp = GameObject.Find("Helicopters_Available/" + helicopter_name + "/Animation_Pilot/" + helicopter_name + "_Pilot_Animation");
                if (temp != null) animator_pilot = temp.gameObject.GetComponent<Animator>(); else animator_pilot = null;

                // setup doors animation
                temp = GameObject.Find("Helicopters_Available/" + helicopter_name + "/Animation_Doors/" + helicopter_name + "_Doors_Animation");
                if (temp != null)
                {
                    animator_doors = temp.gameObject.GetComponent<Animator>();
                    doors_status = Doors_Status_Variants.closed;
                    doors_status_002 = Doors_Status_Variants.closed;
                }
                else animator_doors = null;

                // setup air brake / speed brake animation
                temp = GameObject.Find("Helicopters_Available/" + helicopter_name + "/Animation_Speed_Brake/" + helicopter_name + "_Speed_Brake_Animation");
                if (temp != null) animator_speed_brake = temp.gameObject.GetComponent<Animator>(); else animator_speed_brake = null;



                // enable wheel brake and speed brake, if helicopter has landing-gears to avoid rolling after startup
                if (helicopter_ODE.par.transmitter_and_helicopter.helicopter.collision.positions_left_type.val == 1 ||
                   helicopter_ODE.par.transmitter_and_helicopter.helicopter.collision.positions_right_type.val == 1) // 0:skids 1:gear 
                {
                    // enable wheel brake
                    if (helicopter_ODE.par.simulation.gameplay.wheel_brake_on_after_heli_change.val)
                    {
                        helicopter_ODE.Wheel_Brake_Enable();
                        wheel_brake_status = Wheel_Brake_Status_Variants.enabled;
                        commentator_audio_source = GameObject.Find("Commentator/Commentator Sound").gameObject.GetComponent<AudioSource>();
                        Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_parking_brake_applied.wav");

                        // enable air brake (on Sikorsky S67-wings)
                        if (animator_speed_brake != null)
                        {
                            animator_speed_brake.SetTrigger("Speed_Brake_Status");
                            speed_brake_status = Animator_Speed_Brake_Status_Variants.opened;
                        }
                    }
                }
                else
                {
                    // disable wheel brake
                    helicopter_ODE.Wheel_Brake_Disable();
                    wheel_brake_status = Wheel_Brake_Status_Variants.disabled;
                    //commentator_audio_source = GameObject.Find("Commentator/Commentator Sound").gameObject.GetComponent<AudioSource>();
                    //Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_parking_brake_released.wav");

                    // disable air brake (on Sikorsky S67-wings)
                    if (animator_speed_brake != null)
                    {
                        animator_speed_brake.ResetTrigger("Speed_Brake_Status");
                        speed_brake_status = Animator_Speed_Brake_Status_Variants.closed;
                    }
                }



                // load vortex_ring_state particles
                temp = GameObject.Find("Helicopters_Available/" + helicopter_name + "/VortexRing");
                if (temp != null) helicopter_vortex_ring_state_particles_gameobject = temp.gameObject; else helicopter_vortex_ring_state_particles_gameobject = null;




                // mainrotor mechanics 
                // if calculation method is "simplified" then also no "mainrotor mechanics" calculation is needed
                // if calculation method is "BEMT" then "mainrotor mechanics" calculation is needed but
                //                     if 3D geometry is available, then update the parts position
                //                     else do only the calculation for the BEMT method. (default kinematic model is the LOGO600SE-V3 mechanics)
                if (helicopter_name == "Logo600SE_V3")
                { 
                    mainrotor_simplified0_or_BEMT1 = PlayerPrefs.GetInt("SavedSetting____" + helicopter_name + "____mainrotor_simplified0_or_BEMT1", 1); // only Logo600SE_V3 has sofar all settings/geomeries for BEMT calculatins
                }
                else
                { 
                    mainrotor_simplified0_or_BEMT1 = PlayerPrefs.GetInt("SavedSetting____" + helicopter_name + "____mainrotor_simplified0_or_BEMT1", 0);
                }
                // setup the 3D geometry --> initalize its position 
                Helicopter_Mainrotor_Mechanics = new Helicopter_Mainrotor_Mechanics.Helicopter_Mainrotor_Mechanics();
                Transform Mainrotor_Mechanics_Model = helicopters_available.transform.Find(helicopter_name).gameObject.transform.Find("Mainrotor_Mechanics_Model");
                if (Mainrotor_Mechanics_Model != null)
                {
                    Helicopter_Mainrotor_Mechanics.rotor_3d_mechanics_geometry_available = true;
                    Mainrotor_Mechanics_Model.transform.localPosition = Helper.ConvertRightHandedToLeftHandedVector(helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor.posLH.vect3);
                    Mainrotor_Mechanics_Model.transform.localRotation = Helper.ConvertRightHandedToLeftHandedQuaternion(Helper.S123toQuat(helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor.oriLH.vect3));
                }
                else
                {
                    Helicopter_Mainrotor_Mechanics.rotor_3d_mechanics_geometry_available = false;
                }
                // initialize kinematics parameter and 3D geometry (if 3D geometry is available)
                Helicopter_Mainrotor_Mechanics.Initialize(helicopter_name, "Mainrotor", Mainrotor_Mechanics_Model);


                // tailrotor mechanics
                // setup the 3D geometry --> initalize its position 
                Helicopter_Tailrotor_Mechanics = new Helicopter_Tailrotor_Mechanics.Helicopter_Tailrotor_Mechanics();
                Transform Tailrotor_Mechanics_Model = helicopters_available.transform.Find(helicopter_name).gameObject.transform.Find("Tailrotor_Mechanics_Model");
                if (Tailrotor_Mechanics_Model != null)
                {
                    Helicopter_Tailrotor_Mechanics.rotor_3d_mechanics_geometry_available = true;
                    Tailrotor_Mechanics_Model.transform.localPosition = Helper.ConvertRightHandedToLeftHandedVector(helicopter_ODE.par.transmitter_and_helicopter.helicopter.tailrotor.posLH.vect3);
                    Tailrotor_Mechanics_Model.transform.localRotation = Helper.ConvertRightHandedToLeftHandedQuaternion(Helper.S123toQuat(helicopter_ODE.par.transmitter_and_helicopter.helicopter.tailrotor.oriLH.vect3));
                }
                else
                {
                    Helicopter_Tailrotor_Mechanics.rotor_3d_mechanics_geometry_available = false;
                }
                // initialize kinematics parameter and 3D geometry (if 3D geometry is available)
                Helicopter_Tailrotor_Mechanics.Initialize(helicopter_name, "Tailrotor", Tailrotor_Mechanics_Model);



                // update ui
                UI_Update();
            }
            else
            {
                // deactivate the other helicopters
                each_helicopter.gameObject.SetActive(false);
            }
            helicopter_count++;
        }
        // ##################################################################################




        // ##################################################################################
        // get model data
        // ##################################################################################
        ambient_audio_source = GameObject.Find("Ambient Sound").gameObject.GetComponent<AudioSource>();
        transmitter_audio_source = GameObject.Find("Pilot/Transmitter Sound").gameObject.GetComponent<AudioSource>();
        commentator_audio_source = GameObject.Find("Commentator/Commentator Sound").gameObject.GetComponent<AudioSource>();
        crash_audio_source = GameObject.Find("Helicopters_Available").gameObject.GetComponent<AudioSource>();
        if (GameObject.Find("Booster/Audio Source Booster") != null)
            booster_audio_source = GameObject.Find("Booster/Audio Source Booster").gameObject.GetComponent<AudioSource>();
        if (GameObject.Find("Booster/Audio Source Booster Continous") != null)
            booster_audio_source_continous = GameObject.Find("Booster/Audio Source Booster Continous").gameObject.GetComponent<AudioSource>();
        

        GameObject Helicopter_Selected = helicopters_available.transform.Find(helicopter_name).gameObject;

        helicopter_object = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.GetChild(0).gameObject;
        mainrotor_object.Init_Rotor_Data(ref helicopter_ODE, ref Helicopter_Selected, ref helicopter_name, helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor, "Mainrotor", helicopter_id);
        tailrotor_object.Init_Rotor_Data(ref helicopter_ODE, ref Helicopter_Selected, ref helicopter_name, helicopter_ODE.par.transmitter_and_helicopter.helicopter.tailrotor, "Tailrotor", helicopter_id);
        propeller_object.Init_Rotor_Data(ref helicopter_ODE, ref Helicopter_Selected, ref helicopter_name, helicopter_ODE.par.transmitter_and_helicopter.helicopter.propeller, "Propeller", helicopter_id);


        // list of materials 
        // (.sharedMaterial changes all this material in all other object instaces which use this material)
        helicopter_canopy_material.Clear();
        Transform go_A = helicopter_object.transform.Find("Canopy_A");
        if (go_A)
        {
            helicopter_canopy_material.Add(go_A.gameObject.GetComponent<MeshRenderer>().sharedMaterial);
            Transform go_B = helicopter_object.transform.Find("Canopy_B");
            if (go_B)
            {
                helicopter_canopy_material.Add(go_B.gameObject.GetComponent<MeshRenderer>().sharedMaterial);
                Transform go_C = helicopter_object.transform.Find("Canopy_C");
                if (go_C)
                {
                    helicopter_canopy_material.Add(go_C.gameObject.GetComponent<MeshRenderer>().sharedMaterial);
                    Transform go_D = helicopter_object.transform.Find("Canopy_D");
                    if (go_D)
                    {
                        helicopter_canopy_material.Add(go_D.gameObject.GetComponent<MeshRenderer>().sharedMaterial);
                        Transform go_E = helicopter_object.transform.Find("Canopy_E");
                        if (go_E)
                        {
                            helicopter_canopy_material.Add(go_E.gameObject.GetComponent<MeshRenderer>().sharedMaterial);
                            Transform go_F = helicopter_object.transform.Find("Canopy_F");
                            if (go_F)
                            {
                                helicopter_canopy_material.Add(go_F.gameObject.GetComponent<MeshRenderer>().sharedMaterial);
                            }
                        }
                    }
                }
            }
        }

        audio_source_motor = Helicopter_Selected.transform.Find("Audio Source Motor").gameObject.GetComponent<AudioSource>();

        if (Helicopter_Mainrotor_Mechanics.rotor_3d_mechanics_geometry_available == true)
        {
            for (int i=0; i<4; i++)
                audio_source_servo[i] = Helicopter_Selected.transform.Find("Audio Source Servo " + i.ToString() ).gameObject.GetComponent<AudioSource>();
        }
        else
        {
            for (int i = 0; i < 4; i++)
                audio_source_servo[i] = null;
        }
        // ##################################################################################



        // ##################################################################################
        // select and load different helicopter setup (change texture, an minor 3d-objects)
        // ##################################################################################
        helicopter_canopy_material_ID = PlayerPrefs.GetInt("SavedSetting____" + helicopter_name + "____actual_selected_helicopter_canopy_material_ID", -1);
        if (helicopter_canopy_material_ID == -1)
        {
            helicopter_canopy_material_ID = 0;
            PlayerPrefs.SetInt("SavedSetting____" + helicopter_name + "____actual_selected_helicopter_canopy_material_ID", helicopter_canopy_material_ID);
        }

        Load_Helicopter_Different_Setups();

        helicopter_ODE.Set_AABB_Helicopter_Collision_Points();
        // ##################################################################################




        // ##################################################################################
        // audio message
        // ##################################################################################
        // Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_helicopter_loaded.wav");
        // ##################################################################################




        // ##################################################################################
        // position light
        // ##################################################################################
        position_lights_state = (Position_Lights_Status_Variants)System.Enum.Parse(typeof(Position_Lights_Status_Variants), PlayerPrefs.GetString("SavedSetting____" + helicopter_name + "____actual_selected_helicopter_position_lights_state", "off"));
        // ##################################################################################




        // ##################################################################################
        // dactivate heat blur effect - GrabPass in "Custom/GlassStainedBumpDistort" seams not to work in "single pass instenced" VR
        // ##################################################################################
        //UnityEngine.Debug.Log("XRSettings.stereoRenderingMode: " + XRSettings.stereoRenderingMode);
        //if (XRSettings.enabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced)
        //{
        Transform go;
        go = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.Find("Particle System");
        if (go != null) go.gameObject.SetActive(!XRSettings.enabled);
        go = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.Find("Particle System (1)");
        if (go != null) go.gameObject.SetActive(!XRSettings.enabled);
        go = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.Find("Particle System Left");
        if (go != null) go.gameObject.SetActive(!XRSettings.enabled);
        go = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.Find("Particle System Right");
        if (go != null) go.gameObject.SetActive(!XRSettings.enabled);
        // ##################################################################################




        // ##################################################################################
        // brushless motor rotation
        // ##################################################################################
        if (Helicopter_Selected.transform.Find("Motor_Model") != null)
            motor_to_rotate = Helicopter_Selected.transform.Find("Motor_Model").gameObject;  //GameObject motor = Helicopter_Selected.transform.Find("Motor_Model/" + helicopter_name + "_Motor").gameObject;
        else
            motor_to_rotate = null;

        if (Helicopter_Selected.transform.Find("Maingear_Model") != null)
            maingear_to_rotate = Helicopter_Selected.transform.Find("Maingear_Model").gameObject;  // Helicopter_Selected.transform.Find("Motor_Model/" + helicopter_name + "_Maingear").gameObject;
        else
            maingear_to_rotate = null;
        // ##################################################################################




        // ##################################################################################
        // save helicopter_id under playeprefs 
        // ##################################################################################
        PlayerPrefs.SetInt("active_helicopter_id", active_helicopter_id);
        // ##################################################################################


        //helicopters_available.gameObject.SetActive(false);
    }
    #endregion










    // ##################################################################################
    //                                                                                 dddddddd                                                    dddddddd                                                            
    //  FFFFFFFFFFFFFFFFFFFFFF  iiii                                                   d::::::dUUUUUUUU     UUUUUUUU                               d::::::d                          tttt                              
    //  F::::::::::::::::::::F i::::i                                                  d::::::dU::::::U     U::::::U                               d::::::d                       ttt:::t                              
    //  F::::::::::::::::::::F  iiii                                                   d::::::dU::::::U     U::::::U                               d::::::d                       t:::::t                              
    //  FF::::::FFFFFFFFF::::F                                                         d:::::d UU:::::U     U:::::UU                               d:::::d                        t:::::t                              
    //    F:::::F       FFFFFFiiiiiii xxxxxxx      xxxxxxx eeeeeeeeeeee        ddddddddd:::::d  U:::::U     U:::::Uppppp   ppppppppp       ddddddddd:::::d   aaaaaaaaaaaaa  ttttttt:::::ttttttt        eeeeeeeeeeee    
    //    F:::::F             i:::::i  x:::::x    x:::::xee::::::::::::ee    dd::::::::::::::d  U:::::D     D:::::Up::::ppp:::::::::p    dd::::::::::::::d   a::::::::::::a t:::::::::::::::::t      ee::::::::::::ee  
    //    F::::::FFFFFFFFFF    i::::i   x:::::x  x:::::xe::::::eeeee:::::ee d::::::::::::::::d  U:::::D     D:::::Up:::::::::::::::::p  d::::::::::::::::d   aaaaaaaaa:::::at:::::::::::::::::t     e::::::eeeee:::::ee
    //    F:::::::::::::::F    i::::i    x:::::xx:::::xe::::::e     e:::::ed:::::::ddddd:::::d  U:::::D     D:::::Upp::::::ppppp::::::pd:::::::ddddd:::::d            a::::atttttt:::::::tttttt    e::::::e     e:::::e
    //    F:::::::::::::::F    i::::i     x::::::::::x e:::::::eeeee::::::ed::::::d    d:::::d  U:::::D     D:::::U p:::::p     p:::::pd::::::d    d:::::d     aaaaaaa:::::a      t:::::t          e:::::::eeeee::::::e
    //    F::::::FFFFFFFFFF    i::::i      x::::::::x  e:::::::::::::::::e d:::::d     d:::::d  U:::::D     D:::::U p:::::p     p:::::pd:::::d     d:::::d   aa::::::::::::a      t:::::t          e:::::::::::::::::e 
    //    F:::::F              i::::i      x::::::::x  e::::::eeeeeeeeeee  d:::::d     d:::::d  U:::::D     D:::::U p:::::p     p:::::pd:::::d     d:::::d  a::::aaaa::::::a      t:::::t          e::::::eeeeeeeeeee  
    //    F:::::F              i::::i     x::::::::::x e:::::::e           d:::::d     d:::::d  U::::::U   U::::::U p:::::p    p::::::pd:::::d     d:::::d a::::a    a:::::a      t:::::t    tttttte:::::::e           
    //  FF:::::::FF           i::::::i   x:::::xx:::::xe::::::::e          d::::::ddddd::::::dd U:::::::UUU:::::::U p:::::ppppp:::::::pd::::::ddddd::::::dda::::a    a:::::a      t::::::tttt:::::te::::::::e          
    //  F::::::::FF           i::::::i  x:::::x  x:::::xe::::::::eeeeeeee   d:::::::::::::::::d  UU:::::::::::::UU  p::::::::::::::::p  d:::::::::::::::::da:::::aaaa::::::a      tt::::::::::::::t e::::::::eeeeeeee  
    //  F::::::::FF           i::::::i x:::::x    x:::::xee:::::::::::::e    d:::::::::ddd::::d    UU:::::::::UU    p::::::::::::::pp    d:::::::::ddd::::d a::::::::::aa:::a       tt:::::::::::tt  ee:::::::::::::e  
    //  FFFFFFFFFFF           iiiiiiiixxxxxxx      xxxxxxx eeeeeeeeeeeeee     ddddddddd   ddddd      UUUUUUUUU      p::::::pppppppp       ddddddddd   ddddd  aaaaaaaaaa  aaaa         ttttttttttt      eeeeeeeeeeeeee  
    //                                                                                                              p:::::p                                                                                            
    //                                                                                                              p:::::p                                                                                            
    //                                                                                                             p:::::::p                                                                                           
    //                                                                                                             p:::::::p                                                                                           
    //                                                                                                             p:::::::p                                                                                           
    //                                                                                                             ppppppppp 
    //  
    // ##################################################################################
    /// <summary>  </summary>
    // ##################################################################################
    #region FixedUpdate
    //void FixedUpdate_TEST()

    void FixedUpdate()
    {

        IO_AntiStutter__Get_FixedUpdate_TimeTick();

    }
    #endregion








    // ##################################################################################
    //                                                        dddddddd                                                            
    //    UUUUUUUU     UUUUUUUU                               d::::::d                          tttt                              
    //    U::::::U     U::::::U                               d::::::d                       ttt:::t                              
    //    U::::::U     U::::::U                               d::::::d                       t:::::t                              
    //    UU:::::U     U:::::UU                               d:::::d                        t:::::t                              
    //     U:::::U     U:::::Uppppp   ppppppppp       ddddddddd:::::d   aaaaaaaaaaaaa  ttttttt:::::ttttttt        eeeeeeeeeeee    
    //     U:::::D     D:::::Up::::ppp:::::::::p    dd::::::::::::::d   a::::::::::::a t:::::::::::::::::t      ee::::::::::::ee  
    //     U:::::D     D:::::Up:::::::::::::::::p  d::::::::::::::::d   aaaaaaaaa:::::at:::::::::::::::::t     e::::::eeeee:::::ee
    //     U:::::D     D:::::Upp::::::ppppp::::::pd:::::::ddddd:::::d            a::::atttttt:::::::tttttt    e::::::e     e:::::e
    //     U:::::D     D:::::U p:::::p     p:::::pd::::::d    d:::::d     aaaaaaa:::::a      t:::::t          e:::::::eeeee::::::e
    //     U:::::D     D:::::U p:::::p     p:::::pd:::::d     d:::::d   aa::::::::::::a      t:::::t          e:::::::::::::::::e 
    //     U:::::D     D:::::U p:::::p     p:::::pd:::::d     d:::::d  a::::aaaa::::::a      t:::::t          e::::::eeeeeeeeeee  
    //     U::::::U   U::::::U p:::::p    p::::::pd:::::d     d:::::d a::::a    a:::::a      t:::::t    tttttte:::::::e           
    //     U:::::::UUU:::::::U p:::::ppppp:::::::pd::::::ddddd::::::dda::::a    a:::::a      t::::::tttt:::::te::::::::e          
    //      UU:::::::::::::UU  p::::::::::::::::p  d:::::::::::::::::da:::::aaaa::::::a      tt::::::::::::::t e::::::::eeeeeeee  
    //        UU:::::::::UU    p::::::::::::::pp    d:::::::::ddd::::d a::::::::::aa:::a       tt:::::::::::tt  ee:::::::::::::e  
    //          UUUUUUUUU      p::::::pppppppp       ddddddddd   ddddd  aaaaaaaaaa  aaaa         ttttttttttt      eeeeeeeeeeeeee  
    //                         p:::::p                                                                                            
    //                         p:::::p                                                                                            
    //                        p:::::::p                                                                                           
    //                        p:::::::p                                                                                           
    //                        p:::::::p                                                                                           
    //                        ppppppppp    
    // ##################################################################################
    #region Update
    // ##################################################################################
    // Update
    // ##################################################################################
    // Update is called once per frame
    void Update()
    {
        

        Correct_And_Limit_XR_Camera_Vertical_Position();


        // ##################################################################################
        // wait a moment after start - TODO remove this: unnecessary 
        // ##################################################################################
        update_called_cntr++;
        //////if(Time.frameCount == 200)
        if (update_called_cntr == 1) // TODO remove this complete if(...){}
        {
            helicopter_ODE.Set_Initial_Conditions();

            position = helicopters_available.transform.position;
            position.x = (float)helicopter_ODE.x_states[0]; // [m] x in reference frame
            position.y = (float)helicopter_ODE.x_states[1]; // [m] y in reference frame
            position.z = (float)helicopter_ODE.x_states[2]; // [m] z in reference frame
            position = Helper.ConvertRightHandedToLeftHandedVector(position);
            helicopters_available.transform.position = position;
            rotation = helicopters_available.transform.rotation;
            rotation.w = (float)helicopter_ODE.x_states[3]; // [-] w
            rotation.x = (float)helicopter_ODE.x_states[4]; // [-] x
            rotation.y = (float)helicopter_ODE.x_states[5]; // [-] y
            rotation.z = (float)helicopter_ODE.x_states[6]; // [-] z
            rotation = Helper.ConvertRightHandedToLeftHandedQuaternion(rotation);
            helicopters_available.transform.rotation = rotation;
        }
        if (update_called_cntr == 60) // TODO remove this complete if(...){}
        {
            // reset xr-camera height correction value
            if (XRSettings.enabled)
                xr_camera_vertical_position_offset = helicopter_ODE.par.scenery.camera_height.val - main_camera.transform.localPosition.y + 0.0f; // [m]

            //Simulation_Thread_Start();
            Pause_ODE(gl_pause_flag = false);
            ui_pause_flag = false;
        }
        // ##################################################################################




        //if (Time.frameCount > 200)
        if (update_called_cntr > 300)
        {
            // find the exact referesharte
            Find_Exact_Monitor_Refreshrate();
        }




#if DEBUG_LOG
            // debug time ticks to file
            Debug_Collect_Time_Ticks(enum_ID.t_update, stopwatch_antistutter.Elapsed.Ticks);
#endif
        // ##################################################################################
        // 
        // ##################################################################################
        // game timescale,... (ODE has its independent calculation)
        Time.timeScale = helicopter_ODE.par.simulation.physics.timescale.val;

        //int v_sync = Helper.Clamp(helicopter_ODE.par.simulation.v_sync);
        //if (v_sync != v_sync_old)
        //{
        //    QualitySettings.vSyncCount = v_sync;
        //    // make the target_frame_rate variable not accessible, if v_sync > 0
        //    if (v_sync > 0)
        //        helicopter_ODE.par_temp.simulation.target_frame_rate.calculated = true;
        //    else
        //        helicopter_ODE.par_temp.simulation.target_frame_rate.calculated = false;
        //    helicopter_ODE.flag_load_new_parameter_in_ODE_thread = true;
        //    UI_Update_Parameter_Settings_UI();
        //    v_sync_old = v_sync;
        //}

        //int target_frame_rate = helicopter_ODE.par.simulation.target_frame_rate.val;
        //if (target_frame_rate != target_frame_rate_old)
        //{
        //    Application.targetFrameRate = Helper.Clamp(helicopter_ODE.par.simulation.target_frame_rate);
        //    target_frame_rate_old = target_frame_rate;
        //}

        bool motion_blur = helicopter_ODE.par_temp.simulation.graphic_quality.motion_blur.val;
        if (motion_blur != motion_blur_old)
        {
            if (motion_blur_layer != null)
                motion_blur_layer.enabled.value = motion_blur;
            motion_blur_old = motion_blur;
        }

        bool bloom = helicopter_ODE.par_temp.simulation.graphic_quality.bloom.val;
        if (bloom != bloom_old)
        {
            if (bloom_layer != null)
                bloom_layer.enabled.value = bloom;
            bloom_old = bloom;
        }

        int quality_setting = Helper.Clamp(helicopter_ODE.par_temp.simulation.graphic_quality.quality_setting);
        if (quality_setting != quality_setting_old)
        {
            QualitySettings.SetQualityLevel(quality_setting, true);
            quality_setting_old = quality_setting;
        }

        int resolution_setting = Helper.Clamp(helicopter_ODE.par_temp.simulation.graphic_quality.resolution_setting);
        if (resolution_setting != resolution_setting_old)
        {
            //Resolution[] resolutions = Screen.resolutions;
            //Screen.SetResolution(resolutions[resolution_setting].width, resolutions[resolution_setting].height, true);

            string[] splitArray = helicopter_ODE.par_temp.simulation.graphic_quality.resolution_setting.str[resolution_setting].Split(char.Parse("x"));
            Screen.SetResolution(Int32.Parse(splitArray[0]), Int32.Parse(splitArray[1]), true);
            //UnityEngine.Debug.Log("resolution : " + Int32.Parse(splitArray[0]) + "  " + Int32.Parse(splitArray[1]));

            resolution_setting_old = resolution_setting;
        }
        // ##################################################################################





        // ##################################################################################
        // anti stutter
        // ##################################################################################
        // setup physics update freqency to be as high as fps, so FixedUpdate() is in every 
        // frame FixedUpdate() is called. 
        if (QualitySettings.vSyncCount > 0) // if V_sync on
        {
            Time.fixedDeltaTime = refresh_rate_sec * 0.98f * helicopter_ODE.par.simulation.physics.timescale.val;
        }
        else
        {
            Time.fixedDeltaTime = 0.01f;
        }
        // ##################################################################################





        // ##################################################################################
        // new input system
        // ##################################################################################
        for (int i = 0; i < 8; i++)
            input_channel_used_in_game[i] = input_channel_from_event_proccessing[i];

        //UnityEngine.Debug.Log("AXIS CHANNELS  C0: " + Helper.FormatNumber(input_channel_used_in_game[0], "0.000") +
        //                                    " C1: " + Helper.FormatNumber(input_channel_used_in_game[1], "0.000") +
        //                                    " C2: " + Helper.FormatNumber(input_channel_used_in_game[2], "0.000") +
        //                                    " C3: " + Helper.FormatNumber(input_channel_used_in_game[3], "0.000") +
        //                                    " C4: " + Helper.FormatNumber(input_channel_used_in_game[4], "0.000") +
        //                                    " C5: " + Helper.FormatNumber(input_channel_used_in_game[5], "0.000") +
        //                                    " C6: " + Helper.FormatNumber(input_channel_used_in_game[6], "0.000") +
        //                                    " C7: " + Helper.FormatNumber(input_channel_used_in_game[7], "0.000"));
        //UnityEngine.Debug.Log("   Gamepad.current " + Gamepad.current.name + "   Joystick.current " + Joystick.current.name);
        // ##################################################################################









        // ##################################################################################
        // Get Keys
        // ##################################################################################
        if (first_start_flag == 1)
        {
            ui_welcome_panel_flag = true;

            ui_info_panel_flag = false;
            ui_debug_panel_state = 0;
            ui_exit_panel_flag = false;
            ui_pause_flag = false;
            ui_parameter_panel_flag = false;
            ui_helicopter_selection_menu_flag = false;
            ui_scenery_selection_menu_flag = false;
            ui_pie_menu_flag = false;

            gl_pause_flag = true;
        }



        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (ui_welcome_panel_flag == true || ui_info_panel_flag == true || ui_parameter_panel_flag == true ||
                ui_pause_flag == true || ui_exit_panel_flag == true || ui_helicopter_selection_menu_flag == true ||
                ui_scenery_selection_menu_flag == true || ui_pie_menu_flag == true ) // || ui_debug_panel_state > 0
            {
                ui_welcome_panel_flag = false;
                ui_info_panel_flag = false;
                ui_debug_panel_state = 0;
                ui_exit_panel_flag = false;
                ui_pause_flag = false;
                ui_parameter_panel_flag = false;
                ui_helicopter_selection_menu_flag = false;
                ui_scenery_selection_menu_flag = false;
                ui_pie_menu_flag = false;

                gl_pause_flag = false;
            }
            else
            {
                ui_exit_panel_flag = true;

                gl_pause_flag = true;
            }

            if(calibration_abortable && calibration_state != State_Calibration.not_running)
            {
                calibration_state = State_Calibration.abort;
                ui_exit_panel_flag = false;

                gl_pause_flag = false;
            }

            if (first_start_flag == 1)
            {
                first_start_flag = 0;
            }
        }


        if (first_start_flag == 0)
        {
            if ((UnityEngine.InputSystem.Keyboard.current.wKey.wasPressedThisFrame && !Is_Text_Input_Field_Focused()))
            {
                ui_menu_logic_welcome_menu();
            }


            // in options menu parameter can be saved with unique names, given by user. While user enters text, disable the other key actions.
            if (!Is_Text_Input_Field_Focused() && !ui_controller_calibration_flag)
            {
                // ##################################################################################
                // Get Keys
                // ##################################################################################
                if (UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame)
                {
                    ui_menu_logic_pause();
                }

                if (UnityEngine.InputSystem.Keyboard.current.oKey.wasPressedThisFrame)
                {
                    ui_menu_logic_parameter();
                }

                if (UnityEngine.InputSystem.Keyboard.current.f1Key.wasPressedThisFrame)
                {
                    ui_menu_logic_info(); 
                }

                if (UnityEngine.InputSystem.Keyboard.current.numpadEnterKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    ui_menu_logic_pie_menu();
                }


                if ((Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor))
                {
                    if (UnityEngine.InputSystem.Keyboard.current.pageUpKey.wasPressedThisFrame)
                    {
                        xr_mode_flag ^= true;

                        if (xr_mode_flag)
                        {
                            co_StartXR = StartCoroutine(StartXR());
                        }
                        else
                        {
                            StopXR();
                        }
                    }
                }

                if (UnityEngine.InputSystem.Keyboard.current.f5Key.wasPressedThisFrame)
                {
                    if(helicopter_name == "Logo600SE_V3") // TODO remove 
                    { 
                        mainrotor_simplified0_or_BEMT1 = mainrotor_simplified0_or_BEMT1 == 0 ? 1 : 0;
                        PlayerPrefs.SetInt("SavedSetting____" + helicopter_name + "____mainrotor_simplified0_or_BEMT1", mainrotor_simplified0_or_BEMT1);

                        // reset model
                        Pause_ODE(gl_pause_flag = true);
                        Reset_Simulation_States();
                        Pause_ODE(gl_pause_flag = false);
                    }
                    else
                    {
                        mainrotor_simplified0_or_BEMT1 = 0;
                        PlayerPrefs.SetInt("SavedSetting____" + helicopter_name + "____mainrotor_simplified0_or_BEMT1", mainrotor_simplified0_or_BEMT1);
                    }
                    //Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_bank_three_selected.wav");
                }

                if (UnityEngine.InputSystem.Keyboard.current.f12Key.wasPressedThisFrame)
                {
                    string date = System.DateTime.Now.ToString();
                    date = date.Replace("/", "-"); date = date.Replace(" ", "_"); date = date.Replace(":", "_"); date = date.Replace(".", "_");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Application.persistentDataPath, "screenshot_" + date + ".png"));
                    //ScreenCapture.CaptureScreenshot(Application.dataPath + "/ScreenShots/screenshot_" + date + ".png");

                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_screenshot_saved.wav");
                }

                // h-key has two functions: pressing and release fast triggers first function. Holding longer than 0.3 sec triggers second function
                if (ui_scenery_selection_menu_flag == false && !gl_pause_flag)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame)
                    {
                        if (ui_helicopter_selection_menu_flag == false)
                        {
                            // start timer to detect later, if first or second function is triggered
                            helicopter_selection_key_delay_timer = Time.time;
                            helicopter_selection_key_delay_flag = true;
                        }
                        else
                        {
                            // deactivate helicopter selection panel
                            ui_helicopter_selection_menu_flag = false;
                            helicopter_selection_key_delay_flag = false;
                            gl_pause_flag = false;
                        }
                    }
                    // first function: short pressing (during release) changes helicopter directly
                    if (UnityEngine.InputSystem.Keyboard.current.hKey.wasReleasedThisFrame && (helicopter_selection_key_delay_flag == true))
                    {
                        if ((Time.time - helicopter_selection_key_delay_timer) < 0.3f)
                        {
                            if (UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightShiftKey.isPressed)
                                active_helicopter_id--;
                            else
                                active_helicopter_id++;

                            Pause_ODE(gl_pause_flag = true);

                            Load_Helicopter(ref active_helicopter_id);

                            UI_Update_Parameter_Settings_UI();

                            // reset model to initial position
                            Reset_Simulation_States();
                            Pause_ODE(gl_pause_flag = false);

                            Reset_Animation_Wheels(Wheels_Status_Variants.lowered);

                            helicopter_selection_key_delay_timer = 0;
                            helicopter_selection_key_delay_flag = false;
                        }
                    }
                    // second function: long pressing triggers opens helicopter selection menu
                    if ((helicopter_selection_key_delay_flag == true) && (Time.time >= (helicopter_selection_key_delay_timer + 0.3f)))
                    {
                        ui_menu_logic_select_helicopter();
                        //ui_welcome_panel_flag = false;
                        ////ui_debug_panel_state = 0;
                        //ui_exit_panel_flag = false;
                        //ui_pause_flag = false;
                        //ui_parameter_panel_flag = false;
                        //ui_helicopter_selection_menu_flag = true; // <==
                        //ui_scenery_selection_menu_flag = false;

                        //helicopter_selection_key_delay_flag = false;

                        //UI_Update_Helicopter_Or_Scenery_Selection_Panel_UI(Ui_Selection_Type.helicopter, ui_helicopter_selection);

                        //gl_pause_flag = true;
                    }
                }






                // s-key has two functions: pressing and release fast triggers first function. Holding longer than 0.3 sec triggers second function
                if (ui_helicopter_selection_menu_flag == false && !gl_pause_flag)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.sKey.wasPressedThisFrame)
                    {
                        Check_Skymaps(ref active_scenery_id, ref list_skymap_paths);

                        if (ui_scenery_selection_menu_flag == false)
                        {
                            // start timer to detect later, if first or second function is triggered
                            scenery_selection_key_delay_timer = Time.time;
                            scenery_selection_key_delay_flag = true;
                        }
                        else
                        {
                            // deactivate scenery selection panel
                            ui_scenery_selection_menu_flag = false;
                            scenery_selection_key_delay_flag = false;
                            gl_pause_flag = false;
                        }

                        //ui_scenery_selection_menu_flag = false;
                    }
                    // first function: short pressing (during release) changes scenery directly
                    if (UnityEngine.InputSystem.Keyboard.current.sKey.wasReleasedThisFrame && scenery_selection_key_delay_flag == true)
                    {
                        if ((Time.time - scenery_selection_key_delay_timer) < 0.3f)
                        {
                            if (UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightShiftKey.isPressed)
                                active_scenery_id--;
                            else
                                active_scenery_id++;

                            load_skymap_state = State_Load_Skymap.prepare_starting;

                            scenery_selection_key_delay_timer = 0;
                            scenery_selection_key_delay_flag = false;
                        }
                    }
                    // second function: long pressing triggers opens scenery selection menu
                    if (scenery_selection_key_delay_flag && Time.time >= (scenery_selection_key_delay_timer + 0.3f))
                    {
                        ui_menu_logic_select_scenery();
                        //ui_welcome_panel_flag = false;
                        ////ui_debug_panel_state = 0;
                        //ui_exit_panel_flag = false;
                        //ui_pause_flag = false;
                        //ui_parameter_panel_flag = false;
                        //ui_helicopter_selection_menu_flag = false;
                        //ui_scenery_selection_menu_flag = true; // <==

                        //scenery_selection_key_delay_flag = false;

                        //UI_Update_Helicopter_Or_Scenery_Selection_Panel_UI(Ui_Selection_Type.scenery, ui_scenery_selection);

                        //gl_pause_flag = true;
                    }
                }






                // a-key has two functions: pressing and release fast triggers first function. Holding longer than 0.3 sec triggers second function
                if (ui_helicopter_selection_menu_flag == false && !gl_pause_flag)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.aKey.wasPressedThisFrame)
                    {
                        // start timer to detect later, if first or second function is triggered
                        animation_selection_key_delay_timer = Time.time;
                        animation_selection_key_delay_flag = true;
                    }
                    // first function: short pressing (during release) changes scenery directly
                    if (UnityEngine.InputSystem.Keyboard.current.aKey.wasReleasedThisFrame && animation_selection_key_delay_flag == true)
                    {
                        if ((Time.time - animation_selection_key_delay_timer) < 0.3f)
                        {
                            if (animator_doors != null) doors_status ^= Doors_Status_Variants.opened;

                            animation_selection_key_delay_timer = 0;
                            animation_selection_key_delay_flag = false;
                        }
                    }
                    // second function: long pressing triggers opens scenery selection menu
                    if (animation_selection_key_delay_flag && Time.time >= (animation_selection_key_delay_timer + 0.3f))
                    {
                        if (animator_doors != null) doors_status_002 ^= Doors_Status_Variants.opened;

                        animation_selection_key_delay_timer = 0;
                        animation_selection_key_delay_flag = false;
                    }
                }









                if (!ui_parameter_panel_flag && !gl_pause_flag)
                {

                    if (UnityEngine.InputSystem.Keyboard.current.digit1Key.wasPressedThisFrame)
                    {
                        helicopter_ODE.flight_bank = 0;
                        PlayerPrefs.SetInt("SavedSetting____" + helicopter_name + "____selected_bank_id", 1);
                        Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_bank_one_selected.wav");
                    }
                    if (UnityEngine.InputSystem.Keyboard.current.digit2Key.wasPressedThisFrame)
                    {
                        helicopter_ODE.flight_bank = 1;
                        PlayerPrefs.SetInt("SavedSetting____" + helicopter_name + "____selected_bank_id", 2);
                        Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_bank_two_selected.wav");
                    }
                    if (UnityEngine.InputSystem.Keyboard.current.digit3Key.wasPressedThisFrame)
                    {
                        helicopter_ODE.flight_bank = 2;
                        PlayerPrefs.SetInt("SavedSetting____" + helicopter_name + "____selected_bank_id", 3);
                        Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_bank_three_selected.wav");
                    }

                    if (UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
                    {
                        if (helicopter_ODE.par.transmitter_and_helicopter.helicopter.collision.positions_left_type.val == 1)  // 1:gear  0: skids
                        {
                            // wheel brake
                            if (wheel_brake_status == Wheel_Brake_Status_Variants.disabled)
                            {
                                helicopter_ODE.Wheel_Brake_Enable();
                                wheel_brake_status = Wheel_Brake_Status_Variants.enabled;
                                Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_parking_brake_applied.wav");
                            }
                            else
                            {
                                helicopter_ODE.Wheel_Brake_Disable();
                                wheel_brake_status = Wheel_Brake_Status_Variants.disabled;
                                Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_parking_brake_released.wav");
                            }

                            // air brake / speed brake animation     TODO add air resistance effect to simulation
                            if (animator_speed_brake != null)
                            {
                                if (speed_brake_status == Animator_Speed_Brake_Status_Variants.closed)
                                {
                                    animator_speed_brake.SetTrigger("Speed_Brake_Status");
                                    speed_brake_status = Animator_Speed_Brake_Status_Variants.opened;
                                }
                                else
                                {
                                    animator_speed_brake.ResetTrigger("Speed_Brake_Status");
                                    speed_brake_status = Animator_Speed_Brake_Status_Variants.closed;
                                }
                            }
                        }
                    }


                    if (UnityEngine.InputSystem.Keyboard.current.dKey.wasPressedThisFrame)
                    {
                        if (UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightShiftKey.isPressed)
                            ui_debug_panel_state--;
                        else
                            ui_debug_panel_state++;

                        ui_debug_panel_state = (ui_debug_panel_state < 0) ? 0 : (ui_debug_panel_state > 2) ? 0 : ui_debug_panel_state;
                    }

                    if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
                    {
                        Pause_ODE(gl_pause_flag = true);
                        Reset_Simulation_States();
                        Pause_ODE(gl_pause_flag = false);
                    }

                    if (UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame)
                    {
                        helicopter_ODE.Toggle_Start_Motor();
                    }

                    if (UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame)
                    {
                        ui_menu_logic_controller_calibration_menu();
                        //if (calibration_state == State_Calibration.not_running && !ui_parameter_panel_flag && first_start_flag == 0)
                        //{
                        //    calibration_state = State_Calibration.starting;
                        //    calibration_abortable = true; // if started manually, calbration can be abborted
                        //}
                    }

                    if (UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
                    {
                        if (UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightShiftKey.isPressed)
                            helicopter_canopy_material_ID--;
                        else
                            helicopter_canopy_material_ID++;

                        int number_of_available_textures = 0;
                        foreach (var t in all_textures)
                        {
                            if (t.name.Contains(helicopter_name + "_Canopy_0") && t.name.Substring(t.name.Length - 2).Contains("_A"))
                                number_of_available_textures++;
                        }

                        // clamp canopy texture number to number of availabe textures for selected helicopter
                        helicopter_canopy_material_ID = (helicopter_canopy_material_ID < 0) ? number_of_available_textures - 1 : (helicopter_canopy_material_ID > number_of_available_textures - 1) ? 0 : helicopter_canopy_material_ID;
                        PlayerPrefs.SetInt("SavedSetting____" + helicopter_name + "____actual_selected_helicopter_canopy_material_ID", helicopter_canopy_material_ID);

                        Load_Helicopter_Different_Setups();
                    }

                    // position lights 
                    if (UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame)
                    {
                        position_lights_state = (Position_Lights_Status_Variants)((int)(position_lights_state + 1) % 3);
                        PlayerPrefs.SetString("SavedSetting____" + helicopter_name + "____actual_selected_helicopter_position_lights_state", position_lights_state.ToString());

                        // audio
                        switch (position_lights_state)
                        {
                            case Helicopter_Main.Position_Lights_Status_Variants.off:
                                {
                                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_lights_off.wav");
                                    break;
                                }
                            case Helicopter_Main.Position_Lights_Status_Variants.blinking:
                                {
                                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_lights_blinking.wav");
                                    break;
                                }
                            case Helicopter_Main.Position_Lights_Status_Variants.on:
                                {
                                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_lights_on.wav");
                                    break;
                                }
                            default: break;
                        }
                    }


                    if (UnityEngine.InputSystem.Keyboard.current.gKey.wasPressedThisFrame)
                    {
                        if (animator_wheels_left != null && animator_wheels_right != null)
                        {
                            wheels_status ^= Wheels_Status_Variants.raised;

                            if (System.Array.Exists(animator_wheels_left.parameters, p => p.name == "Wheel_Only_Allowed_Lowered"))
                                wheels_status = Wheels_Status_Variants.lowered;

                            helicopter_ODE.wheel_status = (Helisimulator.Helicopter_ODE.Wheels_Status_Variants)wheels_status;
                        }
                    }

                    //if (UnityEngine.InputSystem.Keyboard.current.aKey.wasPressedThisFrame)
                    //{
                    //    if (animator_doors != null) doors_status ^= Doors_Status_Variants.opened;
                    //}


                    // pusher propeller at Chayenne tail or Airwolf's rocket booster
                    if (UnityEngine.InputSystem.Keyboard.current.upArrowKey.wasPressedThisFrame)
                    {
                        if (helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.propeller.rotor_exists.val)
                            u_inputs[4] = 1;

                        if (helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.booster.booster_exists.val && helicopter_canopy_material_ID==0)
                        {
                            u_inputs[5] = 1;

                            Transform go_;
                            GameObject Helicopter_Selected_ = helicopters_available.transform.Find(helicopter_name).gameObject;
                            go_ = Helicopter_Selected_.transform.Find("Booster").gameObject.transform.Find("Afterburner_Left");
                            if (go_ != null) go_.gameObject.SetActive(true);
                            go_ = Helicopter_Selected_.transform.Find("Booster").gameObject.transform.Find("Afterburner_Right");
                            if (go_ != null) go_.gameObject.SetActive(true);

                            booster_audio_source.volume = (helicopter_ODE.par.simulation.audio.master_sound_volume.val / 100f);
                            booster_audio_source_continous.volume = (helicopter_ODE.par.simulation.audio.master_sound_volume.val / 100f);
                            if (!booster_audio_source.isPlaying)
                                booster_audio_source.Play();
                            if (!booster_audio_source_continous.isPlaying)
                                booster_audio_source_continous.Play();
                        }
                    }
                    if (UnityEngine.InputSystem.Keyboard.current.leftArrowKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.rightArrowKey.wasPressedThisFrame)
                    {

                        if (helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.propeller.rotor_exists.val)
                            u_inputs[4] = 0;

                        if (helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.booster.booster_exists.val)
                        {
                            u_inputs[5] = 0;

                            Transform go_;
                            GameObject Helicopter_Selected_ = helicopters_available.transform.Find(helicopter_name).gameObject;
                            go_ = Helicopter_Selected_.transform.Find("Booster").gameObject.transform.Find("Afterburner_Left");
                            if (go_ != null) go_.gameObject.SetActive(false);
                            go_ = Helicopter_Selected_.transform.Find("Booster").gameObject.transform.Find("Afterburner_Right");
                            if (go_ != null) go_.gameObject.SetActive(false);

                            booster_audio_source.Stop();
                            booster_audio_source_continous.Stop();
                        }
                    }
                    if (UnityEngine.InputSystem.Keyboard.current.downArrowKey.wasPressedThisFrame)
                    {
                        if (helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.propeller.rotor_exists.val)
                            u_inputs[4] = -1;
                    }




                    if (UnityEngine.InputSystem.Keyboard.current.iKey.wasPressedThisFrame)
                    {
                        if (ui_informations_overlay != null)
                        {
                            ui_informations_overlay_visibility_state ^= true;
                            ui_informations_overlay.transform.gameObject.SetActive(ui_informations_overlay_visibility_state);
                            PlayerPrefs.SetInt("ui_informations_overlay_visibility_state", ui_informations_overlay_visibility_state ? 1 : 0);
                        }
                    }


                }
            }
        }

        if (calibration_state != State_Calibration.not_running && first_start_flag == 0 && !ui_welcome_panel_flag)
        {
            gl_pause_flag = true;
            Controller_Calibration();
        }


        //if (UnityEngine.InputSystem.Keyboard.current.jKey.wasPressedThisFrame && !Is_Text_Input_Field_Focused() && !ui_parameter_panel_flag && calibration_state == State_Calibration.not_running)
        if (UnityEngine.InputSystem.Keyboard.current.jKey.wasPressedThisFrame && !Is_Text_Input_Field_Focused() && !ui_parameter_panel_flag )
        {
            if (connected_input_devices_count > 0)
            {
                //  if calibartion window is open, close it 
                if (connected_input_devices_count > 1)
                { 
                    if (calibration_state != State_Calibration.not_running) // calibration_abortable
                    {
                        calibration_abortable = true; // if there are other controller available, calbration can be abborted
                        calibration_state = State_Calibration.abort;
                    }
                }

                if (UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightShiftKey.isPressed)
                    selected_input_device_id--;
                else
                    selected_input_device_id++;

                // clamp controller id to number of availabe controllers 
                selected_input_device_id = (selected_input_device_id < 0) ? connected_input_devices_count - 1 : (selected_input_device_id > connected_input_devices_count - 1) ? 0 : selected_input_device_id;

                Get_Connected_Controller();

                UI_show_new_controller_name_flag = true;
                //connected_input_devices_names[selected_input_device_id];
                PlayerPrefs.SetInt("CC___selected_input_device_i", selected_input_device_id);
            }
        }
        //UnityEngine.Debug.Log("selected_input_device_id " + selected_input_device_id + "    " + connected_input_devices_count);
        // ##################################################################################




        // ##################################################################################
        // handling of loading skymap (takes several frames)
        // ##################################################################################
        // 3.)
        if (load_skymap_state == State_Load_Skymap.finising)
        {
            ui_loading_panel_flag = false;

            Reset_Simulation_States();

            Pause_ODE(gl_pause_flag = false);

            // setup pilot scale to match camera with pilot's head/eyes position
            Scale_Pilot_To_Match_Camera_Height();

            load_skymap_state = State_Load_Skymap.not_running;
        }
        // 2.)
        if (load_skymap_state == State_Load_Skymap.running)
        {
            Load_Skymap(list_skymap_paths, active_scenery_id);

            load_skymap_state = State_Load_Skymap.finising;
        }
        // 1.)
        if (load_skymap_state == State_Load_Skymap.starting)
        {
            Pause_ODE(gl_pause_flag = true);

            //ui_dropdown_actual_selected_scenery_xml_filename = null; // use dafault value, is done in Check_Skymaps()
            Check_Skymaps(ref active_scenery_id, ref list_skymap_paths);

            // ui setup
            ui_loading_panel_flag = true;
            ui_loading_panel.transform.Find("Text Header").GetComponent<Text>().text = "Loading Skymap " + scenery_name + ".";

            //ui_loading_panel.transform.Find("Image").GetComponent<Image>()= "Loading Skymap " + list_skymap_paths[active_scenery_id].name + ".";
            Texture2D texture2D = Load_Image(list_skymap_paths[active_scenery_id].fullpath_preview_image);
            ui_loading_panel.transform.Find("Image").GetComponent<Image>().sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0, 0), 1);

            load_skymap_state = State_Load_Skymap.running;
        }
        // 0.)
        if (load_skymap_state == State_Load_Skymap.prepare_starting)
        {
            Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_callibration_loading_scenery.wav");

            load_skymap_state = State_Load_Skymap.starting;
        }
        // ##################################################################################




        // ##################################################################################
        // show / hide ui elemets
        // ##################################################################################
        ui_welcome_panel.gameObject.SetActive(ui_welcome_panel_flag);
        ui_info_panel.gameObject.SetActive(ui_info_panel_flag);
        ui_loading_panel.gameObject.SetActive(ui_loading_panel_flag);
        ui_debug_lines.gameObject.SetActive(ui_debug_panel_state > 0);
        ui_debug_panel.gameObject.SetActive(ui_debug_panel_state > 1);
        ui_exit_menu.gameObject.SetActive(ui_exit_panel_flag);
        ui_pause_menu.gameObject.SetActive(ui_pause_flag);
        ui_parameter_panel.gameObject.SetActive(ui_parameter_panel_flag);
        ui_controller_calibration_panel.gameObject.SetActive(ui_controller_calibration_flag);
        ui_no_controller_panel.gameObject.SetActive(ui_no_controller_panel_flag);
        ui_helicopter_selection_panel.gameObject.SetActive(ui_helicopter_selection_menu_flag);
        ui_scenery_selection_panel.gameObject.SetActive(ui_scenery_selection_menu_flag);
        ui_pie_menu.gameObject.SetActive(ui_pie_menu_flag);


        if (helicopter_ODE.par_temp.simulation.gameplay.show_fps.val == true && coroutine_frames_per_second_running == null)
        {
            ui_frames_per_sec_text.gameObject.SetActive(true);
            coroutine_frames_per_second_running = StartCoroutine(FramesPerSecond());
        }
        if (helicopter_ODE.par_temp.simulation.gameplay.show_fps.val == false)
        {
            ui_frames_per_sec_text.gameObject.SetActive(false);
            if (coroutine_frames_per_second_running != null)
            {
                StopCoroutine(coroutine_frames_per_second_running);
                coroutine_frames_per_second_running = null;
            }
        }
        // ##################################################################################




        // ##################################################################################
        // show / hide pilot
        // ##################################################################################
        pilot.SetActive(helicopter_ODE.par_temp.simulation.gameplay.show_pilot.val);
        // ##################################################################################




        // ##################################################################################
        // pause game if requested
        // ##################################################################################
        Pause_ODE(gl_pause_flag);
        // ##################################################################################



        // ##################################################################################
        // reset sim. if collision forces to hight
        // ##################################################################################
        if (helicopter_ODE.collision_force_too_high_flag)
        {
            Pause_ODE(gl_pause_flag = true);
            Crash_Play_Audio(Application.streamingAssetsPath + "/Audio/crash_audio_001.wav");
            Reset_Simulation_States();
            Pause_ODE(gl_pause_flag = false);

            helicopter_ODE.collision_force_too_high_flag = false;
        }
        // ##################################################################################



        // ##################################################################################
        // hide mouse cursor
        // ##################################################################################
        // https://stackoverflow.com/questions/37618140/how-to-make-cursor-dissapear-after-inactivity-in-unity
        if ((UnityEngine.InputSystem.Mouse.current.delta.ReadValue().x == 0 &&
            (UnityEngine.InputSystem.Mouse.current.delta.ReadValue().y == 0)) ||
            (UnityEngine.InputSystem.Mouse.current.middleButton.isPressed == true) ||
            (UnityEngine.InputSystem.Mouse.current.rightButton.isPressed == true))
        {
            if (co_hide_cursor == null)
                co_hide_cursor = StartCoroutine(Hide_Cursor());
        }
        else
        {
            if (co_hide_cursor != null)
            {
                StopCoroutine(co_hide_cursor);
                co_hide_cursor = null;
                Cursor.visible = true;
            }
        }
        // ##################################################################################



        // ##################################################################################
        // 
        // ##################################################################################
        if (XRSettings.enabled)
        {
            // if mouse is moved or right mouse is pressed, show selection menü
            if ( (Mathf.Abs(UnityEngine.InputSystem.Mouse.current.delta.ReadValue().x) > 30) ||
                 (Mathf.Abs(UnityEngine.InputSystem.Mouse.current.delta.ReadValue().y) > 30) || 
                 (UnityEngine.InputSystem.Mouse.current.rightButton.isPressed == true))
            { 
                // if all menues are closed, open pie menu
                if (ui_welcome_panel_flag == false &&
                 ui_info_panel_flag == false &&
                 ui_no_controller_panel_flag == false &&
                 ui_loading_panel_flag == false &&
                 ui_parameter_panel_flag == false &&
                 ui_pause_flag == false &&
                 ui_exit_panel_flag == false &&
                 ui_controller_calibration_flag == false &&
                 ui_helicopter_selection_menu_flag == false &&
                 ui_scenery_selection_menu_flag == false &&
                 ui_pie_menu_flag == false)
                {
                    ui_pie_menu_flag = true;
                    gl_pause_flag = true;
                }
            }


            if (ui_welcome_panel_flag == true ||
                ui_info_panel_flag == true ||
                ui_no_controller_panel_flag == true ||
                //ui_loading_panel_flag == true ||
                ui_parameter_panel_flag == true ||
                ui_pause_flag == true ||
                ui_exit_panel_flag == true ||
                ui_controller_calibration_flag == true ||
                ui_helicopter_selection_menu_flag == true ||
                ui_scenery_selection_menu_flag == true ||
                ui_pie_menu_flag == true)
            {
                ui_canvas_xr_mouse_arrow.SetActive(true); 
            }
            else
            {
                ui_canvas_xr_mouse_arrow.SetActive(false);
            }
        }
        else
        {
            ui_canvas_xr_mouse_arrow.SetActive(false);
        }
        // ##################################################################################





        // ##################################################################################
        // check if a joystick is still connected
        // ##################################################################################
        if (calibration_state == State_Calibration.not_running)
        {
            // get names of connected joysticks
            IO_Get_Connected_Gamepads_And_Joysticks();

            // UI setup
            if (connected_input_devices_count == 0)
            {
                gl_pause_flag = true;
                ui_no_controller_panel_flag = true;
            }
            else
            {
                // if number of "connected input devices" has changed from zero to >zero --> hide "no_controller_panel"
                if ((connected_input_devices_count != connected_input_devices_count_old) && connected_input_devices_count_old == 0)
                {
                    ui_no_controller_panel_flag = false;
                    if (ui_welcome_panel_flag == false &&
                        ui_info_panel_flag == false &&
                        ui_exit_panel_flag == false &&
                        ui_pause_flag == false &&
                        ui_parameter_panel_flag == false &&
                        ui_helicopter_selection_menu_flag == false &&
                        ui_scenery_selection_menu_flag == false && 
                        ui_pie_menu_flag == false)
                    {
                        gl_pause_flag = false;
                    }
                }
            }

            // if number of connected joysticks changed
            if (connected_input_devices_count != connected_input_devices_count_old)
            {
                //Init_Controller();
                Get_Connected_Controller();
            }

            // nedded to detect change in variable
            connected_input_devices_count_old = connected_input_devices_count;
        }
        // ##################################################################################




        // ##################################################################################
        // set inputs u[] and get outputs x[] from ODE-thread
        // ##################################################################################
        if (gl_controller_connected_flag && calibration_state == State_Calibration.not_running && gl_pause_flag == false)
        {

            // ##################################################################################
            // get controller input axes and apply scaling, expo and dualrate 
            // ##################################################################################
            u_inputs[0] = Expo_and_Dualrate(input_channel_used_in_game[stru_controller_settings_work.channel_collective], stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_collective].axis_settings);
            u_inputs[1] = Expo_and_Dualrate(input_channel_used_in_game[stru_controller_settings_work.channel_yaw], stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_yaw].axis_settings);
            u_inputs[2] = Expo_and_Dualrate(input_channel_used_in_game[stru_controller_settings_work.channel_pitch], stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_pitch].axis_settings);
            u_inputs[3] = Expo_and_Dualrate(input_channel_used_in_game[stru_controller_settings_work.channel_roll], stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_roll].axis_settings);

            // u_inputs[4] = 0; pusher propeller set by keys
            // u_inputs[5] = 0; booster set by keys
            u_inputs[6] = 0;
            u_inputs[7] = 0;
            // ##################################################################################


            // ##################################################################################
            // get controller input switches
            // ##################################################################################
            if (stru_controller_settings_work.channel_switch0 != -12345)
            {
                float switch_value = input_channel_used_in_game[stru_controller_settings_work.channel_switch0];
                float switch_state0 = stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_switch0].switch_settings.state0;
                float switch_state1 = stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_switch0].switch_settings.state1;
                int switch0_status = 0;

                if (switch_value > (switch_state0 - 0.05f) && switch_value < (switch_state0 + 0.05f))
                    switch0_status = 0;

                if (switch_value > (switch_state1 - 0.05f) && switch_value < (switch_state1 + 0.05f))
                    switch0_status = 1;

                if (helicopter_ODE.par.transmitter_and_helicopter.transmitter.switch0.type.val == 0) // 0=>switch_

                {
                    //UnityEngine.Debug.Log("aa switch0_status " + switch0_status);
                    if (switch0_status == 0) { helicopter_ODE.Stop_Motor(); }
                    if (switch0_status == 1) { helicopter_ODE.Start_Motor(); }
                }
                if (helicopter_ODE.par.transmitter_and_helicopter.transmitter.switch0.type.val == 1) // 1=>rising flank trigger
                {
                    //UnityEngine.Debug.Log("bb switch0_status " + switch0_status);
                    if (switch0_status > switch0_status_old) // detect rising flank
                        helicopter_ODE.Toggle_Start_Motor();
                }

                switch0_status_old = switch0_status;
            }


            if (stru_controller_settings_work.channel_switch1 != -12345)
            {
                float switch_value = input_channel_used_in_game[stru_controller_settings_work.channel_switch1];
                float switch_state0 = stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_switch1].switch_settings.state0;
                float switch_state1 = stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_switch1].switch_settings.state1;
                int switch1_status = 0;

                if (switch_value > (switch_state0 - 0.05f) && switch_value < (switch_state0 + 0.05f))
                    switch1_status = 0;

                if (switch_value > (switch_state1 - 0.05f) && switch_value < (switch_state1 + 0.05f))
                    switch1_status = 1;

                if (helicopter_ODE.par.transmitter_and_helicopter.transmitter.switch1.type.val == 0) // 0=>switch, 1=>rising flank trigger
                {
                    if (switch1_status == 0)
                    {
                        // if two animations are available for selected helicopter, then use this key for the wheels, else for opening/closing doors
                        if (animator_wheels_left != null && animator_wheels_right != null)
                        {
                            if (System.Array.Exists(animator_wheels_left.parameters, p => p.name == "Wheel_Status") &&
                                System.Array.Exists(animator_wheels_right.parameters, p => p.name == "Wheel_Status"))
                            {
                                wheels_status = Wheels_Status_Variants.lowered;
                                helicopter_ODE.wheel_status = (Helisimulator.Helicopter_ODE.Wheels_Status_Variants)wheels_status;
                            }
                            else if (animator_doors != null)
                            {
                                doors_status = Doors_Status_Variants.opened;
                            }
                        }
                        else if (animator_doors != null)
                        {
                            doors_status = Doors_Status_Variants.opened;
                        }
                    }
                    if (switch1_status == 1)
                    {
                        // if two animations are available for selected helicopter, then use this key for the wheels, else for opening/closing doors
                        if (animator_wheels_left != null && animator_wheels_right != null)
                        {
                            if (System.Array.Exists(animator_wheels_left.parameters, p => p.name == "Wheel_Status") &&
                                System.Array.Exists(animator_wheels_right.parameters, p => p.name == "Wheel_Status"))
                            {
                                wheels_status ^= Wheels_Status_Variants.raised;

                                if (System.Array.Exists(animator_wheels_left.parameters, p => p.name == "Wheel_Only_Allowed_Lowered"))
                                    wheels_status = Wheels_Status_Variants.lowered;

                                helicopter_ODE.wheel_status = (Helisimulator.Helicopter_ODE.Wheels_Status_Variants)wheels_status;
                            }
                            else if (animator_doors != null)
                            {
                                doors_status = Doors_Status_Variants.closed;
                            }
                        }
                        else if (animator_doors != null)
                        {
                            doors_status = Doors_Status_Variants.closed;
                        }
                    }
                }
                if (helicopter_ODE.par.transmitter_and_helicopter.transmitter.switch1.type.val == 1) // 0=>switch, 1=>rising flank trigger
                {
                    if (switch1_status > switch1_status_old) // detect rising flank
                    {
                        // if two animations are available for selected helicopter, then use this key for the wheels, else for opening/closing doors
                        if (animator_wheels_left != null && animator_wheels_right != null)
                        {
                            if (System.Array.Exists(animator_wheels_left.parameters, p => p.name == "Wheel_Status") &&
                                System.Array.Exists(animator_wheels_right.parameters, p => p.name == "Wheel_Status"))
                            {
                                if (animator_wheels_left != null && animator_wheels_right != null)
                                {
                                    wheels_status ^= Wheels_Status_Variants.raised;

                                    if (System.Array.Exists(animator_wheels_left.parameters, p => p.name == "Wheel_Only_Allowed_Lowered"))
                                        wheels_status = Wheels_Status_Variants.lowered;

                                    helicopter_ODE.wheel_status = (Helisimulator.Helicopter_ODE.Wheels_Status_Variants)wheels_status;
                                }
                            }
                            else if (animator_doors != null)
                            {
                                doors_status ^= Doors_Status_Variants.opened;
                            }
                        }
                        else if (animator_doors != null)
                        {
                            doors_status ^= Doors_Status_Variants.opened;
                        }
                    }
                }

                switch1_status_old = switch1_status;
            }
            // ##################################################################################




            // ##################################################################################
            // wheel animation 
            // ##################################################################################
            if (animator_wheels_left != null && animator_wheels_right != null)
            {
                // lowering or rising landing gear
                if (wheels_status == Wheels_Status_Variants.raised && wheels_status_old == Wheels_Status_Variants.lowered) // rising flank
                {
                    if (System.Array.Exists(animator_wheels_left.parameters, p => p.name == "Wheel_Status"))
                    {
                        animator_wheels_left.ResetTrigger("Wheel_Status"); // 0 triggers transition up -> rised
                        collision_positions_landing_gear_left_rising_offset_target = 1.0f; // helicopter_ODE.par.transmitter_and_helicopter.helicopter.collision.positions_left_rised_offset.val; //[0...1]    
                    }
                    if (System.Array.Exists(animator_wheels_right.parameters, p => p.name == "Wheel_Status"))
                    {
                        animator_wheels_right.ResetTrigger("Wheel_Status"); // 0 triggers transition up -> rised
                        collision_positions_landing_gear_right_rising_offset_target = 1.0f; //helicopter_ODE.par.transmitter_and_helicopter.helicopter.collision.positions_right_rised_offset.val; //[0...1]    
                    }
                    if (animator_wheels_steering_center != null && System.Array.Exists(animator_wheels_steering_center.parameters, p => p.name == "Wheel_Status"))
                    {
                        helicopter_ODE.wheel_steering_center_lock_to_initial_direction = true;

                        animator_wheels_steering_center.ResetTrigger("Wheel_Status"); // 0 triggers transition up -> rised
                        collision_positions_landing_gear_steering_center_rising_offset_target = 1.0f; // helicopter_ODE.par.transmitter_and_helicopter.helicopter.collision.positions_steering_center_rised_offset.val; //[0...1] 
                    }
                    if (animator_wheels_steering_left != null && System.Array.Exists(animator_wheels_steering_left.parameters, p => p.name == "Wheel_Status"))
                    {
                        helicopter_ODE.wheel_steering_left_lock_to_initial_direction = true;

                        animator_wheels_steering_left.ResetTrigger("Wheel_Status"); // 0 triggers transition up -> rised
                        collision_positions_landing_gear_steering_left_rising_offset_target = 1.0f; // helicopter_ODE.par.transmitter_and_helicopter.helicopter.collision.positions_steering_left_rised_offset.val; //[0...1] 
                    }
                    if (animator_wheels_steering_right != null && System.Array.Exists(animator_wheels_steering_right.parameters, p => p.name == "Wheel_Status"))
                    {
                        helicopter_ODE.wheel_steering_right_lock_to_initial_direction = true;

                        animator_wheels_steering_right.ResetTrigger("Wheel_Status"); // 0 triggers transition up -> rised
                        collision_positions_landing_gear_steering_right_rising_offset_target = 1.0f; // helicopter_ODE.par.transmitter_and_helicopter.helicopter.collision.positions_steering_left_rised_offset.val; //[0...1] 
                    }
                }
                if (wheels_status == Wheels_Status_Variants.lowered && wheels_status_old == Wheels_Status_Variants.raised) // falling flank
                {
                    if (System.Array.Exists(animator_wheels_left.parameters, p => p.name == "Wheel_Status"))
                    {
                        animator_wheels_left.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                        collision_positions_landing_gear_left_rising_offset_target = 0.0f; //[0...1]
                    }
                    if (System.Array.Exists(animator_wheels_right.parameters, p => p.name == "Wheel_Status"))
                    {
                        animator_wheels_right.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                        collision_positions_landing_gear_right_rising_offset_target = 0.0f; //[0...1]
                    }
                    if (animator_wheels_steering_center != null && System.Array.Exists(animator_wheels_steering_center.parameters, p => p.name == "Wheel_Status"))
                    {
                        helicopter_ODE.wheel_steering_center_lock_to_initial_direction = false;

                        animator_wheels_steering_center.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                        collision_positions_landing_gear_steering_center_rising_offset_target = 0.0f; //[0...1]
                    }
                    if (animator_wheels_steering_left != null && System.Array.Exists(animator_wheels_steering_left.parameters, p => p.name == "Wheel_Status"))
                    {
                        helicopter_ODE.wheel_steering_left_lock_to_initial_direction = false;

                        animator_wheels_steering_left.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                        collision_positions_landing_gear_steering_left_rising_offset_target = 0.0f; //[0...1]
                    }
                    if (animator_wheels_steering_right != null && System.Array.Exists(animator_wheels_steering_right.parameters, p => p.name == "Wheel_Status"))
                    {
                        helicopter_ODE.wheel_steering_right_lock_to_initial_direction = false;

                        animator_wheels_steering_right.SetTrigger("Wheel_Status"); // 1 triggers transition down -> lowered
                        collision_positions_landing_gear_steering_right_rising_offset_target = 0.0f; //[0...1]
                    }
                }
                wheels_status_old = wheels_status;

                // add an y-offset to the collision points for the landing gear collision detection, if the landing gear gets raised.
                float smoothTime; // [s]
                smoothTime = helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_gear.val + helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_bay.val;
                helicopter_ODE.collision_positions_landing_gear_left_rising_offset = Mathf.SmoothDamp(helicopter_ODE.collision_positions_landing_gear_left_rising_offset, collision_positions_landing_gear_left_rising_offset_target, ref collision_positions_landing_gear_left_rising_offset_velocity, smoothTime);
                smoothTime = helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_gear.val + helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_bay.val;
                helicopter_ODE.collision_positions_landing_gear_right_rising_offset = Mathf.SmoothDamp(helicopter_ODE.collision_positions_landing_gear_right_rising_offset, collision_positions_landing_gear_right_rising_offset_target, ref collision_positions_landing_gear_right_rising_offset_velocity, smoothTime);
                smoothTime = helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_gear.val + helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_bay.val; // todo TAIL-wheel
                helicopter_ODE.collision_positions_landing_gear_steering_center_rising_offset = Mathf.SmoothDamp(helicopter_ODE.collision_positions_landing_gear_steering_center_rising_offset, collision_positions_landing_gear_steering_center_rising_offset_target, ref collision_positions_landing_gear_steering_center_rising_offset_velocity, smoothTime);
                smoothTime = helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_gear.val + helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_bay.val; // todo TAIL-wheel
                helicopter_ODE.collision_positions_landing_gear_steering_left_rising_offset = Mathf.SmoothDamp(helicopter_ODE.collision_positions_landing_gear_steering_left_rising_offset, collision_positions_landing_gear_steering_left_rising_offset_target, ref collision_positions_landing_gear_steering_left_rising_offset_velocity, smoothTime);
                smoothTime = helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_gear.val + helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_transition_time_bay.val; // todo TAIL-wheel
                helicopter_ODE.collision_positions_landing_gear_steering_right_rising_offset = Mathf.SmoothDamp(helicopter_ODE.collision_positions_landing_gear_steering_right_rising_offset, collision_positions_landing_gear_steering_right_rising_offset_target, ref collision_positions_landing_gear_steering_right_rising_offset_velocity, smoothTime);


                // elastic deflection of the landing gears or the skids on left or right side
                float stiffness_normalized = helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_or_skids_deflection_stiffness.val;
                if (stiffness_normalized > 0)
                {
                    if (animator_wheels_left != null)
                    {
                        float deflection_left = Mathf.Abs(helicopter_ODE.force_y_gear_or_skid_leftLH / stiffness_normalized);
                        float deflection_left_filtered = (float)exponential_moving_average_filter_for_wheels_left_deflection.Calculate(5, (double)deflection_left);
                        animator_wheels_left.SetFloat("Wheel_Deflection", deflection_left_filtered);
                    }
                    if (animator_wheels_right != null)
                    {
                        float deflection_right = Mathf.Abs(helicopter_ODE.force_y_gear_or_skid_rightLH / stiffness_normalized);
                        float deflection_right_filtered = (float)exponential_moving_average_filter_for_wheels_right_deflection.Calculate(5, (double)deflection_right);
                        animator_wheels_right.SetFloat("Wheel_Deflection", deflection_right_filtered);
                    }

                    if (animator_wheels_steering_center != null)
                    {
                        float deflection_steering_center = Mathf.Abs(helicopter_ODE.force_y_gear_or_support_steering_centerLH / stiffness_normalized);
                        float deflection_steering_center_filtered = (float)exponential_moving_average_filter_for_wheels_steering_center_deflection.Calculate(5, (double)deflection_steering_center);
                        animator_wheels_steering_center.SetFloat("Wheel_Deflection", deflection_steering_center_filtered);
                    }
                    if (animator_wheels_steering_left != null)
                    {
                        float deflection_steering_left = Mathf.Abs(helicopter_ODE.force_y_gear_or_support_steering_leftLH / stiffness_normalized);
                        float deflection_steering_left_filtered = (float)exponential_moving_average_filter_for_wheels_steering_left_deflection.Calculate(5, (double)deflection_steering_left);
                        animator_wheels_steering_left.SetFloat("Wheel_Deflection", deflection_steering_left_filtered);
                    }
                    if (animator_wheels_steering_right != null)
                    {
                        float deflection_steering_right = Mathf.Abs(helicopter_ODE.force_y_gear_or_support_steering_rightLH / stiffness_normalized);
                        float deflection_steering_right_filtered = (float)exponential_moving_average_filter_for_wheels_steering_right_deflection.Calculate(5, (double)deflection_steering_right);
                        animator_wheels_steering_right.SetFloat("Wheel_Deflection", deflection_steering_right_filtered);
                    }
                }

                // wheel/gear rotation 
                float wheel_radius = helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.landing_gear_main_radius.val; //[m] TODO seams that diameter has to be entered here, but why???
                if (wheel_radius > 0)
                {
                    if (helicopter_ODE.par.transmitter_and_helicopter.helicopter.collision.positions_left_type.val == 1) // [0:skids, 1:gear] 
                        animator_wheels_left.SetFloat("Wheel_Rolling", Mathf.Abs(((helicopter_ODE.wheel_rolling_distance_left - 10000) / ((2 * wheel_radius * Mathf.PI)))) % 1.0f);
                    if (helicopter_ODE.par.transmitter_and_helicopter.helicopter.collision.positions_right_type.val == 1) // [0:skids, 1:gear] 
                        animator_wheels_right.SetFloat("Wheel_Rolling", Mathf.Abs(((helicopter_ODE.wheel_rolling_distance_right - 10000) / ((2 * wheel_radius * Mathf.PI)))) % 1.0f);
                    if (animator_wheels_steering_center != null)
                        animator_wheels_steering_center.SetFloat("Wheel_Rolling", Mathf.Abs(((helicopter_ODE.wheel_rolling_distance_steering_center - 10000) / ((2 * wheel_radius * Mathf.PI)))) % 1.0f);
                    if (animator_wheels_steering_left != null)
                        animator_wheels_steering_left.SetFloat("Wheel_Rolling", Mathf.Abs(((helicopter_ODE.wheel_rolling_distance_steering_left - 10000) / ((2 * wheel_radius * Mathf.PI)))) % 1.0f);
                    if (animator_wheels_steering_right != null)
                        animator_wheels_steering_right.SetFloat("Wheel_Rolling", Mathf.Abs(((helicopter_ODE.wheel_rolling_distance_steering_right - 10000) / ((2 * wheel_radius * Mathf.PI)))) % 1.0f);
                }

                // center wheel/gear automatic steering
                //UnityEngine.Debug.Log("wheel_steering_center" + helicopter_ODE.wheel_steering_center);
                if (animator_wheels_steering_center != null)
                {
                    float steering_direction = (1 - (helicopter_ODE.wheel_steering_center - 0.25f)) % 1; // [0..1]  // 0.25 bacause of 90° offset, 1- because of steering direction 
                    float steering_direction_filtered = (float)exponential_moving_average_filter_for_rotations_for_steering_wheel_center.Calculate(30, (double)steering_direction * 360) / 360;
                    animator_wheels_steering_center.SetFloat("Wheel_Steering", steering_direction_filtered);
                }
                // left wheel/gear automatic steering
                //UnityEngine.Debug.Log("wheel_steering_left" + helicopter_ODE.wheel_steering_left);
                if (animator_wheels_steering_left != null)
                {
                    float steering_direction = (1 - (helicopter_ODE.wheel_steering_left - 0.25f)) % 1; // [0..1]  // 0.25 bacause of 90° offset, 1- because of steering direction 
                    float steering_direction_filtered = (float)exponential_moving_average_filter_for_rotations_for_steering_wheel_left.Calculate(30, (double)steering_direction * 360) / 360;
                    animator_wheels_steering_left.SetFloat("Wheel_Steering", steering_direction_filtered);
                }
                // right wheel/gear automatic steering
                //UnityEngine.Debug.Log("wheel_steering_right" + helicopter_ODE.wheel_steering_right);
                if (animator_wheels_steering_right != null)
                {
                    float steering_direction = (1 - (helicopter_ODE.wheel_steering_right - 0.25f)) % 1; // [0..1]  // 0.25 bacause of 90° offset, 1- because of steering direction 
                    float steering_direction_filtered = (float)exponential_moving_average_filter_for_rotations_for_steering_wheel_right.Calculate(30, (double)steering_direction * 360) / 360;
                    animator_wheels_steering_right.SetFloat("Wheel_Steering", steering_direction_filtered);
                }

            }
            // ##################################################################################



            // ##################################################################################
            // doors animation 
            // ##################################################################################
            if (animator_doors != null)
            {
                // front doors
                if (doors_status == Doors_Status_Variants.closed && doors_status_old == Doors_Status_Variants.opened) // rising flank
                {
                    animator_doors.ResetTrigger("DoorsStatus"); // 0 triggers transittion up -> rised

                    //if(animator_special_animation != null) animator_special_animation.ResetTrigger("Status"); 
                }
                if (doors_status == Doors_Status_Variants.opened && doors_status_old == Doors_Status_Variants.closed) // falling flank
                {
                    animator_doors.SetTrigger("DoorsStatus"); // 1 triggers transittion down -> lowered

                    //if (animator_special_animation != null) animator_special_animation.SetTrigger("Status");
                }
                doors_status_old = doors_status;

                // rear doors
                if (doors_status_002 == Doors_Status_Variants.closed && doors_status_002_old == Doors_Status_Variants.opened) // rising flank
                {
                    animator_doors.ResetTrigger("DoorsRearStatus"); // 0 triggers transittion up -> rised

                    if (animator_special_animation != null) animator_special_animation.ResetTrigger("Status"); // also trigges special animations (ie.e foldable ladder)
                }
                if (doors_status_002 == Doors_Status_Variants.opened && doors_status_002_old == Doors_Status_Variants.closed) // falling flank
                {
                    animator_doors.SetTrigger("DoorsRearStatus"); // 1 triggers transittion down -> lowered

                    if (animator_special_animation != null) animator_special_animation.SetTrigger("Status"); // also trigges special animations (ie.e foldable ladder)
                }
                doors_status_002_old = doors_status_002;
            }
            // ##################################################################################



            // ##################################################################################
            // Missile https://roystanross.wordpress.com/beginnertutorialpart9/
            // ##################################################################################
            if (helicopter_setup_missile_gameobject != null && (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame && !Is_Text_Input_Field_Focused() && !ui_parameter_panel_flag))
            {
                list_helicopter_setup_missile_pylon_localposition_current_active++;
                list_helicopter_setup_missile_pylon_localposition_current_active %= list_helicopter_setup_missile_pylon_localposition.Count();

                Vector3 pylon_localposition = list_helicopter_setup_missile_pylon_localposition[list_helicopter_setup_missile_pylon_localposition_current_active].localPosition;
                //UnityEngine.Debug.Log(" p : " + Quaternion.Euler(0, 90, 0) * pylon_localposition);

                GameObject go = (GameObject)Instantiate(helicopter_setup_missile_gameobject,
                    (helicopters_available.transform.TransformPoint(Quaternion.Euler(0, 0, 0) * pylon_localposition)),
                    Quaternion.LookRotation(helicopters_available.transform.right));

                Rigidbody rb = go.transform.Find("Missile_HOT3").gameObject.GetComponent<Rigidbody>();

                rb.velocity = Helper.ConvertRightHandedToLeftHandedVector(helicopter_ODE.Translational_Velocity_At_Local_Point_Expressed_In_Global_Frame_RightHanded(Helper.ConvertLeftHandedToRightHandedVector(Quaternion.Euler(0, 0, 0) * pylon_localposition)));
                rb.angularVelocity = Helper.ConvertRightHandedToLeftHandedVector(helicopter_ODE.Rotational_Velocity_Expressed_In_Global_Frame_RightHanded());
                rb.angularVelocity = new Vector3(-rb.angularVelocity.x, -rb.angularVelocity.y, -rb.angularVelocity.z); // TODO Why??? maybe Lefthanded vs Righthanded velocity     ???
                //rb.AddRelativeForce(Vector3.right * 5f, ForceMode.Force);

                //Physics.IgnoreCollision(GetComponent<Collider>(), go.GetComponent<Collider>());

                Destroy(go, 6.0f);
            }
            // ##################################################################################



            // ##################################################################################
            // map values from ODE to Unity (ODE:right handed, Unity:left handed)
            // ##################################################################################
           /* if (QualitySettings.vSyncCount == 0)
            {
                // <==== moved to IO_AntiStutter__Set_Transform()               
                position = helicopters_available.transform.position;
                position.x = (float)helicopter_ODE.x_states[0]; // [m] x in reference frame
                position.y = (float)helicopter_ODE.x_states[1]; // [m] y in reference frame
                position.z = (float)helicopter_ODE.x_states[2]; // [m] z in reference frame
                position = Helper.ConvertRightHandedToLeftHandedVector(position);
                helicopters_available.transform.position = position;

                rotation = helicopters_available.transform.rotation;
                rotation.w = (float)helicopter_ODE.x_states[3]; // [-] w
                rotation.x = (float)helicopter_ODE.x_states[4]; // [-] x
                rotation.y = (float)helicopter_ODE.x_states[5]; // [-] y
                rotation.z = (float)helicopter_ODE.x_states[6]; // [-] z
                rotation = Helper.ConvertRightHandedToLeftHandedQuaternion(rotation);
                helicopters_available.transform.rotation = rotation;
                // <==== moved to IO_AntiStutter__Set_Transform() 
            }
            else
            {*/
                IO_AntiStutter__Set_Transform();
            // }


            velocity = new Vector3
            {
                x = (float)helicopter_ODE.x_states[7], // [m] dxdt in reference frame
                y = (float)helicopter_ODE.x_states[8], // [m] dydt in reference frame
                z = (float)helicopter_ODE.x_states[9] // [m] dzdt in reference frame
            };
            velocity = Helper.ConvertRightHandedToLeftHandedVector(velocity);

            // mainrotor flapping
            flapping_a_s_mr_L = (float)helicopter_ODE.x_states[13];  // [rad] mainrotor pitch flapping angle a_s (longitudial direction)
            flapping_b_s_mr_L = (float)helicopter_ODE.x_states[14];  // [rad] mainrotor roll flapping angle b_s (lateral direction)

            // tailrotor flapping
            flapping_a_s_tr_L = (float)helicopter_ODE.x_states[15];  // [rad] tailrotor pitch flapping angle a_s (longitudial direction) - used for tandem rotor setup
            flapping_b_s_tr_L = (float)helicopter_ODE.x_states[16];  // [rad] tailrotor roll flapping angle b_s (lateral direction) - used for tandem rotor setup

            // drivetrain
            current_mo = (float)helicopter_ODE.I_mo; // [A]
            omega_mo = (float)helicopter_ODE.x_states[17] * RIGHT2LEFT_HANDED; // [rad/sec] brushless motor rotation speed
            omega_mr = (float)helicopter_ODE.x_states[19] * RIGHT2LEFT_HANDED; // [rad/sec] mainrotor rotation speed 
            omega_tr = ((float)helicopter_ODE.x_states[19] / helicopter_ODE.par.transmitter_and_helicopter.helicopter.transmission.n_mr2tr.val) * RIGHT2LEFT_HANDED; // [rad/sec] tailrotor rotation speed 
            omega_pr = ((float)helicopter_ODE.x_states[19] / helicopter_ODE.par.transmitter_and_helicopter.helicopter.transmission.n_mr2pr.val) * RIGHT2LEFT_HANDED; // [rad/sec] propeller rotation speed 

            Omega_mo_ += (float)helicopter_ODE.x_states[17] * Time.deltaTime * RIGHT2LEFT_HANDED; // [rad]      [rad/sec] brushless motor rotational speed, integrated here. Do not use x_states[18]==Omega_mo, because during freewheeling motor angle Omega_mo (times gearbox) is set to mainrotor rotation angle.
            Omega_mr = (float)helicopter_ODE.x_states[20] * RIGHT2LEFT_HANDED; // [rad] mainrotor rotation angle
            Omega_tr = ((float)helicopter_ODE.x_states[20] / helicopter_ODE.par.transmitter_and_helicopter.helicopter.transmission.n_mr2tr.val) * RIGHT2LEFT_HANDED; // [rad] tailrotor rotation angle 
            Omega_pr = ((float)helicopter_ODE.x_states[20] / helicopter_ODE.par.transmitter_and_helicopter.helicopter.transmission.n_mr2pr.val) * RIGHT2LEFT_HANDED; // [rad] propeller rotation angle 



            //rotation = helicopters_available.transform.rotation;
            //rotation.w = (float)helicopter_ODE.x_states[3]; // [-] w
            //rotation.x = (float)helicopter_ODE.x_states[4]; // [-] x
            //rotation.y = (float)helicopter_ODE.x_states[5]; // [-] y
            //rotation.z = (float)helicopter_ODE.x_states[6]; // [-] z
            //rotation = Helper.ConvertRightHandedToLeftHandedQuaternion(rotation);
            //helicopters_available.transform.rotation = rotation;

            //mainrotor_object.Update_Rotor_Flapping_BEMT();

            // ##################################################################################
        }
        else
        {
            u_inputs[0] = 0; u_inputs[1] = 0; u_inputs[2] = 0; u_inputs[3] = 0; u_inputs[4] = 0; u_inputs[5] = 0; u_inputs[6] = 0; u_inputs[7] = 0;
            omega_mr = 0;  omega_tr = 0;  omega_pr = 0;
            //Omega_mr = 0;  Omega_tr = 0;  Omega_pr = 0;
            flapping_a_s_mr_L = 0; flapping_b_s_mr_L = 0; flapping_a_s_tr_L = 0; flapping_b_s_tr_L = 0;
        }
        // ##################################################################################





        // ##################################################################################
        // pilot with rc transmitter - change orientation
        // ##################################################################################
        float pilot_rotation_angle_around_y = pilot.transform.eulerAngles.y; // [deg]
        float camera_rotation_angle_around_y = main_camera.transform.eulerAngles.y; // [deg]
        float pilot_follow_angle_rotation_speed = 5f;
        float pilot_follow_angle = 80f;  // [deg]

        // if camera rotates around y-axis too much, then let the pilots's body follow the camera/head.
        if ((camera_rotation_angle_around_y < pilot_follow_angle && camera_rotation_angle_around_y >= 0) || (camera_rotation_angle_around_y > (360f - pilot_follow_angle) && camera_rotation_angle_around_y <= 360f))
        {
            pilot_follow_angle_rotation_speed = 0.5f;
            pilot.transform.rotation = Quaternion.Slerp(pilot.transform.rotation, Quaternion.AngleAxis(0, Vector3.up), Time.deltaTime * pilot_follow_angle_rotation_speed);
        }
        else
        {
            pilot_follow_angle_rotation_speed = 3f;
            pilot.transform.rotation = Quaternion.Slerp(pilot.transform.rotation, main_camera.transform.rotation, Time.deltaTime * pilot_follow_angle_rotation_speed);
        }
        pilot.transform.eulerAngles = new Vector3(0, pilot.transform.eulerAngles.y, 0);

        // center the pilot's head at camera position 
        pilot.transform.position = new Vector3(main_camera.transform.position.x, main_camera.transform.position.y - helicopter_ODE.par.scenery.camera_height.val, main_camera.transform.position.z);
        // ##################################################################################





        // ##################################################################################
        // animation of small pilot-figure, sitting in helicopter 
        // ##################################################################################
        if (animator_pilot != null)
        {
            const float increase_movement = 1.5f;
            float collective = Mathf.Clamp((float)u_inputs[0] * increase_movement, -0.99f, 0.99f); // u[0] collective
            float yaw_left_right = Mathf.Clamp((float)u_inputs[1] * increase_movement, -0.99f, 0.99f); // u[1] channel_yaw
            float stick_axis_forward_backward = Mathf.Clamp((float)u_inputs[2] * increase_movement, -0.99f, 0.99f); // u[2] channel_pitch
            float stick_axis_left_right = Mathf.Clamp((float)u_inputs[3] * increase_movement, -0.99f, 0.99f); // u[3] channel_roll

            // stick - cross movements are mixed in Unity's "Animator" by using layers, setting weight to 1 and blending to additive 
            if (stick_axis_forward_backward >= 0)
            {
                animator_pilot.SetFloat("stick_backward", Mathf.Abs(stick_axis_forward_backward));
                animator_pilot.SetFloat("stick_forward", 0);
            }
            if (stick_axis_forward_backward < 0)
            {
                animator_pilot.SetFloat("stick_backward", 0);
                animator_pilot.SetFloat("stick_forward", Mathf.Abs(stick_axis_forward_backward));
            }
            if (stick_axis_left_right >= 0)
            {
                animator_pilot.SetFloat("stick_right", Mathf.Abs(stick_axis_left_right));
                animator_pilot.SetFloat("stick_left", 0);
            }
            if (stick_axis_left_right < 0)
            {
                animator_pilot.SetFloat("stick_right", 0);
                animator_pilot.SetFloat("stick_left", Mathf.Abs(stick_axis_left_right));
            }

            // pedals
            if (yaw_left_right >= 0)
            {
                animator_pilot.SetFloat("yaw_left", Mathf.Abs(yaw_left_right));
                animator_pilot.SetFloat("yaw_right", 0);
            }
            if (yaw_left_right < 0)
            {
                animator_pilot.SetFloat("yaw_left", 0);
                animator_pilot.SetFloat("yaw_right", Mathf.Abs(yaw_left_right));
            }

            // collective 
            animator_pilot.SetFloat("collective", Mathf.Abs((collective / 2.0f) + 0.5f));
        }
        // ##################################################################################






        // ##################################################################################
        // sun / eye adaption (faked by bloom effect)
        // ##################################################################################
        // is camera looking into sun ? --> value goes --> 1.0
        float scalarproduct = Vector3.Dot(main_camera.transform.forward.normalized, directional_light.transform.position.normalized); // (dot product, projection of main rotor axis to global y ) * velocity in y direction
        // handling and changing
        float bloom_layer_intensity = Common.Helper.Step(scalarproduct, 0.95f, helicopter_ODE.par.scenery.lighting.sun_bloom_intensity.val, 0.98f, helicopter_ODE.par.scenery.lighting.sun_bloom_blinded_by_sun_intensity.val);
        // change bloom
        if (bloom_layer_intensity != bloom_layer_intensity_old)
        {
            if (bloom_layer != null)
            {
                bloom_layer.enabled.value = true;
                bloom_layer.threshold.value = 1f;
                bloom_layer.intensity.value = bloom_layer_intensity;
                bloom_layer_intensity_old = bloom_layer_intensity;
            }
        }
        // ##################################################################################




        // ##################################################################################
        // update rotor mechanics model
        // ##################################################################################
        Helicopter_Mainrotor_Mechanics.Update_3D_Objects();
        // ##################################################################################



        // ##################################################################################
        // update rotor mechanics model
        // ##################################################################################
        Helicopter_Tailrotor_Mechanics.Update_3D_Objects();
        // ##################################################################################



        // ##################################################################################
        // rotate rotor model (shaft with blades)
        // ##################################################################################
        mainrotor_object.Update_Rotor_Rotation(helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor, ref Omega_mr);
        tailrotor_object.Update_Rotor_Rotation(helicopter_ODE.par.transmitter_and_helicopter.helicopter.tailrotor, ref Omega_tr);
        propeller_object.Update_Rotor_Rotation(helicopter_ODE.par.transmitter_and_helicopter.helicopter.propeller, ref Omega_pr);
        // ##################################################################################




        // ##################################################################################
        // change rotor visiblitiy as a function of rotation velocity
        // ##################################################################################
        mainrotor_object.Update_Rotor_Visiblitiy(ref helicopter_ODE, helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor, (float)helicopter_ODE.Theta_col_mr, ref omega_mr);
        tailrotor_object.Update_Rotor_Visiblitiy(ref helicopter_ODE, helicopter_ODE.par.transmitter_and_helicopter.helicopter.tailrotor, (float)helicopter_ODE.Theta_col_tr, ref omega_tr);
        propeller_object.Update_Rotor_Visiblitiy(ref helicopter_ODE, helicopter_ODE.par.transmitter_and_helicopter.helicopter.propeller, (float)helicopter_ODE.Theta_col_pr, ref omega_pr);
        // ##################################################################################




        // ##################################################################################
        // brushless motor and maingear rotation
        // ##################################################################################
        if (motor_to_rotate != null)
        {
            motor_to_rotate.transform.localRotation = Quaternion.AngleAxis(Omega_mo_ * Mathf.Rad2Deg, Helper.ConvertRightHandedToLeftHandedVector(Vector3.up)) *
                  (Helper.ConvertRightHandedToLeftHandedQuaternion(Helper.S123toQuat(new Vector3 (0,0,180) ))); // [deg]
        }
        if (maingear_to_rotate != null)
        {
            maingear_to_rotate.transform.localRotation = Quaternion.AngleAxis(Omega_mo_ * helicopter_ODE.par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val * Mathf.Rad2Deg, Helper.ConvertRightHandedToLeftHandedVector(helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor.dirLH.vect3)) *
                  (Helper.ConvertRightHandedToLeftHandedQuaternion(Helper.S123toQuat(helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor.oriLH.vect3))); // [deg]
        }
        // ##################################################################################




        // ##################################################################################
        // heli- and stall sound 
        // ##################################################################################
        mainrotor_object.Update_Rotor_Audio(0, ref helicopter_ODE, ref helicopters_available, helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor, ref omega_mr, position, velocity, helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor_sound_recorded_rpm.val, helicopter_ODE.sound_volume_mainrotor, helicopter_ODE.sound_volume_mainrotor_stall, gl_pause_flag);
        tailrotor_object.Update_Rotor_Audio(1, ref helicopter_ODE, ref helicopters_available, helicopter_ODE.par.transmitter_and_helicopter.helicopter.tailrotor, ref omega_tr, position, velocity, helicopter_ODE.par.transmitter_and_helicopter.helicopter.tailrotor_sound_recorded_rpm.val, helicopter_ODE.sound_volume_tailrotor, helicopter_ODE.sound_volume_tailrotor_stall, gl_pause_flag);
        //propeller_object.Update_Rotor_Audio(ref helicopter_ODE,  ref omega_pr, position, velocity, gl_pause_flag);
        // ##################################################################################




        // ##################################################################################--
        // motor sound
        // ##################################################################################
        audio_source_motor.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic; // unity's doppler effect 

        audio_source_motor.pitch = Mathf.Abs((omega_mo / (helicopter_ODE.par.transmitter_and_helicopter.helicopter.transmission.n_mo2mr.val)) / (helicopter_ODE.par.transmitter_and_helicopter.helicopter.motor_sound_recorded_rpm.val * 6 * Mathf.Deg2Rad));
        audio_source_motor.volume = audio_source_motor.pitch * Helper.Step(Mathf.Abs(current_mo), 0, 0.700000000000f, 30.000000000000f, 1.0f ); // TODO into parameter

        // unity's doppler effect still sounds weird in build therefore a custum equation is implemented here (doppler effect has to be set to 0 in unity)
        float velocity_for_doppler_effect = Vector3.Dot(-position.normalized, velocity); // helicopters velocity vector component pointing to camera
        const float speed_of_sound = 343; // [m / s]
        audio_source_motor.pitch *= (speed_of_sound + 0f) / (speed_of_sound - velocity_for_doppler_effect);

        // conical volume control
        Vector3 audio_cone_direction_motor_L = new Vector3(1f, 0.2f, 0f); // in heli's local coordinate system - L, (left handed)        TODO into paramerter
        var audio_cone_direction_motor_R = helicopters_available.transform.TransformDirection(audio_cone_direction_motor_L); // world coordiante system - R, (left handed)
        float audio_cone_angle = Vector3.Angle(-helicopters_available.transform.position, audio_cone_direction_motor_R); // [deg] angle between two vector
        float audio_volume = Helper.Step(audio_cone_angle, 30f, 0.30f, 75f, 1.0f); // smooth transition  TODO into paramerter
        audio_source_motor.volume *= audio_volume;

        // reduce motor volume if motor is turned off
        if (helicopter_ODE.flag_motor_enabled)
        {
            if (audio_source_motor_smooth_transition < 1.0f)
                audio_source_motor_smooth_transition += 0.05f;  // smooth transition between on and off state (here on)
        }
        else
        {
            if (audio_source_motor_smooth_transition > 0.4f)
                audio_source_motor_smooth_transition -= 0.005f;  // smooth transition between on and off state (here off)  
        }
        audio_source_motor.volume *= audio_source_motor_smooth_transition;
        
        // limit sound by master volume settings
        float pause_activated_reduces_audio_volume = gl_pause_flag ? 0.0f : 1.0f;
        audio_source_motor.volume *= (helicopter_ODE.par.transmitter_and_helicopter.helicopter.sound_volume.val / 100f) * pause_activated_reduces_audio_volume * (helicopter_ODE.par.simulation.audio.master_sound_volume.val / 100f);
        
        ambient_audio_source.volume = (helicopter_ODE.par.scenery.ambient_sound_volume.val / 100f) * (helicopter_ODE.par.simulation.audio.master_sound_volume.val / 100f);  //  * opened_parameter_menu_reduced_audio_volume
        // ##################################################################################


        // ##################################################################################
        // servo sound
        // ##################################################################################
        if (Helicopter_Mainrotor_Mechanics.rotor_3d_mechanics_geometry_available == true)
        {
            // mainrotor servos (x3)
            for (int i = 0; i < 3; i++)
            {
                servo_sigma[i] = Helicopter_Mainrotor_Mechanics.servo_sigma[i]; // [rad]
                float servo_speed = (servo_sigma_old[i] - servo_sigma[i]) / Time.deltaTime; // [rad/sec]
                servo_sigma_old[i] = servo_sigma[i];
                audio_source_servo[i].pitch = 1;
                audio_source_servo[i].volume = Helper.Step(Mathf.Abs(servo_speed) * 1.0000000f, 0.5f, 0, 1.5f, 1);
            }

            // tailrotor servo (x1)
            audio_source_servo[3].volume = 0;
        }

        if (Helicopter_Tailrotor_Mechanics.rotor_3d_mechanics_geometry_available == true)
        {
            servo_sigma[3] = Helicopter_Tailrotor_Mechanics.servo_sigma[0]; // [rad] 
            float servo_speed = (servo_sigma_old[3] - servo_sigma[3]) / Time.deltaTime; // [rad/sec]
            servo_sigma_old[3] = servo_sigma[3];
            audio_source_servo[3].pitch = 1;
            audio_source_servo[3].volume = Helper.Step(Mathf.Abs(servo_speed) * 1.0000000f, 0.5f, 0, 1.5f, 1);
        }
        // ##################################################################################







        // ##################################################################################
        // mainrotorblades deformation 
        // ##################################################################################
        mainrotor_object.Update_Rotor_Deformation(ref helicopter_ODE, (stru_rotor)helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor, flapping_a_s_mr_L, flapping_b_s_mr_L, omega_mr, Omega_mr);
        tailrotor_object.Update_Rotor_Deformation(ref helicopter_ODE, (stru_rotor)helicopter_ODE.par.transmitter_and_helicopter.helicopter.tailrotor, flapping_a_s_tr_L, flapping_b_s_tr_L, omega_tr, Omega_tr);
        propeller_object.Update_Rotor_Deformation(ref helicopter_ODE, (stru_rotor)helicopter_ODE.par.transmitter_and_helicopter.helicopter.propeller, flapping_a_s_pr_L, flapping_b_s_pr_L, omega_pr, Omega_pr);
        // ##################################################################################         




        // ##################################################################################
        // mainrotor-disc BEMT  
        // ##################################################################################
        //if(Helicopter_Mainrotor_Mechanics.rotor_3d_mechanics_geometry_available == true)
        if (mainrotor_simplified0_or_BEMT1 == 1)
        {
            //Vector3 T_stiffLR_CH = new Vector3(0,0,0);
            //Vector3 T_stiffLR_LD = new Vector3(0,0,0);

            //Helicopter_Rotor_Physics.Rotor_Disc_BEMT_Calculations(ref helicopter_ODE, (stru_rotor)helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor, mainrotor_object.rotordisk_BEMT, ref T_stiffLR_CH, ref T_stiffLR_LD);

            // mainrotor_object.rotordisk_BEMT...
            mainrotor_object.rotordisk.transform.localPosition = Helper.ConvertRightHandedToLeftHandedVector(helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor.posLH.vect3);
            Quaternion rotation_LD = mainrotor_object.rotordisk.transform.rotation;
            rotation_LD.w = (float)helicopter_ODE.x_states[31]; // [-] w
            rotation_LD.x = (float)helicopter_ODE.x_states[32]; // [-] x
            rotation_LD.y = (float)helicopter_ODE.x_states[33]; // [-] y
            rotation_LD.z = (float)helicopter_ODE.x_states[34]; // [-] z
            //mainrotor_object.rotordisk.transform.rotation = Helper.ConvertRightHandedToLeftHandedQuaternion(rotation_LD); // global rotation - with rotation around shaft
            mainrotor_object.rotordisk.transform.rotation = Helper.ConvertRightHandedToLeftHandedQuaternion(Helper.QuaternionFromMatrix(helicopter_ODE.A_OLDnorot)); // global rotation - without rotation around shaft


            //helicopter_ODE.T_stiffLR_CH;
            //helicopter_ODE.T_stiffLR_LD;
        }
        // ##################################################################################





        // ##################################################################################
        // transmitter countdown timer
        // ##################################################################################
        if (gl_pause_flag == false)
            transmitter_countdown_minutes_timer -= Time.deltaTime;

        float timer_limit_in_seconds = helicopter_ODE.par.transmitter_and_helicopter.transmitter.countdown_minutes.val * 60.0f;

        int rounded_rest_seconds = Mathf.CeilToInt(transmitter_countdown_minutes_timer);
        int display_seconds_rest = rounded_rest_seconds % 60;
        int display_minutes_rest = rounded_rest_seconds / 60;
        int rounded_spent_seconds = Mathf.CeilToInt(timer_limit_in_seconds) - rounded_rest_seconds;
        int display_seconds_spent = rounded_spent_seconds % 60;
        int display_minutes_spent = rounded_spent_seconds / 60;

        if (display_seconds_spent != display_seconds_spent_old) // flank  detection
        {
            if (rounded_rest_seconds > 20 && display_seconds_spent == 0) // minute changes when second is zero
                Transmitter_Play_Audio(Application.streamingAssetsPath + "/Audio/Futaba_T18SZ_Sounds/Futaba_T18SZ_Minutes_" + display_minutes_spent.ToString("000") + ".wav");

            if (rounded_rest_seconds == 20) // remainder for last 20 seconds
                Transmitter_Play_Audio(Application.streamingAssetsPath + "/Audio/Futaba_T18SZ_Sounds/Futaba_T18SZ_20_Seconds_Remaining.wav");

            if (rounded_rest_seconds == 10) // countdown for last 10 seconds   
                Transmitter_Play_Audio(Application.streamingAssetsPath + "/Audio/Futaba_T18SZ_Sounds/Futaba_T18SZ_Countdown.wav");

            // update digits on transmitter display
            Transmitter_Update_Time_Digits_On_Display(rounded_rest_seconds);
        }
        display_seconds_spent_old = display_seconds_spent;
        // ##################################################################################




        // ##################################################################################
        // bird and insects flock target update
        // ##################################################################################
        Flocks_Change_Target_Positions(ref all_animal_flocks, false);
        // ##################################################################################




        // ##################################################################################
        // debug
        // ##################################################################################
        float msec_per_thread_call = (float)((stopwatch.Elapsed.TotalMilliseconds) / counter);
        if (!float.IsNaN(msec_per_thread_call) && !float.IsInfinity(msec_per_thread_call))
            msec_per_thread_call_filtered += (msec_per_thread_call - msec_per_thread_call_filtered)/50f; // simple moving average filter
        stopwatch.Reset(); counter = 0;


        // plot forces and torques
        if (ui_debug_panel_state > 0)
        {
            float force_arrow_scale = helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.debug_force_arrow_scale.val; // [N/m]
            float torque_arrow_scale = helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.debug_torque_arrow_scale.val; // [Nm/m]

            for (var i = 0; i < helicopter_ODE.ODEDebug.contact_forceR.Count; i++)
                Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_contact_forceR[i], helicopter_ODE.ODEDebug.contact_positionR[i], helicopter_ODE.ODEDebug.contact_positionR[i] + helicopter_ODE.ODEDebug.contact_forceR[i] * force_arrow_scale);

            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_mainrotor_forceO, helicopter_ODE.ODEDebug.mainrotor_positionO, helicopter_ODE.ODEDebug.mainrotor_positionO + helicopter_ODE.ODEDebug.mainrotor_forceO * force_arrow_scale);
            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_mainrotor_torqueO, helicopter_ODE.ODEDebug.mainrotor_positionO, helicopter_ODE.ODEDebug.mainrotor_positionO + helicopter_ODE.ODEDebug.mainrotor_torqueO * torque_arrow_scale);
            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_mainrotor_flapping_stiffness_torqueO, helicopter_ODE.ODEDebug.mainrotor_positionO, helicopter_ODE.ODEDebug.mainrotor_positionO + helicopter_ODE.ODEDebug.mainrotor_flapping_stiffness_torqueO * torque_arrow_scale);

            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_tailrotor_forceO, helicopter_ODE.ODEDebug.tailrotor_positionO, helicopter_ODE.ODEDebug.tailrotor_positionO + helicopter_ODE.ODEDebug.tailrotor_forceO * force_arrow_scale);
            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_tailrotor_torqueO, helicopter_ODE.ODEDebug.tailrotor_positionO, helicopter_ODE.ODEDebug.tailrotor_positionO + helicopter_ODE.ODEDebug.tailrotor_torqueO * torque_arrow_scale);
            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_tailrotor_flapping_stiffness_torqueO, helicopter_ODE.ODEDebug.tailrotor_positionO, helicopter_ODE.ODEDebug.tailrotor_positionO + helicopter_ODE.ODEDebug.tailrotor_flapping_stiffness_torqueO * torque_arrow_scale);

            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_propeller_forceO, helicopter_ODE.ODEDebug.propeller_positionO, helicopter_ODE.ODEDebug.propeller_positionO + helicopter_ODE.ODEDebug.propeller_forceO * force_arrow_scale * 1);
            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_propeller_torqueO, helicopter_ODE.ODEDebug.propeller_positionO, helicopter_ODE.ODEDebug.propeller_positionO + helicopter_ODE.ODEDebug.propeller_torqueO * torque_arrow_scale);
           
            for (var i = 0; i < helicopter_ODE.ODEDebug.line_object_booster_forceO.Count; i++)
                Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_booster_forceO[i], helicopter_ODE.ODEDebug.booster_positionO[i], helicopter_ODE.ODEDebug.booster_positionO[i] - helicopter_ODE.ODEDebug.booster_forceO[i] * force_arrow_scale * 1);
            
            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_drag_on_fuselage_drag_on_fuselage_forceO, helicopter_ODE.ODEDebug.drag_on_fuselage_positionO, helicopter_ODE.ODEDebug.drag_on_fuselage_positionO + helicopter_ODE.ODEDebug.drag_on_fuselage_drag_on_fuselage_forceO * force_arrow_scale * 5);
            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_force_on_horizontal_fin_forceO, helicopter_ODE.ODEDebug.force_on_horizontal_fin_positionO, helicopter_ODE.ODEDebug.force_on_horizontal_fin_positionO + helicopter_ODE.ODEDebug.force_on_horizontal_fin_forceO * force_arrow_scale * 5);
            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_force_on_vertical_fin_forceO, helicopter_ODE.ODEDebug.force_on_vertical_fin_positionO, helicopter_ODE.ODEDebug.force_on_vertical_fin_positionO + helicopter_ODE.ODEDebug.force_on_vertical_fin_forceO * force_arrow_scale * 5);

            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_force_on_horizontal_wing_left_forceO, helicopter_ODE.ODEDebug.force_on_horizontal_wing_left_positionO, helicopter_ODE.ODEDebug.force_on_horizontal_wing_left_positionO + helicopter_ODE.ODEDebug.force_on_horizontal_wing_left_forceO * force_arrow_scale * 5);
            Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_force_on_horizontal_wing_right_forceO, helicopter_ODE.ODEDebug.force_on_horizontal_wing_right_positionO, helicopter_ODE.ODEDebug.force_on_horizontal_wing_right_positionO + helicopter_ODE.ODEDebug.force_on_horizontal_wing_right_forceO * force_arrow_scale * 5);


            int r_n = 4;  // radial steps - (polar coordiantes)
            int c_n = 10; // circumferencial steps - (polar coordiantes) - number of virtual blades
            for (int r = 0; r < r_n; r++)
            {
                for (int c = 0; c < c_n; c++)
                {
                    Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_BEMT_blade_segment_velocity[r][c], helicopter_ODE.ODEDebug.BEMT_blade_segment_position[r][c], helicopter_ODE.ODEDebug.BEMT_blade_segment_position[r][c] + helicopter_ODE.ODEDebug.BEMT_blade_segment_velocity[r][c] * 1f);
                    Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_BEMT_blade_segment_thrust[r][c], helicopter_ODE.ODEDebug.BEMT_blade_segment_position[r][c], helicopter_ODE.ODEDebug.BEMT_blade_segment_position[r][c] + helicopter_ODE.ODEDebug.BEMT_blade_segment_thrust[r][c] * 1f);
                    Helper.Update_Line(helicopter_ODE.ODEDebug.line_object_BEMT_blade_segment_torque[r][c], helicopter_ODE.ODEDebug.BEMT_blade_segment_position[r][c], helicopter_ODE.ODEDebug.BEMT_blade_segment_position[r][c] + helicopter_ODE.ODEDebug.BEMT_blade_segment_torque[r][c] * 1f);
                }
            }

        }

        // update debug text
        if (ui_debug_panel_state > 1)
        {
            // ci = sqrt( (T/A) * 1/(2*tho) )
            float vi_theoretical_hover = Mathf.Sqrt((helicopter_ODE.par.transmitter_and_helicopter.helicopter.mass_total.val * 9.81f) / (Mathf.Pow(helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor.R.val, 2) * Mathf.PI) * (1 / (2.0f * helicopter_ODE.par.scenery.weather.rho_air.val)));
               
            ui_debug_text.text = ui_string_connected_input_devices_names + "  er" + error_counter.ToString() + " xr" + (XRSettings.enabled ? 1 : 0) + "\n" + 
                "thread_ODE_deltat = " + (thread_ODE_deltat*1000).ToString() + "msec" +
                "   msec_per_thread_call = " + Helper.FormatNumber(msec_per_thread_call_filtered, "0.00") + "msec" +
                "   monitor_frequency = " + Helper.FormatNumber(refresh_rate_hz, "0.000") + "Hz" + (refresh_rate_sec_found_flag ? "*" : "") +
                "   time = " + Helper.FormatNumber(time, "0.00") + "s" +  
                "   v_i_hover" + Helper.FormatNumber(vi_theoretical_hover, "0.00") + "m/s" + "\n" +
                helicopter_ODE.ODEDebug.debug_text;
        }


        // plot 2d graph
        if (ui_debug_panel_state > 1 && GraphManager.Graph != null)
        {
            GraphManager.Graph.Plot("mainrotor_forceLH [N]", helicopter_ODE.ODEDebug.mainrotor_forceLH.y, Color.black, plot2D_graph_rect[0]);
            GraphManager.Graph.Plot("mainrotor_torqueLH [Nm]", helicopter_ODE.ODEDebug.mainrotor_torqueLH.y, Color.black, plot2D_graph_rect[1]);
            GraphManager.Graph.Plot("omega_mr [rpm]", omega_mr * Helper.RadPerSec_to_Rpm, Color.black, plot2D_graph_rect[2]);
            GraphManager.Graph.Plot("tailrotor_forceLH [N]", helicopter_ODE.ODEDebug.tailrotor_forceLH.z, Color.black, plot2D_graph_rect[3]);
            GraphManager.Graph.Plot("veloLH.y [m/s]", helicopter_ODE.veloLH.y, Color.black, plot2D_graph_rect[4]);
            GraphManager.Graph.Plot("veloLH.xz [m/s]", Mathf.Sqrt(helicopter_ODE.veloLH.x * helicopter_ODE.veloLH.x + helicopter_ODE.veloLH.z * helicopter_ODE.veloLH.z), Color.black, plot2D_graph_rect[5]);
        }
        else
        {
            if (ui_debug_panel_state_old != ui_debug_panel_state)
            {
                GraphManager.Graph.Reset("mainrotor_forceLH [N]");
                GraphManager.Graph.Reset("mainrotor_torqueLH [Nm]");
                GraphManager.Graph.Reset("omega_mr [rpm]");
                GraphManager.Graph.Reset("tailrotor_forceLH [N]");
                GraphManager.Graph.Reset("veloLH.y [m/s]");
                GraphManager.Graph.Reset("veloLH.xz [m/s]");
            }
        }
        ui_debug_panel_state_old = ui_debug_panel_state;
        // ##################################################################################




        //// ##################################################################################
        ////
        //// ##################################################################################
        //if (first_start_flag == 1)
        //{
        //    first_start_flag = 0;
        //    PlayerPrefs.SetInt("first_start_flag", first_start_flag);
        //}
        //// ##################################################################################




        // ##################################################################################
        // Vortex Ring State
        // ##################################################################################
        if(helicopter_ODE.par.transmitter_and_helicopter.helicopter.visual_effects.vortex_ring_visualize.val &&
            helicopter_vortex_ring_state_particles_gameobject != null)
        { 
            if(helicopter_ODE.ODEDebug.vortex_ring_state_mr > 0.5)
            {
                //helicopter_vortex_ring_state_particles_gameobject.transform.GetChild(0).gameObject;
               // Transform[] ts = helicopter_vortex_ring_state_particles_gameobject.transform.GetComponentsInChildren<Transform>();
                foreach (Transform t in helicopter_vortex_ring_state_particles_gameobject.transform)
                {
                    ParticleSystem ps = t.GetComponent<ParticleSystem>();
                    if (!ps.isPlaying)
                        ps.Play();
                }
            }
            if (helicopter_ODE.ODEDebug.vortex_ring_state_mr < 0.1)
            {
                //Transform[] ts = helicopter_vortex_ring_state_particles_gameobject.transform.GetComponentsInChildren<Transform>();
                foreach (Transform t in helicopter_vortex_ring_state_particles_gameobject.transform)
                {
                    ParticleSystem ps = t.GetComponent<ParticleSystem>();
                    if (ps.isPlaying)
                        ps.Stop();
                }
            }
        }
        // ##################################################################################




        // ##################################################################################
        // UI information overlay
        // ##################################################################################
        ui_informations_overlay.engine = helicopter_ODE.flag_motor_enabled ? 1 : 0;
        ui_informations_overlay.autorotation = helicopter_ODE.flag_freewheeling ? 1 : 0;
        ui_informations_overlay.engine_restart_time = 1-Mathf.Clamp01((Time.time - helicopter_ODE.engine_restart_time_stopped_time) / helicopter_ODE.par.transmitter_and_helicopter.helicopter.governor.engine_restart_time.val);
        ui_informations_overlay.brakes = helicopter_ODE.wheel_brake_strength;
        ui_informations_overlay.vortex_ring_state = helicopter_ODE.ODEDebug.vortex_ring_state_mr;
        ui_informations_overlay.turbulence = helicopter_ODE.ODEDebug.turbulence_mr;
        ui_informations_overlay.ground_effect = helicopter_ODE.ODEDebug.ground_effect_mr;
        ui_informations_overlay.flap_up = helicopter_ODE.ODEDebug.flap_up_mr;
        ui_informations_overlay.landing_gear = 1-helicopter_ODE.collision_positions_landing_gear_left_rising_offset;
        ui_informations_overlay.rpm = omega_mr * Helper.RadPerSec_to_Rpm;
        ui_informations_overlay.mainrotor_cyclic = helicopter_ODE.ODEDebug.Theta_col_mr * Helper.Rad_to_Deg; 
        ui_informations_overlay.tailrotor_cyclic = helicopter_ODE.ODEDebug.Theta_col_tr * Helper.Rad_to_Deg;
        ui_informations_overlay.speed = helicopter_ODE.veloLH.magnitude;
        ui_informations_overlay.target_rpm = helicopter_ODE.par.transmitter_and_helicopter.helicopter.governor.target_rpm.vect3[helicopter_ODE.flight_bank]; ;
        ui_informations_overlay.bank = helicopter_ODE.flight_bank+1;
        // ##################################################################################



        // ##################################################################################
        // anti stuttering
        // ##################################################################################
        // Multiple FixedUpdate() calls may arise at beginning of frame and we only want to update once per frame
        io_antistutter__fixedupdate_calls_in_this_frame_counter = 0;
        // ##################################################################################
    }
    // ##################################################################################
    #endregion













    // ##################################################################################
    //                                                                                                                                                 dddddddd                                                            
    //    LLLLLLLLLLL                                       tttt                              UUUUUUUU     UUUUUUUU                               d::::::d                          tttt                              
    //    L:::::::::L                                    ttt:::t                              U::::::U     U::::::U                               d::::::d                       ttt:::t                              
    //    L:::::::::L                                    t:::::t                              U::::::U     U::::::U                               d::::::d                       t:::::t                              
    //    LL:::::::LL                                    t:::::t                              UU:::::U     U:::::UU                               d:::::d                        t:::::t                              
    //      L:::::L                 aaaaaaaaaaaaa  ttttttt:::::ttttttt        eeeeeeeeeeee     U:::::U     U:::::Uppppp   ppppppppp       ddddddddd:::::d   aaaaaaaaaaaaa  ttttttt:::::ttttttt        eeeeeeeeeeee    
    //      L:::::L                 a::::::::::::a t:::::::::::::::::t      ee::::::::::::ee   U:::::D     D:::::Up::::ppp:::::::::p    dd::::::::::::::d   a::::::::::::a t:::::::::::::::::t      ee::::::::::::ee  
    //      L:::::L                 aaaaaaaaa:::::at:::::::::::::::::t     e::::::eeeee:::::ee U:::::D     D:::::Up:::::::::::::::::p  d::::::::::::::::d   aaaaaaaaa:::::at:::::::::::::::::t     e::::::eeeee:::::ee
    //      L:::::L                          a::::atttttt:::::::tttttt    e::::::e     e:::::e U:::::D     D:::::Upp::::::ppppp::::::pd:::::::ddddd:::::d            a::::atttttt:::::::tttttt    e::::::e     e:::::e
    //      L:::::L                   aaaaaaa:::::a      t:::::t          e:::::::eeeee::::::e U:::::D     D:::::U p:::::p     p:::::pd::::::d    d:::::d     aaaaaaa:::::a      t:::::t          e:::::::eeeee::::::e
    //      L:::::L                 aa::::::::::::a      t:::::t          e:::::::::::::::::e  U:::::D     D:::::U p:::::p     p:::::pd:::::d     d:::::d   aa::::::::::::a      t:::::t          e:::::::::::::::::e 
    //      L:::::L                a::::aaaa::::::a      t:::::t          e::::::eeeeeeeeeee   U:::::D     D:::::U p:::::p     p:::::pd:::::d     d:::::d  a::::aaaa::::::a      t:::::t          e::::::eeeeeeeeeee  
    //      L:::::L         LLLLLLa::::a    a:::::a      t:::::t    tttttte:::::::e            U::::::U   U::::::U p:::::p    p::::::pd:::::d     d:::::d a::::a    a:::::a      t:::::t    tttttte:::::::e           
    //    LL:::::::LLLLLLLLL:::::La::::a    a:::::a      t::::::tttt:::::te::::::::e           U:::::::UUU:::::::U p:::::ppppp:::::::pd::::::ddddd::::::dda::::a    a:::::a      t::::::tttt:::::te::::::::e          
    //    L::::::::::::::::::::::La:::::aaaa::::::a      tt::::::::::::::t e::::::::eeeeeeee    UU:::::::::::::UU  p::::::::::::::::p  d:::::::::::::::::da:::::aaaa::::::a      tt::::::::::::::t e::::::::eeeeeeee  
    //    L::::::::::::::::::::::L a::::::::::aa:::a       tt:::::::::::tt  ee:::::::::::::e      UU:::::::::UU    p::::::::::::::pp    d:::::::::ddd::::d a::::::::::aa:::a       tt:::::::::::tt  ee:::::::::::::e  
    //    LLLLLLLLLLLLLLLLLLLLLLLL  aaaaaaaaaa  aaaa         ttttttttttt      eeeeeeeeeeeeee        UUUUUUUUU      p::::::pppppppp       ddddddddd   ddddd  aaaaaaaaaa  aaaa         ttttttttttt      eeeeeeeeeeeeee  
    //                                                                                                              p:::::p                                                                                            
    //                                                                                                              p:::::p                                                                                            
    //                                                                                                             p:::::::p                                                                                           
    //                                                                                                             p:::::::p                                                                                           
    //                                                                                                             p:::::::p                                                                                           
    //                                                                                                             ppppppppp 
    // ##################################################################################
    #region LateUpdate
    // ##################################################################################
    // LateUpdate
    // ##################################################################################
    // Update is called once per frame
    void LateUpdate()
    {


        // ##################################################################################
        // zoom with mouse wheel
        // ##################################################################################
        if (UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y != 0f &&
            ui_parameter_panel_flag == false && ui_welcome_panel_flag == false && ui_info_panel_flag == false &&
            ui_helicopter_selection_menu_flag == false && ui_scenery_selection_menu_flag == false && ui_pie_menu_flag == false )
        {
            mouse_fov -= UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y * mouse_scroll_fov_increment;
            mouse_fov = Mathf.Clamp(mouse_fov, -mouse_fov_limit, +mouse_fov_limit);
        }
        // ##################################################################################


        // ##################################################################################
        // look around with mouse while holding middle mouse wheel or right mouse button
        // ##################################################################################
        if (UnityEngine.InputSystem.Mouse.current.middleButton.isPressed ||
            UnityEngine.InputSystem.Mouse.current.rightButton.isPressed ||
            UnityEngine.InputSystem.Keyboard.current.leftAltKey.isPressed ||
            UnityEngine.InputSystem.Keyboard.current.rightAltKey.isPressed)
        {
            // rotate camera
            mouse_camera_yaw += (mouse_camera_speed_horizontaly * (fieldOfView / fieldOfView_max) * Time.deltaTime) * UnityEngine.InputSystem.Mouse.current.delta.ReadValue().x; // * Helper.Step(Mathf.Abs(x), 30, 0, 80, 1);
            mouse_camera_pitch -= (mouse_camera_speed_verticaly * (fieldOfView / fieldOfView_max) * Time.deltaTime) * UnityEngine.InputSystem.Mouse.current.delta.ReadValue().y; // * Helper.Step(Mathf.Abs(y), 30, 0, 80, 1);

            mouse_camera_yaw -= Mathf.Sign(mouse_camera_yaw) * Helper.Step(fieldOfView, fieldOfView_min, 0.0010f, fieldOfView_max, 0.015f); // reduce velocity -> slow down movement
            mouse_camera_pitch -= Mathf.Sign(mouse_camera_pitch) * Helper.Step(fieldOfView, fieldOfView_min, 0.0010f, fieldOfView_max, 0.015f); // reduce velocity -> slow down movement

            //UnityEngine.Debug.Log( "x " + x.ToString() + " y " + y.ToString() ); 
        }
        else
        {
            // get view override back to zero (look again at helicopter)
            mouse_camera_yaw = 0;
            mouse_camera_pitch = 0;
        }
        // ##################################################################################




        // ##################################################################################
        // update camera position and view
        // ##################################################################################
        float vertical_camera_angle;
        if (XRSettings.enabled)
        {
            XRDevice.fovZoomFactor = Helper.Clamp(helicopter_ODE.par.simulation.camera.xr_zoom_factor);

            Correct_And_Limit_XR_Camera_Vertical_Position();
        }
        else
        {
            if (helicopter_ODE.par.simulation.camera.shaking.val > 0)
            {
                // camera shaking
                //Vector3 localPosition = UnityEngine.Random.insideUnitSphere * 0.05f;
                //float x = (float)exponential_moving_average_filter_for_camera_position_x.Calculate(100000, (double)localPosition.x) * 2000;
                //float y = (float)exponential_moving_average_filter_for_camera_position_y.Calculate(100000, (double)localPosition.y) * 2000;
                //float z = (float)exponential_moving_average_filter_for_camera_position_z.Calculate(100000, (double)localPosition.z) * 2000;

                float factor = Helper.Clamp(helicopter_ODE.par.simulation.camera.shaking) / 100f; // [%]->[0...1]
                float shaking_factor = 0.03f * factor;
                float x = Mathf.PerlinNoise(Time.timeSinceLevelLoad / 4.0f + 1, Time.timeSinceLevelLoad / 2f + 100) * shaking_factor;
                float y = Mathf.PerlinNoise(Time.timeSinceLevelLoad / 3.0f + 10, 0) * shaking_factor * 3;
                float z = Mathf.PerlinNoise(Time.timeSinceLevelLoad / 2.0f + 100, 0) * shaking_factor;

                main_camera.transform.position = new Vector3(x, helicopter_ODE.par.scenery.camera_height.val + y, z);
            }
            else
            {
                main_camera.transform.position = new Vector3(0, helicopter_ODE.par.scenery.camera_height.val, 0);
            }
            //main_camera.transform.position = new Vector3(0, helicopter_ODE.par.scenery.camera_height.val, 0);



            
            if (helicopter_ODE.par.simulation.camera.auto_zoom.val == false)
            {
                fieldOfView = Mathf.Clamp(helicopter_ODE.par.simulation.camera.fov.val + mouse_fov, fieldOfView_min, fieldOfView_max);
            }
            else
            { 
                float distance_helicopter_to_camera = (helicopters_available.transform.position - main_camera.transform.position).magnitude; // [m]

                /*
                // auto-zoom
                // 1.) linear part  
                //      y = (y2-y1)/(x2-x1) * (x-x1) + y1 = m * (x-x1) + y1
                float x_ = distance_helicopter_to_camera; // [m] distance 
                float x1 = 0; // [m] distance
                float y1 = 30; // [deg] FOV
                float x2 = 47; // [m] distance
                float y2 = 8; // [deg] FOV

                float m = (y2 - y1)/(x2 - x1);

                if (x_ < x2)
                {
                    fieldOfView = m * (x_ - x1) + y1; // [deg]
                }
                else
                {

                    // 2.) parabola part
                    //     point A: yA = a*xA^2+b*xA+c
                    //     point B: yB = a*xB^2+b*xB+c
                    //     slope A: yA' = 2*a*xA+b
                    // --> a = -(yA - yB - xA*yA_ + xB*yA_)/(xA^2 - 2*xA*xB + xB^2)
                    // --> b = (2*xA*yA - 2*xA*yB - xA^2*yA_ + xB^2*yA_)/(xA^2 - 2*xA*xB + xB^2)
                    // --> c = (yA_*xA^2*xB + yB*xA^2 - yA_*xA*xB^2 - 2*yA*xA*xB + yA*xB^2)/(xA^2 - 2*xA*xB + xB^2)
                    float xA = x2; // [m] distance
                    float yA = y2; // [deg] FOV
                    float yA_ = m; // [m/deg] slope
                    float xB = 60; // [m] distance
                    float yB = 5; // [deg] FOV

                    if (x_ < xB)
                    {
                        float a = -(yA - yB - xA * yA_ + xB * yA_) / (xA * xA - 2 * xA * xB + xB * xB); 
                        float b = (2 * xA * yA - 2 * xA * yB - xA * xA * yA_ + xB * xB * yA_) / (xA * xA - 2 * xA * xB + xB * xB);
                        float c = (yA_ * xA * xA * xB + yB * xA * xA - yA_ * xA * xB * xB - 2 * yA * xA * xB + yA * xB * xB) / (xA * xA - 2 * xA * xB + xB * xB);
                        fieldOfView = a * x_ * x_ + b * x_ + c; // [deg]
                    }
                    else
                    {
                        fieldOfView = yB; // [deg]
                    }

                }*/

                // auto-zoom
                // 1.) linear part  
                //      y = (y2-y1)/(x2-x1) * (x-x1) + y1 = m * (x-x1) + y1
                float x_ = distance_helicopter_to_camera; // [m] distance 
                float x1 = 0; // [m] distance
                float y1 = Mathf.Clamp(helicopter_ODE.par.simulation.camera.fov.val + mouse_fov, fieldOfView_min, fieldOfView_max); // [deg] FOV    30 
                float x2 = 60; // [m] distance
                float y2 = 5; // [deg] FOV

                float m = (y2 - y1) / (x2 - x1);

                if (x_ < x2)
                {
                    fieldOfView = m * (x_ - x1) + y1; // [deg]
                }
                else
                {
                    fieldOfView = y2; // [deg]
                }  
            }

            main_camera.fieldOfView = Mathf.SmoothDamp(main_camera.fieldOfView, fieldOfView, ref fieldOfView_velocity, 0.1f, 1000, Time.deltaTime);






            // filter camera movement and add offset (helicopter should not be in focus during debugging, or ground visibility)
            var camera_rotation = Quaternion.LookRotation(helicopters_available.transform.position - main_camera.transform.position);
            if (!UnityEngine.InputSystem.Mouse.current.middleButton.isPressed && 
                !UnityEngine.InputSystem.Mouse.current.rightButton.isPressed && 
                !UnityEngine.InputSystem.Keyboard.current.leftAltKey.isPressed &&
                !UnityEngine.InputSystem.Keyboard.current.rightAltKey.isPressed)
            {
                main_camera_rotation = Quaternion.Slerp(main_camera_rotation, camera_rotation, Time.deltaTime * helicopter_ODE.par.simulation.camera.stiffness.val);

                // if debugging is activated, the camrera should not focus on the heli, but show it on the right side of the screen
                const float main_camera_rotation_offset_during_debuging_ = -11; // [deg]
                if (ui_debug_panel_state <= 1)
                    main_camera_rotation_offset_during_debuging -= Time.deltaTime * 1.5f;
                else
                    main_camera_rotation_offset_during_debuging += Time.deltaTime * 1.5f;
                main_camera_rotation_offset_during_debuging = Mathf.Clamp01(Mathf.Abs(main_camera_rotation_offset_during_debuging));
                float vertical_offset = Helper.Step(main_camera_rotation_offset_during_debuging, 0.1f, 0, 0.9f, main_camera_rotation_offset_during_debuging_);

                // keep ground visible           
                vertical_camera_angle = (-((main_camera.transform.eulerAngles.x + 180f) % 360f) + 180f);
                float horizontal_offset = Helper.Step(vertical_camera_angle, 0, 0, (main_camera.fieldOfView / 2.0f), helicopter_ODE.par.simulation.camera.keep_ground_visible.val);

                // apply all rotations to camera
                main_camera.transform.rotation = Quaternion.Euler(0, vertical_offset * (main_camera.fieldOfView / 30f), 0) *
                    main_camera_rotation * Quaternion.Euler(horizontal_offset * (main_camera.fieldOfView / 30f), 0, 0); // rotation order is essential

            }
            else
            {
                main_camera.transform.eulerAngles += new Vector3(mouse_camera_pitch, mouse_camera_yaw, 0.0f);
                main_camera_rotation = main_camera.transform.rotation;
            }

            // main_camera.transform.eulerAngles.x geos from ...+360° -> 0° -> neg values... thus has a discontinuity, if looking at the horizont. 
            // Rearange values where looking above the horizont has positive, below negative values
            vertical_camera_angle = (-((main_camera.transform.eulerAngles.x + 180f) % 360f) + 180f);

            // limit down view
            if (vertical_camera_angle < -50f && vertical_camera_angle > -90)
            {
                main_camera.transform.eulerAngles = new Vector3(50f, main_camera.transform.eulerAngles.y, main_camera.transform.eulerAngles.z);
                mouse_camera_pitch = 0;
            }
        }

        if (helicopter_ODE.par_temp.simulation.gameplay.show_pilot.val) 
        {
            vertical_camera_angle = (-((main_camera.transform.eulerAngles.x + 180f) % 360f) + 180f);
            // lifting the transmitter
            if (vertical_camera_angle < -30)
            {
                animator_pilot_with_transmitter.SetTrigger("lifting_arm");
                animator_pilot_with_transmitter.ResetTrigger("lowering_arm");
            }
            if (vertical_camera_angle > -10)
            {
                animator_pilot_with_transmitter.SetTrigger("lowering_arm");
                animator_pilot_with_transmitter.ResetTrigger("lifting_arm");
            }
        }

        if (!XRSettings.enabled)
        { 
            // Limit the mouse movement to the game window
            if (helicopter_ODE.par.simulation.gameplay.limit_mouse_to_game.val)
            {
            
    #if UNITY_EDITOR
                    Cursor.lockState = CursorLockMode.None;
    #else
                Cursor.lockState = CursorLockMode.Confined;
    #endif
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }

        }
        else
        {
            Cursor.lockState = CursorLockMode.Confined;
        }
        // ##################################################################################

    }
    #endregion






    // ##################################################################################
    //        CCCCCCCCCCCCC                                                                               tttt            iiii                                                        
    //     CCC::::::::::::C                                                                            ttt:::t           i::::i                                                       
    //   CC:::::::::::::::C                                                                            t:::::t            iiii                                                        
    //  C:::::CCCCCCCC::::C                                                                            t:::::t                                                                        
    // C:::::C       CCCCCC   ooooooooooo   rrrrr   rrrrrrrrr      ooooooooooo   uuuuuu    uuuuuuttttttt:::::ttttttt    iiiiiiinnnn  nnnnnnnn        eeeeeeeeeeee        ssssssssss   
    //C:::::C               oo:::::::::::oo r::::rrr:::::::::r   oo:::::::::::oo u::::u    u::::ut:::::::::::::::::t    i:::::in:::nn::::::::nn    ee::::::::::::ee    ss::::::::::s  
    //C:::::C              o:::::::::::::::or:::::::::::::::::r o:::::::::::::::ou::::u    u::::ut:::::::::::::::::t     i::::in::::::::::::::nn  e::::::eeeee:::::eess:::::::::::::s 
    //C:::::C              o:::::ooooo:::::orr::::::rrrrr::::::ro:::::ooooo:::::ou::::u    u::::utttttt:::::::tttttt     i::::inn:::::::::::::::ne::::::e     e:::::es::::::ssss:::::s
    //C:::::C              o::::o     o::::o r:::::r     r:::::ro::::o     o::::ou::::u    u::::u      t:::::t           i::::i  n:::::nnnn:::::ne:::::::eeeee::::::e s:::::s  ssssss 
    //C:::::C              o::::o     o::::o r:::::r     rrrrrrro::::o     o::::ou::::u    u::::u      t:::::t           i::::i  n::::n    n::::ne:::::::::::::::::e    s::::::s      
    //C:::::C              o::::o     o::::o r:::::r            o::::o     o::::ou::::u    u::::u      t:::::t           i::::i  n::::n    n::::ne::::::eeeeeeeeeee        s::::::s   
    // C:::::C       CCCCCCo::::o     o::::o r:::::r            o::::o     o::::ou:::::uuuu:::::u      t:::::t    tttttt i::::i  n::::n    n::::ne:::::::e           ssssss   s:::::s 
    //  C:::::CCCCCCCC::::Co:::::ooooo:::::o r:::::r            o:::::ooooo:::::ou:::::::::::::::uu    t::::::tttt:::::ti::::::i n::::n    n::::ne::::::::e          s:::::ssss::::::s
    //   CC:::::::::::::::Co:::::::::::::::o r:::::r            o:::::::::::::::o u:::::::::::::::u    tt::::::::::::::ti::::::i n::::n    n::::n e::::::::eeeeeeee  s::::::::::::::s 
    //     CCC::::::::::::C oo:::::::::::oo  r:::::r             oo:::::::::::oo   uu::::::::uu:::u      tt:::::::::::tti::::::i n::::n    n::::n  ee:::::::::::::e   s:::::::::::ss  
    //        CCCCCCCCCCCCC   ooooooooooo    rrrrrrr               ooooooooooo       uuuuuuuu  uuuu        ttttttttttt  iiiiiiii nnnnnn    nnnnnn    eeeeeeeeeeeeee    sssssssssss  
    // ##################################################################################
    #region Coroutines
    // ##################################################################################
    // Coroutines
    // ##################################################################################


    // ##################################################################################
    // Hide mouse cursor
    // ##################################################################################
    private IEnumerator Hide_Cursor()
    {
        yield return new WaitForSeconds(2);
        Cursor.visible = false;
    }
    // ##################################################################################





    // ##################################################################################
    // XR start and stop
    // ##################################################################################
    public IEnumerator StartXR()
    {
        UnityEngine.Debug.Log("Initializing XR...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            UnityEngine.Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
            StopXR();
        }
        else
        {
            UnityEngine.Debug.Log("Starting XR...");

            //XRGeneralSettings.Instance.Manager.StartSubsystems();



            //Try to start all subsystems and check if they were all successfully started ( thus HMD prepared).
            bool loaderSuccess = XRGeneralSettings.Instance.Manager.activeLoader.Start();
            if (loaderSuccess)
            {
                
                // --------------------------- TRY TO FIND OUT IF DEVICIE IS ACTIVE: DOES NOT WORK YET ---------------------
                UnityEngine.Debug.Log("-----XR Device Present: " + XRDeviceIsPresent().ToString());
                UnityEngine.Debug.Log("-----XR Device IsActive: " + IsActive()); // https://forum.unity.com/threads/deprecation-nightmare.812688/
                UnityEngine.Debug.Log("-----XR Device IsVrRunning: " + IsVrRunning()); // https://forum.unity.com/threads/deprecation-nightmare.812688/
                //UnityEngine.Debug.Log("XR User Presence: " + UnityEngine.InputSystem.CommonUsages.userPresence);

                UnityEngine.XR.InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                UnityEngine.Debug.Log("-----XR headDevice: " + headDevice);
                //if (headDevice.isValid == false) return;
                bool presenceFeatureSupported = headDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.userPresence, out bool userPresent);
                UnityEngine.Debug.Log(headDevice.isValid + " ** " + presenceFeatureSupported + " ** " + userPresent);

                //UnityEngine.Debug.Log("XR Model: " + UnityEngine.XR.InputDevice.....); // https://forum.unity.com/threads/detailed-xr-inputdevice-names.720614/
                UnityEngine.Debug.Log("-----XR Device Active: " + XRSettings.isDeviceActive);
                UnityEngine.Debug.Log("-----XR Enabled: " + XRSettings.enabled);

                //UnityEngine.Debug.Log("-----XR OpenVR: " + OpenVR.IsHmdPresent());
                // OpenVR.System.IsTrackedDeviceConnected()

                // https://docs.unity3d.com/2020.2/Documentation/Manual/xr_input.html
                var inputDevices = new List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevices(inputDevices);
                bool XR_devide_is_active = false;
                foreach (var device in inputDevices)
                {
                    UnityEngine.Debug.Log(string.Format("  xxxxxxxxxxxx  Device found with name '{0}' and role '{1}'", device.name, device.role.ToString()));
                    XR_devide_is_active = true;
                }
                // --------------------------- TRY TO FIND OUT IF DEVICIE IS ACTIVE: DOES NOT WORK YET ---------------------




                if (IsActive() && IsVrRunning())
                //if (XR_devide_is_active)
                { 
                    UnityEngine.Debug.Log("All Subsystems Started!");

                    // disable postprocessing layer (beacuse graphics problems?)
                    if (main_camera.GetComponent<PostProcessLayer>() != null) main_camera.GetComponent<PostProcessLayer>().enabled = false;
                    // disable graphmanger (plots curves during debug mode)  TODO instead disable move figures to canvas
                    if (main_camera.GetComponent<GraphManager>() != null) main_camera.GetComponent<GraphManager>().enabled = false;
                    // enable tracked pose diriver
                    //if (main_camera.GetComponent<TrackedPoseDriver>() != null) main_camera.GetComponent<TrackedPoseDriver>().enabled = true;

                    // fps
                    refresh_rate_sec_old = 0;
                    exponential_moving_average_filter_for_refresh_rate_sec.Init_Mean_Value(1.0f / Screen.currentResolution.refreshRate);
                    refresh_rate_sec_found_flag = false;
                    //Find_Exact_Monitor_Refreshrate();

                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_virtual_reality_mode_enabled.wav");

                    // set ui-canvas to worldspace
                    Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
                    RectTransform canva_rt = canvas.GetComponent<RectTransform>(); 
                    canvas.renderMode = RenderMode.WorldSpace;
                    canva_rt.anchorMin = new Vector2(0, 0);
                    canva_rt.anchorMax = new Vector2(0, 0);
                    canva_rt.pivot = new Vector2(0.5f, 0.5f);
                    canva_rt.localScale = new Vector3(0.005f, 0.005f, 0.005f);
                    canva_rt.sizeDelta = new Vector2(1300, 1000);
                    canva_rt.localPosition = Quaternion.Euler(0, helicopter_ODE.par.scenery.xr.canvas_rotation.vect3.y, 0) * helicopter_ODE.par.scenery.xr.canvas_position.vect3;
                    canva_rt.localRotation = Quaternion.Euler(helicopter_ODE.par.scenery.xr.canvas_rotation.vect3);



                    //dactivate heat blur effect -GrabPass in "Custom/GlassStainedBumpDistort" seams not to work in "single pass instenced" VR
                    // hat blur appears in xr-not correct, therefore as workaround deactivate it. TODO fix heatblur in VR
                    Transform go;
                    GameObject Helicopter_Selected = helicopters_available.transform.Find(helicopter_name).gameObject;
                    go = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.Find("Particle System");
                    if (go != null) go.gameObject.SetActive(false);
                    go = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.Find("Particle System (1)");
                    if (go != null) go.gameObject.SetActive(false);
                    go = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.Find("Particle System Left");
                    if (go != null) go.gameObject.SetActive(false);
                    go = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.Find("Particle System Right");
                    if (go != null) go.gameObject.SetActive(false);



                }
                else
                {
                    GameObject.Find("Canvas").GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    UnityEngine.Debug.LogError("Starting Subsystems Failed. Directing to Normal Interaciton Mode...!");
                    StopXR();
                }

            }
            else
            {
                GameObject.Find("Canvas").GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                UnityEngine.Debug.LogError("Starting Subsystems Failed. Directing to Normal Interaciton Mode...!");
                StopXR();
            }

        }
    }

    void StopXR()
    {
        UnityEngine.Debug.Log("Stopping XR...");

        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        UnityEngine.Debug.Log("XR stopped completely.");

        // reset monitor resolution
        int resolution_setting = Helper.Clamp(helicopter_ODE.par_temp.simulation.graphic_quality.resolution_setting);
        string[] splitArray = helicopter_ODE.par_temp.simulation.graphic_quality.resolution_setting.str[resolution_setting].Split(char.Parse("x"));
        Screen.SetResolution(Int32.Parse(splitArray[0]), Int32.Parse(splitArray[1]), true);


        // enable postprocessing layer
        if (main_camera.GetComponent<PostProcessLayer>() != null) main_camera.GetComponent<PostProcessLayer>().enabled = true;
        // enable graphmanger (plots curves during debug mode)  TODO instead disable move figures to canvas
        if (main_camera.GetComponent<GraphManager>() != null) main_camera.GetComponent<GraphManager>().enabled = true;
        // disable tracked pose diriver
        //if (main_camera.GetComponent<TrackedPoseDriver>() != null) main_camera.GetComponent<TrackedPoseDriver>().enabled = false;


        refresh_rate_sec_old = 0;
        exponential_moving_average_filter_for_refresh_rate_sec.Init_Mean_Value(1.0f / Screen.currentResolution.refreshRate);
        refresh_rate_sec_found_flag = false;
        // Find_Exact_Monitor_Refreshrate();

        Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/female_voice_virtual_reality_mode_disabled.wav");

        // set ui-canvas to screenspaceoverlay
        GameObject.Find("Canvas").GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        //activate heat blur effect -GrabPass in "Custom/GlassStainedBumpDistort" seams not to work in "single pass instenced" VR
        // hat blur appears in xr-not correct, therefore as workaround deactivate it.--> here activate it again.  TODO fix heatblur in VR
        Transform go;
        GameObject Helicopter_Selected = helicopters_available.transform.Find(helicopter_name).gameObject;
        go = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.Find("Particle System");
        if (go != null) go.gameObject.SetActive(true);
        go = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.Find("Particle System (1)");
        if (go != null) go.gameObject.SetActive(true);
        go = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.Find("Particle System Left");
        if (go != null) go.gameObject.SetActive(true);
        go = Helicopter_Selected.transform.Find("Helicopter_Model").gameObject.transform.Find("Particle System Right");
        if (go != null) go.gameObject.SetActive(true);

    }
    // ##################################################################################

    #endregion







    // ##################################################################################   
    //         QQQQQQQQQ                         iiii          tttt          
    //       QQ:::::::::QQ                      i::::i      ttt:::t          
    //     QQ:::::::::::::QQ                     iiii       t:::::t          
    //    Q:::::::QQQ:::::::Q                               t:::::t          
    //    Q::::::O   Q::::::Quuuuuu    uuuuuu  iiiiiiittttttt:::::ttttttt    
    //    Q:::::O     Q:::::Qu::::u    u::::u  i:::::it:::::::::::::::::t    
    //    Q:::::O     Q:::::Qu::::u    u::::u   i::::it:::::::::::::::::t    
    //    Q:::::O     Q:::::Qu::::u    u::::u   i::::itttttt:::::::tttttt    
    //    Q:::::O     Q:::::Qu::::u    u::::u   i::::i      t:::::t          
    //    Q:::::O     Q:::::Qu::::u    u::::u   i::::i      t:::::t          
    //    Q:::::O  QQQQ:::::Qu::::u    u::::u   i::::i      t:::::t          
    //    Q::::::O Q::::::::Qu:::::uuuu:::::u   i::::i      t:::::t    tttttt
    //    Q:::::::QQ::::::::Qu:::::::::::::::uui::::::i     t::::::tttt:::::t
    //     QQ::::::::::::::Q  u:::::::::::::::ui::::::i     tt::::::::::::::t
    //       QQ:::::::::::Q    uu::::::::uu:::ui::::::i       tt:::::::::::tt
    //         QQQQQQQQ::::QQ    uuuuuuuu  uuuuiiiiiiii         ttttttttttt  
    //                 Q:::::Q                                               
    //                  QQQQQQ  
    // ##################################################################################
    // Quit
    // ##################################################################################
    #region Quit
    void OnApplicationQuit()
    {
        //UnityEngine.Debug.Log("Application ending after " + Time.time + " seconds");
#if UNITY_EDITOR
        Thread.Sleep(0);
        if (thread_ODE != null)
            thread_ODE.Abort();
#endif

#if DEBUG_LOG  
        // debug time ticks to file
        Debug_Save_Time_Ticks();
#endif
    }
    // ##################################################################################
    #endregion


}

















