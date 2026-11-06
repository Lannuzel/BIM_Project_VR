using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Recette du 6 novembre 2026 : baisse de FPS en XP2 et XP3 par rapport à XP1. Les maquettes BIM de ces scènes
/// (FBX « MAQUETTE STRUCTURE NON CORRIGEE » : 8 566 nœuds de géométrie, « VIDE » : 1 198) sont instanciées
/// sans l'indicateur « Batching Static » : chaque nœud est un appel de rendu séparé sur le casque. Au moment du
/// build, tous les renderers issus d'un modèle FBX fixe (sans Rigidbody, objet réseau ni canvas dans leur
/// hiérarchie) sont marqués Batching Static : Unity combine alors leurs maillages par matériau dans l'APK.
/// Les scènes sur disque ne sont pas modifiées.
/// </summary>
public class StaticBatchingAtBuild : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null) return; // ouverture dans l'éditeur, pas un build

        int flagged = 0, already = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var go = renderer.gameObject;
                if (!IsStaticModelPart(go)) continue;
                var flags = GameObjectUtility.GetStaticEditorFlags(go);
                if ((flags & StaticEditorFlags.BatchingStatic) != 0) { already++; continue; }
                GameObjectUtility.SetStaticEditorFlags(go, flags | StaticEditorFlags.BatchingStatic);
                flagged++;
            }
        }
        Debug.Log($"[StaticBatchingAtBuild] {scene.name} : {flagged} renderer(s) marqué(s) Batching Static ({already} l'étaient déjà).");
    }

    /// <summary>Renderer d'un modèle FBX de la scène, sans rien de mobile ou réseau au-dessus de lui.</summary>
    private static bool IsStaticModelPart(GameObject go)
    {
        if (go.GetComponentInParent<Rigidbody>() != null) return false;
        if (go.GetComponentInParent<Fusion.NetworkObject>() != null) return false;
        if (go.GetComponentInParent<Canvas>() != null) return false;
        if (go.GetComponentInParent<OVRCameraRig>() != null) return false;

        // Pendant le build, les instances de prefab ne sont plus reconnues comme telles : on se fie au
        // fichier source du maillage (un maillage importé d'un FBX du projet).
        var filter = go.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return false;
        string source = AssetDatabase.GetAssetPath(filter.sharedMesh);
        if (string.IsNullOrEmpty(source) || !source.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) return false;
        foreach (var prefix in ModelFolders)
            if (source.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>
    /// Dossiers des modèles fixes (salle, maquettes BIM). Les autres FBX de la scène (touches de la calculatrice,
    /// manettes, mains…) peuvent être déplacés à l'exécution et ne doivent pas être combinés.
    /// </summary>
    private static readonly string[] ModelFolders =
    {
        "Assets/EXP2/Maquette/",
        "Assets/EXP3/Maquettes/",
        "Assets/BIM Resources/EXP2_Resources/",
        "Assets/Resources/3dmax_1_1.fbx",
    };
}
