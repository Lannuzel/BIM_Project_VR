using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField]
    private NetworkPrefabRef playerPrefab;

    /// <summary>Prefab de l'avatar (F18 : utilisé aussi pour les joueurs simulés).</summary>
    public NetworkPrefabRef PlayerPrefab => playerPrefab;

    // Dictionary of spawned user prefabs, to destroy them on disconnection
    private Dictionary<PlayerRef, NetworkObject> _spawnedUsers = new Dictionary<PlayerRef, NetworkObject>();

    void Start()
    {
        var runner = GetComponent<NetworkRunner>();
        if (runner == null && NetworkManager.Instance != null)
            runner = NetworkManager.Instance.Runner;
        if (runner != null)
            runner.AddCallbacks(this);
    }

    #region INetworkRunnerCallbacks
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // N5 : en mode Shared, OnPlayerJoined est appelé sur chaque casque pour chaque joueur.
        // Chaque casque ne crée que SON avatar (sinon 3 joueurs = 9 avatars superposés).
        if (player != runner.LocalPlayer)
            return;

        if (playerPrefab == null || _spawnedUsers.ContainsKey(player))
            return;

        Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(1, 5), 0.5f, UnityEngine.Random.Range(1, 5));

        // Spawner le joueur local
        NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

        // Attribuer le nom (rôle) du joueur local ; PlayerName est [Networked] et se réplique chez les autres.
        var playerController = networkPlayerObject.GetComponent<PlayerController>();
        if (playerController != null)
        {
            string playerName = NetworkManager.GetLocalPlayerName();
            Debug.Log($"Nom récupéré pour le joueur {player.PlayerId} : {playerName}");
            playerController.PlayerName = playerName;
        }

        // Suivi des joueurs connectés
        _spawnedUsers.Add(player, networkPlayerObject);
        StartCoroutine(WaitForPlayerName(runner, player, networkPlayerObject));
    }

    private IEnumerator WaitForPlayerName(NetworkRunner runner, PlayerRef player, NetworkObject networkPlayerObject)
    {
        while (string.IsNullOrEmpty(PlayerPrefs.GetString("PlayerName")))
        {
            yield return null; // Attendre une frame
        }

        if (networkPlayerObject == null || !networkPlayerObject.IsValid)
            yield break;

        string playerName = NetworkManager.GetLocalPlayerName();
        var playerController = networkPlayerObject.GetComponent<PlayerController>();
        if (playerController != null && playerController.Object != null && playerController.Object.HasStateAuthority)
        {
            playerController.PlayerName = playerName;
            Debug.Log($"Nom final attribué : {playerName}");
        }
    }


    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Seul l'avatar local est dans ce dictionnaire ; les avatars des autres sont détruits par Fusion
        // (flag DestroyWhenStateAuthorityLeaves du prefab joueur).
        if (_spawnedUsers.TryGetValue(player, out NetworkObject networkObject))
        {
            if (networkObject != null && networkObject.IsValid && networkObject.HasStateAuthority)
                runner.Despawn(networkObject);
            _spawnedUsers.Remove(player);
        }
    }
    #endregion

    #region Unsed INetworkRunnerCallbacks
    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {

    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {

    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {

    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }



    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {

    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {

    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {

    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {

    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {

    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {

    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {

    }
    #endregion

}
