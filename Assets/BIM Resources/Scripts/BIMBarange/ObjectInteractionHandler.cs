using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Création, sélection et suppression des objets du Modélisateur (XP1).
/// N5 : suppression réseau (Despawn) au lieu de Destroy, prise d'autorité à la sélection d'un objet
/// créé par un autre casque ; F19 : création et suppression annulables.
/// </summary>
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
        if (!NetworkObjectUtils.IsRunning)
        {
            NetworkStatusPanel.Show("Session réseau inactive : création impossible.", 3f);
            return;
        }

        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;

        Fusion.NetworkObject spawnObj = NetworkObjectUtils.Runner.Spawn(spawnedObjectPrefab, spawnPosition, spawnedObjectPrefab.transform.rotation, NetworkObjectUtils.Runner.LocalPlayer);
        if (spawnObj == null)
        {
            Debug.LogError("[ObjectInteractionHandler] Échec de création de l'objet.");
            return;
        }

        spawnObj.transform.position = spawnPosition;
        spawnObj.transform.rotation = spawnedObjectPrefab.transform.rotation;
        spawnObj.transform.parent = instantiatedChaires;
        SelectObjects(spawnObj.gameObject);
        spawnedObject = null;

        UndoManager.RecordSpawn("Créer un objet", spawnObj.gameObject);
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
        if (hitObject == null) return;
        if (!selectedObjects.Contains(hitObject))
        {
            objectOutliner.EnableOutLine(hitObject.transform);
            //HighlightObject(hitObject);
            CreateBoundingBox(hitObject);
            selectedObjects.Add(hitObject);

            // N5 : un objet créé par un autre casque devient manipulable (prise d'autorité d'état)
            NetworkObjectUtils.EnsureAuthority(hitObject, ok =>
            {
                if (!ok) NetworkStatusPanel.Show($"Impossible de prendre la main sur {hitObject.name}.", 3f);
            });
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


    /// <summary>Supprime les objets sélectionnés pour tous les participants (Despawn réseau), action annulable.</summary>
    public void DeleteAllSelectedObjects()
    {
        selectedObjects.RemoveAll(o => o == null);
        if (selectedObjects.Count == 0) return;

        var toDelete = new List<GameObject>(selectedObjects);
        RemoveBoundingBoxes();
        RemoveOutLines();
        DeleteReferenceLines();
        selectedObjects.Clear();

        var undo = UndoManager.CaptureForDelete(toDelete.Count > 1 ? $"Supprimer {toDelete.Count} objets" : "Supprimer un objet", toDelete);
        foreach (GameObject obj in toDelete)
        {
            if (obj != null)
                NetworkObjectUtils.DespawnHierarchy(obj);
        }
        if (undo != null)
            UndoManager.Push(undo);
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
        if (obj == null) return;
        // N5 : les lignes de référence sont nommées DirectionLine<axe><instance> ; suppression réseau.
        var toRemove = new List<GameObject>();
        foreach (Transform child in obj.transform)
        {
            if (child.name.StartsWith("DirectionLine"))
                toRemove.Add(child.gameObject);
        }
        foreach (var line in toRemove)
            NetworkObjectUtils.DespawnHierarchy(line);
    }
    public void DeselectAllObjects()
    {
        RemoveBoundingBoxes();
        RemoveOutLines();
        DeleteReferenceLines();
        selectedObjects.Clear();

    }
   public void DeselectObject(GameObject selectedObject)
    {
        if (selectedObject == null) return;
        if (objectOutliner != null) objectOutliner.DisableOutLine(selectedObject.transform);
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
