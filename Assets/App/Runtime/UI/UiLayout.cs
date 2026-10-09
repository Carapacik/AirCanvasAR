using UnityEngine;
using UnityEngine.UIElements;

namespace AirCanvas.UI
{
    public static class UiLayout
    {
        public static VisualElement Element(string classes, VisualElement parent = null)
        {
            var element = new VisualElement();
            foreach (var name in classes.Split(' ')) if (name.Length > 0) element.AddToClassList(name);
            parent?.Add(element); return element;
        }
        public static Label Label(string text, string classes, VisualElement parent)
        {
            var label = new Label(text);
            foreach (var name in classes.Split(' ')) if (name.Length > 0) label.AddToClassList(name);
            label.pickingMode = PickingMode.Ignore;
            parent.Add(label); return label;
        }
        public static Button Button(string text, string classes, VisualElement parent, System.Action clicked)
        {
            var button = new Button(clicked) { text = text };
            foreach (var name in classes.Split(' ')) if (name.Length > 0) button.AddToClassList(name);
            parent.Add(button); return button;
        }
        public static void Fit(UIDocument document, VisualElement safe)
        {
            const bool portrait = true;
            float scale = Mathf.Min(Screen.width / 430f, Screen.height / 820f);
            document.panelSettings.scale = Mathf.Max(.5f, scale);
            Rect area = Screen.safeArea;
            safe.style.left = area.xMin / scale;
            safe.style.right = (Screen.width - area.xMax) / scale;
            safe.style.top = (Screen.height - area.yMax) / scale;
            safe.style.bottom = area.yMin / scale;
            safe.EnableInClassList("portrait", portrait);
        }
    }
}
