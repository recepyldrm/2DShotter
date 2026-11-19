using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    // ==================================
    // DAMAGE
    // ==================================

    [Header("Damage")]
    public float damageMultiplier = 1f;

    // ==================================
    // FIRE RATE
    // ==================================

    [Header("Fire Rate")]
    public float fireRateMultiplier = 1f;

    // ==================================
    // BULLET
    // ==================================

    [Header("Bullet")]
    public float bulletSpeedMultiplier = 1f;
    public int extraBullets = 0;
    public int piercing = 0;

    // ==================================
    // UPGRADE LEVELS
    // ==================================

    [Header("Upgrade Levels")]
    public int damageLevel = 0;
    public int fireRateLevel = 0;
    public int maxHealthLevel = 0;
    public int bulletSpeedLevel = 0;
    public int criticalLevel = 0;
    public int piercingLevel = 0;
    public int extraBulletLevel = 0;

    // ==================================
    // CHAIN LIGHTNING
    // ==================================

    [Header("Chain Lightning")]
    public bool chainLightning = false;

    public int chainLightningLevel = 0;

    public int chainCount = 2;

    public float chainRange = 3f;

    [Range(0f, 1f)]
    public float chainDamageMultiplier = 0.5f;

    public int chainCountPerLevel = 1;

    public float chainRangePerLevel = 0.25f;

    // ==================================
    // EXPLOSIVE BULLET
    // ==================================

    [Header("Explosive Bullet")]
    public bool explosiveBullets = false;

    public int explosiveLevel = 0;

    public float explosionRadius = 1.5f;

    public float explosionDamageMultiplier = 0.5f;

    public float explosionRadiusPerLevel = 0.25f;

    public float explosionDamagePerLevel = 0.10f;

    // ==================================
    // FREEZE BULLET
    // ==================================

    [Header("Freeze Bullet")]
    public bool freezeBullets = false;

    public int freezeLevel = 0;

    [Range(0f, 0.8f)]
    public float freezeSlowAmount = 0.20f;

    public float freezeDuration = 2f;

    public float freezeSlowPerLevel = 0.10f;

    public float freezeDurationPerLevel = 0.25f;

    // ==================================
    // BURN BULLET
    // ==================================

    [Header("Burn Bullet")]
    public bool burnBullets = false;

    public int burnLevel = 0;

    public int burnDamage = 5;

    public float burnDuration = 3f;

    public float burnTickRate = 1f;

    public int burnDamagePerLevel = 3;

    // ==================================
    // CRITICAL
    // ==================================

    [Header("Critical")]

    [Range(0f, 1f)]
    public float criticalChance = 0f;

    public float criticalDamageMultiplier = 2f;

    // ==================================
    // HEALTH
    // ==================================

    [Header("Health")]
    public int bonusMaxHealth = 0;

    // ==================================
    // AWAKE
    // ==================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ana menüden alınan kalıcı yükseltmeler
        damageMultiplier += PermanentUpgrades.BonusDamage;
        fireRateMultiplier += PermanentUpgrades.BonusFireRate;
    }

    // ==================================
    // DAMAGE
    // ==================================

    public void UpgradeDamage(float amount)
    {
        damageLevel++;

        damageMultiplier += amount;

        GameLog.Log(
            "DAMAGE LV." +
            damageLevel +
            " | Multiplier: " +
            damageMultiplier
        );
    }

    // ==================================
    // FIRE RATE
    // ==================================

    public void UpgradeFireRate(float amount)
    {
        fireRateLevel++;

        fireRateMultiplier += amount;

        GameLog.Log(
            "FIRE RATE LV." +
            fireRateLevel +
            " | Multiplier: " +
            fireRateMultiplier
        );
    }

    // ==================================
    // BULLET SPEED
    // ==================================

    public void UpgradeBulletSpeed(float amount)
    {
        bulletSpeedLevel++;

        bulletSpeedMultiplier += amount;

        GameLog.Log(
            "BULLET SPEED LV." +
            bulletSpeedLevel +
            " | Multiplier: " +
            bulletSpeedMultiplier
        );
    }

    // ==================================
    // EXTRA BULLET / MULTI SHOT
    // ==================================

    public void AddExtraBullet(int amount = 1)
    {
        extraBulletLevel++;

        extraBullets += Mathf.Max(1, amount);

        GameLog.Log(
            "MULTI SHOT LV." +
            extraBulletLevel +
            " | Extra Bullets: " +
            extraBullets
        );
    }

    // ==================================
    // PIERCING
    // ==================================

    public void AddPiercing(int amount = 1)
    {
        piercingLevel++;

        piercing += Mathf.Max(1, amount);

        GameLog.Log(
            "PIERCING LV." +
            piercingLevel +
            " | Piercing: " +
            piercing
        );
    }

    // ==================================
    // CRITICAL
    // ==================================

    public void UpgradeCriticalChance(float amount)
    {
        criticalLevel++;

        criticalChance += amount;

        criticalChance =
            Mathf.Clamp01(
                criticalChance
            );

        GameLog.Log(
            "CRITICAL LV." +
            criticalLevel +
            " | Chance: %" +
            (criticalChance * 100f)
        );
    }

    // ==================================
    // EXPLOSIVE BULLET
    // ==================================

    // ==================================
    // RARITY ADIMI
    // ==================================

    // Özel mermi kartlarında seviye başı artışın kaç katı uygulanacağı.
    // İlk seviye özelliği açar; rarity Common'dan yüksekse ekstra
    // (çarpan - 1) adım bonus verir. Sonraki seviyelerde adım x çarpan.
    // Örn. Legendary (x3): ilk alışta +2 adım, sonrakilerde +3 adım.
    public static float RarityStep(int newLevel, float rarityMultiplier)
    {
        rarityMultiplier = Mathf.Max(1f, rarityMultiplier);

        return newLevel > 1
            ? rarityMultiplier
            : rarityMultiplier - 1f;
    }

    // Diğer mermi kartları gibi kalıcıdır: ilk seçim açar,
    // sonraki seviyeler yarıçapı ve hasarı artırır.
    public void UpgradeExplosiveBullets(float rarityMultiplier = 1f)
    {
        explosiveLevel++;

        explosiveBullets = true;

        float step =
            RarityStep(
                explosiveLevel,
                rarityMultiplier
            );

        explosionRadius +=
            explosionRadiusPerLevel * step;

        explosionDamageMultiplier +=
            explosionDamagePerLevel * step;

        GameLog.Log(
            "EXPLOSIVE LV." +
            explosiveLevel +
            " | Radius: " +
            explosionRadius +
            " | Damage Multiplier: " +
            explosionDamageMultiplier
        );
    }

    // ==================================
    // FREEZE BULLET
    // ==================================

    public void UpgradeFreezeBullets(float rarityMultiplier = 1f)
    {
        freezeLevel++;

        freezeBullets = true;

        float step =
            RarityStep(
                freezeLevel,
                rarityMultiplier
            );

        freezeSlowAmount +=
            freezeSlowPerLevel * step;

        freezeDuration +=
            freezeDurationPerLevel * step;

        freezeSlowAmount =
            Mathf.Clamp(
                freezeSlowAmount,
                0f,
                0.8f
            );

        GameLog.Log(
            "FREEZE LV." +
            freezeLevel +
            " | Slow: %" +
            (freezeSlowAmount * 100f) +
            " | Duration: " +
            freezeDuration
        );
    }

    // ==================================
    // BURN BULLET
    // ==================================

    public void UpgradeBurnBullets(float rarityMultiplier = 1f)
    {
        burnLevel++;

        burnBullets = true;

        burnDamage +=
            Mathf.RoundToInt(
                burnDamagePerLevel *
                RarityStep(
                    burnLevel,
                    rarityMultiplier
                )
            );

        GameLog.Log(
            "BURN LV." +
            burnLevel +
            " | Damage: " +
            burnDamage +
            "/sn" +
            " | Duration: " +
            burnDuration
        );
    }

    // ==================================
    // CHAIN LIGHTNING
    // ==================================

    public void UpgradeChainLightning(float rarityMultiplier = 1f)
    {
        chainLightningLevel++;

        chainLightning = true;

        float step =
            RarityStep(
                chainLightningLevel,
                rarityMultiplier
            );

        chainCount +=
            Mathf.RoundToInt(
                chainCountPerLevel * step
            );

        chainRange +=
            chainRangePerLevel * step;

        GameLog.Log(
            "CHAIN LIGHTNING LV." +
            chainLightningLevel +
            " | Chain: " +
            chainCount +
            " | Range: " +
            chainRange
        );
    }

    // ==================================
    // MAX HEALTH
    // ==================================

    public void UpgradeMaxHealth(int amount)
    {
        maxHealthLevel++;

        bonusMaxHealth += amount;

        PlayerHealth playerHealth =
            FindAnyObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.IncreaseMaxHealth(
                amount
            );
        }

        GameLog.Log(
            "MAX HEALTH LV." +
            maxHealthLevel +
            " | Bonus Health: " +
            bonusMaxHealth
        );
    }

    // ==================================
    // UPGRADE LEVEL GET
    // ==================================

    public int GetUpgradeLevel(
        UpgradeCardData.UpgradeType type
    )
    {
        switch (type)
        {
            case UpgradeCardData.UpgradeType.Damage:
                return damageLevel;

            case UpgradeCardData.UpgradeType.FireRate:
                return fireRateLevel;

            case UpgradeCardData.UpgradeType.MaxHealth:
                return maxHealthLevel;

            case UpgradeCardData.UpgradeType.BulletSpeed:
                return bulletSpeedLevel;

            case UpgradeCardData.UpgradeType.CriticalChance:
                return criticalLevel;

            case UpgradeCardData.UpgradeType.Piercing:
                return piercingLevel;

            case UpgradeCardData.UpgradeType.ExtraBullet:
                return extraBulletLevel;

            case UpgradeCardData.UpgradeType.ExplosiveBullet:
                return explosiveLevel;

            case UpgradeCardData.UpgradeType.FreezeBullet:
                return freezeLevel;

            case UpgradeCardData.UpgradeType.BurnBullet:
                return burnLevel;

            case UpgradeCardData.UpgradeType.ChainLightning:
                return chainLightningLevel;

            default:
                return 0;
        }
    }
}