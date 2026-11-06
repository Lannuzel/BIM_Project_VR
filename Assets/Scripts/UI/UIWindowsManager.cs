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
            // La croix posée sur la boîte englobante a besoin de renderers actifs : on attend l'ouverture.
            if (crossOutsideNames.Contains(t.name) && !t.gameObject.activeInHierarchy)
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
    [Tooltip("Taille minimale de la croix dans le monde (m), pour les canvas aux unités très petites (calculatrice : 30 x 20 unités).")]
    public float crossMinWorldSize = 0.05f;
    [Tooltip("Fenêtres dont le canvas est recouvert par un objet 3D (socle de la calculatrice) : la croix est posée au-dessus du coin supérieur droit, hors du canvas.")]
    public List<string> crossOutsideNames = new List<string> { "CalculatorRV" };

    /// <summary>Ajoute une croix en haut à droite du canvas principal de la fenêtre ; un clic désactive la fenêtre.</summary>
    private void AddCloseCross(GameObject window)
    {
        if (crossOutsideNames.Contains(window.name))
        {
            AddCloseCrossOnBounds(window);
            return;
        }

        var canvas = window.GetComponentInChildren<Canvas>(true);
        if (canvas == null) return;
        var canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null || canvasRect.Find("CloseCross") != null) return;

        // Les canvas des fenêtres ont déjà les raycasters XR nécessaires à leurs propres boutons.
        // Taille exprimée en unités du canvas, mais bornée par une taille réelle (m).
        float height = canvasRect.rect.height;
        float unitsPerMeter = canvasRect.lossyScale.y > 1e-6f ? 1f / canvasRect.lossyScale.y : 1f;
        float size = height * crossSizeRatio;
        size = Mathf.Max(size, crossMinWorldSize * unitsPerMeter);
        size = Mathf.Min(size, height * 0.5f);

        var go = new GameObject("CloseCross", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvasRect, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-size * 0.15f, -size * 0.15f);
        rect.sizeDelta = new Vector2(size, size);
        go.transform.SetAsLastSibling();

        FillCross(go, window, size);
    }

    /// <summary>
    /// Recette du 6 novembre 2026 : la calculatrice n'a pas de canvas dans le plan de sa façade (ses touches
    /// sont des objets 3D, le rectangle de son canvas est horizontal), si bien qu'une croix posée dans ce canvas
    /// apparaissait allongée au milieu de la façade. Ici la croix est un petit canvas indépendant posé sur le
    /// coin supérieur droit de la boîte englobante réelle de la fenêtre, tourné vers l'utilisateur, et rattaché
    /// à la fenêtre pour la suivre quand elle est déplacée.
    /// </summary>
    private void AddCloseCrossOnBounds(GameObject window)
    {
        if (window.transform.Find("CloseCrossWorld") != null) return;

        bool hasBounds = false;
        var bounds = new Bounds(window.transform.position, Vector3.zero);
        foreach (var r in window.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;
            if (!hasBounds) { bounds = r.bounds; hasBounds = true; } else bounds.Encapsulate(r.bounds);
        }
        var corners = new Vector3[4];
        foreach (var g in window.GetComponentsInChildren<Graphic>(true))
        {
            g.rectTransform.GetWorldCorners(corners);
            foreach (var c in corners)
            {
                if (!hasBounds) { bounds = new Bounds(c, Vector3.zero); hasBounds = true; } else bounds.Encapsulate(c);
            }
        }
        if (!hasBounds) return;

        // Normale de la façade : axe du repère de la fenêtre le plus orienté vers la caméra (ou l'axe le plus
        // mince de la boîte à défaut de caméra).
        Transform root = window.transform;
        Vector3[] axes = { root.right, -root.right, root.up, -root.up, root.forward, -root.forward };
        Vector3 normal = -root.forward;
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 toCam = (cam.transform.position - bounds.center).normalized;
            float best = float.NegativeInfinity;
            foreach (var a in axes)
            {
                float d = Vector3.Dot(a, toCam);
                if (d > best) { best = d; normal = a; }
            }
        }
        normal.Normalize();
        Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.9f ? root.up : Vector3.up;
        Vector3 right = Vector3.Cross(up, -normal).normalized; // droite de l'utilisateur qui regarde la façade
        up = Vector3.Cross(-normal, right).normalized;

        float ExtentAlong(Vector3 axis) =>
            Mathf.Abs(axis.x) * bounds.extents.x + Mathf.Abs(axis.y) * bounds.extents.y + Mathf.Abs(axis.z) * bounds.extents.z;

        float size = crossMinWorldSize;
        Vector3 corner = bounds.center + right * (ExtentAlong(right) + size * 0.6f) + up * (ExtentAlong(up) - size * 0.5f)
                         + normal * (ExtentAlong(normal) + 0.005f);

        var go = new GameObject("CloseCrossWorld", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;
        XRUiCanvas.MakeInteractive(canvas);
        var rect = go.GetComponent<RectTransform>();
        const float units = 100f;
        rect.sizeDelta = new Vector2(units, units);
        go.transform.SetPositionAndRotation(corner, Quaternion.LookRotation(-normal, up));
        go.transform.localScale = Vector3.one * (size / units);
        go.transform.SetParent(root, true);

        var button = new GameObject("CloseCross", typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(go.transform, false);
        var bRect = button.GetComponent<RectTransform>();
        bRect.anchorMin = Vector2.zero; bRect.anchorMax = Vector2.one;
        bRect.offsetMin = Vector2.zero; bRect.offsetMax = Vector2.zero;
        FillCross(button, window, units);
    }

    private static void FillCross(GameObject go, GameObject window, float size)
    {
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
