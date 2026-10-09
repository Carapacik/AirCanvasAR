using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AirCanvas.UI
{
    public sealed class ColorPalette : IDisposable
    {
        public VisualElement Root { get; }
        private readonly VisualElement field, marker, swatch;
        private readonly TextField hex;
        private readonly Slider hue;
        private readonly Texture2D texture;
        private readonly Action<Color> changed;
        private float h, s, v;
        private bool setting;
        public ColorPalette(VisualElement parent, Color initial, Action<Color> changed, Action close)
        {
            this.changed = changed;
            Root = UiLayout.Element("palette card", parent);
            var heading = UiLayout.Element("row spaced", Root);
            UiLayout.Label("Your color", "card-title", heading);
            UiLayout.Button("Done", "small", heading, close);
            field = UiLayout.Element("color-field", Root);
            marker = UiLayout.Element("color-marker", field); marker.pickingMode = PickingMode.Ignore;
            texture = new Texture2D(192, 128, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear };
            field.style.backgroundImage = texture;
            field.RegisterCallback<PointerDownEvent>(e => { field.CapturePointer(e.pointerId); Pick(e.localPosition); e.StopPropagation(); });
            field.RegisterCallback<PointerMoveEvent>(e => { if (field.HasPointerCapture(e.pointerId)) Pick(e.localPosition); });
            field.RegisterCallback<PointerUpEvent>(e => { field.ReleasePointer(e.pointerId); e.StopPropagation(); });
            hue = new Slider("Hue", 0, 1);
            hue.AddToClassList("slider"); Root.Add(hue);
            hue.RegisterValueChangedCallback(e => { if (!setting) { h = e.newValue; Paint(); Apply(); } });
            var bottom = UiLayout.Element("row spaced", Root);
            swatch = UiLayout.Element("swatch", bottom);
            hex = new TextField { maxLength = 7, isDelayed = true }; hex.AddToClassList("hex"); bottom.Add(hex);
            hex.RegisterValueChangedCallback(e =>
            {
                if (setting) return;
                string value = e.newValue.StartsWith("#") ? e.newValue : "#" + e.newValue;
                if (value.Length == 7 && ColorUtility.TryParseHtmlString(value, out var color)) Set(color);
                else hex.SetValueWithoutNotify("#" + ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(h,s,v)));
            });
            Set(initial);
        }
        private void Set(Color color)
        {
            Color.RGBToHSV(color, out h, out s, out v);
            setting = true; hue.SetValueWithoutNotify(h); setting = false;
            Paint(); Apply();
        }
        private void Pick(Vector2 p)
        {
            s = Mathf.Clamp01(p.x / field.contentRect.width);
            v = 1 - Mathf.Clamp01(p.y / field.contentRect.height);
            Apply();
        }
        private void Paint()
        {
            var pixels = new Color32[texture.width * texture.height];
            for (int y=0; y<texture.height; y++) for (int x=0; x<texture.width; x++)
                pixels[y*texture.width+x] = Color.HSVToRGB(h, x/(float)(texture.width-1), y/(float)(texture.height-1));
            texture.SetPixels32(pixels); texture.Apply();
        }
        private void Apply()
        {
            Color color = Color.HSVToRGB(h, s, v);
            marker.style.left = Length.Percent(s*100); marker.style.top = Length.Percent((1-v)*100);
            swatch.style.backgroundColor = color;
            hex.SetValueWithoutNotify("#" + ColorUtility.ToHtmlStringRGB(color));
            changed(color);
        }
        public void Dispose() { UnityEngine.Object.Destroy(texture); Root.RemoveFromHierarchy(); }
    }
}
