using UnityEngine;

public class CubeDimensionFinder : MonoBehaviour
{
    public GameObject cube;
    void Start()
    {
        // Get the Renderer component attached to the cube
        Renderer cubeRenderer = cube.GetComponent<Renderer>();

        if (cubeRenderer != null)
        {
            // Get the width (x-axis size) from the bounds of the renderer
            float width = cubeRenderer.bounds.size.x;
            Debug.Log("Width of the cube: " + width);
        }
        if (cubeRenderer != null)
        {
            // Get the width (x-axis size) from the bounds of the renderer
            float height = cubeRenderer.bounds.size.y;
            Debug.Log("height of the cube: " + height);
        }
        if (cubeRenderer != null)
        {
            // Get the width (z-axis size) from the bounds of the renderer
            float depth = cubeRenderer.bounds.size.z;
            Debug.Log("depth of the cube: " + depth);
        }
        else
        {
            Debug.LogError("Renderer component not found on this GameObject.");
        }
    }
}
