using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using Meta.XR.MRUtilityKit;

public class DrawLineBetweenTwoHitsNW : Fusion.NetworkBehaviour
{
   // private XRIBIMInputActions playerInputActions;
    private PlayerInput playerInput;
    public Transform cameraRef;

    public Transform controller;
    public Camera mainCamera; // Assign the main camera or the camera used for raycasting
    public float rayLength = 30f; // Length of the raycast
    public Material lineMaterial; // Material for the LineRenderer
    private GameObject lineObj;

    public GameObject  lineObjNW;

    public GameObject spawnObj;
    private GameObject spawnRefStart;
    private GameObject spawnRefEnd;


    private TMP_Text measureText;

    public NetworkedLine linePrefab; // Assign this in the Inspector
    private NetworkedLine spawnedLine;
    private Vector3 pointA = new Vector3(0, 0, 2.36f); // Example position
    private Vector3 pointB = new Vector3(0, 0, -1.4f); // Example position


    // Layer mask for raycast targets (optional)
    public LayerMask raycastLayerMask;

    private LineRenderer currentLine;
    private bool isDrawing = false;

    public NetworkedMUI measureUI;
    private NetworkedMUI spawnedUI;

    private int count = 0;

    private void DrawLine_performed()
    {
        if (!isDrawing) {
            DrawLine();
            isDrawing = true;
        }

    }

    private void DrawLine_endDrawing()
    {
        if (isDrawing)
        {
            isDrawing = false;
            currentLine = null;
            spawnedLine = null;
            spawnRefStart = null;
            spawnRefEnd = null;
            if (lineObj.activeSelf)
            {
                MeasurementHandler.Instance.AddLine(lineObj);
                lineObj = null;
                MeasurementHandler.Instance.lineCount++;
               // Destroy(lineObj);
            }
            spawnedUI = null;
            lineObj = null;
            
        }
    }
    private void DeleteLastLine_performed()
    {
        MeasurementHandler.Instance.DeleteLastLine();
    }

