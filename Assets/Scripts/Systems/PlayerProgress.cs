using System.Collections.Generic;
using UnityEngine;

// Oyunlar arası kalıcı veriler (PlayerPrefs):
// - Kalıcı gold: her oyun sonunda o oyunda kazanılan gold eklenir,
//   skin satın alırken harcanır. Oyun içi gold (reroll / shop) ayrıdır.
// - Dost skinleri: sahip olunanlar ve seçili olan.
public static class PlayerProgress
{
    private const char SkinSeparator = ',';

    // ==================================
    // KALICI GOLD
    // ==================================

    public static int TotalGold =>
        PlayerPrefs.GetInt(GameKeys.TotalGold, 0);

    public static event System.Action<int> OnTotalGoldChanged;

    public static void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        SetTotalGold(TotalGold + amount);
    }

    public static bool TrySpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        int total = TotalGold;

        if (total < amount)
            return false;

        SetTotalGold(total - amount);
        return true;
    }

    static void SetTotalGold(int value)
    {
        PlayerPrefs.SetInt(GameKeys.TotalGold, Mathf.Max(0, value));
        Touch();
        PlayerPrefs.Save();

        OnTotalGoldChanged?.Invoke(TotalGold);
    }

    // ==================================
    // DEĞİŞİKLİK TAKİBİ (bulut kayıt için)
    // ==================================

    // Bulut kayıt bunu dinleyip değişiklikleri yükler.
    public static event System.Action OnSaveDataChanged;

    // İlerleme değişti: zamanı güncelle (iki cihaz birleştirilirken
    // gold gibi değerlerde en son değişen kazanır).
    static void Touch()
    {
        PlayerPrefs.SetString(
            GameKeys.ProgressModifiedAt,
            System.DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)
        );

        OnSaveDataChanged?.Invoke();
    }

    // Buluttan gelen kayıt uygulandıktan sonra bütün ekranlar güncellensin.
    public static void NotifyAllChanged()
    {
        OnTotalGoldChanged?.Invoke(TotalGold);
        OnProgressChanged?.Invoke();
    }

    // ==================================
    // İSTATİSTİKLER (başarımlar için)
    // ==================================

    public static int TotalKills => PlayerPrefs.GetInt(GameKeys.StatTotalKills, 0);
    public static int TotalRuns => PlayerPrefs.GetInt(GameKeys.StatTotalRuns, 0);
    public static int BestLevel => PlayerPrefs.GetInt(GameKeys.StatBestLevel, 0);
    public static int BestScore => PlayerPrefs.GetInt(GameKeys.HighScore, 0);

    // Sadece oyun içinde kazanılan gold (satın alma / ödül hariç)
    public static int TotalGoldEarned => PlayerPrefs.GetInt(GameKeys.StatTotalGoldEarned, 0);

    public static int TotalBossKills => PlayerPrefs.GetInt(GameKeys.StatTotalBossKills, 0);

    // Başarım ekranı / bildirim rozetleri günceller.
    public static event System.Action OnProgressChanged;

    // Oyun sonunda (veya sahneden çıkarken) toplu olarak çağrılır,
    // her öldürmede diske yazmamak için.
    public static void RecordRunProgress(int kills, int goldEarned, int levelReached, bool countRun, int bossKills = 0)
    {
        if (kills > 0)
        {
            PlayerPrefs.SetInt(GameKeys.StatTotalKills, TotalKills + kills);
        }

        if (bossKills > 0)
        {
            PlayerPrefs.SetInt(GameKeys.StatTotalBossKills, TotalBossKills + bossKills);
        }

        if (goldEarned > 0)
        {
            PlayerPrefs.SetInt(GameKeys.StatTotalGoldEarned, TotalGoldEarned + goldEarned);
        }

        if (levelReached > BestLevel)
        {
            PlayerPrefs.SetInt(GameKeys.StatBestLevel, levelReached);
        }

        if (countRun)
        {
            PlayerPrefs.SetInt(GameKeys.StatTotalRuns, TotalRuns + 1);
        }

        Touch();
        PlayerPrefs.Save();
        NotifyProgressChanged();
    }

    public static void NotifyProgressChanged()
    {
        OnProgressChanged?.Invoke();
    }

    // ==================================
    // KALICI YÜKSELTMELER
    // ==================================

    public static int GetPermanentUpgradeLevel(string upgradeId)
    {
        return PlayerPrefs.GetInt(GameKeys.PermanentUpgradePrefix + upgradeId, 0);
    }

    public static void SetPermanentUpgradeLevel(string upgradeId, int level)
    {
        PlayerPrefs.SetInt(GameKeys.PermanentUpgradePrefix + upgradeId, Mathf.Max(0, level));
        Touch();
        PlayerPrefs.Save();
        NotifyProgressChanged();
    }

    // ==================================
    // BAŞARIM ÖDÜLLERİ
    // ==================================

    public static bool IsAchievementClaimed(string achievementId)
    {
        return ReadSet(GameKeys.ClaimedAchievements).Contains(achievementId);
    }

    public static void MarkAchievementClaimed(string achievementId)
    {
        HashSet<string> claimed = ReadSet(GameKeys.ClaimedAchievements);

        if (!claimed.Add(achievementId))
            return;

        WriteSet(GameKeys.ClaimedAchievements, claimed);
        NotifyProgressChanged();
    }

    static HashSet<string> ReadSet(string key)
    {
        HashSet<string> set = new HashSet<string>();

        foreach (string id in PlayerPrefs.GetString(key, string.Empty).Split(SkinSeparator))
        {
            if (!string.IsNullOrEmpty(id))
            {
                set.Add(id);
            }
        }

        return set;
    }

    static void WriteSet(string key, HashSet<string> set)
    {
        PlayerPrefs.SetString(key, string.Join(SkinSeparator.ToString(), set));
        Touch();
        PlayerPrefs.Save();
    }

    // ==================================
    // DOST SKINLERİ
    // ==================================

    public static string SelectedCompanionSkin
    {
        get => PlayerPrefs.GetString(GameKeys.SelectedCompanionSkin, string.Empty);
        set
        {
            PlayerPrefs.SetString(GameKeys.SelectedCompanionSkin, value ?? string.Empty);
            Touch();
            PlayerPrefs.Save();
        }
    }

    public static bool IsCompanionSkinOwned(string skinId)
    {
        if (string.IsNullOrEmpty(skinId))
            return false;

        return GetOwnedCompanionSkins().Contains(skinId);
    }

    public static void UnlockCompanionSkin(string skinId)
    {
        if (string.IsNullOrEmpty(skinId))
            return;

        HashSet<string> owned = GetOwnedCompanionSkins();

        if (!owned.Add(skinId))
            return;

        PlayerPrefs.SetString(
            GameKeys.OwnedCompanionSkins,
            string.Join(SkinSeparator.ToString(), owned)
        );
        Touch();
        PlayerPrefs.Save();
    }

    // ==================================
    // GERÇEK PARA SATIN ALIMLARI
    // ==================================

    private const int MaxRememberedTransactions = 50;

    // Aynı satın alma (örn. onay yarıda kaldığı için tekrar gelirse)
    // iki kez ödül vermesin. İlk kez görülüyorsa true döner ve kaydeder.
    public static bool TryMarkTransactionGranted(string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId))
            return true;

        string saved = PlayerPrefs.GetString(GameKeys.GrantedTransactions, string.Empty);

        List<string> granted = new List<string>(
            saved.Split(new[] { SkinSeparator }, System.StringSplitOptions.RemoveEmptyEntries)
        );

        if (granted.Contains(transactionId))
            return false;

        granted.Add(transactionId);

        if (granted.Count > MaxRememberedTransactions)
        {
            granted.RemoveRange(0, granted.Count - MaxRememberedTransactions);
        }

        PlayerPrefs.SetString(
            GameKeys.GrantedTransactions,
            string.Join(SkinSeparator.ToString(), granted)
        );
        PlayerPrefs.Save();

        return true;
    }

    static HashSet<string> GetOwnedCompanionSkins()
    {
        string saved = PlayerPrefs.GetString(GameKeys.OwnedCompanionSkins, string.Empty);

        HashSet<string> owned = new HashSet<string>();

        foreach (string id in saved.Split(SkinSeparator))
        {
            if (!string.IsNullOrEmpty(id))
            {
                owned.Add(id);
            }
        }

        return owned;
    }
}
