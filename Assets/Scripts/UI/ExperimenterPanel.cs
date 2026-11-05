using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Fenêtre d'administration réservée au rôle « Experimentateur » (F18).
/// Créée en code (voir ShowPersonalisedMenu) devant la caméra ; déplaçable à la gâchette intérieure.
/// Commandes : lancer / arrêter l'enregistrement synchronisé, forcer la fin de session,
/// et un état en direct (session réseau, joueurs connectés, enregistreurs locaux).
/// </summary>
public class ExperimenterPanel : MonoBehaviour
{
    private const float PixelsPerMeter = 1000f;

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
        gameObject.AddComponent<GraphicRaycaster>();
        gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(700f, 620f);
        rect.localScale = Vector3.one / PixelsPerMeter;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.08f, 0.92f);

        var title = CreateText("Title", "Expérimentateur", 40f, FontStyles.Bold);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f); titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -15f);
        titleRect.sizeDelta = new Vector2(-40f, 60f);

        statusText = CreateText("Status", "", 24f, FontStyles.Normal);
        var statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f); statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.offsetMin = new Vector2(25f, 20f);
        statusRect.offsetMax = new Vector2(-25f, -300f);
        statusText.alignment = TextAlignmentOptions.TopLeft;

        float y = -90f;
        y = AddButton("Lancer l'enregistrement synchronisé", new Color(0.2f, 0.55f, 0.25f), y, StartRecording);
        y = AddButton("Arrêter l'enregistrement (fin de tâche)", new Color(0.6f, 0.45f, 0.15f), y, StopRecording);
        AddButton("Forcer la fin de session (tous les casques)", new Color(0.6f, 0.2f, 0.2f), y, ForceExit);
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

    private float AddButton(string label, Color color, float y, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(-50f, 60f);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(action);

        var text = CreateText("Label", label, 24f, FontStyles.Bold);
        text.transform.SetParent(go.transform, false);
        var tr = text.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        return y - 70f;
    }

    private void PlaceInFrontOfCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        transform.position = cam.transform.position + fwd * 1.4f - right * 0.6f + Vector3.up * 0.1f;
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }

    private void Update()
    {
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

        var sync = SyncManager.Instance;
        string recording = sync != null && sync.IsRecordingStarted ? "en cours" : "arrêté";

        var audio = FindObjectsOfType<AudioTracker>();
        var eye = FindObjectsOfType<EyeTrackingDataLogger>();
        var face = FindObjectsOfType<FaceTrackingRecorder>();
        var pos = FindObjectsOfType<PositionLogger>();

        statusText.text =
            $"Session réseau : {session}\n" +
            $"Joueurs connectés : {players}\n" +
            $"Enregistrement synchronisé : {recording}\n" +
            $"Audio (ce casque) : {(audio.Any(a => a.IsRecording) ? "actif" : "inactif")}\n" +
            $"Regard : {(eye.Any(e => e.IsRecording) ? "actif" : "inactif")}   " +
            $"Visage : {(face.Any(f => f.IsRecording) ? "actif" : "inactif")}   " +
            $"Positions : {(pos.Any(p => p.IsRecording) ? "actif" : "inactif")}\n" +
            $"Heure : {System.DateTime.Now:HH:mm:ss}";
    }

    private static HostController LocalHost()
    {
        if (HostController.instance != null) return HostController.instance;
        return FindObjectsOfType<HostController>().FirstOrDefault(h => h.Object != null && h.Object.HasStateAuthority);
    }

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
        if (ExitManager.Instance != null)
            ExitManager.Instance.BeginExit();
        else
            NetworkStatusPanel.Show("ExitManager introuvable : fin de session impossible.", 4f);
    }
}
