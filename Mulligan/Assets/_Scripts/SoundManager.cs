using System;
using UnityEngine;

public enum SoundType
{
    Tap,
    ButtonTap,
    WindowOpen,
    WindowClose,
    Success,
    Error,
    Tooltip,
    InfoPopup,
    CardTap,
    CardDraw,
    CardMove,
    CardDiscard,
    CardDamageNumber,
    Reroll,
    AttackButton,
    DamageTotal,
    CritTotal,
    DeckShuffle,
    EnemyAttack,
    EnemyDamage,
    EnemyDeath,
    PlayerDamage,
    Dodge,
    Potion,
    PotionTap,
    PotionHeal,
    PotionBuff,
    PotionDestroy,
    ArtifactObtained,
    ArtifactTrigger,
    ArtifactSold,
    RuneObtained,
    RuneTrigger,
    UnitUpgradeSelected,
    UnitUpgradeApplied,
    RankUp,
    Gold,
    ShopEnter,
    ShopReroll,
    ShopPurchase,
    ShopItemDragStart,
    ShopItemDropCancel,
    BattleFromShop,
    Victory,
    Lose,
    BossIntro,
    LevelSelectionOpen,
    InventoryDeckOverviewOpen,
    Unlock
}

public class SoundManager : Singleton<SoundManager>
{
    [Serializable]
    public class SoundSettings
    {
        public AudioClip[] Clips;
        [Range(0f, 1f)] public float Volume = 1f;
        public float PitchMin = 1f;
        public float PitchMax = 1f;
    }

    public bool SoundsEnabled = true;
    public bool MusicEnabled = true;
    [Range(0f, 1f)] public float MasterVolume = 1f;
    [Range(0f, 1f)] public float SfxVolume = 1f;
    [Range(0f, 1f)] public float MusicVolume = 1f;
    [Range(0f, 1f)] public float MenuMusicVolume = 1f;
    [Range(0f, 1f)] public float CombatMusicVolume = 1f;

    public AudioSource SfxSource;
    public AudioSource MusicSource;

    [Header("Music")]
    public AudioClip DefaultMusic;
    public AudioClip MenuMusic;
    public AudioClip CombatMusic;
    public bool PlayMusicOnAwake = false;

    [Header("SFX")]
    public SoundSettings Tap = new SoundSettings();
    public SoundSettings ButtonTap = new SoundSettings();
    public SoundSettings WindowOpen = new SoundSettings();
    public SoundSettings WindowClose = new SoundSettings();
    public SoundSettings Success = new SoundSettings();
    public SoundSettings Error = new SoundSettings();
    public SoundSettings Tooltip = new SoundSettings();
    public SoundSettings InfoPopup = new SoundSettings();
    public SoundSettings CardTap = new SoundSettings();
    public SoundSettings CardDraw = new SoundSettings();
    public SoundSettings CardMove = new SoundSettings();
    public SoundSettings CardDiscard = new SoundSettings();
    public SoundSettings CardDamageNumber = new SoundSettings();
    public SoundSettings Reroll = new SoundSettings();
    public SoundSettings AttackButton = new SoundSettings();
    public SoundSettings DamageTotal = new SoundSettings();
    public SoundSettings CritTotal = new SoundSettings();
    public SoundSettings DeckShuffle = new SoundSettings();
    public SoundSettings EnemyAttack = new SoundSettings();
    public SoundSettings EnemyDamage = new SoundSettings();
    public SoundSettings EnemyDeath = new SoundSettings();
    public SoundSettings PlayerDamage = new SoundSettings();
    public SoundSettings Dodge = new SoundSettings();
    public SoundSettings Potion = new SoundSettings();
    public SoundSettings PotionTap = new SoundSettings();
    public SoundSettings PotionHeal = new SoundSettings();
    public SoundSettings PotionBuff = new SoundSettings();
    public SoundSettings PotionDestroy = new SoundSettings();
    public SoundSettings ArtifactObtained = new SoundSettings();
    public SoundSettings ArtifactTrigger = new SoundSettings();
    public SoundSettings ArtifactSold = new SoundSettings();
    public SoundSettings RuneObtained = new SoundSettings();
    public SoundSettings RuneTrigger = new SoundSettings();
    public SoundSettings UnitUpgradeSelected = new SoundSettings();
    public SoundSettings UnitUpgradeApplied = new SoundSettings();
    public SoundSettings RankUp = new SoundSettings();
    public SoundSettings Gold = new SoundSettings();
    public SoundSettings ShopEnter = new SoundSettings();
    public SoundSettings ShopReroll = new SoundSettings();
    public SoundSettings ShopPurchase = new SoundSettings();
    public SoundSettings ShopItemDragStart = new SoundSettings();
    public SoundSettings ShopItemDropCancel = new SoundSettings();
    public SoundSettings BattleFromShop = new SoundSettings();
    public SoundSettings Victory = new SoundSettings();
    public SoundSettings Lose = new SoundSettings();
    public SoundSettings BossIntro = new SoundSettings();
    public SoundSettings LevelSelectionOpen = new SoundSettings();
    public SoundSettings InventoryDeckOverviewOpen = new SoundSettings();
    public SoundSettings Unlock = new SoundSettings();

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
            DontDestroyOnLoad(gameObject);

