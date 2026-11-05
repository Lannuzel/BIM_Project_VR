using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;
using System;
using CleanLaboratory.Gameplay;

namespace CleanLaboratory.UI
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private TMP_InputField PseudoField, IPField, PortField;

        /// <summary>
        /// Starts the host using the given connection data.
        /// </summary>
        public void StartHost()
        {
            SetUtpConnectionData();
            var result = Unity.Netcode.NetworkManager.Singleton.StartHost();
            if (result)
            {
                Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene("Playground", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        /// <summary>
        /// Starts the Client using the given connection data.
        /// </summary>
        public void StartClient()
        {
            SetUtpConnectionData();
            var result = Unity.Netcode.NetworkManager.Singleton.StartClient();
        }

        /// <summary>
        /// Use sanitized IP and Port to set up the connection.
        /// </summary>
        void SetUtpConnectionData()
        {
            var sanitizedIPText = SanitizeAlphaNumeric(IPField.text);
            var sanitizedPortText = SanitizeAlphaNumeric(PortField.text);
            if (IPField.text == "")
            {
                sanitizedIPText = "127.0.0.1";
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
    }
}