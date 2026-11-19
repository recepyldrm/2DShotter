using UnityEngine;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    // ==================================
    // MERMİ
    // ==================================

    [Header("Mermi")]
    public float speed = 12f;
    public int damage = 25;
    public float lifetime = 3f;

    // ==================================
    // EXPLOSION
    // ==================================

    [Header("Patlama Efekti")]
    public string explosionPoolKey = GameKeys.ExplosionPool;

    // ==================================
    // PIERCING
    // ==================================

    private int remainingPiercing = 0;

    // ==================================
    // MULTI SHOT
    // ==================================

    // WeaponController her atışta ayarlar: 1 + ek mermi sayısı.
    private int shotMultiplier = 1;

    public void SetShotMultiplier(int multiplier)
    {
        shotMultiplier = Mathf.Max(1, multiplier);
    }

    // ==================================
    // ORTAK BUFFER'LAR
    // ==================================

    // Her isabette yeni dizi / HashSet oluşturmamak için paylaşılır.
    // Oyun tek thread'de çalıştığı için güvenli.
    private static readonly List<Collider2D> overlapResults = new List<Collider2D>();
    private static readonly HashSet<EnemyHealth> hitEnemiesBuffer = new HashSet<EnemyHealth>();

    // ==================================
    // ENABLE
    // ==================================

    void OnEnable()
    {
        remainingPiercing = 0;

        // Havuzdan gelen mermide önceki atışın değeri kalmasın.
        shotMultiplier = 1;

        if (PlayerStats.Instance != null)
        {
            remainingPiercing =
                PlayerStats.Instance.piercing;
        }

        Invoke(
            nameof(ReturnToPool),
            lifetime
        );
    }

    // ==================================
    // DISABLE
    // ==================================

    void OnDisable()
    {
        CancelInvoke(
            nameof(ReturnToPool)
        );
    }

    // ==================================
    // UPDATE
    // ==================================

    void Update()
    {
        float finalSpeed = speed;

        if (PlayerStats.Instance != null)
        {
            finalSpeed *=
                PlayerStats.Instance
                    .bulletSpeedMultiplier;
        }

        transform.Translate(
            Vector3.right *
            finalSpeed *
            Time.deltaTime,
            Space.World
        );
    }

    // ==================================
    // COLLISION
    // ==================================

    void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth enemyHealth =
            GetEnemyHealth(other);

        if (enemyHealth == null)
            return;

        // ==================================
        // DAMAGE MULTIPLIER
        // ==================================

        float multiplier = 1f;

        if (CompanionController.Instance != null)
        {
            multiplier *=
                CompanionController.Instance
                    .DamageMultiplier;
        }

        if (PlayerStats.Instance != null)
        {
            multiplier *=
                PlayerStats.Instance
                    .damageMultiplier;
        }

        // Shop'tan alınan silah hasarı
        if (UpgradeManager.Instance != null)
        {
            multiplier *=
                UpgradeManager.Instance
                    .WeaponDamageMultiplier;
        }

        // Multi Shot: ek mermiler ayrı görünmez, hasar olarak eklenir
        multiplier *= shotMultiplier;

        int finalDamage =
            Mathf.RoundToInt(
                damage *
                multiplier
            );

        // ==================================
        // CRITICAL
        // ==================================

        bool isCritical = false;

        if (PlayerStats.Instance != null)
        {
            // "<" kullanılıyor: criticalChance 0 iken asla kritik olmaz.
            if (
                Random.value <
                PlayerStats.Instance
                    .criticalChance
            )
            {
                isCritical = true;

                finalDamage =
                    Mathf.RoundToInt(
                        finalDamage *
                        PlayerStats.Instance
                            .criticalDamageMultiplier
                    );
            }
        }

        // ==================================
        // NORMAL DAMAGE
        // ==================================

        enemyHealth.TakeDamage(
            finalDamage,
            isCritical ? DamageNumbers.Style.Critical : DamageNumbers.Style.Normal
        );

        // ==================================
        // BURN BULLET
        // ==================================

        if (
            PlayerStats.Instance != null &&
            PlayerStats.Instance.burnBullets
        )
        {
            enemyHealth.ApplyBurn(
                PlayerStats.Instance.burnDamage,
                PlayerStats.Instance.burnDuration,
                PlayerStats.Instance.burnTickRate
            );
        }

        // ==================================
        // FREEZE BULLET
        // ==================================

        if (
            PlayerStats.Instance != null &&
            PlayerStats.Instance.freezeBullets
        )
        {
            EnemyMovement enemyMovement =
                enemyHealth.GetComponent<EnemyMovement>();

            if (enemyMovement == null)
            {
                enemyMovement =
                    enemyHealth
                        .GetComponentInParent<EnemyMovement>();
            }

            if (enemyMovement != null)
            {
                enemyMovement.ApplyFreeze(
                    PlayerStats.Instance.freezeSlowAmount,
                    PlayerStats.Instance.freezeDuration
                );
            }
        }

        // ==================================
        // EXPLOSIVE BULLET
        // ==================================

        if (
            PlayerStats.Instance != null &&
            PlayerStats.Instance.explosiveBullets
        )
        {
            Explode(
                other.transform.position,
                finalDamage,
                enemyHealth
            );
        }

        // ==================================
        // CHAIN LIGHTNING
        // ==================================

        if (
            PlayerStats.Instance != null &&
            PlayerStats.Instance.chainLightning
        )
        {
            ChainLightning(
                enemyHealth,
                finalDamage
            );
        }

        // ==================================
        // CRITICAL LOG
        // ==================================

        if (isCritical)
        {
            GameLog.Log(
                "CRITICAL HIT! Damage: " +
                finalDamage
            );
        }

        // ==================================
        // PIERCING
        // ==================================

        if (remainingPiercing > 0)
        {
            remainingPiercing--;
            return;
        }

        ReturnToPool();
    }

    // ==================================
    // ENEMY HEALTH BUL
    // ==================================

    static EnemyHealth GetEnemyHealth(Collider2D collider)
    {
        EnemyHealth enemy =
            collider.GetComponent<EnemyHealth>();

        if (enemy == null)
        {
            enemy =
                collider.GetComponentInParent<EnemyHealth>();
        }

        return enemy;
    }

    // ==================================
    // ALAN SORGUSU
    // ==================================

    static void OverlapCircle(
        Vector2 center,
        float radius
    )
    {
        ContactFilter2D filter =
            ContactFilter2D.noFilter;

        filter.useTriggers =
            Physics2D.queriesHitTriggers;

        Physics2D.OverlapCircle(
            center,
            radius,
            filter,
            overlapResults
        );
    }

    // ==================================
    // CHAIN LIGHTNING
    // ==================================

    void ChainLightning(
        EnemyHealth firstTarget,
        int mainDamage
    )
    {
        if (PlayerStats.Instance == null)
            return;

        int maxChains =
            PlayerStats.Instance.chainCount;

        float range =
            PlayerStats.Instance.chainRange;

        int chainDamage =
            Mathf.RoundToInt(
                mainDamage *
                PlayerStats.Instance
                    .chainDamageMultiplier
            );

        if (maxChains <= 0)
            return;

        if (chainDamage <= 0)
            return;

        // Aynı düşmana tekrar yıldırım
        // gitmesini engeller.
        HashSet<EnemyHealth> hitEnemies =
            hitEnemiesBuffer;

        hitEnemies.Clear();

        hitEnemies.Add(
            firstTarget
        );

        EnemyHealth currentTarget =
            firstTarget;

        // ==================================
        // ZİNCİR
        // ==================================

        for (int i = 0; i < maxChains; i++)
        {
            EnemyHealth nextTarget =
                FindNearestEnemy(
                    currentTarget.transform.position,
                    range,
                    hitEnemies
                );

            // Yakında başka düşman yok.
            if (nextTarget == null)
                break;

            hitEnemies.Add(
                nextTarget
            );

            // ==================================
            // POZİSYONLARI KAYDET
            // ==================================

            Vector3 startPosition =
                currentTarget.transform.position;

            Vector3 endPosition =
                nextTarget.transform.position;

            // ==================================
            // CHAIN DAMAGE
            // ==================================

            nextTarget.TakeDamage(
                chainDamage,
                DamageNumbers.Style.Chain
            );

            // Yıldırım çizgisi
            CreateChainEffect(
                startPosition,
                endPosition
            );

            // Hedefte elektrik patlaması
            CreateChainHitEffect(
                endPosition
            );

            GameLog.Log(
                "⚡ CHAIN LIGHTNING → " +
                nextTarget.gameObject.name +
                " | Damage: " +
                chainDamage
            );

            // Sonraki yıldırım buradan
            // devam edecek.
            currentTarget =
                nextTarget;
        }

        hitEnemies.Clear();
    }

    // ==================================
    // CHAIN LIGHTNING EFFECT
    // ==================================

    void CreateChainEffect(
        Vector3 startPosition,
        Vector3 endPosition
    )
    {
        if (ObjectPool.Instance == null)
        {
            Debug.LogWarning(
                "ObjectPool bulunamadı!"
            );

            return;
        }

        GameObject effect =
            ObjectPool.Instance.Spawn(
                GameKeys.ChainLightningPool,
                Vector3.zero,
                Quaternion.identity
            );

        if (effect == null)
            return;

        ChainLightningEffect lightning =
            effect.GetComponent<ChainLightningEffect>();

        if (lightning == null)
        {
            Debug.LogError(
                "ChainLightning prefabında " +
                "ChainLightningEffect scripti yok!"
            );

            ObjectPool.Instance.Despawn(
                GameKeys.ChainLightningPool,
                effect
            );

            return;
        }

        lightning.Play(
            startPosition,
            endPosition
        );
    }

    // ==================================
    // CHAIN HIT EFFECT
    // ==================================

    void CreateChainHitEffect(
        Vector3 position
    )
    {
        if (ObjectPool.Instance == null)
            return;

        ObjectPool.Instance.Spawn(
            GameKeys.ChainHitPool,
            position,
            Quaternion.identity
        );
    }

    // ==================================
    // EN YAKIN DÜŞMANI BUL
    // ==================================

    EnemyHealth FindNearestEnemy(
        Vector3 center,
        float range,
        HashSet<EnemyHealth> ignoredEnemies
    )
    {
        OverlapCircle(
            center,
            range
        );

        EnemyHealth nearestEnemy = null;

        float nearestDistance =
            Mathf.Infinity;

        foreach (Collider2D hit in overlapResults)
        {
            EnemyHealth enemy =
                GetEnemyHealth(hit);

            if (enemy == null)
                continue;

            // Daha önce vurulan düşmana
            // tekrar sıçrama.
            if (
                ignoredEnemies.Contains(
                    enemy
                )
            )
            {
                continue;
            }

            float distance =
                Vector2.Distance(
                    center,
                    enemy.transform.position
                );

            if (distance < nearestDistance)
            {
                nearestDistance =
                    distance;

                nearestEnemy =
                    enemy;
            }
        }

        return nearestEnemy;
    }

    // ==================================
    // EXPLOSION
    // ==================================

    void Explode(
        Vector3 explosionPosition,
        int mainDamage,
        EnemyHealth mainTarget
    )
    {
        if (PlayerStats.Instance == null)
            return;

        // ==================================
        // EXPLOSION EFFECT
        // ==================================

        if (
            ObjectPool.Instance != null &&
            !string.IsNullOrEmpty(
                explosionPoolKey
            )
        )
        {
            ObjectPool.Instance.Spawn(
                explosionPoolKey,
                explosionPosition,
                Quaternion.identity
            );
        }

        // ==================================
        // EXPLOSION DAMAGE
        // ==================================

        float radius =
            PlayerStats.Instance
                .explosionRadius;

        // Parlama + hasar yarıçapı kadar büyüyen şok dalgası
        ExplosionBlast.Play(explosionPosition, radius);

        float explosionMultiplier =
            PlayerStats.Instance
                .explosionDamageMultiplier;

        int explosionDamage =
            Mathf.RoundToInt(
                mainDamage *
                explosionMultiplier
            );

        OverlapCircle(
            explosionPosition,
            radius
        );

        // Aynı düşmanın birden fazla
        // collider'ı olabilir.
        HashSet<EnemyHealth> damagedEnemies =
            hitEnemiesBuffer;

        damagedEnemies.Clear();

        // Ana hedef zaten normal
        // mermi hasarını aldı.
        damagedEnemies.Add(
            mainTarget
        );

        foreach (Collider2D hit in overlapResults)
        {
            EnemyHealth enemy =
                GetEnemyHealth(hit);

            if (enemy == null)
                continue;

            if (!damagedEnemies.Add(enemy))
                continue;

            enemy.TakeDamage(
                explosionDamage,
                DamageNumbers.Style.Explosion
            );
        }

        damagedEnemies.Clear();
    }

    // ==================================
    // RETURN TO POOL
    // ==================================

    void ReturnToPool()
    {
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.Despawn(
                GameKeys.BulletPool,
                gameObject
            );
        }
        else
        {
            Destroy(
                gameObject
            );
        }
    }
}
