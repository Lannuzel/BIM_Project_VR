using Fusion;

using System.Collections.Generic;
using TMPro;

using UnityEngine;



public class ReservationInteractionHandler : NetworkBehaviour
{
    public XRIBIMInputActions playerInputActions;
    public Transform cameraTransform; // Player's camera transform
    public Transform rightController;
    private static ReservationInteractionHandler _instance;
    public float spawnDistance = 2.0f; // Distance at which the object spawns
    public GameObject spawnedRectangleSolPrefab; // The prefab to spawn
    public GameObject spawnedRectangleMurPrefab; // The prefab to spawn

    public GameObject spawnedCircularSolPrefab; // The prefab to spawn

    public GameObject spawnedCircularMurPrefab; // The prefab to spawn

    public Transform runTimeGeneratedResources;
    public List<GameObject> selectedReservations = new List<GameObject>(); // List of selected objects

    public TMP_InputField rectangleLengthInput;
    public TMP_InputField rectangleWidthInput;
    public TMP_InputField rectangleHeightInput;


    public TMP_InputField rectangleLengthInputMur;
    public TMP_InputField rectangleWidthInputMur;
    public TMP_InputField rectangleHeightInputMur;


    public TMP_InputField circleDiameterInputSol;
    public TMP_InputField circleHeightInputSol;

    public TMP_InputField circleDiameterInputMur;
    public TMP_InputField circleHeightInputMur;

