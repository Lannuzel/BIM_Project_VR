using UnityEngine;

public class MoveObj : MonoBehaviour
{
    // The GameObject to move
    public GameObject targetObject;
    public GameObject spawnObj;
    // Layer mask for raycast targets (optional)
    public LayerMask raycastLayerMask;

    // The distance to maintain from the nearest point on the hit surface
    public float targetDistanceFromSurface = 5f;

    // Maximum raycast distance
    public float raycastMaxDistance = 100f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Perform the raycast
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, raycastMaxDistance, raycastLayerMask))
            {
                MoveToNearestPointWithDistance(hit);
            }
        }
    }

    void MoveToNearestPointWithDistance(RaycastHit hit)
    {
        // Get the collider of the hit object
        Collider hitCollider = hit.collider;

        if (hitCollider != null)
        {
            // Calculate the nearest point on the surface of the hit collider to the targetObject
            Vector3 nearestPointOnSurface = hitCollider.ClosestPoint(targetObject.transform.position);
            
            spawnObj = Instantiate(spawnObj, nearestPointOnSurface, Quaternion.identity);

            // Get the surface normal at the hit point (if applicable)
            Vector3 surfaceNormal = hit.normal;

            // Calculate the new position for the target object
            Vector3 targetPosition = nearestPointOnSurface + surfaceNormal * targetDistanceFromSurface;

            // Move the target object to the calculated position
            targetObject.transform.position = targetPosition;
        }
    }
}
