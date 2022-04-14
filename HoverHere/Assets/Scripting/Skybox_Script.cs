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
using System.IO;
using UnityEngine.Networking;
using System.Linq;
using System.Xml.Serialization;
using System;
using System.Xml;
using System.Xml.Linq;
using UnityEngine.UI;
using UnityEngine.XR;
using Common;


// ############################################################################                                                                                                                   
//                                                                  bbbbbbbb                                               
//       SSSSSSSSSSSSSSS kkkkkkkk                                   b::::::b                                               
//     SS:::::::::::::::Sk::::::k                                   b::::::b                                               
//    S:::::SSSSSS::::::Sk::::::k                                   b::::::b                                               
//    S:::::S     SSSSSSSk::::::k                                    b:::::b                                               
//    S:::::S             k:::::k    kkkkkkkyyyyyyy           yyyyyyyb:::::bbbbbbbbb       ooooooooooo xxxxxxx      xxxxxxx
//    S:::::S             k:::::k   k:::::k  y:::::y         y:::::y b::::::::::::::bb   oo:::::::::::oox:::::x    x:::::x 
//     S::::SSSS          k:::::k  k:::::k    y:::::y       y:::::y  b::::::::::::::::b o:::::::::::::::ox:::::x  x:::::x  
//      SS::::::SSSSS     k:::::k k:::::k      y:::::y     y:::::y   b:::::bbbbb:::::::bo:::::ooooo:::::o x:::::xx:::::x   
//        SSS::::::::SS   k::::::k:::::k        y:::::y   y:::::y    b:::::b    b::::::bo::::o     o::::o  x::::::::::x    
//           SSSSSS::::S  k:::::::::::k          y:::::y y:::::y     b:::::b     b:::::bo::::o     o::::o   x::::::::x     
//                S:::::S k:::::::::::k           y:::::y:::::y      b:::::b     b:::::bo::::o     o::::o   x::::::::x     
//                S:::::S k::::::k:::::k           y:::::::::y       b:::::b     b:::::bo::::o     o::::o  x::::::::::x    
//    SSSSSSS     S:::::Sk::::::k k:::::k           y:::::::y        b:::::bbbbbb::::::bo:::::ooooo:::::o x:::::xx:::::x   
//    S::::::SSSSSS:::::Sk::::::k  k:::::k           y:::::y         b::::::::::::::::b o:::::::::::::::ox:::::x  x:::::x  
//    S:::::::::::::::SS k::::::k   k:::::k         y:::::y          b:::::::::::::::b   oo:::::::::::oox:::::x    x:::::x 
//     SSSSSSSSSSSSSSS   kkkkkkkk    kkkkkkk       y:::::y           bbbbbbbbbbbbbbbb      ooooooooooo xxxxxxx      xxxxxxx
//                                                y:::::y                                                                  
//                                               y:::::y                                                                   
//                                              y:::::y                                                                    
//                                             y:::::y                                                                     
//                                            yyyyyyy                                                                                                                       
// ############################################################################
// third part of Helicopter_Main.cs with Skybox handling
// ############################################################################
public partial class Helicopter_Main : Helicopter_TimestepModel
{


