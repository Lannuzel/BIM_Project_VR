using Photon.Pun;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings(); // Connexion au serveur Photon
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby(); // Rejoindre un lobby après connexion
        Debug.Log("Connected to Photon!");
    }

    public override void OnJoinedLobby()
    {
        PhotonNetwork.JoinOrCreateRoom("VRRoom", new Photon.Realtime.RoomOptions { MaxPlayers = 10 }, null);
        Debug.Log("Joined a room!");
    }
}
