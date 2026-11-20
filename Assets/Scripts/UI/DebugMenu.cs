#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Test menüsü: sadece Editor'da ve Development Build'de vardır;
// yayın build'inde bu dosya hiç derlenmez.
// Ekranın sol altındaki "TEST" butonu ya da F1 ile açılır.
// Açıkken oyun durur.
public class DebugMenu : MonoBehaviour
{
    private static readonly float[] Speeds = { 1f, 2f, 4f, 0.5f };

    private UITheme theme;
    private GameObject panel;
    private TextMeshProUGUI statusText;

    // Sadece oyun sahnesinde anlamlı olan butonlar
    private readonly List<GameObject> gameplayButtons = new List<GameObject>();

    private TextMeshProUGUI godModeText;
    private TextMeshProUGUI speedText;
    private TextMeshProUGUI resetAllText;
    private int speedIndex;
    private bool resetAllArmed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        GameObject go = new GameObject("DebugMenu");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugMenu>();
    }

    void Start()
    {
        theme = UITheme.Load();
        Build();
    }

    void OnDestroy()
    {
        TimeScaleController.Resume(this);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;

        if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
        {
            Toggle();
        }
#endif

        if (panel.activeSelf)
        {
            UpdateStatus();
        }
    }

    // ==================================
    // AÇ / KAPAT
    // ==================================

    void Toggle()
    {
        if (panel.activeSelf)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    void Open()
    {
        bool inGame = GameManager.Instance != null;

        foreach (GameObject button in gameplayButtons)
        {
            button.SetActive(inGame);
        }

        resetAllArmed = false;
        resetAllText.text = "TÜM KAYITLARI SİL";

        UpdateLabels();
        panel.SetActive(true);

        TimeScaleController.Pause(this);
    }

    void Close()
    {
        panel.SetActive(false);
        TimeScaleController.Resume(this);
    }

    // ==================================
    // EYLEMLER
    // ==================================

    void AddRunGold()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(1000);
        }
    }

    void AddBankGold()
    {
        PlayerProgress.AddGold(1000);
    }

    void LevelUp()
    {
        LevelSystem levels = LevelSystem.Instance;

        if (levels != null)
        {
            // Menü kapanınca kart ekranı açılır.
            Close();
            levels.AddXP(Mathf.Max(1, levels.xpNeeded - levels.currentXP));
        }
    }

    void SpawnBoss()
    {
        Close();
        BossWaveManager.ForceBoss();
    }

    void OpenCards()
    {
        if (LevelUpCardManager.Instance != null)
        {
            Close();
            LevelUpCardManager.Instance.ShowCards();
        }
    }

    void KillAllEnemies()
    {
        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>())
        {
            if (enemy != null && !enemy.IsDead())
            {
                enemy.TakeDamage(9999999);
            }
        }
    }

    void HealPlayer()
    {
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();

        if (player != null)
        {
            player.Heal(999999);
        }
    }

    void ToggleGodMode()
    {
        PlayerHealth.GodMode = !PlayerHealth.GodMode;
        UpdateLabels();
    }

    void CycleSpeed()
    {
        speedIndex = (speedIndex + 1) % Speeds.Length;
        TimeScaleController.SetDebugSpeed(Speeds[speedIndex]);
        UpdateLabels();
    }

    // Silahlar bir sonraki oyunda geçerli olur (ana menüden seçilir).
    void UnlockAllWeapons()
    {
        foreach (WeaponCatalog.Weapon weapon in WeaponCatalog.All)
        {
            PlayerProgress.UnlockWeapon(weapon.id);
        }
    }

    void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(GameKeys.TutorialSeen);
        PlayerPrefs.DeleteKey(GameKeys.CardHintSeen);
        PlayerPrefs.Save();
    }

    // Düşman ve boss tanıtımları tekrar gösterilsin
    void ResetIntros()
    {
        EnemyIntroUI.ResetAll(EnemyVariant.IntroIds);
        EnemyIntroUI.ResetAll(BossWaveManager.IntroIds);
    }

    void ResetDailyReward()
    {
        PlayerPrefs.DeleteKey(GameKeys.DailyLastClaimDate);
        PlayerPrefs.DeleteKey(GameKeys.DailyLastClaimIndex);
        PlayerPrefs.Save();
        PlayerProgress.NotifyProgressChanged();
    }

    // Yanlışlıkla basılmasın diye iki kez basmak gerekir.
    void ResetAll()
    {
        if (!resetAllArmed)
        {
            resetAllArmed = true;
            resetAllText.text = "EMİN MİSİN? TEKRAR BAS";
            return;
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Close();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdateLabels()
    {
        godModeText.text = PlayerHealth.GodMode ? "ÖLÜMSÜZLÜK: AÇIK" : "ÖLÜMSÜZLÜK: KAPALI";
        speedText.text = "OYUN HIZI: x" + TimeScaleController.DebugSpeed.ToString("0.#");
    }

    void UpdateStatus()
    {
        string text = "Kalıcı altın: " + PlayerProgress.TotalGold;

        if (GameManager.Instance != null)
        {
            text += "   Oyun içi altın: " + GameManager.Instance.gold;
        }

        if (LevelSystem.Instance != null)
        {
            text += "\nSeviye: " + LevelSystem.Instance.currentLevel +
                    "   XP: " + LevelSystem.Instance.currentXP + " / " + LevelSystem.Instance.xpNeeded;
        }

        statusText.text = text;
    }

    // ==================================
    // ARAYÜZ
    // ==================================

    void Build()
    {
        Transform canvas = UIBuilder.CreateCanvas(transform, "DebugCanvas", 2000);
        RectTransform safeRoot = UIBuilder.CreateSafeRoot(canvas);

        // Açma butonu (sol alt)
        Button openButton = UIBuilder.CreateButton(theme, safeRoot, "DebugOpenButton", "TEST", false, out _);
        UIBuilder.Place((RectTransform)openButton.transform, new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(170f, 70f));
        openButton.onClick.AddListener(Toggle);

        // Panel
        RectTransform dim = UIBuilder.CreateDim(canvas, "DebugPanel", 0.6f);
        panel = dim.gameObject;

        RectTransform window = UIBuilder.CreateWindow(
            theme,
            UIBuilder.CreateSafeRoot(dim),
            new Vector2(0.03f, 0.05f),
            new Vector2(0.4f, 0.95f)
        );

        UIBuilder.CreateWindowTitle(theme, window, "TEST MENÜSÜ");
        UIBuilder.CreateWindowCloseButton(theme, window).onClick.AddListener(Close);

        statusText = UIBuilder.CreateText(theme, window, string.Empty, 24f, TextAlignmentOptions.TopLeft, theme.greyLight);
        UIBuilder.AnchorTop(statusText.rectTransform, 145f, 70f);
        statusText.margin = new Vector4(50f, 0f, 50f, 0f);

        RectTransform content = UIBuilder.CreateScrollList(window, new Vector2(40f, 35f), new Vector2(-40f, -220f), 10f);

        // Oyun içi
        AddButton(content, "+1000 ALTIN (OYUN İÇİ)", AddRunGold, true, true);
        AddButton(content, "SEVİYE ATLA", LevelUp, true, true);
        AddButton(content, "BOSS ÇAĞIR", SpawnBoss, true, true);
        AddButton(content, "KART EKRANINI AÇ", OpenCards, false, true);
        AddButton(content, "TÜM DÜŞMANLARI ÖLDÜR", KillAllEnemies, false, true);
        AddButton(content, "CANI DOLDUR", HealPlayer, false, true);
        godModeText = AddButton(content, string.Empty, ToggleGodMode, false, true);
        speedText = AddButton(content, string.Empty, CycleSpeed, false, true);

        // Her sahnede
        AddButton(content, "+1000 KALICI ALTIN", AddBankGold, true, false);
        AddButton(content, "TÜM SİLAHLARI AÇ", UnlockAllWeapons, false, false);
        AddButton(content, "EĞİTİMİ SIFIRLA", ResetTutorial, false, false);
        AddButton(content, "GÜNLÜK ÖDÜLÜ SIFIRLA", ResetDailyReward, false, false);
        AddButton(content, "GÖREVLERİ TAMAMLA", QuestBoard.CompleteAllForDebug, false, false);
        AddButton(content, "GÖREVLERİ SIFIRLA", QuestBoard.ResetAllForDebug, false, false);
        AddButton(content, "TANITIMLARI SIFIRLA", ResetIntros, false, false);
        AddButton(content, "+1 SEZON SEVİYESİ", SeasonPass.AddTierForDebug, false, false);
        AddButton(content, "SEZONU SIFIRLA", SeasonPass.ResetForDebug, false, false);
        AddButton(content, "SEZON PREMİUM AÇ", SeasonPass.UnlockPremium, false, false);
        AddButton(content, "HATA KAYDINI KOPYALA", () => CrashReporter.CopyToClipboard(), false, false);
        AddButton(content, "TEST HATASI ÜRET", CrashReporter.ThrowTestException, false, false);
        resetAllText = AddButton(content, "TÜM KAYITLARI SİL", ResetAll, false, false);

        panel.SetActive(false);
    }

    TextMeshProUGUI AddButton(RectTransform content, string label, UnityEngine.Events.UnityAction action, bool red, bool gameplayOnly)
    {
        Button button = UIBuilder.CreateButton(theme, content, "DebugButton", label, red, out TextMeshProUGUI text);
        button.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;
        button.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            action();
        });

        if (gameplayOnly)
        {
            gameplayButtons.Add(button.gameObject);
        }

        return text;
    }
}
#endif
