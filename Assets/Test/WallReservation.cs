using UnityEngine;

public class WallReservation : MonoBehaviour
{
    public Renderer wallRenderer;
    public Vector3 reservationCenter = new Vector3(0, 1, 0);
    public Vector3 reservationSize = new Vector3(0.5f, 0.5f, 0.5f);

    private Material wallMaterial;

    void Start()
    {
        if (wallRenderer == null)
        {
            Debug.LogError("Wall Renderer not assigned.");
            return;
        }

        // Create an instance of the wall material
        wallMaterial = wallRenderer.material;
    }

    void Update()
    {
        if (wallMaterial != null)
        {
            wallMaterial.SetVector("_ClipCenter", reservationCenter);
            wallMaterial.SetVector("_ClipSize", reservationSize);
        }
    }

    // Optional method to set reservation programmatically
    public void SetReservation(Vector3 center, Vector3 size)
    {
        reservationCenter = center;
        reservationSize = size;
    }
}
