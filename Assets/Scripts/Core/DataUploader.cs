using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Envoi des fichiers de la session (loggers + manifeste) vers le PC de l'expérimentateur en fin de
/// session, par HTTP POST multipart vers <see cref="SessionConfig.uploadUrl"/> (récepteur :
/// docs/receive_data.py). Les fichiers restent sur le casque dans tous les cas ; l'état est affiché
/// devant la caméra (NetworkStatusPanel). Sans URL configurée, rien n'est envoyé.
/// </summary>
public class DataUploader : MonoBehaviour
{
    private static DataUploader instance;

    private static DataUploader Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("DataUploader");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<DataUploader>();
            }
            return instance;
        }
    }

    /// <summary>Dernier état lisible (pour le panneau de l'expérimentateur).</summary>
    public static string LastStatus { get; private set; } = "pas d'envoi";

    /// <summary>
    /// Envoie les fichiers du manifeste courant puis appelle <paramref name="onDone"/> (succès ou non).
    /// Retourne immédiatement si l'envoi n'est pas configuré.
    /// </summary>
    public static void UploadSession(Action<bool> onDone)
    {
        var config = SessionConfig.Instance;
        if (!config.UploadEnabled || SessionManifest.Current == null)
        {
            onDone?.Invoke(false);
            return;
        }
        Instance.StartCoroutine(Instance.UploadRoutine(config, onDone));
    }

    private IEnumerator UploadRoutine(SessionConfig config, Action<bool> onDone)
    {
        var manifest = SessionManifest.Current;
        string dir = Path.GetDirectoryName(SessionManifest.FilePath);
        var files = new List<string>();
        foreach (var f in manifest.files)
        {
            string p = Path.Combine(dir, f.name);
            if (File.Exists(p)) files.Add(p);
        }
        if (File.Exists(SessionManifest.FilePath)) files.Add(SessionManifest.FilePath);

        string url = config.uploadUrl.Trim();
        float deadline = Time.realtimeSinceStartup + config.uploadTimeoutSeconds;
        int sent = 0;
        bool allOk = true;

        for (int i = 0; i < files.Count; i++)
        {
            string path = files[i];
            string name = Path.GetFileName(path);
            LastStatus = $"envoi {i + 1}/{files.Count} : {name}";
            NetworkStatusPanel.Show($"Envoi des données {i + 1}/{files.Count}\n{name}", 0f);

            byte[] bytes;
            try { bytes = File.ReadAllBytes(path); }
            catch (Exception e)
            {
                Debug.LogError($"[DataUploader] Lecture impossible : {path} ({e.Message})");
                allOk = false;
                continue;
            }

            var form = new WWWForm();
            form.AddField("session", manifest.sessionName ?? "");
            form.AddField("role", manifest.role ?? "");
            form.AddField("device", manifest.deviceName ?? "");
            form.AddField("markerUtc", manifest.markerUtc ?? "");
            form.AddBinaryData("file", bytes, name, "application/octet-stream");

            using (var request = UnityWebRequest.Post(url, form))
            {
                request.timeout = Mathf.Max(5, Mathf.CeilToInt(deadline - Time.realtimeSinceStartup));
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[DataUploader] Échec pour {name} : {request.error}");
                    allOk = false;
                }
                else
                {
                    sent++;
                }
            }

            if (Time.realtimeSinceStartup > deadline)
            {
                Debug.LogError("[DataUploader] Délai d'envoi dépassé.");
                allOk = false;
                break;
            }
        }

        LastStatus = allOk ? $"{sent} fichier(s) envoyé(s)" : $"{sent}/{files.Count} envoyé(s), échec : fichiers conservés sur le casque";
        NetworkStatusPanel.Show(allOk
            ? $"Données envoyées ({sent} fichiers)."
            : $"Envoi incomplet ({sent}/{files.Count}) : les fichiers restent sur le casque.", 5f);
        onDone?.Invoke(allOk);
    }
}
