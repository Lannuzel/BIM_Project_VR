using Fusion;
using Fusion.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Génère le prefab réseau des feedbacks collaboratifs (Resources/Feedback/FeedbackNetwork.prefab : NetworkObject
/// suivant le maître de session + <see cref="FeedbackNetwork"/>) et le marque FusionPrefab pour la table des prefabs.
/// Menu Outils BIM VR, et appelé par BuildExperiments avant chaque build s'il manque.
/// </summary>
public static class FeedbackPrefabBuilder
{
    public const string PrefabPath = "Assets/Resources/Feedback/FeedbackNetwork.prefab";

    [MenuItem("Outils BIM VR/Créer le prefab réseau des feedbacks")]
    public static void Create()
    {
        Build(true);
    }

    /// <summary>Crée le prefab s'il n'existe pas (ou s'il n'a pas le composant attendu).</summary>
    public static void EnsurePrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null && existing.GetComponent<FeedbackNetwork>() != null && existing.GetComponent<NetworkObject>() != null)
        {
            if (!AssetDatabaseUtils.HasLabel(PrefabPath, NetworkProjectConfigImporter.FusionPrefabTag))
                AssetDatabaseUtils.SetLabel(PrefabPath, NetworkProjectConfigImporter.FusionPrefabTag, true);
            return;
        }
        Build(false);
    }

    private static void Build(bool log)
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
        var go = new GameObject("FeedbackNetwork");
        try
        {
            var no = go.AddComponent<NetworkObject>();
            no.Flags |= NetworkObjectFlags.MasterClientObject; // l'autorité suit le maître de session
            go.AddComponent<FeedbackNetwork>();
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabaseUtils.SetLabel(PrefabPath, NetworkProjectConfigImporter.FusionPrefabTag, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"[FeedbackPrefabBuilder] Prefab réseau des feedbacks écrit : {PrefabPath}");
    }
}
