using System.Collections.Generic;
using System.IO;
using Fusion;
using UnityEngine;

/// <summary>
/// Journal des réservations (XP2) : position, rotation et dimensions finales.
/// N5 : toutes les réservations réseau sont consignées (y compris celles des autres casques),
/// le filtre par tag est évalué par objet, l'échelle consignée est l'échelle réelle,
/// et la ligne END est écrite à l'arrêt.
/// </summary>
public class ReservationPositionDataLogger : MonoBehaviour
{
    private string folderName = "Data";
    private string filePath;
    private StreamWriter writer;
    private bool isRecording = false;

    [Tooltip("Fragments de tag identifiant une réservation (XP1 utilise « Reservation », XP2 des tags détaillés).")]
    public List<string> reservationTagParts = new List<string> { "cylinderReservationSol", "cylinderReservationMur", "SquareReservationMur", "SquareReservationSol", "Reservation" };

    public bool IsRecording => isRecording;
    public string FilePath => filePath;
    /// <summary>Nombre de réservations consignées au dernier bilan.</summary>
    public int EntryCount { get; private set; }

    void Start()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{timestamp}_ReservationPositionData.csv";
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
            Debug.LogError($"issue in adding marker at ReservationPositionDataLogger");
        }
    }

    private bool IsReservation(Transform t)
    {
        foreach (var part in reservationTagParts)
            if (!string.IsNullOrEmpty(part) && t.tag.Contains(part)) return true;
        return false;
    }

    private List<Transform> CollectObjects()
    {
        var result = new List<Transform>();
        foreach (var no in FindObjectsOfType<NetworkObject>())
        {
            if (no != null && no.IsValid && IsReservation(no.transform))
                result.Add(no.transform);
        }
        if (result.Count == 0)
        {
            GameObject container = GameObject.Find("InstantiatedReservations");
            if (container != null)
                foreach (Transform child in container.transform)
                    if (IsReservation(child)) result.Add(child);
        }
        return result;
    }

    public void AddReservationPositionLog(bool status)
    {
        if (writer == null) return;

        writer.WriteLine(status ? "Task Status , Succeed" : "Task Status , TimeOut");
        writer.WriteLine();
        Debug.Log("Task Status : " + status);

        writer.WriteLine("time,name,tag, position.x , position.y, position.z, rotation.x ,rotation.y ,rotation.z, rotation.w , scaleX ,scaleY, scaleZ");
        int count = 0;
        foreach (var child in CollectObjects())
        {
            Vector3 s = child.lossyScale;
            writer.WriteLine(Time.time + ", " + child.name + ", " + child.tag + ", " + child.position.x.ToString("F3") + ", " + child.position.y.ToString("F3") + ", " + child.position.z.ToString("F3") + ", " + child.rotation.x.ToString("F3") + ", " + child.rotation.y.ToString("F3") + ", " + child.rotation.z.ToString("F3") + ", " + child.rotation.w.ToString("F3") + ", " + s.x.ToString("F3") + ", " + s.y.ToString("F3") + ", " + s.z.ToString("F3"));
            count++;
        }
        writer.Flush();
        EntryCount = count;
        Debug.Log("Number of Reservations : " + count);
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
