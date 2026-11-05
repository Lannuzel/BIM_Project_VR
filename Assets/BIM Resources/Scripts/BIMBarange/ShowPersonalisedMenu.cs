using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        string playerName = NetworkManager.GetLocalPlayerName();

        if (playerName == "Lecteur")
        {
            foreach (GameObject item in lecteurItems)
            {

                item.SetActive(true);
                if (item.name == "Arreter l'enregistrement")
                    item.SetActive(false);
                
            }
        }
        else if (playerName == "Calculateur")
        {
            foreach (GameObject item in calculateurItems)
            {
                item.SetActive(true);
            }
        }
        else if (playerName == "Modelisateur")
        {
            foreach (GameObject item in modelisateurItems)
            {
                item.SetActive(true);
            }
        }
        else if (playerName == "Experimentateur")
        {
            // F18 : fenetre d'administration reservee a l'experimentateur (creee en code)
            foreach (GameObject item in experimentateurItems)
            {
                item.SetActive(true);
            }
            ExperimenterPanel.Create();
        }
        else if(playerName == "Blue")
        {
            foreach (GameObject item in modelisateurItems)
            {
                item.SetActive(true);
            }

        }
        else if (playerName == "Red")
        {
            foreach (GameObject item in modelisateurItems)
            {
                item.SetActive(true);
            }
        }

    }


}
