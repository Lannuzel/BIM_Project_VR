using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Deux étapes de tutoriel propres aux feedbacks collaboratifs (portage des écrans d'Emery Descours :
/// « Test Feedback Canvas » et « Explication Feedback Canvas »), construites par code et insérées dans
/// <see cref="TutorialManager"/> seulement si un feedback est activé pour la scène d'expérience de l'APK :
/// 1. vérification des capteurs (micro et visage) avant l'accueil ;
/// 2. explication des pictogrammes avant de lancer l'expérience.
/// </summary>
public class TutorialFeedbackSteps : MonoBehaviour
{
    public const string CheckRootName = "FeedbackSensorCheck";
    public const string ExplainRootName = "FeedbackExplanation";
    private const float CanvasScale = 0.003f;

    public const string CheckText =
        "Avant de commencer le tutoriel, vérifiez que les capteurs fonctionnent bien :\n" +
        "le pictogramme du micro doit s'allumer quand vous parlez, celui du visage quand votre visage est suivi.\n\n" +
        "S'ils ne s'allument pas, prévenez l'organisateur.";

    public const string ExplainText =
        "Durant l'expérience, des pictogrammes s'afficheront dans le menu de votre main gauche lorsque vous faites certaines actions.\n\n" +
        "Le micro barré signifie que vous venez de couper la parole à un de vos collaborateurs.\n" +
        "L'ampoule représente votre concentration : plus vous êtes concentré, plus elle est visible.\n" +
        "Lorsque vous regarderez le même endroit qu'un autre participant, l'objet regardé sera entouré d'une bordure blanche ; " +
        "si elle devient bleue, les trois participants regardent le même objet.\n\n" +
        "Ces pictogrammes apparaissent sur le menu de la main gauche : n'hésitez pas à le consulter de temps à autre.";

    private GameObject checkRoot, explainRoot;
    private TutorialManager.Step pendingReplay;
    private GameObject welcomeRoot;

    /// <summary>Construit les deux écrans (inactifs) et insère les étapes dans la liste du tutoriel.</summary>
    public static TutorialFeedbackSteps Install(TutorialManager manager, List<TutorialManager.Step> steps)
    {
        var host = new GameObject("TutorialFeedbackSteps");
        var me = host.AddComponent<TutorialFeedbackSteps>();

        me.checkRoot = me.BuildCanvas(CheckRootName, "Vérification des capteurs", CheckText, "OK",
            new[] { ("Feedback/micro", "Micro"), ("Feedback/visage", "Visage") }, out var checkPictos);
        var check = me.checkRoot.AddComponent<TutorialSensorCheck>();
        check.micImage = checkPictos[0];
        check.faceImage = checkPictos[1];

        me.explainRoot = me.BuildCanvas(ExplainRootName, "Les pictogrammes de feedback", ExplainText, "Lancer l'expérience",
            new[] { ("Feedback/micro_barre", "Parole coupée"), ("Feedback/ampoule", "Concentration"), ("Feedback/contour_blanc", "Regard à deux"), ("Feedback/contour_bleu", "Regard à trois") }, out _);

        steps.Insert(0, new TutorialManager.Step { label = "Vérifier les capteurs", rootName = CheckRootName, okButtonName = "okButton" });
        int loader = steps.FindIndex(s => s.rootName == "ExpSceneLoader");
        var explain = new TutorialManager.Step { label = "Les pictogrammes de feedback", rootName = ExplainRootName, okButtonName = "okButton" };
        if (loader >= 0) steps.Insert(loader, explain); else steps.Add(explain);
        Debug.Log("[TutorialFeedbackSteps] Étapes de feedback ajoutées au tutoriel (capteurs, pictogrammes).");
        return me;
    }

    /// <summary>Après résolution des objets : l'écran des capteurs prend la place de l'accueil, qui attend.</summary>
    public void AfterResolve(GameObject welcome)
    {
        welcomeRoot = welcome;
        if (welcome != null)
        {
            checkRoot.transform.SetPositionAndRotation(welcome.transform.position, welcome.transform.rotation);
            welcome.SetActive(false);
        }
        else
        {
            PlaceInFrontOfCamera(checkRoot.transform);
        }
        checkRoot.SetActive(true);
    }

    /// <summary>
    /// Appelé par le tutoriel quand une étape est validée, avant de rejouer ses actions de scène.
    /// Retourne true si l'enchaînement est pris en charge ici.
    /// </summary>
    public bool Intercept(TutorialManager manager, TutorialManager.Step step)
    {
        if (step.rootName == CheckRootName)
        {
            checkRoot.SetActive(false);
            if (welcomeRoot != null) welcomeRoot.SetActive(true);
            return true;
        }
        if (step.rootName == ExplainRootName)
        {
            explainRoot.SetActive(false);
            var pending = pendingReplay;
            pendingReplay = null;
            if (pending != null) manager.ReplayPersistent(pending);
            return true;
        }
        if (step.rootName == "MoveObjTask" && explainRoot != null && !explainRoot.activeSelf && pendingReplay == null)
        {
            // L'explication s'intercale avant le lancement : les actions de scène de cette étape sont rejouées
            // quand l'écran d'explication est validé.
            pendingReplay = step;
            PlaceInFrontOfCamera(explainRoot.transform);
            explainRoot.SetActive(true);
            return true;
        }
        return false;
    }

    private static void PlaceInFrontOfCamera(Transform t)
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 fwd = cam.transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
        fwd.Normalize();
        t.SetPositionAndRotation(cam.transform.position + fwd * 2.2f + Vector3.up * (1.5f - cam.transform.position.y), Quaternion.LookRotation(fwd, Vector3.up));
    }

    #region Construction des écrans

    private GameObject BuildCanvas(string name, string title, string body, string okLabel, (string resource, string caption)[] pictos, out Image[] pictoImages)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        XRUiCanvas.MakeInteractive(canvas);
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1340f, 720f);
        rect.localScale = Vector3.one * CanvasScale;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.2f, 0.96f);

        var titleText = CreateText(root.transform, "Title", title, 52f, FontStyles.Bold, TextAlignmentOptions.Center);
        var tr = titleText.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -20f); tr.sizeDelta = new Vector2(-60f, 70f);

        var bodyText = CreateText(root.transform, "Body", body, 30f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var br = bodyText.rectTransform;
        br.anchorMin = new Vector2(0f, 1f); br.anchorMax = new Vector2(1f, 1f); br.pivot = new Vector2(0.5f, 1f);
        br.anchoredPosition = new Vector2(0f, -100f); br.sizeDelta = new Vector2(-80f, 330f);

        // Rangée de pictogrammes avec légende
        pictoImages = new Image[pictos.Length];
        float slot = 1340f / Mathf.Max(1, pictos.Length);
        for (int i = 0; i < pictos.Length; i++)
        {
            var img = new GameObject("Picto_" + pictos[i].caption, typeof(RectTransform), typeof(Image));
            img.transform.SetParent(root.transform, false);
            var ir = img.GetComponent<RectTransform>();
            ir.anchorMin = ir.anchorMax = new Vector2(0f, 0f); ir.pivot = new Vector2(0.5f, 0f);
            ir.anchoredPosition = new Vector2(slot * (i + 0.5f), 200f);
            ir.sizeDelta = new Vector2(110f, 110f);
            var image = img.GetComponent<Image>();
            image.sprite = Resources.Load<Sprite>(pictos[i].resource);
            image.preserveAspect = true;
            image.raycastTarget = false;
            pictoImages[i] = image;

            var cap = CreateText(root.transform, "Caption_" + pictos[i].caption, pictos[i].caption, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
            var cr = cap.rectTransform;
            cr.anchorMin = cr.anchorMax = new Vector2(0f, 0f); cr.pivot = new Vector2(0.5f, 1f);
            cr.anchoredPosition = new Vector2(slot * (i + 0.5f), 190f);
            cr.sizeDelta = new Vector2(slot - 20f, 40f);
        }

        var button = new GameObject("okButton", typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(root.transform, false);
        var bRect = button.GetComponent<RectTransform>();
        bRect.anchorMin = bRect.anchorMax = new Vector2(0.5f, 0f); bRect.pivot = new Vector2(0.5f, 0f);
        bRect.anchoredPosition = new Vector2(0f, 40f);
        bRect.sizeDelta = new Vector2(420f, 90f);
        button.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.95f, 1f);
        var label = CreateText(button.transform, "Label", okLabel, 34f, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);

        root.SetActive(false);
        return root;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    #endregion
}

/// <summary>
/// Écran de vérification des capteurs : le pictogramme du micro s'allume (vert) quand un son dépasse le seuil,
/// celui du visage quand le suivi facial fournit des expressions valides. Le niveau sonore est lu dans la
/// capture de l'AudioTracker si elle est active, sinon dans une capture propre, fermée à la fin de l'étape.
/// </summary>
public class TutorialSensorCheck : MonoBehaviour
{
    public Image micImage;
    public Image faceImage;
    [Tooltip("Niveau crête (0..1) à partir duquel le micro est considéré actif.")]
    public float voiceThreshold = 0.02f;

    private static readonly Color Off = new Color(0.5f, 0.5f, 0.5f, 0.6f);
    private static readonly Color On = new Color(0.3f, 0.95f, 0.4f, 1f);

    private AudioTracker audioTracker;
    private AudioClip ownClip;
    private string ownDevice;
    private OVRFaceExpressions face;
    private float[] buffer = new float[256];

    private void OnEnable()
    {
        audioTracker = FindObjectOfType<AudioTracker>();
        face = FindObjectOfType<OVRFaceExpressions>();
        if (face == null && Camera.main != null)
            face = Camera.main.gameObject.AddComponent<OVRFaceExpressions>();
        if (micImage != null) micImage.color = Off;
        if (faceImage != null) faceImage.color = Off;
    }

    private void OnDisable()
    {
        if (ownClip != null && !string.IsNullOrEmpty(ownDevice) && Microphone.IsRecording(ownDevice))
            Microphone.End(ownDevice);
        ownClip = null;
    }

    private void Update()
    {
        float peak = 0f;
        bool hasMic = audioTracker != null && audioTracker.TryGetRecentPeak(256, out peak);
        if (!hasMic) hasMic = OwnMicPeak(out peak);
        if (micImage != null) micImage.color = hasMic && peak > voiceThreshold ? On : Off;

        bool faceOk = face != null && face.FaceTrackingEnabled && face.ValidExpressions;
        if (faceImage != null) faceImage.color = faceOk ? On : Off;
    }

    private bool OwnMicPeak(out float peak)
    {
        peak = 0f;
        if (ownClip == null)
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0) return false;
            ownDevice = Microphone.devices[0];
            ownClip = Microphone.Start(ownDevice, true, 1, 44100);
            return false;
        }
        int pos = Microphone.GetPosition(ownDevice) - buffer.Length;
        if (pos < 0) return false;
        ownClip.GetData(buffer, pos);
        for (int i = 0; i < buffer.Length; i++)
        {
            float a = Mathf.Abs(buffer[i]);
            if (a > peak) peak = a;
        }
        return true;
    }
}
