#if TEXT_MESH_PRO_PRESENT || (UGUI_2_0_PRESENT && UNITY_6000_0_OR_NEWER)
using TMPro;
using UnityEngine.Events;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard
{
    /// <summary>
    /// Utility class to help facilitate input field relationship with <see cref="XRKeyboard"/>
    /// </summary>
    public class XRKeyboardDisplay : MonoBehaviour
    {
  

        [SerializeField, Tooltip("Input  field linked to this display.")]
        TMP_InputField m_InputField;

        bool _isApplyingFromKeyboard;
        string _prevKeyboardText = "";


 


        /// <summary>
        /// Input field linked to this display.
        /// </summary>
        public TMP_InputField inputField
        {
            get => m_InputField;
            set
            {
                if (inputField != null)
                    m_InputField.onSelect.RemoveListener(OnInputFieldGainedFocus);

                m_InputField = value;

                if (inputField != null)
                {
                    m_InputField.resetOnDeActivation = false;
                    m_InputField.onSelect.AddListener(OnInputFieldGainedFocus);
                }
            }
        }




        // The script requires setter property logic to be run, so disable when playing
        [SerializeField, Tooltip("Keyboard for this display to monitor and interact with. If empty this will default to the GlobalNonNativeKeyboard keyboard.")]
        XRKeyboard m_Keyboard;

        /// <summary>
        /// Keyboard for this display to monitor and interact with. If empty this will default to the <see cref="GlobalNonNativeKeyboard"/> keyboard.
        /// </summary>
        public XRKeyboard keyboard
        {
            get => m_Keyboard;
            set => SetKeyboard(value);
        }

        [SerializeField, Tooltip("If true, this display will use the keyboard reference. If false or if the keyboard field is empty, this display will use global keyboard.")]
        bool m_UseSceneKeyboard;

        /// <summary>
        /// If true, this display will use the keyboard reference. If false or if the keyboard field is empty,
        /// this display will use global keyboard.
        /// </summary>
        public bool useSceneKeyboard
        {
            get => m_UseSceneKeyboard;
            set => m_UseSceneKeyboard = value;
        }

        [SerializeField, Tooltip("If true, this display will update with each key press. If false, this display will update on OnTextSubmit.")]
        bool m_UpdateOnKeyPress = true;

        /// <summary>
        /// If true, this display will update with each key press. If false, this display will update on OnTextSubmit.
        /// </summary>
        public bool updateOnKeyPress
        {
            get => m_UpdateOnKeyPress;
            set => m_UpdateOnKeyPress = value;
        }

        [SerializeField, Tooltip("If true, this display will always subscribe to the keyboard updates. If false, this display will subscribe to keyboard when the input field gains focus.")]
        bool m_AlwaysObserveKeyboard;

        /// <summary>
        /// If true, this display will always subscribe to the keyboard updates. If false, this display will subscribe
        /// to keyboard when the input field gains focus.
        /// </summary>
        public bool alwaysObserveKeyboard
        {
            get => m_AlwaysObserveKeyboard;
            set => m_AlwaysObserveKeyboard = value;
        }

        [SerializeField, Tooltip("If true, this display will use the input field's character limit to limit the update text from the keyboard and will pass this into the keyboard when opening.")]
        public bool m_MonitorInputFieldCharacterLimit;

        /// <summary>
        /// If true, this display will use the input field's character limit to limit the update text from the keyboard
        /// and will pass this into the keyboard when opening if.
        /// </summary>
        public bool monitorInputFieldCharacterLimit
        {
            get => m_MonitorInputFieldCharacterLimit;
            set => m_MonitorInputFieldCharacterLimit = value;
        }

        [SerializeField, Tooltip("If true, this display will clear the input field text on text submit from the keyboard.")]
        public bool m_ClearTextOnSubmit;

        /// <summary>
        /// If true, this display will clear the input field text on text submit from the keyboard.
        /// </summary>
        public bool clearTextOnSubmit
        {
            get => m_ClearTextOnSubmit;
            set => m_ClearTextOnSubmit = value;
        }

        [SerializeField, Tooltip("If true, this display will clear the input field text when the keyboard opens.")]
        public bool m_ClearTextOnOpen;

        /// <summary>
        /// If true, this display will clear the input field text on text submit from the keyboard.
        /// </summary>
        public bool clearTextOnOpen
        {
            get => m_ClearTextOnOpen;
            set => m_ClearTextOnOpen = value;
        }


        [SerializeField, Tooltip("If true, this display will close the keyboard it is observing when this GameObject is disabled.")]
        public bool m_HideKeyboardOnDisable = true;
        
        /// <summary>
        /// If true, this display will close the keyboard it is observing when this GameObject is disabled.
        /// </summary>
        /// <remarks>If this display is not observing a keyboard when disabled, this will have not effect on open keyboards.</remarks>
        public bool hideKeyboardOnDisable
        {
            get => m_HideKeyboardOnDisable;
            set => m_HideKeyboardOnDisable = value;
        }
        
        [SerializeField, Tooltip("The event that is called when this display receives a text submitted event from the keyboard. Invoked with the keyboard text as a parameter.")]
        UnityEvent<string> m_OnTextSubmitted = new UnityEvent<string>();

        /// <summary>
        /// The event that is called when this display receives a text submitted event from the keyboard.
        /// </summary>
        public UnityEvent<string> onTextSubmitted
        {
            get => m_OnTextSubmitted;
            set => m_OnTextSubmitted = value;
        }

        [SerializeField, Tooltip("The event that is called when this display opens a keyboard.")]
        UnityEvent m_OnKeyboardOpened = new UnityEvent();

        /// <summary>
        /// The event that is called when this display opens a keyboard.
        /// </summary>
        public UnityEvent onKeyboardOpened
        {
            get => m_OnKeyboardOpened;
            set => m_OnKeyboardOpened = value;
        }

        [SerializeField, Tooltip("The event that is called when the keyboard this display is observing is closed.")]
        UnityEvent m_OnKeyboardClosed = new UnityEvent();

        /// <summary>
        /// The event that is called when the keyboard this display is observing is closed.
        /// </summary>
        public UnityEvent onKeyboardClosed
        {
            get => m_OnKeyboardClosed;
            set => m_OnKeyboardClosed = value;
        }

        [SerializeField, Tooltip("The event that is called when the keyboard changes focus and this display is not focused.")]
        UnityEvent m_OnKeyboardFocusChanged = new UnityEvent();

        /// <summary>
        /// The event that is called when the keyboard changes focus and this display is not focused.
        /// </summary>
        public UnityEvent onKeyboardFocusChanged
        {
            get => m_OnKeyboardFocusChanged;
            set => m_OnKeyboardFocusChanged = value;
        }

        // Active keyboard for this display
        XRKeyboard m_ActiveKeyboard;

        bool m_IsActivelyObservingKeyboard;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        /// 

        public float distance = 0.5f;
        public float verticleOffset = -0.5f;
        public Transform positionSource;

        void Awake()
        {
            // Set active keyboard to any serialized keyboard
            m_ActiveKeyboard = m_Keyboard;

            if (m_InputField != null)
            {
                // resetOnDeActivation should be false so the caret position does not break with the keyboard interaction
                m_InputField.resetOnDeActivation = false;

                // shouldHideSoftKeyboard should be true so there is no conflict with the spatial keyboard and the system keyboard
                m_InputField.shouldHideSoftKeyboard = true;
                positionSource= m_InputField.transform;
                Vector3 direction = -positionSource.forward;
                direction.y = 0;
                direction.Normalize();

                Vector3 targetPosition = positionSource.position + distance * direction + Vector3.up * verticleOffset;

            }

            if (m_AlwaysObserveKeyboard && m_ActiveKeyboard != null)
                StartObservingKeyboard(m_ActiveKeyboard);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void OnEnable()
        {
            if (m_InputField != null)
                m_InputField.onSelect.AddListener(OnInputFieldGainedFocus);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void OnDisable()
        {
            if (m_InputField != null)
                m_InputField.onSelect.RemoveListener(OnInputFieldGainedFocus);
            
            // Close the keyboard this display is observing 
            var isObservingKeyboard = m_ActiveKeyboard != null && m_ActiveKeyboard.gameObject.activeInHierarchy && m_IsActivelyObservingKeyboard;
            if (m_HideKeyboardOnDisable && isObservingKeyboard && m_ActiveKeyboard.isOpen)
                m_ActiveKeyboard.Close();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void OnDestroy()
        {
            StopObservingKeyboard(m_ActiveKeyboard);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void Start()
        {
            // Set active keyboard to global keyboard if needed
            if (m_ActiveKeyboard == null || !m_UseSceneKeyboard)
                m_ActiveKeyboard = GlobalNonNativeKeyboard.instance.keyboard;

            // Observe keyboard if always observe is true
            var observeOnStart = m_AlwaysObserveKeyboard && m_ActiveKeyboard != null & !m_IsActivelyObservingKeyboard;
            if (observeOnStart)
                StartObservingKeyboard(m_ActiveKeyboard);
        }

        void SetKeyboard(XRKeyboard updateKeyboard, bool observeKeyboard = true)
        {
            if (ReferenceEquals(updateKeyboard, m_Keyboard))
                return;

            StopObservingKeyboard(m_ActiveKeyboard);

            // Update serialized referenced
            m_Keyboard = updateKeyboard;

            // Update private keyboard
            m_ActiveKeyboard = m_Keyboard;

            if (m_ActiveKeyboard != null && (observeKeyboard || m_AlwaysObserveKeyboard))
                StartObservingKeyboard(m_ActiveKeyboard);
        }

        void StartObservingKeyboard(XRKeyboard activeKeyboard)
        {
            if (activeKeyboard == null || m_IsActivelyObservingKeyboard)
                return;

            activeKeyboard.onTextUpdated.AddListener(OnTextUpdate);
            activeKeyboard.onTextSubmitted.AddListener(OnTextSubmit);
            activeKeyboard.onClosed.AddListener(KeyboardClosing);
            activeKeyboard.onOpened.AddListener(KeyboardOpening);
            activeKeyboard.onFocusChanged.AddListener(KeyboardFocusChanged);

            m_IsActivelyObservingKeyboard = true;
        }

        void StopObservingKeyboard(XRKeyboard activeKeyboard)
        {
            if (activeKeyboard == null)
                return;

            activeKeyboard.onTextUpdated.RemoveListener(OnTextUpdate);
            activeKeyboard.onTextSubmitted.RemoveListener(OnTextSubmit);
            activeKeyboard.onClosed.RemoveListener(KeyboardClosing);
            activeKeyboard.onOpened.RemoveListener(KeyboardOpening);
            activeKeyboard.onFocusChanged.RemoveListener(KeyboardFocusChanged);

            m_IsActivelyObservingKeyboard = false;
        }

        void OnInputFieldGainedFocus(string text)
        {
            // If this display is already observing keyboard, sync, attempt to reposition, and early out
            // Displays that are always observing keyboards call open to ensure they sync with the keyboard
            if (m_IsActivelyObservingKeyboard && !alwaysObserveKeyboard)
            {
                if (!m_UseSceneKeyboard || m_Keyboard == null)
                    GlobalNonNativeKeyboard.instance.RepositionKeyboardIfOutOfView();

                // Sync input field caret position with keyboard caret position
                m_InputField.caretPosition = m_ActiveKeyboard.caretPosition;
                return;
            }

            if (m_ClearTextOnOpen)
                m_InputField.text = string.Empty;

            // If not using a scene keyboard, use global keyboard.
            if (!m_UseSceneKeyboard || m_Keyboard == null)
            {
                GlobalNonNativeKeyboard.instance.ShowKeyboard(m_InputField, m_MonitorInputFieldCharacterLimit);
            }
            else
            {
                m_ActiveKeyboard.Open(m_InputField, m_MonitorInputFieldCharacterLimit);
            }

            _prevKeyboardText = m_InputField.text; // snapshot for diffing

            m_InputField.selectionAnchorPosition = m_InputField.caretPosition;
            m_InputField.selectionFocusPosition = m_InputField.caretPosition;


            // Sync input field caret position with keyboard caret position
            m_InputField.caretPosition = m_ActiveKeyboard.caretPosition;

            // This display is opening the keyboard
            m_OnKeyboardOpened.Invoke();

            StartObservingKeyboard(m_ActiveKeyboard);
        }

        void OnTextSubmit(KeyboardTextEventArgs args)
        {
            // Treat Enter as "insert newline at caret" (don't overwrite the field)
            _isApplyingFromKeyboard = true;
            try
            {
                InsertAtCaret("\n");           // <-- newline
                _prevKeyboardText = (args.keyboardText ?? string.Empty).Replace("\r\n", "\n");

                m_InputField.ForceLabelUpdate();
            }
            finally { _isApplyingFromKeyboard = false; }

            // Optionally still forward the submit event to listeners
            m_OnTextSubmitted?.Invoke(m_InputField.text);


        }

    /*    void OnTextSubmit(KeyboardTextEventArgs args)
        {
            if (_isApplyingFromKeyboard) return;
            ApplyFromKeyboard(args.keyboardText);
            m_OnTextSubmitted?.Invoke(args.keyboardText);
            
            m_InputField.SetTextWithoutNotify(m_InputField.text + "\n");
            if (m_ClearTextOnSubmit)
            {
                m_InputField.SetTextWithoutNotify(string.Empty);
                m_InputField.caretPosition = 0;
                m_InputField.selectionAnchorPosition = 0;
                m_InputField.selectionFocusPosition = 0;
                _prevKeyboardText = "";
            }
        }*/
        void OnTextUpdate(KeyboardTextEventArgs args)
        {
            if (!m_UpdateOnKeyPress || _isApplyingFromKeyboard)
                return;

            ApplyFromKeyboard(args.keyboardText);
        }


        void ApplyFromKeyboard(string keyboardText)
        {
            _isApplyingFromKeyboard = true;
            try
            {
                // Respect character limit
                string updated = keyboardText;
                if (m_MonitorInputFieldCharacterLimit && m_InputField.characterLimit > 0 && updated.Length > m_InputField.characterLimit)
                    updated = updated.Substring(0, m_InputField.characterLimit);

                string prev = _prevKeyboardText ?? "";

                // --- Simple delta detection ---
                if (updated.Length == prev.Length + 1 && updated.StartsWith(prev))
                {
                    // Single-char insert according to keyboard buffer — insert at caret in the field
                    InsertAtCaret(updated[updated.Length - 1].ToString());
                }
                else if (updated.Length + 1 == prev.Length && prev.StartsWith(updated))
                {
                    // Single backspace in keyboard buffer — delete at caret in the field
                    BackspaceAtCaret();
                }
                else
                {
                    // Fallback: set full text WITHOUT NOTIFY to avoid feedback loops
                    int caret = Mathf.Clamp(m_InputField.caretPosition, 0, m_InputField.text.Length);
                    m_InputField.SetTextWithoutNotify(updated);   // ← breaks the loop
                    caret = Mathf.Clamp(caret, 0, m_InputField.text.Length);
                    m_InputField.caretPosition = caret;
                    m_InputField.selectionAnchorPosition = caret;
                    m_InputField.selectionFocusPosition = caret;
                }

                _prevKeyboardText = updated;
                m_InputField.ForceLabelUpdate();
            }
            finally { _isApplyingFromKeyboard = false; }
        }

        void InsertAtCaret(string toInsert)
        {
            int a = Mathf.Min(m_InputField.selectionAnchorPosition, m_InputField.selectionFocusPosition);
            int b = Mathf.Max(m_InputField.selectionAnchorPosition, m_InputField.selectionFocusPosition);

            string t = m_InputField.text;
            if (b > a) t = t.Remove(a, b - a);
            t = t.Insert(a, toInsert);

            m_InputField.SetTextWithoutNotify(t);    // ← use no-notify
            int caret = a + toInsert.Length;
            m_InputField.caretPosition = caret;
            m_InputField.selectionAnchorPosition = caret;
            m_InputField.selectionFocusPosition = caret;
        }

        void BackspaceAtCaret()
        {
            int a = Mathf.Min(m_InputField.selectionAnchorPosition, m_InputField.selectionFocusPosition);
            int b = Mathf.Max(m_InputField.selectionAnchorPosition, m_InputField.selectionFocusPosition);

            string t = m_InputField.text;
            if (b > a)
            {
                t = t.Remove(a, b - a);
                m_InputField.SetTextWithoutNotify(t);
                m_InputField.caretPosition = a;
            }
            else if (a > 0)
            {
                t = t.Remove(a - 1, 1);
                m_InputField.SetTextWithoutNotify(t);
                m_InputField.caretPosition = a - 1;
            }

            int caret = m_InputField.caretPosition;
            m_InputField.selectionAnchorPosition = caret;
            m_InputField.selectionFocusPosition = caret;
        }


        void UpdateText(string keyboardText)
            {
                // If no input field, skip
                if (m_InputField == null)
                    return;

                // Track current caret and selection
                int caretPos = Mathf.Clamp(m_InputField.caretPosition, 0, m_InputField.text.Length);
                int anchorPos = Mathf.Clamp(m_InputField.selectionAnchorPosition, 0, m_InputField.text.Length);
                int focusPos = Mathf.Clamp(m_InputField.selectionFocusPosition, 0, m_InputField.text.Length);

                // If selection exists, replace selected text with the new keyboard input
                if (anchorPos != focusPos)
                {
                    int start = Mathf.Min(anchorPos, focusPos);
                    int end = Mathf.Max(anchorPos, focusPos);

                    m_InputField.text = m_InputField.text.Remove(start, end - start)
                                                         .Insert(start, keyboardText);
                    caretPos = start + keyboardText.Length;
                }
                else
                {
                    // Insert at caret
                    m_InputField.text = m_InputField.text.Insert(caretPos, keyboardText);
                    caretPos += keyboardText.Length;
                }

                // Respect character limit
                if (m_MonitorInputFieldCharacterLimit &&
                    m_InputField.characterLimit > 0 &&
                    m_InputField.text.Length > m_InputField.characterLimit)
                {
                    m_InputField.text = m_InputField.text.Substring(0, m_InputField.characterLimit);
                    caretPos = Mathf.Min(caretPos, m_InputField.text.Length);
                }

                // Update caret & selection
                m_InputField.caretPosition = caretPos;
                m_InputField.selectionAnchorPosition = caretPos;
                m_InputField.selectionFocusPosition = caretPos;

                m_InputField.ForceLabelUpdate();
            

        }

        void KeyboardOpening(KeyboardTextEventArgs args)
        {
            Debug.Assert(args.keyboard == m_ActiveKeyboard);

            if (args.keyboard != m_ActiveKeyboard)
                return;

            if (!m_InputField.isFocused && !m_AlwaysObserveKeyboard)
                StopObservingKeyboard(m_ActiveKeyboard);
        }

        void KeyboardClosing(KeyboardTextEventArgs args)
        {
            Debug.Assert(args.keyboard == m_ActiveKeyboard);

            if (args.keyboard != m_ActiveKeyboard)
                return;

            if (!m_AlwaysObserveKeyboard)
                StopObservingKeyboard(m_ActiveKeyboard);

            m_OnKeyboardClosed.Invoke();
        }

        void KeyboardFocusChanged(KeyboardTextEventArgs args)
        {
            Debug.Assert(args.keyboard == m_ActiveKeyboard);

            if (args.keyboard != m_ActiveKeyboard)
                return;

            if (!m_InputField.isFocused && !m_AlwaysObserveKeyboard)
                StopObservingKeyboard(m_ActiveKeyboard);

            // The keyboard changed focus and this input field is no longer in focus
            if (!m_InputField.isFocused)
                m_OnKeyboardFocusChanged.Invoke();
        }
    }
}
#endif
