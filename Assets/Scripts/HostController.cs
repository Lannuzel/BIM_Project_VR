using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using XRMultiplayer;
using static Fusion.Sockets.NetBitBuffer;

public class HostController : NetworkBehaviour
{

    [SerializeField] private float delay = 1800f;
    [SerializeField] private float distance = 1.2f;       // meters in front of the user
    [SerializeField] private float verticalOffset = -0.1f; // slightly below gaze
    [SerializeField] bool pinToCamera = true;       // keep it fixed to the view


    public static HostController instance { get; private set; }

    [Header("Goodbye UI")]
    [SerializeField] private GameObject thankGivingWindow;

    [Header("Exit")]
    public float exitDelay = 6f;


    static GameObject _localThanksInstance; // prevent duplicates on each client


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
    public override void Spawned()
    {
        if (Object.HasInputAuthority)
            instance = this;
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

        // 1) Tell everyone to show the local window
        ShowThanksgivingCanvasForAll();
        // After StartGame completes on host:
        

        ExitManager emr = ExitManager.Instance;
        if (emr != null)
        {
            emr.BeginExit();
        }

        // 2) Start the exit sequence everywhere
       // RPC_BeginExitSequence(exitDelay);


        //  HostBeginExit();

        //version old
        /*  HostController.instance.RPC_RequestExit(6f);

          float currentNetworkTime = NetworkManager.Instance.Runner.SimulationTime; // Récupérer l'heure réseau actuelle
           ShowThanksgivingCanvasForAll();



              SyncManager.Instance.RPC_StopSynchronization(currentNetworkTime);
              Debug.LogError("Sent synchronization Stop signal to all clients.");

              // Instantiate(thankGivingWindow);


             HostController.instance.RPC_RequestExit(3f);

          //Invoke("QuitApplication", 3f);*/
    }

    public void HostBeginExit()
    {
        if (!Object || !Object.HasStateAuthority)
        {
            Debug.LogWarning("HostBeginExit called on a non-host instance.");
            return;
        }
        // 1) Tell everyone to show the local window
        RPC_ShowThanks(distance, verticalOffset, pinToCamera);

        // 2) Start the exit sequence everywhere
        RPC_BeginExitSequence(exitDelay);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ShowThanks(float dist, float yOffset, bool pin)
    {
        TryShowThanksgivingLocally(dist, yOffset, pin);
    }


    // Client asks host to end session
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestExit(float delaySeconds)
    {
        RPC_BeginExitSequence(delaySeconds);
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]

    private void RPC_BeginExitSequence(float delaySeconds)
    {
        StartCoroutine(StopApplicationAfterDelayCoroutine(delaySeconds));
    }

    // 3) Local exit sequence each client runs on itself.
    private IEnumerator StopApplicationAfterDelayCoroutine(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

    
        // Give users a moment to see the UI, then quit
        yield return new WaitForSeconds(0.5f);

        // Closes the app in standalone builds
        Application.Quit();

    }


    // --- Local-only UI spawn: in front of THIS client’s camera ---
    void TryShowThanksgivingLocally(float dist, float yOffset, bool pin)
    {
        if (thankGivingWindow == null) return;

        // Don’t create twice on the same client
        if (_localThanksInstance != null) return;

        var cam = GetPlayerCamera();
        if (cam == null)
        {
            Debug.LogWarning("[HostController] No camera found on this client; skipping window.");
            return;
        }

        // Position & rotation directly in front of the camera
        Vector3 pos = cam.transform.position + cam.transform.forward.normalized * dist + cam.transform.up * yOffset;
        Quaternion rot = Quaternion.LookRotation((pos - cam.transform.position).normalized, Vector3.up);

        // 🔑 Local instantiate (NOT Runner.Spawn)
        _localThanksInstance = Instantiate(thankGivingWindow, pos, rot);

        // If the prefab is a Canvas, nudge settings for guaranteed visibility
        var canvas = _localThanksInstance.GetComponentInChildren<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            // Keep world-space if you want a 3D panel; otherwise uncomment to force overlay:
            // canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // canvas.worldCamera = cam;
        }

        // Keep it pinned to the camera view if desired
        if (pin)
        {
            _localThanksInstance.transform.SetParent(cam.transform, true);
        }
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
  /*  public IEnumerator StopApplicationAfterDelayCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Instantiate(thankGivingWindow);
        ShowThanksgivingCanvasForAll();

        StoptLogSynchornisation();

         yield return new WaitForSeconds(3.0f);
        Application.Quit();
    }*/
}
