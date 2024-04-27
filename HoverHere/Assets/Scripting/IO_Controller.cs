using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;
using Common;
using UnityEditor;
using UnityEngine.InputSystem.HID;
using System.IO;
// RX2SIM Game Controller has two frames. --> workaround needed
// https://forum.unity.com/threads/new-input-system-problem-rx2sim-game-controller-every-second-frame-is-zero.871330/#post-5749141




// ##################################################################################                                                                                                                                             
//           CCCCCCCCCCCCC                                            tttt                                              lllllll lllllll                                         
//        CCC::::::::::::C                                         ttt:::t                                              l:::::l l:::::l                                         
//      CC:::::::::::::::C                                         t:::::t                                              l:::::l l:::::l                                         
//     C:::::CCCCCCCC::::C                                         t:::::t                                              l:::::l l:::::l                                         
//    C:::::C       CCCCCC   ooooooooooo   nnnn  nnnnnnnn    ttttttt:::::ttttttt   rrrrr   rrrrrrrrr      ooooooooooo    l::::l  l::::l     eeeeeeeeeeee    rrrrr   rrrrrrrrr   
//   C:::::C               oo:::::::::::oo n:::nn::::::::nn  t:::::::::::::::::t   r::::rrr:::::::::r   oo:::::::::::oo  l::::l  l::::l   ee::::::::::::ee  r::::rrr:::::::::r  
//   C:::::C              o:::::::::::::::on::::::::::::::nn t:::::::::::::::::t   r:::::::::::::::::r o:::::::::::::::o l::::l  l::::l  e::::::eeeee:::::eer:::::::::::::::::r 
//   C:::::C              o:::::ooooo:::::onn:::::::::::::::ntttttt:::::::tttttt   rr::::::rrrrr::::::ro:::::ooooo:::::o l::::l  l::::l e::::::e     e:::::err::::::rrrrr::::::r
//   C:::::C              o::::o     o::::o  n:::::nnnn:::::n      t:::::t          r:::::r     r:::::ro::::o     o::::o l::::l  l::::l e:::::::eeeee::::::e r:::::r     r:::::r
//   C:::::C              o::::o     o::::o  n::::n    n::::n      t:::::t          r:::::r     rrrrrrro::::o     o::::o l::::l  l::::l e:::::::::::::::::e  r:::::r     rrrrrrr
//   C:::::C              o::::o     o::::o  n::::n    n::::n      t:::::t          r:::::r            o::::o     o::::o l::::l  l::::l e::::::eeeeeeeeeee   r:::::r            
//    C:::::C       CCCCCCo::::o     o::::o  n::::n    n::::n      t:::::t    ttttttr:::::r            o::::o     o::::o l::::l  l::::l e:::::::e            r:::::r            
//     C:::::CCCCCCCC::::Co:::::ooooo:::::o  n::::n    n::::n      t::::::tttt:::::tr:::::r            o:::::ooooo:::::ol::::::ll::::::le::::::::e           r:::::r            
//      CC:::::::::::::::Co:::::::::::::::o  n::::n    n::::n      tt::::::::::::::tr:::::r            o:::::::::::::::ol::::::ll::::::l e::::::::eeeeeeee   r:::::r            
//        CCC::::::::::::C oo:::::::::::oo   n::::n    n::::n        tt:::::::::::ttr:::::r             oo:::::::::::oo l::::::ll::::::l  ee:::::::::::::e   r:::::r            
//           CCCCCCCCCCCCC   ooooooooooo     nnnnnn    nnnnnn          ttttttttttt  rrrrrrr               ooooooooooo   llllllllllllllll    eeeeeeeeeeeeee   rrrrrrr 
//     
// ################################################################################## 
// Game controller calibration
// https://html5gamepad.com/
// ##################################################################################   
public partial class Helicopter_Main : Helicopter_TimestepModel
{
    #region Controller

    // ################################################################################## 
    // variables
    // ################################################################################## 
    int connected_input_devices_count = 0;
    int connected_input_devices_count_old = 0;
    List<int> connected_input_devices_id = new List<int>();
    List<string> connected_input_devices_names = new List<string>();
    List<string> connected_input_devices_type = new List<string>(); // gamepad or joystick
    int selected_input_device_id = 0; // always use first connected device id
    bool calibration_abortable = false;

    public enum State_Calibration
    {
        not_running,
        starting,
        find_center,
        find_min_max,
        go_to_center_again,
        find_collective,
        find_yaw,
        find_pitch,
        find_roll,
        calibrate_all_switches_off,
        calibrate_switch0,
        calibrate_switch1,
        finished,
        abort
    }

    public enum Channel_Type
    {
        is_not_defined,
        is_not_used,
        is_axis,
        is_switch
    }

    public class Axis_Settings
    {
        public int direction = 0; // [-1,+1]

        public float min = +1000; // must be positive during init; // <summary> [0...1] </summary>
        public float max = -1000; //  must be negative during init; // <summary> [0...1] </summary>
        public float center = 0.0f; // [-1...1]

