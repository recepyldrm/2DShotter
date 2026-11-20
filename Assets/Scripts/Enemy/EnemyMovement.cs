using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
    [Header("Hareket - BASE DEĞER")]
    public float moveSpeed = 2f;

    [Tooltip("Yedek: collider bulunamazsa merkezden merkeze bu mesafede saldırır.")]
    public float attackDistance = 1.2f;

    [Tooltip("Düşmanın gövdesi ile oyuncunun gövdesi arasında bu kadar boşluk kalınca saldırır (dünya birimi).")]
    public float attackReach = 0.15f;

    [Header("Saldırı - BASE DEĞER")]
    public float attackCooldown = 1f;
    public int damage = 10;

    [Header("Freeze Görseli")]
    public Color freezeColor = new Color(
        0.4f,
        0.8f,
        1f,
        1f
    );

    private Transform player;
    private PlayerHealth playerHealth;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    // Saldırı mesafesi gövdelerin kenarları arasından ölçülür.
    private Collider2D bodyCollider;
    private Collider2D playerCollider;

    private EnemyType enemyType;
    private EnemyHealth enemyHealth;

    // Spawn sırasında eklenebilir (nişancı / kalkanlı / bölünen), tembel alınır.
    private EnemyVariant variant;
    EnemyVariant Variant => variant != null ? variant : (variant = GetComponent<EnemyVariant>());

    private float attackTimer;

    private float scaledMoveSpeed;
    private int scaledDamage;

    private float currentSpeedMultiplier = 1f;

    private Coroutine freezeCoroutine;

    private Color originalColor;

    // Freeze aktif mi?
    private bool isFrozen = false;

    // Update karar verir, FixedUpdate fiziksel olarak hareket ettirir.
    private bool shouldMove = false;

    // ==================================
    // BOSS
    // ==================================

    private float bossSpeedMultiplier = 1f;
    private float bossDamageMultiplier = 1f;

    // Boss hücum ederken geçici hız çarpanı
    private float chargeMultiplier = 1f;

    // BossWaveManager bir düşmanı boss yapınca çağırır.
    // attackRangeMultiplier: boss büyüdüğü için saldırı mesafesi de büyür.
    public void ConfigureAsBoss(float speedMultiplier, float damageMultiplier, float attackRangeMultiplier)
    {
        bossSpeedMultiplier = speedMultiplier;
        bossDamageMultiplier = damageMultiplier;
        attackDistance *= attackRangeMultiplier;

        // Gövde zaten büyüdü; kenar mesafesine sadece biraz pay eklenir.
        attackReach += 0.1f;

        ApplyScaling();
    }

    // Tetik olmayan (fiziksel) collider'ı tercih et.
    static Collider2D FindBodyCollider(GameObject target)
    {
        Collider2D fallback = null;

        foreach (Collider2D candidate in target.GetComponents<Collider2D>())
        {
            if (!candidate.isTrigger)
                return candidate;

            if (fallback == null)
                fallback = candidate;
        }

        return fallback;
    }

    // Oyuncu saldırı menzilinde mi? Gövdelerin kenarları arasındaki
    // boşluğa bakılır; collider yoksa merkezden merkeze mesafe kullanılır.
    bool IsPlayerInAttackRange()
    {
        if (bodyCollider != null &&
            playerCollider != null &&
            bodyCollider.enabled &&
            playerCollider.enabled)
        {
            ColliderDistance2D gap = Physics2D.Distance(bodyCollider, playerCollider);

            if (gap.isValid)
            {
                return gap.distance <= attackReach;
            }
        }

        return Vector2.Distance(transform.position, player.position) <= attackDistance;
    }

    // Nişancı ateş edebilecek konumda mı: oyuncuya yeterince yakın ve ekranın içinde.
    bool IsInRangedPosition()
    {
        if (Vector2.Distance(transform.position, player.position) > Variant.RangedRange)
            return false;

        Camera cam = Camera.main;

        if (cam == null)
            return true;

        Vector3 viewport = cam.WorldToViewportPoint(transform.position);
        return viewport.x > 0.05f && viewport.x < 0.92f && viewport.y > 0.05f && viewport.y < 0.95f;
    }

    // Boss yetenekleri hasarı buna göre ölçekler.
    public int CurrentDamage => scaledDamage;

    public void SetChargeMultiplier(float multiplier)
    {
        chargeMultiplier = Mathf.Max(0f, multiplier);
    }

    void Awake()
    {
        animator =
            GetComponentInChildren<Animator>();

        spriteRenderer =
            GetComponentInChildren<SpriteRenderer>();

        rb =
            GetComponent<Rigidbody2D>();

        bodyCollider =
            FindBodyCollider(gameObject);

        enemyType =
            GetComponent<EnemyType>();

        enemyHealth =
            GetComponent<EnemyHealth>();

        if (spriteRenderer != null)
        {
            originalColor =
                spriteRenderer.color;
        }

        // Donma rengi yanlışlıkla saydam ayarlanırsa düşman görünmez olmasın.
        freezeColor.a = 1f;

        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (playerObject != null)
        {
            player =
                playerObject.transform;

            playerHealth =
                playerObject.GetComponent<PlayerHealth>();

            playerCollider =
                FindBodyCollider(playerObject);
        }
        else
        {
            Debug.LogError(
                "Player tag'li obje bulunamadı!"
            );
        }
    }

    void OnEnable()
    {
        attackTimer = 0f;

        currentSpeedMultiplier = 1f;

        isFrozen = false;

        shouldMove = false;

        if (freezeCoroutine != null)
        {
            StopCoroutine(
                freezeCoroutine
            );

            freezeCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                originalColor;
        }

        ApplyScaling();
    }

    void OnDisable()
    {
        if (freezeCoroutine != null)
        {
            StopCoroutine(
                freezeCoroutine
            );

            freezeCoroutine = null;
        }

        currentSpeedMultiplier = 1f;

        isFrozen = false;

        shouldMove = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                originalColor;
        }
    }

    // ==================================
    // SCALING
    // ==================================

    // EnemyVariant türü değişince hız / hasar yeniden hesaplansın.
    public void RefreshScaling()
    {
        ApplyScaling();
    }

    void ApplyScaling()
    {
        float globalDamageMultiplier = 1f;
        float globalSpeedMultiplier = 1f;

        // ==============================
        // GLOBAL ENEMY SCALING
        // ==============================

        if (EnemyScalingManager.Instance != null)
        {
            globalDamageMultiplier =
                EnemyScalingManager.Instance
                    .DamageMultiplier;

            globalSpeedMultiplier =
                EnemyScalingManager.Instance
                    .SpeedMultiplier;
        }

        // Shop'tan silah hasarı alındıkça enemy'ler de güçlenir
        if (UpgradeManager.Instance != null)
        {
            globalDamageMultiplier *=
                UpgradeManager.Instance
                    .EnemyDamageMultiplier;
        }

        // Ana menüde seçilen zorluk
        globalDamageMultiplier *= Difficulty.EnemyDamageMultiplier;
        globalSpeedMultiplier *= Difficulty.EnemySpeedMultiplier;

        // Bölüm kuralı
        globalDamageMultiplier *= StageThemes.DamageMultiplier;
        globalSpeedMultiplier *= StageThemes.SpeedMultiplier;
        globalSpeedMultiplier *= DailyChallenge.EnemySpeedMultiplier;

        // ==============================
        // ENEMY TYPE SCALING
        // ==============================

        float typeDamageMultiplier = 1f;
        float typeSpeedMultiplier = 1f;

        if (enemyType != null)
        {
            typeDamageMultiplier =
                enemyType.damageMultiplier;

            typeSpeedMultiplier =
                enemyType.speedMultiplier;
        }

        // ==============================
        // FINAL DAMAGE
        // ==============================

        scaledDamage =
            Mathf.RoundToInt(
                damage *
                typeDamageMultiplier *
                globalDamageMultiplier *
                bossDamageMultiplier
            );

        // ==============================
        // FINAL SPEED
        // ==============================

        scaledMoveSpeed =
            moveSpeed *
            typeSpeedMultiplier *
            globalSpeedMultiplier *
            bossSpeedMultiplier *
            (Variant != null ? Variant.SpeedMultiplier : 1f);

        GameLog.Log(
            gameObject.name +
            " | Speed: " +
            scaledMoveSpeed +
            " | Damage: " +
            scaledDamage
        );
    }

    void Update()
    {
        // Oyun durmuşken (pause, kart ekranı, game over) saldırma.
        if (Time.deltaTime <= 0f)
        {
            shouldMove = false;
            return;
        }

        if (player == null)
        {
            shouldMove = false;
            return;
        }

        attackTimer -=
            Time.deltaTime;

        // Nişancı: menzile ve ekrana girince durur, uzaktan ateş eder.
        if (Variant != null && Variant.IsRanged && !IsPlayerInAttackRange() && IsInRangedPosition())
        {
            shouldMove = false;
            SetRunning(false);
            Variant.TickRangedAttack(player, scaledDamage);
            return;
        }

        if (!IsPlayerInAttackRange())
        {
            shouldMove = true;

            // Rigidbody yoksa eski yöntemle hareket et.
            if (rb == null)
            {
                transform.position +=
                    (Vector3)GetMoveDelta(
                        transform.position,
                        Time.deltaTime
                    );
            }

            SetRunning(true);
        }
        else
        {
            shouldMove = false;

            AttackPlayer();
        }
    }

    // ==================================
    // MOVEMENT
    // ==================================

    // Rigidbody2D'yi transform ile değil fizik adımında hareket ettir,
    // böylece çarpışmalarla çakışıp titremez.
    void FixedUpdate()
    {
        if (!shouldMove ||
            rb == null ||
            player == null)
        {
            return;
        }

        rb.MovePosition(
            rb.position +
            GetMoveDelta(
                rb.position,
                Time.fixedDeltaTime
            )
        );
    }

    Vector2 GetMoveDelta(
        Vector2 from,
        float deltaTime
    )
    {
        Vector2 direction =
            (
                (Vector2)player.position -
                from
            ).normalized;

        float finalMoveSpeed =
            scaledMoveSpeed *
            currentSpeedMultiplier *
            chargeMultiplier;

        return
            direction *
            finalMoveSpeed *
            deltaTime;
    }

    void SetRunning(bool running)
    {
        if (animator != null)
        {
            animator.SetBool(
                "isRunning",
                running
            );
        }
    }

    // ==================================
    // ATTACK
    // ==================================

    void AttackPlayer()
    {
        SetRunning(false);

        if (attackTimer > 0f)
            return;

        if (animator != null)
        {
            animator.SetTrigger(
                "Attack"
            );
        }

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                scaledDamage
            );
        }

        attackTimer =
            attackCooldown;
    }

    // ==================================
    // FREEZE
    // ==================================

    public void ApplyFreeze(
        float slowAmount,
        float duration
    )
    {
        // ==============================
        // FREEZE RESISTANCE
        // ==============================

        if (enemyType != null)
        {
            float resistance =
                Mathf.Clamp01(
                    enemyType.freezeResistance
                );

            slowAmount *=
                1f - resistance;
        }

        slowAmount =
            Mathf.Clamp(
                slowAmount,
                0f,
                0.8f
            );

        if (slowAmount <= 0f)
            return;

        if (freezeCoroutine != null)
        {
            StopCoroutine(
                freezeCoroutine
            );
        }

        freezeCoroutine =
            StartCoroutine(
                FreezeRoutine(
                    slowAmount,
                    duration
                )
            );
    }

    IEnumerator FreezeRoutine(
        float slowAmount,
        float duration
    )
    {
        isFrozen = true;

        currentSpeedMultiplier =
            1f - slowAmount;

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                freezeColor;
        }

        GameLog.Log(
            gameObject.name +
            " FREEZE! Slow: %" +
            Mathf.RoundToInt(
                slowAmount * 100f
            )
        );

        yield return new WaitForSeconds(
            duration
        );

        currentSpeedMultiplier = 1f;

        isFrozen = false;

        // Donma bitti: hâlâ yanıyorsa yanma rengine, değilse normale dön.
        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                enemyHealth != null
                    ? enemyHealth.GetRestingColor()
                    : originalColor;
        }

        freezeCoroutine = null;
    }

    // ==================================
    // HIT FLASH İÇİN RENK
    // ==================================

    public Color GetCurrentVisualColor()
    {
        if (isFrozen)
        {
            return freezeColor;
        }

        return originalColor;
    }

    public bool IsFrozen()
    {
        return isFrozen;
    }
}
