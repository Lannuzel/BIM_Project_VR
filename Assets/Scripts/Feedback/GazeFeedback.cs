using UnityEngine;

/// <summary>
/// Feedback de regard partagé (portage d'Emery Descours : GetRaycastedObject + GlowObjectRaycasted).
/// Ce casque envoie l'identifiant de l'objet qu'il regarde (rayon depuis la caméra, direction de l'œil suivi
/// OVREyeGaze si disponible, sinon la tête). Quand au moins deux participants regardent le même objet, celui-ci
/// reçoit un contour blanc dont l'opacité monte sur 10 s ; bleu quand les trois le regardent.
/// </summary>
public class GazeFeedback : MonoBehaviour
{
    [Tooltip("Intervalle (s) entre deux évaluations du regard.")]
    public float sampleInterval = 0.1f;
    [Tooltip("Portée (m) du rayon.")]
    public float maxDistance = 30f;
    [Tooltip("Durée (s) de montée de l'opacité du contour.")]
    public float rampSeconds = 10f;
    [Tooltip("Confiance minimale du suivi oculaire pour utiliser l'œil plutôt que la tête.")]
    [Range(0f, 1f)] public float minConfidence = 0.5f;

    private OVREyeGaze eyeGaze;
    private Transform head;
    private Transform ownRig;
    private int lastSentId;
    private float nextSample;
    private int currentShared;
    private float sharedTimer;
    private GazeOutline currentOutline;
    private float nextLog;

    private void Start()
    {
        head = Camera.main != null ? Camera.main.transform : null;
        eyeGaze = GetComponent<OVREyeGaze>();
        if (eyeGaze == null)
        {
            var left = GameObject.Find("LeftEyeInteractor");
            if (left != null) eyeGaze = left.GetComponent<OVREyeGaze>();
            if (eyeGaze == null) eyeGaze = FindObjectOfType<OVREyeGaze>();
        }
        var rig = FindObjectOfType<OVRCameraRig>();
        ownRig = rig != null ? rig.transform : null;
    }

    private void Update()
    {
        var net = FeedbackNetwork.Instance;
        if (net == null || !net.GazeEnabled)
        {
            ClearShared();
            return;
        }

        if (Time.unscaledTime >= nextSample)
        {
            nextSample = Time.unscaledTime + sampleInterval;
            int role = FeedbackSettings.LocalRoleIndex;
            if (role >= 0)
            {
                int id = CurrentTargetId();
                if (id != lastSentId)
                {
                    lastSentId = id;
                    net.RPC_SetGaze(role, id);
                }
            }
        }

        if (!net.Started) { ClearShared(); return; }
        UpdateShared(net);
    }

    private int CurrentTargetId()
    {
        if (head == null) { head = Camera.main != null ? Camera.main.transform : null; if (head == null) return 0; }
        Vector3 origin = head.position;
        Vector3 dir = head.forward;
        if (eyeGaze != null && eyeGaze.EyeTrackingEnabled && eyeGaze.Confidence >= minConfidence)
            dir = eyeGaze.transform.forward;

        foreach (var hit in Physics.RaycastAll(origin, dir, maxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            // Le plus proche qui n'appartient pas à notre propre rig (mains, manettes, menus).
            if (ownRig != null && hit.transform.IsChildOf(ownRig)) continue;
            return GazeTargets.IdOf(hit.collider);
        }
        return 0;
    }

    /// <summary>Objet regardé par le plus de participants (au moins deux) ; contour et journal.</summary>
    private void UpdateShared(FeedbackNetwork net)
    {
        int bestId = 0, bestCount = 0;
        var who = new bool[FeedbackSettings.ParticipantCount];
        for (int i = 0; i < FeedbackSettings.ParticipantCount; i++)
        {
            int id = net.GazeIds[i];
            if (id == 0) continue;
            int count = 0;
            for (int j = 0; j < FeedbackSettings.ParticipantCount; j++)
                if (net.GazeIds[j] == id) count++;
            if (count >= 2 && count > bestCount) { bestCount = count; bestId = id; }
        }
        if (bestId != 0)
            for (int j = 0; j < FeedbackSettings.ParticipantCount; j++) who[j] = net.GazeIds[j] == bestId;

        if (bestId != currentShared)
        {
            ClearShared();
            currentShared = bestId;
            sharedTimer = 0f;
        }
        if (currentShared == 0) return;

        var go = GazeTargets.Find(currentShared);
        if (go == null) return;
        if (currentOutline == null || currentOutline.gameObject != go)
            currentOutline = GazeOutline.Get(go);

        sharedTimer += Time.deltaTime;
        float opacity = Mathf.Clamp01(sharedTimer / Mathf.Max(0.1f, rampSeconds));
        currentOutline.Show(bestCount >= 3 ? Color.blue : Color.white, opacity);

        if (Time.unscaledTime >= nextLog)
        {
            nextLog = Time.unscaledTime + 0.5f;
            FeedbackManager.Log("gaze", "shared", go.name, opacity, bestCount, null, null,
                $"[{(who[0] ? 1 : 0)},{(who[1] ? 1 : 0)},{(who[2] ? 1 : 0)}]");
        }
    }

    private void ClearShared()
    {
        if (currentOutline != null) currentOutline.Hide();
        currentOutline = null;
        if (currentShared != 0) FeedbackManager.Log("gaze", "end", GazeTargets.NameOf(currentShared), sharedTimer);
        currentShared = 0;
        sharedTimer = 0f;
    }

    private void OnDisable() => ClearShared();
}
