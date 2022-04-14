using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Common;

using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Globalization;
// http://xmltocsharp.azurewebsites.net/

using System.Linq;
using System.Xml.Linq;
using UnityEngine.Networking;

using UnityEngine.UI;



// ##################################################################################
//                                                                                                                                                                                                                    
//    HHHHHHHHH     HHHHHHHHH                   lllllll   iiii                   XXXXXXX       XXXXXXX     IIIIIIIIII                                                                                          tttt          
//    H:::::::H     H:::::::H                   l:::::l  i::::i                  X:::::X       X:::::X     I::::::::I                                                                                       ttt:::t          
//    H:::::::H     H:::::::H                   l:::::l   iiii                   X:::::X       X:::::X     I::::::::I                                                                                       t:::::t          
//    HH::::::H     H::::::HH                   l:::::l                          X::::::X     X::::::X     II::::::II                                                                                       t:::::t          
//      H:::::H     H:::::H      eeeeeeeeeeee    l::::l iiiiiii                  XXX:::::X   X:::::XXX       I::::I     mmmmmmm    mmmmmmm   ppppp   ppppppppp      ooooooooooo   rrrrr   rrrrrrrrr   ttttttt:::::ttttttt    
//      H:::::H     H:::::H    ee::::::::::::ee  l::::l i:::::i                     X:::::X X:::::X          I::::I   mm:::::::m  m:::::::mm p::::ppp:::::::::p   oo:::::::::::oo r::::rrr:::::::::r  t:::::::::::::::::t    
//      H::::::HHHHH::::::H   e::::::eeeee:::::eel::::l  i::::i                      X:::::X:::::X           I::::I  m::::::::::mm::::::::::mp:::::::::::::::::p o:::::::::::::::or:::::::::::::::::r t:::::::::::::::::t    
//      H:::::::::::::::::H  e::::::e     e:::::el::::l  i::::i  ---------------      X:::::::::X            I::::I  m::::::::::::::::::::::mpp::::::ppppp::::::po:::::ooooo:::::orr::::::rrrrr::::::rtttttt:::::::tttttt    
//      H:::::::::::::::::H  e:::::::eeeee::::::el::::l  i::::i  -:::::::::::::-      X:::::::::X            I::::I  m:::::mmm::::::mmm:::::m p:::::p     p:::::po::::o     o::::o r:::::r     r:::::r      t:::::t          
//      H::::::HHHHH::::::H  e:::::::::::::::::e l::::l  i::::i  ---------------     X:::::X:::::X           I::::I  m::::m   m::::m   m::::m p:::::p     p:::::po::::o     o::::o r:::::r     rrrrrrr      t:::::t          
//      H:::::H     H:::::H  e::::::eeeeeeeeeee  l::::l  i::::i                     X:::::X X:::::X          I::::I  m::::m   m::::m   m::::m p:::::p     p:::::po::::o     o::::o r:::::r                  t:::::t          
//      H:::::H     H:::::H  e:::::::e           l::::l  i::::i                  XXX:::::X   X:::::XXX       I::::I  m::::m   m::::m   m::::m p:::::p    p::::::po::::o     o::::o r:::::r                  t:::::t    tttttt
//    HH::::::H     H::::::HHe::::::::e         l::::::li::::::i                 X::::::X     X::::::X     II::::::IIm::::m   m::::m   m::::m p:::::ppppp:::::::po:::::ooooo:::::o r:::::r                  t::::::tttt:::::t
//    H:::::::H     H:::::::H e::::::::eeeeeeee l::::::li::::::i                 X:::::X       X:::::X     I::::::::Im::::m   m::::m   m::::m p::::::::::::::::p o:::::::::::::::o r:::::r                  tt::::::::::::::t
//    H:::::::H     H:::::::H  ee:::::::::::::e l::::::li::::::i                 X:::::X       X:::::X     I::::::::Im::::m   m::::m   m::::m p::::::::::::::pp   oo:::::::::::oo  r:::::r                    tt:::::::::::tt
//    HHHHHHHHH     HHHHHHHHH    eeeeeeeeeeeeee lllllllliiiiiiii                 XXXXXXX       XXXXXXX     IIIIIIIIIImmmmmm   mmmmmm   mmmmmm p::::::pppppppp       ooooooooooo    rrrrrrr                      ttttttttttt  
//                                                                                                                                            p:::::p                                                                        
//                                                                                                                                            p:::::p                                                                        
//                                                                                                                                           p:::::::p                                                                       
//                                                                                                                                           p:::::::p                                                                       
//                                                                                                                                           p:::::::p                                                                       
//                                                                                                                                           ppppppppp
// ##################################################################################  
// IMport functions for Heli-X format
// ################################################################################## 


