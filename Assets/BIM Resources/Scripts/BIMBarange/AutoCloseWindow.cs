using System.Collections;
using UnityEngine;
using Fusion;

public class AutoCloseWindow : NetworkBehaviour
{
    [SerializeField] private float lifeTime = 2f;

    public override void Spawned()
    {
        // Only the state authority decides when to despawn
        if (HasStateAuthority)
        {
            StartCoroutine(DespawnAfterDelay(lifeTime));
        }
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (NetworkManager.Instance.Runner != null && Object != null)
        {
            NetworkManager.Instance.Runner.Despawn(Object);
        }
    }
}
