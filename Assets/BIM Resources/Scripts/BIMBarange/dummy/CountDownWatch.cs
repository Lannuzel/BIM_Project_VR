using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Horloge de l'activité (chronomètre affiché dans la salle).
/// Recette du 6 novembre 2026 : l'horloge n'apparaissait plus. Elle était un objet réseau créé uniquement par
/// le clic sur le bouton « Lancer l'activité » du casque qui lançait ; tout autre chemin de lancement (panneau
/// expérimentateur, autre rôle) ne la créait pas. Elle est désormais une copie locale du prefab, instanciée sur
/// chaque casque au marqueur de lancement (<see cref="SyncManager.RecordingStartedEvent"/>) et retirée à
/// l'arrêt : même position que l'ancien objet réseau, mais sans dépendre du réseau ni de qui lance.
/// Recette du 9 novembre 2026 : la pose du prefab (tableau blanc de XP1) ne convient pas à toutes les scènes (en XP3 le
/// cadran flottait devant le mur est, sous le faux plafond). <see cref="SessionConfig.clockPlacements"/> donne, par
/// scène, la position du cadran ; sans entrée, la pose du prefab est conservée.
/// </summary>
public class CountDownWatch : NetworkBehaviour
{
    public GameObject clockPrefab;
    public GameObject interactionMenu;
    public float distance = 1.2f;       // meters in front of the user
    public float verticalOffset = -0.1f; // slightly below gaze

    private GameObject localClock;

    private void OnEnable()
    {
        SyncManager.RecordingStartedEvent += OnRecordingStarted;
        SyncManager.RecordingStoppedEvent += OnRecordingStopped;
    }

    private void OnDisable()
    {
        SyncManager.RecordingStartedEvent -= OnRecordingStarted;
        SyncManager.RecordingStoppedEvent -= OnRecordingStopped;
    }

    private void OnRecordingStarted() => ShowLocalClock();

    private void OnRecordingStopped(bool timeout) => DeleteClock();

    /// <summary>Appelé par le bouton de lancement de la scène : horloge locale et menu d'interaction.</summary>
    public void DisplayClock()
    {
        ShowLocalClock();
        if (interactionMenu != null)
            interactionMenu.SetActive(true);
    }

    /// <summary>Instancie l'horloge localement (idempotent) à la pose du prefab, rattachée à cet objet.</summary>
    public void ShowLocalClock()
    {
        if (localClock != null) return;
        if (clockPrefab == null)
        {
            Debug.LogWarning("[CountDownWatch] Aucun prefab d'horloge assigné : horloge non affichée.");
            return;
        }

        ClockPose(out Vector3 position, out Quaternion rotation);
        localClock = Instantiate(clockPrefab, position, rotation, transform);
        localClock.name = clockPrefab.name + " (local)";

        // Copie locale : les composants réseau sont rendus inertes (comme la fenêtre de remerciement),
        // seul le chronomètre (WatchTimer) reste actif.
        foreach (var nb in localClock.GetComponentsInChildren<NetworkBehaviour>(true))
            if (!(nb is WatchTimer)) nb.enabled = false;
        foreach (var no in localClock.GetComponentsInChildren<NetworkObject>(true))
            no.enabled = false;

        localClock.SetActive(true);
        Debug.Log($"[CountDownWatch] Horloge locale affichée à {localClock.transform.position}.");
    }

    /// <summary>Pose de la racine du prefab : celle du prefab, ou celle qui amène le cadran à la position configurée pour la scène.</summary>
    private void ClockPose(out Vector3 position, out Quaternion rotation)
    {
        position = clockPrefab.transform.position;
        rotation = clockPrefab.transform.rotation;
        var placement = SessionConfig.Instance.ClockPlacementFor(SceneManager.GetActiveScene().name);
        if (placement == null) return;

        // Le cadran (canvas) est décalé par rapport à la racine du prefab : on compense ce décalage.
        var face = clockPrefab.GetComponentInChildren<Canvas>(true);
        Vector3 faceOffset = face != null ? face.transform.position - clockPrefab.transform.position : Vector3.zero;
        // Rotation relative à la pose du prefab (sa racine est déjà tournée de 90° : cadran parallèle au mur de droite).
        rotation = clockPrefab.transform.rotation * Quaternion.Euler(placement.euler);
        position = placement.position - rotation * faceOffset;
        Debug.Log($"[CountDownWatch] Cadran placé selon la configuration ({placement.scene}) à {placement.position}.");
    }

    public void DeleteClock()
    {
        if (localClock != null)
            Destroy(localClock);
        localClock = null;
    }
}
