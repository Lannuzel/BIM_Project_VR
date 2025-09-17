using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestMoveObject : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float verticalSensitivity = 5f;

    private Vector3 moveDirection;

    void Update()
    {
        // Joystick input (mapped in Input settings)
        float moveX = Input.GetAxis("Horizontal");  // Left/right
        float moveZ = Input.GetAxis("Vertical");    // Forward/backward

        // Get tilt input for vertical Y-axis (device pitch)
        float moveY = GetTiltVertical();

        // Combine into movement vector
        moveDirection = new Vector3(moveX, moveY, moveZ);
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    float GetTiltVertical()
    {
        // Get device tilt
        Vector3 tilt = Input.acceleration;

        // Depending on orientation, try tilt.z or tilt.y
        // For flat portrait device (like holding a phone), use tilt.z
        float tiltZ = tilt.z;

        // Optional: filter noise
        if (Mathf.Abs(tiltZ) < 0.05f)
            tiltZ = 0f;

        return tiltZ * verticalSensitivity;
    }
}

