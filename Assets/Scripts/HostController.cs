using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRMultiplayer;

public class HostController : NetworkBehaviour
{
    public GameObject thankGivingWindow;
    public float delay = 1800f;
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

        Instantiate(thankGivingWindow);

        Invoke("QuitApplication", 3f);
    }

    public void StoptLogSynchornisationTimeout()
    {
        float currentNetworkTime = NetworkManager.Instance.Runner.SimulationTime; // Récupérer l'heure réseau actuelle
        SyncManager.Instance.RPC_StopSynchronizationTimeOut(currentNetworkTime);
        Debug.LogError("Sent synchronization Timeout Stop signal to all clients.");

        Instantiate(thankGivingWindow);

        Invoke("QuitApplication", 3f);
    }

    private void QuitApplication()
    {
        Debug.LogError("Quit Application !!!!!!!!!.");

        Application.Quit();
    }
    public IEnumerator StopApplicationAfterDelayCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        StoptLogSynchornisation();
        Instantiate(thankGivingWindow);

        yield return new WaitForSeconds(3.0f);
        Application.Quit();
    }
}
