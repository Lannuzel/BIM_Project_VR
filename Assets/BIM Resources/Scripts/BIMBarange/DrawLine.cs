using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawLine : MonoBehaviour
{

    public Vector3 pos1;
    public Vector3 pos2;
    public LineRenderer lineRenderer;
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, pos1);
        lineRenderer.SetPosition(1, pos2);
    }

    // Update is called once per frame
    void Update()
    {
        lineRenderer.SetPosition(1, pos2);
    }
}
