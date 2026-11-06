using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Création, sélection et suppression des objets du Modélisateur (XP1, XP3).
/// N5 : suppression réseau (Despawn) au lieu de Destroy, prise d'autorité à la sélection d'un objet
/// créé par un autre casque ; F19 : création et suppression annulables.
/// XP3 : création d'un prefab au choix (luminaire, bouche d'air, ventilateur) posé au plafond,
/// mise à l'échelle par rapport à une dalle de référence, taux de brassage.
/// </summary>
public class ObjectInteractionHandler : NetworkBehaviour
{
    public XRIBIMInputActions playerInputActions;
    public Transform cameraTransform; // Player's camera transform
    private static ObjectInteractionHandler _instance;
    public float spawnDistance = 2.0f; // Distance at which the object spawns
    public GameObject spawnedObjectPrefab; // The prefab to spawn
    private GameObject spawnedObject;

    [Header("XP3 (objets au plafond)")]
    public GameObject spawnedLumierPrefab;
    public GameObject spawnedAirTerminalPrefab;
    public GameObject spawnedVentilatorPrefab;
    [Tooltip("Hauteur de pose (m) des objets qui ne sont pas dans la table ci-dessous : 0 = au sol (XP1).")]
    public float spawnHeight = 0.0f;

    [System.Serializable]
    public class SpawnHeightRule
    {
        [Tooltip("Sous-chaîne du nom du prefab ou de l'objet.")]
        public string nameContains;
        [Tooltip("Hauteur de pose (m).")]
        public float height;
    }
    [Tooltip("XP3 : hauteur de pose selon le nom de l'objet (plafond). Valeurs reprises de la branche EXP3.")]
    public List<SpawnHeightRule> spawnHeightRules = new List<SpawnHeightRule>
    {
        new SpawnHeightRule { nameContains = "Ventilator", height = 2.112f },
        new SpawnHeightRule { nameContains = "Lumier", height = 2.11f },
        new SpawnHeightRule { nameContains = "AirTerminal", height = 2.11f },
    };

    [Tooltip("XP3 : dalle servant de référence à la mise à l'échelle en pourcentage.")]
    public Transform dalleReference;
    public TMP_InputField pourcentageRepereInput;
    public TMP_InputField tauxDeBrassageInput;

    /// <summary>Hauteur de pose (m) d'un objet : règle par nom (XP3), sinon <see cref="spawnHeight"/>.</summary>
    public float HeightFor(GameObject obj)
    {
        if (obj != null)
        {
            string n = obj.name;
            foreach (var rule in spawnHeightRules)
            {
                if (!string.IsNullOrEmpty(rule.nameContains) && n.Contains(rule.nameContains))
                    return rule.height;
            }
        }
        return spawnHeight;
    }

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
        SpawnObject(spawnedObjectPrefab);
    }

    /// <summary>
    /// Crée un prefab devant le joueur (XP1 : chaise au sol ; XP3 : luminaire, bouche d'air ou ventilateur au plafond,
    /// hauteur selon <see cref="HeightFor"/>). Action annulable.
    /// </summary>
    public void SpawnObject(GameObject prefabToSpawn)
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError("[ObjectInteractionHandler] Aucun prefab à créer.");
            return;
        }
        if (!NetworkObjectUtils.IsRunning)
        {
            NetworkStatusPanel.Show("Session réseau inactive : création impossible.", 3f);
            return;
        }

        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = HeightFor(prefabToSpawn);

        Fusion.NetworkObject spawnObj = NetworkObjectUtils.Runner.Spawn(prefabToSpawn, spawnPosition, prefabToSpawn.transform.rotation, NetworkObjectUtils.Runner.LocalPlayer);
        if (spawnObj == null)
        {
            Debug.LogError("[ObjectInteractionHandler] Échec de création de l'objet.");
            return;
        }

        spawnObj.transform.position = spawnPosition;
        spawnObj.transform.rotation = prefabToSpawn.transform.rotation;
        spawnObj.transform.parent = instantiatedChaires;
        SelectObjects(spawnObj.gameObject);
        spawnedObject = null;

        UndoManager.RecordSpawn("Créer un objet", spawnObj.gameObject);
    }

    /// <summary>XP3 : met les objets sélectionnés à un pourcentage de la taille de la dalle de référence.</summary>
    public void ScaleSelectedObjectsWithDalleReference()
    {
        if (pourcentageRepereInput == null || dalleReference == null) return;
        if (!float.TryParse(pourcentageRepereInput.text.Replace(',', '.'), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float percent))
            return;

        Renderer rendA = dalleReference.GetComponentInChildren<Renderer>();
        if (rendA == null)
        {
            Debug.LogError("[ObjectInteractionHandler] La dalle de référence n'a pas de Renderer.");
            return;
        }
        float sizeA = rendA.bounds.size.magnitude;

        // Non annulable : la pile d'annulation ne mémorise pas l'échelle (position / rotation seulement).
        selectedObjects.RemoveAll(o => o == null);
        foreach (GameObject obj in selectedObjects)
        {
            NetworkObject networkObj = obj.GetComponent<NetworkObject>();
            if (networkObj == null || !networkObj.HasStateAuthority) continue;
            Renderer rendB = obj.GetComponentInChildren<Renderer>();
            ScaleNetworked scaleNetworked = obj.GetComponent<ScaleNetworked>();
            if (rendB == null || scaleNetworked == null) continue;

            float sizeB = rendB.bounds.size.magnitude;
            if (sizeB <= 1e-5f) continue;
            float scaleFactor = (sizeA * percent / 100f) / sizeB;
            scaleNetworked.ScaleFactor *= scaleFactor; // facteur pur (N5), appliqué par Render()
            scaleNetworked.ApplyScale();
        }
    }

    /// <summary>XP3 : renseigne le taux de brassage des objets sélectionnés (propriété métier, non réseau).</summary>
    public void setTauxDeBrassage()
    {
        if (tauxDeBrassageInput == null) return;
        if (!float.TryParse(tauxDeBrassageInput.text.Replace(',', '.'), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float taux))
            return;
        foreach (GameObject obj in selectedObjects)
        {
            if (obj == null) continue;
            ResourceProperties resourceProps = obj.GetComponent<ResourceProperties>();
            if (resourceProps != null)
                resourceProps.tauxDeBrassage = taux;
        }
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
