using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using  TMPro; 

public class ButtonMenuHandler : MonoBehaviour
{
    public TMP_Text _tmPro;  
    private int count = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void onClicked(){
      
        _tmPro.text  = "Clicked " + count++;
          Debug.Log(" Button Pressed " + count);
    }
    
}
