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
    public Vector3 progressOffset = new Vector3(1.3f, 0f, 0f);
    public float panelScale = 0.0015f;
    public Color pendingColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    public Color activeColor = Color.white;
    public Color doneColor = new Color(0.45f, 0.9f, 0.45f, 1f);
    public Color warningColor = new Color(1f, 0.35f, 0.3f, 1f);

    private DrawLineTutorial drawLine;
    private RaycastSelectAndMoveTutorial moveObject;
    private readonly Dictionary<TMP_InputField, TMP_Text> fieldWarnings = new Dictionary<TMP_InputField, TMP_Text>();
    private readonly List<TMP_InputField> requiredFields = new List<TMP_InputField>();
    private bool replaying;
    private GameObject progressPanel;
    private int currentStep = -1;

    private void Start()
    {
        ResolveSceneObjects();
        BuildProgressPanel();
        HookButtons();
        HookFields();
        RefreshProgress();
    }

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

        // Rejoue les actions câblées dans la scène (passage à l'étape suivante).
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

    private void ShowHint(Step step, string message, bool isWarning)
    {
        if (step == null || step.root == null) return;

        if (step.hintText == null)
        {
            // Sous le bouton OK du panneau de l'étape
            Transform parent = step.okButton != null ? step.okButton.transform.parent : step.root.transform;
            var go = new GameObject("TutorialHint", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            rect.sizeDelta = new Vector2(-40f, 90f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
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
        rect.sizeDelta = new Vector2(620f, 60f + 70f * steps.Count);
        rect.localScale = Vector3.one * panelScale;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(progressPanel.transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

        var title = CreateLine(progressPanel.transform, "Étapes du tutoriel", 34f, 0);
        title.color = activeColor;
        title.fontStyle = FontStyles.Bold;

        for (int i = 0; i < steps.Count; i++)
        {
            steps[i].progressLine = CreateLine(progressPanel.transform, $"{i + 1}. {steps[i].label}", 28f, i + 1);
        }

        // Placement : ancre explicite, sinon à droite du canvas d'accueil.
        Transform anchor = progressAnchor;
        if (anchor == null)
        {
            var welcome = GetStep("WecomeCanvas");
            if (welcome != null && welcome.root != null)
                anchor = welcome.root.transform;
        }
        if (anchor != null)
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

    private TMP_Text CreateLine(Transform parent, string text, float fontSize, int index)
    {
        var go = new GameObject("Line" + index, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -20f - 70f * index);
        rect.sizeDelta = new Vector2(-40f, 60f);
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
