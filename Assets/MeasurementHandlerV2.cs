using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MeasurementHandlerV2 : NetworkBehaviour
{
    public Material lineMaterial;
    public XRIBIMInputActions playerInputActions;
    private PlayerInput playerInput;

    public GameObject measurePref;

    public List<GameObject> lines;
    public int lineCount = 0;

    private static MeasurementHandlerV2 _instance;

    // Start is called before the first frame update
    private void Start()
    {
        if (lines == null)
            lines = new List<GameObject>();
    }


    public static MeasurementHandlerV2 Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MeasurementHandlerV2>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("MeasurementHandler");
                    _instance = obj.AddComponent<MeasurementHandlerV2>();
                }
            }
            return _instance;
        }
    }


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Ensure only one instance exists
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject); // Optional: keep the instance across scenes

        if (lines == null)
            lines = new List<GameObject>();

        playerInput = GetComponent<PlayerInput>();
        playerInputActions = new XRIBIMInputActions();
    }

    public List<GameObject> Lines()
    {
        return lines;
    }
    public void AddLine(GameObject line)
    {
        if (line != null && !lines.Contains(line))
            lines.Add(line);
    }

    public void RemoveLine(GameObject line)
    {
        lines.Remove(line);
    }
    public int LineCount()
    {
        lines.RemoveAll(l => l == null);
        return lines.Count;
    }

    /// <summary>
    /// Supprime la dernière mesure pour tous les participants (N5 : Despawn réseau de la ligne, de ses
    /// marqueurs et de son étiquette, au lieu d'un Destroy local) ; l'action est annulable (F19).
    /// </summary>
    public void DeleteLastLine()
    {
        lines.RemoveAll(l => l == null);
        if (lines.Count == 0)
            return;

        GameObject line = lines[lines.Count - 1];
        lines.RemoveAt(lines.Count - 1);

        var undo = UndoManager.CaptureForDelete("Supprimer la dernière mesure", new[] { line });
        NetworkObjectUtils.DespawnHierarchy(line);
        if (undo != null)
            UndoManager.Push(undo);
    }

    private Transform FindChildWithTagRecursive(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
            {
                return child;
            }
            Transform found = FindChildWithTagRecursive(child, tag);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
}