        public float clearance = 0.01f; // [0...1] 
        public float expo = 20.0f; // [%] 
        public float dualrate = 100.0f; // [%] 
        // public float offset = 0.0f; // [0...1] 
    }
    //enum switch_type
    //{
    //    two_states,
    //    three_states
    //}
    public class Switch_Settings
    {
        //public switch_type switch_type;
        public float state0 = 0;
        public float state1 = 0;
        //public float state2 = 0;
    }

    public class Channel_Settings
    {
        public Channel_Type channel_Type = Channel_Type.is_not_defined;
        public Axis_Settings axis_settings = new Axis_Settings();
        public Switch_Settings switch_settings = new Switch_Settings();
    }

    public class stru_Controller_Setup
    {
        public List<Channel_Settings> list_channel_settings = new List<Channel_Settings>();
        public int channel_collective; // which input channel should be collective,
        public int channel_yaw; // which input channel should be yaw
        public int channel_pitch; // which input channel should be pitch
        public int channel_roll; // which input channel should be roll
        public int channel_switch0 = -12345; // which input channel should be switch0
        public int channel_switch1 = -12345; // which input channel should be switch1

        public stru_Controller_Setup()
        {
            list_channel_settings.Clear();
            for (int c = 0; c < 8; c++) // eight channels
                list_channel_settings.Add(new Channel_Settings { }); // ???? do not use .Add(), because Deep_Clone the whole object would double them https://stackoverflow.com/questions/12128130/list-xml-serialization-doubling-elements
        }

        // ???? workaround, implement own deep_copy function 
        public stru_Controller_Setup Deep_Copy()
        {
            stru_Controller_Setup deep_copy_stru_Controller_Setup = new stru_Controller_Setup();
            deep_copy_stru_Controller_Setup.channel_collective = channel_collective;
            deep_copy_stru_Controller_Setup.channel_yaw = channel_yaw;
            deep_copy_stru_Controller_Setup.channel_pitch = channel_pitch;
            deep_copy_stru_Controller_Setup.channel_roll = channel_roll;
            deep_copy_stru_Controller_Setup.channel_switch0 = channel_switch0;
            deep_copy_stru_Controller_Setup.channel_switch1 = channel_switch1;

            for (int c = 0; c < 8; c++) // eight channels
                deep_copy_stru_Controller_Setup.list_channel_settings[c] = list_channel_settings[c].Deep_Clone();

            return deep_copy_stru_Controller_Setup;
        }
    }

    float switch0_status_old; // to detect flank
    float switch1_status_old; // to detect flank

    public stru_Controller_Setup stru_controller_settings_work;
    public stru_Controller_Setup stru_controller_settings_temp;
    
    State_Calibration calibration_state = State_Calibration.not_running;
    List<int> available_channels;


    bool calibration_stepping_with_keyboard_trigger;
    // ################################################################################## 



    // ################################################################################## 
    // handle continue to next calibration step by key
    // ################################################################################## 
    bool Check_If_Next_Calibration_Step_Is_Reached()
    {
        bool return_value = false;

        if (( Keyboard.current.spaceKey.wasPressedThisFrame && !Is_Text_Input_Field_Focused() )  &&  
            (calibration_stepping_with_keyboard_trigger == false) )
        {
            calibration_stepping_with_keyboard_trigger = true;
            return_value = true;
        }

        if (Keyboard.current.spaceKey.wasReleasedThisFrame && !Is_Text_Input_Field_Focused() )
        {
            calibration_stepping_with_keyboard_trigger = false;
        }

        return return_value;
    }
    // ################################################################################## 



    // ##################################################################################
    // Init: Get connected controller
    // ##################################################################################
    void Init_Controller()
    {
        stru_controller_settings_work = new stru_Controller_Setup();
    }
    // ##################################################################################





    // ##################################################################################
    // Init: Get connected controller
    // ##################################################################################
    void Get_Connected_Controller()
    {
        // get names of connected joysticks
        IO_Get_Connected_Gamepads_And_Joysticks();


        // create string for UI
        ui_string_connected_input_devices_names = " Connected input devices: ";
        for (int i = 0; i < connected_input_devices_names.Count; i++)
            ui_string_connected_input_devices_names += " (" + connected_input_devices_id[i] + ")" + connected_input_devices_names[i];


        // handle missing joysticks and missing calibration
        if (connected_input_devices_count > 0)
        {
            if (selected_input_device_id >= (connected_input_devices_count))
            {
                selected_input_device_id = 0;
                UI_show_new_controller_name_flag = true;  
                PlayerPrefs.SetInt("CC___selected_input_device_i", selected_input_device_id);
            }

            gl_controller_connected_flag = true;

            // if no calibartion data for selected controller device is stored
            if (!Get_Controller_Calibration())
            {
                // calibration needed
                calibration_state = State_Calibration.starting;
            }
        }
        else
        {
            // no controller connected
            gl_controller_connected_flag = false;
            gl_pause_flag = true;
        }

    }
    // ##################################################################################




