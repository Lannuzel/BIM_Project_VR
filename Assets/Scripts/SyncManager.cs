using Fusion;

using UnityEngine;

public class SyncManager : NetworkBehaviour
{
    public static SyncManager Instance;
    private float syncStartTime; // Temps de synchronisation réseau
    private float syncEndTime; // Temps de synchronisation réseau
    public bool IsRecordingStarted { get; private set; }
    public MarkerManager markerManager;
    public float timerThreshold = 1800f;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_StartSynchronization(float networkTime)
    {
        syncStartTime = networkTime;
        Debug.Log($"Synchronization started at network time: {syncStartTime}");
        StartCoroutine(StartRecordingWithDelay());
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_StopSynchronization(float networkTime)
    {
        syncEndTime = networkTime;
        Debug.Log($"Synchronization stopped at network time: {syncEndTime}");
        StartCoroutine(StopRecordingWithDelay());
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_StopSynchronizationTimeOut(float networkTime)
    {
        syncEndTime = networkTime;
        Debug.Log($"Synchronization stopped at network time: {syncEndTime}");
        StartCoroutine(StopRecordingWithDelayTimeOut());
    }
    private System.Collections.IEnumerator StartRecordingWithDelay()
    {
        float localDelay = (float)(syncEndTime - NetworkManager.Instance.Runner.SimulationTime); // Décalage entre l'horloge réseau et locale
        if (localDelay > 0)
            yield return new WaitForSeconds(localDelay);

        StartRecording();
    }
    private System.Collections.IEnumerator StopRecordingWithDelay()
    {
        float localDelay = (float)(syncEndTime - NetworkManager.Instance.Runner.SimulationTime); // Décalage entre l'horloge réseau et locale
        if (localDelay > 0)
            yield return new WaitForSeconds(localDelay);

        StopRecording();
    }
    private System.Collections.IEnumerator StopRecordingWithDelayTimeOut()
    {
        float localDelay = (float)(syncEndTime - NetworkManager.Instance.Runner.SimulationTime); // Décalage entre l'horloge réseau et locale
        if (localDelay > 0)
            yield return new WaitForSeconds(localDelay);

        StopRecordingTimeout();
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
        Debug.LogError("Runner.SimulationTime : " + NetworkManager.Instance.Runner.SimulationTime +  "SyncStart : " + syncStartTime + "  SyncEnd : " + syncEndTime + "  difference : " + (syncEndTime - syncStartTime));

            markerManager.StopAllRecordings();
        



        Debug.LogError("Recording Stopped for all trackers: ! : " + playerName);
        IsRecordingStarted = false;
       

        // Appeler les méthodes de démarrage des enregistrements ici
        /*  PositionLogger.Instance.StartLogging();
          AudioTracker.Instance.StartRecording();
          FaceTrackingRecorder.Instance.StartRecording();
          EyeTrackingDataLogger.Instance.StartRecording();
        */
    }
    private void StopRecordingTimeout()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", $"Player {NetworkManager.Instance.Runner.LocalPlayer.PlayerId}");
        Debug.LogError("Runner.SimulationTime : " + NetworkManager.Instance.Runner.SimulationTime + "SyncStart : " + syncStartTime + "  SyncEnd : " + syncEndTime + "  difference : " + (syncEndTime - syncStartTime));

        markerManager.StopAllRecordingsTimeOut();
    }


    }
