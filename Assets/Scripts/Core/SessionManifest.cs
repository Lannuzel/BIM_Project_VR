using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Manifeste de session : un fichier JSON <c>&lt;horodatage&gt;_Session.json</c> écrit dans le dossier Data du
/// casque au lancement de l'activité (marqueur) et complété à l'arrêt. Il permet d'apparier sans ambiguïté
/// les fichiers des trois casques : nom de session Photon, rôle, casque, version de l'application, scène,
/// heure du marqueur (locale, réseau, UTC), liste des fichiers produits et leur taille.
/// </summary>
public static class SessionManifest
{
    [Serializable]
    public class FileEntry
    {
        public string name;
        public long bytes;
    }

    [Serializable]
    public class Data
    {
        public string sessionName;
        public string role;
        public string scene;
        public string applicationId;
        public string applicationVersion;
        public string unityVersion;
        public string deviceModel;
        public string deviceName;
        public string deviceId;
        public string appStartedUtc;
        public float markerLocalTime;
        public double markerNetTime;
        public string markerUtc;
        public float endLocalTime;
        public double endNetTime;
        public string endUtc;
        public bool completed;
        public bool stoppedByTimeout;
        public List<FileEntry> files = new List<FileEntry>();
    }

    private static Data current;
    private static string path;
    /// <summary>
    /// Heure UTC de lancement de l'application, déduite de l'horloge locale (recette du 6 novembre 2026 : la valeur
    /// était prise à la première utilisation de la classe, c'est-à-dire au marqueur, et non au démarrage).
    /// </summary>
    private static string AppStartedUtc =>
        DateTime.UtcNow.AddSeconds(-Time.realtimeSinceStartup).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Chemin du manifeste de la session en cours (null tant que l'activité n'est pas lancée).</summary>
    public static string FilePath => path;
    public static Data Current => current;

    /// <summary>Dossier des données : celui des loggers, sinon &lt;persistentDataPath&gt;/Data.</summary>
    public static string DataDirectory(IEnumerable<string> loggerFiles = null)
    {
        if (loggerFiles != null)
        {
            foreach (var f in loggerFiles)
            {
                if (!string.IsNullOrEmpty(f))
                    return Path.GetDirectoryName(f);
            }
        }
        return Path.Combine(Application.persistentDataPath, "Data");
    }

    /// <summary>Appelé au marqueur de lancement : crée le manifeste.</summary>
    public static void Begin(IEnumerable<string> loggerFiles)
    {
        try
        {
            current = new Data
            {
                sessionName = SessionConfig.Instance.sessionName,
                role = NetworkManager.GetLocalPlayerName(),
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                applicationId = Application.identifier,
                applicationVersion = Application.version,
                unityVersion = Application.unityVersion,
                deviceModel = SystemInfo.deviceModel,
                deviceName = SystemInfo.deviceName,
                deviceId = SystemInfo.deviceUniqueIdentifier,
                appStartedUtc = AppStartedUtc,
                markerLocalTime = Time.time,
                markerNetTime = SessionClock.NetTime,
                markerUtc = SessionClock.UtcIso,
            };
            string dir = DataDirectory(loggerFiles);
            Directory.CreateDirectory(dir);
            path = Path.Combine(dir, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_Session.json");
            AddFiles(loggerFiles);
            Save();
            Debug.Log($"[SessionManifest] Manifeste créé : {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManifest] Impossible d'écrire le manifeste : {e.Message}");
        }
    }

    /// <summary>Appelé à l'arrêt des enregistrements : heures de fin, état, tailles des fichiers.</summary>
    public static void End(bool completed, bool stoppedByTimeout, IEnumerable<string> loggerFiles)
    {
        if (current == null) return;
        try
        {
            current.endLocalTime = Time.time;
            current.endNetTime = SessionClock.NetTime;
            current.endUtc = SessionClock.UtcIso;
            current.completed = completed;
            current.stoppedByTimeout = stoppedByTimeout;
            AddFiles(loggerFiles);
            Save();
            Debug.Log($"[SessionManifest] Manifeste complété : {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManifest] Impossible de compléter le manifeste : {e.Message}");
        }
    }

    private static void AddFiles(IEnumerable<string> loggerFiles)
    {
        if (loggerFiles == null) return;
        foreach (var f in loggerFiles)
        {
            if (string.IsNullOrEmpty(f)) continue;
            string name = Path.GetFileName(f);
            var entry = current.files.Find(e => e.name == name);
            if (entry == null)
            {
                entry = new FileEntry { name = name };
                current.files.Add(entry);
            }
            try { entry.bytes = File.Exists(f) ? new FileInfo(f).Length : 0; } catch (Exception) { entry.bytes = 0; }
        }
    }

    private static void Save()
    {
        File.WriteAllText(path, JsonUtility.ToJson(current, true));
    }
}
