using Fusion;

using System.Collections.Generic;
using TMPro;

using UnityEngine;


/// <summary>
/// Création et suppression des réservations (XP2).
/// N5 : facteurs d'échelle purs (voir ScaleNetworked), ordre longueur/largeur respecté, suppression réseau
/// (Despawn) ; F19 : création et suppression annulables.
/// </summary>
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

    private static bool TryParse(TMP_InputField field, out float value)
    {
        value = 0f;
        if (field == null) return false;
        string text = (field.text ?? string.Empty).Trim().Replace(',', '.');
        return float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value) && value > 0f;
    }

    public void SpawnCustomRectangleReservationSol()
    {
        if (TryParse(rectangleLengthInput, out float length) && TryParse(rectangleWidthInput, out float width) && TryParse(rectangleHeightInput, out float height))
        {
            Debug.Log($"Reservation received: Length = {length}, Width = {width}, Height = {height}");
            SpawnRectangelObject(spawnedRectangleSolPrefab, length, width, height);
        }
        else
        {
            Debug.LogError("Invalid input. Please enter numeric length, width and height.");
            NetworkStatusPanel.Show("Dimensions invalides : entrez des nombres positifs.", 3f);
        }

    }
    public void SpawnCustomRectangleReservationMur()
    {
        if (TryParse(rectangleLengthInputMur, out float length) && TryParse(rectangleWidthInputMur, out float width) && TryParse(rectangleHeightInputMur, out float height))
        {
            Debug.Log($"Reservation received: Length = {length}, Width = {width}, Height = {height}");
            SpawnRectangelObject(spawnedRectangleMurPrefab, length, width, height);
        }
        else
        {
            Debug.LogError("Invalid input. Please enter numeric length, width and height.");
            NetworkStatusPanel.Show("Dimensions invalides : entrez des nombres positifs.", 3f);
        }

    }

    public void SpawnCustomRectangleReservation()
    {
        SpawnCustomRectangleReservationSol();
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
        if (TryParse(circleDiameterInputSol, out float diameter) && TryParse(circleHeightInputSol, out float height))
        {
            Debug.Log($"Reservation received: Diameter = {diameter}, Height = {height}");
            SpawnCircleObject(spawnedCircularSolPrefab, diameter, height, diameter);
        }
        else
        {
            Debug.LogWarning("Invalid input. Please enter numeric diameter and height.");
            NetworkStatusPanel.Show("Dimensions invalides : entrez des nombres positifs.", 3f);
        }

    }

    public void SpawnCustomCircularReservationMur()
    {
        if (TryParse(circleDiameterInputMur, out float diameter) && TryParse(circleHeightInputMur, out float width))
        {
            Debug.Log($"Reservation received: Diameter = {diameter}, width = {width}");
            SpawnCircleObject(spawnedCircularMurPrefab, diameter, width, diameter);
        }
        else
        {
            Debug.LogWarning("Invalid input. Please enter numeric diameter and height.");
            NetworkStatusPanel.Show("Dimensions invalides : entrez des nombres positifs.", 3f);
        }

    }

    private bool CheckSession()
    {
        if (NetworkObjectUtils.IsRunning) return true;
        NetworkStatusPanel.Show("Session réseau inactive : création impossible.", 3f);
        return false;
    }

    /// <summary>Longueur sur X, largeur sur Z, hauteur sur Y (facteurs appliqués à l'échelle du prefab).</summary>
    private void SpawnRectangelObject(GameObject spawnedObjectPrefab, float length, float width, float height)
    {
        if (!CheckSession()) return;

        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;

        Fusion.NetworkObject spawnObj = NetworkObjectUtils.Runner.Spawn(spawnedObjectPrefab, spawnPosition, Quaternion.identity, NetworkObjectUtils.Runner.LocalPlayer, (runner, obj) =>
        {
            var scaler = obj.GetComponent<ScaleNetworked>();
            if (scaler != null)
                scaler.ScaleFactor = new Vector3(length, height, width); // N5 : facteur pur, ScaleNetworked multiplie par l'échelle du prefab
        });
        if (spawnObj == null) return;

        spawnObj.transform.position = spawnPosition;
        spawnObj.transform.parent = runTimeGeneratedResources;
        AfterSpawn(spawnObj);
    }
    private void SpawnObject(GameObject spawnedObjectPrefab)
    {
        if (!CheckSession()) return;

        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;

        Fusion.NetworkObject spawnObj = NetworkObjectUtils.Runner.Spawn(spawnedObjectPrefab, spawnPosition, spawnedObjectPrefab.transform.rotation, NetworkObjectUtils.Runner.LocalPlayer);
        if (spawnObj == null) return;

        spawnObj.transform.position = spawnPosition;
        spawnObj.transform.rotation = spawnedObjectPrefab.transform.rotation;
        spawnObj.transform.parent = runTimeGeneratedResources;
        AfterSpawn(spawnObj);
    }

    private void SpawnCircleObject(GameObject spawnedObjectPrefab, float diameterX, float height, float diameterZ)
    {
        if (!CheckSession()) return;

        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;

        Fusion.NetworkObject spawnObj = NetworkObjectUtils.Runner.Spawn(spawnedObjectPrefab, spawnPosition, spawnedObjectPrefab.transform.rotation, NetworkObjectUtils.Runner.LocalPlayer, (runner, obj) =>
        {
            var scaler = obj.GetComponent<ScaleNetworked>();
            if (scaler != null)
                scaler.ScaleFactor = new Vector3(diameterX, height, diameterZ); // N5 : facteur pur
        });
        if (spawnObj == null) return;

        spawnObj.transform.position = spawnPosition;
        spawnObj.transform.rotation = spawnedObjectPrefab.transform.rotation;
        spawnObj.transform.parent = runTimeGeneratedResources;
        AfterSpawn(spawnObj);
    }

    private void AfterSpawn(NetworkObject spawnObj)
    {
        currentReservation = spawnObj.gameObject;
        var translate = currentReservation.GetComponent<TranslateGizmoDrawer>();
        if (translate != null) translate.enabled = false;
        var rotate = currentReservation.GetComponentInChildren<RotationGizmoDrawer>();
        if (rotate != null) rotate.enabled = false;

        selectedReservations.Add(spawnObj.gameObject);
        UndoManager.RecordSpawn("Créer une réservation", spawnObj.gameObject);
    }

    /// <summary>Supprime toutes les réservations sélectionnées pour tous les participants (annulable).</summary>
    public void DeleteAllSelectedObjects()
    {
        selectedReservations.RemoveAll(o => o == null);
        if (selectedReservations.Count == 0) return;

        var toDelete = new List<GameObject>(selectedReservations);
        var undo = UndoManager.CaptureForDelete(toDelete.Count > 1 ? $"Supprimer {toDelete.Count} réservations" : "Supprimer une réservation", toDelete);
        foreach (GameObject obj in toDelete)
            NetworkObjectUtils.DespawnHierarchy(obj);
        currentReservation = null;
        selectedReservations.Clear();
        if (undo != null) UndoManager.Push(undo);
    }

    /// <summary>Supprime la dernière réservation sélectionnée (ou créée) pour tous les participants (annulable).</summary>
    public void DeleteLastSelectedReservation()
    {
        selectedReservations.RemoveAll(o => o == null);
        if (currentReservation == null && selectedReservations.Count > 0)
            currentReservation = selectedReservations[selectedReservations.Count - 1];
        if (currentReservation == null) return;

        var target = currentReservation;
        selectedReservations.Remove(target);
        currentReservation = null;

        var undo = UndoManager.CaptureForDelete("Supprimer la dernière réservation", new[] { target });
        NetworkObjectUtils.DespawnHierarchy(target);
        if (undo != null) UndoManager.Push(undo);
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