    // ##################################################################################
    // Read controller calibartion data from registry (with PlayerPrefs.)
    // ##################################################################################
    bool Get_Controller_Calibration()
    {
        // check, if current controller is already calibrated and read data from registry with PlayerPrefs 
        stru_controller_settings_temp = new stru_Controller_Setup();

        int k = 0;
        string s, s0 = "CC___" + (connected_input_devices_names[selected_input_device_id]).Replace(' ', '_');
        // get the transmitter channel min, center and max values
        for (int i = 0; i < 8; i++)
        {
            s = s0 + "___Channel_" + i.ToString() + "___direction";
            if (PlayerPrefs.HasKey(s)) { k++; stru_controller_settings_temp.list_channel_settings[i].axis_settings.direction = PlayerPrefs.GetInt(s); }
            s = s0 + "___Channel_" + i.ToString() + "___min";
            if (PlayerPrefs.HasKey(s)) { k++; stru_controller_settings_temp.list_channel_settings[i].axis_settings.min = PlayerPrefs.GetFloat(s); }
            s = s0 + "___Channel_" + i.ToString() + "___max";
            if (PlayerPrefs.HasKey(s)) { k++; stru_controller_settings_temp.list_channel_settings[i].axis_settings.max = PlayerPrefs.GetFloat(s); }
            s = s0 + "___Channel_" + i.ToString() + "___center";
            if (PlayerPrefs.HasKey(s)) { k++; stru_controller_settings_temp.list_channel_settings[i].axis_settings.center = PlayerPrefs.GetFloat(s); }
        }

        // get the right transmitter channel assignment
        s = s0 + "___Collective";
        if (PlayerPrefs.HasKey(s)) { k++; stru_controller_settings_temp.channel_collective = PlayerPrefs.GetInt(s); }
        s = s0 + "___Yaw";
        if (PlayerPrefs.HasKey(s)) { k++; stru_controller_settings_temp.channel_yaw = PlayerPrefs.GetInt(s); }
        s = s0 + "___Pitch";
        if (PlayerPrefs.HasKey(s)) { k++; stru_controller_settings_temp.channel_pitch = PlayerPrefs.GetInt(s); }
        s = s0 + "___Roll";
        if (PlayerPrefs.HasKey(s)) { k++; stru_controller_settings_temp.channel_roll = PlayerPrefs.GetInt(s); }
        s = s0 + "___Switch0";
        if (PlayerPrefs.HasKey(s)) { k++; stru_controller_settings_temp.channel_switch0 = PlayerPrefs.GetInt(s); }
        s = s0 + "___Switch1";
        if (PlayerPrefs.HasKey(s)) { k++; stru_controller_settings_temp.channel_switch1 = PlayerPrefs.GetInt(s); }


        s = s0 + "___Switch0__OFF";
        if (PlayerPrefs.HasKey(s)) k++;
        s = s0 + "___Switch0__ON";
        if (PlayerPrefs.HasKey(s)) k++;
        s = s0 + "___Switch1__OFF";
        if (PlayerPrefs.HasKey(s)) k++;
        s = s0 + "___Switch1__ON";
        if (PlayerPrefs.HasKey(s)) k++;


        // get switch states only possible, if channel_switch0 or channel_switch1 has valid channel-id values (0...7)
        if (stru_controller_settings_temp.channel_switch0 != -12345)
        {
            int i = stru_controller_settings_temp.channel_switch0;
            s = s0 + "___Switch0__OFF";
            if (PlayerPrefs.HasKey(s)) { stru_controller_settings_temp.list_channel_settings[i].switch_settings.state0 = PlayerPrefs.GetFloat(s); } 
            s = s0 + "___Switch0__ON";
            if (PlayerPrefs.HasKey(s)) { stru_controller_settings_temp.list_channel_settings[i].switch_settings.state1 = PlayerPrefs.GetFloat(s); }
        }

        if (stru_controller_settings_temp.channel_switch1 != -12345)
        {
            int i = stru_controller_settings_temp.channel_switch1;
            s = s0 + "___Switch1__OFF";
            if (PlayerPrefs.HasKey(s)) { stru_controller_settings_temp.list_channel_settings[i].switch_settings.state0 = PlayerPrefs.GetFloat(s); } 
            s = s0 + "___Switch1__ON";
            if (PlayerPrefs.HasKey(s)) { stru_controller_settings_temp.list_channel_settings[i].switch_settings.state1 = PlayerPrefs.GetFloat(s); } 
        }

        if (k == (4 + 8 * 3) + (6 + 4) || k == (8 * 4) + (6 + 4)) // found all informations
        {
            // controller calibration data found
            //UnityEngine.Debug.Log("Controller calibration data found.");
            stru_controller_settings_work = stru_controller_settings_temp.Deep_Copy();
            return true;
        }
        else
        {
            // controller calibration needed
            //UnityEngine.Debug.Log("Controller calibration needed.");
            stru_controller_settings_work.list_channel_settings.Clear();
            stru_controller_settings_work = new stru_Controller_Setup(); // stru_controller_settings_empty;
            return false;
        }
    }
    // ##################################################################################




