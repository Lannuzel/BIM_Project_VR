using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

public class ReservationPositionDataLogger : MonoBehaviour
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
        string fileName = $"{timestamp}_ReservationPositionData.csv";

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
    public void AddReservationPositionLog(bool status)
    {
        if(status)
            writer.WriteLine("Task Status , Succeed");
        else
            writer.WriteLine("Task Status , TimeOut");

        writer.WriteLine();
        Debug.LogError("Task Status : " +  status);

        writer.WriteLine("time,name, position.x , position.y, position.z, rotation.x ,rotation.y ,rotation.z, rotation.w , scaleX ,scaleY, scaleZ");
        GameObject instantiatedReservation = GameObject.Find("InstantiatedReservations");
        if (instantiatedReservation != null)
        {   int count = 0;
            List<Transform> interactableChildren = new List<Transform>();
            Debug.LogError("::::::::::::::Chaire Position Log :::::::::::::");
            bool childFlag = false;
            foreach (Transform child in instantiatedReservation.transform )
            {
                   
                    float scaleX =0f, scaleY = 0f, scaleZ= 0f;
                    if (child.tag.Contains("cylinderReservationSol"))
                    {
                    childFlag = true;
                        interactableChildren.Add(child);
                        scaleX = child.localScale.x / 1f; 
                        scaleY = child.localScale.y * 2f;
                        scaleZ = child.localScale.z / 1f;  
                    }
                    else if (child.tag.Contains("cylinderReservationMur"))
                    {
                    childFlag = true;
                    interactableChildren.Add(child);
                        scaleX = child.localScale.x / 1f;
                        scaleY = child.localScale.y * 2f;
                        scaleZ = child.localScale.z / 1f;
                        
                    }
                    else if (child.tag.Contains("SquareReservationMur"))
                {
                    childFlag = true;
                    interactableChildren.Add(child);
                         scaleX = child.localScale.x ;
                        scaleY = child.localScale.y ;
                        scaleZ = child.localScale.z ;
                    }
                    else if (child.tag.Contains("SquareReservationSol"))
                {
                    childFlag = true;
                    interactableChildren.Add(child);
                        scaleX = child.localScale.x ;
                        scaleY = child.localScale.y ;
                        scaleZ = child.localScale.z ;
                    }

                if (childFlag == true)
                {
                    writer.WriteLine(Time.time + ", " + child.name + ", " + child.position.x.ToString("F2") + ", " + child.position.y.ToString("F2") + ", " + child.position.z.ToString("F2") + ", " + child.rotation.x.ToString("F2") + ", " + child.rotation.y.ToString("F2") + ", " + child.rotation.z.ToString("F2") + ", " + child.rotation.w.ToString("F2") + ", " + scaleX.ToString("F2") + ", " + scaleY.ToString("F2") + ", " + scaleZ.ToString("F2"));
                    Debug.LogError("Reservation : " + child.name + "  " + child.position.ToString() + "   " + child.rotation.ToString() + "  " + child.localScale.ToString());
                    count++;
                }
                
            }
            writer.Flush();
            Debug.LogError("Number of Reservations : " + count);

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
        Debug.Log("Application quittée sur OnDestroy. Arrêt de l'enregistrement Reservation.");
        StopRecording(); // Assure que l'enregistrement est arrêté proprement
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Application quittée. Arrêt de l'enregistrement Reservation.");
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
