using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices.WindowsRuntime;
using Fusion;
using static Unity.Collections.Unicode;
public class MultiObjectSelector : Fusion.NetworkBehaviour
{
    public Transform controller; // Assign your controller here

    public Transform cameraTransform; // Player's camera transform
    public float moveSpeed = 10.0f; // Speed at which the objects move
    public Material highlightMaterial; // Material to highlight selected objects
    private Material originalMaterial; // Store original material of selected objects

    private List<GameObject> selectedObjects;// = new List<GameObject>(); // List of selected objects
    private List<GameObject> copiedObjects = new List<GameObject>(); // List of copied objects
    private bool isMovingObjects = false;
    private Dictionary<GameObject, GameObject> boundingBoxes = new Dictionary<GameObject, GameObject>(); // Map selected objects to bounding boxes

    public GameObject objectPrefab; // The prefab to spawn

    public float spawnDistance = 2.0f; // Distance at which the object spawns

    private GameObject spawnedObject;
    private bool isObjectSelected = false;

    public Transform chaireResources;

    public GameObject moveToSurfaceGO;

    private ObjectInteractionHandler objectInteractionHandler;
    public Material boundingBoxMaterial; // Optional material for visualizing the bounding box

    public Vector3 copyPasteOffset = Vector3.zero;

    private void Start()
    {
        selectedObjects  = ObjectInteractionHandler.Instance.SelectedObjects();   
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
       
    }

    private void CopyPaste_performed(InputAction.CallbackContext context)
    {
        if(selectedObjects.Count > 0)
        {
            CopyAndPasteObjects();
        }
    }

    private void SpawnObject_performed()
    {
        objectInteractionHandler.SpawnObject();
    }

    private void DeleteSelectedObjects_performed()
    {
        if (selectedObjects.Count > 0)
        {
            ObjectInteractionHandler.Instance.DeleteAllSelectedObjects();
        }
    }



    private void SelectObject_performed()
    {

        Vector3 controllerPosition = controller.position;
        Quaternion controllerRotation = controller.rotation;
        Vector3 rayDirection = controllerRotation * Vector3.forward;

        // Raycast logic
        Ray ray = new Ray(controllerPosition, rayDirection);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {

            if (hit.transform.tag == "Player")
            {

            }

            else if (hit.collider.transform.tag == "Interactable")
            {
                if (selectedObjects.Contains(hit.collider.gameObject))
                    ObjectInteractionHandler.Instance.DeselectObject(hit.collider.gameObject);
                else
                    ObjectInteractionHandler.Instance.SelectObjects(hit.collider.gameObject);
            }
            else
            {
                ObjectInteractionHandler.Instance.DeselectAllObjects();
            }
        }
        else if (selectedObjects.Count > 0)
        {
            ObjectInteractionHandler.Instance.DeselectAllObjects();
        }
    }

    void Update()
    {

        if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
        {
            SelectObject_performed();
        }
     
    

               
        Vector3 controllerPosition = controller.position;
        Quaternion controllerRotation = controller.rotation;
        Vector3 rayDirection = controllerRotation * Vector3.forward;

        // Raycast logic
        Ray ray = new Ray(controllerPosition, rayDirection);
        RaycastHit hit;

       
            if (selectedObjects.Count > 0)
            {
                MoveObjectsXZ();
            }
        if (IsSpawned)
        {
            HandleInput();
        }
    }

    void SpawnObject()
    {

            Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
            spawnPosition.y = 0;

            NetworkManager.Instance.Runner.Spawn(
                objectPrefab,
                spawnPosition,
                Quaternion.identity,
                NetworkManager.Instance.Runner.LocalPlayer, // Assign authority to the local player
                (runner, obj) =>
                {
                    obj.GetComponent<NetworkObject>().AssignInputAuthority(NetworkManager.Instance.Runner.LocalPlayer);
                    obj.transform.parent = chaireResources;
                    SelectObjects(obj.gameObject);
                   
                }
            );

        }


