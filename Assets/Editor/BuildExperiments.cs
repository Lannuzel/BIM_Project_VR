using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Génération des APK des deux expériences (tutoriel + XP1, tutoriel + XP2) sans toucher aux
/// Build Settings du projet : les scènes sont passées explicitement (index 0 = tutoriel,
/// index 1 = scène d'expérience, comme l'attend SceneLoader).
///
/// Ligne de commande (depuis la racine du projet) :
///   Unity.exe -batchmode -nographics -quit -projectPath . -buildTarget Android
///             -executeMethod BuildExperiments.BuildXP1 -logFile build_xp1.log [-outputDir C:\dossier]
/// Menu Unity : Outils BIM VR > Générer l'APK XP1 / XP2.
/// </summary>
public static class BuildExperiments
{
    private const string TutorialScene = "Assets/EXP1/Scenes/Tutorial/BIM-EXP_Tutorial_V2.unity";
    private const string Xp1Scene = "Assets/EXP1/Scenes/Exp1_V3.unity";
    private const string Xp2Scene = "Assets/EXP2/Scene/Exp2-V1-09-06-2025_EXP2_User.unity";

    [MenuItem("Outils BIM VR/Générer l'APK XP1")]
    public static void BuildXP1() => Build("XP1", Xp1Scene, "CollectiveIntelligence.CESI.EXP1");

    [MenuItem("Outils BIM VR/Générer l'APK XP2")]
    public static void BuildXP2() => Build("XP2", Xp2Scene, "CollectiveIntelligence.CESI.EXP2");

    public static void BuildBoth()
    {
        BuildXP1();
        BuildXP2();
    }

    private static void Build(string label, string experimentScene, string applicationId)
    {
        string outputDir = GetArg("-outputDir") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "build"));
        Directory.CreateDirectory(outputDir);
        string apkPath = Path.Combine(outputDir, $"BIM_VR_{label}_{DateTime.Now:yyyyMMdd_HHmm}.apk");

        foreach (var scene in new[] { TutorialScene, experimentScene })
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "..", scene)))
                throw new BuildFailedException($"Scène introuvable : {scene}");
        }

        // Identifiant d'application distinct par expérience : les deux APK coexistent sur le casque.
        string previousId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        string previousName = PlayerSettings.productName;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, applicationId);
        PlayerSettings.productName = label == "XP1" ? "EXP1" : "EXP2";

        var options = new BuildPlayerOptions
        {
            scenes = new[] { TutorialScene, experimentScene },
            locationPathName = apkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        };

        Debug.Log($"[BuildExperiments] Génération {label} -> {apkPath}");
        BuildReport report;
        try
        {
            report = BuildPipeline.BuildPlayer(options);
        }
        finally
        {
            // Les réglages du projet ne sont pas modifiés durablement.
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, previousId);
            PlayerSettings.productName = previousName;
        }

        var summary = report.summary;
        Debug.Log($"[BuildExperiments] {label} : {summary.result}, {summary.totalErrors} erreur(s), {summary.totalWarnings} avertissement(s), {summary.totalSize / (1024 * 1024)} Mo, {summary.totalTime.TotalMinutes:0.0} min");

        if (summary.result != BuildResult.Succeeded)
        {
            foreach (var step in report.steps)
                foreach (var msg in step.messages.Where(m => m.type == LogType.Error || m.type == LogType.Exception))
                    Debug.LogError($"[BuildExperiments] {step.name}: {msg.content}");
            throw new BuildFailedException($"La génération de {label} a échoué ({summary.result}).");
        }
    }

    private static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }
}
