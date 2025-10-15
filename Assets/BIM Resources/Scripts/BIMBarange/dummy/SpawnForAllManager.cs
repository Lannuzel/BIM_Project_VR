using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class SpawnForAllManager : NetworkBehaviour
{
    [Header("Spawned Network Prefab")]
    [Tooltip("Prefab MUST have a NetworkObject. Drag your networked prefab here.")]
    public NetworkObject networkPrefabToSpawn;

    [Header("UI (optional)")]
    public Button goButton;

    [Header("Placement")]
    [Tooltip("Meters in front of each player's avatar transform.")]
    public float distance = 2f;
    [Tooltip("Vertical offset from avatar position.")]
    public float verticalOffset = 0.0f;

    // Optional: enforce singleton
    private static SpawnForAllManager _instance;

    public override void Spawned()
    {

        // (Optional) enforce only one manager
        if (_instance != null && _instance != this)
        {
            if (Object && Object.HasStateAuthority) Runner.Despawn(Object);
            else gameObject.SetActive(false);
            return;
        }
        _instance = this;
        Debug.LogError("called Spawned inside spawnforallmanager");
        RequestSpawnForAll_RPC();
    }


    public void OnGoClicked()
    {
        // Any client can request the host to spawn for everyone
        RequestSpawnForAll_RPC();
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RequestSpawnForAll_RPC(RpcInfo info = default)
    {
        if (!Object || !Object.HasStateAuthority) return;
        if (Runner == null || networkPrefabToSpawn == null) return;

        foreach (var player in Runner.ActivePlayers)
        {
            var t = GetPlayerTransform(player);
            if (t == null) continue;

            Vector3 fwd = t.forward.sqrMagnitude < 1e-4f ? Vector3.forward : t.forward.normalized;
            Vector3 spawnPos = t.position + fwd * distance;
            spawnPos.y = 0f; // keep your ground clamp if you want
            Quaternion spawnRot = Quaternion.LookRotation(fwd, Vector3.up);

            // Give authority to that player (or pass null if you don’t want per-player authority)
            Runner.Spawn(networkPrefabToSpawn, spawnPos, spawnRot, player);
        }
    }

    private Transform GetPlayerTransform(PlayerRef player)
    {
        if (Runner.TryGetPlayerObject(player, out var playerObj) && playerObj)
            return playerObj.transform;

        // Fallback if you didn’t set PlayerObject:
        foreach (var pc in FindObjectsOfType<PlayerController>())
            if (pc && pc.Object && pc.Object.InputAuthority == player)
                return pc.transform;

        return null;
    }
    // ✅ Allow ANY caller to reach StateAuthority (host)
    /*   [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
       private void RequestSpawnForAll_RPC()
       {
           Debug.LogError("trying to call RPC1");
           if (Runner == null || networkPrefabToSpawn == null)
               return;
           Debug.LogError("trying to call RPC1 runner found");
           // Find every PlayerController (your player prefab already has this) and spawn in front of each
           var players = FindObjectsOfType<PlayerController>(); // uses your PlayerController with Networked PlayerName, etc.
           Debug.LogError("trying to call RPC1 : checking for players");

               Transform cameraTransform = Camera.main.transform;
               Vector3 spawnPos = cameraTransform.transform.position + cameraTransform.forward * distance;
                spawnPos.y = 0;
           Quaternion spawnRot = Quaternion.LookRotation(cameraTransform.forward, Vector3.up);

               // Give input authority to that player (optional). Pass null if you don’t want per-player authority.
               NetworkManager.Instance.Runner.Spawn(
                   networkPrefabToSpawn,
                   spawnPos,
                   spawnRot,
                   NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
                   { 
                   }
               );

            Debug.Log($"Managers alive: {FindObjectsOfType<SpawnForAllManager>().Length}");


       }
   */


}