// ################################################################################## 
// Heli-X scenery settings file format (maybe incomplete)
// ################################################################################## 
namespace IO_Heli_X
{


    // ################################################################################## 
    // Heli-X format import class
    // ################################################################################## 
    public class Heli_X_Import
	{



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
        //  static methods                                                                                                                                     
        // ############################################################################          
        #region heli_x_methods

        // ################################################################################## 
        /// <summary>
        /// Start to download scenery file
        /// </summary>
        /// <param name="www_path"></param>
        /// <param name="fullpath_scenery_folder"></param>
        /// <param name="justToStartCoroutine"></param>
        // ################################################################################## 
        public static void Get_Skybox(string www_path, string fullpath_scenery_folder, MonoBehaviour justToStartCoroutine, Button download_button, GameObject ui_canvas, Helicopter_Main.stru_skymap_paths stru_skymap_path)
		{
            if (!Check_Local_Scenery_Files(fullpath_scenery_folder))
			{
                //UnityEngine.Debug.Log("Downloading file");
                download_button.interactable = false;

                justToStartCoroutine.StartCoroutine( Download_And_Import_Skybox(www_path, fullpath_scenery_folder, justToStartCoroutine, ui_canvas) );
            }
			else
            {
                download_button.interactable = false;

                justToStartCoroutine.StartCoroutine(Download_And_Import_Skybox(www_path, fullpath_scenery_folder, justToStartCoroutine, ui_canvas));

                //UnityEngine.Debug.Log("Files already in local directory. No need to download.");
            }
		}
        // ################################################################################## 




        // ################################################################################## 
        // Test, if files are already stored in local storage
        // See also Check_Skymaps() in Helicopter_main.cs method
        // ################################################################################## 
        public static bool Check_Local_Scenery_Files(string fullpath_scenery_folder)
		{
			string filename_scenery_file = Path.GetFileName(fullpath_scenery_folder); // heli-x uses foldername as name for setting file

			// Check if file exists not knowing the extension
			if( (Directory.GetFiles(fullpath_scenery_folder + "\\4096\\", filename_scenery_file + "_back.*")).Length > 0 &&
				(Directory.GetFiles(fullpath_scenery_folder + "\\4096\\", filename_scenery_file + "_left.*")).Length > 0 &&
				(Directory.GetFiles(fullpath_scenery_folder + "\\4096\\", filename_scenery_file + "_right.*")).Length > 0 &&
				(Directory.GetFiles(fullpath_scenery_folder + "\\4096\\", filename_scenery_file + "_front.*")).Length > 0 &&
				(Directory.GetFiles(fullpath_scenery_folder + "\\4096\\", filename_scenery_file + "_bottom.*")).Length > 0 &&
				(Directory.GetFiles(fullpath_scenery_folder + "\\4096\\", filename_scenery_file + "_top.*")).Length > 0 &&
				(System.IO.File.Exists(fullpath_scenery_folder + "\\collision_object.obj")))
				return true;
			else
				return false;
		}
        // ################################################################################## 




