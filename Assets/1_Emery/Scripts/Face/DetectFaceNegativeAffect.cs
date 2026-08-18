using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DetectFaceNegativeAffect : MonoBehaviour
{
	[SerializeField]
	private float weightThreshold = 0.001f; // Seuil pour considérer une expression comme active

	OVRFaceExpressions.FaceExpression au15_L;
	OVRFaceExpressions.FaceExpression au15_R;
	OVRFaceExpressions.FaceExpression au17_T;
	OVRFaceExpressions.FaceExpression au17_B;

	List<OVRFaceExpressions.FaceExpression> face_negative_affect = new List<OVRFaceExpressions.FaceExpression>();

	bool isExpressionActive = false;

	private OVRFaceExpressions _faceExpressions;

	private void Awake()
	{
		au15_L = OVRFaceExpressions.FaceExpression.LipCornerDepressorL;
		au15_R = OVRFaceExpressions.FaceExpression.LipCornerDepressorR;
		au17_T = OVRFaceExpressions.FaceExpression.ChinRaiserT;
		au17_B = OVRFaceExpressions.FaceExpression.ChinRaiserB;

		face_negative_affect.Add(au15_L);
		face_negative_affect.Add(au15_R);
		face_negative_affect.Add(au17_T);
		face_negative_affect.Add(au17_B);

		if (GetComponent<OVRFaceExpressions>() != null)
			_faceExpressions = GetComponent<OVRFaceExpressions>();
		else if (transform.parent.GetComponent<OVRFaceExpressions>() != null)
			_faceExpressions = transform.parent.GetComponent<OVRFaceExpressions>();
		else
			Debug.LogError("[Emery] OVRFaceExpressions component not found on this GameObject or its parent!");
	}

	// Update is called once per frame
	void Update()
	{
		if (_faceExpressions == null || !_faceExpressions.ValidExpressions)
		{
			Debug.LogWarning("OVRFaceExpressions component is missing or invalid!");
			return;
		}

		for (int i = 0; i < face_negative_affect.Count; i++)
		{
			OVRFaceExpressions.FaceExpression expression = face_negative_affect[i];


			if (expression == OVRFaceExpressions.FaceExpression.Invalid ||
				expression == OVRFaceExpressions.FaceExpression.Max)
			{
				//text.text = "Invalid Expression Detected";
				isExpressionActive = false;
				break;
			}

			float weight = _faceExpressions.GetWeight(expression);
			if (weight > weightThreshold) // Seuil pour considérer l'expression comme active
            {
                isExpressionActive = true;
				continue; // Sortir de la boucle dès qu'une expression est active
			}
			else
			{
				isExpressionActive = false;
				break;
			}
		}
	}

	public bool GetIsExpressionActive()
	{
		return isExpressionActive;
	}
}
