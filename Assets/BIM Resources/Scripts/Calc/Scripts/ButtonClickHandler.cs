using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ButtonClickHandler : MonoBehaviour
{
    public DisplayData displayScript;  // Reference to the Display script
   
    public TMP_Text characterToDisplay;

    void Start()
    {
        // Ensure that the button is clickable
        if(characterToDisplay.text == "="){
            transform.GetComponent<Button>().onClick.AddListener(() => displayScript.Evaluate());
        }
        else if( characterToDisplay.text == "<--")
        {
            transform.GetComponent<Button>().onClick.AddListener(() => displayScript.EraseOne());
        }
        else
               transform.GetComponent<Button>().onClick.AddListener(() => displayScript.GetKeyData(characterToDisplay.text[0]));
    }
}
