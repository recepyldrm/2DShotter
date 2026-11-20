using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class EnemySpawnData
{
    [Header("Enemy")]
    public GameObject prefab;

    [Tooltip("ObjectPool'daki key ile birebir aynı olmalı. Örn: Enemy1")]
    public string poolKey;

    [Header("Spawn Ayarları")]

    [Tooltip("Bu düşmanın hangi oyuncu levelinde açılacağı.")]
    public int unlockLevel = 1;

    [Tooltip(
        "Spawn ağırlığı. " +
        "Yüksek değer = daha sık çıkar. " +
        "Güçlü düşmanlara daha düşük değer ver."
    )]
    public float spawnWeight = 1f;
}

public class EnemySpawner : MonoBehaviour
{
    // ==================================
    // ENEMY
    // ==================================

    [Header("Enemy")]
    public List<EnemySpawnData> enemyPrefabs =
        new List<EnemySpawnData>();

    // ==================================
    // SPAWN
    // ==================================

    [Header("Spawn")]
    public float spawnInterval = 1.5f;
    public float initialDelay = 2f;

    [Header("Spawn Interval Limit")]
    public float minimumSpawnInterval = 0.45f;

    [Tooltip(
        "Her oyuncu levelinde spawn süresinden ne kadar düşülsün."
    )]
    public float spawnIntervalDecreasePerLevel = 0.03f;

    // ==================================
    // START
    // ==================================

    void Start()
    {
        StartCoroutine(
            SpawnLoop()
        );
    }

    // ==================================
    // SPAWN LOOP
    // ==================================

    IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(
            initialDelay
        );

