using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject gameOverPanel;

    [Header("Gold")]
    public int gold = 0;

    [Header("Score")]
    public int score = 0;
    public int highScore = 0;

    [Header("UI")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI bestScoreText;

    [Tooltip("Opsiyonel: Game Over panelinde bu oyunda kazanılan ve kalıcı bakiyeye eklenen gold.")]
    public TextMeshProUGUI earnedGoldText;

    [Header("Sayaç Animasyonu")]
    public float countDuration = 0.4f; // sayının hedefe ulaşma süresi

    [Header("Reklam Ödülleri (Game Over)")]
    [Tooltip("Reklamla devam edince canın ne kadarıyla dönülsün (0.5 = yarım can).")]
    [Range(0.1f, 1f)]
    public float reviveHealthFraction = 0.5f;

    [Tooltip("Devam ettikten sonra kaç saniye hasar alınmasın.")]
    public float reviveInvulnerableSeconds = 3f;

    [Tooltip("Devam edince oyuncunun bu mesafedeki düşmanları (ödülsüz) kaldırılır.")]
    public float reviveClearRadius = 4f;

    private int displayedGold = 0;
    private int displayedScore = 0;
    private Coroutine goldCountRoutine;
    private Coroutine scoreCountRoutine;

    private bool isGameOver = false;
    private bool highScoreDirty = false;

    // Bu oyunda kazanılan toplam gold (harcananlar düşülmez).
    // Oyun bitince / sahneden çıkınca kalıcı bakiyeye eklenir.
    private int goldEarnedThisRun = 0;
    private int goldDepositedThisRun = 0;

    // Başarım istatistikleri: her öldürmede diske yazmamak için
    // oyun sonunda / sahneden çıkarken toplu kaydedilir.
    private int killsThisRun = 0;
    private int killsCommitted = 0;
    private int bossKillsThisRun = 0;
    private int bossKillsCommitted = 0;
    private bool runCounted = false;

    // Her biri oyun başına bir kez kullanılabilir.
    private bool reviveUsed = false;
    private bool doubleGoldUsed = false;
    private int doubleGoldBonus = 0;

    private GameOverAdOffers adOffers;

    public bool IsGameOver => isGameOver;

    public int GoldEarnedThisRun => goldEarnedThisRun;

    public bool CanRevive => isGameOver && !reviveUsed;

    public bool CanDoubleGold => isGameOver && !doubleGoldUsed && goldEarnedThisRun > 0;

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
        highScore = PlayerPrefs.GetInt(GameKeys.HighScore, 0);
        UpdateUI();

        GameAnalytics.RunStarted(WeaponCatalog.Selected.id);

        // Kalıcı geliştirmeler uygulandı; oyuncuya göster.
        ActiveBonusBanner.ShowIfAny();
    }

    public void AddScore(int points)
    {
        score += points;
        CheckHighScore();
        UpdateUI();
    }

    // Rekor bellekte güncellenir; diske her kill'de değil,
    // game over / uygulama arka plana alınınca yazılır.
    void CheckHighScore()
    {
        if (score > highScore)
        {
            highScore = score;
            highScoreDirty = true;
        }
    }

    void SaveHighScore()
    {
        if (!highScoreDirty)
            return;

        PlayerPrefs.SetInt(GameKeys.HighScore, highScore);
        PlayerPrefs.Save();
        highScoreDirty = false;
    }

    // Kazanılıp henüz kalıcı bakiyeye eklenmemiş gold'u ekler.
    // Birden fazla kez çağrılabilir (game over, pause, çıkış), aynı gold iki kez eklenmez.
    // Kazanılıp henüz kaydedilmemiş gold'u kalıcı bakiyeye, öldürme / level
    // bilgisini başarım istatistiklerine ekler. Birden fazla kez çağrılabilir
    // (game over, pause, çıkış); aynı değer iki kez eklenmez.
    // leavingRun: oyun bitiyor ya da yarıda bırakılıyor (oyun sayısına dahil edilir).
    void CommitRunProgress(bool leavingRun)
    {
        int pendingGold = goldEarnedThisRun - goldDepositedThisRun;

        if (pendingGold > 0)
        {
            PlayerProgress.AddGold(pendingGold);
            goldDepositedThisRun = goldEarnedThisRun;
        }

        int pendingKills = killsThisRun - killsCommitted;
        killsCommitted = killsThisRun;

        int pendingBossKills = bossKillsThisRun - bossKillsCommitted;
        bossKillsCommitted = bossKillsThisRun;

        // Hiç oynanmadan çıkılan sahneler oyun sayısına eklenmez.
        bool countRun =
            !runCounted &&
            (isGameOver || (leavingRun && killsThisRun > 0));

        if (countRun)
        {
            runCounted = true;
        }

        int levelReached =
            LevelSystem.Instance != null ? LevelSystem.Instance.currentLevel : 0;

        PlayerProgress.RecordRunProgress(
            pendingKills,
            Mathf.Max(0, pendingGold),
            levelReached,
            countRun,
            pendingBossKills
        );
    }

    // EnemyHealth her ölümde çağırır.
    public void RegisterKill()
    {
        killsThisRun++;
    }

    public void RegisterBossKill()
    {
        bossKillsThisRun++;
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveHighScore();
            CommitRunProgress(false);
        }
    }

    void OnApplicationQuit()
    {
        SaveHighScore();
        CommitRunProgress(true);
        SendRunEnded();
    }

    // Restart / ana menüye dönüş: oyun yarıda bırakılsa da kazanılanlar kaybolmaz.
    void OnDestroy()
    {
        if (Instance != this)
            return;

        SaveHighScore();
        CommitRunProgress(true);
        SendRunEnded();
    }

    // ==================================
    // ANALİTİK
    // ==================================

    private bool runEndSent = false;

    int CurrentLevel =>
        LevelSystem.Instance != null ? LevelSystem.Instance.currentLevel : 0;

    // Oyuncunun oyunu bıraktığı son durum (öldü ya da yarıda çıktı).
    void SendRunEnded()
    {
        if (runEndSent || (!isGameOver && killsThisRun == 0))
            return;

        runEndSent = true;

        GameAnalytics.RunEnded(
            CurrentLevel,
            score,
            killsThisRun,
            Time.timeSinceLevelLoad,
            isGameOver,
            WeaponCatalog.Selected.id
        );
    }

    public void AddGold(int amount)
    {
        gold += amount;

        if (amount > 0)
        {
            goldEarnedThisRun += amount;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGoldPickup();
        }

        UpdateUI();
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (gold < amount)
            return false;

        gold -= amount;
        UpdateUI();
        return true;
    }

    void UpdateUI()
    {
        if (goldText != null)
        {
            if (goldCountRoutine != null)
                StopCoroutine(goldCountRoutine);

            goldCountRoutine = StartCoroutine(CountText(goldText, displayedGold, gold, " ", value => displayedGold = value));
        }

        if (scoreText != null)
        {
            if (scoreCountRoutine != null)
                StopCoroutine(scoreCountRoutine);

            scoreCountRoutine = StartCoroutine(CountText(scoreText, displayedScore, score, ": ", value => displayedScore = value));
        }
    }

    IEnumerator CountText(TextMeshProUGUI target, int from, int to, string prefix, System.Action<int> onValueChanged)
    {
        float elapsed = 0f;

        // Zaten aynıysa (örn. spend gold sonrası azalış da bu fonksiyonu kullanıyor) direkt ayarla
        if (from == to)
        {
            target.text = prefix + to;
            onValueChanged(to);
            yield break;
        }

        while (elapsed < countDuration)
        {
            elapsed += Time.unscaledDeltaTime; // shop açıkken (timeScale=0) bile çalışsın
            float t = Mathf.Clamp01(elapsed / countDuration);
            int current = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            target.text = prefix + current;

            // Animasyon yarıda kesilirse yeni animasyon ekrandaki değerden devam etsin.
            onValueChanged(current);

            yield return null;
        }

        target.text = prefix + to;
        onValueChanged(to);
    }

    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        CheckHighScore();
        SaveHighScore();
        CommitRunProgress(true);
        UpdateEarnedGoldText();

        GameAnalytics.PlayerDied(CurrentLevel, score, killsThisRun, Time.timeSinceLevelLoad, reviveUsed);
        Leaderboards.ReportScore(score);

        if (finalScoreText != null)
        {
            finalScoreText.text =
                "SKOR: " + score;
        }

        if (bestScoreText != null)
        {
            bestScoreText.text =
                "EN İYİ: " + highScore;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        TimeScaleController.Pause(this);

        GetAdOffers().Show();
    }

    void UpdateEarnedGoldText()
    {
        if (earnedGoldText == null)
            return;

        earnedGoldText.text =
            "+" + (goldEarnedThisRun + doubleGoldBonus) + " ALTIN" +
            (doubleGoldUsed ? " (x2)" : string.Empty);
    }

    // Sahnede GameOverAdOffers yoksa (butonlar elle bağlanmadıysa)
    // otomatik eklenir ve kendi butonlarını oluşturur.
    GameOverAdOffers GetAdOffers()
    {
        if (adOffers == null)
        {
            adOffers = FindAnyObjectByType<GameOverAdOffers>();
        }

        if (adOffers == null)
        {
            adOffers = gameObject.AddComponent<GameOverAdOffers>();
        }

        return adOffers;
    }

    // ==================================
    // REKLAM ÖDÜLLERİ
    // ==================================

    // Reklam izlendi: oyuncu canın bir kısmıyla devam eder.
    public void Revive()
    {
        if (!CanRevive)
            return;

        reviveUsed = true;
        isGameOver = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            ClearEnemiesAround(playerHealth.transform.position, reviveClearRadius);

            playerHealth.Revive(
                reviveHealthFraction,
                reviveInvulnerableSeconds
            );
        }

        TimeScaleController.Resume(this);
    }

    // Reklam izlendi: bu oyunda kazanılan gold bir kez daha kalıcı bakiyeye eklenir.
    public void DoubleRunGold()
    {
        if (!CanDoubleGold)
            return;

        doubleGoldUsed = true;
        doubleGoldBonus = goldEarnedThisRun;

        PlayerProgress.AddGold(doubleGoldBonus);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGoldPickup();
        }

        UpdateEarnedGoldText();
    }

    static void ClearEnemiesAround(Vector3 center, float radius)
    {
        EnemyHealth[] enemies =
            FindObjectsByType<EnemyHealth>();

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.IsDead() || enemy.IsBoss)
                continue;

            if (Vector2.Distance(center, enemy.transform.position) <= radius)
            {
                enemy.DespawnWithoutReward();
            }
        }
    }

    public void RestartGame()
    {
        TimeScaleController.ResetAll();

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}
