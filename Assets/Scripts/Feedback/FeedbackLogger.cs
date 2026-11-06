using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Journal des feedbacks (portage d'Emery Descours, DataFeedbacks) aligné sur les autres enregistreurs :
/// fichier <c>&lt;horodatage&gt;_FeedbackData.csv</c> dans le dossier Data du casque, colonnes de temps
/// <c>Time;NetTime;Utc</c>, ligne MARKER au lancement et END à l'arrêt (via MarkerManager), listé dans le
/// manifeste et envoyé au PC avec le reste.
/// Colonnes : Feedback (gaze | voice | face), Event, Role (rôle local), Target (objet regardé), Value
/// (opacité ou niveau), Count, Total, Ratio, Who (participants concernés, ex. [1,0,1]).
/// </summary>
public class FeedbackLogger : MonoBehaviour
{
    private string filePath;
    private StreamWriter writer;
    private bool isRecording;

    public bool IsRecording => isRecording;
    public string FilePath => filePath;
    public int EntryCount { get; private set; }

    private void Start()
    {
        string folder = Path.Combine(Application.persistentDataPath, "Data");
        Directory.CreateDirectory(folder);
        filePath = Path.Combine(folder, System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_FeedbackData.csv");
        writer = new StreamWriter(filePath, false) { AutoFlush = true };
        writer.WriteLine(SessionClock.HeaderColumns + ";Feedback;Event;Role;Target;Value;Count;Total;Ratio;Who");
        isRecording = true;
        Debug.Log("[FeedbackLogger] Journal des feedbacks : " + filePath);
    }

    public void AddMarker()
    {
        if (writer == null) return;
        writer.WriteLine($"{SessionClock.Stamp()};MARKER");
    }

    public void Log(string feedback, string evt, string target = "", float? value = null, int? count = null, int? total = null, float? ratio = null, string who = "")
    {
        if (writer == null) return;
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
        if (!isRecording) return;
        isRecording = false;
        if (writer != null)
        {
            writer.Write("END");
            writer.Flush();
            writer.Close();
            writer = null;
        }
    }

    private void OnDestroy() => StopRecording();
    private void OnApplicationQuit() => StopRecording();
}
