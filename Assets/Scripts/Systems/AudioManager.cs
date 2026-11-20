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

    [Header("Silah Sesleri (boşsa kodla üretilen ses çalar)")]
    public AudioClip shotgunShotClip;
    public AudioClip minigunShotClip;
    public AudioClip laserShotClip;
    public AudioClip sniperShotClip;
    public AudioClip rocketShotClip;
    [Range(0f, 1f)] public float weaponShotVolume = 0.55f;

    [Header("Kalkan ve Ödül Sesleri (boşsa kodla üretilen ses çalar)")]
    public AudioClip shieldHitClip;
    public AudioClip shieldBreakClip;
    public AudioClip rewardClip;
    public AudioClip questCompleteClip;
    [Range(0f, 1f)] public float rewardVolume = 0.7f;

    [Header("Müzik")]
    public AudioClip backgroundMusic;

    [Range(0f, 1f)]
    public float musicVolume = 0.4f;

    [Tooltip("Boss sırasında çalan müzik. Boş bırakılırsa ana müzik hızlanır ve üstüne gergin bir bas ritmi eklenir.")]
    public AudioClip bossMusic;

    [Tooltip("Boss müziğine geçiş süresi (saniye)")]
    public float bossMusicFade = 1.2f;

    private const string MusicVolumeKey = GameKeys.MusicVolume;
    private const string SfxVolumeKey = GameKeys.SfxVolume;

    // Ateş klibi bir seri; her atışta sadece ilk patlaması çalınır.
    private const float ShootClipLength = 0.16f;

    private float previousMusicVolume = 0.4f;
    private float previousSfxVolume = 1f;

    private bool allSoundMuted = false;

    // Boss müziği: 0 = normal müzik, 1 = tamamen boss müziği
    private AudioSource bossSource;
    private bool bossMode;
    private float bossBlend;

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
    private Channel enemyShotChannel;

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
        enemyShotChannel = CreateChannel(2, 0.12f);
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
    // BOSS MÜZİĞİ
    // =====================================

    public void SetBossMusic(bool active)
    {
        if (bossMode == active)
            return;

        bossMode = active;

        if (!active || musicSource == null)
            return;

        if (bossSource == null)
        {
            bossSource = gameObject.AddComponent<AudioSource>();
            bossSource.playOnAwake = false;
            bossSource.loop = true;
            bossSource.spatialBlend = 0f;
            bossSource.outputAudioMixerGroup = musicSource.outputAudioMixerGroup;
            bossSource.volume = 0f;
        }

        if (bossSource.clip == null)
        {
            bossSource.clip = bossMusic != null ? bossMusic : CreateTensionPulse();
        }

        if (!bossSource.isPlaying)
        {
            bossSource.Play();
        }
    }

    void Update()
    {
        float target = bossMode ? 1f : 0f;

        if (Mathf.Approximately(bossBlend, target))
            return;

        bossBlend = Mathf.MoveTowards(
            bossBlend,
            target,
            Time.unscaledDeltaTime / Mathf.Max(0.05f, bossMusicFade)
        );

        ApplyMusicBlend();

        if (bossBlend <= 0f && bossSource != null && bossSource.isPlaying)
        {
            bossSource.Stop();
        }
    }

    void ApplyMusicBlend()
    {
        if (musicSource == null)
            return;

        float volume = GetMusicVolume();

        if (bossMusic != null)
        {
            // Ayrı boss müziği: iki parça arasında yumuşak geçiş
            musicSource.volume = volume * (1f - bossBlend);
            musicSource.pitch = 1f;
        }
        else
        {
            // Klip yoksa ana müzik biraz hızlanır, altına bas ritmi eklenir.
            musicSource.volume = volume;
            musicSource.pitch = Mathf.Lerp(1f, 1.1f, bossBlend);
        }

        if (bossSource != null)
        {
            bossSource.volume = volume * bossBlend * (bossMusic != null ? 1f : 1.3f);
        }
    }

    // Kodla üretilen gergin ritim: dakikada 130 vuruşluk derin "kalp atışı"
    // (çift vuruş) ve altta titreşen alçak bir uğultu. Döngüsel çalar.
    static AudioClip CreateTensionPulse()
    {
        const int sampleRate = 22050;
        const float bpm = 130f;
        const int beats = 8;

        float beatLength = 60f / bpm;
        int length = Mathf.RoundToInt(sampleRate * beatLength * beats);
        float[] data = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float beatTime = t % beatLength;
            int beatIndex = (int)(t / beatLength);

            float sample = 0f;

            // Her vuruşta perdesi düşen bir darbe (kick); tek vuruşlarda ikinci, hafif darbe
            sample += Kick(beatTime) * 0.9f;

            if (beatIndex % 2 == 1)
            {
                sample += Kick(beatTime - beatLength * 0.5f) * 0.45f;
            }

            // Alçak uğultu (41 Hz), vuruş hızında nefes alıyor
            float tremolo = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t / beatLength);
            sample += Mathf.Sin(2f * Mathf.PI * 41.2f * t) * 0.18f * tremolo;

            // Her dört vuruşta bir yükselen, gerilim veren ince ses
            float bar = t % (beatLength * 4f) / (beatLength * 4f);
            sample += Mathf.Sin(2f * Mathf.PI * (330f + 110f * bar) * t) * 0.025f * bar;

            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("BossTensionPulse", length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Kick(float time)
    {
        if (time < 0f || time > 0.35f)
            return 0f;

        // 95 Hz'den 40 Hz'e inen sinüs, hızla sönüyor
        float frequency = Mathf.Lerp(95f, 40f, Mathf.Clamp01(time / 0.12f));
        float envelope = Mathf.Exp(-time * 11f);
        return Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope;
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

        ApplyMusicBlend();

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

    // Uzaktan ateş eden düşman: silah sesinin kısa ve tiz hâli
    public void PlayEnemyShot()
    {
        Play(enemyShotChannel, shootClip, shootVolume * 0.35f, 1.7f, 0.06f, 0.09f);
    }

    // Silaha özel ateş sesi. Tüfek orijinal sesi kullanır.
    public void PlayWeaponShot(WeaponCatalog.Weapon weapon)
    {
        if (weapon == null || !weapon.useSynthSound)
        {
            PlayShoot(weapon != null ? weapon.soundPitch : 1f);
            return;
        }

        AudioClip clip = WeaponClip(weapon.synthSound);
        Play(shootChannel, clip, weaponShotVolume, 1f, 0.05f, clip != null ? clip.length : 0f);
    }

    AudioClip WeaponClip(SynthSounds.Sound sound)
    {
        AudioClip custom = null;

        switch (sound)
        {
            case SynthSounds.Sound.Shotgun: custom = shotgunShotClip; break;
            case SynthSounds.Sound.Minigun: custom = minigunShotClip; break;
            case SynthSounds.Sound.Laser: custom = laserShotClip; break;
            case SynthSounds.Sound.Sniper: custom = sniperShotClip; break;
            case SynthSounds.Sound.Rocket: custom = rocketShotClip; break;
        }

        return custom != null ? custom : SynthSounds.Get(sound);
    }

    static AudioClip OrSynth(AudioClip custom, SynthSounds.Sound sound) =>
        custom != null ? custom : SynthSounds.Get(sound);

    // Kalkana isabet: metalik tın
    public void PlayShieldHit()
    {
        Play(hitChannel, OrSynth(shieldHitClip, SynthSounds.Sound.ShieldHit), enemyHitVolume * 0.8f, 1f, 0.08f);
    }

    // Kalkan kırıldı: cam kırılması
    public void PlayShieldBreak()
    {
        Play(explosionChannel, OrSynth(shieldBreakClip, SynthSounds.Sound.ShieldBreak), Mathf.Min(1f, enemyHitVolume * 1.6f), 1f, 0.05f);
    }

    // Ödül alındı (görev, sezon, günlük ödül, başarım)
    public void PlayReward()
    {
        Play(uiChannel, OrSynth(rewardClip, SynthSounds.Sound.RewardChime), rewardVolume);
    }

    // Oyun sırasında bir görev tamamlandı
    public void PlayQuestComplete()
    {
        Play(eventChannel, OrSynth(questCompleteClip, SynthSounds.Sound.QuestComplete), rewardVolume);
    }

    // Boss uyarısı: çok derin bir darbe
    public void PlayBossWarning()
    {
        Play(eventChannel, enemyHitClip, 1f, 0.4f);
    }
}
