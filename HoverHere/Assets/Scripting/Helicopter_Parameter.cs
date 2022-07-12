// ##################################################################################
// Free RC helicopter Simulator
// 20.01.2020 
// Copyright (c) zulu
//
// Unity c# code
// ##################################################################################
//
// scale effect on mass
//  Example mass zylinder
//  m = pi*r^2*h = pi*(r*s)^2*(h*s) ==>
//  m = pi*r^2*h  *  (s^3)
// 
// scale effect on inertia
//  Example inertia zylinder around z-axis, s: scale:
//  Iz = 0.5*m*r^2 = 0.5*(pi*r^2*h)*r^2 = 0.5*(pi*(r*s)^2*(h*s))*(r*s)^2 ==> 
//  Iz = 0.5*(pi*r^2*h)*r^2 * (s^5) 
// 
//
//
//
//
//
// ##################################################################################
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;
using System;
using Common;


namespace Parameter
{

    // ##################################################################################
    // paramter structur setup
    // ##################################################################################
    [Serializable]
    public class stru_hint
    {
        /// <summary> Place for hints </summary>
        public string hint { get; set; } // 
        /// <summary> Comment shonw on parameter UI </summary>
        public string comment { get; set; } // 
        /// <summary> Phyiscal unit of the parameter </summary>
        public string unit { get; set; } // 
        /// <summary> Is the value calulated from other values (true) or a user input (false)  </summary>
        public bool calculated { get; set; }
        /// <summary> Shows parameter as overlay during sim to allow faster manipulation </summary>
        public bool show_in_sim { get; set; }
        /// <summary> if true, then save this variable under PlayerPrefs </summary>
        public bool save_under_player_prefs { get; set; }

        public stru_hint()
        {
            hint = "not set";
            comment = "not set";
            unit = "not set";
            calculated = false;
            show_in_sim = false;
            save_under_player_prefs = false;
        }
    }
    [Serializable]
    public class stru_bool : stru_hint
    {
        public bool val { get; set; }
    }
    [Serializable]
    public class stru_int : stru_hint
    {
        public int val { get; set; }
        public int min { get; set; }
        public int max { get; set; }
    }
    [Serializable]
    public class stru_float : stru_hint
    {
        public float val { get; set; }
        public float min { get; set; }
        public float max { get; set; }
    }
    [Serializable]
    public class stru_Vector3 : stru_hint
    {
        public Vector3 vect3 { get; set; }
    }
    [Serializable]
    public class stru_Vector3_list : stru_hint
    {
        public List<Vector3> vect3 { get; set; }
    }
    [Serializable]
    public class stru_list : stru_hint // dropdown
    {
        public List<string> str { get; set; }
        public int val { get; set; }
    }
    // ##################################################################################




    // ##################################################################################
    // paramter structur setup
    // ##################################################################################
    [Serializable]
    public class stru_physics
    {
        public stru_float delta_t { get; set; } // [sec]
        public stru_float timescale { get; set; } // [-] timescale factor

        public stru_physics()
        {
            delta_t = new stru_float();
            timescale = new stru_float();
        
            delta_t.val = 0.0050f;
            delta_t.min = 0.0001f;
            delta_t.max = 0.0100f;
            delta_t.hint = "Physics simulation step size";
            delta_t.comment = "";
            delta_t.unit = "sec";
            delta_t.save_under_player_prefs = true;

            timescale.val = 1f;
            timescale.max = 3.00f;
            timescale.min = 0.10f;
            timescale.hint = "Timescale factor";
            timescale.comment = "";
            timescale.unit = "-";
            timescale.save_under_player_prefs = true;
        }
    }

    [Serializable]
    public class stru_audio
    {
        public stru_float master_sound_volume { get; set; } // [%]
        public stru_float commentator_audio_source_volume { get; set; }  // [%]
        public stru_float crash_audio_source_volume { get; set; }  // [%]

        public stru_audio()
        {
            master_sound_volume = new stru_float();
            commentator_audio_source_volume = new stru_float();
            crash_audio_source_volume = new stru_float();

            master_sound_volume.val = 100.00f;
            master_sound_volume.min = 0.000f;
            master_sound_volume.max = 100.0f;
            master_sound_volume.hint = "Master audio volume";
            master_sound_volume.comment = "";
            master_sound_volume.unit = "%";
            master_sound_volume.save_under_player_prefs = true;

            commentator_audio_source_volume.val = 70.00f;
            commentator_audio_source_volume.min = 0.000f;
            commentator_audio_source_volume.max = 100.0f;
            commentator_audio_source_volume.hint = "Commentator's audio volume";
            commentator_audio_source_volume.comment = "";
            commentator_audio_source_volume.unit = "%";
            commentator_audio_source_volume.save_under_player_prefs = true;

            crash_audio_source_volume.val = 40.00f;
            crash_audio_source_volume.min = 0.000f;
            crash_audio_source_volume.max = 100.0f;
            crash_audio_source_volume.hint = "Crash audio volume";
            crash_audio_source_volume.comment = "";
            crash_audio_source_volume.unit = "%";
            crash_audio_source_volume.save_under_player_prefs = true;
        }
    }

    [Serializable]
    public class stru_camera
    {
        public stru_float stiffness { get; set; } // [-]
        public stru_bool auto_zoom { get; set; } // [-]
        public stru_float fov { get; set; } // [deg]
        public stru_float shaking { get; set; } // [%]
        public stru_float xr_zoom_factor { get; set; } // []
        public stru_float keep_ground_visible { get; set; } // [deg]

        public stru_camera()
        {
            stiffness = new stru_float();
            auto_zoom = new stru_bool();
            fov = new stru_float();
            shaking = new stru_float();
            xr_zoom_factor = new stru_float();
            keep_ground_visible = new stru_float();

            stiffness.val = 10.0f;
            stiffness.min = 0.0f;
            stiffness.max = 20f;
            stiffness.hint = "Stiffness of the camera-helicopter following rotation";
            stiffness.comment = "";
            stiffness.unit = "-";
            stiffness.save_under_player_prefs = true;

            auto_zoom.val = true;
            auto_zoom.hint = "Automatically zooms camera";
            auto_zoom.comment = "";
            auto_zoom.unit = "-";
            auto_zoom.save_under_player_prefs = true;

            fov.val = 30f;
            fov.min = 1f;
            fov.max = 80.0f;
            fov.hint = "The field of view of the camera.";
            fov.comment = "";
            fov.unit = "deg";
            fov.save_under_player_prefs = true;

            shaking.val = 100f;
            shaking.min = 0f;
            shaking.max = 500f;
            shaking.hint = "Imitates the pilot's motion.";
            shaking.comment = "";
            shaking.unit = "%";
            shaking.save_under_player_prefs = true;

            xr_zoom_factor.val = 1.01f;
            xr_zoom_factor.min = 1f;
            xr_zoom_factor.max = 2f;
            xr_zoom_factor.hint = "Zooms the XR projection in virtual reality mode";
            xr_zoom_factor.comment = "";
            xr_zoom_factor.unit = "-";
            xr_zoom_factor.save_under_player_prefs = true;

            keep_ground_visible.val = 6f;
            keep_ground_visible.min = 1f;
            keep_ground_visible.max = 2f;
            keep_ground_visible.hint = "Try to keep horizont visible.";
            keep_ground_visible.comment = "";
            keep_ground_visible.unit = "deg";
            keep_ground_visible.save_under_player_prefs = true;

            
        }
    }

    [Serializable]
    public class stru_gameplay
    {
        public stru_bool show_pilot { get; set; } // 
        public stru_bool show_fps { get; set; } // 
        public stru_float delay_after_reset { get; set; } // [sec] deactivate user input duration after reset  
        public stru_bool wheel_brake_on_after_heli_change { get; set; } // 
        public stru_bool limit_mouse_to_game { get; set; } // 

        public stru_gameplay()
        {
            show_pilot = new stru_bool();
            show_fps = new stru_bool();
            delay_after_reset = new stru_float();
            wheel_brake_on_after_heli_change = new stru_bool();
            limit_mouse_to_game = new stru_bool();


            show_pilot.val = true;
            show_pilot.hint = "Show pilot with transmitter";
            show_pilot.comment = "";
            show_pilot.unit = "-";
            show_pilot.save_under_player_prefs = true;

            show_fps.val = false;
            show_fps.hint = "Show frame rate";
            show_fps.comment = "";
            show_fps.unit = "-";
            show_fps.save_under_player_prefs = true;

            delay_after_reset.val = 1f;
            delay_after_reset.max = 5.00f;
            delay_after_reset.min = 0.00f;
            delay_after_reset.hint = "Deactivates user input after resetting helicopter for this amount of time.";
            delay_after_reset.comment = "";
            delay_after_reset.unit = "sec";
            delay_after_reset.save_under_player_prefs = true;

            wheel_brake_on_after_heli_change.val = true;
            wheel_brake_on_after_heli_change.hint = "Enable wheel brake automatically after helicopter change";
            wheel_brake_on_after_heli_change.comment = "";
            wheel_brake_on_after_heli_change.unit = "-";
            wheel_brake_on_after_heli_change.save_under_player_prefs = true;

            limit_mouse_to_game.val = true;
            limit_mouse_to_game.hint = "Limit the mouse movement to the game window.";
            limit_mouse_to_game.comment = "";
            limit_mouse_to_game.unit = "-";
            limit_mouse_to_game.save_under_player_prefs = true;

        }
    }

    [Serializable]
    public class stru_graphic_settings
    {
        public stru_bool rotor_disk_complexity { get; set; } // [0,1]
        public stru_float rotor_disk_transparency { get; set; } // [0...1]
        //public stru_float rotor_blade_transparency { get; set; } // [0...1]

        public stru_graphic_settings()
        {
            rotor_disk_complexity = new stru_bool();
            rotor_disk_transparency = new stru_float();
            //rotor_blade_transparency = new stru_float();

            rotor_disk_complexity.val = true;
            rotor_disk_complexity.hint = "Rotor disk simple (false) oder complex (true) model";
            rotor_disk_complexity.comment = "";
            rotor_disk_complexity.unit = "-";
            rotor_disk_complexity.save_under_player_prefs = true;

            rotor_disk_transparency.val = 0.92f;
            rotor_disk_transparency.min = 0f;
            rotor_disk_transparency.max = 1f;
            rotor_disk_transparency.hint = "Rotor disk transparency factor (1: full transparent)";
            rotor_disk_transparency.comment = "";
            rotor_disk_transparency.unit = "(0...1)";
            rotor_disk_transparency.save_under_player_prefs = true;

            //rotor_blade_transparency.val = 0.98f;
            //rotor_blade_transparency.min = 0f;
            //rotor_blade_transparency.max = 1f;
            //rotor_blade_transparency.hint = "Rotor blade transparency factor when rotating (1: full transparent)";
            //rotor_blade_transparency.comment = "";
            //rotor_blade_transparency.unit = "(0...1)";
            //rotor_blade_transparency.save_under_player_prefs = true;

        }
    }


    [Serializable]
    public class stru_graphic_quality
    {
        public stru_bool motion_blur { get; set; } // 
        public stru_bool bloom { get; set; } // 
        public stru_bool depthoffield { get; set; } // 
        public stru_list quality_setting { get; set; } // 
        public stru_list resolution_setting { get; set; } // 


        public stru_graphic_quality()
        {
            motion_blur = new stru_bool();
            bloom = new stru_bool();
            depthoffield = new stru_bool();
            quality_setting = new stru_list();
            resolution_setting = new stru_list();


            motion_blur.val = true;
            motion_blur.hint = "Enables or disables motion blur effect.";
            motion_blur.comment = "";
            motion_blur.unit = "-";
            motion_blur.save_under_player_prefs = true;

            bloom.val = true;
            bloom.hint = "Enables or disables bloom effect. (stength see under scenery -> sun_bloom_intensity)";
            bloom.comment = "";
            bloom.unit = "-";
            bloom.save_under_player_prefs = true;

            depthoffield.val = true;
            depthoffield.hint = "Enables or disables depth of field effect.";
            depthoffield.comment = "";
            depthoffield.unit = "-";
            depthoffield.save_under_player_prefs = true;

            quality_setting.val = 2;
            quality_setting.str = new List<string> { "Low", "High", "Very High", "Ultra" }; // 0 1 2 3
            quality_setting.hint = "Sets graphics quality level.";
            quality_setting.comment = "";
            quality_setting.unit = ""; 
            quality_setting.save_under_player_prefs = true;

            resolution_setting.val = 0;
            resolution_setting.str = new List<string>();
            Resolution[] resolutions = Screen.resolutions;
            for (int i = 0; i<resolutions.Length; i++)
            {
                if(resolutions[i].width > 1024)
                    resolution_setting.str.Add(resolutions[i].width + " x " + resolutions[i].height);
                    //if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                    //    resolution_setting.val = i; // currentResolutionIndex 
            }
            if(resolution_setting.str.Count > 0) // set highest resolution as default
                resolution_setting.val = resolution_setting.str.Count-1;
            resolution_setting.hint = "Change monitor resolution.";
            resolution_setting.comment = "";
            resolution_setting.unit = "";
            resolution_setting.save_under_player_prefs = true;

        }
    }


    [Serializable]
    public class stru_graphic_quality_VR
    {
        public stru_bool motion_blur_VR { get; set; } // 
        public stru_bool bloom_VR { get; set; } // 
        public stru_bool depthoffield_VR { get; set; } // 
        public stru_list quality_setting_VR { get; set; } // 
        public stru_list resolution_setting_VR { get; set; } // 


