using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Aides Fusion (mode Shared) partagées par les outils du Modélisateur (N5) :
/// - prise d'autorité d'état sur un objet créé par un autre casque (RequestStateAuthority) ;
/// - suppression réseau d'une hiérarchie (Despawn de chaque NetworkObject, enfants d'abord)
///   à la place de Destroy(), qui laissait des objets orphelins chez les autres participants.
/// </summary>
public class NetworkObjectUtils : MonoBehaviour
{
    private static NetworkObjectUtils _instance;

    public static NetworkObjectUtils Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("NetworkObjectUtils");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<NetworkObjectUtils>();
            }
            return _instance;
        }
    }

    public static NetworkRunner Runner => NetworkManager.Instance != null ? NetworkManager.Instance.Runner : null;

    public static bool IsRunning => NetworkManager.Instance != null && NetworkManager.Instance.IsRunning;

    /// <summary>Vrai si ce casque a l'autorité d'état sur l'objet (ou si l'objet n'est pas réseau).</summary>
    public static bool HasAuthority(GameObject go)
    {
        if (go == null) return false;
        var no = go.GetComponent<NetworkObject>();
        return no == null || !no.IsValid || no.HasStateAuthority;
    }

    /// <summary>
    /// Demande l'autorité d'état sur l'objet si nécessaire (le prefab doit autoriser la reprise d'autorité :
    /// flag « Allow State Authority Override »). Le rappel est invoqué quand l'autorité est acquise
    /// (immédiatement si elle l'est déjà) ou avec false après le délai maximal.
    /// </summary>
    public static void EnsureAuthority(GameObject go, Action<bool> onDone = null, float timeout = 2f)
    {
        if (go == null) { onDone?.Invoke(false); return; }
        var no = go.GetComponent<NetworkObject>();
        if (no == null || !no.IsValid || no.HasStateAuthority) { onDone?.Invoke(true); return; }

        no.RequestStateAuthority();
        Instance.StartCoroutine(Instance.WaitForAuthority(no, onDone, timeout));
    }

    private IEnumerator WaitForAuthority(NetworkObject no, Action<bool> onDone, float timeout)
    {
        float deadline = Time.unscaledTime + timeout;
        while (no != null && no.IsValid && !no.HasStateAuthority && Time.unscaledTime < deadline)
            yield return null;

        bool ok = no != null && no.IsValid && no.HasStateAuthority;
        if (!ok)
            Debug.LogWarning($"[NetworkObjectUtils] Autorité non obtenue sur {(no != null ? no.name : "?")} : vérifiez le flag Allow State Authority Override du prefab.");
        onDone?.Invoke(ok);
    }

    /// <summary>
    /// Supprime l'objet pour tous les participants : chaque NetworkObject de la hiérarchie est despawné
    /// (enfants d'abord), après prise d'autorité si besoin. Les objets non réseau sont détruits localement.
    /// </summary>
    public static void DespawnHierarchy(GameObject go, Action onDone = null)
    {
        if (go == null) { onDone?.Invoke(); return; }

        var runner = Runner;
        var objects = new List<NetworkObject>(go.GetComponentsInChildren<NetworkObject>(true));
        if (runner == null || !runner.IsRunning || objects.Count == 0)
        {
            Destroy(go);
            onDone?.Invoke();
            return;
        }

        Instance.StartCoroutine(Instance.DespawnRoutine(go, objects, onDone));
    }

    private IEnumerator DespawnRoutine(GameObject root, List<NetworkObject> objects, Action onDone)
    {
        // Enfants d'abord (profondeur décroissante), racine en dernier.
        objects.Sort((a, b) => Depth(b.transform).CompareTo(Depth(a.transform)));

        foreach (var no in objects)
        {
            if (no == null || !no.IsValid) continue;
            if (!no.HasStateAuthority)
            {
                bool done = false;
                EnsureAuthority(no.gameObject, _ => done = true);
                while (!done) yield return null;
            }
            if (no != null && no.IsValid && no.HasStateAuthority && Runner != null)
                Runner.Despawn(no);
        }

        // Ce qui reste (objets locaux sans NetworkObject) est détruit localement.
        if (root != null)
            Destroy(root);

        onDone?.Invoke();
    }

    private static int Depth(Transform t)
    {
        int d = 0;
        while (t.parent != null) { d++; t = t.parent; }
        return d;
    }

    /// <summary>Déplace un objet réseau (téléportation NetworkTransform si présent) ; l'appelant doit avoir l'autorité.</summary>
    public static void Teleport(GameObject go, Vector3 position, Quaternion rotation)
    {
        if (go == null) return;
        var nt = go.GetComponent<NetworkTransform>();
        if (nt != null)
            nt.Teleport(position, rotation);
        else
            go.transform.SetPositionAndRotation(position, rotation);
    }
}
