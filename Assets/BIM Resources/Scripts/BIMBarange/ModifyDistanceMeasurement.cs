using Meta.WitAi;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Fusion;

public class ModifyDistanceMeasurement : NetworkBehaviour
{
    public Transform controller;
    public Transform cameraRef;
    public Material lineMaterial;
    public GameObject endPointPref;
    public GameObject measurePref;
    public bool isMeasurementActive = true; // Tracks if the measurement tool is active

    private XRIBIMInputActions playerInputActions;
    private MeasurementHandler measurementHandler;


    private LineRenderer currentLine;
    private TMP_Text measureText;

    private Vector3 startPoint;
    private bool isDrawing = false;

    private Renderer objectRenderer; // Renderer of the GameObject
                                     // Start is called before the first frame update

    private bool isModifyingEndPoint;
    private bool isModifyingStartPoint;

    //line count
    private int lineCount = 0;
    private List<Transform> lines;
    private int count = 0;

    private GameObject lineObj;
    private NetworkedLine spawnedLine;


    private GameObject spawnRefStart;
    private GameObject spawnRefEnd;

    private NetworkedMUI spawnedUI;

    private void Awake()
    {
        measurementHandler = MeasurementHandler.Instance;
        playerInputActions = measurementHandler.playerInputActions;
        playerInputActions.XRIRightInteraction.Enable();
        playerInputActions.XRILeftInteraction.Enable(); 

        //adding actionListeners
        playerInputActions.XRIRightInteraction.Select.performed += DrawLine_performed;
        playerInputActions.XRIRightInteraction.Select.canceled += DrawLine_endDrawing;

        playerInputActions.XRILeftInteraction.Delete.performed += DeleteLastLine_performed;

    }
    private void OnEnable()
    {
        measurementHandler = MeasurementHandler.Instance;
        playerInputActions = measurementHandler.playerInputActions;
        playerInputActions.XRIRightInteraction.Enable();
        playerInputActions.XRILeftInteraction.Enable();

        //adding actionListeners
        playerInputActions.XRIRightInteraction.Select.performed += DrawLine_performed;
        playerInputActions.XRIRightInteraction.Select.canceled += DrawLine_endDrawing;

        playerInputActions.XRILeftInteraction.Delete.performed += DeleteLastLine_performed;

    }
    private void OnDisable()
    {
        playerInputActions.XRIRightInteraction.Select.performed -= DrawLine_performed;
        playerInputActions.XRIRightInteraction.Select.canceled -= DrawLine_endDrawing;

        playerInputActions.XRILeftInteraction.Delete.performed -= DeleteLastLine_performed;

    }


