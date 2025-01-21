using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    // Reference to the prefab you want to instantiate
    public GameObject myPrefab;

    void Start()
    {
        // Step 1: Create a new empty GameObject named "myObject"
        GameObject myObject = new GameObject("myObject");

        // Step 2: Instantiate the prefab (make sure the prefab is assigned in the Inspector)
        GameObject instantiatedPrefab = Instantiate(myPrefab);

        // Step 3: Make the instantiated prefab a child of the "myObject"
        instantiatedPrefab.transform.SetParent(myObject.transform);

        // Optional: Reset the prefab's position to align with its parent (if needed)
        instantiatedPrefab.transform.localPosition = Vector3.zero;
    }
}