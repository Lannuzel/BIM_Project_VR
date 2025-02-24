using Fusion;
using TMPro;
using UnityEngine;
using System.Collections;

public class NetworkedMUI : NetworkBehaviour
{
    private TextMeshProUGUI textMeshPro;

    [Networked] public Vector3 networkedPosition { get; set; }
    [Networked] public bool isLineActive { get; set; }
    [Networked] public int networkedCounter { get; set; }
    [Networked] public string networkedTextData { get; set; } // No OnChanged
    private Transform camRef {  get; set; }  

    private float xRange = 0.5f; // ±0.5m movement range
    private string lastText = ""; // Tracks previous text state

    private void Awake()
    {
        textMeshPro = GetComponentInChildren<TextMeshProUGUI>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            SetUIPositions(Vector3.zero, "Counter: 0", false);
            networkedCounter = 0;
            StartCoroutine(IncrementCounter());
        }
    }
    public void SetCameraRef(Transform cameraRef)
    {
        camRef = cameraRef;

    }
    private IEnumerator IncrementCounter()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            if (HasStateAuthority)
            {
                networkedCounter++;

                // ✅ Randomly move UI along X-axis (within ±0.5m)
                float randomX = Random.Range(-xRange, xRange);
                networkedPosition = new Vector3(networkedPosition.x + randomX, networkedPosition.y, networkedPosition.z);

                // ✅ Update text so all clients see the change
                networkedTextData = "Counter: " + networkedCounter.ToString();
            }
        }
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

            // ✅ Manually check if text changed
            if (networkedTextData != lastText)
            {
                textMeshPro.text = networkedTextData;
                lastText = networkedTextData; // Store last updated value
            }
            // ✅ Make UI always face the camera
            if (camRef != null)
            {
                transform.LookAt(camRef.transform);
                transform.rotation = Quaternion.LookRotation(transform.position - camRef.transform.position);
            }
        }
    }
}
