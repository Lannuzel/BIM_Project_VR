using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// État réseau partagé des feedbacks (portage d'Emery Descours : SpeakingInterruption, GlowObjectRaycasted,
/// GroupFaceFeedback réunis). Un seul objet par session, créé par le maître de session (prefab
/// Resources/Feedback/FeedbackNetwork généré par Outils BIM VR) ; l'autorité suit le maître (MasterClientObject).
/// Les casques envoient leur état par RPC ; l'autorité tient la machine à états de la parole et décide des
/// messages collectifs ; chacun lit l'état répliqué pour afficher les feedbacks.
/// Écarts documentés par rapport à la version publiée (corrections) : tolérance de pause réellement appliquée
/// (le compteur de silence n'était jamais incrémenté), pas de temps-image Unity dans la boucle réseau
/// (Runner.DeltaTime), propriétés réseau écrites uniquement par l'autorité, comparaison des trois regards corrigée.
/// </summary>
public class FeedbackNetwork : NetworkBehaviour
{
    public static FeedbackNetwork Instance { get; private set; }

    public const int MessageKindVoice = 1;
    public const int MessageKindFace = 2;

    [Networked] public NetworkBool GazeEnabled { get; set; }
    [Networked] public NetworkBool VoiceEnabled { get; set; }
    [Networked] public NetworkBool FaceEnabled { get; set; }
    [Networked] public NetworkBool Started { get; set; }

    /// <summary>Identifiant d'objet regardé par rôle (0 = rien), voir <see cref="GazeTargets"/>.</summary>
    [Networked, Capacity(FeedbackSettings.ParticipantCount)] public NetworkArray<int> GazeIds => default;
    [Networked, Capacity(FeedbackSettings.ParticipantCount)] public NetworkArray<NetworkBool> Speaking => default;
    [Networked, Capacity(FeedbackSettings.ParticipantCount)] public NetworkArray<NetworkBool> Pausing => default;
    [Networked, Capacity(FeedbackSettings.ParticipantCount)] public NetworkArray<NetworkBool> Focused => default;

    [Networked] public int CurrentSpeaker { get; set; }
    [Networked] public int InterruptionSeq { get; set; }
    [Networked] public int LastInterrupter { get; set; }
    [Networked] public int FailedSeq { get; set; }
    [Networked] public int LastFailedInterrupter { get; set; }

    [Networked] public int MessageSeq { get; set; }
    [Networked] public int MessageKind { get; set; }
    [Networked] public float MessageSeconds { get; set; }
    [Networked, Capacity(200)] public string Message { get; set; }
    [Networked] public NetworkBool FaceGroupActive { get; set; }

    [Tooltip("Parole : silence (s) toléré avant de considérer que l'orateur s'est tu.")]
    public float noPauseTiming = 0.5f;
    [Tooltip("Visage : durée (s) d'affichage du message collectif.")]
    public float faceMessageSeconds = 5f;
    public const string FaceGroupMessage = "N'hésitez pas à relire vos informations.";

    // Machine à états de la parole (autorité uniquement)
    private readonly float[] silence = { 999f, 999f, 999f };
    private HashSet<int> speakingLast = new HashSet<int>();
    private readonly HashSet<int> interrupters = new HashSet<int>();
    private bool faceShown;
    private float faceMessageUntil;

