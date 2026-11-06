using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Socle commun des scènes d'expérience, installé par code au chargement de chaque scène :
/// plus besoin de recopier à la main la même configuration dans XP1, XP2 et XP3.
/// - objet « SessionCore » portant UIWindowsManager (fenêtres déplaçables, croix de fermeture),
///   XRKeyboardTweaks (clavier AZERTY, Entrée valide) et UndoManager (pile d'annulation) ;
/// - option « Experimentateur » dans la liste des rôles si elle manque ;
/// - bloc-notes en saisie multiligne (Entrée = retour à la ligne, uniquement là).
/// Une scène de tutoriel (TutorialManager présent) n'est pas concernée : elle porte ses propres composants.
/// </summary>
public static class SessionCore
{
    public const string ObjectName = "SessionCore";
    public const string ExperimenterRole = "Experimentateur";

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
        if (!isExperiment || isTutorial) return;

        var host = GameObject.Find(ObjectName);
        if (host == null)
            host = new GameObject(ObjectName);

        Ensure<UIWindowsManager>(host);
        Ensure<XRKeyboardTweaks>(host);
        Ensure<UndoManager>(host);

        EnsureExperimenterRole();
        EnsureNotepadMultiline();

        Debug.Log($"[SessionCore] Socle installé dans la scène « {scene.name} ». {SessionConfig.Instance.Summary()}");
    }

    private static void Ensure<T>(GameObject host) where T : Component
    {
        if (Object.FindObjectOfType<T>(true) == null)
            host.AddComponent<T>();
    }

    /// <summary>Ajoute le rôle Expérimentateur à la liste déroulante des rôles s'il n'y figure pas.</summary>
    private static void EnsureExperimenterRole()
    {
        foreach (var lobby in Object.FindObjectsOfType<LobbyManager>(true))
        {
            if (lobby.dropdown == null) continue;
            bool present = lobby.dropdown.options.Exists(o => o.text == ExperimenterRole);
            if (!present)
            {
                lobby.dropdown.options.Add(new TMP_Dropdown.OptionData(ExperimenterRole));
                lobby.dropdown.RefreshShownValue();
            }
        }
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
