using Fusion;
using UnityEngine;

public class SyncManager : NetworkBehaviour
{
    public static SyncManager Instance;
    private float syncTime; // Temps de synchronisation réseau
    public bool IsRecordingStarted { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_StartSynchronization(float networkTime)
    {
        syncTime = networkTime;
        Debug.Log($"Synchronization started at network time: {syncTime}");
        StartCoroutine(StartRecordingWithDelay());
    }

    private System.Collections.IEnumerator StartRecordingWithDelay()
    {
        float localDelay = (float)(syncTime - Runner.SimulationTime); // Décalage entre l'horloge réseau et locale
        if (localDelay > 0)
            yield return new WaitForSeconds(localDelay);

        StartRecording();
    }

    private void StartRecording()
    {
        Debug.Log("Recording started for all trackers!");
        IsRecordingStarted = true;

        // Appeler les méthodes de démarrage des enregistrements ici
        PositionLogger.Instance.StartLogging();
        AudioTracker.Instance.StartRecording();
        FaceTrackingRecorder.Instance.StartRecording();
        EyeTrackingDataLogger.Instance.StartRecording();
    }
}