        // ################################################################################## 
        // downloading zip-files from internet, unzip, copy and convert the collision files to .obj 
        // https://forum.unity.com/threads/downloading-progress.184353/
        // ################################################################################## 
        static IEnumerator Download_And_Import_Skybox(string url, string fullpath_scenery_folder, MonoBehaviour justToStartCoroutine, GameObject ui_canvas)
		{
			//UnityEngine.Debug.Log("Downloading file...");

			UnityWebRequest www = new UnityWebRequest(url);
			www.downloadHandler = new DownloadHandlerBuffer();

			justToStartCoroutine.StartCoroutine( Download_Progress(www, Path.GetFileName(fullpath_scenery_folder), justToStartCoroutine, ui_canvas) );

			yield return www.SendWebRequest();
			if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
			{
				//UnityEngine.Debug.Log(www.error);// TODO error handling
			}
			else
			{
                // The zip file has to be extracted temporaly, because we do not need every file. The rest is deleted again.
                string fullpath_temporary_folder = Path.GetTempPath();

                // Retrieve results as binary data
                byte[] results = www.downloadHandler.data;

				//Path.Combine(Application.streamingAssetsPath, "Skymaps\\" + filename_scenery_file);
				string scenery_name = Path.GetFileName(fullpath_scenery_folder); // heli-x uses foldername as name for setting file

                // (the data in the unziped files are stored in subfolders: i.e. \SkyBox\Ahornkopf\)
                string subfolder = Path.Combine("SkyBox", scenery_name);

                // because of too long path names (260 characters limit) the zip file is extracted to the temp directory
                //string fullpath_scenery_file_zipped = @"\\?\" + fullpath_scenery_folder + "\\" + filename_scenery_file + ".zip";
                if (!System.IO.Directory.Exists(fullpath_temporary_folder))
                    System.IO.Directory.CreateDirectory(fullpath_temporary_folder);
                 
                //string fullpath_scenery_file_zipped = @"\\?\" + fullpath_temporary_folder + scenery_name + ".zip";
                //fullpath_scenery_file_zipped = @fullpath_scenery_file_zipped.Replace('/', '\\');
                string fullpath_scenery_file_zipped = Path.Combine(fullpath_temporary_folder, scenery_name + ".zip");

                string fullpath_scenery_file_unzipped = fullpath_temporary_folder;
                
                if( System.IO.Directory.Exists(Path.Combine(fullpath_temporary_folder, subfolder)))
                    System.IO.Directory.Delete(Path.Combine(fullpath_temporary_folder, subfolder), true);

                // store data from memory
                System.IO.File.WriteAllBytes(fullpath_scenery_file_zipped, results);

                //UnityEngine.Debug.Log("Download finished, unzipping file: " + fullpath_scenery_file_zipped);

                // use short path names to avoid 260 / 248 character limitation
                if (fullpath_scenery_file_zipped.Length > 247)
                    UnityEngine.Debug.Log("Scenery's downloaded zipped file path is too long (" + fullpath_scenery_file_zipped.Length.ToString() + ")");

                if (System.IO.File.Exists(fullpath_scenery_file_zipped))
				{
					// unzip the downloaded file
					// https://forum.unity.com/threads/extracting-zip-files.472537/	
                    try
                    {
                        System.IO.Compression.ZipFile.ExtractToDirectory(fullpath_scenery_file_zipped, fullpath_scenery_file_unzipped);
                    }
                    catch (System.ArgumentException ex)
                    {
                        //UnityEngine.Debug.Log("\n" + "ZipFile Error\n" + ex.ToString());
                        //return;
                        // TODO handle error
                    }

					// copy the skybox images
					string sourceDirectory = Path.Combine(Path.Combine(fullpath_scenery_file_unzipped, subfolder),"4096");
					string destinationDirectory = Path.Combine(fullpath_scenery_folder, "4096");
					if (System.IO.Directory.Exists(sourceDirectory))
                        Directory_Copy(sourceDirectory, destinationDirectory, true);

					// copy eula german version
					string sourceFileName = Path.Combine(Path.Combine(fullpath_scenery_file_unzipped, subfolder), "EULA.txt");
					string destFileName = Path.Combine(fullpath_scenery_folder, "EULA.txt");
					if (System.IO.File.Exists(sourceFileName))
						File.Copy(sourceFileName, destFileName, true);

                    // copy eula englisch 
                    sourceFileName = Path.Combine(Path.Combine(fullpath_scenery_file_unzipped, subfolder), "EULA_en.txt");
                    destFileName = Path.Combine(fullpath_scenery_folder, "EULA_en.txt");
                    if (System.IO.File.Exists(sourceFileName))
                        File.Copy(sourceFileName, destFileName, true);

                    // the collision files are formated to .obj and do not need to be copied
                    Heli_X_Scenery_Setting_Xml heli_x_scenery_setting_xml = new Heli_X_Scenery_Setting_Xml();
                    string filename_settings_file = Path.GetFileName(Path.Combine(fullpath_scenery_file_unzipped, subfolder)); // heli-x uses foldername as name for setting file
                    string fullpath_settings_file = Path.Combine(fullpath_scenery_file_unzipped, subfolder) + "\\" + filename_settings_file + ".xml";
                    if (System.IO.File.Exists(fullpath_settings_file))
                        Convert_Heli_X_Topography_To_Wavefront_Obj(Path.Combine(fullpath_scenery_file_unzipped, subfolder), fullpath_scenery_folder, ref heli_x_scenery_setting_xml);
                    //else
                        // scenery is not a heli-x scenery, therefore no need for conversion

                    // update the ui - TODO only if success
                    //Helicopter_Main.UI_Update_Helicopter_Or_Scenery_Selection_Panel_UI( Helicopter_Main.Ui_Selection_Type.scenery, ui_scenery_selection);
                    foreach (Transform transform in ui_canvas.transform.Find("UI Info Menu Scenery Selection/List Panel/Scroll View/Viewport/Content").GetComponentsInChildren<Transform>(true))
                    {
                        //if (transform.name == "Button Download " + scenery_name) { transform.gameObject.SetActive(false);} // hide Button Download
                        if (transform.name == "Button Select " + scenery_name) { transform.gameObject.SetActive(true);} // reset Button Select
                        if (transform.name == "Image " + scenery_name) {
                            Image image = transform.GetComponent<Image>();
                            var tempColor = image.color; tempColor.a = 1.0f; image.color = tempColor; // reset image alpha
                        }
                    }

                    if (System.IO.Directory.Exists(Path.Combine(fullpath_temporary_folder, subfolder)))
                        System.IO.Directory.Delete(Path.Combine(fullpath_temporary_folder, subfolder), true);
                }
			}

			yield return null;
		}
        // ################################################################################## 




