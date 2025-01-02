using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastSelector : MonoBehaviour
{
    public OVRInput.Controller controller; // Assign your controller in the Inspector
    public Material highlightMaterial;     // Material to highlight selected line
    public Material originalMaterial;     // To store the original material
    private GameObject selectedLine;       // Currently selected line
    Vector3 controllerPosition;
    Quaternion controllerRotation;
    Vector3 rayDirection;
    private Renderer objectRenderer; // Renderer of the GameObject
                                     // Start is called before the first frame update

    bool flag = false;
    void Start()
    {
        // Get the controller's position and rotation

        
    }

    // Update is called once per frame
    void Update()
    {
        controllerPosition = OVRInput.GetLocalControllerPosition(controller);
        controllerRotation = OVRInput.GetLocalControllerRotation(controller);
        rayDirection = controllerRotation * Vector3.forward;

        Ray ray = new Ray(controllerPosition, rayDirection);
        RaycastHit hit;
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.tag == "EndPoint")
                {
                    Debug.LogError("EndPoint object found" + hit.transform.name);
                    objectRenderer = GetComponent<Renderer>();
                    if (objectRenderer != null)
                    {
                        Debug.LogError("Found renderer");
                        objectRenderer.material = highlightMaterial;
                    }
                    else
                        Debug.LogError("unable to find renderer");

                }
            }
           
        }
        



    }
    private void OnRaycastHit()
    {
        
    }
}
