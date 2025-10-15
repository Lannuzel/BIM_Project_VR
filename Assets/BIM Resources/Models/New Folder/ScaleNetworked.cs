using Fusion;
using UnityEngine;

public class ScaleNetworked : NetworkBehaviour
{
    [Networked] public Vector3 ScaleFactor { get; set; } = Vector3.one;
   private Vector3 _originalScale { get; set; }  =  Vector3.one;

   

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

    private void ApplyScale()
    {
        transform.localScale = ScaleFactor;
        // Component-wise multiply: original ⊙ factor
      //  transform.localScale = Vector3.Scale(_originalScale, ScaleFactor);
    }
}
