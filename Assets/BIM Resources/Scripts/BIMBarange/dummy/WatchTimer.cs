using UnityEngine;
using TMPro;
using Fusion;

public class WatchTimer : NetworkBehaviour
{
    public TMP_Text timerText;       // Assign a UI Text object in the Inspector
    private float elapsedTime = 0f;
    private bool isRunning = true;
    public float maxTime = 25f * 60f; // 25 minutes in seconds

    private void Start()
    {

           timerText = gameObject.GetComponentInChildren<TMP_Text>();
    }
    void Update()
    {
        if (!isRunning) return;

        // Increase elapsed time
        elapsedTime += Time.deltaTime;

        // Stop at 25 minutes
        if (elapsedTime >= maxTime)
        {
            elapsedTime = maxTime;
            isRunning = false;
        }

        // Convert to hours:minutes:seconds
        int hours = Mathf.FloorToInt(elapsedTime / 3600f);
        int minutes = Mathf.FloorToInt((elapsedTime % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        // Format into 00:00:00
        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }
}
