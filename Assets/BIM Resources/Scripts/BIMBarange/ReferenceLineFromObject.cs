using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Fusion;

/// <summary>
/// Lignes de référence « distance au mur / aux autres objets » depuis les objets sélectionnés.
/// N5 : recherche dans les deux sens de chaque axe (+X, -X, +Z, -Z) à partir du bord de l'objet,
/// la ligne part de la face du collider (distance bord à obstacle), chaque instance (ToWall, ToChair)
/// a ses propres lignes (noms suffixés), l'étiquette est téléportée sur sa propre NetworkTransform.
/// </summary>
public class ReferenceLineFromObject : NetworkBehaviour
{
    public Material lineMaterial; // Material for the LineRenderer

    public Transform cameraRef;

    private List<GameObject> selectedObjects;// = new List<GameObject>(); // List of selected objects

    public LayerMask layerMask;
    private bool showRefLines = false;

    [Tooltip("Décalage (m) du point de départ par rapport au pivot (ancien comportement). Ignoré si useColliderBounds est vrai.")]
    public Vector3 posOffset = Vector3.zero;
    [Tooltip("Partir du bord du collider de l'objet (distance bord à obstacle) plutôt que du pivot.")]
    public bool useColliderBounds = true;
    [Tooltip("Portée (m) de la recherche d'obstacle.")]
    public float maxDistance = 20f;

    public NetworkedLine linePrefab; // Assign this in the Inspector
    public NetworkedMUI measureUI;

    public float networkedDistanceX { get; set; }
    public float networkedDistanceZ { get; set; }

    private string suffix;

    private void Awake()
    {
        suffix = "_" + gameObject.name;
    }

