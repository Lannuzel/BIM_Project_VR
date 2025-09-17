using Fusion;

using System.Linq;
using UnityEngine;
using TMPro;

public class StartActivity : NetworkBehaviour
{
    [SerializeField]
    TMP_Text NBplayers;
    [SerializeField]
    int MaxPlayers = 3;
    [SerializeField]
    GameObject activityStartButton;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
      int playerCount =   NetworkManager.Instance.Runner.ActivePlayers.Count();
        NBplayers.text = "Joueurs :  " + playerCount.ToString()+ "/" + MaxPlayers;
        if(playerCount == MaxPlayers)
        {
            activityStartButton.SetActive(true);
        }
    }
}
