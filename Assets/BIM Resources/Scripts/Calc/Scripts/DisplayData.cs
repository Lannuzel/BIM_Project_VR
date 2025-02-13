using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;

using System.Collections;
using Meta.XR.MRUtilityKit;

public class DisplayData : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private bool inFaultState = false;
    private bool lastOpIsEvaluation = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void GetKeyData(char c)
    {
          

        if (inFaultState)
        {
            Clear();
            inFaultState = false;
        }
        
        if (textMesh.text == "0" || (IsNumberOrBracket(c) && lastOpIsEvaluation))
        {
            textMesh.text = string.Empty;
        }
        lastOpIsEvaluation = false;
        textMesh.text += c;
    }
    private bool IsNumberOrBracket(char c)
    {
        return double.TryParse(c.ToString(), out _) || c == '(' || c == ')';
    }
    // Update is called once per frame
    public void EraseOne()
    {
        if (inFaultState || lastOpIsEvaluation || textMesh.text.Length == 1)
        {
            Clear();
            return;
        }
        Debug.LogError("  Last Data" + textMesh.text + "    "+ textMesh.text.Length);
        textMesh.text = textMesh.text.Remove(textMesh.text.Length - 1);
        Debug.LogError(" Removed Last Data" + textMesh.text + "    "+ textMesh.text.Length);
    }
     private void Clear()
    {
        textMesh.text = "0";
    }

    public void Evaluate()
    {
        string expression = textMesh.text.Replace("E+", "*10^")
                                         .Replace("E-", "*10^-");

        List<string> tokens = Tokenizer.Tokenize(expression);
        Parser parser = new(tokens);
        try
        {
            Node node = parser.Parse();
            string result = node.Evaluate().ToString();

            textMesh.text = result;
        }
        catch (Exception)
        {
            textMesh.text = "Invalid syntax";
            inFaultState = true;
        }

        lastOpIsEvaluation = true;     
    }
    
    
    
    
    void Update()
    {
        
    }
    
}
