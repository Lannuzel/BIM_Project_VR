using UnityEngine;
using System.Collections.Generic;
using Fusion;

/// <summary>Copier-coller des objets sélectionnés (copies créées sur le réseau, action annulable F19).</summary>
public class CopyPasteObject : Fusion.NetworkBehaviour
{
    private List<GameObject> selectedObjects;// = new List<GameObject>(); // List of selected objects
    private List<GameObject> copiedObjects = new List<GameObject>(); // List of copied objects
    public Transform chaireResources;

    public Vector3 copyPasteOffset = Vector3.zero;
    // Start is called before the first frame update
    private void Start()
    {
        selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();
    }

    public void CopyAndPasteObjects()
    {
        if (selectedObjects == null)
            selectedObjects = ObjectInteractionHandler.Instance.SelectedObjects();

        if (!NetworkObjectUtils.IsRunning)
        {
            NetworkStatusPanel.Show("Session réseau inactive : copie impossible.", 3f);
            return;
        }

        copiedObjects.Clear();
        var runner = NetworkObjectUtils.Runner;

        // Copy selected objects
        foreach (GameObject obj in new List<GameObject>(selectedObjects))
        {
            if (obj == null) continue;

            var source = obj.GetComponent<NetworkObject>();
            Vector3 spawnPosition = obj.transform.position + copyPasteOffset;
            spawnPosition.y = ObjectInteractionHandler.Instance.HeightFor(obj); // XP1 : 0 (sol) ; XP3 : hauteur plafond selon l'objet

            NetworkObject pasted;
            if (source != null && source.IsValid && source.NetworkTypeId.IsPrefab)
            {
                // Copie depuis le prefab d'origine (fonctionne aussi pour un objet créé par un autre casque)
                pasted = runner.Spawn(source.NetworkTypeId.AsPrefabId, spawnPosition, obj.transform.rotation, runner.LocalPlayer, (r, o) =>
                {
                    var srcScale = obj.GetComponent<ScaleNetworked>();
                    var dstScale = o.GetComponent<ScaleNetworked>();
                    if (srcScale != null && dstScale != null) dstScale.ScaleFactor = srcScale.ScaleFactor;
                });
            }
            else
            {
                pasted = runner.Spawn(obj, spawnPosition, obj.transform.rotation, runner.LocalPlayer);
            }

            if (pasted == null) continue;
            pasted.transform.parent = chaireResources;
            copiedObjects.Add(pasted.gameObject);
            Debug.Log($"Copied: {pasted.name}");
        }

        //deselect all currently selected objects
        ObjectInteractionHandler.Instance.DeselectAllObjects();
        // Add copied objects to selected list
        foreach (GameObject copy in copiedObjects)
        {
            ObjectInteractionHandler.Instance.SelectObjects(copy);
        }

        UndoManager.RecordSpawn(copiedObjects.Count > 1 ? $"Coller {copiedObjects.Count} objets" : "Coller un objet", copiedObjects.ToArray());
        Debug.Log("Pasted copied objects");
    }
}
