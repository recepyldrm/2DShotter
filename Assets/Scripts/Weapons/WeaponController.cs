using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Ateş")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.2f;

    [Header("Ateş Efekti")]
    public GameObject muzzleFlash;

    // Ana menüde seçilen silah (tüfek, pompalı, lazer, roketatar)
    private WeaponCatalog.Weapon weapon;

    private float fireTimer;

    private float levelDamageBonus = 1f;
    private float levelFireRateBonus = 1f;
    private Color bulletTint = Color.white;

    public WeaponCatalog.Weapon CurrentWeapon => weapon;

    void Awake()
    {
        weapon = WeaponCatalog.Selected;

        // Silah güçlendirme seviyesi (oyun boyunca değişmez)
        levelDamageBonus = WeaponCatalog.DamageBonus(weapon);
        levelFireRateBonus = WeaponCatalog.FireRateBonus(weapon);

        // Seçili mermi görünümü (klasikse silahın kendi rengi)
        bulletTint = BulletSkins.TintFor(weapon);
    }

    void Update()
    {
        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            Shoot();

            float finalFireRate = fireRate * weapon.fireIntervalMultiplier / levelFireRateBonus;

            if (PlayerStats.Instance != null)
            {
                finalFireRate /= PlayerStats.Instance.fireRateMultiplier;
            }

            fireTimer = Mathf.Max(finalFireRate, 0.03f);
        }
    }

    void Shoot()
    {
        if (firePoint == null)
        {
            Debug.LogError(
                "WeaponController: FirePoint atanmadı!"
            );

            return;
        }

        // Multi Shot ekranda ek mermi çıkarmaz; tek merminin hasarını
        // ek mermi sayısı kadar katlar (her ek mermi = +%100 hasar).
        int shotMultiplier = 1;

        if (PlayerStats.Instance != null)
        {
            shotMultiplier += PlayerStats.Instance.extraBullets;
        }

        // Pompalı gibi silahlar tek atışta yelpaze şeklinde birden çok mermi atar.
        int pellets = Mathf.Max(1, weapon.pellets);

        for (int i = 0; i < pellets; i++)
        {
            float angle = 0f;

            if (pellets > 1)
            {
                float t = i / (float)(pellets - 1);
                angle = Mathf.Lerp(-weapon.spreadAngle / 2f, weapon.spreadAngle / 2f, t);

                // Hafif rastgelelik: her atış birebir aynı görünmesin.
                angle += Random.Range(-2f, 2f);
            }

            // Minigun gibi silahlarda her mermi biraz farklı yöne gider.
            if (weapon.randomSpread > 0f)
            {
                angle += Random.Range(-weapon.randomSpread, weapon.randomSpread);
            }

            SpawnBullet(
                firePoint.position,
                Quaternion.Euler(0f, 0f, angle),
                shotMultiplier
            );
        }

        ShowMuzzleFlash();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWeaponShot(weapon);
        }
    }

    void SpawnBullet(Vector3 spawnPosition, Quaternion rotation, int shotMultiplier)
    {
        GameObject bullet = null;

        if (ObjectPool.Instance != null)
        {
            bullet =
                ObjectPool.Instance.Spawn(
                    GameKeys.BulletPool,
                    spawnPosition,
                    rotation
                );
        }
        else if (bulletPrefab != null)
        {
            bullet =
                Instantiate(
                    bulletPrefab,
                    spawnPosition,
                    rotation
                );
        }

        if (bullet == null)
        {
            Debug.LogError(
                "WeaponController: Bullet bulunamadı!"
            );

            return;
        }

        bullet.transform.SetPositionAndRotation(spawnPosition, rotation);

        Bullet bulletComponent = bullet.GetComponent<Bullet>();

        if (bulletComponent != null)
        {
            bulletComponent.Configure(shotMultiplier, weapon, levelDamageBonus, bulletTint);
        }
    }

    void ShowMuzzleFlash()
    {
        if (muzzleFlash == null)
            return;

        MuzzleFlash flash =
            muzzleFlash.GetComponent<MuzzleFlash>();

        if (flash != null)
        {
            flash.Show();
        }
        else
        {
            muzzleFlash.SetActive(true);
        }
    }
}