    // ############################################################################
    //      SSSSSSSSSSSSSSS      tttt                                                         tttt                                                                   
    //     SS:::::::::::::::S  ttt:::t                                                      ttt:::t                                                                   
    //    S:::::SSSSSS::::::S  t:::::t                                                      t:::::t                                                                   
    //    S:::::S     SSSSSSS  t:::::t                                                      t:::::t                                                                   
    //    S:::::S        ttttttt:::::ttttttt    uuuuuu    uuuuuu      ccccccccccccccccttttttt:::::ttttttt    uuuuuu    uuuuuu rrrrr   rrrrrrrrr       eeeeeeeeeeee    
    //    S:::::S        t:::::::::::::::::t    u::::u    u::::u    cc:::::::::::::::ct:::::::::::::::::t    u::::u    u::::u r::::rrr:::::::::r    ee::::::::::::ee  
    //     S::::SSSS     t:::::::::::::::::t    u::::u    u::::u   c:::::::::::::::::ct:::::::::::::::::t    u::::u    u::::u r:::::::::::::::::r  e::::::eeeee:::::ee
    //      SS::::::SSSSStttttt:::::::tttttt    u::::u    u::::u  c:::::::cccccc:::::ctttttt:::::::tttttt    u::::u    u::::u rr::::::rrrrr::::::re::::::e     e:::::e
    //        SSS::::::::SS    t:::::t          u::::u    u::::u  c::::::c     ccccccc      t:::::t          u::::u    u::::u  r:::::r     r:::::re:::::::eeeee::::::e
    //           SSSSSS::::S   t:::::t          u::::u    u::::u  c:::::c                   t:::::t          u::::u    u::::u  r:::::r     rrrrrrre:::::::::::::::::e 
    //                S:::::S  t:::::t          u::::u    u::::u  c:::::c                   t:::::t          u::::u    u::::u  r:::::r            e::::::eeeeeeeeeee  
    //                S:::::S  t:::::t    ttttttu:::::uuuu:::::u  c::::::c     ccccccc      t:::::t    ttttttu:::::uuuu:::::u  r:::::r            e:::::::e           
    //    SSSSSSS     S:::::S  t::::::tttt:::::tu:::::::::::::::uuc:::::::cccccc:::::c      t::::::tttt:::::tu:::::::::::::::uur:::::r            e::::::::e          
    //    S::::::SSSSSS:::::S  tt::::::::::::::t u:::::::::::::::u c:::::::::::::::::c      tt::::::::::::::t u:::::::::::::::ur:::::r             e::::::::eeeeeeee  
    //    S:::::::::::::::SS     tt:::::::::::tt  uu::::::::uu:::u  cc:::::::::::::::c        tt:::::::::::tt  uu::::::::uu:::ur:::::r              ee:::::::::::::e  
    //     SSSSSSSSSSSSSSS         ttttttttttt      uuuuuuuu  uuuu    cccccccccccccccc          ttttttttttt      uuuuuuuu  uuuurrrrrrr                eeeeeeeeeeeeee 
    // ############################################################################
    #region structure

    // ############################################################################
    // skymap structure holing path information
    // ############################################################################
    [Serializable]
    public class stru_skymap_paths
    {
        public string name { get; set; }

        public bool is_downloadable { get; set; }
        public bool is_downloaded { get; set; }

        public string fullpath_skymap_folder { get; set; }

        public string fullpath_information_file { get; set; }
        public string fullpath_preview_image { get; set; }

        public string fullpath_front_texture { get; set; }
        public string fullpath_back_texture { get; set; }
        public string fullpath_left_texture { get; set; }
        public string fullpath_right_texture { get; set; }
        public string fullpath_up_texture { get; set; }
        public string fullpath_down_texture { get; set; }

        public string fullpath_parameter_file { get; set; }  /// stored i.e. in ...\Free-RC-Helicopter-Simulator\Assets\StreamingAssets\Skymaps\MFC-Ulm_Neu-Ulm_001\
        public string fullpath_parameter_file_used { get; set; }  /// stored i.e. in c:\Users\ .... \AppData\LocalLow\Free RC Helicopter Simulator\Free RC Helicopter Simulator\Resources\SavedSceneriesParametersets\

        public string fullpath_collision_object { get; set; }
        public string fullpath_collision_landing_object { get; set; }

        public string fullpath_ambient_audio_file { get; set; }


        public stru_skymap_paths()
        {
            name = "not set";

            is_downloadable = false;
            is_downloaded = false;

            fullpath_information_file = "not set";
            fullpath_preview_image = "not set";

            fullpath_front_texture = "not set";
            fullpath_back_texture = "not set";
            fullpath_left_texture = "not set";
            fullpath_right_texture = "not set";
            fullpath_up_texture = "not set";
            fullpath_down_texture = "not set";

            fullpath_parameter_file = "not set";

            fullpath_collision_object = "not set";
            fullpath_collision_landing_object = "not set";
            
            fullpath_ambient_audio_file = "not set";
        }
    }
    // ############################################################################
 



    // ############################################################################
    // main skymap strucutre
    // ############################################################################
    [Serializable]
    public class stru_skymap
    {
        public stru_skymap_paths skymap_paths { get; set; }

        public stru_skymap()
        {
            skymap_paths = new stru_skymap_paths();
        }
    }
    // ############################################################################    
    #endregion







    // ############################################################################
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
    // ############################################################################
    #region fields
    // ############################################################################
    // class fields
    // ############################################################################
    public Material skybox_material;

    private Cubemap skybox_cubemap;
    public Material ground_material;

    private int skymap_number_old;
    // ############################################################################
    #endregion







