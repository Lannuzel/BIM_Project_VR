using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using System.Net;

public class DistanceMeasurementTool : NetworkBehaviour
{
    private XRIBIMInputActions playerInputActions;
    private PlayerInput playerInput;
    public Transform cameraRef;

    public Transform controller;
    public Camera mainCamera; // Assign the main camera or the camera used for raycasting
    public float rayLength = 30f; // Length of the raycast
    public Material lineMaterial; // Material for the LineRenderer
    private GameObject lineObj;

    public GameObject lineObjNW;

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

    private int count = 0;

    private Vector3 firstPoint;
    private Vector3 secondPoint;


    public NetworkedMUI measureUI;
    private NetworkedMUI spawnedUI;




    private void Awake()
    {
        playerInputActions = MeasurementHandler.Instance.playerInputActions;
        playerInputActions.XRIRightInteraction.Enable();

        //adding actionListeners
        playerInputActions.XRIRightInteraction.Select.performed += DrawLine_performed;
        playerInputActions.XRIRightInteraction.Select.canceled += DrawLine_endDrawing;

        playerInputActions.XRILeftInteraction.Delete.performed += DeleteLastLine_performed;
    }
    private void OnEnable()
    {
        playerInputActions = MeasurementHandler.Instance.playerInputActions;
        playerInputActions.XRIRightInteraction.Enable();

        //adding actionListeners
        playerInputActions.XRIRightInteraction.Select.performed += DrawLine_performed;
        playerInputActions.XRIRightInteraction.Select.canceled += DrawLine_endDrawing;
        playerInputActions.XRILeftInteraction.Delete.performed += DeleteLastLine_performed;
    }
    private void OnDisable()
    {
        playerInputActions.XRIRightInteraction.Select.performed -= DrawLine_performed;
        playerInputActions.XRIRightInteraction.Select.canceled -= DrawLine_endDrawing;
        playerInputActions.XRILeftInteraction.Delete.performed += DeleteLastLine_performed;

    }

    private void DrawLine_performed(InputAction.CallbackContext context)
    {
        if (!isDrawing)
        {
            DrawLine();
            isDrawing = true;
        }

    }
    private void DrawLine_performed()
    {
        if (!isDrawing)
        {
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

    private void DrawLine_endDrawing(InputAction.CallbackContext context)
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
    private void DeleteLastLine_performed(InputAction.CallbackContext context)
    {
        MeasurementHandler.Instance.DeleteLastLine();
    }

    void Update()
    {

        // ********************************
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            Debug.Log("intex key pressed");
            DrawLine_performed();
        }
        // Deselect
        else if (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger))
        {
            Debug.Log("intex key removed");
            DrawLine_endDrawing();
        }

        // Get right-hand trigger value (0.0f to 1.0f)
        // Read right index trigger (mapped)
        float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);


        if (triggerValue > 0.3f) // Small threshold to avoid accidental movement
        {
            if (isDrawing)
            {
                UpdateLine();
            }
        }

        if (playerInputActions.XRIRightInteraction.Select.ReadValue<float>() > 0.3)
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

    private void DrawLine()
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
             firstPoint = firstHit.point;
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
        }

         Fusion.NetworkObject spawnNWObjEnd = null;
        NetworkManager.Instance.Runner.Spawn(spawnObj, Vector3.zero, spawnObj.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
        {
            spawnNWObjEnd = obj;
        });

        spawnRefEnd = spawnNWObjEnd.gameObject;
        spawnRefEnd.transform.parent = lineObj.transform;
        spawnRefEnd.transform.tag = "EndPoint";


    }
    private void DrawUI()
    {
        Debug.LogError("Hurry it workds*************");
        if (spawnedUI == null)
        {

            spawnedUI = NetworkManager.Instance.Runner.Spawn(measureUI, Vector3.zero, Quaternion.identity);
        }
        // Calculate midpoint between pointA and pointB
        Vector3 midpoint = (currentLine.GetPosition(0)+ currentLine.GetPosition(1)) / 2f;

        spawnedUI.transform.parent = lineObj.transform;
        spawnedUI.SetCameraRef(cameraRef);
        // Set UI position at the midpoint
        spawnedUI.SetUIPositions(midpoint, "", true);

        Debug.Log(" UI ");
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
        if (Physics.Raycast(ray, out RaycastHit secondHit, rayLength, raycastLayerMask))
        {
            lineObj.SetActive(true);
            secondPoint = secondHit.point;

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
                     string data = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString("F2") + " mètre";
                    spawnedUI.SetUIPositions((firstPoint + secondPoint) / 2, data, true);
                    
                }


                //Debug.Log($"Moved {obj.name} to {newPosition}");
            }







        }

    }




}







