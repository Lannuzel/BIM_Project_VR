using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PositionLogger : MonoBehaviour
{
    private string folderName = "Data";
    private string filePath;
    private List<UserTracker> userTrackers = new List<UserTracker>();
    private StreamWriter writer;
    private bool isRecording = false;

    private float nextLogTime = 0f;

    [SerializeField] private float logInterval = 0.5f; // Enregistre toutes les 0.5 secondes

    void Start()
    {
        // Combine correctement les chemins
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);

        // Créez le dossier si nécessaire
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Crée le nom du fichier au format date_heure + type de tracker
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{timestamp}_UsersPositions.csv";

        // Chemin complet du fichier
        filePath = Path.Combine(folderPath, fileName);

        // Trouver tous les UserTracker dans la scène
        userTrackers.AddRange(FindObjectsOfType<UserTracker>());

        // Initialiser l'enregistrement
        StartLogging();
    }

    public void StartLogging()
    {

        writer = new StreamWriter(filePath, false); // Ouvre en écrasant le fichier existant

        // Écrire l'en-tête (une colonne pour chaque utilisateur avec ses coordonnées)
        writer.Write("Timestamp");

        foreach (var user in userTrackers)
        {
            writer.Write($";{user.GetUserName()}_PosX;{user.GetUserName()}_PosY;{user.GetUserName()}_PosZ");
        }
        writer.WriteLine();

        isRecording = true;
        Debug.Log($"Logging started. File saved at {filePath}");
    }

    void Update()
    {
        if (isRecording && Time.time >= nextLogTime)
        {
            LogUserPositions();
            nextLogTime = Time.time + logInterval;
        }
    }
    void LogUserPositions()
    {
        writer.Write($"{Time.time}"); // Ajoute le timestamp en première colonne

        foreach (var user in userTrackers)
        {
            if (user != null)
            {
                Vector3 position = user.GetCurrentPosition();
                writer.Write($";{position.x:F4};{position.y:F4};{position.z:F4}");
            }
            else
            {
                // Si l'utilisateur est manquant, écrire des valeurs nulles
                writer.Write($";N/A;N/A;N/A");
            }
        }

        writer.WriteLine(); // Passe à la ligne suivante
        writer.Flush(); // Écrire immédiatement les données
    }

    public void AddMarker()
    {
        writer.WriteLine($"{Time.time};MARKER");
        Debug.Log($"Marker ajouté dans le fichier CSV : {filePath}");
    }

    public void StopRecording()
    {
        if (writer != null)
        {
            writer.Write($"END");   
            writer.Close();
            writer = null;
        }

        isRecording = false;
        Debug.Log("Logging stopped.");
    }
    void OnDestroy()
    {
        Debug.Log("Application quittée sur OnDestroy. Arrêt de l'enregistrement Position.");
        StopRecording();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Application quittée. Arrêt de l'enregistrement Position.");
        StopRecording();
    }
    // private void OnApplicationPause(bool isPaused)
    // {
    //     if (isPaused)
    //     {
    //         Debug.Log("Application quittée sur OnApplicationPause. Arrêt de l'enregistremen Position.");
    //         StopRecording(); // Assure que l'enregistrement est arrêté proprement
    //     }
    // }

}
