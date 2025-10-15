using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Collections.Unicode;

public class MeasurementHandlerV2 : NetworkBehaviour
{
    private MeasurementHandlerV2 instance = null;
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



        playerInput = GetComponent<PlayerInput>();
        playerInputActions = new XRIBIMInputActions();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public List<GameObject> Lines()
    {
        return lines;
    }
    public void AddLine(GameObject line)
    {
        lines.Add(line);
    }

    public void RemoveLine(GameObject line)
    {
        lines.Remove(line);
    }
    public int LineCount()
    {
        return lines.Count;
    }
    public void DeleteLastLine()
    {
            if (lines.Count > 0)
            {
               GameObject line = lines[lines.Count - 1];
              /*  
            

                if (NetworkManager.Instance.Runner != null)
                {   NetworkObject nwLine = line.GetComponent<NetworkObject>();
                         nwLine.gameObject.SetActive(false);
                Transform startPoint = FindChildWithTagRecursive(line.transform, "StartPoint");
                    if (startPoint != null)
                    {  
                        NetworkObject startPointNW = startPoint.GetComponent<NetworkObject>();
                         if (startPointNW != null && startPointNW.HasStateAuthority)
                                NetworkManager.Instance.Runner.Despawn(startPointNW);
                    }
                    Transform endPoint = FindChildWithTagRecursive(line.transform, "EndPoint");
                    if (endPoint != null)
                    {
                        NetworkObject endtPointNW = endPoint.GetComponent<NetworkObject>();
                        if (endtPointNW != null && endtPointNW.HasStateAuthority)
                        NetworkManager.Instance.Runner.Despawn(endtPointNW);
                    }
                     if (nwLine != null && nwLine.HasStateAuthority)
                         NetworkManager.Instance.Runner.Despawn(nwLine);
                    }
              */
            
                Destroy(line.gameObject);
                lines.RemoveAt(lines.Count - 1);
            }
  

      //  RPC_RequestDeleteLastLine();
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



    // Runs on host (state authority)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestDeleteLastLine()
    {
        InternalDeleteLastLineAsHost();
    }

    // Host-only: actually remove the last line
    private void InternalDeleteLastLineAsHost()
    {
        // Walk back to find the last valid transform
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var t = lines[i];
            if (t == null) { lines.RemoveAt(i); continue; }

            // ✅ Make sure we pick up the NetworkObject even if it's on a parent
            var no = t.GetComponentInParent<NetworkObject>();

            if (no != null && Runner != null)
            {
                Runner.Despawn(no);        // networked delete for everyone
                lines.RemoveAt(i);
                return;
            }
            else
            {
                // Fallback for non-networked lines: local delete
                Destroy(t.gameObject);
                lines.RemoveAt(i);
                return;
            }
        }
    }
}
