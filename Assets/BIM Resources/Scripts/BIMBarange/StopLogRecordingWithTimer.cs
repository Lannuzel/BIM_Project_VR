using UnityEngine;
using System.Collections;
using Fusion;

public class StopLogRecordingWithTimer : NetworkBehaviour
{

    private bool timerStarted = false;
    
    public HostController hostController;
    public float thresholdTimer = 1800f;
    public void ActivateStopLogRecordTimeout()
    {
        if (!timerStarted)
        {
            StartCoroutine(ActivateWellcomeAfterDelay(thresholdTimer)); // Start the timer
            timerStarted = true;
        }
    }

    IEnumerator ActivateWellcomeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.LogError("Stop Log Recording TimeOut");

        GameObject clockObject = GameObject.Find("CountDownWatch");
        if (clockObject != null)
        {
            clockObject.SetActive(false);
            CountDownWatch watch = clockObject.GetComponent<CountDownWatch>();
            watch.DeleteClock(); 
        }


        hostController.StoptLogSynchornisationTimeout();
    }
}

