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
        return $"Session « {sessionName} », région {photonRegion}, {maxPlayers} joueur(s), arrêt auto {sessionTimeoutMinutes:0} min, {upload}";
    }
}
