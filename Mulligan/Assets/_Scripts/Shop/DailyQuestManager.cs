using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DailyQuestType
{
    DealDamage,
    DefeatBosses,
    ConsumePotions,
    Heal,
    PlayRaceUnits,
    PlayClassUnits,
    ReachLevel,
    DestroyUnits,
    PlayRuns,
    EarnGold,
    SingleAttackDamage,
    UpgradeUnits
}

[Serializable]
public class DailyQuestDefinition
{
    public DailyQuestType Type;
    public string Text;
    public int TargetAmount;
    public CardRace Race;
    public CardClass Class;
}

public class DailyQuestManager : Singleton<DailyQuestManager>
{
    private class ActiveQuest
    {
        public int DefinitionIndex;
        public int Progress;
        public bool Completed;
    }

    private const int QuestAmount = 3;
    private const int RewardTarget = 3;
    private const string ActiveIndexKey = "DailyQuest_ActiveIndex_";
    private const string ProgressKey = "DailyQuest_Progress_";
    private const string CompletedKey = "DailyQuest_Completed_";

    public GameObject ShopWindow;
    public CanvasGroup bgCanvasGroup;
    public Transform QuestParent;
    public GameObject QuestTemplate;
    public TMP_Text ResetLabel;

    [Header("Reward")]
    public Image RewardProgressBar;
    public TMP_Text RewardProgressLabel;
    public Button ClaimButton;
    public List<ArtifactData> DailyQuestArtifactRewards = new List<ArtifactData>();

    [Header("Quests")]
    public List<DailyQuestDefinition> QuestDefinitions = new List<DailyQuestDefinition>();

    public Vector3 startPosition;

    private List<ActiveQuest> activeQuests = new List<ActiveQuest>();
    private bool isInitialized = false;
    private float suppressHideUntilTime = 0f;

    public void Init()
    {
        if (ShopWindow != null)
            startPosition = ShopWindow.GetComponent<RectTransform>().anchoredPosition;

        if (QuestTemplate != null)
            QuestTemplate.SetActive(false);

        if (ClaimButton != null)
        {
            ClaimButton.onClick.RemoveAllListeners();
            ClaimButton.onClick.AddListener(ClaimArtifactReward);
        }

        LoadOrCreateDailyQuests();
        isInitialized = true;
        UpdateUI();
    }

    void Update()
    {
        if (ShopWindow == null || ShopWindow.activeSelf == false)
            return;

        UpdateResetLabel();
    }

    public void ShowWindow()
    {
        LoadOrCreateDailyQuests();
        UpdateUI();

        SoundManager.TryPlay(SoundType.WindowOpen);
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);

        bgCanvasGroup.gameObject.SetActive(true);
        bgCanvasGroup.alpha = 0;
        LeanTween.alphaCanvas(bgCanvasGroup, 1f, 0.25f).setEaseOutQuad();