        while (true)
        {
            SpawnEnemy();

            float currentInterval =
                GetCurrentSpawnInterval();

            yield return new WaitForSeconds(
                currentInterval
            );
        }
    }

    // ==================================
    // SPAWN INTERVAL
    // ==================================

    float GetCurrentSpawnInterval()
    {
        int currentLevel =
            GetCurrentLevel();

        float reduction =
            (currentLevel - 1) *
            spawnIntervalDecreasePerLevel;

        float finalInterval =
            spawnInterval -
            reduction;

        finalInterval = Mathf.Max(
            minimumSpawnInterval,
            finalInterval
        );

        // Boss sahadayken normal düşmanlar daha seyrek gelir.
        if (BossWaveManager.IsBossActive)
        {
            finalInterval *= bossSpawnIntervalMultiplier;
        }

        return finalInterval;
    }

    // ==================================
    // BOSS
    // ==================================

    [Header("Boss")]
    [Tooltip("Boss sahadayken normal spawn aralığı bu kadarla çarpılır.")]
    public float bossSpawnIntervalMultiplier = 2f;

    // Boss'un yardımcı düşman çağırması için.
    public void SpawnMinion()
    {
        SpawnEnemy();
    }

    // Boss'un belirli türde yardımcı çağırması (örn. Sürü Anası bölünenler çağırır).
    public void SpawnMinion(EnemyVariant.Kind kind)
    {
        GameObject enemy = SpawnEnemy();

        if (enemy == null)
            return;

        EnemyType type = enemy.GetComponent<EnemyType>();

        // Bombacı rastgele seçildiyse sade kalsın; bölünme havuz ister.
        if (type != null && type.IsBomber())
            return;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (kind == EnemyVariant.Kind.Splitter && (health == null || string.IsNullOrEmpty(health.poolKey)))
            return;

        EnemyVariant.Apply(enemy, kind);
    }

    public int CurrentLevel => GetCurrentLevel();

    // ==================================
    // SPAWN ENEMY
    // ==================================

    GameObject SpawnEnemy()
    {
        EnemySpawnData data =
            GetRandomEnemyData();

        if (data == null)
        {
            Debug.LogWarning(
                "Bu level için spawn edilebilecek düşman yok!"
            );

            return null;
        }

        GameObject enemy = null;

        // ==================================
        // OBJECT POOL
        // ==================================

        if (
            ObjectPool.Instance != null &&
            !string.IsNullOrEmpty(data.poolKey)
        )
        {
            enemy =
                ObjectPool.Instance.Spawn(
                    data.poolKey,
                    transform.position,
                    Quaternion.identity
                );
        }

        // ==================================
        // FALLBACK
        // ==================================

        if (enemy == null)
        {
            if (data.prefab == null)
            {
                Debug.LogError(
                    "Enemy prefab boş!"
                );

                return null;
            }

            enemy =
                Instantiate(
                    data.prefab,
                    transform.position,
                    Quaternion.identity
                );
        }

        // Pozisyonu garanti et
        enemy.transform.position =
            transform.position;

        enemy.transform.rotation =
            Quaternion.identity;

        // ==================================
        // ÖZEL TÜR (nişancı / kalkanlı / bölünen)
        // ==================================

        ApplyVariant(enemy, data);

        // ==================================
        // DEBUG
        // ==================================

        EnemyType type =
            enemy.GetComponent<EnemyType>();

        if (type != null)
        {
            GameLog.Log(
                "SPAWN: " +
                enemy.name +
                " | Type: " +
                type.enemyType +
                " | Player Level: " +
                GetCurrentLevel()
            );
        }
        else
        {
            GameLog.Log(
                "SPAWN: " +
                enemy.name +
                " | EnemyType bulunamadı!"
            );
        }
        return enemy;
    }

    // Seviyeye göre bazı düşmanlar özel davranış kazanır.
    // Bombacılar sade kalır; bölünme havuz gerektirdiği için havuzsuz düşmanda olmaz.
    void ApplyVariant(GameObject enemy, EnemySpawnData data)
    {
        EnemyType type = enemy.GetComponent<EnemyType>();

        EnemyVariant.Kind kind =
            type != null && type.IsBomber()
                ? EnemyVariant.Kind.None
                : EnemyVariant.Roll(GetCurrentLevel());

        if (kind == EnemyVariant.Kind.Splitter && string.IsNullOrEmpty(data.poolKey))
        {
            kind = EnemyVariant.Kind.None;
        }

        EnemyVariant.Apply(enemy, kind);
    }

    // ==================================
    // RANDOM ENEMY
    // ==================================

    EnemySpawnData GetRandomEnemyData()
    {
        List<EnemySpawnData> validEntries =
            new List<EnemySpawnData>();

        float totalWeight = 0f;

        int currentLevel =
            GetCurrentLevel();

        // ==================================
        // BU LEVELDE AÇIK OLANLARI BUL
        // ==================================

        foreach (EnemySpawnData entry in enemyPrefabs)
        {
            if (entry == null)
                continue;

            if (entry.prefab == null)
                continue;

            if (entry.spawnWeight <= 0f)
                continue;

            // Henüz açılmadı
            if (currentLevel < entry.unlockLevel)
                continue;

            validEntries.Add(
                entry
            );

            totalWeight +=
                entry.spawnWeight;
        }

        // ==================================
        // HİÇ DÜŞMAN YOK
        // ==================================

        if (
            validEntries.Count == 0 ||
            totalWeight <= 0f
        )
        {
            return null;
        }

        // ==================================
        // WEIGHTED RANDOM
        // ==================================

        float roll =
            Random.value *
            totalWeight;

        float cumulative =
            0f;

        foreach (EnemySpawnData entry in validEntries)
        {
            cumulative +=
                entry.spawnWeight;

            if (roll <= cumulative)
            {
                return entry;
            }
        }

        return validEntries[
            validEntries.Count - 1
        ];
    }

    // ==================================
    // CURRENT LEVEL
    // ==================================

    int GetCurrentLevel()
    {
        if (LevelSystem.Instance != null)
        {
            return Mathf.Max(
                1,
                LevelSystem.Instance.currentLevel
            );
        }

        return 1;
    }
}