using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AirCanvas.Editor
{
    public static class AndroidReleaseBuild
    {
        [MenuItem("AirCanvas/Build Android signed release APK and AAB")]
        public static void Run()
        {
            ProjectSetup.Prepare();
            ProjectChecks.Run();
            string keystore = PlayerSettings.Android.keystoreName;
            string alias = PlayerSettings.Android.keyaliasName;
            string storePassword = PlayerSettings.Android.keystorePass;
            string keyPassword = PlayerSettings.Android.keyaliasPass;
            bool custom = PlayerSettings.Android.useCustomKeystore;
            bool bundle = EditorUserBuildSettings.buildAppBundle;
            bool development = EditorUserBuildSettings.development;
            try
            {
                ConfigureSigning();
                EditorUserBuildSettings.development = false;
                Directory.CreateDirectory("Builds/Android");
                Build(true);
                Build(false);
            }
            finally
            {
                PlayerSettings.Android.keystoreName = keystore;
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keystorePass = storePassword;
                PlayerSettings.Android.keyaliasPass = keyPassword;
                PlayerSettings.Android.useCustomKeystore = custom;
                EditorUserBuildSettings.buildAppBundle = bundle;
                EditorUserBuildSettings.development = development;
                AssetDatabase.SaveAssets();
            }
        }

        private static void Build(bool bundle)
        {
            EditorUserBuildSettings.buildAppBundle = bundle;
            string path = "Builds/Android/AirCanvasAR-" + PlayerSettings.bundleVersion + (bundle ? ".aab" : ".apk");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = ProjectSetup.Scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Android release build failed. See the Unity build log.");
            Debug.Log("AIR_CANVAS_RELEASE_SUCCEEDED: " + path);
        }

        private static void ConfigureSigning()
        {
            string file = Environment.GetEnvironmentVariable("AIRCANVAS_SIGNING_PROPERTIES");
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                throw new BuildFailedException("Set AIRCANVAS_SIGNING_PROPERTIES to an external key.properties file.");
            var values = new Dictionary<string, string>();
            foreach (string line in File.ReadAllLines(file))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("#") || trimmed.StartsWith("!")) continue;
                int separator = trimmed.IndexOf('=');
                if (separator > 0) values[trimmed.Substring(0, separator).Trim()] = trimmed.Substring(separator + 1).Trim();
            }
            foreach (string key in new[] { "storePassword", "keyAlias", "keyPassword" })
                if (!values.ContainsKey(key) || string.IsNullOrEmpty(values[key]))
                    throw new BuildFailedException("Signing properties are incomplete.");
            string path = Environment.GetEnvironmentVariable("AIRCANVAS_KEYSTORE");
            if (string.IsNullOrEmpty(path))
            {
                if (!values.TryGetValue("storeFile", out path) || string.IsNullOrWhiteSpace(path))
                    throw new BuildFailedException("Set AIRCANVAS_KEYSTORE or storeFile in the signing properties.");
                path = path.Replace(@"\\", @"\").Replace(@"\:", ":");
            }
            if (!Path.IsPathRooted(path)) path = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(file)), path);
            if (!File.Exists(path)) throw new BuildFailedException("The external signing keystore was not found.");
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(path);
            PlayerSettings.Android.keystorePass = values["storePassword"];
            PlayerSettings.Android.keyaliasName = values["keyAlias"];
            PlayerSettings.Android.keyaliasPass = values["keyPassword"];
        }
    }
}
