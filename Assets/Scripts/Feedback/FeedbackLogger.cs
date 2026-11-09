using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Journal des feedbacks (portage d'Emery Descours, DataFeedbacks) aligné sur les autres enregistreurs :
/// fichier <c>&lt;horodatage&gt;_FeedbackData.csv</c> dans le dossier Data du casque, colonnes de temps
/// <c>Time;NetTime;Utc</c>, ligne MARKER au lancement et END à l'arrêt (via MarkerManager), listé dans le
/// manifeste et envoyé au PC avec le reste.
/// Colonnes : Feedback (gaze | voice | face | session), Event, Role (rôle local), Target (objet regardé), Value
/// (opacité ou niveau), Count, Total, Ratio, Who (participants concernés, ex. [1,0,1]).
/// Recette du 9 novembre 2026 : le gestionnaire des feedbacks est installé dans toutes les scènes ; le fichier n'est
/// créé qu'à la première écriture, pour ne rien produire dans une session où les feedbacks restent désactivés.
/// </summary>
public class FeedbackLogger : MonoBehaviour
{
    private string filePath;
    private StreamWriter writer;
    private bool closed;

    /// <summary>Vrai dès que le fichier existe (première écriture) et tant qu'il n'est pas clos.</summary>
    public bool IsRecording => writer != null;
    /// <summary>Chemin du fichier ; null tant qu'aucune ligne n'a été écrite.</summary>
    public string FilePath => filePath;
    public int EntryCount { get; private set; }

    private bool EnsureOpen()
    {
        if (writer != null) return true;
        if (closed) return false;
        string folder = Path.Combine(Application.persistentDataPath, "Data");
        Directory.CreateDirectory(folder);
        filePath = Path.Combine(folder, System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_FeedbackData.csv");
        writer = new StreamWriter(filePath, false) { AutoFlush = true };
        writer.WriteLine(SessionClock.HeaderColumns + ";Feedback;Event;Role;Target;Value;Count;Total;Ratio;Who");
        Debug.Log("[FeedbackLogger] Journal des feedbacks : " + filePath);
        return true;
    }

    public void AddMarker()
    {
        // Sans feedback actif ni fichier déjà ouvert, rien à consigner.
        if (writer == null && !FeedbackManager.AnyEnabled) return;
        if (!EnsureOpen()) return;
        writer.WriteLine($"{SessionClock.Stamp()};MARKER");
    }

    public void Log(string feedback, string evt, string target = "", float? value = null, int? count = null, int? total = null, float? ratio = null, string who = "")
    {
        if (!EnsureOpen()) return;
        var inv = CultureInfo.InvariantCulture;
        writer.WriteLine(string.Join(";", new[]
        {
            SessionClock.Stamp(), feedback, evt, NetworkManager.GetLocalPlayerName(), target,
            value.HasValue ? value.Value.ToString("F3", inv) : "",
            count.HasValue ? count.Value.ToString(inv) : "",
            total.HasValue ? total.Value.ToString(inv) : "",
            ratio.HasValue ? ratio.Value.ToString("F3", inv) : "",
            who,
        }));
        EntryCount++;
    }

    public void StopRecording()
    {
        closed = true;
        if (writer == null) return;
        writer.Write("END");
        writer.Flush();
        writer.Close();
        writer = null;
    }

    private void OnDestroy() => StopRecording();
    private void OnApplicationQuit() => StopRecording();
}