    // ############################################################################
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
    // ############################################################################s
    #region initialize
    //// ############################################################################
    //// Use this for initialization
    //// ############################################################################
    //// is called by Start() in main partial class file

    //// ############################################################################
    #endregion















    // ############################################################################
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
    // ############################################################################
    //                                                                                                                                       
    // ############################################################################          
    #region skybox_handling
    public void Check_Skymaps(ref int skymap_number, ref List<stru_skymap_paths> list_skymap_paths)
    {
        list_skymap_paths.Clear(); 

        // check the skymap folder and get the info about the content
        Get_Skymaps_Paths(ref list_skymap_paths);

        // check if next or previous scenery is selected ( if stepping with sKey throug sceneries )
        bool couning_up1_or_down0 = skymap_number > skymap_number_old ? true : false;

        // not downloaded sceneries can't be selected
        while (true)
        { 
            // check if selected skmyap number is available
            if (skymap_number >= list_skymap_paths.Count)
                skymap_number = 0;
            if (skymap_number < 0)
                skymap_number = list_skymap_paths.Count - 1;

            // allow only with this "installation" shipped or already downloaded sceneries to be selected
            if ( (list_skymap_paths[skymap_number].is_downloadable == true &&
                  list_skymap_paths[skymap_number].is_downloaded == true) || 
                 (list_skymap_paths[skymap_number].is_downloadable == false &&
                  list_skymap_paths[skymap_number].is_downloaded == false) )
            {
                break;
            }
            else
            {
                skymap_number += couning_up1_or_down0 ? 1 : -1;
            }  
        }
        skymap_number_old = skymap_number;

        scenery_name = list_skymap_paths[skymap_number].name;

        // get parameter xml-file name
        ui_dropdown_actual_selected_scenery_xml_filename = PlayerPrefs.GetString("SavedSetting____" + scenery_name + "____actual_selected_xml_filename", null);
        if (string.IsNullOrEmpty(ui_dropdown_actual_selected_scenery_xml_filename) ||
             !File.Exists(folder_saved_parameter_for_sceneries + ui_dropdown_actual_selected_scenery_xml_filename) )
        {
            ui_dropdown_actual_selected_scenery_xml_filename = scenery_name + "_default_parameter.xml";
            PlayerPrefs.SetString("ui_dropdown_actual_selected_scenery_xml_filename", ui_dropdown_actual_selected_scenery_xml_filename);
            PlayerPrefs.SetString("SavedSetting____" + scenery_name + "____actual_selected_xml_filename", ui_dropdown_actual_selected_scenery_xml_filename);
        }
    }


    public bool Load_Skymap(List<stru_skymap_paths> list_skymap_paths, int skymap_number)
    {
        // if at least one skymap exists load selected skymap
        if (list_skymap_paths.Count > 0)
        {  
            // load textures into the skybox
            Change_Skybox_Material(list_skymap_paths[skymap_number]);

            // load scenery parameter from xml file into parameter structure
            IO_Load_Scenery_Parameter(folder_saved_parameter_for_sceneries + ui_dropdown_actual_selected_scenery_xml_filename); // list_skymap_paths[skymap_number].fullpath_parameter_file_used);

            // change/apply scenery parameter
            Change_Skybox_Sun_Sound_Camera_Parameter();

            // load collision object files
            Change_Skybox_Collision_Object(list_skymap_paths[skymap_number]);

            // load ambient sound file
            Change_Skybox_Ambient_Sound(list_skymap_paths[skymap_number]);

            // setup animals: birds and insects
            Flocks_Update(ref all_animal_flocks);
        }

        PlayerPrefs.SetInt("active_scenery_id", active_scenery_id);

        Manage_Tab_Button_Logic(Available_Tabs.scenery);

        // update reflexion probe
        reflextion_probe.RenderProbe(null);

        return true;
    }
    // ############################################################################



   
    // ############################################################################
    // change the skybox material textures
    // ############################################################################
    private void Change_Skybox_Material(stru_skymap_paths skymap_paths)
    {
        Change_Skybox_Material_Subfunction(skymap_paths.fullpath_front_texture, "_FrontTex", CubemapFace.PositiveZ, true); // left -> front  
        Change_Skybox_Material_Subfunction(skymap_paths.fullpath_back_texture, "_BackTex", CubemapFace.NegativeZ, false);  // right -> back 
        Change_Skybox_Material_Subfunction(skymap_paths.fullpath_left_texture, "_LeftTex", CubemapFace.NegativeX, false); // back -> right
        Change_Skybox_Material_Subfunction(skymap_paths.fullpath_right_texture, "_RightTex", CubemapFace.PositiveX, false); // front -> left
        Change_Skybox_Material_Subfunction(skymap_paths.fullpath_up_texture, "_UpTex", CubemapFace.PositiveY, false); // top
        Change_Skybox_Material_Subfunction(skymap_paths.fullpath_down_texture, "_DownTex", CubemapFace.NegativeY, false); // bottom

        skybox_cubemap.Apply();
        
        Resources.UnloadUnusedAssets();
        DynamicGI.UpdateEnvironment();
    }
    // ############################################################################




