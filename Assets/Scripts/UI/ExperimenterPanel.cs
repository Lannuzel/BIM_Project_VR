using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Fenêtre d'administration réservée au rôle « Experimentateur » (F18).
/// Créée en code (voir ShowPersonalisedMenu) devant la caméra ; déplaçable à la gâchette intérieure.
/// Commandes : lancer / arrêter l'enregistrement synchronisé, forcer la fin de session, changer de rôle
/// (échange avec un joueur simulé qui le tient déjà), ajouter / retirer des joueurs simulés,
/// et un état en direct (session réseau, joueurs et leurs rôles, enregistreurs locaux).
/// </summary>
public class ExperimenterPanel : MonoBehaviour
{
    private const float PixelsPerMeter = 1000f;
    private const float Width = 760f;

    private TextMeshProUGUI statusText;
    private float nextRefresh;

    public static ExperimenterPanel Create()
    {
        var existing = FindObjectOfType<ExperimenterPanel>();
        if (existing != null) return existing;

        var go = new GameObject("ExperimenterPanel");
        var panel = go.AddComponent<ExperimenterPanel>();
        panel.Build();
        panel.PlaceInFrontOfCamera();
        go.AddComponent<WindowGrabMover>();
        return panel;
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        gameObject.AddComponent<CanvasScaler>();
        XRUiCanvas.MakeInteractive(canvas); // pointeur laser Meta (OVRRaycaster) + XRI

        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(Width, 1180f);
        rect.localScale = Vector3.one / PixelsPerMeter;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.08f, 0.94f);

