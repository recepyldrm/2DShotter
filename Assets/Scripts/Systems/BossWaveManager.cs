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

    // Topçu: yelpaze şeklinde mermi yağmuru
    private const int BarrageVolleys = 3;
    private const int BarrageProjectiles = 5;
    private const float BarrageSpread = 34f;
    private const float BarrageDamageMultiplier = 0.35f;

    // Muhafız: süreli kalkan (canın bu oranı kadar hasar emer)
    private const float ShieldRatio = 0.12f;
    private const float ShieldDuration = 5f;

    // Yıkıcı: yere vuruş. Hazırlık sırasında canının bu oranı kadar
    // hasar alırsa sersemler ve vuruş iptal olur.
    private const float SlamWindup = 1.8f;
    private const float SlamInterruptRatio = 0.06f;
    private const float SlamDamageMultiplier = 1.6f;
    private const float StunDuration = 2.2f;

    public enum Archetype
    {
        Charger,
        Artillery,
        Guardian,
        Smasher,
        Broodmother,
        Regenerator
    }

    private static readonly string[] ArchetypeTitles = { "HÜCUMCU", "TOPÇU", "MUHAFIZ", "YIKICI", "SÜRÜ ANASI", "YENİLENEN" };

    private static readonly string[] ArchetypeIntros =
    {
        "Gövdesi şiştiğinde sana doğru hızla atılır ve yardımcı düşmanlar çağırır.",
        "Uzaktan yelpaze şeklinde mermi yağdırır. Ne kadar çabuk ölürse o kadar az hasar alırsın.",
        "Belirli aralıklarla mavi bir kalkan açar. Kalkan kısa sürede kendiliğinden kalkar; yanma hasarı kalkanı deler.",
        "Durup yere vurmaya hazırlanır. Hazırlık sırasında ona yeterince hasar verirsen sersemler; veremezsen ağır bir şok dalgası gelir!",
        "Durmadan bölünen düşmanlar doğurur. Çocuklarına boğulmadan önce onu indir; patlayıcı mermiler çok işe yarar.",
        "Sürekli kendini iyileştirir. Yanarken iyileşemez: yakıcı mermi ya da yakıcı drone kullan!"
    };

    private static readonly Color[] ArchetypeColors =
    {
        new Color(1f, 0.35f, 0.25f, 1f),
        new Color(1f, 0.6f, 0.2f, 1f),
        new Color(0.55f, 0.8f, 1f, 1f),
        new Color(0.85f, 0.4f, 1f, 1f),
        new Color(0.55f, 0.95f, 0.45f, 1f),
        new Color(0.4f, 1f, 0.75f, 1f)
    };

    private Archetype archetype;
    private EnemyVariant bossVariant;
    private int healthAtSlamStart;

    public static string[] IntroIds => new[] { "boss_charger", "boss_artillery", "boss_guardian", "boss_smasher", "boss_broodmother", "boss_regenerator" };

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
        if (level <= 0 || level % DailyChallenge.LevelsPerBoss != 0)
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

        // Sahne değişince boss müziği açık kalmasın.
        PlaySound(audio => audio.SetBossMusic(false));
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

        TickRegeneration();

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

            ShowBanner(Loc.T("BOSS GELİYOR!"), 2.5f);
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

        // Boss tipi sırayla değişir: hücumcu, topçu, muhafız, yıkıcı...
        archetype = (Archetype)((n - 1) % ArchetypeTitles.Length);
        int archetypeIndex = (int)archetype;

        bossVariant = bossObject.GetComponent<EnemyVariant>();
        if (bossVariant == null)
        {
            bossVariant = bossObject.AddComponent<EnemyVariant>();
        }


        barName.text =
            Loc.T(bossName) + "  " +
            UIBuilder.ColorTag(ArchetypeColors[archetypeIndex], "<size=75%>" + Loc.T(ArchetypeTitles[archetypeIndex]) + "</size>");

        EnemyIntroUI.ShowOnce(
            IntroIds[archetypeIndex],
            Loc.T(ArchetypeTitles[archetypeIndex]),
            Loc.T(ArchetypeIntros[archetypeIndex]),
            ArchetypeColors[archetypeIndex],
            bossObject.transform,
            true
        );
        barRoot.SetActive(true);
        PlaySound(audio => audio.SetBossMusic(true));

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
        int step = 0;

        while (boss != null)
        {
            // Topçu daha sık ateş eder; diğerleri normal aralıkla.
            float interval = archetype == Archetype.Artillery ? AbilityInterval * 0.6f : AbilityInterval;
            yield return new WaitForSeconds(interval);

            if (boss == null)
                yield break;

            bool primary = step % 2 == 0;

            switch (archetype)
            {
                case Archetype.Charger:
                    yield return primary ? Charge() : SummonMinions(spawner);
                    break;

                case Archetype.Artillery:
                    // Üç atıştan biri yardımcı çağırma
                    yield return step % 3 == 2 ? SummonMinions(spawner) : Barrage();
                    break;

                case Archetype.Guardian:
                    yield return primary ? RaiseShield() : SummonMinions(spawner);
                    break;

                case Archetype.Smasher:
                    yield return primary ? GroundSlam() : Charge();
                    break;

                case Archetype.Broodmother:
                    yield return primary ? SpawnBrood(spawner) : Charge();
                    break;

                case Archetype.Regenerator:
                    // İyileşme Update'te sürer; yetenek olarak nişancı çağırır ve hücum eder.
                    yield return primary ? SummonGunners(spawner) : Charge();
                    break;
            }

            step++;
        }
    }

    // ==================================
    // SÜRÜ ANASI: BÖLÜNENLER DOĞURUR
    // ==================================

    IEnumerator SpawnBrood(EnemySpawner spawner)
    {
        yield return Telegraph(0.6f, ArchetypeColors[(int)Archetype.Broodmother]);

        int count = Mathf.Min(4, 2 + (bossCount - 1) / 3);

        for (int i = 0; i < count; i++)
        {
            if (boss == null || spawner == null)
                yield break;

            spawner.SpawnMinion(EnemyVariant.Kind.Splitter);
            yield return new WaitForSeconds(0.25f);
        }
    }

    // ==================================
    // YENİLENEN: İYİLEŞME + NİŞANCI ÇAĞIRMA
    // ==================================

    private const float RegenInterval = 2.5f;
    private const float RegenFraction = 0.025f;
    private float regenTimer;

    void TickRegeneration()
    {
        if (archetype != Archetype.Regenerator || boss == null)
            return;

        regenTimer -= Time.deltaTime;

        if (regenTimer > 0f)
            return;

        regenTimer = RegenInterval;

        // Yanarken iyileşemez (oyuncuya karşı koyma yolu)
        if (boss.IsBurning)
            return;

        if (boss.HealFraction(RegenFraction) > 0)
        {
            ExplosionBlast.Play(GetBossCenter(), GetBossSize() * 0.55f, Color.white, ArchetypeColors[(int)Archetype.Regenerator]);
        }
    }

    IEnumerator SummonGunners(EnemySpawner spawner)
    {
        int count = Mathf.Min(3, 1 + (bossCount - 1) / 3);

        for (int i = 0; i < count; i++)
        {
            if (boss == null || spawner == null)
                yield break;

            spawner.SpawnMinion(EnemyVariant.Kind.Ranged);
            yield return new WaitForSeconds(0.25f);
        }
    }

    // ==================================
    // TOPÇU: MERMİ YAĞMURU
    // ==================================

    IEnumerator Barrage()
    {
        yield return Telegraph(0.6f, ArchetypeColors[(int)Archetype.Artillery]);

        Transform player = FindPlayer();

        for (int volley = 0; volley < BarrageVolleys; volley++)
        {
            if (boss == null || player == null)
                yield break;

            Vector3 origin = GetBossCenter();
            origin.x -= GetBossSize() * 0.4f;

            Vector2 toPlayer = player.position - origin;
            float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            int damage = Mathf.Max(1, Mathf.RoundToInt(GetBossDamage() * BarrageDamageMultiplier));

            // Her yaylım biraz kayık: aralarında boşluk olsun.
            float offset = (volley % 2 == 0 ? 0f : BarrageSpread / (BarrageProjectiles - 1) / 2f);

            for (int i = 0; i < BarrageProjectiles; i++)
            {
                float t = i / (float)(BarrageProjectiles - 1);
                float angle = baseAngle + Mathf.Lerp(-BarrageSpread / 2f, BarrageSpread / 2f, t) + offset;
                Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
                EnemyProjectile.FireDirection(origin, direction, player, damage);
            }

            PlaySound(audio => audio.PlayEnemyShot());
            yield return new WaitForSeconds(0.45f);
        }
    }

    // ==================================
    // MUHAFIZ: KALKAN
    // ==================================

    IEnumerator RaiseShield()
    {
        if (bossVariant == null || bossVariant.HasShield)
            yield break;

        yield return Telegraph(0.5f, ArchetypeColors[(int)Archetype.Guardian]);

        if (boss == null)
            yield break;

        int amount = Mathf.RoundToInt(boss.GetMaxHealth() * ShieldRatio);
        bossVariant.GrantTemporaryShield(amount, ShieldDuration);

        ExplosionBlast.Play(GetBossCenter(), GetBossSize() * 0.7f, Color.white, ArchetypeColors[(int)Archetype.Guardian]);
        PlaySound(audio => audio.PlayShieldHit());
    }

    // ==================================
    // YIKICI: YERE VURUŞ
    // ==================================

    IEnumerator GroundSlam()
    {
        if (boss == null)
            yield break;

        healthAtSlamStart = boss.GetCurrentHealth();
        int interruptDamage = Mathf.Max(1, Mathf.RoundToInt(boss.GetMaxHealth() * SlamInterruptRatio));

        if (bossMovement != null)
        {
            bossMovement.SetChargeMultiplier(0f);
        }

        ShowBanner(Loc.T("DURDUR ONU!"), SlamWindup);
        Color color = ArchetypeColors[(int)Archetype.Smasher];

        float elapsed = 0f;
        bool interrupted = false;

        while (elapsed < SlamWindup)
        {
            if (boss == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;

            // Yavaşça şişer ve renklenir
            float t = elapsed / SlamWindup;
            boss.transform.localScale = bossBaseScale * (1f + 0.12f * t + 0.03f * Mathf.Sin(elapsed * 30f));

            if (healthAtSlamStart - boss.GetCurrentHealth() >= interruptDamage)
            {
                interrupted = true;
                break;
            }

            yield return null;
        }

        if (boss == null)
        {
            yield break;
        }

        boss.transform.localScale = bossBaseScale;

        if (interrupted)
        {
            // Sersemledi: bir süre hareketsiz kalır.
            ShowBanner(Loc.T("SERSEMLEDİ!"), 1.5f);
            RunStats.RegisterBossStun();
            ExplosionBlast.Play(GetBossCenter(), GetBossSize() * 0.5f, Color.white, color);
            PlaySound(audio => audio.PlayShieldBreak());
            Haptics.Medium();

            yield return new WaitForSeconds(StunDuration);
        }
        else
        {
            // Şok dalgası oyuncuya ulaşır.
            Transform player = FindPlayer();

            ExplosionBlast.Play(GetBossCenter(), GetBossSize() * 1.4f, Color.white, color);
            PlaySound(audio => audio.PlayExplosion());
            Shake(0.35f, 0.45f);
            Haptics.Heavy();

            if (player != null)
            {
                ExplosionBlast.Play(player.position, 1.4f, Color.white, color);

                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(GetBossDamage() * SlamDamageMultiplier)));
                }
            }
        }

        if (bossMovement != null)
        {
            bossMovement.SetChargeMultiplier(1f);
        }

    }

    // ==================================
    // YARDIMCILAR
    // ==================================

    // Yetenekten önce kısa şişme uyarısı
    IEnumerator Telegraph(float duration, Color color)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (boss == null)
                yield break;

            elapsed += Time.deltaTime;
            float pulse = 1f + 0.1f * Mathf.Sin(elapsed / duration * Mathf.PI);
            boss.transform.localScale = bossBaseScale * pulse;
            yield return null;
        }

        if (boss != null)
        {
            boss.transform.localScale = bossBaseScale;
        }
    }

    static Transform FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }

    Vector3 GetBossCenter()
    {
        Collider2D body = boss.GetComponent<Collider2D>();
        return body != null ? body.bounds.center : boss.transform.position;
    }

    float GetBossSize()
    {
        SpriteRenderer renderer = boss.GetComponentInChildren<SpriteRenderer>();

        if (renderer == null)
            return 1.5f;

        Vector3 size = renderer.bounds.size;
        return Mathf.Max(0.5f, Mathf.Min(size.x, size.y));
    }

    int GetBossDamage() => bossMovement != null ? bossMovement.CurrentDamage : 15;

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

        ShowBanner(Loc.T("BOSS YENİLDİ!"), 2.5f);
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
        PlaySound(audio => audio.SetBossMusic(false));
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
