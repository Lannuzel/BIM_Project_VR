using System.Collections;
using UnityEngine;

/// <summary>
/// Recette du 6 novembre 2026 : au chargement d'une scène, le casque est recentré devant le panneau de départ
/// (accueil du tutoriel, choix du rôle dans les expériences), quelle que soit l'orientation physique du casque
/// au lancement. Le rig OVR est tourné autour de la caméra puis translaté pour que la caméra se retrouve à
/// <see cref="SessionConfig.startDistance"/> du panneau, face à lui, du côté lisible.
/// Installé par <see cref="SessionCore"/> ; aucune modification de scène.
/// </summary>
public class StartPlacement : MonoBehaviour
{
    /// <summary>Panneaux de départ, par ordre de préférence (tutoriel puis expériences).</summary>
    public static readonly string[] TargetNames = { "WecomeCanvas", "User Role Canvas ", "User Role Canvas" };

    [Tooltip("Délai maximal (s) d'attente d'une pose de tête valide avant de recentrer quand même.")]
    public float trackingTimeout = 2f;

    private void Start()
    {
        StartCoroutine(PlaceWhenTracked());
    }

    private IEnumerator PlaceWhenTracked()
    {
        var config = SessionConfig.Instance;
        if (!config.startInFrontOfPanel) yield break;

        Transform target = FindTarget();
        if (target == null)
        {
            Debug.Log("[StartPlacement] Aucun panneau de départ dans cette scène : position inchangée.");
            yield break;
        }

        // Le suivi de la tête n'est pas disponible dès la première image : attendre que la caméra bouge
        // dans l'espace de suivi (ou le délai maximal).
        float deadline = Time.unscaledTime + trackingTimeout;
        Camera cam = Camera.main;
        while (Time.unscaledTime < deadline)
        {
            cam = Camera.main;
            if (cam != null && cam.transform.localPosition.sqrMagnitude > 1e-6f)
                break;
            yield return null;
        }
        yield return null;
        if (cam == null) yield break;

        var rig = FindObjectOfType<OVRCameraRig>();
        Transform rigTransform = rig != null ? rig.transform : cam.transform.root;
        Place(rigTransform, cam.transform, target, config.startDistance);
    }

    /// <summary>Déplace le rig pour que la caméra soit à <paramref name="distance"/> du panneau, face à lui.</summary>
    public static void Place(Transform rig, Transform camera, Transform target, float distance)
    {
        Vector3 normal = target.forward;
        normal.y = 0f;
        if (normal.sqrMagnitude < 1e-4f) normal = Vector3.forward;
        normal.Normalize();

        // Côté lisible : celui où le rig a été placé par le concepteur de la scène (côté -forward d'un canvas
        // à rotation nulle). Si le rig est dans le plan du panneau, on prend -forward.
        Vector3 toRig = rig.position - target.position;
        toRig.y = 0f;
        float side = Vector3.Dot(toRig, normal);
        float sign = Mathf.Abs(side) < 0.05f ? -1f : Mathf.Sign(side);

        Vector3 desiredCam = target.position + normal * sign * distance;
        Vector3 lookDir = -normal * sign; // de la caméra vers le panneau

        float camYaw = Mathf.Atan2(camera.forward.x, camera.forward.z) * Mathf.Rad2Deg;
        float wantedYaw = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;
        rig.RotateAround(camera.position, Vector3.up, Mathf.DeltaAngle(camYaw, wantedYaw));

        Vector3 delta = desiredCam - camera.position;
        delta.y = 0f; // la hauteur reste celle du suivi (sol)
        rig.position += delta;

        Debug.Log($"[StartPlacement] Casque recentré devant « {target.name} » à {distance:0.0} m.");
    }

    private static Transform FindTarget()
    {
        foreach (var name in TargetNames)
        {
            var go = GameObject.Find(name);
            if (go != null) return go.transform;
        }
        // Objets inactifs (le panneau de rôle peut être désactivé au départ par la scène).
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (!t.gameObject.scene.IsValid() || t.hideFlags != HideFlags.None) continue;
            foreach (var name in TargetNames)
                if (t.name == name) return t;
        }
        return null;
    }
}
