using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextToFloatTest : MonoBehaviour
{
    // Reference to the InputField in the UI
    // Reference to the TMP_InputField in the UI
    public TMP_InputField inputField;

    // Method to process the input and print the double
    public void ProcessInput()
    {
        // Check if the input field is not empty
        if (!string.IsNullOrEmpty(inputField.text))
        {
            // Try to parse the input text to a number
            if (float.TryParse(inputField.text, out float number))
            {
                // Calculate the double of the number
                float doubledValue = number * 2;

                // Print the doubled value to the console
                Debug.Log("Doubled Value: " + doubledValue);
            }
            else
            {
                // If parsing fails, print an error message
                Debug.LogError("Invalid input. Please enter a valid number.");
            }
        }
        else
        {
            // Handle empty input
            Debug.LogError("Input field is empty. Please enter a value.");
        }
    }
}