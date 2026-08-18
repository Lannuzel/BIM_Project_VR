using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class TangentVisualizer : MonoBehaviour
{

    public float tangentLength = 5f; // Length of the tangent line
    public Transform cameraRef;
    public Transform controller; // Assign your controller here
    public Material lineMaterial;
    public GameObject endPointPref;
    private GameObject lineObj;
    private Vector3 normal;
    private Vector3 tangentEnd;
    private Ray ray;
    private bool isTengentPointSelected = false;

    public LayerMask raycastLayerMask;

    // Maximum raycast distance
    public float raycastMaxDistance = 100f;

    private XRIBIMInputActions playerInputActions;

    public GameObject measurePref;
    private GameObject measurementUI;
    private TMP_Text measureText;
    public Transform objToMove;

    private void Awake()
    {
        playerInputActions = new XRIBIMInputActions();
        playerInputActions.XRIRightInteraction.Enable();


    }
    private void OnEnable()
    {
        playerInputActions = playerInputActions = new XRIBIMInputActions();
        playerInputActions.XRIRightInteraction.Enable();

        //adding actionListeners
        playerInputActions.XRIRightInteraction.Activate.performed += DrawTengentLine_performed;
    }

    private void DrawTengentLine_performed(InputAction.CallbackContext context)
    {
       
        DrawTengentLine();
    }

    private void OnDisable()
    {
        playerInputActions.XRIRightInteraction.Activate.performed -= DrawTengentLine_performed;


    }

    private void LateUpdate()
    {
        if (lineObj != null)
        {
            if (playerInputActions.XRIRightInteraction.ActivateValue.ReadValue<float>() > 0.3)
            {            // Get the controller's position and orientation
                Vector3 controllerPosition = controller.position;
                Quaternion controllerRotation = controller.rotation;
                Vector3 rayDirection = controllerRotation * Vector3.forward;

                // Raycast logic
                Ray ray = new Ray(controllerPosition, rayDirection);
                // Perform a raycast
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    LineRenderer currentLine = lineObj.transform.GetComponent<LineRenderer>();
                    normal = hit.normal;
                    // Calculate the endpoint of the tangent line
                    tangentEnd = hit.point + normal * tangentLength;
                    currentLine.SetPosition(0,hit.point);
                    currentLine.SetPosition(1,tangentEnd);
                    Vector3 pos = objToMove.position;
                    Collider collider = objToMove.transform.GetComponent<Collider>();

                    Debug.LogError(" line normal " + normal);
                    if (normal.x < 0)
                    {
                        pos.x = tangentEnd.x - collider.bounds.size.x / 2;
                    }
                    else if (normal.x >0)
                    {
                        pos.x = tangentEnd.x + collider.bounds.size.x / 2;
                    }
                    else if (normal.z > 0)
                    {
                        pos.z = tangentEnd.z + collider.bounds.size.z / 2;
                    }
                    else if (normal.z <0)
                    {
                        pos.z = tangentEnd.z - collider.bounds.size.z / 2;
                    }

                    objToMove.position = pos;

                }

            }
            else Destroy(lineObj);
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
                        // Get the surface normal at the hit point
                        normal = hit.normal;

                        // Calculate the endpoint of the tangent line
                        tangentEnd = hit.point + normal * tangentLength;

                        Quaternion rotation = Quaternion.identity; // No rotation
                        Vector3 tangent = Vector3.Cross(hit.normal, Vector3.up).normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(tangent, Vector3.up);
                   //     GameObject instance = Instantiate(endPointPref, hit.point, targetRotation);
                         

                        //create new line and assign start point 
                        lineObj = new GameObject("Line");
                    //    instance.transform.parent = lineObj.transform;

                      //  instance.transform.tag = "StartPoint";

                        DrawTengentLine(hit);


                    }
                }
            
        }
    

    public void DrawTengentLine(RaycastHit hit)
    {   
           
            LineRenderer currentLine = lineObj.AddComponent<LineRenderer>();
            currentLine.material = lineMaterial;
            currentLine.startWidth = 0.01f;
            currentLine.endWidth = 0.01f;
            currentLine.positionCount = 2;
            currentLine.SetPosition(0, hit.point);
            currentLine.SetPosition(1, tangentEnd); // Temporarily set the second point to the start
                                                    //assign endpoint marker
            Vector3 tangent = Vector3.Cross(hit.normal, Vector3.up).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(tangent, Vector3.up);
            //Quaternion rotation = Quaternion.identity; // No rotation
         //   GameObject instance = Instantiate(endPointPref, tangentEnd, targetRotation);
           // instance.transform.parent = lineObj.transform;
           // instance.transform.tag = "EndPoint";

            measurementUI = Instantiate(measurePref);
            measurementUI.transform.parent = currentLine.transform;
            measureText = measurementUI.GetComponentInChildren<TMP_Text>();
            measurementUI.transform.position = (hit.point + tangentEnd) / 2;
            measurementUI.transform.LookAt(-(transform.position + cameraRef.forward));
            measureText.text = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString("F2" ) + " mètre";

        Vector3 pos = objToMove.position;
        Collider collider = objToMove.transform.GetComponent<Collider>();
        if (normal.x < 0)
        {
            pos.x = tangentEnd.x - collider.bounds.size.x / 2;
        }
        else if (normal.x > 0)
        {
            pos.x = tangentEnd.x + collider.bounds.size.x / 2;
        }
        else if (normal.z > 0)
        {
            pos.z = tangentEnd.z + collider.bounds.size.z / 2;
        }
        else if (normal.z < 0)
        {
            pos.z = tangentEnd.z - collider.bounds.size.z / 2;
        }

        objToMove.position = pos;

    }
}
