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
    private FileStream fileStream;     // N2 : écriture en flux sur disque (plus de tampon mémoire de 30 min)
    private BinaryWriter binaryWriter;
    private int lastSamplePosition = 0; // Position précédente dans le clip audio
    private string micDevice;
    public AudioClip syncSignal; // Clip sonore de synchronisation

    [Tooltip("N1 : si vrai, l'enregistrement démarre dès le chargement de la scène (ancien comportement). " +
             "Sinon il démarre au lancement de l'activité (marqueur de synchronisation).")]
    [SerializeField] private bool recordFromSceneStart = false;

    [Tooltip("Délai maximal (s) d'attente du démarrage du micro avant de signaler une erreur.")]
    [SerializeField] private float micStartTimeout = 3f;

    [Tooltip("N2 : intervalle (s) entre deux écritures effectives sur disque (flush).")]
    [SerializeField] private float flushInterval = 2f;

    private float nextFlushTime;
    private float[] sampleBuffer = new float[0];

    public bool IsRecording => isRecording;

    /// <summary>Chemin du fichier WAV en cours (vide tant que l'enregistrement n'a pas démarré).</summary>
    public string FilePath => filePath;

    /// <summary>Durée (s) d'audio déjà écrite sur disque.</summary>
    public float RecordedSeconds => fileStream != null ? Mathf.Max(0f, (fileStream.Length - 44) / (float)(sampleRate * 2)) : 0f;

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

        // Recette du 6 novembre 2026 : l'audio démarre dès la scène, comme les autres capteurs ; le bip de
        // synchronisation inséré au marqueur (AddAudioMarker) aligne le fichier sur les CSV.
        if (recordFromSceneStart || SessionConfig.Instance.audioFromSceneStart)
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

        if (!InitWAV())
            return;

        audioSource.clip = Microphone.Start(micDevice, true, 10, sampleRate);
        audioSource.mute = true;
        lastSamplePosition = 0;
        nextFlushTime = Time.realtimeSinceStartup + flushInterval;
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

            if (Time.realtimeSinceStartup >= nextFlushTime)
            {
                nextFlushTime = Time.realtimeSinceStartup + flushInterval;
                Flush();
            }
        }
    }

    private bool InitWAV()
    {
        try
        {
            fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            binaryWriter = new BinaryWriter(fileStream);

            // Écrire un en-tête WAV vide, à remplir lors de l'arrêt (ou à chaque flush pour rester lisible).
            binaryWriter.Write(new byte[44]); // Réserve 44 octets pour l'en-tête WAV
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AudioTracker] Impossible de créer le fichier audio {filePath} : {e.Message}");
            binaryWriter = null;
            fileStream = null;
            return false;
        }
    }

    /// <summary>Bip de synchronisation généré (sinusoïde avec 10 ms de fondu), à la fréquence d'échantillonnage de l'enregistrement.</summary>
    private float[] GenerateSyncTone(float frequency, float seconds)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(sampleRate * seconds));
        float fade = sampleRate * 0.01f;
        float[] samples = new float[n];
        for (int i = 0; i < n; i++)
        {
            float env = Mathf.Min(1f, i / fade, (n - i) / fade);
            samples[i] = 0.8f * env * Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate);
        }
        return samples;
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

        // Récupérer les échantillons du signal sonore ; à défaut de clip assigné (le fichier bip.wav
        // n'avait jamais été versionné), un bip de 1 kHz de 0,5 s est généré pour que le marqueur existe toujours.
        float[] markerSamples = syncSignal != null ? GetSamplesFromAudioClip(syncSignal) : GenerateSyncTone(1000f, 0.5f);
        if (syncSignal == null)
            Debug.LogWarning("[AudioTracker] syncSignal non assigné : bip de synchronisation généré (1 kHz, 0,5 s).");
        if (markerSamples == null || markerSamples.Length == 0)
        {
            Debug.LogError("Impossible de récupérer les échantillons du signal sonore !");
            return;
        }

        // Convertir les échantillons en données audio et les insérer dans le flux
        WriteSamples(markerSamples, markerSamples.Length);
        Flush();

        // Bip audible dans le casque au lancement : les participants entendent le départ, et le bip capté par
        // le micro double le marqueur inséré (repère physique commun si les casques sont dans la même pièce).
        if (SessionConfig.Instance.playSyncBeep)
        {
            AudioClip beep = syncSignal;
            if (beep == null)
            {
                beep = AudioClip.Create("SyncBeep", markerSamples.Length, 1, sampleRate, false);
                beep.SetData(markerSamples, 0);
            }
            Vector3 at = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(beep, at, 0.7f);
        }

        Debug.Log("Signal sonore inséré dans le fichier audio.");
    }

    private void SaveNewAudioData()
    {
        if (audioSource.clip == null || binaryWriter == null)
            return;

        int currentPosition = Microphone.GetPosition(micDevice);
        if (currentPosition > 0 && currentPosition != lastSamplePosition)
        {
            int samplesToRead = (currentPosition > lastSamplePosition)
                ? currentPosition - lastSamplePosition
                : audioSource.clip.samples - lastSamplePosition + currentPosition;

            if (sampleBuffer.Length < samplesToRead)
                sampleBuffer = new float[samplesToRead];

            if (currentPosition > lastSamplePosition)
            {
                audioSource.clip.GetData(sampleBuffer, lastSamplePosition);
                WriteSamples(sampleBuffer, samplesToRead);
            }
            else
            {
                // Le clip a bouclé : fin du tampon puis début.
                int tail = audioSource.clip.samples - lastSamplePosition;
                audioSource.clip.GetData(sampleBuffer, lastSamplePosition);
                WriteSamples(sampleBuffer, tail);
                audioSource.clip.GetData(sampleBuffer, 0);
                WriteSamples(sampleBuffer, currentPosition);
            }

            lastSamplePosition = currentPosition; // Mise à jour de la dernière position
        }
    }

    private void WriteSamples(float[] samples, int count)
    {
        for (int i = 0; i < count; i++)
        {
            short intData = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
            binaryWriter.Write(intData);
        }
    }

    /// <summary>Écrit l'en-tête courant et vide les tampons : le fichier reste lisible même en cas de crash.</summary>
    private void Flush()
    {
        if (binaryWriter == null || fileStream == null)
            return;

        try
        {
            long end = fileStream.Position;
            WriteWAVHeader();
            fileStream.Position = end;
            binaryWriter.Flush();
            fileStream.Flush(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AudioTracker] Erreur d'écriture audio : {e.Message}");
        }
    }

    public void StopRecording()
    {
        if (isRecording)
        {
            isRecording = false;

            SaveNewAudioData();
            Microphone.End(micDevice);
            audioSource.Stop();

            WriteWAVHeader();

            binaryWriter?.Flush();
            binaryWriter?.Close();
            fileStream?.Close();
            binaryWriter = null;
            fileStream = null;

            Debug.Log($"Audio recording stopped.");
        }
    }

    private void WriteWAVHeader()
    {
        if (binaryWriter == null || fileStream == null)
            return;

        // Calcul de la taille finale et écriture de l'en-tête WAV
        int fileSize = (int)fileStream.Length;
        fileStream.Seek(0, SeekOrigin.Begin);
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

    private void OnApplicationPause(bool isPaused)
    {
        // Casque retiré ou application en arrière-plan : on sécurise ce qui est déjà capté.
        if (isPaused && isRecording)
            Flush();
    }

}
