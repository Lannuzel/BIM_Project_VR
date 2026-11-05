using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

/// <summary>
/// Outils de l'expérimentateur (F18) : changement de rôle à chaud et joueurs simulés.
/// - Un joueur simulé est un avatar réseau (même prefab que les vrais joueurs) marqué IsSimulated :
///   visible par tous, compté dans « Joueurs : n/N », sans logique de session (SyncManager, HostController,
///   ExitManager et NetworkRig l'ignorent).
/// - Changer de rôle réapplique le rôle partout (menu personnalisé, documents, nom de l'avatar, canvas du
///   Lecteur) ; si un joueur simulé a déjà ce rôle, les deux rôles sont échangés.
/// </summary>
public static class AdminTools
{
    public const string Lecteur = "Lecteur";
    public const string Calculateur = "Calculateur";
    public const string Modelisateur = "Modelisateur";
    public const string Experimentateur = "Experimentateur";

    public static readonly string[] Roles = { Lecteur, Calculateur, Modelisateur, Experimentateur };
    public static readonly string[] ParticipantRoles = { Lecteur, Calculateur, Modelisateur };

    private static readonly List<NetworkObject> simulated = new List<NetworkObject>();

    /// <summary>Déclenché après un changement de rôle local (nouveau rôle).</summary>
    public static event Action<string> LocalRoleChanged;

    public static string LocalRole => NetworkManager.GetLocalPlayerName();

    public static int SimulatedCount
    {
        get
        {
            simulated.RemoveAll(o => o == null || !o.IsValid);
            return simulated.Count;
        }
    }

    /// <summary>Tous les avatars valides (vrais joueurs et simulés).</summary>
    public static List<PlayerController> AllAvatars()
    {
        return UnityEngine.Object.FindObjectsOfType<PlayerController>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();
    }

    /// <summary>Avatar du joueur local (autorité d'état, non simulé).</summary>
    public static PlayerController LocalAvatar()
    {
        return AllAvatars().FirstOrDefault(p => p.Object.HasStateAuthority && !p.IsSimulated);
    }

    public static bool IsSimulated(NetworkObject obj)
    {
        if (obj == null) return false;
        var pc = obj.GetComponent<PlayerController>();
        return pc != null && pc.IsSimulated;
    }

    #region Joueurs simulés

    /// <summary>Ajoute un joueur simulé ; rôle libre choisi automatiquement si <paramref name="role"/> est vide.</summary>
    public static PlayerController AddSimulatedPlayer(string role = null)
    {
        var runner = NetworkObjectUtils.Runner;
        if (runner == null || !runner.IsRunning)
        {
            NetworkStatusPanel.Show("Session réseau inactive : impossible d'ajouter un joueur simulé.", 3f);
            return null;
        }

        var spawner = UnityEngine.Object.FindObjectOfType<PlayerSpawner>();
        if (spawner == null || !spawner.PlayerPrefab.IsValid)
        {
            NetworkStatusPanel.Show("Prefab joueur introuvable (PlayerSpawner).", 3f);
            return null;
        }

        if (string.IsNullOrEmpty(role))
        {
            var taken = AllAvatars().Select(p => p.PlayerName).ToHashSet();
            role = ParticipantRoles.FirstOrDefault(r => !taken.Contains(r)) ?? $"Joueur {SimulatedCount + 1}";
        }

        int index = SimulatedCount;
        var cam = Camera.main;
        Vector3 basePos = cam != null ? cam.transform.position : Vector3.zero;
        Vector3 forward = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 pos = basePos + forward * 1.8f + right * (index - 1) * 1.2f;
        pos.y = 0.5f;
        Quaternion rot = Quaternion.LookRotation(-forward, Vector3.up);

        string roleToApply = role;
        var obj = runner.Spawn(spawner.PlayerPrefab, pos, rot, runner.LocalPlayer, (r, o) =>
        {
            var pc = o.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.IsSimulated = true;
                pc.PlayerName = roleToApply;
            }
        });
        if (obj == null) return null;

        simulated.Add(obj);
        Debug.Log($"[AdminTools] Joueur simulé ajouté : {role}");
        return obj.GetComponent<PlayerController>();
    }

    public static void RemoveLastSimulatedPlayer()
    {
        simulated.RemoveAll(o => o == null || !o.IsValid);
        if (simulated.Count == 0) return;
        var last = simulated[simulated.Count - 1];
        simulated.RemoveAt(simulated.Count - 1);
        NetworkObjectUtils.DespawnHierarchy(last.gameObject);
    }

    public static void RemoveAllSimulatedPlayers()
    {
        simulated.RemoveAll(o => o == null || !o.IsValid);
        foreach (var o in simulated.ToList())
            NetworkObjectUtils.DespawnHierarchy(o.gameObject);
        simulated.Clear();
    }

    #endregion

    #region Rôle

    /// <summary>Change le rôle du joueur local ; si un joueur simulé tient déjà ce rôle, les rôles sont échangés.</summary>
    public static void SetLocalRole(string newRole)
    {
        if (string.IsNullOrEmpty(newRole)) return;
        string previous = LocalRole;
        if (newRole == previous) return;

        var holder = AllAvatars().FirstOrDefault(p => p.IsSimulated && p.PlayerName == newRole && p.Object.HasStateAuthority);
        if (holder != null)
        {
            holder.PlayerName = previous;
            Debug.Log($"[AdminTools] Rôles échangés avec un joueur simulé : il devient {previous}.");
        }

        PlayerPrefs.SetString("PlayerName", newRole);
        PlayerPrefs.Save();

        var local = LocalAvatar();
        if (local != null)
            local.PlayerName = newRole;

        ApplyLocalRoleEverywhere(newRole);
        LocalRoleChanged?.Invoke(newRole);
        NetworkStatusPanel.Show($"Rôle : {newRole}", 2.5f);
    }

    /// <summary>Réapplique le rôle local aux éléments qui ne le lisent qu'au démarrage.</summary>
    public static void ApplyLocalRoleEverywhere(string role)
    {
        foreach (var menu in Resources.FindObjectsOfTypeAll<ShowPersonalisedMenu>())
            if (menu.gameObject.scene.IsValid()) menu.ApplyRole(role);
        foreach (var pdf in Resources.FindObjectsOfTypeAll<PdfDocumentLoader>())
            if (pdf.gameObject.scene.IsValid()) pdf.ApplyRole(role);
        foreach (var img in Resources.FindObjectsOfTypeAll<RawImageTextureLoader>())
            if (img.gameObject.scene.IsValid()) img.ApplyRole(role);
        BIMInteractionManager.ReapplyRoles(role);
    }

    #endregion
}
