using Fusion;
using System.Collections;
using UnityEngine;

public class GroupFaceFeedback : NetworkBehaviour
{
	public static GroupFaceFeedback Instance { get; private set; }

	[Networked]
	[SerializeField]
	private NetworkObject feedbackGroupImage { get; set; }
	[SerializeField]
	private float timerBeforeDisablingFeedback = 5f;
	[SerializeField]
	private AudioSource announcementFeedback { get; set; }

	[Networked]
	private bool isLecteurFocused { get; set; } = true;
	[Networked]
	private bool isCalculateurFocused { get; set; } = true;
	[Networked]
	private bool isModelisateurFocused { get; set; } = true;
	[Networked]
	private bool hasAlreadyShowFeedback { get; set; } = false;
	[Networked]
	private bool isGameStarted { get; set; } = false;

	[Networked]
	public bool isFeedbackGroupImageActive { get; set; } = false;

	private void Awake()
	{
		announcementFeedback = GetComponent<AudioSource>();
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void RPC_SetFocus(int roleIndex, bool isFocused)
	{
		switch (roleIndex)
		{
			case 0:
				isLecteurFocused = isFocused;
				break;
			case 1:
				isCalculateurFocused = isFocused;
				break;
			case 2:
				isModelisateurFocused = isFocused;
				break;
			default:
				Debug.LogWarning("[Emery] Invalid role index: " + roleIndex);
				break;
		}
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void RPC_StartGame()
	{
		if (Object == null || !Object.IsValid)
		{
			Debug.LogWarning("[Emery] Tentative d'envoi ignorée : L'entité réseau n'est pas encore prête.");
			return;
		}
		isGameStarted = true;
	}

	public override void Render()
	{
		if (!isGameStarted) return;

		if (!isCalculateurFocused && !isLecteurFocused && !isModelisateurFocused && !hasAlreadyShowFeedback)
		{
			if (!feedbackGroupImage.gameObject.activeSelf)
			{
				feedbackGroupImage.gameObject.SetActive(true);
				if (announcementFeedback != null && !announcementFeedback.isPlaying)
				{
					announcementFeedback.Play();
				}
				hasAlreadyShowFeedback = true;
				StartCoroutine(DisableFeedbackGroupImage());
			}
		}
		else if (isCalculateurFocused || isLecteurFocused || isModelisateurFocused)
		{
			hasAlreadyShowFeedback = false;
		}
	}

	private IEnumerator DisableFeedbackGroupImage()
	{
		//DataFeedbacks.Instance.AddFeedbackLog(5, "Feedback displayed for all roles.");
		isFeedbackGroupImageActive = true;
		yield return new WaitForSeconds(timerBeforeDisablingFeedback);
		feedbackGroupImage.gameObject.SetActive(false);
		//DataFeedbacks.Instance.AddFeedbackLog(5, "Feedback hidden after timer.");
		isFeedbackGroupImageActive = false;
	}

	/// <summary>
	/// Création de l'objet et récupération de tous les objets ciblés par chacun des rôles.
	/// </summary>
	public override void Spawned()
	{
		Debug.Log("[Emery] GroupFaceFeedback properly Spawned via Fusion.");

		// Pour un objet de scène unique (comportement de Singleton)
		if (GroupFaceFeedback.Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			// Si un autre existe déjà en scène, on retire silencieusement celui-ci du réseau
			Runner.Despawn(Object);
		}
	}
}
