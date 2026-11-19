using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Ses Kaynakları")]
    [Tooltip("Ses seviyesi ve mikser grubu buradan alınır; efektler kendi kanallarından çalar.")]
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

    // Ateş klibi bir seri; her atışta sadece ilk patlaması çalınır.
    private const float ShootClipLength = 0.16f;

    private float previousMusicVolume = 0.4f;
    private float previousSfxVolume = 1f;

    private bool allSoundMuted = false;

    // ==================================
    // SES KANALLARI
    // ==================================

    // Her ses türü kendi kanalında çalar: aynı anda çalan ses sayısı ve
    // iki ses arasındaki en kısa süre sınırlıdır. Böylece çok sayıda isabet /
    // ölüm aynı anda olunca sesler üst üste binip gürültüye dönüşmez.
    private class Channel
    {
        public AudioSource[] voices;
        public int next;
        public float minInterval;
        public float lastPlayTime = -10f;
    }

    private Channel shootChannel;
    private Channel hitChannel;
    private Channel deathChannel;
    private Channel goldChannel;
    private Channel hurtChannel;
    private Channel explosionChannel;
    private Channel uiChannel;
    private Channel eventChannel;

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

        shootChannel = CreateChannel(1, 0f);
        hitChannel = CreateChannel(3, 0.05f);
        deathChannel = CreateChannel(4, 0.07f);
        goldChannel = CreateChannel(2, 0.09f);
        hurtChannel = CreateChannel(1, 0.15f);
        explosionChannel = CreateChannel(2, 0.1f);
        uiChannel = CreateChannel(3, 0.03f);
        eventChannel = CreateChannel(2, 0.2f);
    }

    Channel CreateChannel(int voiceCount, float minInterval)
    {
        Channel channel = new Channel
        {
            voices = new AudioSource[voiceCount],
            minInterval = minInterval
        };

        for (int i = 0; i < voiceCount; i++)
        {
            AudioSource voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 0f;

            if (sfxSource != null)
            {
                voice.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
            }

            channel.voices[i] = voice;
        }

        return channel;
    }

    float SfxVolume =>
        sfxSource != null ? sfxSource.volume : GetSfxVolume();

    // pitchVariance: her çalışta perde biraz değişsin (tekrar eden ses yorucu olmasın)
    // maxLength: > 0 ise ses bu süre sonra kesilir
    void Play(Channel channel, AudioClip clip, float volume, float pitch = 1f, float pitchVariance = 0f, float maxLength = 0f)
    {
        if (clip == null || channel == null)
            return;

        float now = Time.unscaledTime;

        if (now - channel.lastPlayTime < channel.minInterval)
            return;

        float finalVolume = volume * SfxVolume;

        if (finalVolume <= 0.001f)
            return;

        channel.lastPlayTime = now;

        // Kanal doluysa en eski sesin yerine çalar.
        AudioSource voice = channel.voices[channel.next];
        channel.next = (channel.next + 1) % channel.voices.Length;

        voice.Stop();
        voice.clip = clip;
        voice.volume = finalVolume;
        voice.pitch = pitch * Random.Range(1f - pitchVariance, 1f + pitchVariance);
        voice.Play();

        if (maxLength > 0f)
        {
            voice.SetScheduledEndTime(AudioSettings.dspTime + maxLength / Mathf.Max(0.1f, voice.pitch));
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
        Play(uiChannel, clip, volume);
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

    // pitch: silaha göre perde (pompalı tok, lazer ince, roket derin)
    public void PlayShoot(float pitch = 1f)
    {
        Play(shootChannel, shootClip, shootVolume, pitch, 0.04f, ShootClipLength);
    }

    public void PlayEnemyHit(bool critical = false)
    {
        // Kritik vuruş daha tiz ve biraz daha yüksek
        Play(
            hitChannel,
            enemyHitClip,
            critical ? Mathf.Min(1f, enemyHitVolume * 1.3f) : enemyHitVolume,
            critical ? 1.45f : 1f,
            0.1f
        );
    }

    public void PlayEnemyDeath()
    {
        Play(deathChannel, enemyDeathClip, enemyDeathVolume, 1f, 0.12f);
    }

    public void PlayGoldPickup()
    {
        Play(goldChannel, goldPickupClip, goldPickupVolume, 1.05f, 0.08f);
    }

    public void PlayPlayerHurt()
    {
        Play(hurtChannel, playerHurtClip, playerHurtVolume, 1f, 0.05f);
    }

    public void PlayUpgradePurchase()
    {
        Play(uiChannel, upgradePurchaseClip, upgradePurchaseVolume);
    }

    public void PlayButtonClick()
    {
        Play(uiChannel, buttonClickClip, buttonClickVolume);
    }

    // Patlama: yere çarpma sesinin derin hâli
    public void PlayExplosion()
    {
        Play(explosionChannel, enemyHitClip, Mathf.Min(1f, enemyHitVolume * 1.6f), 0.55f, 0.06f);
    }

    // Seviye atlama: altın sesinin tiz hâli
    public void PlayLevelUp()
    {
        Play(eventChannel, goldPickupClip, Mathf.Min(1f, goldPickupVolume * 1.6f), 1.35f);
    }

    // Boss uyarısı: çok derin bir darbe
    public void PlayBossWarning()
    {
        Play(eventChannel, enemyHitClip, 1f, 0.4f);
    }
}
