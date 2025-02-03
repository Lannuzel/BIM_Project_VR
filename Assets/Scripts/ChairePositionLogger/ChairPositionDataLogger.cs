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

        Debug.Log("Recording started: " + filePath);
    }



    public void AddMarker()
    {
        writer.WriteLine($"{Time.time};MARKER");
        Debug.Log($"Marker ajouté dans le fichier CSV : {filePath}");
    }
    public void AddChairPositionLog(bool status)
    {
        
        writer.WriteLine("Task Status, ", status);
        Debug.LogError("Task Status : " +  status);
        GameObject instantiatedChaires = GameObject.Find("InstantiatedChaires");
        if (instantiatedChaires != null)
        {
            List<Transform> interactableChildren = new List<Transform>();

            foreach (Transform child in instantiatedChaires.transform)
            {
                if (child.CompareTag("Interactable")) // Check if the child has the tag
                {
                    interactableChildren.Add(child);
                    writer.WriteLine(", "+ child.name + ", " + child.position.ToString());
                    Debug.LogError("Found Interactable: " + child.name + "  " + child.position.ToString());
                }
            }

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
