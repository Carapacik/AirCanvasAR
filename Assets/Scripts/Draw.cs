using System.Collections.Generic;
using UnityEngine;

public class Draw : MonoBehaviour
{
    public static bool DrawingOnSpace = false;
    public static bool Drawing;
    public GameObject currentStroke;
    public List<GameObject> currentStrokes;
    public Color color;
    public GameObject buttonDraw;
    public GameObject buttonOffSpace;
    public GameObject buttonRed;
    public GameObject buttonBlue;
    public GameObject buttonGreen;
    public GameObject buttonBlack;
    public GameObject buttonWhite;

    public GameObject spacePenPoint;
    public GameObject stroke;
    public bool mouseLookTesting;

    private float _pitch;
    private float _yaw;

    // Start is called before the first frame update
    private void Start()
    {
        spacePenPoint.SetActive(false);
        buttonDraw = GameObject.Find("ButtonDraw");
        buttonOffSpace = GameObject.Find("ButtonDraw2");
        buttonRed = GameObject.Find("Red");
        buttonBlue = GameObject.Find("Blue");
        buttonGreen = GameObject.Find("Green");
        buttonWhite = GameObject.Find("White");
        buttonBlack = GameObject.Find("Black");

        buttonRed.SetActive(false);
        buttonBlue.SetActive(false);
        buttonGreen.SetActive(false);
        buttonWhite.SetActive(false);
        buttonBlack.SetActive(false);

        spacePenPoint.GetComponent<Renderer>().material.color = new Color(255, 255, 255);
        stroke.GetComponent<Renderer>().sharedMaterial.color = new Color(255, 255, 255);
    }

    // Update is called once per frame
    private void Update()
    {
        if (mouseLookTesting)
        {
            _yaw += 2 * Input.GetAxis("Mouse X");
            _pitch -= 2 * Input.GetAxis("Mouse Y");
            transform.eulerAngles = new Vector3(_pitch, _yaw, 0.0f);
        }

        if (PenManager.DrawingOnSpace) spacePenPoint.SetActive(true);
    }

    public void StartStroke()
    {
        Drawing = true;
        currentStroke = Instantiate(stroke, spacePenPoint.transform.position, spacePenPoint.transform.rotation);
        currentStrokes.Add(currentStroke);
    }

    public void EndStroke()
    {
        Drawing = false;
    }

    public void DrawOnSpace()
    {
        Drawing = true;

        buttonDraw.SetActive(false);

        spacePenPoint.SetActive(true);
        buttonOffSpace.SetActive(true);
    }

    public void DrawOffSpace()
    {
        Drawing = false;

        buttonOffSpace.SetActive(false);

        buttonDraw.SetActive(true);
        spacePenPoint.SetActive(false);
    }

    public void ColorVisible()
    {
        buttonRed.SetActive(true);
        buttonBlue.SetActive(true);
        buttonGreen.SetActive(true);
        buttonWhite.SetActive(true);
        buttonBlack.SetActive(true);
    }

    public void ColorRed()
    {
        buttonRed.SetActive(false);
        buttonBlue.SetActive(false);
        buttonGreen.SetActive(false);
        buttonWhite.SetActive(false);
        buttonBlack.SetActive(false);

        spacePenPoint.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        stroke.GetComponent<Renderer>().sharedMaterial.color = new Color(255, 0, 0);
    }

    public void ColorBlue()
    {
        buttonRed.SetActive(false);
        buttonBlue.SetActive(false);
        buttonGreen.SetActive(false);
        buttonWhite.SetActive(false);
        buttonBlack.SetActive(false);

        spacePenPoint.GetComponent<Renderer>().material.color = new Color(0, 0, 255);
        stroke.GetComponent<Renderer>().sharedMaterial.color = new Color(0, 0, 255);
    }

    public void ColorGreen()
    {
        buttonRed.SetActive(false);
        buttonBlue.SetActive(false);
        buttonGreen.SetActive(false);
        buttonWhite.SetActive(false);
        buttonBlack.SetActive(false);

        spacePenPoint.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
        stroke.GetComponent<Renderer>().sharedMaterial.color = new Color(0, 255, 0);
    }

    public void ColorWhite()
    {
        buttonRed.SetActive(false);
        buttonBlue.SetActive(false);
        buttonGreen.SetActive(false);
        buttonWhite.SetActive(false);
        buttonBlack.SetActive(false);

        spacePenPoint.GetComponent<Renderer>().material.color = new Color(255, 255, 255);
        stroke.GetComponent<Renderer>().sharedMaterial.color = new Color(255, 255, 255);
    }

    public void ColorBlack()
    {
        buttonRed.SetActive(false);
        buttonBlue.SetActive(false);
        buttonGreen.SetActive(false);
        buttonWhite.SetActive(false);
        buttonBlack.SetActive(false);

        spacePenPoint.GetComponent<Renderer>().material.color = new Color(0, 0, 0);
        stroke.GetComponent<Renderer>().sharedMaterial.color = new Color(0, 0, 0);
    }

    public void DeleteLine()
    {
        foreach (var a in currentStrokes) Destroy(a);
    }
}