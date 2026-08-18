using UnityEngine;
using UnityEngine.UI;

public class DisplayImage : MonoBehaviour
{
    [Header("UI Elements")]
    private string imageName = "lecteurInfo"; // Image name without file extension.
    public RawImage displayImage; // RawImage to display the image.

    void Start()
    {
        // Optionally call DisplaySelectedImage here if you want the image to load on start.
        // DisplaySelectedImage();
    }

    // Method to display the image when called.
    public void DisplaySelectedImage()
    {
        if (string.IsNullOrEmpty(imageName))
        {
            Debug.LogError("Image name cannot be empty.");
            return;
        }

        // Load the image from the Resources folder.
        Texture2D texture = Resources.Load<Texture2D>(imageName);

        if (texture == null)
        {
            Debug.LogError($"Image not found in Resources folder with name: {imageName}");
            return;
        }

        // Create a new Texture2D to ensure compatibility with RawImage.
        Texture2D compatibleTexture = new Texture2D(texture.width, texture.height, texture.format, false);
        compatibleTexture.SetPixels(texture.GetPixels());
        compatibleTexture.Apply();

        // Assign the texture to the RawImage.
        displayImage.texture = compatibleTexture;
        displayImage.enabled = true; // Ensure the RawImage is visible.
    }
}
