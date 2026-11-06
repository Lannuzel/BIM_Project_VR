using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Active les éléments de menu propres au rôle du joueur local.
/// F18 : le rôle peut être réappliqué à chaud (ApplyRole) ; l'expérimentateur dispose d'un panneau d'administration.
/// </summary>
public class ShowPersonalisedMenu : MonoBehaviour
{
    public List<GameObject> lecteurItems;
    public List<GameObject> calculateurItems;
    public List<GameObject> modelisateurItems;
    public List<GameObject> tutorialItems;
    [Tooltip("F18 : elements de menu visibles seulement par l'experimentateur (optionnel).")]
    public List<GameObject> experimentateurItems = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        ApplyRole(NetworkManager.GetLocalPlayerName());
    }

    private static void SetAll(List<GameObject> items, bool active)
    {
        if (items == null) return;
        foreach (GameObject item in items)
            if (item != null) item.SetActive(active);
    }

    /// <summary>
    /// Le bouton « Arreter l'enregistrement » n'appartient qu'au Lecteur (fin de l'activité). La scène XP3,
    /// issue de la branche Network, le listait aussi pour le Modélisateur : il est retiré par code pour tous
    /// les autres rôles, quel que soit le scénario.
    /// </summary>
    private static void HideStopItem(List<GameObject> items)
    {
        if (items == null) return;
        foreach (GameObject item in items)
            if (item != null && item.name == "Arreter l'enregistrement")
                item.SetActive(false);
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
            foreach (GameObject item in lecteurItems)
            {
                if (item == null) continue;
                item.SetActive(true);
                if (item.name == "Arreter l'enregistrement")
                    item.SetActive(false);
            }
        }
        else if (playerName == "Calculateur")
        {
            SetAll(calculateurItems, true);
            HideStopItem(calculateurItems);
        }
        else if (playerName == "Modelisateur" || playerName == "Blue" || playerName == "Red")
        {
            SetAll(modelisateurItems, true);
            HideStopItem(modelisateurItems);
        }
        else if (playerName == "Experimentateur")
        {
            // F18 : fenetre d'administration reservee a l'experimentateur (creee en code)
            SetAll(experimentateurItems, true);
            ExperimenterPanel.Create();
        }
    }
}
