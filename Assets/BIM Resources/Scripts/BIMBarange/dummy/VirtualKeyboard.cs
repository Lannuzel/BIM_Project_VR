using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class VirtualKeyboard : MonoBehaviour
{
    private TouchScreenKeyboard touchScreenKeyboard;
    // Start is called before the first frame update
    void Start()
    {
        touchScreenKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
            Debug.Log("virutal keyword initialised");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
