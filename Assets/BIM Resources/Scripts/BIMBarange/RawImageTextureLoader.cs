using UnityEngine;
using UnityEngine.UI;
using Fusion;
using static Unity.Collections.Unicode;
using Unity.Netcode;
using System.Collections.Generic;
public class RawImageTextureLoader : MonoBehaviour
{
    public RawImage rawImage; // Assign the RawImage in the Inspector
    public List<Texture2D> userDocuments;

    public void Awake()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", $"Player {NetworkManager.Instance.Runner.LocalPlayer.PlayerId}");
        if (playerName == "Lecteur")
        {
            rawImage.texture = userDocuments[0];
        }
        else if (playerName == "Calculateur")
        {
            rawImage.texture = userDocuments[1];
        }
        else if (playerName == "Modelisateur")
        {
            rawImage.texture = userDocuments[2];
        }
     
    }

}