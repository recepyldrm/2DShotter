using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Drone yetenekleri: her görünümün küçük bir özelliği var.
// Görünüm Id'sine göre seçilir (CompanionSkinDatabase'e dokunmak gerekmez).
// Aktif yetenekler (yavaşlatma, yakma, iyileştirme) bu bileşenle çalışır;
// pasif olanları (kritik, delme, altın) Bullet / EnemyHealth buradan okur.
public class CompanionAbilities : MonoBehaviour
{
    public enum Ability
    {
        None,
        Slow,       // En yakın düşmana yavaşlatıcı atış
        Burn,       // En yakın düşmana yakıcı atış
        Heal,       // Düzenli olarak can doldurur
        GoldMagnet, // Ölen düşmanlardan daha çok altın
        Crit,       // Mermilere ek kritik şansı
        Pierce      // Mermiler bir düşmanı fazladan deler
    }

    public struct Info
    {
        public Ability ability;
        public float strength;

        public Info(Ability ability, float strength)
        {
            this.ability = ability;
            this.strength = strength;
        }
    }

    // Görünüm -> yetenek. strength yeteneğe göre anlam taşır (aşağıdaki açıklamalara bak).
    private static readonly Dictionary<string, Info> BySkin = new Dictionary<string, Info>
    {
        { "drone_saucer",      new Info(Ability.Burn, 1f) },
        { "drone_crimson",     new Info(Ability.Burn, 1.4f) },
        { "drone_hover",       new Info(Ability.Heal, 1f) },
        { "drone_sentinel",    new Info(Ability.Heal, 1.5f) },
        { "drone_box_mini",    new Info(Ability.Slow, 1f) },
        { "drone_box",         new Info(Ability.Slow, 1.2f) },
        { "drone_frost",       new Info(Ability.Slow, 1.5f) },
        { "drone_gunner",      new Info(Ability.Crit, 0.06f) },
        { "drone_gunner_fire", new Info(Ability.Crit, 0.1f) },
        { "drone_shadow",      new Info(Ability.Pierce, 1f) },
        { "drone_phantom",     new Info(Ability.Pierce, 1f) },
        { "drone_jet",         new Info(Ability.Pierce, 1f) },
        { "drone_chopper",     new Info(Ability.GoldMagnet, 0.1f) },
        { "drone_golden",      new Info(Ability.GoldMagnet, 0.15f) },
        { "drone_season",      new Info(Ability.GoldMagnet, 0.15f) }
    };

    // Aktif yetenek bekleme süreleri (saniye)
    private const float SlowInterval = 4f;
    private const float BurnInterval = 3.5f;
    private const float HealInterval = 12f;

    private const float SlowAmount = 0.35f;
    private const float SlowDuration = 2f;
    private const float BurnDuration = 3f;
    private const float BurnTickRate = 0.5f;
    private const int BurnBaseTick = 4;
    private const float HealFraction = 0.04f;
    private const float ShotRange = 12f;

    public static Info For(string skinId)
    {
        if (!string.IsNullOrEmpty(skinId) && BySkin.TryGetValue(skinId, out Info info))
            return info;

        return new Info(Ability.None, 0f);
    }

    public static Info Selected => For(PlayerProgress.SelectedCompanionSkin);

    // Oyun içinde geçerli yetenek (sahnede drone yoksa yetenek de yok)
    private static Info active;
    private static bool hasActive;

    public static float BonusCritChance => hasActive && active.ability == Ability.Crit ? active.strength : 0f;
    public static int BonusPierce => hasActive && active.ability == Ability.Pierce ? Mathf.RoundToInt(active.strength) : 0;
    public static float GoldMultiplier => hasActive && active.ability == Ability.GoldMagnet ? 1f + active.strength : 1f;

    // Mağazada görünümün altında görünen kısa açıklama
    public static string Describe(Info info)
    {
        switch (info.ability)
        {
            case Ability.Slow:
                return Loc.Pick("Yavaşlatıcı atış: düşmanı %", "Slowing shot: slows an enemy by ") +
                       Mathf.RoundToInt(SlowAmount * Mathf.Min(info.strength, 2f) * 100f) + Loc.Pick(" yavaşlatır", "%");
            case Ability.Burn:
                return Loc.Pick("Yakıcı atış: düşmanı tutuşturur", "Burning shot: sets an enemy on fire");
            case Ability.Heal:
                return Loc.Pick("İyileştirme: her ", "Healing: restores ") +
                       Loc.Pick(Mathf.RoundToInt(HealInterval) + " sn'de %" + Mathf.RoundToInt(HealFraction * info.strength * 100f) + " can",
                                Mathf.RoundToInt(HealFraction * info.strength * 100f) + "% health every " + Mathf.RoundToInt(HealInterval) + "s");
            case Ability.GoldMagnet:
                return Loc.Pick("Altın mıknatısı: +%", "Gold magnet: +") + Mathf.RoundToInt(info.strength * 100f) + Loc.Pick(" altın", "% gold");
            case Ability.Crit:
                return Loc.Pick("Kritik destek: +%", "Critical boost: +") + Mathf.RoundToInt(info.strength * 100f) + Loc.Pick(" kritik şansı", "% crit chance");
            case Ability.Pierce:
                return Loc.Pick("Delici atış: mermiler +1 düşman deler", "Piercing shot: bullets pierce +1 enemy");
            default:
                return Loc.Pick("Yetenek yok (sadece +%15 hasar)", "No ability (just +15% damage)");
        }
    }

