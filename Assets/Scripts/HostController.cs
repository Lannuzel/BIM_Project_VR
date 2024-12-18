using Fusion;
using UnityEngine;

public class HostController : NetworkBehaviour
{
    void Update()
    {
        // Appuyer sur la touche "Space" pour démarrer la synchronisation
        if (Input.GetKeyDown(KeyCode.Space) && Object.HasStateAuthority)
        {
            float currentNetworkTime = Runner.SimulationTime; // Récupérer l'heure réseau actuelle
            SyncManager.Instance.RPC_StartSynchronization(currentNetworkTime);
            Debug.Log("Sent synchronization signal to all clients.");
        }
    }
}
