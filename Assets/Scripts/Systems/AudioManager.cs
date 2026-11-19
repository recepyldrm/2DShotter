using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Ses Kaynakları")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Efektler")]
    public AudioClip shootClip;
    [Range(0f, 1f)] public float shootVolume = 1f;

    public AudioClip enemyHitClip;
    [Range(0f, 1f)] public float enemyHitVolume = 1f;

    public AudioClip enemyDeathClip;
    [Range(0f, 1f)] public float enemyDeathVolume = 1f;

    public AudioClip goldPickupClip;
    [Range(0f, 1f)] public float goldPickupVolume = 1f;

    public AudioClip playerHurtClip;
    [Range(0f, 1f)] public float playerHurtVolume = 1f;

    public AudioClip upgradePurchaseClip;
    [Range(0f, 1f)] public float upgradePurchaseVolume = 1f;

    public AudioClip buttonClickClip;
    [Range(0f, 1f)] public float buttonClickVolume = 1f;

    [Header("Müzik")]
    public AudioClip backgroundMusic;

    [Range(0f, 1f)]
    public float musicVolume = 0.4f;

    private const string MusicVolumeKey = GameKeys.MusicVolume;
    private const string SfxVolumeKey = GameKeys.SfxVolume;

    private float previousMusicVolume = 0.4f;
    private float previousSfxVolume = 1f;

    private bool allSoundMuted = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        float savedMusicVolume =
            PlayerPrefs.GetFloat(
                MusicVolumeKey,
                musicVolume
            );

        float savedSfxVolume =
            PlayerPrefs.GetFloat(
                SfxVolumeKey,
                1f
            );

        // Eğer ikisi de 0 ise bütün sesler kapalı kabul edilir
        allSoundMuted =
            savedMusicVolume <= 0.001f &&
            savedSfxVolume <= 0.001f;

        // Önceki ses değerlerini sakla
        if (savedMusicVolume > 0.001f)
        {
            previousMusicVolume =
                savedMusicVolume;
        }

        if (savedSfxVolume > 0.001f)
        {
            previousSfxVolume =
                savedSfxVolume;
        }

        // MUSIC
        if (musicSource != null)
        {
            musicSource.volume =
                savedMusicVolume;

            if (backgroundMusic != null)
            {
                musicSource.clip =
                    backgroundMusic;

                musicSource.loop = true;

                if (!musicSource.isPlaying)
                {
                    musicSource.Play();
                }
            }
        }

        // SFX
        if (sfxSource != null)
        {
            sfxSource.volume =
                savedSfxVolume;
        }
    }

    // =====================================
    // GENEL SFX ÇALMA
    // =====================================

    public void PlaySFX(
        AudioClip clip,
        float volume
    )
    {
        if (sfxSource == null)
            return;

        if (clip == null)
            return;

        sfxSource.PlayOneShot(
            clip,
            volume
        );
    }

    // =====================================
    // MUSIC VOLUME
    // =====================================

    public void SetMusicVolume(float value)
    {
        musicVolume = value;

        if (musicSource != null)
        {
            musicSource.volume = value;
        }

        PlayerPrefs.SetFloat(
            MusicVolumeKey,
            value
        );

        PlayerPrefs.Save();
    }

    public float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(
            MusicVolumeKey,
            musicVolume
        );
    }

    // =====================================
    // SFX VOLUME
    // =====================================

    public void SetSfxVolume(float value)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = value;
        }

        PlayerPrefs.SetFloat(
            SfxVolumeKey,
            value
        );

        PlayerPrefs.Save();
    }

    public float GetSfxVolume()
    {
        return PlayerPrefs.GetFloat(
            SfxVolumeKey,
            1f
        );
    }

    // =====================================
    // BÜTÜN SESLERİ AÇ / KAPAT
    // =====================================

    public void ToggleAllSound()
    {
        if (!allSoundMuted)
        {
            float currentMusicVolume =
                GetMusicVolume();

            float currentSfxVolume =
                GetSfxVolume();

            // Mevcut seviyeleri hatırla
            if (currentMusicVolume > 0.001f)
            {
                previousMusicVolume =
                    currentMusicVolume;
            }

            if (currentSfxVolume > 0.001f)
            {
                previousSfxVolume =
                    currentSfxVolume;
            }

            // Tüm sesleri kapat
            SetMusicVolume(0f);
            SetSfxVolume(0f);

            allSoundMuted = true;
        }
        else
        {
            // Önceki ses seviyelerine dön
            float musicToRestore =
                previousMusicVolume > 0.001f
                ? previousMusicVolume
                : 0.4f;

            float sfxToRestore =
                previousSfxVolume > 0.001f
                ? previousSfxVolume
                : 1f;

            SetMusicVolume(
                musicToRestore
            );

            SetSfxVolume(
                sfxToRestore
            );

            allSoundMuted = false;
        }
    }

    public bool IsAllSoundMuted()
    {
        return allSoundMuted;
    }

    // =====================================
    // SES KISAYOLLARI
    // =====================================

    public void PlayShoot()
    {
        PlaySFX(
            shootClip,
            shootVolume
        );
    }

    public void PlayEnemyHit()
    {
        PlaySFX(
            enemyHitClip,
            enemyHitVolume
        );
    }

    public void PlayEnemyDeath()
    {
        PlaySFX(
            enemyDeathClip,
            enemyDeathVolume
        );
    }

    public void PlayGoldPickup()
    {
        PlaySFX(
            goldPickupClip,
            goldPickupVolume
        );
    }

    public void PlayPlayerHurt()
    {
        PlaySFX(
            playerHurtClip,
            playerHurtVolume
        );
    }

    public void PlayUpgradePurchase()
    {
        PlaySFX(
            upgradePurchaseClip,
            upgradePurchaseVolume
        );
    }

    public void PlayButtonClick()
    {
        PlaySFX(
            buttonClickClip,
            buttonClickVolume
        );
    }
}