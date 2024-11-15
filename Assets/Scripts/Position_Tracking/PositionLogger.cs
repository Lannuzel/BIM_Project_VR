using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PositionLogger : MonoBehaviour
{
    [SerializeField] private string fileName = "./Data/UsersPositions.csv"; // Nom du fichier CSV
    private List<UserTracker> userTrackers = new List<UserTracker>();
    private StreamWriter writer;
    private bool isRecording = false;

    private float nextLogTime = 0f;

    [SerializeField] private float logInterval = 0.5f; // Enregistre toutes les 0.5 secondes
    

    void Start()
    {
        // Trouver tous les UserTracker dans la scène
        userTrackers.AddRange(FindObjectsOfType<UserTracker>());

        // Initialiser l'enregistrement
        StartLogging();
    }

    void StartLogging()
    {
       
        writer = new StreamWriter(fileName, false); // Ouvre en écrasant le fichier existant

        // Écrire l'en-tête (une colonne pour chaque utilisateur avec ses coordonnées)
        writer.Write("Timestamp");

        foreach (var user in userTrackers)
        {
            writer.Write($";{user.GetUserName()}_PosX;{user.GetUserName()}_PosY;{user.GetUserName()}_PosZ");
        }
        writer.WriteLine();

        isRecording = true;
        Debug.Log($"Logging started. File saved at {fileName}");
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
        writer.Write($"{Time.time:F2}"); // Ajoute le timestamp en première colonne

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

    void OnDestroy()
    {
        StopLogging();
    }

    void StopLogging()
    {
        if (writer != null)
        {
            writer.Close();
            writer = null;
        }

        isRecording = false;
        Debug.Log("Logging stopped.");
    }
}
