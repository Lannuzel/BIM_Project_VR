using UnityEngine;
using UnityEngine.UI;
using Fusion;
using static Unity.Collections.Unicode;
using Unity.Netcode;
public class RawImageTextureLoader : MonoBehaviour
{
    public RawImage rawImage; // Assign the RawImage in the Inspector
    public string texturePath = "Assets/BIM Resources/Images/"; // Path to the textures folder

    private void Start()
    {

        if (rawImage == null)
        {
            Debug.LogError("RawImage is not assigned.");
        }
    }

    public void Awake()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", $"Player {NetworkManager.Instance.Runner.LocalPlayer.PlayerId}");

        // Specify the texture name (this could be dynamic based on user input or other logic)
        string textureName = playerName + "Info.png"; // Replace with the desired texture name

        // Load the texture
        Texture2D texture = LoadTexture(textureName);
        if (texture != null)
        {
            // Assign the texture to the RawImage
            rawImage.texture = texture;
            Debug.Log("Texture successfully loaded and assigned: " + textureName);
        }
        else
        {
            Debug.LogError("Failed to load texture: " + textureName);
        }
    }

    private Texture2D LoadTexture(string textureName)
    {
        // Full path to the texture
        string fullPath = texturePath + textureName;

        // Load the texture from file
        byte[] fileData;
        if (System.IO.File.Exists(fullPath))
        {
            fileData = System.IO.File.ReadAllBytes(fullPath);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(fileData)) // Automatically resizes the texture
            {
                return texture;
            }
        }

        return null;
    }
}