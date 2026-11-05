using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class DrawLineBetweenTwoHits : MonoBehaviour
{
    private XRIBIMInputActions playerInputActions;
    private PlayerInput playerInput;
    public Transform cameraRef;

    public Transform controller;
    public Camera mainCamera; // Assign the main camera or the camera used for raycasting
    public float rayLength = 30f; // Length of the raycast
    public Material lineMaterial; // Material for the LineRenderer
    private GameObject lineObj;


    public GameObject spawnObj;
    private GameObject spawnRefStart;
    private GameObject spawnRefEnd;

    public GameObject measurePref;
    private GameObject measurementUI;
    private TMP_Text measureText;

    // Layer mask for raycast targets (optional)
    public LayerMask raycastLayerMask;

    private LineRenderer currentLine;
    private bool isDrawing = false;

    private int count = 0;

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
        if (!isDrawing) {
            DrawLine();
            isDrawing = true;
        }

    }

    private void DrawLine_endDrawing(InputAction.CallbackContext context)
    {
        if (isDrawing)
        {
            isDrawing = false;
            currentLine = null;
            spawnRefStart = null;
            spawnRefEnd = null;
            if (lineObj.activeSelf)
            {
                MeasurementHandler.Instance.AddLine(lineObj);
                lineObj = null;
                MeasurementHandler.Instance.lineCount++;
               // Destroy(lineObj);
            }
            lineObj = null;
            
        }
    }
    private void DeleteLastLine_performed(InputAction.CallbackContext context)
    {
        MeasurementHandler.Instance.DeleteLastLine();
    }

    void Update()
    {
        if(playerInputActions.XRIRightInteraction.Select.ReadValue<float>()>0.3)
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
        //create new line and assign start point 
        GameObject line = new GameObject("Line" + count++);

        lineObj = line;
        currentLine = lineObj.AddComponent<LineRenderer>();
        currentLine.material = lineMaterial;
        currentLine.startWidth = 0.01f;
        currentLine.endWidth = 0.01f;
        currentLine.positionCount = 2;

        currentLine.enabled = false; // Initially, the line is hidden
                                     //add endPointMarquers
        spawnRefStart = Instantiate(spawnObj);
        spawnRefStart.transform.parent = lineObj.transform;
        spawnRefStart.transform.tag = "StartPoint";

        spawnRefEnd = Instantiate(spawnObj);
        spawnRefEnd.transform.parent = lineObj.transform;
        spawnRefEnd.transform.tag = "EndPoint";

        measurementUI = Instantiate(measurePref);
        measurementUI.transform.parent = currentLine.transform;        
        measureText = measurementUI.GetComponentInChildren<TMP_Text>();
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

                spawnRefEnd.transform.position = secondPoint;
                currentLine.enabled = true; // Initially, the line is hidden

                currentLine.SetPosition(0, firstPoint);
                currentLine.SetPosition(1, secondPoint);

                measurementUI.transform.position = (firstPoint + secondPoint) / 2;
                measurementUI.transform.LookAt(-(transform.position + cameraRef.forward));
                measureText.text = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString("F2") + " mètre";
                Debug.Log("First Point: " + firstPoint + " | Second Point: " + secondPoint);
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

        measurementUI = Instantiate(measurePref);
        measurementUI.transform.parent = currentLine.transform;
        measureText = measurementUI.GetComponentInChildren<TMP_Text>();
        measurementUI.transform.position = (p1 + p2) / 2;
        measurementUI.transform.LookAt(-(transform.position + cameraRef.forward));
        measureText.text = (Vector3.Distance(dLine.GetPosition(0), dLine.GetPosition(1))).ToString("F2") + " mètre";


        return directionLine;
    }



    public void DrawPositionLine(InputAction.CallbackContext context)
    {
        Debug.Log("Fire!");


    }


}
