using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Boss dalgaları: her LevelsPerBoss seviyede bir boss gelir.
// Boss, o seviyede açık olan en dayanıklı düşmanın büyütülmüş ve
// güçlendirilmiş hâlidir; belirli aralıklarla hücum eder ve yardımcı çağırır.
// İlk ihtiyaçta kendiliğinden oluşur, sahneye eklemek gerekmez.
public class BossWaveManager : MonoBehaviour
{
    public const int LevelsPerBoss = 5;

    // Boss gücü (n = kaçıncı boss)
    private const float BossScale = 1.8f;
    private const float BaseHealthMultiplier = 8f;
    private const float HealthGrowthPerBoss = 0.3f;
    private const float BaseRewardMultiplier = 10f;
    private const float RewardGrowthPerBoss = 4f;
    private const float SpeedMultiplier = 0.6f;
    private const float BaseDamageMultiplier = 1.3f;
    private const float DamageGrowthPerBoss = 0.25f;

    // Yetenekler
    private const float AbilityInterval = 8f;
    private const float ChargeSpeedMultiplier = 2.5f;
    private const float ChargeDuration = 0.6f;
    private const float TelegraphDuration = 0.8f;
    private const int MaxMinions = 3;

    private static readonly string[] BossNames =
    {
        "ÇELİK DEV",
        "KASAP",
        "GECE AVCISI",
        "YIKIM USTASI",
        "KARANLIK LORD"
    };

    private static BossWaveManager instance;

    public static bool IsBossActive => instance != null && instance.boss != null;

    private EnemyHealth boss;
    private EnemyMovement bossMovement;
    private Vector3 bossBaseScale;
    private int bossCount;
    private float bossSpawnTime;
    private int pendingBosses;
    private bool spawning;
    private Coroutine abilityRoutine;

    private UITheme theme;
    private GameObject barRoot;
    private RectTransform barFill;
    private TextMeshProUGUI barName;
    private TextMeshProUGUI barHealth;
    private GameObject banner;
    private TextMeshProUGUI bannerText;
    private Coroutine bannerRoutine;

    // ==================================
    // DIŞARIDAN
    // ==================================

    // LevelSystem her seviye atlamada çağırır.
    public static void NotifyLevelUp(int level)
    {
        if (level <= 0 || level % LevelsPerBoss != 0)
            return;

        QueueBoss();
    }

    // Test menüsü: seviye beklemeden boss çağırır.
    public static void ForceBoss()
    {
        QueueBoss();
    }

    static void QueueBoss()
    {
        if (instance == null)
        {
            instance = new GameObject("BossWaveManager").AddComponent<BossWaveManager>();
        }

        instance.pendingBosses++;

        if (!instance.spawning)
        {
            instance.StartCoroutine(instance.SpawnLoop());
        }
    }

    // ==================================
    // YAŞAM DÖNGÜSÜ
    // ==================================

