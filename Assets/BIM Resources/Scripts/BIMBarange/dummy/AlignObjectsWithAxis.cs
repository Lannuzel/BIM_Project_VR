using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class AlignObjectsWithAxis : MonoBehaviour
{
    public List<GameObject> selectedObjects = new List<GameObject>(); // List of selected objects
    public bool alignToXAxis = true; // Set to true to align to X-axis, false for Z-axis

    private LineRenderer lineRenderer;

    void Start()
    {
        // Setup LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
        lineRenderer.positionCount = 2; // Two points for the line
    }

    void Update()
    {
        DisplayAlignmentAxis();
    }

    void DisplayAlignmentAxis()
    {
        if (selectedObjects == null || selectedObjects.Count == 0)
            return;

        // Get the reference object
        GameObject referenceObject = selectedObjects[0];

        if (referenceObject == null)
            return;

        // Get the reference position
        Vector3 referencePosition = referenceObject.transform.position;

        // Define start and end points of the axis
        Vector3 startPoint, endPoint;

        if (alignToXAxis)
        {
            startPoint = referencePosition + Vector3.left * 10;
            endPoint = referencePosition + Vector3.right * 10;
        }
        else
        {
            startPoint = referencePosition + Vector3.back * 10;
            endPoint = referencePosition + Vector3.forward * 10;
        }

        // Update LineRenderer positions
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }

    public void AlignObjectsToReference()
    {
        AlignToReference();
    }

    void AlignToReference()
    {
        if (selectedObjects == null || selectedObjects.Count < 2)
        {
            Debug.LogError("Please add at least two objects to the list.");
            return;
        }

        // Get the reference object
        GameObject referenceObject = selectedObjects[0];
        Vector3 referencePosition = referenceObject.transform.position;

        // Iterate through the rest of the objects and align them
        for (int i = 1; i < selectedObjects.Count; i++)
        {
            if (selectedObjects[i] != null)
            {
                Vector3 newPosition = selectedObjects[i].transform.position;

                if (alignToXAxis)
                {
                    newPosition.x = referencePosition.x; // Align to the X-axis
                }
                else
                {
                    newPosition.z = referencePosition.z; // Align to the Z-axis
                }

                selectedObjects[i].transform.position = newPosition;
            }
        }

        Debug.Log("Objects aligned successfully.");
    }
}
