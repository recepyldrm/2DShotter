using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Oyuncunun bütün kalıcı ilerlemesi tek pakette (bulut kayıt için).
// Ses ayarları, eğitim gibi cihaza özel şeyler dahil değildir.
[Serializable]
public class SaveSnapshot
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;

    // Son değişiklik zamanı (UTC, Unix saniye). "En son değişen kazanır"
    // kuralı için: gold, seçili skin, günlük ödül.
    public long modifiedAtUtc;

    public int totalGold;
    public string selectedSkin;
    public string dailyLastClaimDate;
    public int dailyLastClaimIndex = -1;

    // Birleşim (union): hiçbir cihazdaki kazanım kaybolmaz
    public List<string> ownedSkins = new List<string>();
    public List<string> claimedAchievements = new List<string>();

    // Kalıcı yükseltmeler "id:seviye" olarak; birleşirken yüksek seviye kazanır
    public List<string> permanentUpgrades = new List<string>();

    // En büyük değer kazanır
    public int highScore;
    public int totalKills;
    public int totalRuns;
    public int bestLevel;
    public int totalGoldEarned;
    public int totalBossKills;

    public string ToJson() => JsonUtility.ToJson(this);

    public static SaveSnapshot FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonUtility.FromJson<SaveSnapshot>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("SaveSnapshot: Bozuk kayıt okunamadı: " + exception.Message);
            return null;
        }
    }

    // ==================================
    // CİHAZ <-> PAKET
    // ==================================

    private const char Separator = ',';

    public static SaveSnapshot CaptureLocal()
    {
        return new SaveSnapshot
        {
            modifiedAtUtc = long.Parse(
                PlayerPrefs.GetString(GameKeys.ProgressModifiedAt, "0"),
                CultureInfo.InvariantCulture
            ),
            totalGold = PlayerProgress.TotalGold,
            selectedSkin = PlayerProgress.SelectedCompanionSkin,
            dailyLastClaimDate = PlayerPrefs.GetString(GameKeys.DailyLastClaimDate, string.Empty),
            dailyLastClaimIndex = PlayerPrefs.GetInt(GameKeys.DailyLastClaimIndex, -1),
            ownedSkins = ReadList(GameKeys.OwnedCompanionSkins),
            claimedAchievements = ReadList(GameKeys.ClaimedAchievements),
            permanentUpgrades = CaptureUpgrades(),
            highScore = PlayerProgress.BestScore,
            totalKills = PlayerProgress.TotalKills,
            totalRuns = PlayerProgress.TotalRuns,
            bestLevel = PlayerProgress.BestLevel,
            totalGoldEarned = PlayerProgress.TotalGoldEarned,
            totalBossKills = PlayerProgress.TotalBossKills
        };
    }

    // Paketi cihaza yazar (değişiklik zamanını da paketinkiyle aynı yapar).
    public void ApplyToLocal()
    {
        PlayerPrefs.SetInt(GameKeys.TotalGold, Mathf.Max(0, totalGold));
        PlayerPrefs.SetString(GameKeys.SelectedCompanionSkin, selectedSkin ?? string.Empty);
        PlayerPrefs.SetString(GameKeys.DailyLastClaimDate, dailyLastClaimDate ?? string.Empty);
        PlayerPrefs.SetInt(GameKeys.DailyLastClaimIndex, dailyLastClaimIndex);
        PlayerPrefs.SetString(GameKeys.OwnedCompanionSkins, string.Join(Separator.ToString(), ownedSkins));
        PlayerPrefs.SetString(GameKeys.ClaimedAchievements, string.Join(Separator.ToString(), claimedAchievements));
        foreach (KeyValuePair<string, int> upgrade in ParseUpgrades(permanentUpgrades))
        {
            PlayerPrefs.SetInt(GameKeys.PermanentUpgradePrefix + upgrade.Key, upgrade.Value);
        }

        PlayerPrefs.SetInt(GameKeys.HighScore, highScore);
        PlayerPrefs.SetInt(GameKeys.StatTotalKills, totalKills);
        PlayerPrefs.SetInt(GameKeys.StatTotalRuns, totalRuns);
        PlayerPrefs.SetInt(GameKeys.StatBestLevel, bestLevel);
        PlayerPrefs.SetInt(GameKeys.StatTotalGoldEarned, totalGoldEarned);
        PlayerPrefs.SetInt(GameKeys.StatTotalBossKills, totalBossKills);
        PlayerPrefs.SetString(GameKeys.ProgressModifiedAt, modifiedAtUtc.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.Save();

        PlayerProgress.NotifyAllChanged();
    }

    // ==================================
    // BİRLEŞTİRME
    // ==================================

    // İki cihazın ilerlemesini birleştirir. Hiçbir kazanım kaybolmaz:
    // skin / başarım birleşimi, istatistiklerin büyüğü, gold vb. en son değişenden.
    public static SaveSnapshot Merge(SaveSnapshot a, SaveSnapshot b)
    {
        if (a == null) return b;
        if (b == null) return a;

        SaveSnapshot newer = a.modifiedAtUtc >= b.modifiedAtUtc ? a : b;

        SaveSnapshot merged = new SaveSnapshot
        {
            modifiedAtUtc = Math.Max(a.modifiedAtUtc, b.modifiedAtUtc),
            totalGold = newer.totalGold,
            selectedSkin = newer.selectedSkin,
            dailyLastClaimDate = newer.dailyLastClaimDate,
            dailyLastClaimIndex = newer.dailyLastClaimIndex,
            ownedSkins = Union(a.ownedSkins, b.ownedSkins),
            claimedAchievements = Union(a.claimedAchievements, b.claimedAchievements),
            permanentUpgrades = MergeUpgrades(a.permanentUpgrades, b.permanentUpgrades),
            highScore = Math.Max(a.highScore, b.highScore),
            totalKills = Math.Max(a.totalKills, b.totalKills),
            totalRuns = Math.Max(a.totalRuns, b.totalRuns),
            bestLevel = Math.Max(a.bestLevel, b.bestLevel),
            totalGoldEarned = Math.Max(a.totalGoldEarned, b.totalGoldEarned),
            totalBossKills = Math.Max(a.totalBossKills, b.totalBossKills)
        };

        return merged;
    }

    public bool ContentEquals(SaveSnapshot other)
    {
        return other != null && ToJson() == other.ToJson();
    }

    static List<string> Union(List<string> a, List<string> b)
    {
        HashSet<string> set = new HashSet<string>();

        if (a != null) set.UnionWith(a);
        if (b != null) set.UnionWith(b);

        set.Remove(string.Empty);

        List<string> list = new List<string>(set);
        list.Sort(StringComparer.Ordinal);
        return list;
    }

    // ==================================
    // KALICI YÜKSELTMELER
    // ==================================

    static List<string> CaptureUpgrades()
    {
        List<string> list = new List<string>();

        foreach (PermanentUpgrades.Upgrade upgrade in PermanentUpgrades.All)
        {
            int level = PermanentUpgrades.GetLevel(upgrade);

            if (level > 0)
            {
                list.Add(upgrade.id + ":" + level.ToString(CultureInfo.InvariantCulture));
            }
        }

        return list;
    }

    static Dictionary<string, int> ParseUpgrades(List<string> entries)
    {
        Dictionary<string, int> levels = new Dictionary<string, int>();

        if (entries == null)
            return levels;

        foreach (string entry in entries)
        {
            int separator = entry != null ? entry.LastIndexOf(':') : -1;

            if (separator <= 0)
                continue;

            if (int.TryParse(entry.Substring(separator + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
            {
                levels[entry.Substring(0, separator)] = level;
            }
        }

        return levels;
    }

    static List<string> MergeUpgrades(List<string> a, List<string> b)
    {
        Dictionary<string, int> merged = ParseUpgrades(a);

        foreach (KeyValuePair<string, int> upgrade in ParseUpgrades(b))
        {
            merged.TryGetValue(upgrade.Key, out int existing);
            merged[upgrade.Key] = Math.Max(existing, upgrade.Value);
        }

        List<string> list = new List<string>();

        foreach (KeyValuePair<string, int> upgrade in merged)
        {
            list.Add(upgrade.Key + ":" + upgrade.Value.ToString(CultureInfo.InvariantCulture));
        }

        list.Sort(StringComparer.Ordinal);
        return list;
    }

    static List<string> ReadList(string key)
    {
        List<string> list = new List<string>();

        foreach (string id in PlayerPrefs.GetString(key, string.Empty).Split(Separator))
        {
            if (!string.IsNullOrEmpty(id))
            {
                list.Add(id);
            }
        }

        list.Sort(StringComparer.Ordinal);
        return list;
    }
}
