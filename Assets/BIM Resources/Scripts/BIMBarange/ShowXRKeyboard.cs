using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShowXRKeyboard : MonoBehaviour
{
     private TMP_InputField inputField;
    // Start is called before the first frame update
    void Start()
    {
        inputField = transform.GetComponent<TMP_InputField>();
      //  inputField.onSelect.AddListener(x => OpenKeyboard());
    }

    public void SetCaretColorAlpha(float value)
    {
        inputField.customCaretColor = true;
        Color caretColor = inputField.caretColor;
        caretColor.a = value;
        inputField.caretColor = caretColor;
    }

    public void AddNewLine()
    {
        inputField = transform.GetComponent<TMP_InputField>();
        if (inputField.text != "")
        {
            inputField.text = inputField.text + "\n";
        }

    } 
    // Update is called once per frame
    void Update()
    {
        
    }
}
