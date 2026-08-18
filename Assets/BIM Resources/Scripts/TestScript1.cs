using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class TestScript1 : MonoBehaviour
{
    public TMP_Text data;
    public TMP_InputField inputField;
    private PlayerInput playerInput;
    public Image image;
    public InputActionReference grabActionReference;
    public InputActionMap actionMap;

    private XRIBIMInputActions userInputActions;

    [Header("Grab Settings")]
    public Transform grabPoint;
    public float grabRange = 0.5f;

    private GameObject grabbedObject;



    [Header("Grab Settings")]
    
    private XRBaseInteractor interactor; // Used for velocity calculation
    private XRIBIMInputActions inputActions;

    // Start is called before the first frame update
    void Start()
    {
        
        
        loadImage();
             
    }
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();


        // Instantiate XRIDefaultInputActions
        inputActions = new XRIBIMInputActions();

        // Enable actions for the right hand trigger
        inputActions.XRIRightInteraction.Enable();

        // Bind grab actions
        inputActions.XRIRightInteraction.Activate.performed += Fire_performed;
       // inputActions.XRIRightInteraction.Activate.canceled += Fire_performed;

        // Optionally bind for the left hand if needed
        // inputActions.XRILeftHand.Activate.started += OnGrabPressed;
        // inputActions.XRILeftHand.Activate.canceled += OnGrabReleased;

        // Get interactor (optional for velocity application)
        interactor = GetComponent<XRBaseInteractor>();
    }

    void Update()
    {
        if ((inputActions.XRIRightInteraction.Activate.ReadValue<float>()> 0.2f))
        {
            Debug.Log(" trigger remains pressed");
        }

    }
    private void Fire_performed(InputAction.CallbackContext context)
    {
        Debug.Log(" context " + context);
        Debug.Log("Fire!");
    }

    private void OnEnable()
    {
        // Subscribe to the grab action
        if (grabActionReference != null)
        {
            grabActionReference.action.started += OnGrabPressed;
            grabActionReference.action.canceled += OnGrabReleased;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from the grab action
        if (grabActionReference != null)
        {
            grabActionReference.action.started -= OnGrabPressed;
            grabActionReference.action.canceled -= OnGrabReleased;
        }
    }

    private void OnGrabPressed(InputAction.CallbackContext context)
    {
        if (grabbedObject == null)
        {
            Debug.Log(" context " + context);
            TryGrabObject();
        }
    }

    private void OnGrabReleased(InputAction.CallbackContext context)
    {
        if (grabbedObject != null)
        {
            Debug.Log(" context " + context);
            ReleaseObject();
        }
    }

    public void TryGrabObject()
    {
        Debug.Log(" called TryGrabObjet");
    }
    public void ReleaseObject()
    {
       
        Debug.Log(" called ReleaseObject");
    }



    // Update is called once per frame

    public void ChangeText()
    {
        data.text = "Jai Gayatri Mata";
    }
    public void Evaluate()
    {
        string expression = inputField.text.Replace("E+", "*10^")
                                         .Replace("E-", "*10^-");

        List<string> tokens = Tokenizer.Tokenize(expression);
        Parser parser = new(tokens);
        try
        {
            Node node = parser.Parse();
            string result = node.Evaluate().ToString();

            data.text = result;
        }
        catch (Exception)
        {
            data.text = "Invalid syntax";
            
        }

    }
    public void loadImage()
    {
        Sprite FULLHP = Resources.Load<Sprite>("ganeshji.png");      //FULL
        if (FULLHP != null )
        {
            image.sprite = FULLHP;
                
        }
    }

    public void DrawPositionLine(InputAction.CallbackContext context)
    {
        Debug.Log(" context " + context);
        Debug.Log("Fire!");
    }
}
