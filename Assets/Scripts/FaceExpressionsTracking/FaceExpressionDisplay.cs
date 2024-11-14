using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FaceExpressionDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textPrefab;
    [SerializeField] private Transform contentPanel;
    [SerializeField] private OVRFaceExpressions faceExpressions;

    private Dictionary<string, TextMeshProUGUI> expressionTexts = new Dictionary<string, TextMeshProUGUI>();

    void Start()
    {
        if (faceExpressions == null)
        {
            faceExpressions = FindObjectOfType<OVRFaceExpressions>();
        }

        if (faceExpressions == null)
        {
            Debug.LogError("OVRFaceExpressions component is missing!");
            enabled = false;
            return;
        }

        if (textPrefab == null || contentPanel == null)
        {
            Debug.LogError("UI elements are not properly assigned in the Inspector!");
            enabled = false;
            return;
        }

        foreach (var expression in Enum.GetValues(typeof(OVRFaceExpressions.FaceExpression)))
        {
            if ((OVRFaceExpressions.FaceExpression)expression == OVRFaceExpressions.FaceExpression.Invalid ||
                (OVRFaceExpressions.FaceExpression)expression == OVRFaceExpressions.FaceExpression.Max)
            {
                continue;
            }

            TextMeshProUGUI newText = Instantiate(textPrefab, contentPanel);
            string expressionName = expression.ToString();
            newText.text = $"{expressionName}: 0.00";
            newText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 20); // Limiter la hauteur
            expressionTexts[expressionName] = newText;
        }

        Canvas.ForceUpdateCanvases(); // Mise à jour des layouts
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

                string expressionName = expression.ToString();
                if (expressionTexts.ContainsKey(expressionName))
                {
                    var textComponent = expressionTexts[expressionName];

                    if (textComponent != null)
                    {
                        float weight = faceExpressions.GetWeight((OVRFaceExpressions.FaceExpression)expression);
                        textComponent.text = $"{expressionName}: {weight:F2}";
                    }
                }
            }
        }
    }
}
