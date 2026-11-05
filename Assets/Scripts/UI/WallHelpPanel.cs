using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panneau mural permanent expliquant la téléportation (F7).
/// Créé en code au démarrage de la scène : soit à une pose fixe (réglable dans l'inspecteur),
/// soit, par défaut, face au joueur à sa position de départ, contre le mur le plus proche
/// dans la direction du regard (raycast), à hauteur des yeux.
/// </summary>
public class WallHelpPanel : MonoBehaviour
{
    [Header("Contenu")]
    public string title = "Se téléporter";
    [TextArea(3, 8)]
    public string body = "Poussez le joystick gauche vers l'avant,\nvisez le sol avec l'arc,\nrelâchez pour vous téléporter.";

    [Header("Placement")]
    [Tooltip("Si vrai, le panneau est placé à la pose fixe ci-dessous. Sinon il est placé face au joueur au démarrage.")]
    public bool useFixedPose = false;
    public Vector3 fixedPosition = new Vector3(0f, 1.6f, 3f);
    public Vector3 fixedEulerAngles = Vector3.zero;
    [Tooltip("Distance maximale (m) de recherche d'un mur devant le joueur.")]
    public float maxWallDistance = 8f;
    [Tooltip("Distance (m) utilisée s'il n'y a pas de mur devant le joueur.")]
    public float fallbackDistance = 3f;
    [Tooltip("Hauteur (m) du centre du panneau.")]
    public float height = 1.6f;
    [Tooltip("Décalage (m) du panneau par rapport au mur, vers le joueur.")]
    public float wallOffset = 0.03f;
    public LayerMask wallLayers = ~0;

    [Header("Apparence")]
    public Vector2 sizeMeters = new Vector2(1.2f, 0.6f);
    public float titleFontSize = 64f;
    public float bodyFontSize = 44f;
    public Color backgroundColor = new Color(0.08f, 0.1f, 0.16f, 0.92f);
    public Color textColor = Color.white;

    private GameObject panel;

    private void Start()
    {
        Build();
        Place();
    }

    private void Build()
    {
        panel = new GameObject("WallHelpPanel");
        panel.transform.SetParent(transform, false);
        var canvas = panel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        panel.AddComponent<CanvasScaler>();

        const float pixelsPerMeter = 1000f;
        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = sizeMeters * pixelsPerMeter;
        rect.localScale = Vector3.one / pixelsPerMeter;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(panel.transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = backgroundColor;

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(panel.transform, false);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.7f); titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(40f, 0f); titleRect.offsetMax = new Vector2(-40f, -20f);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = titleFontSize;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = textColor;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;

        var bodyGo = new GameObject("Body", typeof(RectTransform));
        bodyGo.transform.SetParent(panel.transform, false);
        var bodyRect = bodyGo.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f); bodyRect.anchorMax = new Vector2(1f, 0.7f);
        bodyRect.offsetMin = new Vector2(40f, 30f); bodyRect.offsetMax = new Vector2(-40f, 0f);
        var bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        bodyText.text = body;
        bodyText.fontSize = bodyFontSize;
        bodyText.color = textColor;
        bodyText.alignment = TextAlignmentOptions.Center;
        bodyText.enableWordWrapping = true;
        bodyText.raycastTarget = false;
    }

    private void Place()
    {
        if (useFixedPose)
        {
            panel.transform.SetPositionAndRotation(fixedPosition, Quaternion.Euler(fixedEulerAngles));
            return;
        }

        var cam = Camera.main;
        if (cam == null)
        {
            panel.transform.SetPositionAndRotation(fixedPosition, Quaternion.Euler(fixedEulerAngles));
            return;
        }

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 origin = new Vector3(cam.transform.position.x, height, cam.transform.position.z);
        Vector3 position;
        Vector3 facing; // direction du panneau vers le joueur

        if (Physics.Raycast(origin, forward, out RaycastHit hit, maxWallDistance, wallLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 normal = hit.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.001f) normal = -forward;
            normal.Normalize();
            position = new Vector3(hit.point.x, height, hit.point.z) + normal * wallOffset;
            facing = normal;
        }
        else
        {
            position = origin + forward * fallbackDistance;
            facing = -forward;
        }

        panel.transform.position = position;
        // Le canvas regarde dans la direction de son axe Z : on l'oriente vers le joueur.
        panel.transform.rotation = Quaternion.LookRotation(-facing, Vector3.up);
    }
}