    void SpawnObject1()
    {
    /*    Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;
        spawnedObject = Instantiate(objectPrefab, spawnPosition, Quaternion.identity);
        spawnedObject.transform.rotation = objectPrefab.transform.rotation;
        spawnedObject.transform.parent = chaireResources;       

        SelectObjects(spawnedObject);
        spawnedObject = null;
    */
        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;
        /*  spawnedObject = Instantiate(spawnedObjectPrefab, spawnPosition, Quaternion.identity);
          spawnedObject.transform.rotation = spawnedObjectPrefab.transform.rotation;
          spawnedObject.transform.parent = runTimeGeneratedResources;
        */

        Fusion.NetworkObject spawnObj = null;
        NetworkManager.Instance.Runner.Spawn(objectPrefab, Vector3.zero, Quaternion.identity, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
        {
            spawnObj = obj;
        });
        
        spawnObj.transform.parent = chaireResources;
        SelectObjects(spawnObj.gameObject);
        spawnedObject = null;


    }


    void SelectObjects(GameObject hitObject)
    {
         if (!selectedObjects.Contains(hitObject))
         {
            //HighlightObject(hitObject);
            CreateBoundingBox(hitObject);
            selectedObjects.Add(hitObject);
            Debug.Log($"Selected: {hitObject.name}");
            isMovingObjects=true;
         }
        
    }

