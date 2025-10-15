using Fusion;
using System.Collections;
using UnityEngine;

public class ExitManager : NetworkBehaviour
{
    [SerializeField] private float defaultDelay = 2f;

    // --- PUBLIC API ---

    /// <summary>
    /// Call on the host to exit everyone.
    /// </summary>
    /// 
    public override void Spawned()
    {
        
    }

    public void BeginExit(float delaySeconds = -1f)
    {
        if (!HasStateAuthority)
        {
            Debug.LogError("[ExitManager] BeginExit called on a client. Use RequestExitFromHost() or call BeginExit on the host.");
            RequestExitFromHost();
          //  return;
        }

        if (delaySeconds < 0) delaySeconds = defaultDelay;
        RPC_BeginExit(delaySeconds);
    }

    /// <summary>
    /// Call on a client to ask the host to start the exit sequence.
    /// </summary>
    public void RequestExitFromHost(float delaySeconds = -1f)
    {
        if (delaySeconds < 0) delaySeconds = defaultDelay;
        RPC_RequestHostToBeginExit(delaySeconds);
    }

    // --- RPCs ---

    // Client -> Host: ask host to start exit
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestHostToBeginExit(float delaySeconds)
    {
        Debug.LogError($"[ExitManager][HOST] Received exit request. Broadcasting exit in {delaySeconds:0.##}s to all peers.");
        RPC_BeginExit(delaySeconds);
    }

    // Host -> All: tell everyone to exit
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BeginExit(float delaySeconds)
    {
        Debug.LogError($"[ExitManager][ALL] Exit sequence received. Quitting in {delaySeconds:0.##}s.");
        // Use 'this' component on every peer; do not rely on a static instance here.
        StartCoroutine(ExitAfterDelay(delaySeconds));
    }

    // --- Impl ---

    private IEnumerator ExitAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

#if UNITY_EDITOR
        Debug.LogError("[ExitManager] Stopping play mode (Editor).");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Debug.Log("[ExitManager] Quitting application (Build).");
        Application.Quit();
#endif
    }
}
