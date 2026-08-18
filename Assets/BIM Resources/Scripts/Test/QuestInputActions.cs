using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class QuestInputActions : MonoBehaviour
{
    public InputActionReference aButton;
    public InputActionReference bButton;
    public InputActionReference triggerButton;
    public InputActionReference gripButton;

    public TMP_Text messageText;

    private void OnEnable()
    {
        aButton.action.Enable();
        bButton.action.Enable();
        triggerButton.action.Enable();
        gripButton.action.Enable();

        aButton.action.performed += OnAPressed;
        bButton.action.performed += OnBPressed;
        triggerButton.action.performed += OnTriggerPressed;
        gripButton.action.performed += OnGripPressed;
    }

    private void OnDisable()
    {
        aButton.action.performed -= OnAPressed;
        bButton.action.performed -= OnBPressed;
        triggerButton.action.performed -= OnTriggerPressed;
        gripButton.action.performed -= OnGripPressed;

        aButton.action.Disable();
        bButton.action.Disable();
        triggerButton.action.Disable();
        gripButton.action.Disable();
    }

    void OnAPressed(InputAction.CallbackContext ctx)
    {
        ShowMessage("A Button Pressed");
    }

    void OnBPressed(InputAction.CallbackContext ctx)
    {
        ShowMessage("B Button Pressed");
    }

    void OnTriggerPressed(InputAction.CallbackContext ctx)
    {
        ShowMessage("Trigger Pressed");
    }

    void OnGripPressed(InputAction.CallbackContext ctx)
    {
        ShowMessage("Grip Pressed");
    }

    void ShowMessage(string msg)
    {
      //  messageText.text = msg;
        Debug.Log(msg);
    }
}