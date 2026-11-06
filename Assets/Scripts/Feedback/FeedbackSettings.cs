using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Feedbacks collaboratifs (regard partagé, parole, concentration) — portage des travaux d'Emery Descours
/// (branche « feedback », août 2026 ; article hal-05781898) dans l'application de recette.
/// Lecture des réglages de <see cref="SessionConfig"/> et aides communes : rôle → index participant
/// (0 Lecteur, 1 Calculateur, 2 Modélisateur ; -1 pour l'Expérimentateur et les joueurs simulés, qui
/// ne comptent pas comme participants), scène d'expérience de l'APK.
/// </summary>
public static class FeedbackSettings
{
    public const int ParticipantCount = 3;

    public static bool AnyEnabled(SessionConfig c) => c != null && (c.feedbackGaze || c.feedbackVoice || c.feedbackFace);

    /// <summary>Vrai si au moins un feedback est activé dans la configuration pour cette scène.</summary>
    public static bool AppliesToScene(string sceneName)
    {
        var c = SessionConfig.Instance;
        if (!AnyEnabled(c)) return false;
        if (c.feedbackScenes == null || c.feedbackScenes.Count == 0) return true;
        foreach (var s in c.feedbackScenes)
            if (!string.IsNullOrWhiteSpace(s) && string.Equals(s.Trim(), sceneName, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>Nom de la scène d'expérience embarquée dans cet APK (première scène du build qui n'est pas le tutoriel).</summary>
    public static string ExperimentSceneName()
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(name) && name.IndexOf("Tutorial", StringComparison.OrdinalIgnoreCase) < 0)
                return name;
        }
        return null;
    }

    /// <summary>Vrai si les feedbacks s'appliqueront à la scène d'expérience qui suit le tutoriel.</summary>
    public static bool EnabledForNextExperiment => AppliesToScene(ExperimentSceneName() ?? string.Empty);

    public static int RoleIndex(string role) => string.IsNullOrEmpty(role) ? -1 : Array.IndexOf(AdminTools.ParticipantRoles, role);

    public static int LocalRoleIndex => RoleIndex(NetworkManager.GetLocalPlayerName());

    public static string RoleLabel(int index)
    {
        switch (index)
        {
            case 0: return "Lecteur";
            case 1: return "Calculateur";
            case 2: return "Modélisateur";
            default: return "?";
        }
    }
}
