using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Fusion;
public class AlignObjects : MonoBehaviour
{
    public List<GameObject> selectedObjects;     // List of selected objects
    public bool alignToZAxis = true; // Set to true to align to X-axis, false for Z-axis


    private void Start()
    {
       selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();

    }
    private void Awake()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
    }
    public void AlignObjectsToReferenceAxisZ()
    {
        alignToZAxis = true;
        AlignObjectsToReference();

    }
    public void AlignObjectsToReferenceAxisX()
    {
        alignToZAxis = false;
        AlignObjectsToReference();
    }
   public void AlignObjectsToReference()
    {
        if (selectedObjects == null || selectedObjects.Count < 2)
        {
            Debug.LogError("Please add at least two objects to the list.");
            return;
        }

        // Get the reference object (the first object in the list)
        GameObject referenceObject = selectedObjects[0];
        Vector3 referencePosition = referenceObject.transform.position;

        // Iterate through the rest of the objects and align them
        for (int i = 1; i < selectedObjects.Count; i++)
        {
            if (selectedObjects[i] != null)
            {
                Vector3 newPosition = selectedObjects[i].transform.position;

                if (alignToZAxis)
                {
                    newPosition.z = referencePosition.z; // Align to the Z-axis
                }
                else
                {

                    newPosition.x = referencePosition.x; // Align to the X-axis
                }

                NetworkObject networkObj = selectedObjects[i].GetComponent<NetworkObject>();
                if (networkObj != null && networkObj.HasStateAuthority)
                {
                    NetworkTransform networkTransform = selectedObjects[i].GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {
                        networkTransform.Teleport(newPosition);
                    }
                    else
                    {
                        selectedObjects[i].transform.position = newPosition; // Fallback if no NetworkTransform
                    }
                }
                else
                {
                    Debug.LogWarning($"Cannot move {selectedObjects[i].name}. No State Authority!");
                }

            }
        }

        Debug.Log("Objects aligned successfully.");
    }
}

