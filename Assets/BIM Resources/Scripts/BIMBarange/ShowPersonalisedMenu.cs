using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Active les éléments de menu propres au rôle du joueur local.
/// F18 : le rôle peut être réappliqué à chaud (ApplyRole) ; l'expérimentateur dispose d'un panneau d'administration.
/// Recette du 6 novembre 2026 : le bouton « Arreter l'enregistrement » n'appartient qu'au rôle
/// <see cref="SessionConfig.stopActivityRole"/> (Lecteur) et n'apparaît qu'une fois l'activité lancée, quel que
/// soit le casque qui l'a lancée (il n'était auparavant activé que par le clic local sur « Lancer l'activité »).
/// </summary>
public class ShowPersonalisedMenu : MonoBehaviour
{
    public const string StopItemName = "Arreter l'enregistrement";

    public List<GameObject> lecteurItems;
    public List<GameObject> calculateurItems;
    public List<GameObject> modelisateurItems;
    public List<GameObject> tutorialItems;
    [Tooltip("F18 : elements de menu visibles seulement par l'experimentateur (optionnel).")]
    public List<GameObject> experimentateurItems = new List<GameObject>();

    private static readonly List<ShowPersonalisedMenu> instances = new List<ShowPersonalisedMenu>();

    // Start is called before the first frame update
    void Start()
    {
        ApplyRole(NetworkManager.GetLocalPlayerName());
    }

    private void OnEnable()
    {
        instances.Add(this);
        SyncManager.RecordingStartedEvent += OnRecordingStarted;
        SyncManager.RecordingStoppedEvent += OnRecordingStopped;
    }

    private void OnDisable()
    {
        instances.Remove(this);
        SyncManager.RecordingStartedEvent -= OnRecordingStarted;
        SyncManager.RecordingStoppedEvent -= OnRecordingStopped;
    }

    private void OnRecordingStarted() => ApplyStopItemVisibility(true);

    private void OnRecordingStopped(bool timeout) => ApplyStopItemVisibility(false);

    private static void SetAll(List<GameObject> items, bool active)
    {
        if (items == null) return;
        foreach (GameObject item in items)
            if (item != null) item.SetActive(active);
    }

    private IEnumerable<GameObject> AllItems()
    {
        foreach (var list in new[] { lecteurItems, calculateurItems, modelisateurItems, experimentateurItems })
        {
            if (list == null) continue;
            foreach (var item in list)
                if (item != null) yield return item;
        }
    }

    /// <summary>Vrai si le rôle local est celui qui arrête l'activité.</summary>
    public static bool LocalRoleStops()
    {
        string stopRole = SessionConfig.Instance.stopActivityRole;
        if (string.IsNullOrWhiteSpace(stopRole)) stopRole = AdminTools.Lecteur;
        return NetworkManager.GetLocalPlayerName() == stopRole.Trim();
    }

    /// <summary>
    /// Montre le bouton d'arrêt si l'activité est lancée et que le rôle local est le rôle d'arrêt ; le cache sinon.
    /// </summary>
    public void ApplyStopItemVisibility(bool recordingStarted)
    {
        bool show = recordingStarted && LocalRoleStops();
        foreach (var item in AllItems())
            if (item.name == StopItemName)
                item.SetActive(show);
    }

    /// <summary>Réapplique la visibilité du bouton d'arrêt sur tous les menus (appelé après un lancement).</summary>
    public static void RefreshStopItems(bool recordingStarted)
    {
        foreach (var m in instances)
            if (m != null) m.ApplyStopItemVisibility(recordingStarted);
    }

    /// <summary>Désactive les éléments de tous les rôles puis active ceux du rôle donné.</summary>
    public void ApplyRole(string playerName)
    {
        SetAll(lecteurItems, false);
        SetAll(calculateurItems, false);
        SetAll(modelisateurItems, false);
        SetAll(experimentateurItems, false);

        if (playerName == "Lecteur")
        {
            SetAll(lecteurItems, true);
        }
        else if (playerName == "Calculateur")
        {
            SetAll(calculateurItems, true);
        }
        else if (playerName == "Modelisateur" || playerName == "Blue" || playerName == "Red")
        {
            SetAll(modelisateurItems, true);
        }
        else if (playerName == "Experimentateur")
        {
            // F18 : fenetre d'administration reservee a l'experimentateur (creee en code)
            SetAll(experimentateurItems, true);
            ExperimenterPanel.Create();
        }

        // Le bouton d'arrêt ne dépend pas de la liste : rôle d'arrêt et activité lancée uniquement.
        bool started = SyncManager.Instance != null && SyncManager.Instance.IsRecordingStarted;
        ApplyStopItemVisibility(started);
    }
}