        EnsureAudioSources();

        if (PlayMusicOnAwake && DefaultMusic != null)
            PlayMusic(DefaultMusic);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstanceIfMissing()
    {
        if (FindObjectOfType<SoundManager>() != null)
            return;

        GameObject go = new GameObject("SoundManager");
        go.AddComponent<SoundManager>();
    }

    public static void TryPlay(SoundType type)
    {
        SoundManager manager = GetOrCreateInstance();
        if (manager != null)
            manager.Play(type);
    }

    public static void TryPlayMusic(AudioClip clip, bool loop = true)
    {
        SoundManager manager = GetOrCreateInstance();
        if (manager != null)
            manager.PlayMusic(clip, loop);
    }

    public static void TryStopMusic()
    {
        SoundManager manager = GetOrCreateInstance();
        if (manager != null)
            manager.StopMusic();
    }

    public static void TryPlayMenuMusic()
    {
        SoundManager manager = GetOrCreateInstance();
        if (manager != null)
            manager.PlayMenuMusic();
    }

    public static void TryPlayCombatMusic()
    {
        SoundManager manager = GetOrCreateInstance();
        if (manager != null)
            manager.PlayCombatMusic();
    }

    public void Play(SoundType type)
    {
        Play(GetSettings(type));
    }

    public void Play(SoundSettings settings)
    {
        if (!SoundsEnabled || settings == null || settings.Clips == null || settings.Clips.Length == 0)
            return;

        AudioClip clip = settings.Clips[UnityEngine.Random.Range(0, settings.Clips.Length)];
        if (clip == null)
            return;

        EnsureAudioSources();

        SfxSource.pitch = UnityEngine.Random.Range(settings.PitchMin, settings.PitchMax);
        SfxSource.PlayOneShot(clip, settings.Volume * SfxVolume * MasterVolume);
        SfxSource.pitch = 1f;
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (!MusicEnabled || clip == null)
            return;

        EnsureAudioSources();

        if (MusicSource.clip == clip && MusicSource.isPlaying)
        {
            MusicSource.volume = GetMusicVolume(clip);
            return;
        }

        MusicSource.clip = clip;
        MusicSource.loop = loop;
        MusicSource.volume = GetMusicVolume(clip);
        MusicSource.pitch = 1f;
        MusicSource.Play();
    }

    public void PlayMenuMusic()
    {
        PlayMusic(MenuMusic);
    }

    public void PlayCombatMusic()
    {
        PlayMusic(CombatMusic);
    }

    public void StopMusic()
    {
        if (MusicSource != null)
            MusicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        if (MusicSource != null)
            MusicSource.volume = GetMusicVolume(MusicSource.clip);
    }

    public void SetMenuMusicVolume(float volume)
    {
        MenuMusicVolume = Mathf.Clamp01(volume);
        if (MusicSource != null && MusicSource.clip == MenuMusic)
            MusicSource.volume = GetMusicVolume(MusicSource.clip);
    }

    public void SetCombatMusicVolume(float volume)
    {
        CombatMusicVolume = Mathf.Clamp01(volume);
        if (MusicSource != null && MusicSource.clip == CombatMusic)
            MusicSource.volume = GetMusicVolume(MusicSource.clip);
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
    }

