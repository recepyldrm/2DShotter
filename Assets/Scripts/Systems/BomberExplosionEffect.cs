using UnityEngine;

public class BomberExplosionEffect : MonoBehaviour
{
    public float lifetime = 1f;

    void Start()
    {
        // ObjectPool üzerinden geldiyse geri dönüşünü havuz yönetir.
        if (ObjectPool.Instance != null &&
            ObjectPool.Instance.IsPrefabPooled(gameObject))
        {
            return;
        }

        Destroy(gameObject, lifetime);
    }
}
