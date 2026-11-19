using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("Referanslar")]
    public WeaponController weaponController;
    public CompanionController companionController;
    public PlayerHealth playerHealth;

    [Header("Silah Hasarı")]
    public int weaponDamageLevel = 0;
    public int weaponDamageBaseCost = 20;
    public float weaponDamageCostGrowth = 1.4f;
    public float weaponDamagePerLevel = 0.15f;

    // Bullet bu değeri hasar çarpanlarına ekler.
    public float WeaponDamageMultiplier => 1f + (weaponDamageLevel * weaponDamagePerLevel);

    [Header("Enemy Güçlenme (Silah Upgrade'ine Bağlı)")]
    [Tooltip("Silah hasarı her upgrade'lendiğinde enemy canı yüzde kaç artsın")]
    public float enemyHealthIncreasePerWeaponLevel = 0.10f; // +%10 can / seviye
    [Tooltip("Silah hasarı her upgrade'lendiğinde enemy hasarı yüzde kaç artsın")]
    public float enemyDamageIncreasePerWeaponLevel = 0.08f; // +%8 hasar / seviye

    // EnemyHealth / EnemyMovement spawn olurken bu değerleri okur.
    public float EnemyHealthMultiplier => 1f + (weaponDamageLevel * enemyHealthIncreasePerWeaponLevel);
    public float EnemyDamageMultiplier => 1f + (weaponDamageLevel * enemyDamageIncreasePerWeaponLevel);

    // Sahnede zaten var olan enemy'ler isterse bunu dinleyip kendini güncelleyebilir.
    public System.Action OnEnemyScalingChanged;

    [Header("Dost Bonusları")]
    public int companionLevel = 0;
    public int companionBaseCost = 25;
    public float companionCostGrowth = 1.4f;
    [Tooltip("Her seviyede dostun verdiği hasar bonusu ne kadar artsın (0.1 = +%10)")]
    public float companionDamagePerLevel = 0.1f;

    [Header("Max Can")]
    public int healthLevel = 0;
    public int healthBaseCost = 30;
    public float healthCostGrowth = 1.5f;
    public int healthPerLevel = 20;

    [Header("Shop UI")]
    public GameObject shopPanel;

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
        if (weaponController == null)
            weaponController = FindAnyObjectByType<WeaponController>();

        if (companionController == null)
            companionController = CompanionController.Instance;

        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();
    }

    void OnDestroy()
    {
        TimeScaleController.Resume(this);
    }

    public void OpenShop()
    {
        TimeScaleController.Pause(this);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        if (shopPanel != null)
            shopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        TimeScaleController.Resume(this);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    // ---- Silah Hasarı ----
    public int GetWeaponDamageCost()
    {
        return Mathf.RoundToInt(weaponDamageBaseCost * Mathf.Pow(weaponDamageCostGrowth, weaponDamageLevel));
    }

    public bool TryUpgradeWeaponDamage()
    {
        int cost = GetWeaponDamageCost();

        if (GameManager.Instance == null || !GameManager.Instance.TrySpendGold(cost))
            return false;

        weaponDamageLevel++;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUpgradePurchase();
        }

        // Silah güçlendi -> enemyler de güçlensin
        OnEnemyScalingChanged?.Invoke();

        return true;
    }

    // ---- Dost Bonusları ----
    public int GetCompanionUpgradeCost()
    {
        return Mathf.RoundToInt(companionBaseCost * Mathf.Pow(companionCostGrowth, companionLevel));
    }

    public bool TryUpgradeCompanion()
    {
        int cost = GetCompanionUpgradeCost();

        if (GameManager.Instance == null || !GameManager.Instance.TrySpendGold(cost))
            return false;

        companionLevel++;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUpgradePurchase();
        }

        if (companionController != null)
        {
            companionController.passiveDamageMultiplier += companionDamagePerLevel;
        }

        return true;
    }

    // ---- Max Can ----
    public int GetHealthUpgradeCost()
    {
        return Mathf.RoundToInt(healthBaseCost * Mathf.Pow(healthCostGrowth, healthLevel));
    }

    public bool TryUpgradeHealth()
    {
        int cost = GetHealthUpgradeCost();

        if (GameManager.Instance == null || !GameManager.Instance.TrySpendGold(cost))
            return false;

        healthLevel++;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUpgradePurchase();
        }

        if (playerHealth != null)
        {
            playerHealth.IncreaseMaxHealth(healthPerLevel);
        }

        return true;
    }
}
