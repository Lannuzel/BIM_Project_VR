using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

[RequireComponent(typeof(OVRFaceExpressions))]
public class CheckCapteurs : MonoBehaviour
{
    [SerializeField]
    private GameObject imageFeedbackVoice;
    [SerializeField]
    private GameObject imageFeedbackFace; 

    [SerializeField]
    [Tooltip("Référence au composant OVRFaceExpressions (ex: attaché au casque VR)")]
    private OVRFaceExpressions faceExpressions;

    [SerializeField]
    [Tooltip("Le seuil de volume sonore à partir duquel on considère que le joueur parle.")]
    private float voiceThreshold = 0.02f;

    private AudioClip micClip;
    private string micDeviceName;
    private bool isMicInitialized = false;
    private int sampleWindow = 256;

    private void Start()
    {
        // Demande la permission d'accéder au microphone (requis sur Android/Quest)
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }

        faceExpressions = GetComponent<OVRFaceExpressions>();
    }

    private void Update()
    {
        InitializeMicrophoneIfNeeded();
        CheckMicrophoneStatus();
        CheckFaceExpressions();
    }

    private void InitializeMicrophoneIfNeeded()
    {
        // On attend que la permission soit accordée et on initialise le micro
        if (!isMicInitialized && Permission.HasUserAuthorizedPermission(Permission.Microphone) && Microphone.devices.Length > 0)
        {
            micDeviceName = Microphone.devices[0];
            // Enregistrement en boucle de 1 seconde à la fréquence standard
            micClip = Microphone.Start(micDeviceName, true, 1, 44100);
            isMicInitialized = true;
        }
    }

    private void CheckMicrophoneStatus()
    {
        bool isTalking = false;

        if (isMicInitialized && Microphone.IsRecording(micDeviceName))
        {
            float currentVolume = GetMicrophoneVolume();
            isTalking = currentVolume > voiceThreshold;
        }

        if (imageFeedbackVoice != null)
        {
            imageFeedbackVoice.SetActive(isTalking);
        }
    }

    private float GetMicrophoneVolume()
    {
        // Position actuelle de l'enregistrement du micro
        int micPosition = Microphone.GetPosition(micDeviceName) - sampleWindow + 1;
        
        if (micPosition < 0) 
            return 0;

        float[] waveData = new float[sampleWindow];
        micClip.GetData(waveData, micPosition);

        // Trouve le pic de volume actuel (Peak)
        float levelMax = 0;
        for (int i = 0; i < sampleWindow; i++)
        {
            float wavePeak = Mathf.Abs(waveData[i]);
            if (wavePeak > levelMax)
            {
                levelMax = wavePeak;
            }
        }
        
        return levelMax;
    }

    private void CheckFaceExpressions()
    {
        // Vérifie si l'objet OVRFaceExpressions est assigné, si le tracking est activé et si les expressions captées sont valides.
        bool isFaceTrackingValid = faceExpressions != null && 
                                   faceExpressions.FaceTrackingEnabled && 
                                   faceExpressions.ValidExpressions;

        if (imageFeedbackFace != null)
        {
            imageFeedbackFace.SetActive(isFaceTrackingValid);
        }
    }
}
