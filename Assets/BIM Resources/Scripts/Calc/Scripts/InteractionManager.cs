using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.XR;

public class InteractionManager : MonoBehaviour
{
    public GameObject interactionMenu;
    
    private bool isActive = false;
    // Start is called before the first frame update
    void Start()
    {
        Debug.LogError(" InteractionManager ********************"+ transform.name);
        
    }



    public void ToggleMeasurement()
    {

    
    }

}
