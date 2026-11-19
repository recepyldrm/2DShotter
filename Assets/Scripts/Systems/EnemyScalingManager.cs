using UnityEngine;

public class EnemyScalingManager : MonoBehaviour
{
    public static EnemyScalingManager Instance;

    [Header("Enemy Scaling")]
    public int currentScalingLevel = 0;

    [Header("Her Level'da Artış")]
    [Range(0f, 1f)]
    public float healthIncreasePerLevel = 0.12f;

    [Range(0f, 1f)]
    public float damageIncreasePerLevel = 0.08f;

    [Range(0f, 1f)]
    public float speedIncreasePerLevel = 0.03f;

    public float HealthMultiplier
    {
        get
        {
            return 1f +
                   (currentScalingLevel * healthIncreasePerLevel);
        }
    }

    public float DamageMultiplier
    {
        get
        {
            return 1f +
                   (currentScalingLevel * damageIncreasePerLevel);
        }
    }

    public float SpeedMultiplier
    {
        get
        {
            return 1f +
                   (currentScalingLevel * speedIncreasePerLevel);
        }
    }

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

    public void IncreaseDifficulty()
    {
        currentScalingLevel++;

        GameLog.Log(
            "Enemy güçlendi! Scaling Level: " +
            currentScalingLevel +
            " | HP x" +
            HealthMultiplier +
            " | Damage x" +
            DamageMultiplier +
            " | Speed x" +
            SpeedMultiplier
        );
    }
}