    // ##################################################################################
    // Write controller calibartion data to registry (with PlayerPrefs.)
    // ##################################################################################
    void Set_Controller_Calibration(stru_Controller_Setup stru_controller_settings)
    {
        string s, s0 = "CC___" + (connected_input_devices_names[selected_input_device_id]).Replace(' ', '_');
        // set the transmitter channel min, center and max values
        for (int i = 0; i < 8; i++)
        {
            s = s0 + "___Channel_" + i.ToString() + "___direction";
            PlayerPrefs.SetInt(s, stru_controller_settings.list_channel_settings[i].axis_settings.direction);
            s = s0 + "___Channel_" + i.ToString() + "___min";
            PlayerPrefs.SetFloat(s, stru_controller_settings.list_channel_settings[i].axis_settings.min);
            s = s0 + "___Channel_" + i.ToString() + "___max";
            PlayerPrefs.SetFloat(s, stru_controller_settings.list_channel_settings[i].axis_settings.max);
            s = s0 + "___Channel_" + i.ToString() + "___center";
            PlayerPrefs.SetFloat(s, stru_controller_settings.list_channel_settings[i].axis_settings.center);
        }

        // set the transmitter channel assignment
        s = s0 + "___Collective";
        PlayerPrefs.SetInt(s, stru_controller_settings.channel_collective);
        s = s0 + "___Yaw";
        PlayerPrefs.SetInt(s, stru_controller_settings.channel_yaw);
        s = s0 + "___Pitch";
        PlayerPrefs.SetInt(s, stru_controller_settings.channel_pitch);
        s = s0 + "___Roll";
        PlayerPrefs.SetInt(s, stru_controller_settings.channel_roll);
        s = s0 + "___Switch0";
        PlayerPrefs.SetInt(s, stru_controller_settings.channel_switch0);
        s = s0 + "___Switch1";
        PlayerPrefs.SetInt(s, stru_controller_settings.channel_switch1);


        // set switch states only possible, if channel_switch0 or channel_switch1 has valid channel-id values (0...7)
        if (stru_controller_settings.channel_switch0 != -12345)
        {
            s = s0 + "___Switch0__OFF";
            PlayerPrefs.SetFloat(s, stru_controller_settings.list_channel_settings[stru_controller_settings.channel_switch0].switch_settings.state0);
            s = s0 + "___Switch0__ON";
            PlayerPrefs.SetFloat(s, stru_controller_settings.list_channel_settings[stru_controller_settings.channel_switch0].switch_settings.state1);
        }
        else
        {
            s = s0 + "___Switch0__OFF";
            PlayerPrefs.SetFloat(s, 0);
            s = s0 + "___Switch0__ON";
            PlayerPrefs.SetFloat(s, 0);
        }

        if (stru_controller_settings.channel_switch1 != -12345)
        {
            s = s0 + "___Switch1__OFF";
            PlayerPrefs.SetFloat(s, stru_controller_settings.list_channel_settings[stru_controller_settings.channel_switch1].switch_settings.state0);
            s = s0 + "___Switch1__ON";
            PlayerPrefs.SetFloat(s, stru_controller_settings.list_channel_settings[stru_controller_settings.channel_switch1].switch_settings.state1);
        }
        else
        {
            s = s0 + "___Switch1__OFF";
            PlayerPrefs.SetFloat(s, 0);
            s = s0 + "___Switch1__ON";
            PlayerPrefs.SetFloat(s, 0);
        }
    }
    // ##################################################################################




    // ##################################################################################
    // Calibrate controller
    // ##################################################################################
    void Controller_Calibration() // stored by Unity in registry under [\HKEY_CURRENT_USER\Software\...\Free RC Helicopter Simulator]
    {

        // #################################################################################
        // start calibration
        // #################################################################################
        if (calibration_state == State_Calibration.starting)
        {
            calibration_state = State_Calibration.find_center;
            available_channels = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 }; // list will be reduced during calibration by the channels already assigned: avoids double selection of channel

            // UI 
            ui_controller_calibration_flag = true; // show UI
            ui_controller_calibration_panel_text.text = "Please move every axis to CENTER position and press space bar.";
            ui_controller_calibration_panel_image.sprite = Resources.Load<Sprite>("Sprites/Controller_center");
            ui_controller_calibration_panel_device_name_text.text = connected_input_devices_names[selected_input_device_id];
            Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_center_position.wav");

            // eight channels
            //stru_controller_settings.list_channel_settings.Clear();
            stru_controller_settings_temp = new stru_Controller_Setup();

            calibration_stepping_with_keyboard_trigger = false;
        }
        // #################################################################################




        // #################################################################################
        // update UI channel status
        // #################################################################################
        for (int i = 0; i < ui_controller_calibration_panel_channel_slider.Length; i++)
        {
            ui_controller_calibration_panel_channel_slider[i].value = input_channel_used_in_game[i];
            ui_controller_calibration_panel_channel_text_value[i].text = input_channel_used_in_game[i].ToString();
        }
        // #################################################################################




