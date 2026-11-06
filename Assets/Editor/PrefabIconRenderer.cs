using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Recette du 6 novembre 2026 : icônes des boutons « Générer … » du menu du plafond (XP3) rendues à partir des
/// prefabs eux-mêmes (image du modèle), au lieu de l'icône générique de la chaise.
/// Menu Outils BIM VR, ou en batch (avec rendu, donc sans -nographics) :
/// -executeMethod PrefabIconRenderer.RenderAll
/// Les PNG sont écrits dans Assets/Resources/Icone/ et affectés aux boutons par code (SessionCore.EnsureMenuIcons).
/// </summary>
public static class PrefabIconRenderer
{
    public const string OutputFolder = "Assets/Resources/Icone";

    /// <summary>Prefab à rendre → nom du PNG (sans extension), aussi nom de la ressource chargée à l'exécution.</summary>
    public static readonly (string prefabPath, string iconName)[] Icons =
    {
        ("Assets/EXP3/Prefabs/VentilatorExp3.prefab", "XP3_Ventilateur"),
        ("Assets/EXP3/Prefabs/LumierExpV3.prefab", "XP3_Lumiere"),
        ("Assets/EXP3/Prefabs/AirTerminalExp3.prefab", "XP3_BoucheAeration"),
    };

    [MenuItem("Outils BIM VR/Générer les icônes des prefabs XP3")]
    public static void RenderAll()
    {
        Directory.CreateDirectory(OutputFolder);
        int ok = 0;
        foreach (var (prefabPath, iconName) in Icons)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Debug.LogError($"[PrefabIconRenderer] Prefab introuvable : {prefabPath}"); continue; }
            string png = Path.Combine(OutputFolder, iconName + ".png").Replace('\\', '/');
            if (Render(prefab, png, 256)) ok++;
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var (_, iconName) in Icons)
            MakeSprite(Path.Combine(OutputFolder, iconName + ".png").Replace('\\', '/'));
        AssetDatabase.SaveAssets();
        Debug.Log($"[PrefabIconRenderer] {ok}/{Icons.Length} icône(s) rendue(s) dans {OutputFolder}.");
    }

    /// <summary>Rend le prefab vu de trois quarts par en dessous (objets de plafond), fond transparent.</summary>
    private static bool Render(GameObject prefab, string pngPath, int size)
    {
        var pru = new PreviewRenderUtility();
        try
        {
            pru.camera.clearFlags = CameraClearFlags.SolidColor;
            pru.camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            pru.camera.fieldOfView = 25f;
            pru.camera.nearClipPlane = 0.01f;
            pru.camera.farClipPlane = 100f;
            pru.lights[0].intensity = 1.3f;
            pru.lights[0].transform.rotation = Quaternion.Euler(-30f, 40f, 0f); // éclaire par en dessous
            pru.lights[1].intensity = 0.9f;
            pru.lights[1].transform.rotation = Quaternion.Euler(50f, -140f, 0f);
            pru.ambientColor = new Color(0.45f, 0.45f, 0.45f, 1f);

            var go = pru.InstantiatePrefabInScene(prefab);
            go.transform.position = Vector3.zero;
            go.transform.rotation = prefab.transform.rotation;

            bool has = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!has) { bounds = r.bounds; has = true; } else bounds.Encapsulate(r.bounds);
            }
            if (!has) { Debug.LogError($"[PrefabIconRenderer] {prefab.name} : aucun Renderer."); return false; }

            Vector3 dir = new Vector3(1f, -0.75f, -1f).normalized; // caméra en dessous, de trois quarts
            float radius = bounds.extents.magnitude;
            float dist = radius / Mathf.Sin(pru.camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.08f;
            pru.camera.transform.position = bounds.center + dir * dist;
            pru.camera.transform.LookAt(bounds.center, Vector3.up);

            pru.BeginStaticPreview(new Rect(0, 0, size, size));
            pru.camera.Render();
            Texture2D tex = pru.EndStaticPreview();
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Debug.Log($"[PrefabIconRenderer] {prefab.name} → {pngPath} (rayon {radius:0.00} m)");
            return true;
        }
        finally
        {
            pru.Cleanup();
        }
    }

    private static void MakeSprite(string pngPath)
    {
        var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 256;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
