using UnityEngine;

// Düşmana spawn anında verilen ek davranış. Prefab gerekmez: EnemySpawner
// havuzdan gelen düşmana bu bileşeni ekler (yoksa) ve Setup ile türünü seçer.
//  - Nişancı: belli bir mesafede durur, uzaktan mermi atar.
//  - Kalkanlı: önündeki kalkan kırılana kadar hasarı kalkan alır (yanma kalkanı deler).
//  - Bölünen: ölünce iki küçük, zayıf kopyaya ayrılır.
public class EnemyVariant : MonoBehaviour
{
    public enum Kind
    {
        None,
        Ranged,
        Shielded,
        Splitter,
        SplitChild
    }

    // ==================================
    // AYARLAR
    // ==================================

    // Hangi seviyeden itibaren ve hangi olasılıkla çıkar
    private const int RangedUnlockLevel = 3;
    private const float RangedChance = 0.14f;

    private const int ShieldedUnlockLevel = 5;
    private const float ShieldedChance = 0.12f;

    private const int SplitterUnlockLevel = 7;
    private const float SplitterChance = 0.12f;

    // Nişancı
    private const float RangedStopDistance = 6.5f;
    private const float RangedFireInterval = 2.2f;
    private const float RangedDamageMultiplier = 0.6f;
    private const float RangedHealthMultiplier = 0.8f;

    // Kalkanlı: kalkan canı = düşmanın canı x bu oran
    private const float ShieldHealthRatio = 0.7f;
    private const float ShieldedSpeedMultiplier = 0.85f;

    // Bölünen
    private const float SplitterHealthMultiplier = 1.2f;
    private const float ChildScale = 0.65f;
    private const float ChildHealthMultiplier = 0.35f;
    private const float ChildRewardMultiplier = 0.4f;
    private const float ChildSpeedMultiplier = 1.25f;

    private static readonly Color RangedGlow = new Color(1f, 0.25f, 0.2f, 0.55f);
    private static readonly Color SplitterGlow = new Color(0.75f, 0.35f, 1f, 0.55f);
    private static readonly Color ShieldColor = new Color(0.55f, 0.8f, 1f, 0.75f);

    // ==================================
    // DURUM
    // ==================================

    public Kind CurrentKind { get; private set; } = Kind.None;

    public bool IsRanged => CurrentKind == Kind.Ranged;

    public float HealthMultiplier
    {
        get
        {
            switch (CurrentKind)
            {
                case Kind.Ranged: return RangedHealthMultiplier;
                case Kind.Splitter: return SplitterHealthMultiplier;
                case Kind.SplitChild: return ChildHealthMultiplier;
                default: return 1f;
            }
        }
    }

    public float SpeedMultiplier
    {
        get
        {
            switch (CurrentKind)
            {
                case Kind.Shielded: return ShieldedSpeedMultiplier;
                case Kind.SplitChild: return ChildSpeedMultiplier;
                default: return 1f;
            }
        }
    }

    public float RewardMultiplier
    {
        get
        {
            switch (CurrentKind)
            {
                case Kind.Ranged: return 1.3f;
                case Kind.Shielded: return 1.5f;
                case Kind.Splitter: return 1.2f;
                case Kind.SplitChild: return ChildRewardMultiplier;
                default: return 1f;
            }
        }
    }

    public float RangedRange => RangedStopDistance;

    private EnemyHealth health;
    private EnemyMovement movement;
    private SpriteRenderer bodyRenderer;
    private Collider2D bodyCollider;

    private Vector3 originalScale;
    private bool scaleCaptured;

    private SpriteRenderer glow;
    private SpriteRenderer shield;

    private int shieldHealth;
    private int shieldMaxHealth;
    private float shieldFlashTimer;

    private float fireTimer;

    // Boss kalkanı: süre dolunca kendiliğinden kalkar (0 = süresiz)
    private float temporaryShieldTimer;

