using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    public static void PerformLinuxBuild()
    {
        string version = PlayerSettings.bundleVersion;
        if (string.IsNullOrEmpty(version)) version = "1.0.0";

        string[] scenes = EditorBuildSettingsScene.GetActiveSceneList(
            EditorBuildSettings.scenes
        );

        BuildReport report = BuildPipeline.BuildPlayer(
            scenes,
            "build/StandaloneLinux64/project-game",
            BuildTarget.StandaloneLinux64,
            BuildOptions.None
        );

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] ✓ Linux build succeeded (v{version})");
        }
        else
        {
            Debug.LogError($"[BuildScript] ✗ Linux build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
