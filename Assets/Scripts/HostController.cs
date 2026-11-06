using Fusion;
using System.Collections;
using UnityEngine;

/// <summary>
/// Commandes de session du joueur local (N5) : lancement / arrêt de l'enregistrement synchronisé
/// via <see cref="SyncManager"/>, et fenêtre de remerciement affichée localement sur chaque casque.
/// Une instance vit sur chaque avatar ; <see cref="instance"/> est celle du joueur local.
/// </summary>
public class HostController : NetworkBehaviour
{
    [SerializeField] private float delay = 1800f;
    [SerializeField] private float distance = 1.2f;       // meters in front of the user
    [SerializeField] private float verticalOffset = -0.1f; // slightly below gaze
    [SerializeField] bool pinToCamera = true;       // keep it fixed to the view

    public static HostController instance { get; private set; }

    [Header("Goodbye UI")]
    [SerializeField] private GameObject thankGivingWindow;

    [Header("Exit")]
    public float exitDelay = 6f;

    static GameObject _localThanksInstance; // prevent duplicates on each client

    void Update()
    {
        // Appuyer sur la touche "Space" (éditeur) pour démarrer la synchronisation
        if (Input.GetKeyDown(KeyCode.Space) && Object != null && Object.HasStateAuthority)
            StartLogSynchornisation();
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority && !AdminTools.IsSimulated(Object))
            instance = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (instance == this) instance = null;
    }

    private static SyncManager LocalSync()
    {
        if (SyncManager.Instance != null) return SyncManager.Instance;
        foreach (var s in FindObjectsOfType<SyncManager>())
            if (s.Object != null && s.Object.IsValid && s.Object.HasStateAuthority) return s;
        return null;
    }

    public void StartLogSynchornisation()
    {
        var sync = LocalSync();
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsRunning || sync == null)
        {
            Debug.LogError("[HostController] Impossible de lancer la synchronisation : session réseau inactive.");
            NetworkStatusPanel.Show("Session réseau inactive : impossible de lancer l'activité.", 5f);
            return;
        }
        sync.StartSynchronizedRecording();
        Debug.LogError("Sent synchronization signal to all clients.");
    }

    public void StoptLogSynchornisation()
    {
        var sync = LocalSync();
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsRunning || sync == null)
        {
            Debug.LogError("[HostController] Session réseau inactive : arrêt local des enregistrements.");
            var marker = FindObjectOfType<MarkerManager>();
            if (marker != null) marker.StopAllRecordings();
            ShowThanksLocally();
            ExitManager.QuitLocallyAfter(exitDelay);
            return;
        }
        sync.StopSynchronizedRecording(false);
        Debug.LogError("Sent synchronization Stop signal to all clients.");
    }

    public void StoptLogSynchornisationTimeout()
    {
        var sync = LocalSync();
        if (sync == null) { StoptLogSynchornisation(); return; }
        sync.StopSynchronizedRecording(true);
        Debug.LogError("Sent synchronization Timeout Stop signal to all clients.");
    }

    /// <summary>Affiche la fenêtre de remerciement devant la caméra de CE casque (instanciation locale, pas de Spawn réseau).</summary>
    public static void ShowThanksLocally()
    {
        HostController source = instance;
        if (source == null)
        {
            foreach (var h in FindObjectsOfType<HostController>()) { source = h; break; }
        }
        if (source == null) { Debug.LogWarning("[HostController] Aucune instance : fenêtre de fin non affichée."); return; }
        source.TryShowThanksgivingLocally(source.distance, source.verticalOffset, source.pinToCamera);
    }

    /// <summary>Texte de la fenêtre de remerciement (recette du 6 novembre 2026 : panneau suivi par la caméra).</summary>
    public const string ThanksMessage = "Merci pour votre participation !";

    void TryShowThanksgivingLocally(float dist, float yOffset, bool pin)
    {
        // Recette du 6 novembre 2026 : la fenêtre prefab instanciée devant la caméra a été vue hors du champ de
        // vision sur casque. Le message passe par le panneau d'information local (NetworkStatusPanel), construit
        // par code, qui suit la tête et affiche ensuite l'état de l'envoi des données sous le remerciement.
        NetworkStatusPanel.Header = ThanksMessage;
        NetworkStatusPanel.Show("", 0f);
        return;
#pragma warning disable CS0162 // ancien affichage conservé pour référence
        if (thankGivingWindow == null) return;
        if (_localThanksInstance != null) return; // Don't create twice on the same client

        var cam = GetPlayerCamera();
        if (cam == null)
        {
            Debug.LogWarning("[HostController] No camera found on this client; skipping window.");
            return;
        }

        Vector3 pos = cam.transform.position + cam.transform.forward.normalized * dist + cam.transform.up * yOffset;
        Quaternion rot = Quaternion.LookRotation((pos - cam.transform.position).normalized, Vector3.up);

        // Instanciation locale (PAS Runner.Spawn) : chaque casque voit la fenêtre devant lui.
        _localThanksInstance = Instantiate(thankGivingWindow, pos, rot);
        foreach (var nb in _localThanksInstance.GetComponentsInChildren<NetworkBehaviour>(true))
            nb.enabled = false; // composants réseau inertes sur une copie locale
        var no = _localThanksInstance.GetComponent<NetworkObject>();
        if (no != null) no.enabled = false;

        if (pin)
            _localThanksInstance.transform.SetParent(cam.transform, true);
#pragma warning restore CS0162
    }

    private static Camera GetPlayerCamera()
    {
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;

        Camera[] cams = GameObject.FindObjectsOfType<Camera>(true);
        foreach (var c in cams)
        {
            if (c.isActiveAndEnabled && c.targetDisplay == 0)
                return c;
        }
        return null;
    }
}
