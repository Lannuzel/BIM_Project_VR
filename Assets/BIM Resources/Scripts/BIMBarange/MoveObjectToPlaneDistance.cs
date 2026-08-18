using System.Globalization;
using UnityEngine;

public class MoveObjectToPlaneDistance : MonoBehaviour
{
    // Reference to the plane (the GameObject with the plane's transform)
    public Transform referencePlane;

    // Distance to maintain from the plane
    public float distanceFromPlane = 5f;

    // GameObject to move

    public GameObject objectToMove;
    public GameObject spawnObj;

    public LayerMask raycastLayerMask;

    // Maximum raycast distance
    public float raycastMaxDistance = 100f;

    void Start()
    {
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Perform the raycast
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, raycastMaxDistance, raycastLayerMask))
            {
                //MoveToClosestSurface(hit);
                MoveToShortestDistanceFromPlane(hit);
            }
        }
    }
    void MoveToClosestSurface(RaycastHit hit)
    {
        // Get the collider of the hit object
        Collider hitCollider = hit.collider;

        // Get the collider of the target object
        Collider targetCollider = objectToMove.GetComponent<Collider>();

        if (hitCollider != null && targetCollider != null)
        {
            // Find the closest point on the hit surface to the target object
            Vector3 closestPointOnHitSurface = hitCollider.ClosestPoint(objectToMove.transform.position);
            spawnObj = Instantiate(spawnObj, closestPointOnHitSurface, Quaternion.identity);
            Vector3 surfaceNormal = hit.normal;

            // Find the closest point on the target object's surface to the hit surface
            Vector3 closestPointOnTargetSurface = targetCollider.ClosestPoint(closestPointOnHitSurface);
            spawnObj = Instantiate(spawnObj, closestPointOnTargetSurface, Quaternion.identity);
            // Calculate the direction vector from the hit surface to the target object's surface
            Vector3 direction = closestPointOnTargetSurface - closestPointOnHitSurface;

            // Calculate the current closest distance between the two surfaces
            float currentClosestDistance = direction.magnitude;

            // Calculate the adjustment needed to achieve the target distance
            float adjustmentDistance = distanceFromPlane - currentClosestDistance;

            // Move the target object along the direction vector to achieve the target distance
            Vector3 targetPosition = objectToMove.transform.position + direction.normalized * adjustmentDistance;

            // Set the new position of the target object
            objectToMove.transform.position = targetPosition;
        }
        else
        {
            Debug.LogWarning("Either the hit object or the target object is missing a collider.");
        }
    }

    void MoveToShortestDistanceFromPlane(RaycastHit hit)
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
        spawnObj = Instantiate(spawnObj, nearestPointOnPlane, Quaternion.identity);
        Collider collider = objectToMove.GetComponent<Collider>();
        // Calculate the target position at the specified distance from the plane
        Vector3 targetPosition = nearestPointOnPlane + planeNormal * distanceFromPlane + new Vector3(planeNormal.x* collider.bounds.size.x/2, 0 , planeNormal.z * collider.bounds.size.z / 2);

        // Move the object to the target position
        objectToMove.transform.position = targetPosition;



    }





}