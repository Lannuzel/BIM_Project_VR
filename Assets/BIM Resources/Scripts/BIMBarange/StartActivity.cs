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

    private int lastDisplayedCount = -1;
    private string lastDisplayedState = string.Empty;

        public override void Spawned()
        {
            // N5 : objet de scène en mode Shared (autorité d'état = master client, pas d'autorité d'entrée) :
            // chaque casque a sa copie locale, c'est elle qui sert de point d'entrée.
            Local = this;
            MaxPlayers = SessionConfig.Instance.maxPlayers; // Assets/Resources/SessionConfig.asset

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
        // F17 : plus de try/catch muet. Tant que la session n'est pas active, on l'indique
        // au lieu de laisser le texte figé « Joueurs : 0/3 ».
        var nm = NetworkManager.Instance;

        if (nm == null || nm.Runner == null)
        {
            SetState("Joueurs : en attente de connexion...", -1);
            return;
        }

        if (!nm.IsRunning)
        {
            SetState(nm.IsReconnecting ? "Joueurs : reconnexion en cours..." : "Joueurs : connexion en cours...", -1);
            return;
        }

        // ActivePlayers inclut le joueur local dès que la session est active ; F18 : les joueurs simulés
        // par l'expérimentateur comptent comme connectés.
        int playerCount = nm.Runner.ActivePlayers.Count() + AdminTools.SimulatedCount;
        SetState("Joueurs :  " + playerCount.ToString() + "/" + MaxPlayers, playerCount);

        if (playerCount >= MaxPlayers)
        {
            if (activityStartButton != null && !activityStartButton.activeSelf)
                activityStartButton.SetActive(true);
            if (startActivityGO != null && !startActivityGO.activeSelf)
                startActivityGO.SetActive(true);
        }
    }

    private void SetState(string text, int count)
    {
        if (text == lastDisplayedState && count == lastDisplayedCount)
            return;

        lastDisplayedState = text;
        lastDisplayedCount = count;

        if (NBplayers != null)
            NBplayers.text = text;
        else
            Debug.LogError("[StartActivity] Le champ NBplayers n'est pas assigné : le compteur de joueurs ne peut pas s'afficher.");
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

    // UI hook : only the input owner asks; StateAuthority writes
    public void ActivateInteractionMenu()
    {
        if (Object != null && Object.IsValid)
            RPC_SetInteractionMenu(true);
    }

    // N5 : tout casque peut demander ; l'autorité d'état (master client) écrit la valeur répliquée.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
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
