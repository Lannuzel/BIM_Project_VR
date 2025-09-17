using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectInteractionHandler : NetworkBehaviour
{
    public XRIBIMInputActions playerInputActions;
    public Transform cameraTransform; // Player's camera transform
    private static ObjectInteractionHandler _instance;
    public float spawnDistance = 2.0f; // Distance at which the object spawns
    public GameObject spawnedObjectPrefab; // The prefab to spawn
    private GameObject spawnedObject;

    public List<GameObject> selectedObjects = new List<GameObject>(); // List of selected objects
    public Material boundingBoxMaterial; // Optional material for visualizing the bounding box

    public ObjectOutline objectOutliner;
    public Transform runTimeGeneratedResources;
    public Transform instantiatedChaires;



    public static ObjectInteractionHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ObjectInteractionHandler>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("ObjectInteractionHandler");
                    _instance = obj.AddComponent<ObjectInteractionHandler>();
                }
            }
            return _instance;
        }
    }


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Ensure only one instance exists
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject); // Optional: keep the instance across scenes
        playerInputActions = new XRIBIMInputActions();
    }
    public List<GameObject> SelectedObjects()
    {
        return selectedObjects;
    }


    public void SpawnObject()
    {
        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;
        /*  spawnedObject = Instantiate(spawnedObjectPrefab, spawnPosition, Quaternion.identity);
          spawnedObject.transform.rotation = spawnedObjectPrefab.transform.rotation;
          spawnedObject.transform.parent = runTimeGeneratedResources;
        */
        Fusion.NetworkObject spawnObj = null;
        NetworkManager.Instance.Runner.Spawn(spawnedObjectPrefab, Vector3.zero, spawnedObjectPrefab.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
        {
            spawnObj = obj;
        });


        /*Fusion.NetworkObject spawnObj =  NetworkManager.Instance.Runner.Spawn(spawnedObjectPrefab);
        spawnObj.AssignInputAuthority(NetworkManager.Instance.Runner.LocalPlayer);
       */
        spawnObj.transform.position = spawnPosition;
        spawnObj.transform.rotation = spawnedObjectPrefab.transform.rotation;
        spawnObj.transform.parent = instantiatedChaires;
        SelectObjects(spawnObj.gameObject);
        spawnedObject = null;
    }
    public void SpawnObjectTutorial()
    {
        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;
          spawnedObject = Instantiate(spawnedObjectPrefab, spawnPosition, Quaternion.identity);
          spawnedObject.transform.rotation = spawnedObjectPrefab.transform.rotation;
          spawnedObject.transform.parent = runTimeGeneratedResources;
        SelectObjects(spawnedObject.gameObject);
        spawnedObject = null;
    }
    public void SelectObjects(GameObject hitObject)
    {
        if (!selectedObjects.Contains(hitObject))
        {
            objectOutliner.EnableOutLine(hitObject.transform);
            //HighlightObject(hitObject);
            CreateBoundingBox(hitObject);
            selectedObjects.Add(hitObject);
        }

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
        boxCollider.size = bounds.size* 0.8f;
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


    public void DeleteAllSelectedObjects()
    {
        List<GameObject> objectsToBeDeleted = new List<GameObject>(); // List of copied objects
        RemoveBoundingBoxes();
        RemoveOutLines();
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
    private void RemoveOutLines()
    {

        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {
                objectOutliner.DisableOutLine(obj.transform);   
            }
        }
        //clear list
        // boundingBoxes.Clear();
    }

    public void DeleteReferenceLines()
    {
        foreach (GameObject obj in selectedObjects)
        {
            DeleteReferenceLine(obj);
        }

    }
    public void DeleteReferenceLine(GameObject obj)
    {
             Transform directionLineZ = obj.transform.Find("DirectionLineZ");
            if (directionLineZ != null)
                Destroy(directionLineZ.gameObject);

            Transform directionLineX = obj.transform.Find("DirectionLineX");
            if (directionLineX != null)
                Destroy(directionLineX.gameObject);
        

    }
    public void DeselectAllObjects()
    {
        RemoveBoundingBoxes();
        DeleteReferenceLines();
        selectedObjects.Clear();

    }
   public void DeselectObject(GameObject selectedObject)
    {
        // RemoveHighlight(selectedObject);
        DeleteReferenceLine(selectedObject);
        RemoveBoundingBox(selectedObject);
        selectedObjects.Remove(selectedObject);
    }
    void RemoveBoundingBox(GameObject obj)
    {
        Transform bb = obj.transform.Find("BoundingBox");
        if (bb != null)
        {
            Destroy(bb.gameObject);
        }
    }

    public Transform FindChildWithTagRecursive(Transform parent, string tag)
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
