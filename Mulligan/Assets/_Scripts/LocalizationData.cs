using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalizedTextValue
{
    public string Language;
    [TextArea(1, 6)] public string Text;
}

[Serializable]
public class LocalizationEntry
{
    public string TextID;
    public List<LocalizedTextValue> Texts = new List<LocalizedTextValue>();

    public string GetText(string language)
    {
        if (string.IsNullOrEmpty(language))
            return null;

        if (Texts == null)
            return null;

        for (int i = 0; i < Texts.Count; i++)
        {
            LocalizedTextValue value = Texts[i];
            if (value != null && string.Equals(value.Language, language, StringComparison.OrdinalIgnoreCase))
                return value.Text;
        }

        return null;
    }

    public void SetText(string language, string text)
    {
        if (Texts == null)
            Texts = new List<LocalizedTextValue>();

        for (int i = 0; i < Texts.Count; i++)
        {
            LocalizedTextValue value = Texts[i];
            if (value != null && string.Equals(value.Language, language, StringComparison.OrdinalIgnoreCase))
            {
                value.Language = language;
                value.Text = text;
                return;
            }
        }

        Texts.Add(new LocalizedTextValue
        {
            Language = language,
            Text = text
        });
    }
}

[CreateAssetMenu(fileName = "LocalizationData", menuName = "Mulligan Rush/Localization Data")]
public class LocalizationData : ScriptableObject
{
    public const string ResourceName = "LocalizationData";

    [Header("Google Sheet")]
    public string GoogleSheetUrl;
    public string GameDataJsonUrl = "http://gamerce.net/mulligan/data.json";

    [Header("Languages")]
    public string DefaultLanguage = "English";
    public List<string> Languages = new List<string> { "English" };

    [Header("Translations")]
    public List<LocalizationEntry> Entries = new List<LocalizationEntry>();

    private Dictionary<string, LocalizationEntry> entryLookup;

    public bool TryGetText(string textId, string language, out string text)
    {
        text = null;
        if (string.IsNullOrEmpty(textId))
            return false;

        EnsureLookup();
        if (!entryLookup.TryGetValue(textId, out LocalizationEntry entry) || entry == null)
            return false;

        text = entry.GetText(language);
        return string.IsNullOrEmpty(text) == false;
    }

    public bool HasLanguage(string language)
    {
        if (string.IsNullOrEmpty(language))
            return false;

        for (int i = 0; i < Languages.Count; i++)
        {
            if (string.Equals(Languages[i], language, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public string GetLanguage(string language)
    {
        for (int i = 0; i < Languages.Count; i++)
        {
            if (string.Equals(Languages[i], language, StringComparison.OrdinalIgnoreCase))
                return Languages[i];
        }

        return DefaultLanguage;
    }

    public void RebuildLookup()
    {
        entryLookup = new Dictionary<string, LocalizationEntry>(StringComparer.OrdinalIgnoreCase);
        if (Entries == null)
            return;

        for (int i = 0; i < Entries.Count; i++)
        {
            LocalizationEntry entry = Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.TextID) || entryLookup.ContainsKey(entry.TextID))
                continue;

            entryLookup.Add(entry.TextID, entry);
        }
    }

    private void EnsureLookup()
    {
        if (entryLookup == null)
            RebuildLookup();
    }

    private void OnEnable()
    {
        RebuildLookup();
    }

    private void OnValidate()
    {
        if (Languages == null)
            Languages = new List<string>();

        if (Languages.Count == 0)
            Languages.Add("English");

        if (string.IsNullOrWhiteSpace(DefaultLanguage))
            DefaultLanguage = "English";

        RebuildLookup();
    }
}
