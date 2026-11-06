using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Feedback de concentration (portage d'Emery Descours : DetectFaceNegativeAffect + FeedbackFace).
/// Unités d'action 15 (coins des lèvres abaissés, gauche et droite) et 17 (menton relevé, haut et bas) : une
/// expression est comptée quand les quatre poids dépassent le seuil. Sur une fenêtre glissante, la proportion
/// d'échantillons avec expression donne l'opacité de l'ampoule du menu (ratio / seuil, borné à 1) ; le
/// participant est jugé concentré si le ratio atteint le seuil, et cesse de l'être si cela ne se reproduit pas
/// pendant une fenêtre. L'autorité réseau envoie le message collectif quand les trois sont non concentrés.
/// </summary>
public class FaceFeedback : MonoBehaviour
{
    private OVRFaceExpressions face;
    private readonly Queue<(float time, bool active)> samples = new Queue<(float, bool)>();
    private int activeCount;
    private float firstSampleTime = -1f;
    private bool focused = true;
    private bool focusSent = true;
    private float lastFocusedTime;
    private float nextLog;
    private int seenMessageSeq = -1;

    private static readonly OVRFaceExpressions.FaceExpression[] NegativeAffect =
    {
        OVRFaceExpressions.FaceExpression.LipCornerDepressorL,
        OVRFaceExpressions.FaceExpression.LipCornerDepressorR,
        OVRFaceExpressions.FaceExpression.ChinRaiserT,
        OVRFaceExpressions.FaceExpression.ChinRaiserB,
    };

    private void Start()
    {
        face = GetComponent<OVRFaceExpressions>();
        if (face == null) face = FindObjectOfType<OVRFaceExpressions>();
        if (face == null) Debug.LogWarning("[FaceFeedback] OVRFaceExpressions introuvable : feedback de concentration inactif.");
    }

    /// <summary>Vrai si les quatre unités d'action dépassent le seuil (version publiée).</summary>
    public static bool NegativeAffectActive(OVRFaceExpressions f, float threshold)
    {
        if (f == null || !f.ValidExpressions) return false;
        foreach (var e in NegativeAffect)
            if (f.GetWeight(e) <= threshold) return false;
        return true;
    }

    private void FixedUpdate()
    {
        var net = FeedbackNetwork.Instance;
        var c = SessionConfig.Instance;
        if (net == null || !net.FaceEnabled || face == null) return;
        int role = FeedbackSettings.LocalRoleIndex;

        float window = c.faceEvaluationSeconds;
        bool active = NegativeAffectActive(face, c.faceWeightThreshold);
        samples.Enqueue((Time.time, active));
        if (active) activeCount++;
        while (samples.Count > 0 && Time.time - samples.Peek().time > window)
        {
            if (samples.Dequeue().active) activeCount--;
        }
        if (firstSampleTime < 0f) { firstSampleTime = Time.time; lastFocusedTime = Time.time; }
        if (Time.time - firstSampleTime < window) return; // fenêtre pas encore remplie

        float ratio = samples.Count > 0 ? (float)activeCount / samples.Count : 0f;
        float t = Mathf.Clamp01(ratio / c.faceThreshold);
        FeedbackManager.SetBulbOpacity(t);
        if (Time.unscaledTime >= nextLog)
        {
            nextLog = Time.unscaledTime + 0.5f;
            FeedbackManager.Log("face", "opacity", "", t, activeCount, samples.Count, ratio);
        }

        if (ratio >= c.faceThreshold)
        {
            lastFocusedTime = Time.time;
            focused = true;
        }
        else if (focused && Time.time - lastFocusedTime >= window)
        {
            focused = false;
        }

        if (net.Started && role >= 0 && focused != focusSent)
        {
            focusSent = focused;
            net.RPC_SetFocus(role, focused);
            FeedbackManager.Log("face", focused ? "focused" : "unfocused", "", ratio);
        }

        if (seenMessageSeq < 0) seenMessageSeq = net.MessageSeq;
        if (net.MessageSeq != seenMessageSeq)
        {
            seenMessageSeq = net.MessageSeq;
            if (net.MessageKind == FeedbackNetwork.MessageKindFace)
                FeedbackManager.Log("face", "group", "", 1f);
        }
    }
}
