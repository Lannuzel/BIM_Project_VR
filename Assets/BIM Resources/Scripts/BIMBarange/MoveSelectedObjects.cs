using UnityEngine;
using System.Collections.Generic;

using Fusion;
public class MoveSelectedObjects : Fusion.NetworkBehaviour
{
    public Transform controller; // Assign your controller here

    public Transform cameraTransform; // Player's camera transform
    public float moveSpeed = 10.0f; // Speed at which the objects move
   
    private List<GameObject> selectedObjects;// = new List<GameObject>(); // List of selected objects
  
    private ObjectInteractionHandler objectInteractionHandler;
 
    private void Start()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
    }
    private void Awake()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
        objectInteractionHandler = ObjectInteractionHandler.Instance;
      
    }
    private void OnEnable()
    {
      }
    private void OnDisable()
    {

    }

   
    void Update()
    {
        if (selectedObjects.Count > 0)
        {
            MoveObjects();
        }
    }

    void MoveObjects()
    {
       
        Vector2 joystickInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        Vector3 moveDirection = new Vector3(joystickInput.x, 0, joystickInput.y) * moveSpeed * Time.deltaTime;

        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {
                NetworkObject networkObj = obj.GetComponent<NetworkObject>();
                if (networkObj != null && networkObj.HasStateAuthority)
                {
                    Vector3 newPosition = obj.transform.position + moveDirection;

                    // Use NetworkTransform if present
                    NetworkTransform networkTransform = obj.GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {
                        networkTransform.Teleport(newPosition);
                    }
                    else
                    {
                        obj.transform.position = newPosition; // Fallback if no NetworkTransform
                    }

                    //Debug.Log($"Moved {obj.name} to {newPosition}");
                }
                else
                {
                    Debug.LogWarning($"Cannot move {obj.name}. No State Authority!");
                }
            }
        }
    }



}



