using UnityEngine;

public class PenManager : MonoBehaviour
{
    public static bool DrawingOnSpace;
    public GameObject spacePenPoint;
    private GameObject _button;
    private GameObject _button2;

    public void DrawOnSpace()
    {
        DrawingOnSpace = true;

        _button = GameObject.Find("ButtonDraw");
        _button.SetActive(false);
    }

    public void DrawOffSpace()
    {
        DrawingOnSpace = false;

        _button2 = GameObject.Find("ButtonDraw2");
        _button2.SetActive(false);

        _button.SetActive(true);
        _button2.SetActive(true);
    }
}