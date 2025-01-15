using System;
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

    void Start()
    {

        // Sélection du microphone du casque
        SelectHeadsetMicrophone();

        // Combine correctement les chemins
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);

        // Créez le dossier si nécessaire
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Crée le nom du fichier avec timestamp + nom utilisateur
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{timestamp}_Audio.wav";

        // Chemin complet du fichier
        filePath = Path.Combine(folderPath, fileName);

        Debug.Log($"Chemin du fichier audio : {filePath}");

        // Initialisation de l'audio
        audioSource = GetComponent<AudioSource>();
        StartRecording();
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

        
        micDevice = Microphone.devices[0]; // Utilise le premier micro disponible
        if (!string.IsNullOrEmpty(micDevice))
        {
            Debug.Log($"Microphone sélectionné : {micDevice}");
            return;
        }
        Debug.LogError("Aucun microphone de casque trouvé !");
        micDevice = null;
    }

    public void StartRecording()
    {
        if (!string.IsNullOrEmpty(micDevice))
        {
            audioSource.clip = Microphone.Start(micDevice, true, 10, sampleRate);
            while (!(Microphone.GetPosition(micDevice) > 0)) { } // Attendre le démarrage

            audioSource.Play();

            InitWAV();

            isRecording = true;

            Debug.Log($"Enregistrement audio en cours : {filePath}");
        }
        else
        {
            Debug.LogError("Microphone introuvable !");
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

    public void AddAudioMarker()
    {
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
