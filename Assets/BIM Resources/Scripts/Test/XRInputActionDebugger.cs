using UnityEngine;
using UnityEngine.InputSystem;

public class QuestProInputTest : MonoBehaviour
{
    private XRIBIMInputActions playerInputActions;

    void Awake()
    {
        playerInputActions = new XRIBIMInputActions();
    }

    void OnEnable()
    {
        playerInputActions.Enable();

        playerInputActions.XRIRightInteraction.Select.performed += OnGrab;
        playerInputActions.XRIRightInteraction.Activate.performed += OnTrigger;
    }

    void OnDisable()
    {
        playerInputActions.XRIRightInteraction.Select.performed -= OnGrab;
        playerInputActions.XRIRightInteraction.Activate.performed -= OnTrigger;

        playerInputActions.Disable();
    }

    void Update()
    {
        // GRIP (Select Value)
        float grip = playerInputActions.XRIRightInteraction.SelectValue.ReadValue<float>();

        if (grip > 0.1f)
        {
            Debug.Log("GRIP VALUE: " + grip);
        }

        // TRIGGER (Activate Value)
        float trigger = playerInputActions.XRIRightInteraction.ActivateValue.ReadValue<float>();

        if (trigger > 0.1f)
        {
            Debug.Log("TRIGGER VALUE: " + trigger);
        }

    }

    private void OnGrab(InputAction.CallbackContext ctx)
    {
        Debug.Log("GRAB (Select) PERFORMED");
    }

    private void OnTrigger(InputAction.CallbackContext ctx)
    {
        Debug.Log("TRIGGER (Activate) PERFORMED");
    }
}