    private void Start()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
    }

    private string LineName(string axis) => "DirectionLine" + axis + suffix;

    public void Update()
    {
        if (!showRefLines || selectedObjects == null) return;

        foreach (GameObject obj in selectedObjects)
        {
            if (obj == null) continue;
            if (obj.transform.Find(LineName("Z")) == null || obj.transform.Find(LineName("X")) == null)
                DrawPositionIndicatorV0(obj);
            else
                UpdatePositionIndicator(obj);
        }
    }

    public void ToggleShowReferenceLines()
    {
        showRefLines = !showRefLines;
        if (!showRefLines)
            DeleteReferenceLines();
    }

    /// <summary>Point de départ et étendue de l'objet le long d'un axe (bords du collider ou pivot).</summary>
    private void GetExtent(GameObject go, Vector3 axis, out Vector3 center, out float halfExtent)
    {
        center = go.transform.position + posOffset;
        halfExtent = 0f;
        if (!useColliderBounds) return;

        Bounds? b = null;
        foreach (var c in go.GetComponentsInChildren<Collider>())
        {
            if (c.name == "BoundingBox" || c.isTrigger) continue;
            b = b.HasValue ? Encapsulate(b.Value, c.bounds) : c.bounds;
        }
        if (!b.HasValue) return;
        center = b.Value.center;
        halfExtent = Mathf.Abs(Vector3.Dot(b.Value.extents, axis));
    }

    private static Bounds Encapsulate(Bounds a, Bounds b) { a.Encapsulate(b); return a; }

    /// <summary>Obstacle le plus proche le long de ±axis ; retourne false si rien dans la portée.</summary>
    private bool FindNearest(GameObject go, Vector3 axis, out Vector3 from, out Vector3 to)
    {
        GetExtent(go, axis, out Vector3 center, out float half);
        from = to = center;
        float best = float.MaxValue;
        bool found = false;

        foreach (float sign in new[] { 1f, -1f })
        {
            Vector3 dir = axis * sign;
            Vector3 origin = center + dir * (half + 0.005f);
            var hits = Physics.RaycastAll(origin, dir, maxDistance, layerMask);
            foreach (var h in hits)
            {
                if (h.collider.transform == go.transform || h.collider.transform.IsChildOf(go.transform)) continue;
                if (h.distance < best)
                {
                    best = h.distance;
                    from = origin;
                    to = h.point;
                    found = true;
                }
            }
        }
        return found;
    }

    public void UpdatePositionIndicator(GameObject go)
    {
        UpdateAxis(go, LineName("Z"), Vector3.forward, true);
        UpdateAxis(go, LineName("X"), Vector3.right, false);
    }

    private void UpdateAxis(GameObject go, string lineName, Vector3 axis, bool isZ)
    {
        Transform lineT = go.transform.Find(lineName);
        if (lineT == null) return;

        var networkLine = lineT.GetComponent<NetworkObject>();
        var spawnedLine = lineT.GetComponent<NetworkedLine>();
        Transform uiT = ObjectInteractionHandler.Instance.FindChildWithTagRecursive(lineT, "MeasureUI");
        var spawnedUI = uiT != null ? uiT.GetComponent<NetworkedMUI>() : null;

        if (FindNearest(go, axis, out Vector3 from, out Vector3 to))
        {
            lineT.gameObject.SetActive(true);
            float distance = Vector3.Distance(from, to);
            if (isZ) networkedDistanceZ = distance; else networkedDistanceX = distance;

            if (networkLine != null && networkLine.HasStateAuthority && spawnedLine != null)
            {
                spawnedLine.SetLinePositions(from, to, true);
                if (spawnedUI != null && spawnedUI.Object != null && spawnedUI.Object.HasStateAuthority)
                {
                    Vector3 mid = (from + to) / 2f;
                    var uiTransform = uiT.GetComponent<NetworkTransform>();
                    if (uiTransform != null) uiTransform.Teleport(mid);
                    spawnedUI.SetUIPositions(mid, distance.ToString("F2") + " m", true);
                }
            }
        }
        else
        {
            if (networkLine != null && networkLine.HasStateAuthority && spawnedLine != null)
            {
                spawnedLine.SetLinePositions(from, from, false);
                if (spawnedUI != null && spawnedUI.Object != null && spawnedUI.Object.HasStateAuthority)
                    spawnedUI.SetUIPositions(from, "", false);
            }
        }
    }

    public void DeleteReferenceLines()
    {
        showRefLines = false;
        if (selectedObjects == null) return;

        foreach (GameObject obj in selectedObjects)
        {
            if (obj == null) continue;
            foreach (var axis in new[] { "Z", "X" })
            {
                Transform t = obj.transform.Find(LineName(axis));
                if (t != null)
                    NetworkObjectUtils.DespawnHierarchy(t.gameObject);
            }
        }
    }

    public void DrawPositionIndicatorV0(GameObject go)
    {
        if (!NetworkObjectUtils.IsRunning) return;

        Vector3 position = go.transform.position + posOffset;

        foreach (var axis in new[] { "Z", "X" })
        {
            Transform existing = go.transform.Find(LineName(axis));
            if (existing != null)
                NetworkObjectUtils.DespawnHierarchy(existing.gameObject);

            GameObject line = DrawLine(go, position, position, LineName(axis));
            if (line == null) continue;
            DrawUI(line.transform, position);
            line.SetActive(false);
        }
    }

    private void DrawUI(Transform parentRef, Vector3 position)
    {
        NetworkedMUI spawnedUI = NetworkObjectUtils.Runner.Spawn(measureUI, Vector3.zero, Quaternion.identity);
        if (spawnedUI == null) return;
        spawnedUI.transform.parent = parentRef;
        spawnedUI.SetCameraRef(cameraRef);
        spawnedUI.SetUIPositions(position, "", false);
    }

    private GameObject DrawLine(GameObject go, Vector3 p1, Vector3 p2, string name)
    {
        NetworkedLine spawnedLineComponent = NetworkObjectUtils.Runner.Spawn(linePrefab, Vector3.zero, linePrefab.transform.rotation, NetworkObjectUtils.Runner.LocalPlayer);
        if (spawnedLineComponent == null) return null;
        Fusion.NetworkObject spawnNWLine = spawnedLineComponent.Object;

        spawnNWLine.transform.name = name;
        spawnNWLine.transform.parent = go.transform;

        GameObject lineObj = spawnNWLine.gameObject;
        if (spawnNWLine.HasStateAuthority)
        {
            NetworkedLine spawnedLine = lineObj.GetComponent<NetworkedLine>();
            if (spawnedLine != null)
                spawnedLine.SetLinePositions(p1, p2, false);
        }
        return lineObj;
    }

    // Calculates rotation to face the main camera
    private Quaternion GetRotationFacingCamera(Vector3 objectPosition)
    {
        Vector3 directionToCamera = (cameraRef.position - objectPosition).normalized;
        return Quaternion.LookRotation(-directionToCamera, Vector3.up);
    }
}
