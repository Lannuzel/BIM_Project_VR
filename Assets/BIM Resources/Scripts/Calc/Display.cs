using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Display : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private bool inFaultState = false;
    private bool lastOpIsEvaluation = false;

    private void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Type(char c)
    {
        lastOpIsEvaluation = false;

        if (inFaultState)
        {
            Clear();
            inFaultState = false;
        }

        if (textMesh.text == "0" && IsNumberOrBracket(c))
        {
            textMesh.text = string.Empty;
        }

        textMesh.text += c;
    }
      public void Test(){

      }
      public void Test1(char c){

      }
       public void GetData(char c)
    {
        lastOpIsEvaluation = false;

        if (inFaultState)
        {
            Clear();
            inFaultState = false;
        }

        if (textMesh.text == "0" && IsNumberOrBracket(c))
        {
            textMesh.text = string.Empty;
        }

        textMesh.text += c;
    }

    public void EraseOne()
    {
        if (inFaultState || lastOpIsEvaluation || textMesh.text.Length == 1)
        {
            if (lastOpIsEvaluation) { Debug.LogError(" Last op is EvaluationOp"); }
            else if (inFaultState) { Debug.LogError(" Last op is fault state"); }
            else if (textMesh.text.Length==1 ) { Debug.LogError(" string lenght is 1 "); }


            Clear();
            Debug.LogError(" Removed Last Data");
            return;

        }

      //  textMesh.text = textMesh.text.Remove(textMesh.text.Length - 1);
  

        Debug.LogError("  Last Data    " + textMesh.text + "  lenth   " + textMesh.text.Length);
        textMesh.text = textMesh.text.Remove(textMesh.text.Length - 1);
        Debug.LogError(" Removed Last Data  " + textMesh.text + "  Length    " + textMesh.text.Length);
    }

    private void Clear()
    {
        Debug.LogError("  Indide Clear() Last Data    " + textMesh.text + "  lenth   " + textMesh.text.Length + " setting it to 0 " );
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
            string result = node.Evaluate().ToString("F3");

            textMesh.text = result;
        }
        catch (Exception)
        {
            textMesh.text = "Invalid syntax";
            inFaultState = true;
        }

        lastOpIsEvaluation = true;     
    }

    private bool IsNumberOrBracket(char c)
    {
        return double.TryParse(c.ToString(), out _) || c == '(' || c == ')';
    }
}
