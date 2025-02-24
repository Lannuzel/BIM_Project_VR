using Fusion;
using UnityEngine;

public class NetworkedLine : NetworkBehaviour
{
    private LineRenderer lineRenderer;

    // Networked properties for synchronized positions
    [Networked] public Vector3 networkedPointA { get; set; }
    [Networked] public Vector3 networkedPointB { get; set; }
    [Networked] public bool isLineActive { get; set; } // Sync visibility of the line
    public Material lineMaterial; // Material for the LineRenderer

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2; // LineRenderer needs two points

        lineRenderer.material = lineMaterial;
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
         lineRenderer.enabled = false; // Hide at start
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            // Set initial positions
            SetLinePositions(Vector3.zero, Vector3.zero, false);
        }
    }

    public void SetLinePositions(Vector3 pA, Vector3 pB, bool active)
    {
        if (HasStateAuthority)
        {
            networkedPointA = pA;
            networkedPointB = pB;
            isLineActive = active;
        }
    }

    public override void Render()
    {
        // Update LineRenderer for all players
        lineRenderer.enabled = isLineActive;
        if (isLineActive)
        {
            lineRenderer.SetPosition(0, networkedPointA);
            lineRenderer.SetPosition(1, networkedPointB);
        }
    }
}
