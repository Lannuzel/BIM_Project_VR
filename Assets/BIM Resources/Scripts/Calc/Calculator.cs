using Meta.XR.MRUtilityKit;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Calculator : MonoBehaviour
{
    public TMP_Text outputText;
    public TMP_InputField inputField;

    // Start is called before the first frame update

    public void Evaluate()
    {
        string expression = inputField.text.Replace("E+", "*10^")
                                         .Replace("E-", "*10^-");

        List<string> tokens = Tokenizer.Tokenize(expression);
        Parser parser = new(tokens);
        try
        {
            Node node = parser.Parse();
            string resultString = node.Evaluate().ToString();

            outputText.text = resultString;
        }
        catch (Exception)
        {
            outputText.text = "Invalid syntax";

        }

    }
}
