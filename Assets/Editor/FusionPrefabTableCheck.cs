using System.Linq;
using Fusion;
using Fusion.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Diagnostic : liste la table des prefabs réseau Fusion (NetworkProjectConfig) et vérifie que les
/// prefabs créés par l'application (avatar, tables, réservations, équipements XP3) y figurent.
/// Menu Outils BIM VR, ou en batch : -executeMethod FusionPrefabTableCheck.Run
/// </summary>
public static class FusionPrefabTableCheck
{
    static readonly string[] Expected =
    {
        "Assets/Prefabs/PlayerPrefabMB.prefab",
        "Assets/Prefabs/TablePref.prefab",
        "Assets/EXP3/Prefabs/LumierExpV3.prefab",
        "Assets/EXP3/Prefabs/VentilatorExp3.prefab",
        "Assets/EXP3/Prefabs/AirTerminalExp3.prefab",
    };

    [MenuItem("Outils BIM VR/Vérifier la table des prefabs Fusion")]
    public static void Run()
    {
        Dump("avant reconstruction");
        NetworkProjectConfigUtilities.RebuildPrefabTable();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Dump("après reconstruction");
    }

    static void Dump(string title)
    {
        var table = NetworkProjectConfig.Global.PrefabTable;
        int count = table.Prefabs.Count;
        var guids = new System.Collections.Generic.HashSet<string>();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[FusionPrefabTableCheck] {title} : {count} prefab(s)");
        for (int i = 0; i < count; i++)
        {
            var id = NetworkPrefabId.FromIndex(i);
            var src = table.GetSource(id);
            string guid = src != null ? src.AssetGuid.ToUnityGuidString() : "";
            string path = string.IsNullOrEmpty(guid) ? "" : AssetDatabase.GUIDToAssetPath(guid);
            guids.Add(guid);
            sb.AppendLine($"  {i,3}  {guid}  {(string.IsNullOrEmpty(path) ? "(ASSET INTROUVABLE)" : path)}");
        }
        foreach (var p in Expected)
        {
            string g = AssetDatabase.AssetPathToGUID(p);
            sb.AppendLine($"  attendu {p} : {(guids.Contains(g) ? "PRÉSENT" : "ABSENT")}  (label FusionPrefab : {AssetDatabase.GetLabels(AssetDatabase.LoadMainAssetAtPath(p)).Contains("FusionPrefab")})");
        }
        Debug.Log(sb.ToString());
    }
}
