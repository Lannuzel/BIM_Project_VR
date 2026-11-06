using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BIMInteractionManager : MonoBehaviour
{
    [SerializeField]
    private string userName;
    public GameObject interactionMenu;

    // F18 : canvas réservés à un rôle (ex. « Lancer l'activité » pour le Lecteur), mémorisés pour être
    // réappliqués lors d'un changement de rôle à chaud.
    private static readonly List<(GameObject canvas, string role)> roleCanvases = new List<(GameObject, string)>();

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.LTouch))
        {
            if (interactionMenu != null)
            {
                interactionMenu.SetActive(!interactionMenu.activeSelf);
            }
            else
            {
                Debug.LogWarning("No menu assigend assigned!");
            }
        }

    }

    public void ToggleActivation(GameObject gameObject)
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
        else gameObject.SetActive(true);
    }

    /// <summary>
    /// Rôle qui voit les fenêtres de lancement (vérification des participants, « Lancer l'activité »).
    /// Recette du 6 novembre 2026 : lu dans SessionConfig.startActivityRole (Modélisateur) au lieu du champ de
    /// scène, qui valait Lecteur dans XP1 et XP2 et Modélisateur dans XP3.
    /// </summary>
    private string LaunchRole
    {
        get
        {
            string configured = SessionConfig.Instance.startActivityRole;
            return string.IsNullOrWhiteSpace(configured) ? userName : configured.Trim();
        }
    }

    public void ActiveLogRecorder(GameObject gameObject)
    {
        if (gameObject == null) return;
        string role = LaunchRole;
        if (!roleCanvases.Exists(c => c.canvas == gameObject))
            roleCanvases.Add((gameObject, role));

        string playerName = NetworkManager.GetLocalPlayerName();
        if (playerName == role)
        {
            ToggleActivation(gameObject);
        }
    }

    /// <summary>Active les canvas du nouveau rôle et masque ceux des autres rôles.</summary>
    public static void ReapplyRoles(string playerName)
    {
        roleCanvases.RemoveAll(c => c.canvas == null);
        foreach (var (canvas, role) in roleCanvases)
            canvas.SetActive(role == playerName);
    }
}
