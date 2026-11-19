using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("XP")]
    public int xpReward = 20;

    [Header("Hit Flash")]
    public Color flashColor = Color.white;
    public float flashDuration = 0.08f;

    [Tooltip("Yanan düşmanın rengi (siyah silüetlerde alevin fark edilmesi için).")]
    public Color burnColor = new Color(1f, 0.45f, 0.1f, 1f);

    [Header("Ölüm Efekti")]
    public GameObject deathParticlePrefab;

    [Header("Can - BASE DEĞER")]
    public int maxHealth = 100;

    [Header("Puan")]
    public int scoreValue = 10;

    [Header("Ölüm Altını")]
    public int deathGoldMin = 1;
    public int deathGoldMax = 3;

    [Range(0f, 1f)]
    public float deathGoldChance = 0.5f;

    [Header("Pool")]
    public string poolKey;

    [Header("Burn Effect")]
    public GameObject burnEffectPrefab;

    // ==================================
    // HEALTH
    // ==================================

    private int currentHealth;
    private int scaledMaxHealth;

    // ==================================
    // COMPONENTS
    // ==================================

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D enemyCollider;

    private EnemyType enemyType;
    private EnemyMovement enemyMovement;
    private PlayerHealth playerHealth;

    // ==================================
    // STATE
    // ==================================

    private bool isDead = false;

    // ==================================
    // BOSS
    // ==================================

    // BossWaveManager bir düşmanı boss yapınca can ve ödüller bu oranlarla katlanır.
    private float bossHealthMultiplier = 1f;
    private float bossRewardMultiplier = 1f;

    public bool IsBoss => bossHealthMultiplier > 1f;

    // Ölünce haber verir (boss can barı / boss yönetimi için).
    public event System.Action<EnemyHealth> OnDied;

    // Havuzdan değil Instantiate ile oluşturulmuş bir düşmanı boss yapar.
    // Boss havuza geri dönmez, ölünce yok edilir.
    public void ConfigureAsBoss(float healthMultiplier, float rewardMultiplier)
    {
        bossHealthMultiplier = Mathf.Max(1f, healthMultiplier);
        bossRewardMultiplier = Mathf.Max(1f, rewardMultiplier);
        poolKey = null;

        ApplyHealthScaling();
    }

    // ==================================
    // HIT FLASH
    // ==================================

    private Color originalColor;
    private Coroutine flashCoroutine;

    // ==================================
    // BURN
    // ==================================

    private Coroutine burnCoroutine;
    private GameObject activeBurnEffect;

    // Burn zaten aktifken gelen yeni isabetler coroutine'i baştan
    // başlatmaz, sadece bu değerleri günceller. Böylece sürekli ateş
    // altındaki düşman da tick hasarı alır.
    private int burnDamagePerTick;
    private float burnTickRate;
    private float burnTimeRemaining;

    // ==================================
    // AWAKE
    // ==================================

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();

        enemyType = GetComponent<EnemyType>();
        enemyMovement = GetComponent<EnemyMovement>();

        FindPlayer();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (animator == null)
        {
            Debug.LogError(
                gameObject.name +
                " üzerinde Animator bulunamadı!"
            );
        }
    }

    // ==================================
    // PLAYER BUL
    // ==================================

    void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerHealth =
                playerObject.GetComponent<PlayerHealth>();
        }
    }

    // ==================================
    // ON ENABLE
    // ==================================

    void OnEnable()
    {
        CancelInvoke();

        isDead = false;

        // Coroutine temizliği
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            burnCoroutine = null;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        // Pool'dan tekrar gelen enemy üzerinde
        // eski burn referansı kalmasın.
        CleanupBurnReferenceOnEnable();

        // Player değişmiş / yeniden oluşmuş olabilir.
        if (playerHealth == null)
        {
            FindPlayer();
        }

        // Can değerini yeniden hesapla.
        ApplyHealthScaling();

        // Sprite rengini sıfırla.
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // Collider tekrar aç.
        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        // Rigidbody tekrar aktif duruma getir.
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Hareket scriptini tekrar aç.
        if (enemyMovement != null)
        {
            enemyMovement.enabled = true;
        }

        // Animator reset.
        if (animator != null)
        {
            animator.ResetTrigger("Die");
            animator.ResetTrigger("Hurt");
            animator.ResetTrigger("Attack");

            animator.SetBool(
                "isRunning",
                true
            );
        }
    }

    // ==================================
    // ON DISABLE
    // ==================================

    void OnDisable()
    {
        CancelInvoke();

        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            burnCoroutine = null;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        /*
         * Burada StopBurnEffect() çağırmıyoruz.
         *
         * Çünkü Enemy tam SetActive(false) olurken
         * child BurnEffect'in parent'ını değiştirmek
         * Unity'de:
         *
         * "Cannot set the parent..."
         *
         * hatasına neden olabilir.
         *
         * Normal ölüm sırasında BurnEffect zaten
         * Die() içerisinde pool'a gönderiliyor.
         */

        activeBurnEffect = null;
    }

    // ==================================
    // ON ENABLE BURN TEMİZLİĞİ
    // ==================================

    void CleanupBurnReferenceOnEnable()
    {
        /*
         * Normal durumda Die() zaten BurnEffect'i
         * pool'a göndermiş olur.
         *
         * Bu kısım sadece eski referansın yeni
         * spawn'a taşınmasını engeller.
         */

        activeBurnEffect = null;
    }

    // ==================================
    // HEALTH SCALING
    // ==================================

    void ApplyHealthScaling()
    {
        float globalHealthMultiplier = 1f;
        float typeHealthMultiplier = 1f;

        // Global difficulty
        if (EnemyScalingManager.Instance != null)
        {
            globalHealthMultiplier =
                EnemyScalingManager.Instance.HealthMultiplier;
        }

        // Shop'tan silah hasarı alındıkça enemy'ler de güçlenir
        if (UpgradeManager.Instance != null)
        {
            globalHealthMultiplier *=
                UpgradeManager.Instance.EnemyHealthMultiplier;
        }

        // Enemy type
        if (enemyType != null)
        {
            typeHealthMultiplier =
                enemyType.healthMultiplier;
        }

        scaledMaxHealth = Mathf.Max(
            1,
            Mathf.RoundToInt(
                maxHealth *
                typeHealthMultiplier *
                globalHealthMultiplier *
                bossHealthMultiplier
            )
        );

        currentHealth = scaledMaxHealth;

        GameLog.Log(
            gameObject.name +
            " | HP: " +
            scaledMaxHealth
        );
    }

    // ==================================
    // NORMAL DAMAGE
    // ==================================

    public void TakeDamage(int damage, DamageNumbers.Style style = DamageNumbers.Style.Normal)
    {
        if (isDead)
            return;

        damage = Mathf.Max(
            0,
            damage
        );

        currentHealth -= damage;

        DamageNumbers.Show(GetDamageNumberPosition(), damage, style);

        GameLog.Log(
            gameObject.name +
            " Hasar: " +
            damage +
            " | Kalan HP: " +
            currentHealth +
            "/" +
            scaledMaxHealth
        );

        // Hit flash
        if (spriteRenderer != null)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }

            flashCoroutine =
                StartCoroutine(
                    FlashRoutine()
                );
        }

        // Hit sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyHit();
        }

        // Hurt animation
        if (animator != null &&
            currentHealth > 0)
        {
            animator.SetTrigger("Hurt");
        }

        // Death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // ==================================
    // BURN
    // ==================================

    public void ApplyBurn(
        int damagePerTick,
        float duration,
        float tickRate
    )
    {
        if (isDead)
            return;

        if (damagePerTick <= 0)
            return;

        if (duration <= 0f)
            return;

        if (tickRate <= 0f)
        {
            tickRate = 1f;
        }

        // ==================================
        // BURN RESISTANCE
        // ==================================

        if (enemyType != null)
        {
            float resistance =
                Mathf.Clamp01(
                    enemyType.burnResistance
                );

            damagePerTick =
                Mathf.RoundToInt(
                    damagePerTick *
                    (1f - resistance)
                );
        }

        // %100 resistance
        if (damagePerTick <= 0)
        {
            GameLog.Log(
                gameObject.name +
                " Burn'a direnç gösterdi!"
            );

            return;
        }

        // Burn zaten aktif: sadece süreyi yenile, daha güçlüyse hasarı
        // yükselt. Coroutine'e ve efekte dokunma.
        if (burnCoroutine != null)
        {
            burnDamagePerTick =
                Mathf.Max(
                    burnDamagePerTick,
                    damagePerTick
                );

            burnTickRate = tickRate;

            burnTimeRemaining =
                Mathf.Max(
                    burnTimeRemaining,
                    duration
                );

            return;
        }

        burnDamagePerTick = damagePerTick;
        burnTickRate = tickRate;
        burnTimeRemaining = duration;

        StartBurnEffect();

        burnCoroutine =
            StartCoroutine(
                BurnRoutine()
            );

        ApplyRestingColor();
    }

    // ==================================
    // DURUM RENGİ
    // ==================================

    // Vuruş parlaması dışında düşmanın olması gereken renk.
    // Öncelik: donma > yanma > normal.
    public Color GetRestingColor()
    {
        if (enemyMovement != null && enemyMovement.IsFrozen())
            return enemyMovement.GetCurrentVisualColor();

        if (burnCoroutine != null && !isDead)
            return burnColor;

        return originalColor;
    }

    void ApplyRestingColor()
    {
        // Parlama sürüyorsa bittiğinde zaten doğru renge döner.
        if (spriteRenderer != null && flashCoroutine == null)
        {
            spriteRenderer.color = GetRestingColor();
        }
    }

    // ==================================
    // BURN ROUTINE
    // ==================================

    IEnumerator BurnRoutine()
    {
        GameLog.Log(
            gameObject.name +
            " BURN BAŞLADI!"
        );

        while (
            burnTimeRemaining > 0f &&
            !isDead
        )
        {
            float tick = burnTickRate;

            yield return new WaitForSeconds(
                tick
            );

            if (isDead)
                break;

            burnTimeRemaining -= tick;

            TakeBurnDamage(
                burnDamagePerTick
            );
        }

        // Enemy hâlâ aktifse efekti kapat.
        if (gameObject.activeInHierarchy)
        {
            StopBurnEffect();
        }

        burnCoroutine = null;

        if (!isDead)
        {
            ApplyRestingColor();
        }
    }

    // ==================================
    // BURN DAMAGE
    // ==================================

    void TakeBurnDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        DamageNumbers.Show(GetDamageNumberPosition(), damage, DamageNumbers.Style.Burn);

        GameLog.Log(
            gameObject.name +
            " BURN DAMAGE: " +
            damage +
            " | Kalan HP: " +
            currentHealth
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // ==================================
    // START BURN EFFECT
    // ==================================

    void StartBurnEffect()
    {
        if (ObjectPool.Instance == null)
            return;

        activeBurnEffect =
            ObjectPool.Instance.Spawn(
                GameKeys.BurnEffectPool,
                transform.position,
                Quaternion.identity
            );

        if (activeBurnEffect == null)
            return;

        // Enemy aktifken child yap.
        if (gameObject.activeInHierarchy)
        {
            activeBurnEffect.transform.SetParent(
                transform
            );

            activeBurnEffect.transform.localPosition =
                Vector3.zero;

            activeBurnEffect.transform.localRotation =
                Quaternion.identity;
        }
    }

    // ==================================
    // STOP BURN EFFECT
    // ==================================

    void StopBurnEffect()
    {
        if (activeBurnEffect == null)
            return;

        GameObject effectToRemove =
            activeBurnEffect;

        // Önce referansı temizle.
        activeBurnEffect = null;

        /*
         * SetParent sadece Enemy tamamen aktifken
         * çalıştırılır.
         *
         * Böylece:
         * Cannot set the parent while activating
         * or deactivating parent GameObject
         * hatasını önlüyoruz.
         */

        if (gameObject.activeInHierarchy &&
            effectToRemove.activeInHierarchy)
        {
            effectToRemove.transform.SetParent(
                null
            );
        }

        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.Despawn(
                GameKeys.BurnEffectPool,
                effectToRemove
            );
        }
        else
        {
            Destroy(effectToRemove);
        }
    }

    // ==================================
    // DIE
    // ==================================

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // ==================================
        // VURUŞ HİSSİ
        // ==================================

        // Çok kısa duraklama + hafif titreşim (art arda öldürmelerde sınırlı).
        // Boss öldüğünde daha uzun ve güçlü.
        if (IsBoss)
        {
            TimeScaleController.HitStop(0.25f, 0.05f, 0f);
            Haptics.Heavy();
        }
        else
        {
            TimeScaleController.HitStop(0.04f);
            Haptics.Light();
        }

        OnDied?.Invoke(this);

        // ==================================
        // BOMBER
        // ==================================

        TryBomberExplosion();

        // ==================================
        // BURN TEMİZLE
        // ==================================

        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            burnCoroutine = null;
        }

        /*
         * Enemy hâlâ aktif olduğu için burada
         * BurnEffect güvenli şekilde ayrılır
         * ve pool'a gönderilir.
         */

        StopBurnEffect();

        // ==================================
        // FLASH TEMİZLE
        // ==================================

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // ==================================
        // AUDIO
        // ==================================

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyDeath();
        }

        // ==================================
        // SCORE
        // ==================================

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(
                Mathf.RoundToInt(scoreValue * bossRewardMultiplier)
            );

            // Başarım istatistiği
            GameManager.Instance.RegisterKill();

            if (IsBoss)
            {
                GameManager.Instance.RegisterBossKill();
            }
        }

        // ==================================
        // GOLD
        // ==================================

        GrantDeathGold();

        // ==================================
        // XP
        // ==================================

        GrantXP();

        // ==================================
        // DEATH PARTICLE
        // ==================================

        if (deathParticlePrefab != null)
        {
            ObjectPool.SpawnTemporary(
                deathParticlePrefab,
                transform.position,
                Quaternion.identity,
                1f
            );
        }

        // ==================================
        // MOVEMENT OFF
        // ==================================

        if (enemyMovement != null)
        {
            enemyMovement.enabled = false;
        }

        // ==================================
        // PHYSICS
        // ==================================

        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;

            rb.angularVelocity =
                0f;

            rb.gravityScale =
                0f;

            rb.bodyType =
                RigidbodyType2D.Kinematic;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled =
                false;
        }

        // ==================================
        // ANIMATION
        // ==================================

        if (animator != null)
        {
            animator.SetBool(
                "isRunning",
                false
            );

            animator.ResetTrigger("Hurt");
            animator.ResetTrigger("Attack");

            animator.SetTrigger(
                "Die"
            );
        }

        // ==================================
        // RETURN TO POOL
        // ==================================

        Invoke(
            nameof(ReturnToPool),
            2f
        );
    }

    // ==================================
    // BOMBER EXPLOSION
    // ==================================

    void TryBomberExplosion()
    {
        if (enemyType == null)
            return;

        if (!enemyType.IsBomber())
            return;

        // ==================================
        // EFFECT
        // ==================================

        // Bomber patlaması kırmızı; halka oyuncuya hasar verdiği alanı gösterir.
        ExplosionBlast.Play(
            transform.position,
            enemyType.explosionRadius,
            new Color(1f, 0.55f, 0.35f, 1f),
            new Color(0.85f, 0.05f, 0f, 1f)
        );

        if (enemyType.explosionEffectPrefab != null)
        {
            // Prefab'da BomberExplosionEffect varsa onun süresi kullanılır.
            float effectLifetime = 1.5f;

            BomberExplosionEffect effectSettings =
                enemyType.explosionEffectPrefab
                    .GetComponent<BomberExplosionEffect>();

            if (effectSettings != null)
            {
                effectLifetime =
                    Mathf.Min(
                        effectLifetime,
                        effectSettings.lifetime
                    );
            }

            ObjectPool.SpawnTemporary(
                enemyType.explosionEffectPrefab,
                transform.position,
                Quaternion.identity,
                effectLifetime
            );
        }

        // ==================================
        // PLAYER
        // ==================================

        if (playerHealth == null)
        {
            FindPlayer();
        }

        if (playerHealth == null)
            return;

        Vector3 playerPosition =
            playerHealth.transform.position;

        float distance =
            Vector2.Distance(
                transform.position,
                playerPosition
            );

        // Menzil dışı
        if (distance >
            enemyType.explosionRadius)
        {
            GameLog.Log(
                "Bomber patladı ama oyuncu menzil dışındaydı."
            );

            return;
        }

        // ==================================
        // DAMAGE
        // ==================================

        playerHealth.TakeDamage(
            enemyType.explosionDamage
        );

        GameLog.Log(
            "BOMBER PATLADI! Player Damage: " +
            enemyType.explosionDamage
        );
    }

    // ==================================
    // XP
    // ==================================

    void GrantXP()
    {
        if (LevelSystem.Instance == null)
            return;

        float multiplier = 1f;

        if (enemyType != null)
        {
            multiplier =
                enemyType.xpMultiplier;
        }

        // Kalıcı "Hızlı Öğrenme" yükseltmesi
        multiplier *= PermanentUpgrades.XpMultiplier;

        multiplier *= bossRewardMultiplier;

        int finalXP =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    xpReward *
                    multiplier
                )
            );

        LevelSystem.Instance.AddXP(
            finalXP
        );

        GameLog.Log(
            gameObject.name +
            " XP verdi: " +
            finalXP
        );
    }

    // ==================================
    // RETURN TO POOL
    // ==================================

    void ReturnToPool()
    {
        // Güvenlik
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            burnCoroutine = null;
        }

        // Enemy henüz aktifken burn temizlenir.
        StopBurnEffect();

        if (
            ObjectPool.Instance != null &&
            !string.IsNullOrEmpty(poolKey)
        )
        {
            ObjectPool.Instance.Despawn(
                poolKey,
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

    // ==================================
    // GOLD
    // ==================================

    void GrantDeathGold()
    {
        if (deathGoldMax <= 0)
            return;

        // Boss her zaman altın bırakır.
        if (!IsBoss &&
            Random.value >
            deathGoldChance)
        {
            return;
        }

        int baseAmount =
            Random.Range(
                deathGoldMin,
                deathGoldMax + 1
            );

        float multiplier = 1f;

        if (enemyType != null)
        {
            multiplier =
                enemyType.goldMultiplier;
        }

        // Kalıcı "Altın Avcısı" yükseltmesi
        multiplier *= PermanentUpgrades.GoldMultiplier;

        multiplier *= bossRewardMultiplier;

        int finalAmount =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    baseAmount *
                    multiplier
                )
            );

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(
                finalAmount
            );
        }

        if (RewardPopupManager.Instance != null)
        {
            RewardPopupManager.Instance.ShowGold(
                finalAmount,
                transform.position
            );
        }
    }

    // ==================================
    // HIT FLASH
    // ==================================

    IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null)
        {
            flashCoroutine = null;
            yield break;
        }

        spriteRenderer.color =
            flashColor;

        yield return new WaitForSeconds(
            flashDuration
        );

        if (spriteRenderer == null)
        {
            flashCoroutine = null;
            yield break;
        }

        // Donma / yanma varsa o renge, yoksa normale dön.
        flashCoroutine = null;

        spriteRenderer.color =
            GetRestingColor();
    }

    // ==================================
    // ÖDÜLSÜZ KALDIRMA
    // ==================================

    // Puan / gold / XP vermeden sahneden kaldırır
    // (örn. reklamla devam edince oyuncunun etrafını temizlemek için).
    public void DespawnWithoutReward()
    {
        if (isDead)
            return;

        isDead = true;
        ReturnToPool();
    }

    // Hasar sayısı düşmanın başının biraz üstünde çıksın.
    Vector3 GetDamageNumberPosition()
    {
        if (enemyCollider != null && enemyCollider.enabled)
        {
            Bounds bounds = enemyCollider.bounds;
            return new Vector3(bounds.center.x, bounds.max.y + 0.1f, 0f);
        }

        return transform.position + Vector3.up;
    }

    // ==================================
    // PUBLIC INFO
    // ==================================

    public bool IsDead()
    {
        return isDead;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return scaledMaxHealth;
    }
}