using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.XR;


public class LineScript : MonoBehaviour
{

   public Transform linkedHandPosition;
   private List<Vector3> points = new List<Vector3>();
    private LineRenderer line;

    private Vector3 rayHitPos;
    private Vector3 mousePos;
    public Material material;
    private int currLines = 0;
    public float startWidth = 0.05f;
    public float endWidth = 0.05f;
     private float distance;
     private Vector3 startPos;

    public GameObject textPrefab;
       // Reference to the prefab you want to instantiate
    public Transform camera;
    public GameObject lineManipMenu;
    private Transform selectedLine;
 
    void Start()
    {
 
    }
 
   /* void Update()
    {

            if(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.Touch)>0)
            {
                Debug.Log("Right trigger Pressed");

                
            }


        if (Input.GetMouseButtonDown(0))
        {
            if (line == null)
            {
                createLine();
            }
 
          //  mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos = GetMousePosition();
            //mousePos.z = 0;
            line.SetPosition(0, mousePos);
            line.SetPosition(1, mousePos);
        }
        else if (Input.GetMouseButtonUp(0) && line)
        {
           // mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos = GetMousePosition();
            //mousePos.z = 0;
            line.SetPosition(1, mousePos);
            line = null;
            currLines++;
        }
        else if (Input.GetMouseButton(0) && line)
        {
            //mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos = GetMousePosition();
           // mousePos.z = 0;
            line.SetPosition(1, mousePos);
            Vector3[] positions= new Vector3[2];
            line.GetPositions(positions);
            Transform textObj =  line.transform.Find("Canvas(Clone)");
            if(textObj != null){
                textObj.position = new Vector3((positions[1].x - positions[0].x)/2, (positions[1].y - positions[0].y)/2, (positions[1].z - positions[0].z)/2);;
              distance = (positions[1] - positions[0]).magnitude;
               TMP_Text mytext = textObj.GetComponentInChildren<TMP_Text>();
              if (mytext != null)
                mytext.text = distance.ToString("F2") + "m"  ;
                textObj.rotation  = line.transform.rotation;
            }
            else Debug.LogWarning("txt is null");
        }

    } */

 Vector3 GetMousePosition()
    {
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    void createLine()
    {
       GameObject newLine = new GameObject("Line" + currLines);
        line = newLine.AddComponent<LineRenderer>();
        newLine.AddComponent<MeshCollider>();
        newLine.tag = "line";
        newLine.AddComponent<MeshRenderer>();      
        line.material = material;
        line.positionCount = 2;
        line.startWidth = startWidth;
        line.endWidth = endWidth;
        line.useWorldSpace = false;
        line.numCapVertices = 50;

        GameObject textObj = Instantiate(textPrefab, points[0], Quaternion.identity);
        if(textObj!=null){
            textObj.transform.SetParent(line.transform);
            Debug.LogWarning(" text Prefab created successfully ");
            TMP_Text mytext = textObj.GetComponentInChildren<TMP_Text>();
            if (mytext != null)
                 mytext.text = "TEst";
            textObj.transform.LookAt(camera);
            textObj.transform.RotateAround(textObj.transform.position, textObj.transform.up, 180f);
        }
        else Debug.Log("issue creating prefab");    
        
          
    }



private void LateUpdate()
{
   



     if(OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger, OVRInput.Controller.Touch)>0){

        Transform target = GetVRRayHitTransform();
        if(target!= null)
        {
            if(target.CompareTag("line")){
                selectedLine = target;
                lineManipMenu.SetActive(true);
            }
        }


    //  else 
    {
              //Debug.Log("Right trigger Pressed");
                rayHitPos = GetVRRayHitPosition();
            //mousePos.z = 0;
            
            points.Add(rayHitPos);
            if (line == null)
            {
                createLine();
            }
            line.SetPosition(0, rayHitPos);
 
          //  mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            line.SetPosition(1, rayHitPos);
        }   
            
            }

          if(OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger, OVRInput.Controller.Touch)>0 && line){
            rayHitPos = GetVRRayHitPosition();
           points[1] = rayHitPos;
           line.SetPositions(points.ToArray());


            Transform textObj =  line.transform.Find("MeasureLine(Clone)");
            if(textObj != null){
              textObj.position = new Vector3((points[1].x + points[0].x)/2, (points[1].y + points[0].y)/2, (points[1].z + points[0].z)/2);
              distance = (points[1] - points[0]).magnitude;
               TMP_Text mytext = textObj.GetComponentInChildren<TMP_Text>();
              if (mytext != null){
                mytext.text = "hello";
                mytext.text = distance.ToString("F2") + "m"  ;
              }
              else 
                 Debug.Log("unable to find text component");
                
                textObj.LookAt(camera);
                textObj.RotateAround(textObj.position, textObj.up, 180f);
            }
            Debug.Log("unable to find Measure component");


            
         }
        else if(OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger, OVRInput.Controller.Touch) == 0 && line){
            rayHitPos = GetVRRayHitPosition();
           points[1] = rayHitPos;
           line.SetPositions(points.ToArray());
            line = null;
            currLines++;
            Debug.Log("  LIne is over");
            points.Clear();
         }
    
   
}

Vector3 GetVRRayHitPosition()
    {
         RaycastHit hit;
        if(Physics.Raycast(linkedHandPosition.position, linkedHandPosition.forward, out hit, 10f)){
            return hit.point;
        }
        return Vector3.zero;
    }

    Transform GetVRRayHitTransform()
    {
         RaycastHit hit;
        if(Physics.Raycast(linkedHandPosition.position, linkedHandPosition.forward, out hit, 10f)){
            return hit.transform;
        }
        return null;
    }
    public void DestroyLine(){
        Destroy(selectedLine.gameObject);
        lineManipMenu.SetActive(false);
    }    
}