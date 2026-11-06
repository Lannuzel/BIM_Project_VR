using System.Collections.Generic;
using System.IO;
using Fusion;
using UnityEngine;

/// <summary>
/// Journal des positions finales des objets (chaises, tables) de XP1.
/// N5 : tous les objets réseau portant le tag « Interactable » sont consignés, y compris ceux créés
/// par les autres casques (ils ne sont pas parentés localement) ; l'enregistrement est bien marqué
/// comme actif pour que la ligne END soit écrite à l'arrêt.
/// </summary>
public class ChairPositionDataLogger : MonoBehaviour
{
    private string folderName = "Data";
    private string filePath;
    private StreamWriter writer;
    private bool isRecording = false; // Contrôle l'état de l'enregistrement

    [Tooltip("Tag des objets à consigner.")]
    public string objectTag = "Interactable";

    public bool IsRecording => isRecording;
    public string FilePath => filePath;
    /// <summary>Nombre d'objets consignés au dernier bilan.</summary>
    public int EntryCount { get; private set; }

    void Start()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{timestamp}_ChairePositionData.csv";
        filePath = Path.Combine(folderPath, fileName);

        writer = new StreamWriter(filePath, false) { AutoFlush = true };
        writer.WriteLine("Time; ID ; Position");
        writer.WriteLine();
        isRecording = true;
        Debug.Log("Recording started: " + filePath);
    }

    public void AddMarker()
    {
        if (writer != null)
        {
            writer.WriteLine($"{Time.time};MARKER");
            writer.Flush();
            Debug.Log($"Marker ajouté dans le fichier CSV : {filePath}");
        }
        else
        {
            Debug.LogError($"issue in adding marker at ChairPositionDataLogger");
        }
    }

    /// <summary>
    /// Vrai si l'objet ou l'un de ses descendants porte le tag. Recette du 6 novembre 2026 : les prefabs créés
    /// (TablePref, équipements XP3) portent le tag « Interactable » sur un enfant, pas sur la racine réseau ;
    /// le fichier ChairePositionData restait vide.
    /// </summary>
    public static bool HasTagInHierarchy(Transform root, string tag)
    {
        if (root == null || string.IsNullOrEmpty(tag)) return false;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.CompareTag(tag)) return true;
        return false;
    }

    /// <summary>Objets à consigner : objets réseau valides portant le tag (racine ou descendant), sinon enfants du conteneur InstantiatedChaires.</summary>
    private List<Transform> CollectObjects()
    {
        var result = new List<Transform>();
        foreach (var no in FindObjectsOfType<NetworkObject>())
        {
            if (no == null || !no.IsValid) continue;
            if (no.transform.parent != null && no.transform.parent.GetComponentInParent<NetworkObject>() != null) continue; // sous-objet (ligne, étiquette)
            if (HasTagInHierarchy(no.transform, objectTag))
                result.Add(no.transform);
        }
        if (result.Count == 0)
        {
            GameObject container = GameObject.Find("InstantiatedChaires");
            if (container != null)
                foreach (Transform child in container.transform)
                    if (HasTagInHierarchy(child, objectTag)) result.Add(child);
        }
        return result;
    }

    public void AddChairPositionLog(bool status)
    {
        if (writer == null) return;

        writer.WriteLine(status ? "Task Status , Succeed" : "Task Status , TimeOut");
        writer.WriteLine();
        Debug.Log("Task Status : " + status);

        writer.WriteLine("time, name, position.x, position.y, position.z, rotation.y");
        int count = 0;
        foreach (var t in CollectObjects())
        {
            writer.WriteLine(Time.time + ", " + t.name + ", " + t.position.x.ToString("F3") + ", " + t.position.y.ToString("F3") + ", " + t.position.z.ToString("F3") + ", " + t.eulerAngles.y.ToString("F1"));
            count++;
        }
        writer.Flush();
        EntryCount = count;
        Debug.Log("Number of Chaires : " + count);
    }

    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("No recording is in progress to stop.");
            return;
        }

        isRecording = false;
        if (writer != null)
        {
            writer.Write("END");
            writer.Flush();
            writer.Close();
            writer.Dispose();
            writer = null;
        }
        Debug.Log("Recording stopped.");
    }

    private void OnDestroy()
    {
        StopRecording();
    }

    private void OnApplicationQuit()
    {
        StopRecording();
    }
}
