using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuOptionControl : MonoBehaviour
{
    public List<Transform> options = new List<Transform>();

    [Tooltip("F12 : options pouvant rester ouvertes en meme temps (bloc-notes et calculatrice).")]
    public List<string> sharedOptionNames = new List<string> { "NotePad", "CalculatorRV", "Calculator" };
    
    // Start is called before the first frame update
    void Start()
    {
        
    }


    public void DisactivateAllOptions()
    {
        if (options.Count > 0)
        {
            foreach (Transform t in options)
            {
                t.gameObject.SetActive(false);
            }
        }
    }
    public void ActivateOption(Transform option)
    {
        DisactivateAllOptions();

        option.gameObject.SetActive(true);  // always activate menu item
       

    }
    public void ToggleActivation(Transform option)
    {
        if (option.gameObject.activeSelf)
        {
            DisactivateAllOptions(option);
            option.gameObject.SetActive(false);
        }
        else
        {
            DisactivateAllOptions(option);
            option.gameObject.SetActive(true);
        }

    }

    /// <summary>
    /// F12 : désactive toutes les options sauf celles qui peuvent rester ouvertes en même temps
    /// que <paramref name="keep"/> (bloc-notes et calculatrice).
    /// </summary>
    private void DisactivateAllOptions(Transform keep)
    {
        bool keepIsShared = keep != null && sharedOptionNames.Contains(keep.name);
        foreach (Transform t in options)
        {
            if (t == null) continue;
            if (keepIsShared && sharedOptionNames.Contains(t.name))
                continue; // bloc-notes + calculatrice : affichage simultané autorisé
            t.gameObject.SetActive(false);
        }
    }

}
