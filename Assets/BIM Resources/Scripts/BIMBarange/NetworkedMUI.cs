using Fusion;
using TMPro;
using UnityEngine;
using System.Collections;

public class NetworkedMUI : NetworkBehaviour
{
    private TextMeshProUGUI textMeshPro;

    [Networked] public Vector3 networkedPosition { get; set; }
    [Networked] public bool isLineActive { get; set; }
    [Networked] public string networkedTextData { get; set; } // No OnChanged
    private Transform camRef {  get; set; }

    private string lastText = ""; // Tracks previous text state

    private void Awake()
    {
        textMeshPro = GetComponentInChildren<TextMeshProUGUI>();
    }

    public override void Spawned()
    {
        // N5 : le compteur de test (« Counter: N » toutes les 5 s, décalage aléatoire en X) est supprimé :
        // il écrasait la valeur de mesure affichée sur tous les casques.
        if (HasStateAuthority)
        {
            SetUIPositions(Vector3.zero, "", false);
        }
    }
    public void SetCameraRef(Transform cameraRef)
    {
        camRef = cameraRef;

    }

    public void SetUIPositions(Vector3 position, string data, bool active)
    {
        if (HasStateAuthority)
        {
            networkedPosition = position;
            networkedTextData = data;
            isLineActive = active;
        }
    }

    public override void Render()
    {
        gameObject.SetActive(isLineActive);

        if (isLineActive)
        {
            transform.position = networkedPosition;

            if (networkedTextData != lastText)
            {
                textMeshPro.text = networkedTextData;
                lastText = networkedTextData; // Store last updated value
            }

            if (camRef == null && Camera.main != null)
                camRef = Camera.main.transform;

            // Make UI always face the camera
            if (camRef != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - camRef.transform.position);
            }
        }
    }
}