    void HighlightObject(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalMaterial = renderer.material;
            renderer.material = highlightMaterial;
        }
    }

    void RemoveHighlight(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && originalMaterial != null)
        {
            renderer.material = originalMaterial;
        }
    }
    void MoveObjects()
    {
        Vector2 movementInput =  OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Flatten the vectors to avoid vertical movement
        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();
            

        Vector3 movement = (forward * movementInput.y + right * movementInput.x) * moveSpeed * Time.deltaTime; 



      //  Vector3 moveDirection = new Vector3(joystickInput.x, 0, joystickInput.y) * moveSpeed * Time.deltaTime;

        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {
                NetworkObject networkObj = obj.GetComponent<NetworkObject>();
                if (networkObj != null && networkObj.HasStateAuthority)
                {
                    Vector3 newPosition = obj.transform.position + movement;

                    // Use NetworkTransform if present
                    NetworkTransform networkTransform = obj.GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {
                        networkTransform.Teleport(newPosition);
                    }
                    else
                    {
                        obj.transform.position = newPosition; // Fallback if no NetworkTransform
                    }

                    //Debug.Log($"Moved {obj.name} to {newPosition}");
                }
                else
                {
                    Debug.LogWarning($"Cannot move {obj.name}. No State Authority!");
                }
            }
        }
    }

    public void  MoveObjectsXZ()
    {
        Vector2 movementInput =  OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Flatten the vectors to avoid vertical movement
        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();
        float x= Mathf.Abs(movementInput.y);
        float z = Mathf.Abs(movementInput.x);

       // movement = (forward * movementInput.y + right * movementInput.x) * moveSpeed * Time.deltaTime;   
        Vector3 movement;
        if (x > z)
            movement = (forward * movementInput.y ) * moveSpeed * Time.deltaTime;
        else
            movement = (right * movementInput.x) * moveSpeed * Time.deltaTime;


        //  Vector3 moveDirection = new Vector3(joystickInput.x, 0, joystickInput.y) * moveSpeed * Time.deltaTime;

        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {
                NetworkObject networkObj = obj.GetComponent<NetworkObject>();
                if (networkObj != null && networkObj.HasStateAuthority)
                {
                    Vector3 newPosition = obj.transform.position + movement;

                    // Use NetworkTransform if present
                    NetworkTransform networkTransform = obj.GetComponent<NetworkTransform>();
                    if (networkTransform != null)
                    {
                        networkTransform.Teleport(newPosition);
                    }
                    else
                    {
                        obj.transform.position = newPosition; // Fallback if no NetworkTransform
                    }

                    //Debug.Log($"Moved {obj.name} to {newPosition}");
                }
                else
                {
                    Debug.LogWarning($"Cannot move {obj.name}. No State Authority!");
                }
            }
        }
    }


    void MoveObjects1()
    {

        Vector2 joystickInput =   OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        Vector3 moveDirection = new Vector3(joystickInput.x, 0, joystickInput.y) * moveSpeed * Time.deltaTime;

        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {
                NetworkObject networkObj = obj.GetComponent<NetworkObject>();
                if (networkObj.HasStateAuthority)
                {
                    Vector3 direcion  =  cameraTransform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.z));
                    direcion.y = 0;
                    networkObj.transform.position += direcion;
                    Debug.Log(" Move Direction x "+ direcion.x + "  z :  " +direcion.z);    

                 /*  NetworkCharacterController networkCharacterController = obj.GetComponent<NetworkCharacterController>();
                    if (networkCharacterController != null)
                    {
                        networkCharacterController.Move(moveDirection);
                    }*/
                }
            

                
                // Constrain movement to X and Z directions
                
               
               // Vector3 direcion  =  cameraTransform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.z));
                //direcion.y = 0;
              //  obj.transform.position += direcion;


                // UpdateBoundingBox(obj);
            }
        }
    }

    public void DeleteAllSelectedObjects()
    {
        List<GameObject> objectsToBeDeleted = new List<GameObject>(); // List of copied objects
        RemoveBoundingBoxes();

        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null) // Check if the GameObject is not null
            {
                Destroy(obj); // Destroy the GameObject
            }
        }
        selectedObjects.Clear();

    }
    private void RemoveBoundingBoxes()
    {
        
        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {
                //   RemoveHighlight(obj);
                RemoveBoundingBox(obj);
            }
        }
        //clear list
       // boundingBoxes.Clear();
    }

    void DeselectAllObjects()
    {
        RemoveBoundingBoxes();
        selectedObjects.Clear();
       
    }
    void DeselectObject(GameObject selectedObject)
    {
       // RemoveHighlight(selectedObject);
        RemoveBoundingBox(selectedObject);
        selectedObjects.Remove(selectedObject);
    }





    void CreateBoundingBox1(GameObject obj)
    {
        Bounds bounds = obj.GetComponent<Renderer>().bounds;

        GameObject boundingBox = new GameObject("BoundingBox");
        LineRenderer lineRenderer = boundingBox.AddComponent<LineRenderer>();
       
        boundingBox.transform.SetParent(obj.transform);
        lineRenderer.material = highlightMaterial;
        lineRenderer.positionCount = 8;
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;

        Vector3[] corners = new Vector3[8];
        corners[0] = bounds.min;
        corners[1] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[2] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[3] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[4] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[5] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[6] = bounds.max;
        corners[7] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);

        lineRenderer.SetPositions(new Vector3[] {
            corners[0], corners[1], corners[5], corners[4], corners[0],
            corners[3], corners[7], corners[6], corners[2], corners[3],
            corners[2], corners[1], corners[5], corners[6], corners[7], corners[4]
        });

        boundingBox.transform.position = obj.transform.position;
        // Make the bounding box a child of the target object
        boundingBox.transform.SetParent(obj.transform);

        //  boundingBoxes[obj] = boundingBox;
    }

    void RemoveBoundingBox(GameObject obj)
    {
        Transform bb = obj.transform.Find("BoundingBox");
        if (bb != null)
        {
            Destroy(bb.gameObject);
        }

      /*  if (boundingBoxes.ContainsKey(obj))
        {
            Destroy(boundingBoxes[obj]);
            boundingBoxes.Remove(obj);
        }
      */
    }
    void UpdateBoundingBox(GameObject obj)
    {
        Transform boundingBox = obj.transform.Find("BoundingBox");
        if (boundingBox!= null)
        {
            Bounds bounds = obj.GetComponent<Renderer>().bounds;
            LineRenderer lineRenderer = boundingBox.GetComponent<LineRenderer>();

            Vector3[] corners = new Vector3[8];
            corners[0] = bounds.min;
            corners[1] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            corners[2] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            corners[3] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            corners[4] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            corners[5] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            corners[6] = bounds.max;
            corners[7] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);

            lineRenderer.SetPositions(new Vector3[] {
                corners[0], corners[1], corners[5], corners[4], corners[0],
                corners[3], corners[7], corners[6], corners[2], corners[3],
                corners[2], corners[1], corners[5], corners[6], corners[7], corners[4]
            });
        }
    }


   public void CopyAndPasteObjects()
    {
        copiedObjects.Clear();

        // Copy selected objects
        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {

                Vector3 spawnPosition = obj.transform.position + copyPasteOffset;
                spawnPosition.y = 0;

                NetworkManager.Instance.Runner.Spawn(
                    obj,
                    spawnPosition,
                    Quaternion.identity,
                    NetworkManager.Instance.Runner.LocalPlayer, // Assign authority to the local player
                    (runner, pasteObj) =>
                    {
                        pasteObj.GetComponent<NetworkObject>().AssignInputAuthority(NetworkManager.Instance.Runner.LocalPlayer);
                        pasteObj.transform.parent = chaireResources;
                        copiedObjects.Add(pasteObj.gameObject);

                        Debug.Log($"Copied: {pasteObj.name}");

                           
                    }
                );



      /*          GameObject copy = Instantiate(obj);
                copy.transform.position = obj.transform.position + copyPasteOffset; // Offset copied object
                copy.transform.parent = chaireResources;

                copiedObjects.Add(copy);

                Debug.Log($"Copied: {obj.name}");
      */
            }
        }





        //deselect all currently selected objects 

        ObjectInteractionHandler.Instance.DeselectAllObjects();
        // Add copied objects to selected list
        foreach (GameObject copy in copiedObjects)
        {
            //CreateBoundingBox(copy);
            ObjectInteractionHandler.Instance.SelectObjects(copy);
           // selectedObjects.Add(copy);
        }

        Debug.Log("Pasted copied objects");
    }
    public void CreateBoundingBox(GameObject targetObject)
    {
         GameObject boundingBox;
        if (targetObject == null)
        {
            Debug.LogError("Target object is not assigned!");
            return;
        }

        // Create a new GameObject for the bounding box
        boundingBox = new GameObject("BoundingBox");

        // Add a BoxCollider to the bounding box
        BoxCollider boxCollider = boundingBox.AddComponent<BoxCollider>();

        // Add a MeshRenderer and MeshFilter for visualization (optional)
        MeshRenderer meshRenderer = boundingBox.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = boundingBox.AddComponent<MeshFilter>();

        // Set the MeshFilter to use a cube mesh
        meshFilter.mesh = CreateCubeMesh();

        // Assign the material if provided
        if (boundingBoxMaterial != null)
        {
            meshRenderer.material = boundingBoxMaterial;
        }

        // Set the bounding box size to match the target object
        Bounds bounds = CalculateBounds(targetObject);
        boxCollider.center = bounds.center - targetObject.transform.position;
        boxCollider.size = bounds.size;
        boundingBox.transform.position = bounds.center;

        // Make the bounding box a child of the target object
        boundingBox.transform.SetParent(targetObject.transform);

        Debug.Log("Bounding box created and attached.");
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    private Mesh CreateCubeMesh()
    {
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cubeMesh = tempCube.GetComponent<MeshFilter>().mesh;
        DestroyImmediate(tempCube);
        return cubeMesh;
    }

  

    private NetworkObject controlledObject; // Reference to the spawned object
    bool IsSpawned = true;


    private void HandleInput()
    {
        // Spawn object when pressing 'B'
        if (Input.GetKeyDown(KeyCode.B) && controlledObject == null)
        {
            SpawnControlledObject();
        }

        // Move the controlled object if it exists
        if (controlledObject != null && controlledObject.HasStateAuthority)
        {
            MoveControlledObject();
        }
    }

    private void SpawnControlledObject()
    {
        Vector3 spawnPosition = transform.position + transform.forward * 2f; // Spawn in front of the player
       
        spawnPosition.y = 0;

        NetworkManager.Instance.Runner.Spawn(
            objectPrefab,
            spawnPosition,
            Quaternion.identity,
            NetworkManager.Instance.Runner.LocalPlayer, // Assign authority to the local player
            (runner, obj) =>
            {
                obj.GetComponent<NetworkObject>().AssignInputAuthority(NetworkManager.Instance.Runner.LocalPlayer);
                obj.transform.parent = chaireResources;
                ObjectInteractionHandler.Instance.SelectObjects(obj.gameObject);

            }
        );
        Debug.Log("Spawned controlled object");
    }

    private void MoveControlledObject()
    {
        // Get input from keyboard or joystick
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(moveX, 0, moveZ).normalized * moveSpeed * Time.deltaTime;

        if (moveDirection != Vector3.zero)
        {
            // Update position
            controlledObject.transform.position += moveDirection;
            Debug.Log($"Moving object to: {controlledObject.transform.position}");
        }
    }
}