    void Update()
    {
     if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
        {
            Debug.Log("intex key pressed");
            DrawLine_performed();
        }
        // Deselect
        else if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger))
        {
            Debug.Log("intex key removed");
            DrawLine_endDrawing();
        }
    
        // Get right-hand trigger value (0.0f to 1.0f)
        // Read right index trigger (mapped)
       //  float triggerValue = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);


        if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger)) // Small threshold to avoid accidental movement
        {
            if (isDrawing)
            {
                UpdateLine();   
            }

        }
 /*       if (Input.GetMouseButtonUp(1))
        {
            DrawPositionIndicator();
        }
 */
    }
    private void DrawUI()
    {
        Debug.LogError("Hurry it workds*************");
        if (spawnedUI == null)
        {

            spawnedUI = NetworkManager.Instance.Runner.Spawn(measureUI, Vector3.zero, Quaternion.identity);
        }
        // Calculate midpoint between pointA and pointB
        Vector3 midpoint = (currentLine.GetPosition(0) + currentLine.GetPosition(1)) / 2f;

        spawnedUI.transform.parent = lineObj.transform;
        spawnedUI.SetCameraRef(cameraRef);
        // Set UI position at the midpoint
        spawnedUI.SetUIPositions(midpoint, "", true);

        Debug.Log(" UI ");
    }

    private void DrawLine()
    {
          {
            if (currentLine == null)
            {
                Fusion.NetworkObject spawnNWLine = null;
                NetworkManager.Instance.Runner.Spawn(linePrefab, Vector3.zero, linePrefab.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
                {
                    spawnNWLine = obj;
                    spawnedLine = spawnNWLine.gameObject.GetComponent<NetworkedLine>();
                    lineObj = spawnNWLine.gameObject;
                    currentLine = lineObj.GetComponent<LineRenderer>();

					MeasurementHandlerV2.Instance.AddLine(spawnNWLine.gameObject);
					MeasurementHandlerV2.Instance.lineCount++;
					updateLinePosition(pointA, pointB);
                    DrawUI();

                });




                // spawnedLine = NetworkManager.Instance.Runner.Spawn(linePrefab, Vector3.zero, Quaternion.identity);
            }


            /*   currentLine.material = lineMaterial;
               currentLine.startWidth = 0.01f;
               currentLine.endWidth = 0.01f;
               currentLine.positionCount = 2;
            */

            currentLine.enabled = false; // Initially, the line is hidden
                                         //add endPointMarquers

            Fusion.NetworkObject spawnNWObjStart = null;
            NetworkManager.Instance.Runner.Spawn(spawnObj, Vector3.zero, spawnObj.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
            {
                spawnNWObjStart = obj;
            });


            spawnRefStart = spawnNWObjStart.gameObject;
            spawnRefStart.transform.parent = lineObj.transform;
            spawnRefStart.transform.tag = "StartPoint";

            /*
            spawnRefStart = Instantiate(spawnObj);
            spawnRefStart.transform.parent = lineObj.transform;
            spawnRefStart.transform.tag = "StartPoint";
            */


            Fusion.NetworkObject spawnNWObjEnd = null;
            NetworkManager.Instance.Runner.Spawn(spawnObj, Vector3.zero, spawnObj.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
            {
                spawnNWObjEnd = obj;
            });

            spawnRefEnd = spawnNWObjEnd.gameObject;
            spawnRefEnd.transform.parent = lineObj.transform;
            spawnRefEnd.transform.tag = "EndPoint";
        }

    }


    private void updateLinePosition(Vector3 pointA, Vector3 pointB)
    {
        spawnedLine.SetLinePositions(pointA, pointB, true);
    }


    private void UpdateLine()
    {
        // Get the controller's position and orientation
        Vector3 controllerPosition = controller.position;
        Quaternion controllerRotation = controller.rotation;
        Vector3 rayDirection = controllerRotation * Vector3.forward;

        // Raycast logic
        Ray ray = new Ray(controllerPosition, rayDirection);
 
        lineObj.SetActive(false);
        // Perform the first raycast
        if (Physics.Raycast(ray, out RaycastHit firstHit, rayLength))
        {
            Vector3 firstPoint = firstHit.point;
            Vector3 normal = firstHit.normal;
            //Spawn marquer
            spawnRefStart.transform.position = firstPoint;

            NetworkObject networkObjStart = spawnRefStart.GetComponent<NetworkObject>();
            if (networkObjStart != null && networkObjStart.HasStateAuthority)
            {
                // Use NetworkTransform if present
                NetworkTransform networkTransform = spawnRefStart.GetComponent<NetworkTransform>();
                if (networkTransform != null)
                {
                    networkTransform.Teleport(firstPoint);
                }
                else
                {
                    spawnRefStart.transform.position = firstPoint; // Fallback if no NetworkTransform
                }

                //Debug.Log($"Moved {obj.name} to {newPosition}");
            }


            // Calculate the tangent direction
            Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;

            // If the tangent is zero, use a different reference vector
            if (tangent == Vector3.zero)
            {
                tangent = Vector3.Cross(normal, Vector3.right).normalized;
            }

            // Perform the second raycast from the first hit point in the tangent direction
            Ray secondRay = new Ray(firstPoint, normal);
            if (Physics.Raycast(secondRay, out RaycastHit secondHit, rayLength))
            {
                lineObj.SetActive(true);
                Vector3 secondPoint = secondHit.point;

                NetworkObject networkObjEnd = spawnRefEnd.GetComponent<NetworkObject>();
                if (networkObjEnd != null && networkObjEnd.HasStateAuthority)
                {
                    // Use NetworkTransform if present
                    NetworkTransform networkTransform = spawnRefEnd.GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {
                        networkTransform.Teleport(secondPoint);
                    }
                    else
                    {
                        spawnRefEnd.transform.position = secondPoint; // Fallback if no NetworkTransform
                    }

                    //Debug.Log($"Moved {obj.name} to {newPosition}");
                }



                NetworkObject networklineObj = lineObj.GetComponent<NetworkObject>();
                if (networklineObj != null && networklineObj.HasStateAuthority)
                {
                    // Use NetworkTransform if present
                    NetworkTransform networkTransform = lineObj.GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {
                        updateLinePosition(firstPoint, secondPoint);
                        string data = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString("F2") + " m�tre";

                        spawnedUI.SetUIPositions((firstPoint + secondPoint) / 2, data, true);

                    }


                    //Debug.Log($"Moved {obj.name} to {newPosition}");
                }



                
            }
        }

    }



    public void DrawPositionIndicator()
    {
        // Get the controller's position and orientation
        Vector3 controllerPosition = controller.position;
        Quaternion controllerRotation = controller.rotation;
        Vector3 rayDirection = controllerRotation * Vector3.forward;
        RaycastHit hit;
        // Raycast logic
        Ray ray = new Ray(controllerPosition, rayDirection);

        if (Physics.Raycast(ray, out hit, rayLength, raycastLayerMask))
        {  
            //TODO Check if the hit Object has specific tag

            Vector3 transformPos = hit.transform.position;
            Collider collider = hit.collider;
            Vector3 position = new Vector3(transformPos.x - hit.collider.bounds.size.x / 2, transformPos.y + hit.collider.bounds.size.y / 2, transformPos.z - hit.collider.bounds.size.z / 2);
            GameObject marker = Instantiate(spawnObj);
            marker.transform.position = position;

            //draw a line towards the -Z axis from the corner point
            ray = new Ray(position, new Vector3(0, 0, -1));
            // Perform a raycast
            if (Physics.Raycast(ray, out hit, rayLength, raycastLayerMask))
            {
                DrawLine(position, hit.point);
            }
            //draw a line towards the -X axis from the corner point
            ray = new Ray(position, new Vector3(-1, 0, 0));
            // Perform a raycast
            if (Physics.Raycast(ray, out hit))
            {
                DrawLine(position, hit.point);
            }
        }
    }
    private GameObject DrawLine(Vector3 p1, Vector3 p2)
    {

        GameObject directionLine = new GameObject("Line" + count++);
        LineRenderer dLine = directionLine.AddComponent<LineRenderer>();
        dLine.material = lineMaterial;
        dLine.startWidth = 0.01f;
        dLine.endWidth = 0.01f;
        dLine.positionCount = 2;
        dLine.SetPosition(0, p1);
        dLine.SetPosition(1, p2);

        


        return directionLine;
    }



    public void DrawPositionLine(InputAction.CallbackContext context)
    {
        Debug.Log("Fire!");


    }


}
