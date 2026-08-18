using System.Collections.Generic;
using UnityEngine;
public class ShowCoordinateAxes : MonoBehaviour
{
    public float axisLength = 1.0f; // Length of each axis line
    private LineRenderer[] axisLines; // Array to hold LineRenderers for axes
    public Vector3 axesOffset = Vector3.zero;   
    private List<GameObject> selectedObjects;
    private GameObject axes;
    public GameObject xAxisText;
    public GameObject zAxisText;
    private GameObject xAxisTextGo;
    private GameObject zAxisTextGo;
    private void Awake()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
    }
    void Start()
    {
        axes = new GameObject("Axes");
        axes.transform.rotation = Quaternion.identity;
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();

        xAxisTextGo = Instantiate(xAxisText);
        zAxisTextGo = Instantiate(zAxisText);
        xAxisTextGo.transform.parent = axes.transform;
        zAxisTextGo.transform.parent = axes.transform;

        // Initialize LineRenderers for X, Y, and Z axes
        axisLines = new LineRenderer[3];

        for (int i = 0; i < 3; i++)
        {
           GameObject axis = new GameObject("Axis" + i);
            axis.transform.SetParent(axes.transform);

            LineRenderer lr = axis.AddComponent<LineRenderer>();
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.material =  new Material(Shader.Find("Sprites/Default"));

            if (i == 0) lr.startColor = lr.endColor = Color.red; // X-axis (Red)
            if (i == 1) lr.startColor = lr.endColor = Color.blue; // Z-axis (Blue)
            if (i == 2) lr.startColor = lr.endColor = Color.green; // Y-axis (Green) 

            lr.positionCount = 2; // Each axis is a line with two points
            axisLines[i] = lr;
        }

    }

    void Update()
    {
        if (selectedObjects.Count > 0)
        {
            if (selectedObjects[0] != null)
            {
                axes.SetActive(true);

                UpdateAxes(selectedObjects[0].transform);

            }

        }
        else
        {
            axes.SetActive(false);
        }
            

    }

    private void UpdateAxes(Transform positionRef)
    {
        if (axisLines == null || axisLines.Length < 3) return;

        // X-axis
        axisLines[0].SetPosition(0, positionRef.position+ axesOffset);
        axisLines[0].SetPosition(1, positionRef.position+ axesOffset + positionRef.right * axisLength);

        // Y-axis
        axisLines[1].SetPosition(0, positionRef.position + axesOffset);
        axisLines[1].SetPosition(1, positionRef.position + axesOffset -positionRef.up * axisLength);

        // Z-axis
        axisLines[2].SetPosition(0, positionRef.position + axesOffset);
        axisLines[2].SetPosition(1, positionRef.position + axesOffset+ positionRef.forward * axisLength);

        xAxisTextGo.transform.position = axisLines[0].GetPosition(1);
        zAxisTextGo.transform.position = axisLines[1].GetPosition(1);


    }
}