        public stru_graphic_quality_VR()
        {
            motion_blur_VR = new stru_bool();
            bloom_VR = new stru_bool();
            depthoffield_VR = new stru_bool();
            quality_setting_VR = new stru_list();
            resolution_setting_VR = new stru_list();


            motion_blur_VR.val = false;
            motion_blur_VR.hint = "Enables or disables motion blur effect.";
            motion_blur_VR.comment = "";
            motion_blur_VR.unit = "-";
            motion_blur_VR.save_under_player_prefs = true;

            bloom_VR.val = false;
            bloom_VR.hint = "Enables or disables bloom effect. (stength see under scenery -> sun_bloom_intensity)";
            bloom_VR.comment = "";
            bloom_VR.unit = "-";
            bloom_VR.save_under_player_prefs = true;

            depthoffield_VR.val = true;
            depthoffield_VR.hint = "Enables or disables depth of field effect.";
            depthoffield_VR.comment = "";
            depthoffield_VR.unit = "-";
            depthoffield_VR.save_under_player_prefs = true;

            quality_setting_VR.val = 2;
            quality_setting_VR.str = new List<string> { "Low", "High", "Very High", "Ultra" }; // 0 1 2 3
            quality_setting_VR.hint = "Sets graphics quality level.";
            quality_setting_VR.comment = "";
            quality_setting_VR.unit = "";
            quality_setting_VR.save_under_player_prefs = true;

            resolution_setting_VR.val = 0;
            resolution_setting_VR.str = new List<string>();
            Resolution[] resolutions = Screen.resolutions;
            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].width > 1024)
                    resolution_setting_VR.str.Add(resolutions[i].width + " x " + resolutions[i].height);
                //if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                //    resolution_setting.val = i; // currentResolutionIndex 
            }
            if (resolution_setting_VR.str.Count > 0) // set highest resolution as default
                resolution_setting_VR.val = resolution_setting_VR.str.Count - 1;
            resolution_setting_VR.hint = "Change monitor resolution.";
            resolution_setting_VR.comment = "";
            resolution_setting_VR.unit = "";
            resolution_setting_VR.save_under_player_prefs = true;

        }
    }



    [Serializable]
    public class stru_various
    {
        public stru_list sceneries_file_location { get; set; } // []
        public stru_float V_h { get; set; } // [m/s] friction to stiction transition velocity 
        public stru_float U_max { get; set; } // [m] stiction model's maximal distance to ancor point
        public stru_float stiction_factor { get; set; } // [-]
        
        public stru_various()
        {
            sceneries_file_location = new stru_list();
            V_h = new stru_float();
            U_max = new stru_float();
            stiction_factor = new stru_float();


            sceneries_file_location.val = 1;
            sceneries_file_location.str = new List<string> { "Streaming assets" ,  "Persistent data" };
            sceneries_file_location.hint = "Where should sceneries be saved. 'Peristent data' (only option on MacOS) is outside of game folder.";
            sceneries_file_location.comment = "If 'Peristent data' is seleceted, downloaded files does not need to be downloaded every time, if game is updated.";
            sceneries_file_location.unit = "";
            sceneries_file_location.save_under_player_prefs = true;

            V_h.val = 0.09f;
            V_h.max = 1.000f;
            V_h.min = 0.001f;
            V_h.hint = "Friction to stiction transition velocity";
            V_h.comment = "";
            V_h.unit = "m/s";
            V_h.save_under_player_prefs = true;

            U_max.val = 0.02f;
            U_max.max = 0.100f;
            U_max.min = 0.001f;
            U_max.hint = "Stiction model's maximal distance to ancor point";
            U_max.comment = "";
            U_max.unit = "m";
            U_max.save_under_player_prefs = true;

            stiction_factor.val = 1.0f;
            stiction_factor.max = 10.00f;
            stiction_factor.min = 0.001f;
            stiction_factor.hint = "Defines the stiction factor (static friction) as x-times the sliding factor";
            stiction_factor.comment = "";
            stiction_factor.unit = "-";
            stiction_factor.save_under_player_prefs = true;
        }
    }

    [Serializable]
    public class stru_simulation
    {
        public stru_physics physics { get; set; }
        public stru_audio audio { get; set; }
        public stru_camera camera { get; set; }
        public stru_gameplay gameplay { get; set; }
        public stru_graphic_settings graphic_settings { get; set; }
        public stru_graphic_quality graphic_quality { get; set; }
        public stru_graphic_quality_VR graphic_quality_VR { get; set; }
        public stru_various various { get; set; }

        public stru_simulation()
        {
            physics = new stru_physics();
            audio = new stru_audio();
            camera = new stru_camera();
            gameplay = new stru_gameplay();
            graphic_settings = new stru_graphic_settings();
            graphic_quality = new stru_graphic_quality();
            graphic_quality_VR = new stru_graphic_quality_VR(); // VR mode
            various = new stru_various();
        }

        public void get_stru_simulation_settings_from_player_prefs() 
        {
            // ##################################################################################
            // Get PlayerPrefs for "simulation"-structure
            // ##################################################################################
            this.physics.delta_t.val = PlayerPrefs.GetFloat("__simulation_" + "delta_t", this.physics.delta_t.val);
            this.physics.timescale.val = PlayerPrefs.GetFloat("__simulation_" + "timescale", this.physics.timescale.val);

            this.audio.master_sound_volume.val = PlayerPrefs.GetFloat("__simulation_" + "master_sound_volume", this.audio.master_sound_volume.val);
            this.audio.commentator_audio_source_volume.val = PlayerPrefs.GetFloat("__simulation_" + "commentator_audio_source_volume", this.audio.commentator_audio_source_volume.val);
            this.audio.crash_audio_source_volume.val = PlayerPrefs.GetFloat("__simulation_" + "crash_audio_source_volume", this.audio.crash_audio_source_volume.val);
            
            this.camera.stiffness.val = PlayerPrefs.GetFloat("__simulation_" + "stiffness", this.camera.stiffness.val);
            this.camera.auto_zoom.val = (PlayerPrefs.GetInt("__simulation_" + "auto_zoom", this.camera.auto_zoom.val == false ? 0 : 1)) == 0 ? false : true;
            this.camera.fov.val = PlayerPrefs.GetFloat("__simulation_" + "fov", this.camera.fov.val);
            this.camera.shaking.val = PlayerPrefs.GetFloat("__simulation_" + "shaking", this.camera.shaking.val);
            this.camera.xr_zoom_factor.val = PlayerPrefs.GetFloat("__simulation_" + "xr_zoom_factor", this.camera.xr_zoom_factor.val);
            this.camera.keep_ground_visible.val = PlayerPrefs.GetFloat("__simulation_" + "keep_ground_visible", this.camera.keep_ground_visible.val);

            this.gameplay.show_pilot.val = (PlayerPrefs.GetInt("__simulation_" + "show_pilot", this.gameplay.show_pilot.val == false ? 0 : 1)) == 0 ? false : true;
            this.gameplay.show_fps.val = (PlayerPrefs.GetInt("__simulation_" + "show_fps", this.gameplay.show_fps.val == false ? 0 : 1)) == 0 ? false : true;
            this.gameplay.delay_after_reset.val = PlayerPrefs.GetFloat("__simulation_" + "delay_after_reset", this.gameplay.delay_after_reset.val);
            this.gameplay.wheel_brake_on_after_heli_change.val = (PlayerPrefs.GetInt("__simulation_" + "wheel_brake_on_after_heli_change", this.gameplay.wheel_brake_on_after_heli_change.val == false ? 0 : 1)) == 0 ? false : true;
            this.gameplay.limit_mouse_to_game.val = (PlayerPrefs.GetInt("__simulation_" + "limit_mouse_to_game", this.gameplay.limit_mouse_to_game.val == false ? 0 : 1)) == 0 ? false : true;

            this.graphic_settings.rotor_disk_complexity.val = (PlayerPrefs.GetInt("__simulation_" + "rotor_disk_complexity", this.graphic_settings.rotor_disk_complexity.val == false ? 0 : 1)) == 0 ? false : true;
            this.graphic_settings.rotor_disk_transparency.val = PlayerPrefs.GetFloat("__simulation_" + "rotor_disk_transparency", this.graphic_settings.rotor_disk_transparency.val);
            //this.graphic_settings.rotor_blade_transparency.val = PlayerPrefs.GetFloat("__simulation_" + "rotor_blade_transparency", this.graphic_settings.rotor_blade_transparency.val);

            this.graphic_quality.motion_blur.val = (PlayerPrefs.GetInt("__simulation_" + "motion_blur", this.graphic_quality.motion_blur.val == false ? 0 : 1)) == 0 ? false : true;
            this.graphic_quality.bloom.val = (PlayerPrefs.GetInt("__simulation_" + "bloom", this.graphic_quality.bloom.val == false ? 0 : 1)) == 0 ? false : true;
            this.graphic_quality.depthoffield.val = (PlayerPrefs.GetInt("__simulation_" + "depthoffield", this.graphic_quality.depthoffield.val == false ? 0 : 1)) == 0 ? false : true;
            this.graphic_quality.quality_setting.val = (PlayerPrefs.GetInt("__simulation_" + "quality_setting", this.graphic_quality.quality_setting.val));
            this.graphic_quality.resolution_setting.val = (PlayerPrefs.GetInt("__simulation_" + "resolution_setting", this.graphic_quality.resolution_setting.val));

            this.graphic_quality_VR.motion_blur_VR.val = (PlayerPrefs.GetInt("__simulation_" + "motion_blur_VR", this.graphic_quality_VR.motion_blur_VR.val == false ? 0 : 1)) == 0 ? false : true;
            this.graphic_quality_VR.bloom_VR.val = (PlayerPrefs.GetInt("__simulation_" + "bloom_VR", this.graphic_quality_VR.bloom_VR.val == false ? 0 : 1)) == 0 ? false : true;
            this.graphic_quality_VR.depthoffield_VR.val = (PlayerPrefs.GetInt("__simulation_" + "depthoffield_VR", this.graphic_quality_VR.depthoffield_VR.val == false ? 0 : 1)) == 0 ? false : true;
            this.graphic_quality_VR.quality_setting_VR.val = (PlayerPrefs.GetInt("__simulation_" + "quality_setting_VR", this.graphic_quality_VR.quality_setting_VR.val));
            this.graphic_quality_VR.resolution_setting_VR.val = (PlayerPrefs.GetInt("__simulation_" + "resolution_setting_VR", this.graphic_quality_VR.resolution_setting_VR.val));


            this.various.sceneries_file_location.val = (PlayerPrefs.GetInt("__simulation_" + "sceneries_file_location", this.various.sceneries_file_location.val));
            this.various.V_h.val = (PlayerPrefs.GetFloat("__simulation_" + "V_h", this.various.V_h.val));
            this.various.U_max.val = (PlayerPrefs.GetFloat("__simulation_" + "U_max", this.various.U_max.val));
            this.various.stiction_factor.val = (PlayerPrefs.GetFloat("__simulation_" + "stiction_factor", this.various.stiction_factor.val));

        // ##################################################################################
    }

    }
    // ##################################################################################




    // ##################################################################################
    // scenery parameter
    // ##################################################################################
    [Serializable]
    public class stru_scenery
    {
        public stru_float gravity { get; set; } // [m/sec^2] acceleration of gravity
        public stru_float camera_height { get; set; } // [m] 
        public stru_float ambient_sound_volume { get; set; } // [0...1] 
        public stru_initial_position initial_position { get; set; } // [m m m deg deg deg] 
        public stru_weather weather { get; set; } // 
        public stru_animals animals { get; set; } //  
        public stru_lighting lighting { get; set; } // 
        public stru_skybox skybox { get; set; } // 
        public stru_ground ground { get; set; } // 
        public stru_xr xr { get; set; } // 

        public stru_scenery()
        {
            gravity = new stru_float();
            camera_height = new stru_float();
            ambient_sound_volume = new stru_float();
            initial_position = new stru_initial_position();
            weather = new stru_weather();
            animals = new stru_animals();
            lighting = new stru_lighting();
            skybox = new stru_skybox();
            ground = new stru_ground();
            xr = new stru_xr(); 

            gravity.val = 9.81f;
            gravity.max = 0.00f;
            gravity.min = 20.0f;
            gravity.hint = "Gravity";
            gravity.comment = "";
            gravity.unit = "m/s^2";

            camera_height.val = 1.4f; // [m]
            camera_height.max = 0.00f;
            camera_height.min = 3.0f;
            camera_height.hint = "Camera height";
            camera_height.comment = "";
            camera_height.unit = "m";

            ambient_sound_volume.val = 50.00f;
            ambient_sound_volume.min = 0.000f;
            ambient_sound_volume.max = 100.0f;
            ambient_sound_volume.hint = "Ambient sound volume";
            ambient_sound_volume.comment = "";
            ambient_sound_volume.unit = "%";
        }
    }
    // ##################################################################################



    // ##################################################################################
    // weather parameter
    // ##################################################################################
    [Serializable]
    public class stru_weather
    {
        public stru_float rho_air { get; set; } // [kg/m^3] local air density
        public stru_float temperature { get; set; } // [°C] air temperature
        public stru_float wind_direction { get; set; } // [-] wind direction
        public stru_float wind_speed { get; set; } // [m/s] wind strength
        public stru_float wind_speed_vertical { get; set; } // [m/s] wind strength in vertical direction
        //public stru_float wind_turbulence { get; set; } // [m/s] wind turbulence strength

        public stru_weather()
        {
            rho_air = new stru_float();
            temperature = new stru_float();
            wind_direction = new stru_float();
            wind_speed = new stru_float();
            wind_speed_vertical = new stru_float();

            rho_air.val = 1.27f;
            rho_air.max = 2.00f;
            rho_air.min = 0.50f;
            rho_air.hint = "Air density";
            rho_air.comment = "";
            rho_air.unit = "kg/m^3";

            temperature.val = 20.0f;
            temperature.max = 0.00f;
            temperature.min = 40.0f;
            temperature.hint = "Air temperature";
            temperature.comment = "";
            temperature.unit = "°C";

            wind_direction.val = 0f;
            wind_direction.max = 0f;
            wind_direction.min = 360f;
            wind_direction.hint = "Wind direction";
            wind_direction.comment = "";
            wind_direction.unit = "deg";

            wind_speed.val = 0f;
            wind_speed.max = 0f;
            wind_speed.min = 100f;
            wind_speed.hint = "Wind speed in horizontal direction";
            wind_speed.comment = "";
            wind_speed.unit = "m/sec";

            wind_speed_vertical.val = 0f;
            wind_speed_vertical.max = 0f;
            wind_speed_vertical.min = 100f;
            wind_speed_vertical.hint = "Wind speed in vertical direction";
            wind_speed_vertical.comment = "";
            wind_speed_vertical.unit = "m/sec";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // animals parameter
    // ##################################################################################
    [Serializable]
    public class stru_animals
    {
        public stru_int number_of_bird_flocks { get; set; } // [-] 
        public stru_int number_of_insect_flocks { get; set; } // [-] 
        public stru_bird_flock bird_flock { get; set; }          
        public stru_insect_flock insect_flock { get; set; }

        public stru_animals()
        {
            number_of_bird_flocks = new stru_int();
            number_of_insect_flocks = new stru_int();
            bird_flock = new stru_bird_flock();
            insect_flock = new stru_insect_flock();

            number_of_bird_flocks.val = 1;
            number_of_bird_flocks.max = 0;
            number_of_bird_flocks.min = 5;
            number_of_bird_flocks.hint = "Number of bird flocks";
            number_of_bird_flocks.comment = "";
            number_of_bird_flocks.unit = "-";

            number_of_insect_flocks.val = 1;
            number_of_insect_flocks.max = 0;
            number_of_insect_flocks.min = 5;
            number_of_insect_flocks.hint = "Number of insects flocks";
            number_of_insect_flocks.comment = "";
            number_of_insect_flocks.unit = "-";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // animal flock parameter
    // ##################################################################################
    [Serializable]
    public class stru_flock
    {
        public stru_int number_of_animals { get; set; } // [-] 
        public stru_float area_size { get; set; } // [m] 
        public stru_float area_height_min { get; set; } // [m]
        public stru_float area_height_max { get; set; } // [m]
        public stru_float animal_scale_variation { get; set; } // [%]
        public stru_float animal_animation_speed { get; set; } // [%]
        public stru_float animal_speed_min { get; set; } // [m/sec]
        public stru_float animal_speed_max { get; set; } // [m/sec]
        public stru_float animal_rotation_speed { get; set; } // [?]
        public stru_float animal_neighbour_distance { get; set; } // [m]
        public stru_float target_update_value { get; set; } // [-]
        public stru_float apply_rules_value { get; set; } // [-]
        public stru_float avoid_animal_distance { get; set; } // [m]
        public stru_float avoid_helicopter_distance { get; set; } // [m]       
        public stru_bool show_flock_target { get; set; } // [-]

        public stru_flock()
        {
            number_of_animals = new stru_int();
            area_size = new stru_float();
            area_height_min = new stru_float();
            area_height_max = new stru_float();
            animal_scale_variation = new stru_float();
            animal_animation_speed = new stru_float();
            animal_speed_min = new stru_float();
            animal_speed_max = new stru_float();
            animal_rotation_speed = new stru_float();
            animal_neighbour_distance = new stru_float();
            target_update_value = new stru_float();
            apply_rules_value = new stru_float();
            avoid_animal_distance = new stru_float();
            avoid_helicopter_distance = new stru_float();  
            show_flock_target = new stru_bool();
        }
    }


    // ##################################################################################
    // animal flock parameter
    // ##################################################################################
    [Serializable]
    public class stru_bird_flock : stru_flock
    {
        public stru_bird_flock()
        {
            number_of_animals.val = 10;
            number_of_animals.min = 0;
            number_of_animals.max = 50;
            number_of_animals.hint = "Number of birds";
            number_of_animals.comment = "";
            number_of_animals.unit = "-";

            area_size.val = 300;
            area_size.min = 50;
            area_size.max = 1000;
            area_size.hint = "Squared area length/2 for birds to fly in";
            area_size.comment = "";
            area_size.unit = "m";

            area_height_min.val = 25;
            area_height_min.min = 0;
            area_height_min.max = 500;
            area_height_min.hint = "Minimum flight height of birds";
            area_height_min.comment = "";
            area_height_min.unit = "m";

            area_height_max.val = 55;
            area_height_max.min = 0;
            area_height_max.max = 500;
            area_height_max.hint = "Maximum flight height of birds";
            area_height_max.comment = "";
            area_height_max.unit = "m";

            animal_scale_variation.val = 25f;
            animal_scale_variation.min = 0;
            animal_scale_variation.max = 500;
            animal_scale_variation.hint = "Variation in size of birds";
            animal_scale_variation.comment = "";
            animal_scale_variation.unit = "%";

            animal_animation_speed.val = 200f;
            animal_animation_speed.min = 0;
            animal_animation_speed.max = 10000;
            animal_animation_speed.hint = "Animation speed factor";
            animal_animation_speed.comment = "";
            animal_animation_speed.unit = "%";

            animal_speed_min.val = 3;
            animal_speed_min.min = 0;
            animal_speed_min.max = 1;
            animal_speed_min.hint = "Minimum flight speed of birds";
            animal_speed_min.comment = "";
            animal_speed_min.unit = "m/sec";

            animal_speed_max.val = 20;
            animal_speed_max.min = 0;
            animal_speed_max.max = 40;
            animal_speed_max.hint = "Maximum flight speed of birds";
            animal_speed_max.comment = "";
            animal_speed_max.unit = "m/sec";

            animal_rotation_speed.val = 0.5f;
            animal_rotation_speed.min = 0.1f;
            animal_rotation_speed.max = 10;
            animal_rotation_speed.hint = "Maximum turning speed of birds";
            animal_rotation_speed.comment = "";
            animal_rotation_speed.unit = "rad/sec?";

            animal_neighbour_distance.val = 75;
            animal_neighbour_distance.min = 0.1f;
            animal_neighbour_distance.max = 500;
            animal_neighbour_distance.hint = "Distance between two birds below where they start to build flocks";
            animal_neighbour_distance.comment = "";
            animal_neighbour_distance.unit = "m";

            target_update_value.val = 20;
            target_update_value.min = 0.1f;
            target_update_value.max = 50;
            target_update_value.hint = "How often the target is updated";
            target_update_value.comment = "";
            target_update_value.unit = "-";

            apply_rules_value.val = 7;
            apply_rules_value.min = 0.1f;
            apply_rules_value.max = 20;
            apply_rules_value.hint = "How often the flocking algorithm should be updated for each bird";
            apply_rules_value.comment = "";
            apply_rules_value.unit = "-";

            avoid_animal_distance.val = 25;
            avoid_animal_distance.min = 0.1f;
            avoid_animal_distance.max = 20;
            avoid_animal_distance.hint = "Distance under where birds try to avoid collision each other";
            avoid_animal_distance.comment = "";
            avoid_animal_distance.unit = "m";

            avoid_helicopter_distance.val = 30;
            avoid_helicopter_distance.min = 10;
            avoid_helicopter_distance.max = 200;
            avoid_helicopter_distance.hint = "Distance under where birds try to avoid collision with the helicopter";
            avoid_helicopter_distance.comment = "";
            avoid_helicopter_distance.unit = "m";

            show_flock_target.val = false;
            show_flock_target.hint = "Show flock target as white sphere";
            show_flock_target.comment = "";
            show_flock_target.unit = "-";
    
        }
    }
    // ##################################################################################



    // ##################################################################################
    // animal flock parameter
    // ##################################################################################
    [Serializable]
    public class stru_insect_flock : stru_flock
    {
        public stru_insect_flock()
        {
            number_of_animals.val = 40;
            number_of_animals.min = 0;
            number_of_animals.max = 200;
            number_of_animals.hint = "Number of insects";
            number_of_animals.comment = "";
            number_of_animals.unit = "-";

            area_size.val = 2.5f;
            area_size.min = 1;
            area_size.max = 1000;
            area_size.hint = "Squared area length/2 for insects to fly in";
            area_size.comment = "";
            area_size.unit = "m";

            area_height_min.val = 1.25f;
            area_height_min.min = 0;
            area_height_min.max = 2;
            area_height_min.hint = "Minimum flight height of insects";
            area_height_min.comment = "";
            area_height_min.unit = "m";

            area_height_max.val = 2.5f;
            area_height_max.min = 0;
            area_height_max.max = 10;
            area_height_max.hint = "Maximum flight height of insects";
            area_height_max.comment = "";
            area_height_max.unit = "m";

            animal_scale_variation.val = 25;
            animal_scale_variation.min = 0;
            animal_scale_variation.max = 500;
            animal_scale_variation.hint = "Variation in size of insects";
            animal_scale_variation.comment = "Variation in size of insects";
            animal_scale_variation.unit = "%";

            animal_animation_speed.val = 1000;
            animal_animation_speed.min = 0;
            animal_animation_speed.max = 10000;
            animal_animation_speed.hint = "Animation speed factor";
            animal_animation_speed.comment = "";
            animal_animation_speed.unit = "%";

            animal_speed_min.val = 0.3f;
            animal_speed_min.min = 0;
            animal_speed_min.max = 1;
            animal_speed_min.hint = "Minimum flight speed of insects";
            animal_speed_min.comment = "";
            animal_speed_min.unit = "m/sec";

            animal_speed_max.val = 0.6f;
            animal_speed_max.min = 0;
            animal_speed_max.max = 4;
            animal_speed_max.hint = "Maximum flight speed of insects";
            animal_speed_max.comment = "";
            animal_speed_max.unit = "m/sec";

            animal_rotation_speed.val = 1.0f;
            animal_rotation_speed.min = 0.1f;
            animal_rotation_speed.max = 5;
            animal_rotation_speed.hint = "Maximum turning speed of insects";
            animal_rotation_speed.comment = "";
            animal_rotation_speed.unit = "rad/sec?";

            animal_neighbour_distance.val = 0.6f;
            animal_neighbour_distance.min = 0.1f;
            animal_neighbour_distance.max = 5;
            animal_neighbour_distance.hint = "Distance between two insects below where they start to build flocks";
            animal_neighbour_distance.comment = "";
            animal_neighbour_distance.unit = "m";

            target_update_value.val = 20;
            target_update_value.min = 0.1f;
            target_update_value.max = 50;
            target_update_value.hint = "How often the target is updated";
            target_update_value.comment = "";
            target_update_value.unit = "-";

            apply_rules_value.val = 10;
            apply_rules_value.min = 0.1f;
            apply_rules_value.max = 20;
            apply_rules_value.hint = "How often the flocking algorithm should be updated for each insect";
            apply_rules_value.comment = "";
            apply_rules_value.unit = "-";

            avoid_animal_distance.val = 0.4f;
            avoid_animal_distance.min = 0.1f;
            avoid_animal_distance.max = 10;
            avoid_animal_distance.hint = "Distance under where insects try to avoid collision each other";
            avoid_animal_distance.comment = "";
            avoid_animal_distance.unit = "m";

            avoid_helicopter_distance.val = 4;
            avoid_helicopter_distance.min = 1;
            avoid_helicopter_distance.max = 200;
            avoid_helicopter_distance.hint = "Distance under where insects try to avoid collision with the helicopter";
            avoid_helicopter_distance.comment = "";
            avoid_helicopter_distance.unit = "m";

            show_flock_target.val = false;
            show_flock_target.hint = "Show flock target as white sphere";
            show_flock_target.comment = "";
            show_flock_target.unit = "-";
        }
    }
    // ##################################################################################





    // ##################################################################################
    // environment lighting parameter
    // ##################################################################################
    [Serializable]
    public class stru_lighting
    {
        public stru_Vector3 sun_position { get; set; } // [m] 
        public stru_float sun_intensity { get; set; } // [-] 
        public stru_float sun_shadow_strength { get; set; } // [-] 
        public stru_float sun_bloom_intensity { get; set; } // [-] 
        public stru_float sun_bloom_blinded_by_sun_intensity { get; set; } // [] 
        public stru_Vector3 ambient_light_color { get; set; } // [0...1, 0...1, 0...1] 
        public stru_float ambient_light_intensity { get; set; }// [0...1] 

        public stru_lighting()
        {
            sun_position = new stru_Vector3();
            sun_intensity = new stru_float();
            sun_shadow_strength = new stru_float();
            sun_bloom_intensity = new stru_float();
            sun_bloom_blinded_by_sun_intensity = new stru_float();
            ambient_light_color = new stru_Vector3();
            ambient_light_intensity = new stru_float();


            sun_position.vect3 = new Vector3 { x = 10.55f, y = 9.64f, z = 0.48f }; // [m]
            sun_position.hint = "Sun position";
            sun_position.comment = "";
            sun_position.unit = "m";

            sun_intensity.val = 3.0f;
            sun_intensity.max = 0.00f;
            sun_intensity.min = 10.0f;
            sun_intensity.hint = "Sun intensity";
            sun_intensity.comment = "";
            sun_intensity.unit = "-";

            sun_shadow_strength.val = 0.4f;
            sun_shadow_strength.max = 0f;
            sun_shadow_strength.min = 1f;
            sun_shadow_strength.hint = "Shadow strength";
            sun_shadow_strength.comment = "";
            sun_shadow_strength.unit = "0...1";

            sun_bloom_intensity.val = 1.2f;
            sun_bloom_intensity.max = 0f;
            sun_bloom_intensity.min = 10f;
            sun_bloom_intensity.hint = "Sun bloom effect intensity";
            sun_bloom_intensity.comment = "";
            sun_bloom_intensity.unit = " ";

            sun_bloom_blinded_by_sun_intensity.val = 18.0f;
            sun_bloom_blinded_by_sun_intensity.max = 0f;
            sun_bloom_blinded_by_sun_intensity.min = 30f;
            sun_bloom_blinded_by_sun_intensity.hint = "Sun bloom effect intensity while blinded by sun";
            sun_bloom_blinded_by_sun_intensity.comment = "";
            sun_bloom_blinded_by_sun_intensity.unit = " ";

            ambient_light_color.vect3 = new Vector3 { x = 191.0f / 255.0f, y = 191.0f / 255.0f, z = 191.0f / 255.0f }; // []
            ambient_light_color.hint = "Ambient light color";
            ambient_light_color.comment = "";
            ambient_light_color.unit = "rgb";

            ambient_light_intensity.val = 1.0f; // []
            ambient_light_intensity.max = 0.00f;
            ambient_light_intensity.min = 10.0f;
            ambient_light_intensity.hint = "Ambient light intensity";
            ambient_light_intensity.comment = "";
            ambient_light_intensity.unit = "-";

        }
    }
    // ##################################################################################



    // ##################################################################################
    // skybox parameter
    // ##################################################################################
    [Serializable]
    public class stru_skybox
    {
        public stru_Vector3 skybox_tint_color { get; set; } // [0...1, 0...1, 0...1] 
        public stru_float skybox_exposure { get; set; } // [] 
        public stru_bool skybox_flipping_horizontally { get; set; } // [bool] 
        public stru_float skybox_rotation { get; set; } // [deg] 
      
        public stru_skybox()
        {
            skybox_tint_color = new stru_Vector3();
            skybox_exposure = new stru_float();
            skybox_flipping_horizontally = new stru_bool();
            skybox_rotation = new stru_float();


            skybox_tint_color.vect3 = new Vector3 { x = 1, y = 1, z = 1 };
            skybox_tint_color.hint = "Tint color";
            skybox_tint_color.comment = "";
            skybox_tint_color.unit = "rgb";

            skybox_exposure.val = 0.5f;
            skybox_exposure.max = 0.00f;
            skybox_exposure.min = 2.0f;
            skybox_exposure.hint = "Exposure";
            skybox_exposure.comment = "";
            skybox_exposure.unit = "-";

            skybox_flipping_horizontally.val = true;
            skybox_flipping_horizontally.hint = "Flipping horizontally";
            skybox_flipping_horizontally.comment = "";
            skybox_flipping_horizontally.unit = "-";

            skybox_rotation.val = 0;
            skybox_rotation.max = 0.00f;
            skybox_rotation.min = 360f;
            skybox_rotation.hint = "Rotate horizontally";
            skybox_rotation.comment = "";
            skybox_rotation.unit = "-";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // ground parameter
    // ##################################################################################
    [Serializable]
    public class stru_ground
    {
        public stru_float bump_height { get; set; } // [m] 
        public stru_float bump_density_scale { get; set; } // [1/m] 

        public stru_ground()
        {
            bump_height = new stru_float();
            bump_density_scale = new stru_float();

            bump_height.val = 0.007f;
            bump_height.max = 0.000f;
            bump_height.min = 0.100f;
            bump_height.hint = "Bump height of ground.";
            bump_height.comment = "";
            bump_height.unit = "m";

            bump_density_scale.val = 7.00f;
            bump_density_scale.max = 0.00f;
            bump_density_scale.min = 0.10f;
            bump_density_scale.hint = "Bump density factor of ground.";
            bump_density_scale.comment = "";
            bump_density_scale.unit = "1/m";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // virtual reality parameter
    // ##################################################################################
    [Serializable]
    public class stru_xr
    {
        public stru_Vector3 canvas_position { get; set; } // [m m m] 
        public stru_Vector3 canvas_rotation { get; set; } // [deg deg deg] 

        public stru_xr()
        {
            canvas_position = new stru_Vector3();
            canvas_rotation = new stru_Vector3();

            canvas_position.vect3 = new Vector3 { x = 0.0f, y = 3.2f, z = 7.0f }; 
            canvas_position.hint = "Canvas position";
            canvas_position.comment = "";
            canvas_position.unit = "m";

            canvas_rotation.vect3 = new Vector3 { x = 0, y = 0, z = 0 };
            canvas_rotation.hint = "Canvas rotation";
            canvas_rotation.comment = "";
            canvas_rotation.unit = "deg";
        }
    }
    // ##################################################################################



    // ##################################################################################
    // fuselage parameter
    // ##################################################################################
    [Serializable]
    public class stru_fuselage
    {
        public stru_Vector3 S_fxyz { get; set; } // [m^2] effective drag area along the body-frame XYZ (front,vertical,side)
        public stru_float downwash_factor_mainrotor { get; set; } // [0..1] Wing is hit by mainrotor downwash
        public stru_float downwash_factor_tailrotor { get; set; } // [0..1] Wing is hit by tailrotor downwash  (tandem heli)


        public stru_fuselage()
        {
            S_fxyz = new stru_Vector3();
            downwash_factor_mainrotor = new stru_float();
            downwash_factor_tailrotor = new stru_float();

            S_fxyz.vect3 = new Vector3 { x = 0.02f, y = 0.08f, z = 0.09f };
            S_fxyz.hint = "Effective cross section area";
            S_fxyz.comment = "";
            S_fxyz.unit = "m^2";

            downwash_factor_mainrotor.val = 0.5f;
            downwash_factor_mainrotor.min = 0.0f;
            downwash_factor_mainrotor.max = 1.0f;
            downwash_factor_mainrotor.hint = "Wing is hit by mainrotor downwash";
            downwash_factor_mainrotor.comment = "";
            downwash_factor_mainrotor.unit = "[0...1]";

            downwash_factor_tailrotor.val = 0.0f;
            downwash_factor_tailrotor.min = 0.0f;
            downwash_factor_tailrotor.max = 1.0f;
            downwash_factor_tailrotor.hint = "Wing is hit by tailrotor downwash (if tandem rotor heli)"; // todo
            downwash_factor_tailrotor.comment = "";
            downwash_factor_tailrotor.unit = "[0...1]";

        }
    }
    // ##################################################################################




    // ##################################################################################
    // fin parameter
    // ##################################################################################
    [Serializable]
    public class stru_wing
    {
        public stru_bool wing_exists { get; set; } // [-] 
        public stru_bool horizontal0_or_vertical1 { get; set; } // [-] 

        public stru_float area { get; set; } // [m^2] Wing effective drag area along the body-frame y (vertical)
        public stru_float drag_coeff { get; set; } // [-] Wing air friction ceofficient 
        public stru_float C_l_alpha { get; set; } // [rad^-1] Wing lift curve slope 
        public stru_float alpha_stall { get; set; } // [rad^-1] Wing lift curve critical slope at stall
        public stru_float downwash_factor_mainrotor { get; set; } // [0..1] Wing is hit by mainrotor downwash
        public stru_float downwash_factor_tailrotor { get; set; } // [0..1] Wing is hit by tailrotor downwash
        public stru_float downwash_factor_propeller { get; set; } // [0..1] Wing is hit by propeller downwash
        public stru_Vector3 posLH { get; set; } // [m] Wing position relative to center of gravity in local coordinate system
        //public stru_Vector3 dirL { get; set; } // [-] Wing direction in local coordinate system


        public stru_wing()
        {
            wing_exists = new stru_bool();
            horizontal0_or_vertical1 = new stru_bool();
            area = new stru_float();
            drag_coeff = new stru_float();
            C_l_alpha = new stru_float();
            alpha_stall = new stru_float();
            downwash_factor_mainrotor = new stru_float();
            downwash_factor_tailrotor = new stru_float();
            downwash_factor_propeller = new stru_float();
            posLH = new stru_Vector3();


 
            wing_exists.val = false;
            wing_exists.hint = "Wing exists.";
            wing_exists.comment = "";
            wing_exists.unit = "-";

            horizontal0_or_vertical1.val = false;
            horizontal0_or_vertical1.hint = "Wing orientation.";
            horizontal0_or_vertical1.comment = "";
            horizontal0_or_vertical1.unit = "-";

            area.val = 0.10f * 0.05f; // [m^2]
            area.min = 0.000000f;
            area.max = 1.000000f;
            area.hint = "Effective cross section area (of a planar surface)";
            area.comment = "";
            area.unit = "m^2";

            drag_coeff.val = 1.00f;
            drag_coeff.min = 0.02f;
            drag_coeff.max = 1.20f;
            drag_coeff.hint = "Drag coefficient (of a planar surface)";
            drag_coeff.comment = "";
            drag_coeff.unit = "-";

            C_l_alpha.val = 5.50f; // TODO find parameter
            C_l_alpha.min = 0.10f;
            C_l_alpha.max = 10.0f;
            C_l_alpha.hint = "Wing lift curve slope";
            C_l_alpha.comment = "";
            C_l_alpha.unit = "1/rad";

            alpha_stall.val = 20;  // TODO find parameter
            alpha_stall.min = 5.0f;
            alpha_stall.max = 45.0f;
            alpha_stall.hint = "Critical angle of attack in stall";
            alpha_stall.comment = "";
            alpha_stall.unit = "deg";

            downwash_factor_mainrotor.val = 0.2f;
            downwash_factor_mainrotor.min = 0.0f;
            downwash_factor_mainrotor.max = 1.0f;
            downwash_factor_mainrotor.hint = "Wing is hit by mainrotor downwash";
            downwash_factor_mainrotor.comment = "";
            downwash_factor_mainrotor.unit = "[0...1]";

            downwash_factor_tailrotor.val = 0.0f;
            downwash_factor_tailrotor.min = 0.0f;
            downwash_factor_tailrotor.max = 1.0f;
            downwash_factor_tailrotor.hint = "Wing is hit by tailrotor downwash";
            downwash_factor_tailrotor.comment = "";
            downwash_factor_tailrotor.unit = "[0...1]";

            downwash_factor_propeller.val = 0.0f;
            downwash_factor_propeller.min = 0.0f;
            downwash_factor_propeller.max = 1.0f;
            downwash_factor_propeller.hint = "Wing is hit by propeller downwash";
            downwash_factor_propeller.comment = "";
            downwash_factor_propeller.unit = "[0...1]";

            posLH.vect3 = new Vector3 { x = -0.660f, y = -0.01f, z = 0.000f };
            posLH.hint = "Wing position relative to center of gravity in local coordinate system";
            posLH.comment = "";
            posLH.unit = "m";
        }
    }




    // ##################################################################################
    // horizontal tail fin parameter
    // ##################################################################################
    [Serializable]
    public class stru_horizontal_fin : stru_wing
    {
        public stru_horizontal_fin()
        {
            wing_exists.val = true;
            wing_exists.hint = "Wing exists.";
            wing_exists.comment = "";
            wing_exists.unit = "-";

            horizontal0_or_vertical1.val = false;
            horizontal0_or_vertical1.hint = "Wing orientation.";
            horizontal0_or_vertical1.comment = "";
            horizontal0_or_vertical1.unit = "-";

            area.val = 1.00f * 0.05f; // [m^2]
            area.min = 0.000000f;
            area.max = 1.000000f;
            area.hint = "Effective cross section area (of a planar surface)";
            area.comment = "";
            area.unit = "m^2";

            drag_coeff.val = 1.00f;
            drag_coeff.min = 0.02f;
            drag_coeff.max = 1.20f;
            drag_coeff.hint = "Drag coefficient (of a planar surface)";
            drag_coeff.comment = "";
            drag_coeff.unit = "-";

            C_l_alpha.val = 5.50f; // TODO find parameter
            C_l_alpha.min = 0.10f;
            C_l_alpha.max = 10.0f;
            C_l_alpha.hint = "Wing lift curve slope";
            C_l_alpha.comment = "";
            C_l_alpha.unit = "1/rad";

            alpha_stall.val = 20;  // TODO find parameter
            alpha_stall.min = 5.0f;
            alpha_stall.max = 45.0f;
            alpha_stall.hint = "Critical angle of attack in stall";
            alpha_stall.comment = "";
            alpha_stall.unit = "deg";

            downwash_factor_mainrotor.val = 0.2f;
            downwash_factor_mainrotor.min = 0.0f;
            downwash_factor_mainrotor.max = 1.0f;
            downwash_factor_mainrotor.hint = "Wing is hit by mainrotor downwash";
            downwash_factor_mainrotor.comment = "";
            downwash_factor_mainrotor.unit = "[0...1]";

            downwash_factor_tailrotor.val = 0.0f;
            downwash_factor_tailrotor.min = 0.0f;
            downwash_factor_tailrotor.max = 1.0f;
            downwash_factor_tailrotor.hint = "Wing is hit by tailrotor downwash";
            downwash_factor_tailrotor.comment = "";
            downwash_factor_tailrotor.unit = "[0...1]";

            downwash_factor_propeller.val = 0.0f;
            downwash_factor_propeller.min = 0.0f;
            downwash_factor_propeller.max = 1.0f;
            downwash_factor_propeller.hint = "Wing is hit by propeller downwash";
            downwash_factor_propeller.comment = "";
            downwash_factor_propeller.unit = "[0...1]";

            posLH.vect3 = new Vector3 { x = -0.660f, y = -0.01f, z = 0.000f };
            posLH.hint = "Wing position relative to center of gravity in local coordinate system";
            posLH.comment = "";
            posLH.unit = "m";

        }
    }


    // ##################################################################################
    // vertical tail fin parameter
    // ##################################################################################
    [Serializable]
    public class stru_vertical_fin : stru_wing
    {
        public stru_vertical_fin()
        {
            wing_exists.val = true;
            wing_exists.hint = "Wing exists.";
            wing_exists.comment = "";
            wing_exists.unit = "-";

            horizontal0_or_vertical1.val = true;
            horizontal0_or_vertical1.hint = "Wing orientation.";
            horizontal0_or_vertical1.comment = "";
            horizontal0_or_vertical1.unit = "-";

            area.val = 0.15f * 0.05f; // [m^2]
            area.min = 0.000000f;
            area.max = 1.000000f;
            area.hint = "Effective cross section area (of a planar surface)";
            area.comment = "";
            area.unit = "m^2";

            drag_coeff.val = 1.00f;
            drag_coeff.min = 0.02f;
            drag_coeff.max = 1.20f;
            drag_coeff.hint = "Drag coefficient (of a planar surface)";
            drag_coeff.comment = "";
            drag_coeff.unit = "-";

            C_l_alpha.val = 6.00f; // TODO find parameter
            C_l_alpha.min = 0.10f;
            C_l_alpha.max = 10.0f;
            C_l_alpha.hint = "Fin lift curve slope";
            C_l_alpha.comment = "";
            C_l_alpha.unit = "1/rad";

            alpha_stall.val = 20; // TODO find parameter
            alpha_stall.min = 5.0f;
            alpha_stall.max = 45.0f;
            alpha_stall.hint = "Critical angle of attack in stall";
            alpha_stall.comment = "";
            alpha_stall.unit = "deg";

            downwash_factor_mainrotor.val = 0.0f;
            downwash_factor_mainrotor.min = 0.0f;
            downwash_factor_mainrotor.max = 1.0f;
            downwash_factor_mainrotor.hint = "Wing is hit by mainrotor downwash";
            downwash_factor_mainrotor.comment = "";
            downwash_factor_mainrotor.unit = "[0...1]";

            downwash_factor_tailrotor.val = 0.5f;
            downwash_factor_tailrotor.min = 0.0f;
            downwash_factor_tailrotor.max = 1.0f;
            downwash_factor_tailrotor.hint = "Wing is hit by tailrotor downwash";
            downwash_factor_tailrotor.comment = "";
            downwash_factor_tailrotor.unit = "[0...1]";

            downwash_factor_propeller.val = 0.0f;
            downwash_factor_propeller.min = 0.0f;
            downwash_factor_propeller.max = 1.0f;
            downwash_factor_propeller.hint = "Wing is hit by propeller downwash";
            downwash_factor_propeller.comment = "";
            downwash_factor_propeller.unit = "[0...1]";

            posLH.vect3 = new Vector3 { x = -0.900f, y = -0.01f, z = -0.03f };
            posLH.hint = "Fin position relative to center of gravity in local coordinate system";
            posLH.comment = "";
            posLH.unit = "m";             
        }
    }
    // ##################################################################################




    // ##################################################################################
    // rotor parameter
    // ##################################################################################
    [Serializable]
    public class stru_rotor
    {
        public stru_bool rotor_exists { get; set; } // [-] 

        public stru_int b { get; set; } // [-] rotor blade count
        public stru_float R { get; set; } // [m] rotor blade radius
        public stru_float C_l_alpha { get; set; } // [rad^-1] lift curve slope of the rotor blade  TODO??
        public stru_float c { get; set; } // [m] is the chord length of the main rotor blade
        public stru_float J { get; set; } // [kg*m^2] rotor inertia w.r.t. rotor hub
        public stru_float C_D0 { get; set; } // [-] Drag coefficient of rotor blade
        public stru_float K_col { get; set; } // [deg] Ratio of rotor blade collective pitch angle to collective pitch servo deflection
        public stru_float Theta_col_0 { get; set; } // [deg] Offset angle
        public stru_Vector3 posLH { get; set; } // [m] Rotor position relative to helicopter's center of gravity in helicopter's local coordinate system
        public stru_Vector3 oriLH { get; set; } // [deg] Rotor orientation, relative to helicopter's local coordinate system (as right handed S123 or B321 rotation )
        public stru_Vector3 dirLH { get; set; } // [-] Rotor y-axis direction unit vector, relative to helicopter's local coordinate system


        public stru_rotor()
        {
            rotor_exists = new stru_bool();

            b = new stru_int();
            R = new stru_float();
            C_l_alpha = new stru_float();
            c = new stru_float();
            J = new stru_float();
            C_D0 = new stru_float();

            K_col = new stru_float();
            Theta_col_0 = new stru_float();
            posLH = new stru_Vector3();
            oriLH = new stru_Vector3();
            dirLH = new stru_Vector3();
        }
    }
    // ##################################################################################




    // ##################################################################################
    // mainrotor parameter
    // ##################################################################################
    [Serializable]
    public class stru_mainrotor : stru_rotor
    {
        public stru_mainrotor()
        {
            rotor_exists.val = true;
            rotor_exists.hint = "Mainrotor exists";
            rotor_exists.comment = "";
            rotor_exists.unit = "-";

            b.val = 2;
            b.min = 2;
            b.max = 6;
            b.hint = "Number of main rotor blades";
            b.comment = "";
            b.unit = "-";

            R.val = 0.775f;
            R.min = 0.01f;
            R.max = 3.00f;
            R.hint = "Main rotor blade radius";
            R.comment = "";
            R.unit = "m";

            C_l_alpha.val = 4.00f;
            C_l_alpha.min = 0.01f;
            C_l_alpha.max = 10.0f;
            C_l_alpha.hint = "Lift curve slope of the main rotor blades";
            C_l_alpha.comment = "";
            C_l_alpha.unit = "rad^-1";

            c.val = 0.058f;
            c.min = 0.005f;
            c.max = 0.100f;
            c.hint = "Chord length of the main rotor blade";
            c.comment = "";
            c.unit = "m";

            J.val = 0.092146f;
            J.min = 0.00100f;
            J.max = 1.00000f;
            J.hint = "Rotor inertia w.r.t. main rotor's rotation axis";
            J.comment = "";
            J.unit = "kg*m^2";

            C_D0.val = 0.016f;
            C_D0.min = 0.005f;
            C_D0.max = 0.050f;
            C_D0.hint = "Drag coefficient of main rotor blade";
            C_D0.comment = "";
            C_D0.unit = "-";

            K_col.val = 12f;
            K_col.min = 0.01f;
            K_col.max = 10.0f;
            K_col.hint = "Ratio of mainrotor blade collective pitch angle to collective pitch servo deflection";
            K_col.comment = "";
            K_col.unit = "deg";

            Theta_col_0.val = 0.00f;
            Theta_col_0.min = -13f;
            Theta_col_0.max = 13f;
            Theta_col_0.hint = "Offset angle for mainrotor blade collective pitch angle";
            Theta_col_0.comment = "";
            Theta_col_0.unit = "deg";

            posLH.vect3 = new Vector3 { x = 0.00f, y = 0.182387f, z = 0.00f };
            posLH.hint = "Rotor position relative to center of gravity in local coordinate system";
            posLH.comment = "";
            posLH.unit = "m";

            oriLH.vect3 = new Vector3 { x = 0.0f, y = 0.0f, z = 0.0f };
            oriLH.hint = "Rotor coordinate system orientation, relative to helicopter local coordinate system. (right handed, S123 or B321)";
            oriLH.comment = "";
            oriLH.unit = "deg";

            dirLH.calculated = true; // see Update_Calculated_Parameter()
            dirLH.hint = "Rotor y-axis direction unit vector, relative to helicopter's local coordinate system.";
            dirLH.comment = "";
            dirLH.unit = "-";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // tailrotor parameter
    // ##################################################################################
    [Serializable]
    public class stru_tailrotor : stru_rotor
    {
        //public stru_float K_ped { get; set; } // [-] Ratio of θped to δped

        public stru_tailrotor()
        {
            rotor_exists.val = true;
            rotor_exists.hint = "Tailrotor exists";
            rotor_exists.comment = "";
            rotor_exists.unit = "-";

            //K_ped = new stru_float();

            //K_ped.val = 10f;
            //K_ped.min = 0.1f;
            //K_ped.max = 100f;
            //K_ped.hint = "Ratio of θped to δped";
            //K_ped.comment = "Ratio of θped to δped";
            //K_ped.unit = "-";

            b.val = 2;
            b.min = 2;
            b.max = 6;
            b.hint = "Number of tail rotor blades";
            b.comment = "";
            b.unit = "-";

            R.val = 0.138f;
            R.min = 0.01f;
            R.max = 0.50f;
            R.hint = "Tail rotor blade radius";
            R.comment = "";
            R.unit = "m";

            C_l_alpha.val = 6.50f;
            C_l_alpha.min = 0.01f;
            C_l_alpha.max = 10.0f;
            C_l_alpha.hint = "Lift curve slope of the tail rotor blades";
            C_l_alpha.comment = "";
            C_l_alpha.unit = "rad^-1";

            c.val = 0.03f;
            c.min = 0.005f;
            c.max = 0.100f;
            c.hint = "Chord length of the tail rotor blades";
            c.comment = "";
            c.unit = "m";

            J.val = 0.000115f;
            J.min = 0.000010f;
            J.max = 0.001000f;
            J.hint = "Rotor inertia w.r.t. tail rotor's rotation axis";
            J.comment = "";
            J.unit = "kg*m^2";

            C_D0.val = 0.050f; // 0.016f;
            C_D0.min = 0.005f;
            C_D0.max = 0.050f;
            C_D0.hint = "Drag coefficient of tail rotor blade";
            C_D0.comment = "";
            C_D0.unit = "-";

            //e.val = 0.03f;
            //e.min = 0.01f;
            //e.max = 0.20f;
            //e.hint = "Effective hinge offset of rotor blade - not used for tailrotor";
            //e.comment = "Effective hinge offset of rotor blade - not used";
            //e.unit = "m";

            K_col.val = 23f;
            K_col.min = -30.0f;
            K_col.max = 30.0f;
            K_col.hint = "Ratio of tail rotor blade collective pitch angle to collective pitch servo deflection";
            K_col.comment = "";
            K_col.unit = "deg";

            Theta_col_0.val = -7.5f;
            Theta_col_0.min = -20f;
            Theta_col_0.max = 20f;
            Theta_col_0.hint = "Offset angle for tail rotor blade collective pitch angle";
            Theta_col_0.comment = "";
            Theta_col_0.unit = "deg";

            posLH.vect3 = new Vector3 { x = -0.9005973f, y = -0.01054892f, z = 0.053341444f };
            posLH.hint = "Rotor position relative to center of gravity in local coordinate system";
            posLH.comment = "";
            posLH.unit = "m";

            oriLH.vect3 = new Vector3 { x = 90.0f, y = 0.0f, z = 0.0f };
            oriLH.hint = "Rotor coordinate system orientation, relative to helicopter local coordinate system. (right handed, S123 or B321)";
            oriLH.comment = "";
            oriLH.unit = "deg";

            dirLH.calculated = true; // see Update_Calculated_Parameter()
            dirLH.hint = "Rotor y-axis direction unit vector, relative to helicopter's local coordinate system.";
            dirLH.comment = "";
            dirLH.unit = "-";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // tailrotor parameter
    // ##################################################################################
    [Serializable]
    public class stru_propeller : stru_rotor
    {
        //public stru_float K_ped { get; set; } // [-] Ratio of θped to δped

        public stru_propeller()
        {
            rotor_exists.val = false;
            rotor_exists.hint = "Pusher propeller exists (AH56 Cheyenne)";
            rotor_exists.comment = "";
            rotor_exists.unit = "-";

            //K_ped = new stru_float();

            //K_ped.val = 10f;
            //K_ped.min = 0.1f;
            //K_ped.max = 100f;
            //K_ped.hint = "Ratio of θped to δped";
            //K_ped.comment = "Ratio of θped to δped";
            //K_ped.unit = "-";


            b.val = 3;
            b.min = 2;
            b.max = 6;
            b.hint = "Number of propeller blades";
            b.comment = "";
            b.unit = "-";

            R.val = 0.30f;
            R.min = 0.01f;
            R.max = 0.50f;
            R.hint = "Propeller blade radius";
            R.comment = "";
            R.unit = "m";

            C_l_alpha.val = 5.00f;
            C_l_alpha.min = 0.01f;
            C_l_alpha.max = 10.0f;
            C_l_alpha.hint = "Lift curve slope of the propeller blades";
            C_l_alpha.comment = "";
            C_l_alpha.unit = "rad^-1";

            c.val = 0.060f;
            c.min = 0.005f;
            c.max = 0.100f;
            c.hint = "Chord length of the propeller blades";
            c.comment = "";
            c.unit = "m";

            J.val = 0.000300f; 
            J.min = 0.000010f;
            J.max = 0.001000f;
            J.hint = "Propeller's inertia w.r.t. propeller's rotation axis";
            J.comment = "";
            J.unit = "kg*m^2";

            C_D0.val = 0.016f;
            C_D0.min = 0.005f;
            C_D0.max = 0.050f;
            C_D0.hint = "Drag coefficient of propeller blade";
            C_D0.comment = "";
            C_D0.unit = "-";

            //e.val = 0.03f;
            //e.min = 0.01f;
            //e.max = 0.20f;
            //e.hint = "Effective hinge offset of rotor blade - not used for tailrotor";
            //e.comment = "Effective hinge offset of rotor blade - not used";
            //e.unit = "m";

            K_col.val = 20f;
            K_col.min = 0.01f;
            K_col.max = 10.0f;
            K_col.hint = "Ratio of propeller blade collective pitch angle to collective pitch servo deflection";
            K_col.comment = "";
            K_col.unit = "deg";

            Theta_col_0.val = 0.0f;
            Theta_col_0.min = -15f;
            Theta_col_0.max = 15f;
            Theta_col_0.hint = "Offset angle for propeller blade collective pitch angle";
            Theta_col_0.comment = "";
            Theta_col_0.unit = "deg";

            posLH.vect3 = new Vector3 { x = -1.9517f, y = 0.00f, z = 0.00f };
            posLH.hint = "Propeller position relative to center of gravity in local coordinate system";
            posLH.comment = "";
            posLH.unit = "m";

            oriLH.vect3 = new Vector3 { x = 00.0f, y = 0.0f, z = 270.0f };
            oriLH.hint = "Rotor coordinate system orientation, relative to helicopter local coordinate system. (right handed, B123)";
            oriLH.comment = "";
            oriLH.unit = "deg";

            dirLH.calculated = true; // see Update_Calculated_Parameter()
            dirLH.hint = "Rotor y-axis direction unit vector, relative to helicopter's local coordinate system.";
            dirLH.comment = "";
            dirLH.unit = "-";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // Airwolf's booster parameter
    // ##################################################################################
    [Serializable]
    public class stru_booster
    {
        public stru_bool booster_exists { get; set; } // [-] 
        public stru_bool booster_symmetric { get; set; } // [-] create two boosters if symmetric is true
        public stru_float thrust { get; set; } // [N] one booster'S thrusters
        public stru_Vector3 posLH { get; set; } // [m] Booster position relative to helicopter's center of gravity in helicopter's local coordinate system
        public stru_Vector3 oriLH { get; set; } // [deg] Booster orientation, relative to helicopter's local coordinate system (as right handed S123 or B321 rotation )
        public stru_Vector3 dirLH { get; set; } // [-] Booster y-axis direction unit vector, relative to helicopter's local coordinate system

        public stru_booster()
        {
            booster_exists = new stru_bool();
            booster_symmetric = new stru_bool();
            thrust = new stru_float();
            posLH = new stru_Vector3();
            oriLH = new stru_Vector3();
            dirLH = new stru_Vector3();


            booster_exists.val = false;
            booster_exists.hint = "Booster exists (Airwolf)";
            booster_exists.comment = "";
            booster_exists.unit = "-";

            booster_symmetric.val = true;
            booster_symmetric.hint = "Create two booster";
            booster_symmetric.comment = "";
            booster_symmetric.unit = "-";

            thrust.val = 200;
            thrust.min = 0;
            thrust.max = 1000;
            thrust.hint = "Thrust";
            thrust.comment = "";
            thrust.unit = "N";

            posLH.vect3 = new Vector3 { x = 0.0f, y = 0.0f, z = 0.0f };
            posLH.hint = "Booster position relative to center of gravity in local coordinate system";
            posLH.comment = "";
            posLH.unit = "m";

            oriLH.vect3 = new Vector3 { x = 0.0f, y = 0.0f, z = 270.0f };
            oriLH.hint = "Booster coordinate system orientation, relative to helicopter local coordinate system. (right handed, B123)";
            oriLH.comment = "";
            oriLH.unit = "deg";

            dirLH.calculated = true; // see Update_Calculated_Parameter()
            dirLH.hint = "Booster y-axis direction unit vector, relative to helicopter's local coordinate system.";
            dirLH.comment = "";
            dirLH.unit = "-";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // mainrotor flapping parameter
    // ##################################################################################
    [Serializable]
    public class stru_flapping
    {
        public stru_float hub_stiffness_mr { get; set; } // [Nm/rad] Hub torsional stiffnes
        public stru_float A_lon { get; set; } // [deg] Linkage gain ratio of θcyc,as to δlon
        public stru_float B_lat { get; set; } // [deg] Linkage gain ratio of θcyc,bs to δlat
        public stru_float A_b_s { get; set; } // [1/sec] Coupling effect from b_s to a_s
        public stru_float B_a_s { get; set; } // [1/sec] Coupling effect from a_s to b_s
        public stru_float A_a_r { get; set; } // [1/sec] Large advance ratio tilts rotor (forward)
        public stru_float B_a_r { get; set; } // [1/sec] Large advance ratio tilts rotor (sideward)
        public stru_float e { get; set; } // [-] Effective hinge offset of main rotor 
        public stru_float I_flapping { get; set; } // [-] Rotor inertia w.r.t. rotor's flapping axis in rotor hub

        public stru_flapping()
        {
            hub_stiffness_mr = new stru_float();
            A_lon = new stru_float();
            B_lat = new stru_float();
            A_b_s = new stru_float();
            B_a_s = new stru_float();
            A_a_r = new stru_float();
            B_a_r = new stru_float();
            e = new stru_float();
            I_flapping = new stru_float();

            e.val = 0.07f;
            e.min = 0.01f;
            e.max = 0.20f;
            e.hint = "Effective hinge offset of mainrotor blade";
            e.comment = "";
            e.unit = "m";

            I_flapping.val = 0.03487f; // 0.055 kg*m^2  
            I_flapping.min = 0.00100f;
            I_flapping.max = 1.00000f;
            I_flapping.hint = "One rotorblade's inertia w.r.t. rotorblade's effective hinge offset axis";
            I_flapping.comment = "";
            I_flapping.unit = "kg*m^2";


            hub_stiffness_mr.val = 120f;
            hub_stiffness_mr.min = 0.001f;
            hub_stiffness_mr.max = 1000f;
            hub_stiffness_mr.hint = "Hub stiffness in rotor head. (O-ring)";
            hub_stiffness_mr.comment = "";
            hub_stiffness_mr.unit = "Nm/rad";


            A_lon.val = 10f; // 0.2
            A_lon.min = 0.001f;
            A_lon.max = 1f;
            A_lon.hint = "Linkage gain ratio of θcyc,as to δlon";
            A_lon.comment = "";
            A_lon.unit = "deg";

            B_lat.val = 10f; // 0.2
            B_lat.min = 0.001f;
            B_lat.max = 1f;
            B_lat.hint = "Linkage gain ratio of θcyc,bs to δlat";
            B_lat.comment = "";
            B_lat.unit = "deg";

            A_b_s.val = 5f;// ~10?
            A_b_s.min = 0.0f;
            A_b_s.max = 100f;
            A_b_s.hint = "Coupling effect from b_s to a_s";
            A_b_s.comment = "";
            A_b_s.unit = "1/sec";

            B_a_s.val = 5f; // ~10?
            B_a_s.min = 0.0f;
            B_a_s.max = 100f;
            B_a_s.hint = "Coupling effect from a_s to b_s";
            B_a_s.comment = "Coupling effect from a_s to b_s";
            B_a_s.unit = "1/sec";


            A_a_r.val = 0.015f;
            A_a_r.min = 0.0f;
            A_a_r.max = 100f;
            A_a_r.hint = "Fast forward flight (large advance ratio) tilts rotor disc to \"nose up\".";
            A_a_r.comment = "";
            A_a_r.unit = "rad/m";

            B_a_r.val = 0.015f; 
            B_a_r.min = 0.0f;
            B_a_r.max = 100f;
            B_a_r.hint = "Fast sideward flight (large advance ratio) tilts rotor disc to \"side up\".";
            B_a_r.comment = "";
            B_a_r.unit = "rad/m";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // physics - tunining - vortex_ring_state
    // ##################################################################################
    [Serializable]
    public class stru_vortex_ring_state
    {
        public stru_float v_vertical { get; set; } // [m/sec] 
        public stru_float force_reduction_factor { get; set; } // [-] 
        public stru_float torque_reduction_factor { get; set; } // [-] 
        public stru_float stength_rising_speed_factor { get; set; } // [-] 

        public stru_vortex_ring_state()
        {
            v_vertical = new stru_float();
            force_reduction_factor = new stru_float();
            torque_reduction_factor = new stru_float();
            stength_rising_speed_factor = new stru_float();

            v_vertical.val = -2.5f;
            v_vertical.min = -20.0f;
            v_vertical.max = 0f;
            v_vertical.hint = "Local vertical velocity where vortex-ring-state grows at most.";
            v_vertical.comment = "";
            v_vertical.unit = "m/sec";

            force_reduction_factor.val = 0.45f;
            force_reduction_factor.min = 0.1f;
            force_reduction_factor.max = 1.0f;
            force_reduction_factor.hint = "If vortex fully developed rotor thrust is reduced at this amount (larger value => worse).";
            force_reduction_factor.comment = "";
            force_reduction_factor.unit = "-";

            torque_reduction_factor.val = 0.35f;
            torque_reduction_factor.min = 0.1f;
            torque_reduction_factor.max = 1f;
            torque_reduction_factor.hint = "If vortex fully developed rotor thrust is reduced at this amount (larger value => worse).";
            torque_reduction_factor.comment = "";
            torque_reduction_factor.unit = "-";

            stength_rising_speed_factor.val = 0.015f;
            stength_rising_speed_factor.min = 0.1f;
            stength_rising_speed_factor.max = 1f;
            stength_rising_speed_factor.hint = "How fast the vortex develops and decays.";
            stength_rising_speed_factor.comment = "";
            stength_rising_speed_factor.unit = "-";

        }
    }
    // ##################################################################################




    // ##################################################################################
    // physics - tunining - turbulence
    // ##################################################################################
    [Serializable]
    public class stru_turbulence
    {
        public stru_float rotational_velocity_limit { get; set; } // [rad/sec] 
        public stru_float translational_velocity_limit { get; set; } // [m/sec] 
        public stru_float frequency { get; set; } // [-] 
        public stru_Vector3 force_strength { get; set; } // [N] 
        public stru_Vector3 torque_strength { get; set; } // [Nm] 

        public stru_turbulence()
        {
            rotational_velocity_limit = new stru_float();
            translational_velocity_limit = new stru_float();
            frequency = new stru_float();
            force_strength = new stru_Vector3();
            torque_strength = new stru_Vector3();

            rotational_velocity_limit.val = 4f;
            rotational_velocity_limit.min = 0.01f;
            rotational_velocity_limit.max = 20f;
            rotational_velocity_limit.hint = "Rotational velocity below whitch turbulence is increased.";
            rotational_velocity_limit.comment = "";
            rotational_velocity_limit.unit = "rad/s";

            translational_velocity_limit.val = 10f;
            translational_velocity_limit.min = 0.01f;
            translational_velocity_limit.max = 20f;
            translational_velocity_limit.hint = "Translational velocity above whitch turbulence is reduced, even if rotation speed is high.";
            translational_velocity_limit.comment = "";
            translational_velocity_limit.unit = "m/s";

            frequency.val = 5.0f;
            frequency.min = 0.01f;
            frequency.max = 100f;
            frequency.hint = "Turbulence frequency.";
            frequency.comment = "";
            frequency.unit = "Hz";

            force_strength.vect3 = new Vector3 { x = 2f, y = 12f, z = 2f };
            force_strength.hint = ".";
            force_strength.comment = "";
            force_strength.unit = "N";

            torque_strength.vect3 = new Vector3 { x = 50f, y = 2f, z = 50f };
            torque_strength.hint = ".";
            torque_strength.comment = "";
            torque_strength.unit = "Nm";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // physics - tunining - startup_rotating_unbalance
    // ##################################################################################
    [Serializable]
    public class stru_startup_rotating_unbalance
    {
        public stru_float mass { get; set; } // [kg] 
        public stru_float turn_off_rpm { get; set; } // [rpm] 

        public stru_startup_rotating_unbalance()
        {
            mass = new stru_float();
            turn_off_rpm = new stru_float();

            mass.val = 0.020f;
            mass.min = 0.01f;
            mass.max = 100f;
            mass.hint = "Startup rotationg unbalance mass (r=0.1m)";
            mass.comment = "";
            mass.unit = "kg";

            turn_off_rpm.val = 250.0f;
            turn_off_rpm.min = 0.01f;
            turn_off_rpm.max = 1000f;
            turn_off_rpm.hint = "Over this shaft rotational speed the unbalance dissapears.";
            turn_off_rpm.comment = "";
            turn_off_rpm.unit = "rpm";
        }
    }
    // ##################################################################################





    // ##################################################################################
    // physics - tunining - rotor_tuning
    // ##################################################################################
    [Serializable]
    public class stru_rotor_tuning_base
    {
        public stru_float inflow_factor { get; set; } // [-] 
        public stru_float inflow_offset { get; set; } // [m/s] 

        public stru_rotor_tuning_base()
        {
            inflow_factor = new stru_float();
            inflow_offset = new stru_float();

            inflow_factor.min = 0.5f;
            inflow_factor.max = 3.0f;
            inflow_factor.comment = "";
            inflow_factor.unit = "-";

            inflow_offset.min = 0.0f;
            inflow_offset.max = 3.0f;
            inflow_offset.comment = "";
            inflow_offset.unit = "m/s";
        }
     }
    // ##################################################################################





    // ##################################################################################
    // physics - tuning - mainrotor_tuning
    // ##################################################################################
    [Serializable]
    public class stru_mainrotor_tuning : stru_rotor_tuning_base
    {
        public stru_mainrotor_tuning()
        {
            inflow_factor.val = 1.2f;
            inflow_factor.hint = "Multiplies mainrotor inflow velocity.";

            inflow_offset.val = 2.0f;
            inflow_offset.hint = "Add mainrotor inflow velocity offset.";
        }
    }
    // ##################################################################################
    // physics - tuning - tailrotor_tuning
    // ##################################################################################
    [Serializable]
    public class stru_tailrotor_tuning : stru_rotor_tuning_base
    {
        public stru_tailrotor_tuning()
        {
            inflow_factor.val = 1.0f;
            inflow_factor.hint = "Multiplies tailrotor inflow velocity.";

            inflow_offset.val = 2.0f;
            inflow_offset.hint = "Add tailrotor inflow velocity offset.";
        }
    }
    // ##################################################################################
    // physics - tuning - propeller_tuning
    // ##################################################################################
    [Serializable]
    public class stru_propeller_tuning : stru_rotor_tuning_base
    {
        public stru_propeller_tuning()
        {
            inflow_factor.val = 1.0f;
            inflow_factor.hint = "Multiplies propeller inflow velocity.";

            inflow_offset.val = 2.0f;
            inflow_offset.hint = "Add propeller inflow velocity offset.";
        }
    }
    // ##################################################################################





    // ##################################################################################
    // physics - tuning - rotor_tuning
    // ##################################################################################
    [Serializable]
    public class stru_rotor_tuning
    {
        public stru_mainrotor_tuning mainrotor_tuning { get; set; } // [-] 
        public stru_tailrotor_tuning tailrotor_tuning { get; set; } // [-] 
        public stru_propeller_tuning propeller_tuning { get; set; } // [-] 

        public stru_rotor_tuning()
        {
            mainrotor_tuning = new stru_mainrotor_tuning();
            tailrotor_tuning = new stru_tailrotor_tuning();
            propeller_tuning = new stru_propeller_tuning();
        }
    }
    // ##################################################################################





    // ##################################################################################
    // physics - tuning variables
    // ##################################################################################
    [Serializable]
    public class stru_tuning
    {
        public stru_vortex_ring_state vortex_ring_state { get; set; }
        public stru_turbulence turbulence { get; set; }
        public stru_startup_rotating_unbalance startup_rotating_unbalance { get; set; }
        public stru_rotor_tuning rotor_tuning { get; set; }
        
        public stru_tuning()
        {
            vortex_ring_state = new stru_vortex_ring_state();
            turbulence = new stru_turbulence();
            startup_rotating_unbalance = new stru_startup_rotating_unbalance();
            rotor_tuning = new stru_rotor_tuning();
        } 
    }
    // ##################################################################################




    // ##################################################################################
    // brushless parameter
    // ##################################################################################
    [Serializable]
    public class stru_brushless
    {
        // http://ocw.nctu.edu.tw/course/dssi032/DSSI_2.pdf
        public stru_float R_a { get; set; } // [ohm] brushless motor + connector + controller
        public stru_float B_M { get; set; } // [Nm/(rad/sec)] rotational friction
        public stru_float ns { get; set; }// [rpm/Volt] ideal specific no load speed            560KV  / (60 / (2 * Mathf.PI)); // [rpm/Volt] --> (rad/sec)/Volt
        [XmlIgnore]
        public stru_float Ke { get; set; } // [Volt/(rad/sec)]  back-emf factor , The motor Kv constant is the reciprocal of the back-emf constant: K_v = \frac{1}{K_e}
        [XmlIgnore]
        public stru_float Kt { get; set; } // [Nm/A]  torque constant   
        [XmlIgnore]
        public stru_float k { get; set; } // [Volt/(rad/sec)] is the same as [Nm/A]  k = Ke = Kt
        public stru_float J { get; set; } // [kg*m^2] motor inertia w.r.t. motor hub


        public stru_brushless()
        {
            R_a = new stru_float();
            B_M = new stru_float();
            ns = new stru_float();
            Ke = new stru_float();
            Kt = new stru_float();
            k = new stru_float();
            J = new stru_float();


            R_a.val = 0.038f; // Logo600seV3: http://www.drivecalc.de/ - Scorpion HK-4035-560KV
            R_a.min = 0.001f;
            R_a.max = 0.200f;
            R_a.hint = "El. resistance brushless motor + connector + controller";
            R_a.comment = "";
            R_a.unit = "ohm";

            B_M.val = 4.5E-05f;
            B_M.min = 1.0E-06f;
            B_M.max = 1.0E-04f;
            B_M.hint = "Rotational friction";
            B_M.comment = "";
            B_M.unit = "Nm/(rad/sec)";

            ns.val = 560.0f;    // 560ns[rpm/Volt] == 560kV[rpm/Volt] ==>   560[rpm/Volt] * pi/30 --> 58.6430[(rad/sec)/Volt] 
            ns.min = 1.0f;
            ns.max = 200f;
            ns.hint = "Ideal specific no load speed";
            ns.comment = "";
            ns.unit = "rpm/Volt";

            Ke.calculated = true; // see Update_Calculated_Parameter()
            Ke.val = 0f;
            Ke.min = 0f;
            Ke.max = 0f;
            Ke.hint = "Back-emf factor";
            Ke.comment = "The motor Kv constant is the reciprocal of the back-emf constant: K_v = \frac{1}{K_e}";
            Ke.unit = "rpm/Volt";

            Kt.calculated = true; // see Update_Calculated_Parameter()
            Kt.val = 0f;
            Kt.min = 0f;
            Kt.max = 0f;
            Kt.hint = "Torque constant";
            Kt.comment = "";
            Kt.unit = "Nm/A";

            k.calculated = true; // see Update_Calculated_Parameter()
            k.val = 0f;
            k.min = 0f;
            k.max = 0f;
            k.hint = "Torque constant = Back-emf factor = k";
            k.comment = "Volt/(rad/sec) is the same as [Nm/A]  k = Ke = Kt";
            k.unit = "Volt/(rad/sec)";

            J.val = 8E-05f;
            J.min = 1E-06f;
            J.max = 1E-03f;
            J.hint = "Motor inertia w.r.t. motor hub";
            J.comment = "";
            J.unit = "kg*m^2";

        }

    }
    // ##################################################################################




    // ##################################################################################
    // governor parameter
    // ##################################################################################
    [Serializable]
    public class stru_governor
    {
        public stru_Vector3 target_rpm { get; set; } // first target rotational speed of mainrotor 
        public stru_float Kp { get; set; } // rotational speed controller proportional gain
        public stru_float Ki { get; set; } // rotational speed controller Helicopter_Integrator gain
        [XmlIgnore]
        public stru_float saturation_max { get; set; } // [Volt] 
        public stru_float saturation_min { get; set; } // [Volt]
        public stru_float soft_start_factor { get; set; } // []
        public stru_float engine_restart_time { get; set; } // [sec]
        public stru_float engine_restart_factor { get; set; } // []

        public stru_governor()
        {
            target_rpm = new stru_Vector3();
            Kp = new stru_float();
            Ki = new stru_float();
            saturation_max = new stru_float();
            saturation_min = new stru_float();
            soft_start_factor = new stru_float();
            engine_restart_time = new stru_float();
            engine_restart_factor = new stru_float();

            target_rpm.vect3 = new Vector3 { x = 1160f, y = 1450f, z = 1550f }; // bank 1 2 3
            //target_rpm_1.min = 500f;
            //target_rpm_1.max = 5000f;
            target_rpm.hint = "First target rotational speed of mainrotor";
            target_rpm.comment = "";
            target_rpm.unit = "rpm";

            Kp.val = 0.001f;
            Kp.min = 0.001f;
            Kp.max = 10.0f;
            Kp.hint = "Rotational speed controller proportional gain";
            Kp.comment = "";
            Kp.unit = "-";

            Ki.val = 0.10f;
            Ki.min = 0.01f;
            Ki.max = 10.0f;
            Ki.hint = "Rotational speed controller Helicopter_Integrator gain";
            Ki.comment = "";
            Ki.unit = "-";

            saturation_max.calculated = true; // see Update_Calculated_Parameter()
            saturation_max.val = 0f;
            saturation_max.min = 0f;
            saturation_max.max = 0f;
            saturation_max.hint = "Maximum voltage at governor output";
            saturation_max.comment = "";
            saturation_max.unit = "Volt";

            saturation_min.val = 0f;
            saturation_min.min = 0f;
            saturation_min.max = 0f;
            saturation_min.hint = "Minimum voltage at governor output";
            saturation_min.comment = "";
            saturation_min.unit = "Volt";

            soft_start_factor.val = 0.25f;
            soft_start_factor.min = 0.001f;
            soft_start_factor.max = 10.000f;
            soft_start_factor.hint = "Governor soft start factor";
            soft_start_factor.comment = "Governor soft start factor";
            soft_start_factor.unit = "-";

            engine_restart_time.val = 5f;
            engine_restart_time.min = 0.01f;
            engine_restart_time.max = 10.0f;
            engine_restart_time.hint = "Within this time period after motor stop, the motor will rapidly start up.";
            engine_restart_time.comment = "";
            engine_restart_time.unit = "sec";

            engine_restart_factor.val = 1f;
            engine_restart_factor.min = 0.01f;
            engine_restart_factor.max = 10.000f;
            engine_restart_factor.hint = "Factor to fast restart the motor and accelerate to the full speed.";
            engine_restart_factor.comment = "";
            engine_restart_factor.unit = "-";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // accumulator parameter
    // ##################################################################################
    [Serializable]
    public class stru_accumulator
    {
        public stru_float voltage_per_cell { get; set; } // [Volt] singe cell nominal voltage
        public stru_int cell_count { get; set; } // [-] count of cells in accu
        public stru_float Ri { get; set; } // [ohm] inner resistance
        public stru_float I_max { get; set; } // [A] maximal current
        public stru_float capacity { get; set; } // [mAh] capacity
        [XmlIgnore]
        public stru_float voltage { get; set; } // [Volt] nominal voltage of accu

        public stru_accumulator()
        {
            voltage_per_cell = new stru_float();
            cell_count = new stru_int();
            Ri = new stru_float();
            I_max = new stru_float();
            capacity = new stru_float();
            voltage = new stru_float();



            voltage_per_cell.val = 3.7f;
            voltage_per_cell.min = 2.0f;
            voltage_per_cell.max = 4.5f;
            voltage_per_cell.hint = "Singe cell nominal voltage";
            voltage_per_cell.comment = "";
            voltage_per_cell.unit = "Volt";

            cell_count.val = 6;
            cell_count.min = 1;
            cell_count.max = 14;
            cell_count.hint = "Number of cells in accu";
            cell_count.comment = "";
            cell_count.unit = "-";

            Ri.val = 0.0040f;
            Ri.min = 0.0001f;
            Ri.max = 0.0500f;
            Ri.hint = "Inner resistance";
            Ri.comment = "";
            Ri.unit = "Ohm";

            I_max.val = 120f;
            I_max.min = 0f;
            I_max.max = 250f;
            I_max.hint = "Maximal current";
            I_max.comment = "";
            I_max.unit = "A";

            capacity.val = 5000f;
            capacity.min = 100f;
            capacity.max = 10000f;
            capacity.hint = "Capacity";
            capacity.comment = "";
            capacity.unit = "mAh";

            voltage.calculated = true; // see Update_Calculated_Parameter()
            voltage.val = 0;
            voltage.min = 0;
            voltage.max = 0;
            voltage.hint = "Nominal voltage of accu";
            voltage.comment = "";
            voltage.unit = "Volt";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // transmission parameter
    // ##################################################################################
    [Serializable]
    public class stru_transmission
    {
        public stru_bool invert_mainrotor_rotation_direction { get; set; } // [-] 
        public stru_bool invert_motor2maingear_transmission { get; set; } // [-] 
        public stru_bool invert_mainrotor2tailrotor_transmission { get; set; } // [-] 
        public stru_bool invert_mainrotor2propeller_transmission { get; set; } // [-]
        public stru_int i_gear_motor { get; set; }  // [-] motor's gear teeth count
        public stru_int i_gear_maingear { get; set; }  // [-] mainrotor's gear teeth count
        public stru_int i_gear_mainrotor { get; set; }  // [-] mainrotor's gear teeth count 
        //public stru_int i_gear_mainrotor2propeller { get; set; }  // [-] propeller's gear teeth count 
        public stru_int i_gear_tailrotor { get; set; }  // [-] tailrotor's gear teeth count
        public stru_int i_gear_propeller { get; set; }  // [-] propeller's gear teeth count
        public stru_float k_freewheel { get; set; } // [Nm/rad] rotational stiffness
        public stru_float d_freewheel { get; set; } // [Nm/(rad/s)] rotational damping
        public stru_float J_mg { get; set; } // [kg*m^2] 
        [XmlIgnore]
        public stru_float J_momg_mo { get; set; } // [kg*m^2]
        [XmlIgnore]
        public stru_float J_mrtrpr_mr { get; set; } // [kg*m^2] 
        //[XmlIgnore]
        //public stru_float J_all { get; set; } // [[kg*m^2] 
        [XmlIgnore]
        public stru_float n_mo2mr { get; set; } // [-] gear ratio 
        [XmlIgnore]
        public stru_float n_mr2tr { get; set; } // [-] gear ratio 
        [XmlIgnore]
        public stru_float n_mo2tr { get; set; } // [-] gear ratio
        [XmlIgnore]
        public stru_float n_mr2pr { get; set; } // [-] gear ratio 
        [XmlIgnore]
        public stru_float n_mo2pr { get; set; } // [-] gear ratio




        public stru_transmission()
        {
            invert_mainrotor_rotation_direction = new stru_bool();
            invert_motor2maingear_transmission = new stru_bool();
            invert_mainrotor2tailrotor_transmission = new stru_bool();
            invert_mainrotor2propeller_transmission = new stru_bool();
            i_gear_motor = new stru_int();
            i_gear_maingear = new stru_int();
            i_gear_mainrotor = new stru_int();
            //i_gear_mainrotor2propeller = new stru_int(); 
            i_gear_tailrotor = new stru_int();
            i_gear_propeller = new stru_int();
            k_freewheel = new stru_float();
            d_freewheel = new stru_float();
            J_mg = new stru_float();
            J_momg_mo = new stru_float();
            J_mrtrpr_mr = new stru_float();
            //J_all = new stru_float();
            n_mo2mr = new stru_float();
            n_mr2tr = new stru_float();
            n_mo2tr = new stru_float();
            n_mr2pr = new stru_float();
            n_mo2pr = new stru_float();



            invert_mainrotor_rotation_direction.val = true;
            invert_mainrotor_rotation_direction.hint = "Invert mainrotor rotational direction";
            invert_mainrotor_rotation_direction.comment = "";
            invert_mainrotor_rotation_direction.unit = "-";

            invert_motor2maingear_transmission.val = true;
            invert_motor2maingear_transmission.hint = "Invert maingear rotational direction";
            invert_motor2maingear_transmission.comment = "";
            invert_motor2maingear_transmission.unit = "-";

            invert_mainrotor2tailrotor_transmission.val = false;
            invert_mainrotor2tailrotor_transmission.hint = "Invert tailrotor rotational direction";
            invert_mainrotor2tailrotor_transmission.comment = "";
            invert_mainrotor2tailrotor_transmission.unit = "-";

            invert_mainrotor2propeller_transmission.val = false;
            invert_mainrotor2propeller_transmission.hint = "Invert propeller rotational direction";
            invert_mainrotor2propeller_transmission.comment = "";
            invert_mainrotor2propeller_transmission.unit = "-";

            i_gear_motor.val = 15;
            i_gear_motor.min = 5;
            i_gear_motor.max = 50;
            i_gear_motor.hint = "Motor's gear teeth count";
            i_gear_motor.comment = "";
            i_gear_motor.unit = "-";

            i_gear_maingear.val = 106; // maingear
            i_gear_maingear.min = 10;
            i_gear_maingear.max = 200;
            i_gear_maingear.hint = "Mainrotor's gear teeth count - to motor";
            i_gear_maingear.comment = "";
            i_gear_maingear.unit = "-";

            i_gear_mainrotor.val = 42;
            i_gear_mainrotor.min = 10;
            i_gear_mainrotor.max = 200;
            i_gear_mainrotor.hint = "Mainrotor's gear teeth count - to tailrotor";
            i_gear_mainrotor.comment = "";
            i_gear_mainrotor.unit = "-";

            //i_gear_mainrotor2propeller.val = 42;
            //i_gear_mainrotor2propeller.min = 10;
            //i_gear_mainrotor2propeller.max = 200;
            //i_gear_mainrotor2propeller.hint = "Mainrotor's gear teeth count - to propeller";
            //i_gear_mainrotor2propeller.comment = "Mainrotor's gear teeth count - to propeller";
            //i_gear_mainrotor2propeller.unit = "-";

            i_gear_tailrotor.val = 9;
            i_gear_tailrotor.min = 5;
            i_gear_tailrotor.max = 50;
            i_gear_tailrotor.hint = "Tailrotor's gear teeth count - to mainrotor";
            i_gear_tailrotor.comment = "";
            i_gear_tailrotor.unit = "-";

            i_gear_propeller.val = 9;
            i_gear_propeller.min = 5;
            i_gear_propeller.max = 50;
            i_gear_propeller.hint = "Propeller's gear teeth count - to mainrotor";
            i_gear_propeller.comment = "";
            i_gear_propeller.unit = "-";

            k_freewheel.val = 10f; // very weak stiffness for numerical "stability"
            k_freewheel.min = 0;
            k_freewheel.max = 0;
            k_freewheel.hint = "Freewheel (between maingear and mainrotor) rotational stiffness";
            k_freewheel.comment = "";
            k_freewheel.unit = "Nm/rad";

            d_freewheel.val = 0.001f; // damping for numerical "stability"
            d_freewheel.min = 0;
            d_freewheel.max = 0;
            d_freewheel.hint = "Freewheel (between maingear and mainrotor) rotational damping";
            d_freewheel.comment = "";
            d_freewheel.unit = "Nm/(rad/sec)";

            J_mg.val = 0.000200f; 
            J_mg.min = 0;
            J_mg.max = 0;
            J_mg.hint = "Maingear inertia w.r.t. maingear";
            J_mg.comment = "";
            J_mg.unit = "kg*m^2";

            J_momg_mo.calculated = true; // see Update_Calculated_Parameter()
            J_momg_mo.val = 0;
            J_momg_mo.min = 0;
            J_momg_mo.max = 0;
            J_momg_mo.hint = "Motor and maingear inertia w.r.t. motor";
            J_momg_mo.comment = "";
            J_momg_mo.unit = "kg*m^2";

            J_mrtrpr_mr.calculated = true; // see Update_Calculated_Parameter()
            J_mrtrpr_mr.val = 0;
            J_mrtrpr_mr.min = 0;
            J_mrtrpr_mr.max = 0;
            J_mrtrpr_mr.hint = "Mainrotor, tailrotor and propeller inertia w.r.t. mainrotor";
            J_mrtrpr_mr.comment = "";
            J_mrtrpr_mr.unit = "kg*m^2";

            //J_all.calculated = true;
            //J_all.val = 0;
            //J_all.min = 0;
            //J_all.max = 0;
            //J_all.hint = "Complete drivetrain inertia w.r.t. motor";
            //J_all.comment = "Complete drivetrain inertia w.r.t. motor";
            //J_all.unit = "kg*m^2";

            n_mo2mr.calculated = true; // see Update_Calculated_Parameter()
            n_mo2mr.val = 0;
            n_mo2mr.min = 0;
            n_mo2mr.max = 0;
            n_mo2mr.hint = "Gear ratio motor-mainrotor";
            n_mo2mr.comment = "";
            n_mo2mr.unit = "-";

            n_mr2tr.calculated = true; // see Update_Calculated_Parameter()
            n_mr2tr.val = 0;
            n_mr2tr.min = 0;
            n_mr2tr.max = 0;
            n_mr2tr.hint = "Gear ratio mainrotor-tailrotor";
            n_mr2tr.comment = "";
            n_mr2tr.unit = "-";

            n_mo2tr.calculated = true; // see Update_Calculated_Parameter()
            n_mo2tr.val = 0;
            n_mo2tr.min = 0;
            n_mo2tr.max = 0;
            n_mo2tr.hint = "Gear ratio motor-tailrotor";
            n_mo2tr.comment = "";
            n_mo2tr.unit = "-";

            n_mr2pr.calculated = true; // see Update_Calculated_Parameter()
            n_mr2pr.val = 0;
            n_mr2pr.min = 0;
            n_mr2pr.max = 0;
            n_mr2pr.hint = "Gear ratio mainrotor-propeller";
            n_mr2pr.comment = "";
            n_mr2pr.unit = "-";

            n_mo2pr.calculated = true; // see Update_Calculated_Parameter()
            n_mo2pr.val = 0;
            n_mo2pr.min = 0;
            n_mo2pr.max = 0;
            n_mo2pr.hint = "Gear ratio motor-propeller";
            n_mo2pr.comment = "";
            n_mo2pr.unit = "-";



        }
    }
    // ##################################################################################




    // ##################################################################################
    // collision points with groundplane parameter
    // ##################################################################################
    [Serializable]
    public class stru_collision_points_with_groundplane
    {
        public stru_float friction_coeff { get; set; } // [-] friction ceofficient (ground <-> heli) for ground contact
        public stru_float friction_coeff_forward { get; set; } // [-] friction coefficient (ground <-> heli) in heli's forward direction
        public stru_float friction_coeff_sideward { get; set; } // [-] friction coefficient (ground <-> heli) in heli's sideward direction
        public stru_float friction_coeff_steering { get; set; } // [-] friction coefficient (ground <-> heli) for steering gear";
        public stru_float stiffness_factor { get; set; } // [N/m] 
        public stru_float damping_factor { get; set; }  // [N/m/s] 
        public stru_list positions_left_type { get; set; }  // [skids, gear]  
        public stru_list positions_right_type { get; set; } // [skids, gear] 
        public stru_list positions_steering_center_type { get; set; }  // [support, gear]
        public stru_list positions_steering_left_type { get; set; }  // [support, gear]
        public stru_list positions_steering_right_type { get; set; }  // [support, gear]
        public stru_Vector3 positions_left_rised_offset { get; set; } // [m] 
        public stru_Vector3 positions_right_rised_offset { get; set; } // [m] 
        public stru_Vector3 positions_steering_center_rised_offset { get; set; } // [m] 
        public stru_Vector3 positions_steering_left_rised_offset { get; set; } // [m] 
        public stru_Vector3 positions_steering_right_rised_offset { get; set; } // [m] 
        public stru_Vector3_list positions_usual { get; set; } // [m] in local reference frame
        public stru_Vector3_list positions_left { get; set; } // [m] in local reference frame
        public stru_Vector3_list positions_right { get; set; } // [m] in local reference frame
        public stru_Vector3_list positions_steering_center { get; set; } // [m] in local reference frame
        public stru_Vector3_list positions_steering_left { get; set; } // [m] in local reference frame
        public stru_Vector3_list positions_steering_right { get; set; } // [m] in local reference frame
        public stru_float collision_force_max { get; set; } // [N] crashes modell if to high


        public stru_collision_points_with_groundplane()
        {
            friction_coeff = new stru_float();
            friction_coeff_forward = new stru_float();
            friction_coeff_sideward = new stru_float();
            friction_coeff_steering = new stru_float();
            stiffness_factor = new stru_float();
            damping_factor = new stru_float();
            positions_left_type = new stru_list();
            positions_right_type = new stru_list();
            positions_steering_center_type = new stru_list();
            positions_steering_left_type = new stru_list();
            positions_steering_right_type = new stru_list();
            positions_left_rised_offset = new stru_Vector3(); //
            positions_right_rised_offset = new stru_Vector3(); //
            positions_steering_center_rised_offset = new stru_Vector3(); //
            positions_steering_left_rised_offset = new stru_Vector3(); //
            positions_steering_right_rised_offset = new stru_Vector3(); //
            positions_left = new stru_Vector3_list();
            positions_right = new stru_Vector3_list();
            positions_steering_center = new stru_Vector3_list();
            positions_steering_left = new stru_Vector3_list();
            positions_steering_right = new stru_Vector3_list();
            positions_usual = new stru_Vector3_list();
            collision_force_max = new stru_float();


            friction_coeff.val = 0.30f;
            friction_coeff.min = 0.01f;
            friction_coeff.max = 0.80f;
            friction_coeff.hint = "Friction coefficient between ground plane and heli";
            friction_coeff.comment = "";
            friction_coeff.unit = "-";

            friction_coeff_forward.val = 0.25f;
            friction_coeff_forward.min = 0.01f;
            friction_coeff_forward.max = 0.80f;
            friction_coeff_forward.hint = "Friction coefficient (ground <-> heli) in heli's forward direction"; // for gears or skids
            friction_coeff_forward.comment = "";
            friction_coeff_forward.unit = "-";

            friction_coeff_sideward.val = 0.35f;
            friction_coeff_sideward.min = 0.01f;
            friction_coeff_sideward.max = 0.80f;
            friction_coeff_sideward.hint = "Friction coefficient (ground <-> heli) in heli's sideward direction"; // for gears or skids
            friction_coeff_sideward.comment = "";
            friction_coeff_sideward.unit = "-";

            friction_coeff_steering.val = 0.01f;
            friction_coeff_steering.min = 0.01f;
            friction_coeff_steering.max = 0.80f;
            friction_coeff_steering.hint = "Friction coefficient (ground <-> heli) for rear gear"; // for gears 
            friction_coeff_steering.comment = "";
            friction_coeff_steering.unit = "-";

            stiffness_factor.val = 4000.0f;
            stiffness_factor.max = 100000f;
            stiffness_factor.min = 20.0f;
            stiffness_factor.hint = "Contact stiffness factor";
            stiffness_factor.comment = "";
            stiffness_factor.unit = "N/m";

            damping_factor.val = 100.0f;
            damping_factor.max = 1000f;
            damping_factor.min = 20.0f;
            damping_factor.hint = "Contact damping factor";
            damping_factor.comment = "";
            damping_factor.unit = "N*(m/s)";


            positions_left_type.val = 0;
            positions_left_type.str = new List<string> { "Skids", "Grear" };
            positions_left_type.hint = "Contact type";
            positions_left_type.comment = "";
            positions_left_type.unit = "";

            positions_right_type.val = 0;
            positions_right_type.str = new List<string> { "Skids", "Grear" };
            positions_right_type.hint = "Contact type";
            positions_right_type.comment = "";
            positions_right_type.unit = "";

            positions_steering_center_type.val = 0;
            positions_steering_center_type.str = new List<string> { "Skids", "Grear" };
            positions_steering_center_type.hint = "Contact type";
            positions_steering_center_type.comment = "";
            positions_steering_center_type.unit = "";

            positions_steering_left_type.val = 0;
            positions_steering_left_type.str = new List<string> { "Skids", "Grear" };
            positions_steering_left_type.hint = "Contact type";
            positions_steering_left_type.comment = "";
            positions_steering_left_type.unit = "";

            positions_steering_right_type.val = 0;
            positions_steering_right_type.str = new List<string> { "Skids", "Grear" };
            positions_steering_right_type.hint = "Contact type";
            positions_steering_right_type.comment = "";
            positions_steering_right_type.unit = "";


            positions_left_rised_offset.vect3 = new Vector3 { x = 0.0f, y = 0.035f, z = +0.095f };
            positions_left_rised_offset.hint = "If left landing gear or skid is rised their collision point is moved in y-direction by this value.";
            positions_left_rised_offset.comment = "";
            positions_left_rised_offset.unit = "[m]";

            positions_right_rised_offset.vect3 = new Vector3 { x = 0.0f, y = 0.035f, z = -0.095f };
            positions_right_rised_offset.hint = "If right landing gear or skid is rised their collision point is moved in y-direction by this value.";
            positions_right_rised_offset.comment = "";
            positions_right_rised_offset.unit = "[m]";

            positions_steering_center_rised_offset.vect3 = new Vector3 { x = 0.0f, y = 0.0f, z = 0.0f };
            positions_steering_center_rised_offset.hint = "If center steering/landing gear or skid is rised their collision point is moved in y-direction by this value.";
            positions_steering_center_rised_offset.comment = "";
            positions_steering_center_rised_offset.unit = "[m]";

            positions_steering_left_rised_offset.vect3 = new Vector3 { x = 0.0f, y = 0.0f, z = 0.0f };
            positions_steering_left_rised_offset.hint = "If left steering/landing gear or skid is rised their collision point is moved in y-direction by this value.";
            positions_steering_left_rised_offset.comment = "";
            positions_steering_left_rised_offset.unit = "[m]";

            positions_steering_right_rised_offset.vect3 = new Vector3 { x = 0.0f, y = 0.0f, z = 0.0f };
            positions_steering_right_rised_offset.hint = "If right steering/landing gear or skid is rised their collision point is moved in y-direction by this value.";
            positions_steering_right_rised_offset.comment = "";
            positions_steering_right_rised_offset.unit = "[m]";


            positions_usual.vect3 = new List<Vector3>(); // right handed: x forward, y top, z to right
            //positions_usual.vect3.Add(new Vector3 { x = -0.93f, y = -0.16f, z = 0.0f });  // this parameter has to be set in the xml file, othervise every model would have this value  
            positions_usual.hint = "Contact position heli to ground";
            positions_usual.comment = "";
            positions_usual.unit = "m";

            positions_left.vect3 = new List<Vector3>(); // right handed: x forward, y top, z to right
            //positions_left.vect3.Add(new Vector3 { x = 0.2f, y = -0.16f, z = -0.1f });  // this parameter has to be set in the xml file, othervise every model would have this value  
            //positions_left.vect3.Add(new Vector3 { x = -0.1f, y = -0.18f, z = -0.1f });  // this parameter has to be set in the xml file, othervise every model would have this value  
            positions_left.hint = "Contact position heli's left landing gear or skid to ground";
            positions_left.comment = "";
            positions_left.unit = "m";

            positions_right.vect3 = new List<Vector3>(); // right handed: x forward, y top, z to right
            //positions_right.vect3.Add(new Vector3 { x = 0.2f, y = -0.16f, z = 0.1f });   // this parameter has to be set in the xml file, othervise every model would have this value  
            //positions_right.vect3.Add(new Vector3 { x = -0.1f, y = -0.18f, z = 0.1f });  // this parameter has to be set in the xml file, othervise every model would have this value  
            positions_right.hint = "Contact position heli's right landing gear or skid to ground";
            positions_right.comment = "";
            positions_right.unit = "m";

            positions_steering_center.vect3 = new List<Vector3>(); // right handed: x forward, y top, z to right
            //positions_steering_center.vect3.Add(new Vector3 { x = 0.2f, y = -0.16f, z = 0.1f });   // this parameter has to be set in the xml file, othervise every model would have this value  
            positions_steering_center.hint = "Contact position heli's center steering/landing gear or support to ground";
            positions_steering_center.comment = "";
            positions_steering_center.unit = "m";

            positions_steering_left.vect3 = new List<Vector3>(); // right handed: x forward, y top, z to right
            //positions_steering_left.vect3.Add(new Vector3 { x = 0.2f, y = -0.16f, z = 0.1f });   // this parameter has to be set in the xml file, othervise every model would have this value  
            positions_steering_left.hint = "Contact position heli's left steering/landing gear or support to ground";
            positions_steering_left.comment = "";
            positions_steering_left.unit = "m";

            positions_steering_right.vect3 = new List<Vector3>(); // right handed: x forward, y top, z to right
            //positions_steering_right.vect3.Add(new Vector3 { x = 0.2f, y = -0.16f, z = 0.1f });  // this parameter has to be set in the xml file, othervise every model would have this value   
            positions_steering_right.hint = "Contact position heli's right steering/landing gear or support to ground";
            positions_steering_right.comment = "";
            positions_steering_right.unit = "m";

            collision_force_max.val = 500f;
            collision_force_max.min = 1.00f;
            collision_force_max.max = 10000f;
            collision_force_max.hint = "If a collision force is larger then this value, the heli will crash.";
            collision_force_max.comment = "";
            collision_force_max.unit = "N";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // parameter for helicopter's initial postion in scenery 
    // ##################################################################################
    [Serializable]
    public class stru_initial_position
    {
        public stru_Vector3 position { get; set; } // [m] heli initial position in unity's left handed system
        public stru_Vector3 orientation { get; set; } // [deg] heli initial orientation in unity's left handed system

        public stru_initial_position()
        {
            position = new stru_Vector3();
            orientation = new stru_Vector3();

            position.vect3 = new Vector3 { x = 0.0f, y = 0.00f, z = 5.0f };
            position.hint = "Initial position in unity's left handed system.";
            position.comment = "";
            position.unit = "m";

            orientation.vect3 = new Vector3 { x = 0.0f, y = 30.0f, z = 0.0f };
            orientation.hint = "Initial orientation, left handed, intrinsic rotation z-x-y (S312 or B213)";
            orientation.comment = "";
            orientation.unit = "deg";
        }
    }
    // ##################################################################################



    // ##################################################################################
    // reference position from helicopters reference point to center of mass in helicopters's local coordiante system
    // ##################################################################################
    [Serializable]
    public class stru_reference_to_masscentrum
    {
        public stru_Vector3 position { get; set; } // [m] 
        public stru_Vector3 orientation { get; set; } // [deg] 

        public stru_reference_to_masscentrum()
        {
            position = new stru_Vector3();
            orientation = new stru_Vector3();

            position.vect3 = new Vector3 { x = 0.0f, y = 0.275f, z = 0.0f }; // [m]     
            position.hint = "Helicopter's reference point to center of mass in helicopters's local coordiante system. (Bright handed)";
            position.comment = "";
            position.unit = "m";

            orientation.vect3 = new Vector3 { x = 0.0f, y = 0.0f, z = -3.0f }; // [deg]
            orientation.hint = "Center of mass coordinate system orientation relative to inital frame. Right handed, extrinsic rotation z-y-x (B321 or S123)";
            orientation.comment = "";
            orientation.unit = "deg";
        }
    }
    // ##################################################################################
   



    // ##################################################################################
    // initial conditions parameter
    // ##################################################################################
    [Serializable]
    public class stru_visual_effects
    {
        public stru_float mainrotor_idle_deformation { get; set; } // [rad] 
        public stru_float mainrotor_running_deformation { get; set; } // [rad] 
        public stru_float mainrotor_blur_transparency { get; set; } // [0...1] 
        public stru_float mainrotor_blur_rpm_factor { get; set; } // [0...1] 
        public stru_float tailrotor_blur_transparency { get; set; } // [0...1] 
        public stru_float tailrotor_blur_rpm_factor { get; set; } // [0...1] 
        public stru_float propeller_blur_transparency { get; set; } // [0...1] 
        public stru_float propeller_blur_rpm_factor { get; set; } // [0...1] 
        public stru_float landing_gear_or_skids_deflection_stiffness { get; set; } // [-] 
        public stru_float landing_gear_main_radius { get; set; } // [-] 
        public stru_float landing_gear_main_transition_time_gear { get; set; } // [sec] 
        public stru_float landing_gear_main_transition_time_bay { get; set; } // [sec] 
        public stru_float landing_gear_main_mechanism_tilted_forward { get; set; } // [deg] 
        public stru_float debug_force_arrow_scale { get; set; } // [m/N] 
        public stru_float debug_torque_arrow_scale { get; set; } // [m/N] 
        public stru_bool vortex_ring_visualize { get; set; } // [-] 

        public stru_visual_effects()
        {
            mainrotor_idle_deformation = new stru_float();
            mainrotor_running_deformation = new stru_float();
            mainrotor_blur_transparency = new stru_float();
            mainrotor_blur_rpm_factor = new stru_float();
            tailrotor_blur_transparency = new stru_float();
            tailrotor_blur_rpm_factor = new stru_float();
            propeller_blur_transparency = new stru_float();
            propeller_blur_rpm_factor = new stru_float();
            landing_gear_or_skids_deflection_stiffness = new stru_float();
            landing_gear_main_radius = new stru_float();
            landing_gear_main_transition_time_gear = new stru_float();
            landing_gear_main_transition_time_bay = new stru_float();
            landing_gear_main_mechanism_tilted_forward = new stru_float();
            debug_force_arrow_scale = new stru_float();
            debug_torque_arrow_scale = new stru_float();
            vortex_ring_visualize = new stru_bool();

            mainrotor_idle_deformation.val = 2.0f;
            mainrotor_idle_deformation.max = 0.00f;
            mainrotor_idle_deformation.min = 10.0f;
            mainrotor_idle_deformation.hint = "Mainrotor deformation during ideling due to gravity.";
            mainrotor_idle_deformation.comment = "";
            mainrotor_idle_deformation.unit = "deg";

            mainrotor_running_deformation.val = 0.02f;
            mainrotor_running_deformation.max = 0.00f;
            mainrotor_running_deformation.min = 10.0f;
            mainrotor_running_deformation.hint = "Mainrotor deformation due to lifting force.";
            mainrotor_running_deformation.comment = "";
            mainrotor_running_deformation.unit = "deg/N";

            mainrotor_blur_transparency.val = 1.0f;
            mainrotor_blur_transparency.max = 0.0f;
            mainrotor_blur_transparency.min = 1.0f;
            mainrotor_blur_transparency.hint = "Mainrotor motion blured transparency";
            mainrotor_blur_transparency.comment = "";
            mainrotor_blur_transparency.unit = "-";

            mainrotor_blur_rpm_factor.val = 1.0f;
            mainrotor_blur_rpm_factor.max = 0.0f;
            mainrotor_blur_rpm_factor.min = 1.0f;
            mainrotor_blur_rpm_factor.hint = "Mainrotor motion blured rotation speed factor";
            mainrotor_blur_rpm_factor.comment = "";
            mainrotor_blur_rpm_factor.unit = "-";

            tailrotor_blur_transparency.val = 1.0f;
            tailrotor_blur_transparency.max = 0.0f;
            tailrotor_blur_transparency.min = 1.0f;
            tailrotor_blur_transparency.hint = "Tailrotor motion blured transparency";
            tailrotor_blur_transparency.comment = "";
            tailrotor_blur_transparency.unit = "-";

            tailrotor_blur_rpm_factor.val = 1.0f;
            tailrotor_blur_rpm_factor.max = 0.0f;
            tailrotor_blur_rpm_factor.min = 1.0f;
            tailrotor_blur_rpm_factor.hint = "Tailrotor motion blured rotation speed factor";
            tailrotor_blur_rpm_factor.comment = "";
            tailrotor_blur_rpm_factor.unit = "-";

            propeller_blur_transparency.val = 1.0f;
            propeller_blur_transparency.max = 0.0f;
            propeller_blur_transparency.min = 1.0f;
            propeller_blur_transparency.hint = "Propeller motion blured transparency";
            propeller_blur_transparency.comment = "";
            propeller_blur_transparency.unit = "-";

            propeller_blur_rpm_factor.val = 1.0f;
            propeller_blur_rpm_factor.max = 0.0f;
            propeller_blur_rpm_factor.min = 1.0f;
            propeller_blur_rpm_factor.hint = "Propeller motion blured rotation speed factor";
            propeller_blur_rpm_factor.comment = "";
            propeller_blur_rpm_factor.unit = "-";

            landing_gear_or_skids_deflection_stiffness.val = 100f;
            landing_gear_or_skids_deflection_stiffness.max = 0.00f;
            landing_gear_or_skids_deflection_stiffness.min = 100000.0f;
            landing_gear_or_skids_deflection_stiffness.hint = "Normalized stiffness parameter for deformation of landing gear or skids.";
            landing_gear_or_skids_deflection_stiffness.comment = "";
            landing_gear_or_skids_deflection_stiffness.unit = "-";

            landing_gear_main_radius.val = 0.00f;
            landing_gear_main_radius.max = 1.00f;
            landing_gear_main_radius.min = 0.001f;
            landing_gear_main_radius.hint = "Radius of main landing gear.";
            landing_gear_main_radius.comment = "";
            landing_gear_main_radius.unit = "m";

            landing_gear_main_transition_time_gear.val = 0.70f;
            landing_gear_main_transition_time_gear.max = 10.00f;
            landing_gear_main_transition_time_gear.min = 0.000f;
            landing_gear_main_transition_time_gear.hint = "Transition time for rising/lowering gear (only collision detection, animation duration is not effected).";
            landing_gear_main_transition_time_gear.comment = "";
            landing_gear_main_transition_time_gear.unit = "sec";

            landing_gear_main_transition_time_bay.val = 0.20f;
            landing_gear_main_transition_time_bay.max = 10.00f;
            landing_gear_main_transition_time_bay.min = 0.000f;
            landing_gear_main_transition_time_bay.hint = "Transition time for closing/opening gear bay doors (only collision detection, animation duration is not effected).";
            landing_gear_main_transition_time_bay.comment = "";
            landing_gear_main_transition_time_bay.unit = "sec";

            landing_gear_main_mechanism_tilted_forward.val = 25f;
            landing_gear_main_mechanism_tilted_forward.max = 90.00f;
            landing_gear_main_mechanism_tilted_forward.min = -90.00f;
            landing_gear_main_mechanism_tilted_forward.hint = "How much is the gear mechnism tilted forward (relative to vertical) if lowered.";
            landing_gear_main_mechanism_tilted_forward.comment = "";
            landing_gear_main_mechanism_tilted_forward.unit = "deg";

            debug_force_arrow_scale.val = 0.05f;
            debug_force_arrow_scale.max = 100f;
            debug_force_arrow_scale.min = 0.001f;
            debug_force_arrow_scale.hint = "Size of the force arrows if debugging is enabled (key d x2).";
            debug_force_arrow_scale.comment = "";
            debug_force_arrow_scale.unit = "N/m";

            debug_torque_arrow_scale.val = 0.50f;
            debug_torque_arrow_scale.max = 100f;
            debug_torque_arrow_scale.min = 0.001f;
            debug_torque_arrow_scale.hint = "Size of the toruqe arrows if debugging is enabled (key d x2).";
            debug_torque_arrow_scale.comment = "";
            debug_torque_arrow_scale.unit = "N/m";

            vortex_ring_visualize.val = false;
            vortex_ring_visualize.hint = "Show vortex ring as particle cloud around main rotor. (Sikorsky S61 only so far)";
            vortex_ring_visualize.comment = "";
            vortex_ring_visualize.unit = "-";
          
        }
    }
    // ##################################################################################

    







    // ##################################################################################
    // stick parameter
    // ##################################################################################
    [Serializable]
    public class stru_stick
    {
        public stru_Vector3 clearance { get; set; } // [0...1] 
        public stru_Vector3 expo { get; set; } // [%] 
        public stru_Vector3 dualrate { get; set; } // [%] 

        public stru_stick()
        {
            clearance = new stru_Vector3();
            expo = new stru_Vector3();
            dualrate = new stru_Vector3();

            clearance.vect3 = new Vector3 { x = 0.01f, y = 0.01f, z = 0.01f }; // bank 1 2 3 
            //clearance.min = 0.00f;
            //clearance.max = 0.50f;
            clearance.hint = "Stick center position clearance";
            clearance.comment = "";
            clearance.unit = "0...1";

            expo.vect3 = new Vector3 { x = 20f, y = 20f, z = 20f }; // bank 1 2 3
            //expo.min = 0.00f;
            //expo.max = 100f;
            expo.hint = "Stick expo";
            expo.comment = "";
            expo.unit = "%";

            dualrate.vect3 = new Vector3 { x = 100f, y = 100f, z = 100f }; // bank 1 2 3
            //dualrate.min = 0f;
            //dualrate.max = 100f;
            dualrate.hint = "Stick dualrate";
            dualrate.comment = "";
            dualrate.unit = "%";
        }
    }
    // ##################################################################################



    // ##################################################################################
    // switch parameter
    // ##################################################################################
    [Serializable]
    public class stru_switch
    {
        public stru_list type { get; set; } // [0,1] 

        public stru_switch()
        {
            type = new stru_list();
        }
    }
    // ##################################################################################



    // ##################################################################################
    // switch-0 parameter
    // ##################################################################################
    [Serializable]
    public class stru_switch0 : stru_switch
    {
        public stru_switch0()
        {
            type.val = 1;
            type.str = new List<string> { "Switch", "Button"};
            type.hint = "Switch-0 type: 0=>switch, 1=>button  (rising flank trigger)";
            type.comment = "";
            type.unit = "";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // switch-1 parameter
    // ##################################################################################
    [Serializable]
    public class stru_switch1 : stru_switch
    {
        public stru_switch1()
        {
            type.val = 1;
            type.str = new List<string> { "Switch", "Button" };
            type.hint = "Switch-1 type: 0=>switch, 1=>button  (rising flank trigger)";
            type.comment = "";
            type.unit = "";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // transmitter parameter
    // ##################################################################################
    [Serializable]
    public class stru_transmitter
    {
        public stru_float countdown_minutes { get; set; }// [min] 
        public stru_float countdown_volume { get; set; } // [%]
        public stru_stick stick_roll { get; set; }// [] Stick-roll
        public stru_stick stick_pitch { get; set; } // []  Stick-pitch
        public stru_stick stick_yaw { get; set; } // []  Stick-yaw
        public stru_stick stick_collective { get; set; } // []  Stick-collective
        public stru_switch0 switch0 { get; set; }
        public stru_switch1 switch1 { get; set; }

        public stru_transmitter()
        {
            countdown_minutes = new stru_float();
            countdown_volume = new stru_float();

            countdown_minutes.val = 6.00f; //7.50f;
            countdown_minutes.min = 0.00f;
            countdown_minutes.max = 30.0f;
            countdown_minutes.hint = "Transmitter countdown timer";
            countdown_minutes.comment = "";
            countdown_minutes.unit = "min";

            countdown_volume.val = 70.00f;
            countdown_volume.min = 0.000f;
            countdown_volume.max = 100.0f;
            countdown_volume.hint = "Transmitter's audio volume";
            countdown_volume.comment = "";
            countdown_volume.unit = "%";

            stick_roll = new stru_stick();
            stick_pitch = new stru_stick();
            stick_yaw = new stru_stick();
            stick_collective = new stru_stick();
            switch0 = new stru_switch0();
            switch1 = new stru_switch1();
        }

    }
    // ##################################################################################




    // ##################################################################################
    // flybarless controller parameter - gyro
    // ##################################################################################
    [Serializable]
    public class stru_yaw_rate_controller
    {
        public stru_Vector3 K_a { get; set; } /// [-] yaw rate controller input signal amplifier
        public stru_Vector3 K_I { get; set; } /// [-] yaw rate controller integral gain
        public stru_Vector3 K_p { get; set; } /// [-] yaw rate controller proportional gain
        public stru_Vector3 K_d { get; set; } /// [-] yaw rate controller differential gain
        public stru_bool direct_feadtrough_gyroscope { get; set; } /// [-] Turn off gyro. 


        public stru_yaw_rate_controller() 
        {
            K_a = new stru_Vector3();
            K_I = new stru_Vector3();
            K_p = new stru_Vector3();
            K_d = new stru_Vector3();
            direct_feadtrough_gyroscope = new stru_bool();

            K_a.vect3 = new Vector3 { x = 430, y = 480, z = 500 }; // bank 1 2 3
            //K_a.min = 100f;
            //K_a.max = 2000f;
            K_a.hint = "Tailrotor maximum rotation velocity.";
            K_a.comment = "";
            K_a.unit = "deg/sec";

            K_I.vect3 = new Vector3 { x = 2, y = 2, z = 2 }; // bank 1 2 3
            //K_I.min = 0.01f;
            //K_I.max = 100f;
            K_I.hint = "Tailrotor integral gain (Heading Hold)";
            K_I.comment = "";
            K_I.unit = "1/sec";

            K_p.vect3 = new Vector3 { x = 0.20f, y = 0.20f, z = 0.20f }; // bank 1 2 3
            //K_p.min = 0.01f;
            //K_p.max = 100f;
            K_p.hint = "Tailrotor proportional gain";
            K_p.comment = "";
            K_p.unit = "-";

            K_d.vect3 = new Vector3 { x = 0.0001f, y = 0.0001f, z = 0.0001f }; // bank 1 2 3
            //K_d.min = 0.0001f;
            //K_d.max = 100f;
            K_d.hint = "Tailrotor differential gain";
            K_d.comment = "";
            K_d.unit = "-";
            
            direct_feadtrough_gyroscope.val = false;
            direct_feadtrough_gyroscope.hint = "Turn off gyro.";
            direct_feadtrough_gyroscope.comment = "";
            direct_feadtrough_gyroscope.unit = "-";

        }
    }
    // ##################################################################################




    // ##################################################################################
    // flybarless controller parameter
    // ##################################################################################
    [Serializable]
    public class stru_flybarless_controller
    {
        public stru_Vector3 K_a { get; set; } // [-] flybarless controller input signal amplifier
        public stru_Vector3 K_I { get; set; } // [-] flybarless controller integral gain
        public stru_Vector3 K_p { get; set; } // [-] flybarless controller proportional gain
        public stru_Vector3 K_d { get; set; } // [-] flybarless controller differential  gain
        public stru_bool direct_feadtrough_flybarless { get; set; } // [-] Turn off flybarless. 
        public stru_float initial_roll_angle { get; set; } /// [-] initial yaw angle after reset
        public stru_float initial_pitch_angle { get; set; } /// [-] initial yaw angle after reset

        public stru_flybarless_controller()
        {
            K_a = new stru_Vector3();
            K_I = new stru_Vector3();
            K_p = new stru_Vector3();
            K_d = new stru_Vector3();
            direct_feadtrough_flybarless = new stru_bool();
            initial_roll_angle = new stru_float(); /// [-] initial roll angle after reset
            initial_pitch_angle = new stru_float(); /// [-] initial pitch angle after reset

            K_a.vect3 = new Vector3 { x = 180, y = 220, z = 240 }; // bank 1 2 3
            //K_a.min = 0.01f;
            //K_a.max = 100f;
            K_a.hint = "Flybarless maximum rotation velocity. (Should be smaller, than what heli can perform, othervise stick range will be not fully used)";
            K_a.comment = "";
            K_a.unit = "deg/sec";

            K_I.vect3 = new Vector3 { x = 2.5f, y = 2.5f, z = 2.5f }; // bank 1 2 3
            //K_I.min = 0.01f;
            //K_I.max = 10f;
            K_I.hint = "Flybarless integral gain";
            K_I.comment = "";
            K_I.unit = "1/sec";

            K_p.vect3 = new Vector3 { x = 0.05f, y = 0.06f, z = 0.08f }; // bank 1 2 3
            //K_p.min = 0.01f;
            //K_p.max = 10f;
            K_p.hint = "Flybarless proportional gain";
            K_p.comment = "";
            K_p.unit = "-";

            K_d.vect3 = new Vector3 { x = 0.0003f, y = 0.0003f, z = 0.0003f }; // bank 1 2 3
            //K_d.min = 0.00001f;
            //K_d.max = 100f;
            K_d.hint = "Flybarless differential gain";
            K_d.comment = "";
            K_d.unit = "-";

            direct_feadtrough_flybarless.val = false;
            direct_feadtrough_flybarless.hint = "Turn off flybarless.";
            direct_feadtrough_flybarless.comment = "";
            direct_feadtrough_flybarless.unit = "-";

            initial_roll_angle.val = -0.0f;
            initial_roll_angle.min = -10f;
            initial_roll_angle.max = 10f;
            initial_roll_angle.hint = "Initial yaw angle after reset";
            initial_roll_angle.comment = "";
            initial_roll_angle.unit = "deg";

            initial_pitch_angle.val = 0.0f;
            initial_pitch_angle.min = -10f;
            initial_pitch_angle.max = 10f;
            initial_pitch_angle.hint = "Initial pitch angle after reset";
            initial_pitch_angle.comment = "";
            initial_pitch_angle.unit = "deg";

        }
    }
    // ##################################################################################




    // ##################################################################################
    // servo rotation damping
    // ##################################################################################
    [Serializable]
    public class stru_servo_damping
    {
        public stru_float servo_col_mr_time_constant { get; set; } // [sec] damping of mainrotor collective movement - Collective
        public stru_float servo_lat_mr_time_constant { get; set; } // [sec] damping of mainrotor lateral movement - Roll
        public stru_float servo_lon_mr_time_constant { get; set; } // [sec] damping of mainrotor longitudial movement - Pitch
        public stru_float servo_col_tr_time_constant { get; set; } // [sec] damping of tailrotor collective movement - Yaw
        public stru_float servo_lat_tr_time_constant { get; set; } // [sec] damping of tailrotor lateral movement 
        public stru_float servo_lon_tr_time_constant { get; set; } // [sec] damping of tailrotor longitudial movement 

        public stru_servo_damping()
        {
            servo_col_mr_time_constant = new stru_float();
            servo_lat_mr_time_constant = new stru_float();
            servo_lon_mr_time_constant = new stru_float();
            servo_col_tr_time_constant = new stru_float();
            servo_lat_tr_time_constant = new stru_float();
            servo_lon_tr_time_constant = new stru_float();

            // PT1-elemnt Step response: a = K * (1 - e^(t-T))   --> if t = 3.0000 T then output reaches 0.95 %  // https://de.wikipedia.org/wiki/PT1-Glied
            // SAVÖX SB-2270SG datasheet sais: 0.1500 [sec/60°] @ 6.0V --> real movement -30°...0...~30° --> 0.075 [sec/30°] --> 0.075 sec for normalized "servo_col_mr" 0...1 --> For PT1-element reach 95%: T is then 0.075 / 3.0000  = 0.025 sec

            servo_col_mr_time_constant.val = 0.025f; 
            servo_col_mr_time_constant.min = 0.010f;
            servo_col_mr_time_constant.max = 1.000f;
            servo_col_mr_time_constant.hint = "PT1-time constant for damping of mainrotor collective movement (Collective).";
            servo_col_mr_time_constant.comment = "";
            servo_col_mr_time_constant.unit = "sec";

            servo_lat_mr_time_constant.val = 0.025f;
            servo_lat_mr_time_constant.min = 0.010f;
            servo_lat_mr_time_constant.max = 1.000f;
            servo_lat_mr_time_constant.hint = "PT1-time constant for damping of mainrotor lateral movement (Roll).";
            servo_lat_mr_time_constant.comment = "";
            servo_lat_mr_time_constant.unit = "sec";

            servo_lon_mr_time_constant.val = 0.025f;
            servo_lon_mr_time_constant.min = 0.010f;
            servo_lon_mr_time_constant.max = 1.000f;
            servo_lon_mr_time_constant.hint = "PT1-time constant for damping of mainrotor longitudial movement (Pitch).";
            servo_lon_mr_time_constant.comment = "";
            servo_lon_mr_time_constant.unit = "sec";


            servo_col_tr_time_constant.val = 0.015f;
            servo_col_tr_time_constant.min = 0.010f;
            servo_col_tr_time_constant.max = 1.000f;
            servo_col_tr_time_constant.hint = "PT1-time constant for damping of tailrotor collective movement (Yaw).";
            servo_col_tr_time_constant.comment = "";
            servo_col_tr_time_constant.unit = "sec";

            servo_lat_tr_time_constant.val = 0.025f;
            servo_lat_tr_time_constant.min = 0.010f;
            servo_lat_tr_time_constant.max = 1.000f;
            servo_lat_tr_time_constant.hint = "PT1-time constant for damping of tailrotor lateral movement (Roll).";
            servo_lat_tr_time_constant.comment = "";
            servo_lat_tr_time_constant.unit = "sec";

            servo_lon_tr_time_constant.val = 0.025f;
            servo_lon_tr_time_constant.min = 0.010f;
            servo_lon_tr_time_constant.max = 1.000f;
            servo_lon_tr_time_constant.hint = "PT1-time constant for damping of tailrotor longitudial movement (Pitch).";
            servo_lon_tr_time_constant.comment = "";
            servo_lon_tr_time_constant.unit = "sec";
        }
    }
    // ##################################################################################





    // ##################################################################################
    // helicopter parameter
    // ##################################################################################
    [Serializable]
    public class stru_helicopter
    {
        public string name;

        /// [int] 0:Single Main Rotor, (1:Tandem rotor, 2:Coaxial, 3:Intermeshing rotors)  https://www.skybrary.aero/index.php/Helicopter_Rotor_Systems_Configuration
        public stru_list rotor_systems_configuration { get; set; } 

        public stru_float mass_total { get; set; }// [kg] total mass of the whole heli
        public stru_Vector3 Jxx_Jyy_Jzz { get; set; } // [kg*m²] forward up right
        public stru_Vector3 Jxy_Jxz_Jyz { get; set; } // [kg*m²] 
        public stru_float sound_volume { get; set; } // [%]
        public stru_float mainrotor_sound_recorded_rpm { get; set; } // [rpm]
        public stru_float tailrotor_sound_recorded_rpm { get; set; } // [rpm]
        public stru_float motor_sound_recorded_rpm { get; set; } // [rpm]
        public stru_float stall_sound_recorded_rpm { get; set; } // [rpm]


        public stru_reference_to_masscentrum reference_to_masscentrum { get; set; }
        public stru_yaw_rate_controller gyro { get; set; }
        public stru_flybarless_controller flybarless { get; set; }
        public stru_servo_damping servo_damping { get; set; }
        public stru_governor governor { get; set; }
        public stru_brushless brushless { get; set; }
        public stru_accumulator accumulator { get; set; }
        public stru_transmission transmission { get; set; }
        public stru_mainrotor mainrotor { get; set; }
        //public stru_mainrotor mainrotor_rear { get; set; }
        public stru_flapping flapping { get; set; }
        public stru_tailrotor tailrotor { get; set; }
        public stru_propeller propeller { get; set; }
        public stru_booster booster { get; set; }
        public stru_fuselage fuselage { get; set; }
        public stru_horizontal_fin horizontal_fin { get; set; }
        public stru_vertical_fin vertical_fin { get; set; }
        public stru_wing horizontal_wing_left { get; set; }
        public stru_wing horizontal_wing_right { get; set; }
        public stru_collision_points_with_groundplane collision { get; set; }
        public stru_visual_effects visual_effects { get; set; }

        public stru_helicopter()
        {
            name = "Logo600SE_V3";

            rotor_systems_configuration = new stru_list();

            mass_total = new stru_float();
            Jxx_Jyy_Jzz = new stru_Vector3();
            Jxy_Jxz_Jyz = new stru_Vector3();
            sound_volume = new stru_float();
            mainrotor_sound_recorded_rpm = new stru_float();
            tailrotor_sound_recorded_rpm = new stru_float();
            motor_sound_recorded_rpm = new stru_float();
            stall_sound_recorded_rpm = new stru_float();

            reference_to_masscentrum = new stru_reference_to_masscentrum();
            gyro = new stru_yaw_rate_controller();
            flybarless = new stru_flybarless_controller();
            servo_damping = new stru_servo_damping();
            governor = new stru_governor();
            brushless = new stru_brushless();
            accumulator = new stru_accumulator();
            transmission = new stru_transmission();
            mainrotor = new stru_mainrotor();
            //mainrotor_rear = new stru_mainrotor(); // for tandem (coaxial, intermeshing) setup
            flapping = new stru_flapping();
            tailrotor = new stru_tailrotor();
            propeller = new stru_propeller();
            booster = new stru_booster();
            fuselage = new stru_fuselage();
            horizontal_fin = new stru_horizontal_fin();       // tail
            vertical_fin = new stru_vertical_fin();         // tail
            horizontal_wing_left = new stru_wing();
            horizontal_wing_right = new stru_wing();
            collision = new stru_collision_points_with_groundplane();
            visual_effects = new stru_visual_effects();


            rotor_systems_configuration.val = 0;
            rotor_systems_configuration.str = new List<string> { "Single main rotor", "Tandem rotor"};
            rotor_systems_configuration.hint = "Rotor system type"; // (2:Coaxial, 3:Intermeshing rotors)";
            rotor_systems_configuration.comment = "";
            rotor_systems_configuration.unit = "";

            mass_total.val = 3.6f;
            mass_total.min = 0.1f;
            mass_total.max = 100f;
            mass_total.hint = "Total mass of heli";
            mass_total.comment = "";
            mass_total.unit = "kg";

            Jxx_Jyy_Jzz.vect3 = new Vector3 { x = 0.0373f, y = 0.2637f, z = 0.2916f }; //  0.040791 0.1872171  0.2238604
            Jxx_Jyy_Jzz.hint = "Moment of inertia heli. (diagonal elements)";
            Jxx_Jyy_Jzz.comment = "";
            Jxx_Jyy_Jzz.unit = "kg m^2";

            Jxy_Jxz_Jyz.vect3 = new Vector3 { x = 0.006f, y = 0.005f, z = 0.001f }; // 0.006  -0.0011598  0.0002123
            Jxy_Jxz_Jyz.hint = "Moment of inertia heli. (off-diagonal elements)";
            Jxy_Jxz_Jyz.comment = "";
            Jxy_Jxz_Jyz.unit = "kg m^2";

            sound_volume.val = 100.00f;
            sound_volume.min = 0.000f;
            sound_volume.max = 100.0f;
            sound_volume.hint = "Helicopter's audio volume";
            sound_volume.comment = "";
            sound_volume.unit = "%";

            motor_sound_recorded_rpm.val = 1000f;
            motor_sound_recorded_rpm.min = 0.000f;
            motor_sound_recorded_rpm.max = 10000.0f;
            motor_sound_recorded_rpm.hint = "Helicopter's brushless-motor audio file recorded rpm";
            motor_sound_recorded_rpm.comment = "";
            motor_sound_recorded_rpm.unit = "rpm";

            mainrotor_sound_recorded_rpm.val = 1160f;
            mainrotor_sound_recorded_rpm.min = 0.000f;
            mainrotor_sound_recorded_rpm.max = 10000.0f;
            mainrotor_sound_recorded_rpm.hint = "Helicopter's mainrotor audio file recorded rpm";
            mainrotor_sound_recorded_rpm.comment = "";
            mainrotor_sound_recorded_rpm.unit = "rpm";

            tailrotor_sound_recorded_rpm.val = 6500f;
            tailrotor_sound_recorded_rpm.min = 0.000f;
            tailrotor_sound_recorded_rpm.max = 10000.0f;
            tailrotor_sound_recorded_rpm.hint = "Helicopter's tailrotor audio file recorded rpm";
            tailrotor_sound_recorded_rpm.comment = "";
            tailrotor_sound_recorded_rpm.unit = "rpm";

            stall_sound_recorded_rpm.val = 1160f;
            stall_sound_recorded_rpm.min = 0.000f;
            stall_sound_recorded_rpm.max = 10000.0f;
            stall_sound_recorded_rpm.hint = "Helicopter's rotor stall audio file recorded rpm";
            stall_sound_recorded_rpm.comment = "";
            stall_sound_recorded_rpm.unit = "rpm";
        }
    }
    // ##################################################################################




    // ##################################################################################
    // helicopter and transmitter
    // ##################################################################################
    [Serializable]
    public class stru_transmitter_and_helicopter
    {
        public stru_transmitter transmitter { get; set; }
        public stru_helicopter helicopter { get; set; }
        public stru_tuning tuning { get; set; }

        public stru_transmitter_and_helicopter()
        {
            transmitter = new stru_transmitter();
            helicopter = new stru_helicopter();
            tuning = new stru_tuning();
        }


        // some parameters can be direct derived by the input values
        public void Update_Calculated_Parameter()
        {
            //UnityEngine.Debug.Log("UpdateCalculatedParameter");

            helicopter.mainrotor.dirLH.vect3 = Helper.A_RL_S123(helicopter.mainrotor.oriLH.vect3 * Helper.Deg_to_Rad, new Vector3(0, 1, 0) ); // per definition the rotor local y-axis is the shaft axis
            helicopter.tailrotor.dirLH.vect3 = Helper.A_RL_S123(helicopter.tailrotor.oriLH.vect3 * Helper.Deg_to_Rad, new Vector3(0, 1, 0) ); // per definition the rotor local y-axis is the shaft axis
            helicopter.propeller.dirLH.vect3 = Helper.A_RL_S123(helicopter.propeller.oriLH.vect3 * Helper.Deg_to_Rad, new Vector3(0, 1, 0) ); // per definition the rotor local y-axis is the shaft axis
            helicopter.booster.dirLH.vect3 = Helper.A_RL_S123(helicopter.booster.oriLH.vect3 * Helper.Deg_to_Rad, new Vector3(0, 1, 0) ); // per definition the booster local y-axis is the thrust axis
            
            //http://www.powerditto.de/Modellierung-Theorie2.doc
            helicopter.brushless.Ke.val = 1f / (helicopter.brushless.ns.val * Mathf.PI / 30f); // [Volt/(rad/sec)]  back-emf factor , The motor Kv constant is the reciprocal of the back-emf constant: K_v = \frac{1}{K_e}
            helicopter.brushless.Kt.val = 1f / (helicopter.brushless.ns.val * Mathf.PI / 30f); // [Nm/A]  torque constant
            helicopter.brushless.k.val = 1f / (helicopter.brushless.ns.val * Mathf.PI / 30f); // [Volt/(rad/sec)] is the same as [Nm/A]  k = Ke = Kt

            helicopter.accumulator.voltage.val = helicopter.accumulator.voltage_per_cell.val * helicopter.accumulator.cell_count.val; // [Volt] nominal voltage of accu

            helicopter.governor.saturation_max.val = helicopter.accumulator.voltage.val; // [Volt]

            // reduction i = n_B/n_A, with input gear n_A teeth and output gear n_B 
            helicopter.transmission.n_mo2mr.val = - (helicopter.transmission.invert_motor2maingear_transmission.val ? -1.0f : 1.0f) * helicopter.transmission.i_gear_maingear.val / helicopter.transmission.i_gear_motor.val; // [-] gear ratio 
            helicopter.transmission.n_mr2tr.val = - (helicopter.transmission.invert_mainrotor2tailrotor_transmission.val ? -1.0f : 1.0f) * helicopter.transmission.i_gear_tailrotor.val / helicopter.transmission.i_gear_mainrotor.val; // [-] gear ratio 
            helicopter.transmission.n_mr2pr.val = - (helicopter.transmission.invert_mainrotor2propeller_transmission.val ? -1.0f : 1.0f) * helicopter.transmission.i_gear_propeller.val / helicopter.transmission.i_gear_mainrotor.val; // [-] gear ratio 
            helicopter.transmission.n_mo2tr.val = helicopter.transmission.n_mr2tr.val * helicopter.transmission.n_mo2mr.val; // [-] gear ratio 
            helicopter.transmission.n_mo2pr.val = helicopter.transmission.n_mr2pr.val * helicopter.transmission.n_mo2mr.val; // [-] gear ratio 

            // http://institute.unileoben.ac.at/etechnik/Ger/Lehre_G/Skripten_G/Files_G/El_Antriebst/D_Getriebe_Tragheitsmoment.pdf
            // intertia as seen from motor (motor is rotating very fast)
            // J = J1 + J2 / ü^2 = J1 + J2 / ((i_driven/i_driving)^2) = J1 + J2 * (omega_driven/omega_driving)^2
            helicopter.transmission.J_momg_mo.val =   helicopter.brushless.J.val +
                                                     (helicopter.transmission.J_mg.val / Mathf.Pow(helicopter.transmission.n_mo2mr.val, 2)); // [kg m^2] w.r.t. motor
            if(helicopter.tailrotor.rotor_exists.val && helicopter.propeller.rotor_exists.val)
                helicopter.transmission.J_mrtrpr_mr.val = helicopter.mainrotor.J.val +
                                                         (helicopter.tailrotor.J.val / Mathf.Pow(helicopter.transmission.n_mr2tr.val, 2)) +
                                                         (helicopter.propeller.J.val / Mathf.Pow(helicopter.transmission.n_mr2pr.val, 2)); // [kg m^2] w.r.t. mainrotor

            if (helicopter.tailrotor.rotor_exists.val && !helicopter.propeller.rotor_exists.val)
                helicopter.transmission.J_mrtrpr_mr.val = helicopter.mainrotor.J.val +
                                                         (helicopter.tailrotor.J.val / Mathf.Pow(helicopter.transmission.n_mr2tr.val, 2)); // [kg m^2] w.r.t. mainrotor

            if (!helicopter.tailrotor.rotor_exists.val && !helicopter.propeller.rotor_exists.val)
                helicopter.transmission.J_mrtrpr_mr.val = helicopter.mainrotor.J.val; // [kg m^2] w.r.t. mainrotor

        }
    }
    // ##################################################################################




    // ##################################################################################
    // main parameter 
    // ##################################################################################
    [Serializable]
    public class Parameter
    {
        public stru_simulation simulation { get; set; }
        public stru_scenery scenery { get; set; }
        public stru_transmitter_and_helicopter transmitter_and_helicopter { get; set; }

        public Parameter()
        {
            simulation = new stru_simulation();
            scenery = new stru_scenery();
            transmitter_and_helicopter = new stru_transmitter_and_helicopter();
        }

    }
    // ##################################################################################



}



