using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panneau d'information local (non réseau) affiché devant la caméra du joueur,
/// utilisé pour signaler une perte de connexion ou une reconnexion (N4).
/// Il est créé en code à la première utilisation : aucune modification de scène n'est nécessaire.
/// </summary>
public class NetworkStatusPanel : MonoBehaviour
{
    private static NetworkStatusPanel _instance;

    [SerializeField] private float distance = 1.5f;
    [SerializeField] private float verticalOffset = -0.15f;
    [SerializeField] private float followSpeed = 4f;

    private TextMeshProUGUI _text;
    private Canvas _canvas;
    private Coroutine _hideRoutine;

    /// <summary>Affiche le message. Si <paramref name="autoHideSeconds"/> &gt; 0, le panneau se masque ensuite.</summary>
    public static void Show(string message, float autoHideSeconds = 0f)
    {
        var panel = GetOrCreate();
        panel.SetMessage(message);
        panel.gameObject.SetActive(true);
        panel.SnapInFrontOfCamera();

        if (panel._hideRoutine != null)
            panel.StopCoroutine(panel._hideRoutine);
        panel._hideRoutine = null;

        if (autoHideSeconds > 0f)
            panel._hideRoutine = panel.StartCoroutine(panel.HideAfter(autoHideSeconds));
    }

    public static void Hide()
    {
        if (_instance != null)
            _instance.gameObject.SetActive(false);
    }

    private static NetworkStatusPanel GetOrCreate()
    {
        if (_instance != null)
            return _instance;

        var go = new GameObject("NetworkStatusPanel");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<NetworkStatusPanel>();
        _instance.Build();
        return _instance;
    }

    private void Build()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 1000;
        gameObject.AddComponent<CanvasScaler>();

        var rect = _canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900f, 320f);
        rect.localScale = Vector3.one * 0.001f; // 0.9 m x 0.32 m

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(30f, 30f);
        textRect.offsetMax = new Vector2(-30f, -30f);
        _text = textGo.AddComponent<TextMeshProUGUI>();
        _text.fontSize = 42f;
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = Color.white;
        _text.enableWordWrapping = true;
    }

    private void SetMessage(string message)
    {
        if (_text != null)
            _text.text = message;
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
        _hideRoutine = null;
    }

    private static Camera GetPlayerCamera()
    {
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;

        foreach (var c in FindObjectsOfType<Camera>())
        {
            if (c.isActiveAndEnabled && c.targetDisplay == 0)
                return c;
        }
        return null;
    }

    private void SnapInFrontOfCamera()
    {
        var cam = GetPlayerCamera();
        if (cam == null)
            return;

        ComputeTarget(cam, out var pos, out var rot);
        transform.SetPositionAndRotation(pos, rot);
        if (_canvas != null)
            _canvas.worldCamera = cam;
    }

    private void ComputeTarget(Camera cam, out Vector3 pos, out Quaternion rot)
    {
        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = cam.transform.forward;
        forward.Normalize();

        pos = cam.transform.position + forward * distance + Vector3.up * verticalOffset;
        rot = Quaternion.LookRotation(forward, Vector3.up);
    }

    private void LateUpdate()
    {
        var cam = GetPlayerCamera();
        if (cam == null)
            return;

        ComputeTarget(cam, out var pos, out var rot);
        float t = Time.unscaledDeltaTime * followSpeed;
        transform.position = Vector3.Lerp(transform.position, pos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, t);
    }
}