        var title = CreateText("Title", "Expérimentateur", 40f, FontStyles.Bold);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f); titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);
        titleRect.sizeDelta = new Vector2(-40f, 56f);

        float y = -80f;

        // --- Session ---
        y = AddSectionLabel("Session", y);
        y = AddButton("Lancer l'enregistrement synchronisé", new Color(0.2f, 0.55f, 0.25f), y, StartRecording);
        y = AddButton("Arrêter l'enregistrement (fin de tâche)", new Color(0.6f, 0.45f, 0.15f), y, StopRecording);
        y = AddButton("Forcer la fin de session (tous les casques)", new Color(0.6f, 0.2f, 0.2f), y, ForceExit);

        // --- Feedbacks collaboratifs (Emery Descours) : identiques sur les trois casques (RPC) ---
        y = AddSectionLabel("Feedbacks (regard, parole, visage) : condition expérimentale", y);
        y = AddButtonRow(new[]
        {
            ("Regard", (UnityEngine.Events.UnityAction)(() => ToggleFeedback(0))),
            ("Parole", (UnityEngine.Events.UnityAction)(() => ToggleFeedback(1))),
            ("Visage", (UnityEngine.Events.UnityAction)(() => ToggleFeedback(2))),
        }, new Color(0.35f, 0.3f, 0.55f), y);
        feedbackLabels = new[] { FindButtonLabel("Regard"), FindButtonLabel("Parole"), FindButtonLabel("Visage") };

        // --- Rôle du casque ---
        y = AddSectionLabel("Mon rôle (échange avec un joueur simulé s'il le tient)", y);
        y = AddButtonRow(AdminTools.Roles.Select(r => (Label(r), (UnityEngine.Events.UnityAction)(() => AdminTools.SetLocalRole(r)))).ToArray(),
                         new Color(0.2f, 0.35f, 0.6f), y);

        // --- Joueurs simulés ---
        y = AddSectionLabel("Joueurs simulés (comptés comme connectés)", y);
        y = AddButtonRow(new[]
        {
            ("+ Ajouter", (UnityEngine.Events.UnityAction)(() => AdminTools.AddSimulatedPlayer())),
            ("- Retirer le dernier", (UnityEngine.Events.UnityAction)AdminTools.RemoveLastSimulatedPlayer),
            ("Retirer tous", (UnityEngine.Events.UnityAction)AdminTools.RemoveAllSimulatedPlayers),
        }, new Color(0.35f, 0.3f, 0.55f), y);

        statusText = CreateText("Status", "", 23f, FontStyles.Normal);
        var statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f); statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.offsetMin = new Vector2(25f, 15f);
        statusRect.offsetMax = new Vector2(-25f, y - 10f);
        statusText.alignment = TextAlignmentOptions.TopLeft;
    }

    private static string Label(string role)
    {
        switch (role)
        {
            case AdminTools.Modelisateur: return "Modélisateur";
            case AdminTools.Experimentateur: return "Expérim.";
            default: return role;
        }
    }

    private TextMeshProUGUI CreateText(string name, string text, float size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private float AddSectionLabel(string label, float y)
    {
        var text = CreateText("Section_" + label, label, 22f, FontStyles.Italic);
        text.color = new Color(1f, 0.85f, 0.6f);
        text.alignment = TextAlignmentOptions.Left;
        var r = text.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, y);
        r.sizeDelta = new Vector2(-50f, 34f);
        return y - 38f;
    }

    private float AddButton(string label, Color color, float y, UnityEngine.Events.UnityAction action)
    {
        var text = CreateButton(label, color, new Vector2(0f, y), new Vector2(Width - 50f, 64f), new Vector2(0.5f, 1f), action);
        if (action == (UnityEngine.Events.UnityAction)ForceExit) exitButtonLabel = text;
        return y - 76f;
    }

    private float AddButtonRow((string label, UnityEngine.Events.UnityAction action)[] buttons, Color color, float y)
    {
        float gap = 10f;
        float width = (Width - 50f - gap * (buttons.Length - 1)) / buttons.Length;
        float left = -(Width - 50f) / 2f;
        for (int i = 0; i < buttons.Length; i++)
        {
            float x = left + i * (width + gap);
            CreateButton(buttons[i].label, color, new Vector2(x, y), new Vector2(width, 56f), new Vector2(0f, 1f), buttons[i].action);
        }
        return y - 64f;
    }

    private TextMeshProUGUI CreateButton(string label, Color color, Vector2 anchoredPos, Vector2 size, Vector2 pivot, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f); rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(action);

        var text = CreateText("Label", label, 22f, FontStyles.Bold);
        text.transform.SetParent(go.transform, false);
        var tr = text.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(4f, 0f); tr.offsetMax = new Vector2(-4f, 0f);
        return text;
    }

    private void PlaceInFrontOfCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        transform.position = cam.transform.position + fwd * 1.5f - right * 0.7f + Vector3.up * 0.15f;
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }

    private void Update()
    {
        Update_ExitLabel();
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.5f;
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        var nm = NetworkManager.Instance;
        string session = nm == null || nm.Runner == null ? "non démarrée"
            : nm.IsRunning ? "active" : (nm.IsReconnecting ? "reconnexion..." : "connexion...");
        int players = nm != null && nm.IsRunning ? nm.Runner.ActivePlayers.Count() : 0;
        int simulated = AdminTools.SimulatedCount;

        var sync = SyncManager.Instance;
        string recording = sync != null && sync.IsRecordingStarted ? "en cours" : "arrêté";

        var audio = FindObjectsOfType<AudioTracker>();
        var eye = FindObjectsOfType<EyeTrackingDataLogger>();
        var face = FindObjectsOfType<FaceTrackingRecorder>();
        var pos = FindObjectsOfType<PositionLogger>();

        var sb = new StringBuilder();
        sb.AppendLine($"Session réseau : {session}   Joueurs : {players} réel(s) + {simulated} simulé(s)");
        sb.AppendLine($"Mon rôle : {AdminTools.LocalRole}   Enregistrement synchronisé : {recording}");
        sb.AppendLine("Enregistrements (mis à jour en continu) :");
        foreach (var a in audio)
            sb.AppendLine($"  • Audio : {(a.IsRecording ? $"actif, {a.RecordedSeconds / 60f:0.0} min" : "en attente du lancement")}  {ShortName(a.FilePath)}");
        foreach (var e in eye)
            sb.AppendLine($"  • Regard : {(e.IsRecording ? "actif" : "arrêté")}, {e.SampleCount:n0} éch.  {ShortName(e.FilePath)}");
        foreach (var f in face)
            sb.AppendLine($"  • Visage : {(f.IsRecording ? "actif" : "arrêté")}, {f.SampleCount:n0} éch.  {ShortName(f.FilePath)}");
        foreach (var p in pos)
            sb.AppendLine($"  • Positions : {(p.IsRecording ? "actif" : "arrêté")}, {p.SampleCount:n0} éch.  {ShortName(p.FilePath)}");
        foreach (var c in FindObjectsOfType<ChairPositionDataLogger>())
            sb.AppendLine($"  • Objets (bilan de fin) : {(c.IsRecording ? "ouvert" : "fermé")}, {c.EntryCount} objet(s)  {ShortName(c.FilePath)}");
        foreach (var r in FindObjectsOfType<ReservationPositionDataLogger>())
            sb.AppendLine($"  • Réservations (bilan de fin) : {(r.IsRecording ? "ouvert" : "fermé")}, {r.EntryCount} réservation(s)  {ShortName(r.FilePath)}");
        sb.AppendLine("Avatars :");
        foreach (var p in AdminTools.AllAvatars().OrderBy(p => p.IsSimulated ? 1 : 0).ThenBy(p => p.PlayerName))
        {
            string kind = p.IsSimulated ? "simulé" : (p.Object.HasStateAuthority ? "moi" : "casque distant");
            sb.AppendLine($"  • {p.PlayerName}  ({kind})");
        }
        RefreshFeedbackLabels();
        string feedbackState = FeedbackManager.Instance == null ? "non installés dans cette scène"
            : $"regard {(FeedbackManager.IsEnabled(0) ? "ON" : "OFF")}, parole {(FeedbackManager.IsEnabled(1) ? "ON" : "OFF")}, visage {(FeedbackManager.IsEnabled(2) ? "ON" : "OFF")}{(FeedbackNetwork.Instance == null ? " (objet réseau en attente)" : "")}";
        sb.AppendLine($"Feedbacks : {feedbackState}");
        sb.AppendLine(SessionConfig.Instance.Summary());
        sb.AppendLine($"Envoi des données : {DataUploader.LastStatus}");
        sb.Append($"Heure : {System.DateTime.Now:HH:mm:ss}");
        statusText.text = sb.ToString();
    }

    private TextMeshProUGUI[] feedbackLabels;

    private TextMeshProUGUI FindButtonLabel(string label)
    {
        var t = transform.Find("Button_" + label);
        return t != null ? t.GetComponentInChildren<TextMeshProUGUI>() : null;
    }

    private void ToggleFeedback(int kind)
    {
        bool gaze = FeedbackManager.IsEnabled(0), voice = FeedbackManager.IsEnabled(1), face = FeedbackManager.IsEnabled(2);
        if (kind == 0) gaze = !gaze; else if (kind == 1) voice = !voice; else face = !face;
        FeedbackManager.SetEnabled(gaze, voice, face);
        RefreshFeedbackLabels();
    }

    private void RefreshFeedbackLabels()
    {
        if (feedbackLabels == null) return;
        string[] names = { "Regard", "Parole", "Visage" };
        for (int i = 0; i < 3; i++)
            if (feedbackLabels[i] != null)
                feedbackLabels[i].text = $"{names[i]} : {(FeedbackManager.IsEnabled(i) ? "ON" : "OFF")}";
    }

    private static string ShortName(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        return System.IO.Path.GetFileName(path);
    }

    private static HostController LocalHost()
    {
        if (HostController.instance != null) return HostController.instance;
        return FindObjectsOfType<HostController>().FirstOrDefault(h => h.Object != null && h.Object.HasStateAuthority && !AdminTools.IsSimulated(h.Object));
    }

    private static ExitManager LocalExit()
    {
        if (ExitManager.Instance != null) return ExitManager.Instance;
        return FindObjectsOfType<ExitManager>().FirstOrDefault(e => e.Object != null && e.Object.IsValid && !AdminTools.IsSimulated(e.Object));
    }

    private float exitConfirmUntil = -1f;
    private TextMeshProUGUI exitButtonLabel;

    private void StartRecording()
    {
        var host = LocalHost();
        if (host == null) { NetworkStatusPanel.Show("Aucun joueur réseau local : impossible de lancer.", 4f); return; }
        host.StartLogSynchornisation();
    }

    private void StopRecording()
    {
        var host = LocalHost();
        if (host == null) { NetworkStatusPanel.Show("Aucun joueur réseau local : impossible d'arrêter.", 4f); return; }
        host.StoptLogSynchornisation();
    }

    private void ForceExit()
    {
        // Confirmation en deux clics (4 s) : évite une fermeture de tous les casques par un clic mal placé.
        if (Time.unscaledTime > exitConfirmUntil)
        {
            exitConfirmUntil = Time.unscaledTime + 4f;
            if (exitButtonLabel != null) exitButtonLabel.text = "Confirmer la fin de session ? (cliquez à nouveau)";
            NetworkStatusPanel.Show("Cliquez à nouveau pour fermer tous les casques.", 4f);
            return;
        }
        exitConfirmUntil = -1f;
        if (exitButtonLabel != null) exitButtonLabel.text = "Forcer la fin de session (tous les casques)";

        var exit = LocalExit();
        if (exit != null)
            exit.BeginExit();
        else
        {
            NetworkStatusPanel.Show("ExitManager introuvable : fermeture locale seulement.", 4f);
            ExitManager.QuitLocallyAfter(2f);
        }
    }

    private void Update_ExitLabel()
    {
        if (exitButtonLabel != null && exitConfirmUntil > 0f && Time.unscaledTime > exitConfirmUntil)
        {
            exitConfirmUntil = -1f;
            exitButtonLabel.text = "Forcer la fin de session (tous les casques)";
        }
    }
}
