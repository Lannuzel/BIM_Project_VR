using UnityEngine;
using TMPro; // Pour afficher le texte du compteur
using System.Collections; // Nécessaire pour IEnumerator

public class CountdownManager : MonoBehaviour
{
    public TextMeshProUGUI countdownText; // Texte pour le compteur
    public float countdownTime = 5f; // Temps de décompte
    public System.Action OnCountdownFinished; // Événement à déclencher
    private bool isRunning = false;

    private void Start()
    {
        // StartCoroutine(StartCountdown());
        countdownText.gameObject.SetActive(false); // Masquer le texte du compteur au démarrage
    }

    public void StartCountdown()
    {
        if (!isRunning)
        {
            countdownText.gameObject.SetActive(true); // Masquer le texte du compteur au démarrage
            StartCoroutine(CountdownCoroutine());
        }
    }

    public IEnumerator CountdownCoroutine()
    {
        isRunning = true;

        countdownText.gameObject.SetActive(true); // Afficher le texte du compteur
        float remainingTime = countdownTime;

        while (remainingTime > 0)
        {
            countdownText.text = Mathf.Ceil(remainingTime).ToString(); // Affiche le temps restant
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);
        countdownText.gameObject.SetActive(false);

        // Déclencher l'événement de fin du compteur
        OnCountdownFinished?.Invoke();
    }
}
