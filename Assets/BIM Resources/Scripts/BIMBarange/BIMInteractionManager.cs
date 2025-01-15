using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
public class BIMInteractionManager : MonoBehaviour
{
    [SerializeField]
    private string userName;

    
    public void ToggleActivation(GameObject gameObject)
    {
        if (gameObject.activeSelf) 
            gameObject.SetActive(false);
        else gameObject.SetActive(true);    
    }

    public void ActiveLogRecorder(GameObject gameObject)
    {
        string playerName = PlayerPrefs.GetString("PlayerName", $"Player {NetworkManager.Instance.Runner.LocalPlayer.PlayerId}");
        if(playerName == userName)
        {
            ToggleActivation(gameObject);   
        }

    }
}
