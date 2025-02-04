using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PDFViewer : MonoBehaviour
{
    [Header("Assign PDF File Path in Inspector")]
    public string pdfPath;  // Set the full file path in Unity Inspector
    public GameObject imagePrefab;
    public Transform contentPanel;

    void Start()
    {
        if (string.IsNullOrEmpty(pdfPath))
        {
            Debug.LogError("PDF Path is empty! Please assign the file in the Inspector.");
            return;
        }

        if (!File.Exists(pdfPath))
        {
            Debug.LogError("PDF file not found at: " + pdfPath);
            return;
        }

        LoadPDF(pdfPath);
    }

    void LoadPDF(string path)
    {
        List<Texture2D> pages = PDFToTextures(path);

        foreach (var tex in pages)
        {
            GameObject newImage = Instantiate(imagePrefab, contentPanel);
            newImage.GetComponent<RawImage>().texture = tex;
        }

        AdjustContentSize(pages.Count);
    }

    List<Texture2D> PDFToTextures(string pdfFilePath)
    {
        List<Texture2D> textures = new List<Texture2D>();

        // Load and convert PDF pages to images using PdfiumViewer or another library

        return textures;
    }

    void AdjustContentSize(int pageCount)
    {
        RectTransform rt = contentPanel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, pageCount * 800); // Adjust based on pages
    }
}
