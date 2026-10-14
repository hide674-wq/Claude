using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SlingshotHeroes.EditorTools
{
    public static class BuildScript
    {
        /// <summary>Menu: Tools &gt; Slingshot Heroes &gt; Build Android APK.</summary>
        [MenuItem("Tools/Slingshot Heroes/Build Android APK")]
        public static void BuildAndroid()
        {
            Build(false);
        }

        /// <summary>Google Play needs an app bundle (.aab).</summary>
        [MenuItem("Tools/Slingshot Heroes/Build Android App Bundle (AAB)")]
        public static void BuildAndroidBundle()
        {
            Build(true);
        }

        static void Build(bool bundle)
        {
            ProjectSetup.Apply();
            ProjectSetup.EnsureScene();

            Directory.CreateDirectory("Builds");
            string path = "Builds/SlingshotHeroes" + (bundle ? ".aab" : ".apk");

            EditorUserBuildSettings.buildAppBundle = bundle;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded: " + path + " (" + report.summary.totalSize / 1024 / 1024 + " MB)");
            }
            else
            {
                Debug.LogError("Build failed: " + report.summary.result);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