    void Awake()
    {
        theme = UITheme.Load();
        BuildUI();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    void Update()
    {
        if (boss == null)
        {
            // Boss ödülsüz kaldırıldıysa (örn. sahne değişimi) arayüzü kapat.
            if (barRoot.activeSelf)
            {
                EndBossFight();
            }

            return;
        }

        int max = Mathf.Max(1, boss.GetMaxHealth());
        int current = Mathf.Max(0, boss.GetCurrentHealth());

        barFill.anchorMax = new Vector2((float)current / max, 1f);
        barHealth.text = current + " / " + max;
    }

    // ==================================
    // BOSS ÇIKIŞI
    // ==================================

    IEnumerator SpawnLoop()
    {
        spawning = true;

        while (pendingBosses > 0)
        {
            // Bir boss sahadayken sıradaki bekler.
            while (boss != null)
                yield return null;

            pendingBosses--;

            // Seviye atlayınca kart ekranı açılır (oyun durur);
            // bu bekleme kart seçildikten sonra işlemeye devam eder.
            yield return new WaitForSeconds(1f);

            ShowBanner("BOSS GELİYOR!", 2.5f);
            PlaySound(audio => audio.PlayBossWarning());
            Shake(0.2f, 0.4f);
            Haptics.Heavy();

            yield return new WaitForSeconds(2f);

            SpawnBoss();
        }

        spawning = false;
    }

    void SpawnBoss()
    {
        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();

        if (spawner == null)
        {
            Debug.LogWarning("BossWaveManager: EnemySpawner bulunamadı, boss çıkmadı.");
            return;
        }

        EnemySpawnData data = PickBossBase(spawner);

        if (data == null)
        {
            Debug.LogWarning("BossWaveManager: Boss olabilecek düşman bulunamadı.");
            return;
        }

        bossCount++;
        int n = bossCount;

        // Havuzdan değil: boss'a özel değişiklikler havuzdaki düşmanlara bulaşmasın.
        GameObject bossObject = Instantiate(data.prefab, spawner.transform.position, Quaternion.identity);

        string bossName = BossNames[(n - 1) % BossNames.Length];
        bossObject.name = "BOSS " + bossName;

        bossObject.transform.localScale *= BossScale;
        bossBaseScale = bossObject.transform.localScale;

        EnemyType type = bossObject.GetComponent<EnemyType>();

        if (type != null)
        {
            // Dondurma / yakma boss'u çok kolay yapmasın.
            type.freezeResistance = Mathf.Max(type.freezeResistance, 0.6f);
            type.burnResistance = Mathf.Max(type.burnResistance, 0.3f);
        }

        boss = bossObject.GetComponent<EnemyHealth>();
        bossMovement = bossObject.GetComponent<EnemyMovement>();

        if (boss == null)
        {
            Destroy(bossObject);
            return;
        }

        boss.ConfigureAsBoss(
            BaseHealthMultiplier * (1f + HealthGrowthPerBoss * (n - 1)),
            BaseRewardMultiplier + RewardGrowthPerBoss * n
        );

        if (bossMovement != null)
        {
            bossMovement.ConfigureAsBoss(
                SpeedMultiplier,
                BaseDamageMultiplier + DamageGrowthPerBoss * (n - 1),
                BossScale
            );
        }

        boss.OnDied += OnBossDied;
        bossSpawnTime = Time.time;

        barName.text = bossName;
        barRoot.SetActive(true);

        abilityRoutine = StartCoroutine(AbilityLoop(spawner));

        GameAnalytics.BossSpawned(spawner.CurrentLevel, n, type != null ? type.enemyType.ToString() : data.poolKey);
    }

    // Bu seviyede açık, bomber olmayan düşmanlardan en dayanıklıları sırayla boss olur.
    EnemySpawnData PickBossBase(EnemySpawner spawner)
    {
        int level = spawner.CurrentLevel;
        List<EnemySpawnData> candidates = new List<EnemySpawnData>();

        foreach (EnemySpawnData entry in spawner.enemyPrefabs)
        {
            if (entry == null || entry.prefab == null || entry.unlockLevel > level)
                continue;

            EnemyType type = entry.prefab.GetComponent<EnemyType>();

            // Bomber ölünce patlar; boss olarak uygun değil.
            if (type != null && type.IsBomber())
                continue;

            candidates.Add(entry);
        }

        if (candidates.Count == 0)
            return null;

        candidates.Sort((a, b) => GetHealthMultiplier(b).CompareTo(GetHealthMultiplier(a)));

        return candidates[bossCount % candidates.Count];
    }

    static float GetHealthMultiplier(EnemySpawnData entry)
    {
        EnemyType type = entry.prefab.GetComponent<EnemyType>();
        return type != null ? type.healthMultiplier : 1f;
    }

    // ==================================
    // YETENEKLER
    // ==================================

    IEnumerator AbilityLoop(EnemySpawner spawner)
    {
        bool chargeNext = true;

        while (boss != null)
        {
            yield return new WaitForSeconds(AbilityInterval);

            if (boss == null)
                yield break;

            if (chargeNext)
            {
                yield return Charge();
            }
            else
            {
                yield return SummonMinions(spawner);
            }

            chargeNext = !chargeNext;
        }
    }

    // Kısa bir "şişme" uyarısının ardından oyuncuya doğru hızla atılır.
    IEnumerator Charge()
    {
        float elapsed = 0f;

        while (elapsed < TelegraphDuration)
        {
            if (boss == null)
                yield break;

            elapsed += Time.deltaTime;
            float pulse = 1f + 0.15f * Mathf.Sin(elapsed / TelegraphDuration * Mathf.PI);
            boss.transform.localScale = bossBaseScale * pulse;
            yield return null;
        }

        if (boss == null)
            yield break;

        boss.transform.localScale = bossBaseScale;

        if (bossMovement != null)
        {
            bossMovement.SetChargeMultiplier(ChargeSpeedMultiplier);
        }

        yield return new WaitForSeconds(ChargeDuration);

        if (bossMovement != null)
        {
            bossMovement.SetChargeMultiplier(1f);
        }
    }

    IEnumerator SummonMinions(EnemySpawner spawner)
    {
        // 1. ve 2. boss 1, 3. ve 4. boss 2, sonrakiler 3 yardımcı çağırır.
        int count = Mathf.Min(MaxMinions, 1 + (bossCount - 1) / 2);

        for (int i = 0; i < count; i++)
        {
            if (boss == null || spawner == null)
                yield break;

            spawner.SpawnMinion();
            yield return new WaitForSeconds(0.2f);
        }
    }

    // ==================================
    // BOSS YENİLDİ
    // ==================================

    void OnBossDied(EnemyHealth deadBoss)
    {
        deadBoss.OnDied -= OnBossDied;

        int level = LevelSystem.Instance != null ? LevelSystem.Instance.currentLevel : 0;
        GameAnalytics.BossDefeated(level, bossCount, Time.time - bossSpawnTime);

        boss = null;
        EndBossFight();

        ShowBanner("BOSS YENİLDİ!", 2.5f);
        PlaySound(audio => audio.PlayExplosion());
        Shake(0.25f, 0.5f);
    }

    void EndBossFight()
    {
        if (abilityRoutine != null)
        {
            StopCoroutine(abilityRoutine);
            abilityRoutine = null;
        }

        if (bossMovement != null)
        {
            bossMovement.SetChargeMultiplier(1f);
        }

        bossMovement = null;
        barRoot.SetActive(false);
    }

    static void PlaySound(System.Action<AudioManager> play)
    {
        if (AudioManager.Instance != null)
        {
            play(AudioManager.Instance);
        }
    }

    static void Shake(float magnitude, float duration)
    {
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(magnitude, duration);
        }
    }

