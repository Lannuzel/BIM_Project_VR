using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.XR;

public class InteractionTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
         // Switch ui display types
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                Debug.Log("A button pressed");
            }

            // Trigger loading simulator via keyboard
            if (OVRInput.GetDown(OVRInput.Button.Two))
            {
               Debug.Log("B button pressed");
            }
                     // Switch ui display types
            if (OVRInput.GetDown(OVRInput.Button.Three))
            {
                Debug.Log("X button pressed");
            }

            // Trigger loading simulator via keyboard
            if (OVRInput.GetDown(OVRInput.Button.Four))
            {
               Debug.Log("Y button pressed");
            }         // Switch ui display types
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
            {
                Debug.Log("Left Stick Pressed");
            }
        if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
            {
                Debug.Log("Right Stick Pressed");
            }

            if(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger)>0)
            {
                Debug.Log("Left trigger Pressed Continue");
            }
             if(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.Touch)>0)
            {
                Debug.Log("Right trigger Pressed");
            }
            if(OVRInput.Get(OVRInput.Touch.SecondaryThumbstick))
            {
                Debug.Log("SecondaryThumbstick pressed");

            }

            
    }
}
