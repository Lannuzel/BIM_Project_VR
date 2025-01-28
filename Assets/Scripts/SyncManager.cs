using Fusion;
using UnityEngine;

public class SyncManager : NetworkBehaviour
{
    public static SyncManager Instance;
    private float syncTime; // Temps de synchronisation réseau
    public bool IsRecordingStarted { get; private set; }
    public MarkerManager markerManager;
    public GameObject thankGivingWindow;
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_StopSynchronization(float networkTime)
    {
        syncTime = networkTime;
        Debug.Log($"Synchronization stopped at network time: {syncTime}");
        StartCoroutine(StopRecordingWithDelay());
    }

    private System.Collections.IEnumerator StartRecordingWithDelay()
    {
        float localDelay = (float)(syncTime - Runner.SimulationTime); // Décalage entre l'horloge réseau et locale
        if (localDelay > 0)
            yield return new WaitForSeconds(localDelay);

        StartRecording();
    }
    private System.Collections.IEnumerator StopRecordingWithDelay()
    {
        float localDelay = (float)(syncTime - Runner.SimulationTime); // Décalage entre l'horloge réseau et locale
        if (localDelay > 0)
            yield return new WaitForSeconds(localDelay);

        StopRecording();
    }

    private void StartRecording()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", $"Player {NetworkManager.Instance.Runner.LocalPlayer.PlayerId}");
        markerManager.AddMarkerToAllLogs();
        Debug.LogError("Recording started for all trackers: ! : "+ playerName);
        IsRecordingStarted = true;

        // Appeler les méthodes de démarrage des enregistrements ici
      /*  PositionLogger.Instance.StartLogging();
        AudioTracker.Instance.StartRecording();
        FaceTrackingRecorder.Instance.StartRecording();
        EyeTrackingDataLogger.Instance.StartRecording();
      */
    }

    private void StopRecording()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", $"Player {NetworkManager.Instance.Runner.LocalPlayer.PlayerId}");
        markerManager.StopAllRecordings();
        Debug.LogError("Recording Stopped for all trackers: ! : " + playerName);
        IsRecordingStarted = false;
        StartCoroutine(QuitApplication());

        // Appeler les méthodes de démarrage des enregistrements ici
        /*  PositionLogger.Instance.StartLogging();
          AudioTracker.Instance.StartRecording();
          FaceTrackingRecorder.Instance.StartRecording();
          EyeTrackingDataLogger.Instance.StartRecording();
        */
    }

    private System.Collections.IEnumerator QuitApplication()
    {
       

        Instantiate(thankGivingWindow);

        yield return new WaitForSeconds(3.0f); 
        Application.Quit();
    }
}
