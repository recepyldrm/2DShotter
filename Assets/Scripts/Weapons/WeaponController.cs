using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Ateş")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.2f;

    [Header("Ateş Efekti")]
    public GameObject muzzleFlash;

    private float fireTimer;

    void Update()
    {
        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            Shoot();

            float finalFireRate = fireRate;

            if (PlayerStats.Instance != null)
            {
                finalFireRate =
                    fireRate /
                    PlayerStats.Instance.fireRateMultiplier;
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

        SpawnBullet(
            firePoint.position,
            shotMultiplier
        );

        ShowMuzzleFlash();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayShoot();
        }
    }

    void SpawnBullet(Vector3 spawnPosition, int shotMultiplier)
    {
        GameObject bullet = null;

        if (ObjectPool.Instance != null)
        {
            bullet =
                ObjectPool.Instance.Spawn(
                    GameKeys.BulletPool,
                    spawnPosition,
                    Quaternion.identity
                );
        }
        else if (bulletPrefab != null)
        {
            bullet =
                Instantiate(
                    bulletPrefab,
                    spawnPosition,
                    Quaternion.identity
                );
        }

        if (bullet == null)
        {
            Debug.LogError(
                "WeaponController: Bullet bulunamadı!"
            );

            return;
        }

        bullet.transform.position = spawnPosition;
        bullet.transform.rotation = Quaternion.identity;

        Bullet bulletComponent = bullet.GetComponent<Bullet>();

        if (bulletComponent != null)
        {
            bulletComponent.SetShotMultiplier(shotMultiplier);
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
