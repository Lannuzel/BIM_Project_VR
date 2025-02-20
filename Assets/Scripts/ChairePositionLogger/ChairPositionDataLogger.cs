using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class ChairPositionDataLogger : MonoBehaviour
{
    private string folderName = "Data";
    private string filePath;  
    private StreamWriter writer;
    private bool isRecording = false; // Contrôle l'état de l'enregistrement

    void Start()
    {
        // Combine correctement les chemins
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);

        // Crée le dossier si nécessaire
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Crée le nom du fichier au format date_heure + type de tracker
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{timestamp}_ChairePositionData.csv";

        // Chemin complet du fichier
        filePath = Path.Combine(folderPath, fileName);

        // Initialise le StreamWriter
        writer = new StreamWriter(filePath, false);  // false écrase le fichier existant
        writer.WriteLine("Time; ID ; Position");
        writer.WriteLine();
        Debug.Log("Recording started: " + filePath);
    }



    public void AddMarker()
    {
        if (writer != null)
        {
            writer.WriteLine($"{Time.time};MARKER");
            writer.Flush(); // Ensure data is written to file immediately
            Debug.Log($"Marker ajouté dans le fichier CSV : {filePath}");
        }
        else
        {
            Debug.LogError($"issue in adding marker at PositionLogger");
        }
    }
    public void AddChairPositionLog(bool status)
    {
        if(status)
            writer.WriteLine("Task Status , Succeed");
        else
            writer.WriteLine("Task Status , TimeOut");

        writer.WriteLine();
        Debug.LogError("Task Status : " +  status);
        GameObject instantiatedChaires = GameObject.Find("InstantiatedChaires");
        if (instantiatedChaires != null)
        {   int count = 0;
            List<Transform> interactableChildren = new List<Transform>();

            Debug.LogError("::::::::::::::Chaire Position Log :::::::::::::");

            foreach (Transform child in instantiatedChaires.transform )
            {
                if (child.CompareTag("Interactable")) // Check if the child has the tag
                {
                    interactableChildren.Add(child);
                    writer.WriteLine(Time.time + ", " + child.name + ", " + child.position.ToString());
                    Debug.LogError("chair : " + child.name + "  " + child.position.ToString());
                    count++;
                }
            }
            writer.Flush();
            Debug.LogError("Number of Chaires : " + count);

        }
        
        Debug.Log($"log ajouté dans le fichier CSV : {filePath}");
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
            writer.Flush(); // Ensure all data is written to disk
            writer.Close();
            writer.Dispose(); // Ensure file is properly closed
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
