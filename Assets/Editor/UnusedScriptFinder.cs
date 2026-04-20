using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class UnusedScriptFinder
{
    private const string TARGET_FOLDER = "Assets/UnusedScripts";

    [MenuItem("Tools/Find and Move Unused Scripts")]
    public static void FindAndMoveUnusedScripts()
    {
        // Ensure target folder exists
        if (!AssetDatabase.IsValidFolder(TARGET_FOLDER))
        {
            AssetDatabase.CreateFolder("Assets", "UnusedScripts");
        }

        // Get all script GUIDs
        string[] scriptGUIDs = AssetDatabase.FindAssets("t:MonoScript");
        HashSet<string> usedScripts = new HashSet<string>();

        // Get all scene paths
        string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene");

        foreach (string sceneGUID in sceneGUIDs)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (GameObject root in rootObjects)
            {
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);

                foreach (MonoBehaviour mb in behaviours)
                {
                    if (mb == null) continue;

                    MonoScript script = MonoScript.FromMonoBehaviour(mb);
                    if (script != null)
                    {
                        string path = AssetDatabase.GetAssetPath(script);
                        usedScripts.Add(path);
                    }
                }
            }
        }

        int movedCount = 0;

        foreach (string guid in scriptGUIDs)
        {
            string scriptPath = AssetDatabase.GUIDToAssetPath(guid);

            if (!usedScripts.Contains(scriptPath))
            {
                string fileName = Path.GetFileName(scriptPath);
                string newPath = Path.Combine(TARGET_FOLDER, fileName);

                newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

                string error = AssetDatabase.MoveAsset(scriptPath, newPath);

                if (string.IsNullOrEmpty(error))
                {
                    movedCount++;
                }
                else
                {
                    Debug.LogError($"Failed to move {scriptPath}: {error}");
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Moved {movedCount} unused scripts to {TARGET_FOLDER}");
    }
}