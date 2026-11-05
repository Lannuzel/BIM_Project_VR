using System;
using System.Collections;
using Fusion;
using UnityEngine;

/// <summary>
/// Synchronisation du lancement et de l'arrêt des enregistrements entre les casques (N5).
/// Une instance vit sur chaque avatar (PlayerPrefabMB) ; <see cref="Instance"/> est celle du joueur local.
/// Le Lecteur (ou l'expérimentateur) envoie une heure réseau cible (maintenant + <see cref="syncLeadSeconds"/>) ;
/// chaque casque attend cette heure sur sa propre horloge réseau pour poser le marqueur : les marqueurs sont alignés.
/// Le délai maximal (<see cref="timerThreshold"/>) est armé localement sur chaque casque à partir de la même heure.
/// </summary>
public class SyncManager : NetworkBehaviour
{
    /// <summary>Instance du joueur local (celle sur laquelle ce casque a l'autorité d'état).</summary>
    public static SyncManager Instance;

    private float syncStartTime; // Heure réseau du lancement
    private float syncEndTime;   // Heure réseau de l'arrêt
    public bool IsRecordingStarted { get; private set; }
    public MarkerManager markerManager;
    [Tooltip("Durée maximale (s) de la tâche : arrêt automatique sur tous les casques.")]
    public float timerThreshold = 1800f;
    [Tooltip("Avance (s) donnée à l'heure cible pour que tous les casques la reçoivent avant qu'elle n'arrive.")]
    public float syncLeadSeconds = 1f;
    [Tooltip("Délai (s) après l'arrêt avant de quitter l'application.")]
    public float exitDelayAfterStop = 6f;

    public static event Action RecordingStartedEvent;
    public static event Action<bool> RecordingStoppedEvent; // true = délai dépassé

    private Coroutine timeoutRoutine;
    private Coroutine pendingStart;
    private Coroutine pendingStop;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            Instance = this;
        if (markerManager == null)
            markerManager = GetComponentInChildren<MarkerManager>(true);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    private MarkerManager LocalMarkerManager()
    {
        var local = Instance != null ? Instance : this;
        if (local.markerManager != null) return local.markerManager;
        return FindObjectOfType<MarkerManager>();
    }

    #region Commandes locales (bouton du Lecteur / panneau expérimentateur)

    /// <summary>Lance l'enregistrement synchronisé sur tous les casques.</summary>
    public void StartSynchronizedRecording()
    {
        if (!CanSend()) return;
        float target = Runner.SimulationTime + syncLeadSeconds;
        RPC_StartSynchronization(target);
    }

    /// <summary>Arrête l'enregistrement sur tous les casques (fin de tâche ou délai dépassé).</summary>
    public void StopSynchronizedRecording(bool timeout = false)
    {
        if (!CanSend()) return;
        float target = Runner.SimulationTime + syncLeadSeconds;
        if (timeout) RPC_StopSynchronizationTimeOut(target);
        else RPC_StopSynchronization(target);
    }

    private bool CanSend()
    {
        if (Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning)
        {
            Debug.LogError("[SyncManager] Session réseau inactive : commande ignorée.");
            return false;
        }
        if (!Object.HasStateAuthority)
        {
            Debug.LogError("[SyncManager] Cette instance n'est pas celle du joueur local : utilisez SyncManager.Instance.");
            return false;
        }
        return true;
    }

    #endregion

