using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Feedback de parole (portage d'Emery Descours : DetectSpeaking). Le niveau du micro est lu dans le flux déjà
/// ouvert par <see cref="AudioTracker"/> (Unity n'accepte qu'une capture par micro : la version publiée ouvrait
/// un second enregistrement et pouvait corrompre le fichier vocal). Au-delà du seuil dBFS pendant
/// <c>voiceTimeRequired</c>, ce casque est déclaré parlant ; l'autorité réseau détermine les interruptions.
/// Feedback individuel : micro barré 1 s dans le menu de la main gauche pour celui qui coupe la parole.
/// Feedback collectif : message à tous (et son) quand, sur une fenêtre glissante, le ratio interruptions
/// réussies / tentatives dépasse le seuil et le nombre de réussites dépasse le seuil.
/// </summary>
public class VoiceFeedback : MonoBehaviour
{
    [Tooltip("Durée (s) de silence avant de signaler une pause (l'autorité ne compte pas comme interrupteur celui qui parle pendant une pause de l'orateur).")]
    public float pauseAfterSeconds = 0.2f;
    public float pictoSeconds = 1f;

    private AudioTracker audioTracker;
    private bool speaking;
    private bool pausingSent;
    private float speakingTime, silentTime;
    private int seenInterruptionSeq = -1, seenFailedSeq = -1;
    private readonly List<float> successes = new List<float>();
    private readonly List<float> failures = new List<float>();
    private float windowStart;
    private float nextWindowCheck;
    private bool warnedNoMic;

    private void Update()
    {
        var net = FeedbackNetwork.Instance;
        var c = SessionConfig.Instance;
        if (net == null || !net.VoiceEnabled)
        {
            if (speaking) { speaking = false; speakingTime = silentTime = 0f; }
            return;
        }

        int role = FeedbackSettings.LocalRoleIndex;
        if (role >= 0) DetectSpeaking(net, c, role);
        WatchEvents(net, role);
        if (net.Started && role >= 0) EvaluateWindow(net, c, role);
    }

    private void DetectSpeaking(FeedbackNetwork net, SessionConfig c, int role)
    {
        if (audioTracker == null) audioTracker = FindObjectOfType<AudioTracker>();
        if (audioTracker == null || !audioTracker.TryGetRecentPeak(256, out float peak))
        {
            if (!warnedNoMic)
            {
                warnedNoMic = true;
                Debug.LogWarning("[VoiceFeedback] Capture micro de l'enregistrement indisponible : détection de la parole inactive.");
            }
            return;
        }
        warnedNoMic = false;

        float db = peak > 0f ? 20f * Mathf.Log10(peak) : -140f;
        if (db >= c.voiceDbThreshold)
        {
            speakingTime += Time.deltaTime;
            silentTime = 0f;
            if (pausingSent) { pausingSent = false; net.RPC_SetPausing(role, false); }
            if (!speaking && speakingTime >= c.voiceTimeRequired)
            {
                speaking = true;
                net.RPC_SetSpeaking(role, true);
                FeedbackManager.Log("voice", "speaking", "", db);
            }
        }
        else
        {
            silentTime += Time.deltaTime;
            if (speaking && silentTime > pauseAfterSeconds && !pausingSent)
            {
                pausingSent = true;
                net.RPC_SetPausing(role, true);
            }
            if (silentTime >= c.voiceTimeRequired)
            {
                speakingTime = 0f;
                if (speaking)
                {
                    speaking = false;
                    net.RPC_SetSpeaking(role, false);
                    if (pausingSent) { pausingSent = false; net.RPC_SetPausing(role, false); }
                    FeedbackManager.Log("voice", "silent", "", db);
                }
            }
        }
    }

    /// <summary>Interruptions décidées par l'autorité : journal pour tous, pictogramme pour l'interrupteur.</summary>
    private void WatchEvents(FeedbackNetwork net, int role)
    {
        if (seenInterruptionSeq < 0) seenInterruptionSeq = net.InterruptionSeq;
        if (seenFailedSeq < 0) seenFailedSeq = net.FailedSeq;

        if (net.InterruptionSeq != seenInterruptionSeq)
        {
            seenInterruptionSeq = net.InterruptionSeq;
            int who = net.LastInterrupter;
            FeedbackManager.Log("voice", "interruption", FeedbackSettings.RoleLabel(who), null, null, null, null, who.ToString());
            if (who == role && net.Started)
            {
                successes.Add(Time.time);
                FeedbackManager.ShowMicPicto(pictoSeconds);
                FeedbackManager.Log("voice", "individual", "", 1f);
            }
        }
        if (net.FailedSeq != seenFailedSeq)
        {
            seenFailedSeq = net.FailedSeq;
            int who = net.LastFailedInterrupter;
            FeedbackManager.Log("voice", "failed", FeedbackSettings.RoleLabel(who), null, null, null, null, who.ToString());
            if (who == role && net.Started) failures.Add(Time.time);
        }
    }

    private void EvaluateWindow(FeedbackNetwork net, SessionConfig c, int role)
    {
        float window = c.voiceWindowSeconds;
        if (windowStart <= 0f) windowStart = Time.time;
        successes.RemoveAll(t => Time.time - t > window);
        failures.RemoveAll(t => Time.time - t > window);

        if (Time.time - windowStart < window || Time.time < nextWindowCheck) return;
        nextWindowCheck = Time.time + 1f;

        int s = successes.Count, total = s + failures.Count;
        float ratio = total > 0 ? (float)s / total : 0f;
        if (ratio > c.voiceInterruptionRatio && s > c.voiceInterruptionCount)
        {
            net.RPC_GroupVoice(role, s);
            FeedbackManager.Log("voice", "group", "", null, s, total, ratio, role.ToString());
            successes.Clear();
            failures.Clear();
            windowStart = Time.time;
        }
    }
}