    public override void Spawned()
    {
        if (Instance != null && Instance != this && Instance.Object != null && Instance.Object.IsValid)
        {
            if (HasStateAuthority) Runner.Despawn(Object);
            return;
        }
        Instance = this;
        if (HasStateAuthority)
        {
            // État initial : choix de l'expérimentateur s'il a déjà basculé, sinon configuration (feedbackScenes comprise).
            GazeEnabled = FeedbackManager.InitialEnabled(0);
            VoiceEnabled = FeedbackManager.InitialEnabled(MessageKindVoice);
            FaceEnabled = FeedbackManager.InitialEnabled(MessageKindFace);
            CurrentSpeaker = -1;
            LastInterrupter = -1;
            LastFailedInterrupter = -1;
            for (int i = 0; i < FeedbackSettings.ParticipantCount; i++) Focused.Set(i, true);
            Started = SyncManager.Instance != null && SyncManager.Instance.IsRecordingStarted;
        }
        Debug.Log($"[FeedbackNetwork] Objet réseau des feedbacks prêt (autorité : {HasStateAuthority}).");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    public bool IsEnabled(int kind) => kind == MessageKindVoice ? VoiceEnabled : kind == MessageKindFace ? FaceEnabled : GazeEnabled;

    #region RPC (tous → autorité)

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetGaze(int role, int id)
    {
        if (role < 0 || role >= FeedbackSettings.ParticipantCount) return;
        GazeIds.Set(role, id);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetSpeaking(int role, NetworkBool speaking)
    {
        if (role < 0 || role >= FeedbackSettings.ParticipantCount) return;
        Speaking.Set(role, speaking);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetPausing(int role, NetworkBool pausing)
    {
        if (role < 0 || role >= FeedbackSettings.ParticipantCount) return;
        Pausing.Set(role, pausing);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetFocus(int role, NetworkBool focused)
    {
        if (role < 0 || role >= FeedbackSettings.ParticipantCount) return;
        Focused.Set(role, focused);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_GroupVoice(int role, int count)
    {
        PostMessage($"Attention {FeedbackSettings.RoleLabel(role)},\nvous avez coupé et repris {count} fois\nla parole dans la dernière minute.", MessageKindVoice, 10f);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetEnabled(NetworkBool gaze, NetworkBool voice, NetworkBool face)
    {
        GazeEnabled = gaze;
        VoiceEnabled = voice;
        FaceEnabled = face;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetStarted(NetworkBool started)
    {
        Started = started;
        if (started)
        {
            CurrentSpeaker = -1;
            interrupters.Clear();
            speakingLast.Clear();
            faceShown = false;
        }
    }

    #endregion

    private void PostMessage(string text, int kind, float seconds)
    {
        if (!HasStateAuthority) return;
        Message = text;
        MessageKind = kind;
        MessageSeconds = seconds;
        MessageSeq++;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !Started) return;
        float dt = Runner.DeltaTime;

        if (VoiceEnabled) StepVoice(dt);
        if (FaceEnabled) StepFace();
    }

    /// <summary>Orateur courant, interruptions réussies (l'orateur se tait pendant qu'un autre parle encore) ou ratées.</summary>
    private void StepVoice(float dt)
    {
        var now = new HashSet<int>();
        for (int i = 0; i < FeedbackSettings.ParticipantCount; i++)
        {
            if (Speaking[i])
            {
                silence[i] = 0f;
                now.Add(i);
            }
            else
            {
                silence[i] += dt;
                // Tolérance de pause : un orateur qui vient de se taire reste considéré comme parlant un court instant.
                if (speakingLast.Contains(i) && silence[i] <= noPauseTiming)
                    now.Add(i);
            }
        }

        // Nouvelles prises de parole
        foreach (int p in now)
        {
            if (speakingLast.Contains(p)) continue;
            if (CurrentSpeaker == -1)
                CurrentSpeaker = p;
            else if (p != CurrentSpeaker && !Pausing[CurrentSpeaker])
                interrupters.Add(p);
        }

        // Arrêts de parole
        foreach (int p in speakingLast)
        {
            if (now.Contains(p)) continue;
            if (p == CurrentSpeaker)
            {
                interrupters.IntersectWith(now);
                int taker = -1;
                foreach (int i in interrupters) { taker = i; break; }
                if (taker != -1)
                {
                    LastInterrupter = taker;
                    InterruptionSeq++;
                    CurrentSpeaker = taker;
                    interrupters.Remove(taker);
                }
                else
                {
                    CurrentSpeaker = -1;
                }
            }
            else if (interrupters.Contains(p))
            {
                LastFailedInterrupter = p;
                FailedSeq++;
                interrupters.Remove(p);
            }
        }

        interrupters.IntersectWith(now);
        if (CurrentSpeaker != -1 && !now.Contains(CurrentSpeaker))
            CurrentSpeaker = -1;
        speakingLast = now;
    }

    /// <summary>Message collectif quand les trois participants sont jugés non concentrés (une fois par épisode).</summary>
    private void StepFace()
    {
        bool anyFocused = false;
        for (int i = 0; i < FeedbackSettings.ParticipantCount; i++)
            if (Focused[i]) { anyFocused = true; break; }

        if (!anyFocused && !faceShown)
        {
            faceShown = true;
            FaceGroupActive = true;
            faceMessageUntil = Runner.SimulationTime + faceMessageSeconds;
            PostMessage(FaceGroupMessage, MessageKindFace, faceMessageSeconds);
        }
        else if (anyFocused)
        {
            faceShown = false;
        }
        if (FaceGroupActive && Runner.SimulationTime >= faceMessageUntil)
            FaceGroupActive = false;
    }
}
