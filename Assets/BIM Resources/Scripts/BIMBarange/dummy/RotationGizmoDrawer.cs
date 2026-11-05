using Fusion;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class RotationGizmoDrawer : NetworkBehaviour
{
    public float radius = 0.5f;   // Radius of the rings
    public int segments = 64;     // Number of segments for smoothness
    public float lineWidth = 4f;  // Line thickness
    public Renderer rend;
    private Material lineMaterial;

    void Awake()
    {
        // Simple unlit colored material for drawing lines
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;

            // Enable alpha blending and disable depth writes
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }

    void OnRenderObject()
    {
       
        if (lineMaterial == null) return;

        lineMaterial.SetPass(0);
   
        Bounds b = rend.bounds;
        Vector3 pos = transform.position;
        Vector3 center = new Vector3(pos.x, b.max.y + 0.25f, pos.z);
        // Draw three colored circles
        DrawCircle(center, Vector3.right, Color.red);    // X-axis
        DrawCircle(center, Vector3.up, Color.green);     // Y-axis
        DrawCircle(center, Vector3.forward, Color.blue); // Z-axis

        
    }

    void DrawCircle(Vector3 center, Vector3 normal, Color color)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);
        Bounds b = rend.bounds;
        radius = 0.3f;
        Quaternion rot = Quaternion.LookRotation(normal);
        Vector3 prevPoint = center + rot * Vector3.right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i * 360f) / segments;
            Vector3 nextPoint = center + rot * (Quaternion.Euler(0, angle, 0) * Vector3.right * radius);
            GL.Vertex(prevPoint);
            GL.Vertex(nextPoint);
            prevPoint = nextPoint;
        }

        GL.End();
    }
}
