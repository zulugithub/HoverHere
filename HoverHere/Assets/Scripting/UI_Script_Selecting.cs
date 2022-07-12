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
    public enum Ui_Selection_Type
    {
        helicopter,
        scenery
    }

    public struct UI_Helicopter_Or_Scenery_Selection
    {
        public Transform ui_scroll_view_content;
        public Scrollbar ui_scroll_view_scrollbar;

        public GameObject ui_prefab_selection;  // prefab for holding selection informations with picture

        public Toggle ui_toggle_show_only_favourites;
    }

    public UI_Helicopter_Or_Scenery_Selection ui_helicopter_selection = new UI_Helicopter_Or_Scenery_Selection();
    public UI_Helicopter_Or_Scenery_Selection ui_scenery_selection = new UI_Helicopter_Or_Scenery_Selection();


    [Serializable]
    public class stru_selection_content
    {
        public string name { get; set; } // 
        public string author { get; set; } // 
        public string date { get; set; } // 
        public string version { get; set; } //
        public string info { get; set; } // 
        public string homepagelink { get; set; } // 
        public string downloadlink { get; set; } // 

        public stru_selection_content()
        {
            name = "-";
            author = "-";
            date = "-";
            version = "1.0";
            info = "-";
            homepagelink = "-";
            downloadlink = "not downloadable";
        }
    }

    stru_selection_content helicopter_selection_content = new stru_selection_content();
    stru_selection_content scenery_selection_content = new stru_selection_content();

    int helicopter__prefabs_vertical_position_on_scroll_view_ui = 0;
    int scenery__prefabs_vertical_position_on_scroll_view_ui = 0;
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
    void UI_Initialize_Selection_UI(Ui_Selection_Type ui_Selection_Type)
    {
        UI_Helicopter_Or_Scenery_Selection ui_selection = new UI_Helicopter_Or_Scenery_Selection();

        string type_setup = null;
        if (ui_Selection_Type == Ui_Selection_Type.helicopter)
            type_setup = "Helicopter";
        if (ui_Selection_Type == Ui_Selection_Type.scenery)
            type_setup = "Scenery";

        ui_selection.ui_scroll_view_content = ui_canvas.transform.Find("UI Info Menu " + type_setup + " Selection/List Panel/Scroll View/Viewport/Content");
        ui_selection.ui_scroll_view_scrollbar = ui_canvas.transform.Find("UI Info Menu " + type_setup + " Selection/List Panel/Scroll View/Scrollbar Vertical").GetComponent<Scrollbar>();
        ui_selection.ui_scroll_view_scrollbar.value = 1f;

        ui_selection.ui_toggle_show_only_favourites = ui_canvas.transform.Find("UI Info Menu " + type_setup + " Selection/Toggle_Only_Favourites").GetComponent<Toggle>();

        // seleted object from list
        ui_selection.ui_toggle_show_only_favourites.isOn = PlayerPrefs.GetInt("SavedSetting____" + type_setup + "____Toggle_Show_Only_Favourites", 0) == 0 ? false : true;
        ui_selection.ui_toggle_show_only_favourites.onValueChanged.AddListener(delegate { Listener_UI_Toggle_Show_Only_Favourites_OnValueChanged(ui_selection.ui_toggle_show_only_favourites, ui_Selection_Type); });


        ui_selection.ui_prefab_selection = (GameObject)Resources.Load("Prefabs/UI_Selection_Panel", typeof(GameObject));

        if (ui_Selection_Type == Ui_Selection_Type.helicopter)
            ui_helicopter_selection = ui_selection;
        if (ui_Selection_Type == Ui_Selection_Type.scenery)
            ui_scenery_selection = ui_selection;
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


    // ##################################################################################
    // update every parameter prefab on ui
    // ##################################################################################
    public void UI_Update_Helicopter_Or_Scenery_Selection_Panel_UI(Ui_Selection_Type ui_Selection_Type, UI_Helicopter_Or_Scenery_Selection ui_selection)
    {   
        // remove all children form Scroll View Content UI
        foreach (Transform child in ui_selection.ui_scroll_view_content)
           GameObject.Destroy(child.gameObject);


        // helicopter: get information xml and fill list on ui  
        if (ui_Selection_Type == Ui_Selection_Type.helicopter)
        {
            helicopter__prefabs_vertical_position_on_scroll_view_ui = 0;
            int selection_id = 0;

            foreach (Transform each_helicopter in helicopters_available.transform)
            {
                string selection_name = each_helicopter.name;

                // only show helicopter in list, which are marked as favourit when filter toggle "ui_toggle_show_only_favourites.isOn == true" is active
                if (!(ui_selection.ui_toggle_show_only_favourites.isOn == true && (PlayerPrefs.GetInt("SavedSetting____" + selection_name + "____is_helicopter_favourite", 0) == 0 ? false : true) == false ) )
                {
                    // import xml
                    helicopter_selection_content = new stru_selection_content();
                    TextAsset txtAsset = (TextAsset)Resources.Load(each_helicopter.name + "_information_file", typeof(TextAsset));
                    if (txtAsset != null)
                         helicopter_selection_content = Common.Helper.IO_XML_Deserialize<stru_selection_content>(txtAsset);

                    //Common.Helper.IO_XML_Serialize(txtAsset,"c:/Temp/test1.txt");

                    // create ui-prefab instance
                    GameObject prefab_instance = Instantiate_Selection_Element_Prefab(ui_selection, ref helicopter__prefabs_vertical_position_on_scroll_view_ui);

                    // setup instanced prefab (settings and listeners)
                    Setup_Selection_Element_Instance(ref prefab_instance, ui_Selection_Type, selection_id, selection_name, helicopter_selection_content, null);
                }
                selection_id++;
            }

            // resize rect-transform area now to fit all UI-prefabs into
            ui_selection.ui_scroll_view_content.GetComponent<RectTransform>().sizeDelta = new Vector2(0, helicopter__prefabs_vertical_position_on_scroll_view_ui + 20);
        }



        // scenery: get information xml and fill list on ui  
        if (ui_Selection_Type == Ui_Selection_Type.scenery)
        {
            scenery__prefabs_vertical_position_on_scroll_view_ui = 0;
            for (int selection_id = 0; selection_id < list_skymap_paths.Count; selection_id++)
            {
                string selection_name = list_skymap_paths[selection_id].name;

                // only show scenery in list, which are marked as favourit when filter toggle "ui_toggle_show_only_favourites.isOn == true" is active
                if (!(ui_selection.ui_toggle_show_only_favourites.isOn == true && (PlayerPrefs.GetInt("SavedSetting____" + selection_name + "____is_scenery_favourite", 0) == 0 ? false : true) == false))
                {
                    // import xml
                    scenery_selection_content = new stru_selection_content();
                    if (File.Exists(list_skymap_paths[selection_id].fullpath_information_file))
                        scenery_selection_content = Common.Helper.IO_XML_Deserialize<stru_selection_content>(list_skymap_paths[selection_id].fullpath_information_file);

                    //Common.Helper.IO_XML_Serialize(scenery_selection_content, "c:/Temp/test2.txt");

                    // create ui-prefab instance
                    GameObject prefab_instance = Instantiate_Selection_Element_Prefab(ui_selection, ref scenery__prefabs_vertical_position_on_scroll_view_ui);

                    // setup instanced prefab (settings and listeners)
                    Setup_Selection_Element_Instance(ref prefab_instance, ui_Selection_Type, selection_id, selection_name, scenery_selection_content, list_skymap_paths[selection_id]);
                }
            }

            // resize rect-transform area now to fit all UI-prefabs into
            ui_selection.ui_scroll_view_content.GetComponent<RectTransform>().sizeDelta = new Vector2(0, scenery__prefabs_vertical_position_on_scroll_view_ui + 20);
        }

    }
    // ##################################################################################




    // ##################################################################################
    // instantiate prefab
    // ##################################################################################
    GameObject Instantiate_Selection_Element_Prefab( UI_Helicopter_Or_Scenery_Selection ui_selection, ref int prefabs_vertical_position_on_scroll_view_ui)
    {
        GameObject prefab_instance = Instantiate(ui_selection.ui_prefab_selection);
        prefab_instance.transform.SetParent(ui_selection.ui_scroll_view_content, true);
        prefab_instance.transform.localScale = new Vector3(1, 1, 1);
        prefab_instance.transform.localPosition = new Vector3(10, -prefabs_vertical_position_on_scroll_view_ui, 0);
        prefabs_vertical_position_on_scroll_view_ui += 150;

        return prefab_instance;
    }
    // ##################################################################################




    // ##################################################################################
    // setup prefab instance
    // ##################################################################################
    void Setup_Selection_Element_Instance(ref GameObject prefab_instance, Ui_Selection_Type ui_Selection_Type, int selection_id, 
        string selection_name, stru_selection_content selection_content, stru_skymap_paths stru_skymap_path)
    {
        // object's favourite toggle 
        Toggle favourite_toggle = prefab_instance.transform.Find("Toggle_Favourite").GetComponent<Toggle>();

        Texture2D texture2D;

        // setup favourite toggle state
        if (ui_Selection_Type == Ui_Selection_Type.scenery)
        {
            favourite_toggle.isOn = PlayerPrefs.GetInt("SavedSetting____" + selection_name + "____is_scenery_favourite", 0) == 0 ? false : true;

            // load preview image
            texture2D = Load_Image(list_skymap_paths[selection_id].fullpath_preview_image);
            if (texture2D != null)
                prefab_instance.transform.Find("Image").GetComponent<Image>().sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0, 0), 1);

        }
        if (ui_Selection_Type == Ui_Selection_Type.helicopter)
        {
            favourite_toggle.isOn = PlayerPrefs.GetInt("SavedSetting____" + selection_name + "____is_helicopter_favourite", 0) == 0 ? false : true;

            // load preview image
            texture2D = (Texture2D)Resources.Load(selection_name + "_preview_image", typeof(Texture2D));
            if (texture2D != null)
                prefab_instance.transform.Find("Image").GetComponent<Image>().sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0, 0), 1); 
        }

        // add listener to favourite toggle
        favourite_toggle.onValueChanged.AddListener(delegate { Listener_UI_Toggle_Selection_Favourite_OnValueChanged(ui_Selection_Type, selection_name, favourite_toggle); });

        // fill ui-prefab instance with information
        prefab_instance.transform.Find("Text_Name").GetComponent<Text>().text = selection_content.name;
        prefab_instance.transform.Find("Text_Author").GetComponent<Text>().text = selection_content.author;
        prefab_instance.transform.Find("Text_Date").GetComponent<Text>().text = selection_content.date;
        prefab_instance.transform.Find("Text_Version").GetComponent<Text>().text = selection_content.version;
        prefab_instance.transform.Find("Text_Info").GetComponent<Text>().text = selection_content.info;
        Button open_link_button = prefab_instance.transform.Find("Button Open Link").GetComponent<Button>();
        if (selection_content.homepagelink != "-")
        {
            open_link_button.gameObject.SetActive(true);
            open_link_button.onClick.AddListener(delegate { Application.OpenURL(selection_content.homepagelink); });
        }
        else
        {
            open_link_button.gameObject.SetActive(false);
        }


        if (ui_Selection_Type == Ui_Selection_Type.scenery)
        {
            string scenery_name = selection_name;
            string fullpath_scenery_folder;
            if (((Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)) &&
                helicopter_ODE.par_temp.simulation.various.sceneries_file_location.val == 0)
                fullpath_scenery_folder = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Skymaps"), scenery_name); // Windows
            else
                fullpath_scenery_folder = Path.Combine(Path.Combine(Application.persistentDataPath, "Skymaps"), scenery_name); // Only choice on MacIO, ...

            Button download_button = prefab_instance.transform.Find("Button Download").GetComponent<Button>();
            Button select_button = prefab_instance.transform.Find("Button Select").GetComponent<Button>();
            Image image = prefab_instance.transform.Find("Image").GetComponent<Image>();
           
            // add unique name to be able to select it by name in static coroutine Download_And_Import_Skybox()
            download_button.name = "Button Download " + scenery_name;
            select_button.name = "Button Select " + scenery_name;
            image.name = "Image " + scenery_name;

            // if a download link is given in i.e. 'Ahornkopf_information_file.xml' ...
            //if (selection_content.downloadlink != "not downloadable" &&
            //    !IO_Heli_X.Heli_X_Import.Check_Local_Scenery_Files(fullpath_scenery_folder))       
            if ( stru_skymap_path.is_downloadable &&
                !stru_skymap_path.is_downloaded)       
            {
                //... then show download button
                download_button.gameObject.GetComponentInChildren<Text>().text = "Download";
                download_button.gameObject.SetActive(true);

                // and add button listener with download function
                download_button.onClick.AddListener(delegate { IO_Heli_X.Heli_X_Import.Get_Skybox(selection_content.downloadlink, fullpath_scenery_folder, this, download_button, ui_canvas, stru_skymap_path); });

                // change preview image appearance, to show that scenery is not available (change alpha)
                var tempColor = image.color; tempColor.a = 0.2f; image.color = tempColor;

                // we also do not need the selection button
                prefab_instance.transform.Find("Button Select " + scenery_name).GetComponent<Button>().gameObject.SetActive(false);
            }

            if (stru_skymap_path.is_downloadable &&
                stru_skymap_path.is_downloaded)
            {
                // hide download button
                download_button.gameObject.GetComponentInChildren<Text>().text = "Download Again";
                download_button.gameObject.SetActive(true);

                // and add button listener with download function
                download_button.onClick.AddListener(delegate { IO_Heli_X.Heli_X_Import.Get_Skybox(selection_content.downloadlink, fullpath_scenery_folder, this, download_button, ui_canvas, stru_skymap_path); });
            }

            // add listener to button
            prefab_instance.transform.Find("Button Select " + scenery_name).GetComponent<Button>().onClick.AddListener(delegate { Listener_UI_Button_Selection_Select_OnClick(ui_Selection_Type, selection_id); });
        }


        if (ui_Selection_Type == Ui_Selection_Type.helicopter)
        {
            // selected object from list
            prefab_instance.transform.Find("Button Select").GetComponent<Button>().onClick.AddListener(delegate { Listener_UI_Button_Selection_Select_OnClick(ui_Selection_Type, selection_id); });
        }
        //prefab_instance.transform.Find("Text_Mass").GetComponent<Text>().text = helicopter_ODE.par.transmitter_and_helicopter.helicopter.mass_total.val + " kg";
        //prefab_instance.transform.Find("Text_Rotor_Diameter").GetComponent<Text>().text = helicopter_ODE.par.transmitter_and_helicopter.helicopter.mainrotor.R.val + " m";
    }
    // ##################################################################################




    // ##################################################################################
    // Listener for prefab's onklick for selection of helicopter or scenery
    // ##################################################################################
    void Listener_UI_Button_Selection_Select_OnClick(Ui_Selection_Type ui_Selection_Type, int selection_id)
    {
        if (ui_Selection_Type == Ui_Selection_Type.scenery)
        {
            active_scenery_id = selection_id;

            load_skymap_state = State_Load_Skymap.prepare_starting;

            ui_scenery_selection_menu_flag = false;
        }
        if (ui_Selection_Type == Ui_Selection_Type.helicopter)
        {

            active_helicopter_id = selection_id;

            Load_Helicopter(ref active_helicopter_id);

            // reset model to initial position
            Reset_Simulation_States();

            UI_Update_Parameter_Settings_UI();

            Pause_ODE(gl_pause_flag = false);

            ui_helicopter_selection_menu_flag = false;
        }
    }
    // ##################################################################################




    // ##################################################################################
    // Listener for prefab's favourite
    // ##################################################################################
    void Listener_UI_Toggle_Selection_Favourite_OnValueChanged(Ui_Selection_Type ui_Selection_Type, string selection_name, Toggle favourite_toggle)
    {
        if (ui_Selection_Type == Ui_Selection_Type.scenery)
        {  
            PlayerPrefs.SetInt("SavedSetting____" + selection_name + "____is_scenery_favourite", favourite_toggle.isOn ? 1 : 0);

            UI_Update_Helicopter_Or_Scenery_Selection_Panel_UI(Ui_Selection_Type.scenery, ui_scenery_selection);
        }
        if (ui_Selection_Type == Ui_Selection_Type.helicopter)
        {
            PlayerPrefs.SetInt("SavedSetting____" + selection_name + "____is_helicopter_favourite", favourite_toggle.isOn ? 1 : 0);

            UI_Update_Helicopter_Or_Scenery_Selection_Panel_UI(Ui_Selection_Type.helicopter, ui_helicopter_selection);
        }
    }
    // ##################################################################################




    // ##################################################################################
    // Listener for panels "show only favourites" toggle
    // ##################################################################################
    void Listener_UI_Toggle_Show_Only_Favourites_OnValueChanged(Toggle toggle, Ui_Selection_Type ui_Selection_Type)
    {
        string type_setup = null;
        if (ui_Selection_Type == Ui_Selection_Type.helicopter)
        {  
            type_setup = "Helicopter";
            UI_Update_Helicopter_Or_Scenery_Selection_Panel_UI(ui_Selection_Type, ui_helicopter_selection);
        }
        if (ui_Selection_Type == Ui_Selection_Type.scenery)
        {
            type_setup = "Scenery";
            UI_Update_Helicopter_Or_Scenery_Selection_Panel_UI(ui_Selection_Type, ui_scenery_selection);
        }
           
        PlayerPrefs.SetInt("SavedSetting____" + type_setup + "____Toggle_Show_Only_Favourites", toggle.isOn ? 1 : 0 );
    }
    // ##################################################################################

    #endregion




}



