using Fusion;
using Meta.WitAi.Events;
using Oculus.Interaction;
using System;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class MoveToClosestSurfaceDistance : MonoBehaviour
{
    public Transform controller;
    // Layer mask for raycast targets (optional)
    public LayerMask raycastLayerMask;

    public Transform targetObject;
    // Maximum raycast distance
    public float raycastMaxDistance = 100f;

    public float targetClosestDistance = 5f;

    public GameObject spawnObjTarget;
    public GameObject spawnObjRef;

    private GameObject spawnObj;

    public Transform cameraRef;

   public Material lineMaterial;
    public GameObject endPointPref;
    private GameObject lineObj;
    private Vector3 normal;
    private Vector3 tangentEnd;
   public GameObject measurePref;
    public TMP_InputField inputField;

    private List<GameObject> selectedObjects;// = new List<GameObject>(); // List of selected objects
    private ObjectInteractionHandler objectInteractionHandler;


    public Vector3 positionOffset = Vector3.zero;
    public float positionOffsetXp = 0f;
    public float positionOffsetXn = 0f;
    public float positionOffsetZp = 0f;
    public float positionOffsetZn = 0f;
    public float xZOffset = 0;
    public float zXOffset = 0;

    private void Start()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
    }
    private void Awake()
    {
        objectInteractionHandler = ObjectInteractionHandler.Instance;
        

    }

    private void OnEnable()
    {
       
    }
    private void OnDisable()
    {
      
        if(lineObj != null)
        {
            Destroy(lineObj);
            lineObj = null;
        }
    }

    private void MoveToSurfaceDistance(InputAction.CallbackContext context)
    {
        DrawTengentLine();
    }


    private void LateUpdate()
    {
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            Debug.Log("intex key pressed");
            DrawTengentLine();
        }

    



        if (lineObj != null)
        {
            if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger))
            {            // Get the controller's position and orientation
                Vector3 controllerPosition = controller.position;
                Quaternion controllerRotation = controller.rotation;
                Vector3 rayDirection = controllerRotation * Vector3.forward;

                // Raycast logic
                Ray ray = new Ray(controllerPosition, rayDirection);
                // Perform a raycast
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.transform.tag == "Wall" || hit.transform.tag == "Interactable")
                    {
                        LineRenderer currentLine = lineObj.transform.GetComponent<LineRenderer>();
                        normal = hit.normal;
                        // Calculate the endpoint of the tangent line
                        tangentEnd = hit.point + normal * targetClosestDistance;
                        currentLine.SetPosition(0, hit.point);
                        currentLine.SetPosition(1, tangentEnd);

                        foreach (GameObject objToMove in selectedObjects)
                        {
                            MoveObjectToNormalDistance1(objToMove, tangentEnd);
                            // MoveToClosestSurface(hit, objToMove);

                        }
                    }
                }

            }
            else
            {
                Destroy(lineObj);
               // lineObj = null;
               transform.gameObject.SetActive(false);   
            }
        }

    }

    void DrawTengentLine()
    {
        {
            // Get the controller's position and orientation
            Vector3 controllerPosition = controller.position;
            Quaternion controllerRotation = controller.rotation;
            Vector3 rayDirection = controllerRotation * Vector3.forward;

            // Raycast logic
            Ray ray = new Ray(controllerPosition, rayDirection);
            // Perform a raycast
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.tag == "Wall" ||hit.transform.tag == "Interactable")
                {
                    // Get the surface normal at the hit point
                    normal = hit.normal;

                    // Calculate the endpoint of the tangent line
                    tangentEnd = hit.point + normal * targetClosestDistance;
                    //create new line and assign start point 
                    lineObj = new GameObject("Line");

                    LineRenderer currentLine = lineObj.AddComponent<LineRenderer>();
                    currentLine.material = lineMaterial;
                    currentLine.startWidth = 0.01f;
                    currentLine.endWidth = 0.01f;
                    currentLine.positionCount = 2;
                    currentLine.SetPosition(0, hit.point);
                    currentLine.SetPosition(1, tangentEnd);

                    foreach (GameObject objToMove in selectedObjects)
                    {
                        MoveObjectToNormalDistance1(objToMove, tangentEnd);
                      //  MoveToClosestSurface(hit, objToMove);
                    }
                }
            }
        }

    }



    public void MoveObjectToNormalDistance(GameObject objToMove, RaycastHit hit)
    {

        NetworkObject networkObj = objToMove.GetComponent<NetworkObject>();
        if (networkObj != null && networkObj.HasStateAuthority)
        {
            Vector3 newPosition = tangentEnd;
            // newPosition = newPosition;
            newPosition.y = 0;


            // Get the collider of the hit object
            Collider hitCollider = hit.collider;

            // Get the collider of the target object
            Collider targetCollider = objToMove.transform.GetComponent<Collider>();

                spawnObj = Instantiate(spawnObjRef);
                spawnObj.transform.position = hit.point;
                Vector3 surfaceNormal = hit.normal;

            // Calculate the endpoint of the tangent line
            tangentEnd = hit.point + normal * targetClosestDistance;

            Debug.LogError("step 2  spawn marquer1");
                // Find the closest point on the target object's surface to the hit surface
                Vector3 closestPointOnTargetSurface = targetCollider.ClosestPoint(hit.point);
                spawnObj = Instantiate(spawnObjTarget);
                spawnObj.transform.position = closestPointOnTargetSurface;


                //Vector3 newPosition = objToMove.transform.position;
                Collider collider = objToMove.transform.GetComponent<Collider>();
            Debug.Log("normal" + normal.ToString());
            if (normal.x < 0)
            {
                newPosition.x = newPosition.x + positionOffset.x + (collider.bounds.size.x / 2);
            }
            else if (normal.x > 0)
            {
                newPosition.x = newPosition.x + positionOffset.x + (collider.bounds.size.x / 2);
            }
            if (normal.z > 0)
            {
                newPosition.z = newPosition.z + positionOffset.z + (collider.bounds.size.z / 2);
            }
            else if (normal.z < 0)
            {
                newPosition.z = newPosition.z + positionOffset.z + (collider.bounds.size.z / 2);
            }
            // Use NetworkTransform if present
            NetworkTransform networkTransform = objToMove.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                networkTransform.Teleport(newPosition);
            }
            else
            {
                objToMove.transform.position = newPosition; // Fallback if no NetworkTransform
            }

            //Debug.Log($"Moved {obj.name} to {newPosition}");
        }
        else
        {
            Debug.LogWarning($"Cannot move {objToMove.name}. No State Authority!");
        }


    }

    public void MoveObjectToNormalDistance1(GameObject objToMove, Vector3 tangentEnd)
    {

        NetworkObject networkObj = objToMove.GetComponent<NetworkObject>();
        if (networkObj != null && networkObj.HasStateAuthority)
        {

            Vector3 newPosition = tangentEnd;
            // newPosition = newPosition;
            newPosition.y = 0;

            //Vector3 newPosition = objToMove.transform.position;
            Collider collider = objToMove.transform.GetComponent<Collider>();
            if (normal.x < 0)
            {
                newPosition.x = newPosition.x + positionOffsetXn + (collider.bounds.size.x / 2);
                newPosition.z = newPosition.z + xZOffset;
            }
            else if (normal.x > 0)
            {
                newPosition.x = newPosition.x + positionOffsetXp  + (collider.bounds.size.x / 2);
                newPosition.z = newPosition.z + xZOffset;
            }
            if (normal.z > 0)
            {
                newPosition.z = newPosition.z + positionOffsetZp + (collider.bounds.size.z / 2);
                newPosition.x = newPosition.x + zXOffset;
            }
            else if (normal.z < 0)
            {
                newPosition.z = newPosition.z + positionOffsetZn + (collider.bounds.size.z / 2);
                newPosition.x = newPosition.x + zXOffset;

            }
            // Use NetworkTransform if present
            NetworkTransform networkTransform = objToMove.GetComponent<NetworkTransform>();
                if (networkTransform != null)
                {
                    networkTransform.Teleport(newPosition);
                }
                else
                {
                    objToMove.transform.position = newPosition; // Fallback if no NetworkTransform
                }

                //Debug.Log($"Moved {obj.name} to {newPosition}");
            }
            else
            {
                Debug.LogWarning($"Cannot move {objToMove.name}. No State Authority!");
            }

        
    }
    void MoveToClosestSurface(RaycastHit hit, GameObject objToMove)
    {
        NetworkObject networkObj = objToMove.GetComponent<NetworkObject>();
        if (networkObj != null && networkObj.HasStateAuthority)
        {

            // Get the collider of the hit object
            Collider hitCollider = hit.collider;

            // Get the collider of the target object
            Collider targetCollider = objToMove.transform.GetComponent<Collider>();

            if (hitCollider != null && targetCollider != null)
            {
                spawnObj = Instantiate(spawnObjRef);
                spawnObj.transform.position = hit.point;
                Vector3 surfaceNormal = hit.normal;

                Debug.LogError("step 2  spawn marquer1");
                // Find the closest point on the target object's surface to the hit surface
                Vector3 closestPointOnTargetSurface = targetCollider.ClosestPoint(hit.point);
                spawnObj = Instantiate(spawnObjTarget);
                spawnObj.transform.position = closestPointOnTargetSurface;

                Debug.LogError("step 3  spawn marquer2");
                // Calculate the direction vector from the hit surface to the target object's surface
                Vector3 direction = closestPointOnTargetSurface - hit.point;

                // Calculate the current closest distance between the two surfaces
                float currentClosestDistance = direction.magnitude;

                // Calculate the adjustment needed to achieve the target distance
                float adjustmentDistance = targetClosestDistance - currentClosestDistance;

                // Move the target object along the direction vector to achieve the target distance
                Vector3 targetPosition = objToMove.transform.position + direction.normalized * adjustmentDistance;
                targetPosition.y = 0;


                
                // Use NetworkTransform if present
                NetworkTransform networkTransform = objToMove.GetComponent<NetworkTransform>();
                if (networkTransform != null)
                {
                    networkTransform.Teleport(targetPosition);
                }
                else
                {
                    objToMove.transform.position = targetPosition; // Fallback if no NetworkTransform
                }
            }
        }
    }
    public void MoveToSurfaceDistance()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Perform the raycast
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, raycastMaxDistance, raycastLayerMask))
            {
                foreach(GameObject obj in selectedObjects)
                {
                    MoveToClosestSurface(hit, obj);
                }               
            }
        }

    }
   


    void MoveToClosestSurface(RaycastHit hit)
    {
        // Get the collider of the hit object
        Collider hitCollider = hit.collider;

        // Get the collider of the target object
        Collider targetCollider = targetObject.GetComponent<Collider>();

        if (hitCollider != null && targetCollider != null)
        {
            // Find the closest point on the hit surface to the target object
            Vector3 closestPointOnHitSurface = hitCollider.ClosestPoint(targetObject.transform.position);
            spawnObj = Instantiate(spawnObjRef, closestPointOnHitSurface, Quaternion.identity);
            Vector3 surfaceNormal = hit.normal;

            // Find the closest point on the target object's surface to the hit surface
            Vector3 closestPointOnTargetSurface = targetCollider.ClosestPoint(closestPointOnHitSurface);
            spawnObj = Instantiate(spawnObjTarget, closestPointOnTargetSurface, Quaternion.identity);
            // Calculate the direction vector from the hit surface to the target object's surface
            Vector3 direction = closestPointOnTargetSurface - closestPointOnHitSurface;

            // Calculate the current closest distance between the two surfaces
            float currentClosestDistance = direction.magnitude;

            // Calculate the adjustment needed to achieve the target distance
            float adjustmentDistance = targetClosestDistance - currentClosestDistance;

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



    void MoveToShortestDistanceFromPlane(RaycastHit hit, GameObject objectToMove)
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
        Vector3 targetPosition = nearestPointOnPlane + planeNormal * targetClosestDistance;// + new Vector3(planeNormal.x * collider.bounds.size.x / 2, 0, planeNormal.z * collider.bounds.size.z / 2);
           

        targetPosition.y = 0;
        // Move the object to the target position
        objectToMove.transform.position = targetPosition;
    }

    public void SetDistance(TMP_InputField inputField)
    {
        // Check if the input field is not empty
        if (!string.IsNullOrEmpty(inputField.text))
        {
            // Try to parse the input text to a number
            if (float.TryParse(inputField.text, out float number))
            {
                // Calculate the double of the number
                targetClosestDistance = number;

                // Print the doubled value to the console
                Debug.Log("Doubled Value: " + targetClosestDistance);
            }
            else
            {
                // If parsing fails, print an error message
                Debug.LogError("Invalid input. Please enter a valid number.");
            }
        }
        else
        {
            // Handle empty input
            Debug.LogError("Input field is empty. Please enter a value.");
        }

    }
}