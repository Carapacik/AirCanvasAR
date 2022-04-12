using UnityEngine;

public class Stroke : MonoBehaviour
{
    private GameObject _penPoint;

    // Start is called before the first frame update
    private void Start()
    {
        _penPoint = GameObject.Find("PenPoint");
    }

    // Update is called once per frame
    private void Update()
    {
        if (_penPoint == null) return;
        if (Draw.Drawing)
        {
            transform.position = _penPoint.transform.position;
            transform.rotation = _penPoint.transform.rotation;
        }
        else
        {
            enabled = false;
        }
    }
}