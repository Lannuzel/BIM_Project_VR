using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Installe et pilote les feedbacks collaboratifs d'Emery Descours dans une scène d'expérience (XP1, XP2, XP3),
/// par code au chargement (SessionCore), dans toutes les scènes ; inerte tant qu'aucun feedback n'est activé
/// (<see cref="SessionConfig"/> ou interrupteurs du panneau de l'expérimentateur, recette du 9 novembre 2026) :
/// - objet réseau <see cref="FeedbackNetwork"/> créé par le maître de session ;
/// - <see cref="GazeFeedback"/>, <see cref="VoiceFeedback"/>, <see cref="FaceFeedback"/> ;
/// - pictogrammes sous le menu de la main gauche (micro barré, ampoule) ;
/// - messages collectifs (panneau suivi par la tête) avec son ;
/// - journal <see cref="FeedbackLogger"/> ; démarrage au marqueur, arrêt avec les enregistrements.
/// </summary>
public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance { get; private set; }

    public const string PrefabResource = "Feedback/FeedbackNetwork";

    /// <summary>Choix fait dans le panneau de l'expérimentateur avant que l'objet réseau existe (prioritaire sur la configuration).</summary>
    private static bool? overrideGaze, overrideVoice, overrideFace;

    /// <summary>État initial d'un feedback pour cette scène : choix de l'expérimentateur, sinon configuration (restreinte par feedbackScenes).</summary>
    public static bool InitialEnabled(int kind)
    {
        bool? chosen = kind == FeedbackNetwork.MessageKindVoice ? overrideVoice : kind == FeedbackNetwork.MessageKindFace ? overrideFace : overrideGaze;
        return chosen ?? FeedbackSettings.InitialEnabled(kind);
    }

    public static bool AnyEnabled => IsEnabled(0) || IsEnabled(FeedbackNetwork.MessageKindVoice) || IsEnabled(FeedbackNetwork.MessageKindFace);

    private FeedbackLogger logger;
    private Image micPicto, bulbPicto;
    private float micPictoUntil;
    private float nextSpawnTry;
    private int seenMessageSeq = -1;
    private AudioClip bell;
    private bool pictosBuilt;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        logger = GetComponent<FeedbackLogger>();
        if (logger == null) logger = gameObject.AddComponent<FeedbackLogger>();
        bell = Resources.Load<AudioClip>("Feedback/notification");

        if (GetComponent<GazeFeedback>() == null) gameObject.AddComponent<GazeFeedback>();
        if (GetComponent<VoiceFeedback>() == null) gameObject.AddComponent<VoiceFeedback>();
        var faceHost = FindObjectOfType<OVRFaceExpressions>();
        if (faceHost != null && faceHost.GetComponent<FaceFeedback>() == null) faceHost.gameObject.AddComponent<FaceFeedback>();
        else if (faceHost == null && GetComponent<FaceFeedback>() == null) gameObject.AddComponent<FaceFeedback>();

        SyncManager.RecordingStartedEvent += OnRecordingStarted;
        SyncManager.RecordingStoppedEvent += OnRecordingStopped;
        StartCoroutine(BuildPictosWhenMenuExists());
        Debug.Log($"[FeedbackManager] Feedbacks installés : {SessionConfig.Instance.FeedbackSummary()}");
    }

    private void OnDestroy()
    {
        SyncManager.RecordingStartedEvent -= OnRecordingStarted;
        SyncManager.RecordingStoppedEvent -= OnRecordingStopped;
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        EnsureNetworkObject();
        WatchMessages();
        // Le gestionnaire est toujours installé : l'ampoule (concentration) n'est visible que si le feedback visage est actif.
        bool faceOn = IsEnabled(FeedbackNetwork.MessageKindFace);
        if (bulbPicto != null && bulbPicto.gameObject.activeSelf != faceOn)
            bulbPicto.gameObject.SetActive(faceOn);
        if (micPicto != null && micPicto.gameObject.activeSelf && Time.unscaledTime >= micPictoUntil)
        {
            micPicto.gameObject.SetActive(false);
            Log("voice", "individual", "", 0f);
        }
    }

    #region Objet réseau

    private void EnsureNetworkObject()
    {
        if (FeedbackNetwork.Instance != null || Time.unscaledTime < nextSpawnTry) return;
        nextSpawnTry = Time.unscaledTime + 2f;
        var runner = NetworkObjectUtils.Runner;
        if (runner == null || !runner.IsRunning || !runner.IsSharedModeMasterClient) return;
        var prefab = Resources.Load<GameObject>(PrefabResource);
        if (prefab == null)
        {
            Debug.LogError($"[FeedbackManager] Prefab Resources/{PrefabResource} introuvable : générer avec Outils BIM VR › Créer le prefab réseau des feedbacks.");
            nextSpawnTry = float.MaxValue;
            return;
        }
        var no = runner.Spawn(prefab, Vector3.zero, Quaternion.identity);
        if (no == null) Debug.LogError("[FeedbackManager] Échec de création de l'objet réseau des feedbacks (table des prefabs Fusion ?).");
    }

    private void OnRecordingStarted()
    {
        var net = FeedbackNetwork.Instance;
        if (net != null && net.HasStateAuthority) net.RPC_SetStarted(true);
        if (AnyEnabled)
            Log("session", "start", "", null, null, null, null, $"gaze={IsEnabled(0)} voice={IsEnabled(FeedbackNetwork.MessageKindVoice)} face={IsEnabled(FeedbackNetwork.MessageKindFace)}");
    }

    private void OnRecordingStopped(bool timeout)
    {
        var net = FeedbackNetwork.Instance;
        if (net != null && net.HasStateAuthority) net.RPC_SetStarted(false);
    }

    /// <summary>Feedback activé (état réseau s'il existe, sinon configuration). kind : 0 regard, 1 parole, 2 visage.</summary>
    public static bool IsEnabled(int kind)
    {
        var net = FeedbackNetwork.Instance;
        if (net != null && net.Object != null && net.Object.IsValid) return net.IsEnabled(kind);
        return InitialEnabled(kind);
    }

    /// <summary>Change l'activation pour tous les casques (RPC vers l'autorité) ou, hors session, dans la configuration locale.</summary>
    public static void SetEnabled(bool gaze, bool voice, bool face)
    {
        var net = FeedbackNetwork.Instance;
        if (net != null && net.Object != null && net.Object.IsValid)
            net.RPC_SetEnabled(gaze, voice, face);
        overrideGaze = gaze; overrideVoice = voice; overrideFace = face;
        Log("session", "toggle", "", null, null, null, null, $"gaze={gaze} voice={voice} face={face}");
    }

    #endregion

    #region Messages collectifs

    private void WatchMessages()
    {
        var net = FeedbackNetwork.Instance;
        if (net == null) return;
        if (seenMessageSeq < 0) { seenMessageSeq = net.MessageSeq; return; }
        if (net.MessageSeq == seenMessageSeq) return;
        seenMessageSeq = net.MessageSeq;

        string text = net.Message;
        NetworkStatusPanel.ShowNamed("Feedback", text, net.MessageSeconds > 0f ? net.MessageSeconds : 10f, 0.12f);
        if (bell != null && Camera.main != null) AudioSource.PlayClipAtPoint(bell, Camera.main.transform.position, 0.8f);
        Log(net.MessageKind == FeedbackNetwork.MessageKindFace ? "face" : "voice", "message", text.Replace('\n', ' ').Replace(';', ','));
    }

    #endregion

    #region Pictogrammes (menu de la main gauche)

    private IEnumerator BuildPictosWhenMenuExists()
    {
        while (!pictosBuilt)
        {
            TryBuildPictos();
            yield return new WaitForSeconds(1f);
        }
    }

    private void TryBuildPictos()
    {
        Transform menu = null;
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name == "BIM Menu" && t.gameObject.scene.IsValid() && t.hideFlags == HideFlags.None && t.GetComponent<RectTransform>() != null
                && t.GetComponentInParent<OVRCameraRig>() != null)
            { menu = t; break; }
        }
        if (menu == null) return;
        if (menu.Find("FeedbackPictos") != null) { pictosBuilt = true; return; }

        var panel = new GameObject("FeedbackPictos", typeof(RectTransform));
        panel.transform.SetParent(menu, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f); rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -3f);
        rect.sizeDelta = new Vector2(0f, 30f);

        micPicto = CreatePicto(panel.transform, "PictoMicro", "Feedback/micro_barre", new Vector2(-22f, 0f), Color.white);
        micPicto.gameObject.SetActive(false);
        bulbPicto = CreatePicto(panel.transform, "PictoAmpoule", "Feedback/ampoule", new Vector2(22f, 0f), new Color(0.118f, 0.188f, 0.314f, 0.333f));
        pictosBuilt = true;
        Debug.Log("[FeedbackManager] Pictogrammes de feedback ajoutés sous le menu de la main gauche.");
    }

    private static Image CreatePicto(Transform parent, string name, string resource, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(25f, 25f);
        var img = go.GetComponent<Image>();
        img.sprite = Resources.Load<Sprite>(resource);
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = color;
        return img;
    }

    public static void ShowMicPicto(float seconds)
    {
        if (Instance == null || Instance.micPicto == null) return;
        Instance.micPicto.gameObject.SetActive(true);
        Instance.micPictoUntil = Time.unscaledTime + seconds;
    }

    public static void SetBulbOpacity(float opacity)
    {
        if (Instance == null || Instance.bulbPicto == null) return;
        var c = Instance.bulbPicto.color;
        c.a = Mathf.Clamp01(opacity);
        Instance.bulbPicto.color = c;
    }

    #endregion

    public static void Log(string feedback, string evt, string target = "", float? value = null, int? count = null, int? total = null, float? ratio = null, string who = "")
    {
        if (Instance == null || Instance.logger == null) return;
        Instance.logger.Log(feedback, evt, target, value, count, total, ratio, who);
    }
}
