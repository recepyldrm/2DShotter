using UnityEngine;

public class PooledParticle : MonoBehaviour
{
    [Header("Pool")]
    public string poolKey = GameKeys.ExplosionPool;

    [Header("Süre")]
    public float lifetime = 1f;

    void OnEnable()
    {
        Invoke(
            nameof(ReturnToPool),
            lifetime
        );
    }

    void OnDisable()
    {
        CancelInvoke(
            nameof(ReturnToPool)
        );
    }

    void ReturnToPool()
    {
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.Despawn(
                poolKey,
                gameObject
            );
        }
        else
        {
            Destroy(gameObject);
        }
    }
}