using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayUIPosition : MonoBehaviour
{
    public GameObject uiCanvas;       // Reference to the Canvas containing your UI
    public Transform cameraRef;    // Reference to the VR headset or camera
    public float distance = 2.0f; // Distance of the UI from the user
    public Vector3 offset = Vector3.zero;
    // Start is called before the first frame update
   
    void OnEnable()
    {
        ShowUIAtPosition();
    }
 /*   private void Update()
    {
        ShowUIAtPosition(); 
    }
   */
    void ShowUIAtPosition()
    {
        if (uiCanvas != null && cameraRef != null)
        {
            // Position the UI in front of the user
            Vector3 targetPosition = cameraRef.position + cameraRef.forward * distance;
            uiCanvas.transform.position = targetPosition + offset;
            transform.LookAt(transform.position + cameraRef.transform.forward);

            // Rotate the UI to face the user
           // uiCanvas.transform.rotation = Quaternion.LookRotation(uiCanvas.transform.position - cameraRef.position);
        }
    }
}
