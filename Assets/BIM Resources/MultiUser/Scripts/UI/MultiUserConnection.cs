using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;
using System;
using CleanLaboratory.Gameplay;
using UnityEngine.SceneManagement;
using Unity.Netcode.Samples;

public class MultiUserConnection : MonoBehaviour
{
    [SerializeField] private TMP_InputField PseudoField, IPField, PortField;
    public Transform playArea; 
    Unity.Netcode.NetworkManager networkManager = Unity.Netcode.NetworkManager.Singleton;

    public void StartHost()
    {
        SetUtpConnectionData();
        var result = Unity.Netcode.NetworkManager.Singleton.StartHost();
        if (result)
        {
            playArea.gameObject.SetActive(true);
            Debug.Log("Host is up now");
            // Unity.Netcode.NetworkManager.Singleton.SceneManager.L LoadScene("Playground", UnityEngine.SceneManagement.LoadSceneMode.Single);
            // SceneManager.LoadSceneAsync(2);
            initUser();

        }
    }

    /// <summary>
    /// Starts the Client using the given connection data.
    /// </summary>
    public void StartClient()
    {
        SetUtpConnectionData();
        var result = Unity.Netcode.NetworkManager.Singleton.StartClient();
        if (result)
        {
            playArea.gameObject.SetActive(true);
        }
        Debug.Log("Client is up now");
        initUser();
    }

    void SetUtpConnectionData()
    {
        var sanitizedIPText = SanitizeAlphaNumeric(IPField.text);
        var sanitizedPortText = SanitizeAlphaNumeric(PortField.text);
        if (IPField.text == "")
        {
            //  sanitizedIPText = "127.0.0.1";
            sanitizedIPText = "0.0.0.0";//      "192.168.1.10"; // "192.168.1.160";

        }
        if (PortField.text == "")
        {
            sanitizedPortText = "4242";
        }

        ushort.TryParse(sanitizedPortText, out var port);
        var utp = (UnityTransport)Unity.Netcode.NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        utp.SetConnectionData(sanitizedIPText, port);
    }

    /// <summary>
    /// Sanitize user port InputField box allowing only alphanumerics and '.'
    /// </summary>
    /// <param name="dirtyString"> string to sanitize. </param>
    /// <returns> Sanitized text string. </returns>
    static string SanitizeAlphaNumeric(string dirtyString)
    {
        return Regex.Replace(dirtyString, "[^A-Za-z0-9.]", "");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void initUser()
    {
        networkManager = Unity.Netcode.NetworkManager.Singleton;
        if (networkManager.IsClient)
        {
                if (networkManager.LocalClient != null)
                {
                    // Get `BootstrapPlayer` component from the player's `PlayerObject`
                    if (networkManager.LocalClient.PlayerObject.TryGetComponent(out BootstrapPlayer bootstrapPlayer))
                    {
                        // Invoke a `ServerRpc` from client-side to teleport player to a random position on the server-side
                        bootstrapPlayer.RandomTeleportServerRpc();
                    }
                }
           
        }
    }
}
