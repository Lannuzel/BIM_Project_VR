using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyBoardManager : MonoBehaviour
{   public GameObject keyboard;
    public Transform leftMallet;
    public Transform rightMallet;
    public Transform rightHandController;
    public Transform leftHandController;
    bool isActivated = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleKeyboardActivation()
    { 
        if(!isActivated){
            keyboard.SetActive(true);
            isActivated = true;
            rightMallet.position  = rightHandController.position;
            rightMallet.rotation = rightHandController.rotation;
            rightMallet.SetParent(rightHandController);
            leftMallet.position = leftHandController.position;
            leftMallet.rotation = leftHandController.rotation;
            leftMallet.SetParent(leftHandController);
            isActivated = true;
        }
        else{
            keyboard.SetActive(false);
            rightMallet.gameObject.SetActive(false);
            leftMallet.gameObject.SetActive(false) ;
            isActivated = false;
        
        }      
    }
}
