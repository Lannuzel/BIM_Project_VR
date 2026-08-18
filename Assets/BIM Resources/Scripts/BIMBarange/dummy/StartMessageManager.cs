using UnityEngine;
using UnityEngine.UI;
using Fusion;
using TMPro;

public class StartMessageManager : NetworkBehaviour
{
    [Networked] private bool isGameStarting { get; set; } // Synced across all users

    [SerializeField] private TMP_Text statusText; // UI text to display the "Starting" message

    public void OnStartButtonPressed()
    {
        // Check if the current user has authority
        if (Object.HasStateAuthority)
        {
            isGameStarting = true; // Update the networked state
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Update the UI for all users based on the synchronized state
        if (isGameStarting)
        {
            DisplayStartingMessage();
        }
        else
        {
            ClearMessage();
        }
    }

    private void DisplayStartingMessage()
    {
        if (statusText != null)
        {
            statusText.text = "Starting...";
        }
    }

    private void ClearMessage()
    {
        if (statusText != null)
        {
            statusText.text = ""; // Clear the message when not starting
        }
    }
}