        // ################################################################################## 
        // Display download status in button's text
        // ################################################################################## 
        static IEnumerator Download_Progress(UnityWebRequest req, string scenery_name, MonoBehaviour justToStartCoroutine, GameObject ui_canvas)
		{
            while (!req.isDone)
			{
                // find the trigger button by its name (the button is destroyed during updating the ui, thus no direct connection to the button-object worked), and add the download percentage value to its text
                Transform download_button = null;
                foreach (Transform g in ui_canvas.transform.Find("UI Info Menu Scenery Selection/List Panel/Scroll View/Viewport/Content").GetComponentsInChildren<Transform>())
                    if (g.name == "Button Download " + scenery_name) { download_button = g; break; }

                if(download_button != null)
                {
                    // make the button not interactable
                    download_button.GetComponent<Button>().interactable = false;

                    // show download status in %
                    if (req.downloadProgress == -1)
                        download_button.GetComponentInChildren<Text>().text = (0).ToString("0") + "%";
                    else
                        download_button.GetComponentInChildren<Text>().text = (req.downloadProgress * 100).ToString("0") + "%";
                }
                yield return new WaitForSeconds(0.2f);
            }

            // when finished
            if (req.isDone)
            {
                // find the trigger button by its name (the button is destroyed during updating the ui, thus no direct connetction to the button-object worked)
                Transform download_button = null;
                foreach (Transform g in ui_canvas.transform.Find("UI Info Menu Scenery Selection/List Panel/Scroll View/Viewport/Content").GetComponentsInChildren<Transform>())
                    if (g.name == "Button Download " + scenery_name) { download_button = g; break; }

                // change button text
                if (download_button != null)
                {
                    //download_button.GetComponentInChildren<Text>().text = "Download Finished";
                    download_button.GetComponentInChildren<Text>().text = "Download Again";
                    download_button.GetComponent<Button>().interactable = true; 
                }
            }
        }
        // ################################################################################## 




