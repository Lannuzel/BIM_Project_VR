using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

[RequireComponent(typeof(OVREyeGaze))]
public class GetRaycastedObject : MonoBehaviour
{
	private int index = 0;
	private NetworkObject lastSentObject = null;
	
	private Transform headTransform;
	private OVREyeGaze eyeGaze;

	private void Start()
	{
		// Récupère l'OVREyeGaze placé sur le même GameObject
		eyeGaze = GetComponent<OVREyeGaze>();

		// On récupère automatiquement la caméra principale du casque
		if (Camera.main != null)
		{
			headTransform = Camera.main.transform;
		}
		else
		{
			Debug.LogError("[Emery] Aucune Main Camera trouvée dans la scène !");
		}
	}

	public NetworkObject GetObject()
	{
		if (headTransform == null)
			return null;

		// La position de départ est TOUJOURS au niveau de la caméra (des yeux du joueur)
		Vector3 rayOrigin = headTransform.position;
		
		// Direction par défaut (là où regarde la tête)
		Vector3 rayDirection = headTransform.forward;

		// Si l'Eye Tracking est actif et qu'il capte le regard, on utilise sa rotation
		// "transform.forward" correspond à l'orientation de cet objet, qui est gérée par l'OVREyeGaze
		if (eyeGaze != null && eyeGaze.EyeTrackingEnabled)
		{
			rayDirection = transform.forward;
		}

		RaycastHit hit;
		// Lancement du raycast avec l'origine (caméra) et la direction (yeux/tête)
		if (Physics.Raycast(rayOrigin, rayDirection, out hit))
		{
			return hit.collider.GetComponent<NetworkObject>();
		}
		return null;
	}

	public void SetIndex(int index)
	{
		Debug.Log($"[Emery] Index set to {index}");
		this.index = index;
	}

	public void FixedUpdate()
	{
		NetworkObject currentObject = GetObject();

		// Optimisation réseau : On n'envoie la valeur que s'il y a un changement d'objet ciblé
		if (currentObject != lastSentObject && GlowObjectRaycasted.Instance != null)
		{
			lastSentObject = currentObject;
			GlowObjectRaycasted.Instance.RPC_SetGazedObject(index, currentObject);
		}
	}

	public int GetIndex()
	{
		return index;
	}

	public NetworkObject GetLastSentObject()
	{
		return lastSentObject;
	}
}
