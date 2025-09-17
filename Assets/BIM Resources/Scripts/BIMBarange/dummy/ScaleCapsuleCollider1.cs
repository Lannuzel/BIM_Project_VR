using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class ScaleCapsuleCollider1 : MonoBehaviour
{
    private CapsuleCollider capsule;
    private Vector3 initialScale;
    private float initialHeight;
    private float initialRadius;
    private Vector3 lastScale;

    void Awake()
    {
        capsule = GetComponent<CapsuleCollider>();
        initialScale = transform.localScale;
        initialHeight = capsule.height;
        initialRadius = capsule.radius;
        lastScale = transform.localScale;
    }

    void Update()
    {
        if (transform.localScale != lastScale)
        {
            UpdateCollider();
            lastScale = transform.localScale;
        }
    }

    private void UpdateCollider()
    {
        // Scale radius by the average of X and Z
        float scaleXY = (transform.localScale.x + transform.localScale.y) * 0.5f /
                        ((initialScale.x + initialScale.y) * 0.5f);
        capsule.radius = initialRadius * scaleXY;

        // Scale height by Y
        float scaleZ = transform.localScale.z / initialScale.z;
        capsule.height = initialHeight * scaleZ;
    }
}
