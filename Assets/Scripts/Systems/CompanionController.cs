using UnityEngine;

// Oyuncunun yanındaki dost: oyuncuyu takip eder, mermilere pasif hasar
// bonusu verir ve ana menüde seçilen skini gösterir.
public class CompanionController : MonoBehaviour
{
    public static CompanionController Instance;

    [Header("Takip")]
    public Transform player;
    public Vector2 followOffset = new Vector2(-1f, 0f);
    public float followSmoothing = 5f;

    [Header("Pasif Bonus (dost yanındayken sürekli aktif)")]
    [Tooltip("Bullet hasarına çarpılan bonus. Örn: 1.15 = +%15 hasar")]
    public float passiveDamageMultiplier = 1.15f;

    public float DamageMultiplier => passiveDamageMultiplier;

    // Seçili skin animasyonluysa kareleri oynatmak için
    private CompanionSkinDatabase.Skin activeSkin;
    private SpriteRenderer skinRenderer;
    private float skinAnimationTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ApplySelectedSkin();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        AnimateSkin();

        if (player == null)
            return;

        FollowPlayer();
    }

    void FollowPlayer()
    {
        float desiredX = player.position.x + followOffset.x;

        float newX = Mathf.Lerp(
            transform.position.x,
            desiredX,
            followSmoothing * Time.deltaTime
        );

        transform.position = new Vector2(newX, transform.position.y);
    }

    // ==================================
    // SKIN
    // ==================================

    // Ana menüdeki dükkandan seçilen skini uygular.
    void ApplySelectedSkin()
    {
        CompanionSkinDatabase database = CompanionSkinDatabase.Load();

        if (database == null)
            return;

        CompanionSkinDatabase.Skin skin = database.GetSelectedSkin();

        if (skin == null)
            return;

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        Sprite firstSprite = skin.FirstSprite;

        if (firstSprite != null)
        {
            spriteRenderer.sprite = firstSprite;
        }

        // Çarpma, sahnedeki saydamlığı (alpha) korur.
        spriteRenderer.color *= skin.tint;

        spriteRenderer.transform.localScale *= skin.Scale;

        if (skin.flipX)
        {
            spriteRenderer.flipX = !spriteRenderer.flipX;
        }

        if (skin.IsAnimated)
        {
            activeSkin = skin;
            skinRenderer = spriteRenderer;
        }
    }

    void AnimateSkin()
    {
        if (activeSkin == null || skinRenderer == null)
            return;

        skinAnimationTime += Time.deltaTime;

        Sprite frame = activeSkin.GetFrame(skinAnimationTime);

        if (frame != null && skinRenderer.sprite != frame)
        {
            skinRenderer.sprite = frame;
        }
    }
}
