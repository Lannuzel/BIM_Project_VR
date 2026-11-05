using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogRecordActivation : MonoBehaviour
{
    private string userName;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ActiveLogRecorder(GameObject go)
    {
        string playerName = NetworkManager.GetLocalPlayerName();
        if (playerName == userName)
        {
            go.SetActive(true);
        }

    }
}
