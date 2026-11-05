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

    public void ActiveLogRecorder(GameObject gameObject)
    {
        if (gameObject == null) return;
        if (!roleCanvases.Exists(c => c.canvas == gameObject))
            roleCanvases.Add((gameObject, userName));

        string playerName = NetworkManager.GetLocalPlayerName();
        if (playerName == userName)
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
