using UnityEngine;
using UnityEngine.InputSystem;

public class TestInput : MonoBehaviour

{public InputActionReference aButton;

    void OnEnable()
    {
        aButton.action.Enable();
        aButton.action.performed += _ =>
        {
            Debug.Log("A BUTTON WORKS");
        };
    }

    void OnDisable()
    {
        aButton.action.Disable();
    }
}