using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Import de TextMeshPro

public class FaceExpressionDebugger : MonoBehaviour
{
    public TextMeshProUGUI textPrefab; // Le prefab contenant un TextMeshProUGUI
    public GameObject panel; // Le panel qui contiendra tous les textes
    private Dictionary<string, TextMeshProUGUI> expressionTexts = new Dictionary<string, TextMeshProUGUI>();

    [SerializeField] private OVRFaceExpressions faceExpressions;

    void Start()
    {
        if (faceExpressions == null) // Si pas déjà assigné dans l'inspecteur
        {
            faceExpressions = FindObjectOfType<OVRFaceExpressions>();
        }
        
        if (faceExpressions == null)
        {
            Debug.LogError("OVRFaceExpressions component is missing!");
            enabled = false;
            return;
        }

        if (textPrefab == null)
        {
            Debug.LogError("TextMeshProUGUI prefab is not assigned in the Inspector!");
            enabled = false;
            return;
        }

        if (panel == null)
        {
            Debug.LogError("Panel is not assigned in the Inspector!");
            enabled = false;
            return;
        }

        // Créer une entrée dans le panel pour chaque expression faciale
        foreach (var expression in Enum.GetValues(typeof(OVRFaceExpressions.FaceExpression)))
        {
            if ((OVRFaceExpressions.FaceExpression)expression == OVRFaceExpressions.FaceExpression.Invalid ||
                (OVRFaceExpressions.FaceExpression)expression == OVRFaceExpressions.FaceExpression.Max)
            {
                continue;
            }

            TextMeshProUGUI newText = Instantiate(textPrefab, panel.transform);
            string expressionName = expression.ToString();
            newText.text = $"{expressionName}: 0.00"; // Initialisation de la valeur
            expressionTexts[expressionName] = newText;
        }
    }

    void Update()
    {
        if (faceExpressions.ValidExpressions)
        {
            foreach (var expression in Enum.GetValues(typeof(OVRFaceExpressions.FaceExpression)))
            {
                if ((OVRFaceExpressions.FaceExpression)expression == OVRFaceExpressions.FaceExpression.Invalid ||
                    (OVRFaceExpressions.FaceExpression)expression == OVRFaceExpressions.FaceExpression.Max)
                {
                    continue;
                }

                float weight = faceExpressions.GetWeight((OVRFaceExpressions.FaceExpression)expression);
                string expressionName = expression.ToString();

                if (expressionTexts.ContainsKey(expressionName))
                {
                    expressionTexts[expressionName].text = $"{expressionName}: {weight:F2}";
                }
            }
        }
    }
}
