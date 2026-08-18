using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackFace : MonoBehaviour
{
	[Header("Variables")]
	[Tooltip("Time range to evaluate the affect.")]
	[SerializeField]
	float evaluationTime = 10f;
	[Tooltip("Threshold to consider the user as focused.")]
	[SerializeField]
	float faceThreshold = 0.012f;
	[Tooltip("You can let it empty if Component already on GameObject.")]
	[SerializeField]
	DetectFaceNegativeAffect SC_detectFaceNegativeAffect;
	[SerializeField]
	GameObject feedbackImage;

	private int index = 0;
	private bool hasBeenFocusedInLastElapsedTime = true;
	List<int> lastMinute = new List<int>();
	float timer = 0f;

	// Variables de tests
	//[SerializeField]
	//TextMeshProUGUI text;

	int cpt = 0;
	private void Awake()
	{
		if (SC_detectFaceNegativeAffect == null)
		{
			SC_detectFaceNegativeAffect = GetComponent<DetectFaceNegativeAffect>();
		}
	}

	private float timerFocusInLastElapsedTime = 0f;
	void FixedUpdate()
	{
		if (NetworkManager.Instance.Runner == null 
			|| GroupFaceFeedback.Instance == null 
			/*|| NetworkManager.Instance.Runner.ActivePlayers.Count() < StartActivity.Local.GetMaxPlayer()*/)
			return;

		timer += Time.deltaTime;

		if (timer < evaluationTime)
		{
			lastMinute.Add(SC_detectFaceNegativeAffect.GetIsExpressionActive() ? 1 : 0);
			timerFocusInLastElapsedTime = evaluationTime;
		} 
		else
		{
			lastMinute.Add(SC_detectFaceNegativeAffect.GetIsExpressionActive() ? 1 : 0);
			lastMinute.RemoveAt(0);

			cpt = 0;
			foreach (int f in lastMinute)
			{
				cpt += f;
			}

			float ratio = (float)cpt / lastMinute.Count;
			float t = Mathf.Clamp01(ratio / faceThreshold);
			feedbackImage.GetComponent<Image>().color = new Color(
				feedbackImage.GetComponent<Image>().color.r, 
				feedbackImage.GetComponent<Image>().color.g,
				feedbackImage.GetComponent<Image>().color.b, 
				t);
			DataFeedbacks.Instance.AddFeedbackLog($";;;;;;;;;;;;;{t};");
			float tmpCpt = cpt;
			float tmpLastMinuteCount = lastMinute.Count;
			if (tmpCpt / tmpLastMinuteCount >= faceThreshold)
			{
				hasBeenFocusedInLastElapsedTime = true;
				GroupFaceFeedback.Instance.RPC_SetFocus(index, hasBeenFocusedInLastElapsedTime);
				timerFocusInLastElapsedTime = 0f;
				//feedbackImage.SetActive(true);
			}

			if (hasBeenFocusedInLastElapsedTime)
			{
				timerFocusInLastElapsedTime += Time.deltaTime;
				if (timerFocusInLastElapsedTime >= evaluationTime)
				{
					hasBeenFocusedInLastElapsedTime = false;
					GroupFaceFeedback.Instance.RPC_SetFocus(index, hasBeenFocusedInLastElapsedTime);
					//feedbackImage.SetActive(false);
				}
			}
		}

		if (GroupFaceFeedback.Instance.isFeedbackGroupImageActive)
		{
			DataFeedbacks.Instance.AddFeedbackLog($";;;;;;;;;;;;;;1;");
		}
	}

	public void SetIndex(int i)
	{
		index = i;
	}
}
