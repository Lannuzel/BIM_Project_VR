using Fusion;

using System.Linq;
using UnityEngine;
using TMPro;

using System.Collections;


/// <summary>
/// Fenêtre « Vous pouvez maintenant commencer l'activité » (N5) : affichée localement devant la caméra
/// de chaque casque (RPC vers tous, instanciation locale), au lieu d'un objet réseau posé devant le Lecteur.
/// </summary>
public class StartActivityManager : NetworkBehaviour
{
    [SerializeField] private GameObject startActivityWindow; // your UI window (not necessarily a NetworkObject)

    public float distance = 1.2f;       // meters in front of the user
    public float verticalOffset = -0.1f; // slightly below gaze
    [Tooltip("Durée (s) d'affichage de la fenêtre sur chaque casque.")]
    public float localLifetime = 3f;

    private GameObject localWindow;

    public static StartActivityManager Local { get; private set; }

    public override void Spawned()
    {
        Local = this;
    }

    public void CloseWindow(float delay)
    {
        RPC_DeactivateWindow(delay);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_DeactivateWindow(float delay)
    {
        StartCoroutine(DeactivateAfterDelay(localWindow, delay));
    }

    /// <summary>Bouton du Lecteur : chaque casque affiche la fenêtre devant lui.</summary>
    public void ShowStartActivtyWindowForAll()
    {
        if (Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning)
        {
            ShowLocally();
            return;
        }
        RPC_ShowWindow();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ShowWindow()
    {
        ShowLocally();
    }

    private void ShowLocally()
    {
        if (startActivityWindow == null) return;

        Camera cam = GetPlayerCamera();
        if (cam == null)
        {
            Debug.LogError("[StartActivityManager] No player camera found on this client.");
            return;
        }

        Vector3 spawnPos = cam.transform.position + cam.transform.forward.normalized * distance + cam.transform.up * verticalOffset;
        Quaternion spawnRot = Quaternion.LookRotation((spawnPos - cam.transform.position).normalized, Vector3.up);

        if (localWindow != null) Destroy(localWindow);
        localWindow = Instantiate(startActivityWindow, spawnPos, spawnRot);
        // Copie locale : les composants réseau du prefab sont inertes.
        foreach (var nb in localWindow.GetComponentsInChildren<NetworkBehaviour>(true)) nb.enabled = false;
        var no = localWindow.GetComponent<NetworkObject>();
        if (no != null) no.enabled = false;

        StartCoroutine(DeactivateAfterDelay(localWindow, localLifetime));
    }

    private IEnumerator DeactivateAfterDelay(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
            Destroy(target);
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

}
