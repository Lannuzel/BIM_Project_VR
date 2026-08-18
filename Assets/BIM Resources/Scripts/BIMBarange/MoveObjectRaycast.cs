using UnityEngine;
using UnityEngine.EventSystems;
using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
public class MoveObjectRaycastA : NetworkBehaviour
{
    private XRIBIMInputActions playerInputActions;
    private ObjectInteractionHandler objectInteractionHandler;

    private List<GameObject> selectedObjects;// = new List<GameObject>(); // List of selected objects
    public Transform  controller; // Assign the main camera or another camera in the Inspector
    public GameObject objectToMove; // The object to move, assign in the Inspector
    public float distanceFromSurface = 2.0f; // Distance to move the object away from the surface
    public TMP_InputField inputField;
    private void Awake()
    {
        objectInteractionHandler = ObjectInteractionHandler.Instance;
       // playerInputActions = objectInteractionHandler.playerInputActions;
        playerInputActions.XRIRightInteraction.Enable();
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
    }
    private void OnEnable()
    {
        playerInputActions.XRIRightInteraction.Enable();
        //adding actionListeners
        playerInputActions.XRIRightInteraction.Activate.performed += MoveToSurfaceDistance;

    }

    private void OnDisable()
    {
        //adding actionListeners
        playerInputActions.XRIRightInteraction.Activate .performed -= MoveToSurfaceDistance;

    }



