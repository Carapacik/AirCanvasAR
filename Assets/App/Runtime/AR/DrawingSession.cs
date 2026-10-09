using System;
using System.Collections;
using System.Collections.Generic;
using AirCanvas.UI;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace AirCanvas.AR
{
    public sealed class DrawingSession : MonoBehaviour
    {
        [SerializeField] private GameObject rig;
        [SerializeField] private ARSession session;
        [SerializeField] private Camera arCamera;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private ARRaycastManager raycasts;
        [SerializeField] private ARAnchorManager anchors;
        private readonly List<ARRaycastHit> hits = new();
        private ARAnchor anchor;
        private bool initialized, addingAnchor, closing;
        public Transform Canvas { get; private set; }
        public Camera Camera => Preview ? previewCamera : arCamera;
        public bool Preview { get; private set; }
        public bool Ready => Preview || initialized && ARSession.state == ARSessionState.SessionTracking &&
            anchor && anchor.trackingState == TrackingState.Tracking;
        public string Status { get; private set; } = "";
        public void Configure(GameObject xrRig, ARSession arSession, Camera camera, Camera preview, ARRaycastManager rays, ARAnchorManager anchorManager)
        { rig = xrRig; session = arSession; arCamera = camera; previewCamera = preview; raycasts = rays; anchors = anchorManager; }

        private IEnumerator Start()
        {
            Canvas = new GameObject("Drawing canvas").transform;
            Canvas.SetParent(transform, false);
#if UNITY_ANDROID && !UNITY_EDITOR
            Preview = false;
#else
            Preview = true;
#endif
            previewCamera.gameObject.SetActive(true);
            if (Preview) { Status = "Desktop preview • virtual surface"; yield break; }
            Status = "Starting camera…";
#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                float deadline = Time.realtimeSinceStartup + 30;
                while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera) && Time.realtimeSinceStartup < deadline) yield return null;
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                { Status = "Allow camera access in Android settings, then reopen the canvas."; yield break; }
            }
#endif
            var manager = XRGeneralSettings.Instance?.Manager;
            if (!manager) { Status = "AR configuration is unavailable."; yield break; }
            yield return manager.InitializeLoader();
            initialized = manager.activeLoader != null;
            if (!initialized) { Status = "AR could not start on this device."; yield break; }
            manager.StartSubsystems();
            rig.SetActive(true); session.gameObject.SetActive(true); previewCamera.gameObject.SetActive(false);
            yield return ARSession.CheckAvailability();
            if (ARSession.state == ARSessionState.NeedsInstall) yield return ARSession.Install();
            if (ARSession.state == ARSessionState.Unsupported || ARSession.state == ARSessionState.NeedsInstall)
            { Status = "Install or update Google Play Services for AR."; yield break; }
            Status = "Move your phone slowly to scan the room.";
        }
        private void Update()
        {
            if (Preview || !initialized || closing) return;
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                Status = "Tracking paused • move slowly toward a textured area";
                return;
            }
            if (!anchor && !addingAnchor) AddCanvasAnchor();
            if (anchor) Status = Ready ? "Ready to draw" : "Recovering the canvas position…";
        }
        private async void AddCanvasAnchor()
        {
            addingAnchor = true;
            Status = "Anchoring your canvas…";
            try
            {
                var pose = new Pose(arCamera.transform.position + arCamera.transform.forward * .5f, Quaternion.identity);
                var result = await anchors.TryAddAnchorAsync(pose);
                if (closing || !this) { if (result.status.IsSuccess() && anchors) anchors.TryRemoveAnchor(result.value); return; }
                if (result.status.IsSuccess())
                { anchor = result.value; Canvas.SetParent(anchor.transform, false); }
                else Status = "Could not anchor the canvas. Keep scanning.";
            }
            catch (Exception e) { if (!closing) Debug.LogException(e); }
            finally { addingAnchor = false; }
        }
        public bool SurfacePoint(Vector2 screen, out Vector3 point, out Plane plane)
        {
            if (Preview)
            {
                plane = new Plane(Vector3.back, Vector3.zero);
                var ray = Camera.ScreenPointToRay(screen);
                if (plane.Raycast(ray, out float distance)) { point = ray.GetPoint(distance); return true; }
            }
            else if (Ready && raycasts.Raycast(screen, hits, TrackableType.PlaneWithinPolygon))
            {
                var pose = hits[0].pose;
                Vector3 normal = pose.rotation * Vector3.up;
                plane = new Plane(normal, pose.position + normal * .003f);
                point = pose.position + normal * .003f; return true;
            }
            point = default; plane = default; return false;
        }
        public Vector3 AirPoint(float distance) => Camera.transform.position + Camera.transform.forward * distance;
        private void OnDestroy()
        {
            closing = true;
            if (Canvas) Destroy(Canvas.gameObject);
            if (anchor && anchors) anchors.TryRemoveAnchor(anchor);
            if (initialized && XRGeneralSettings.Instance && XRGeneralSettings.Instance.Manager)
            {
                XRGeneralSettings.Instance.Manager.StopSubsystems();
                XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            }
        }
    }
}
