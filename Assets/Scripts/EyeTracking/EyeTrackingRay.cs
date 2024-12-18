using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class EyeTrackingRay : MonoBehaviour
{
    [SerializeField]
    private float rayDistance = 1.0f;

    [SerializeField]
    private float rayWidth = 0.01f;

    [SerializeField]
    private LayerMask layersToInclude;

    [SerializeField]
    private Color rayColorDefaultState = Color.yellow;

    [SerializeField]
    private Color rayColorHoverState = Color.red;

    [SerializeField]
    private OVRHand handUsedForPinchSelection;

    [SerializeField]
    private bool mockHandPinchGesture;

    private bool intercepting;

    private bool allowPinchSelection;

    private LineRenderer lineRenderer;

    private Dictionary<int, EyeInteractable> interactables = new Dictionary<int, EyeInteractable>();

    private EyeInteractable lastEyeInteractable;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        allowPinchSelection = handUsedForPinchSelection != null;
        SetupRay();
    }

    private void SetupRay()
    {
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth;
        lineRenderer.startColor = rayColorDefaultState;
        lineRenderer.endColor = rayColorDefaultState;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, new Vector3(transform.position.x, transform.position.y, transform.position.z + rayDistance));
    }

    private void Update()
    {
        lineRenderer.enabled = !IsPinching();
        
        SelectionStarted();

        // clear all hover selections when no intercepting
        if (!intercepting)
        {
            lineRenderer.startColor = lineRenderer.endColor = rayColorDefaultState;
            lineRenderer.SetPosition(1, new Vector3(0, 0, transform.position.z + rayDistance));
            HoverEnded();
        }
    }

    private void SelectionStarted()
    {
        if (IsPinching())
        {
            lastEyeInteractable?.Select(true, handUsedForPinchSelection.IsTracked ? 
                handUsedForPinchSelection.transform : transform);
        }
        else
        {
            lastEyeInteractable?.Select(false);
        }
    }

    private void FixedUpdate()
    {
        if (IsPinching()) return;

        Vector3 rayDirection = transform.TransformDirection(Vector3.forward) * rayDistance;

        // Check if eye ray intersects with any objects included in the layersToInclude
        intercepting = Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, Mathf.Infinity, layersToInclude);

        if (intercepting)
        {
            // hover ended
            HoverEnded();

            lineRenderer.startColor = lineRenderer.endColor = rayColorHoverState;

            if (interactables.TryGetValue(hit.transform.gameObject.GetHashCode(), out EyeInteractable eyeInteractable))
            {
                if (eyeInteractable != null)  // Vérifie si l'objet interactable existe
                {
                    var toLocalSpace = transform.InverseTransformPoint(eyeInteractable.transform.position);
                    lineRenderer.SetPosition(1, new Vector3(0, 0, toLocalSpace.z));

                    // hover started
                    eyeInteractable.Hover(true);

                    lastEyeInteractable = eyeInteractable;
                }
                else
                {
                    //Debug.LogWarning($"EyeInteractable component is missing on {hit.transform.gameObject.name}.");
                }
            }
            else
            {
                //Debug.LogWarning($"Interactable object not found in dictionary for {hit.transform.gameObject.name}.");
            }
        }

    }

    private void HoverEnded(bool reset = false)
    {
        foreach (var interactable in interactables)
        {
            if (interactable.Value != null) // Vérifie si la référence n'est pas nulle
            {
                interactable.Value.Hover(false);
            }
        }

        if (reset)
        {
            interactables.Clear();
        }
    }

    private void OnDestroy() => interactables.Clear();

    private bool IsPinching() => (allowPinchSelection && handUsedForPinchSelection.GetFingerIsPinching(OVRHand.HandFinger.Index)) || mockHandPinchGesture;

    public bool TryGetRayHit(out RaycastHit hit)
    {
        Vector3 rayDirection = transform.TransformDirection(Vector3.forward) * rayDistance;
        return Physics.Raycast(transform.position, rayDirection, out hit, Mathf.Infinity, layersToInclude);
    }

}
