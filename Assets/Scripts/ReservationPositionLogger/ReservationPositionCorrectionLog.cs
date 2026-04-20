using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

public class ReservationPositionCorrectionLog : MonoBehaviour
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
        string fileName = $"ReservationPositionCorrectionsData.csv";

        // Chemin complet du fichier
        filePath = Path.Combine(folderPath, fileName);

        // Initialise le StreamWriter
        writer = new StreamWriter(filePath, false);  // false �crase le fichier existant
        writer.WriteLine("Time; ID ; PositionCorrectionLog");
        writer.WriteLine();
        Debug.Log("Recording started: " + filePath);
        AddReservationPositionLog();
    }



    public void AddReservationPositionLog()
    {
        
        writer.WriteLine();
       
        writer.WriteLine("name; position.x ; position.y; position.z; rotation.x ; rotation.y; rotation.z; rotation.w ; scaleX ; scaleY;  scaleZ");
        GameObject instantiatedReservation = GameObject.Find("ReservationCorrections");
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
                    writer.WriteLine( child.name + "; " + child.position.x.ToString("F2") + ";" + child.position.y.ToString("F2") + "; " + child.position.z.ToString("F2") + "; " + child.rotation.x.ToString("F2") + "; " + child.rotation.y.ToString("F2") + "; " + child.rotation.z.ToString("F2") + "; " + child.rotation.w.ToString("F2") + "; " + scaleX.ToString("F2") + "; " + scaleY.ToString("F2") + "; " + scaleZ.ToString("F2"));
                    Debug.LogError("Reservation : " + child.name + "  " + child.position.ToString() + "   " + child.rotation.ToString() + "  " + child.localScale.ToString());
                    count++;
                }
                
            }
            writer.Flush();
            Debug.LogError("Number of Reservations : " + count);

        }
        
        Debug.Log($"log ajout� dans le fichier CSV : {filePath}");
    }

}
