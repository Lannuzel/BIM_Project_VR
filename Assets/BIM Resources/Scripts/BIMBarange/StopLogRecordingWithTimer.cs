using UnityEngine;
using System.Collections;
using Fusion;

/// <summary>
/// Délai maximal de la tâche (N5). Le compte à rebours n'est plus local au Lecteur :
/// il est armé sur chaque casque par <see cref="SyncManager"/> à l'heure synchronisée du lancement.
/// Ce composant ne sert plus qu'à transmettre la durée configurée dans la scène.
/// </summary>
public class StopLogRecordingWithTimer : NetworkBehaviour
{
    public HostController hostController;
    public float thresholdTimer = 1800f;

    public void ActivateStopLogRecordTimeout()
    {
        var sync = SyncManager.Instance;
        if (sync != null)
        {
            sync.timerThreshold = thresholdTimer;
            Debug.Log($"[StopLogRecordingWithTimer] Délai maximal transmis : {thresholdTimer} s (armé sur tous les casques au lancement).");
        }
        else
        {
            Debug.LogWarning("[StopLogRecordingWithTimer] SyncManager local introuvable : délai par défaut conservé.");
        }
    }
}
