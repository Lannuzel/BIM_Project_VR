using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Étape « déplacer un objet » du tutoriel (F5).
/// Sélection au grip droit (gâchette intérieure) en pointant l'objet marqué « TutorialObject »,
/// déplacement au joystick droit, bascule déplacement / rotation avec le bouton A.
/// L'étape est validée quand l'objet est posé à moins de <see cref="positionTolerance"/> de sa cible
/// (l'ombre bleue, <see cref="targetTransform"/>).
/// </summary>
public class RaycastSelectAndMoveTutorial : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform rightController;       // Assign RightHandAnchor
    public LayerMask selectableLayer;
    public float maxRayDistance = 10f;
    public float moveSpeed = 1.0f;
    public float rayDistance = 10f;
    public float objectFollowSpeed = 10f;   // How fast the object follows
    [Tooltip("F8 : vitesse de rotation au joystick (degres par seconde).")]
    public float rotationSpeed = 180f;

    public Material highlightMaterial;     // Material to highlight selected line

    [Header("Validation (F5)")]
    [Tooltip("Cible à atteindre (l'ombre bleue). Si vide : l'enfant nommé « Cylinder (1) ».")]
    public Transform targetTransform;
    [Tooltip("Objet à déplacer. Si vide : le premier enfant portant le tag « TutorialObject ».")]
    public Transform tutorialObject;
    [Tooltip("Écart de position toléré (m) entre l'objet et sa cible.")]
    public float positionTolerance = 0.08f;
    [Tooltip("Ne comparer que la position horizontale (X/Z) : l'ombre au sol et le pivot de l'objet n'ont pas la même hauteur.")]
    public bool horizontalDistanceOnly = true;
    [Tooltip("Vérifier aussi l'orientation (inutile pour un cylindre symétrique).")]
    public bool checkRotation = false;
    [Tooltip("Écart d'orientation toléré (degrés) si checkRotation est actif.")]
    public float angleTolerance = 15f;
    [Tooltip("Texte optionnel d'état pour le tutoriel.")]
    public TMP_Text statusText;

    [Header("Indicateur de mode (F6)")]
    [Tooltip("Texte optionnel du panneau indiquant le mode courant.")]
    public TMP_Text modeText;
    [Tooltip("Afficher une étiquette flottante « Mode : ... » au-dessus de l'objet.")]
    public bool showFloatingModeLabel = true;
    [Tooltip("Hauteur (m) de l'étiquette au-dessus de l'objet.")]
    public float labelHeight = 0.3f;

    /// <summary>Libellé du mode courant : « déplacement » ou « rotation ».</summary>
    public string CurrentModeLabel => currentMode == ControlMode.Rotate ? "rotation" : "déplacement";

    /// <summary>Déclenché à chaque bascule de mode (bouton A).</summary>
    public event Action<string> ModeChanged;

    private TextMeshPro floatingLabel;

    /// <summary>Vrai quand l'objet est posé sur sa cible (dans la tolérance) et relâché.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>Déclenché quand l'état de validation change.</summary>
    public event Action<bool> CompletionChanged;

    /// <summary>Dernier message d'aide affiché (écart restant, conseil).</summary>
    public string LastMessage { get; private set; } = string.Empty;

    private GameObject selectedObject;
    private Vector3 initialLocalOffset; // Local offset from controller to object when selected
    private Vector3 cumulativeJoystickOffset = Vector3.zero;

    private enum ControlMode { Translate, Rotate }
    private ControlMode currentMode = ControlMode.Translate;
    private bool aButtonWasPressed = false;

    private float previousControllerRoll = 0f;

    private Material originalMaterial;
    private Renderer objectRenderer; // Renderer of the selected object

    private void Awake()
    {
        if (targetTransform == null)
            targetTransform = transform.Find("Cylinder (1)");

        if (tutorialObject == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && child.CompareTag("TutorialObject"))
                {
                    tutorialObject = child;
                    break;
                }
            }
        }
    }

    private void OnEnable()
    {
        UpdateStatus();
        UpdateModeLabel();
    }

    private void OnDisable()
    {
        Deselect();
        if (floatingLabel != null)
            floatingLabel.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (floatingLabel == null || tutorialObject == null)
            return;

        floatingLabel.transform.position = tutorialObject.position + Vector3.up * labelHeight;
        Transform cam = cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);
        if (cam != null)
        {
            Vector3 toLabel = floatingLabel.transform.position - cam.position;
            toLabel.y = 0f;
            if (toLabel.sqrMagnitude > 0.0001f)
                floatingLabel.transform.rotation = Quaternion.LookRotation(toLabel.normalized, Vector3.up);
        }
    }

    /// <summary>Met à jour le texte du panneau et l'étiquette flottante (F6).</summary>
    private void UpdateModeLabel()
    {
        string label = "Mode : " + CurrentModeLabel + "  (bouton A pour changer)";

        if (modeText != null)
            modeText.text = label;

        if (!showFloatingModeLabel)
            return;

        if (floatingLabel == null)
        {
            var go = new GameObject("ModeLabel");
            go.transform.SetParent(transform, false);
            floatingLabel = go.AddComponent<TextMeshPro>();
            floatingLabel.fontSize = 1.5f;
            floatingLabel.alignment = TextAlignmentOptions.Center;
            floatingLabel.color = Color.white;
            floatingLabel.outlineWidth = 0.2f;
            floatingLabel.outlineColor = Color.black;
            var rect = floatingLabel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1.2f, 0.3f);
        }

        floatingLabel.gameObject.SetActive(true);
        floatingLabel.text = label;
        floatingLabel.color = currentMode == ControlMode.Rotate ? new Color(1f, 0.75f, 0.2f) : new Color(0.5f, 0.9f, 1f);
    }

    void Update()
    {
        if (rightController == null)
            return;

        // Select
        if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
        {
            TrySelectObject();
        }
        // Deselect
        else if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger))
        {
            Deselect();
        }

        // Toggle mode with A button (button one)
        bool aButton = OVRInput.Get(OVRInput.Button.One);
        if (aButton && !aButtonWasPressed)
        {
            currentMode = currentMode == ControlMode.Translate ? ControlMode.Rotate : ControlMode.Translate;
            Debug.Log("Mode switched to: " + currentMode);
            UpdateModeLabel();
            ModeChanged?.Invoke(CurrentModeLabel);
        }
        aButtonWasPressed = aButton;

        // Apply behavior
        if (selectedObject != null)
        {
            if (currentMode == ControlMode.Translate)
            {
                FollowControllerPosition();
            }
            else if (currentMode == ControlMode.Rotate)
            {
                ApplyControllerRotation();
            }
        }

        EvaluateCompletion();
    }

    private void FollowControllerPosition()
    {
        // Step 1: Position offset
        Vector3 baseOffset = rightController.rotation * initialLocalOffset;
        Vector3 controllerPosition = rightController.position + baseOffset;

        // Step 2: Joystick movement
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 forward = rightController.forward;
            Vector3 right = rightController.right;

            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 joystickDelta = (right * input.x + forward * input.y) * moveSpeed * Time.deltaTime;
            cumulativeJoystickOffset += joystickDelta;
        }

        Vector3 targetPosition = controllerPosition + cumulativeJoystickOffset;

        selectedObject.transform.position = Vector3.Lerp(
            selectedObject.transform.position,
            targetPosition,
            Time.deltaTime * objectFollowSpeed
        );
    }

    private void ApplyControllerRotation()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        if (selectedObject == null) return;

        float yaw = 0f;

        // Joystick horizontal : rotation autour de l'axe vertical
        if (input.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                yaw = input.x * rotationSpeed * Time.deltaTime; // Right = +Y, Left = -Y
            }
        }

        Quaternion deltaJoystickRotation = Quaternion.Euler(0f, 0f, yaw);

        float currentRoll = rightController.rotation.eulerAngles.x;
        float deltaRoll = Mathf.DeltaAngle(previousControllerRoll, currentRoll);
        previousControllerRoll = currentRoll;

        // F8 : rotation appliquee directement (le Slerp precedent divisait la vitesse effective).
        selectedObject.transform.rotation = selectedObject.transform.rotation * deltaJoystickRotation;
    }

    private void TrySelectObject()
    {
        Vector3 rayOrigin = rightController.position;
        Vector3 rayDirection = rightController.forward;

        Ray ray = new Ray(rayOrigin, rayDirection);
        int mask = selectableLayer.value != 0 ? selectableLayer.value : Physics.DefaultRaycastLayers;

        // Le masque de calques est maintenant passé comme masque (et non comme distance).
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, mask))
        {
            if (hit.collider.CompareTag("TutorialObject"))
            {
                selectedObject = hit.collider.gameObject;

                // Save position offset
                Vector3 worldOffset = selectedObject.transform.position - rightController.position;
                initialLocalOffset = Quaternion.Inverse(rightController.rotation) * worldOffset;

                previousControllerRoll = rightController.rotation.eulerAngles.x;
                cumulativeJoystickOffset = Vector3.zero;

                // Surbrillance de l'objet sélectionné (et non de l'objet porteur du script)
                objectRenderer = selectedObject.GetComponent<Renderer>();
                if (objectRenderer != null && highlightMaterial != null)
                {
                    originalMaterial = objectRenderer.material;
                    objectRenderer.material = highlightMaterial;
                }

                SetMessage(string.Empty);
                Debug.Log("Selected: " + selectedObject.name);
            }
            else
            {
                SetMessage("Pointez l'objet vert, puis maintenez la gâchette intérieure.");
            }
        }
        else
        {
            SetMessage("Pointez l'objet vert, puis maintenez la gâchette intérieure.");
        }
    }

    private void Deselect()
    {
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }

        objectRenderer = null;
        originalMaterial = null;
        selectedObject = null;
        cumulativeJoystickOffset = Vector3.zero;
    }

    /// <summary>Écart de position actuel (m) entre l'objet et sa cible, ou -1 si inconnu.</summary>
    public float CurrentPositionError()
    {
        if (tutorialObject == null || targetTransform == null)
            return -1f;
        Vector3 a = tutorialObject.position, b = targetTransform.position;
        if (horizontalDistanceOnly) { a.y = 0f; b.y = 0f; }
        return Vector3.Distance(a, b);
    }

    private void EvaluateCompletion()
    {
        if (tutorialObject == null || targetTransform == null)
            return;

        float error = CurrentPositionError();
        bool positionOk = error <= positionTolerance;
        bool rotationOk = !checkRotation
                          || Quaternion.Angle(tutorialObject.rotation, targetTransform.rotation) <= angleTolerance;

        // L'objet doit être relâché sur la cible.
        bool completed = positionOk && rotationOk && selectedObject == null;

        if (completed != IsCompleted)
        {
            IsCompleted = completed;
            CompletionChanged?.Invoke(IsCompleted);
        }

        if (selectedObject != null)
        {
            SetMessage($"Écart avec la cible : {error * 100f:0} cm");
        }
        else if (completed)
        {
            SetMessage("Objet placé sur sa cible. Cliquez sur OK.");
        }
        else if (string.IsNullOrEmpty(LastMessage) || LastMessage.StartsWith("Écart"))
        {
            SetMessage($"Posez l'objet vert sur l'ombre bleue (écart actuel : {error * 100f:0} cm).");
        }
    }

    private void SetMessage(string message)
    {
        if (message == LastMessage)
            return;
        LastMessage = message;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (statusText != null)
            statusText.text = LastMessage;
    }
}
