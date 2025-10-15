using Fusion;

using System.Linq;
using UnityEngine;
using TMPro;

using System.Collections;


public class StartActivityManager : NetworkBehaviour
{
    [SerializeField] private GameObject startActivityWindow; // your UI window (not necessarily a NetworkObject)
   

    public float distance = 1.2f;       // meters in front of the user
    public float verticalOffset = -0.1f; // slightly below gaze

    private Fusion.NetworkObject spawnedSAW;

    public static StartActivityManager Local { get; private set; }

    public override void Spawned()
    {
        if (HasInputAuthority) Local = this;
    }

    public void CloseWindow(float delay)
    {
        if (HasStateAuthority) // only the state authority schedules it
            StartCoroutine(DeactivateAfterDelay(delay));
    }
    private IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RPC_DeactivateWindow(); // tell everyone to turn it off
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DeactivateWindow()
    {
        if (startActivityWindow != null)
            startActivityWindow.SetActive(false);
    }


    public void ShowStartActivtyWindowForAll()
    {
        if (startActivityWindow != null)
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

            Debug.LogError("Trying to launch start Activity Window: step1");

            spawnedSAW = null;
            NetworkManager.Instance.Runner.Spawn(startActivityWindow, spawnPos, spawnRot, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
            {
                spawnedSAW = obj;

                Debug.LogError("Trying to launch start Activity Window: step2");

            });
            
            Debug.LogError("Trying to launch start Activity Window: step3");

        }
    }
    IEnumerator DisactivateStartActivtyWindow()
    {
        yield return new WaitForSeconds(2f);
        if (spawnedSAW != null)
        { 
        spawnedSAW.gameObject.SetActive(false);
          }
    }
    private IEnumerator DeactivateAfterDelay(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
        {
            target.SetActive(false);
        }
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
