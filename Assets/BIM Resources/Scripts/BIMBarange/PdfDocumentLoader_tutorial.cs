using Paroxe.PdfRenderer;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class PdfDocumentLoader_tutorial : MonoBehaviour
{
    public string tutorialPdffile;

    public void Awake()
    {

            ShowPdfFromStreamingAssets(tutorialPdffile);

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


}
