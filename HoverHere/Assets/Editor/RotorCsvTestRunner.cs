using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class RotorCsvTestRunner
{
    [MenuItem("Tools/Tests/Export Rotor Test CSVs")]
    public static void ExportCsvFiles()
    {
        try
        {
            Assembly rotorAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "Assembly-CSharp");
            if (rotorAssembly == null)
                throw new InvalidOperationException("Assembly-CSharp has not loaded.");

            Type rotorPhysics = rotorAssembly.GetType("Rotor.Helicopter_Rotor_Physics");
            if (rotorPhysics == null)
                throw new InvalidOperationException("Rotor.Helicopter_Rotor_Physics was not found.");

            FieldInfo exportHook = rotorPhysics.GetField("flag_export_diagram", BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo testingMode = rotorPhysics.GetField("is_testing", BindingFlags.NonPublic | BindingFlags.Static);
            if (exportHook == null || testingMode == null)
                throw new MissingFieldException(rotorPhysics.FullName, "flag_export_diagram/is_testing");

            object previousExportHook = exportHook.GetValue(null);
            object previousTestingMode = testingMode.GetValue(null);
            try
            {
                exportHook.SetValue(null, false);
                testingMode.SetValue(null, true);
                InvokeExporter(rotorPhysics, "ExportCompleteBEMTRotorTest", "bemt_rotor_test.csv");
                InvokeExporter(rotorPhysics, "ExportVRSAndBaselineDiagram", "vrs_plot_data.csv");
            }
            finally
            {
                exportHook.SetValue(null, previousExportHook);
                testingMode.SetValue(null, previousTestingMode);
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string[] outputFiles = { "bemt_rotor_test.csv", "vrs_plot_data.csv" };
            foreach (string outputFile in outputFiles)
            {
                string outputPath = Path.Combine(projectRoot, outputFile);
                if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
                    throw new IOException("CSV export did not create a non-empty file: " + outputPath);

                Debug.Log("[RotorCsvTestRunner] Verified " + outputPath);
            }

            Debug.Log("[RotorCsvTestRunner] All rotor CSV exports completed successfully.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void InvokeExporter(Type rotorPhysics, string methodName, string filename)
    {
        MethodInfo exporter = rotorPhysics.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static);
        if (exporter == null)
            throw new MissingMethodException(rotorPhysics.FullName, methodName);

        exporter.Invoke(null, new object[] { filename });
    }
}
