using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.InputSystem;

public class JoystickMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5.0f;

    [Header("References")]
    public Transform playerCamera; // Reference to the player's camera transform

    private Vector2 movementInput;
    private CharacterController characterController;

    private XRIBIMInputActions playerInputActions;
    private ObjectInteractionHandler objectInteractionHandler;

    private void Awake()
    {
        objectInteractionHandler = ObjectInteractionHandler.Instance;
        playerInputActions = objectInteractionHandler.playerInputActions;
        playerInputActions.XRIRightInteraction.Enable();
        // Ensure the CharacterController is assigned or added
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }

        if (playerCamera == null)
        {
            Debug.LogError("Player Camera is not assigned. Please assign the camera in the Inspector.");
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        // Get the input from the joystick

        
    }

    private void FixedUpdate()
    {
        // Calculate movement direction relative to the player's camera
        if (playerCamera != null)
        {
            movementInput = playerInputActions.XRIRightInteraction.Move.ReadValue<Vector2>();
            
            Vector3 forward = playerCamera.forward;
            Vector3 right = playerCamera.right;

            // Flatten the vectors to avoid vertical movement
            forward.y = 0;
            right.y = 0;

            forward.Normalize();
            right.Normalize();

            Vector3 movement = (forward * movementInput.y + right * movementInput.x) * speed * Time.deltaTime; 
            characterController.Move(movement * Time.fixedDeltaTime);
        }
    }
}