    private void EnsureAudioSources()
    {
        if (SfxSource == null)
        {
            SfxSource = gameObject.AddComponent<AudioSource>();
            SfxSource.playOnAwake = false;
            SfxSource.loop = false;
        }

        if (MusicSource == null)
        {
            MusicSource = gameObject.AddComponent<AudioSource>();
            MusicSource.playOnAwake = false;
            MusicSource.loop = true;
        }
    }

    private float GetMusicVolume(AudioClip clip)
    {
        float clipVolume = 1f;
        if (clip == MenuMusic)
            clipVolume = MenuMusicVolume;
        else if (clip == CombatMusic)
            clipVolume = CombatMusicVolume;

        return MusicVolume * clipVolume * MasterVolume;
    }

    private static SoundManager GetOrCreateInstance()
    {
        SoundManager manager = FindObjectOfType<SoundManager>();
        if (manager != null)
            return manager;

        GameObject go = new GameObject("SoundManager");
        return go.AddComponent<SoundManager>();
    }

    private SoundSettings GetSettings(SoundType type)
    {
        switch (type)
        {
            case SoundType.ButtonTap:
                return ButtonTap;
            case SoundType.WindowOpen:
                return WindowOpen;
            case SoundType.WindowClose:
                return WindowClose;
            case SoundType.Success:
                return Success;
            case SoundType.Error:
                return Error;
            case SoundType.Tooltip:
                return Tooltip;
            case SoundType.InfoPopup:
                return InfoPopup;
            case SoundType.CardTap:
                return CardTap;
            case SoundType.CardDraw:
                return CardDraw;
            case SoundType.CardMove:
                return CardMove;
            case SoundType.CardDiscard:
                return CardDiscard;
            case SoundType.CardDamageNumber:
                return CardDamageNumber;
            case SoundType.Reroll:
                return Reroll;
            case SoundType.AttackButton:
                return AttackButton;
            case SoundType.DamageTotal:
                return DamageTotal;
            case SoundType.CritTotal:
                return CritTotal;
            case SoundType.DeckShuffle:
                return DeckShuffle;
            case SoundType.EnemyAttack:
                return EnemyAttack;
            case SoundType.EnemyDamage:
                return EnemyDamage;
            case SoundType.EnemyDeath:
                return EnemyDeath;
            case SoundType.PlayerDamage:
                return PlayerDamage;
            case SoundType.Dodge:
                return Dodge;
            case SoundType.Potion:
                return Potion;
            case SoundType.PotionTap:
                return PotionTap;
            case SoundType.PotionHeal:
                return PotionHeal;
            case SoundType.PotionBuff:
                return PotionBuff;
            case SoundType.PotionDestroy:
                return PotionDestroy;
            case SoundType.ArtifactObtained:
                return ArtifactObtained;
            case SoundType.ArtifactTrigger:
                return ArtifactTrigger;
            case SoundType.ArtifactSold:
                return ArtifactSold;
            case SoundType.RuneObtained:
                return RuneObtained;
            case SoundType.RuneTrigger:
                return RuneTrigger;
            case SoundType.UnitUpgradeSelected:
                return UnitUpgradeSelected;
            case SoundType.UnitUpgradeApplied:
                return UnitUpgradeApplied;
            case SoundType.RankUp:
                return RankUp;
            case SoundType.Gold:
                return Gold;
            case SoundType.ShopEnter:
                return ShopEnter;
            case SoundType.ShopReroll:
                return ShopReroll;
            case SoundType.ShopPurchase:
                return ShopPurchase;
            case SoundType.ShopItemDragStart:
                return ShopItemDragStart;
            case SoundType.ShopItemDropCancel:
                return ShopItemDropCancel;
            case SoundType.BattleFromShop:
                return BattleFromShop;
            case SoundType.Victory:
                return Victory;
            case SoundType.Lose:
                return Lose;
            case SoundType.BossIntro:
                return BossIntro;
            case SoundType.LevelSelectionOpen:
                return LevelSelectionOpen;
            case SoundType.InventoryDeckOverviewOpen:
                return InventoryDeckOverviewOpen;
            case SoundType.Unlock:
                return Unlock;
            default:
                return Tap;
        }
    }
}
