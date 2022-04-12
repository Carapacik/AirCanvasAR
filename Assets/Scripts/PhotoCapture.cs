using System;
using System.Collections;
using UnityEngine;

public class PhotoCapture : MonoBehaviour
{
    public bool takingScreenshot;

    public SpriteRenderer star;
    private IEnumerator _blinkCoroutine;

    public void CaptureScreenshot()
    {
        StartCoroutine(TakeScreenshotAndSave());
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator TakeScreenshotAndSave()
    {
        takingScreenshot = true;
        yield return new WaitForEndOfFrame();

        var ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        ss.Apply();

        // Save the screenshot to Gallery/Photos
        var formattedName =
            $"{Application.productName}_Capture{{0}}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        Debug.Log("Permission result: " +
                  NativeGallery.SaveImageToGallery(ss, Application.productName + " Captures", formattedName));
        takingScreenshot = false;
    }
}