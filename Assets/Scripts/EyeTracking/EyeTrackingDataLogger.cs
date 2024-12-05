using System.IO;
using UnityEngine;
using System;

public class EyeTrackingDataLogger : MonoBehaviour
{
    private string fileName;
    private string timestamp;
    private string folderName = "Data";
    private string filePath;
    [SerializeField] private EyeTrackingRay eyeTrackingRay;
    private StreamWriter csvWriter;
    private void Start()
    {
        timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"); // Format : 2024-12-04_14-23-15
        fileName = $"{timestamp}_EyeTrackingData.csv";
        // Combine correctement les chemins
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);
        // Créez le dossier si nécessaire
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        filePath =  Path.Combine(Application.persistentDataPath,folderName,fileName);
        csvWriter = new StreamWriter(filePath, false);  // false écrase le fichier existant
        csvWriter.WriteLine("Time;RayOriginX;RayOriginY;RayOriginZ;HitPointX;HitPointY;HitPointZ;ObjectHit");
    }

    public void AddMarker()
    {
        if (csvWriter != null)
        {
            csvWriter.WriteLine($"{Time.time},MARKER");
            csvWriter.Flush();
        }
    }

    private void Update()
    {
        if (eyeTrackingRay != null && eyeTrackingRay.TryGetRayHit(out RaycastHit hit))
        {
            csvWriter.WriteLine($"{Time.time};{eyeTrackingRay.transform.position.x};{eyeTrackingRay.transform.position.y};{eyeTrackingRay.transform.position.z};" +
                                $"{hit.point.x};{hit.point.y};{hit.point.z};{hit.transform.name}");
        }
    }

    private void OnDestroy()
    {
        if (csvWriter != null)
        {
            csvWriter.Close();
        }
    }
}
