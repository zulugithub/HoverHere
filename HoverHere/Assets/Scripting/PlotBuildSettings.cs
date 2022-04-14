// ##################################################################################
// https://forum.unity.com/threads/get-build-number-from-a-script.641725/
// see file Assets/Editor/PlotBuildTools.cs
// see file Assets/Scripting/PlotBuildSettings.cs
// they generate - Build_Settings.asset (Scriptable Object) that can be accessed in game to show the version/build info
//               - Build/Version.txt that is used by InnoSetup when creating an installer (see InnoSetup.iss)
// ##################################################################################
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PlotKids.Infra
{
    [CreateAssetMenu(fileName = "Build_Settings.asset", menuName = "Plot Kids/Build Settings")]
    public class PlotBuildSettings : ScriptableObject
    {
        public string LastBuildTime;
        public string ScriptingBackend;
        public string RepositoryVersion;

#if UNITY_EDITOR

        private void OnEnable()
        {
            RepositoryVersion = GetHgVersion();
            EditorRefreshScriptingBackend(EditorUserBuildSettings.activeBuildTarget);
        }

        public void EditorRefreshScriptingBackend(BuildTarget buildTarget)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);
            
            ScriptingBackend = PlayerSettings.GetScriptingBackend(group).ToString();
        }

        [MenuItem("Tools/Gouda/Pring Hg Version")]
        public static string GetHgVersion()
        {
            try
            {
                bool isWindows = Application.platform == RuntimePlatform.WindowsEditor;
                // Get the short commit hash of the current branch.
                string cmdArguments = isWindows ? "/c hg id -i" : "-c \"hg id -i\"";

                string processName = isWindows ? "cmd" : "/bin/bash";
                var procStartInfo = new System.Diagnostics.ProcessStartInfo(processName, cmdArguments);

                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                procStartInfo.WorkingDirectory = Application.dataPath;
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;

                // Do not create the black window.
                procStartInfo.CreateNoWindow = true;

                // Now we create a process, assign its ProcessStartInfo and start it
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();

                string hgVersion = proc.StandardOutput.ReadToEnd();
                Debug.LogFormat("[GetHgVersion] Version: {0}", hgVersion);
                // Get the output into a string
                return hgVersion;
            }
            catch
            {
                Debug.LogError("Unable to get hg hash.");
                return "unable to get version";
            }
        }
#endif
    }
}
