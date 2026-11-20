using UnityEngine;

[CreateAssetMenu(
    fileName = "NewUpgradeCard",
    menuName = "Upgrade System/Upgrade Card"
)]
public class UpgradeCardData : ScriptableObject
{
    public enum UpgradeType
    {
        Damage,
        FireRate,
        MaxHealth,
        BulletSpeed,
        CriticalChance,
        Piercing,
        ExtraBullet,
        ExplosiveBullet,
        FreezeBullet,
        BurnBullet,
        ChainLightning,

        // Yeni düşmanlara karşı (kodla oluşturulur, asset gerekmez)
        ShieldBreaker,
        SplitStopper,
        BossHunter
    }

    public enum Rarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [Header("Kart Bilgileri")]
    public string cardName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    [Header("Upgrade")]
    public UpgradeType upgradeType;

    public float floatValue = 0.2f;
    public int intValue = 1;

    [Header("Level")]
    public int maxLevel = 5;
}