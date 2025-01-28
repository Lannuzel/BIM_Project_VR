using Fusion;
using UnityEngine;

public class HostController : NetworkBehaviour
{
    void Update()
    {
        // Appuyer sur la touche "Space" pour démarrer la synchronisation
        if (Input.GetKeyDown(KeyCode.Space))
        {
            float currentNetworkTime = NetworkManager.Instance.Runner.SimulationTime; // Récupérer l'heure réseau actuelle
            SyncManager.Instance.RPC_StartSynchronization(currentNetworkTime);
            Debug.LogError("Sent synchronization signal to all clients.");
        }
    }
    public void StartLogSynchornisation()
    {
        float currentNetworkTime = NetworkManager.Instance.Runner.SimulationTime; // Récupérer l'heure réseau actuelle
        SyncManager.Instance.RPC_StartSynchronization(currentNetworkTime);
        Debug.LogError("Sent synchronization signal to all clients.");
    }
    public void StoptLogSynchornisation()
    {
        float currentNetworkTime = NetworkManager.Instance.Runner.SimulationTime; // Récupérer l'heure réseau actuelle
        SyncManager.Instance.RPC_StopSynchronization(currentNetworkTime);
        Debug.LogError("Sent synchronization Stop signal to all clients.");
    }
}
