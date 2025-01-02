using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MeasurementHandler : MonoBehaviour
{
    private MeasurementHandler instance = null;
    public  Material lineMaterial;
    public XRIBIMInputActions playerInputActions;
    private PlayerInput playerInput;

    public GameObject measurePref;

    public List<Transform> lines;
    public int lineCount = 0;

    private static MeasurementHandler _instance;




    // Start is called before the first frame update
    private void Start()
    {
        lines = new List<Transform>();
    }


    public static MeasurementHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MeasurementHandler>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("MeasurementHandler");
                    _instance = obj.AddComponent<MeasurementHandler>();
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



        playerInput = GetComponent<PlayerInput>();
        playerInputActions = new XRIBIMInputActions();
    }

        // Update is called once per frame
        void Update()
    {
        
    }
    public List<Transform> Lines()
    {
        return lines;
    }
    public void AddLine(Transform line)
    {
        lines.Add(line);
    }

    public void RemoveLine(Transform line)
    {
        lines.Remove(line);
    }
    public int LineCount()
    { return lines.Count; 
    }
    public void DeleteLastLine()
    {
        if (lines.Count > 0)
        {
            Transform line = lines[lines.Count - 1];
            Destroy(line.gameObject);
            lines.RemoveAt(lines.Count - 1);
        }
    }
}
