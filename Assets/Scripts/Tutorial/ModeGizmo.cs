using UnityEngine;

/// <summary>
/// Repère visuel du mode courant sur l'objet manipulé dans le tutoriel, repris de l'XP2 :
/// flèches X/Y/Z (rouge, vert, bleu) en mode déplacement (<c>TranslateGizmoDrawer</c>),
/// trois anneaux en mode rotation (<c>RotationGizmoDrawer</c>). Même dessin (GL, matériau
/// « Hidden/Internal-Colored »), même placement au-dessus de l'objet, mais sans dépendance réseau :
/// l'objet du tutoriel n'est pas un NetworkObject.
/// Utilisation : <see cref="Show"/> à la sélection, <see cref="Hide"/> au relâchement.
/// </summary>
public class ModeGizmo : MonoBehaviour
{
    public enum Mode { Translate, Rotate }

    [Tooltip("Mode affiché : flèches (déplacement) ou anneaux (rotation).")]
    public Mode mode = Mode.Translate;
    [Tooltip("Longueur des flèches (m), comme TranslateGizmoDrawer.")]
    public float axisLength = 0.5f;
    [Tooltip("Taille des pointes de flèche (m), comme TranslateGizmoDrawer.")]
    public float coneSize = 0.1f;
    [Tooltip("Rayon des anneaux (m), comme RotationGizmoDrawer.")]
    public float ringRadius = 0.3f;
    [Tooltip("Hauteur des anneaux au-dessus de l'objet (m), comme RotationGizmoDrawer.")]
    public float ringHeight = 0.25f;
    [Tooltip("Nombre de segments des anneaux.")]
    public int segments = 64;

    private Renderer rend;
    private Material lineMaterial;

    /// <summary>Affiche (ou met à jour) le repère sur <paramref name="target"/> dans le mode demandé.</summary>
    public static ModeGizmo Show(GameObject target, Mode mode)
    {
        if (target == null) return null;
        var gizmo = target.GetComponent<ModeGizmo>();
        if (gizmo == null) gizmo = target.AddComponent<ModeGizmo>();
        gizmo.mode = mode;
        gizmo.enabled = true;
        return gizmo;
    }

    /// <summary>Masque le repère de <paramref name="target"/> s'il en a un.</summary>
    public static void Hide(GameObject target)
    {
        if (target == null) return;
        var gizmo = target.GetComponent<ModeGizmo>();
        if (gizmo != null) gizmo.enabled = false;
    }

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();

        // Même matériau que les gizmos de l'XP2.
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null) return;
        lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
    }

    private void OnDestroy()
    {
        if (lineMaterial != null) Destroy(lineMaterial);
    }

    private void OnRenderObject()
    {
        if (lineMaterial == null) return;
        lineMaterial.SetPass(0);

        float top = rend != null ? rend.bounds.max.y : transform.position.y;
        Vector3 pos = transform.position;

        if (mode == Mode.Translate)
        {
            // TranslateGizmoDrawer : origine au sommet de l'objet, axes monde.
            Vector3 center = new Vector3(pos.x, top, pos.z);
            DrawArrow(center, Vector3.right * axisLength, Color.red);
            DrawArrow(center, Vector3.up * axisLength, Color.green);
            DrawArrow(center, Vector3.forward * axisLength, Color.blue);
        }
        else
        {
            // RotationGizmoDrawer : anneaux un peu au-dessus de l'objet.
            Vector3 center = new Vector3(pos.x, top + ringHeight, pos.z);
            DrawCircle(center, Vector3.right, Color.red);
            DrawCircle(center, Vector3.up, Color.green);
            DrawCircle(center, Vector3.forward, Color.blue);
        }
    }

    private void DrawArrow(Vector3 origin, Vector3 direction, Color color)
    {
        Vector3 end = origin + direction;

        GL.Begin(GL.LINES);
        GL.Color(color);
        GL.Vertex(origin);
        GL.Vertex(end);
        GL.End();

        Quaternion rot = Quaternion.LookRotation(direction.normalized);
        int coneSegments = 20;
        float radius = coneSize * 0.5f;
        Vector3 coneBase = end - direction.normalized * coneSize;

        GL.Begin(GL.TRIANGLES);
        GL.Color(color);
        for (int i = 0; i < coneSegments; i++)
        {
            float a0 = (i * Mathf.PI * 2f) / coneSegments;
            float a1 = ((i + 1) * Mathf.PI * 2f) / coneSegments;
            Vector3 p0 = coneBase + rot * new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, 0f);
            Vector3 p1 = coneBase + rot * new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0f);
            GL.Vertex(end);
            GL.Vertex(p0);
            GL.Vertex(p1);
        }
        GL.End();
    }

    private void DrawCircle(Vector3 center, Vector3 normal, Color color)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);
        Quaternion rot = Quaternion.LookRotation(normal);
        Vector3 prev = center + rot * Vector3.right * ringRadius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i * 360f) / segments;
            Vector3 next = center + rot * (Quaternion.Euler(0f, angle, 0f) * Vector3.right * ringRadius);
            GL.Vertex(prev);
            GL.Vertex(next);
            prev = next;
        }
        GL.End();
    }
}
