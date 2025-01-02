using Meta.XR.MRUtilityKit;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Calculator : MonoBehaviour
{
    public TMP_Text data;
    public TMP_InputField inputField;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void ChangeText()
    {
        data.text = "Jai Gayatri Mata";
    }
    public void Evaluate()
    {
        string expression = inputField.text.Replace("E+", "*10^")
                                         .Replace("E-", "*10^-");

        List<string> tokens = Tokenizer.Tokenize(expression);
        Parser parser = new(tokens);
        try
        {
            Node node = parser.Parse();
            string result = node.Evaluate().ToString();

            data.text = result;
        }
        catch (Exception)
        {
            data.text = "Invalid syntax";

        }

    }
}
