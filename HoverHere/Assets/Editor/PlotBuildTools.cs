// ##################################################################################
// https://forum.unity.com/threads/get-build-number-from-a-script.641725/
// see file Assets/Editor/PlotBuildTools.cs
// see file Assets/Scripting/PlotBuildSettings.cs
// they generate - Build_Settings.asset (Scriptable Object) that can be accessed in game to show the version/build info
//               - Build/Version.txt that is used by InnoSetup when creating an installer (see InnoSetup.iss)
// ##################################################################################
using System;
using PlotKids.Infra;
using UnityEditor;
using Debug = UnityEngine.Debug;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class PlotBuildTools : Editor, IPreprocessBuildWithReport
{
    public int callbackOrder
    {
        get { return 0; }
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log(
            $"[PlotBuildTools] OnPreprocessBuild for target {report.summary.platform} at path {report.summary.outputPath}");

        #region Set Build Properties LastBuildTime - Reference: https: //answers.unity.com/questions/1425758/how-can-i-find-all-instances-of-a-scriptable-objec.html
        //FindAssets uses tags check documentation for more info
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(PlotBuildSettings)}");
        if (guids.Length > 1)
            Debug.LogErrorFormat("[PlotBuildTools] Found more than 1 Build Properties: {0}. Using first one!",
                guids.Length);

        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            PlotBuildSettings buildSettings = AssetDatabase.LoadAssetAtPath<PlotBuildSettings>(path);
            buildSettings.LastBuildTime = DateTime.Now.ToString("yyyy/MM/dd (HH:mm)"); // case sensitive
            buildSettings.EditorRefreshScriptingBackend(report.summary.platform);
            buildSettings.RepositoryVersion = PlotBuildSettings.GetHgVersion();
#if UNITY_CLOUD_BUILD
            buildSettings.RepositoryVersion += "-cloud";
#endif
            EditorUtility.SetDirty(buildSettings);
            Debug.LogFormat("[PlotBuildTools] Updated settings LastBuildDate to \"{0}\". Settings Path: {1}",
                buildSettings.LastBuildTime, path);

            System.IO.File.WriteAllText(@"Build\Version.txt", "version = '" + buildSettings.LastBuildTime + "'");

        }
        else
        {
            // TODO: AUTO-CREATE ONE!
            Debug.LogWarning("[PlotBuildTools] Couldn't find Build Settings, please create one!");
        }
        #endregion
    }
}