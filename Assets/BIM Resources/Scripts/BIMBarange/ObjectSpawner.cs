using UnityEngine;
using Fusion;
public class ObjectSpawner : SimulationBehaviour
{
    public OVRInput.Controller controller; // Assign your controller here
    public GameObject objectPrefab; // The prefab to spawn
    public Transform cameraTransform; // Player's camera transform
    public float spawnDistance = 2.0f; // Distance at which the object spawns

    private GameObject spawnedObject;
    private bool isObjectSelected = false;

    public Transform chaireResources;

    void Update()
    {
        // Check if button A is pressed to spawn an object
        if (OVRInput.GetDown(OVRInput.Button.One) && spawnedObject == null)
        {
            SpawnObject();
        }

        // Check if the trigger is pressed to enable movement of the object
       else if (spawnedObject != null)
        {            
            MoveObject();
        }

        if (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger) && (isObjectSelected))
        {
            isObjectSelected = false;
            ReleaseObject();
        }

    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || OVRInput.GetDown(OVRInput.Button.Two))
        {
            NetworkManager.Instance.Runner.Spawn(objectPrefab, new Vector3(0, 1, 0), Quaternion.identity);
        }

    }
    void SpawnObject()
    {
        Vector3 spawnPosition = cameraTransform.transform.position + cameraTransform.forward * spawnDistance;
        spawnPosition.y = 0;
        spawnedObject = Instantiate(objectPrefab, spawnPosition, Quaternion.identity);
        spawnedObject.transform.rotation = objectPrefab.transform.rotation;
        spawnedObject.transform.parent = chaireResources;
        isObjectSelected = true;
    }

    void MoveObject()
    {
        if (spawnedObject == null) return;

        Vector2 joystickInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        Vector3 moveDirection = new Vector3(joystickInput.x, 0, joystickInput.y);
      

        // Constrain movement to X and Z directions
        moveDirection.y = 0;

        Vector3 direction  = cameraTransform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.z)) * Time.deltaTime;
        direction.y = 0;
        spawnedObject.transform.position += direction;
    }

    void ReleaseObject()
    {
        // Release the object; additional logic can be added here if needed
        Debug.Log("Object released");
        spawnedObject = null;
    }
}
