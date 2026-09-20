using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public static class LocalizationService
{
    public const string LanguagePlayerPrefsKey = "Settings_Language";

    public static event Action LanguageChanged;

    private static LocalizationData data;
    private static readonly HashSet<string> MissingIds = new HashSet<string>();
    private static bool missingAssetLogged;

    public static LocalizationData Data
    {
        get
        {
            if (data == null)
                data = Resources.Load<LocalizationData>(LocalizationData.ResourceName);

            if (data == null && !missingAssetLogged)
            {
                missingAssetLogged = true;
                Debug.LogWarning("LocalizationData was not found in Resources. Raw English text will be used.");
            }

            return data;
        }
    }

    public static string CurrentLanguage
    {
        get
        {
            LocalizationData localizationData = Data;
            string defaultLanguage = localizationData != null ? localizationData.DefaultLanguage : "English";
            string language = PlayerPrefs.GetString(LanguagePlayerPrefsKey, defaultLanguage);

            if (localizationData != null && !localizationData.HasLanguage(language))
                return localizationData.DefaultLanguage;

            return language;
        }
    }

    public static IReadOnlyList<string> Languages
    {
        get
        {
            if (Data != null && Data.Languages != null && Data.Languages.Count > 0)
                return Data.Languages;

            return new[] { "English" };
        }
    }

    public static void SetLanguage(string language)
    {
        LocalizationData localizationData = Data;
        string selectedLanguage = localizationData != null
            ? localizationData.GetLanguage(language)
            : "English";

        bool changed = !string.Equals(CurrentLanguage, selectedLanguage, StringComparison.OrdinalIgnoreCase);
        string savedLanguage = PlayerPrefs.GetString(LanguagePlayerPrefsKey, "");

        if (!string.Equals(savedLanguage, selectedLanguage, StringComparison.OrdinalIgnoreCase))
        {
            PlayerPrefs.SetString(LanguagePlayerPrefsKey, selectedLanguage);
            PlayerPrefs.Save();
        }

        if (changed)
            LanguageChanged?.Invoke();
    }

    public static string Get(string textId, string fallback = "")
    {
        LocalizationData localizationData = Data;
        if (localizationData != null)
        {
            if (localizationData.TryGetText(textId, CurrentLanguage, out string localizedText))
                return localizedText;

            if (localizationData.TryGetText(textId, "English", out string englishText))
                return englishText;
        }

        LogMissingId(textId);

        if (!string.IsNullOrEmpty(fallback))
            return fallback;

        return textId ?? string.Empty;
    }

    public static string Format(string textId, string fallback, params object[] values)
    {
        string format = Get(textId, fallback);

        try
        {
            return string.Format(CultureInfo.InvariantCulture, format, values);
        }
        catch (FormatException exception)
        {
            Debug.LogWarning("Invalid localization format for '" + textId + "': " + exception.Message);
            return format;
        }
    }

    public static string Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";

        string normalized = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(normalized.Length);
        bool previousUnderscore = false;

        for (int i = 0; i < normalized.Length; i++)
        {
            char character = normalized[i];
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (character <= 127 && char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousUnderscore = false;
            }
            else if (!previousUnderscore && builder.Length > 0)
            {
                builder.Append('_');
                previousUnderscore = true;
            }
        }

        string slug = builder.ToString().Trim('_');
        return string.IsNullOrEmpty(slug) ? "unknown" : slug;
    }

    public static string Number(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    public static void Refresh()
    {
        if (Data != null)
            Data.RebuildLookup();

        LanguageChanged?.Invoke();
    }

    private static void LogMissingId(string textId)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (string.IsNullOrEmpty(textId) || !MissingIds.Add(textId))
            return;

        Debug.LogWarning("Missing localization ID: " + textId);
#endif
    }
}
