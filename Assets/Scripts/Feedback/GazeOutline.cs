using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contour d'un objet pour le feedback de regard partagé. Réutilise les matériaux du contour de sélection
/// (Resources/Materials/OutlineMask et OutlineFill, shader QuickOutline déjà dans le projet) avec des instances
/// propres à l'objet : couleur et opacité réglables sans toucher au contour de sélection du Modélisateur
/// (la copie QuickOutline de la branche feedback n'est pas reprise).
/// </summary>
public class GazeOutline : MonoBehaviour
{
    private static Material maskSource, fillSource;

    private Material mask, fill;
    private readonly List<Renderer> applied = new List<Renderer>();
    public bool IsShown => applied.Count > 0;

    public static GazeOutline Get(GameObject go)
    {
        var o = go.GetComponent<GazeOutline>();
        return o != null ? o : go.AddComponent<GazeOutline>();
    }

    private void Awake()
    {
        if (maskSource == null) maskSource = Resources.Load<Material>("Materials/OutlineMask");
        if (fillSource == null) fillSource = Resources.Load<Material>("Materials/OutlineFill");
        if (maskSource == null || fillSource == null)
        {
            Debug.LogWarning("[GazeOutline] Matériaux Resources/Materials/OutlineMask ou OutlineFill introuvables.");
            return;
        }
        mask = new Material(maskSource) { name = "GazeOutlineMask (Instance)" };
        fill = new Material(fillSource) { name = "GazeOutlineFill (Instance)" };
        mask.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        fill.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        fill.SetFloat("_OutlineWidth", 10f); // largeur de la version publiée
    }

    /// <summary>Affiche le contour (ajout des matériaux si besoin) avec la couleur et l'opacité données.</summary>
    public void Show(Color color, float opacity)
    {
        if (fill == null) return;
        if (applied.Count == 0)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                var mats = new List<Material>(r.sharedMaterials);
                if (mats.Contains(mask)) continue;
                mats.Add(mask);
                mats.Add(fill);
                r.materials = mats.ToArray();
                applied.Add(r);
            }
        }
        color.a = Mathf.Clamp01(opacity);
        fill.SetColor("_OutlineColor", color);
    }

    public void Hide()
    {
        foreach (var r in applied)
        {
            if (r == null) continue;
            var mats = new List<Material>(r.sharedMaterials);
            mats.RemoveAll(m => m == mask || m == fill);
            r.materials = mats.ToArray();
        }
        applied.Clear();
    }

    private void OnDestroy()
    {
        Hide();
        if (mask != null) Destroy(mask);
        if (fill != null) Destroy(fill);
    }
}
