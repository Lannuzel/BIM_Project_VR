
using Fusion;

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices.WindowsRuntime;
using static Unity.Collections.Unicode;
using Unity.Services.Lobbies.Models;
using Fusion.Sockets;
using static FindSpawnPositions;
public class ObjectSelectorNW : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField]
    private NetworkPrefabRef NWObjectPrefab;

    public Transform controller; // Assign your controller here

    public Transform cameraTransform; // Player's camera transform
    public float moveSpeed = 1.0f; // Speed at which the objects move
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

    private XRIBIMInputActions playerInputActions;
    private ObjectInteractionHandler objectInteractionHandler;
    public Material boundingBoxMaterial; // Optional material for visualizing the bounding box

    public Vector3 copyPasteOffset = Vector3.zero;

    private void Start()
    {
       // selectedObjects  = ObjectInteractionHandler.Instance.SelectedObjects();   
    }
    private void Awake()
    {

       // playerInputActions.XRIRightInteraction.Enable();
        //playerInputActions.XRILeftInteraction.Enable();

        /*     objectInteractionHandler = ObjectInteractionHandler.Instance;
             playerInputActions = objectInteractionHandler.playerInputActions;
             playerInputActions.XRIRightInteraction.Enable();
             playerInputActions.XRILeftInteraction.Enable();

             //adding actionListeners
             playerInputActions.XRIRightInteraction.Select.performed += SelectObject_performed;
             playerInputActions.XRIRightInteraction.CopyPaste.performed += CopyPaste_performed;
             playerInputActions.XRIRightInteraction.Spawn.performed += SpawnObject_performed;

             // playerInputActions.XRIRightInteraction.Activate.canceled += DrawLine_endDrawing;

             playerInputActions.XRILeftInteraction.Delete.performed += DeleteSelectedObjects_performed;
       */
    }
    private void OnEnable()
    {
   /*     playerInputActions.XRIRightInteraction.Enable();
        playerInputActions.XRILeftInteraction.Enable();

        //adding actionListeners
        playerInputActions.XRIRightInteraction.Select.performed += SelectObject_performed;
        playerInputActions.XRIRightInteraction.CopyPaste.performed += CopyPaste_performed;
        playerInputActions.XRIRightInteraction.Spawn.performed += SpawnObject_performed;

        // playerInputActions.XRIRightInteraction.Activate.canceled += DrawLine_endDrawing;

       playerInputActions.XRILeftInteraction.Delete.performed += DeleteSelectedObjects_performed;
   */ }
    private void OnDisable()
    {
  /*      //adding actionListeners
        playerInputActions.XRIRightInteraction.Select.performed -= SelectObject_performed;
        playerInputActions.XRIRightInteraction.CopyPaste.performed -= CopyPaste_performed;
        playerInputActions.XRIRightInteraction.Spawn.performed -= SpawnObject_performed;

        // playerInputActions.XRIRightInteraction.Activate.canceled += DrawLine_endDrawing;

        playerInputActions.XRILeftInteraction.Delete.performed -= DeleteSelectedObjects_performed;
   */ }

    private void CopyPaste_performed(InputAction.CallbackContext context)
    {
        if(selectedObjects.Count > 0)
        {
            CopyAndPasteObjects();
        }
    }

    private void SpawnObject_performed(InputAction.CallbackContext context)
    {
        SpawnObject();
    }

    private void DeleteSelectedObjects_performed(InputAction.CallbackContext context)
    {
        if (selectedObjects.Count > 0)
        {
            DeleteAllSelectedObjects();
        }
    }

    public void SpawnNWObject()
    {
        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
       
        
        NetworkObject networkPlayerObject = NetworkManager.Instance.Runner.Spawn(NWObjectPrefab, spawnPosition);

    }


    private void SelectObject_performed(InputAction.CallbackContext context)
    {

        Vector3 controllerPosition = controller.position;
        Quaternion controllerRotation = controller.rotation;
        Vector3 rayDirection = controllerRotation * Vector3.forward;

        // Raycast logic
        Ray ray = new Ray(controllerPosition, rayDirection);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {

            Debug.LogError("Hit : " + hit.transform.name +"   :" +hit.collider.transform.name + " " + hit.transform.tag);
            if (hit.transform.tag == "Player")
            {

            }

            else if (hit.collider.transform.tag == "Interactable")
            {
                if (selectedObjects.Contains(hit.collider.gameObject))
                    DeselectObject(hit.collider.gameObject);
                else
                    SelectObjects(hit.collider.gameObject);
            }
            else
            {
                DeselectAllObjects();
            }
        }
        else if (selectedObjects.Count > 0)
        {
            DeselectAllObjects();
        }
    }

    void Update()
    {
    /*    Vector3 controllerPosition = controller.position;
        Quaternion controllerRotation = controller.rotation;
        Vector3 rayDirection = controllerRotation * Vector3.forward;

        // Raycast logic
        Ray ray = new Ray(controllerPosition, rayDirection);
        RaycastHit hit;

       if (selectedObjects.Count > 0)
            {
                MoveObjects();
            }

*/

 

    }

    void SpawnObject()
    {
        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;
        spawnedObject = Instantiate(objectPrefab, spawnPosition, Quaternion.identity);
        spawnedObject.transform.rotation = objectPrefab.transform.rotation;
        spawnedObject.transform.parent = chaireResources;       

        SelectObjects(spawnedObject);
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

        Vector2 joystickInput = playerInputActions.XRIRightInteraction.Move.ReadValue<Vector2>();

        Vector3 moveDirection = new Vector3(joystickInput.x, 0, joystickInput.y) * moveSpeed * Time.deltaTime;

        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {
                
                // Constrain movement to X and Z directions
                
               
                Vector3 direcion  =  cameraTransform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.z));
                direcion.y = 0;
                obj.transform.position += direcion;
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
                GameObject copy = Instantiate(obj);
                copy.transform.position = obj.transform.position + copyPasteOffset; // Offset copied object
                copy.transform.parent = chaireResources;

                copiedObjects.Add(copy);

                Debug.Log($"Copied: {obj.name}");
            }
        }

        //deselect all currently selected objects 

        DeselectAllObjects();
        // Add copied objects to selected list
        foreach (GameObject copy in copiedObjects)
        {
            //CreateBoundingBox(copy);
            selectedObjects.Add(copy);
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

    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }
}