/*
 * using Meta.WitAi;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class DistanceMeasurementTool : MonoBehaviour
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
    private GameObject measurementUI;
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
            StartDrawing(hit.point);

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
                if (isDrawing || isModifyingEndPoint)
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
        GameObject lineObj = new GameObject("Line" + count++);
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
        measurementUI = Instantiate(measurePref);
        measurementUI.transform.parent = currentLine.transform;
        measurementUI.transform.position = startPoint;
        measureText = measurementUI.GetComponentInChildren<TMP_Text>();

    }

    private void UpdateLineStart(Vector3 currentPoint)
    {
        if (currentLine != null)
        {
            currentLine.SetPosition(0, currentPoint); // Update the line's Start to the current hit point
            measurementUI.transform.position = (currentPoint + currentLine.GetPosition(1)) / 2;
            measurementUI.transform.LookAt(-(measurementUI.transform.position + cameraRef.forward));
            //measurementUI.transform.LookAt(-(transform.position + cameraRef.forward));
            measureText.text = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString("F2") + " mètre";
        }
    }
    private void UpdateLine(Vector3 currentPoint)
    {
        if (currentLine != null)
        {
            currentLine.SetPosition(1, currentPoint); // Update the line's endpoint to the current hit point
            measurementUI.transform.position = (currentPoint + currentLine.GetPosition(0)) / 2;
            measurementUI.transform.LookAt(-(measurementUI.transform.position + cameraRef.forward));
            // measurementUI.transform.LookAt(-(transform.position + cameraRef.forward));
            measureText.text = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString("F2") + " mètre";
        }
    }
    private void UpdateLine(Vector3 currentPoint, Transform endPointTransform)
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

        if (isModifyingEndPoint || isModifyingStartPoint)
        {
            if (measurementHandler.LineCount() > 0)
                measurementHandler.RemoveLine(currentLine.transform);
        }

        //increment line count
        measurementHandler.AddLine(currentLine.transform);

        measurementUI.transform.position = (currentLine.GetPosition(0) + currentLine.GetPosition(1)) / 2;
        measurementUI.transform.LookAt(-(measurementUI.transform.position + cameraRef.forward));
        // measurementUI.transform.LookAt(-(transform.position + cameraRef.forward));
        measureText.text = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString("F2") + " mètre";

        if (isDrawing || isModifyingEndPoint)
        {
            Quaternion rotation = Quaternion.LookRotation((currentLine.GetPosition(1) - currentLine.GetPosition(0)).normalized);
            // Quaternion rotation = Quaternion.identity; // No rotation
            GameObject instance = Instantiate(endPointPref);
            instance.transform.position = currentLine.GetPosition(1);

            instance.transform.parent = currentLine.transform;
            instance.transform.tag = "EndPoint";

        }
        else if (isModifyingStartPoint)
        {
            Quaternion rotation = Quaternion.LookRotation((currentLine.GetPosition(1) - currentLine.GetPosition(0)).normalized);
            //Quaternion rotation = Quaternion.identity; // No rotation
            GameObject instance = Instantiate(endPointPref);
            instance.transform.position = currentLine.GetPosition(0);

            instance.transform.parent = currentLine.transform;
            instance.transform.tag = "StartPoint";
        }

        lastLine = currentLine.transform;
        isDrawing = false;
        isModifyingEndPoint = false;
        isModifyingStartPoint = false;


        currentLine = null; // Reset currentLine to allow drawing a new one
        measurementUI = null;
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

*/






