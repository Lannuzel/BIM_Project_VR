using Fusion;
using UnityEngine;

public class WaitForGameToStart : MonoBehaviour
{
    public void StartGameInFeedbacks()
    {
        if (SpeakingInterruption.Instance != null && GlowObjectRaycasted.Instance != null && GroupFaceFeedback.Instance != null)
		{
			GroupFaceFeedback.Instance.RPC_StartGame();
            GlowObjectRaycasted.Instance.RPC_StartGame();
            SpeakingInterruption.Instance.RPC_StartGame();
		} else
        {
            Debug.LogWarning("[Emery] Tentative d'envoi ignorée : Une des instances n'est pas encore prête.");
		}
	}
}
