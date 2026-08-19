using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

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

public class HighscoreManager : Singleton<HighscoreManager>
{
    public const string DailyLeaderboardId = "daily_global";
    public const string AllTimeLeaderboardId = "all_time_global";

    private const string MaxLevelKey = "Highscore_MaxLevel_Hero_";
    private const string MaxDamageKey = "Highscore_MaxDamage_Hero_";

    public bool IsUGSReady { get; private set; }
    public string PlayerId { get; private set; } = "";

    private int currentRunHeroIndex = -1;
    private int currentRunTopHit = 0;
    private bool hasActiveRun = false;
    private bool initStarted = false;

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
            DontDestroyOnLoad(gameObject);
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

        initStarted = true;
        _ = InitUGSAsync();
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
            PlayerName = string.IsNullOrEmpty(entry.PlayerName) ? "Player" : entry.PlayerName,
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
}
