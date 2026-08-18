using UnityEngine;

using Fusion;

public class DataLogger : NetworkBehaviour
{
    // Variable to track if gaze logging is active
    [Networked] private bool isLoggingActive { get; set; }

    private void Start()
    {
        isLoggingActive = false;
    }

    private void Update()
    {
        // Log gaze data if logging is active
        if (isLoggingActive)
        {
            LogData();
        }
    }

    // Function to be called by the StartLogButton UI
    public void StartLogRecording()
    {
        if (!NetworkManager.Instance.Runner || !NetworkManager.Instance.Runner.IsRunning)
        {
            Debug.LogError("NetworkRunner is not initialized. Cannot send RPC.");
            return;
        }

        {
            isLoggingActive = true;
            Debug.Log("Gaze logging activated for all players.");
            // Activate logging for all players via RPC
            //  RPC_ActivateLogging();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ActivateLogging()
    {
        isLoggingActive = true;
        Debug.Log("Gaze logging activated for all players.");
    }

    // Function to log gaze data
    private void LogData()
    {


        Debug.LogError($"Log started for the player ");

        // TODO: Save this data to a file or send it to a server
    }

    // Optional: Add a stop logging function
    public void StopLogRecording()
    {
        {
            RPC_DeactivateLogging();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DeactivateLogging()
    {
        isLoggingActive = false;
        Debug.LogError("Data logging deactivated for all players.");
    }
}
