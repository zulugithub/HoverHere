// ##################################################################################
// Free RC helicopter Simulator
// 20.01.2020 
// Copyright (c) zulu
//
// Unity c# code
// ##################################################################################
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.IO;
using System.Text;
using Parameter;
using System.Diagnostics;
using Common;
using PlotKids.Infra; // automatic version numbering https://forum.unity.com/threads/get-build-number-from-a-script.641725/

// ##################################################################################
//    UUUUUUUU     UUUUUUUUIIIIIIIIII
//    U::::::U     U::::::UI::::::::I
//    U::::::U     U::::::UI::::::::I
//    UU:::::U     U:::::UUII::::::II
//     U:::::U     U:::::U   I::::I  
//     U:::::D     D:::::U   I::::I  
//     U:::::D     D:::::U   I::::I  
//     U:::::D     D:::::U   I::::I  
//     U:::::D     D:::::U   I::::I  
//     U:::::D     D:::::U   I::::I  
//     U:::::D     D:::::U   I::::I  
//     U::::::U   U::::::U   I::::I  
//     U:::::::UUU:::::::U II::::::II
//      UU:::::::::::::UU  I::::::::I
//        UU:::::::::UU    I::::::::I
//          UUUUUUUUU      IIIIIIIIII
// ##################################################################################
// second part of Helicopter_Main.cs with Parameter-UI handling
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
    #region fields
    // ##################################################################################
    // class fields
    // ##################################################################################
    //[Header("UI Parameter Objects")]
    GameObject ui_prefab_headline; // prefab for headlines
    GameObject ui_prefab_bool; // prefab for boolian values
    GameObject ui_prefab_scalar; // prefab for scalar values (stru_float, stru_int) 
    GameObject ui_prefab_vector3; // prefab for scalar Vector3
    GameObject ui_prefab_vector3_list_setting; // prefab for size info/setting of Vector3-List
    GameObject ui_prefab_vector3_list_vector3; // prefab: Vector3-List's one Vector3
    GameObject ui_prefab_list; // prefab: dropdown list

    /// <summary>
    /// folder with parameter and settings for "transmitter_and_helicopter": transmitter and helicopter
    /// c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedHelicopterParametersets\
    /// </summary>
    private string folder_saved_parameter_for_transmitter_and_helicopter = string.Empty;
    /// <summary>
    /// folder with parameter and settings for sceneries
    /// c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedSceneriesParametersets\
    /// </summary>
    private string folder_saved_parameter_for_sceneries = string.Empty;

    // satuts flags 
    bool ui_welcome_panel_flag = false;
    bool ui_info_panel_flag = false;
    bool ui_no_controller_panel_flag = false;
    bool ui_loading_panel_flag = false;
    //bool ui_debug_panel_flag = false;
    int ui_debug_panel_state = 0;
    int ui_debug_panel_state_old = 0;
    bool ui_parameter_panel_flag = false;
    bool ui_pause_flag = false;
    bool ui_exit_panel_flag = false;
    bool ui_controller_calibration_flag = false;
    bool ui_helicopter_selection_menu_flag = false;
    bool ui_scenery_selection_menu_flag = false;
    bool ui_pie_menu_flag = false;


    // Main UI Canvas GameObject
    GameObject ui_canvas;
 
    // UI welcome panel
    Transform ui_info_panel;
    Button ui_button_close_info_panel;

    // UI no_controller_panel 
    Transform ui_no_controller_panel;
    
    // UI loading panel
    Transform ui_loading_panel;

    // UI select helicopter from list
    Transform ui_helicopter_selection_panel;
    Button ui_button_close_helicopter_selection_panel;

    // UI select scenery from list
    Transform ui_scenery_selection_panel;
    Button ui_button_close_scenery_selection_panel;

    // UI info panel
    Transform ui_welcome_panel;
    Button ui_button_close_welcome_panel;

    // UI debug panel
    Transform ui_debug_panel;
    Transform ui_debug_lines;
    Text ui_debug_text;

    // UI exit panel
    Transform ui_exit_menu;
    Button ui_exit_menu_exit_button;
    Button ui_exit_menu_exit_game_button;

    // UI select menu  
    Transform ui_pie_menu;
    Button ui_pie_menu_welcome_button;
    Button ui_pie_menu_parameter_button;
    Button ui_pie_menu_controller_calibration_button;
    Button ui_pie_menu_select_scenery_button;
    Button ui_pie_menu_select_helicopter_button;
    Button ui_pie_menu_keyboard_shorcuts_button;
    Button ui_pie_menu_exit_game_button;
    Button ui_pie_menu_exit_button;
    
    // UI pause menu
    Transform ui_pause_menu;

    // UI controller calibration
    Transform ui_controller_calibration_panel;
    Text ui_controller_calibration_panel_text;
    Text ui_controller_calibration_panel_device_name_text;
    Image ui_controller_calibration_panel_image;
    Slider[] ui_controller_calibration_panel_channel_slider = new Slider[8];
    Text[] ui_controller_calibration_panel_channel_text_value = new Text[8];
    Button ui_button_close_controller_calibration_panel;
    string ui_string_connected_input_devices_names;


    // UI parameter menu
    Transform ui_parameter;
    Transform ui_parameter_panel;
    Transform ui_scroll_view_content;
    Scrollbar ui_scroll_view_scrollbar;
    Transform ui_overlaid_parameterlist;

    Transform ui_panel_load_save;
    Transform ui_panel_simulation;
    Dropdown ui_dropdown_load;
    Button ui_button_save;
    Button ui_button_delete;
    Toggle ui_toggle_filter_names;
    Text ui_text_fullpath_echo;
    InputField ui_inputfield_save;
    Text ui_text_title;


    Button ui_button_close_parameter;

    Button ui_button_open_folder;
    Button ui_button_open_regedit;
    Button ui_button_open_folder_screenshots;
    
    Button ui_button_simulation;
    Button ui_button_scenery;
    Button ui_button_transmitter;
    Button ui_button_helicopter;
    Button ui_button_tuning;
    enum Available_Tabs
    {
        simulation,
        scenery,
        transmitter,
        helicopter,
        tuning
    }
    Available_Tabs ui_which_tab_is_selected;

    int prefabs_vertical_position_on_scroll_view_ui = 0;
    int prefabs_vertical_position_on_scroll_view_ui_START_VALUE = 70;
    int prefabs_vertical_position_on_screen_overlay_ui = 0;
    int prefabs_vertical_position_on_screen_overlay_ui_START_VALUE = 5;

    public enum Ui_Styles
    {
        ui_scroll_view_content,
        ui_overlaid_parameterlist
    };

   
    string ui_dropdown_actual_selected_scenery_xml_filename;
    string ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename;

    // frames per second
    Transform ui_frames_per_sec_text;
    public int frames_per_sec { get; protected set; }
    public float frames_per_sec_display_update_frequency = 0.5f; // [Hz]
    Coroutine coroutine_frames_per_second_running = null;

    // XR 3D-arrow
    GameObject ui_canvas_xr_mouse_arrow;

    bool UI_show_new_controller_name_flag = false;
    float UI_show_new_controller_name_time;

    public PlotBuildSettings plot_build_setting;
    // ##################################################################################
    #endregion






    // ##################################################################################
    //    IIIIIIIIII                  iiii          tttt          
    //    I::::::::I                 i::::i      ttt:::t          
    //    I::::::::I                  iiii       t:::::t          
    //    II::::::II                             t:::::t          
    //      I::::Innnn  nnnnnnnn    iiiiiiittttttt:::::ttttttt    
    //      I::::In:::nn::::::::nn  i:::::it:::::::::::::::::t    
    //      I::::In::::::::::::::nn  i::::it:::::::::::::::::t    
    //      I::::Inn:::::::::::::::n i::::itttttt:::::::tttttt    
    //      I::::I  n:::::nnnn:::::n i::::i      t:::::t          
    //      I::::I  n::::n    n::::n i::::i      t:::::t          
    //      I::::I  n::::n    n::::n i::::i      t:::::t          
    //      I::::I  n::::n    n::::n i::::i      t:::::t    tttttt
    //    II::::::IIn::::n    n::::ni::::::i     t::::::tttt:::::t
    //    I::::::::In::::n    n::::ni::::::i     tt::::::::::::::t
    //    I::::::::In::::n    n::::ni::::::i       tt:::::::::::tt
    //    IIIIIIIIIInnnnnn    nnnnnniiiiiiii         ttttttttttt                                                 
    // ##################################################################################s
    #region initialize
    // ##################################################################################
    // Use this for initialization
    // ##################################################################################
    // is called by Start() in main partial class file
    void UI_Initialize()
    {
        // find Unity objects by their names
        UI_Initialize_UI_GameObjects();

        UI_Initialize_Selection_UI(Ui_Selection_Type.helicopter);
        UI_Initialize_Selection_UI(Ui_Selection_Type.scenery);


        //// search ALL UI canvas children (recursively) and change image component
        //foreach (Image im in GameObject.Find("Canvas").GetComponentsInChildren<Image>(true))
        //{
        //
        //    if (im.sprite != null)
        //    {
        //        if (string.Equals(im.sprite.name, "Carbon-Fiber"))
        //        {
        //            if (im.pixelsPerUnitMultiplier == 6)
        //            {
        //                //im.sprite = Resources.Load<Sprite>("Sprites/CarbonFiberUkraineBlue");
        //                //im.color = new Color(0.6415094f, 0.6415094f , 0.6415094f);
        //               // im.pixelsPerUnitMultiplier = 1;
        //            }
        //            else
        //            {
        //                im.sprite = Resources.Load<Sprite>("Sprites/CarbonFiberUkraine");
        //                im.color = Color.white;
        //            }
        //
        //            if (im.pixelsPerUnitMultiplier == 5)
        //            {
        //                im.pixelsPerUnitMultiplier = 2;
        //            }
        //        }
        //    }
        //}

    }
    // ##################################################################################
    #endregion






    // ##################################################################################
    //      SSSSSSSSSSSSSSS                                                               LLLLLLLLLLL                                                           d::::::d
    //    SS:::::::::::::::S                                                              L:::::::::L                                                           d::::::d
    //   S:::::SSSSSS::::::S                                                              L:::::::::L                                                           d::::::d
    //   S:::::S     SSSSSSS                                                              LL:::::::LL                                                           d:::::d 
    //   S:::::S              aaaaaaaaaaaaavvvvvvv           vvvvvvv eeeeeeeeeeee           L:::::L                  ooooooooooo     aaaaaaaaaaaaa      ddddddddd:::::d 
    //   S:::::S              a::::::::::::av:::::v         v:::::vee::::::::::::ee         L:::::L                oo:::::::::::oo   a::::::::::::a   dd::::::::::::::d 
    //    S::::SSSS           aaaaaaaaa:::::av:::::v       v:::::ve::::::eeeee:::::ee       L:::::L               o:::::::::::::::o  aaaaaaaaa:::::a d::::::::::::::::d 
    //     SS::::::SSSSS               a::::a v:::::v     v:::::ve::::::e     e:::::e       L:::::L               o:::::ooooo:::::o           a::::ad:::::::ddddd:::::d 
    //       SSS::::::::SS      aaaaaaa:::::a  v:::::v   v:::::v e:::::::eeeee::::::e       L:::::L               o::::o     o::::o    aaaaaaa:::::ad::::::d    d:::::d 
    //          SSSSSS::::S   aa::::::::::::a   v:::::v v:::::v  e:::::::::::::::::e        L:::::L               o::::o     o::::o  aa::::::::::::ad:::::d     d:::::d 
    //               S:::::S a::::aaaa::::::a    v:::::v:::::v   e::::::eeeeeeeeeee         L:::::L               o::::o     o::::o a::::aaaa::::::ad:::::d     d:::::d 
    //               S:::::Sa::::a    a:::::a     v:::::::::v    e:::::::e                  L:::::L         LLLLLLo::::o     o::::oa::::a    a:::::ad:::::d     d:::::d 
    //   SSSSSSS     S:::::Sa::::a    a:::::a      v:::::::v     e::::::::e               LL:::::::LLLLLLLLL:::::Lo:::::ooooo:::::oa::::a    a:::::ad::::::ddddd::::::dd
    //   S::::::SSSSSS:::::Sa:::::aaaa::::::a       v:::::v       e::::::::eeeeeeee       L::::::::::::::::::::::Lo:::::::::::::::oa:::::aaaa::::::a d:::::::::::::::::d
    //   S:::::::::::::::SS  a::::::::::aa:::a       v:::v         ee:::::::::::::e       L::::::::::::::::::::::L oo:::::::::::oo  a::::::::::aa:::a d:::::::::ddd::::d
    //    SSSSSSSSSSSSSSS     aaaaaaaaaa  aaaa        vvv            eeeeeeeeeeeeee       LLLLLLLLLLLLLLLLLLLLLLLL   ooooooooooo     aaaaaaaaaa  aaaa  ddddddddd   ddddd
    // ##################################################################################
    #region save_load


    // ##################################################################################
    // setup path to Application.persistentDataPath   
    // c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedHelicopterParametersets\
    // ##################################################################################
    void IO_Initialize()
    {
        // setup target folder
        folder_saved_parameter_for_sceneries = System.IO.Path.Combine(Application.persistentDataPath, "Resources/SavedSceneriesParametersets/");
        folder_saved_parameter_for_transmitter_and_helicopter = System.IO.Path.Combine(Application.persistentDataPath, "Resources/SavedHelicopterAndTransmitterParametersets/");   
    }
    // ##################################################################################




    // ##################################################################################
    // Update UI
    // ##################################################################################
    void UI_Update()
    {
        if (ui_which_tab_is_selected == Available_Tabs.transmitter || ui_which_tab_is_selected == Available_Tabs.helicopter || ui_which_tab_is_selected == Available_Tabs.tuning)
        {
            UI_Dropdown_Update_Items(folder_saved_parameter_for_transmitter_and_helicopter);
            UI_Dropdown_Select_By_Name(ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);
            UI_Update_Parameter_Settings_UI();
            ui_inputfield_save.text = helicopter_name;
        }
    }
    // ##################################################################################




    // ##################################################################################
    // xml-files are listen in ui-dropdown, update them
    // ##################################################################################
    void UI_Dropdown_Update_Items(string folder)
    {       
        // do not trigger the OnValueChanged Listener, if entries are changed by code
        ui_dropdown_load.onValueChanged.RemoveAllListeners();
        
        // perpare variables and get file names
        string[] fullpath_files = null;
        fullpath_files = Directory.GetFiles(folder);

        // update dropdown list with file names from folder and filter the names, if ui_toggle_filter_names.isOn 
        ui_dropdown_load.ClearOptions();
        foreach (string each_fullpath_file in fullpath_files)
        {
            bool put_name_into_list_flag = false;
            if (ui_toggle_filter_names.isOn == true)
            {
                if (ui_which_tab_is_selected == Available_Tabs.scenery)
                {
                    if (Path.GetFileName(each_fullpath_file).StartsWith(scenery_name) || Path.GetFileName(each_fullpath_file) == ui_dropdown_actual_selected_scenery_xml_filename)
                        put_name_into_list_flag = true;
                }
                if (ui_which_tab_is_selected == Available_Tabs.transmitter || ui_which_tab_is_selected == Available_Tabs.helicopter || ui_which_tab_is_selected == Available_Tabs.tuning)
                {
                    if (Path.GetFileName(each_fullpath_file).StartsWith(helicopter_name) || Path.GetFileName(each_fullpath_file) == ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename)
                        put_name_into_list_flag = true;
                }
            }
            else
            {
                put_name_into_list_flag = true;
            }
            
            if (put_name_into_list_flag)
                ui_dropdown_load.options.Add(new Dropdown.OptionData() { text = Path.GetFileName(each_fullpath_file) });
        }

        ui_dropdown_load.RefreshShownValue();

        // activate trigger the OnValueChanged Listener again to react to user changes in the dropdown
        ui_dropdown_load.onValueChanged.AddListener(delegate { Listener_UI_Dropdown_Load_OnValueChanged(); });
    }
    // ##################################################################################




    // ##################################################################################
    // serach for xml-files in ui-dropdown
    // ##################################################################################
    void UI_Dropdown_Select_By_Name(string filename)
    {
        //UnityEngine.Debug.Log("uuuuuuuu UI_Dropdown_Select_By_Name  filename " + filename);

        // do not trigger the OnValueChanged Listener, if entries are changed by code
        ui_dropdown_load.onValueChanged.RemoveAllListeners();

        if (ui_which_tab_is_selected == Available_Tabs.scenery)
        {
            List<Dropdown.OptionData> menuOptions = ui_dropdown_load.GetComponent<Dropdown>().options;
            for (int i = 0; i < menuOptions.Count; i++)
            {
                if (menuOptions[i].text.Equals(filename))
                {
                    ui_dropdown_load.value = i;
                    ui_dropdown_actual_selected_scenery_xml_filename = menuOptions[ui_dropdown_load.value].text;
                    // PlayerPrefs.SetString("ui_dropdown_actual_selected_scenery_xml_filename", ui_dropdown_actual_selected_scenery_xml_filename);
                    PlayerPrefs.SetString("SavedSetting____" + scenery_name + "____actual_selected_xml_filename", ui_dropdown_actual_selected_scenery_xml_filename);
                    break;
                }
            }
        }
        if (ui_which_tab_is_selected == Available_Tabs.transmitter || ui_which_tab_is_selected == Available_Tabs.helicopter || ui_which_tab_is_selected == Available_Tabs.tuning)
        {
            List<Dropdown.OptionData> menuOptions = ui_dropdown_load.GetComponent<Dropdown>().options;
            for (int i = 0; i < menuOptions.Count; i++)
            {
                if (menuOptions[i].text.Equals(filename))
                {
                    ui_dropdown_load.value = i;
                    ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename = menuOptions[ui_dropdown_load.value].text;
                    //PlayerPrefs.SetString("ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename", ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);
                    PlayerPrefs.SetString("SavedSetting____" + helicopter_name + "____actual_selected_xml_filename", ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);
                    break;
                }
            }
        }

        // activate trigger the OnValueChanged Listener again to react to user changes in the dropdown
        ui_dropdown_load.onValueChanged.AddListener(delegate { Listener_UI_Dropdown_Load_OnValueChanged(); });
    }
    // ##################################################################################




    // ##################################################################################
    // listener: load in dropdown selected parameter file
    // ##################################################################################
    void Listener_UI_Dropdown_Load_OnValueChanged()
    {
        // This listener is called if the user changes the dropdown entry

        //UnityEngine.Debug.Log(" --------- has_ui_tab_changed0_or_has_the_dropdown_changed1 " );
       
        // reload the selected new xml file
        if (ui_which_tab_is_selected == Available_Tabs.scenery)
        {
            List<Dropdown.OptionData> menuOptions = ui_dropdown_load.GetComponent<Dropdown>().options;
            ui_dropdown_actual_selected_scenery_xml_filename = menuOptions[ui_dropdown_load.value].text;
            //PlayerPrefs.SetString("ui_dropdown_actual_selected_scenery_xml_filename", ui_dropdown_actual_selected_scenery_xml_filename);
            PlayerPrefs.SetString("SavedSetting____" + scenery_name + "____actual_selected_xml_filename", ui_dropdown_actual_selected_scenery_xml_filename);

            list_skymap_paths[active_scenery_id].fullpath_parameter_file_used = folder_saved_parameter_for_sceneries + ui_dropdown_actual_selected_scenery_xml_filename;
            IO_Load_Scenery_Parameter(list_skymap_paths[active_scenery_id].fullpath_parameter_file_used);

            Change_Skybox_Sun_Sound_Camera_Parameter();
        }
        if (ui_which_tab_is_selected == Available_Tabs.transmitter || ui_which_tab_is_selected == Available_Tabs.helicopter || ui_which_tab_is_selected == Available_Tabs.tuning)
        {
            List<Dropdown.OptionData> menuOptions = ui_dropdown_load.GetComponent<Dropdown>().options;
            ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename = menuOptions[ui_dropdown_load.value].text;
            //PlayerPrefs.SetString("ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename", ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);
            PlayerPrefs.SetString("SavedSetting____" + helicopter_name + "____actual_selected_xml_filename", ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);

            IO_Load_Transmitter_And_Helicopter_Parameter(ui_dropdown_load.options[ui_dropdown_load.value].text);
        }

        // update UI
        UI_Update_Parameter_Settings_UI();
    }
    // ##################################################################################




    // ##################################################################################
    // listener: save in inputfield entered parameter file as xml-file
    // ##################################################################################
    void Listener_UI_Button_Save_OnClick()
    {
        if (ui_which_tab_is_selected == Available_Tabs.scenery)
        {
            if (ui_inputfield_save.text != (helicopter_name + "_default_parameter" + ".xml"))
            {
                string filename = Save_Check_InputField_Text(ui_inputfield_save.text);
                IO_Save_Parameter(helicopter_ODE.par.scenery, folder_saved_parameter_for_sceneries, filename);
                UI_Dropdown_Update_Items(folder_saved_parameter_for_sceneries);
                UI_Dropdown_Select_By_Name(filename);
            }
        }
        if (ui_which_tab_is_selected == Available_Tabs.transmitter || ui_which_tab_is_selected == Available_Tabs.helicopter || ui_which_tab_is_selected == Available_Tabs.tuning)
        {
            if (ui_inputfield_save.text != (scenery_name + "_default_parameter" + ".xml"))
            {
                string filename = Save_Check_InputField_Text(ui_inputfield_save.text);
                IO_Save_Parameter(helicopter_ODE.par.transmitter_and_helicopter, folder_saved_parameter_for_transmitter_and_helicopter, filename);
                UI_Dropdown_Update_Items(folder_saved_parameter_for_transmitter_and_helicopter);
                UI_Dropdown_Select_By_Name(filename);
            }
        } 
    }
    // ##################################################################################




    // ##################################################################################
    // listener: delete xml file
    // ##################################################################################
    void Listener_UI_Button_Delete_OnClick()
    {
        if (ui_which_tab_is_selected == Available_Tabs.scenery)
        {
            if (ui_dropdown_load.options[ui_dropdown_load.value].text != (scenery_name + "_default_parameter" + ".xml"))
            {
                // delete selected file
                File.Delete(System.IO.Path.Combine(folder_saved_parameter_for_sceneries, ui_dropdown_load.options[ui_dropdown_load.value].text));
                ui_dropdown_load.options.RemoveAt(ui_dropdown_load.value);

                // use default parameter instead
                ui_dropdown_actual_selected_scenery_xml_filename = scenery_name + "_default_parameter" + ".xml";
                PlayerPrefs.SetString("ui_dropdown_actual_selected_scenery_xml_filename", ui_dropdown_actual_selected_scenery_xml_filename);
                PlayerPrefs.SetString("SavedSetting____" + scenery_name + "____actual_selected_xml_filename", ui_dropdown_actual_selected_scenery_xml_filename);
                UI_Dropdown_Update_Items(folder_saved_parameter_for_sceneries);
                UI_Dropdown_Select_By_Name(ui_dropdown_actual_selected_scenery_xml_filename);

                // update scenery parameter
                list_skymap_paths[active_scenery_id].fullpath_parameter_file_used = folder_saved_parameter_for_sceneries + ui_dropdown_actual_selected_scenery_xml_filename;
                IO_Load_Scenery_Parameter(list_skymap_paths[active_scenery_id].fullpath_parameter_file_used);
                Change_Skybox_Sun_Sound_Camera_Parameter();
                UI_Update_Parameter_Settings_UI();
            }

        }
        if (ui_which_tab_is_selected == Available_Tabs.transmitter || ui_which_tab_is_selected == Available_Tabs.helicopter || ui_which_tab_is_selected == Available_Tabs.tuning)
        {
            //if (ui_dropdown_load.options[ui_dropdown_load.value].text != (helicopter_name + "_default_parameter" + ".xml"))
            if (!ui_dropdown_load.options[ui_dropdown_load.value].text.Contains(helicopter_name + "_default_parameter"))
            {
                // delete selected file
                File.Delete(System.IO.Path.Combine(folder_saved_parameter_for_transmitter_and_helicopter, ui_dropdown_load.options[ui_dropdown_load.value].text));
                ui_dropdown_load.options.RemoveAt(ui_dropdown_load.value);

                // use default parameter instead
                ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename = helicopter_name + "_default_parameter" + ".xml";
                //PlayerPrefs.SetString("ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename", ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);
                PlayerPrefs.SetString("SavedSetting____" + helicopter_name + "____actual_selected_xml_filename", ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);
                UI_Dropdown_Update_Items(folder_saved_parameter_for_transmitter_and_helicopter);
                UI_Dropdown_Select_By_Name(ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);

                // update helicopter parameter
                IO_Load_Transmitter_And_Helicopter_Parameter(ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);
                UI_Update_Parameter_Settings_UI();
            }
        }
    }
    // ##################################################################################




    // ##################################################################################
    // listener: format inputfield text to give a valid filename
    // ##################################################################################
    string Save_Check_InputField_Text(string filename)
    {
        string extension = Path.GetExtension(filename);
        if (extension == string.Empty)
            filename = filename + ".xml";
        if (extension != "xml")
            filename = Path.GetFileNameWithoutExtension(filename) + ".xml";
        return filename;
    }
    // ##################################################################################




    // ##################################################################################
    // save parameter (serialize them into xml)
    // ##################################################################################
    void IO_Save_Parameter(object select_object, string folder, string filename, bool overrides_flag = true)
    {
        // setup folder to save parameter setup (as xml)
        string targetDataFile = System.IO.Path.Combine(folder, filename);

        // create directory, if not already existing
        if (!Directory.Exists(folder))
            System.IO.Directory.CreateDirectory(folder);

        //// if file already exists
        //string[] files = Directory.GetFiles(folder_saved_parameter_for_transmitter_and_helicopter);
        //foreach (string file in files)
        //{
        //    //if filename already exits, 
        //    if (file == filename)
        //    {
        //        filename = filename + ...;
        //    }
        //}

        //folder_saved_parameter_for_transmitter_and_helicopter = System.IO.Path.Combine(Application.dataPath, "Resources/SavedHelicopterParametersets/");
        //UnityEngine.Debug.Log("folder_saved_parameter_for_transmitter_and_helicopter = " + folder_saved_parameter_for_transmitter_and_helicopter);

        // serialize parameter data as xml-file
        Helper.IO_XML_Serialize(select_object, targetDataFile, overrides_flag); //helicopter_ODE.par.transmitter_and_helicopter or helicopter_ODE.par.scenery
    }
    // ##################################################################################


    


    // ##################################################################################
    // save parameter (serialize them into xml)
    // ##################################################################################
    void IO_Load_Scenery_Parameter(string fullpath)
    {
        // deserialize parameter file 
        helicopter_ODE.par_temp.scenery = Common.Helper.IO_XML_Deserialize<Parameter.stru_scenery>(fullpath);

        // update UI
        if (ui_which_tab_is_selected == Available_Tabs.scenery)
        {
            ui_inputfield_save.text = scenery_name;
            ui_text_fullpath_echo.text = fullpath.Replace(@"\", "/");
        }
    }
    // ##################################################################################





    // ##################################################################################
    // load parameter from xml-file (deserialize xml)
    // ##################################################################################
    void IO_Load_Transmitter_And_Helicopter_Parameter(string fielname)
    {
        string fullpath = System.IO.Path.Combine(folder_saved_parameter_for_transmitter_and_helicopter, fielname);

        // debug lines showing contact forces and helicopter forces/torques
        Destroy_Debug_Line_GameObjects();

        // thread safe setting of parameter
        if (gl_pause_flag == false)
        {
            // if game is running the ODE-thread updates the parameter 
            // serialize in a frist step into temp-object
            helicopter_ODE.par_temp.transmitter_and_helicopter = Helper.IO_XML_Deserialize<Parameter.stru_transmitter_and_helicopter>(fullpath);
            // Get settings for "simulation"-structure from PlayerPrefs 
            helicopter_ODE.par_temp.simulation.get_stru_simulation_settings_from_player_prefs();

            // flag signals in ODE-thread, that new data is available
            helicopter_ODE.flag_load_new_parameter_in_ODE_thread = true;

            // wait until ODE-thread have handled the parameter. it sets flag_load_new_parameter_in_ODE_thread = true
            while (helicopter_ODE.flag_load_new_parameter_in_ODE_thread == true){}
        }
        else
        {
            // if game is paused the ODE-thread is sleeping, therefore update the parameter direct here
            helicopter_ODE.par_temp.transmitter_and_helicopter = Helper.IO_XML_Deserialize<Parameter.stru_transmitter_and_helicopter>(fullpath);
            // Get settings for "simulation"-structure from PlayerPrefs 
            helicopter_ODE.par_temp.simulation.get_stru_simulation_settings_from_player_prefs();
            helicopter_ODE.par_temp.transmitter_and_helicopter.Update_Calculated_Parameter();
            helicopter_ODE.par = helicopter_ODE.par_temp.Deep_Clone();
            helicopter_ODE.par.transmitter_and_helicopter.Update_Calculated_Parameter();

            //UnityEngine.Debug.Log(" helicopter_ODE.par " + helicopter_ODE.par.transmitter_and_helicopter.helicopter.sound_volume.val + "  " + helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.sound_volume.val);
           
            ////helicopter_ODE.par.transmitter_and_helicopter.Update_Calculated_Parameter();
            helicopter_ODE.Update_ODE_Debug_Variables();
            helicopter_ODE.flag_load_new_parameter_in_ODE_thread = false;
        }

        // debug lines showing contact forces and helicopter forces/torques
        Create_Debug_Line_GameObjects();

        // update birds and insects flocks, if parameter has changed
        if (Flocks_Check_If_Parameter_Has_Changed())
            Flocks_Update(ref all_animal_flocks);

        // update UI
        if (ui_which_tab_is_selected == Available_Tabs.transmitter || ui_which_tab_is_selected == Available_Tabs.helicopter || ui_which_tab_is_selected == Available_Tabs.tuning)
        {
            ui_inputfield_save.text = helicopter_name;
            ui_text_fullpath_echo.text = fullpath.Replace(@"\", "/");
        }
            
    }
    // ##################################################################################
    #endregion












    // ##################################################################################
    //                                                                                                           dddddddd                                                     
    //    UUUUUUUU     UUUUUUUUIIIIIIIIII     hhhhhhh                                                            d::::::dlllllll   iiii                                       
    //    U::::::U     U::::::UI::::::::I     h:::::h                                                            d::::::dl:::::l  i::::i                                      
    //    U::::::U     U::::::UI::::::::I     h:::::h                                                            d::::::dl:::::l   iiii                                       
    //    UU:::::U     U:::::UUII::::::II     h:::::h                                                            d:::::d l:::::l                                              
    //     U:::::U     U:::::U   I::::I        h::::h hhhhh         aaaaaaaaaaaaa  nnnn  nnnnnnnn        ddddddddd:::::d  l::::l iiiiiiinnnn  nnnnnnnn       ggggggggg   ggggg
    //     U:::::D     D:::::U   I::::I        h::::hh:::::hhh      a::::::::::::a n:::nn::::::::nn    dd::::::::::::::d  l::::l i:::::in:::nn::::::::nn    g:::::::::ggg::::g
    //     U:::::D     D:::::U   I::::I        h::::::::::::::hh    aaaaaaaaa:::::an::::::::::::::nn  d::::::::::::::::d  l::::l  i::::in::::::::::::::nn  g:::::::::::::::::g
    //     U:::::D     D:::::U   I::::I        h:::::::hhh::::::h            a::::ann:::::::::::::::nd:::::::ddddd:::::d  l::::l  i::::inn:::::::::::::::ng::::::ggggg::::::gg
    //     U:::::D     D:::::U   I::::I        h::::::h   h::::::h    aaaaaaa:::::a  n:::::nnnn:::::nd::::::d    d:::::d  l::::l  i::::i  n:::::nnnn:::::ng:::::g     g:::::g 
    //     U:::::D     D:::::U   I::::I        h:::::h     h:::::h  aa::::::::::::a  n::::n    n::::nd:::::d     d:::::d  l::::l  i::::i  n::::n    n::::ng:::::g     g:::::g 
    //     U:::::D     D:::::U   I::::I        h:::::h     h:::::h a::::aaaa::::::a  n::::n    n::::nd:::::d     d:::::d  l::::l  i::::i  n::::n    n::::ng:::::g     g:::::g 
    //     U::::::U   U::::::U   I::::I        h:::::h     h:::::ha::::a    a:::::a  n::::n    n::::nd:::::d     d:::::d  l::::l  i::::i  n::::n    n::::ng::::::g    g:::::g 
    //     U:::::::UUU:::::::U II::::::II      h:::::h     h:::::ha::::a    a:::::a  n::::n    n::::nd::::::ddddd::::::ddl::::::li::::::i n::::n    n::::ng:::::::ggggg:::::g 
    //      UU:::::::::::::UU  I::::::::I      h:::::h     h:::::ha:::::aaaa::::::a  n::::n    n::::n d:::::::::::::::::dl::::::li::::::i n::::n    n::::n g::::::::::::::::g 
    //        UU:::::::::UU    I::::::::I      h:::::h     h:::::h a::::::::::aa:::a n::::n    n::::n  d:::::::::ddd::::dl::::::li::::::i n::::n    n::::n  gg::::::::::::::g 
    //          UUUUUUUUU      IIIIIIIIII      hhhhhhh     hhhhhhh  aaaaaaaaaa  aaaa nnnnnn    nnnnnn   ddddddddd   dddddlllllllliiiiiiii nnnnnn    nnnnnn    gggggggg::::::g 
    //                                                                                                                                                                g:::::g 
    //                                                                                                                                                    gggggg      g:::::g 
    //                                                                                                                                                    g:::::gg   gg:::::g 
    //                                                                                                                                                     g::::::ggg:::::::g 
    //                                                                                                                                                      gg:::::::::::::g  
    //                                                                                                                                                        ggg::::::ggg    
    // ##################################################################################                                                                        gggggg       
    #region ui_handling
    GUIStyle style = new GUIStyle(); 
    public void OnGUI()
    {
        // show version number
        if (Time.time < 8)
        {
            style.fontSize = 40;
            style.fontStyle = FontStyle.Bold;

            style.normal.textColor = Color.black;
            GUI.Label(new Rect(104, 44, 200, 100), "Version " + plot_build_setting.LastBuildTime + "", style);
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(100, 40, 200, 100), "Version " + plot_build_setting.LastBuildTime + "", style);
        }

        // show connected controller
        if (UI_show_new_controller_name_flag == false)
            UI_show_new_controller_name_time = Time.time;
        else
        {
            if (Time.time - UI_show_new_controller_name_time < 5)
            {
                //Time.deltaTime
                //Time.time >= currenttime
                style.fontSize = 40;
                style.fontStyle = FontStyle.Bold;

                if (connected_input_devices_names.Count > 0)
                {
                    style.normal.textColor = Color.black;
                    GUI.Label(new Rect(104, 44, 200, 100), "Version " + plot_build_setting.LastBuildTime + "", style);
                    style.normal.textColor = Color.white;
                    GUI.Label(new Rect(100, 40, 200, 100), "Version " + plot_build_setting.LastBuildTime + "", style);

                    style.normal.textColor = Color.black;
                    GUI.Label(new Rect(104, 104, 200, 100), "Using controller \"" + connected_input_devices_names[selected_input_device_id] + "\"", style);
                    style.normal.textColor = Color.white;
                    GUI.Label(new Rect(100, 100, 200, 100), "Using controller \"" + connected_input_devices_names[selected_input_device_id] + "\"", style);
                }
            }
            else
            {
                UI_show_new_controller_name_flag = false;
            }
        }

    }



    public void UI_Initialize_UI_GameObjects()
    {
        // ##################################################################################
        // get ui objects
        // ##################################################################################  
        // get prefabs for parameter UI
        ui_prefab_headline = (GameObject)Resources.Load("Prefabs/UI_Headline", typeof(GameObject)); // prefab for headlines
        ui_prefab_bool = (GameObject)Resources.Load("Prefabs/UI_Input_Toggle_Bool", typeof(GameObject)); // prefab for scalar values (stru_float, stru_int) 
        ui_prefab_scalar = (GameObject)Resources.Load("Prefabs/UI_Inputfield_Scalar", typeof(GameObject)); // prefab for scalar values (stru_float, stru_int) 
        ui_prefab_vector3 = (GameObject)Resources.Load("Prefabs/UI_Inputfield_Vector3", typeof(GameObject)); // prefab for scalar Vector3
        ui_prefab_vector3_list_setting = (GameObject)Resources.Load("Prefabs/UI_Inputfield_Vector3_List_Settings", typeof(GameObject)); // prefab for size info/setting of Vector3-List
        ui_prefab_vector3_list_vector3 = (GameObject)Resources.Load("Prefabs/UI_Inputfield_Vector3_List_Vector3", typeof(GameObject));  // prefab: Vector3-List's one Vector3
        ui_prefab_list = (GameObject)Resources.Load("Prefabs/UI_Dropdown_List", typeof(GameObject));  // prefab: dropdown list


        // find the main Canvas object
        ui_canvas = GameObject.Find("Canvas");

        // UI welcome panel
        ui_welcome_panel = ui_canvas.transform.Find("UI Info Menu Welcome");
        ui_button_close_welcome_panel = ui_welcome_panel.transform.Find("Button Close").GetComponent<Button>();
        ui_button_close_welcome_panel.onClick.AddListener(delegate { Listener_UI_Button_Close_Menu(); });

        // UI info panel
        ui_info_panel = ui_canvas.transform.Find("UI Info Menu");
        ui_button_close_info_panel = ui_info_panel.transform.Find("Button Close").GetComponent<Button>();
        ui_button_close_info_panel.onClick.AddListener(delegate { Listener_UI_Button_Close_Menu(); });

        // UI no controller panel 
        ui_no_controller_panel = ui_canvas.transform.Find("UI Info Menu No Controller");

        // UI loading panel
        ui_loading_panel = ui_canvas.transform.Find("UI Info Menu Loading");

        // UI select helicopter from list
        ui_helicopter_selection_panel = ui_canvas.transform.Find("UI Info Menu Helicopter Selection");
        ui_button_close_helicopter_selection_panel = ui_helicopter_selection_panel.transform.Find("Button Close").GetComponent<Button>();
        ui_button_close_helicopter_selection_panel.onClick.AddListener(delegate { Listener_UI_Button_Close_Menu(); });

        // UI select scenery from list
        ui_scenery_selection_panel = ui_canvas.transform.Find("UI Info Menu Scenery Selection");
        ui_button_close_scenery_selection_panel = ui_scenery_selection_panel.transform.Find("Button Close").GetComponent<Button>();
        ui_button_close_scenery_selection_panel.onClick.AddListener(delegate { Listener_UI_Button_Close_Menu(); });

        // UI debug panel
        ui_debug_panel = ui_canvas.transform.Find("UI Debug/UI Debug Panel");
        ui_debug_lines = ui_canvas.transform.Find("UI Debug/Debug_Lines");
        ui_debug_text = ui_debug_panel.transform.Find("Debug Info Panel").GetComponent<Text>();

        // UI exit panel
        ui_exit_menu = ui_canvas.transform.Find("UI Exit Menu");
        ui_exit_menu_exit_game_button = ui_exit_menu.transform.Find("Panel/Exit_Game_Button").GetComponent<Button>(); 
        ui_exit_menu_exit_button = ui_exit_menu.transform.Find("Panel/Button Close").GetComponent<Button>();  
        ui_exit_menu_exit_game_button.onClick.AddListener(delegate { Listener_UI_Exit_Menu_Exit_Game_Button(); });
        ui_exit_menu_exit_button.onClick.AddListener(delegate { Listener_UI_Button_Close_Menu(); });

        // UI pie menu  
        ui_pie_menu = ui_canvas.transform.Find("UI Pie Menu");
        ui_pie_menu_welcome_button = ui_pie_menu.transform.Find("Panel/Welcome_Button").GetComponent<Button>();
        ui_pie_menu_parameter_button = ui_pie_menu.transform.Find("Panel/Parameter_Button").GetComponent<Button>();
        ui_pie_menu_controller_calibration_button = ui_pie_menu.transform.Find("Panel/Controller_Calibration_Button").GetComponent<Button>();
        ui_pie_menu_select_scenery_button = ui_pie_menu.transform.Find("Panel/Select_Secenery_Button").GetComponent<Button>();
        ui_pie_menu_select_helicopter_button = ui_pie_menu.transform.Find("Panel/Select_Helicopter_Button").GetComponent<Button>();
        ui_pie_menu_keyboard_shorcuts_button = ui_pie_menu.transform.Find("Panel/Keyboard_Shortcuts_Button").GetComponent<Button>();
        ui_pie_menu_exit_game_button = ui_pie_menu.transform.Find("Panel/Exit_Game_Button").GetComponent<Button>();
        ui_pie_menu_exit_button = ui_pie_menu.transform.Find("Panel/Button Close").GetComponent<Button>();
        ui_pie_menu_welcome_button.onClick.AddListener(delegate { Listener_UI_Pie_Menu_Welcome_Button(); });
        ui_pie_menu_parameter_button.onClick.AddListener(delegate { Listener_UI_Pie_Menu_Parameter_Button(); });
        ui_pie_menu_controller_calibration_button.onClick.AddListener(delegate { Listener_UI_Pie_Menu_Controller_Calibration_Button(); });
        ui_pie_menu_select_scenery_button.onClick.AddListener(delegate { Listener_UI_Pie_Menu_Select_Scenery_Button(); });
        ui_pie_menu_select_helicopter_button.onClick.AddListener(delegate { Listener_UI_Pie_Menu_Select_Helicopter_Button(); });
        ui_pie_menu_keyboard_shorcuts_button.onClick.AddListener(delegate { Listener_UI_Pie_Menu_Keyboard_Shorcuts_Button(); });
        ui_pie_menu_exit_game_button.onClick.AddListener(delegate { Listener_UI_Pie_Menu_Exit_Game_Button(); });
        ui_pie_menu_exit_button.onClick.AddListener(delegate { Listener_UI_Button_Close_Menu(); });
        

        // UI pause menu
        ui_pause_menu = ui_canvas.transform.Find("UI Pause Menu");

        // UI controller calibration
        ui_controller_calibration_panel = ui_canvas.transform.Find("UI Controller Calibration");
        ui_controller_calibration_panel_text = ui_controller_calibration_panel.transform.Find("Text").GetComponent<Text>();
        ui_controller_calibration_panel_device_name_text = ui_controller_calibration_panel.transform.Find("Text Device Name").GetComponent<Text>();
        ui_controller_calibration_panel_image = ui_controller_calibration_panel.transform.Find("Image").GetComponent<Image>();
        ui_controller_calibration_panel_channel_slider[0] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 0/Slider").GetComponent<Slider>();
        ui_controller_calibration_panel_channel_slider[1] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 1/Slider").GetComponent<Slider>();
        ui_controller_calibration_panel_channel_slider[2] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 2/Slider").GetComponent<Slider>();
        ui_controller_calibration_panel_channel_slider[3] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 3/Slider").GetComponent<Slider>();
        ui_controller_calibration_panel_channel_slider[4] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 4/Slider").GetComponent<Slider>();
        ui_controller_calibration_panel_channel_slider[5] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 5/Slider").GetComponent<Slider>();
        ui_controller_calibration_panel_channel_slider[6] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 6/Slider").GetComponent<Slider>();
        ui_controller_calibration_panel_channel_slider[7] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 7/Slider").GetComponent<Slider>();
        ui_controller_calibration_panel_channel_text_value[0] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 0/Text Value").GetComponent<Text>();
        ui_controller_calibration_panel_channel_text_value[1] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 1/Text Value").GetComponent<Text>();
        ui_controller_calibration_panel_channel_text_value[2] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 2/Text Value").GetComponent<Text>();
        ui_controller_calibration_panel_channel_text_value[3] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 3/Text Value").GetComponent<Text>();
        ui_controller_calibration_panel_channel_text_value[4] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 4/Text Value").GetComponent<Text>();
        ui_controller_calibration_panel_channel_text_value[5] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 5/Text Value").GetComponent<Text>();
        ui_controller_calibration_panel_channel_text_value[6] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 6/Text Value").GetComponent<Text>();
        ui_controller_calibration_panel_channel_text_value[7] = ui_controller_calibration_panel.transform.Find("Channel Status/UI Slider Channel 7/Text Value").GetComponent<Text>();
        ui_button_close_controller_calibration_panel = ui_controller_calibration_panel.transform.Find("Button Close").GetComponent<Button>();
        ui_button_close_controller_calibration_panel.onClick.AddListener(delegate { Listener_UI_Button_Abort_Calibration_And_Close_Calibration_Panel(); });


        // ui_parameter objects
        ui_parameter = ui_canvas.transform.Find("UI Parameter");
        ui_parameter_panel = ui_parameter.transform.Find("Parameter Panel");

        ui_button_close_parameter = ui_parameter.transform.Find("Parameter Panel/Button Close Parameter").GetComponent<Button>();
        ui_button_close_parameter.onClick.AddListener(delegate { Listener_UI_Button_Close_Menu(); });
            
        // find the two objects used to hold the parameter prefabs 
        ui_scroll_view_content = ui_parameter.transform.Find("Parameter Panel/Scroll View/Viewport/Content");
        ui_overlaid_parameterlist = ui_parameter.transform.Find("Overlaid Parameterlist");
        ui_scroll_view_scrollbar = ui_parameter.transform.Find("Parameter Panel/Scroll View/Scrollbar Vertical").GetComponent<Scrollbar>();
        ui_scroll_view_scrollbar.value = 1f;

        // find the laod/save buttons, dropdown, inputfield and text 
        ui_panel_load_save = ui_parameter.transform.Find("Parameter Panel/Load Save").GetComponent<Transform>();
        ui_panel_simulation = ui_parameter.transform.Find("Parameter Panel/Simulation Panel").GetComponent<Transform>();
        ui_dropdown_load = ui_parameter.transform.Find("Parameter Panel/Load Save/Dropdown Load").GetComponent<Dropdown>();
        ui_button_save = ui_parameter.transform.Find("Parameter Panel/Load Save/Button Save").GetComponent<Button>();
        ui_button_delete = ui_parameter.transform.Find("Parameter Panel/Load Save/Button Delete").GetComponent<Button>();
        ui_toggle_filter_names = ui_parameter.transform.Find("Parameter Panel/Load Save/Toggle Filter Names").GetComponent<Toggle>();
        ui_toggle_filter_names.onValueChanged.AddListener(delegate { Listener_UI_Toggle_Filter_Names(); });
        ui_inputfield_save = ui_parameter.transform.Find("Parameter Panel/Load Save/InputField Save").GetComponent<InputField>();
        ui_text_fullpath_echo = ui_parameter.transform.Find("Parameter Panel/Load Save/Text Path Echo").GetComponent<Text>();

        ui_button_open_folder = ui_parameter.transform.Find("Parameter Panel/Load Save/Button Open Folder").GetComponent<Button>();
        ui_button_open_folder.onClick.AddListener(delegate { Listener_UI_Button_Open_Folder(); });
        ui_button_open_regedit = ui_parameter.transform.Find("Parameter Panel/Simulation Panel/Button Open Regedit").GetComponent<Button>();
        ui_button_open_regedit.onClick.AddListener(delegate { Listener_UI_Button_Open_Regedit(); });
        ui_button_open_folder_screenshots = ui_parameter.transform.Find("Parameter Panel/Button Open Folder Screenshots").GetComponent<Button>();
        ui_button_open_folder_screenshots.onClick.AddListener(delegate { Listener_UI_Button_Open_Folder_Screenshots(); });


        ui_text_title = ui_scroll_view_content.Find("Text_Title").GetComponent<Text>();

        ui_button_simulation = ui_parameter.transform.Find("Parameter Panel/Button Simulation").GetComponent<Button>();
        ui_button_scenery = ui_parameter.transform.Find("Parameter Panel/Button Scenery").GetComponent<Button>();
        ui_button_transmitter = ui_parameter.transform.Find("Parameter Panel/Button Transmitter").GetComponent<Button>();
        ui_button_helicopter = ui_parameter.transform.Find("Parameter Panel/Button Helicopter").GetComponent<Button>();
        ui_button_tuning = ui_parameter.transform.Find("Parameter Panel/Button Tuning").GetComponent<Button>();

        ui_button_simulation.onClick.AddListener(delegate { Listener_Tab_Button(Available_Tabs.simulation); });
        ui_button_scenery.onClick.AddListener(delegate { Listener_Tab_Button(Available_Tabs.scenery); });
        ui_button_transmitter.onClick.AddListener(delegate { Listener_Tab_Button(Available_Tabs.transmitter); });
        ui_button_helicopter.onClick.AddListener(delegate { Listener_Tab_Button(Available_Tabs.helicopter); });
        ui_button_tuning.onClick.AddListener(delegate { Listener_Tab_Button(Available_Tabs.tuning); });

        // add listener for when the value of the Dropdown changes, to take action
        ui_dropdown_load.onValueChanged.AddListener(delegate { Listener_UI_Dropdown_Load_OnValueChanged(); });
        ui_button_save.onClick.AddListener(delegate { Listener_UI_Button_Save_OnClick(); });
        ui_button_delete.onClick.AddListener(delegate { Listener_UI_Button_Delete_OnClick(); });

        // frames per second
        ui_frames_per_sec_text = ui_canvas.transform.Find("FPS_Text");

        // XR 3D-arrow
        ui_canvas_xr_mouse_arrow = ui_canvas.transform.Find("UI_XR_Mouse_Arrow").gameObject;

    }

    // ##################################################################################





    // ##################################################################################
    // Frames pre second
    // ##################################################################################
    private IEnumerator FramesPerSecond()
    {
        for (;;)
        {
            // calculate frame per second
            float time_old = Time.realtimeSinceStartup;
            int last_frame_count = Time.frameCount;
            yield return new WaitForSeconds(frames_per_sec_display_update_frequency);
            float timeSpan = Time.realtimeSinceStartup - time_old;
            int frameCount = Time.frameCount - last_frame_count;

            // display frame per second
            frames_per_sec = Mathf.RoundToInt(frameCount / timeSpan);
            ui_frames_per_sec_text.GetComponent<Text>().text = frames_per_sec.ToString() + " fps";
        }
    }
    // ##################################################################################





    //// ##################################################################################
    //// listener: button play game
    //// ##################################################################################
    //void Listener_UI_Exit_Menu_Play_Button()
    //{
    //    ui_exit_panel_flag = false; 
    //    gl_pause_flag = false;
    //}
    //// ##################################################################################


    // ##################################################################################
    // listener: all buttons on pie menu
    // ##################################################################################
    void Listener_UI_Pie_Menu_Welcome_Button() { ui_menu_logic_welcome_menu(); }
    void Listener_UI_Pie_Menu_Parameter_Button() { ui_menu_logic_parameter(); }
    void Listener_UI_Pie_Menu_Controller_Calibration_Button() { ui_menu_logic_controller_calibration_menu(); }
    void Listener_UI_Pie_Menu_Select_Scenery_Button() { ui_menu_logic_select_scenery(); }
    void Listener_UI_Pie_Menu_Select_Helicopter_Button() { ui_menu_logic_select_helicopter(); }
    void Listener_UI_Pie_Menu_Keyboard_Shorcuts_Button() { ui_menu_logic_info(); }
    void Listener_UI_Pie_Menu_Exit_Game_Button( )
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        //if (!Application.isEditor) System.Diagnostics.Process.GetCurrentProcess().Kill();
        Application.Quit(0);
        //System.Diagnostics.Process.GetCurrentProcess().Kill();
//#if ENABLE_WINMD_SUPPORT
//Windows.ApplicationModel.Core.CoreApplication.Exit();
//#endif
        //System.Diagnostics.Process.GetCurrentProcess().Kill();
    }
    // ##################################################################################


    // ##################################################################################
    // listener: button exit game
    // ##################################################################################
    void Listener_UI_Exit_Menu_Exit_Game_Button()
    {
#if DEBUG_LOG  
        //// debug time ticks to file
        Debug_Save_Time_Ticks();
#endif

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        //if (!Application.isEditor) System.Diagnostics.Process.GetCurrentProcess().Kill();
        Application.Quit(0);
        //System.Diagnostics.Process.GetCurrentProcess().Kill();

//#if ENABLE_WINMD_SUPPORT
//Windows.ApplicationModel.Core.CoreApplication.Exit();
//#endif
//        System.Diagnostics.Process.GetCurrentProcess().Kill();
    }
    // ##################################################################################





    // ##################################################################################
    // listener: toggle name filtering in load file dropdown
    // ##################################################################################
    void Listener_UI_Toggle_Filter_Names()
    { 
        if (ui_which_tab_is_selected == Available_Tabs.scenery)
        {
            UI_Dropdown_Update_Items(folder_saved_parameter_for_sceneries);
            UI_Dropdown_Select_By_Name(ui_dropdown_actual_selected_scenery_xml_filename);
        }
        if (ui_which_tab_is_selected == Available_Tabs.transmitter || ui_which_tab_is_selected == Available_Tabs.helicopter || ui_which_tab_is_selected == Available_Tabs.tuning)
        {
            UI_Dropdown_Update_Items(folder_saved_parameter_for_transmitter_and_helicopter);
            UI_Dropdown_Select_By_Name(ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);
        }
    }
    // ##################################################################################





    // ##################################################################################
    // listener: button closing the parameter menu
    // ##################################################################################
    void Listener_UI_Button_Close_Menu()
    {
        ui_welcome_panel_flag = false;
        ui_info_panel_flag = false;
        ui_no_controller_panel_flag = false;
        ui_loading_panel_flag = false;
        ui_parameter_panel_flag = false;
        ui_pie_menu_flag = false;

        ui_helicopter_selection_menu_flag = false;
        ui_scenery_selection_menu_flag = false;

        ui_exit_panel_flag = false;

        ui_pause_flag = false;
        gl_pause_flag = false;
    }
    // ##################################################################################




    // ##################################################################################
    // listener: abort and exist controller calibration window
    // ##################################################################################
    void Listener_UI_Button_Abort_Calibration_And_Close_Calibration_Panel()
    {
        if (calibration_abortable && calibration_state != State_Calibration.not_running)
        {
            calibration_state = State_Calibration.abort;
            ui_exit_panel_flag = false;

            gl_pause_flag = false;
        }
    }
    // ##################################################################################




    // ##################################################################################
    // listener: button to open folder with saved parameter sets
    // ##################################################################################
    void Listener_UI_Button_Open_Folder()
    {
        //Process.Start(@ui_text_fullpath_echo.text);
        //System.Diagnostics.Process.Start(@Path.GetDirectoryName(ui_text_fullpath_echo.text));
        // il2cpp does not support Process, see https://forum.unity.com/threads/solved-il2cpp-and-process-start.533988/
        Application.OpenURL(@Path.GetDirectoryName(ui_text_fullpath_echo.text));
    }
    // ##################################################################################




    // ##################################################################################
    // listener: button to open regedit where unity stores PlayerPrefs
    // ##################################################################################
    void Listener_UI_Button_Open_Regedit()
    {
        if ((Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor))
        {
            // open regedit where unity stores PlayerPrefs
#if UNITY_EDITOR
            string regedit_path_for_playerprefs = "Computer\\HKEY_CURRENT_USER\\Software\\Unity\\UnityEditor\\" + Application.companyName + "\\" + Application.productName;
#else
            string regedit_path_for_playerprefs = "Computer\\HKEY_CURRENT_USER\\Software\\" + Application.companyName + "\\" + Application.productName;
#endif
            //UnityEngine.Debug.Log(regedit_path_for_playerprefs);
            System.Diagnostics.Process.Start("cmd.exe", "/c REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Applets\\Regedit /v LastKey /t REG_SZ /d \"" + regedit_path_for_playerprefs + "\" /f");
            System.Diagnostics.Process.Start("regedit");

            // il2cpp does not support Process, see https://forum.unity.com/threads/solved-il2cpp-and-process-start.533988/
            // Application.OpenURL();
        }
        else
        {
            // MacOS

        }
    }
    // ##################################################################################




    // ##################################################################################
    // listener: button to open screenshots folder
    // ##################################################################################
    void Listener_UI_Button_Open_Folder_Screenshots()
    {
        if ((Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor))
        {
            //Process.Start(@ui_text_fullpath_echo.text);
            //System.Diagnostics.Process.Start(@Path.GetDirectoryName(Application.persistentDataPath + "/Free RC Helicopter Simulator"));
            // il2cpp does not support Process, see https://forum.unity.com/threads/solved-il2cpp-and-process-start.533988/
            Application.OpenURL(@Path.GetDirectoryName(Application.persistentDataPath + "/Free RC Helicopter Simulator"));

        }
        else
        {
            // MacOS 
            // TODO test???  https://answers.unity.com/questions/43422/how-to-implement-show-in-explorer.html
            //System.Diagnostics.Process.Start(@Path.GetDirectoryName(Application.persistentDataPath + "/Free RC Helicopter Simulator"));
            // il2cpp does not support Process, see https://forum.unity.com/threads/solved-il2cpp-and-process-start.533988/
            Application.OpenURL(@Path.GetDirectoryName(Application.persistentDataPath + "/Free RC Helicopter Simulator"));
        }
    }
    // ##################################################################################
    





    // ##################################################################################
    // listener: tab buttons to switch between wold, sim, heli,...
    // ##################################################################################
    void Listener_Tab_Button(Available_Tabs selected_tab)
    {
        Manage_Tab_Button_Logic(selected_tab);
    }
    // ##################################################################################




    // ##################################################################################
    // function to handle tab buttons
    // ##################################################################################
    void Manage_Tab_Button_Logic(Available_Tabs selected_tab)
    {
        ColorBlock colors;
        Color color_selected = new Color(1f, 1f, 1f, 1f);
        Color color_unselected = new Color(0.5f, 0.5f, 0.5f, 1f);
        colors = ui_button_simulation.colors; colors.normalColor = color_unselected; ui_button_simulation.colors = colors;
        colors = ui_button_scenery.colors; colors.normalColor = color_unselected; ui_button_scenery.colors = colors;
        colors = ui_button_transmitter.colors; colors.normalColor = color_unselected; ui_button_transmitter.colors = colors;
        colors = ui_button_helicopter.colors; colors.normalColor = color_unselected; ui_button_helicopter.colors = colors;
        colors = ui_button_tuning.colors; colors.normalColor = color_unselected; ui_button_tuning.colors = colors;

        ui_which_tab_is_selected = selected_tab;

        if (ui_which_tab_is_selected == Available_Tabs.simulation)
        {
            colors = ui_button_simulation.colors; colors.normalColor = color_selected; ui_button_simulation.colors = colors;
            ui_panel_load_save.gameObject.SetActive(false);
            ui_panel_simulation.gameObject.SetActive(true);
        }
        if (ui_which_tab_is_selected == Available_Tabs.scenery)
        {
            colors = ui_button_scenery.colors; colors.normalColor = color_selected; ui_button_scenery.colors = colors;
            ui_panel_load_save.gameObject.SetActive(true);
            ui_panel_simulation.gameObject.SetActive(false);
        }
        if (ui_which_tab_is_selected == Available_Tabs.transmitter)
        {
            colors = ui_button_transmitter.colors; colors.normalColor = color_selected; ui_button_transmitter.colors = colors;
            ui_panel_load_save.gameObject.SetActive(true);
            ui_panel_simulation.gameObject.SetActive(false);
        }
        if (ui_which_tab_is_selected == Available_Tabs.helicopter)
        {
            colors = ui_button_helicopter.colors; colors.normalColor = color_selected; ui_button_helicopter.colors = colors;
            ui_panel_load_save.gameObject.SetActive(true);
            ui_panel_simulation.gameObject.SetActive(false);
        }
        if (ui_which_tab_is_selected == Available_Tabs.tuning)
        {
            colors = ui_button_tuning.colors; colors.normalColor = color_selected; ui_button_tuning.colors = colors;
            ui_panel_load_save.gameObject.SetActive(true);
            ui_panel_simulation.gameObject.SetActive(false);
        }


        //UnityEngine.Debug.Log(" iiiiiiiiiii  Manage_Tab_Button_Logic    ui_which_tab_is_selected:" + ui_which_tab_is_selected);


        // user interface settings
        if (ui_which_tab_is_selected == Available_Tabs.simulation)
        {
            //
        }
        if (ui_which_tab_is_selected == Available_Tabs.scenery)
        {
            UI_Dropdown_Update_Items(folder_saved_parameter_for_sceneries);
            UI_Dropdown_Select_By_Name(ui_dropdown_actual_selected_scenery_xml_filename);
            ui_inputfield_save.text = scenery_name;
            ui_text_fullpath_echo.text = (folder_saved_parameter_for_sceneries + ui_dropdown_actual_selected_scenery_xml_filename).Replace(@"\", "/");
        }
        if (ui_which_tab_is_selected == Available_Tabs.transmitter || ui_which_tab_is_selected == Available_Tabs.helicopter || ui_which_tab_is_selected == Available_Tabs.tuning)
        {
            UI_Dropdown_Update_Items(folder_saved_parameter_for_transmitter_and_helicopter);
            UI_Dropdown_Select_By_Name(ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename);
            ui_inputfield_save.text = helicopter_name;
            ui_text_fullpath_echo.text = (folder_saved_parameter_for_transmitter_and_helicopter + ui_dropdown_actual_selected_transmitter_and_helicopter_xml_filename).Replace(@"\", "/");
        }


        UI_Update_Parameter_Settings_UI();


        // move scrollbar to top position
        ui_scroll_view_scrollbar.value = 1f;
    }
    // ##################################################################################




    // ##################################################################################
    // update every parameter prefab on ui
    // ##################################################################################
    public void UI_Update_Parameter_Settings_UI()
    {
        // debug lines showing contact forces and helicopter forces/torques
        Destroy_Debug_Line_GameObjects();

        // update the variables also in the ODE thread
        if (gl_pause_flag == false) 
        {
            // if game is running the ODE-thread updates the parameter 
            // flag signals in ODE-thread, that new data is available
            helicopter_ODE.flag_load_new_parameter_in_ODE_thread = true;

            // wait until ODE-thread have handled the parameter. it sets flag_load_new_parameter_in_ODE_thread = true
            while (helicopter_ODE.flag_load_new_parameter_in_ODE_thread == true) { }
        }
        else
        {
            helicopter_ODE.par_temp.simulation.get_stru_simulation_settings_from_player_prefs();
            helicopter_ODE.par_temp.transmitter_and_helicopter.Update_Calculated_Parameter();
            helicopter_ODE.par = helicopter_ODE.par_temp.Deep_Clone();
            helicopter_ODE.par.transmitter_and_helicopter.Update_Calculated_Parameter();
            helicopter_ODE.Update_ODE_Debug_Variables();
            helicopter_ODE.flag_load_new_parameter_in_ODE_thread = false;
        }

        // debug lines showing contact forces and helicopter forces/torques
        Create_Debug_Line_GameObjects();

        // update birds and insects flocks, if parameter has changed
        if (Flocks_Check_If_Parameter_Has_Changed())
            Flocks_Update(ref all_animal_flocks);


        ////if(!helicopter_ODE.par_temp.scenery.animals.Deep_Compare(animals_parameter_old))
        //if (!helicopter_ODE.par_temp.scenery.animals.Deep_Compare(animals_parameter_old))
        //{
        //    UnityEngine.Debug.Log(" ja");
        //    // setup animals: birds and insects
        //    Flocks_Update(ref all_animal_flocks);
        //}
        //else
        //{
        //    UnityEngine.Debug.Log(" nein");
        //}

        //UnityEngine.Debug.Log(" AAA    " +
        //    " helicopter_ODE.par_temp.scenery.animals.number_of_bird_flocks " + helicopter_ODE.par_temp.scenery.animals.number_of_bird_flocks.val +
        //    " animals_parameter_old.number_of_bird_flocks " + animals_parameter_old.number_of_bird_flocks.val);

        //animals_parameter_old = helicopter_ODE.par_temp.scenery.animals.Deep_Clone();

        //UnityEngine.Debug.Log(" BBB    " +
        //    " helicopter_ODE.par_temp.scenery.animals.number_of_bird_flocks " + helicopter_ODE.par_temp.scenery.animals.number_of_bird_flocks.val +
        //    " animals_parameter_old.number_of_bird_flocks " + animals_parameter_old.number_of_bird_flocks.val);








        //UnityEngine.Debug.Log("xxxxxx wind speed   " + gl_pause_flag + "    fov:" + helicopter_ODE.par_temp.simulation.fov.val  + "        expo:" +   helicopter_ODE.par_temp.transmitter_and_helicopter.transmitter.stick_roll.expo.val + "        " + helicopter_ODE.par_temp.scenery.weather.wind_speed.val + "    " + helicopter_ODE.par_temp.scenery.weather.wind_speed.val + "    " + helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.mass_total.val + "    " + helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.mass_total.val);

        // recalulate parameter 
        helicopter_ODE.par_temp.transmitter_and_helicopter.Update_Calculated_Parameter();

        // remove almost all children form Scroll View Content UI
        foreach (Transform child in ui_scroll_view_content)
        {
            if (child.name != "Text_Title" && child.name != "Text_Show_in_Sim")
                GameObject.Destroy(child.gameObject);
        }

        foreach (Transform child in ui_overlaid_parameterlist)
        {
            // if (child.name != "Text_Title" && child.name != "Text_Show_in_Sim")
            GameObject.Destroy(child.gameObject);
        }

        // setup header string
        if (ui_which_tab_is_selected == Available_Tabs.simulation)
            ui_text_title.text = "Game Settings";
        if (ui_which_tab_is_selected == Available_Tabs.scenery)
            ui_text_title.text = "Setup Scenery - " + scenery_name;
        if (ui_which_tab_is_selected == Available_Tabs.transmitter)
            ui_text_title.text = "Setup Transmitter - " + helicopter_name;
        if (ui_which_tab_is_selected == Available_Tabs.helicopter)
            ui_text_title.text = "Setup Helicopter - " + helicopter_name;
        if (ui_which_tab_is_selected == Available_Tabs.tuning)
            ui_text_title.text = "Setup Tuning - " + helicopter_name;

        // reset variables that define the vertical position of each row 
        prefabs_vertical_position_on_scroll_view_ui = prefabs_vertical_position_on_scroll_view_ui_START_VALUE; // start at this vertical position to put prefabs on Croll View's Content 
        prefabs_vertical_position_on_screen_overlay_ui = prefabs_vertical_position_on_screen_overlay_ui_START_VALUE;

        
        // generate UI elements from prefabs
        if (ui_which_tab_is_selected == Available_Tabs.simulation)
            ParameterObject_To_UIPrefab(ui_scroll_view_content, Ui_Styles.ui_scroll_view_content, (object)helicopter_ODE.par_temp.simulation); 
        if (ui_which_tab_is_selected == Available_Tabs.scenery)
            ParameterObject_To_UIPrefab(ui_scroll_view_content, Ui_Styles.ui_scroll_view_content, (object)helicopter_ODE.par_temp.scenery); 
        if (ui_which_tab_is_selected == Available_Tabs.transmitter)
            ParameterObject_To_UIPrefab(ui_scroll_view_content, Ui_Styles.ui_scroll_view_content, (object)helicopter_ODE.par_temp.transmitter_and_helicopter.transmitter); 
        if (ui_which_tab_is_selected == Available_Tabs.helicopter)
             ParameterObject_To_UIPrefab(ui_scroll_view_content, Ui_Styles.ui_scroll_view_content, (object)helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter);
        if (ui_which_tab_is_selected == Available_Tabs.tuning)
            ParameterObject_To_UIPrefab(ui_scroll_view_content, Ui_Styles.ui_scroll_view_content, (object)helicopter_ODE.par_temp.transmitter_and_helicopter.tuning);

        ParameterObject_To_UIPrefab(ui_overlaid_parameterlist, Ui_Styles.ui_overlaid_parameterlist, (object)helicopter_ODE.par_temp);

        // resize rect-transform area now to fit all UI-prefabs into
        ui_scroll_view_content.GetComponent<RectTransform>().sizeDelta = new Vector2(0, prefabs_vertical_position_on_scroll_view_ui + 100);
       
        // change/apply scenery parameter
        Change_Skybox_Sun_Sound_Camera_Parameter();

        // change ODE timing parameter
        //thread_ODE_deltat = Helper.Clamp(helicopter_ODE.par.simulation.physics.delta_t);

        // controller parameter
        stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_collective].axis_settings.clearance = helicopter_ODE.par.transmitter_and_helicopter.transmitter.stick_collective.clearance.vect3[helicopter_ODE.flight_bank];
        stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_collective].axis_settings.expo = helicopter_ODE.par.transmitter_and_helicopter.transmitter.stick_collective.expo.vect3[helicopter_ODE.flight_bank];
        stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_collective].axis_settings.dualrate = helicopter_ODE.par.transmitter_and_helicopter.transmitter.stick_collective.dualrate.vect3[helicopter_ODE.flight_bank];
        stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_yaw].axis_settings.clearance = helicopter_ODE.par.transmitter_and_helicopter.transmitter.stick_yaw.clearance.vect3[helicopter_ODE.flight_bank];
        stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_yaw].axis_settings.expo = helicopter_ODE.par.transmitter_and_helicopter.transmitter.stick_yaw.expo.vect3[helicopter_ODE.flight_bank];
        stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_yaw].axis_settings.dualrate = helicopter_ODE.par.transmitter_and_helicopter.transmitter.stick_yaw.dualrate.vect3[helicopter_ODE.flight_bank];
        stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_pitch].axis_settings.clearance = helicopter_ODE.par.transmitter_and_helicopter.transmitter.stick_pitch.clearance.vect3[helicopter_ODE.flight_bank];
        stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_pitch].axis_settings.expo = helicopter_ODE.par.transmitter_and_helicopter.transmitter.stick_pitch.expo.vect3[helicopter_ODE.flight_bank];
        stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_pitch].axis_settings.dualrate = helicopter_ODE.par.transmitter_and_helicopter.transmitter.stick_pitch.dualrate.vect3[helicopter_ODE.flight_bank];
        stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_roll].axis_settings.clearance = helicopter_ODE.par.transmitter_and_helicopter.transmitter.stick_roll.clearance.vect3[helicopter_ODE.flight_bank];
        stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_roll].axis_settings.expo = helicopter_ODE.par.transmitter_and_helicopter.transmitter.stick_roll.expo.vect3[helicopter_ODE.flight_bank];
        stru_controller_settings_work.list_channel_settings[stru_controller_settings_work.channel_roll].axis_settings.dualrate = helicopter_ODE.par.transmitter_and_helicopter.transmitter.stick_roll.dualrate.vect3[helicopter_ODE.flight_bank];

    }
    // ##################################################################################






    // ##################################################################################
    // recursively get all properties of a class and generate the parameter UI 
    // https://stackoverflow.com/questions/20554103/recursively-get-properties-child-properties-of-a-class 
    // the string output is not used here and could be removed
    // ##################################################################################
    public void ParameterObject_To_UIPrefab(Transform parent_ui_container, Ui_Styles ui_style, object obj, int indent = 0)
    {
        //var sb = new StringBuilder();

        if (obj != null)
        {
            string indent_string = new string(' ', indent);

            Type objType = obj.GetType();
            PropertyInfo[] props = objType.GetProperties();

            //// handle tab selection: print only parameter, that are inside structure, that is selected by UI tab
            //if(indent==0)
            //{
            //    UnityEngine.Debug.Log("iiiiiiiiiiiiiiiiiiiiiii objType" + objType.ToString() + " props " + props.ToString());
            //}

            foreach (PropertyInfo prop in props)
            {
                if (prop.GetIndexParameters().Length == 0)
                {
                    object prop_value = prop.GetValue(obj);
                    var elems = prop_value as IList;

                    if (elems != null)
                    {
                        foreach (var item in elems)
                        {
                            //sb.Append($"{indent_string}- {prop.Name} :\n");
                            //sb.Append(ParameterObject_To_UIPrefab(parent_ui_container, ui_style, item, indent + 4));  // recursive call 
                            ParameterObject_To_UIPrefab(parent_ui_container, ui_style, item, indent + 4);  // recursive call 
                        }
                    }
                    else if (prop.Name != "ExtensionData")
                    {
                        //sb.Append($" {indent_string}O {prop.Name} = {prop_value}\n");

                        if (prop.PropertyType.Assembly == objType.Assembly)
                        {
                            if (prop_value.GetType() == null)
                            {
                                //sb.Append(ParameterObject_To_UIPrefab(parent_ui_container, ui_style, prop_value, indent + 4)); // recursive call 
                            }
                            else if (prop_value.GetType() == typeof(stru_bool))
                            {
                                CreatePrefab_Parameter_Instance(parent_ui_container, ui_style, prop, prop_value, indent_string);
                                //sb.Append($"{indent_string}   - {prop.Name} val = { ((stru_bool)prop_value).val }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} hint = { ((stru_bool)prop_value).hint }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} comment = { ((stru_bool)prop_value).comment }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} unit = { ((stru_bool)prop_value).unit }\n");
                            }
                            else if (prop_value.GetType() == typeof(stru_int))
                            {
                                CreatePrefab_Parameter_Instance(parent_ui_container, ui_style, prop, prop_value, indent_string);
                                //sb.Append($"{indent_string}   - {prop.Name} val = { ((stru_int)prop_value).val }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} min = { ((stru_int)prop_value).min }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} max = { ((stru_int)prop_value).max }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} hint = { ((stru_int)prop_value).hint }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} comment = { ((stru_int)prop_value).comment }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} unit = { ((stru_int)prop_value).unit }\n");
                            }
                            else if (prop_value.GetType() == typeof(stru_float))
                            {
                                CreatePrefab_Parameter_Instance(parent_ui_container, ui_style, prop, prop_value, indent_string);
                                //sb.Append($"{indent_string}   - {prop.Name} val = { ((stru_float)prop_value).val }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} min = { ((stru_float)prop_value).min }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} max = { ((stru_float)prop_value).max }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} hint = { ((stru_float)prop_value).hint }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} comment = { ((stru_float)prop_value).comment }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} unit = { ((stru_float)prop_value).unit }\n");
                            }
                            else if (prop_value.GetType() == typeof(stru_Vector3))
                            {
                                CreatePrefab_Parameter_Instance(parent_ui_container, ui_style, prop, prop_value, indent_string);
                                //sb.Append($"{indent_string}   - {prop.Name} val = { ((stru_Vector3)prop_value).vect3.x} { ((stru_Vector3)prop_value).vect3.y} { ((stru_Vector3)prop_value).vect3.z}\n");
                                //sb.Append($"{indent_string}   - {prop.Name} hint = { ((stru_Vector3)prop_value).hint }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} comment = { ((stru_Vector3)prop_value).comment }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} unit = { ((stru_Vector3)prop_value).unit }\n");
                            }
                            else if (prop_value.GetType() == typeof(stru_Vector3_list))
                            {
                                CreatePrefab_Parameter_Instance(parent_ui_container, ui_style, prop, prop_value, indent_string);
                                //for (int i = 0; i < ((stru_Vector3_list)prop_value).vect3.Count; i++)
                                //{
                                //    sb.Append($"{indent_string}   - {prop.Name} val[{i}] = { ((stru_Vector3_list)prop_value).vect3[i].x} { ((stru_Vector3_list)prop_value).vect3[i].y} { ((stru_Vector3_list)prop_value).vect3[i].z}\n");
                                //}
                                //sb.Append($"{indent_string}   - {prop.Name} hint = { ((stru_Vector3_list)prop_value).hint }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} comment = { ((stru_Vector3_list)prop_value).comment }\n");
                                //sb.Append($"{indent_string}   - {prop.Name} unit = { ((stru_Vector3_list)prop_value).unit }\n");
                            }
                            else if (prop_value.GetType() == typeof(stru_list))
                            {
                                CreatePrefab_Parameter_Instance(parent_ui_container, ui_style, prop, prop_value, indent_string);
                            }
                            else
                            {
                                CreatePrefab_Headline_Instance(parent_ui_container, ui_style, prop, prop_value, indent_string);
                                ParameterObject_To_UIPrefab(parent_ui_container, ui_style, prop_value, indent + 4);  // recursive call   
                                //sb.Append(ParameterObject_To_UIPrefab(parent_ui_container, ui_style, prop_value, indent + 4));  // recursive call         
                            }
                        }
                    }
                }

            }
        }

        //return sb.ToString();
    }
    // ##################################################################################





    // ##################################################################################
    // instantiate headline prefab
    // ##################################################################################
    void CreatePrefab_Headline_Instance(Transform parent_ui_container, Ui_Styles ui_Style, PropertyInfo prop, object prop_value, string indent_string)
    {
        // headlines only on ui_scroll_view_content not on ui_overlaid_parameterlist
        if (ui_Style == Ui_Styles.ui_scroll_view_content)
        {
            prefabs_vertical_position_on_scroll_view_ui += 45;

            GameObject prefab_object = (GameObject)Instantiate(ui_prefab_headline);
            prefab_object.transform.SetParent(parent_ui_container, true);
            prefab_object.transform.localScale = new Vector3(1, 1, 1);
            prefab_object.transform.localPosition = new Vector3(40 + indent_string.Length * 0, -(prefabs_vertical_position_on_scroll_view_ui), 00);
            prefab_object.transform.localRotation = Quaternion.Euler(0, 0, 0);

            Text text_object_name = prefab_object.transform.Find("Text").GetComponent<Text>();

            text_object_name.text = prop.Name.ToUpper();
        }
    }
    // ##################################################################################





    // ##################################################################################
    // instantiate parameter prefabs, whick prefab is used depends on parameter type (stru_int, stru_float,...)
    // ##################################################################################
    void CreatePrefab_Parameter_Instance(Transform parent_ui_container, Ui_Styles ui_Style, PropertyInfo prop, object prop_value, string indent_string)
    {
        //Type mytype = prop.PropertyType;
        //Component myGameObject = prop_value.GetComponent(mytype);
        bool show_in_sim = false;
        float prefabs_vertical_position = 0;

        // TODO better???
        if (prop.PropertyType == typeof(stru_bool))
            show_in_sim = ((stru_bool)prop_value).show_in_sim;
        if (prop.PropertyType == typeof(stru_int))
            show_in_sim = ((stru_int)prop_value).show_in_sim;
        if (prop.PropertyType == typeof(stru_float))
            show_in_sim = ((stru_float)prop_value).show_in_sim;
        if (prop.PropertyType == typeof(stru_Vector3))
            show_in_sim = ((stru_Vector3)prop_value).show_in_sim;
        if (prop.PropertyType == typeof(stru_Vector3_list))
            show_in_sim = ((stru_Vector3_list)prop_value).show_in_sim;
        if (prop.PropertyType == typeof(stru_list))
            show_in_sim = ((stru_list)prop_value).show_in_sim;


        if (ui_Style == Ui_Styles.ui_scroll_view_content || show_in_sim)
        {
            //
            if (ui_Style == Ui_Styles.ui_scroll_view_content)
            {
                prefabs_vertical_position_on_scroll_view_ui += 27;
                prefabs_vertical_position = -prefabs_vertical_position_on_scroll_view_ui;
            }
            if (ui_Style == Ui_Styles.ui_overlaid_parameterlist)
            {
                prefabs_vertical_position_on_screen_overlay_ui += 27;
                prefabs_vertical_position = +prefabs_vertical_position_on_screen_overlay_ui;
            }


            // type: bool
            if (prop.PropertyType == typeof(stru_bool))
            {
                // instantiate prefab
                GameObject prefab_object = (GameObject)Instantiate(ui_prefab_bool);
                prefab_object.transform.SetParent(parent_ui_container, false);
                prefab_object.transform.localScale = new Vector3(1, 1, 1);
                prefab_object.transform.localPosition = new Vector3(50 + indent_string.Length * 0, prefabs_vertical_position, 00);
                if (ui_Style == Ui_Styles.ui_overlaid_parameterlist) // todo 
                {
                    RectTransform rect_transform = prefab_object.GetComponent<RectTransform>();
                    rect_transform.anchorMax = new Vector2(0, 0); // BottomLeft
                    rect_transform.anchorMin = new Vector2(0, 0); // BottomLeft
                }

                // get from prefab TextObject and inputfield_value_object
                Text text_object_name = prefab_object.transform.Find("Text_name").GetComponent<Text>();
                Toggle toggle_show_in_sim_object = prefab_object.transform.Find("Toggle_show_in_sim").GetComponent<Toggle>();
                Toggle toggle_input_object = prefab_object.transform.Find("Toggle_input").GetComponent<Toggle>();
                Text text_units_object = prefab_object.transform.Find("Text_units").GetComponent<Text>();
                Text text_hints_object = prefab_object.transform.Find("Text_hints").GetComponent<Text>();
                
                // setup prefab's TextObject
                text_object_name.text = prop.Name;

                // setup prefab's inputfield_value_object
                toggle_show_in_sim_object.isOn = ((stru_bool)prop_value).show_in_sim;
                toggle_input_object.isOn = ((stru_bool)prop_value).val;
                text_units_object.text = ((stru_bool)prop_value).unit;
                text_hints_object.text = ((stru_bool)prop_value).hint;
                if (((stru_bool)prop_value).calculated)
                {
                    toggle_input_object.interactable = false;
                    toggle_input_object.image.color = Color.gray;
                }
                else
                {
                    // AddListener to inputfield_value_object
                    toggle_input_object.onValueChanged.AddListener(delegate { Listener_Toggle_Input_Object_OnValueChanged_Bool(text_object_name, toggle_input_object, prop_value); });
                }
                toggle_show_in_sim_object.onValueChanged.AddListener(delegate { Listener_Toggle_Show_In_Simulation_As_Overlay(text_object_name, toggle_show_in_sim_object, prop_value); });
               
            }


            // type: float and int
            //UnityEngine.Debug.Log("uuuu: " + prop.Name + " " + prop.GetValue(par) + " " + prop.GetType() + " " + prop.FieldType);
            if ( prop.PropertyType == typeof(stru_int) || prop.PropertyType == typeof(stru_float) )
            {
                // instantiate prefab
                GameObject prefab_object = (GameObject)Instantiate(ui_prefab_scalar);
                prefab_object.transform.SetParent(parent_ui_container, false);
                prefab_object.transform.localScale = new Vector3(1, 1, 1);
                prefab_object.transform.localPosition = new Vector3(50 + indent_string.Length * 0, prefabs_vertical_position, 00);
                if (ui_Style == Ui_Styles.ui_overlaid_parameterlist) // todo 
                {
                    RectTransform rect_transform = prefab_object.GetComponent<RectTransform>();
                    rect_transform.anchorMax = new Vector2(0, 0); // BottomLeft
                    rect_transform.anchorMin = new Vector2(0, 0); // BottomLeft
                }

                // get from prefab TextObject and inputfield_value_object
                Text text_object_name = prefab_object.transform.Find("Text_name").GetComponent<Text>();
                Toggle toggle_show_in_sim_object = prefab_object.transform.Find("Toggle_show_in_sim").GetComponent<Toggle>();
                InputField inputfield_value_object = prefab_object.transform.Find("InputField").GetComponent<InputField>();
                Text text_units_object = prefab_object.transform.Find("Text_units").GetComponent<Text>();
                Text text_hints_object = prefab_object.transform.Find("Text_hints").GetComponent<Text>();
                
                // setup prefab's TextObject
                text_object_name.text = prop.Name;

                // setup prefab's inputfield_value_object
                if (prop.PropertyType == typeof(stru_int))
                {
                    toggle_show_in_sim_object.isOn = ((stru_int)prop_value).show_in_sim;
                    inputfield_value_object.contentType = InputField.ContentType.IntegerNumber;
                    inputfield_value_object.text = (((stru_int)prop_value).val).ToString();
                    text_units_object.text = ((stru_int)prop_value).unit;
                    text_hints_object.text = ((stru_int)prop_value).hint;
                    if (((stru_int)prop_value).calculated)
                    {
                        inputfield_value_object.readOnly = true;
                        inputfield_value_object.image.color = Color.gray;
                    }
                    else
                    {
                        // AddListener to inputfield_value_object
                        inputfield_value_object.onEndEdit.AddListener(delegate { Listener_Input_Field_OnEndEdit_Scalar(text_object_name, inputfield_value_object, prop_value); });
                    }
                    toggle_show_in_sim_object.onValueChanged.AddListener(delegate { Listener_Toggle_Show_In_Simulation_As_Overlay(text_object_name, toggle_show_in_sim_object, prop_value); });

                }
                if (prop.PropertyType == typeof(stru_float))
                {
                    toggle_show_in_sim_object.isOn = ((stru_float)prop_value).show_in_sim;
                    inputfield_value_object.contentType = InputField.ContentType.DecimalNumber;
                    inputfield_value_object.text = (((stru_float)prop_value).val).ToString("F8");
                    text_units_object.text = ((stru_float)prop_value).unit;
                    text_hints_object.text = ((stru_float)prop_value).hint;
                    if (((stru_float)prop_value).calculated)
                    {
                        inputfield_value_object.readOnly = true;
                        inputfield_value_object.image.color = Color.gray;
                    }
                    else
                    {
                        // AddListener to inputfield_value_object
                        inputfield_value_object.onEndEdit.AddListener(delegate { Listener_Input_Field_OnEndEdit_Scalar(text_object_name, inputfield_value_object, prop_value); });
                    }
                    toggle_show_in_sim_object.onValueChanged.AddListener(delegate { Listener_Toggle_Show_In_Simulation_As_Overlay(text_object_name, toggle_show_in_sim_object, prop_value); });
                }

            }


            // type: Vector3
            if (prop.PropertyType == typeof(stru_Vector3))
            {
                // instantiate prefab
                GameObject prefab_object = (GameObject)Instantiate(ui_prefab_vector3);
                prefab_object.transform.SetParent(parent_ui_container, false);
                prefab_object.transform.localScale = new Vector3(1, 1, 1);
                prefab_object.transform.localPosition = new Vector3(50 + indent_string.Length * 0, prefabs_vertical_position, 00);
                if (ui_Style == Ui_Styles.ui_overlaid_parameterlist) // todo 
                {
                    RectTransform rect_transform = prefab_object.GetComponent<RectTransform>();
                    rect_transform.anchorMax = new Vector2(0, 0); // BottomLeft
                    rect_transform.anchorMin = new Vector2(0, 0); // BottomLeft
                }

                // get from prefab TextObject and inputfield_value_object
                Text text_object_name = prefab_object.transform.Find("Text_name").GetComponent<Text>();
                Toggle toggle_show_in_sim_object = prefab_object.transform.Find("Toggle_show_in_sim").GetComponent<Toggle>();
                InputField inputfield_value_object_x = prefab_object.transform.Find("InputField_x").GetComponent<InputField>();
                InputField inputfield_value_object_y = prefab_object.transform.Find("InputField_y").GetComponent<InputField>();
                InputField inputfield_value_object_z = prefab_object.transform.Find("InputField_z").GetComponent<InputField>();
                Text text_units_object = prefab_object.transform.Find("Text_units").GetComponent<Text>();
                Text text_hints_object = prefab_object.transform.Find("Text_hints").GetComponent<Text>();

                // setup prefab's TextObject
                text_object_name.text = prop.Name;

                toggle_show_in_sim_object.isOn = ((stru_Vector3)prop_value).show_in_sim;
                inputfield_value_object_x.contentType = InputField.ContentType.DecimalNumber;
                inputfield_value_object_y.contentType = InputField.ContentType.DecimalNumber;
                inputfield_value_object_z.contentType = InputField.ContentType.DecimalNumber;
                inputfield_value_object_x.text = (((stru_Vector3)prop_value).vect3.x).ToString("0.000");
                inputfield_value_object_y.text = (((stru_Vector3)prop_value).vect3.y).ToString("0.000");
                inputfield_value_object_z.text = (((stru_Vector3)prop_value).vect3.z).ToString("0.000");
                text_units_object.text = ((stru_Vector3)prop_value).unit;
                text_hints_object.text = ((stru_Vector3)prop_value).hint;
                if (((stru_Vector3)prop_value).calculated)
                {
                    inputfield_value_object_x.readOnly = true;
                    inputfield_value_object_x.image.color = Color.gray;
                    inputfield_value_object_y.readOnly = true;
                    inputfield_value_object_y.image.color = Color.gray;
                    inputfield_value_object_z.readOnly = true;
                    inputfield_value_object_z.image.color = Color.gray;
                }
                else
                {
                    // AddListener to inputfield_value_object
                    inputfield_value_object_x.onEndEdit.AddListener(delegate { Listener_Input_Field_OnEndEdit_Vector3(text_object_name, inputfield_value_object_x, inputfield_value_object_y, inputfield_value_object_z, prop_value); });
                    inputfield_value_object_y.onEndEdit.AddListener(delegate { Listener_Input_Field_OnEndEdit_Vector3(text_object_name, inputfield_value_object_x, inputfield_value_object_y, inputfield_value_object_z, prop_value); });
                    inputfield_value_object_z.onEndEdit.AddListener(delegate { Listener_Input_Field_OnEndEdit_Vector3(text_object_name, inputfield_value_object_x, inputfield_value_object_y, inputfield_value_object_z, prop_value); });
                }
                toggle_show_in_sim_object.onValueChanged.AddListener(delegate { Listener_Toggle_Show_In_Simulation_As_Overlay(text_object_name, toggle_show_in_sim_object, prop_value); });
            }


            // type: Vector3-list
            if (prop.PropertyType == typeof(stru_Vector3_list))
            {
                // instantiate prefab
                GameObject prefab_object = (GameObject)Instantiate(ui_prefab_vector3_list_setting);
                prefab_object.transform.SetParent(parent_ui_container, false);
                prefab_object.transform.localScale = new Vector3(1, 1, 1);
                prefab_object.transform.localPosition = new Vector3(50 + indent_string.Length * 0, prefabs_vertical_position, 00);
                if (ui_Style == Ui_Styles.ui_overlaid_parameterlist) // todo 
                {
                    RectTransform rect_transform = prefab_object.GetComponent<RectTransform>();
                    rect_transform.anchorMax = new Vector2(0, 0); // BottomLeft
                    rect_transform.anchorMin = new Vector2(0, 0); // BottomLeft
                }

                // get from prefab TextObject and inputfield_value_object
                Text text_object_name = prefab_object.transform.Find("Text_name").GetComponent<Text>();
                Toggle toggle_show_in_sim_object = prefab_object.transform.Find("Toggle_show_in_sim").GetComponent<Toggle>();
                InputField inputfield_value_object_Size = prefab_object.transform.Find("InputField_Size").GetComponent<InputField>();
                Text text_units_object = prefab_object.transform.Find("Text_units").GetComponent<Text>();
                Text text_hints_object = prefab_object.transform.Find("Text_hints").GetComponent<Text>();
                
                // setup prefab's TextObject
                text_object_name.text = prop.Name;

                toggle_show_in_sim_object.isOn = ((stru_Vector3_list)prop_value).show_in_sim;
                inputfield_value_object_Size.contentType = InputField.ContentType.IntegerNumber;
                inputfield_value_object_Size.text = (((stru_Vector3_list)prop_value).vect3.Count).ToString();
                text_units_object.text = ((stru_Vector3_list)prop_value).unit;
                text_hints_object.text = ((stru_Vector3_list)prop_value).hint;
                if (((stru_Vector3_list)prop_value).calculated)
                {
                    inputfield_value_object_Size.readOnly = true;
                    inputfield_value_object_Size.image.color = Color.gray;
                }

                // AddListener to inputfield_value_object
                inputfield_value_object_Size.onEndEdit.AddListener(delegate { Listener_Input_Field_OnEndEdit_Vector3_List_Setting(text_object_name, inputfield_value_object_Size, prop_value); });

                toggle_show_in_sim_object.onValueChanged.AddListener(delegate { Listener_Toggle_Show_In_Simulation_As_Overlay(text_object_name, toggle_show_in_sim_object, prop_value); });



                List<GameObject> prefab_object_list = new List<GameObject>(new GameObject[(((stru_Vector3_list)prop_value).vect3.Count)]);
                List<InputField> inputfield_value_object_x_list = new List<InputField>(new InputField[(((stru_Vector3_list)prop_value).vect3.Count)]);
                List<InputField> inputfield_value_object_y_list = new List<InputField>(new InputField[(((stru_Vector3_list)prop_value).vect3.Count)]);
                List<InputField> inputfield_value_object_z_list = new List<InputField>(new InputField[(((stru_Vector3_list)prop_value).vect3.Count)]);

                // create UI for all the Vector3-elements in List
                for (int i = 0; i < (((stru_Vector3_list)prop_value).vect3.Count); i++)
                {

                    if (ui_Style == Ui_Styles.ui_scroll_view_content)
                    {
                        prefabs_vertical_position_on_scroll_view_ui += 27;
                        prefabs_vertical_position = -prefabs_vertical_position_on_scroll_view_ui; // list downwards
                    }
                    if (ui_Style == Ui_Styles.ui_overlaid_parameterlist)
                    {
                        prefabs_vertical_position_on_screen_overlay_ui += 27;
                        prefabs_vertical_position = +prefabs_vertical_position_on_screen_overlay_ui; // list upwards
                    }

                    // instantiate prefab
                    prefab_object_list[i] = (GameObject)Instantiate(ui_prefab_vector3_list_vector3);
                    prefab_object_list[i].transform.SetParent(parent_ui_container, false);
                    prefab_object_list[i].transform.localScale = new Vector3(1, 1, 1);
                    prefab_object_list[i].transform.localPosition = new Vector3(50 + indent_string.Length * 0, prefabs_vertical_position, 00);
                    if (ui_Style == Ui_Styles.ui_overlaid_parameterlist) // todo 
                    {
                        RectTransform rect_transform = prefab_object_list[i].GetComponent<RectTransform>();
                        rect_transform.anchorMax = new Vector2(0, 0); // BottomLeft
                        rect_transform.anchorMin = new Vector2(0, 0); // BottomLeft
                    }

                    inputfield_value_object_x_list[i] = prefab_object_list[i].transform.Find("InputField_x").GetComponent<InputField>();
                    inputfield_value_object_y_list[i] = prefab_object_list[i].transform.Find("InputField_y").GetComponent<InputField>();
                    inputfield_value_object_z_list[i] = prefab_object_list[i].transform.Find("InputField_z").GetComponent<InputField>();

                    inputfield_value_object_x_list[i].contentType = InputField.ContentType.DecimalNumber;
                    inputfield_value_object_y_list[i].contentType = InputField.ContentType.DecimalNumber;
                    inputfield_value_object_z_list[i].contentType = InputField.ContentType.DecimalNumber;
                    inputfield_value_object_x_list[i].text = (((stru_Vector3_list)prop_value).vect3[i].x).ToString();
                    inputfield_value_object_y_list[i].text = (((stru_Vector3_list)prop_value).vect3[i].y).ToString();
                    inputfield_value_object_z_list[i].text = (((stru_Vector3_list)prop_value).vect3[i].z).ToString();
                    if (((stru_Vector3_list)prop_value).calculated)
                    {
                        inputfield_value_object_x_list[i].readOnly = true;
                        inputfield_value_object_x_list[i].image.color = Color.gray;
                        inputfield_value_object_y_list[i].readOnly = true;
                        inputfield_value_object_y_list[i].image.color = Color.gray;
                        inputfield_value_object_z_list[i].readOnly = true;
                        inputfield_value_object_z_list[i].image.color = Color.gray;
                    }
                    else
                    {
                        //// AddListener to inputfield_value_object
                        inputfield_value_object_x_list[i].onEndEdit.AddListener(delegate { Listener_Input_Field_OnEndEdit_Vector3_List_Vector3(text_object_name, inputfield_value_object_x_list, inputfield_value_object_y_list, inputfield_value_object_z_list, prop_value); });
                        inputfield_value_object_y_list[i].onEndEdit.AddListener(delegate { Listener_Input_Field_OnEndEdit_Vector3_List_Vector3(text_object_name, inputfield_value_object_x_list, inputfield_value_object_y_list, inputfield_value_object_z_list, prop_value); });
                        inputfield_value_object_z_list[i].onEndEdit.AddListener(delegate { Listener_Input_Field_OnEndEdit_Vector3_List_Vector3(text_object_name, inputfield_value_object_x_list, inputfield_value_object_y_list, inputfield_value_object_z_list, prop_value); });
                    }
                }
            }


            // type: list of strings
            if (prop.PropertyType == typeof(stru_list))
            {
                // instantiate prefab
                GameObject prefab_object = (GameObject)Instantiate(ui_prefab_list);
                prefab_object.transform.SetParent(parent_ui_container, false);
                prefab_object.transform.localScale = new Vector3(1, 1, 1);
                prefab_object.transform.localPosition = new Vector3(50 + indent_string.Length * 0, prefabs_vertical_position, 00);
                if (ui_Style == Ui_Styles.ui_overlaid_parameterlist) // todo 
                {
                    RectTransform rect_transform = prefab_object.GetComponent<RectTransform>();
                    rect_transform.anchorMax = new Vector2(0, 0); // BottomLeft
                    rect_transform.anchorMin = new Vector2(0, 0); // BottomLeft
                }

                // get from prefab TextObject and inputfield_value_object
                Text text_object_name = prefab_object.transform.Find("Text_name").GetComponent<Text>();
                Toggle toggle_show_in_sim_object = prefab_object.transform.Find("Toggle_show_in_sim").GetComponent<Toggle>();
                Dropdown dropdown_object = prefab_object.transform.Find("Dropdown").GetComponent<Dropdown>();
                Text text_units_object = prefab_object.transform.Find("Text_units").GetComponent<Text>();
                Text text_hints_object = prefab_object.transform.Find("Text_hints").GetComponent<Text>();

                // setup prefab's TextObject
                text_object_name.text = prop.Name;

                toggle_show_in_sim_object.isOn = ((stru_list)prop_value).show_in_sim;
        
                dropdown_object.ClearOptions();
                for (int i = 0; i < (((stru_list)prop_value).str.Count); i++)   // Populate List
                    dropdown_object.options.Add(new Dropdown.OptionData() { text = ((stru_list)prop_value).str[i] }  );
                dropdown_object.value = ((stru_list)prop_value).val;
                dropdown_object.RefreshShownValue();
                text_units_object.text = ((stru_list)prop_value).unit;
                text_hints_object.text = ((stru_list)prop_value).hint;
              
                // AddListener to inputfield_value_object
                dropdown_object.onValueChanged.AddListener(delegate { Listener_UI_Dropdown_List_OnValueChanged(text_object_name, dropdown_object, prop_value); });

                toggle_show_in_sim_object.onValueChanged.AddListener(delegate { Listener_Toggle_Show_In_Simulation_As_Overlay(text_object_name, toggle_show_in_sim_object, prop_value); });
            }

        }
    }
    // ##################################################################################




    // ##################################################################################
    // Listener for prefab's dropdown-list
    // ##################################################################################
    void Listener_UI_Dropdown_List_OnValueChanged(Text text_object_name, Dropdown dropdown, object prop_value)
    {
        ((stru_list)prop_value).val = dropdown.value;

        if (((stru_list)prop_value).save_under_player_prefs)
            PlayerPrefs.SetInt("__simulation_" + text_object_name.text, ((stru_list)prop_value).val);

        UI_Update_Parameter_Settings_UI();
    }
    // ##################################################################################




    // ##################################################################################
    // Listener for prefab's inputfield_value_object - Vector3-list's one Vector3
    // ##################################################################################
    void Listener_Input_Field_OnEndEdit_Vector3_List_Vector3(Text text_object_name, List<InputField> input_x, List<InputField> input_y, List<InputField> input_z, object prop_value)
    {
        for (int i = 0; i < (((stru_Vector3_list)prop_value).vect3.Count); i++)
        {
            if (input_x[i].text.Length > 0 && input_y[i].text.Length > 0 && input_z[i].text.Length > 0)
            {
                //UnityEngine.Debug.Log("Text has been entered: " + input_x[i].text + " " + input_y[i].text + " " + input_z[i].text + ",  prop_value.GetType() " + prop_value.GetType() + ", i: " + i);
                ((stru_Vector3_list)prop_value).vect3[i] = new Vector3(Single.Parse(input_x[i].text.Replace(",", ".")), Single.Parse(input_y[i].text.Replace(",", ".")), Single.Parse(input_z[i].text.Replace(",", ".")));
            }
        }
        UI_Update_Parameter_Settings_UI();
    }
    // ##################################################################################




    // ##################################################################################
    // Listener for prefab's inputfield_value_object - List setting (sets the size) of Vector3-list
    // ##################################################################################
    void Listener_Input_Field_OnEndEdit_Vector3_List_Setting(Text text_object_name, InputField input, object prop_value)
    {
        if (input.text.Length > 0)
        {
            // UnityEngine.Debug.Log("Text has been entered: " + input.text + ",  prop_value.GetType() " + prop_value.GetType());
            Helper.Resize_List<Vector3>(((stru_Vector3_list)prop_value).vect3, Int32.Parse(input.text.Replace(",", ".")), new Vector3(0, 0, 0));

            //if (((stru_Vector3)prop_value).save_under_player_prefs)
            //{
                // not implemented
            //}
            UI_Update_Parameter_Settings_UI();
        }
    }
    // ##################################################################################




    // ##################################################################################
    // Listener for prefab's inputfield_value_object - Vector3
    // ##################################################################################
    void Listener_Input_Field_OnEndEdit_Vector3(Text text_object_name, InputField input_x, InputField input_y, InputField input_z, object prop_value)
    {
        if (input_x.text.Length > 0 && input_y.text.Length > 0 && input_z.text.Length > 0)
        {
            //UnityEngine.Debug.Log("Text has been entered: " + input_x.text + " " + input_y.text + " " + input_z.text + ",  prop_value.GetType() " + prop_value.GetType());
            ((stru_Vector3)prop_value).vect3 = new Vector3(Single.Parse(input_x.text.Replace(",", ".")), Single.Parse(input_y.text.Replace(",", ".")), Single.Parse(input_z.text.Replace(",", ".")));

            if (((stru_Vector3)prop_value).save_under_player_prefs)
            {
                PlayerPrefs.SetFloat("__simulation_" + text_object_name.text + "_x", ((stru_Vector3)prop_value).vect3.x);
                PlayerPrefs.SetFloat("__simulation_" + text_object_name.text + "_y", ((stru_Vector3)prop_value).vect3.y);
                PlayerPrefs.SetFloat("__simulation_" + text_object_name.text + "_z", ((stru_Vector3)prop_value).vect3.z);
            }

            UI_Update_Parameter_Settings_UI();
        }
    }
    // ##################################################################################




    // ##################################################################################
    // Listener for prefab's inputfield_value_object - stru_float and stru_int
    // ##################################################################################
    void Listener_Input_Field_OnEndEdit_Scalar(Text text_object_name, InputField input, object prop_value)
    {
        if (input.text.Length > 0)
        {
            //UnityEngine.Debug.Log("Text has been entered: " + input.text + ",  prop_value.GetType() " + prop_value.GetType() + " text_object_name " + text_object_name);
            if (prop_value.GetType() == typeof(stru_float))
            { 
                ((stru_float)prop_value).val = Single.Parse(input.text.Replace(",", "."));

                if (((stru_float)prop_value).save_under_player_prefs)
                    PlayerPrefs.SetFloat("__simulation_" + text_object_name.text, ((stru_float)prop_value).val); 
            }
            if (prop_value.GetType() == typeof(stru_int))
            { 
                ((stru_int)prop_value).val = Int32.Parse(input.text.Replace(",", "."));

                if (((stru_int)prop_value).save_under_player_prefs)
                    PlayerPrefs.SetInt("__simulation_" + text_object_name.text, ((stru_int)prop_value).val); 
            }

            UI_Update_Parameter_Settings_UI();
        }
    }
    // ##################################################################################




    // ##################################################################################
    // Listener for prefab's inputfield_value_object - stru_bool
    // ##################################################################################
    void Listener_Toggle_Input_Object_OnValueChanged_Bool(Text text_object_name, Toggle input, object prop_value)
    {
        //UnityEngine.Debug.Log("Text has been entered: " + input.isOn + ",  prop_value.GetType() " + prop_value.GetType());
        ((stru_bool)prop_value).val = input.isOn;

        if (((stru_bool)prop_value).save_under_player_prefs)
            PlayerPrefs.SetInt("__simulation_" + text_object_name.text, (((stru_bool)prop_value).val) ? 1 : 0 );  // no SetBool exists, so use SetInt

        UI_Update_Parameter_Settings_UI();
    }
    // ##################################################################################




    // ##################################################################################
    // Listener for prefab's Toggle_show_in_sim
    // ##################################################################################
    void Listener_Toggle_Show_In_Simulation_As_Overlay(Text text_object_name, Toggle input, object prop_value)
    {
        if (prop_value.GetType() == typeof(stru_bool))
        {
            ((stru_bool)prop_value).show_in_sim = input.isOn;

            if (((stru_bool)prop_value).save_under_player_prefs)
                PlayerPrefs.SetInt("__simulation_" + text_object_name.text, (((stru_bool)prop_value).val) ? 1 : 0); // SetBool does not exits, use int instead
        }
        if (prop_value.GetType() == typeof(stru_int))
        {
            ((stru_int)prop_value).show_in_sim = input.isOn;

            if (((stru_int)prop_value).save_under_player_prefs)
                PlayerPrefs.SetInt("__simulation_" + text_object_name.text, ((stru_int)prop_value).val); 
        }
        if (prop_value.GetType() == typeof(stru_float))
        { 
            ((stru_float)prop_value).show_in_sim = input.isOn;

            if (((stru_float)prop_value).save_under_player_prefs)
                PlayerPrefs.SetFloat("__simulation_" + text_object_name.text, ((stru_float)prop_value).val);
        }
        if (prop_value.GetType() == typeof(stru_Vector3))
        { 
            ((stru_Vector3)prop_value).show_in_sim = input.isOn;

            if (((stru_Vector3)prop_value).save_under_player_prefs)
            {
                PlayerPrefs.SetFloat("__simulation_" + text_object_name.text + "_x", ((stru_Vector3)prop_value).vect3.x);
                PlayerPrefs.SetFloat("__simulation_" + text_object_name.text + "_y", ((stru_Vector3)prop_value).vect3.y);
                PlayerPrefs.SetFloat("__simulation_" + text_object_name.text + "_z", ((stru_Vector3)prop_value).vect3.z);
            }
        }
        if (prop_value.GetType() == typeof(stru_Vector3_list))
        { 
            ((stru_Vector3_list)prop_value).show_in_sim = input.isOn;

            //if (((stru_Vector3)prop_value).save_under_player_prefs)
            //{
                // not implemented
            //}
        }
        if (prop_value.GetType() == typeof(stru_list))
        {
            ((stru_list)prop_value).show_in_sim = input.isOn;

            if (((stru_list)prop_value).save_under_player_prefs)
                PlayerPrefs.SetFloat("__simulation_" + text_object_name.text, ((stru_list)prop_value).val);
        }
        UI_Update_Parameter_Settings_UI();
    }
    // ##################################################################################
#endregion




}



