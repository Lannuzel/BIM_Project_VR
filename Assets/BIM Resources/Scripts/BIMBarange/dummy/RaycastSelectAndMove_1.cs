using UnityEngine;
using OVR;
using UnityEngine.Animations.Rigging;
using Oculus.Interaction;
using Fusion;
using UnityEngine.InputSystem;
using System.IO.MemoryMappedFiles;
using Meta.WitAi;
using Oculus.Interaction.DebugTree;

public class RaycastSelectAndMove : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform rightController;       // Assign RightHandAnchor
    public LayerMask selectableLayer;
    public float maxRayDistance = 10f;
    public float moveSpeed = 1.0f;
    public float rayDistance = 10f;
    public float objectFollowSpeed = 10f;   // How fast the object follows

    private GameObject selectedObject;
    private float grabDistance = 3f;
    private Vector3 initialLocalOffset; // Local offset from controller to object when selected

    private XRIBIMInputActions playerInputActions;
    private ObjectInteractionHandler objectInteractionHandler;
    private Vector3 cumulativeJoystickOffset = Vector3.zero;
    private Quaternion initialRotationOffset;

    private enum ControlMode { Translate, Rotate }
    private ControlMode currentMode = ControlMode.Translate;
    private bool aButtonWasPressed = false;

    private float previousControllerRoll = 0f;
    
    private void Start()
    {
       
    }


    void Update()
    {
        // Select
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            Debug.Log("intex key pressed");

            TrySelectObject();
        }
        // Deselect
        else if (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger))
        {
            selectedObject = null;
            cumulativeJoystickOffset = Vector3.zero;
        }

        // Toggle mode with A button (button one)
        bool aButton = OVRInput.Get(OVRInput.Button.One);
        if (aButton && !aButtonWasPressed)
        {
            currentMode = currentMode == ControlMode.Translate ? ControlMode.Rotate : ControlMode.Translate;
            Debug.Log("Mode switched to: " + currentMode);
        }
        aButtonWasPressed = aButton;

        // Apply behavior
        if (selectedObject != null)
        {
            if (currentMode == ControlMode.Translate)
            {
                FollowControllerPosition();
            }
            else if (currentMode == ControlMode.Rotate)
            {
                ApplyControllerRotation();
            }
        }
    }

    private void FollowControllerPosition()
    {
        // Step 1: Position offset
        Vector3 baseOffset = rightController.rotation * initialLocalOffset;
        Vector3 controllerPosition = rightController.position + baseOffset;

        // Step 2: Joystick movement
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 forward = rightController.forward;
            Vector3 right = rightController.right;

            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 joystickDelta = (right * input.x + forward * input.y) * moveSpeed * Time.deltaTime;
            cumulativeJoystickOffset += joystickDelta;
        }

        Vector3 targetPosition = controllerPosition + cumulativeJoystickOffset;

        // Step 3: Rotation from controller
       // Quaternion targetRotation = rightController.rotation * initialRotationOffset;

        // Step 4: Apply to object
        if (selectedObject != null)
        {
            NetworkObject networkObj = selectedObject.GetComponent<NetworkObject>();
            if (networkObj != null && networkObj.HasStateAuthority)
            {
                NetworkTransform networkTransform = selectedObject.GetComponent<NetworkTransform>();
                if (networkTransform != null)
                {
                    networkTransform.Teleport(targetPosition);
                  //  networkTransform.Teleport(targetPosition, targetRotation);
                }
                else
                {
                    selectedObject.transform.position = Vector3.Lerp(
                        selectedObject.transform.position,
                        targetPosition,
                        Time.deltaTime * objectFollowSpeed
                    );

                 /*   selectedObject.transform.rotation = Quaternion.Slerp(
                        selectedObject.transform.rotation,
                        targetRotation,
                        Time.deltaTime * objectFollowSpeed
                    );*/
                }
            }
            else
            {
                selectedObject.transform.position = Vector3.Lerp(
                    selectedObject.transform.position,
                    targetPosition,
                    Time.deltaTime * objectFollowSpeed
                );

                /*   selectedObject.transform.rotation = Quaternion.Slerp(
                       selectedObject.transform.rotation,
                       targetRotation,
                       Time.deltaTime * objectFollowSpeed
                   );*/
            }
        }
    }

    private void ApplyControllerRotation()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        if (selectedObject == null) return;

        float rotationSpeed = 90f; // degrees/sec
        float yaw = 0f;
        float pitch = 0f;

        // Step 1: Apply joystick-based pitch or yaw — only one at a time
        if (input.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                yaw = input.x * rotationSpeed * Time.deltaTime; // Right = +Y, Left = -Y
            }
         /*   else
            {
                pitch = input.y * rotationSpeed * Time.deltaTime; // Up = +X, Down = -X
            }*/
        }

        Quaternion deltaJoystickRotation = Quaternion.Euler(0f, yaw, 0f);

        // Step 2: Controller Z rotation (roll)

        // Step 2: Controller Z rotation (roll)
        float currentRoll = rightController.rotation.eulerAngles.z;
        float deltaRoll = Mathf.DeltaAngle(previousControllerRoll, currentRoll);
        previousControllerRoll = currentRoll;

        Quaternion deltaRollRotation = Quaternion.Euler(0, deltaRoll,0 );

