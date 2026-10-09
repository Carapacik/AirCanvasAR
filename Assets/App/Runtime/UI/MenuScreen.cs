using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace AirCanvas.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuScreen : MonoBehaviour
    {
        private UIDocument document;
        private VisualElement safe;
        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            document = GetComponent<UIDocument>();
            var root = document.rootVisualElement;
            root.AddToClassList("app");
            root.AddToClassList("menu-background");
            safe = UiLayout.Element("safe", root);
            var header = UiLayout.Element("header row spaced", safe);
            UiLayout.Label("AIR / CANVAS", "brand", header);
            UiLayout.Label("AR", "tag", header);
            var content = UiLayout.Element("menu-content", safe);
            var intro = UiLayout.Element("intro", content);
            UiLayout.Label("MAKE ROOM FOR IDEAS", "eyebrow", intro);
            UiLayout.Label("The world.\nYour canvas.", "hero", intro);
            UiLayout.Label("Turn a little space into something only you could imagine.", "description", intro);
            var art = new AirMark(); art.AddToClassList("air-mark"); intro.Add(art);
            var card = UiLayout.Element("start-card card", content);
            UiLayout.Label("A new perspective", "card-title", card);
            UiLayout.Label("Two ways to leave your mark.", "description", card);
            UiLayout.Label("01  On a surface", "mode-title", card);
            UiLayout.Label("Draw with your finger on a wall, table or floor.", "body", card);
            UiLayout.Label("02  In the air", "mode-title", card);
            UiLayout.Label("Hold to paint and move your phone like a brush.", "body", card);
            UiLayout.Button("Open canvas", "primary start", card, () => SceneManager.LoadScene("Canvas"));
            UiLayout.Label("Switch modes at any time. Undo every stroke.", "hint", card);
            UiLayout.Label("YOUR COLORS. YOUR SPACE. YOUR SIGNATURE.", "menu-footer", safe);
            UiLayout.Fit(document, safe);
        }
        private void Update() { if (safe != null) UiLayout.Fit(document, safe); }
    }
    internal sealed class AirMark : VisualElement
    {
        public AirMark() { pickingMode = PickingMode.Ignore; generateVisualContent += Draw; }
        private void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D;
            float w = contentRect.width, h = contentRect.height;
            p.strokeColor = new Color32(141,215,58,255); p.lineWidth = 16; p.lineCap = LineCap.Round;
            p.BeginPath(); p.MoveTo(new Vector2(w*.06f,h*.78f));
            p.BezierCurveTo(new Vector2(w*.12f,-h*.1f),new Vector2(w*.78f,h*.05f),new Vector2(w*.59f,h*.54f));
            p.BezierCurveTo(new Vector2(w*.35f,h*1.1f),new Vector2(w*.17f,h*.5f),new Vector2(w*.9f,h*.3f)); p.Stroke();
            p.strokeColor = new Color32(30,43,24,255); p.lineWidth = 3;
            p.BeginPath(); p.MoveTo(new Vector2(w*.24f,h*.85f)); p.LineTo(new Vector2(w*.76f,h*.79f)); p.Stroke();
        }
    }
}