        // ################################################################################## 
        /// <summary>
        /// Convert downloaded topography files to wavefront .obj files
        /// </summary>
        /// <param name="fullpath_source_folder"></param>
        /// <param name="fullpath_scenery_folder"></param>
        /// <param name="heli_x_scenery_setting_xml"></param>
        // ################################################################################## 
        private static void Convert_Heli_X_Topography_To_Wavefront_Obj(string fullpath_source_folder, string fullpath_scenery_folder, 
			ref Heli_X_Scenery_Setting_Xml heli_x_scenery_setting_xml)
		{
			// get heli-x main scenery settings file
			heli_x_scenery_setting_xml = Load_Heli_X_Skybox_Setup_File(fullpath_source_folder);

			// convert all collision/topography files to a single liste of triangles
			List<Heli_X_Triangle> heli_x_triangles = new List<Heli_X_Triangle>();
			foreach (string topography_file in heli_x_scenery_setting_xml.TopographyFile)
				Load_Heli_X_Collision_Topography_Files(fullpath_source_folder + "\\" + topography_file, ref heli_x_triangles);
	
			// after all collision/topography files are converted to one singe list of triangles, convert them to .obj
			Export_Heli_X_Collision_Object_As_Wavefront_Obj(fullpath_scenery_folder, heli_x_triangles);
		}
        // ################################################################################## 




        // ################################################################################## 
        // import the heli-x scenery settings file
        // ################################################################################## 
        private static Heli_X_Scenery_Setting_Xml Load_Heli_X_Skybox_Setup_File(string fullpath_scenery)
		{
			string filename_settings_file = Path.GetFileName(fullpath_scenery); // heli-x uses foldername as name for setting file
			string fullpath_settings_file = fullpath_scenery + "\\" + filename_settings_file + ".xml";
			//UnityEngine.Debug.Log("fullpath_settings_file " + fullpath_settings_file);

			return Helper.IO_XML_Deserialize<Heli_X_Scenery_Setting_Xml>(fullpath_settings_file);
			//UnityEngine.Debug.Log("HeightOfEyes " + Heli_X_xml.HeightOfEyes  +  "  " + float.Parse(Heli_X_xml.HeightOfEyes, CultureInfo.InvariantCulture) );
		}
        // ################################################################################## 




