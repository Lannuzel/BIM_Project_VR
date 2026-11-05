using UnityEngine;

/// <summary>
/// Permet de déplacer une fenêtre (canvas monde) avec la gâchette intérieure (grip) de la main droite (F10) :
/// pointer la fenêtre avec la manette, maintenir le grip, la fenêtre suit la main, relâcher pour la poser.
/// Ajouté à chaque fenêtre par <see cref="UIWindowsManager"/> ; aucun collider n'est nécessaire,
/// l'intersection est calculée avec le plan du canvas.
/// </summary>
public class WindowGrabMover : MonoBehaviour
{
    [Tooltip("Rect (canvas) servant de zone de saisie. Si vide : le premier RectTransform de canvas trouvé sous l'objet.")]
    public RectTransform grabRect;
    [Tooltip("Transform de la manette droite. Si vide : OVR RightHandAnchor / RightControllerAnchor recherché dans la scène.")]
    public Transform rightController;
    [Tooltip("Portée (m) du pointeur pour saisir une fenêtre.")]
    public float maxGrabDistance = 6f;
    [Tooltip("Lissage du suivi (0 = instantané).")]
    public float followSmoothing = 20f;
    [Tooltip("Garder la fenêtre verticale pendant le déplacement.")]
    public bool keepUpright = true;

    /// <summary>Fenêtre en cours de déplacement (une seule à la fois).</summary>
    public static WindowGrabMover Current { get; private set; }

    private bool grabbing;
    private Vector3 localPosOffset;
    private Quaternion localRotOffset;

    private void Awake()
    {
        if (grabRect == null)
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null)
                grabRect = canvas.GetComponent<RectTransform>();
        }
    }

    private void OnDisable()
    {
        if (Current == this) Current = null;
        grabbing = false;
    }

    private void Update()
    {
        if (rightController == null)
            rightController = UIWindowsManager.FindRightController();
        if (rightController == null || grabRect == null)
            return;

        if (!grabbing)
        {
            if (Current == null && OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger) && PointerHitsRect())
                BeginGrab();
            return;
        }

        if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger))
            FollowController();
        else
            EndGrab();
    }

    private bool PointerHitsRect()
    {
        var plane = new Plane(grabRect.forward, grabRect.position);
        var ray = new Ray(rightController.position, rightController.forward);
        if (!plane.Raycast(ray, out float enter) || enter > maxGrabDistance)
            return false;

        Vector3 local = grabRect.InverseTransformPoint(ray.GetPoint(enter));
        return grabRect.rect.Contains(new Vector2(local.x, local.y));
    }

    private void BeginGrab()
    {
        grabbing = true;
        Current = this;
        localPosOffset = rightController.InverseTransformPoint(transform.position);
        localRotOffset = Quaternion.Inverse(rightController.rotation) * transform.rotation;
    }

    private void FollowController()
    {
        Vector3 targetPos = rightController.TransformPoint(localPosOffset);
        Quaternion targetRot = rightController.rotation * localRotOffset;
        if (keepUpright)
        {
            Vector3 fwd = targetRot * Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.001f)
                targetRot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        }

        float t = followSmoothing <= 0f ? 1f : Mathf.Clamp01(Time.deltaTime * followSmoothing);
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
    }

    private void EndGrab()
    {
        grabbing = false;
        if (Current == this) Current = null;
    }
}
