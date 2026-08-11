using System;
using ImpulseVibrations;
using UnityEngine;

public enum VibrationType
{
    Tap,
    CardTap,
    ButtonTap,
    Potion,
    EnemyDamage,
    PlayerDamage,
    Success,
    Error
}

public class VibrationsManager : Singleton<VibrationsManager>
{
    [Serializable]
    public class VibrationSettings
    {
        public long AndroidMilliseconds = 20;
        [Range(-1, 255)] public int AndroidAmplitude = 80;
        public bool UseAndroidHapticFeedback = true;
        public HapticFeedbackConstants AndroidFeedback = HapticFeedbackConstants.VIRTUAL_KEY;
        public ImpactTypeFeedback IOSImpact = ImpactTypeFeedback.IMPACT_LIGHT;
        public bool UseIOSNotification = false;
        public NotificationTypeFeedback IOSNotification = NotificationTypeFeedback.NOTIFICATION_SUCCESS;
    }

    public bool VibrationsEnabled = true;
    public bool VibrateInEditor = false;
    public bool UseAndroidUnityFallback = true;

    [Header("Presets")]
    public VibrationSettings Tap = new VibrationSettings { AndroidMilliseconds = 35, AndroidAmplitude = 120, AndroidFeedback = HapticFeedbackConstants.VIRTUAL_KEY, IOSImpact = ImpactTypeFeedback.IMPACT_LIGHT };
    public VibrationSettings CardTap = new VibrationSettings { AndroidMilliseconds = 40, AndroidAmplitude = 140, AndroidFeedback = HapticFeedbackConstants.KEYBOARD_TAP, IOSImpact = ImpactTypeFeedback.IMPACT_LIGHT };
    public VibrationSettings ButtonTap = new VibrationSettings { AndroidMilliseconds = 35, AndroidAmplitude = 130, AndroidFeedback = HapticFeedbackConstants.VIRTUAL_KEY, IOSImpact = ImpactTypeFeedback.IMPACT_LIGHT };
    public VibrationSettings Potion = new VibrationSettings { AndroidMilliseconds = 55, AndroidAmplitude = 170, AndroidFeedback = HapticFeedbackConstants.CONTEXT_CLICK, IOSImpact = ImpactTypeFeedback.IMPACT_MEDIUM };
    public VibrationSettings EnemyDamage = new VibrationSettings { AndroidMilliseconds = 60, AndroidAmplitude = 190, AndroidFeedback = HapticFeedbackConstants.CONTEXT_CLICK, IOSImpact = ImpactTypeFeedback.IMPACT_MEDIUM };
    public VibrationSettings PlayerDamage = new VibrationSettings { AndroidMilliseconds = 80, AndroidAmplitude = 230, AndroidFeedback = HapticFeedbackConstants.LONG_PRESS, IOSImpact = ImpactTypeFeedback.IMPACT_HEAVY };
    public VibrationSettings Success = new VibrationSettings { AndroidMilliseconds = 70, AndroidAmplitude = 200, AndroidFeedback = HapticFeedbackConstants.CONFIRM, UseIOSNotification = true, IOSNotification = NotificationTypeFeedback.NOTIFICATION_SUCCESS };
    public VibrationSettings Error = new VibrationSettings { AndroidMilliseconds = 90, AndroidAmplitude = 255, AndroidFeedback = HapticFeedbackConstants.REJECT, UseIOSNotification = true, IOSNotification = NotificationTypeFeedback.NOTIFICATION_ERROR };

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
            DontDestroyOnLoad(gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstanceIfMissing()
    {
        if (FindObjectOfType<VibrationsManager>() != null)
            return;

        GameObject go = new GameObject("VibrationsManager");
        go.AddComponent<VibrationsManager>();
    }

    public static void TryVibrate(VibrationType type = VibrationType.Tap)
    {
        if (Instance != null)
            Instance.Vibrate(type);
    }

    public void Vibrate()
    {
        Vibrate(VibrationType.Tap);
    }

    public void Vibrate(VibrationType type = VibrationType.Tap)
    {
        if (!VibrationsEnabled)
            return;

#if UNITY_EDITOR
        if (!VibrateInEditor)
            return;
#endif

        VibrationSettings settings = GetSettings(type);
        if (settings == null)
            return;

        try
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (!Vibrator.IsHapticEngineSupported)
                return;

            if (settings.UseIOSNotification)
                Vibrator.iOSVibrate(settings.IOSNotification);
            else
                Vibrator.iOSVibrate(settings.IOSImpact);
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (!Vibrator.IsHapticEngineSupported)
                return;

            if (settings.UseAndroidHapticFeedback && Vibrator.AndroidVibrate(settings.AndroidFeedback))
                return;

            Vibrator.AndroidVibrate(settings.AndroidMilliseconds, settings.AndroidAmplitude);
            if (UseAndroidUnityFallback)
                Handheld.Vibrate();
#endif
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Vibration failed: " + exception.Message);
        }
    }

    private VibrationSettings GetSettings(VibrationType type)
    {
        switch (type)
        {
            case VibrationType.CardTap:
                return CardTap;
            case VibrationType.ButtonTap:
                return ButtonTap;
            case VibrationType.Potion:
                return Potion;
            case VibrationType.EnemyDamage:
                return EnemyDamage;
            case VibrationType.PlayerDamage:
                return PlayerDamage;
            case VibrationType.Success:
                return Success;
            case VibrationType.Error:
                return Error;
            default:
                return Tap;
        }
    }
}
