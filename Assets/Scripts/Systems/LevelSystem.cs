using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LevelSystem : MonoBehaviour
{
    public static LevelSystem Instance;

    [Header("Level")]
    public int currentLevel = 1;

    [Header("XP")]
    public int currentXP = 0;
    public int xpNeeded = 100;

    [Header("XP Artışı")]
    public float xpMultiplier = 1.25f;

    [Header("UI")]
    public Slider xpSlider;
    public TextMeshProUGUI levelText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateUI();
    }

    public void AddXP(int amount)
    {
        currentXP += amount;

        GameLog.Log("XP GELDİ: " + currentXP + " / " + xpNeeded);

        // Tek seferde birden fazla level atlanabilir.
        // Kart ekranı açıkken gelen level-up'ları LevelUpCardManager sıraya alır.
        while (xpNeeded > 0 && currentXP >= xpNeeded)
        {
            currentXP -= xpNeeded;
            LevelUp();
        }

        UpdateUI();
    }

    void LevelUp()
    {
        currentLevel++;

        Haptics.Light();

        GameAnalytics.LevelReached(currentLevel, Time.timeSinceLevelLoad);

        // Her 5 seviyede bir boss (kart seçildikten sonra gelir)
        BossWaveManager.NotifyLevelUp(currentLevel);

        xpNeeded = Mathf.RoundToInt(
            xpNeeded * xpMultiplier
        );

        GameLog.Log(
            "LEVEL UP! Yeni Level: " +
            currentLevel
        );

        UpdateUI();

        // Düşmanları güçlendir
        if (EnemyScalingManager.Instance != null)
        {
            EnemyScalingManager.Instance.IncreaseDifficulty();
        }

        // Kart ekranını aç
        if (LevelUpCardManager.Instance != null)
        {
            LevelUpCardManager.Instance.ShowCards();
        }
    }

    void UpdateUI()
    {
        if (xpSlider != null)
        {
            xpSlider.minValue = 0;
            xpSlider.maxValue = xpNeeded;
            xpSlider.value = currentXP;
        }

        if (levelText != null)
        {
            levelText.text = "SV. " + currentLevel;
        }
    }
}