using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public CountdownManager countdownManager;
    public AudioTracker audioLogger;
    public PositionLogger positionLogger;
    public FaceTrackingRecorder faceTrackerRecorder;
    public EyeTrackingDataLogger eyeTrackerLogger;

    public KeyCode startKey = KeyCode.Space; // Touche pour démarrer le compte à rebours

    void Start()
    {
        // Assurez-vous que tous les composants sont reliés
        countdownManager.OnCountdownFinished += OnCountdownFinished;
    }
    void Update()
    {
        // Vérifier si l'utilisateur appuie sur la touche pour lancer le compte à rebours
        if (Input.GetKeyDown(startKey))
        {
            StartTaskWithCountdown();
        }
    }
    public void StartTaskWithCountdown()
    {
        if (countdownManager != null)
        {
            Debug.Log("Countdown started by host!");
            countdownManager.StartCountdown();
        }
    }

    private void OnCountdownFinished()
    {
        // Ajouter un marqueur dans les fichiers CSV
        positionLogger.AddMarker();
        faceTrackerRecorder.AddMarker();
        eyeTrackerLogger.AddMarker();

        // Jouer un signal sonore pour synchronisation audio
        audioLogger.AddSyncSignalToRecording();

        // Démarrer la tâche principale ici (par exemple, une expérience ou un événement)
        Debug.Log("Task started!");
    }
}
