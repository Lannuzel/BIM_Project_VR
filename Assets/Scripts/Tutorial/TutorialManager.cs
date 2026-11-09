using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Pilote le tutoriel (F2, F3, F4, F5) sans modifier le câblage existant des boutons OK :
/// - un panneau de progression (liste des étapes, grisées tant qu'elles ne sont pas validées) ;
/// - la vérification des champs de l'étape 1 (nom, âge) avec avertissement à côté du champ ;
/// - la validation des étapes « tracer 4 lignes » et « déplacer l'objet » avant de passer à la suite.
/// Les listeners persistants des boutons OK (SetActive des étapes) sont désactivés au démarrage
/// et rejoués seulement quand l'étape est valide.
/// Les objets sont retrouvés par leur nom dans la scène ; les références peuvent aussi être assignées.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Serializable]
    public class Step
    {
        [Tooltip("Libellé affiché dans le panneau de progression.")]
        public string label;
        [Tooltip("Nom de l'objet racine de l'étape dans la scène (ex. Task1EnterName).")]
        public string rootName;
        [Tooltip("Nom du bouton OK de l'étape (recherché sous la racine).")]
        public string okButtonName = "OKButton";
        [NonSerialized] public GameObject root;
        [NonSerialized] public Button okButton;
        [NonSerialized] public bool done;
        [NonSerialized] public TMP_Text progressLine;
        [NonSerialized] public TMP_Text hintText;
    }

    [Header("Étapes (ordre d'affichage)")]
    public List<Step> steps = new List<Step>
    {
        new Step { label = "Bienvenue", rootName = "WecomeCanvas", okButtonName = "okButton" },
        new Step { label = "Saisir vos informations", rootName = "Task1EnterName" },
        new Step { label = "Tracer 4 lignes sur le tableau", rootName = "DrawLineTask" },
        new Step { label = "Placer l'objet vert sur l'ombre bleue", rootName = "MoveObjTask" },
        new Step { label = "Lancer l'expérience", rootName = "ExpSceneLoader", okButtonName = "" },
    };

    [Header("Étape 1 : champs à vérifier (F3)")]
    [Tooltip("Noms des objets parents des champs TMP_InputField (sous Task1EnterNameCanvas/Panel).")]
    public List<string> requiredFieldParents = new List<string> { "name", "Age" };
    [Tooltip("Champs qui doivent contenir un nombre.")]
    public List<string> numericFieldParents = new List<string> { "Age" };

    [Header("Panneau de progression (F2)")]
    [Tooltip("Ancre du panneau. Si vide : à droite du canvas d'accueil (WecomeCanvas).")]
    public Transform progressAnchor;
    public Vector3 progressOffset = new Vector3(1.6f, 0f, 0f);
    [Tooltip("Recette du 6 novembre 2026 : poser le panneau sur le mur à droite du canvas d'accueil (trouvé par lancer " +
             "de rayon vers la droite), centré horizontalement sur ce mur, au lieu de le laisser chevaucher le canvas.")]
    public bool progressOnRightWall = true;
    [Tooltip("Hauteur (m) du centre du panneau quand il est posé sur le mur.")]
    public float progressWallHeight = 1.5f;
    [Tooltip("Décalage (m) du panneau par rapport au mur, vers la salle.")]
    public float progressWallOffset = 0.03f;
    [Tooltip("Portée (m) de la recherche du mur de droite.")]
    public float progressWallMaxDistance = 12f;
    [Tooltip("Recette du 9 novembre 2026 : le mur de droite porte aussi la consigne « déplacer l'objet » (canvas MoveObject, 0,5 m devant " +
             "le mur). Les canvas posés à moins de cette distance (m) du mur sont des obstacles : le panneau est décalé le long du mur " +
             "vers la zone libre la plus proche du centre.")]
    public float progressWallObstacleDepth = 1f;
    [Tooltip("Marge (m) gardée entre le panneau et un obstacle ou le bord du mur.")]
    public float progressWallClearance = 0.15f;
    [Tooltip("Échelle du panneau (unités canvas -> mètres). 0,0025 donne environ 1,7 m de large.")]
    public float panelScale = 0.0025f;
    public float progressTitleFontSize = 44f;
    public float progressLineFontSize = 36f;
    public Color pendingColor = new Color(0.62f, 0.62f, 0.62f, 1f);
    public Color activeColor = new Color(1f, 0.9f, 0.3f, 1f);
    public Color doneColor = new Color(0.45f, 0.95f, 0.45f, 1f);
    public Color warningColor = new Color(1f, 0.35f, 0.3f, 1f);
    public Color progressBackground = new Color(0.05f, 0.08f, 0.2f, 0.96f);
    public Color progressTitleBar = new Color(0.15f, 0.45f, 0.95f, 1f);

    private DrawLineTutorial drawLine;
    private RaycastSelectAndMoveTutorial moveObject;
    private readonly Dictionary<TMP_InputField, TMP_Text> fieldWarnings = new Dictionary<TMP_InputField, TMP_Text>();
    private readonly List<TMP_InputField> requiredFields = new List<TMP_InputField>();
    private bool replaying;
    private GameObject progressPanel;
    private int currentStep = -1;

    private TutorialFeedbackSteps feedbackSteps;

    private void Start()
    {
        // Feedbacks collaboratifs (Emery Descours) : deux écrans de plus si un feedback est activé pour l'expérience de cet APK.
        if (FeedbackSettings.EnabledForNextExperiment)
            feedbackSteps = TutorialFeedbackSteps.Install(this, steps);
        ResolveSceneObjects();
        if (feedbackSteps != null)
        {
            var welcome = GetStep("WecomeCanvas");
            feedbackSteps.AfterResolve(welcome != null ? welcome.root : null);
        }
        BuildProgressPanel();
        BuildQuickActions();
        HookButtons();
        HookFields();
        RefreshProgress();
    }

    #region Raccourcis : passer le tutoriel

    [Header("Raccourcis (sous le panneau de progression)")]
    [Tooltip("Afficher le bouton « Passer le tutoriel » sous le panneau de progression.")]
    public bool showQuickActions = true;
    public string skipLabel = "Passer le tutoriel ›";

    private GameObject quickPanel;

    private void BuildQuickActions()
    {
        if (!showQuickActions || progressPanel == null) return;

        quickPanel = new GameObject("TutorialQuickActions");
        var canvas = quickPanel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        quickPanel.AddComponent<CanvasScaler>();
        XRUiCanvas.MakeInteractive(canvas); // pointeur laser Meta (OVRRaycaster) + XRI

        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(700f, 130f);
        rect.localScale = Vector3.one * panelScale;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(quickPanel.transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = progressBackground;

        // Juste sous le panneau de progression
        var progressRect = progressPanel.GetComponent<RectTransform>();
        float progressHeight = progressRect.sizeDelta.y * panelScale;
        quickPanel.transform.SetPositionAndRotation(
            progressPanel.transform.position - progressPanel.transform.up * (progressHeight / 2f + 0.03f + rect.sizeDelta.y * panelScale / 2f),
            progressPanel.transform.rotation);

        // Recette du 9 novembre 2026 : plus de choix du rôle ici (le rôle se choisit dans la scène d'expérience ;
        // le mode expérimentateur s'obtient par A, A, B, B sur la manette droite, voir ExperimenterShortcut).
        CreateQuickButton(skipLabel, new Vector2(0f, -30f), new Vector2(660f, 70f), new Vector2(0.5f, 1f), SkipTutorial);
    }

    private Image CreateQuickButton(string text, Vector2 pos, Vector2 size, Vector2 pivot, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("Button_" + text, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(quickPanel.transform, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 1f); r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = pivot;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = new Color(0.2f, 0.35f, 0.6f, 1f);
        go.GetComponent<Button>().onClick.AddListener(action);

        var t = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(go.transform, false);
        var tr = t.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(4f, 0f); tr.offsetMax = new Vector2(-4f, 0f);
        t.text = text;
        t.fontSize = progressLineFontSize * 0.72f;
        t.fontStyle = FontStyles.Bold;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return img;
    }

    /// <summary>Charge directement la scène d'expérience (même action que la fin du tutoriel).</summary>
    public void SkipTutorial()
    {
        foreach (var loader in Resources.FindObjectsOfTypeAll<SceneLoader>())
        {
            if (!loader.gameObject.scene.IsValid()) continue;
            Debug.Log("[TutorialManager] Tutoriel passé : chargement de la scène d'expérience.");
            loader.LoadSceneByIndex();
            return;
        }
        Debug.LogError("[TutorialManager] SceneLoader introuvable : impossible de passer le tutoriel.");
    }

    #endregion

    private void Update()
    {
        // Suit l'étape active (les étapes s'activent/désactivent par SetActive).
        int active = -1;
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i].root != null && steps[i].root.activeInHierarchy && !steps[i].done)
            {
                active = i;
                break;
            }
        }
        if (active != currentStep)
        {
            currentStep = active;
            RefreshProgress();
        }

        UpdateHints();
    }

    #region Résolution des objets

    private void ResolveSceneObjects()
    {
        foreach (var step in steps)
        {
            step.root = FindInScene(step.rootName);
            if (step.root == null)
            {
                Debug.LogWarning($"[TutorialManager] Étape « {step.label} » : objet « {step.rootName} » introuvable.");
                continue;
            }
            if (!string.IsNullOrEmpty(step.okButtonName))
            {
                var t = FindChildRecursive(step.root.transform, step.okButtonName);
                step.okButton = t != null ? t.GetComponent<Button>() : null;
                if (step.okButton == null)
                    Debug.LogWarning($"[TutorialManager] Étape « {step.label} » : bouton « {step.okButtonName} » introuvable.");
            }
        }

        drawLine = FindObjectOfType<DrawLineTutorial>(true);
        moveObject = FindObjectOfType<RaycastSelectAndMoveTutorial>(true);

        var task1 = GetStep("Task1EnterName");
        if (task1 != null && task1.root != null)
        {
            foreach (var parentName in requiredFieldParents)
            {
                var parent = FindChildRecursive(task1.root.transform, parentName);
                var field = parent != null ? parent.GetComponentInChildren<TMP_InputField>(true) : null;
                if (field != null)
                    requiredFields.Add(field);
                else
                    Debug.LogWarning($"[TutorialManager] Champ « {parentName} » introuvable sous Task1EnterName.");
            }
        }
    }

    private Step GetStep(string rootName)
    {
        foreach (var s in steps)
            if (s.rootName == rootName) return s;
        return null;
    }

    private static GameObject FindInScene(string name)
    {
        // Inclut les objets inactifs (contrairement à GameObject.Find).
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name == name && t.gameObject.scene.IsValid() && t.hideFlags == HideFlags.None)
                return t.gameObject;
        }
        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    #endregion

    #region Boutons OK (F3, F4, F5)

    private void HookButtons()
    {
        foreach (var step in steps)
        {
            if (step.okButton == null) continue;

            var s = step;
            // Les actions persistantes (SetActive des étapes) sont suspendues : rejouées si l'étape est valide.
            int count = s.okButton.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
                s.okButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

            s.okButton.onClick.AddListener(() => OnOkClicked(s));
        }
    }

    private void OnOkClicked(Step step)
    {
        if (replaying) return;

        if (!Validate(step, out string message))
        {
            ShowHint(step, message, true);
            return;
        }

        ShowHint(step, string.Empty, false);
        step.done = true;
        RefreshProgress();

        if (feedbackSteps != null && feedbackSteps.Intercept(this, step))
            return;

        ReplayPersistent(step);
    }

    /// <summary>Rejoue les actions câblées dans la scène sur le bouton OK de l'étape (passage à l'étape suivante).</summary>
    public void ReplayPersistent(Step step)
    {
        if (step == null || step.okButton == null) return;
        replaying = true;
        try
        {
            int count = step.okButton.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
                step.okButton.onClick.SetPersistentListenerState(i, UnityEventCallState.RuntimeOnly);
            step.okButton.onClick.Invoke();
            for (int i = 0; i < count; i++)
                step.okButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }
        finally
        {
            replaying = false;
        }
    }

    private bool Validate(Step step, out string message)
    {
        message = string.Empty;

        switch (step.rootName)
        {
            case "Task1EnterName":
                return ValidateFields(out message);

            case "DrawLineTask":
                if (drawLine == null) return true;
                if (drawLine.IsCompleted) return true;
                int missing = Mathf.Max(0, drawLine.requiredLines - drawLine.ValidLineCount);
                message = $"Tracez encore {missing} ligne{(missing > 1 ? "s" : "")} sur le tableau avant de continuer.";
                return false;

            case "MoveObjTask":
                if (moveObject == null) return true;
                if (moveObject.IsCompleted) return true;
                float err = moveObject.CurrentPositionError();
                message = err >= 0f
                    ? $"Posez l'objet vert sur l'ombre bleue avant de continuer (écart : {err * 100f:0} cm, toléré : {moveObject.positionTolerance * 100f:0} cm)."
                    : "Posez l'objet vert sur l'ombre bleue avant de continuer.";
                return false;
        }
        return true;
    }

    #endregion

    #region Champs de saisie (F3)

    private void HookFields()
    {
        foreach (var field in requiredFields)
        {
            var f = field;
            f.onValueChanged.AddListener(_ => ValidateField(f, false));
            f.onEndEdit.AddListener(_ => ValidateField(f, true));
        }
    }

    private bool ValidateFields(out string message)
    {
        bool allOk = true;
        message = string.Empty;
        foreach (var field in requiredFields)
        {
            if (!ValidateField(field, true))
                allOk = false;
        }
        if (!allOk)
            message = "Complétez les champs signalés avant de continuer.";
        return allOk;
    }

    private bool ValidateField(TMP_InputField field, bool showWarning)
    {
        string text = (field.text ?? string.Empty).Trim();
        string parentName = field.transform.parent != null ? field.transform.parent.name : field.name;
        string warning = string.Empty;

        if (string.IsNullOrEmpty(text))
            warning = "Champ obligatoire";
        else if (numericFieldParents.Contains(parentName) && !int.TryParse(text, out _))
            warning = "Entrez un nombre";

        bool ok = string.IsNullOrEmpty(warning);
        SetFieldWarning(field, ok || !showWarning ? string.Empty : warning);
        return ok;
    }

    private void SetFieldWarning(TMP_InputField field, string warning)
    {
        if (!fieldWarnings.TryGetValue(field, out var label) || label == null)
        {
            label = CreateWarningLabel(field);
            fieldWarnings[field] = label;
        }
        label.text = warning;
        label.gameObject.SetActive(!string.IsNullOrEmpty(warning));
    }

    private TMP_Text CreateWarningLabel(TMP_InputField field)
    {
        var fieldRect = field.GetComponent<RectTransform>();
        var go = new GameObject("Warning", typeof(RectTransform));
        go.transform.SetParent(field.transform.parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = fieldRect.anchorMin;
        rect.anchorMax = fieldRect.anchorMax;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(fieldRect.rect.width, fieldRect.rect.height);
        // À droite du champ
        rect.anchoredPosition = fieldRect.anchoredPosition + new Vector2(fieldRect.rect.width * (1f - fieldRect.pivot.x) + 12f, 0f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = Mathf.Max(14f, fieldRect.rect.height * 0.5f);
        tmp.color = warningColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        MakeNonBlocking(go);
        go.SetActive(false);
        return tmp;
    }

    #endregion

    #region Messages d'aide dans les panneaux

    private void UpdateHints()
    {
        var dl = GetStep("DrawLineTask");
        if (dl != null && dl.root != null && dl.root.activeInHierarchy && drawLine != null && !dl.done)
        {
            string msg = $"Lignes valides : {drawLine.ValidLineCount}/{drawLine.requiredLines}";
            if (!string.IsNullOrEmpty(drawLine.LastMessage)) msg += "\n" + drawLine.LastMessage;
            ShowHint(dl, msg, !drawLine.IsCompleted);
        }

        var mo = GetStep("MoveObjTask");
        if (mo != null && mo.root != null && mo.root.activeInHierarchy && moveObject != null && !mo.done)
        {
            string msg = moveObject.LastMessage;
            if (!string.IsNullOrEmpty(moveObject.CurrentModeLabel))
                msg = $"Mode : {moveObject.CurrentModeLabel} (bouton A pour changer)\n" + msg;
            ShowHint(mo, msg, !moveObject.IsCompleted);
        }
    }

    [Header("Bande d'aide sous les panneaux")]
    [Tooltip("Hauteur (unités canvas) ajoutée en bas de chaque panneau d'étape pour accueillir le texte d'aide, sans recouvrir le bouton OK.")]
    public float hintBandHeight = 130f;
    public float hintFontSize = 28f;

    /// <summary>Empêche un texte créé en code d'intercepter les rayons de la manette (le bouton OK restait masqué).</summary>
    private static void MakeNonBlocking(GameObject go)
    {
        var group = go.GetComponent<CanvasGroup>();
        if (group == null) group = go.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    private void ShowHint(Step step, string message, bool isWarning)
    {
        if (step == null || step.root == null) return;

        if (step.hintText == null)
        {
            // Le panneau est agrandi vers le bas d'une bande dédiée : le texte d'aide n'y recouvre ni le
            // bouton OK ni le texte de consigne (les enfants sont ancrés au centre, ils ne bougent pas).
            Transform parent = step.okButton != null ? step.okButton.transform.parent : step.root.transform;
            var canvas = parent.GetComponentInParent<Canvas>();
            var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (canvasRect != null)
                canvasRect.sizeDelta += new Vector2(0f, hintBandHeight);

            var go = new GameObject("TutorialHint", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            rect.sizeDelta = new Vector2(-40f, hintBandHeight - 16f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = hintFontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            MakeNonBlocking(go);
            step.hintText = tmp;
        }

        step.hintText.text = message ?? string.Empty;
        step.hintText.color = isWarning ? warningColor : doneColor;
    }

    #endregion

    #region Panneau de progression (F2)

    private void BuildProgressPanel()
    {
        progressPanel = new GameObject("TutorialProgressPanel");
        var canvas = progressPanel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        progressPanel.AddComponent<CanvasScaler>();

        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(700f, 90f + 80f * steps.Count);
        rect.localScale = Vector3.one * panelScale;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(progressPanel.transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = progressBackground;

        // Bandeau de titre coloré pour que le panneau soit repérable de loin
        var bar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(progressPanel.transform, false);
        var barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 1f); barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta = new Vector2(0f, 80f);
        bar.GetComponent<Image>().color = progressTitleBar;

        var title = CreateLine(progressPanel.transform, "Étapes du tutoriel", progressTitleFontSize, 0);
        title.color = Color.white;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;

        for (int i = 0; i < steps.Count; i++)
        {
            steps[i].progressLine = CreateLine(progressPanel.transform, $"{i + 1}. {steps[i].label}", progressLineFontSize, i + 1);
        }
        MakeNonBlocking(progressPanel);

        // Placement : ancre explicite, sinon à droite du canvas d'accueil.
        Transform anchor = progressAnchor;
        if (anchor == null)
        {
            var welcome = GetStep("WecomeCanvas");
            if (welcome != null && welcome.root != null)
                anchor = welcome.root.transform;
        }
        if (anchor != null && progressOnRightWall && PlaceOnRightWall(anchor))
        {
            // posé sur le mur de droite
        }
        else if (anchor != null)
        {
            progressPanel.transform.SetPositionAndRotation(anchor.TransformPoint(progressOffset / Mathf.Max(0.0001f, anchor.lossyScale.x)), anchor.rotation);
            if (anchor.lossyScale.x > 0.5f) // ancre non mise à l'échelle : offset en mètres
                progressPanel.transform.position = anchor.position + anchor.rotation * progressOffset;
        }
        else
        {
            var cam = Camera.main;
            if (cam != null)
                progressPanel.transform.SetPositionAndRotation(cam.transform.position + cam.transform.forward * 2f + Vector3.right * 1f,
                    Quaternion.LookRotation(cam.transform.forward, Vector3.up));
        }
    }

    /// <summary>
    /// Pose le panneau sur le mur trouvé à droite de l'ancre (rayon vers anchor.right), centré horizontalement sur
    /// ce mur, à <see cref="progressWallHeight"/>, face à la salle. Retourne false si aucun mur n'est trouvé.
    /// </summary>
    private bool PlaceOnRightWall(Transform anchor)
    {
        Vector3 origin = anchor.position;
        origin.y = progressWallHeight;
        Vector3 dir = anchor.right;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return false;
        dir.Normalize();

        RaycastHit best = default;
        bool found = false;
        foreach (var hit in Physics.RaycastAll(origin, dir, progressWallMaxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(anchor)) continue;
            if (hit.collider.GetComponentInParent<Canvas>() != null) continue; // fenêtres, pas des murs
            if (Mathf.Abs(hit.normal.y) > 0.5f) continue;                        // sol / plafond
            if (!found || hit.distance < best.distance) { best = hit; found = true; }
        }
        if (!found) return false;

        Vector3 normal = best.normal;
        normal.y = 0f;
        if (normal.sqrMagnitude < 1e-4f) return false;
        normal.Normalize();
        if (Vector3.Dot(normal, dir) > 0f) normal = -normal; // normale tournée vers la salle

        // Centre horizontal du mur touché, hauteur fixée, léger décalage vers la salle.
        Vector3 center = best.collider.bounds.center;
        Vector3 pos = new Vector3(center.x, progressWallHeight, center.z) + normal * progressWallOffset;
        // Le long de la normale on garde le point d'impact (le centre d'une boîte englobante épaisse serait dans le mur).
        pos -= normal * Vector3.Dot(pos - best.point, normal);
        pos += normal * progressWallOffset;

        // Décalage le long du mur pour ne recouvrir aucun autre panneau posé sur ce mur (consigne « déplacer l'objet »).
        Vector3 tangent = Vector3.Cross(Vector3.up, normal).normalized;
        pos += tangent * (FreeOffsetAlongWall(best, normal, tangent, pos) - Vector3.Dot(pos, tangent));

        progressPanel.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-normal, Vector3.up));
        Debug.Log($"[Tutorial] Panneau de progression posé sur le mur « {best.collider.name} » à {pos}.");
        return true;
    }

    /// <summary>
    /// Abscisse (le long de <paramref name="tangent"/>) la plus proche du centre du mur où le panneau (et les raccourcis
    /// sous lui, même largeur) ne chevauche aucun canvas de la scène posé devant ce mur, dans les limites du mur.
    /// </summary>
    private float FreeOffsetAlongWall(RaycastHit wall, Vector3 normal, Vector3 tangent, Vector3 pos)
    {
        var panelRect = progressPanel.GetComponent<RectTransform>();
        float halfWidth = Mathf.Max(panelRect.sizeDelta.x, 700f) * panelScale / 2f + progressWallClearance;
        float centerT = Vector3.Dot(pos, tangent);

        // Étendue du mur le long de la tangente.
        var b = wall.collider.bounds;
        float wallMin = float.MaxValue, wallMax = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
            float t = Vector3.Dot(corner, tangent);
            wallMin = Mathf.Min(wallMin, t); wallMax = Mathf.Max(wallMax, t);
        }
        float low = wallMin + halfWidth, high = wallMax - halfWidth;
        if (low > high) return centerT;

        // Obstacles : canvas monde (actifs ou non) parallèles au mur et à moins de progressWallObstacleDepth devant lui.
        var blocked = new List<Vector2>();
        var corners = new Vector3[4];
        foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            if (!c.gameObject.scene.IsValid() || c.renderMode != RenderMode.WorldSpace) continue;
            if (c.transform.IsChildOf(progressPanel.transform) || c.GetComponentInParent<OVRCameraRig>() != null) continue;
            if (Mathf.Abs(Vector3.Dot(c.transform.forward, normal)) < 0.7f) continue;
            float depth = Vector3.Dot(c.transform.position - wall.point, normal);
            if (depth < -0.2f || depth > progressWallObstacleDepth) continue;
            c.GetComponent<RectTransform>().GetWorldCorners(corners);
            float a = float.MaxValue, z = float.MinValue;
            foreach (var k in corners) { float t = Vector3.Dot(k, tangent); a = Mathf.Min(a, t); z = Mathf.Max(z, t); }
            if (z - a < 0.05f) continue;
            blocked.Add(new Vector2(a - progressWallClearance, z + progressWallClearance));
            Debug.Log($"[Tutorial] Obstacle sur le mur : « {c.name} » de {a:0.00} à {z:0.00} m.");
        }

        bool Free(float t)
        {
            if (t < low || t > high) return false;
            foreach (var iv in blocked)
                if (t + halfWidth > iv.x && t - halfWidth < iv.y) return false;
            return true;
        }

        float best = Mathf.Clamp(centerT, low, high);
        if (Free(best)) return best;
        float bestDist = float.MaxValue;
        bool found = false;
        foreach (var iv in blocked)
        {
            foreach (float t in new[] { iv.x - halfWidth, iv.y + halfWidth })
            {
                if (!Free(t)) continue;
                float d = Mathf.Abs(t - centerT);
                if (d < bestDist) { bestDist = d; best = t; found = true; }
            }
        }
        return found ? best : Mathf.Clamp(centerT, low, high);
    }

    private TMP_Text CreateLine(Transform parent, string text, float fontSize, int index)
    {
        var go = new GameObject("Line" + index, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, (index == 0 ? -10f : -100f) - 80f * Mathf.Max(0, index - 1));
        rect.sizeDelta = new Vector2(-40f, index == 0 ? 60f : 76f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = pendingColor;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void RefreshProgress()
    {
        for (int i = 0; i < steps.Count; i++)
        {
            var s = steps[i];
            if (s.progressLine == null) continue;
            string prefix = s.done ? "✔ " : (i == currentStep ? "➜ " : "   ");
            s.progressLine.text = $"{prefix}{i + 1}. {s.label}";
            s.progressLine.color = s.done ? doneColor : (i == currentStep ? activeColor : pendingColor);
            s.progressLine.fontStyle = i == currentStep && !s.done ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    #endregion
}
