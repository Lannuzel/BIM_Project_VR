using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class Key : MonoBehaviour
{
    [SerializeField]
    private char token;

    private float pushOffset = 0.25f;
    private float movementSpeed = 1.5f;

    private bool isPressed;

    private Vector3 targetPosition;
    private Vector3 initialPosition;

    public UnityEvent<char> OnPressed;

    private void Awake()
    {
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable = GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        interactable.hoverEntered.AddListener((args) => { PressVRHand(); });
        interactable.hoverExited.AddListener((args) => { ReleaseVRHand(); });

        XRPokeFilter filter = GetComponentInParent<XRPokeFilter>();
        filter.pokeCollider = GetComponent<BoxCollider>();

        XRPokeFollowAffordance affordance = GetComponentInParent<XRPokeFollowAffordance>();
        affordance.pokeFollowTransform = transform;
    }

    private void OnCollisionEnter(Collision col)
    {
        Press();
    }

    public void PressVRHand()
    {
        if (isPressed)
            return;

        isPressed = true;
        OnPressed?.Invoke(token);
    }

    public void ReleaseVRHand()
    {
        isPressed = false;
    }

    public void Press()
    {
        if (isPressed)
            return;

        isPressed = true;

        initialPosition = transform.position;
        targetPosition = new Vector3(transform.position.x, transform.position.y - pushOffset, transform.position.z);

        StartCoroutine(MoveTo(targetPosition, MoveBack, true));
    }

    private IEnumerator MoveTo(Vector3 position, Action onComplete, bool invokeOnPressed)
    {
        while (Vector3.Distance(transform.position, position) > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, position, movementSpeed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        onComplete?.Invoke();

        if (invokeOnPressed)
        {
            OnPressed?.Invoke(token);
        }
    }

    private void MoveBack()
    {
        StartCoroutine(MoveTo(initialPosition, Unlock, false));
    }

    private void Unlock()
    {
        isPressed = false;
    }
}
