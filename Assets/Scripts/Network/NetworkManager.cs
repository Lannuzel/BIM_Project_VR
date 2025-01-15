using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro; // Import TextMeshPro
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // Singleton
    public static NetworkManager Instance { get; private set; }

    [SerializeField]
    private GameObject _runnerPrefab;

    public NetworkRunner Runner { get; private set; }

    public TextMeshProUGUI lobbyCounterText; // TextMeshProUGUI pour afficher les joueurs
    [SerializeField] public int requiredPlayers = 3; // Nombre de joueurs nécessaires pour lancer le compte à rebours
    private bool countdownStarted = false; // Pour éviter de démarrer plusieurs fois le compte à rebours

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Start()
    {
        // Fixing the server to a particular region
        Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.FixedRegion = "eu";
    }

    public async void CreateSession(string roomCode)
    {
        CreateRunner();
       // await LoadScene();
        await Connect(roomCode);
    }

    public async void JoinSession(string roomCode)
    {
        CreateRunner();
      //  await LoadScene();
        await Connect(roomCode);
    }

    public void CreateRunner()
    {
        Runner = Instantiate(_runnerPrefab, transform).GetComponent<NetworkRunner>();
        Runner.AddCallbacks(this);
    }

    public async Task LoadScene()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1); // Load scene 1 (lobby)

        while (!asyncLoad.isDone)
        {
            await Task.Yield();
        }
    }

    private async Task Connect(string sessionName)
    {
        var args = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            SceneManager = GetComponent<NetworkSceneManagerDefault>(),
           // Scene = SceneRef.FromIndex(1)
        };
        await Runner.StartGame(args);
    }

    #region INetworkRunnerCallbacks
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        int playerCount = Runner.ActivePlayers.Count();
        UpdateLobbyCounter(playerCount, requiredPlayers);
        Debug.Log($"Player joined: {player.PlayerId}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        int playerCount = Runner.ActivePlayers.Count();
        UpdateLobbyCounter(playerCount, requiredPlayers);
        Debug.Log($"Player left: {player.PlayerId}");
    }

    public void UpdateLobbyCounter(int playerCount, int requiredPlayers)
    {
        if (lobbyCounterText != null)
        {
            lobbyCounterText.text = $"Players: {playerCount}/{requiredPlayers}";
        }
    }

    private void UpdatePlayerCount()
    {
        int playerCount = Runner.ActivePlayers.Count();
        UpdateLobbyCounter(playerCount, requiredPlayers);

        // if (playerCount >= requiredPlayers && !countdownStarted)
        // {
        //     StartCoroutine(StartCountdown());
        // }
    }

    public void StartCountdown()
    {
        StartCoroutine(StartCountdownCoroutine());
    }

    private IEnumerator StartCountdownCoroutine()
    {
        int countdown = 10; // Durée du compte à rebours en secondes

        while (countdown > 0)
        {
            if (lobbyCounterText != null)
                lobbyCounterText.text = $"Game starting in {countdown}...";
            yield return new WaitForSeconds(1);
            countdown--;
        }

        if (lobbyCounterText != null)
            lobbyCounterText.text = "Starting...";

      //  LoadNextScene();
        StartTrackers();
    }

    private void StartTrackers()
    {
        Debug.Log("Trackers launched for all players!");

        // Appeler ici les méthodes qui activent les trackers pour chaque utilisateur.
        // Exemple :
        foreach (var player in Runner.ActivePlayers)
        {
            Debug.Log($"Tracker started for player {player.PlayerId}");
            // Insérer ici le code pour initialiser les trackers.
        }
    }

    private async void LoadNextScene()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(2); // Charge la scène 2

        while (!asyncLoad.isDone)
        {
            await Task.Yield();
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("Runner Shutdown: " + shutdownReason);
    }
    #endregion

    #region INetworkRunnerCallbacks (Unused)
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    #endregion
}