        ShopWindow.SetActive(true);
        Vector2 targetPos = ShopWindow.GetComponent<RectTransform>().anchoredPosition;
        ShopWindow.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPos.x, -Screen.height);

        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack();
    }

    public void HideWindow()
    {
        if (Time.unscaledTime < suppressHideUntilTime)
            return;

        SoundManager.TryPlay(SoundType.WindowClose);
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);

        bgCanvasGroup.alpha = 1;
        LeanTween.alphaCanvas(bgCanvasGroup, 0f, 0.25f).setEaseInQuad();

        Vector2 hidePos = new Vector2(ShopWindow.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                ShopWindow.SetActive(false);
                ShopWindow.GetComponent<RectTransform>().anchoredPosition = startPosition;
                bgCanvasGroup.gameObject.SetActive(false);
            });
    }

    public void UpdateUI()
    {
        PopulateQuestItems(QuestParent, QuestTemplate);

        UpdateResetLabel();
        UpdateRewardUI();
    }

    public void PopulateQuestItems(Transform parent, GameObject template)
    {
        LoadOrCreateDailyQuests();

        if (parent == null || template == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            if (parent.GetChild(i).gameObject != template)
                Destroy(parent.GetChild(i).gameObject);
        }

        for (int i = 0; i < activeQuests.Count; i++)
        {
            DailyQuestDefinition definition = GetDefinition(activeQuests[i]);
            if (definition == null)
                continue;

            GameObject go = Instantiate(template, parent);
            go.SetActive(true);

            DailyQuestItem item = go.GetComponent<DailyQuestItem>();
            if (item != null)
                item.Init(GetQuestText(definition), activeQuests[i].Progress, definition.TargetAmount, activeQuests[i].Completed);
        }
    }

    public void UpdateRewardUI(Image rewardProgressBar, TMP_Text rewardProgressLabel, Button claimButton)
    {
        int progress = GameData.CompletedQuestsTowardsReward;

        if (rewardProgressLabel != null)
            rewardProgressLabel.text = progress + " / " + RewardTarget;

        if (rewardProgressBar != null)
            rewardProgressBar.fillAmount = GetRewardProgressFillAmount(progress);

        if (claimButton != null)
        {
            claimButton.transform.parent.gameObject.SetActive(progress >= RewardTarget && GameData.DailyQuestArtifactRewardIndex < DailyQuestArtifactRewards.Count);
            claimButton.gameObject.SetActive(progress >= RewardTarget && GameData.DailyQuestArtifactRewardIndex < DailyQuestArtifactRewards.Count);
        }
    }

    public void AddProgress(DailyQuestType type, int amount = 1)
    {
        if (amount <= 0)
            return;

        LoadOrCreateDailyQuests();

        bool changed = false;
        foreach (var quest in activeQuests)
        {
            DailyQuestDefinition definition = GetDefinition(quest);
            if (definition == null || definition.Type != type || quest.Completed)
                continue;

            quest.Progress += amount;
            CompleteQuestIfReady(quest, definition);
            changed = true;
        }

        if (changed)
            SaveAndRefresh();
    }

    public void SetProgressIfHigher(DailyQuestType type, int amount)
    {
        LoadOrCreateDailyQuests();

        bool changed = false;
        foreach (var quest in activeQuests)
        {
            DailyQuestDefinition definition = GetDefinition(quest);
            if (definition == null || definition.Type != type || quest.Completed || amount <= quest.Progress)
                continue;

            quest.Progress = amount;
            CompleteQuestIfReady(quest, definition);
            changed = true;
        }

        if (changed)
            SaveAndRefresh();
    }

    public void OnUnitPlayed(CardInstance card)
    {
        if (card == null || card.data == null)
            return;

        LoadOrCreateDailyQuests();

        bool changed = false;
        foreach (var quest in activeQuests)
        {
            DailyQuestDefinition definition = GetDefinition(quest);
            if (definition == null || quest.Completed)
                continue;

            if (definition.Type == DailyQuestType.PlayRaceUnits && card.data.race == definition.Race)
            {
                quest.Progress++;
                CompleteQuestIfReady(quest, definition);
                changed = true;
            }

            if (definition.Type == DailyQuestType.PlayClassUnits && card.data.cardClass == definition.Class)
            {
                quest.Progress++;
                CompleteQuestIfReady(quest, definition);
                changed = true;
            }
        }

        if (changed)
            SaveAndRefresh();
    }

    public bool IsArtifactAvailable(ArtifactData artifact)
    {
        if (IsDailyRaceGatedArtifact(artifact))
            return GetUnlockedRacesForArtifact(artifact).Count > 0;

        int rewardIndex = GetRewardIndexForArtifact(artifact);
        if (rewardIndex < 0)
            return true;

        return rewardIndex < GameData.DailyQuestArtifactRewardIndex;
    }

    public bool IsArtifactRaceAvailable(ArtifactData artifact, CardRace race)
    {
        if (IsDailyRaceGatedArtifact(artifact) == false)
            return IsArtifactAvailable(artifact);

        return GetUnlockedRacesForArtifact(artifact).Contains(race);
    }

    public bool IsDailyRaceGatedArtifact(ArtifactData artifact)
    {
        if (artifact == null || artifact.effect != ArtifactEffectType.RaceHasExtraDamage)
            return false;

        for (int i = 0; i < DailyQuestArtifactRewards.Count; i++)
        {
            ArtifactData rewardArtifact = DailyQuestArtifactRewards[i];
            if (MatchesArtifactTemplate(rewardArtifact, artifact))
                return true;
        }

        return false;
    }

    public List<CardRace> GetUnlockedRacesForArtifact(ArtifactData artifact)
    {
        List<CardRace> races = new List<CardRace>();
        if (artifact == null)
            return races;

        int rewardCount = Mathf.Min(GameData.DailyQuestArtifactRewardIndex, DailyQuestArtifactRewards.Count);
        for (int i = 0; i < rewardCount; i++)
        {
            ArtifactData rewardArtifact = DailyQuestArtifactRewards[i];
            if (MatchesArtifactTemplate(rewardArtifact, artifact) && races.Contains(rewardArtifact.RandomRace) == false)
            {
                races.Add(rewardArtifact.RandomRace);
            }
        }

        return races;
    }

    public void RollUnlockedRaceForArtifact(ArtifactData artifact)
    {
        if (IsDailyRaceGatedArtifact(artifact) == false)
            return;

        List<CardRace> races = GetUnlockedRacesForArtifact(artifact);
        if (races.Count == 0)
            return;

        artifact.RandomRace = races[UnityEngine.Random.Range(0, races.Count)];
    }

    public void ClaimArtifactReward()
    {
        if (GameData.CompletedQuestsTowardsReward < RewardTarget)
            return;

        if (GameData.DailyQuestArtifactRewardIndex >= DailyQuestArtifactRewards.Count)
            return;

        ArtifactData unlockedArtifact = GetDailyQuestRewardArtifact(GameData.DailyQuestArtifactRewardIndex);

        GameData.CompletedQuestsTowardsReward -= RewardTarget;
        GameData.DailyQuestArtifactRewardIndex++;
        PlayerPrefs.Save();

        UpdateRewardUI();

        if (InventoryOverviewManager.Instance != null)
            InventoryOverviewManager.Instance.MarkArtifactAsNew(unlockedArtifact);

        UnlockManager.Instance.ShowDailyQuestArtifactReward(unlockedArtifact);
    }

    public void DebugCompleteQuest(int index)
    {
        LoadOrCreateDailyQuests();
        if (index < 0 || index >= activeQuests.Count)
            return;

        DailyQuestDefinition definition = GetDefinition(activeQuests[index]);
        if (definition == null)
            return;

        activeQuests[index].Progress = definition.TargetAmount;
        CompleteQuestIfReady(activeQuests[index], definition);
        SaveAndRefresh();
    }

    public void DebugProgressActiveQuest(int index = 0)
    {
        LoadOrCreateDailyQuests();
        if (index < 0 || index >= activeQuests.Count)
            return;

        DailyQuestDefinition definition = GetDefinition(activeQuests[index]);
        if (definition == null || activeQuests[index].Completed)
            return;

        int amount = Mathf.Max(1, Mathf.CeilToInt(definition.TargetAmount / 4f));
        activeQuests[index].Progress += amount;
        CompleteQuestIfReady(activeQuests[index], definition);
        SaveAndRefresh();
    }

    public void DebugResetDailyQuests()
    {
        GenerateNewDailyQuests();
        SaveAndRefresh();
    }

    public void SuppressHideForDebugClick()
    {
        suppressHideUntilTime = Time.unscaledTime + 0.35f;
    }

    public void DebugAddRewardProgress()
    {
        GameData.CompletedQuestsTowardsReward++;
        PlayerPrefs.Save();
        UpdateRewardUI();
    }

    public void DebugForceResetTimer()
    {
        GameData.DailyQuestNextResetUtcTicks = 0;
        LoadOrCreateDailyQuests();
        UpdateUI();
    }

    private void LoadOrCreateDailyQuests()
    {
        long nowTicks = DateTime.UtcNow.Ticks;
        if (GameData.DailyQuestNextResetUtcTicks <= 0 || nowTicks >= GameData.DailyQuestNextResetUtcTicks)
        {
            GenerateNewDailyQuests();
            return;
        }

        activeQuests.Clear();
        for (int i = 0; i < QuestAmount; i++)
        {
            int definitionIndex = PlayerPrefs.GetInt(ActiveIndexKey + i, -1);
            if (definitionIndex < 0 || definitionIndex >= QuestDefinitions.Count)
            {
                GenerateNewDailyQuests();
                return;
            }

            activeQuests.Add(new ActiveQuest
            {
                DefinitionIndex = definitionIndex,
                Progress = PlayerPrefs.GetInt(ProgressKey + i, 0),
                Completed = PlayerPrefs.GetInt(CompletedKey + i, 0) == 1
            });
        }
    }

    private void GenerateNewDailyQuests()
    {
        activeQuests.Clear();

        List<int> indexes = new List<int>();
        for (int i = 0; i < QuestDefinitions.Count; i++)
        {
            if (QuestDefinitions[i] != null && QuestDefinitions[i].TargetAmount > 0)
                indexes.Add(i);
        }

        indexes.Shuffle();
        int count = Mathf.Min(QuestAmount, indexes.Count);
        for (int i = 0; i < count; i++)
        {
            activeQuests.Add(new ActiveQuest
            {
                DefinitionIndex = indexes[i],
                Progress = 0,
                Completed = false
            });
        }

        GameData.DailyQuestNextResetUtcTicks = DateTime.UtcNow.AddHours(24).Ticks;
        SaveActiveQuests();
    }

    private void SaveAndRefresh()
    {
        SaveActiveQuests();
        PlayerPrefs.Save();

        if (isInitialized)
            UpdateUI();
    }

    private void SaveActiveQuests()
    {
        for (int i = 0; i < QuestAmount; i++)
        {
            if (i < activeQuests.Count)
            {
                PlayerPrefs.SetInt(ActiveIndexKey + i, activeQuests[i].DefinitionIndex);
                PlayerPrefs.SetInt(ProgressKey + i, activeQuests[i].Progress);
                PlayerPrefs.SetInt(CompletedKey + i, activeQuests[i].Completed ? 1 : 0);
            }
            else
            {
                PlayerPrefs.SetInt(ActiveIndexKey + i, -1);
                PlayerPrefs.SetInt(ProgressKey + i, 0);
                PlayerPrefs.SetInt(CompletedKey + i, 0);
            }
        }
    }

    private void CompleteQuestIfReady(ActiveQuest quest, DailyQuestDefinition definition)
    {
        if (quest.Completed || quest.Progress < definition.TargetAmount)
            return;

        quest.Progress = definition.TargetAmount;
        quest.Completed = true;
        GameData.CompletedQuestsTowardsReward++;
    }

    private DailyQuestDefinition GetDefinition(ActiveQuest quest)
    {
        if (quest == null || quest.DefinitionIndex < 0 || quest.DefinitionIndex >= QuestDefinitions.Count)
            return null;

        return QuestDefinitions[quest.DefinitionIndex];
    }

    private string GetQuestText(DailyQuestDefinition definition)
    {
        if (definition == null)
            return "";

        if (!string.IsNullOrEmpty(definition.Text))
            return definition.Text;

        switch (definition.Type)
        {
            case DailyQuestType.PlayRaceUnits:
                return "Play " + definition.TargetAmount + " " + definition.Race + " units";
            case DailyQuestType.PlayClassUnits:
                return "Play " + definition.TargetAmount + " " + definition.Class + " units";
            case DailyQuestType.ReachLevel:
                return "Reach level " + definition.TargetAmount;
            case DailyQuestType.SingleAttackDamage:
                return "Deal " + definition.TargetAmount + " damage in one attack";
            default:
                return definition.Type.ToString() + " " + definition.TargetAmount;
        }
    }

    private void UpdateResetLabel()
    {
        if (ResetLabel == null)
            return;

        TimeSpan remaining = new TimeSpan(Math.Max(0, GameData.DailyQuestNextResetUtcTicks - DateTime.UtcNow.Ticks));
        ResetLabel.text = "New quests in " + Mathf.FloorToInt((float)remaining.TotalHours) + "h " + remaining.Minutes + "m";
    }

    private void UpdateRewardUI()
    {
        UpdateRewardUI(RewardProgressBar, RewardProgressLabel, ClaimButton);
    }

    private float GetRewardProgressFillAmount(int progress)
    {
        return Mathf.Clamp01((float)progress / RewardTarget);
    }

    private int GetRewardIndexForArtifact(ArtifactData artifact)
    {
        if (artifact == null)
            return -1;

        for (int i = 0; i < DailyQuestArtifactRewards.Count; i++)
        {
            ArtifactData rewardArtifact = DailyQuestArtifactRewards[i];
            if (MatchesArtifactTemplate(rewardArtifact, artifact))
            {
                return i;
            }
        }

        return -1;
    }

    private ArtifactData GetDailyQuestRewardArtifact(int rewardIndex)
    {
        if (rewardIndex < 0 || rewardIndex >= DailyQuestArtifactRewards.Count)
            return null;

        ArtifactData rewardArtifact = DailyQuestArtifactRewards[rewardIndex];
        if (rewardArtifact == null)
            return null;

        ArtifactData matchingArtifact = null;
        if (CardContainer.Instance != null && CardContainer.Instance.ArtifactDataList != null)
        {
            matchingArtifact = CardContainer.Instance.ArtifactDataList
                .FirstOrDefault(artifact => MatchesArtifactTemplate(rewardArtifact, artifact));
        }

        ArtifactData source = matchingArtifact != null ? matchingArtifact : rewardArtifact;
        return new ArtifactData
        {
            name = source.name,
            UnlockRun = source.UnlockRun,
            description = source.description,
            sprite_icon = source.sprite_icon,
            effect = source.effect,
            value = source.value,
            rarity = source.rarity,
            RandomRace = rewardArtifact.RandomRace
        };
    }

    private bool MatchesArtifactTemplate(ArtifactData rewardArtifact, ArtifactData artifact)
    {
        return rewardArtifact != null &&
               artifact != null &&
               rewardArtifact.effect == artifact.effect &&
               rewardArtifact.name == artifact.name &&
               rewardArtifact.value == artifact.value;
    }
}
