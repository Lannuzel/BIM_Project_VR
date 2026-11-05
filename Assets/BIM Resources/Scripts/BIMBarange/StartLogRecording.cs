using UnityEngine;
using Fusion;

public class StartLogRecording : NetworkBehaviour
{
    public GameObject dataLogger;

    private bool isLoggingStarted = false;
    [Networked] private bool isGameStarted { get; set; } // Synced across all players

    public void OnStartButtonPressed()
    {
        if (Object.HasStateAuthority) // Ensure only the authority can change the state
        {
            isGameStarted = true;
        }
    }

    public void OnEndButtonPressed()
    {
        if (Object.HasStateAuthority) // Ensure only the authority can change the state
        {
            isGameStarted = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Check if the object is spawned before accessing networked variables
        if (!Object || !Object.IsValid)
            return;

        // Handle the state of logging based on `isGameStarted`
        if (isGameStarted)
        {
            if (!isLoggingStarted)
            {
                StartLogging(); // Called once when the game starts
                isLoggingStarted = true;
            }
        }
        else
        {
            if (isLoggingStarted)
            {
                StopLogging(); // Called once when the game stops
                isLoggingStarted = false;
            }
        }
    }

    private void StartLogging()
    {
        if (dataLogger != null)
        {
            dataLogger.SetActive(true);
        }
    }

    private void StopLogging()
    {
        if (dataLogger != null)
        {
            dataLogger.SetActive(false);
        }
    }

    public override void Spawned()
    {
        // Ensure the initial state is synced and consistent across all players
        if (Object.HasStateAuthority)
        {
            isGameStarted = false; // Set a default initial state for the game
        }

        // Initialize the logging state based on the synced variable
        UpdateLoggingState();
    }

    private void UpdateLoggingState()
    {
        if (dataLogger != null)
        {
            dataLogger.SetActive(isGameStarted);
        }
    }
}
