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
    private RaycastHit hit;
    private bool isTengentPointSelected = false;

    public LayerMask raycastLayerMask;

    // Maximum raycast distance
    public float raycastMaxDistance = 100f;

    private XRIBIMInputActions playerInputActions;

    public GameObject measurePref;
    private GameObject measurementUI;
    private TMP_Text measureText;


    private void Awake()
    {
        playerInputActions = MeasurementHandler.Instance.playerInputActions;
        playerInputActions.XRIRightInteraction.Enable();


    }
    private void OnEnable()
    {
        playerInputActions = MeasurementHandler.Instance.playerInputActions;
        playerInputActions.XRIRightInteraction.Enable();

        //adding actionListeners
        playerInputActions.XRIRightInteraction.Select.performed += DrawTengentLine_performed;
    }

    private void DrawTengentLine_performed(InputAction.CallbackContext context)
    {
        DrawTengentLine();
    }

    private void OnDisable()
    {
        playerInputActions.XRIRightInteraction.Select.performed -= DrawTengentLine_performed;


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
            if (Physics.Raycast(ray, out RaycastHit firstHit , raycastMaxDistance))
                    {
                        // Get the surface normal at the hit point
                        normal = hit.normal;

                        // Calculate the endpoint of the tangent line
                        tangentEnd = hit.point + normal * tangentLength;

                        Quaternion rotation = Quaternion.identity; // No rotation
                        Vector3 tangent = Vector3.Cross(hit.normal, Vector3.up).normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(tangent, Vector3.up);
                        GameObject instance = Instantiate(endPointPref, hit.point, targetRotation);


                        //create new line and assign start point 
                        lineObj = new GameObject("Line");
                        instance.transform.parent = lineObj.transform;

                        instance.transform.tag = "StartPoint";

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
            GameObject instance = Instantiate(endPointPref, tangentEnd, targetRotation);
            instance.transform.parent = lineObj.transform;
            instance.transform.tag = "EndPoint";

            measurementUI = Instantiate(measurePref);
            measurementUI.transform.parent = currentLine.transform;
            measureText = measurementUI.GetComponentInChildren<TMP_Text>();
            measurementUI.transform.position = (hit.point + tangentEnd) / 2;
            measurementUI.transform.LookAt(-(transform.position + cameraRef.forward));
            measureText.text = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString() + " mètre";

    }
}