        // ################################################################################## 
        // import and interpret the heli-x topography(collision) files 
        // https://stackoverflow.com/questions/58153701/in-an-xml-xsd-schema-file-how-can-we-extract-parse-certain-information-from-a
        // ################################################################################## 
        private static void Load_Heli_X_Collision_Topography_Files(string fullpath, ref List<Heli_X_Triangle> heli_x_triangles)
		{
			XDocument xdocument = XDocument.Load(@fullpath);

			//var xdocument = xmlDoc.ToXDocument();
			foreach (var element in xdocument.Elements())
			{
				//UnityEngine.Debug.Log("1 Name: " + element.Name + " Value: " + element.Value);
				foreach (var element_1 in element.Elements()) 
				{
					if (element_1.Name.LocalName.Equals("Triangle"))
					{
                        try
                        {
                            // get triangle attributes 
                            var heli_x_triangle = new Heli_X_Triangle();
                            foreach (var element_2 in element_1.Attributes())
                            {
                                if (element_2.Name == "FrictionFactor")
                                    heli_x_triangle.attributes.FrictionFactor = float.Parse(element_2.Value, CultureInfo.InvariantCulture);
                                if (element_2.Name == "CrashSensitivityFactor")
                                    heli_x_triangle.attributes.CrashSensitivityFactor = float.Parse(element_2.Value, CultureInfo.InvariantCulture);
                                if (element_2.Name == "CrashSensitiThicknessvityFactor")
                                    heli_x_triangle.attributes.Thickness = float.Parse(element_2.Value, CultureInfo.InvariantCulture);
                                if (element_2.Name == "Alpha")
                                    heli_x_triangle.attributes.Alpha = float.Parse(element_2.Value, CultureInfo.InvariantCulture);
                                if (element_2.Name == "MakeCrashObject")
                                    heli_x_triangle.attributes.MakeCrashObject = bool.Parse(element_2.Value);
                                if (element_2.Name == "ShadowReceiver")
                                    heli_x_triangle.attributes.ShadowReceiver = bool.Parse(element_2.Value);
                                if (element_2.Name == "CeilingType")
                                    heli_x_triangle.attributes.CeilingType = int.Parse(element_2.Value, CultureInfo.InvariantCulture);
                                if (element_2.Name == "GroundType")
                                {
                                    if (element_2.Value == "true" || element_2.Value == "false")
                                        heli_x_triangle.attributes.GroundType = bool.Parse(element_2.Value) ? 1 : 0;
                                    else
                                        heli_x_triangle.attributes.GroundType = int.Parse(element_2.Value, CultureInfo.InvariantCulture);
                                }
                                //UnityEngine.Debug.Log("2 Name: " + element_2.Name + " Value: " + element_2.Value);
                            }

                            // get triangle points and normal
                            int i = 0;
                            foreach (var element_2 in element_1.Elements())
                            {
                                //if (i == 0) heli_x_triangle.data.point0 = Parse_Heli_X_To_Vector3(element_2.Value);
                                //if (i == 1) heli_x_triangle.data.point1 = Parse_Heli_X_To_Vector3(element_2.Value);
                                //if (i == 2) heli_x_triangle.data.point2 = Parse_Heli_X_To_Vector3(element_2.Value);
                                //if (i == 3) heli_x_triangle.data.normal = Vector3.Normalize(Parse_Heli_X_To_Vector3(element_2.Value));

                                // flip normal with changing the order of triangles (flipping normals itself didnt's show an effect)
                                if (i == 0) heli_x_triangle.data.point2 = Parse_Heli_X_To_Vector3(element_2.Value);
                                if (i == 1) heli_x_triangle.data.point1 = Parse_Heli_X_To_Vector3(element_2.Value);
                                if (i == 2) heli_x_triangle.data.point0 = Parse_Heli_X_To_Vector3(element_2.Value);
                                if (i == 3) heli_x_triangle.data.normal = Vector3.Normalize(Parse_Heli_X_To_Vector3(element_2.Value)) * -1.0f; // flip normal

                                i++;

                                //UnityEngine.Debug.Log("2 Name: " + element_2.Name + " Value: " + element_2.Value);
                            }

                            heli_x_triangles.Add(heli_x_triangle);
                        }
                        catch
                        {
                            UnityEngine.Debug.Log("error ");
                        }
					}
				}

				/*
				UnityEngine.Debug.Log("point0 " + String.Format("{0:F3}", heli_x_triangles[heli_x_triangles.Count - 1].data.point0));
				UnityEngine.Debug.Log("point1 " + String.Format("{0:F3}", heli_x_triangles[heli_x_triangles.Count - 1].data.point1));
				UnityEngine.Debug.Log("point2 " + String.Format("{0:F3}", heli_x_triangles[heli_x_triangles.Count - 1].data.point2));
				UnityEngine.Debug.Log("normal " + String.Format("{0:F3}", heli_x_triangles[heli_x_triangles.Count - 1].data.normal[0]));
				*/
			}
		}
        // ################################################################################## 