    // ############################################################################
    // change the skybox material textures - helper function
    // ############################################################################
    private void Change_Skybox_Material_Subfunction(string path, string material_direction, CubemapFace cubamp_direction, bool init)
    {
        Texture2D tex_front = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Load_Image(path, ref tex_front);

        if (init) // change the resolution of the skybox_cubemap to fit the size of the image
        {
            skybox_cubemap = new Cubemap(tex_front.width, TextureFormat.RGBA32, false);
            ground_material.SetTexture("_SkyCube", skybox_cubemap);
        }

        // cubemap is used for the ground
        tex_front.Rotate(Common.Texture2DExtensions.Rotation.HalfCircle);
        skybox_cubemap.SetPixels(tex_front.GetPixels(), cubamp_direction);  
        // skybox is the background
        tex_front.Rotate(Common.Texture2DExtensions.Rotation.HalfCircle);
        skybox_material.SetTexture(material_direction, tex_front);
    }
    // ############################################################################




    // ############################################################################
    // change settings according to parameter file "..._parameter_file.xml"
    // ############################################################################
    private void Change_Skybox_Sun_Sound_Camera_Parameter()
    {
        // setup sun light
        Light directional_light = GameObject.Find("Sun").gameObject.GetComponent<Light>();
        directional_light.transform.position = helicopter_ODE.par_temp.scenery.lighting.sun_position.vect3;
        directional_light.transform.forward = -helicopter_ODE.par_temp.scenery.lighting.sun_position.vect3; // sun should point to origin (close to camera)
        directional_light.intensity = helicopter_ODE.par_temp.scenery.lighting.sun_intensity.val;
        directional_light.shadowStrength = helicopter_ODE.par_temp.scenery.lighting.sun_shadow_strength.val;

        // setup ambient light
        RenderSettings.ambientLight = new Color(helicopter_ODE.par_temp.scenery.lighting.ambient_light_color.vect3.x, helicopter_ODE.par_temp.scenery.lighting.ambient_light_color.vect3.y, helicopter_ODE.par_temp.scenery.lighting.ambient_light_color.vect3.z,255)* helicopter_ODE.par_temp.scenery.lighting.ambient_light_intensity.val;  
        //RenderSettings.ambientIntensity = helicopter_ODE.par_temp.scenery.lighting.ambient_light_intensity.val;
    
        // setup skymap
        skybox_material.SetColor("_TintColor", new Color(helicopter_ODE.par_temp.scenery.skybox.skybox_tint_color.vect3.x, helicopter_ODE.par_temp.scenery.skybox.skybox_tint_color.vect3.y, helicopter_ODE.par_temp.scenery.skybox.skybox_tint_color.vect3.z,0));
        skybox_material.SetFloat("_Exposure", helicopter_ODE.par_temp.scenery.skybox.skybox_exposure.val);
        skybox_material.SetFloat("_FlippingHorizontaly", helicopter_ODE.par_temp.scenery.skybox.skybox_flipping_horizontally.val ? 1.0f : 0.0f );  // convert bool to float
        skybox_material.SetFloat("_Rotation", helicopter_ODE.par_temp.scenery.skybox.skybox_rotation.val);


        ground_material.SetColor("_TintColor", new Color(helicopter_ODE.par_temp.scenery.skybox.skybox_tint_color.vect3.x, helicopter_ODE.par_temp.scenery.skybox.skybox_tint_color.vect3.y, helicopter_ODE.par_temp.scenery.skybox.skybox_tint_color.vect3.z, 0));
        ground_material.SetFloat("_Exposure", helicopter_ODE.par_temp.scenery.skybox.skybox_exposure.val);
        ground_material.SetFloat("_CameraHeight", helicopter_ODE.par_temp.scenery.camera_height.val);


        // setup ambient audio
        AudioSource ambient_audio_source = GameObject.Find("Ambient Sound").gameObject.GetComponent<AudioSource>();
        ambient_audio_source.volume = (helicopter_ODE.par_temp.scenery.ambient_sound_volume.val / 100f);
       
        // setup camera position
        Camera main_camera = GameObject.Find("Main Camera").gameObject.GetComponent<Camera>();
        Vector3 main_camera_position = main_camera.transform.position;
        main_camera_position.y = helicopter_ODE.par_temp.scenery.camera_height.val;
        main_camera.transform.position = main_camera_position;

        // set ui-canvas worldspace poition and rotation
        if (XRSettings.enabled)
        {
            Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
            RectTransform canva_rt = canvas.GetComponent<RectTransform>();
            canva_rt.localPosition = Quaternion.Euler(0, helicopter_ODE.par.scenery.xr.canvas_rotation.vect3.y, 0) * helicopter_ODE.par.scenery.xr.canvas_position.vect3;
            canva_rt.localRotation = Quaternion.Euler(helicopter_ODE.par.scenery.xr.canvas_rotation.vect3);



        }

        /*
                // setup camera position
                if (!XRSettings.enabled)
                {
                    Camera main_camera = GameObject.Find("Main Camera").gameObject.GetComponent<Camera>();
                    Vector3 main_camera_position = main_camera.transform.position;
                    main_camera_position.y = helicopter_ODE.par_temp.scenery.camera_height.val;
                    main_camera.transform.position = main_camera_position;
                }
                else
                {
                    //Camera main_camera = GameObject.Find("XRRig").gameObject.GetComponent<Camera>();
                    //GameObject XRRig = GameObject.FindWithTag("XRRig").GetComponent<Camera_Offset>()

                    CameraOffset XRRig = GameObject.Find("XRRig").gameObject.GetComponent<CameraOffset> ();
                    XRRig.cameraYOffset = helicopter_ODE.par_temp.scenery.camera_height.val;

                    //private Helicopter_Main Helicopter_Main;
                    //Helicopter_Main = GameObject.Find("Game_Controller").GetComponent<Helicopter_Main>();
                }*/

    }
    // ############################################################################




