using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.XR;
public class SelectLineWithRayCast : MonoBehaviour
{
    public Transform controller; // Assign your VR controller transform here
    public Transform linkedHandPosition;
    public string selectButton = "TriggerButton"; // Input name for the select button
    public LayerMask lineLayer; // LayerMask for filtering objects to raycast against
    public Material selectedMaterial; // Material to indicate the selected line
    public Material defaultMaterial; // Default material for the lines

    private GameObject selectedLine; // The currently selected line

    void Update()
    {
        // Check if the select button is pressed
       if(OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger, OVRInput.Controller.Touch)>0){

        Transform target = GetVRRayHitTransform();
        if(target!= null)
        {
            if(target.CompareTag("line")!){
                selectedLine = target.gameObject;
         //       lineManipMenu.SetActive(true);
            }
        }
            SelectLine();
        }
    } Transform GetVRRayHitTransform()
    {
         RaycastHit hit;
        if(Physics.Raycast(linkedHandPosition.position, linkedHandPosition.forward, out hit, 10f)){
            return hit.transform;
        }
        return null;
    }

    void SelectLine()
    {
        Ray ray = new Ray(controller.position, controller.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, lineLayer))
        {
            GameObject hitObject = hit.collider.gameObject;

            // If the hit object is a line and it's not already selected
            if (hitObject != selectedLine)
            {
                // Reset the material of the previously selected line
                if (selectedLine != null)
                {
                    ResetLineMaterial(selectedLine);
                }

                // Update the selected line
                selectedLine = hitObject;

                // Change the material of the selected line
                ChangeLineMaterial(selectedLine, selectedMaterial);
            }
        }
    }

    void ResetLineMaterial(GameObject line)
    {
        var renderer = line.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = defaultMaterial;
        }
    }

    void ChangeLineMaterial(GameObject line, Material newMaterial)
    {
        var renderer = line.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = newMaterial;
        }
    }
}