        // ################################################################################## 
        // parse and convert heli-x string as Vector3
        // ################################################################################## 
        static private Vector3 Parse_Heli_X_To_Vector3(string str)
		{
			var sStrings = str.Split(","[0]);
			float x = float.Parse(sStrings[0], CultureInfo.InvariantCulture);
			float y = float.Parse(sStrings[1], CultureInfo.InvariantCulture);
			float z = float.Parse(sStrings[2], CultureInfo.InvariantCulture);
			return new Vector3(x,y,z);
		}
        // ################################################################################## 




        // ################################################################################## 
        // export list of triangles as obj format
        // ################################################################################## 
        public static void Export_Heli_X_Collision_Object_As_Wavefront_Obj(string fullpath_scenery, List<Heli_X_Triangle> heli_x_triangles)
		{
			StringBuilder sb_v = new StringBuilder(); // List of geometric vertices
			StringBuilder sb_vn = new StringBuilder(); // List of vertex normals
			StringBuilder sb_f = new StringBuilder(); // List of polygonal face elements

			int vi = 1; // vertex_counter
			int vn = 1; // face_normal counter
			foreach (Heli_X_Triangle heli_x_triangle in heli_x_triangles)
			{
				if(heli_x_triangle.attributes.MakeCrashObject)
				{ 
					sb_v.Append(string.Format($"v {heli_x_triangle.data.point0.x} {heli_x_triangle.data.point0.y} {heli_x_triangle.data.point0.z}\n").Replace(",", "."));
					sb_v.Append(string.Format($"v {heli_x_triangle.data.point1.x} {heli_x_triangle.data.point1.y} {heli_x_triangle.data.point1.z}\n").Replace(",", ".")); 
					sb_v.Append(string.Format($"v {heli_x_triangle.data.point2.x} {heli_x_triangle.data.point2.y} {heli_x_triangle.data.point2.z}\n").Replace(",", "."));
					sb_vn.Append(string.Format($"vn {heli_x_triangle.data.normal.x} {heli_x_triangle.data.normal.y} {heli_x_triangle.data.normal.z}\n").Replace(",", "."));
					sb_f.Append(string.Format($"f {vi + 0}//{vn} {vi + 1}//{vn} {vi + 2}//{vn}\n"));
					vi+=3; 
					vn++;
				}
			}

			string filename_obj_file = Path.GetFileName(fullpath_scenery); // heli-x uses foldername as name for setting file
			string fullpath_obj_file = fullpath_scenery + "\\collision_object.obj";
			using (StreamWriter sw = new StreamWriter(fullpath_obj_file))
			{
				sw.Write("# Reduced OBJ File. \n" + "# Free-RC-Helicopter-Simulator. \n" + sb_v + sb_vn + "s off\n" + sb_f);
			}
		}
        // ################################################################################## 




