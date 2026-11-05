using Fusion;
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Fin de session (N5) : tout casque peut demander la fermeture de tous les casques (RPC vers tous),
/// et chaque casque exécute sa propre fermeture locale, quelle que soit la copie d'ExitManager qui reçoit l'ordre.
/// </summary>
public class ExitManager : NetworkBehaviour
{
    [SerializeField] private float defaultDelay = 2f;

    /// <summary>Instance du joueur local.</summary>
    public static ExitManager Instance { get; private set; }

    private static bool exitScheduled;

    public override void Spawned()
    {
        if (Object.HasStateAuthority || Instance == null)
            Instance = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Demande la fermeture de l'application sur tous les casques.</summary>
    public void BeginExit(float delaySeconds = -1f)
    {
        if (delaySeconds < 0) delaySeconds = defaultDelay;

        if (Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning)
        {
            Debug.LogWarning("[ExitManager] Session inactive : fermeture locale seulement.");
            QuitLocallyAfter(delaySeconds);
            return;
        }
        RPC_BeginExit(delaySeconds);
    }

    public void RequestExitFromHost(float delaySeconds = -1f) => BeginExit(delaySeconds);

    // N5 : n'importe quel casque peut émettre, tous exécutent localement (plus de condition Instance == this).
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_BeginExit(float delaySeconds)
    {
        Debug.LogError($"[ExitManager][ALL] Exit sequence received. Quitting in {delaySeconds:0.##}s.");
        QuitLocallyAfter(delaySeconds);
    }

    /// <summary>Ferme l'application de ce casque après un délai (indépendant des objets réseau, qui peuvent être despawnés).</summary>
    public static void QuitLocallyAfter(float delaySeconds)
    {
        if (exitScheduled) return;
        exitScheduled = true;
        NetworkObjectUtils.Instance.StartCoroutine(ExitAfterDelay(delaySeconds));
    }

    private static IEnumerator ExitAfterDelay(float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delaySeconds));

#if UNITY_EDITOR
        Debug.Log("[ExitManager] Stopping play mode in Editor.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Debug.Log("[ExitManager] Quitting application (Build).");
        Application.Quit();
#endif
    }
}