    void MoveToSurfaceDistance(InputAction.CallbackContext context)
    {
            Vector3 controllerPosition = controller.position;
            Quaternion controllerRotation = controller.rotation;
            Vector3 rayDirection = controllerRotation * Vector3.forward;

            // Raycast logic
            Ray ray = new Ray(controllerPosition, rayDirection);
            RaycastHit hit;
            // Perform the raycast
            if (Physics.Raycast(ray, out hit))
            {
            if (hit.collider.transform.tag != "Floor")
                {
                foreach (GameObject obj in selectedObjects)
                    {
                     MoveToShortestDistanceFromPlane_V1(hit, obj);
                    //  MoveToClosestSurface(hit, obj);
                    //MoveToSurface(hit, obj);
                    //  MoveToSurfaceNW(hit, obj);
                }

            }
            }
    }
    public void MoveToSurface(RaycastHit hit, GameObject objectToMove)
    {
                  // Find the closest point on the surface of the reference collider
            Vector3 closestPoint = hit.collider.ClosestPoint(transform.position);

            // Calculate the direction away from the surface
            Vector3 directionFromSurface = (objectToMove.transform.position - closestPoint).normalized;

            // Set the new position at the specified distance from the surface
            transform.position = closestPoint + directionFromSurface * distanceFromSurface;
    }
    void MoveToSurfaceNW(RaycastHit hit, GameObject objectToMove)
    {
        // Find the closest point on the surface of the reference collider
        Vector3 closestPoint = hit.collider.ClosestPoint(transform.position);

        // Calculate the direction away from the surface
        Vector3 directionFromSurface = (objectToMove.transform.position - closestPoint).normalized;

        NetworkObject networkObj = objectToMove.GetComponent<NetworkObject>();
        if (networkObj != null && networkObj.HasStateAuthority)
        {
          // Check if the input field is not empty
            if (!string.IsNullOrEmpty(inputField.text))
            {
                // Try to parse the input text to a number
                if (float.TryParse(inputField.text, out float number))
                {
                    // Calculate the double of the number
                    distanceFromSurface = number;
                }
                else
                {
                    // If parsing fails, print an error message
                    Debug.LogError("Invalid input. Please enter a valid number.");
                }
            }

            // Set the new position at the specified distance from the surface
            Vector3 newPosition = closestPoint + directionFromSurface * distanceFromSurface;
            newPosition.y = 0;

            // Use NetworkTransform if present
            NetworkTransform networkTransform = objectToMove.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                networkTransform.Teleport(newPosition);
            }
            else
            {
                objectToMove.transform.position = newPosition; // Fallback if no NetworkTransform
            }
        }
    }
    void MoveToShortestDistanceFromPlane_V1(RaycastHit hit, GameObject objectToMove)
    {
    // Get the normal of the surface hit
            Vector3 hitNormal = hit.normal;

        Vector3 hitPoint = hit.point;

        NetworkObject networkObj = objectToMove.GetComponent<NetworkObject>();
        if (networkObj != null && networkObj.HasStateAuthority)
        {
            Vector3 direction = (objectToMove.transform.position - hitPoint).normalized;

            // Check if the input field is not empty
            if (!string.IsNullOrEmpty(inputField.text))
            {
                // Try to parse the input text to a number
                if (float.TryParse(inputField.text, out float number))
                {
                    // Calculate the double of the number
                    distanceFromSurface = number ;
                }
                else
                {
                    // If parsing fails, print an error message
                    Debug.LogError("Invalid input. Please enter a valid number.");
                }
            }

            // Calculate the new position 2 meters away from the surface hit point
            Vector3 newPosition = hit.point + direction * Mathf.Min(distanceFromSurface, Vector3.Distance(hit.point, objectToMove.transform.position));
            newPosition.y = 0;

            // Use NetworkTransform if present
            NetworkTransform networkTransform = objectToMove.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                networkTransform.Teleport(newPosition);
            }
            else
            {
                objectToMove.transform.position = newPosition; // Fallback if no NetworkTransform
            }
        }
    }
    void MoveToShortestDistanceFromPlane_V2(RaycastHit hit, GameObject objectToMove)
    {

        Vector3 planeNormal = hit.normal; // Use up or forward depending on your plane orientation

        // Get the plane's position
        Vector3 planePosition = hit.collider.transform.position;

        // Get the object's current position
        Vector3 objectPosition = objectToMove.transform.position;

        // Calculate the vector from the plane to the object
        Vector3 planeToObject = objectPosition - planePosition;

        // Project this vector onto the plane's normal to find the shortest distance
        float shortestDistance = Vector3.Dot(planeToObject, planeNormal);

        // Calculate the nearest point on the plane
        Vector3 nearestPointOnPlane = objectPosition - planeNormal * shortestDistance;
        // spawnObj = Instantiate(spawnObj, nearestPointOnPlane, Quaternion.identity);

        Collider collider = objectToMove.GetComponent<Collider>();
        // Calculate the target position at the specified distance from the plane
        Vector3 targetPosition = nearestPointOnPlane + planeNormal * distanceFromSurface + new Vector3(planeNormal.x * collider.bounds.size.x / 2, 0, planeNormal.z * collider.bounds.size.z / 2);


        targetPosition.y = 0;
        // Move the object to the target position
        objectToMove.transform.position = targetPosition;
    }
    void MoveToClosestSurface(RaycastHit hit, GameObject targetObject)
    {
        // Get the collider of the hit object
        Collider hitCollider = hit.collider;

        // Get the collider of the target object
        Collider targetCollider = targetObject.GetComponent<Collider>();

        if (hitCollider != null && targetCollider != null)
        {
            // Find the closest point on the hit surface to the target object
            Vector3 closestPointOnHitSurface = hitCollider.ClosestPoint(targetObject.transform.position);
            Vector3 surfaceNormal = hit.normal;

            // Find the closest point on the target object's surface to the hit surface
            Vector3 closestPointOnTargetSurface = targetCollider.ClosestPoint(closestPointOnHitSurface);
            // Calculate the direction vector from the hit surface to the target object's surface
            Vector3 direction = closestPointOnTargetSurface - closestPointOnHitSurface;

            // Calculate the current closest distance between the two surfaces
            float currentClosestDistance = direction.magnitude;

            // Calculate the adjustment needed to achieve the target distance
            float adjustmentDistance = distanceFromSurface - currentClosestDistance;

            // Move the target object along the direction vector to achieve the target distance
            Vector3 targetPosition = targetObject.transform.position + direction.normalized * adjustmentDistance;

            // Set the new position of the target object
            targetObject.transform.position = targetPosition;
        }
        else
        {
            Debug.LogWarning("Either the hit object or the target object is missing a collider.");
        }
    }

}
