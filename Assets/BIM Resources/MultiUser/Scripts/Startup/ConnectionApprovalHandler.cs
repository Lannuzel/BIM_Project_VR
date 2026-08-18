using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Connection Approval Handler Component
/// </summary>
/// <remarks>
/// This should be placed on the same GameObject as the NetworkManager.
/// It automatically declines the client connection for example purposes.
/// </remarks>
public class ConnectionApprovalHandler : MonoBehaviour
{
    private Unity.Netcode.NetworkManager m_NetworkManager;

    public int MaxNumberOfPlayers = 6;
    private int _numberOfPlayers = 0;

    private void Start()
    {
        m_NetworkManager = GetComponent<Unity.Netcode.NetworkManager>();
        if (m_NetworkManager != null)
        {
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            m_NetworkManager.ConnectionApprovalCallback = CheckApprovalCallback;
        }
        if(MaxNumberOfPlayers == 0)
        {
            MaxNumberOfPlayers++;
        }
    }

    private void CheckApprovalCallback(Unity.Netcode.NetworkManager.ConnectionApprovalRequest request, Unity.Netcode.NetworkManager.ConnectionApprovalResponse response)
    {
        bool isApproved = true;
        _numberOfPlayers++;
        //// The client identifier to be authenticated
        //var clientId = request.ClientNetworkId;
        //
        //// Additional connection data defined by user code
        //var connectionData = request.Payload;

        // Your approval logic determines the following values
        if (_numberOfPlayers > MaxNumberOfPlayers)
        {
            isApproved = false;
            response.Reason = "Too many players in lobby!";
        }
        response.Approved = isApproved;
        response.CreatePlayerObject = isApproved;
        //response.PlayerPrefabHash = null;
        response.Position = new Vector3(0,3,0);
        //response.Rotation = Quaternion.identity;
        //response.Pending = false;
    }

    private void OnClientDisconnectCallback(ulong obj)
    {
        if (!m_NetworkManager.IsServer && m_NetworkManager.DisconnectReason != string.Empty && !m_NetworkManager.IsApproved)
        {
            Debug.Log($"Approval Declined Reason: {m_NetworkManager.DisconnectReason}");
        }
        _numberOfPlayers--;
    }
}