using System;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioTracker : MonoBehaviour
{
    [SerializeField] private string userName = "User_test"; // Nom unique de l'utilisateur
    private AudioSource audioSource;
    private string filePath;
    private bool isRecording = false;
    private int sampleRate = 44100; // Fréquence d'échantillonnage standard

    private MemoryStream memoryStream;
    private BinaryWriter binaryWriter;

    private int lastSamplePosition = 0; // Position précédente dans le clip audio
    private string micDevice;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        filePath =  $"./Data/{userName}_Audio.wav";
        StartRecording();
    }

    public void StartRecording()
    {
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0]; // Utilise le premier micro disponible
            audioSource.clip = Microphone.Start(micDevice, true, 1, sampleRate);
            while (!(Microphone.GetPosition(micDevice) > 0)) { } // Attente du démarrage du micro
            audioSource.Play();

            InitWAV();
            isRecording = true;

            Debug.Log($"Recording audio for {userName}. File saved at {filePath}");
        }
        else
        {
            Debug.LogError("No microphone detected!");
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
