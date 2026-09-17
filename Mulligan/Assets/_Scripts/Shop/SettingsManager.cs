using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : Singleton<SettingsManager>
{
    private const string MusicVolumeKey = "Settings_MusicVolume";
    private const string SoundVolumeKey = "Settings_SoundVolume";
    private const string VibrationsEnabledKey = "Settings_VibrationsEnabled";
    private const string LanguageIndexKey = "Settings_LanguageIndex";

    [Header("Window")]
    public GameObject SettingsWindow;
    public CanvasGroup bgCanvasGroup;

    [Header("Audio")]
    public Slider MusicVolumeSlider;
    public Slider SoundVolumeSlider;

    [Header("Vibrations")]
    public Image VibrationToggleImage;
    public Sprite VibrationOnSprite;
    public Sprite VibrationOffSprite;

    [Header("Language")]
    public TMP_Text CurrentLanguageLabel;
    public List<string> Languages = new List<string> { "English" };

    [Header("Defaults")]
    [Range(0f, 1f)] public float DefaultMusicVolume = 1f;
    [Range(0f, 1f)] public float DefaultSoundVolume = 1f;
    public bool DefaultVibrationsEnabled = true;

    private Vector2 startPosition;
    private int currentLanguageIndex;
    private bool isInitialized;

    public void Init()
    {
        if (isInitialized)
            return;

        if (SettingsWindow != null)
            startPosition = SettingsWindow.GetComponent<RectTransform>().anchoredPosition;

        EnsureLanguageList();
        LoadSettings();
        ApplySettings();
        UpdateUI();
        isInitialized = true;
    }

    public void ShowWindow()
    {
        if (!isInitialized)
            Init();

        SoundManager.TryPlay(SoundType.WindowOpen);
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        UpdateUI();

        if (bgCanvasGroup != null)
        {
            bgCanvasGroup.gameObject.SetActive(true);
            bgCanvasGroup.alpha = 0f;
            LeanTween.alphaCanvas(bgCanvasGroup, 1f, 0.25f).setEaseOutQuad();
        }

        if (SettingsWindow == null)
            return;

        RectTransform windowRect = SettingsWindow.GetComponent<RectTransform>();
        SettingsWindow.SetActive(true);
        windowRect.anchoredPosition = new Vector2(startPosition.x, -Screen.height);
        LeanTween.move(windowRect, startPosition, 0.5f).setEaseOutBack();
    }

    public void HideWindow()
    {
        SoundManager.TryPlay(SoundType.WindowClose);
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        SaveSettings();

        if (bgCanvasGroup != null)
        {
            bgCanvasGroup.alpha = 1f;
            LeanTween.alphaCanvas(bgCanvasGroup, 0f, 0.25f).setEaseInQuad();
        }

        if (SettingsWindow == null)
            return;

        RectTransform windowRect = SettingsWindow.GetComponent<RectTransform>();
        Vector2 hidePosition = new Vector2(windowRect.anchoredPosition.x, -Screen.height);

        LeanTween.move(windowRect, hidePosition, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                SettingsWindow.SetActive(false);
                windowRect.anchoredPosition = startPosition;

                if (bgCanvasGroup != null)
                    bgCanvasGroup.gameObject.SetActive(false);
            });
    }

    public void SetMusicVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, clampedVolume);

        if (SoundManager.Instance != null)
            SoundManager.Instance.SetMusicVolume(clampedVolume);
    }

    public void SetSoundVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SoundVolumeKey, clampedVolume);

        if (SoundManager.Instance != null)
            SoundManager.Instance.SetSfxVolume(clampedVolume);
    }

    public void ToggleVibrations()
    {
        bool wasEnabled = PlayerPrefs.GetInt(VibrationsEnabledKey, DefaultVibrationsEnabled ? 1 : 0) == 1;
        bool vibrationsEnabled = !wasEnabled;

        SoundManager.TryPlay(SoundType.ButtonTap);
        if (wasEnabled)
            VibrationsManager.TryVibrate(VibrationType.ButtonTap);

        PlayerPrefs.SetInt(VibrationsEnabledKey, vibrationsEnabled ? 1 : 0);

        if (VibrationsManager.Instance != null)
            VibrationsManager.Instance.VibrationsEnabled = vibrationsEnabled;

        UpdateVibrationImage(vibrationsEnabled);

        if (!wasEnabled)
            VibrationsManager.TryVibrate(VibrationType.ButtonTap);
    }

    public void PreviousLanguage()
    {
        ChangeLanguage(-1);
    }

    public void NextLanguage()
    {
        ChangeLanguage(1);
    }

    public void ResetToDefault()
    {
        SoundManager.TryPlay(SoundType.ButtonTap);
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);

        currentLanguageIndex = GetDefaultLanguageIndex();
        PlayerPrefs.SetFloat(MusicVolumeKey, DefaultMusicVolume);
        PlayerPrefs.SetFloat(SoundVolumeKey, DefaultSoundVolume);
        PlayerPrefs.SetInt(VibrationsEnabledKey, DefaultVibrationsEnabled ? 1 : 0);
        PlayerPrefs.SetInt(LanguageIndexKey, currentLanguageIndex);

        ApplySettings();
        UpdateUI();
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        currentLanguageIndex = PlayerPrefs.GetInt(LanguageIndexKey, GetDefaultLanguageIndex());
        currentLanguageIndex = Mathf.Clamp(currentLanguageIndex, 0, Languages.Count - 1);
    }

    private void ApplySettings()
    {
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
        float soundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, DefaultSoundVolume);
        bool vibrationsEnabled = PlayerPrefs.GetInt(VibrationsEnabledKey, DefaultVibrationsEnabled ? 1 : 0) == 1;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusicVolume(musicVolume);
            SoundManager.Instance.SetSfxVolume(soundVolume);
        }

        if (VibrationsManager.Instance != null)
            VibrationsManager.Instance.VibrationsEnabled = vibrationsEnabled;
    }

    private void UpdateUI()
    {
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
        float soundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, DefaultSoundVolume);
        bool vibrationsEnabled = PlayerPrefs.GetInt(VibrationsEnabledKey, DefaultVibrationsEnabled ? 1 : 0) == 1;

        if (MusicVolumeSlider != null)
            MusicVolumeSlider.SetValueWithoutNotify(musicVolume);

        if (SoundVolumeSlider != null)
            SoundVolumeSlider.SetValueWithoutNotify(soundVolume);

        UpdateVibrationImage(vibrationsEnabled);

        if (CurrentLanguageLabel != null)
            CurrentLanguageLabel.text = Languages[currentLanguageIndex];
    }

    private void UpdateVibrationImage(bool vibrationsEnabled)
    {
        if (VibrationToggleImage == null)
            return;

        VibrationToggleImage.sprite = vibrationsEnabled ? VibrationOnSprite : VibrationOffSprite;
    }

    private void ChangeLanguage(int direction)
    {
        SoundManager.TryPlay(SoundType.ButtonTap);
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        EnsureLanguageList();

        currentLanguageIndex = (currentLanguageIndex + direction + Languages.Count) % Languages.Count;
        PlayerPrefs.SetInt(LanguageIndexKey, currentLanguageIndex);

        if (CurrentLanguageLabel != null)
            CurrentLanguageLabel.text = Languages[currentLanguageIndex];
    }

    private void EnsureLanguageList()
    {
        if (Languages == null)
            Languages = new List<string>();

        if (Languages.Count == 0)
            Languages.Add("English");
    }

    private int GetDefaultLanguageIndex()
    {
        for (int i = 0; i < Languages.Count; i++)
        {
            if (string.Equals(Languages[i], "English", System.StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return 0;
    }

    private void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveSettings();
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }
}
