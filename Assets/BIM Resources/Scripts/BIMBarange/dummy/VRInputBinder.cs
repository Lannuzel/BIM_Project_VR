using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

public class VRInputBinder : MonoBehaviour, ISelectHandler
{
    public TMP_InputField field;           // your editor field
    public NonNativeKeyboard keyboard;     // your MRTK non-native keyboard

    void Awake()
    {
        field.onFocusSelectAll = false;    // avoid select-all on Android
        field.shouldHideSoftKeyboard = true;
    }

    public void OnSelect(BaseEventData _)
    {
        // Make the keyboard operate on THIS field directly
        keyboard.InputField = field;
        keyboard.PresentKeyboard();

        // Next frame: collapse any selection so typing inserts at caret
        StartCoroutine(PlaceCaretNextFrame());
    }

    System.Collections.IEnumerator PlaceCaretNextFrame()
    {
        yield return null;
        int pos = field.caretPosition;
        field.selectionAnchorPosition = pos;
        field.selectionFocusPosition = pos;
        field.ForceLabelUpdate();
    }
}
