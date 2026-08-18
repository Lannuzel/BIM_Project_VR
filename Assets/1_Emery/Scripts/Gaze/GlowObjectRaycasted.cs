using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class GlowObjectRaycasted : NetworkBehaviour
{
	public static GlowObjectRaycasted Instance { get; private set; }

	[Networked]
	public NetworkObject lecteur_GazedObject { get; set; }
	[Networked]
	public NetworkObject calculateur_GazedObject { get; set; }
	[Networked]
	public NetworkObject modelisateur_GazedObject { get; set; }

	[Networked]
	public bool isGameStarted { get; set; } = false;
	public bool hasNeverPutMarkerInData { get; set; } = true;

	private NetworkObject currentGameObject = null;
	private float timer = 0f;


	/// <summary>
	/// Récupération de tous les objets ciblés par chacun des rôles.
	/// </summary>

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void RPC_SetGazedObject(int index, NetworkObject gazedObject)
	{
		if (Object == null || !Object.IsValid)
		{
			Debug.LogWarning("[Emery] Tentative d'envoi ignorée : L'entité réseau n'est pas encore prête.");
			return;
		}

		switch (index)
		{
			case 0:
				lecteur_GazedObject = gazedObject;
				break;
			case 1:
				calculateur_GazedObject = gazedObject;
				break;
			case 2:
				modelisateur_GazedObject = gazedObject;
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

	/// <summary>
	/// Tour de boucles.
	/// </summary>
	public override void Render()
	{
		if (!isGameStarted)
			return;

		if (hasNeverPutMarkerInData)
		{
			DataFeedbacks.Instance.AddFeedbackLog("MARKER;MARKER;MARKER;MARKER;MARKER;MARKER;MARKER;MARKER;MARKER;MARKER;MARKER;MARKER;MARKER;MARKER;MARKER;");
			hasNeverPutMarkerInData = false;
		}

		if (Object == null || !Object.IsValid)
		{
			Debug.Log("[Emery] Update skipped : L'entité réseau n'est pas encore prête : " + Object);
			return;
		}

		NetworkObject tmpGameObject = null;
		bool[] whoLookAtObject = new bool[3] { false, false, false };

		if (lecteur_GazedObject != null && (lecteur_GazedObject == calculateur_GazedObject || lecteur_GazedObject == modelisateur_GazedObject))
		{
			tmpGameObject = lecteur_GazedObject;

			//Gestionnaire pour savoir qui regarde l'objet ciblé, pour les données
			if (lecteur_GazedObject == calculateur_GazedObject)
			{
				whoLookAtObject[0] = true; whoLookAtObject[1] = true;
			}
			else if (lecteur_GazedObject == modelisateur_GazedObject)
			{
				whoLookAtObject[0] = true; whoLookAtObject[2] = true;
			}
			else if (lecteur_GazedObject == calculateur_GazedObject && lecteur_GazedObject == modelisateur_GazedObject)
			{
				whoLookAtObject[0] = true; whoLookAtObject[1] = true; whoLookAtObject[2] = true;
			}
		}
		else if (calculateur_GazedObject != null && calculateur_GazedObject == modelisateur_GazedObject)
		{
			tmpGameObject = calculateur_GazedObject;
		} 

		if (currentGameObject == tmpGameObject && currentGameObject != null)
		{
			if (currentGameObject.TryGetComponent(out QuickOutline outline))
			{
				int nbPlayerLookingAtObject = 2;
				if (calculateur_GazedObject == modelisateur_GazedObject == lecteur_GazedObject)
				{
					outline.OutlineColor = Color.blue;
					nbPlayerLookingAtObject = 3;
				}
				else
				{
					outline.OutlineColor = Color.white;
				}

				timer += Time.deltaTime;
				if (!outline.enabled)
					outline.enabled = true;
				outline.SetOpacity(timer / 10);
				DataFeedbacks.Instance.AddFeedbackLog($";{currentGameObject};{timer / 10};{nbPlayerLookingAtObject};[{whoLookAtObject[0]},{whoLookAtObject[1]},{whoLookAtObject[2]}];");
			}
			else
			{
				Debug.LogWarning("[Emery] Il manque un QuickOutline sur l'objet ciblé.");
			}
		} 
		else 
		{
			if (currentGameObject != null && currentGameObject.TryGetComponent(out QuickOutline currentOutline))
			{
				currentOutline.SetOpacity(0);
				currentOutline.enabled = false;
			}
			
			if (tmpGameObject != null)
			{
				timer = 0f;
			}
		}

		currentGameObject = tmpGameObject;
	}


	/// <summary>
	/// Création de l'objet et récupération de tous les objets ciblés par chacun des rôles.
	/// </summary>
	public override void Spawned()
	{
		Debug.Log("[Emery] GlowObjectRaycasted properly Spawned via Fusion.");

		// Pour un objet de scène unique (comportement de Singleton)
		if (GlowObjectRaycasted.Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			// Si un autre existe déjà en scène, on retire silencieusement celui-ci du réseau
			Runner.Despawn(Object);
		}
	}

	// Important pour nettoyer la variable statique lors de l'arrêt 
	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}
}
