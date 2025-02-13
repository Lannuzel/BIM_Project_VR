
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

public class ReferenceLineFromObject : MonoBehaviour
{
    private int count = 0;
    public Material lineMaterial; // Material for the LineRenderer
    private GameObject lineObj;

    public GameObject measurePref;
    private GameObject measurementUI;
    private TMP_Text measureText;

    public Transform cameraRef;

    private List<GameObject> selectedObjects;// = new List<GameObject>(); // List of selected objects
    private ObjectInteractionHandler objectInteractionHandler;

    public LayerMask layerMask;
    private bool showRefLines = false;

    public Vector3 posOffset = Vector3.zero;   

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
                Transform DirectionLineZ = obj.transform.Find("DirectionLineZ");
                if (DirectionLineZ != null)
                {
                    UpdatePositionIndicator(obj);
                }
                else DrawPositionIndicator(obj);
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
    public void DrawPositionIndicators()
    {
        foreach (GameObject obj in selectedObjects)
        {
            DrawPositionIndicator(obj);
        }
        showRefLines = true;

    }
    public void UpdatePositionIndicator(GameObject go)
    {
        RaycastHit hit;
        Vector3 transformPos = go.transform.position;
        Vector3 position = new Vector3(transformPos.x + posOffset.x, transformPos.y + posOffset.y, transformPos.z + posOffset.z);

        Transform DirectionLineZ = go.transform.Find("DirectionLineZ");
        if (DirectionLineZ == null) return;

        if (DirectionLineZ != null)
        {
            LineRenderer dLine = DirectionLineZ.GetComponent<LineRenderer>();


            //draw a line towards the -Z axis from the corner point
            Ray ray = new Ray(position, new Vector3(0, 0, 1));
            // Perform a raycast
            if (Physics.Raycast(ray, out hit, 20, layerMask))
            {
                dLine.SetPosition(0, position);
                dLine.SetPosition(1, hit.point);


                measurementUI = ObjectInteractionHandler.Instance.FindChildWithTagRecursive(DirectionLineZ, "Measure").gameObject;
                measureText = measurementUI.GetComponentInChildren<TMP_Text>();
                measurementUI.transform.position = (dLine.GetPosition(0) + dLine.GetPosition(1)) / 2;
                measurementUI.transform.LookAt(-(measurementUI.transform.position + cameraRef.forward));
                measureText.text = (Vector3.Distance(dLine.GetPosition(0), dLine.GetPosition(1))).ToString() + " mètre";

            }
        }
        Transform DirectionLineX = go.transform.Find("DirectionLineX");
        if (DirectionLineX == null) return;

        if (DirectionLineX != null)
        {
            LineRenderer dLine = DirectionLineX.GetComponent<LineRenderer>();


            //draw a line towards the -X axis from the corner point
            Ray ray = new Ray(position, new Vector3(1, 0, 0));
            // Perform a raycast
            if (Physics.Raycast(ray, out hit, 20, layerMask))
            {
                dLine.SetPosition(0, position);
                dLine.SetPosition(1, hit.point);
                measurementUI = ObjectInteractionHandler.Instance.FindChildWithTagRecursive(DirectionLineX, "Measure").gameObject;
                measureText = measurementUI.GetComponentInChildren<TMP_Text>();
                measurementUI.transform.position = (dLine.GetPosition(0) + dLine.GetPosition(1)) / 2;
                measurementUI.transform.LookAt(-(measurementUI.transform.position + cameraRef.forward));
                measureText.text = (Vector3.Distance(dLine.GetPosition(0), dLine.GetPosition(1))).ToString() + " mètre";
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


    public void DrawPositionIndicator(GameObject go)
    {

        RaycastHit hit;
        //TODO Check if the hit Object has specific tag

        Vector3 transformPos = go.transform.position;
        Collider collider = go.GetComponent<Collider>();
        Vector3 position = new Vector3(transformPos.x + posOffset.x, transformPos.y + posOffset.y, transformPos.z + posOffset.z);

        //draw a line towards the -Z axis from the corner point
        Ray ray = new Ray(position, new Vector3(0, 0, 1));
        // Perform a raycast
        if (Physics.Raycast(ray, out hit, 20, layerMask))
        {
            Transform directionLineZ = go.transform.Find("DirectionLineZ");
            if (directionLineZ != null)
                Destroy(directionLineZ.gameObject);
            DrawLine(go, position, hit.point, "DirectionLineZ");
        }
        //draw a line towards the -X axis from the corner point
        ray = new Ray(position, new Vector3(1, 0, 0));
        // Perform a raycast
        if (Physics.Raycast(ray, out hit, 20, layerMask))
        {
            Transform directionLineX = go.transform.Find("DirectionLineX");
            if (directionLineX != null)
                Destroy(directionLineX.gameObject);
            DrawLine(go, position, hit.point, "DirectionLineX");
        }
    }

    private GameObject DrawLine(GameObject go, Vector3 p1, Vector3 p2, string name)
    {
        GameObject directionLine = new GameObject(name);
        directionLine.transform.parent = go.transform;
        LineRenderer dLine = directionLine.AddComponent<LineRenderer>();
        dLine.material = lineMaterial;
        dLine.startWidth = 0.0051f;
        dLine.endWidth = 0.0051f;
        dLine.positionCount = 2;
        dLine.SetPosition(0, p1);
        dLine.SetPosition(1, p2);

        measurementUI = Instantiate(measurePref);
        measurementUI.transform.parent = directionLine.transform;
        measureText = measurementUI.GetComponentInChildren<TMP_Text>();
        measurementUI.transform.position = (p1 + p2) / 2;
        measurementUI.transform.LookAt(-(transform.position + cameraRef.forward));
        measureText.text = (Vector3.Distance(dLine.GetPosition(0), dLine.GetPosition(1))).ToString() + " mètre";

        return directionLine;
    }
}



/*
 * 
 * using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Samples.Hands;

public class ReferenceLineFromObject : MonoBehaviour
{
    private int count = 0;
    public Material lineMaterial; // Material for the LineRenderer
    private GameObject lineObj;

    public GameObject measurePref;
    private GameObject measurementUI;
    private TMP_Text measureText;

    public Transform cameraRef;
    public Vector3 posOffset = Vector3.zero;    

    private List<GameObject> selectedObjects;// = new List<GameObject>(); // List of selected objects
    private ObjectInteractionHandler objectInteractionHandler;

    public LayerMask layerMask;
    private bool showRefLines = false;
    // Start is called before the first frame update
    private void Start()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
    }

    public void  LateUpdate()
    {
        if (showRefLines)
        {
            foreach (GameObject obj in selectedObjects)
            {
                Transform DirectionLineZ = obj.transform.Find("DirectionLineZ");
                if (DirectionLineZ != null)
                {
                    UpdatePositionIndicator(obj);
                }
                else DrawPositionIndicator(obj);
            }
        }
        else
        {
            
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
    public void DrawPositionIndicators()
    {
        foreach (GameObject obj in selectedObjects)
        {
                DrawPositionIndicator(obj);
        }
        showRefLines = true;

    }
    public void UpdatePositionIndicator(GameObject go)
    {
        RaycastHit hit;
        Vector3 transformPos = go.transform.position;
        Collider collider = go.GetComponent<Collider>();
        // Vector3 position = new Vector3(transformPos.x - collider.bounds.size.x / 2, transformPos.y + collider.bounds.size.y / 2, transformPos.z - collider.bounds.size.z / 2);
        //Vector3 position = new Vector3((collider.bounds.max.x + collider.bounds.min.x) / 2, collider.bounds.max.y, (collider.bounds.min.z + collider.bounds.max.z) / 2);
        Vector3 position = new Vector3(transformPos.x+ posOffset.x, transformPos.y+posOffset.y,  transformPos.z+posOffset.z );
        Transform DirectionLineZ = go.transform.Find("DirectionLineZ");
        if (DirectionLineZ == null)  return;

        if (DirectionLineZ != null) {
            LineRenderer dLine = DirectionLineZ.GetComponent<LineRenderer>();


            //draw a line towards the -Z axis from the corner point
            Ray ray = new Ray(position, new Vector3(0, 0, -1));
            // Perform a raycast
            if (Physics.Raycast(ray, out hit,20,layerMask))
            {
                dLine.SetPosition(0, position);
                dLine.SetPosition(1, hit.point);


                measurementUI = ObjectInteractionHandler.Instance.FindChildWithTagRecursive(DirectionLineZ, "Measure").gameObject;
                measureText = measurementUI.GetComponentInChildren<TMP_Text>();
                measurementUI.transform.position = (dLine.GetPosition(0) + dLine.GetPosition(1)) / 2;
                measurementUI.transform.LookAt(-(measurementUI.transform.position + cameraRef.forward));
                measureText.text = (Vector3.Distance(dLine.GetPosition(0), dLine.GetPosition(1))).ToString() + " mètre";

            }
        }
        Transform DirectionLineX = go.transform.Find("DirectionLineX");
        if (DirectionLineX != null)
        {
            LineRenderer dLine = DirectionLineX.GetComponent<LineRenderer>();


            //draw a line towards the -X axis from the corner point
            Ray ray = new Ray(position, new Vector3(-1, 0, 0));
            // Perform a raycast
            if (Physics.Raycast(ray, out hit, 20, layerMask))
            {
                dLine.SetPosition(0, position);
                dLine.SetPosition(1, hit.point);
                measurementUI = ObjectInteractionHandler.Instance.FindChildWithTagRecursive(DirectionLineX, "Measure").gameObject;
                measureText = measurementUI.GetComponentInChildren<TMP_Text>();
                measurementUI.transform.position = (dLine.GetPosition(0) + dLine.GetPosition(1)) / 2;
                measurementUI.transform.LookAt(-(measurementUI.transform.position + cameraRef.forward));
                measureText.text = (Vector3.Distance(dLine.GetPosition(0), dLine.GetPosition(1))).ToString() + " mètre";
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


    public void DrawPositionIndicator(GameObject go)
    {

        RaycastHit hit;
            //TODO Check if the hit Object has specific tag

            Vector3 transformPos = go.transform.position;
            Collider collider = go.GetComponent<Collider>();
       // Vector3 position = new Vector3((collider.bounds.max.x + collider.bounds.min.x) / 2, collider.bounds.max.y, (collider.bounds.min.z + collider.bounds.max.z) / 2);
        Vector3 position = new Vector3(transformPos.x + posOffset.x, transformPos.y + posOffset.y, transformPos.z + posOffset.z);
        //Vector3 position = new Vector3(collider.bounds.max.x, collider.bounds.max.y, collider.bounds.max.z);

        //draw a line towards the -Z axis from the corner point
        Ray ray = new Ray(position, new Vector3(0, 0, 1));
            // Perform a raycast
            if (Physics.Raycast(ray, out hit,20, layerMask))
            {
            DrawLine(go, position, hit.point, "DirectionLineZ"); 
            }
            //draw a line towards the -X axis from the corner point
            ray = new Ray(position, new Vector3(-1, 0, 0));
            // Perform a raycast
            if (Physics.Raycast(ray, out hit, 20, layerMask ))
            {
                DrawLine(go, position, hit.point, "DirectionLineX");
            }
    }

    private GameObject DrawLine(GameObject go,  Vector3 p1, Vector3 p2, string name)
    {
        GameObject directionLine = new GameObject(name);
        directionLine.transform.parent = go.transform;
        LineRenderer dLine = directionLine.AddComponent<LineRenderer>();
        dLine.material = lineMaterial;
        dLine.startWidth = 0.0051f;
        dLine.endWidth = 0.0051f;
        dLine.positionCount = 2;
        dLine.SetPosition(0, p1);
        dLine.SetPosition(1, p2);

        measurementUI = Instantiate(measurePref);
        measurementUI.transform.parent = directionLine.transform;
        measureText = measurementUI.GetComponentInChildren<TMP_Text>();
        measurementUI.transform.position = (p1 + p2) / 2;
        measurementUI.transform.LookAt(-(transform.position + cameraRef.forward));
        measureText.text = (Vector3.Distance(dLine.GetPosition(0), dLine.GetPosition(1))).ToString() + " mètre";

        return directionLine;
    }
}
*/

