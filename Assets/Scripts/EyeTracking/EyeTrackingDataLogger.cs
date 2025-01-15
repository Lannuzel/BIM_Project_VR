using System.IO;
using UnityEngine;

public class EyeTrackingDataLogger : MonoBehaviour
{
    private string folderName = "Data";
    private string filePath;
    [SerializeField] private EyeTrackingRay eyeTrackingRay;
    private StreamWriter writer;
    private bool isRecording = false; // Contrôle l'état de l'enregistrement

    void Start()
    {
        StartRecording();
    }
    
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("Recording is already in progress!");
            return;
        }

        // Combine correctement les chemins
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);
        
        // Crée le dossier si nécessaire
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Crée le nom du fichier au format date_heure + type de tracker
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{timestamp}_EyeTrackingData.csv";

        // Chemin complet du fichier
        filePath = Path.Combine(folderPath, fileName);
        
        // Initialise le StreamWriter
        writer = new StreamWriter(filePath, false);  // false écrase le fichier existant
        writer.WriteLine("Time;RayOriginX;RayOriginY;RayOriginZ;HitPointX;HitPointY;HitPointZ;ObjectHit");

        isRecording = true;
        Debug.Log("Recording started: " + filePath);
    }

    private void Update()
    {
        if (isRecording && eyeTrackingRay != null && eyeTrackingRay.TryGetRayHit(out RaycastHit hit))
        {
            writer.WriteLine($"{Time.time};{eyeTrackingRay.transform.position.x};{eyeTrackingRay.transform.position.y};{eyeTrackingRay.transform.position.z};" +
                                $"{hit.point.x};{hit.point.y};{hit.point.z};{hit.transform.name}");
        }
    }

    public void AddMarker()
    {
        writer.WriteLine($"{Time.time};MARKER");
        Debug.Log($"Marker ajouté dans le fichier CSV : {filePath}");
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
            writer.Write($"END");

            writer.Close();
            writer = null;
        }
        Debug.Log("Recording stopped.");
    }


    private void OnDestroy()
    {
        Debug.Log("Application quittée sur OnDestroy. Arrêt de l'enregistremen Eye.");
        StopRecording(); // Assure que l'enregistrement est arrêté proprement
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Application quittée. Arrêt de l'enregistrement EyeTracker.");
        StopRecording();
    }
    
    // private void OnApplicationPause(bool isPaused)
    // {
    //     if (isPaused)
    //     {
    //         Debug.Log("Application quittée sur OnApplicationPause. Arrêt de l'enregistremen Eye.");
    //         StopRecording(); // Assure que l'enregistrement est arrêté proprement
    //     }
    // }

}
