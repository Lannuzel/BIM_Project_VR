using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices.WindowsRuntime;
using Fusion;
using static Unity.Collections.Unicode;
public class CopyPasteObject : Fusion.NetworkBehaviour
{
    private List<GameObject> selectedObjects;// = new List<GameObject>(); // List of selected objects
    private List<GameObject> copiedObjects = new List<GameObject>(); // List of copied objects
    private bool isMovingObjects = false;
    private Dictionary<GameObject, GameObject> boundingBoxes = new Dictionary<GameObject, GameObject>(); // Map selected objects to bounding boxes
    public Transform chaireResources;

    private XRIBIMInputActions playerInputActions;
    private ObjectInteractionHandler objectInteractionHandler;
    

    public Vector3 copyPasteOffset = Vector3.zero;
    // Start is called before the first frame update
    private void Start()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
    }

    public void CopyAndPasteObjects()
    {
        copiedObjects.Clear();

        // Copy selected objects
        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {

                Vector3 spawnPosition = obj.transform.position + copyPasteOffset;
                spawnPosition.y = 0;

                NetworkManager.Instance.Runner.Spawn(
                    obj,
                    spawnPosition,
                    obj.transform.rotation,
                    NetworkManager.Instance.Runner.LocalPlayer, // Assign authority to the local player
                    (runner, pasteObj) =>
                    {
                        pasteObj.GetComponent<NetworkObject>().AssignInputAuthority(NetworkManager.Instance.Runner.LocalPlayer);
                        pasteObj.transform.parent = chaireResources;
                        copiedObjects.Add(pasteObj.gameObject);

                        Debug.Log($"Copied: {pasteObj.name}");
                    }
                );
                /*          GameObject copy = Instantiate(obj);
                          copy.transform.position = obj.transform.position + copyPasteOffset; // Offset copied object
                          copy.transform.parent = chaireResources;

                          copiedObjects.Add(copy);

                          Debug.Log($"Copied: {obj.name}");
                */
            }
        }
        //deselect all currently selected objects 

        ObjectInteractionHandler.Instance.DeselectAllObjects();
        // Add copied objects to selected list
        foreach (GameObject copy in copiedObjects)
        {
            //CreateBoundingBox(copy);
            ObjectInteractionHandler.Instance.SelectObjects(copy);
            // selectedObjects.Add(copy);
        }

        Debug.Log("Pasted copied objects");
    }
}
