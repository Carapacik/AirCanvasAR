using System;
using System.IO;
using System.Linq;
using AirCanvas.AR;
using AirCanvas.Drawing;
using AirCanvas.UI;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace AirCanvas.Editor
{
    public static class ProjectSetup
    {
        private const string Settings = "Assets/App/Settings";
        public static readonly string[] Scenes = { "Assets/App/Scenes/Menu.unity", "Assets/App/Scenes/Canvas.unity" };
        private static T Asset<T>(string name) where T : ScriptableObject
        {
            string path = Settings + "/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }
        [MenuItem("AirCanvas/Prepare project")]
        public static void Prepare()
        {
            Directory.CreateDirectory(Settings); Directory.CreateDirectory("Assets/App/Scenes"); AssetDatabase.Refresh();
            PlayerSettings.companyName = "CarapacikSpace"; PlayerSettings.productName = "AirCanvasAR"; PlayerSettings.bundleVersion = "2.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.carapacik.aircanvasar");
            PlayerSettings.Android.bundleVersionCode = 2;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel37;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false; PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 430; PlayerSettings.defaultScreenHeight = 932;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.SplashScreen.show = false;
            var player = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            player.FindProperty("activeInputHandler").intValue = 1; player.ApplyModifiedPropertiesWithoutUndo();
            EditorSettings.serializationMode = SerializationMode.ForceText;
            ConfigureRendering(); ConfigureXR(); LauncherIcons.Configure();
            var panel = Asset<PanelSettings>("App Panel");
            panel.scaleMode = PanelScaleMode.ConstantPixelSize; panel.scale = 1;
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/App/UI/AppTheme.tss");
            EditorUtility.SetDirty(panel);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Settings + "/Ink.mat");
            if (!material) { material = new Material(Shader.Find("AirCanvas/Ink")); AssetDatabase.CreateAsset(material, Settings + "/Ink.mat"); }
            CreateMenu(panel); CreateCanvas(panel, material);
            EditorBuildSettings.scenes = Scenes.Select(path => new EditorBuildSettingsScene(path,true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("AIR_CANVAS_PREPARED");
        }
        private static void ConfigureRendering()
        {
            var renderer = Asset<UniversalRendererData>("Mobile Renderer");
            if (!renderer.rendererFeatures.OfType<ARBackgroundRendererFeature>().Any())
            {
                var feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
                feature.name = "AR Background"; AssetDatabase.AddObjectToAsset(feature, renderer); renderer.rendererFeatures.Add(feature); EditorUtility.SetDirty(renderer);
            }
            var pipeline = Asset<UniversalRenderPipelineAsset>("Mobile URP");
            var serialized = new SerializedObject(pipeline);
            var renderers = serialized.FindProperty("m_RendererDataList"); renderers.arraySize = 1; renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serialized.FindProperty("m_AdditionalLightsRenderingMode").intValue = (int)LightRenderingMode.Disabled;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            pipeline.supportsHDR = false; pipeline.msaaSampleCount = 2; pipeline.shadowDistance = 0;
            pipeline.supportsCameraDepthTexture = false; pipeline.supportsCameraOpaqueTexture = false;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i=0; i<QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i); QualitySettings.renderPipeline = pipeline; QualitySettings.vSyncCount=0; }
            EditorUtility.SetDirty(pipeline);
        }
        private static void ConfigureXR()
        {
            var targets = Asset<XRGeneralSettingsPerBuildTarget>("XR Targets");
            var general = Asset<XRGeneralSettings>("Android XR");
            var manager = Asset<XRManagerSettings>("Android XR Manager");
            general.Manager = manager; general.InitManagerOnStart = false; manager.automaticLoading = false; manager.automaticRunning = false;
            targets.SetSettingsForBuildTarget(BuildTargetGroup.Android, general);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, targets, true);
            if (!XRPackageMetadataStore.AssignLoader(manager,"UnityEngine.XR.ARCore.ARCoreLoader",BuildTargetGroup.Android)) throw new InvalidOperationException("ARCore loader unavailable.");
            var core = Asset<ARCoreSettings>("ARCore"); core.requirement = ARCoreSettings.Requirement.Optional; core.depth = ARCoreSettings.Requirement.Optional; ARCoreSettings.currentSettings=core;
            EditorUtility.SetDirty(core); EditorUtility.SetDirty(general); EditorUtility.SetDirty(manager); EditorUtility.SetDirty(targets);
        }
        private static Camera Camera(string name)
        {
            var camera = new GameObject(name, typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.97f,.98f,.96f);
            camera.nearClipPlane=.01f; camera.farClipPlane=30; camera.transform.position=new Vector3(0,0,-2);
            camera.GetUniversalAdditionalCameraData().allowXRRendering=false;
            return camera;
        }
        private static void CreateMenu(PanelSettings panel)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Camera("Menu Camera").backgroundColor=Color.white;
            var ui=new GameObject("Menu UI").AddComponent<UIDocument>(); ui.panelSettings=panel; ui.gameObject.AddComponent<MenuScreen>();
            EditorSceneManager.SaveScene(scene,Scenes[0]);
        }
        private static void CreateCanvas(PanelSettings panel, Material ink)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var preview=Camera("Preview Camera");
            var rig=new GameObject("XR Origin"); rig.SetActive(false);
            var origin=rig.AddComponent<XROrigin>();
            var offset=new GameObject("Camera Offset"); offset.transform.SetParent(rig.transform,false); origin.CameraFloorOffsetObject=offset;
            origin.CameraYOffset=0; origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Device;
            var camera=Camera("AR Camera"); camera.transform.SetParent(offset.transform,false); camera.transform.localPosition=Vector3.zero; camera.tag="MainCamera";
            camera.GetUniversalAdditionalCameraData().allowXRRendering=true;
            camera.gameObject.AddComponent<ARCameraManager>().requestedBackgroundRenderingMode=CameraBackgroundRenderingMode.BeforeOpaques;
            camera.gameObject.AddComponent<ARCameraBackground>();
            var driver=camera.gameObject.AddComponent<TrackedPoseDriver>();
            driver.positionInput=new InputActionProperty(new InputAction("Position",InputActionType.Value,"<HandheldARInputDevice>/devicePosition",expectedControlType:"Vector3"));
            driver.rotationInput=new InputActionProperty(new InputAction("Rotation",InputActionType.Value,"<HandheldARInputDevice>/deviceRotation",expectedControlType:"Quaternion"));
            driver.trackingStateInput=default; origin.Camera=camera;
            rig.AddComponent<ARPlaneManager>().requestedDetectionMode=PlaneDetectionMode.Horizontal|PlaneDetectionMode.Vertical;
            var rays=rig.AddComponent<ARRaycastManager>(); var anchors=rig.AddComponent<ARAnchorManager>();
            var sessionObject=new GameObject("AR Session"); sessionObject.SetActive(false);
            var session=sessionObject.AddComponent<ARSession>(); sessionObject.AddComponent<ARInputManager>();
            var drawing=new GameObject("Drawing Session").AddComponent<DrawingSession>(); drawing.Configure(rig,session,camera,preview,rays,anchors);
            var ui=new GameObject("Canvas UI").AddComponent<UIDocument>(); ui.panelSettings=panel;
            ui.gameObject.AddComponent<CanvasController>().Configure(drawing,ink);
            ui = UnityEngine.Object.FindFirstObjectByType<CanvasController>().GetComponent<UIDocument>();
            ui.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(Settings + "/App Panel.asset");
            if (!ui.panelSettings) throw new BuildFailedException("Canvas panel settings are missing.");
            EditorUtility.SetDirty(ui);
            EditorSceneManager.SaveScene(scene,Scenes[1]);
        }
        [MenuItem("AirCanvas/Build Android preview")]
        public static void BuildAndroid()
        {
            Prepare(); ProjectChecks.Run(); Directory.CreateDirectory("Builds/Android");
            PlayerSettings.Android.useCustomKeystore=false;
            EditorUserBuildSettings.buildAppBundle=false;
            Build(BuildTarget.Android,"Builds/Android/AirCanvasAR-2.0-preview.apk",BuildOptions.None);
        }
        public static void BuildDesktop()
        {
            Prepare(); Directory.CreateDirectory("Builds/Windows");
            Build(BuildTarget.StandaloneWindows64,"Builds/Windows/AirCanvasAR.exe",BuildOptions.Development);
        }
        private static void Build(BuildTarget target,string path,BuildOptions options)
        {
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=Scenes,locationPathName=path,target=target,options=options });
            if(result.summary.result!=BuildResult.Succeeded) throw new BuildFailedException("AirCanvas build failed.");
            Debug.Log("AIR_CANVAS_BUILD_SUCCEEDED: "+path);
        }
    }
}