        // #################################################################################
        // run calibration
        // #################################################################################
        if (calibration_state > State_Calibration.starting)
        {

            // #################################################################################
            // Find center position
            // #################################################################################
            if (calibration_state == State_Calibration.find_center)
            {
                if (Check_If_Next_Calibration_Step_Is_Reached())
                {
                    for (int i = 0; i < 8; i++) // find central value
                        stru_controller_settings_temp.list_channel_settings[i].axis_settings.center = input_channel_used_in_game[i];

                    calibration_state = State_Calibration.find_min_max;
                }
                if (calibration_state == State_Calibration.find_min_max)
                {
                    // UI       
                    ui_controller_calibration_panel_text.text = "Please move every axis to MIN and MAX and press space bar."; 
                    ui_controller_calibration_panel_image.sprite = Resources.Load<Sprite>("Sprites/Controller_min_max");

                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_min_max.wav");
                }
            }
            // #################################################################################






            // #################################################################################
            // find min and max range for every channel
            // #################################################################################
            if (calibration_state == State_Calibration.find_min_max)
            {
                for (int i = 0; i < 8; i++) // find min and max values
                {
                    if (input_channel_used_in_game[i] > stru_controller_settings_temp.list_channel_settings[i].axis_settings.max)
                        stru_controller_settings_temp.list_channel_settings[i].axis_settings.max = input_channel_used_in_game[i];
                    if (input_channel_used_in_game[i] < stru_controller_settings_temp.list_channel_settings[i].axis_settings.min)
                        stru_controller_settings_temp.list_channel_settings[i].axis_settings.min = input_channel_used_in_game[i];
                }

                if (Check_If_Next_Calibration_Step_Is_Reached()) // save found min max values
                {
                    for (int i = 0; i < 8; i++)
                    {
                        // if axis has not moved (because it maybe a controller switch or button), then the max and min values are equal. Noise may lead to a difference, that is captured by the threshold of 0.05f.
                        if (Mathf.Abs(stru_controller_settings_temp.list_channel_settings[i].axis_settings.max - stru_controller_settings_temp.list_channel_settings[i].axis_settings.min) < 0.05f)
                        {
                            stru_controller_settings_temp.list_channel_settings[i].channel_Type = Channel_Type.is_not_used; // ... so far
                            stru_controller_settings_temp.list_channel_settings[i].axis_settings.max = 1; // not used later
                            stru_controller_settings_temp.list_channel_settings[i].axis_settings.center = 0; // not used later
                            stru_controller_settings_temp.list_channel_settings[i].axis_settings.min = -1; // not used later
                        }
                    }
                    calibration_state = State_Calibration.go_to_center_again;
                    ui_controller_calibration_panel_text.text = "Please move all sticks back to CENTER and press space bar.";
                    ui_controller_calibration_panel_image.sprite = Resources.Load<Sprite>("Sprites/Controller_center");
                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_axes_back_to_center_position.wav");
                }

            }
            // #################################################################################




            // #################################################################################
            // All sticks back to center
            // #################################################################################
            if (calibration_state == State_Calibration.go_to_center_again)
            {
                if (Check_If_Next_Calibration_Step_Is_Reached()) 
                {
                    calibration_state = State_Calibration.find_collective;
                    ui_controller_calibration_panel_text.text = "Please move collective upwards...";
                    ui_controller_calibration_panel_image.sprite = Resources.Load<Sprite>("Sprites/Collective");
                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_collective.wav");
                }
            }
            // #################################################################################




            // #################################################################################
            // assign controller channel to collective,yaw,pitch,roll
            // #################################################################################
            // Collective
            // #################################################################################
            if (calibration_state == State_Calibration.find_collective)
            {
                for (int i = 0; i < available_channels.Count; i++)
                {
                    int c = available_channels[i];
                    if (stru_controller_settings_temp.list_channel_settings[c].channel_Type != Channel_Type.is_not_used)
                    {
                        float center = stru_controller_settings_temp.list_channel_settings[c].axis_settings.center;
                        float max = stru_controller_settings_temp.list_channel_settings[c].axis_settings.max;
                        float min = stru_controller_settings_temp.list_channel_settings[c].axis_settings.min;
                        if (input_channel_used_in_game[c] > (center + (max - center) * 0.500f))
                        {
                            stru_controller_settings_temp.list_channel_settings[c].channel_Type = Channel_Type.is_axis;
                            stru_controller_settings_temp.channel_collective = c;
                            stru_controller_settings_temp.list_channel_settings[c].axis_settings.direction = +1;
                            available_channels.RemoveAt(i); // remove already assigned channel
                            calibration_state = State_Calibration.find_yaw;
                            break;
                        }
                        if (input_channel_used_in_game[c] < (center - (center - min) * 0.500f))
                        {
                            stru_controller_settings_temp.list_channel_settings[c].channel_Type = Channel_Type.is_axis;
                            stru_controller_settings_temp.channel_collective = c;
                            stru_controller_settings_temp.list_channel_settings[c].axis_settings.direction = -1;
                            available_channels.RemoveAt(i); // remove already assigned channel
                            calibration_state = State_Calibration.find_yaw;
                            break;
                        }
                    }
                }
                if (calibration_state == State_Calibration.find_yaw)
                {
                    ui_controller_calibration_panel_text.text = "Please move yaw in counterclockwise direction...";
                    ui_controller_calibration_panel_image.sprite = Resources.Load<Sprite>("Sprites/Yaw");
                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_yaw.wav");
                }
            }
            // #################################################################################




            // #################################################################################
            // Yaw
            // #################################################################################
            if (calibration_state == State_Calibration.find_yaw)
            {
                for (int i = 0; i < available_channels.Count; i++)
                {
                    int c = available_channels[i];
                    if (stru_controller_settings_temp.list_channel_settings[c].channel_Type != Channel_Type.is_not_used)
                    {
                        float center = stru_controller_settings_temp.list_channel_settings[c].axis_settings.center;
                        float max = stru_controller_settings_temp.list_channel_settings[c].axis_settings.max;
                        float min = stru_controller_settings_temp.list_channel_settings[c].axis_settings.min;

                        if (input_channel_used_in_game[c] > (center + (max - center) * 0.500f))
                        {
                            stru_controller_settings_temp.list_channel_settings[c].channel_Type = Channel_Type.is_axis;
                            stru_controller_settings_temp.channel_yaw = c;
                            stru_controller_settings_temp.list_channel_settings[c].axis_settings.direction = +1;
                            available_channels.RemoveAt(i); // remove already assigned channel
                            calibration_state = State_Calibration.find_pitch;
                            break;
                        }
                        if (input_channel_used_in_game[c] < (center - (center - min) * 0.500f))
                        {
                            stru_controller_settings_temp.list_channel_settings[c].channel_Type = Channel_Type.is_axis;
                            stru_controller_settings_temp.channel_yaw = c;
                            stru_controller_settings_temp.list_channel_settings[c].axis_settings.direction = -1;
                            available_channels.RemoveAt(i); // remove already assigned channel
                            calibration_state = State_Calibration.find_pitch;
                            break;
                        }
                    }
                }
                if (calibration_state == State_Calibration.find_pitch)
                {
                    ui_controller_calibration_panel_text.text = "Please move pitch nose up...";
                    ui_controller_calibration_panel_image.sprite = Resources.Load<Sprite>("Sprites/Pitch");
                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_pitch.wav");
                }
            }
            // #################################################################################




            // #################################################################################
            // Pitch
            // #################################################################################
            if (calibration_state == State_Calibration.find_pitch)
            {
                for (int i = 0; i < available_channels.Count; i++)
                {
                    int c = available_channels[i];
                    if (stru_controller_settings_temp.list_channel_settings[c].channel_Type != Channel_Type.is_not_used)
                    {
                        float center = stru_controller_settings_temp.list_channel_settings[c].axis_settings.center;
                        float max = stru_controller_settings_temp.list_channel_settings[c].axis_settings.max;
                        float min = stru_controller_settings_temp.list_channel_settings[c].axis_settings.min;
                        if (input_channel_used_in_game[c] > (center + (max - center) * 0.500f))
                        {
                            stru_controller_settings_temp.list_channel_settings[c].channel_Type = Channel_Type.is_axis;
                            stru_controller_settings_temp.channel_pitch = c;
                            stru_controller_settings_temp.list_channel_settings[c].axis_settings.direction = +1;
                            available_channels.RemoveAt(i); // remove already assigned channel
                            calibration_state = State_Calibration.find_roll;
                            break;
                        }
                        if (input_channel_used_in_game[c] < (center - (center - min) * 0.500f))
                        {
                            stru_controller_settings_temp.list_channel_settings[c].channel_Type = Channel_Type.is_axis;
                            stru_controller_settings_temp.channel_pitch = c;
                            stru_controller_settings_temp.list_channel_settings[c].axis_settings.direction = -1;
                            available_channels.RemoveAt(i); // remove already assigned channel
                            calibration_state = State_Calibration.find_roll;
                            break;
                        }
                    }
                }
                if (calibration_state == State_Calibration.find_roll)
                {
                    ui_controller_calibration_panel_text.text = "Please roll to right...";
                    ui_controller_calibration_panel_image.sprite = Resources.Load<Sprite>("Sprites/Roll");
                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_roll.wav");
                }
            }
            // #################################################################################




            // #################################################################################
            // Roll
            // #################################################################################
            if (calibration_state == State_Calibration.find_roll)
            {
                for (int i = 0; i < available_channels.Count; i++)
                {
                    int c = available_channels[i];
                    if (stru_controller_settings_temp.list_channel_settings[c].channel_Type != Channel_Type.is_not_used)
                    {
                        float center = stru_controller_settings_temp.list_channel_settings[c].axis_settings.center;
                        float max = stru_controller_settings_temp.list_channel_settings[c].axis_settings.max;
                        float min = stru_controller_settings_temp.list_channel_settings[c].axis_settings.min;
                        if (input_channel_used_in_game[c] > (center + (max - center) * 0.500f))
                        {
                            stru_controller_settings_temp.list_channel_settings[c].channel_Type = Channel_Type.is_axis;
                            stru_controller_settings_temp.channel_roll = c;
                            stru_controller_settings_temp.list_channel_settings[c].axis_settings.direction = +1;
                            available_channels.RemoveAt(i);
                            calibration_state = State_Calibration.calibrate_all_switches_off;
                            break;
                        }
                        if (input_channel_used_in_game[c] < (center - (center - min) * 0.500f))
                        {
                            stru_controller_settings_temp.list_channel_settings[c].channel_Type = Channel_Type.is_axis;
                            stru_controller_settings_temp.channel_roll = c;
                            stru_controller_settings_temp.list_channel_settings[c].axis_settings.direction = -1;
                            available_channels.RemoveAt(i);
                            calibration_state = State_Calibration.calibrate_all_switches_off;
                            break;
                        }
                    }
                }
                if (calibration_state == State_Calibration.calibrate_all_switches_off)
                {
                    ui_controller_calibration_panel_text.text = "Please move all switches to OFF position and press space bar.";
                    ui_controller_calibration_panel_image.sprite = Resources.Load<Sprite>("Sprites/Switch_Position_0");
                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_switches_off.wav");

                    calibration_stepping_with_keyboard_trigger = false;
                }

            }
            // #################################################################################







            // #################################################################################
            // all switches off
            // #################################################################################
            if (calibration_state == State_Calibration.calibrate_all_switches_off)
            {
                if (Check_If_Next_Calibration_Step_Is_Reached())
                {
                    for (int i = 0; i < available_channels.Count; i++)
                    {
                        int c = available_channels[i];
                        float value = input_channel_used_in_game[c];
                        stru_controller_settings_temp.list_channel_settings[c].switch_settings.state0 = value;
                    }
                    ui_controller_calibration_panel_text.text = "Please move switch-0 (motor on/off) to ON position and press space bar.";
                    ui_controller_calibration_panel_image.sprite = Resources.Load<Sprite>("Sprites/Switch_Position_2");

                    calibration_state = State_Calibration.calibrate_switch0;
                    Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_switch0_on.wav");
                }
            }
            // #################################################################################




            // #################################################################################
            // switch0 on
            // #################################################################################
            if (calibration_state == State_Calibration.calibrate_switch0)
            {
                if (Check_If_Next_Calibration_Step_Is_Reached())
                {
                    for (int i = 0; i < available_channels.Count; i++)
                    {
                        int c = available_channels[i];
                        float delta = Mathf.Abs(input_channel_used_in_game[c] - stru_controller_settings_temp.list_channel_settings[c].switch_settings.state0);

                        if (delta > 0.1f)
                        {
                            stru_controller_settings_temp.list_channel_settings[c].channel_Type = Channel_Type.is_switch;
                            stru_controller_settings_temp.list_channel_settings[c].switch_settings.state1 = input_channel_used_in_game[c];
                            stru_controller_settings_temp.channel_switch0 = c;
                            available_channels.RemoveAt(i);
                            calibration_state = State_Calibration.calibrate_switch1;
                            ui_controller_calibration_panel_text.text = "Please move switch-1 (landing gear) to ON position and press space bar.";
                            ui_controller_calibration_panel_image.sprite = Resources.Load<Sprite>("Sprites/Switch_Position_2");
                            Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_switch1_on.wav");
                            break;
                        }
                    }

                    // if no switch delta has been detected
                    if (calibration_state == State_Calibration.calibrate_switch0)
                    {
                        for (int i = 0; i < available_channels.Count; i++)
                        {
                            int c = available_channels[i];
                            stru_controller_settings_temp.list_channel_settings[c].channel_Type = Channel_Type.is_not_used;
                            stru_controller_settings_temp.channel_switch0 = -12345;
                            stru_controller_settings_temp.channel_switch1 = -12345;
                        }
                        calibration_state = State_Calibration.finished; // if switch0 is not used also skipp switch1
                    }

                }
            }
            // #################################################################################




            // #################################################################################
            // switch1 on
            // #################################################################################
            if (calibration_state == State_Calibration.calibrate_switch1)
            {
                if (Check_If_Next_Calibration_Step_Is_Reached())
                {
                    for (int i = 0; i < available_channels.Count; i++)
                    {
                        int c = available_channels[i];
                        float delta = Mathf.Abs(input_channel_used_in_game[c] - stru_controller_settings_temp.list_channel_settings[c].switch_settings.state0);
                        if (delta > 0.1f)
                        {
                            stru_controller_settings_temp.list_channel_settings[c].channel_Type = Channel_Type.is_switch;
                            stru_controller_settings_temp.list_channel_settings[c].switch_settings.state1 = input_channel_used_in_game[c];
                            stru_controller_settings_temp.channel_switch1 = c;
                            available_channels.RemoveAt(i);
                            calibration_state = State_Calibration.finished;
                            //ui_controller_calibration_panel_text.text = "Please move switch 1 to ON position within " + (helicopter_ODE.par.simulation.calibration_duration.val).ToString() + " sec.";
                            //ui_controller_calibration_panel_image.sprite = Resources.Load<Sprite>("Sprites/Switch_Position_2");
                            //Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_finished.wav");
                            break;
                        }
                    }

                    // if no switch delta has been detected
                    if (calibration_state == State_Calibration.calibrate_switch1)
                    {
                        for (int i = 0; i < available_channels.Count; i++)
                        {
                            int c = available_channels[i];
                            stru_controller_settings_temp.list_channel_settings[c].channel_Type = Channel_Type.is_not_used;
                            stru_controller_settings_temp.channel_switch1 = -12345;
                            stru_controller_settings_temp.list_channel_settings[c].switch_settings.state0 = 0;
                            stru_controller_settings_temp.list_channel_settings[c].switch_settings.state1 = 0;
                        }
                        calibration_state = State_Calibration.finished;
                    }
                }
            }
            // #################################################################################







            // #################################################################################
            // we are finished with controller calibration
            // #################################################################################
            if (calibration_state == State_Calibration.finished)
            {
                Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_finished.wav");

                // if calibraten is complete, store the result with PlayerPrefs
                Set_Controller_Calibration(stru_controller_settings_temp);

                // testvise reload immediately the relusts from PlayerPrefs
                Get_Controller_Calibration();

                //UnityEngine.Debug.Log("Calibration finished.");
                ui_controller_calibration_flag = false;
                calibration_state = State_Calibration.not_running;
                calibration_abortable = false;
                gl_pause_flag = false;

                //// debug
                //for (int i = 0; i < 8; i++)
                //{
                //    if (stru_controller_settings.list_channel_settings[i].channel_Type == Channel_Type.is_axis)
                //    {
                //        UnityEngine.Debug.Log("xxxx  value " + i.ToString() + "=" + input_channel_used_in_game[i] +
                //                              "    xxxx  min " + i.ToString() + "=" + stru_controller_settings.list_channel_settings[i].axis_settings.min +
                //                              "    xxxx  center " + i.ToString() + "=" + stru_controller_settings.list_channel_settings[i].axis_settings.center +
                //                              "    xxxx  max " + i.ToString() + "=" + stru_controller_settings.list_channel_settings[i].axis_settings.max +
                //                              "          Channel_Type " + i.ToString() + "=" + stru_controller_settings.list_channel_settings[i].channel_Type);
                //    }
                //}
            }
            // #################################################################################


            // #################################################################################
            // controller calibration aborted
            // #################################################################################
            if (calibration_abortable == true && calibration_state == State_Calibration.abort)
            {
                Commentator_Play_Audio(Application.streamingAssetsPath + "/Audio/Calibration_Sounds/female_voice_callibration_aborted.wav");
                
                ui_controller_calibration_flag = false;
                calibration_state = State_Calibration.not_running;
                calibration_abortable = false;
                gl_pause_flag = false;
            }
            // #################################################################################

        }
    }
    // ##################################################################################




    // ##################################################################################
    /// <summary> controller input signal scaling </summary>
    // ##################################################################################
    float Scale_Min_Max_with_Clearance(float input, Axis_Settings axis_settings)
    {
        float center_plus_clearance = axis_settings.center + axis_settings.clearance;
        float center_minus_clearance = axis_settings.center - axis_settings.clearance;

        if (input >= center_plus_clearance)
        {
            //  y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1;
            return ((1.0f - 0.0f) / (axis_settings.max - center_plus_clearance)) * (input - center_plus_clearance) + 0.0f;
        }
        else
        {
            if (input <= center_minus_clearance)
            {
                //  y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1;
                return ((0.0f - (-1.0f)) / (center_minus_clearance - axis_settings.min)) * (input - axis_settings.min) + (-1.0f);
            }
            else
            {
                return 0.0f;
            }
        }
    }
    // ##################################################################################





    // ##################################################################################
    /// <summary> controller input signal expo and dualrate with scaling to min/max </summary>
    // ##################################################################################
    float Expo_and_Dualrate(float input, Axis_Settings axis_settings)
    {
        input = Scale_Min_Max_with_Clearance(input, axis_settings) * axis_settings.direction;

        const float max_expo = 2.7182818f; //how much EXPO do you want as 100%?
        float temp = Mathf.Abs(axis_settings.expo) / 100.0f * max_expo; //
        return Mathf.Clamp((input * Mathf.Exp(Mathf.Abs(temp * input)) * 1.0f / Mathf.Exp(temp)) * (Mathf.Abs(axis_settings.dualrate) / 100f), -1.0f, 1.0f);
    }
    // ##################################################################################

    #endregion




}




