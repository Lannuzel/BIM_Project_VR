using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountDownWatch : NetworkBehaviour
{
    public GameObject clockPrefab;
    public GameObject interactionMenu;
    public float distance = 1.2f;       // meters in front of the user
    public float verticalOffset = -0.1f; // slightly below gaze
    Fusion.NetworkObject NWClock = null;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void DisplayClock()
    {
        Camera cam = GetPlayerCamera();
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;
        Vector3 up = cam.transform.up;

        Vector3 spawnPos = cam.transform.position + forward.normalized * distance + up * verticalOffset;
        Quaternion spawnRot = Quaternion.LookRotation((spawnPos - cam.transform.position).normalized, Vector3.up);

       

        NetworkManager.Instance.Runner.Spawn(clockPrefab, clockPrefab.transform.position, clockPrefab.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
        {
            NWClock = obj;
            
        });
        NWClock.transform.parent = transform;
        interactionMenu.SetActive(true);

    }

    public void DeleteClock()
    {
        if (NWClock != null)
        {
            NetworkManager.Instance.Runner.Despawn(NWClock);
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
    // Update is called once per frame
    void Update()
    {
        
    }
}
