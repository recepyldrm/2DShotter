using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
    [Header("Hareket - BASE DEĞER")]
    public float moveSpeed = 2f;
    public float attackDistance = 1.2f;

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

    private EnemyType enemyType;

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

    void Awake()
    {
        animator =
            GetComponentInChildren<Animator>();

        spriteRenderer =
            GetComponentInChildren<SpriteRenderer>();

        rb =
            GetComponent<Rigidbody2D>();

        enemyType =
            GetComponent<EnemyType>();

        if (spriteRenderer != null)
        {
            originalColor =
                spriteRenderer.color;
        }

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
                globalDamageMultiplier
            );

        // ==============================
        // FINAL SPEED
        // ==============================

        scaledMoveSpeed =
            moveSpeed *
            typeSpeedMultiplier *
            globalSpeedMultiplier;

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

        float distance =
            Vector2.Distance(
                transform.position,
                player.position
            );

        if (distance > attackDistance)
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
            currentSpeedMultiplier;

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

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                originalColor;
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
