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

    [Header("Reconnexion (N4)")]
    [Tooltip("Nombre de tentatives de reconnexion après une perte de connexion.")]
    [SerializeField] private int reconnectAttempts = 5;
    [Tooltip("Délai (s) entre deux tentatives de reconnexion.")]
    [SerializeField] private float reconnectDelay = 4f;

    /// <summary>Vrai tant que le runner existe et que la session est active.</summary>
    public bool IsRunning => Runner != null && Runner.IsRunning;

    /// <summary>Vrai pendant une tentative de reconnexion automatique.</summary>
    public bool IsReconnecting { get; private set; }

    /// <summary>Déclenché après chaque création de runner (connexion initiale ou reconnexion).</summary>
    public event Action<NetworkRunner> RunnerCreated;

    /// <summary>Déclenché quand la connexion est perdue (raison lisible).</summary>
    public event Action<string> ConnectionLost;

    /// <summary>Déclenché quand la reconnexion a réussi.</summary>
    public event Action Reconnected;

    // Objets qui doivent recevoir les callbacks de chaque runner (y compris après reconnexion).
    private readonly List<INetworkRunnerCallbacks> _persistentCallbacks = new List<INetworkRunnerCallbacks>();

    private string _sessionName;
    private bool _wantConnection;      // la session a été demandée par l'utilisateur
    private bool _handlingLoss;        // évite de traiter deux fois le même arrêt
    private Coroutine _reconnectRoutine;

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

    /// <summary>
    /// Nom du joueur local (rôle choisi dans le menu déroulant), sans dépendre de l'état du runner.
    /// À utiliser à la place de PlayerPrefs.GetString("PlayerName", $"Player {Runner.LocalPlayer.PlayerId}").
    /// </summary>
    public static string GetLocalPlayerName()
    {
        string name = PlayerPrefs.GetString("PlayerName", string.Empty);
        if (!string.IsNullOrEmpty(name))
            return name;

        var runner = Instance != null ? Instance.Runner : null;
        if (runner != null && runner.IsRunning)
            return $"Player {runner.LocalPlayer.PlayerId}";

        return "Player";
    }

    /// <summary>
    /// Enregistre un récepteur de callbacks qui sera ré-attaché à chaque nouveau runner (reconnexion).
    /// </summary>
    public void RegisterPersistentCallbacks(INetworkRunnerCallbacks callbacks)
    {
        if (callbacks == null || _persistentCallbacks.Contains(callbacks))
            return;

        _persistentCallbacks.Add(callbacks);
        if (Runner != null)
            Runner.AddCallbacks(callbacks);
    }

    public void UnregisterPersistentCallbacks(INetworkRunnerCallbacks callbacks)
    {
        _persistentCallbacks.Remove(callbacks);
        if (Runner != null)
            Runner.RemoveCallbacks(callbacks);
    }

    public async void CreateSession(string roomCode)
    {
        _sessionName = roomCode;
        _wantConnection = true;
        CreateRunner();
       // await LoadScene();
        await Connect(roomCode);
    }

    public async void JoinSession(string roomCode)
    {
        _sessionName = roomCode;
        _wantConnection = true;
        CreateRunner();
      //  await LoadScene();
        await Connect(roomCode);
    }

    public void CreateRunner()
    {
        Runner = Instantiate(_runnerPrefab, transform).GetComponent<NetworkRunner>();
        Runner.AddCallbacks(this);
        foreach (var cb in _persistentCallbacks)
        {
            if (cb != null)
                Runner.AddCallbacks(cb);
        }
        _handlingLoss = false;
        RunnerCreated?.Invoke(Runner);
    }

    public async Task LoadScene()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1); // Load scene 1 (lobby)

        while (!asyncLoad.isDone)
        {
            await Task.Yield();
        }
    }

    private async Task<bool> Connect(string sessionName)
    {
        var args = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            SceneManager = GetComponent<NetworkSceneManagerDefault>(),
           // Scene = SceneRef.FromIndex(1)
        };

        StartGameResult result;
        try
        {
            result = await Runner.StartGame(args);
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManager] StartGame exception : {e}");
            return false;
        }

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkManager] Échec de connexion à la session '{sessionName}' : {result.ShutdownReason} {result.ErrorMessage}");
            return false;
        }

        Debug.Log($"[NetworkManager] Connecté à la session '{sessionName}' (joueur local {Runner.LocalPlayer.PlayerId}).");
        return true;
    }

    #region Perte de connexion / reconnexion (N4)

    private void HandleConnectionLost(string reason)
    {
        if (_handlingLoss)
            return;
        _handlingLoss = true;

        Debug.LogError($"[NetworkManager] Connexion réseau perdue : {reason}");
        ConnectionLost?.Invoke(reason);

        if (!_wantConnection)
            return;

        if (_reconnectRoutine != null)
            StopCoroutine(_reconnectRoutine);
        _reconnectRoutine = StartCoroutine(ReconnectRoutine(reason));
    }

    private IEnumerator ReconnectRoutine(string reason)
    {
        IsReconnecting = true;
        NetworkStatusPanel.Show($"Connexion réseau perdue ({reason}).\nReconnexion en cours...");

        for (int attempt = 1; attempt <= reconnectAttempts; attempt++)
        {
            yield return new WaitForSeconds(reconnectDelay);

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                NetworkStatusPanel.Show($"Aucun réseau détecté.\nVérifiez le Wi-Fi du casque.\nTentative {attempt}/{reconnectAttempts}...");
                continue;
            }

            NetworkStatusPanel.Show($"Reconnexion à la session...\nTentative {attempt}/{reconnectAttempts}");

            // L'ancien runner est arrêté : Fusion l'a déjà détruit (ou va le faire).
            if (Runner != null && !Runner.IsShutdown)
            {
                var shutdownTask = Runner.Shutdown();
                while (!shutdownTask.IsCompleted)
                    yield return null;
            }

            CreateRunner();
            var connectTask = Connect(_sessionName);
            while (!connectTask.IsCompleted)
                yield return null;

            if (connectTask.Status == TaskStatus.RanToCompletion && connectTask.Result && IsRunning)
            {
                IsReconnecting = false;
                _reconnectRoutine = null;
                NetworkStatusPanel.Show("Connexion rétablie.", 4f);
                Reconnected?.Invoke();
                yield break;
            }
        }

        IsReconnecting = false;
        _reconnectRoutine = null;
        NetworkStatusPanel.Show("Impossible de rétablir la connexion réseau.\nLes enregistrements locaux continuent.\nPrévenez l'expérimentateur.");
    }

    #endregion

    #region INetworkRunnerCallbacks
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        int playerCount = runner.ActivePlayers.Count();
        UpdateLobbyCounter(playerCount, requiredPlayers);
        Debug.Log($"Player joined: {player.PlayerId}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        int playerCount = runner.ActivePlayers.Count();
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
        if (!IsRunning)
            return;

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

        if (!IsRunning)
            return;

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

        // Arrêt volontaire (fin de session, quitter l'application) : rien à faire.
        if (shutdownReason == ShutdownReason.Ok || !_wantConnection)
            return;

        HandleConnectionLost(shutdownReason.ToString());
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogError("Disconnected from server: " + reason);
        HandleConnectionLost(reason.ToString());
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"Connect failed ({remoteAddress}): {reason}");
        HandleConnectionLost(reason.ToString());
    }
    #endregion

    #region INetworkRunnerCallbacks (Unused)
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
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
