using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRMultiplayer;
using static Fusion.Sockets.NetBitBuffer;

public class HostController : NetworkBehaviour
{
    public GameObject thankGivingWindow;
    public float delay = 1800f;
    public float distance = 1.2f;       // meters in front of the user
    public float verticalOffset = -0.1f; // slightly below gaze
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
       ShowThanksgivingCanvasForAll();
        SyncManager.Instance.RPC_StopSynchronization(currentNetworkTime);
        Debug.LogError("Sent synchronization Stop signal to all clients.");

       // Instantiate(thankGivingWindow);
        

        Invoke("QuitApplication", 3f);
    }

    public void ShowThanksgivingCanvasForAll()
    {
        if (thankGivingWindow != null)
        {

            Camera cam = GetPlayerCamera();
            if (cam == null)
            {
                Debug.LogError("[HostController] No player camera found on this client.");
                return;
            }

            Vector3 forward = cam.transform.forward;
            Vector3 right = cam.transform.right;
            Vector3 up = cam.transform.up;

            Vector3 spawnPos = cam.transform.position + forward.normalized * distance + up * verticalOffset;
            Quaternion spawnRot = Quaternion.LookRotation((spawnPos - cam.transform.position).normalized, Vector3.up);

            Debug.LogError("Trying to launch Thanks Window: step1");


            Fusion.NetworkObject spawnNWObjEnd = null;
            NetworkManager.Instance.Runner.Spawn(thankGivingWindow, spawnPos, spawnRot, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
            {
                spawnNWObjEnd = obj;

                Debug.LogError("Trying to launch Thanks Window: step2");

            });
            Debug.LogError("Trying to launch Thanks Window: step3");

        }
    }


    public void StoptLogSynchornisationTimeout()
    {
        float currentNetworkTime = NetworkManager.Instance.Runner.SimulationTime; // Récupérer l'heure réseau actuelle
        ShowThanksgivingCanvasForAll();
        SyncManager.Instance.RPC_StopSynchronizationTimeOut(currentNetworkTime);
        Debug.LogError("Sent synchronization Timeout Stop signal to all clients.");

       // Instantiate(thankGivingWindow);
       

        Invoke("QuitApplication", 3f);
    }
    private static Camera GetPlayerCamera()
    {
        // 1) Try MainCamera tag
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;

        // 2) Fallback to any enabled camera on display 0
        Camera[] cams = GameObject.FindObjectsOfType<Camera>(true);
        foreach (var c in cams)
        {
            if (c.isActiveAndEnabled && c.targetDisplay == 0)
                return c;
        }
        return null;
    }
    private void QuitApplication()
    {
        Debug.LogError("Quit Application !!!!!!!!!.");

        Application.Quit();
    }
    public IEnumerator StopApplicationAfterDelayCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Instantiate(thankGivingWindow);
        ShowThanksgivingCanvasForAll();

        StoptLogSynchornisation();



         yield return new WaitForSeconds(3.0f);
        Application.Quit();
    }
}
