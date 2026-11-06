using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Rend l'avatar réseau (PlayerPrefabMB) dans une scène vide et enregistre l'image dans docs/images/avatar.png
/// (illustration du README). Menu Outils BIM VR ou, en ligne de commande :
/// Unity.exe -batchmode -quit -projectPath ... -executeMethod AvatarSnapshot.Capture
/// (sans -nographics : le rendu a besoin de la carte graphique).
/// </summary>
public static class AvatarSnapshot
{
    const string PrefabPath = "Assets/Prefabs/PlayerPrefabMB.prefab";
    const string OutputPath = "docs/images/avatar.png";
    const string LabelText = "Modélisateur";
    const int Size = 1200;

    static void PoseHand(GameObject avatar, string name, Vector3 localPosition, Quaternion localRotation)
    {
        foreach (var t in avatar.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != name) continue;
            t.localPosition = localPosition;
            t.localRotation = localRotation;
            return;
        }
    }

    [MenuItem("Outils BIM VR/Capturer l'avatar (docs/images/avatar.png)")]
    public static void Capture()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("AvatarSnapshot : prefab introuvable " + PrefabPath);
            return;
        }

        var avatar = Object.Instantiate(prefab);
        avatar.transform.position = Vector3.zero;
        avatar.transform.rotation = Quaternion.identity;
        foreach (var label in avatar.GetComponentsInChildren<TMP_Text>(true))
        {
            label.text = LabelText;
            label.color = new Color(0.15f, 0.15f, 0.2f);
            label.ForceMeshUpdate();
            Debug.Log($"AvatarSnapshot : étiquette {label.name} en {label.transform.position}, taille {label.bounds.size}");
        }
        // En jeu, les mains suivent les manettes ; dans le prefab elles sont confondues avec la tête.
        // Pose de présentation : manettes devant le corps.
        PoseHand(avatar, "LeftHand", new Vector3(-0.24f, -0.34f, 0.16f), Quaternion.Euler(-50f, 15f, 0f));
        PoseHand(avatar, "RightHand", new Vector3(0.24f, -0.34f, 0.16f), Quaternion.Euler(-50f, -15f, 0f));

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

        var keyLight = new GameObject("Key").AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = 1.3f;
        keyLight.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
        var fillLight = new GameObject("Fill").AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.5f;
        fillLight.color = new Color(0.85f, 0.9f, 1f);
        fillLight.transform.rotation = Quaternion.Euler(20f, 140f, 0f);

        var bounds = new Bounds(avatar.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in avatar.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
        }
        Debug.Log($"AvatarSnapshot : bornes {bounds.center} taille {bounds.size}");

        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.fieldOfView = 28f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 50f;
        // Vue de trois quarts, légèrement en plongée, face à l'avatar (l'avatar regarde vers +Z).
        var dir = Quaternion.Euler(6f, 155f, 0f) * Vector3.forward;
        float radius = bounds.extents.magnitude;
        float distance = radius / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.15f;
        cam.transform.position = bounds.center - dir * distance;
        cam.transform.LookAt(bounds.center);

        var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;

        var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputPath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Debug.Log("AvatarSnapshot : image écrite " + fullPath);

        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}
