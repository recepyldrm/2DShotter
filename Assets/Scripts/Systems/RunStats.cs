using System.Collections.Generic;
using UnityEngine;

// Tek bir oyunun istatistikleri: oyun sonu özeti ve yeni görev türleri için.
// GameManager oyun başında Reset çağırır.
public static class RunStats
{
    public enum DamageSource
    {
        Weapon,     // Normal ve kritik mermi hasarı
        Burn,       // Yakıcı mermi kartı / drone
        Chain,      // Zincir yıldırım kartı
        Explosion   // Patlayıcı mermi kartı / roketatar
    }

    private static readonly Dictionary<DamageSource, long> damage = new Dictionary<DamageSource, long>();

    public static int RangedKills { get; private set; }
    public static int ShieldedKills { get; private set; }
    public static int SplitterKills { get; private set; }
    public static int BossKills { get; private set; }
    public static int BossStuns { get; private set; }
    public static int ShieldsBroken { get; private set; }
    public static int KillGold { get; private set; }

    public static void Reset()
    {
        damage.Clear();
        RangedKills = 0;
        ShieldedKills = 0;
        SplitterKills = 0;
        BossKills = 0;
        BossStuns = 0;
        ShieldsBroken = 0;
        KillGold = 0;
    }

    public static void AddDamage(DamageNumbers.Style style, int amount)
    {
        if (amount <= 0)
            return;

        DamageSource source;

        switch (style)
        {
            case DamageNumbers.Style.Burn: source = DamageSource.Burn; break;
            case DamageNumbers.Style.Chain: source = DamageSource.Chain; break;
            case DamageNumbers.Style.Explosion: source = DamageSource.Explosion; break;
            case DamageNumbers.Style.Shield: return;
            default: source = DamageSource.Weapon; break;
        }

        damage.TryGetValue(source, out long current);
        damage[source] = current + amount;
    }

    public static long GetDamage(DamageSource source)
    {
        damage.TryGetValue(source, out long value);
        return value;
    }

    public static long TotalDamage
    {
        get
        {
            long total = 0;
            foreach (long value in damage.Values) total += value;
            return total;
        }
    }

    // En çok hasar veren kaynak (hiç hasar yoksa silah)
    public static DamageSource TopSource
    {
        get
        {
            DamageSource best = DamageSource.Weapon;
            long bestValue = -1;

            foreach (KeyValuePair<DamageSource, long> entry in damage)
            {
                if (entry.Value > bestValue)
                {
                    bestValue = entry.Value;
                    best = entry.Key;
                }
            }

            return best;
        }
    }

    public static string SourceName(DamageSource source)
    {
        switch (source)
        {
            case DamageSource.Burn: return Loc.Pick("YAKICI MERMİ", "BURNING BULLETS");
            case DamageSource.Chain: return Loc.Pick("ZİNCİR YILDIRIM", "CHAIN LIGHTNING");
            case DamageSource.Explosion: return Loc.Pick("PATLAYICI MERMİ", "EXPLOSIVE BULLETS");
            default: return Loc.T(WeaponCatalog.Selected.title);
        }
    }

    public static void RegisterKill(EnemyVariant.Kind kind, bool isBoss)
    {
        if (isBoss)
        {
            BossKills++;
            return;
        }

        switch (kind)
        {
            case EnemyVariant.Kind.Ranged:
                RangedKills++;
                QuestBoard.Increment(QuestBoard.QuestType.GunnerKills, 1);
                break;

            case EnemyVariant.Kind.Shielded:
                ShieldedKills++;
                QuestBoard.Increment(QuestBoard.QuestType.ShieldedKills, 1);
                break;

            case EnemyVariant.Kind.Splitter:
                SplitterKills++;
                break;
        }
    }

    public static void RegisterBossStun()
    {
        BossStuns++;
        QuestBoard.Increment(QuestBoard.QuestType.BossStuns, 1);
    }

    public static void RegisterShieldBroken()
    {
        ShieldsBroken++;
    }

    public static void AddKillGold(int amount)
    {
        if (amount > 0)
            KillGold += amount;
    }
}
