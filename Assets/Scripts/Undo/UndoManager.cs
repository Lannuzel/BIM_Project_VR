using System;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Pile d'annulation du Modélisateur (F19) : chaque création, suppression, déplacement, rotation,
/// alignement ou collage d'objet est empilé ; « Annuler la dernière action » rejoue l'inverse.
/// Un bouton « Annuler » est ajouté en code aux menus Édition d'objets, Mesure et Réservation
/// (clone du bouton Supprimer existant), et la touche Y (manette gauche) maintenue 0,6 s annule aussi.
/// Les annulations passent par le réseau (Despawn / Spawn / Teleport), donc les autres casques
/// voient le résultat.
/// </summary>
public class UndoManager : MonoBehaviour
{
    public interface IUndoAction
    {
        string Label { get; }
        /// <summary>Exécute l'annulation ; retourne false si elle n'est plus possible.</summary>
        bool Undo();
    }

    private static UndoManager _instance;
    public static UndoManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UndoManager>();
                if (_instance == null)
                    _instance = new GameObject("UndoManager").AddComponent<UndoManager>();
            }
            return _instance;
        }
    }

    [Tooltip("Nombre maximal d'actions mémorisées.")]
    public int maxDepth = 50;
    [Tooltip("Ajouter un bouton « Annuler » aux menus du Modélisateur (clone du bouton Supprimer).")]
    public bool addMenuButtons = true;
    [Tooltip("Noms des boutons de menu à cloner pour créer le bouton Annuler.")]
    public List<string> deleteButtonNames = new List<string> { "Delete", "DeleteSelectedLine" };
    public string undoButtonLabel = "Annuler la dernière action";
    [Tooltip("Durée (s) d'appui sur Y (manette gauche) pour annuler.")]
    public float holdSecondsToUndo = 0.6f;

    /// <summary>Déclenché quand la pile change (libellé de la prochaine annulation, ou vide).</summary>
    public event Action<string> Changed;

    private readonly List<IUndoAction> stack = new List<IUndoAction>();
    private readonly List<TMP_Text> buttonLabels = new List<TMP_Text>();
    private float holdStart = -1f;
    private bool holdConsumed;
    private float nextMenuScan;

    public int Count => stack.Count;
    public string NextLabel => stack.Count > 0 ? stack[stack.Count - 1].Label : string.Empty;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
    }

    private void Start()
    {
        if (addMenuButtons)
            AddUndoButtons();
        RefreshLabels();
    }

    private void Update()
    {
        // Y maintenu : annulation au clavier de la manette gauche (Button.Four = Y).
        if (OVRInput.Get(OVRInput.Button.Four, OVRInput.Controller.LTouch))
        {
            if (holdStart < 0f) { holdStart = Time.unscaledTime; holdConsumed = false; }
            else if (!holdConsumed && Time.unscaledTime - holdStart >= holdSecondsToUndo)
            {
                holdConsumed = true;
                UndoLast();
            }
        }
        else
        {
            holdStart = -1f;
        }

        if (addMenuButtons && Time.unscaledTime >= nextMenuScan)
        {
            nextMenuScan = Time.unscaledTime + 2f;
            AddUndoButtons();
        }
    }

    #region Pile

    public static void Push(IUndoAction action)
    {
        if (action == null) return;
        var m = Instance;
        m.stack.Add(action);
        while (m.stack.Count > m.maxDepth)
            m.stack.RemoveAt(0);
        m.Notify();
        Debug.Log($"[Undo] + {action.Label} ({m.stack.Count})");
    }

    public bool UndoLast()
    {
        while (stack.Count > 0)
        {
            var action = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            bool ok;
            try { ok = action.Undo(); }
            catch (Exception e) { Debug.LogError($"[Undo] Échec de « {action.Label} » : {e}"); ok = false; }
            Notify();
            if (ok)
            {
                NetworkStatusPanel.Show($"Annulé : {action.Label}", 2f);
                return true;
            }
            Debug.LogWarning($"[Undo] « {action.Label} » n'est plus annulable, action suivante.");
        }
        NetworkStatusPanel.Show("Rien à annuler.", 2f);
        return false;
    }

    public void Clear()
    {
        stack.Clear();
        Notify();
    }

    private void Notify()
    {
        RefreshLabels();
        Changed?.Invoke(NextLabel);
    }

    #endregion

    #region Actions prêtes à l'emploi

    /// <summary>Création d'un ou plusieurs objets réseau : l'annulation les supprime.</summary>
    public static void RecordSpawn(string label, params GameObject[] spawned)
    {
        var list = new List<GameObject>();
        foreach (var go in spawned) if (go != null) list.Add(go);
        if (list.Count > 0) Push(new SpawnAction(label, list));
    }

    /// <summary>Instantané de pose d'objets avant un déplacement / rotation / alignement.</summary>
    public static PoseSnapshot CapturePoses(IEnumerable<GameObject> objects)
    {
        var s = new PoseSnapshot();
        foreach (var go in objects)
            if (go != null) s.Add(go);
        return s;
    }

    /// <summary>Empile le retour aux poses capturées si au moins un objet a bougé (ou toujours si <paramref name="force"/>,
    /// par exemple quand le déplacement est différé par une prise d'autorité réseau).</summary>
    public static void RecordPoseChange(string label, PoseSnapshot before, bool force = false)
    {
        if (before == null || before.items.Count == 0) return;
        if (!force && !before.HasChanged()) return;
        Push(new PoseAction(label, before));
    }

    /// <summary>Suppression d'objets réseau : l'annulation les recrée (prefab, pose, échelle, parent, lignes, texte).</summary>
    public static DeleteAction CaptureForDelete(string label, IEnumerable<GameObject> objects)
    {
        var action = new DeleteAction(label);
        foreach (var go in objects)
            if (go != null) action.Add(go);
        return action.Count > 0 ? action : null;
    }

    public class SpawnAction : IUndoAction
    {
        public string Label { get; }
        private readonly List<GameObject> objects;
        public SpawnAction(string label, List<GameObject> objects) { Label = label; this.objects = objects; }
        public bool Undo()
        {
            bool any = false;
            foreach (var go in objects)
            {
                if (go == null) continue;
                any = true;
                ObjectInteractionHandler.Instance?.DeselectObject(go);
                NetworkObjectUtils.DespawnHierarchy(go);
            }
            return any;
        }
    }

    public class PoseSnapshot
    {
        public readonly List<(GameObject go, Vector3 pos, Quaternion rot)> items = new List<(GameObject, Vector3, Quaternion)>();
        public void Add(GameObject go) => items.Add((go, go.transform.position, go.transform.rotation));
        public bool HasChanged()
        {
            foreach (var it in items)
            {
                if (it.go == null) continue;
                if ((it.go.transform.position - it.pos).sqrMagnitude > 1e-6f || Quaternion.Angle(it.go.transform.rotation, it.rot) > 0.05f)
                    return true;
            }
            return false;
        }
    }

    public class PoseAction : IUndoAction
    {
        public string Label { get; }
        private readonly PoseSnapshot before;
        public PoseAction(string label, PoseSnapshot before) { Label = label; this.before = before; }
        public bool Undo()
        {
            bool any = false;
            foreach (var it in before.items)
            {
                if (it.go == null) continue;
                any = true;
                var go = it.go; var pos = it.pos; var rot = it.rot;
                NetworkObjectUtils.EnsureAuthority(go, ok =>
                {
                    if (ok) NetworkObjectUtils.Teleport(go, pos, rot);
                });
            }
            return any;
        }
    }

    /// <summary>Description sérialisable d'un objet réseau supprimé, suffisante pour le recréer.</summary>
    public class DeleteAction : IUndoAction
    {
        private class Entry
        {
            public NetworkPrefabId prefabId;
            public bool hasPrefab;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 localScale;
            public Vector3? scaleFactor;      // ScaleNetworked
            public string parentName;
            public string name;
            public string tag;
            public (Vector3 a, Vector3 b, bool active)? line;   // NetworkedLine
            public (Vector3 pos, string text, bool active)? ui; // NetworkedMUI
            public List<Entry> children = new List<Entry>();
        }

        public string Label { get; }
        private readonly List<Entry> entries = new List<Entry>();
        public int Count => entries.Count;

        public DeleteAction(string label) { Label = label; }

        public void Add(GameObject go)
        {
            var e = Capture(go);
            if (e != null) entries.Add(e);
        }

        private static Entry Capture(GameObject go)
        {
            var no = go.GetComponent<NetworkObject>();
            var e = new Entry
            {
                position = go.transform.position,
                rotation = go.transform.rotation,
                localScale = go.transform.localScale,
                parentName = go.transform.parent != null ? go.transform.parent.name : null,
                name = go.name,
                tag = go.tag,
            };
            if (no != null && no.IsValid && no.NetworkTypeId.IsPrefab)
            {
                e.prefabId = no.NetworkTypeId.AsPrefabId;
                e.hasPrefab = true;
            }
            var scaler = go.GetComponent<ScaleNetworked>();
            if (scaler != null) e.scaleFactor = scaler.ScaleFactor;
            var line = go.GetComponent<NetworkedLine>();
            if (line != null) e.line = (line.networkedPointA, line.networkedPointB, line.isLineActive);
            var ui = go.GetComponent<NetworkedMUI>();
            if (ui != null) e.ui = (ui.networkedPosition, ui.networkedTextData, ui.isLineActive);

            // Sous-objets réseau directs (marqueurs, étiquette d'une mesure)
            foreach (Transform child in go.transform)
            {
                if (child.GetComponent<NetworkObject>() != null)
                {
                    var ce = Capture(child.gameObject);
                    if (ce != null) e.children.Add(ce);
                }
            }
            return e.hasPrefab ? e : null;
        }

        public bool Undo()
        {
            var runner = NetworkObjectUtils.Runner;
            if (runner == null || !runner.IsRunning) return false;
            bool any = false;
            foreach (var e in entries)
            {
                var go = Respawn(runner, e, null);
                if (go != null) any = true;
            }
            return any;
        }

        private static GameObject Respawn(NetworkRunner runner, Entry e, Transform parent)
        {
            var no = runner.Spawn(e.prefabId, e.position, e.rotation, runner.LocalPlayer, (r, obj) =>
            {
                var scaler = obj.GetComponent<ScaleNetworked>();
                if (scaler != null && e.scaleFactor.HasValue) scaler.ScaleFactor = e.scaleFactor.Value;
            });
            if (no == null) return null;

            var go = no.gameObject;
            go.name = e.name;
            try { if (!string.IsNullOrEmpty(e.tag)) go.tag = e.tag; } catch { }
            go.transform.localScale = e.localScale;

            Transform p = parent;
            if (p == null && !string.IsNullOrEmpty(e.parentName))
            {
                var pgo = GameObject.Find(e.parentName);
                if (pgo != null) p = pgo.transform;
            }
            if (p != null) go.transform.SetParent(p, true);

            var line = go.GetComponent<NetworkedLine>();
            if (line != null && e.line.HasValue) line.SetLinePositions(e.line.Value.a, e.line.Value.b, e.line.Value.active);
            var ui = go.GetComponent<NetworkedMUI>();
            if (ui != null && e.ui.HasValue) ui.SetUIPositions(e.ui.Value.pos, e.ui.Value.text, e.ui.Value.active);

            foreach (var ce in e.children)
                Respawn(runner, ce, go.transform);

            // Une mesure recréée est à nouveau connue du gestionnaire de mesures
            if (line != null && MeasurementHandlerV2.Instance != null && parent == null)
                MeasurementHandlerV2.Instance.AddLine(go);

            return go;
        }
    }

    #endregion

    #region Boutons de menu

    private readonly HashSet<GameObject> clonedFrom = new HashSet<GameObject>();

    private void AddUndoButtons()
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (!t.gameObject.scene.IsValid() || t.hideFlags != HideFlags.None) continue;
            if (!deleteButtonNames.Contains(t.name) || t.parent == null || t.parent.name != "Panel(Buttons)") continue;
            if (clonedFrom.Contains(t.gameObject) || t.parent.Find("Annuler") != null) continue;
            if (t.GetComponent<Button>() == null) continue;

            clonedFrom.Add(t.gameObject);
            CloneAsUndoButton(t.gameObject);
        }
    }

    private void CloneAsUndoButton(GameObject template)
    {
        var clone = Instantiate(template, template.transform.parent);
        clone.name = "Annuler";
        clone.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);

        foreach (var tmp in clone.GetComponentsInChildren<TMP_Text>(true)) { tmp.text = undoButtonLabel; buttonLabels.Add(tmp); }
        foreach (var txt in clone.GetComponentsInChildren<Text>(true)) txt.text = undoButtonLabel;

        var button = clone.GetComponent<Button>();
        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        button.onClick.AddListener(() => UndoLast());

        RefreshLabels();
        Debug.Log($"[Undo] Bouton « Annuler » ajouté dans {template.transform.parent.parent?.name}.");
    }

    private void RefreshLabels()
    {
        string label = stack.Count > 0 ? $"{undoButtonLabel}\n({NextLabel})" : undoButtonLabel;
        foreach (var tmp in buttonLabels)
            if (tmp != null) tmp.text = label;
    }

    #endregion
}
