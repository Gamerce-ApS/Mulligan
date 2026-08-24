using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class RunScore
{
    public string HeroId;
    public int HeroIndex;
    public int LevelReached;
    public long TopHit;
}

[Serializable]
public class LeaderboardScoreMetadata
{
    public string hero;
    public int heroIndex;
    public long topHit;
}

public class LeaderboardEntryData
{
    public int Rank;
    public string PlayerId;
    public string PlayerName;
    public string HeroId;
    public int HeroIndex;
    public int LevelReached;
    public long TopHit;
}

public class HeroBestScoreData
{
    public int HeroIndex;
    public string HeroId;
    public int LevelReached;
    public long TopHit;
}

[Serializable]
public class HighscoreHeroPortraitSettings
{
    public string HeroName;
    public Vector2 Offset;
    public float Scale = 1f;
}

public class HighscoreManager : Singleton<HighscoreManager>
{
    private enum HighscoreTab
    {
        Daily,
        AllTime,
        YourBest
    }

    public const string DailyLeaderboardId = "daily_global";
    public const string AllTimeLeaderboardId = "all_time_global";

    private const string MaxLevelKey = "Highscore_MaxLevel_Hero_";
    private const string MaxDamageKey = "Highscore_MaxDamage_Hero_";

    public bool IsUGSReady { get; private set; }
    public string PlayerId { get; private set; } = "";
    public string PlayerDisplayName { get; private set; } = "Player";
    public string PlayerDisplayNameForUI => GetDisplayPlayerName(PlayerDisplayName);

    [Header("Window")]
    public GameObject ShopWindow;
    public CanvasGroup bgCanvasGroup;
    public ScrollRect LeaderboardScrollRect;
    public Transform LeaderboardParent;
    public GameObject ScoreEntryTemplate;
    public GameObject DailyButton;
    public GameObject AllTimeButton;
    public GameObject YourBestButton;
    public int LeaderboardLimit = 50;
    public int PlayerRangeLimit = 5;
    public int VisibleEntryCount = 7;

    [Header("Today's Best")]
    public Image TodaysBestHeroPortrait;
    public TMP_Text TodaysBestLevelLabel;
    public TMP_Text TodaysBestTopHitLabel;

    [Header("Set Name")]
    public GameObject SetNameWindow;
    public CanvasGroup SetNameCanvasGroup;
    public TMP_InputField NameInputField;
    public TMP_Text CurrentNameLabel;
    public TMP_Text NameErrorLabel;
    public int MaxCustomNameLength = 20;

    [Header("Hero Portrait Tuning")]
    public List<HighscoreHeroPortraitSettings> HeroPortraitSettings = new List<HighscoreHeroPortraitSettings>();

    public Vector3 startPosition;

    private int currentRunHeroIndex = -1;
    private int currentRunTopHit = 0;
    private bool hasActiveRun = false;
    private bool initStarted = false;
    private HighscoreTab activeTab = HighscoreTab.Daily;

    protected override void Awake()
    {
        base.Awake();

    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstanceIfMissing()
    {
        if (FindObjectOfType<HighscoreManager>() != null)
            return;

        GameObject go = new GameObject("HighscoreManager");
        go.AddComponent<HighscoreManager>();
    }

    public void Init()
    {
        if (initStarted)
            return;

        if (ShopWindow != null)
            startPosition = ShopWindow.GetComponent<RectTransform>().anchoredPosition;

        if (ScoreEntryTemplate != null)
            ScoreEntryTemplate.SetActive(false);

        if (SetNameWindow != null)
            SetNameWindow.SetActive(false);

        UpdateCurrentNameLabel();

        initStarted = true;
        _ = InitUGSAsync();
    }

    public void ShowWindow()
    {
        SoundManager.TryPlay(SoundType.WindowOpen);
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);

        if (bgCanvasGroup != null)
        {
            bgCanvasGroup.gameObject.SetActive(true);
            bgCanvasGroup.alpha = 0;
            LeanTween.alphaCanvas(bgCanvasGroup, 1f, 0.25f).setEaseOutQuad();
        }

        if (ShopWindow != null)
        {
            ShopWindow.SetActive(true);
            Vector2 targetPos = startPosition;
            ShopWindow.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPos.x, -Screen.height);
            LeanTween.move(ShopWindow.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack();
        }

        activeTab = HighscoreTab.Daily;
        UpdateTabHighlights();
        UpdateCurrentNameLabel();
        PopulateDailyLeaderboard();
        UpdateTodaysBestUI();
    }

