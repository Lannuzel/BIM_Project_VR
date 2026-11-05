using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Fusion;

/// <summary>
/// Alignement des objets sélectionnés sur le premier (référence).
/// N5 : « Aligner X » aligne les objets le long de l'axe X (même Z que la référence) et « Aligner Z »
/// le long de l'axe Z (même X), conformément aux libellés des boutons ; prise d'autorité réseau si besoin ;
/// action annulable (F19).
/// </summary>
public class AlignObjects : MonoBehaviour
{
    public List<GameObject> selectedObjects;     // List of selected objects
    public bool alignToZAxis = true; // true : rangée le long de Z (même X), false : rangée le long de X (même Z)


    private void Start()
    {
       selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();

    }
    private void Awake()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
    }
    public void AlignObjectsToReferenceAxisZ()
    {
        alignToZAxis = true;
        AlignObjectsToReference();

    }
    public void AlignObjectsToReferenceAxisX()
    {
        alignToZAxis = false;
        AlignObjectsToReference();
    }
   public void AlignObjectsToReference()
    {
        if (selectedObjects == null || selectedObjects.Count < 2)
        {
            Debug.LogError("Please add at least two objects to the list.");
            NetworkStatusPanel.Show("Sélectionnez au moins deux objets (le premier sert de référence).", 3f);
            return;
        }

        // Get the reference object (the first object in the list)
        GameObject referenceObject = selectedObjects[0];
        Vector3 referencePosition = referenceObject.transform.position;

        var before = UndoManager.CapturePoses(selectedObjects);

        // Iterate through the rest of the objects and align them
        for (int i = 1; i < selectedObjects.Count; i++)
        {
            var obj = selectedObjects[i];
            if (obj == null) continue;

            Vector3 newPosition = obj.transform.position;
            if (alignToZAxis)
                newPosition.x = referencePosition.x; // rangée le long de Z : même X que la référence
            else
                newPosition.z = referencePosition.z; // rangée le long de X : même Z que la référence

            NetworkObjectUtils.EnsureAuthority(obj, ok =>
            {
                if (ok) NetworkObjectUtils.Teleport(obj, newPosition, obj.transform.rotation);
                else Debug.LogWarning($"Cannot move {obj.name}. No State Authority!");
            });
        }

        UndoManager.RecordPoseChange(alignToZAxis ? "Aligner sur Z" : "Aligner sur X", before, true);
        Debug.Log("Objects aligned successfully.");
    }
}
