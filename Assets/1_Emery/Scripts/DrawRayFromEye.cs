using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawRayFromEye : MonoBehaviour
{
	[Header("Paramètres Eye Tracking")]
	[Tooltip("Le composant OVREyeGaze gérant l'œil à utiliser (gauche ou droit).")]
	public OVREyeGaze eyeGaze;

	[Tooltip("Le composant qui bouge correctement avec la téléportation (ex: CenterEyeAnchor ou MainCamera).")]
	public Transform teleportedHeadTransform;

	[Tooltip("Seuil de confiance minimum (0 à 1) pour afficher le rayon.")]
	[Range(0f, 1f)]
	public float confidenceThreshold = 0.5f;

	[Header("Paramètres du Rayon")]
	public float rayDistance = 50f;
	public float rayWidth = 0.01f;
	public Color rayColor = Color.cyan;

	private LineRenderer lineRenderer;

	private void Start()
	{
		lineRenderer = GetComponent<LineRenderer>();
		lineRenderer.positionCount = 2; // Le rayon a un début et une fin
		lineRenderer.startWidth = rayWidth;
		lineRenderer.endWidth = rayWidth;

		// CRUCIAL : Force le LineRenderer à utiliser les coordonnées globales.
		// Cela garantit que le rayon suit correctement la position dans le monde, 
		// indépendamment de son GameObject parent.
		lineRenderer.useWorldSpace = true;

		lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
		lineRenderer.startColor = rayColor;
		lineRenderer.endColor = rayColor;

		// Optionnel : si l'utilisateur oublie de l'assigner, on essaie de prendre la caméra principale
		if (teleportedHeadTransform == null && Camera.main != null)
		{
			teleportedHeadTransform = Camera.main.transform;
		}
	}

	// Remplacement de Update() par LateUpdate()
	// LateUpdate est appelé après tous les autres Update(). 
	// Cela permet de s'assurer que la téléportation de l'OVR rig est appliquée 
	// AVANT de dessiner le rayon, évitant ainsi un décalage visuel.
	private void LateUpdate()
	{
		if (eyeGaze == null || !eyeGaze.EyeTrackingEnabled || eyeGaze.Confidence < confidenceThreshold)
		{
			lineRenderer.enabled = false;
			return;
		}

		lineRenderer.enabled = true;

		// Utilise le regard de l'œil pour la direction (rotation)
		Vector3 gazeDirection = eyeGaze.transform.forward;
		
		// Utilise la tête / caméra pour la position (qui prend en compte la téléportation)
		Vector3 startPosition = teleportedHeadTransform != null ? teleportedHeadTransform.position : eyeGaze.transform.position;

		lineRenderer.SetPosition(0, startPosition);

		RaycastHit hit;

		if (Physics.Raycast(startPosition, gazeDirection, out hit, rayDistance))
		{
			// Point d'impact 
			lineRenderer.SetPosition(1, hit.point);
		}
		else
		{
			// Point de fin au maximum de la portée
			Vector3 endPosition = startPosition + (gazeDirection * rayDistance);
			lineRenderer.SetPosition(1, endPosition);
		}
	}
}