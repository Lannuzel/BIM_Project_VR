using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AudioTracker : MonoBehaviour
{
    private string folderName = "Data";
    private string filePath;
    private AudioSource audioSource;
    private bool isRecording = false;
    private int sampleRate = 44100; // Fréquence d'échantillonnage standard
    private MemoryStream memoryStream;
    private BinaryWriter binaryWriter;
    private int lastSamplePosition = 0; // Position précédente dans le clip audio
    private string micDevice;
    public AudioClip syncSignal; // Clip sonore de synchronisation

    [Tooltip("N1 : si vrai, l'enregistrement démarre dès le chargement de la scène (ancien comportement). " +
             "Sinon il démarre au lancement de l'activité (marqueur de synchronisation).")]
    [SerializeField] private bool recordFromSceneStart = false;

    [Tooltip("Délai maximal (s) d'attente du démarrage du micro avant de signaler une erreur.")]
    [SerializeField] private float micStartTimeout = 3f;

    public bool IsRecording => isRecording;

    void Start()
    {
        // Combine correctement les chemins
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);

        // Créez le dossier si nécessaire
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Initialisation de l'audio : la source ne doit jamais rejouer le micro dans le casque.
        audioSource = GetComponent<AudioSource>();
        audioSource.mute = true;
        audioSource.playOnAwake = false;
        audioSource.loop = true;

        // Demande de permission et choix du micro (sans démarrer la capture).
        SelectHeadsetMicrophone();

        if (recordFromSceneStart)
            StartRecording();
        else
            Debug.Log("[AudioTracker] Prêt. L'enregistrement audio démarrera au lancement de l'activité.");
    }

    private void SelectHeadsetMicrophone()
    {
        //TODO : Problème reconaissance micro ????
        // foreach (string device in Microphone.devices)
        // {
        //     Debug.Log($"Device : {device}");
        //     if (device.ToLower().Contains("android") || device.ToLower().Contains("input"))            {
        //         micDevice = device;
        //         Debug.Log($"Microphone sélectionné : {micDevice}");
        //         return;
        //     }
        // }

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);

        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogError("Aucun microphone de casque trouvé !");
            micDevice = null;
            return;
        }

        micDevice = Microphone.devices[0]; // Utilise le premier micro disponible
        Debug.Log($"Microphone sélectionné : {micDevice}");
    }

    private string BuildFilePath()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);
        // Crée le nom du fichier avec timestamp (début d'enregistrement)
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{timestamp}_Audio.wav";
        return Path.Combine(folderPath, fileName);
    }

    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("[AudioTracker] Enregistrement déjà en cours.");
            return;
        }

        if (string.IsNullOrEmpty(micDevice))
            SelectHeadsetMicrophone();

        if (string.IsNullOrEmpty(micDevice))
        {
            Debug.LogError("Microphone introuvable !");
            return;
        }

        filePath = BuildFilePath();
        Debug.Log($"Chemin du fichier audio : {filePath}");

        audioSource.clip = Microphone.Start(micDevice, true, 10, sampleRate);
        audioSource.mute = true;
        lastSamplePosition = 0;

        InitWAV();
        isRecording = true;

        // Attente non bloquante du démarrage effectif du micro (l'ancienne boucle while gelait l'application).
        StartCoroutine(WaitForMicrophoneStart());

        Debug.Log($"Enregistrement audio en cours : {filePath}");
    }

    private IEnumerator WaitForMicrophoneStart()
    {
        float deadline = Time.realtimeSinceStartup + micStartTimeout;
        while (isRecording && Microphone.GetPosition(micDevice) <= 0)
        {
            if (Time.realtimeSinceStartup > deadline)
            {
                Debug.LogError($"[AudioTracker] Le micro '{micDevice}' n'a pas démarré après {micStartTimeout} s.");
                yield break;
            }
            yield return null;
        }
    }

    void Update()
    {
        if (isRecording)
        {
            SaveNewAudioData();
        }
    }

    private void InitWAV()
    {
        memoryStream = new MemoryStream();
        binaryWriter = new BinaryWriter(memoryStream);

        // Écrire un en-tête WAV vide, à remplir plus tard
        binaryWriter.Write(new char[44]); // Réserve 44 octets pour l'en-tête WAV
    }

    private float[] GetSamplesFromAudioClip(AudioClip clip)
    {
        if (clip == null) return null;

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        return samples;
    }

    /// <summary>
    /// Appelé par MarkerManager au lancement de l'activité (signal de synchronisation).
    /// N1 : c'est ici que l'enregistrement audio démarre réellement.
    /// </summary>
    public void AddAudioMarker()
    {
        if (!isRecording)
            StartRecording();

        if (binaryWriter == null)
        {
            Debug.LogError("[AudioTracker] Impossible d'insérer le marqueur : enregistrement non démarré.");
            return;
        }

        if (syncSignal == null)
        {
            Debug.LogError("Le signal de synchronisation (syncSignal) n'est pas assigné !");
            return;
        }

        // Récupérer les échantillons du signal sonore
        float[] markerSamples = GetSamplesFromAudioClip(syncSignal);
        if (markerSamples == null || markerSamples.Length == 0)
        {
            Debug.LogError("Impossible de récupérer les échantillons du signal sonore !");
            return;
        }

        // Convertir les échantillons en données audio et les insérer dans le flux
        foreach (float sample in markerSamples)
        {
            short intData = (short)(sample * short.MaxValue); // Convertir en PCM 16 bits
            binaryWriter.Write(intData);
        }

        Debug.Log("Signal sonore inséré dans le fichier audio.");
    }

    private void SaveNewAudioData()
    {
        if (audioSource.clip == null)
            return;

        int currentPosition = Microphone.GetPosition(micDevice);
        if (currentPosition > 0 && currentPosition != lastSamplePosition)
        {
            int samplesToRead = (currentPosition > lastSamplePosition)
                ? currentPosition - lastSamplePosition
                : audioSource.clip.samples - lastSamplePosition + currentPosition;

            float[] samples = new float[samplesToRead];
            audioSource.clip.GetData(samples, lastSamplePosition);

            foreach (float sample in samples)
            {
                short intData = (short)(sample * short.MaxValue);
                binaryWriter.Write(intData);
            }

            lastSamplePosition = currentPosition; // Mise à jour de la dernière position
        }
    }

    public void StopRecording()
    {
        if (isRecording)
        {
            isRecording = false;

            Microphone.End(micDevice);
            audioSource.Stop();

            WriteWAVHeader();
            SaveToFile();

            binaryWriter?.Close();
            memoryStream?.Close();
            binaryWriter = null;
            memoryStream = null;

            Debug.Log($"Audio recording stopped.");
        }
    }

    private void WriteWAVHeader()
    {
        memoryStream.Seek(0, SeekOrigin.Begin);

        // Calcul de la taille finale et écriture de l'en-tête WAV
        int fileSize = (int)memoryStream.Length;
        binaryWriter.Write("RIFF".ToCharArray());
        binaryWriter.Write(fileSize - 8);
        binaryWriter.Write("WAVE".ToCharArray());
        binaryWriter.Write("fmt ".ToCharArray());
        binaryWriter.Write(16); // Subchunk1Size (PCM)
        binaryWriter.Write((short)1); // AudioFormat (PCM = 1)
        binaryWriter.Write((short)1); // NumChannels
        binaryWriter.Write(sampleRate); // SampleRate
        binaryWriter.Write(sampleRate * 2); // ByteRate
        binaryWriter.Write((short)2); // BlockAlign
        binaryWriter.Write((short)16); // BitsPerSample
        binaryWriter.Write("data".ToCharArray());
        binaryWriter.Write(fileSize - 44); // Subchunk2Size
    }

    private void SaveToFile()
    {
        File.WriteAllBytes(filePath, memoryStream.ToArray());
    }

    private void OnDestroy()
    {
        Debug.Log("Application quittée sur OnDestroy. Arrêt de l'enregistremen Audio.");
        StopRecording();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Application quittée. Arrêt de l'enregistrement audio.");
        StopRecording();
    }

    // private void OnApplicationPause(bool isPaused)
    // {
    //     if (isPaused)
    //     {
    //         Debug.Log("Application quittée sur OnApplicationPause. Arrêt de l'enregistremen Audio.");
    //         StopRecording(); // Assure que l'enregistrement est arrêté proprement
    //     }
    // }

}