    public void HideWindow()
    {
        SoundManager.TryPlay(SoundType.WindowClose);
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);

        if (bgCanvasGroup != null)
        {
            bgCanvasGroup.alpha = 1;
            LeanTween.alphaCanvas(bgCanvasGroup, 0f, 0.25f).setEaseInQuad();
        }

        if (ShopWindow == null)
            return;

        Vector2 hidePos = new Vector2(ShopWindow.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                ShopWindow.SetActive(false);
                ShopWindow.GetComponent<RectTransform>().anchoredPosition = startPosition;

                if (bgCanvasGroup != null)
                    bgCanvasGroup.gameObject.SetActive(false);
            });
    }

    public void ClickDaily()
    {
        PlayButtonFeedback();
        activeTab = HighscoreTab.Daily;
        UpdateTabHighlights();
        PopulateDailyLeaderboard();
    }

    public void ClickAllTime()
    {
        PlayButtonFeedback();
        activeTab = HighscoreTab.AllTime;
        UpdateTabHighlights();
        PopulateAllTimeLeaderboard();
    }

    public void ClickYourBest()
    {
        PlayButtonFeedback();
        activeTab = HighscoreTab.YourBest;
        UpdateTabHighlights();
        PopulateYourBestLeaderboard();
    }

    public void ClickSetName()
    {
        PlayButtonFeedback();
        ShowSetNameWindow();
    }

    public void ClickCancelSetName()
    {
        PlayButtonFeedback();
        HideSetNameWindow();
    }

    public void ClickConfirmSetName()
    {
        PlayButtonFeedback();

        if (NameInputField == null)
            return;

        _ = SetPlayerNameAsync(NameInputField.text);
    }

    public void StartRun(int heroIndex)
    {
        currentRunHeroIndex = heroIndex;
        currentRunTopHit = 0;
        hasActiveRun = true;
    }

    public void SubmitCurrentRun()
    {
        if (hasActiveRun == false)
            return;

        RunScore score = new RunScore
        {
            HeroIndex = currentRunHeroIndex >= 0 ? currentRunHeroIndex : GameData.HeroSelected,
            LevelReached = Mathf.Max(0, GameData.CurrentRound),
            TopHit = currentRunTopHit
        };
        score.HeroId = GetHeroId(score.HeroIndex);

        hasActiveRun = false;
        SubmitRun(score);
    }

    public void SubmitRun(RunScore score)
    {
        if (score == null)
            return;

        if (score.HeroIndex < 0)
            score.HeroIndex = GameData.HeroSelected;

        if (string.IsNullOrEmpty(score.HeroId))
            score.HeroId = GetHeroId(score.HeroIndex);

        if (score.LevelReached < 0 || score.TopHit < 0)
        {
            Debug.LogWarning("Invalid highscore skipped.");
            return;
        }

        UpdateLocalHeroBest(score);
        _ = SubmitRunRemoteAsync(score);
    }

    public void UpdateMaxLevel(int level)
    {
        int heroIndex = GameData.HeroSelected;
        if (level <= GetMaxLevel(heroIndex))
            return;

        PlayerPrefs.SetInt(GetMaxLevelKey(heroIndex), level);
        PlayerPrefs.Save();
    }

    public void UpdateMaxDamage(int damage)
    {
        if (damage > currentRunTopHit)
            currentRunTopHit = damage;

        int heroIndex = GameData.HeroSelected;
        if (damage <= GetMaxDamage(heroIndex))
            return;

        PlayerPrefs.SetInt(GetMaxDamageKey(heroIndex), damage);
        PlayerPrefs.Save();
    }

    public int GetMaxLevel(int heroIndex)
    {
        return PlayerPrefs.GetInt(GetMaxLevelKey(heroIndex), 0);
    }

    public int GetMaxDamage(int heroIndex)
    {
        return PlayerPrefs.GetInt(GetMaxDamageKey(heroIndex), 0);
    }

    public List<HeroBestScoreData> GetMyHeroBestScores()
    {
        List<HeroBestScoreData> scores = new List<HeroBestScoreData>();

        int heroCount = CardContainer.Instance != null && CardContainer.Instance.HeroDataList != null
            ? CardContainer.Instance.HeroDataList.Length
            : 0;

        for (int i = 0; i < heroCount; i++)
        {
            scores.Add(new HeroBestScoreData
            {
                HeroIndex = i,
                HeroId = GetHeroId(i),
                LevelReached = GetMaxLevel(i),
                TopHit = GetMaxDamage(i)
            });
        }

        return scores;
    }

    public Task<List<LeaderboardEntryData>> GetDailyLeaderboard(int limit = 50)
    {
        return GetLeaderboard(DailyLeaderboardId, limit);
    }

    public Task<List<LeaderboardEntryData>> GetAllTimeLeaderboard(int limit = 50)
    {
        return GetLeaderboard(AllTimeLeaderboardId, limit);
    }

    public Task<LeaderboardEntryData> GetMyDailyLeaderboardScore()
    {
        return GetMyLeaderboardScore(DailyLeaderboardId);
    }

    public Task<LeaderboardEntryData> GetMyAllTimeLeaderboardScore()
    {
        return GetMyLeaderboardScore(AllTimeLeaderboardId);
    }

    public async Task<LeaderboardEntryData> GetDailyBest()
    {
        List<LeaderboardEntryData> entries = await GetDailyLeaderboard(1);
        if (entries.Count == 0)
            return null;

        return entries[0];
    }

    public Sprite GetHeroPortraitSprite(string heroId)
    {
        return GetHeroPortraitSprite(GetHeroIndex(heroId));
    }

    public Sprite GetHeroPortraitSprite(int heroIndex)
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.TheHero == null ||
            GameManager.Instance.TheHero.HeroPortraits == null ||
            heroIndex < 0 ||
            heroIndex >= GameManager.Instance.TheHero.HeroPortraits.Count)
            return null;

        Image selectedPortrait = GameManager.Instance.TheHero.HeroPortraits[heroIndex].GetComponent<Image>();
        if (selectedPortrait == null)
            return null;

        return selectedPortrait.sprite;
    }

    public void ApplyHeroPortrait(Image target, string heroId)
    {
        ApplyHeroPortrait(target, GetHeroIndex(heroId));
    }

    public void ApplyHeroPortrait(Image target, int heroIndex)
    {
        if (target == null)
            return;

        Sprite portrait = GetHeroPortraitSprite(heroIndex);
        if (portrait != null)
            target.sprite = portrait;

        ApplyHeroPortraitTransform(target, heroIndex);
    }

    public string GetHeroId(int heroIndex)
    {
        return "hero_" + heroIndex;
    }

    public int GetHeroIndex(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
            return -1;

        if (heroId.StartsWith("hero_") == false)
            return -1;

        string value = heroId.Substring("hero_".Length);
        int index;
        if (int.TryParse(value, out index))
            return index;

        return -1;
    }

    private async Task InitUGSAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (AuthenticationService.Instance.IsSignedIn == false)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            PlayerId = AuthenticationService.Instance.PlayerId;
            await InitPlayerNameAsync();
            IsUGSReady = true;
            Debug.Log("Highscore UGS initialized. PlayerID: " + PlayerId);
        }
        catch (Exception e)
        {
            IsUGSReady = false;
            Debug.LogWarning("Highscore UGS init failed. Local highscores still work. " + e.Message);
        }
    }

    private async Task SubmitRunRemoteAsync(RunScore score)
    {
        if (IsUGSReady == false)
            await InitUGSAsync();

        if (IsUGSReady == false)
            return;

        try
        {
            LeaderboardScoreMetadata metadata = new LeaderboardScoreMetadata
            {
                hero = score.HeroId,
                heroIndex = score.HeroIndex,
                topHit = score.TopHit
            };

            await LeaderboardsService.Instance.AddPlayerScoreAsync(
                DailyLeaderboardId,
                score.LevelReached,
                new AddPlayerScoreOptions { Metadata = metadata });

            await LeaderboardsService.Instance.AddPlayerScoreAsync(
                AllTimeLeaderboardId,
                score.LevelReached,
                new AddPlayerScoreOptions { Metadata = metadata });
        }
        catch (Exception e)
        {
            Debug.LogWarning("Leaderboard submit failed. Local highscore was saved. " + e.Message);
        }
    }

    private async Task InitPlayerNameAsync()
    {
        try
        {
            string playerName = await AuthenticationService.Instance.GetPlayerNameAsync(false);
            if (string.IsNullOrEmpty(playerName))
                playerName = await AuthenticationService.Instance.UpdatePlayerNameAsync("Player");

            PlayerDisplayName = playerName;
            UpdateCurrentNameLabel();
        }
        catch (Exception e)
        {
            PlayerDisplayName = "Player";
            UpdateCurrentNameLabel();
            Debug.LogWarning("Failed to set Unity player name. Leaderboards still work. " + e.Message);
        }
    }

    private void ShowSetNameWindow()
    {
        if (SetNameWindow == null)
            return;

        if (NameErrorLabel != null)
            NameErrorLabel.text = "";

        SetNameWindow.SetActive(true);

        if (SetNameCanvasGroup != null)
        {
            SetNameCanvasGroup.alpha = 0f;
            LeanTween.alphaCanvas(SetNameCanvasGroup, 1f, 0.2f).setEaseOutQuad();
        }

        if (NameInputField != null)
        {
            NameInputField.characterLimit = Mathf.Clamp(MaxCustomNameLength, 1, 50);
            NameInputField.text = GetPlayerNameWithoutSuffix(PlayerDisplayName);
            NameInputField.Select();
            NameInputField.ActivateInputField();
        }
    }

    private void HideSetNameWindow()
    {
        if (SetNameWindow == null)
            return;

        if (SetNameCanvasGroup == null)
        {
            SetNameWindow.SetActive(false);
            return;
        }

        SetNameCanvasGroup.alpha = 1f;
        LeanTween.alphaCanvas(SetNameCanvasGroup, 0f, 0.2f)
            .setEaseInQuad()
            .setOnComplete(() =>
            {
                SetNameWindow.SetActive(false);
            });
    }

    private async Task SetPlayerNameAsync(string rawName)
    {
        string playerName = CleanPlayerName(rawName);
        if (string.IsNullOrEmpty(playerName))
        {
            ShowNameError("Enter a name");
            return;
        }

        if (IsUGSReady == false)
            await InitUGSAsync();

        if (IsUGSReady == false)
        {
            ShowNameError("Could not connect");
            return;
        }

        try
        {
            string updatedName = await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            PlayerDisplayName = updatedName;
            UpdateCurrentNameLabel();
            HideSetNameWindow();
            RefreshActiveLeaderboardTab();
        }
        catch (Exception e)
        {
            ShowNameError("Name unavailable");
            Debug.LogWarning("Failed to update player name. " + e.Message);
        }
    }

    private string CleanPlayerName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return "";

        string trimmed = rawName.Trim();
        string cleaned = "";
        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = trimmed[i];
            if (char.IsWhiteSpace(c))
                continue;

            cleaned += c;
        }

        int maxLength = Mathf.Clamp(MaxCustomNameLength, 1, 50);
        if (cleaned.Length > maxLength)
            cleaned = cleaned.Substring(0, maxLength);

        return cleaned;
    }

    private string GetPlayerNameWithoutSuffix(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return "";

        int suffixIndex = playerName.LastIndexOf("#", StringComparison.Ordinal);
        if (suffixIndex <= 0)
            return playerName;

        return playerName.Substring(0, suffixIndex);
    }

    private void ShowNameError(string message)
    {
        if (NameErrorLabel != null)
            NameErrorLabel.text = message;
    }

    private void UpdateCurrentNameLabel()
    {
        if (CurrentNameLabel != null)
            CurrentNameLabel.text = PlayerDisplayNameForUI;
    }

    private void RefreshActiveLeaderboardTab()
    {
        switch (activeTab)
        {
            case HighscoreTab.Daily:
                PopulateDailyLeaderboard();
                break;
            case HighscoreTab.AllTime:
                PopulateAllTimeLeaderboard();
                break;
            case HighscoreTab.YourBest:
                PopulateYourBestLeaderboard();
                break;
        }
    }

    private async Task<List<LeaderboardEntryData>> GetLeaderboard(string leaderboardId, int limit)
    {
        List<LeaderboardEntryData> entries = new List<LeaderboardEntryData>();

        if (IsUGSReady == false)
            await InitUGSAsync();

        if (IsUGSReady == false)
            return entries;

        try
        {
            var scores = await LeaderboardsService.Instance.GetScoresAsync(
                leaderboardId,
                new GetScoresOptions
                {
                    Limit = Mathf.Clamp(limit, 1, 100),
                    IncludeMetadata = true
                });

            foreach (LeaderboardEntry entry in scores.Results)
                entries.Add(ConvertEntry(entry));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Leaderboard read failed for " + leaderboardId + ". " + e.Message);
        }

        return entries;
    }

    private async Task<List<LeaderboardEntryData>> GetLeaderboardAroundPlayer(string leaderboardId, int rangeLimit)
    {
        List<LeaderboardEntryData> entries = new List<LeaderboardEntryData>();

        if (IsUGSReady == false)
            await InitUGSAsync();

        if (IsUGSReady == false)
            return entries;

        try
        {
            var scores = await LeaderboardsService.Instance.GetPlayerRangeAsync(
                leaderboardId,
                new GetPlayerRangeOptions
                {
                    RangeLimit = Mathf.Max(1, rangeLimit),
                    IncludeMetadata = true
                });

            foreach (LeaderboardEntry entry in scores.Results)
                entries.Add(ConvertEntry(entry));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Player leaderboard range read failed for " + leaderboardId + ". " + e.Message);
        }

        return entries;
    }

    private async Task<LeaderboardEntryData> GetMyLeaderboardScore(string leaderboardId)
    {
        if (IsUGSReady == false)
            await InitUGSAsync();

        if (IsUGSReady == false)
            return null;

        try
        {
            LeaderboardEntry entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(
                leaderboardId,
                new GetPlayerScoreOptions { IncludeMetadata = true });

            return ConvertEntry(entry);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Player leaderboard read failed for " + leaderboardId + ". " + e.Message);
            return null;
        }
    }

    private LeaderboardEntryData ConvertEntry(LeaderboardEntry entry)
    {
        LeaderboardScoreMetadata metadata = ReadMetadata(entry.Metadata);
        string heroId = metadata != null ? metadata.hero : "";
        int heroIndex = metadata != null ? metadata.heroIndex : -1;

        if (string.IsNullOrEmpty(heroId) && heroIndex >= 0)
            heroId = GetHeroId(heroIndex);

        if (heroIndex < 0)
            heroIndex = GetHeroIndex(heroId);

        return new LeaderboardEntryData
        {
            Rank = entry.Rank + 1,
            PlayerId = entry.PlayerId,
            PlayerName = GetDisplayPlayerName(entry.PlayerName),
            HeroId = heroId,
            HeroIndex = heroIndex,
            LevelReached = Mathf.RoundToInt((float)entry.Score),
            TopHit = metadata != null ? metadata.topHit : 0
        };
    }

    private LeaderboardScoreMetadata ReadMetadata(object metadata)
    {
        if (metadata == null)
            return null;

        try
        {
            if (metadata is string metadataString)
                return JsonConvert.DeserializeObject<LeaderboardScoreMetadata>(metadataString);

            JObject metadataObject = JObject.FromObject(metadata);
            return new LeaderboardScoreMetadata
            {
                hero = metadataObject.Value<string>("hero") ?? metadataObject.Value<string>("Hero"),
                heroIndex = metadataObject.Value<int?>("heroIndex") ?? metadataObject.Value<int?>("HeroIndex") ?? -1,
                topHit = metadataObject.Value<long?>("topHit") ?? metadataObject.Value<long?>("TopHit") ?? 0
            };
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to parse leaderboard metadata: " + e.Message);
            return null;
        }
    }

    private void UpdateLocalHeroBest(RunScore score)
    {
        int currentLevel = GetMaxLevel(score.HeroIndex);
        int currentDamage = GetMaxDamage(score.HeroIndex);

        bool changed = false;
        if (score.LevelReached > currentLevel)
        {
            PlayerPrefs.SetInt(GetMaxLevelKey(score.HeroIndex), score.LevelReached);
            changed = true;
        }

        if (score.TopHit > currentDamage)
        {
            PlayerPrefs.SetInt(GetMaxDamageKey(score.HeroIndex), (int)score.TopHit);
            changed = true;
        }

        if (changed)
            PlayerPrefs.Save();
    }

    private string GetMaxLevelKey(int heroIndex)
    {
        return MaxLevelKey + heroIndex;
    }

    private string GetMaxDamageKey(int heroIndex)
    {
        return MaxDamageKey + heroIndex;
    }

    private async void PopulateDailyLeaderboard()
    {
        await PopulateRemoteLeaderboard(DailyLeaderboardId, HighscoreTab.Daily);
    }

    private async void PopulateAllTimeLeaderboard()
    {
        await PopulateRemoteLeaderboard(AllTimeLeaderboardId, HighscoreTab.AllTime);
    }

    private async Task PopulateRemoteLeaderboard(string leaderboardId, HighscoreTab tab)
    {
        ClearEntries();

        List<LeaderboardEntryData> entries = await GetLeaderboard(leaderboardId, LeaderboardLimit);
        bool playerIsVisible = entries.Exists(entry => entry.PlayerId == PlayerId);

        if (playerIsVisible == false)
        {
            List<LeaderboardEntryData> playerRange = await GetLeaderboardAroundPlayer(leaderboardId, PlayerRangeLimit);
            if (playerRange.Count > 0)
                entries = playerRange;
        }

        if (activeTab != tab)
            return;

        PopulateEntries(entries);
        ScrollToPlayerIfNeeded(entries);
    }

    private void PopulateYourBestLeaderboard()
    {
        ClearEntries();

        if (LeaderboardParent == null || ScoreEntryTemplate == null)
            return;

        List<HeroBestScoreData> scores = GetMyHeroBestScores();
        scores.Sort((a, b) =>
        {
            int levelCompare = b.LevelReached.CompareTo(a.LevelReached);
            if (levelCompare != 0)
                return levelCompare;

            return b.TopHit.CompareTo(a.TopHit);
        });

        for (int i = 0; i < scores.Count; i++)
        {
            GameObject entryObject = Instantiate(ScoreEntryTemplate, LeaderboardParent);
            entryObject.SetActive(true);

            HighscoreEntryItem item = entryObject.GetComponent<HighscoreEntryItem>();
            if (item != null)
                item.Init(i + 1, PlayerDisplayNameForUI, scores[i].HeroIndex, scores[i].LevelReached, scores[i].TopHit);
        }

        ResetScrollPosition();
    }

    private void PopulateEntries(List<LeaderboardEntryData> entries)
    {
        if (LeaderboardParent == null || ScoreEntryTemplate == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            GameObject entryObject = Instantiate(ScoreEntryTemplate, LeaderboardParent);
            entryObject.SetActive(true);

            HighscoreEntryItem item = entryObject.GetComponent<HighscoreEntryItem>();
            if (item != null)
                item.Init(entries[i]);
        }
    }

    private void ClearEntries()
    {
        if (LeaderboardParent == null || ScoreEntryTemplate == null)
            return;

        for (int i = LeaderboardParent.childCount - 1; i >= 0; i--)
        {
            if (LeaderboardParent.GetChild(i).gameObject != ScoreEntryTemplate)
                Destroy(LeaderboardParent.GetChild(i).gameObject);
        }

        ResetScrollPosition();
    }

    private void UpdateTodaysBestUI()
    {
        _ = UpdateTodaysBestUIAsync();
    }

    private async Task UpdateTodaysBestUIAsync()
    {
        if (TodaysBestLevelLabel != null)
            TodaysBestLevelLabel.text = "...";

        if (TodaysBestTopHitLabel != null)
            TodaysBestTopHitLabel.text = "...";

        LeaderboardEntryData dailyBest = await GetDailyBest();
        if (dailyBest == null)
        {
            if (TodaysBestLevelLabel != null)
                TodaysBestLevelLabel.text = "0";

            if (TodaysBestTopHitLabel != null)
                TodaysBestTopHitLabel.text = "0";

            return;
        }

        if (TodaysBestHeroPortrait != null)
            ApplyHeroPortrait(TodaysBestHeroPortrait, dailyBest.HeroId);

        if (TodaysBestLevelLabel != null)
            TodaysBestLevelLabel.text = "" + dailyBest.LevelReached;

        if (TodaysBestTopHitLabel != null)
            TodaysBestTopHitLabel.text = "" + dailyBest.TopHit;
    }

    private void ScrollToPlayerIfNeeded(List<LeaderboardEntryData> entries)
    {
        if (LeaderboardScrollRect == null || entries.Count <= 1)
            return;

        int playerIndex = entries.FindIndex(entry => entry.PlayerId == PlayerId);
        if (playerIndex < VisibleEntryCount)
            return;

        Canvas.ForceUpdateCanvases();
        LeaderboardScrollRect.verticalNormalizedPosition = 1f - ((float)playerIndex / (entries.Count - 1));
    }

    private void ResetScrollPosition()
    {
        if (LeaderboardScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        LeaderboardScrollRect.verticalNormalizedPosition = 1f;
    }

    private void UpdateTabHighlights()
    {
        SetButtonHighlight(DailyButton, activeTab == HighscoreTab.Daily);
        SetButtonHighlight(AllTimeButton, activeTab == HighscoreTab.AllTime);
        SetButtonHighlight(YourBestButton, activeTab == HighscoreTab.YourBest);
    }

    private void SetButtonHighlight(GameObject button, bool active)
    {
        if (button == null || button.transform.childCount == 0)
            return;

        button.transform.GetChild(0).gameObject.SetActive(active);
    }

    private void PlayButtonFeedback()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        SoundManager.TryPlay(SoundType.ButtonTap);
    }

    private void ApplyHeroPortraitTransform(Image target, int heroIndex)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        if (rectTransform == null)
            return;

        HighscoreHeroPortraitSettings settings = GetHeroPortraitSettings(heroIndex);
        if (settings == null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            return;
        }

        rectTransform.anchoredPosition = settings.Offset;
        rectTransform.localScale = Vector3.one * Mathf.Max(0.01f, settings.Scale);
    }

    private HighscoreHeroPortraitSettings GetHeroPortraitSettings(int heroIndex)
    {
        if (CardContainer.Instance == null ||
            CardContainer.Instance.HeroDataList == null ||
            heroIndex < 0 ||
            heroIndex >= CardContainer.Instance.HeroDataList.Length)
            return null;

        string heroName = CardContainer.Instance.HeroDataList[heroIndex].heroName;
        for (int i = 0; i < HeroPortraitSettings.Count; i++)
        {
            if (HeroPortraitSettings[i] != null && HeroPortraitSettings[i].HeroName == heroName)
                return HeroPortraitSettings[i];
        }

        return null;
    }

    private string GetDisplayPlayerName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return "Player";

        if (playerName.StartsWith("Player#", StringComparison.Ordinal))
            return playerName;

        return GetPlayerNameWithoutSuffix(playerName);
    }
}