;

        // Step 3: Combine and apply
        Quaternion totalRotation = selectedObject.transform.rotation * deltaJoystickRotation; // * deltaRollRotation;

        NetworkObject netObj = selectedObject.GetComponent<NetworkObject>();
        if (netObj != null && netObj.HasStateAuthority)
        {
            NetworkTransform netTransform = selectedObject.GetComponent<NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.Teleport(selectedObject.transform.position, totalRotation);
            }
            else
            {
                selectedObject.transform.rotation = Quaternion.Slerp(
                    selectedObject.transform.rotation,
                    totalRotation,
                    Time.deltaTime * objectFollowSpeed
                );
            }
        }
        else
        {
            selectedObject.transform.rotation = Quaternion.Slerp(
                selectedObject.transform.rotation,
                totalRotation,
                Time.deltaTime * objectFollowSpeed
            );
        }
    }

    private void TrySelectObject()
    {
        Vector3 rayOrigin = rightController.position;
        Vector3 rayDirection = rightController.forward;

        Ray ray = new Ray(rayOrigin, rayDirection);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxRayDistance))
        {
            if (hit.collider.CompareTag("Reservation"))
            {
                selectedObject = hit.collider.gameObject;

                // Save position offset
                Vector3 worldOffset = selectedObject.transform.position - rightController.position;
                initialLocalOffset = Quaternion.Inverse(rightController.rotation) * worldOffset;

                previousControllerRoll = rightController.rotation.eulerAngles.z;
                // Save rotation offset
                //  initialRotationOffset = Quaternion.Inverse(rightController.rotation) * selectedObject.transform.rotation;

                cumulativeJoystickOffset = Vector3.zero;

                Debug.Log("Selected: " + selectedObject.name);
                ReservationInteractionHandler.Instance.currentReservation = selectedObject;



            }
        }
    }



    private void OnCollisionEnter(Collision collision)
    {

        GameObject other = collision.gameObject;

        if (other.CompareTag("Wall") || other.CompareTag("Floor"))
        {
            Renderer surfaceRenderer = other.GetComponent<Renderer>();
            if (surfaceRenderer)
            {
                float surfaceWidth = surfaceRenderer.bounds.size.z; // or .z depending on layout

                NetworkObject netObj = selectedObject.GetComponent<NetworkObject>();
                if (netObj != null && netObj.HasStateAuthority)
                {
                    NetworkTransform netTransform = selectedObject.GetComponent<NetworkTransform>();
                    if (netTransform != null)
                    {

                        netObj.transform.localScale = new Vector3(netObj.transform.localScale.x , surfaceWidth, netObj.transform.localScale.z);

                    }
                }
            }
        }
    }

    private void MoveSelectedObject(Vector2 input)
    {
        Vector3 movement = Vector3.zero;

        // Determine base movement directions from controller orientation
        Vector3 forward = rightController.forward;
        Vector3 right = rightController.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Use joystick normally unless the grip button is held
        if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger)) // e.g., Grip button
        {
            // Up/Down movement mode using joystick Y axis
            Vector3 rayDirection = rightController.forward.normalized;
            movement = rayDirection * input.y * moveSpeed * Time.deltaTime;
        }
        else
        {
            // Standard movement along horizontal plane
            movement = (right * input.x + forward * input.y) * moveSpeed * Time.deltaTime;
        }

        Vector3 newPosition = selectedObject.transform.position + movement;

        // Move using network logic
        NetworkObject networkObj = selectedObject.GetComponent<NetworkObject>();
        if (networkObj != null && networkObj.HasStateAuthority)
        {
            NetworkTransform networkTransform = selectedObject.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                networkTransform.Teleport(newPosition);
            }
            else
            {
                selectedObject.transform.position = newPosition;
            }
        }
        else
        {
            Debug.LogWarning($"Cannot move {selectedObject.name}. No State Authority!");
        }
    }




    private void DeleteSelectedObjects_performed(InputAction.CallbackContext context)
    {
        selectedObject = null;
        grabDistance = 0;
    }
    private void SelectObject_performed(InputAction.CallbackContext context)
    {
        // Step 1: Raycast selection using index trigger
    //    if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            Vector3 controllerPosition = rightController.position;
            Quaternion controllerRotation = rightController.rotation;
            Vector3 rayDirection = controllerRotation * Vector3.forward;

            // Raycast logic
            Ray ray = new Ray(controllerPosition, rayDirection);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject.tag == "Reservation")
                {
                    selectedObject = hit.collider.gameObject;
                    grabDistance = Vector3.Distance(rightController.position, hit.point);
                    Debug.Log("Selected: " + selectedObject.name);
                }
            }
        }
    }


    void Update1()
    {
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            Vector3 controllerPosition = rightController.position;
            Quaternion controllerRotation = rightController.rotation;
            Vector3 rayDirection = controllerRotation * Vector3.forward;

            // Raycast logic
            Ray ray = new Ray(controllerPosition, rayDirection);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject.tag == "Reservation")
                {
                    selectedObject = hit.collider.gameObject;
                    grabDistance = Vector3.Distance(rightController.position, hit.point);
                    Debug.Log("Selected: " + selectedObject.name);
                }
            }
        }


         // Step 2: Deselect on trigger release
         else if (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger))
               {
                   selectedObject = null;
           
               }
      

        // Step 3: Move object in 3D space relative to controller orientation
        if (selectedObject != null)
        {
             Vector2 input = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
            if (input.sqrMagnitude > 0.01f)
            {
                Transform cam = Camera.main.transform;
                Vector3 forward = -cam.forward;
                Vector3 right = cam.right;
                forward.y = 0; right.y = 0;
                forward.Normalize(); right.Normalize();
                Vector3 movement = (forward * input.y + right * input.x) * moveSpeed * Time.deltaTime;
                NetworkObject networkObj = selectedObject.GetComponent<NetworkObject>();
                if (networkObj != null && networkObj.HasStateAuthority)
                {
                    Vector3 newPosition = selectedObject.transform.position + movement;

                    // Use NetworkTransform if present
                    NetworkTransform networkTransform = selectedObject.GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {
                        networkTransform.Teleport(newPosition);
                    }
                    else
                    {
                        selectedObject.transform.position = newPosition; // Fallback if no NetworkTransform
                    }

                    //Debug.Log($"Moved {obj.name} to {newPosition}");
                }
                else
                {
                    Debug.LogWarning($"Cannot move {selectedObject.name}. No State Authority!");
                }
                                
                
            }
            else
            {                // Full movement in controller-relative direction
                
                NetworkObject networkObj = selectedObject.GetComponent<NetworkObject>();
                if (networkObj != null && networkObj.HasStateAuthority)
                {
                    Vector3 targetPosition = rightController.position + rightController.forward * grabDistance;

                    Collider collider = selectedObject.GetComponent<Collider>();

                    Vector3 newPosition = rightController.position + rightController.forward * grabDistance + new Vector3(collider.bounds.size.x / 2, 0, 0);
                

                    // Use NetworkTransform if present
                    NetworkTransform networkTransform = selectedObject.GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {
                        networkTransform.Teleport(newPosition);
                    }
                    else
                    {
                       
                            selectedObject.transform.position = Vector3.Lerp(
                            selectedObject.transform.position,
                            targetPosition,
                            Time.deltaTime * objectFollowSpeed
                        );
                    }

                    //Debug.Log($"Moved {obj.name} to {newPosition}");
                }
                else
                {
                    Debug.LogWarning($"Cannot move {selectedObject.name}. No State Authority!");
                }


          }
        
        }
    }

   

    public void moveXYZ()
    {
        Vector2 movementInput = playerInputActions.XRIRightInteraction.Move.ReadValue<Vector2>();

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        Vector3 newPosition;
        Debug.Log("movemen input : " + movementInput);

        // Flatten the vectors to avoid vertical movement
        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();
        float x = Mathf.Abs(movementInput.y);
        float z = Mathf.Abs(movementInput.x);
       
        if (movementInput.sqrMagnitude > 0.01f)
        {
            // movement = (forward * movementInput.y + right * movementInput.x) * moveSpeed * Time.deltaTime;   
            Vector3 movement;
            if (x > z)
                movement = (forward * movementInput.y) * moveSpeed * Time.deltaTime;
            else
                movement = (right * movementInput.x) * moveSpeed * Time.deltaTime;

            newPosition = selectedObject.transform.position + movement;



            //  Vector3 moveDirection = new Vector3(joystickInput.x, 0, joystickInput.y) * moveSpeed * Time.deltaTime;
            if (selectedObject != null)
            {
                NetworkObject networkObj = selectedObject.GetComponent<NetworkObject>();
                if (networkObj != null && networkObj.HasStateAuthority)
                {

                    // Use NetworkTransform if present
                    NetworkTransform networkTransform = selectedObject.GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {
                        networkTransform.Teleport(newPosition);
                    }
                    //Debug.Log($"Moved {obj.name} to {newPosition}");
                }
                else
                {
                    Debug.LogWarning($"Cannot move {selectedObject.name}. No State Authority!");
                }
            }
        }

        else
        {
                  
            Vector3 targetPosition = rightController.position + rightController.forward * grabDistance;
            NetworkObject networkObj = selectedObject.GetComponent<NetworkObject>();
            if (networkObj != null && networkObj.HasStateAuthority)
            {


                // Use NetworkTransform if present
                NetworkTransform networkTransform = selectedObject.GetComponent<NetworkTransform>();
                if (networkTransform != null)
                {
                    networkTransform.Teleport(targetPosition);
                }
                //Debug.Log($"Moved {obj.name} to {newPosition}");
            }
            else
            {
                Debug.LogWarning($"Cannot move {selectedObject.name}. No State Authority!");
            }

        }

    }

    
    
}