    // ==================================
    // ARAYÜZ
    // ==================================

    void ShowBanner(string text, float duration)
    {
        if (bannerRoutine != null)
        {
            StopCoroutine(bannerRoutine);
        }

        bannerRoutine = StartCoroutine(BannerRoutine(text, duration));
    }

    IEnumerator BannerRoutine(string text, float duration)
    {
        bannerText.text = text;
        banner.SetActive(true);

        // Hafif yanıp sönme; oyun dursa da çalışır.
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            bannerText.alpha = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(elapsed * 5f));
            yield return null;
        }

        banner.SetActive(false);
        bannerRoutine = null;
    }

    void BuildUI()
    {
        Transform canvas = UIBuilder.CreateCanvas(transform, "BossCanvas", 400);
        RectTransform safeRoot = UIBuilder.CreateSafeRoot(canvas);

        // Can barı (üst orta)
        RectTransform bar = UIBuilder.CreateRect("BossBar", safeRoot);
        UIBuilder.Place(bar, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 90f));
        barRoot = bar.gameObject;

        barName = UIBuilder.CreateText(theme, bar, string.Empty, 40f, TextAlignmentOptions.Bottom, theme.mainRedHover);
        UIBuilder.AnchorTop(barName.rectTransform, 0f, 50f);

        RectTransform progress = UIBuilder.CreateProgressBar(theme, bar, out barFill);
        progress.anchorMin = new Vector2(0f, 0f);
        progress.anchorMax = new Vector2(1f, 0f);
        progress.pivot = new Vector2(0.5f, 0f);
        progress.offsetMin = new Vector2(0f, 6f);
        progress.offsetMax = new Vector2(0f, 34f);

        barHealth = UIBuilder.CreateText(theme, progress, string.Empty, 22f, TextAlignmentOptions.Center, theme.mainWhite);
        UIBuilder.Stretch(barHealth.rectTransform);

        barRoot.SetActive(false);

        // Uyarı bandı (ekran ortası)
        RectTransform bannerRect = UIBuilder.CreateRect("BossBanner", safeRoot);
        UIBuilder.Center(bannerRect, new Vector2(0f, 120f), new Vector2(1000f, 160f));

        Image bannerImage = bannerRect.gameObject.AddComponent<Image>();
        theme.ApplyNotice(bannerImage);
        bannerImage.raycastTarget = false;

        bannerText = UIBuilder.CreateText(theme, bannerRect, string.Empty, 72f, TextAlignmentOptions.Center, theme.mainRedHover);
        UIBuilder.Stretch(bannerText.rectTransform);

        banner = bannerRect.gameObject;
        banner.SetActive(false);
    }
}