    private void DrawLine_performed(InputAction.CallbackContext context)
    {
        Vector3 controllerPosition = controller.position;
        Quaternion controllerRotation = controller.rotation;
        Vector3 rayDirection = controllerRotation * Vector3.forward;

        // Raycast logic
        Ray ray = new Ray(controllerPosition, rayDirection);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
             if (hit.transform.tag == "EndPoint" || hit.transform.tag == "StartPoint")
            {
                currentLine = hit.collider.transform.parent.GetComponent<LineRenderer>();
                lineObj = hit.collider.transform.parent.gameObject;
                Transform spawnedUITransform = FindChildWithTagRecursive(hit.collider.transform.parent, "MeasureUI");
                spawnedUI = spawnedUITransform.gameObject.GetComponent<NetworkedMUI>();


                if (hit.transform.tag == "EndPoint")
                {
                    if (currentLine != null)
                    {
                        isModifyingEndPoint = true;
                        Destroy(hit.transform.gameObject);
                    }
                }
                else if (hit.transform.tag == "StartPoint")
                {
                    if (currentLine != null)
                    {
                        isModifyingStartPoint = true;
                        Destroy(hit.transform.gameObject);
                    }
                }
            }
        }
    }
    private void DrawLine_endDrawing(InputAction.CallbackContext context)
    {
        if (currentLine != null)
            EndDrawing();
    }


    private void DeleteLastLine_performed(InputAction.CallbackContext context)
    {
        measurementHandler.DeleteLastLine();
    }


    private Transform lastLine;
    void Update()
    {
        if (!isMeasurementActive) return; // Exit if the measurement tool is inactive

        // Get the controller's position and orientation
        Vector3 controllerPosition = controller.position;
        Quaternion controllerRotation = controller.rotation;
        Vector3 rayDirection = controllerRotation * Vector3.forward;

        // Raycast logic
        Ray ray = new Ray(controllerPosition, rayDirection);
        RaycastHit hit;

        // Update the line's endpoint while holding the trigger
        if (playerInputActions.XRIRightInteraction.Select.ReadValue<float>() > 0.3)
        {
            if (Physics.Raycast(ray, out hit))
            {    
                if(isDrawing || isModifyingEndPoint)
                    UpdateLine(hit.point);

                else if (isModifyingStartPoint)
                    UpdateLineStart(hit.point);
            }
        }
   
    }

    private void StartDrawing(Vector3 point)
    {
        isDrawing = true;
        startPoint = point;

        // Create a new line object
        GameObject lineObj = new GameObject("Line"+count++);
        currentLine = lineObj.AddComponent<LineRenderer>();
        currentLine.material = lineMaterial;
        currentLine.startWidth = 0.01f;
        currentLine.endWidth = 0.01f;
        currentLine.positionCount = 2;
        currentLine.SetPosition(0, startPoint);
        currentLine.SetPosition(1, startPoint); // Temporarily set the second point to the start
        Quaternion rotation = Quaternion.identity; // No rotation
        GameObject instance = Instantiate(endPointPref); 
        instance.transform.position = startPoint;

        instance.transform.parent = currentLine.transform;
        instance.transform.tag = "StartPoint";
        

        Debug.LogError(" Line created by S1");
    }

    private void UpdateLineStart(Vector3 currentPoint)
    {
        if (currentLine != null)
        {

            if (currentLine != null)
            {
                NetworkObject networklineObj = lineObj.GetComponent<NetworkObject>();
                if (networklineObj != null && networklineObj.HasStateAuthority)
                {
                    // Use NetworkTransform if present
                    NetworkTransform networkTransform = lineObj.GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {

                        spawnedLine = networklineObj.gameObject.GetComponent<NetworkedLine>();
                        spawnedLine.SetLinePositions( currentPoint, currentLine.GetPosition(1), true);

                        string data = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString("F2") + " mètre";
                        spawnedUI.SetUIPositions((currentPoint + currentLine.GetPosition(1)) / 2, data, true);
                    

                    }


                    //Debug.Log($"Moved {obj.name} to {newPosition}");
                }

            }









            currentLine.SetPosition(0, currentPoint); // Update the line's Start to the current hit point

        }
    }
    private void UpdateLine(Vector3 currentPoint)
    {
        if (currentLine != null)
        {
            NetworkObject networklineObj = lineObj.GetComponent<NetworkObject>();
            if (networklineObj != null && networklineObj.HasStateAuthority)
            {
                // Use NetworkTransform if present
                NetworkTransform networkTransform = lineObj.GetComponent<NetworkTransform>();
                if (networkTransform != null)
                {

                    spawnedLine = networklineObj.gameObject.GetComponent<NetworkedLine>();
                    spawnedLine.SetLinePositions(currentLine.GetPosition(0), currentPoint, true);


                    string data = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString("F2") + " mètre";
                    spawnedUI.SetUIPositions((currentPoint + currentLine.GetPosition(0)) / 2, data, true);

                }


                //Debug.Log($"Moved {obj.name} to {newPosition}");
            }

        }
    }
    private void UpdateLine(Vector3 currentPoint, Transform endPointTransform )
    {
     if (currentLine != null)
        {
            Debug.LogError("Modifying current line");
            currentLine.SetPosition(1, currentPoint); // Update the line's endpoint to the current hit point
            endPointTransform.position = currentLine.GetPosition(1);
        }
    }

    private void EndDrawing()
    {
        
        if (isModifyingEndPoint|| isModifyingStartPoint)
        {
            if(measurementHandler.LineCount() > 0)
                measurementHandler.RemoveLine(currentLine.transform);            
        }

        //increment line count
        measurementHandler.AddLine(currentLine.transform);

        string data = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString("F2") + " mètre";
        spawnedUI.SetUIPositions((currentLine.GetPosition(0) + currentLine.GetPosition(1)) / 2, data, true);

        if (isDrawing || isModifyingEndPoint)
        {

            Fusion.NetworkObject spawnNWObjEnd = null;
            NetworkManager.Instance.Runner.Spawn(endPointPref, Vector3.zero, endPointPref.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
            {
                spawnNWObjEnd = obj;
            });


            spawnRefEnd = spawnNWObjEnd.gameObject;
            spawnRefEnd.transform.parent = lineObj.transform;
            spawnRefEnd.transform.tag = "EndPoint";


            NetworkObject networkObjStart = spawnRefEnd.GetComponent<NetworkObject>();
                if (networkObjStart != null && networkObjStart.HasStateAuthority)
                {
                    // Use NetworkTransform if present
                    NetworkTransform networkTransform = spawnRefEnd.GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {
                        networkTransform.Teleport(currentLine.GetPosition(1));
                    }
                    else
                    {
                    spawnRefEnd.transform.position = currentLine.GetPosition(1); // Fallback if no NetworkTransform
                    }

                    //Debug.Log($"Moved {obj.name} to {newPosition}");
                }
            

            Quaternion rotation = Quaternion.LookRotation((currentLine.GetPosition(1) - currentLine.GetPosition(0)).normalized);
            // Quaternion rotation = Quaternion.identity; // No rotation
                      
        }
        else if (isModifyingStartPoint)
        {



            Fusion.NetworkObject spawnNWObjStart = null;
            NetworkManager.Instance.Runner.Spawn(endPointPref, Vector3.zero, endPointPref.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
            {
                spawnNWObjStart = obj;
            });


            spawnRefStart = spawnNWObjStart.gameObject;
            spawnRefStart.transform.parent = lineObj.transform;
            spawnRefStart.transform.tag = "StartPoint";


            NetworkObject networkObjStart = spawnRefStart.GetComponent<NetworkObject>();
            if (networkObjStart != null && networkObjStart.HasStateAuthority)
            {
                // Use NetworkTransform if present
                NetworkTransform networkTransform = spawnRefStart.GetComponent<NetworkTransform>();
                if (networkTransform != null)
                {
                    networkTransform.Teleport(currentLine.GetPosition(0));
                }
                else
                {
                    spawnRefStart.transform.position = currentLine.GetPosition(0); // Fallback if no NetworkTransform
                }

                //Debug.Log($"Moved {obj.name} to {newPosition}");
            }

         }

        lastLine = currentLine.transform;
        isDrawing = false;
        isModifyingEndPoint = false;
        isModifyingStartPoint = false;
        
        spawnedUI = null;
        currentLine = null; // Reset currentLine to allow drawing a new one
        
        measureText = null;


    }

    private void DeleteCurrentLine()
    {
        if (currentLine != null)
        {
            Destroy(currentLine.transform.gameObject);
            currentLine = null;
        }

    }


    public void ActivateMeasurement()
    {
        isMeasurementActive = true;
    }

    public void DeactivateMeasurement()
    {
        isMeasurementActive = false;
    }

    void InstantiatePrefab(string tag)
    {
        // Instantiate at a specific position and rotation
        Vector3 position = new Vector3(0, 1, 0); // Example position
        Quaternion rotation = Quaternion.identity; // No rotation
        GameObject instance = Instantiate(endPointPref, position, rotation);

        // Optional: Customize the instantiated object
        instance.name = "New Instance";
    }
    private Transform FindChildWithTagRecursive(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
            {
                return child;
            }
            Transform found = FindChildWithTagRecursive(child, tag);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
}
