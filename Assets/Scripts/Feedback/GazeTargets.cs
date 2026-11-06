using System.Collections.Generic;
using System.Text;
using Fusion;
using UnityEngine;

/// <summary>
/// Identifiant stable, identique sur tous les casques, de l'objet regardé (feedback de regard partagé).
/// Écart assumé par rapport à la version publiée (qui ne reconnaissait que les objets réseau, donc jamais le
/// mobilier fixe) : un objet réseau est identifié par son NetworkId ; tout autre objet à collider l'est par le
/// hachage de son chemin dans la hiérarchie de la scène (identique sur les trois casques, la scène étant la même).
/// </summary>
public static class GazeTargets
{
    private const int NetworkFlag = 1 << 30;
    private const int Mask = 0x3FFFFFFF;

    private static readonly Dictionary<int, GameObject> staticById = new Dictionary<int, GameObject>();
    private static float nextRebuild;

    public static int IdOf(Collider collider)
    {
        if (collider == null) return 0;
        var no = collider.GetComponentInParent<NetworkObject>();
        if (no != null && no.IsValid)
            return NetworkFlag | (int)(no.Id.Raw & Mask);
        return PathHash(collider.transform);
    }

    public static bool IsNetwork(int id) => (id & NetworkFlag) != 0;

    /// <summary>Objet local correspondant à un identifiant (null si inconnu).</summary>
    public static GameObject Find(int id)
    {
        if (id == 0) return null;
        if (IsNetwork(id))
        {
            var runner = NetworkObjectUtils.Runner;
            if (runner == null || !runner.IsRunning) return null;
            var netId = new NetworkId { Raw = (uint)(id & Mask) };
            return runner.TryFindObject(netId, out var no) && no != null ? no.gameObject : null;
        }
        if (staticById.TryGetValue(id, out var go) && go != null) return go;
        if (Time.unscaledTime >= nextRebuild)
        {
            Rebuild();
            if (staticById.TryGetValue(id, out go) && go != null) return go;
        }
        return null;
    }

    /// <summary>Nom lisible pour le journal.</summary>
    public static string NameOf(int id)
    {
        var go = Find(id);
        return go != null ? go.name : (id == 0 ? "" : id.ToString("X"));
    }

    private static void Rebuild()
    {
        nextRebuild = Time.unscaledTime + 2f;
        staticById.Clear();
        foreach (var c in Object.FindObjectsOfType<Collider>(true))
        {
            if (c.GetComponentInParent<NetworkObject>() != null) continue;
            int id = PathHash(c.transform);
            if (!staticById.ContainsKey(id)) staticById[id] = c.gameObject;
        }
    }

    /// <summary>FNV-1a du chemin « racine/…/objet » avec l'index de chaque enfant, sur 30 bits (jamais 0).</summary>
    public static int PathHash(Transform t)
    {
        var sb = new StringBuilder();
        var stack = new List<Transform>();
        for (var cur = t; cur != null; cur = cur.parent) stack.Add(cur);
        for (int i = stack.Count - 1; i >= 0; i--)
        {
            sb.Append(stack[i].name).Append('[').Append(stack[i].GetSiblingIndex()).Append("]/");
        }
        uint hash = 2166136261;
        foreach (char ch in sb.ToString())
        {
            hash ^= ch;
            hash *= 16777619;
        }
        int id = (int)(hash & Mask);
        return id == 0 ? 1 : id;
    }
}
