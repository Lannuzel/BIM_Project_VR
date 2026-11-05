
using UnityEngine;
public class UILookAt_V2: MonoBehaviour
{
    public Transform positionCamera;
    public float distance = 1f;
    public float heightOffset = 0.5f; // Height above/below camera
    public bool applyRotation = false;
    // Start is called before the first frame update
    void Start()
    {

    }

    private void OnEnable()
    {
        Vector3 targetPosition = positionCamera.position + positionCamera.forward * distance;

        // Apply height offset
        targetPosition.y += heightOffset;


        transform.position = targetPosition;

        // Make the UI face the camera
        transform.rotation = Quaternion.LookRotation(positionCamera.forward, Vector3.up);
        if (applyRotation)
            transform.RotateAround(transform.position, transform.up, 180f);
    }
    // Update is called once per frame
   /* void LateUpdate()
    {
        Vector3 targetPosition = positionCamera.position + positionCamera.forward * distance;

        // Apply height offset
        targetPosition.y += heightOffset;


        transform.position = targetPosition;
       
        // Make the UI face the camera
        transform.rotation = Quaternion.LookRotation(positionCamera.forward, Vector3.up);
        if (applyRotation )
            transform.RotateAround(transform.position, transform.up, 180f);

    }*/
}
