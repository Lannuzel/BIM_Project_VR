using System.Collections.Generic;
using UnityEngine;

public class MarkerManager : MonoBehaviour
{
    private List<PositionLogger> positionLoggers = new List<PositionLogger>();
    private List<FaceTrackingRecorder> faceLoggers = new List<FaceTrackingRecorder>();
    private List<EyeTrackingDataLogger> eyeLoggers = new List<EyeTrackingDataLogger>();
    private List<AudioTracker> audioTrackers = new List<AudioTracker>();
    private List<ChairPositionDataLogger> chairPositionLogger = new List<ChairPositionDataLogger>();
    private List<ReservationPositionDataLogger> reservationPositionDataLoggers = new List<ReservationPositionDataLogger>();

    private bool successStatus = true;
    void Start()
    {
        // Recherchez tous les trackers dans la scène
        positionLoggers.AddRange(FindObjectsOfType<PositionLogger>());
        audioTrackers.AddRange(FindObjectsOfType<AudioTracker>());
        faceLoggers.AddRange(FindObjectsOfType<FaceTrackingRecorder>());
        eyeLoggers.AddRange(FindObjectsOfType<EyeTrackingDataLogger>());
        chairPositionLogger.AddRange(FindObjectsOfType<ChairPositionDataLogger>());
        reservationPositionDataLoggers.AddRange (FindObjectsOfType<ReservationPositionDataLogger>());
    }
    public void StopAllRecordingsTimeOut()
    {
        successStatus = false;
        StopAllRecordings();
    }

    public void StopAllRecordings()
    {
        Debug.Log("Arrêt de tous les enregistrements.: begin..");

        // Arrête les enregistrements pour les AudioTrackers
        Debug.Log("Arrêt des enregistrements dans les chairLoggers...");
        foreach (var chairLogger in chairPositionLogger)
        {
            chairLogger.AddChairPositionLog(successStatus);
            chairLogger.StopRecording();
            Debug.LogError("Enregistrement charirLogs arrêté.");
        }

        // Arrête les enregistrements pour les PositionLoggers
        Debug.Log("Arrêt des enregistrements dans les positionLoggers...");
        foreach (var positionLogger in positionLoggers)
        {
            positionLogger.StopRecording();
            Debug.Log("Enregistrement arrêté dans un positionLogger.");
        }

        // Arrête les enregistrements pour les EyeLoggers
        Debug.Log("Arrêt des enregistrements dans les eyeLoggers...");
        foreach (var eyeLogger in eyeLoggers)
        {
            eyeLogger.StopRecording();
            Debug.Log("Enregistrement arrêté dans un eyeLogger.");
        }

        // Arrête les enregistrements pour les FaceLoggers
        Debug.Log("Arrêt des enregistrements dans les faceLoggers...");
        foreach (var faceLogger in faceLoggers)
        {
            faceLogger.StopRecording();
            Debug.Log("Enregistrement arrêté dans un faceLogger.");
        }

        // Arrête les enregistrements pour les AudioTrackers
        Debug.Log("Arrêt des enregistrements dans les audioTrackers...");
        foreach (var audioTracker in audioTrackers)
        {
            audioTracker.StopRecording();
            Debug.Log("Enregistrement audio arrêté dans un audioTracker.");
        }

        Debug.Log("Arrêt des enregistrements dans les reservationLoggers...");
        foreach (var reservationLogger in reservationPositionDataLoggers)
        {
            reservationLogger.AddReservationPositionLog(successStatus);
            reservationLogger.StopRecording();
            Debug.LogError("Enregistrement ReservationLogs arrêté.");
        }


        Debug.Log("Tous les enregistrements ont été arrêtés avec succès.");
    }
    public void AddMarkerToAllLogs()
    {
        Debug.Log($"Ajout d'un marker à tous les logs...");

        // Ajoutez un marker dans les fichiers chairePosition
        Debug.Log("Ajout du marker dans les chairePositionLogger...");
        foreach (var chairLogger in chairPositionLogger)
        {
            chairLogger.AddMarker();
            Debug.LogError("marker ajouté dans une chaireLogger.");
        }
        // Ajoutez un marker dans les fichiers CSV
        Debug.Log("Ajout du marker dans les positionLoggers...");
        foreach (var positionLogger in positionLoggers)
        {
            positionLogger.AddMarker();
            Debug.Log($"Marker ajouté dans un positionLogger.");
        }

        Debug.Log("Ajout du marker dans les eyeLoggers...");
        foreach (var eyeLogger in eyeLoggers)
        {
            eyeLogger.AddMarker();
            Debug.Log($"Marker ajouté dans un eyeLogger.");
        }

        Debug.Log("Ajout du marker dans les faceLoggers...");
        foreach (var faceLogger in faceLoggers)
        {
            faceLogger.AddMarker();
            Debug.Log($"Marker  ajouté dans un faceLogger.");
        }

        // Ajoutez un marker dans les fichiers audio
        Debug.Log("Ajout du marker dans les audioTrackers...");
        foreach (var audioTracker in audioTrackers)
        {
            audioTracker.AddAudioMarker();
            Debug.Log("Audio marker ajouté dans un audioTracker.");
        }

        // Ajoutez un marker dans les fichiers ReservationPosition
        Debug.Log("Ajout du marker dans les ReservationLogger...");
        foreach (var reservationLogger in reservationPositionDataLoggers)
        {
            reservationLogger.AddMarker();
            Debug.LogError("marker ajouté dans une ReservationLogger.");
        }

        Debug.Log($"Ajout du marker terminé.");
    }

}
