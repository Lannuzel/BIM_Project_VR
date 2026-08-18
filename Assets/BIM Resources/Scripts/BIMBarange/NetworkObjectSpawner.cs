using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkObjectSpawner : Fusion.NetworkBehaviour
{
    public NetworkPrefabRef objectPrefab; // Prefab to spawn (assign in the inspector)
    public float moveSpeed = 5f; // Speed of movement

    private NetworkObject controlledObject; // Reference to the spawned object
    bool IsSpawned = true;
    void Update()
    {
        if (IsSpawned)
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        // Spawn object when pressing 'B'
        if (Input.GetKeyDown(KeyCode.B) && controlledObject == null)
        {
            SpawnControlledObject();
        }

        // Move the controlled object if it exists
        if (controlledObject != null && controlledObject.HasStateAuthority)
        {
            MoveControlledObject();
        }
    }

    private void SpawnControlledObject()
    {
        Vector3 spawnPosition = transform.position + transform.forward * 2f; // Spawn in front of the player

        // Spawn the object with local player authority
        NetworkManager.Instance.Runner.Spawn(objectPrefab, spawnPosition, Quaternion.identity, Object.InputAuthority, (runner, obj) =>
        {
            controlledObject = obj; // Assign the spawned object to the local player
        });

        Debug.Log("Spawned controlled object");
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
