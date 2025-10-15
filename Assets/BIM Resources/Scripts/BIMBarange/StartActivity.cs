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
    [SerializeField]
    GameObject interactionMenu;
    [SerializeField]
    GameObject bimInteraction;
    bool isMenuActivated = false;

    public GameObject startActivityGO;

    public float distance = 1.2f;       // meters in front of the user
    public float verticalOffset = -0.1f; // slightly below gaze

    private Fusion.NetworkObject spawnedSAW;

    public static StartActivity Local { get; private set; }

        public override void Spawned()
        {
            if (HasInputAuthority) Local = this;

            // your existing init...
            if (Object.HasStateAuthority) IsInteractionMenuActive = false;
            interactionMenu?.SetActive(false);
            bimInteraction?.SetActive(false);
            isMenuActivated = false;
        }





        [Networked, OnChangedRender(nameof(OnInteractionMenuChanged))]
    public NetworkBool IsInteractionMenuActive { get; set; }



    private void OnInteractionMenuChanged()
    {
        if (!isMenuActivated && IsInteractionMenuActive)
        {
            if (interactionMenu) interactionMenu.SetActive(true);
            if (bimInteraction) bimInteraction.SetActive(true);
            isMenuActivated = true;
        }

    }


    // Update is called once per frame
    void Update()
    {
        try
        {
            int playerCount = NetworkManager.Instance.Runner.ActivePlayers.Count();
            NBplayers.text = "Joueurs :  " + playerCount.ToString() + "/" + MaxPlayers;
            if (playerCount >= MaxPlayers)
            {
                activityStartButton.SetActive(true);
                startActivityGO.SetActive(true);
            }
        }catch(System.Exception e)
        {

        }


    }


   

    private static Camera GetPlayerCamera()
    {
        // 1) Try MainCamera tag
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;

        // 2) Fallback to any enabled camera on display 0
        Camera[] cams = GameObject.FindObjectsOfType<Camera>(true);
        foreach (var c in cams)
        {
            if (c.isActiveAndEnabled && c.targetDisplay == 0)
                return c;
        }
        return null;
    }

    // UI hook — only the input owner asks; StateAuthority writes
    public void ActivateInteractionMenu()
    {
        if (Object.HasInputAuthority)
            RPC_SetInteractionMenu(true);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetInteractionMenu(NetworkBool active)
    {
        IsInteractionMenuActive = active;
    }
    public void OnOtherButtonPressed()
    {
        if (StartActivity.Local != null)
            StartActivity.Local.ActivateInteractionMenu();
        else
            Debug.LogWarning("StartActivity.Local not spawned/ready yet.");
    }

}
