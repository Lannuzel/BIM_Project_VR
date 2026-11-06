using Fusion;
using UnityEngine;

/// <summary>
/// Échelle réseau d'une réservation. N5 : <see cref="ScaleFactor"/> est un facteur pur (1,1,1 = échelle du prefab),
/// appliqué sur l'échelle d'origine du prefab : une réservation « par défaut » garde donc l'échelle du prefab
/// sur tous les casques au lieu d'être étirée.
/// </summary>
public class ScaleNetworked : NetworkBehaviour
{
    [Networked] public Vector3 ScaleFactor { get; set; } = Vector3.one;
    private Vector3 _originalScale = Vector3.one;

    /// <summary>Échelle du prefab avant application du facteur.</summary>
    public Vector3 OriginalScale => _originalScale;

    private void Awake()
    {
        // Store whatever scale the prefab had in the editor
        _originalScale = transform.localScale;
    }
    public override void Spawned()
    {
        ApplyScale();
    }

    public override void Render()
    {
        // Keeps visuals correct if ScaleFactor ever changes at runtime
        ApplyScale();
    }

    /// <summary>Applique le facteur courant (aussi appelé par Render ; public pour la mise à l'échelle XP3).</summary>
    public void ApplyScale()
    {
        Vector3 factor = ScaleFactor;
        if (factor == Vector3.zero) factor = Vector3.one; // valeur réseau pas encore reçue
        transform.localScale = Vector3.Scale(_originalScale, factor);
    }
}