    // ############################################################################
    // load the collision object "collision_object.obj" from streamingassets folder
    // ############################################################################
    private void Change_Skybox_Collision_Object(stru_skymap_paths skymap_paths)
    {
        // collision object for reseting model (collision with blades)
        Mesh holder_mesh = new Mesh();
        //ObjImporter newMesh = new ObjImporter();
        holder_mesh = ObjImporter.ImportFile(skymap_paths.fullpath_collision_object);

        MeshFilter collision_object_mesh_filter = GameObject.Find("Collision Object").transform.Find("collisionobject").gameObject.GetComponent<MeshFilter>();
        collision_object_mesh_filter.mesh = holder_mesh;

        MeshCollider collision_object_mesh_collider = GameObject.Find("Collision Object").transform.Find("collisionobject").gameObject.GetComponent<MeshCollider>();
        collision_object_mesh_collider.sharedMesh = holder_mesh;

        collision_object_mesh_filter.mesh.RecalculateNormals();
        collision_object_mesh_collider.sharedMesh.RecalculateNormals();

        // collision object for landig 
        Mesh holder_mesh2 = new Mesh();
        holder_mesh2 = ObjImporter.ImportFile(skymap_paths.fullpath_collision_landing_object);
        // this is calulated in the helicopter_ODE-thread at high update frequency
        helicopter_ODE.Set_AABB_Skybox_Collision_Landing_Mesh(holder_mesh2);
    }
    // ############################################################################




    // ############################################################################
    // change the ambient sound to "ambient_audio.ogg" from streamingassets folder "Skymaps"
    // ############################################################################
    private void Change_Skybox_Ambient_Sound(stru_skymap_paths skymap_paths)
    {
        AudioSource myAudioSource = GameObject.Find("Ambient Sound").gameObject.GetComponent<AudioSource>();

        Play_Audio(myAudioSource, skymap_paths.fullpath_ambient_audio_file);
    }
    // ############################################################################




