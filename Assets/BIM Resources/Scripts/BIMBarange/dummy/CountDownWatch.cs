using Fusion;
using UnityEngine;

/// <summary>
/// Horloge de l'activité (chronomètre affiché dans la salle).
/// Recette du 6 novembre 2026 : l'horloge n'apparaissait plus. Elle était un objet réseau créé uniquement par
/// le clic sur le bouton « Lancer l'activité » du casque qui lançait ; tout autre chemin de lancement (panneau
/// expérimentateur, autre rôle) ne la créait pas. Elle est désormais une copie locale du prefab, instanciée sur
/// chaque casque au marqueur de lancement (<see cref="SyncManager.RecordingStartedEvent"/>) et retirée à
/// l'arrêt : même position que l'ancien objet réseau, mais sans dépendre du réseau ni de qui lance.
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

        localClock = Instantiate(clockPrefab, clockPrefab.transform.position, clockPrefab.transform.rotation, transform);
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

    public void DeleteClock()
    {
        if (localClock != null)
            Destroy(localClock);
        localClock = null;
    }
}
