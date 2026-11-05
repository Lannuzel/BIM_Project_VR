using System.Collections.Generic;
using UnityEngine;

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
