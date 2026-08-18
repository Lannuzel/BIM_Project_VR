using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LaunchGameButton : NetworkBehaviour
{
    public string GameSceneName = "GameScene";

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            GetComponentInParent<Canvas>().gameObject.SetActive(false);
            return;
        }
        GetComponent<Button>().onClick.AddListener(LaunchGame);
    }

    private void LaunchGame()
    {
        if (IsServer)
        {
            NetworkManager.SceneManager.LoadScene(GameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
