using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GrillLines_Rectangle_Mesh : MonoBehaviour
{
    [Header("Rectangle Bounds (local space)")]
    public float minX = -2f;
    public float maxX = 2f;
    public float minZ = -1f;
    public float maxZ = 1f;

    [Header("Spacing between grill lines")]
    public float spacing = 0.25f;

    [Header("Line thickness")]
    public float thickness = 0.02f;

    public Renderer spacingObject;
    void Start()
    {
        spacing = spacingObject.bounds.size.x;
        GenerateMesh();
    }

    void GenerateMesh()
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        // Helper for adding a rectangular quad
        void AddQuad(Vector3 a, Vector3 b)
        {
            Vector3 dir = (b - a).normalized;
            Vector3 perp = Vector3.Cross(dir, Vector3.up) * (thickness * 0.5f);

            int i = verts.Count;

            verts.Add(a + perp);
            verts.Add(a - perp);
            verts.Add(b - perp);
            verts.Add(b + perp);

            tris.Add(i + 0);
            tris.Add(i + 1);
            tris.Add(i + 2);

            tris.Add(i + 2);
            tris.Add(i + 3);
            tris.Add(i + 0);
        }

        // Horizontal lines
        for (float z = minZ; z <= maxZ; z += spacing)
            AddQuad(new Vector3(minX, 0, z), new Vector3(maxX, 0, z));

        // Vertical lines
        for (float x = minX; x <= maxX; x += spacing)
            AddQuad(new Vector3(x, 0, minZ), new Vector3(x, 0, maxZ));

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material.color = Color.red;
    }
}