    public GameObject currentReservation;

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
        SpawnObject(spawnedRectangleSolPrefab);
    }
    public void SpawnRectangleReservationSol()
    {
        SpawnObject(spawnedRectangleSolPrefab);
    }
    public void SpawnRectangleReservationMur()
    {
        SpawnObject(spawnedRectangleMurPrefab);
    }

    public void SpawnCustomRectangleReservationSol()
    {
        if (float.TryParse(rectangleLengthInput.text, out float length) && float.TryParse(rectangleWidthInput.text, out float width) && float.TryParse(rectangleHeightInput.text, out float height))
        {
            Debug.Log($"Reservation received: Width = {width}, Height = {height}");
            // You can now use width and height for your rectangle logic
            SpawnRectangelObject(spawnedRectangleSolPrefab, width, length, height);
        }
        else
        {
            Debug.LogError("Invalid input. Please enter numeric width and height.");
        }

    }
    public void SpawnCustomRectangleReservationMur()
    {
        if (float.TryParse(rectangleLengthInputMur.text, out float length) && float.TryParse(rectangleWidthInputMur.text, out float width) && float.TryParse(rectangleHeightInputMur.text, out float height))
        {
            Debug.Log($"Reservation received: Width = {width}, Height = {height}");
            // You can now use width and height for your rectangle logic
            SpawnRectangelObject(spawnedRectangleMurPrefab, width, length, height);
        }
        else
        {
            Debug.LogError("Invalid input. Please enter numeric width and height.");
        }

    }

    public void SpawnCustomRectangleReservation()
    {
        if (float.TryParse(rectangleLengthInput.text, out float length) && float.TryParse(rectangleWidthInput.text, out float width) && float.TryParse(rectangleHeightInput.text, out float height))
        {
            Debug.Log($"Reservation received: Width = {width}, Height = {height}");
            // You can now use width and height for your rectangle logic
            SpawnRectangelObject(spawnedRectangleSolPrefab, width, height, length);
        }
        else
        {
            Debug.LogWarning("Invalid input. Please enter numeric width and height.");
        }

    }
    public void SpawnCircularReservation()
    {
        SpawnObject(spawnedCircularSolPrefab);
    }
    public void SpawnCircularReservationSol()
    {
        SpawnObject(spawnedCircularSolPrefab);
    }
    public void SpawnCircularReservationMur()
    {
        SpawnObject(spawnedCircularMurPrefab);
    }


    public void SpawnCustomCircularReservationSol()
    {
        if (float.TryParse(circleDiameterInputSol.text, out float diameter) && float.TryParse(circleHeightInputSol.text, out float height))
        {
            Debug.Log($"Reservation received: Width = {diameter}, Height = {height}");
            // You can now use width and height for your rectangle logic
            SpawnCircleObject(spawnedCircularSolPrefab, diameter, height, diameter);
        }
        else
        {
            Debug.LogWarning("Invalid input. Please enter numeric width and height.");
        }


    }

    public void SpawnCustomCircularReservationMur()
    {
        if (float.TryParse(circleDiameterInputMur.text, out float diameter) && float.TryParse(circleHeightInputMur.text, out float width))
        {
            Debug.Log($"Reservation received: Length= Height = {diameter}, width = {width}");
            // You can now use width and height for your rectangle logic
            SpawnCircleObject(spawnedCircularMurPrefab, diameter, width, diameter);
        }
        else
        {
            Debug.LogWarning("Invalid input. Please enter numeric width and height.");
        }


    }

    private static Quaternion YawLookAtUser(Transform cam, Vector3 fromPos)
    {
        Vector3 dir = cam.position - fromPos;
        dir.y = 0f;                         // <-- Y-axis only (no pitch/roll)
        if (dir.sqrMagnitude < 1e-6f) dir = -cam.forward;
        return Quaternion.LookRotation(dir.normalized, Vector3.up);
    }



    private void SpawnRectangelObject(GameObject spawnedObjectPrefab, float length, float width, float height)
    {
        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;


        Vector3 forward = cameraTransform.forward;
        Fusion.NetworkObject spawnObj = null;

        Quaternion faceUserRot = YawLookAtUser(cameraTransform, spawnPosition);

        NetworkManager.Instance.Runner.Spawn(spawnedObjectPrefab, spawnPosition, Quaternion.identity, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
        {
            spawnObj = obj;
            var scaler = obj.GetComponent<ScaleNetworked>();
            if (scaler != null)
            {
                scaler.ScaleFactor = new Vector3(spawnObj.transform.localScale.x * width, spawnObj.transform.localScale.y * height, spawnObj.transform.localScale.z * length);

            }
        });
        Debug.LogError("spawned obj :" + spawnObj.transform.localScale);
       // spawnObj.transform.localScale = new Vector3(spawnObj.transform.localScale.x * width, spawnObj.transform.localScale.y * height, spawnObj.transform.localScale.z * length);

        spawnObj.transform.position = spawnPosition;
        /*   Vector3 targetPos = cameraTransform.position;
           targetPos.y = transform.position.y; // lock Y-axis so no tilt up/down
           spawnObj.transform.LookAt(targetPos);


           // spawnObj.transform.rotation = spawnedObjectPrefab.transform.rotation;

           Vector3 toUser = (cameraTransform.position - spawnObj.transform.position);

           if (toUser.sqrMagnitude < 1e-4f) toUser = -forward;
           Quaternion rotation = Quaternion.LookRotation(toUser.normalized, Vector3.up);

          // spawnObj.transform.rotation = rotation;
        */
        spawnObj.transform.parent = runTimeGeneratedResources;
        RaycastSelectAndMove raycastSelectAndMove = spawnObj.GetComponentInChildren<RaycastSelectAndMove>();
        if (raycastSelectAndMove != null)
        {
            raycastSelectAndMove.cameraTransform = cameraTransform;
            raycastSelectAndMove.rightController = rightController;
        }

        currentReservation = spawnObj.gameObject;
        if (currentReservation != null)
        {
            NetworkObject networkObj = currentReservation.GetComponent<NetworkObject>();
            if (networkObj != null && networkObj.HasStateAuthority)
            {
                currentReservation.transform.GetComponent<TranslateGizmoDrawer>().enabled = false;
                currentReservation.transform.GetComponent<RotationGizmoDrawer>().enabled = false;
              
            }
        }

        selectedReservations.Add(spawnObj.gameObject);
    }
    private void SpawnObject(GameObject spawnedObjectPrefab)
    {
        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;

        Fusion.NetworkObject spawnObj = null;
        NetworkManager.Instance.Runner.Spawn(spawnedObjectPrefab, spawnPosition, spawnedObjectPrefab.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
        {
            spawnObj = obj;
        });



        spawnObj.transform.position = spawnPosition;
        spawnObj.transform.rotation = spawnedObjectPrefab.transform.rotation;
        spawnObj.transform.parent = runTimeGeneratedResources;
        RaycastSelectAndMove raycastSelectAndMove = spawnObj.GetComponent<RaycastSelectAndMove>();
        if (raycastSelectAndMove != null)
        {
            raycastSelectAndMove.cameraTransform = cameraTransform;
            raycastSelectAndMove.rightController = rightController;
        }

        currentReservation = spawnObj.gameObject;
        selectedReservations.Add(spawnObj.gameObject);
    }

    private void SpawnCircleObject(GameObject spawnedObjectPrefab, float diameterX, float height, float diameterZ)
    {
        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;

        Fusion.NetworkObject spawnObj = null;
        NetworkManager.Instance.Runner.Spawn(spawnedObjectPrefab, spawnPosition, spawnedObjectPrefab.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
        {
            spawnObj = obj;
            var scaler = obj.GetComponent<ScaleNetworked>();
            if (scaler != null)
            {
                scaler.ScaleFactor = new Vector3(spawnObj.transform.localScale.x * diameterX, spawnObj.transform.localScale.y * height, spawnObj.transform.localScale.z * diameterZ);

            }
        });
        NetworkObject networkObj = spawnObj.GetComponent<NetworkObject>();
        if (networkObj != null && networkObj.HasStateAuthority)
        {

         //   spawnObj.transform.localScale = new Vector3(spawnObj.transform.localScale.x * diameterX, spawnObj.transform.localScale.y * height, spawnObj.transform.localScale.z * diameterZ);

            spawnObj.transform.position = spawnPosition;
            spawnObj.transform.rotation = spawnedObjectPrefab.transform.rotation;
            spawnObj.transform.parent = runTimeGeneratedResources;
        }
        RaycastSelectAndMove raycastSelectAndMove = spawnObj.GetComponent<RaycastSelectAndMove>();
        if (raycastSelectAndMove != null)
        {
            raycastSelectAndMove.cameraTransform = cameraTransform;
            raycastSelectAndMove.rightController = rightController;
        }

        currentReservation = spawnObj.gameObject;
        if (currentReservation != null)
        {
            NetworkObject networkObj1 = currentReservation.GetComponent<NetworkObject>();
            if (networkObj1 != null && networkObj1.HasStateAuthority)
            {
                currentReservation.transform.GetComponent<TranslateGizmoDrawer>().enabled = false;
                currentReservation.transform.GetComponent<RotationGizmoDrawer>().enabled = false;
            }
        }
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
        currentReservation = null;
        selectedReservations.Clear();
    }
    public void DeleteLastSelectedReservation()
    {
        if (selectedReservations.Count > 0)
        {
            if (currentReservation == null)
            {
                currentReservation = selectedReservations[selectedReservations.Count - 1];
            }
            selectedReservations.Remove(currentReservation);
            Destroy(currentReservation);


        }
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
