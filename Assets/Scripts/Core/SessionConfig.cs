using UnityEngine;

/// <summary>
/// Configuration d'une session d'expérimentation, en un seul endroit : l'asset
/// <c>Assets/Resources/SessionConfig.asset</c> (menu Création › BIM VR › Configuration de session).
/// Lue par ConnectionManager (nom de session), NetworkManager (région Photon), StartActivity (nombre de
/// joueurs attendus), SyncManager (arrêt automatique, délai de synchronisation, délai de fermeture),
/// DataUploader (envoi des données) et affichée dans le panneau de l'expérimentateur.
/// </summary>
[CreateAssetMenu(menuName = "BIM VR/Configuration de session", fileName = "SessionConfig")]
public class SessionConfig : ScriptableObject
{
    [Header("Réseau (Photon Fusion, mode Shared)")]
    [Tooltip("Nom de la session Photon rejointe par les trois casques.")]
    public string sessionName = "Modelisateur";
    [Tooltip("Région Photon fixe (eu, us, asia…).")]
    public string photonRegion = "eu";
    [Tooltip("Nombre de joueurs attendus avant de pouvoir lancer l'activité (1 pour les tests, 3 en passation).")]
    [Min(1)] public int maxPlayers = 1;

    [Header("Déroulé")]
    [Tooltip("Arrêt automatique des enregistrements après ce délai (minutes).")]
    [Min(1f)] public float sessionTimeoutMinutes = 30f;
    [Tooltip("Avance (s) donnée à l'heure réseau de lancement pour que tous les casques démarrent ensemble.")]
    [Min(0f)] public float syncLeadSeconds = 1f;
    [Tooltip("Délai (s) entre la fenêtre de remerciement et la fermeture de l'application.")]
    [Min(0f)] public float exitDelayAfterStop = 6f;

    [Header("Rôles de pilotage de l'activité")]
    [Tooltip("Rôle qui voit la fenêtre « Lancer l'activité » (vérification des participants et bouton de lancement). " +
             "Recette du 6 novembre 2026 : le Modélisateur lance, le Lecteur arrête.")]
    public string startActivityRole = "Modelisateur";
    [Tooltip("Rôle qui voit le bouton « Arrêter l'enregistrement » (fin de l'activité) une fois celle-ci lancée.")]
    public string stopActivityRole = "Lecteur";

    [Header("Enregistrements")]
    [Tooltip("Si vrai, l'enregistrement audio démarre dès le chargement de la scène, comme les autres capteurs ; " +
             "le bip de synchronisation est inséré au lancement de l'activité. Sinon l'audio démarre au lancement.")]
    public bool audioFromSceneStart = true;
    [Tooltip("Jouer le bip de synchronisation dans le casque au lancement de l'activité (en plus de son insertion dans le fichier).")]
    public bool playSyncBeep = true;

    [Header("Position de départ")]
    [Tooltip("Au chargement d'une scène, recentre le casque devant le panneau d'accueil (tutoriel) ou de choix du rôle (expériences), " +
             "quelle que soit l'orientation physique du casque.")]
    public bool startInFrontOfPanel = true;
    [Tooltip("Distance (m) entre le casque et le panneau au départ.")]
    [Min(0.5f)] public float startDistance = 1.6f;

