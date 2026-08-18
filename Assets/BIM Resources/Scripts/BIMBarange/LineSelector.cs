using UnityEngine;

public class LineSelector : MonoBehaviour
{
    public OVRInput.Controller controller; // Assign your controller in the Inspector
    public Material highlightMaterial;     // Material to highlight selected line
    private Material originalMaterial;     // To store the original material
    private GameObject selectedLine;       // Currently selected line

    void Update()
    {
        // Get the controller's position and rotation
        Vector3 controllerPosition = OVRInput.GetLocalControllerPosition(controller);
        Quaternion controllerRotation = OVRInput.GetLocalControllerRotation(controller);
        Vector3 rayDirection = controllerRotation * Vector3.forward;

        // Create the ray
        Ray ray = new Ray(controllerPosition, rayDirection);
        RaycastHit hit;

        // Check for raycast hit
        if (Physics.Raycast(ray, out hit))
        {
            // Check if the hit object has a LineRenderer
            if (hit.collider != null && hit.collider.GetComponent<LineRenderer>() != null)
            {
                SelectLine(hit.collider.gameObject);
            }
        }
        else
        {
            // If no object is hit, deselect any currently selected line
            DeselectLine();
        }
    }

    private void SelectLine(GameObject lineObject)
    {
        if (selectedLine != null && selectedLine != lineObject)
        {
            // Deselect the previously selected line
            DeselectLine();
        }

        selectedLine = lineObject;

        // Highlight the line by changing its material
        LineRenderer lineRenderer = selectedLine.GetComponent<LineRenderer>();
        if (lineRenderer != null && highlightMaterial != null)
        {
            originalMaterial = lineRenderer.material;
            lineRenderer.material = highlightMaterial;
        }
    }

    private void DeselectLine()
    {
        if (selectedLine != null)
        {
            // Restore the original material
            LineRenderer lineRenderer = selectedLine.GetComponent<LineRenderer>();
            if (lineRenderer != null && originalMaterial != null)
            {
                lineRenderer.material = originalMaterial;
            }

            selectedLine = null;
        }
    }
}