    #region RPC (émis par le propriétaire de l'objet, exécutés sur tous les casques)

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartSynchronization(float networkTime)
    {
        var local = Instance != null ? Instance : this;
        local.HandleStart(networkTime);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StopSynchronization(float networkTime)
    {
        var local = Instance != null ? Instance : this;
        local.HandleStop(networkTime, false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StopSynchronizationTimeOut(float networkTime)
    {
        var local = Instance != null ? Instance : this;
        local.HandleStop(networkTime, true);
    }

    #endregion

    private void HandleStart(float networkTime)
    {
        syncStartTime = networkTime;
        Debug.Log($"Synchronization start scheduled at network time: {syncStartTime} (now {Runner.SimulationTime:0.000})");
        if (pendingStart != null) StopCoroutine(pendingStart);
        pendingStart = StartCoroutine(StartRecordingAt(networkTime));
    }

    private void HandleStop(float networkTime, bool timeout)
    {
        syncEndTime = networkTime;
        Debug.Log($"Synchronization stop scheduled at network time: {syncEndTime} (now {Runner.SimulationTime:0.000})");
        if (timeoutRoutine != null) { StopCoroutine(timeoutRoutine); timeoutRoutine = null; }
        if (pendingStop != null) StopCoroutine(pendingStop);
        pendingStop = StartCoroutine(StopRecordingAt(networkTime, timeout));
    }

    private IEnumerator StartRecordingAt(float networkTime)
    {
        float delay = networkTime - Runner.SimulationTime;
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        StartRecording();

        // Délai maximal local, aligné sur la même heure cible pour tous les casques.
        if (timeoutRoutine != null) StopCoroutine(timeoutRoutine);
        timeoutRoutine = StartCoroutine(TimeoutRoutine());
    }

    private IEnumerator StopRecordingAt(float networkTime, bool timeout)
    {
        float delay = networkTime - Runner.SimulationTime;
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        if (timeout) StopRecordingTimeout(); else StopRecording();
    }

    private IEnumerator TimeoutRoutine()
    {
        yield return new WaitForSecondsRealtime(timerThreshold);
        if (!IsRecordingStarted) yield break;
        Debug.LogError("[SyncManager] Délai maximal atteint : arrêt local des enregistrements.");
        timeoutRoutine = null;
        StopRecordingTimeout();
    }

    private void StartRecording()
    {
        string playerName = NetworkManager.GetLocalPlayerName();
        var mm = LocalMarkerManager();
        if (mm != null) mm.AddMarkerToAllLogs();
        else Debug.LogError("[SyncManager] MarkerManager introuvable : marqueur non posé.");
        Debug.LogError("Recording started for all trackers: ! : " + playerName);
        IsRecordingStarted = true;
        RecordingStartedEvent?.Invoke();
    }

    private void StopRecording()
    {
        string playerName = NetworkManager.GetLocalPlayerName();
        Debug.LogError("Runner.SimulationTime : " + Runner.SimulationTime + "SyncStart : " + syncStartTime + "  SyncEnd : " + syncEndTime + "  difference : " + (syncEndTime - syncStartTime));

        var mm = LocalMarkerManager();
        if (mm != null) mm.StopAllRecordings();

        Debug.LogError("Recording Stopped for all trackers: ! : " + playerName);
        IsRecordingStarted = false;
        RecordingStoppedEvent?.Invoke(false);
        EndOfSessionLocally();
    }

    private void StopRecordingTimeout()
    {
        string playerName = NetworkManager.GetLocalPlayerName();
        Debug.LogError("Runner.SimulationTime : " + Runner.SimulationTime + "SyncStart : " + syncStartTime + "  SyncEnd : " + syncEndTime + "  difference : " + (syncEndTime - syncStartTime));

        var clockObject = GameObject.Find("CountDownWatch");
        if (clockObject != null)
        {
            var watch = clockObject.GetComponent<CountDownWatch>();
            if (watch != null) watch.DeleteClock();
            clockObject.SetActive(false);
        }

        var mm = LocalMarkerManager();
        if (mm != null) mm.StopAllRecordingsTimeOut();
        IsRecordingStarted = false;
        RecordingStoppedEvent?.Invoke(true);
        EndOfSessionLocally();
    }

    /// <summary>Fenêtre de remerciement devant la caméra de ce casque, puis fermeture de l'application.</summary>
    private void EndOfSessionLocally()
    {
        HostController.ShowThanksLocally();
        ExitManager.QuitLocallyAfter(exitDelayAfterStop);
    }
}
