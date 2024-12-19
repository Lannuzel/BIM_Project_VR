using TMPro;
using UnityEngine;
using System.Linq;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI lobbyCounterText;

    void Start()
    {
        // Récupérer le NetworkManager dans la scène persistante
        NetworkManager networkManager = NetworkManager.Instance;

        if (networkManager != null)
        {
            // Lier le TextMeshProUGUI de la deuxième scène au NetworkManager
            networkManager.lobbyCounterText = lobbyCounterText;

            // Mettre à jour immédiatement le compteur
            int playerCount = networkManager.Runner.ActivePlayers.Count();
            networkManager.UpdateLobbyCounter(playerCount, networkManager.requiredPlayers);
        }
        else
        {
            Debug.LogError("NetworkManager instance not found!");
        }
    }
}
