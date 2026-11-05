using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Équipe les fenêtres d'outils (documents, bloc-notes, calculatrice, claviers, saisie de distance)
/// sans modifier leur câblage : déplacement à la gâchette intérieure (F10).
/// Les fenêtres sont retrouvées par nom, y compris inactives ou créées plus tard (clavier global).
/// </summary>
public class UIWindowsManager : MonoBehaviour
{
    [Tooltip("Noms des objets racines des fenêtres à équiper.")]
    public List<string> windowNames = new List<string>
    {
        "Document Viewer",
        "NotePad",
        "CalculatorRV",
        "NonNativeKeyboard (1)",
        "NonNativeKeyboard",
        "XRI Keyboard",
        "PDFCanvas",
        "MoveObjectWithReferenceUI",
        "ObjectReferencePointCanvas",
    };

    [Tooltip("Intervalle (s) de recherche des fenêtres apparues après le démarrage.")]
    public float rescanInterval = 1f;

    private readonly HashSet<GameObject> equipped = new HashSet<GameObject>();
    private float nextScan;

    private void Start()
    {
        Scan();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + rescanInterval;
        Scan();
    }

    private void Scan()
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (!t.gameObject.scene.IsValid() || t.hideFlags != HideFlags.None)
                continue;
            if (!windowNames.Contains(t.name) || equipped.Contains(t.gameObject))
                continue;
            if (t.GetComponentInChildren<Canvas>(true) == null)
                continue;

            Equip(t.gameObject);
            equipped.Add(t.gameObject);
        }
    }

    private void Equip(GameObject window)
    {
        if (window.GetComponent<WindowGrabMover>() == null)
            window.AddComponent<WindowGrabMover>();

        if (addCloseCross && !noCloseCrossNames.Contains(window.name))
            AddCloseCross(window);
    }

    [Header("Croix de fermeture (F11)")]
    public bool addCloseCross = true;
    [Tooltip("Fenêtres qui ont déjà leur propre touche de fermeture (claviers).")]
    public List<string> noCloseCrossNames = new List<string> { "XRI Keyboard", "NonNativeKeyboard (1)", "NonNativeKeyboard" };
    [Tooltip("Taille de la croix en fraction de la hauteur de la fenêtre.")]
    [Range(0.04f, 0.2f)] public float crossSizeRatio = 0.09f;

    /// <summary>Ajoute une croix en haut à droite du canvas principal de la fenêtre ; un clic désactive la fenêtre.</summary>
    private void AddCloseCross(GameObject window)
    {
        var canvas = window.GetComponentInChildren<Canvas>(true);
        if (canvas == null) return;
        var canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null || canvasRect.Find("CloseCross") != null) return;

        // Les canvas des fenêtres ont déjà les raycasters XR nécessaires à leurs propres boutons.
        float size = Mathf.Clamp(canvasRect.rect.height * crossSizeRatio, 24f, 120f);

        var go = new GameObject("CloseCross", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvasRect, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-size * 0.15f, -size * 0.15f);
        rect.sizeDelta = new Vector2(size, size);
        go.transform.SetAsLastSibling();

        var image = go.GetComponent<Image>();
        image.color = new Color(0.75f, 0.15f, 0.15f, 0.95f);

        var textGo = new GameObject("X", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "X";
        tmp.fontSize = size * 0.7f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        go.GetComponent<Button>().onClick.AddListener(() => window.SetActive(false));
    }

    /// <summary>Retrouve la manette droite (OVR ou XR Origin) dans la scène.</summary>
    public static Transform FindRightController()
    {
        string[] names = { "RightHandAnchor", "RightControllerAnchor", "Right Controller", "RightHand Controller", "Right Hand" };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null) return go.transform;
        }
        var rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null) return rig.rightHandAnchor;
        return null;
    }
}
