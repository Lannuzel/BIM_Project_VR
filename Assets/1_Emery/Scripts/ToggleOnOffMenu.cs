using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleOnOffMenu : MonoBehaviour
{
	[SerializeField]
	private GameObject menu;
	[SerializeField]
	private GameObject bimInteractionMenu;
	[SerializeField]
	private InputActionReference toggleMenuInput;

	private bool hasBeenPressedLately = false;

	private void Update()
	{
		if (OVRInput.Get(OVRInput.Button.Three) && !hasBeenPressedLately)
		{
			hasBeenPressedLately = true;
			menu.SetActive(!menu.activeSelf);
			bimInteractionMenu.SetActive(menu.activeSelf);
			StartCoroutine(ResetHasBeenPressedLately());
		}
	}

	private IEnumerator ResetHasBeenPressedLately()
	{
		yield return new WaitForSeconds(0.5f);
		hasBeenPressedLately = false;
	}
}
