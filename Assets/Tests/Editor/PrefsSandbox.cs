using System.Collections.Generic;
using UnityEngine;

// Testler gerçek PlayerPrefs'e yazar. Bu yardımcı, testin dokunacağı anahtarları
// başta yedekler ve sonda eski hâline getirir; böylece editördeki kayıtların bozulmaz.
public class PrefsSandbox
{
    private const string Missing = "\u0000__missing__";

    private struct Saved
    {
        public bool exists;
        public string text;
        public int number;
        public float real;
    }

    private readonly Dictionary<string, Saved> saved = new Dictionary<string, Saved>();

    public PrefsSandbox(IEnumerable<string> keys)
    {
        foreach (string key in keys)
        {
            saved[key] = new Saved
            {
                exists = PlayerPrefs.HasKey(key),
                text = PlayerPrefs.GetString(key, Missing),
                number = PlayerPrefs.GetInt(key, int.MinValue),
                real = PlayerPrefs.GetFloat(key, float.NaN)
            };

            PlayerPrefs.DeleteKey(key);
        }
    }

    public void Restore()
    {
        foreach (KeyValuePair<string, Saved> entry in saved)
        {
            Saved value = entry.Value;

            if (!value.exists)
            {
                PlayerPrefs.DeleteKey(entry.Key);
            }
            else if (value.text != Missing)
            {
                PlayerPrefs.SetString(entry.Key, value.text);
            }
            else if (value.number != int.MinValue)
            {
                PlayerPrefs.SetInt(entry.Key, value.number);
            }
            else
            {
                PlayerPrefs.SetFloat(entry.Key, value.real);
            }
        }

        PlayerPrefs.Save();
    }

    // İlerlemeye dokunan testlerin ortak anahtar listesi
    public static List<string> ProgressKeys()
    {
        List<string> keys = new List<string>
        {
            GameKeys.TotalGold,
            GameKeys.ProgressModifiedAt,
            GameKeys.OwnedWeapons,
            GameKeys.SelectedWeapon,
            GameKeys.OwnedCompanionSkins,
            GameKeys.SelectedCompanionSkin,
            GameKeys.GrantedTransactions,
            GameKeys.StatBestLevel,
            GameKeys.Difficulty,
            GameKeys.QuestDate,
            GameKeys.QuestDate + "_List",
            GameKeys.WeeklyQuestWeek,
            GameKeys.WeeklyQuestWeek + "_List",
            GameKeys.WeeklyChestClaimed,
            GameKeys.SeasonIndex,
            GameKeys.SeasonPoints,
            GameKeys.SeasonPremium,
            GameKeys.SeasonFreeClaimed,
            GameKeys.SeasonPremiumClaimed,
            GameKeys.OwnedBulletSkins,
            GameKeys.SelectedBulletSkin,
            GameKeys.ChallengeDate,
            GameKeys.ChallengeBest
        };

        for (int i = 0; i < QuestBoard.QuestCount; i++)
        {
            keys.Add(GameKeys.QuestProgressPrefix + i);
            keys.Add(GameKeys.QuestClaimedPrefix + i);
            keys.Add(GameKeys.WeeklyQuestProgressPrefix + i);
            keys.Add(GameKeys.WeeklyQuestClaimedPrefix + i);
        }

        foreach (WeaponCatalog.Weapon weapon in WeaponCatalog.All)
        {
            keys.Add(GameKeys.WeaponLevelPrefix + weapon.id);
        }

        return keys;
    }
}
