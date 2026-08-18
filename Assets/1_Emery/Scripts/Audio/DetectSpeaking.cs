using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class DetectSpeaking : MonoBehaviour
{
	[Header("Variables")]
	[Tooltip("Indique si le joueur est en train de parler depuis un certain temps.")]
	private bool isSpeaking = false;

	[Tooltip("Le seuil de volume en décibels (dBFS) pour détecter la voix (ex: -30 pour un son modéré, 0 est le volume maximum).")]
	[SerializeField]
	private float dbThreshold = -70f;

	[Tooltip("Le temps en secondes pendant lequel le joueur doit parler en continu pour valider la détection.")]
	[SerializeField]
	private float timeRequiredToSpeak = 1.0f;

	[Tooltip("Mot-clé du microphone à utiliser (ex: 'Quest', 'Oculus').")]
	[SerializeField]
	private string preferredMicKeyword = "Oculus";

	[SerializeField]
	private float lengthDisplayFeedback = 1.0f;

	[Header("Visual Feedback")]
	[SerializeField]
	private GameObject feedbackImage;
	[SerializeField]
	private GameObject feedbackIsSpeaking;

	private int index = 0;
	private float currentSpeakingTime = 0f;
	private float currentSilentTime = 0f;

	private AudioClip micClip;
	private string micDevice;
	private readonly int sampleWindow = 256;

	private bool hasInterrupted = false;
	private bool hasTriedToInterrupt = false;
	//private int interruptionCount = 0;
	private List<InterruptionData> interruptions = new List<InterruptionData>();
	[SerializeField]
	[Tooltip("Durée maximale d'une section pour l'évaluation des interruptions.")]
	private float sizeTimerCurrentSection = 60f;
	private float timerCurrentSection = 0f;
	[SerializeField]
	private float ratioInterruption = 0.62f;
	[SerializeField]
	private int thresholdInterruption = 3;

	// Start is called before the first frame update
	void Start()
	{
		if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
			Permission.RequestUserPermission(Permission.Microphone);

		if (Microphone.devices.Length > 0)
		{
			// 1. Afficher tous les microphones pour le débogage
			Debug.Log("Microphones disponibles :");
			foreach (var device in Microphone.devices)
			{
				Debug.Log("[Emery] - " + device);
			}

			// 2. Par défaut, on prend le premier
			micDevice = Microphone.devices[0];

			// 3. Chercher le microphone du casque via le mot-clé
			if (!string.IsNullOrEmpty(preferredMicKeyword))
			{
				foreach (var device in Microphone.devices)
				{
					if (device.ToLower().Contains(preferredMicKeyword.ToLower()))
					{
						micDevice = device;
						break; // On a trouvé le bon, on arrête la boucle
					}
				}
			}

			Debug.Log("Microphone sélectionné : " + micDevice);
			
			// Enregistrement continu avec délai d'initialisation
			micClip = Microphone.Start(micDevice, true, 10, 44100);
			StartCoroutine(WaitForMicrophoneInitialization());
		}
		else
		{
			Debug.LogWarning("Aucun microphone détecté !");
		}
	}

	private float GetMicrophoneVolume()
	{
		int micPosition = Microphone.GetPosition(micDevice);
		
		if (micPosition < 0 || micPosition < sampleWindow)
			return 0f;

		int startPosition = micPosition - sampleWindow;
		
		// Vérifier que la position est valide
		if (startPosition < 0)
			startPosition = 0;

		float[] waveData = new float[sampleWindow];
		
		try
		{
			micClip.GetData(waveData, startPosition);
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning($"[Emery] Erreur lecture audio: {ex.Message}");
			return 0f;
		}

		float levelMax = 0f;
		for (int i = 0; i < sampleWindow; i++)
		{
			float wavePeak = waveData[i] * waveData[i];
			if (levelMax < wavePeak)
			{
				levelMax = wavePeak;
			}
		}

		float volume = Mathf.Sqrt(levelMax);

		return volume;
	}

	private IEnumerator WaitForMicrophoneInitialization()
	{
		yield return new WaitForSeconds(1.5f);
		Debug.Log($"[Emery] Microphone initialisé: {Microphone.IsRecording(micDevice)}");
		Debug.Log($"[Emery] Clip longueur: {micClip.length}, Fréquence: {micClip.frequency}");
	}

	// Update is called once per frame
	void Update()
	{
		if (SpeakingInterruption.Instance == null)
			return;

		// Gestion de l'interruption
		CheckLastsInterruptions();

		if (micClip != null)
		{
			float currentVolume = GetMicrophoneVolume();

			// Calcul des décibels en échelle dBFS (0 dB = Volume Max, nombres négatifs pour plus faible)
			float currentDb = -140f; // Considérer -80 dB comme du silence absolu pour éviter -Infinity
			if (currentVolume > 0)
			{
				currentDb = 20f * Mathf.Log10(currentVolume);
			}

			// Si le volume actuel dépasse (est supérieur à) notre seuil cible (ex: > -30 dB)
			if (currentDb >= dbThreshold)
			{
				currentSpeakingTime += Time.deltaTime;

				if (currentSpeakingTime >= timeRequiredToSpeak)
				{
					SpeakingInterruption.Instance.RPC_SetIsPausing(index, false);
					if (!isSpeaking)
					{
						currentSilentTime = 0f; // Réinitialiser le temps de silence
						isSpeaking = true;
						SpeakingInterruption.Instance.SetIsSpeaking(index, isSpeaking);
					}
				}
			}
			else
			{
				if (currentSilentTime >= timeRequiredToSpeak)
				{
					// Si le son baisse, on réinitialise
					currentSpeakingTime = 0f;
					if (isSpeaking)
					{
						isSpeaking = false;
						SpeakingInterruption.Instance.SetIsSpeaking(index, isSpeaking);
						SpeakingInterruption.Instance.RPC_SetIsPausing(index, false);
					}
				}
				else
				{
					currentSilentTime += Time.deltaTime;
					if (currentSilentTime > .2f)
						SpeakingInterruption.Instance.RPC_SetIsPausing(index, true);
				}
			}
		}

		ClearOldInterruptions();
		//feedbackIsSpeaking.SetActive(isSpeaking);
	}

	private void CheckLastsInterruptions()
	{
		//On sauvegarde si le joueur a échoué à interrompre
		if (SpeakingInterruption.Instance.GetLastTriedInterrupter() == index)
		{
			// hasTriedToInterrupt permet d'éviter les problèmes de timing entre le joueur et le serveur.
			if (!hasTriedToInterrupt)
			{
				hasTriedToInterrupt = true;

				interruptions.Add(new InterruptionData
				{
					timeOfInterruption = Time.time,
					hasSucceeded = false
				});
			}
		}
		else
			hasTriedToInterrupt = false;

		//On sauvegarde si le joueur a réussi à interrompre
		if (SpeakingInterruption.Instance.GetLastInterrupter() == index)
		{
			// hasInterrupted permet d'éviter les problèmes de timing entre le joueur et le serveur.
			if (!hasInterrupted)
			{
				hasInterrupted = true;
				ShowFeedback();

				interruptions.Add(new InterruptionData
				{
					timeOfInterruption = Time.time,
					hasSucceeded = true
				});
			}
		}
		else
			hasInterrupted = false;

		// On compte le nombre d'interruptions réussies dans la section actuelle, si cela dépasse le ratio défini,
		// on envoie un feedback à tous les joueurs, et on reset la section, ainsi que les interruptions.
		timerCurrentSection += Time.deltaTime;
		if (timerCurrentSection >= sizeTimerCurrentSection)
		{
			int successfulInterruptions = 0;
			foreach (InterruptionData interruption in interruptions)
			{
				successfulInterruptions += interruption.hasSucceeded ? 1 : 0;
			}
			float ratio = interruptions.Count > 0 ? (float)successfulInterruptions / interruptions.Count : 0f;
			if (ratio > ratioInterruption && successfulInterruptions > thresholdInterruption)
			{
				SpeakingInterruption.Instance.RPC_ShowFeedbackInterruptionToAll(index, successfulInterruptions);
				SendDataFeedbackCollectif(successfulInterruptions, ratio);
				interruptions.Clear();
				timerCurrentSection = 0f;
			}
		}
	}

	private void ClearOldInterruptions()
	{
		if (interruptions.Count == 0)
			return;

		foreach (InterruptionData interruption in interruptions)
		{
			if (Time.time - interruption.timeOfInterruption > sizeTimerCurrentSection)
			{
				interruptions.Remove(interruption);
			}
		}
	}

	public bool GetIsSpeaking()
	{
		return isSpeaking;
	}

	public void ShowFeedback()
	{
		feedbackImage.SetActive(true);
		SpeakingInterruption.Instance.ResetLastInterrupter();
		DataFeedbacks.Instance.AddFeedbackLog(";;;;;;1;");
		StartCoroutine(FeedbackCoroutine());
	}

	private IEnumerator FeedbackCoroutine()
	{
		yield return new WaitForSeconds(lengthDisplayFeedback);
		feedbackImage.SetActive(false);
		DataFeedbacks.Instance.AddFeedbackLog(";;;;;;0;");
	}

	private void SendDataFeedbackCollectif(int successfullInterruptions, float ratio)
	{
		DataFeedbacks.Instance.AddFeedbackLog($";;;;;;;1;{index};{successfullInterruptions};{interruptions.Count};{ratio};");
	}

	public void SetIndex(int newIndex)
	{
		index = newIndex;
	}

	public int GetMicrophonePosition()
	{
		if (micDevice == null)
			return -1;
		return Microphone.GetPosition(micDevice);
	}

	public AudioClip GetMicClip()
	{
		return micClip;
	}

	public struct InterruptionData
	{
		public float timeOfInterruption;
		public bool hasSucceeded;
	}
}