    [Header("Feedbacks (travaux d'Emery Descours, portés de la branche feedback)")]
    [Tooltip("Regard partagé : l'objet regardé par au moins deux participants reçoit un contour (blanc, bleu à trois) dont l'opacité monte sur 10 s.")]
    public bool feedbackGaze = false;
    [Tooltip("Parole : détection des interruptions ; micro barré 1 s dans le menu pour celui qui coupe la parole ; message à tous au-delà du seuil par minute.")]
    public bool feedbackVoice = false;
    [Tooltip("Visage : ampoule dont l'opacité suit la concentration (unités d'action 15 et 17) ; message à tous si les trois participants sont jugés non concentrés.")]
    public bool feedbackFace = false;
    [Tooltip("Scènes d'expérience concernées (noms de scènes, ex. Exp1_V3). Vide = toutes.")]
    public System.Collections.Generic.List<string> feedbackScenes = new System.Collections.Generic.List<string>();
    [Tooltip("Parole : seuil de détection de la voix en dBFS (0 = maximum).")]
    public float voiceDbThreshold = -70f;
    [Tooltip("Parole : durée (s) de voix continue pour considérer que le participant parle (et de silence pour qu'il s'arrête).")]
    [Min(0.1f)] public float voiceTimeRequired = 1f;
    [Tooltip("Parole : fenêtre glissante (s) d'évaluation des interruptions.")]
    [Min(5f)] public float voiceWindowSeconds = 60f;
    [Tooltip("Parole : ratio interruptions réussies / tentatives au-delà duquel le message collectif est envoyé.")]
    [Range(0f, 1f)] public float voiceInterruptionRatio = 0.62f;
    [Tooltip("Parole : nombre d'interruptions réussies dans la fenêtre au-delà duquel le message collectif est envoyé.")]
    [Min(1)] public int voiceInterruptionCount = 3;
    [Tooltip("Visage : poids minimal des unités d'action 15 (coins des lèvres) et 17 (menton) pour compter une expression.")]
    [Range(0.001f, 1f)] public float faceWeightThreshold = 0.1f;
    [Tooltip("Visage : fenêtre (s) d'évaluation de la concentration.")]
    [Min(5f)] public float faceEvaluationSeconds = 60f;
    [Tooltip("Visage : proportion d'échantillons avec expression à partir de laquelle le participant est jugé concentré.")]
    [Range(0.001f, 1f)] public float faceThreshold = 0.02f;

    [Header("Envoi des données en fin de session")]
    [Tooltip("URL du récepteur sur le PC de l'expérimentateur (docs/receive_data.py), ex. http://192.168.1.10:8765/upload. Vide = pas d'envoi.")]
    public string uploadUrl = "";
    [Tooltip("Délai maximal (s) accordé à l'envoi avant de fermer l'application quand même.")]
    [Min(5f)] public float uploadTimeoutSeconds = 180f;

    private static SessionConfig instance;

    /// <summary>Asset de Resources, ou valeurs par défaut (avec avertissement) s'il manque.</summary>
    public static SessionConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<SessionConfig>("SessionConfig");
                if (instance == null)
                {
                    Debug.LogWarning("[SessionConfig] Assets/Resources/SessionConfig.asset introuvable : valeurs par défaut.");
                    instance = CreateInstance<SessionConfig>();
                }
            }
            return instance;
        }
    }

    public bool UploadEnabled => !string.IsNullOrWhiteSpace(uploadUrl);

    /// <summary>Résumé lisible pour le panneau de l'expérimentateur.</summary>
    public string Summary()
    {
        string upload = UploadEnabled ? "envoi vers " + uploadUrl.Trim() : "pas d'envoi des données";
        string feedback = FeedbackSummary();
        return $"Session « {sessionName} », région {photonRegion}, {maxPlayers} joueur(s), lancement par {startActivityRole}, arrêt par {stopActivityRole}, arrêt auto {sessionTimeoutMinutes:0} min, {upload}, feedbacks : {feedback}";
    }

    /// <summary>Feedbacks activés dans la configuration (hors interrupteurs de l'expérimentateur).</summary>
    public string FeedbackSummary()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (feedbackGaze) parts.Add("regard");
        if (feedbackVoice) parts.Add("parole");
        if (feedbackFace) parts.Add("visage");
        if (parts.Count == 0) return "aucun";
        string scenes = feedbackScenes != null && feedbackScenes.Count > 0 ? " (" + string.Join(", ", feedbackScenes) + ")" : "";
        return string.Join(" + ", parts) + scenes;
    }
}
