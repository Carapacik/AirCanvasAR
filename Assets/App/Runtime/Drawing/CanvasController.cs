using AirCanvas.AR;
using AirCanvas.Platform;
using AirCanvas.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace AirCanvas.Drawing
{
    [RequireComponent(typeof(UIDocument), typeof(GalleryCapture))]
    public sealed class CanvasController : MonoBehaviour
    {
        [SerializeField] private DrawingSession session;
        [SerializeField] private Material ink;
        private UIDocument document;
        private GalleryCapture capture;
        private StrokeHistory history;
        private VisualElement safe, pad, reticle, footer, modal, toolRow;
        private Label status, hint, count, modeLabel;
        private Button undo, redo, save, surfaceButton, airButton, colorButton, clear;
        private VisualElement airHold;
        private Slider distanceSlider, widthSlider;
        private ColorPalette palette;
        private StrokeView current;
        private Plane strokePlane;
        private bool air, held, blockedUntilRelease;
        private int pointerId = -1, totalPoints;
        private VisualElement pointerOwner;
        private Vector2 pointer;
        private Color color = new Color32(61, 111, 27, 255);
        private float width = .012f, distance = .5f, toastUntil;
        private const int PointBudget = 120000;
        public void Configure(DrawingSession drawingSession, Material inkMaterial) { session = drawingSession; ink = inkMaterial; }

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            history = new StrokeHistory();
            document = GetComponent<UIDocument>();
            capture = GetComponent<GalleryCapture>();
            var root = document.rootVisualElement;
            root.AddToClassList("app"); root.pickingMode = PickingMode.Ignore;
            pad = UiLayout.Element("draw-pad", root);
            BindStrokeInput(pad);
            safe = UiLayout.Element("safe drawing", root); safe.pickingMode = PickingMode.Ignore;
            var header = UiLayout.Element("header row spaced", safe); header.pickingMode = PickingMode.Ignore;
            UiLayout.Button("Menu", "white", header, Back);
            modeLabel = UiLayout.Label("AIR / CANVAS", "brand floating", header);
            save = UiLayout.Button("Save image", "primary", header, Save);
            var info = UiLayout.Element("info", safe); info.pickingMode = PickingMode.Ignore;
            status = UiLayout.Label("", "status floating", info);
            count = UiLayout.Label("", "stroke-count", info);
            reticle = UiLayout.Element("reticle", root); reticle.pickingMode = PickingMode.Ignore;
            footer = UiLayout.Element("tools card", safe);
            var modeRow = UiLayout.Element("row", footer);
            surfaceButton = UiLayout.Button("Surface", "mode", modeRow, () => SetMode(false));
            airButton = UiLayout.Button("Air", "mode", modeRow, () => SetMode(true));
            toolRow = UiLayout.Element("row tool-row", footer);
            undo = UiLayout.Button("Undo", "", toolRow, () => { EndStroke(); history.Undo(); });
            redo = UiLayout.Button("Redo", "", toolRow, () => { EndStroke(); history.Redo(); });
            colorButton = UiLayout.Button("Color", "color-button", toolRow, TogglePalette);
            clear = UiLayout.Button("Clear", "", toolRow, () => { EndStroke(); history.Clear(); });
            widthSlider = new Slider("Brush", .003f, .04f) { value = width };
            widthSlider.AddToClassList("slider"); footer.Add(widthSlider);
            widthSlider.RegisterValueChangedCallback(e => { EndStroke(); width = e.newValue; });
            distanceSlider = new Slider("Distance", .15f, 1.2f) { value = distance };
            distanceSlider.AddToClassList("slider"); footer.Add(distanceSlider);
            distanceSlider.RegisterValueChangedCallback(e => { EndStroke(); distance = e.newValue; });
            hint = UiLayout.Label("", "hint", footer);
            airHold = UiLayout.Element("air-hold", safe);
            UiLayout.Label("HOLD\nTO DRAW", "hold-label", airHold);
            BindStrokeInput(airHold);
            history.Changed += RefreshHistory;
            SetColor(color); SetMode(false); RefreshHistory();
            UiLayout.Fit(document, safe);
        }
        private void BindStrokeInput(VisualElement element)
        {
            element.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || held || palette != null || modal != null || capture.Busy) return;
                pointerId = e.pointerId; pointer = ScreenPoint(e.position);
                held = true; blockedUntilRelease = false; pointerOwner = element;
                element.CapturePointer(e.pointerId); e.StopPropagation();
                Debug.Log($"AirCanvas pointer down: mode={(air ? "Air" : "Surface")}, ready={session.Ready}");
            });
            element.RegisterCallback<PointerMoveEvent>(e => { if (held && e.pointerId == pointerId) pointer = ScreenPoint(e.position); });
            element.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.pointerId != pointerId) return;
                EndStroke(); element.ReleasePointer(e.pointerId); e.StopPropagation();
            });
            element.RegisterCallback<PointerCancelEvent>(e => { if (pointerOwner == element && e.pointerId == pointerId) EndStroke(); });
            element.RegisterCallback<PointerCaptureOutEvent>(e => { if (pointerOwner == element && e.pointerId == pointerId) EndStroke(); });
        }
        private Vector2 ScreenPoint(Vector3 panelPosition)
        {
            float scale = document.panelSettings.scale;
            return new Vector2(panelPosition.x * scale, Screen.height - panelPosition.y * scale);
        }
        private void Update()
        {
            if (safe == null) return;
            UiLayout.Fit(document, safe);
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) Back();
            if (Time.unscaledTime > toastUntil) status.text = session.Status;
            bool ready = session.Ready;
            reticle.style.display = air && palette == null && modal == null ? DisplayStyle.Flex : DisplayStyle.None;
            reticle.EnableInClassList("ready", ready);
            airHold.SetEnabled(!capture.Busy);
            airHold.EnableInClassList("tracking-paused", !ready);
            save.SetEnabled(ready && !capture.Busy);
            if (!held || blockedUntilRelease || capture.Busy) return;
            if (!ready) { FinishStroke(); return; }
            if (totalPoints >= PointBudget) { FinishStroke(); blockedUntilRelease = true; Toast("Canvas is full. Save your image and start a new canvas."); return; }
            Vector3 point;
            if (air) point = session.AirPoint(distance);
            else if (current == null)
            {
                if (!session.SurfacePoint(pointer, out point, out strokePlane))
                { Toast("Scan a wall, table or floor, then draw on it."); return; }
            }
            else
            {
                // Keep one stroke on its original plane; do not jump between detected surfaces.
                var ray = session.Camera.ScreenPointToRay(pointer);
                if (!strokePlane.Raycast(ray, out float rayDistance) || rayDistance > 8)
                { FinishStroke(); blockedUntilRelease = true; return; }
                point = ray.GetPoint(rayDistance);
            }
            if (current == null)
            {
                current = new GameObject("Stroke").AddComponent<StrokeView>();
                current.Initialize(session.Canvas, ink, color, width, point);
                totalPoints += current.PointCount;
            }
            else if (current.Full) { FinishStroke(); blockedUntilRelease = true; }
            else if (current.Append(point)) totalPoints++;
        }
        private void FinishStroke()
        {
            if (!current) return;
            history.Add(current.gameObject);
            Debug.Log($"AirCanvas stroke committed: points={current.PointCount}, visible={history.VisibleCount}");
            current = null;
        }
        private void EndStroke()
        {
            FinishStroke(); held = false;
            int releasedPointer = pointerId;
            var releasedOwner = pointerOwner;
            pointerId = -1; pointerOwner = null; blockedUntilRelease = false;
            if (releasedPointer >= 0 && releasedOwner != null && releasedOwner.HasPointerCapture(releasedPointer))
                releasedOwner.ReleasePointer(releasedPointer);
            airHold?.RemoveFromClassList("pressed");
        }
        private void SetMode(bool value)
        {
            EndStroke(); air = value;
            Debug.Log($"AirCanvas mode: {(air ? "Air" : "Surface")}, ready={session.Ready}");
            surfaceButton.EnableInClassList("selected", !air); airButton.EnableInClassList("selected", air);
            distanceSlider.style.display = air ? DisplayStyle.Flex : DisplayStyle.None;
            airHold.style.display = air ? DisplayStyle.Flex : DisplayStyle.None;
            hint.text = air ? "Hold the button. Move your phone like a brush." :
                "Scan a surface, then draw on it with your finger.";
            safe.EnableInClassList("air-mode", air);
        }
        private void SetColor(Color value)
        {
            color = value;
            if (colorButton != null) { colorButton.style.borderBottomColor = color; colorButton.style.borderBottomWidth = 5; }
            if (reticle != null) reticle.style.borderTopColor = reticle.style.borderBottomColor = reticle.style.borderLeftColor = reticle.style.borderRightColor = color;
        }
        private void TogglePalette()
        {
            EndStroke();
            if (palette != null) { ClosePalette(); return; }
            palette = new ColorPalette(safe, color, SetColor, ClosePalette);
            footer.style.display = DisplayStyle.None; airHold.style.display = DisplayStyle.None;
        }
        private void ClosePalette()
        {
            palette?.Dispose(); palette = null;
            footer.style.display = DisplayStyle.Flex; SetMode(air);
        }
        private void RefreshHistory()
        {
            undo.SetEnabled(history.CanUndo); redo.SetEnabled(history.CanRedo);
            clear.SetEnabled(history.VisibleCount > 0);
            count.text = history.VisibleCount + " strokes";
        }
        private void Save()
        {
            EndStroke(); if (palette != null) ClosePalette();
            capture.Save(document.rootVisualElement, (success, path) =>
            {
                Toast(success ? "Saved to Pictures / AirCanvasAR" :
                    "Could not save. Please try again.");
                if (success) Debug.Log("AirCanvas image saved: " + path);
            });
        }
        private void Toast(string text) { status.text = text; toastUntil = Time.unscaledTime + 3; }
        private void Back()
        {
            if (capture.Busy) return;
            EndStroke();
            if (palette != null) { ClosePalette(); return; }
            if (modal != null) { modal.RemoveFromHierarchy(); modal = null; return; }
            if (!history.CanUndo && !history.CanRedo) { SceneManager.LoadScene("Menu"); return; }
            modal = UiLayout.Element("modal", safe);
            var card = UiLayout.Element("card dialog", modal);
            UiLayout.Label("Leave this canvas?", "card-title", card);
            UiLayout.Label("Save an image first if you want to keep it. Strokes stay only in this session.", "body", card);
            UiLayout.Button("Keep drawing", "primary", card, Back);
            UiLayout.Button("Leave canvas", "", card, () => SceneManager.LoadScene("Menu"));
        }
        private void OnApplicationPause(bool paused) { if (paused && history != null) EndStroke(); }
        private void OnDestroy()
        {
            palette?.Dispose();
            history?.Dispose();
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
    }
}