    // ############################################################################
    // load audio file
    // ############################################################################
    // https://docs.unity3d.com/Manual/UnityWebRequest-CreatingDownloadHandlers.html?_ga=2.62809708.1726406585.1583568286-240521355.1564957385
    private IEnumerator GetAudioClip(AudioSource myAudioSource, string fileName, AudioType audio_type)
    {
        using (var uwr = UnityWebRequestMultimedia.GetAudioClip(fileName, audio_type))
        {
            yield return uwr.SendWebRequest();

            // if (uwr.isNetworkError || uwr.isHttpError)
            if (!string.IsNullOrEmpty(uwr.error))
            {
                Debug.LogError(uwr.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
            // use clip
            myAudioSource.clip = clip;
            myAudioSource.Play();
        }
    }
    // ############################################################################




    // ############################################################################
    // load texture (i.e. for skymaps)
    // ############################################################################
    private static Texture2D Load_Image(string path)
    {
        //byte[] image_bytes = System.IO.File.ReadAllBytes(path);

        Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, false);// , TextureFormat.RGBA32, false
        texture2D.LoadImage(System.IO.File.ReadAllBytes(path));
        //image_bytes = null;
        //GC.Collect();

        //texture2D.FixTransparency();
        // do not use alpha information
        if (Path.GetExtension(path) == ".png" || Path.GetExtension(path) == ".PNG")
        {
            Color[] pix = texture2D.GetPixels(); // get pixel colors
            for (int i = 0; i < pix.Length; i++)
                pix[i].a = 1; // set the alpha of each pixel to 1
            texture2D.SetPixels(pix); // set changed pixel alphas
            texture2D.Apply(); // upload texture to GPU
        }


        //texture2D.EncodeToJPG();
        texture2D.wrapMode = TextureWrapMode.Clamp;
        return texture2D;
    }


    private static void Load_Image(string path, ref Texture2D texture2D)
    {
        //byte[] image_bytes = System.IO.File.ReadAllBytes(path);

        //Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, false);// , TextureFormat.RGBA32, false
        texture2D.LoadImage(System.IO.File.ReadAllBytes(path));
        //image_bytes = null;
        //GC.Collect();

        //texture2D.FixTransparency();
        // do not use alpha information
        if (Path.GetExtension(path) == ".png" || Path.GetExtension(path) == ".PNG")
        {
            Color[] pix = texture2D.GetPixels(); // get pixel colors
            for (int i = 0; i < pix.Length; i++)
                pix[i].a = 1; // set the alpha of each pixel to 1
            texture2D.SetPixels(pix); // set changed pixel alphas
            texture2D.Apply(); // upload texture to GPU
        }

        //texture2D.EncodeToJPG();
        texture2D.wrapMode = TextureWrapMode.Clamp;
        //return texture2D;
    }
    // ############################################################################







    // ############################################################################
    // search for available skymaps in the streamingassets "Skymaps" folder
    // ############################################################################
    private void Get_Skymaps_Paths(ref List<stru_skymap_paths> list_skymap_paths)
    {
        // On many platforms, the streaming assets folder location is read - only; 
        // you can not modify or write new files there at runtime. Use Application.persistentDataPath 
        // for a folder location that is writable. Therefore all scenery files (folder with all 
        // neccessary images and also partial empty folder with downloadable content) are copied to 
        // Application.persistentDataPath.
        string fullpath_skymap_folder;
        if (((Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)) &&
            helicopter_ODE.par_temp.simulation.storage.sceneries_file_location.val == 0 )
        {
            fullpath_skymap_folder = Path.Combine(Application.streamingAssetsPath, "Skymaps"); // Windows

            // On Windows we can use (read and write) Application.streamingAssetsPath. We can leave the "installed" files there.
        }
        else
        {
            fullpath_skymap_folder = Path.Combine(Application.persistentDataPath, "Skymaps");  // Only choice on MacIO, ...

            // On other systems (MacOS, ...) copy everthing from Application.streamingAssetsPath to Application.persistentDataPath
            string SourcePath = Path.Combine(Application.streamingAssetsPath, "Skymaps");
            string DestinationPath = fullpath_skymap_folder;
            if (!Directory.Exists(DestinationPath) || first_start_flag == 1) // if new version, then also copy the files
            {
                Directory.CreateDirectory(DestinationPath);
                Helper.Directory_Copy(SourcePath, DestinationPath, true);
            }
        }

        List<string> list_fullpath_skymap_folder = new List<string>(Directory.GetDirectories(fullpath_skymap_folder));

        // check if necessary files are available
        foreach (string each_fullpath_skymap_folder in list_fullpath_skymap_folder)
        {
            bool flag_folder_ok = false, flag_images_ok = false;
            bool flag_preview_image_found_ok = false, flag_parameter_file_found_ok = false, flag_collision_file_found_ok = false;
            bool flag_collision_landing_file_found_ok = false, flag_information_file_found_ok = false, flag_information_file_url_found_ok = false;
            //bool flag_ambient_audio_file_found_ok = false, 

            // create skymap struct to store information about skymap
            stru_skymap_paths skymap_paths_temp = new stru_skymap_paths();

            // get skymap name
            skymap_paths_temp.name = Path.GetFileNameWithoutExtension(each_fullpath_skymap_folder);

            // get fullpath to  Path.Combine(Application.streamingAssetsPath, "Skymaps"); or  Path.Combine(Application.persistentDataPath, "Skymaps"))
            skymap_paths_temp.fullpath_skymap_folder = each_fullpath_skymap_folder;

            // find images
            if (Directory.Exists(Path.Combine(each_fullpath_skymap_folder , "4096")))
            {

                flag_folder_ok = true;
                //UnityEngine.Debug.Log("each_skymap " + each_skymap + "/4096/");

                // find images
                //string supportedExtensions = "*.bmp,*.exr,*.gif,*.hdr,*.iff,*.jpeg,*.jpg,*.pict,*.png,*.psd,*.tga,*.tif,*.tiff";
                string supportedExtensions = ".jpeg,*.jpg,*.png";
                IEnumerable<string> image_files = Directory.GetFiles(Path.Combine(each_fullpath_skymap_folder , "4096"), "*.*", SearchOption.TopDirectoryOnly).Where(s => supportedExtensions.Contains(Path.GetExtension(s).ToLower()));

                // check images
                int number_of_textures_found = 0;
                foreach (string each_image_path in image_files)
                {
                    //UnityEngine.Debug.Log(each_image_path);
                    string filename = Path.GetFileNameWithoutExtension(each_image_path);
                    //UnityEngine.Debug.Log(filename);

                    if (filename.Substring(filename.Length - 5).ToLower().CompareTo("front") == 0)
                    {
                        skymap_paths_temp.fullpath_front_texture = each_image_path;
                        number_of_textures_found++;
                    }
                    if (filename.Substring(filename.Length - 4).ToLower().CompareTo("back") == 0)
                    {
                        skymap_paths_temp.fullpath_back_texture = each_image_path;
                        number_of_textures_found++;
                    }
                    if (filename.Substring(filename.Length - 4).ToLower().CompareTo("left") == 0)
                    {
                        skymap_paths_temp.fullpath_right_texture = each_image_path; // left flipped with right !
                        number_of_textures_found++;
                    }
                    if (filename.Substring(filename.Length - 5).ToLower().CompareTo("right") == 0)
                    {
                        skymap_paths_temp.fullpath_left_texture = each_image_path; // right flipped with left !
                        number_of_textures_found++;
                    }
                    if (filename.Substring(filename.Length - 2).ToLower().CompareTo("up") == 0 ||
                        filename.Substring(filename.Length - 3).ToLower().CompareTo("top") == 0)
                    {
                        skymap_paths_temp.fullpath_up_texture = each_image_path;
                        number_of_textures_found++;
                    }
                    if (filename.Substring(filename.Length - 4).ToLower().CompareTo("down") == 0 ||
                        filename.Substring(filename.Length - 6).ToLower().CompareTo("bottom") == 0)
                    {
                        skymap_paths_temp.fullpath_down_texture = each_image_path;
                        number_of_textures_found++;
                    }
                }

                if (number_of_textures_found == 6)
                {  
                    flag_images_ok = true;
                }
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(each_fullpath_skymap_folder, "4096"));
                flag_folder_ok = false;
            }



            // find preview image
            string fullpath_preview_image = Path.Combine(each_fullpath_skymap_folder,"preview.jpg");
            if (File.Exists(fullpath_preview_image))
            {
                skymap_paths_temp.fullpath_preview_image = fullpath_preview_image;
                flag_preview_image_found_ok = true;
            }


            // find settings file
            string fullpath_parameter_file = Path.Combine(each_fullpath_skymap_folder, skymap_paths_temp.name + "_parameter_file.xml");
            if (File.Exists(fullpath_parameter_file))
            {
                skymap_paths_temp.fullpath_parameter_file = fullpath_parameter_file;
                skymap_paths_temp.fullpath_parameter_file_used = fullpath_parameter_file; // set it here only as default
                flag_parameter_file_found_ok = true;
            }

            // find information file
            string fullpath_information_file = Path.Combine(each_fullpath_skymap_folder, skymap_paths_temp.name + "_information_file.xml");
            if (File.Exists(fullpath_information_file))
            {
                skymap_paths_temp.fullpath_information_file = fullpath_information_file;
                flag_information_file_found_ok = true;

                // import xml for checking at the end of this method: if for downloadble sceneries a url is given
                stru_selection_content scenery_selection_content = Common.Helper.IO_XML_Deserialize<stru_selection_content>(skymap_paths_temp.fullpath_information_file);
                if(scenery_selection_content.downloadlink != "not downloadable") // TODO also check if url is valid
                    flag_information_file_url_found_ok = true;
            }


            // find collision object
            string fullpath_collision_object = Path.Combine(each_fullpath_skymap_folder, "collision_object.obj");
            if (File.Exists(fullpath_collision_object))
            {
                skymap_paths_temp.fullpath_collision_object = fullpath_collision_object;
                flag_collision_file_found_ok = true;
            }


            // find collision object for landing 
            string fullpath_collision_landing_object = Path.Combine(each_fullpath_skymap_folder, "collision_landing_object.obj");
            if (File.Exists(fullpath_collision_landing_object))
            {
                skymap_paths_temp.fullpath_collision_landing_object = fullpath_collision_landing_object;
                flag_collision_landing_file_found_ok = true;
            }


            // find ambient audio file
            string fullpath_ambient_audio_file = Path.Combine(each_fullpath_skymap_folder, "ambient_audio.ogg");
            if (File.Exists(fullpath_ambient_audio_file))
            {
                skymap_paths_temp.fullpath_ambient_audio_file = fullpath_ambient_audio_file;
                //flag_ambient_audio_file_found_ok = true;
            }



            // if a minium neccessary files have been found, then assign thhis skymap to the skymap list
            //UnityEngine.Debug.Log("flag_images_ok " + flag_images_ok + " flag_folder_ok " + flag_folder_ok);
            if (flag_folder_ok && flag_preview_image_found_ok && flag_images_ok && flag_collision_file_found_ok && flag_collision_landing_file_found_ok && flag_parameter_file_found_ok && flag_information_file_found_ok)
            {
                list_skymap_paths.Add(skymap_paths_temp);
                list_skymap_paths[list_skymap_paths.Count - 1].is_downloadable = false;
                list_skymap_paths[list_skymap_paths.Count - 1].is_downloaded = false;

                if (flag_information_file_url_found_ok)
                { 
                    list_skymap_paths[list_skymap_paths.Count - 1].is_downloadable = true;
                    list_skymap_paths[list_skymap_paths.Count - 1].is_downloaded = true;
                }
            }

            // some sceneries are downloadable, and thus not included in the basic installation. These sceneries must have some files included and are checked here for them:
            //if (flag_preview_image_found_ok && !flag_images_ok && !flag_collision_file_found_ok && flag_collision_landing_file_found_ok && flag_parameter_file_found_ok && flag_information_file_found_ok && flag_information_file_url_found_ok)
            if (flag_preview_image_found_ok && !flag_images_ok && flag_collision_landing_file_found_ok && flag_parameter_file_found_ok && flag_information_file_found_ok && flag_information_file_url_found_ok)
            {
                list_skymap_paths.Add(skymap_paths_temp);
                list_skymap_paths[list_skymap_paths.Count - 1].is_downloadable = true;
                list_skymap_paths[list_skymap_paths.Count - 1].is_downloaded = false;
            }

            //UnityEngine.Debug.Log("each_fullpath_skymap_folder " + each_fullpath_skymap_folder + "     skymap_paths_temp.name " + skymap_paths_temp.name);
            //UnityEngine.Debug.Log("flag_folder_ok " + flag_folder_ok +
            //                    "  flag_preview_image_found_ok " + flag_preview_image_found_ok +
            //                    "  flag_images_ok " + flag_images_ok +
            //                    "  flag_collision_file_found_ok " + flag_collision_file_found_ok +
            //                    "  flag_collision_landing_file_found_ok " + flag_collision_landing_file_found_ok +
            //                    "  flag_parameter_file_found_ok " + flag_parameter_file_found_ok +
            //                    "  flag_information_file_found_ok " + flag_information_file_found_ok +
            //                    "  flag_information_file_url_found_ok " + flag_information_file_url_found_ok);


        }

    }
    #endregion




}



