using UnityEngine;

public class RotationAxes : MonoBehaviour
{
    private bool isRotating = false;
    private Vector3 rotationAxis;
    private Transform selectedObject;
    private Material originalMaterial;
    public Material highlightMaterial;

    void Start()
    {
        // Create a highlight material to visualize the selection.
      //  highlightMaterial = new Material(Shader.Find("Standard"));
        highlightMaterial.color = Color.yellow;
    }

    void Update()
    {
        HandleMouseInput();
        if (isRotating && selectedObject != null)
        {
            RotateObject(selectedObject);
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform)
                {
                    if (!isRotating)
                    {
                        StartRotation(hit.transform);
                    }
                  /*  else
                    {
                        StopRotation();
                    }*/
                }
            }
        }

        if (Input.GetMouseButtonUp(1) && isRotating)
        {
            StopRotation();
        }
    }

    private void StartRotation(Transform target)
    {
        isRotating = true;
        selectedObject = target;
        rotationAxis = Vector3.zero;

        if (originalMaterial == null)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                originalMaterial = renderer.material;
                renderer.material = highlightMaterial;
            }
        }
    }

    private void StopRotation()
    {
        isRotating = false;
        if (selectedObject != null)
        {
            Renderer renderer = selectedObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = originalMaterial;
            }
        }
        selectedObject = null;
    }

    private void RotateObject(Transform target)
    {
        float rotationSpeed = 100f;
        float rotationX = Input.GetAxis("Mouse X");
        float rotationY = Input.GetAxis("Mouse Y");

        if (Input.GetKey(KeyCode.X))
        {
            target.Rotate(Vector3.right, -rotationY * rotationSpeed * Time.deltaTime, Space.World);
        }
        else if (Input.GetKey(KeyCode.Y))
        {
            target.Rotate(Vector3.up, rotationX * rotationSpeed * Time.deltaTime, Space.World);
        }
        else if (Input.GetKey(KeyCode.Z))
        {
            target.Rotate(Vector3.forward, -rotationY * rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.right);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.up);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}
