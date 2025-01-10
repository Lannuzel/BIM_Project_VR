using System.IO;
using UnityEngine;

public class FaceTrackingRecorder : MonoBehaviour
{
    private string folderName = "Data";
    private string filePath;

    [SerializeField] private OVRFaceExpressions faceExpressions;
    private StreamWriter writer;
    private bool isRecording = false;
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
        string fileName = $"{timestamp}_FaceTrackingData.csv";

        // Chemin complet du fichier
        filePath = Path.Combine(folderPath, fileName);

        // Vérifie la présence du composant faceExpressions
        if (faceExpressions == null) // Si pas déjà assigné dans l'inspecteur
        {
            faceExpressions = FindObjectOfType<OVRFaceExpressions>();
        }

        if (faceExpressions == null)
        {
            Debug.LogError("OVRFaceExpressions component is missing in the scene.");
            enabled = false;
            return;
        }

        // Démarre l'enregistrement
        StartRecording();
    }

    public void StartRecording()
    {
        writer = new StreamWriter(filePath, false); //ecrase fichier existant

        // Écrire l'en-tête (noms des colonnes)
        writer.Write("Timestamp");
        foreach (var expression in System.Enum.GetValues(typeof(OVRFaceExpressions.FaceExpression)))
        {
            if ((OVRFaceExpressions.FaceExpression)expression == OVRFaceExpressions.FaceExpression.Invalid ||
                (OVRFaceExpressions.FaceExpression)expression == OVRFaceExpressions.FaceExpression.Max)
            {
                continue;
            }

            writer.Write($";{expression}");
        }
        writer.WriteLine(); // Nouvelle ligne pour séparer l'en-tête des données

        isRecording = true;
        Debug.Log("Recording started.");
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
        Debug.Log("Recording stopped.");
    }

    void Update()
    {
        if (isRecording && faceExpressions.ValidExpressions)
        {
            writer.Write(Time.time); // Ajouter le timestamp

            foreach (var expression in System.Enum.GetValues(typeof(OVRFaceExpressions.FaceExpression)))
            {
                if ((OVRFaceExpressions.FaceExpression)expression == OVRFaceExpressions.FaceExpression.Invalid ||
                    (OVRFaceExpressions.FaceExpression)expression == OVRFaceExpressions.FaceExpression.Max)
                {
                    continue;
                }

                float weight = faceExpressions.GetWeight((OVRFaceExpressions.FaceExpression)expression);
                writer.Write($";{weight:F4}"); // Valeur formatée
            }

            writer.WriteLine(); // Nouvelle ligne pour la prochaine itération
        }
    }

    public void AddMarker()
    {
        writer.WriteLine($"{Time.time};MARKER");
        Debug.Log($"Marker ajouté dans le fichier CSV : {filePath}");
    }


    private void OnDestroy()
    {
        Debug.Log("Application quittée sur OnDestroy. Arrêt de l'enregistremen Face.");
        StopRecording();
    }
    private void OnApplicationQuit()
    {
        Debug.Log("Application quittée. Arrêt de l'enregistrement FaceRecord.");
        StopRecording();
    }
    // private void OnApplicationPause(bool isPaused)
    // {
    //     if (isPaused)
    //     {
    //         Debug.Log("Application quittée sur OnApplicationPause. Arrêt de l'enregistremen Face.");
    //         StopRecording(); // Assure que l'enregistrement est arrêté proprement
    //     }
    // }
}
