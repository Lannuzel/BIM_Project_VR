using Paroxe.PdfRenderer;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class PdfDocumentLoader : MonoBehaviour
{
    public RawImage rawImage; // Assign the RawImage in the Inspector
    public List<Texture2D> userDocuments;
    public List<string> pdfRoleDocs;
    public string testPdffile;

    public void Awake()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", $"Player {NetworkManager.Instance.Runner.LocalPlayer.PlayerId}");
        if (playerName == "Lecteur")
        {
            ShowPdfFromStreamingAssets(pdfRoleDocs[0]);
        }
        else if (playerName == "Calculateur")
        {
            ShowPdfFromStreamingAssets(pdfRoleDocs[1]);
        }
        else if (playerName == "Modelisateur")
        {
            ShowPdfFromStreamingAssets(pdfRoleDocs[2]);
        }


    }


    public void ShowPdfFromStreamingAssets(string fileName)
    {
        StartCoroutine(LoadAndOpenPdf(fileName));
    }
    private IEnumerator LoadAndOpenPdf(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("PDF file name is null/empty.");
            yield break;
        }

        // IMPORTANT: fileName MUST include the extension and correct case (Android is case-sensitive)
        string src = Path.Combine(Application.streamingAssetsPath, fileName);
        string dst = Path.Combine(Application.persistentDataPath, fileName);

        // If not already copied to persistentDataPath, fetch it from StreamingAssets
        if (!File.Exists(dst))
        {
            string url = src;
            using (var req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    Debug.LogError($"Failed to read PDF from StreamingAssets:\n{url}\n{req.error}");
                    yield break;
                }

                byte[] bytes = req.downloadHandler.data;

                // Ensure directory exists (in case you organize PDFs in subfolders)
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                File.WriteAllBytes(dst, bytes);
            }
        }
        var pdfViewer = gameObject.GetComponentInChildren<Paroxe.PdfRenderer.PDFViewer>();
        // Open with Paroxe
        if (pdfViewer != null)
        {
            // Easiest: let viewer load from file path in persistentDataPath
            pdfViewer.LoadDocumentFromFile(dst);
        }
        else
        {
            // Alternative: load via PDFDocument bytes and pass to a viewer later
            byte[] bytes = File.ReadAllBytes(dst);
            var doc = new PDFDocument(bytes);          // or: new PDFDocument(dst);
            var viewer = FindObjectOfType<Paroxe.PdfRenderer.PDFViewer>(); // fallback
            if (viewer != null) viewer.LoadDocument(doc);
        }
    }







    public void Awake1()
    {

        string fullPath = Path.Combine(UnityEngine.Application.streamingAssetsPath, testPdffile);
        if (File.Exists(fullPath))
        {
            var viewer = gameObject.GetComponentInChildren<Paroxe.PdfRenderer.PDFViewer>();
            if (viewer != null)
            {
                viewer.FilePath = fullPath;
                viewer.LoadDocumentFromFile(fullPath, null);
            }
        }


        /*  string playerName = PlayerPrefs.GetString("PlayerName", $"Player {NetworkManager.Instance.Runner.LocalPlayer.PlayerId}");
          if (playerName == "Lecteur")
          {
              string fullPath = Path.Combine(UnityEngine.Application.streamingAssetsPath, pdfRoleDocs[0]);
              if (File.Exists(fullPath))
              {
                  var viewer = gameObject.GetComponentInChildren<Paroxe.PdfRenderer.PDFViewer>();
                  if (viewer != null)
                  {
                      viewer.FilePath = fullPath;
                      viewer.LoadDocumentFromFile(fullPath, null);
                  }
              }


          }
          else if (playerName == "Calculateur")
          {
              string fullPath = Path.Combine(UnityEngine.Application.streamingAssetsPath, pdfRoleDocs[1]);
              if (File.Exists(fullPath))
              {
                  var viewer = gameObject.GetComponentInChildren<Paroxe.PdfRenderer.PDFViewer>();
                  if (viewer != null)
                  {
                      viewer.FilePath = fullPath;
                      viewer.LoadDocumentFromFile(fullPath, null);
                  }
              }
          }
          else if (playerName == "Modelisateur")
          {
              string fullPath = Path.Combine(UnityEngine.Application.streamingAssetsPath, pdfRoleDocs[2]);
              if (File.Exists(fullPath))
              {
                  var viewer = gameObject.GetComponentInChildren<Paroxe.PdfRenderer.PDFViewer>();
                  if (viewer != null)
                  {
                      viewer.FilePath = fullPath;
                      viewer.LoadDocumentFromFile(fullPath, null);
                  }
              }
          }
          AdjustHeightToAspectRatio();
        */

    }
    public void AdjustHeightToAspectRatio()
    {
        if (rawImage.texture != null)
        {
            float aspectRatio = (float)rawImage.texture.height / rawImage.texture.width;
            RectTransform rt = rawImage.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, rt.sizeDelta.x * aspectRatio);
        }
    }
}