        // ################################################################################## 
        // copy complete directory
        // https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories?redirectedfrom=MSDN
        // ##################################################################################
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
			if (!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, true);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					Directory_Copy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}
        // ################################################################################## 
        #endregion


    }
    // ################################################################################## 





    // ################################################################################## 
    //
    // ################################################################################## 
    [XmlRoot(ElementName = "project")]
    public class Heli_X_Scenery_Setting_Xml
    {
        [XmlElement(ElementName = "Version")]
        public string Version { get; set; }
        [XmlElement(ElementName = "HelixRevision")]
        public string HelixRevision { get; set; }
        [XmlElement(ElementName = "Author")]
        public string Author { get; set; }
        [XmlElement(ElementName = "WWW")]
        public string WWW { get; set; }
        [XmlElement(ElementName = "Comment")]
        public string Comment { get; set; }
        [XmlElement(ElementName = "LightDir")]
        public string LightDir { get; set; }
        [XmlElement(ElementName = "ShadowIntensity")]
        public string ShadowIntensity { get; set; }
        [XmlElement(ElementName = "ShadowLambda")]
        public string ShadowLambda { get; set; }
        [XmlElement(ElementName = "WindSpeed")]
        public string WindSpeed { get; set; }
        [XmlElement(ElementName = "WindDirectionDegree")]
        public string WindDirectionDegree { get; set; }
        [XmlElement(ElementName = "TurbulentWindSpeed")]
        public string TurbulentWindSpeed { get; set; }
        [XmlElement(ElementName = "TurbulentTimeConstant")]
        public string TurbulentTimeConstant { get; set; }
        [XmlElement(ElementName = "WindActive")]
        public string WindActive { get; set; }
        [XmlElement(ElementName = "PlaneStartPosition")]
        public string PlaneStartPosition { get; set; }
        [XmlElement(ElementName = "PlaneStartAngle")]
        public string PlaneStartAngle { get; set; }
        [XmlElement(ElementName = "StartPosition")]
        public string StartPosition { get; set; }
        [XmlElement(ElementName = "StartPosition0")]
        public string StartPosition0 { get; set; }
        [XmlElement(ElementName = "StartPosition1")]
        public string StartPosition1 { get; set; }
        [XmlElement(ElementName = "HeightOfEyes")]
        public string HeightOfEyes { get; set; }
        [XmlElement(ElementName = "UseStandardHeightOfGroundPlane")]
        public string UseStandardHeightOfGroundPlane { get; set; }
        [XmlElement(ElementName = "CrashSensitivityFactorGroundPlane")]
        public string CrashSensitivityFactorGroundPlane { get; set; }
        [XmlElement(ElementName = "FrictionFactorGroundPlane")]
        public string FrictionFactorGroundPlane { get; set; }
        [XmlElement(ElementName = "TopographyFile")]
        public List<string> TopographyFile { get; set; }
        [XmlElement(ElementName = "Resolution1024")]
        public string Resolution1024 { get; set; }
        [XmlElement(ElementName = "Resolution2048")]
        public string Resolution2048 { get; set; }
        [XmlElement(ElementName = "Resolution4096")]
        public string Resolution4096 { get; set; }
    }
    // ################################################################################## 




    // ################################################################################## 
    // data structure for imported heli-x data
    // ################################################################################## 
    [Serializable]
	public class Heli_X_Triangle_Attributes
	{
		public float FrictionFactor { get; set; } //  = 1.0 # default value
		public float CrashSensitivityFactor { get; set; } //  = 1.0 # default value
		public float Thickness { get; set; } //  = 0.0 # default value
		public float Alpha { get; set; } //  = 1.0 # default value
		public bool MakeCrashObject { get; set; } //  = 1 # default value
		public bool ShadowReceiver { get; set; } //  = 0 # default value
		public int CeilingType { get; set; } //  = 0 # default value
		public int GroundType { get; set; } // = 0 # default value

		public Heli_X_Triangle_Attributes()
		{
			FrictionFactor = 1.0f;
			CrashSensitivityFactor = 1.0f;
			Thickness = 0.0f;
			Alpha = 0.0f;
			MakeCrashObject = true;
			ShadowReceiver = false;
			CeilingType = 0;
			GroundType = 0;
		}
	}

	[Serializable]
	public class Heli_X_Triangle_Data
	{
		public Vector3 point0 { get; set; } 
		public Vector3 point1 { get; set; }
		public Vector3 point2 { get; set; }
		public Vector3 normal { get; set; }

		public Heli_X_Triangle_Data()
		{
			point0 = new Vector3();
			point1 = new Vector3();
			point2 = new Vector3();
			normal = new Vector3();
		}
	}

	[Serializable]
	public class Heli_X_Triangle
	{
		public Heli_X_Triangle_Attributes attributes { get; set; }
		public Heli_X_Triangle_Data data { get; set; }

		public Heli_X_Triangle()
		{
			attributes = new Heli_X_Triangle_Attributes();
			data = new Heli_X_Triangle_Data();
		}
	}
    // ################################################################################## 


}



