using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Events;

public class KeyData : MonoBehaviour
{
    private char token;

    public UnityEvent<char> OnPressed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnButtonClicked(){
        OnPressed?.Invoke(token);
    }
    
}
