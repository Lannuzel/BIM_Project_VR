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
        
    }

    // Update is called once per frame
    void Update()
    {
            if (OVRInput.GetDown(OVRInput.Button.Three))
            {
                if(!isActive)
                  {
                    isActive = true;
                    interactionMenu.SetActive(true);
                  }
                  else {
                    isActive = false;
                    interactionMenu.SetActive(false);
                  }
                Debug.Log("X button pressed");
            }
    }

    public void ToggleMeasurement()
    {
      LineScript lineCom = transform.GetComponent<LineScript>();
      if( lineCom.isActiveAndEnabled)
          lineCom.enabled = false;
      else 
          lineCom.enabled = true;
    
    }

}
