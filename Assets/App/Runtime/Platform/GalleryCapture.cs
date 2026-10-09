using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace AirCanvas.Platform
{
    public sealed class GalleryCapture : MonoBehaviour
    {
        public bool Busy { get; private set; }
        public void Save(VisualElement ui, Action<bool, string> completed)
        { if (!Busy) StartCoroutine(Capture(ui, completed)); }

        private IEnumerator Capture(VisualElement ui, Action<bool, string> completed)
        {
            Busy = true;
            var previous = ui.style.visibility;
            ui.style.visibility = Visibility.Hidden;
            yield return null;
            yield return new WaitForEndOfFrame();
            Texture2D texture = null;
            bool success = false; string result = "";
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                byte[] bytes = texture.EncodeToPNG();
                string filename = "AirCanvasAR_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png";
#if UNITY_ANDROID && !UNITY_EDITOR
                using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                using var gallery = new AndroidJavaClass("com.carapacik.aircanvasar.GalleryWriter");
                result = gallery.CallStatic<string>("save", activity, bytes, filename);
#else
                string folder = Path.Combine(Application.persistentDataPath, "Captures");
                Directory.CreateDirectory(folder);
                result = Path.Combine(folder, filename);
                File.WriteAllBytes(result, bytes);
#endif
                success = true;
            }
            catch (Exception e) { Debug.LogException(e); result = e.Message; }
            finally
            {
                if (texture) Destroy(texture);
                ui.style.visibility = previous;
                Busy = false;
            }
            completed?.Invoke(success, result);
        }
    }
}
