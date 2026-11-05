using UnityEngine;
using System.Collections.Generic;

using Fusion;
public class MoveSelectedObjects : Fusion.NetworkBehaviour
{
    public Transform controller; // Assign your controller here

    public Transform cameraTransform; // Player's camera transform
    public float moveSpeed = 10.0f; // Speed at which the objects move
   
    private List<GameObject> selectedObjects;// = new List<GameObject>(); // List of selected objects
  
    private XRIBIMInputActions playerInputActions;
    private ObjectInteractionHandler objectInteractionHandler;
 
    private void Start()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
    }
    private void Awake()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
        objectInteractionHandler = ObjectInteractionHandler.Instance;
        playerInputActions = objectInteractionHandler.playerInputActions;
        playerInputActions.XRIRightInteraction.Enable();

    }
    private void OnEnable()
    {
        playerInputActions.XRIRightInteraction.Enable();
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
        Vector2 joystickInput = playerInputActions.XRIRightInteraction.Move.ReadValue<Vector2>();
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


    void MoveObjects1()
    {

        Vector2 joystickInput = playerInputActions.XRIRightInteraction.Move.ReadValue<Vector2>();

        Vector3 moveDirection = new Vector3(joystickInput.x, 0, joystickInput.y) * moveSpeed * Time.deltaTime;

        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {
                NetworkObject networkObj = obj.GetComponent<NetworkObject>();
                if (networkObj.HasStateAuthority)
                {
                    Vector3 direcion = cameraTransform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.z));
                    direcion.y = 0;
                    networkObj.transform.position += direcion;
                    Debug.Log(" Move Direction x " + direcion.x + "  z :  " + direcion.z);

                    /*  NetworkCharacterController networkCharacterController = obj.GetComponent<NetworkCharacterController>();
                       if (networkCharacterController != null)
                       {
                           networkCharacterController.Move(moveDirection);
                       }*/
                }



                // Constrain movement to X and Z directions


                // Vector3 direcion  =  cameraTransform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.z));
                //direcion.y = 0;
                //  obj.transform.position += direcion;


                // UpdateBoundingBox(obj);
            }
        }
    }

    private NetworkObject controlledObject; // Reference to the spawned object
    bool IsSpawned = true;


    private void HandleInput()
    {
        // Move the controlled object if it exists
        if (controlledObject != null && controlledObject.HasStateAuthority)
        {
            MoveControlledObject();
        }
    }

    private void MoveControlledObject()
    {
        // Get input from keyboard or joystick
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(moveX, 0, moveZ).normalized * moveSpeed * Time.deltaTime;

        if (moveDirection != Vector3.zero)
        {
            // Update position
            controlledObject.transform.position += moveDirection;
            Debug.Log($"Moving object to: {controlledObject.transform.position}");
        }
    }
}