    public bool HasShield => shieldHealth > 0;

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
        movement = GetComponent<EnemyMovement>();
        bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        foreach (Collider2D candidate in GetComponents<Collider2D>())
        {
            if (!candidate.isTrigger || bodyCollider == null)
                bodyCollider = candidate;
        }

        CaptureScale();
    }

    void CaptureScale()
    {
        if (scaleCaptured)
            return;

        originalScale = transform.localScale;
        scaleCaptured = true;
    }

    // Havuza dönünce sıradan düşmana döner.
    void OnDisable()
    {
        ResetVariant();
    }

    void ResetVariant()
    {
        CurrentKind = Kind.None;
        shieldHealth = 0;
        temporaryShieldTimer = 0f;

        if (scaleCaptured)
        {
            transform.localScale = originalScale;
        }

        if (glow != null) glow.gameObject.SetActive(false);
        if (shield != null) shield.gameObject.SetActive(false);
    }

    // ==================================
    // SEÇİM
    // ==================================

    // Bu seviyede çıkacak tür (çoğunlukla None)
    public static Kind Roll(int level)
    {
        float roll = Random.value;

        if (level >= SplitterUnlockLevel)
        {
            if (roll < SplitterChance) return Kind.Splitter;
            roll -= SplitterChance;
        }

        if (level >= ShieldedUnlockLevel)
        {
            if (roll < ShieldedChance) return Kind.Shielded;
            roll -= ShieldedChance;
        }

        if (level >= RangedUnlockLevel)
        {
            if (roll < RangedChance) return Kind.Ranged;
        }

        return Kind.None;
    }

    // Havuzdan çıkan düşmana türünü verir; can / hız buna göre yeniden hesaplanır.
    public static void Apply(GameObject enemy, Kind kind)
    {
        EnemyVariant variant = enemy.GetComponent<EnemyVariant>();

        if (variant == null)
        {
            if (kind == Kind.None)
                return;

            variant = enemy.AddComponent<EnemyVariant>();
        }

        variant.Setup(kind);
    }

    public void Setup(Kind kind)
    {
        CaptureScale();
        ResetVariant();
        CurrentKind = kind;

        if (kind == Kind.SplitChild)
        {
            transform.localScale = originalScale * ChildScale;
        }

        // Can ve hız tür çarpanlarıyla yeniden hesaplansın.
        if (health != null) health.RefreshHealthScaling();
        if (movement != null) movement.RefreshScaling();

        switch (kind)
        {
            case Kind.Ranged:
                ShowGlow(RangedGlow);
                fireTimer = RangedFireInterval * Random.Range(0.4f, 0.8f);
                break;

            case Kind.Splitter:
                ShowGlow(SplitterGlow);
                break;

            case Kind.Shielded:
                shieldMaxHealth = health != null
                    ? Mathf.Max(1, Mathf.RoundToInt(health.GetMaxHealth() * ShieldHealthRatio))
                    : 30;
                shieldHealth = shieldMaxHealth;
                ShowShield();
                break;
        }

        ShowIntroIfNew(kind);
    }

    // İlk karşılaşmada kısa tanıtım
    void ShowIntroIfNew(Kind kind)
    {
        switch (kind)
        {
            case Kind.Ranged:
                EnemyIntroUI.ShowOnce(
                    IntroIds[0],
                    Loc.T("NİŞANCI"),
                    Loc.T("Kırmızı haleli düşmanlar sana yaklaşmaz; belli bir mesafede durup uzaktan ateş eder. Onları öncelikli hedef al."),
                    new Color(1f, 0.35f, 0.3f, 1f),
                    transform
                );
                break;

            case Kind.Shielded:
                EnemyIntroUI.ShowOnce(
                    IntroIds[1],
                    Loc.T("KALKANLI"),
                    Loc.T("Önündeki mavi kalkan hasarı emer ve vurdukça solar. Kalkan kırılınca savunmasız kalır.\n<color=#FF9A3C>Yanma hasarı kalkanı deler!</color>"),
                    new Color(0.55f, 0.8f, 1f, 1f),
                    transform
                );
                break;

            case Kind.Splitter:
                EnemyIntroUI.ShowOnce(
                    IntroIds[2],
                    Loc.T("BÖLÜNEN"),
                    Loc.T("Mor haleli düşmanlar ölünce iki küçük ve hızlı kopyaya ayrılır. Patlayıcı ve delici mermiler çok işe yarar."),
                    new Color(0.8f, 0.45f, 1f, 1f),
                    transform
                );
                break;
        }
    }

    public static readonly string[] IntroIds = { "variant_ranged", "variant_shielded", "variant_splitter" };

    // ==================================
    // GÖRSELLER
    // ==================================

    // Siyah siluetin arkasında renkli hale: türü uzaktan belli eder.
    void ShowGlow(Color color)
    {
        if (glow == null)
        {
            glow = CreateChildRenderer("VariantGlow", RuntimeSprites.SoftCircle, -1);
        }

        float size = GetBodySize() * 1.6f;
        glow.transform.localPosition = GetBodyCenterLocal();
        SetWorldSize(glow.transform, size, size);
        glow.color = color;
        glow.gameObject.SetActive(true);
    }

    void ShowShield()
    {
        if (shield == null)
        {
            shield = CreateChildRenderer("VariantShield", RuntimeSprites.ShieldArc, 2);
        }

        // Kalkan düşmanın oyuncuya bakan (sol) tarafında durur.
        float size = GetBodySize();
        Vector3 center = GetBodyCenterLocal();
        // Ölçek negatifse (aynalanmış düşman) yerel eksen ters döner; işaret korunur.
        float scaleX = transform.lossyScale.x;
        if (Mathf.Abs(scaleX) < 0.01f) scaleX = 0.01f;
        center.x -= size * 0.45f / scaleX;
        shield.transform.localPosition = center;
        SetWorldSize(shield.transform, size * 0.45f, size * 1.15f);
        shield.color = ShieldColor;
        shield.gameObject.SetActive(true);
    }

    SpriteRenderer CreateChildRenderer(string name, Sprite sprite, int orderOffset)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform, false);

        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        UITheme theme = UITheme.Load();
        if (theme != null && theme.unlitEffectMaterial != null)
        {
            renderer.sharedMaterial = theme.unlitEffectMaterial;
        }

        if (bodyRenderer != null)
        {
            renderer.sortingLayerID = bodyRenderer.sortingLayerID;
            renderer.sortingOrder = bodyRenderer.sortingOrder + orderOffset;
        }

        return renderer;
    }

    float GetBodySize()
    {
        if (bodyRenderer != null && bodyRenderer.sprite != null)
        {
            Bounds bounds = bodyRenderer.bounds;
            return Mathf.Max(0.3f, Mathf.Min(bounds.size.x, bounds.size.y));
        }

        return 1f;
    }

    Vector3 GetBodyCenterLocal()
    {
        Vector3 world = bodyCollider != null ? bodyCollider.bounds.center
            : bodyRenderer != null ? bodyRenderer.bounds.center
            : transform.position;

        Vector3 local = transform.InverseTransformPoint(world);
        local.z = 0f;
        return local;
    }

    // Çocuk objeyi, ebeveyn ölçeğinden bağımsız olarak dünya biriminde boyutlandırır.
    static void SetWorldSize(Transform child, float width, float height)
    {
        Vector3 parentScale = child.parent != null ? child.parent.lossyScale : Vector3.one;
        child.localScale = new Vector3(
            width / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            height / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
            1f
        );
    }

    // ==================================
    // KALKAN
    // ==================================

    // EnemyHealth.TakeDamage çağırır. Kalkan varsa hasarı emer; kalkanı aşan kısım
    // düşmana geçer (damage güncellenir). true = hasarın tamamı kalkanda kaldı.
    public bool AbsorbDamage(ref int damage, Vector3 numberPosition)
    {
        if (shieldHealth <= 0 || damage <= 0)
            return false;

        int absorbed = Mathf.Min(shieldHealth, damage);
        shieldHealth -= absorbed;
        damage -= absorbed;

        DamageNumbers.Show(numberPosition, absorbed, DamageNumbers.Style.Shield);

        if (shieldHealth <= 0)
        {
            BreakShield();
        }
        else
        {
            shieldFlashTimer = 0.08f;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayShieldHit();
            }
        }

        return damage <= 0;
    }

    // Boss yeteneği: belli süre hasarı emen kalkan açar.
    public void GrantTemporaryShield(int amount, float duration)
    {
        shieldMaxHealth = Mathf.Max(1, amount);
        shieldHealth = shieldMaxHealth;
        temporaryShieldTimer = duration;
        ShowShield();
    }

    void BreakShield()
    {
        temporaryShieldTimer = 0f;

        if (shield != null)
        {
            ExplosionBlast.Play(
                shield.transform.position,
                GetBodySize() * 0.6f,
                new Color(0.85f, 0.95f, 1f, 1f),
                ShieldColor
            );

            shield.gameObject.SetActive(false);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayShieldBreak();
        }
    }

    // ==================================
    // NİŞANCI ATEŞİ
    // ==================================

    // EnemyMovement, nişancı menzilde durduğunda her kare çağırır.
    public void TickRangedAttack(Transform target, int damage)
    {
        fireTimer -= Time.deltaTime;

        if (fireTimer > 0f)
            return;

        fireTimer = RangedFireInterval * Random.Range(0.9f, 1.1f);

        Vector3 origin = bodyCollider != null ? bodyCollider.bounds.center : transform.position;
        origin.x -= GetBodySize() * 0.4f;

        EnemyProjectile.Fire(
            origin,
            target,
            Mathf.Max(1, Mathf.RoundToInt(damage * RangedDamageMultiplier))
        );

        if (glow != null)
        {
            glow.color = new Color(1f, 0.6f, 0.4f, 0.9f);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyShot();
        }
    }

    void Update()
    {
        if (CurrentKind == Kind.Ranged && glow != null && glow.color != RangedGlow)
        {
            glow.color = Color.Lerp(glow.color, RangedGlow, Time.deltaTime * 6f);
        }

        // Süreli kalkan bitti: sessizce kaybolur.
        if (temporaryShieldTimer > 0f)
        {
            temporaryShieldTimer -= Time.deltaTime;

            if (temporaryShieldTimer <= 0f)
            {
                shieldHealth = 0;
                if (shield != null) shield.gameObject.SetActive(false);
            }
        }

        if (shield != null && shield.gameObject.activeSelf)
        {
            shieldFlashTimer -= Time.deltaTime;

            float strength = shieldMaxHealth > 0 ? (float)shieldHealth / shieldMaxHealth : 1f;
            Color color = shieldFlashTimer > 0f ? Color.white : ShieldColor;
            color.a = Mathf.Lerp(0.35f, ShieldColor.a, strength);
            shield.color = color;
        }
    }

    // ==================================
    // BÖLÜNME
    // ==================================

    // EnemyHealth.Die çağırır (ödüller verildikten sonra).
    public void OnOwnerDied()
    {
        if (CurrentKind != Kind.Splitter || health == null)
            return;

        float offset = GetBodySize() * 0.35f;

        SpawnChild(new Vector3(0f, offset, 0f));
        SpawnChild(new Vector3(offset * 0.6f, -offset, 0f));
    }

    void SpawnChild(Vector3 offset)
    {
        Vector3 position = transform.position + offset;
        GameObject child = null;

        if (ObjectPool.Instance != null && !string.IsNullOrEmpty(health.poolKey))
        {
            child = ObjectPool.Instance.Spawn(health.poolKey, position, Quaternion.identity);
        }

        if (child == null || child == gameObject)
            return;

        child.transform.SetPositionAndRotation(position, Quaternion.identity);
        Apply(child, Kind.SplitChild);
    }
}
