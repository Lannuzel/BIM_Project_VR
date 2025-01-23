using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ReservationInteractionHandler : NetworkBehaviour
{
    public XRIBIMInputActions playerInputActions;
    public Transform cameraTransform; // Player's camera transform
    private static ReservationInteractionHandler _instance;
    public float spawnDistance = 2.0f; // Distance at which the object spawns
    public GameObject spawnedRectanglePrefab; // The prefab to spawn
    public GameObject spawnedCircularPrefab; // The prefab to spawn
    public Transform runTimeGeneratedResources;
    public List<GameObject> selectedReservations = new List<GameObject>(); // List of selected objects


    public static ReservationInteractionHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ReservationInteractionHandler>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("ReservationInteractionHandler");
                    _instance = obj.AddComponent<ReservationInteractionHandler>();
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
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void SpawnRectangleReservation()
    {
        SpawnObject(spawnedRectanglePrefab);
    }
    public void SpawnCircularReservation()
    {
        SpawnObject(spawnedCircularPrefab);
    }

    private void SpawnObject(GameObject spawnedObjectPrefab)
    {
        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;

        Fusion.NetworkObject spawnObj = null;
        NetworkManager.Instance.Runner.Spawn(spawnedObjectPrefab, Vector3.zero, spawnedObjectPrefab.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
        {
            spawnObj = obj;
        });

        spawnObj.transform.position = spawnPosition;
        spawnObj.transform.rotation = spawnedObjectPrefab.transform.rotation;
        spawnObj.transform.parent = runTimeGeneratedResources;
        selectedReservations.Add(spawnObj.gameObject);
    }
    public void DeleteAllSelectedObjects()
    {
        List<GameObject> objectsToBeDeleted = new List<GameObject>(); // List of copied objects 

        foreach (GameObject obj in selectedReservations)
        {
            if (obj != null) // Check if the GameObject is not null
            {
                Destroy(obj); // Destroy the GameObject
            }
        }
        selectedReservations.Clear();
    }
    public void DeselectAllObjects()
    {
        selectedReservations.Clear();
    }
    public void DeselectObject(GameObject selectedObject)
    {
        selectedReservations.Remove(selectedObject);
    }
}
