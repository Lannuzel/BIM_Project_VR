using Fusion;
using System;
using System.Collections;
using UnityEngine;

public class ExitManager : NetworkBehaviour
{
    [SerializeField] private float defaultDelay = 2f;

    public static ExitManager Instance { get; private set; }

    // ---------------- Lifecycle ----------------

    public override void Spawned()
    {
        Instance = this;
        
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void BeginExit(float delaySeconds = -1f)
    {
        if (!HasStateAuthority)
        {
            Debug.LogError("[ExitManager] BeginExit called on a client. Use RequestExitFromHost() or call BeginExit on the host.");
            RequestExitFromHost(delaySeconds);
            return; // <- restore this
        }

        if (delaySeconds < 0) delaySeconds = defaultDelay;
        RPC_BeginExit(delaySeconds); // Host-only RPC
    }

    public void RequestExitFromHost(float delaySeconds = -1f)
    {
        if (delaySeconds < 0) delaySeconds = defaultDelay;
        RPC_RequestHostToBeginExit(delaySeconds);
    }

    // Allow any client to ask the host
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestHostToBeginExit(float delaySeconds)
    {
        Debug.LogError($"[ExitManager][HOST] Received exit request. Broadcasting exit in {delaySeconds:0.##}s to all peers.");
        RPC_BeginExit(delaySeconds);
    }

    // Host -> All remains unchanged and host-only
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BeginExit(float delaySeconds)
    {
        Debug.LogError($"[ExitManager][ALL] Exit sequence received. Quitting in {delaySeconds:0.##}s.");
        if (ExitManager.Instance == null)
            ExitManager.Instance = this;

        if (ExitManager.Instance == this)
            ExitManager.Instance.StartCoroutine(ExitAfterDelay(delaySeconds));

      //  Debug.LogError($"[ExitManager][ALL] Exit sequence received. Quitting in {delaySeconds:0.##}s.");
      //  StartCoroutine(ExitAfterDelay(delaySeconds));
    }
private IEnumerator ExitAfterDelay(float delaySeconds)
    {
        // This yield line is REQUIRED.
        yield return new WaitForSeconds(delaySeconds);

#if UNITY_EDITOR
        Debug.Log("[ExitManager] Stopping play mode in Editor.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Debug.Log("[ExitManager] Quitting application (Build).");
    Application.Quit();
#endif
    

}


// --- Impl ---

}
