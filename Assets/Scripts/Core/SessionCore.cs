using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Socle commun des scènes d'expérience, installé par code au chargement de chaque scène :
/// plus besoin de recopier à la main la même configuration dans XP1, XP2 et XP3.
/// - objet « SessionCore » portant UIWindowsManager (fenêtres déplaçables, croix de fermeture),
///   XRKeyboardTweaks (clavier AZERTY, Entrée valide) et UndoManager (pile d'annulation) ;
/// - mode expérimentateur caché (ExperimenterShortcut : A, A, B, B sur la manette droite), dans le tutoriel aussi ;
/// - bloc-notes en saisie multiligne (Entrée = retour à la ligne, uniquement là).
/// Une scène de tutoriel (TutorialManager présent) ne reçoit que StartPlacement et ExperimenterShortcut.
/// </summary>
public static class SessionCore
{
    public const string ObjectName = "SessionCore";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Install(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Install(scene);
    }

    /// <summary>Installe le socle si la scène est une scène d'expérience (idempotent).</summary>
    public static void Install(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        bool isExperiment = Object.FindObjectOfType<ObjectInteractionHandler>(true) != null
                            || Object.FindObjectOfType<ReservationInteractionHandler>(true) != null;
        bool isTutorial = Object.FindObjectOfType<TutorialManager>(true) != null;
        if (!isExperiment && !isTutorial) return;

        var host = GameObject.Find(ObjectName);
        if (host == null)
            host = new GameObject(ObjectName);

        // Commun au tutoriel et aux expériences : casque recentré devant le panneau de départ.
        Ensure<StartPlacement>(host);
        // Recette du 9 novembre 2026 : le rôle Expérimentateur n'est plus proposé ; il s'obtient par A, A, B, B (manette droite).
        Ensure<ExperimenterShortcut>(host);
        if (isTutorial) return;

        Ensure<UIWindowsManager>(host);
        Ensure<XRKeyboardTweaks>(host);
        Ensure<UndoManager>(host);
        // Feedbacks collaboratifs (Emery Descours) si la configuration les active pour cette scène.
        if (FeedbackSettings.AppliesToScene(scene.name))
            Ensure<FeedbackManager>(host);

        EnsureNotepadMultiline();
        EnsureMenuIcons();

        Debug.Log($"[SessionCore] Socle installé dans la scène « {scene.name} ». {SessionConfig.Instance.Summary()}");
    }

    private static void Ensure<T>(GameObject host) where T : Component
    {
        if (Object.FindObjectOfType<T>(true) == null)
            host.AddComponent<T>();
    }

    /// <summary>Boutons « Générer … » du menu du plafond (XP3) → icône rendue à partir du prefab (PrefabIconRenderer).</summary>
    private static readonly (string keyword, string resource)[] MenuIcons =
    {
        ("ventilateur", "Icone/XP3_Ventilateur"),
        ("lumi", "Icone/XP3_Lumiere"),
        ("bouche", "Icone/XP3_BoucheAeration"),
    };

    /// <summary>
    /// Recette du 6 novembre 2026 : les trois boutons de création d'équipements (XP3) partageaient l'icône de la
    /// chaise. L'image « ButtonIcon » de chaque bouton « Générer … » reçoit l'icône de son modèle, si elle existe
    /// dans Resources/Icone (générée dans l'éditeur, menu Outils BIM VR).
    /// </summary>
    private static void EnsureMenuIcons()
    {
        int count = 0;
        foreach (var image in Resources.FindObjectsOfTypeAll<Image>())
        {
            if (!image.gameObject.scene.IsValid() || image.name != "ButtonIcon" || image.transform.parent == null) continue;
            string button = Normalize(image.transform.parent.name);
            if (!button.StartsWith("generer")) continue;
            foreach (var (keyword, resource) in MenuIcons)
            {
                if (!button.Contains(keyword)) continue;
                var sprite = Resources.Load<Sprite>(resource);
                if (sprite == null) break;
                image.sprite = sprite;
                image.preserveAspect = true;
                count++;
                break;
            }
        }
        if (count > 0) Debug.Log($"[SessionCore] {count} icône(s) de menu affectée(s).");
    }

    /// <summary>Minuscules sans accents ni apostrophes, pour comparer des noms d'objets saisis à la main.</summary>
    private static string Normalize(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s.Normalize(NormalizationForm.FormD))
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c) || c == ' ') sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>Le champ du bloc-notes accepte les retours à la ligne (F13) ; les autres champs valident sur Entrée.</summary>
    private static void EnsureNotepadMultiline()
    {
        foreach (var field in Resources.FindObjectsOfTypeAll<TMP_InputField>())
        {
            if (!field.gameObject.scene.IsValid()) continue;
            Transform t = field.transform;
            bool inNotepad = false;
            while (t != null)
            {
                if (t.name == "NotePad") { inNotepad = true; break; }
                t = t.parent;
            }
            if (inNotepad && field.lineType != TMP_InputField.LineType.MultiLineNewline)
                field.lineType = TMP_InputField.LineType.MultiLineNewline;
        }
    }
}
