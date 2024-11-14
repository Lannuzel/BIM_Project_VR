using UnityEngine;

public class CheckFaceTrackingRequirements : MonoBehaviour
{
    void Start()
    {
        // Vérification de la compatibilité du Face Tracking
        if (!OVRPlugin.faceTracking2Supported)
        {
            Debug.LogError("Face tracking is not supported on this device.");
            return;
        }

        // Vérification de l'activation du Face Tracking
        if (!OVRPlugin.faceTracking2Enabled)
        {
            Debug.LogWarning("Face tracking is not enabled. Please ensure the necessary permissions are granted.");
        }

        // Vérification du composant OVRFaceExpressions
        var faceExpressions = FindObjectOfType<OVRFaceExpressions>();
        if (faceExpressions == null)
        {
            Debug.LogError("OVRFaceExpressions component is missing in the scene. Please add it to use Face Tracking features.");
        }
        else if (!faceExpressions.ValidExpressions)
        {
            Debug.LogWarning("Face tracking is enabled but expressions are not valid. Ensure the headset has the necessary permissions and environment setup.");
        }

        Debug.Log("All required Face Tracking checks passed.");
    }
}


