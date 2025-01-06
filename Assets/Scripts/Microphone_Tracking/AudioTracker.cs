using System;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioTracker : MonoBehaviour
{
    public static AudioTracker Instance;
    private static string userName = "test";
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

    void Awake() { Instance = this; }
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
        foreach (string device in Microphone.devices)
        {
            if (device.ToLower().Contains("headset") /*|| device.ToLower().Contains("microphone")*/)
            {
                micDevice = device;
                Debug.Log($"Microphone sélectionné : {micDevice}");
                return;
            }
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
            audioSource.PlayOneShot(syncSignal); // Jouer un signal sonore pour marquer le début
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

            binaryWriter.Close();
            memoryStream.Close();

            Debug.Log($"Audio recording stopped for {userName}");
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
        StopRecording();
    }
}
