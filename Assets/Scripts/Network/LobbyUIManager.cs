using TMPro;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lobbyCounterText; // Compteur de joueurs
    [SerializeField] private Canvas gameStartCanvas; // Canvas contenant le bouton
    [SerializeField] private Button startGameButton; // Bouton pour lancer le jeu

    private bool canvasVisible = false; // Pour éviter d'afficher plusieurs fois le Canvas

    void Start()
    {
        NetworkManager networkManager = NetworkManager.Instance;

        if (networkManager != null)
        {
            Debug.Log("NetworkManager found!");

            // Connecter le TextMeshProUGUI
            networkManager.lobbyCounterText = lobbyCounterText;

            // Vérification immédiate
            int playerCount = networkManager.Runner.ActivePlayers.Count();
            networkManager.UpdateLobbyCounter(playerCount, networkManager.requiredPlayers);

            // Lancer une mise à jour continue
            StartCoroutine(CheckPlayerCount());
        }
        else
        {
            Debug.LogError("NetworkManager instance not found!");
        }

        // Ajouter une action au clic du bouton
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        }

        // Masquer le Canvas au démarrage
        if (gameStartCanvas != null)
        {
            gameStartCanvas.gameObject.SetActive(false);
        }
    }

    private System.Collections.IEnumerator CheckPlayerCount()
    {
        while (true)
        {
            NetworkManager networkManager = NetworkManager.Instance;

            if (networkManager != null)
            {
                int playerCount = networkManager.Runner.ActivePlayers.Count();

                if (playerCount >= networkManager.requiredPlayers && !canvasVisible)
                {
                    ShowGameStartCanvas();
                }
            }

            yield return new WaitForSeconds(1);
        }
    }

    private void ShowGameStartCanvas()
    {
        if (gameStartCanvas != null)
        {
            gameStartCanvas.gameObject.SetActive(true); // Afficher le Canvas
            canvasVisible = true; // Empêcher plusieurs affichages
        }
    }

    public void OnStartGameButtonClicked()
    {
        NetworkManager networkManager = NetworkManager.Instance;
        if (gameStartCanvas != null)
        {
            gameStartCanvas.gameObject.SetActive(false); // Cacher le Canvas après clic
        }

        Debug.Log("Start Game Button clicked! Starting countdown...");
        NetworkManager.Instance.StartCountdown();
    }
}