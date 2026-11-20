using System.Collections.Generic;
using UnityEngine;

// Nişancı düşmanların attığı mermi. Prefab gerekmez; kendi küçük havuzunu kurar.
// Atıldığı andaki oyuncu konumuna düz gider, oyuncuya değince hasar verir.
public class EnemyProjectile : MonoBehaviour
{
    private const float Speed = 6f;
    private const float Lifetime = 4f;
    private const float HitRadius = 0.15f;
    private const float Size = 0.28f;

    private static readonly Color CoreColor = new Color(1f, 0.85f, 0.75f, 1f);
    private static readonly Color GlowColor = new Color(1f, 0.2f, 0.15f, 0.7f);

    private static Transform root;
    private static readonly List<EnemyProjectile> pool = new List<EnemyProjectile>();

    private static PlayerHealth playerHealth;
    private static Collider2D playerCollider;

    private Vector3 velocity;
    private int damage;
    private float age;

    public static void Fire(Vector3 origin, Transform target, int damage)
    {
        if (target == null)
            return;

        EnsurePlayer(target);

        Vector3 aim = playerCollider != null ? playerCollider.bounds.center : target.position;
        Vector3 direction = aim - origin;
        direction.z = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.left;

        EnemyProjectile projectile = Get();
        projectile.transform.position = new Vector3(origin.x, origin.y, 0f);
        projectile.velocity = direction.normalized * Speed;
        projectile.damage = damage;
        projectile.age = 0f;
        projectile.gameObject.SetActive(true);
    }

    // Belirli bir yöne atış (boss yaylımı). target: hasar alacak oyuncu.
    public static void FireDirection(Vector3 origin, Vector3 direction, Transform target, int damage)
    {
        if (target == null)
            return;

        EnsurePlayer(target);

        direction.z = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.left;

        EnemyProjectile projectile = Get();
        projectile.transform.position = new Vector3(origin.x, origin.y, 0f);
        projectile.velocity = direction.normalized * Speed * 0.8f;
        projectile.damage = damage;
        projectile.age = 0f;
        projectile.gameObject.SetActive(true);
    }

    static void EnsurePlayer(Transform target)
    {
        if (playerHealth != null && playerHealth.transform == target)
            return;

        playerHealth = target.GetComponent<PlayerHealth>();
        playerCollider = null;

        foreach (Collider2D candidate in target.GetComponents<Collider2D>())
        {
            if (playerCollider == null || !candidate.isTrigger)
                playerCollider = candidate;
        }
    }

    static EnemyProjectile Get()
    {
        // Sahne değişince eski kök silinir; havuz da sıfırlanır.
        if (root == null)
        {
            root = new GameObject("EnemyProjectiles").transform;
            pool.Clear();
        }

        foreach (EnemyProjectile candidate in pool)
        {
            if (candidate != null && !candidate.gameObject.activeSelf)
                return candidate;
        }

        EnemyProjectile created = Create();
        pool.Add(created);
        return created;
    }

    static EnemyProjectile Create()
    {
        GameObject go = new GameObject("EnemyProjectile");
        go.transform.SetParent(root, false);

        UITheme theme = UITheme.Load();
        Material material = theme != null ? theme.unlitEffectMaterial : null;

        // Dış hale
        SpriteRenderer glow = go.AddComponent<SpriteRenderer>();
        glow.sprite = RuntimeSprites.SoftCircle;
        glow.color = GlowColor;
        glow.sortingOrder = 58;
        if (material != null) glow.sharedMaterial = material;
        go.transform.localScale = Vector3.one * Size * 2.4f;

        // Parlak çekirdek
        GameObject core = new GameObject("Core");
        core.transform.SetParent(go.transform, false);
        core.transform.localScale = Vector3.one * 0.42f;

        SpriteRenderer coreRenderer = core.AddComponent<SpriteRenderer>();
        coreRenderer.sprite = RuntimeSprites.SolidCircle;
        coreRenderer.color = CoreColor;
        coreRenderer.sortingOrder = 59;
        if (material != null) coreRenderer.sharedMaterial = material;

        EnemyProjectile projectile = go.AddComponent<EnemyProjectile>();
        go.SetActive(false);
        return projectile;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        age += dt;

        if (age >= Lifetime || playerHealth == null)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.position += velocity * dt;

        // Hafif titreşen parıltı
        float pulse = 1f + Mathf.Sin(age * 25f) * 0.08f;
        transform.localScale = Vector3.one * Size * 2.4f * pulse;

        if (playerCollider != null && playerCollider.enabled)
        {
            if (playerCollider.bounds.SqrDistance(transform.position) <= HitRadius * HitRadius)
            {
                playerHealth.TakeDamage(damage);
                ExplosionBlast.Play(transform.position, 0.35f, CoreColor, GlowColor);
                gameObject.SetActive(false);
            }
        }
    }
}
