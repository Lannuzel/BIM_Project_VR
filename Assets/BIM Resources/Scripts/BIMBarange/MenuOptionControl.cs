using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuOptionControl : MonoBehaviour
{
    public List<Transform> options = new List<Transform>();
    
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
            DisactivateAllOptions();
            option.gameObject.SetActive(false);
        }
        else
        {
            DisactivateAllOptions();
            option.gameObject.SetActive(true);
        }

    }

}
