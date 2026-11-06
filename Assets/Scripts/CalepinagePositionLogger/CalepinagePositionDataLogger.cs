using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class CalepinagePositionDataLogger : MonoBehaviour
{
    private string folderName = "Data";
    private string filePath;  
    private StreamWriter writer;
    private bool isRecording = false; // Contr�le l'�tat de l'enregistrement

    void Start()
    {
        // Combine correctement les chemins
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);

        // Cr�e le dossier si n�cessaire
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Cr�e le nom du fichier au format date_heure + type de tracker
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{timestamp}_CalepinagePositionData.csv";

        // Chemin complet du fichier
        filePath = Path.Combine(folderPath, fileName);

        // Initialise le StreamWriter
        writer = new StreamWriter(filePath, false);  // false �crase le fichier existant
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
            Debug.LogError($"issue in adding marker at CalepinagePositionLogger");
        }
    }
    public void AddCalepinagePositionLog(bool status)
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
            string objectType = "";
            foreach (Transform child in instantiatedChaires.transform )
            {
                if (child.CompareTag("Interactable")) // Check if the child has the tag
                {
                    interactableChildren.Add(child);
                    if(child.name.Contains("Ventilator"))
                    {
                        objectType = "Bouche de soufflage";  
                    }
                    else if (child.name.Contains("Lumier"))
                    {
                        objectType = "Lumier";   
                    }
                    else if (child.name.Contains("AirTerminal"))
                    {
                        objectType = "Bouche de reprise";   
                    }
                    
                    ResourceProperties proerties = child.GetComponent<ResourceProperties>();
                    float tauxDeBrassage = 0.0f;
                    if (proerties != null)
                    {
                        tauxDeBrassage = proerties.tauxDeBrassage;
                    }
                    writer.WriteLine(Time.time + "; " + child.name + "; "+ objectType + "; " + child.position.ToString()+ ";  "+ tauxDeBrassage);
                    Debug.LogError("Calepinage : " + child.name + "  " + objectType + "  " + child.position.ToString() + " Taux de brassage : " + tauxDeBrassage);
                    count++;
                }
            }
            writer.Flush();
            Debug.LogError("Number of Calepinage objects : " + count);

        }
        
        Debug.Log($"log ajout� dans le fichier CSV : {filePath}");
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
        Debug.Log("Application quitt�e sur OnDestroy. Arr�t de l'enregistrement Calepinage.");
        StopRecording(); // Assure que l'enregistrement est arr�t� proprement
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Application quitt�e. Arr�t de l'enregistrement Calepinage.");
        StopRecording();
    }

    // private void OnApplicationPause(bool isPaused)
    // {
    //     if (isPaused)
    //     {
    //         Debug.Log("Application quitt�e sur OnApplicationPause. Arr�t de l'enregistremen Eye.");
    //         StopRecording(); // Assure que l'enregistrement est arr�t� proprement
    //     }
    // }

}
