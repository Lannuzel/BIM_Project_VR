
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Fusion;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

public class ReferenceLineFromObject : NetworkBehaviour
{
    private int count = 0;
    public Material lineMaterial; // Material for the LineRenderer
    private GameObject lineObj;



    private TMP_Text measureText;

    public Transform cameraRef;

    private List<GameObject> selectedObjects;// = new List<GameObject>(); // List of selected objects
    private ObjectInteractionHandler objectInteractionHandler;

    public LayerMask layerMask;
    private bool showRefLines = false;

    public Vector3 posOffset = Vector3.zero;

    public NetworkedLine linePrefab; // Assign this in the Inspector
    private NetworkedLine spawnedLine;


    public NetworkedMUI measureUI;

    public float networkedDistanceX { get; set; }
    public float networkedDistanceZ { get; set; }

    // Start is called before the first frame update
    private void Start()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
    }


    public void Update()
    {
        if (showRefLines)
        {
            foreach (GameObject obj in selectedObjects)
            {
                Transform DirectionLineZ =  obj.transform.Find("DirectionLineZ");
                if (DirectionLineZ == null)
                {
                    DrawPositionIndicatorV0(obj);
                }
                else 
                {
                    UpdatePositionIndicator(obj);
                }
                 
            }
        }

    }
    public void ToggleShowReferenceLines()
    {
        showRefLines = !showRefLines;

        if (!showRefLines)
        {
            DeleteReferenceLines();
        }
    }
    // Update is called once per frame

    public void UpdatePositionIndicator(GameObject go)
    {
        RaycastHit hit;
        Vector3 transformPos = go.transform.position;
        Vector3 position = new Vector3(transformPos.x + posOffset.x, transformPos.y + posOffset.y, transformPos.z + posOffset.z);

        Transform DirectionLineZ = go.transform.Find("DirectionLineZ");

        if (DirectionLineZ == null) return;

        if (DirectionLineZ != null)
        {

            Transform spawnedUITransform = ObjectInteractionHandler.Instance.FindChildWithTagRecursive(DirectionLineZ, "MeasureUI");
            NetworkedMUI spawnedUI = spawnedUITransform.gameObject.GetComponent<NetworkedMUI>();

           
            LineRenderer currentLine = DirectionLineZ.transform.GetComponent<LineRenderer>();

            //draw a line towards the -Z axis from the corner point
            Ray ray = new Ray(position, new Vector3(0, 0, 1));
            // Perform a raycast
            if (Physics.Raycast(ray, out hit, 20, layerMask))
            {

                DirectionLineZ.gameObject.SetActive(true);
                NetworkObject networklineObj = DirectionLineZ.gameObject.GetComponent<NetworkObject>();
                if (networklineObj != null && networklineObj.HasStateAuthority)
                {
                    // Use NetworkTransform if present
                    NetworkTransform networkTransform = DirectionLineZ.GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {

                        NetworkedLine spawnedLine = networklineObj.gameObject.GetComponent<NetworkedLine>();
                        spawnedLine.SetLinePositions(position, hit.point, true);

                        NetworkObject networkedUI = spawnedUITransform.GetComponent<NetworkObject>();
                        if (networkedUI != null && networkedUI.HasStateAuthority)
                        {
                            // Use NetworkTransform if present
                            NetworkTransform networkUITransform = spawnedUITransform.GetComponent<NetworkTransform>();
                            if (networkUITransform != null)
                            {
                                networkTransform.Teleport((currentLine.GetPosition(0) + currentLine.GetPosition(1)) / 2);

                                string data = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString("F2") + " mètre";
                                spawnedUI.SetUIPositions((currentLine.GetPosition(0) + currentLine.GetPosition(1)) / 2, data, true);

                                
                            }
                            //Debug.Log($"Moved {obj.name} to {newPosition}");
                        }



                    }
                    //Debug.Log($"Moved {obj.name} to {newPosition}");
                }
            }
            else
            {
                DirectionLineZ.gameObject.SetActive(false);
                currentLine.enabled = false;
               // measurementUI.SetActive(false);
            }
        }
        Transform DirectionLineX = go.transform.Find("DirectionLineX");
        if (DirectionLineX == null) return;

        if (DirectionLineX != null)
        {
            Transform spawnedUITransform = ObjectInteractionHandler.Instance.FindChildWithTagRecursive(DirectionLineX, "MeasureUI");
            NetworkedMUI spawnedUI = spawnedUITransform.gameObject.GetComponent<NetworkedMUI>();

            LineRenderer currentLine = DirectionLineX.GetComponent<LineRenderer>();


            //draw a line towards the -X axis from the corner point
            Ray ray = new Ray(position, new Vector3(1, 0, 0));
            // Perform a raycast
            if (Physics.Raycast(ray, out hit, 20, layerMask))
            {

                DirectionLineX.gameObject.SetActive(true);
                NetworkObject networklineObj = DirectionLineX.GetComponent<NetworkObject>();
                if (networklineObj != null && networklineObj.HasStateAuthority)
                {
                    // Use NetworkTransform if present
                    NetworkTransform networkTransform = DirectionLineX.GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {

                        NetworkedLine spawnedLine = networklineObj.gameObject.GetComponent<NetworkedLine>();
                        spawnedLine.SetLinePositions(position, hit.point, true);

                        NetworkObject networkedUI = spawnedUITransform.GetComponent<NetworkObject>();
                        if (networkedUI != null && networkedUI.HasStateAuthority)
                        {
                            // Use NetworkTransform if present
                            NetworkTransform networkUITransform = spawnedUITransform.GetComponent<NetworkTransform>();
                            if (networkUITransform != null)
                            {
                                networkTransform.Teleport((currentLine.GetPosition(0) + currentLine.GetPosition(1)) / 2);

                                string data = (Vector3.Distance(currentLine.GetPosition(0), currentLine.GetPosition(1))).ToString("F2") + " mètre";
                                spawnedUI.SetUIPositions((currentLine.GetPosition(0) + currentLine.GetPosition(1)) / 2, data, true);

                            
                            }
                            //Debug.Log($"Moved {obj.name} to {newPosition}");
                        }

                    }
                    //Debug.Log($"Moved {obj.name} to {newPosition}");
                }
            }
            else
            {
                DirectionLineX.gameObject.SetActive(false); 

                currentLine.enabled = false;
                //measurementUI.SetActive(false);
            }
        }
   
    }

    public void DeleteReferenceLines()
    {
        showRefLines = false;

        foreach (GameObject obj in selectedObjects)
        {
            Transform directionLineZ = obj.transform.Find("DirectionLineZ");
            if (directionLineZ != null)
                Destroy(directionLineZ.gameObject);

            Transform directionLineX = obj.transform.Find("DirectionLineX");
            if (directionLineX != null)
                Destroy(directionLineX.gameObject);
        }

    }

    public void DrawPositionIndicatorV0(GameObject go)
    {
        Vector3 transformPos = go.transform.position;
        Collider collider = go.GetComponent<Collider>();
        Vector3 position = new Vector3(transformPos.x + posOffset.x, transformPos.y + posOffset.y, transformPos.z + posOffset.z);

        Transform directionLineZ = go.transform.Find("DirectionLineZ");
            if (directionLineZ != null)
                Destroy(directionLineZ.gameObject);
        GameObject dirLineZ =   DrawLine(go, position, position, "DirectionLineZ");
        DrawUI(dirLineZ.transform, position);


         Transform directionLineX = go.transform.Find("DirectionLineX");
            if (directionLineX != null)
                Destroy(directionLineX.gameObject);
        GameObject dirLineX =   DrawLine(go, position, position, "DirectionLineX");
        DrawUI(dirLineX.transform, position);

        dirLineZ.SetActive(false);
        dirLineX.SetActive(false); 

    }



    private void DrawUI(Transform parentRef, Vector3 position)
    {
     //   Debug.LogError("Hurry it workds*************");


        NetworkedMUI spawnedUI = NetworkManager.Instance.Runner.Spawn(measureUI, Vector3.zero, Quaternion.identity);
        
        // Calculate midpoint between pointA and pointB
       

        spawnedUI.transform.parent = parentRef;
        spawnedUI.SetCameraRef(cameraRef);
        // Set UI position at the midpoint
        spawnedUI.SetUIPositions(position, "", true);

    }



    private GameObject DrawLine(GameObject go, Vector3 p1, Vector3 p2, string name)
    {

        Fusion.NetworkObject spawnNWLine = null;
        NetworkManager.Instance.Runner.Spawn(linePrefab, Vector3.zero, linePrefab.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
        {
            spawnNWLine = obj;
            spawnedLine = spawnNWLine.gameObject.GetComponent<NetworkedLine>();
        });
        spawnNWLine.transform.name = name;
        spawnNWLine.transform.parent = go.transform;
        
        GameObject lineObj = spawnNWLine.gameObject;

        NetworkObject networklineObj = lineObj.GetComponent<NetworkObject>();
        if (networklineObj != null && networklineObj.HasStateAuthority)
        {
            // Use NetworkTransform if present
            NetworkTransform networkTransform = lineObj.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {

                NetworkedLine spawnedLine = networklineObj.gameObject.GetComponent<NetworkedLine>();
                spawnedLine.SetLinePositions(p1, p2, true);

                LineRenderer currentLine = lineObj.GetComponent<LineRenderer>();



            }
            //Debug.Log($"Moved {obj.name} to {newPosition}");
        }
        return lineObj;
    }



    // Calculates rotation to face the main camera
    private Quaternion GetRotationFacingCamera(Vector3 objectPosition)
    {
        Vector3 directionToCamera = (cameraRef.position - objectPosition).normalized;
        return Quaternion.LookRotation(-directionToCamera, Vector3.up);
    }

}