    // ==================================
    // OYUN İÇİ
    // ==================================

    private CompanionController companion;
    private PlayerHealth playerHealth;
    private float timer;

    void Awake()
    {
        companion = GetComponent<CompanionController>();
    }

    void OnEnable()
    {
        active = Selected;
        hasActive = true;
        timer = 1.5f;
    }

    void OnDisable()
    {
        hasActive = false;
    }

    void Update()
    {
        if (Time.deltaTime <= 0f)
            return;

        switch (active.ability)
        {
            case Ability.Slow:
            case Ability.Burn:
            case Ability.Heal:
                break;
            default:
                return;
        }

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        switch (active.ability)
        {
            case Ability.Slow:
                timer = TryShoot(new Color(0.5f, 0.85f, 1f, 1f), ApplySlow) ? SlowInterval : 0.5f;
                break;

            case Ability.Burn:
                timer = TryShoot(new Color(1f, 0.5f, 0.15f, 1f), ApplyBurn) ? BurnInterval : 0.5f;
                break;

            case Ability.Heal:
                timer = TryHeal() ? HealInterval : 1f;
                break;
        }
    }

    // En yakın düşmana kısa bir atış; varınca etkiyi uygular.
    bool TryShoot(Color color, System.Action<EnemyHealth> effect)
    {
        EnemyHealth target = FindNearestEnemy();

        if (target == null)
            return false;

        StartCoroutine(ShotRoutine(target, color, effect));
        return true;
    }

    IEnumerator ShotRoutine(EnemyHealth target, Color color, System.Action<EnemyHealth> effect)
    {
        GameObject shot = new GameObject("DroneShot");
        SpriteRenderer renderer = shot.AddComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSprites.SoftCircle;
        renderer.color = color;
        renderer.sortingOrder = 57;

        UITheme theme = UITheme.Load();
        if (theme != null && theme.unlitEffectMaterial != null)
            renderer.sharedMaterial = theme.unlitEffectMaterial;

        shot.transform.localScale = Vector3.one * 0.45f;

        Vector3 start = transform.position;
        const float duration = 0.18f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null || !target.gameObject.activeInHierarchy || target.IsDead())
            {
                Destroy(shot);
                yield break;
            }

            elapsed += Time.deltaTime;
            shot.transform.position = Vector3.Lerp(start, target.transform.position, elapsed / duration);
            yield return null;
        }

        Destroy(shot);

        if (target != null && !target.IsDead())
        {
            effect(target);
            ExplosionBlast.Play(target.transform.position, 0.45f, Color.white, color);
        }
    }

    void ApplySlow(EnemyHealth target)
    {
        EnemyMovement movement = target.GetComponent<EnemyMovement>();

        if (movement != null)
        {
            movement.ApplyFreeze(Mathf.Min(0.8f, SlowAmount * active.strength), SlowDuration);
        }
    }

    void ApplyBurn(EnemyHealth target)
    {
        int tick = Mathf.Max(1, Mathf.RoundToInt(BurnBaseTick * active.strength * GetPlayerDamageScale()));
        target.ApplyBurn(tick, BurnDuration, BurnTickRate);
    }

    // Yanma hasarı oyuncunun hasar çarpanıyla büyüsün (geç oyunda da işe yarasın).
    static float GetPlayerDamageScale()
    {
        float scale = 1f;

        if (PlayerStats.Instance != null)
            scale *= Mathf.Max(1f, PlayerStats.Instance.damageMultiplier);

        if (UpgradeManager.Instance != null)
            scale *= Mathf.Max(1f, UpgradeManager.Instance.WeaponDamageMultiplier);

        return scale;
    }

    bool TryHeal()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            playerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
        }

        if (playerHealth == null || playerHealth.IsDead || playerHealth.IsFullHealth)
            return false;

        int amount = Mathf.Max(1, Mathf.RoundToInt(playerHealth.maxHealth * HealFraction * active.strength));
        playerHealth.Heal(amount);

        ExplosionBlast.Play(playerHealth.transform.position, 0.8f, Color.white, new Color(0.4f, 1f, 0.5f, 1f));
        return true;
    }

    EnemyHealth FindNearestEnemy()
    {
        EnemyHealth best = null;
        float bestDistance = ShotRange * ShotRange;
        Vector3 origin = transform.position;

        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>())
        {
            if (enemy.IsDead())
                continue;

            float distance = (enemy.transform.position - origin).sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = enemy;
            }
        }

        return best;
    }
}
