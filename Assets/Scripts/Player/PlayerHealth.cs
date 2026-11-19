using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Can")]
    public int maxHealth = 100;

    private int currentHealth;

    private bool isDead = false;

    // Reklamla devam edince kısa süre hasar alınmaz.
    private float invulnerableUntil = 0f;

    public bool IsFullHealth => currentHealth >= maxHealth;

    public bool IsDead => isDead;

    public bool IsInvulnerable => Time.time < invulnerableUntil;

    // Test menüsünden açılan ölümsüzlük (sadece geliştirme için).
    public static bool GodMode;

    [Header("UI")]
    public Slider healthBar;
    public TextMeshProUGUI healthText;

    void Awake()
    {
        // Ana menüden alınan kalıcı "Başlangıç Canı" yükseltmesi
        maxHealth += PermanentUpgrades.BonusMaxHealth;
    }

    void Start()
    {
        currentHealth = maxHealth;

        UpdateUI();
    }

    public void TakeDamage(int damage)
    {
        if (isDead || IsInvulnerable || GodMode)
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlayerHurt();
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.15f, 0.15f);
        }

        Haptics.Medium();

        currentHealth -= damage;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        UpdateUI();

        GameLog.Log("Player hasar aldı: " + damage + " | Kalan can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Power-up sistemi ve diğer can yenileme durumları için
    public void Heal(int amount)
    {
        if (isDead)
            return;

        currentHealth += amount;

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        UpdateUI();

        GameLog.Log("Player can yeniledi: " + amount + " | Güncel can: " + currentHealth);
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0)
            return;

        maxHealth += amount;

        // Artan max can kadar mevcut canı da artır (upgrade alınca oyuncu ekstra can kazanmış olsun)
        if (!isDead)
        {
            currentHealth += amount;
        }

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        UpdateUI();

        GameLog.Log("Max can artırıldı: +" + amount + " (Yeni max: " + maxHealth + ")");
    }

    void UpdateUI()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text =
                currentHealth + " / " + maxHealth;
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        GameLog.Log("PLAYER ÖLDÜ!");

        Haptics.Heavy();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    // Reklam izleyip devam etmek için: canın bir kısmıyla geri döner
    // ve birkaç saniye hasar almaz.
    public void Revive(float healthFraction, float invulnerableSeconds)
    {
        isDead = false;

        currentHealth =
            Mathf.Clamp(
                Mathf.RoundToInt(maxHealth * healthFraction),
                1,
                maxHealth
            );

        invulnerableUntil = Time.time + invulnerableSeconds;

        UpdateUI();

        GameLog.Log("PLAYER DEVAM ETTİ! Can: " + currentHealth);
    }
}
