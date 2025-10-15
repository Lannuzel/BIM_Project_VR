using Fusion;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class TranslateGizmoDrawer : NetworkBehaviour
{
    public float axisLength = 0.5f;     // Length of each arrow
    public float coneSize = 0.1f;     // Size of arrowhead cones
    public float lineWidth = 1f;
    public Renderer rend;
    private Material lineMaterial;
    private float posY;
    void Awake()
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;

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
        // Compute center point: (pos.x, bounds.max.y, pos.z)
        Bounds b = rend.bounds;
        Vector3 pos = transform.position;
        Vector3 center = new Vector3(pos.x, b.max.y, pos.z);

        DrawArrow(center, Vector3.right * axisLength, Color.red);    // X (always horizontal)
        DrawArrow(center, Vector3.up * axisLength, Color.green);     // Y (always vertical)
        DrawArrow(center, Vector3.forward * axisLength, Color.blue); // 
    }

    void DrawArrow(Vector3 origin, Vector3 direction, Color color)
    {
        Vector3 end = origin + direction;

        // Draw line
        GL.Begin(GL.LINES);
        GL.Color(color);
        GL.Vertex(origin);
        GL.Vertex(end);
        GL.End();

        // Draw cone arrowhead
        Quaternion rot = Quaternion.LookRotation(direction.normalized);
        int segments = 20;
        float radius = coneSize * 0.5f;
        Vector3 coneBase = end - direction.normalized * coneSize;

        GL.Begin(GL.TRIANGLES);
        GL.Color(color);

        for (int i = 0; i < segments; i++)
        {
            float angle0 = (i * Mathf.PI * 2f) / segments;
            float angle1 = ((i + 1) * Mathf.PI * 2f) / segments;

            Vector3 p0 = coneBase + rot * new Vector3(Mathf.Cos(angle0) * radius, Mathf.Sin(angle0) * radius, 0);
            Vector3 p1 = coneBase + rot * new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);

            GL.Vertex(end);
            GL.Vertex(p0);
            GL.Vertex(p1);
        }

        GL.End();
    }
}

