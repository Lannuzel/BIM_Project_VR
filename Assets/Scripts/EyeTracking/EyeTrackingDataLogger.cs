using System.IO;
using UnityEngine;

public class EyeTrackingDataLogger : MonoBehaviour
{
    public static EyeTrackingDataLogger Instance;
    private string folderName = "Data";
    private string filePath;
    [SerializeField] private EyeTrackingRay eyeTrackingRay;
    private StreamWriter csvWriter;
    private bool isRecording = false; // Contrôle l'état de l'enregistrement
    void Awake() { Instance = this; }
    
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
        csvWriter = new StreamWriter(filePath, false);  // false écrase le fichier existant
        csvWriter.WriteLine("Time;RayOriginX;RayOriginY;RayOriginZ;HitPointX;HitPointY;HitPointZ;ObjectHit");

        isRecording = true;
        Debug.Log("Recording started: " + filePath);
    }

    private void Update()
    {
        if (isRecording && eyeTrackingRay != null && eyeTrackingRay.TryGetRayHit(out RaycastHit hit))
        {
            csvWriter.WriteLine($"{Time.time};{eyeTrackingRay.transform.position.x};{eyeTrackingRay.transform.position.y};{eyeTrackingRay.transform.position.z};" +
                                $"{hit.point.x};{hit.point.y};{hit.point.z};{hit.transform.name}");
        }
    }

    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("No recording is in progress to stop.");
            return;
        }

        isRecording = false;
        if (csvWriter != null)
        {
            csvWriter.Close();
            csvWriter = null;
        }
        Debug.Log("Recording stopped.");
    }

    private void OnDestroy()
    {
        StopRecording(); // Assure que l'enregistrement est arrêté proprement
    }
}
