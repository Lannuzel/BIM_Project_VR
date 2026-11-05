using UnityEngine;

public class CollisionTest : MonoBehaviour
{
    private bool hasScaled = false;

    private void Update()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.1f);
        foreach (var hit in hits)
        {
       //     Debug.Log("Overlapping with: " + hit.name);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (hasScaled) return;

        GameObject other = collision.gameObject;

        if (other.CompareTag("Wall") || other.CompareTag("Floor"))
        {
            Renderer surfaceRenderer = other.GetComponent<Renderer>();
            if (surfaceRenderer)
            {
                float surfaceWidth = surfaceRenderer.bounds.size.z;

                transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, surfaceWidth);
                Debug.Log("Hit the collision and scaled.");

                hasScaled = true;
            }
        }
    }
}

