using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[CustomEditor(typeof(LocalizationData))]
public class LocalizationDataEditor : Editor
{
    private static UnityWebRequest activeRequest;
    private static UnityWebRequestAsyncOperation activeOperation;
    private static Action<string> requestSuccess;
    private static Action<string> requestFailure;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Google Sheet Tools", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(activeRequest != null))
        {
            if (GUILayout.Button("Sync From Google Sheet"))
                SyncFromGoogleSheet((LocalizationData)target);

            if (GUILayout.Button("Copy Table"))
                CopyTable((LocalizationData)target);
        }

        if (GUILayout.Button("Validate"))
            ValidateAndReport((LocalizationData)target);

        if (activeRequest != null)
            EditorGUILayout.HelpBox("Downloading localization data...", MessageType.Info);
    }

    private static void SyncFromGoogleSheet(LocalizationData asset)
    {
        string url = NormalizeGoogleSheetUrl(asset.GoogleSheetUrl);
        if (string.IsNullOrEmpty(url))
        {
            EditorUtility.DisplayDialog("Localization", "Assign a public Google Sheet URL first.", "OK");
            return;
        }

        StartRequest(url,
            csv => ApplyCsv(asset, csv),
            error => EditorUtility.DisplayDialog("Localization Sync Failed", error, "OK"));
    }

    private static void CopyTable(LocalizationData asset)
    {
        if (!string.IsNullOrWhiteSpace(asset.GameDataJsonUrl))
        {
            StartRequest(asset.GameDataJsonUrl,
                json =>
                {
                    CardDataExportWrapper wrapper = CardDataObject.WrapperFromJson(json);
                    if (HasGameData(wrapper))
                        BuildAndCopyTable(asset, wrapper, "live JSON");
                    else
                        BuildAndCopyTable(asset, GetFallbackGameData(), "cached/local data");
                },
                error =>
                {
                    Debug.LogWarning("Could not download live game data for localization table: " + error);
                    BuildAndCopyTable(asset, GetFallbackGameData(), "cached/local data");
                });
            return;
        }

        BuildAndCopyTable(asset, GetFallbackGameData(), "cached/local data");
    }

    private static void BuildAndCopyTable(LocalizationData asset, CardDataExportWrapper gameData, string sourceName)
    {
        LocalizationTable table = new LocalizationTable(asset);

        foreach (LocalizationSourceText source in LocalizationDefaultTexts.GetAll())
            table.AddSource(source.TextID, source.English, "runtime text");

        AddGameData(table, gameData);
        AddSceneText(table);

        if (table.Errors.Count > 0)
        {
            EditorUtility.DisplayDialog("Localization Table Has Conflicts", string.Join("\n", table.Errors), "OK");
            return;
        }

        GUIUtility.systemCopyBuffer = table.BuildTsv();
        Debug.Log("Copied " + table.Count + " localization rows from " + sourceName + " to the clipboard.");
        EditorUtility.DisplayDialog("Localization", "Copied " + table.Count + " rows to the clipboard using " + sourceName + ".", "OK");
    }

    private static void AddGameData(LocalizationTable table, CardDataExportWrapper data)
    {
        if (data == null)
            return;

        if (data.allCards != null)
        {
            foreach (CardData item in data.allCards)
            {
                if (item != null)
                    table.AddSource(LocalizationKeys.UnitName(item), item.cardName, "unit " + item.cardName);
            }
        }

        if (data.allArtifacts != null)
        {
            foreach (ArtifactData item in data.allArtifacts)
            {
                if (item == null)
                    continue;

                string name = item.name != null ? item.name.Replace("RandomRace", "{0}") : string.Empty;
                table.AddSource(LocalizationKeys.Artifact(item, "name"), name, "artifact " + item.name);
                table.AddSource(LocalizationKeys.Artifact(item, "description"), item.description, "artifact " + item.name);
            }
        }

        if (data.allPotions != null)
        {
            foreach (PotionCardData item in data.allPotions)
            {
                if (item == null)
                    continue;

                table.AddSource(LocalizationKeys.Potion(item, "name"), item.name, "potion " + item.name);
                table.AddSource(LocalizationKeys.Potion(item, "description"), item.description, "potion " + item.name);
            }
        }

        if (data.allUpgradeCards != null)
        {
            foreach (UpgradeCardData item in data.allUpgradeCards)
            {
                if (item == null)
                    continue;

                table.AddSource(LocalizationKeys.Upgrade(item, "name"), item.name, "upgrade " + item.name);
                table.AddSource(LocalizationKeys.Upgrade(item, "description"), item.description, "upgrade " + item.name);
            }
        }

        if (data.allRunes != null)
        {
            foreach (RuneData item in data.allRunes)
            {
                if (item == null)
                    continue;

                table.AddSource(LocalizationKeys.Rune(item, "name"), item.name, "rune " + item.name);
                table.AddSource(LocalizationKeys.Rune(item, "description"), item.description, "rune " + item.name);
            }
        }

        if (data.allBosses != null)
        {
            foreach (BossData item in data.allBosses)
            {
                if (item == null)
                    continue;

                table.AddSource(LocalizationKeys.Boss(item, "name"), item.name, "boss " + item.name);
                table.AddSource(LocalizationKeys.Boss(item, "description"), item.description, "boss " + item.name);
            }
        }

        if (data.allEnemies != null)
        {
            foreach (EnemyData item in data.allEnemies)
            {
                if (item != null)
                    table.AddSource(LocalizationKeys.Enemy(item), item.name, "enemy " + item.name);
            }
        }

        if (data.allHeroes != null)
        {
            for (int i = 0; i < data.allHeroes.Length; i++)
            {
                HeroData item = data.allHeroes[i];
                if (item == null)
                    continue;

                table.AddSource(LocalizationKeys.Hero(i, "name"), item.heroName, "hero " + i);
                table.AddSource(LocalizationKeys.Hero(i, "description"), item.description, "hero " + i);
                table.AddSource(LocalizationKeys.Hero(i, "starting_items"), LocalizedContent.GetHeroStartingItemsFallback(item), "hero " + i);
            }
        }

        if (data.allSkipeRewards != null)
        {
            foreach (SkipRewardData item in data.allSkipeRewards)
            {
                if (item == null)
                    continue;

                table.AddSource(LocalizationKeys.SkipReward(item, "title"), item.title, "skip reward " + item.type);
                table.AddSource(LocalizationKeys.SkipReward(item, "description"), item.description, "skip reward " + item.type);
            }
        }

        foreach (CardRace race in Enum.GetValues(typeof(CardRace)))
        {
            if (race != CardRace.END)
                table.AddSource("race." + LocalizationService.Slug(race.ToString()), race.ToString(), "race");
        }

        foreach (CardClass cardClass in Enum.GetValues(typeof(CardClass)))
            table.AddSource("class." + LocalizationService.Slug(cardClass.ToString()), cardClass.ToString(), "class");

        foreach (RarityType rarity in Enum.GetValues(typeof(RarityType)))
            table.AddSource("rarity." + LocalizationService.Slug(rarity.ToString()), rarity.ToString(), "rarity");

        foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
            table.AddSource("upgrade_type." + LocalizationService.Slug(type.ToString()), type.ToString(), "upgrade type");
    }

    private static void AddSceneText(LocalizationTable table)
    {
        TextLocaliser[] localisers = UnityEngine.Object.FindObjectsOfType<TextLocaliser>(true);
        foreach (TextLocaliser localiser in localisers)
        {
            if (localiser == null || string.IsNullOrWhiteSpace(localiser.TextID))
                continue;

            string fallback = localiser.DefaultText;
            TMPro.TMP_Text targetText = localiser.TargetText != null
                ? localiser.TargetText
                : localiser.GetComponent<TMPro.TMP_Text>();
            if (string.IsNullOrEmpty(fallback) && targetText != null)
                fallback = targetText.text;

            table.AddSource(localiser.TextID, fallback, "TextLocaliser " + localiser.name, true);
        }

        TutorialController[] tutorials = UnityEngine.Object.FindObjectsOfType<TutorialController>(true);
        FieldInfo stepsField = typeof(TutorialController).GetField("steps", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (TutorialController tutorial in tutorials)
        {
            if (stepsField?.GetValue(tutorial) is List<TutorialController.TutorialStep> steps)
            {
                foreach (TutorialController.TutorialStep step in steps)
                {
                    if (step != null && !string.IsNullOrWhiteSpace(step.Id))
                        table.AddSource(LocalizationKeys.Tutorial(step.Id), step.Dialogue, "tutorial " + step.Id);
                }
            }
        }

        DailyQuestManager[] questManagers = UnityEngine.Object.FindObjectsOfType<DailyQuestManager>(true);
        foreach (DailyQuestManager manager in questManagers)
        {
            foreach (DailyQuestDefinition definition in manager.QuestDefinitions)
            {
                if (definition != null)
                    table.AddSource(LocalizationKeys.DailyQuest(definition), DailyQuestManager.GetQuestFallbackText(definition), "daily quest");
            }
        }
    }

    private static CardDataExportWrapper GetFallbackGameData()
    {
        if (PlayerPrefs.HasKey("GameData"))
        {
            CardDataExportWrapper cached = CardDataObject.WrapperFromJson(PlayerPrefs.GetString("GameData"));
            if (HasGameData(cached))
                return cached;
        }

        CardDataObject local = Resources.Load<CardDataObject>("CardContainer");
        if (local == null)
            return null;

        return new CardDataExportWrapper
        {
            allCards = local.allCards,
            allArtifacts = local.allArtifacts,
            allPotions = local.allPotions,
            allUpgradeCards = local.allUpgradeCards,
            allBosses = local.allBosses,
            allEnemies = local.allEnemies,
            allSkipeRewards = local.allSkipeRewards,
            allRunes = local.allRunes,
            allHeroes = local.allHeroes
        };
    }

    private static bool HasGameData(CardDataExportWrapper data)
    {
        return data != null &&
               ((data.allCards != null && data.allCards.Length > 0) ||
                (data.allArtifacts != null && data.allArtifacts.Length > 0) ||
                (data.allPotions != null && data.allPotions.Length > 0) ||
                (data.allUpgradeCards != null && data.allUpgradeCards.Length > 0) ||
                (data.allRunes != null && data.allRunes.Length > 0) ||
                (data.allHeroes != null && data.allHeroes.Length > 0));
    }

    private static void ApplyCsv(LocalizationData asset, string csv)
    {
        List<List<string>> rows = ParseCsv(csv);
        if (rows.Count == 0 || rows[0].Count < 2)
        {
            EditorUtility.DisplayDialog("Localization Sync Failed", "The sheet is empty or has no language columns.", "OK");
            return;
        }

        string firstHeader = rows[0][0].Trim().TrimStart('\uFEFF');
        if (!string.Equals(firstHeader, "TextID", StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("Localization Sync Failed", "The first column must be named TextID.", "OK");
            return;
        }

        List<string> languages = new List<string>();
        HashSet<string> languageSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int column = 1; column < rows[0].Count; column++)
        {
            string language = rows[0][column].Trim();
            if (string.IsNullOrEmpty(language) || !languageSet.Add(language))
            {
                EditorUtility.DisplayDialog("Localization Sync Failed", "Language headers must be non-empty and unique.", "OK");
                return;
            }

            languages.Add(language);
        }

        if (!languageSet.Contains("English"))
        {
            EditorUtility.DisplayDialog("Localization Sync Failed", "The sheet must contain an English language column.", "OK");
            return;
        }

        Dictionary<string, List<string>> sheetEntries = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        for (int row = 1; row < rows.Count; row++)
        {
            if (rows[row].Count == 0 || string.IsNullOrWhiteSpace(rows[row][0]))
                continue;

            string textId = rows[row][0].Trim();
            if (sheetEntries.ContainsKey(textId))
            {
                EditorUtility.DisplayDialog("Localization Sync Failed", "Duplicate TextID: " + textId, "OK");
                return;
            }

            List<string> values = new List<string>();
            for (int column = 0; column < languages.Count; column++)
            {
                int sourceColumn = column + 1;
                string value = sourceColumn < rows[row].Count ? DecodeEscapes(rows[row][sourceColumn]) : string.Empty;
                values.Add(value);
            }

            sheetEntries.Add(textId, values);
        }

        Undo.RecordObject(asset, "Sync Localization Data");
        asset.Languages = languages;
        if (!asset.HasLanguage(asset.DefaultLanguage))
            asset.DefaultLanguage = "English";

        Dictionary<string, LocalizationEntry> existing = new Dictionary<string, LocalizationEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (LocalizationEntry entry in asset.Entries)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.TextID) && !existing.ContainsKey(entry.TextID))
                existing.Add(entry.TextID, entry);
        }

        foreach (KeyValuePair<string, List<string>> pair in sheetEntries)
        {
            if (!existing.TryGetValue(pair.Key, out LocalizationEntry entry))
            {
                entry = new LocalizationEntry { TextID = pair.Key };
                asset.Entries.Add(entry);
                existing.Add(pair.Key, entry);
            }

            entry.TextID = pair.Key;
            for (int i = 0; i < languages.Count; i++)
                entry.SetText(languages[i], pair.Value[i]);
        }

        asset.Entries.Sort((a, b) => string.Compare(a?.TextID, b?.TextID, StringComparison.OrdinalIgnoreCase));
        asset.RebuildLookup();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        if (Application.isPlaying)
            LocalizationService.Refresh();

        Debug.Log("Synced " + sheetEntries.Count + " localization rows from Google Sheets.");
        EditorUtility.DisplayDialog("Localization", "Synced " + sheetEntries.Count + " rows and " + languages.Count + " languages.", "OK");
    }

    private static void ValidateAndReport(LocalizationData asset)
    {
        List<string> errors = Validate(asset);
        if (errors.Count == 0)
            EditorUtility.DisplayDialog("Localization", "Localization data is valid.", "OK");
        else
            EditorUtility.DisplayDialog("Localization Validation", string.Join("\n", errors), "OK");
    }

    private static List<string> Validate(LocalizationData asset)
    {
        List<string> errors = new List<string>();
        HashSet<string> languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string language in asset.Languages)
        {
            if (string.IsNullOrWhiteSpace(language) || !languages.Add(language))
                errors.Add("Language names must be non-empty and unique.");
        }

        if (!languages.Contains("English"))
            errors.Add("Languages must contain English for runtime fallback.");

        HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LocalizationEntry entry in asset.Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.TextID))
                errors.Add("An entry has no TextID.");
            else if (!ids.Add(entry.TextID))
                errors.Add("Duplicate TextID: " + entry.TextID);
        }

        if (!asset.HasLanguage(asset.DefaultLanguage))
            errors.Add("DefaultLanguage must exist in Languages.");

        LocalizationTable generatedTable = new LocalizationTable(asset);
        foreach (LocalizationSourceText source in LocalizationDefaultTexts.GetAll())
            generatedTable.AddSource(source.TextID, source.English, "runtime text");
        AddGameData(generatedTable, GetFallbackGameData());
        AddSceneText(generatedTable);
        errors.AddRange(generatedTable.Errors);

        return errors;
    }

    private static string NormalizeGoogleSheetUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        url = url.Trim();
        if (url.Contains("output=csv") || url.Contains("format=csv") || !url.Contains("docs.google.com/spreadsheets"))
            return url;

        Match documentMatch = Regex.Match(url, @"/spreadsheets/d/([^/]+)");
        if (!documentMatch.Success)
            return url;

        Match gidMatch = Regex.Match(url, @"[?&#]gid=([0-9]+)");
        string gid = gidMatch.Success ? gidMatch.Groups[1].Value : "0";
        return "https://docs.google.com/spreadsheets/d/" + documentMatch.Groups[1].Value + "/export?format=csv&gid=" + gid;
    }

    private static void StartRequest(string url, Action<string> onSuccess, Action<string> onFailure)
    {
        if (activeRequest != null)
        {
            EditorUtility.DisplayDialog("Localization", "Another localization download is already running.", "OK");
            return;
        }

        activeRequest = UnityWebRequest.Get(url);
        activeRequest.timeout = 20;
        activeOperation = activeRequest.SendWebRequest();
        requestSuccess = onSuccess;
        requestFailure = onFailure;
        EditorApplication.update += PollRequest;
    }

    private static void PollRequest()
    {
        if (activeOperation == null || !activeOperation.isDone)
            return;

        EditorApplication.update -= PollRequest;
        UnityWebRequest request = activeRequest;
        Action<string> success = requestSuccess;
        Action<string> failure = requestFailure;
        activeRequest = null;
        activeOperation = null;
        requestSuccess = null;
        requestFailure = null;

        if (request.result == UnityWebRequest.Result.Success)
            success?.Invoke(request.downloadHandler.text);
        else
            failure?.Invoke(request.error);

        request.Dispose();
    }

    private static List<List<string>> ParseCsv(string csv)
    {
        List<List<string>> rows = new List<List<string>>();
        List<string> row = new List<string>();
        StringBuilder cell = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char character = csv[i];
            if (inQuotes)
            {
                if (character == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    cell.Append(character);
                }
            }
            else if (character == '"')
            {
                inQuotes = true;
            }
            else if (character == ',')
            {
                row.Add(cell.ToString());
                cell.Length = 0;
            }
            else if (character == '\n' || character == '\r')
            {
                if (character == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                    i++;

                row.Add(cell.ToString());
                cell.Length = 0;
                rows.Add(row);
                row = new List<string>();
            }
            else
            {
                cell.Append(character);
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }

        return rows;
    }

    private static string EscapeTsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        string escaped = value.Replace("\\", "\\\\").Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n").Replace("\t", "\\t");

        // Google Sheets otherwise converts text such as "+10 Gold" into "=+10 Gold".
        if (StartsWithSpreadsheetFormula(value))
            escaped = "'" + escaped;

        return escaped;
    }

    private static string DecodeEscapes(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length > 1 && value[0] == '\'' && IsSpreadsheetFormulaCharacter(value[1]))
            value = value.Substring(1);

        // Repair values previously coerced by Google Sheets before formula-safe export was added.
        if (value.StartsWith("=+", StringComparison.Ordinal))
            value = value.Substring(1);

        StringBuilder builder = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\' || i + 1 >= value.Length)
            {
                builder.Append(value[i]);
                continue;
            }

            char next = value[++i];
            if (next == 'n')
                builder.Append('\n');
            else if (next == 't')
                builder.Append('\t');
            else
                builder.Append(next);
        }

        return builder.ToString();
    }

    private static bool StartsWithSpreadsheetFormula(string value)
    {
        return string.IsNullOrEmpty(value) == false && IsSpreadsheetFormulaCharacter(value[0]);
    }

    private static bool IsSpreadsheetFormulaCharacter(char character)
    {
        return character == '=' || character == '+' || character == '-' || character == '@';
    }

    private class LocalizationTable
    {
        private readonly LocalizationData asset;
        private readonly SortedDictionary<string, string> sources = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> sourceOrigins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> Errors = new List<string>();

        public int Count => sources.Count;

        public LocalizationTable(LocalizationData localizationAsset)
        {
            asset = localizationAsset;
            foreach (LocalizationEntry entry in asset.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.TextID))
                    continue;

                string english = entry.GetText("English");
                if (string.IsNullOrEmpty(english))
                    english = entry.GetText(asset.DefaultLanguage);

                sources[entry.TextID] = english ?? string.Empty;
                sourceOrigins[entry.TextID] = "localization asset";
            }
        }

        public void AddSource(string textId, string english, string origin, bool allowSharedId = false)
        {
            if (string.IsNullOrWhiteSpace(textId))
                return;

            english = english ?? string.Empty;
            if (sources.TryGetValue(textId, out string existing))
            {
                if (allowSharedId && !string.IsNullOrEmpty(existing))
                    return;

                if (!string.IsNullOrEmpty(existing) && !string.IsNullOrEmpty(english) &&
                    !string.Equals(existing, english, StringComparison.Ordinal) &&
                    sourceOrigins[textId] != "localization asset")
                {
                    Errors.Add("Conflicting ID '" + textId + "' from " + sourceOrigins[textId] + " and " + origin + ".");
                }
            }

            sources[textId] = english;
            sourceOrigins[textId] = origin;
        }

        public string BuildTsv()
        {
            List<string> languages = new List<string>();
            languages.Add("English");
            foreach (string language in asset.Languages)
            {
                if (!string.Equals(language, "English", StringComparison.OrdinalIgnoreCase))
                    languages.Add(language);
            }

            Dictionary<string, LocalizationEntry> existing = new Dictionary<string, LocalizationEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (LocalizationEntry entry in asset.Entries)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.TextID) && !existing.ContainsKey(entry.TextID))
                    existing.Add(entry.TextID, entry);
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("TextID");
            foreach (string language in languages)
                builder.Append('\t').Append(EscapeTsv(language));
            builder.AppendLine();

            foreach (KeyValuePair<string, string> source in sources)
            {
                builder.Append(EscapeTsv(source.Key));
                foreach (string language in languages)
                {
                    string text = string.Empty;
                    if (string.Equals(language, "English", StringComparison.OrdinalIgnoreCase))
                        text = source.Value;
                    else if (existing.TryGetValue(source.Key, out LocalizationEntry entry))
                        text = entry.GetText(language) ?? string.Empty;

                    builder.Append('\t').Append(EscapeTsv(text));
                }
                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
