using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "CardContainer", menuName = "Card Game/Card Container")]
    public class CardDataObject : ScriptableObject
    {
        public CardData[] allCards;
        public RaceData[] raceData;
        public ClassData[] classData;
        public ArtifactData[] allArtifacts;
        public PotionCardData[] allPotions;
        public UpgradeCardData[] allUpgradeCards;
        public BossData[] allBosses;
        public EnemyData[] allEnemies;
        public SkipRewardData[] allSkipeRewards;
        public RuneData[] allRunes;
        public HeroData[] allHeroes;
        public int StatingGold = 0;
        public int GoldGainPerLevel = 5;
        public float GoldInflation = 1.2f;
        public int EnemyBaseHealth = 0;
        public int EnemyBaseDamage = 0;
        public float GrowthRate;
        public int[] Rarity;

        public int ExperiencePerKill;
        public int ExperienceToLevelUp;
        public int HealthGainPerLevel;


    public CardDataObject(CardDataExportWrapper aData)
    {
        allCards= aData.allCards;
        raceData = aData.raceData;
        classData = aData.classData;
        allArtifacts = aData.allArtifacts;
        allPotions = aData.allPotions;
        allUpgradeCards = aData.allUpgradeCards;
        allBosses = aData.allBosses;
        allEnemies = aData.allEnemies;
        StatingGold = aData.StatingGold;
        GoldGainPerLevel = aData.GoldGainPerLevel;
        GoldInflation = aData.GoldInflation;
        EnemyBaseHealth = aData.EnemyBaseHealth;
        EnemyBaseDamage = aData.EnemyBaseDamage;
        GrowthRate= aData.GrowthRate;
        allSkipeRewards = aData.allSkipeRewards;
        allRunes = aData.allRunes;
        allHeroes = aData.allHeroes;
        ExperiencePerKill = aData.ExperiencePerKill;
        ExperienceToLevelUp = aData.ExperienceToLevelUp;
        HealthGainPerLevel = aData.HealthGainPerLevel;
        Rarity = aData.Rarity;
    }

    public void LoadFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("LoadFromJson failed: json is null or empty.");
            return;
        }

        CardDataExportWrapper data = JsonUtility.FromJson<CardDataExportWrapper>(json);

        if (data == null)
        {
            Debug.LogError("LoadFromJson failed: could not parse json.");
            return;
        }

        ApplyData(data);
    }

    public void ApplyData(CardDataExportWrapper data)
    {
        if (data == null)
        {
            Debug.LogError("ApplyData failed: data is null.");
            return;
        }

        allCards = data.allCards;
        raceData = data.raceData;
        classData = data.classData;
        allArtifacts = data.allArtifacts;
        allPotions = data.allPotions;
        allUpgradeCards = data.allUpgradeCards;
        allBosses = data.allBosses;
        allEnemies = data.allEnemies;
        allSkipeRewards = data.allSkipeRewards;
        allRunes = data.allRunes;
        allHeroes = data.allHeroes;

        StatingGold = data.StatingGold;
        GoldGainPerLevel = data.GoldGainPerLevel;
        GoldInflation = data.GoldInflation;
        EnemyBaseHealth = data.EnemyBaseHealth;
        EnemyBaseDamage = data.EnemyBaseDamage;
        GrowthRate = data.GrowthRate;
        ExperiencePerKill = data.ExperiencePerKill;
        ExperienceToLevelUp = data.ExperienceToLevelUp;
        HealthGainPerLevel = data.HealthGainPerLevel;
        Rarity = data.Rarity;

        Debug.Log("CardDataObject loaded from json.");
    }

    public static CardDataExportWrapper WrapperFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("WrapperFromJson failed: json is null or empty.");
            return null;
        }

        return JsonUtility.FromJson<CardDataExportWrapper>(json);
    }
}
[System.Serializable]
public class CardDataExportWrapper
{
    public CardData[] allCards;
    public RaceData[] raceData;
    public ClassData[] classData;
    public ArtifactData[] allArtifacts;
    public PotionCardData[] allPotions;
    public UpgradeCardData[] allUpgradeCards;
    public BossData[] allBosses;
    public EnemyData[] allEnemies;
    public int StatingGold = 0;
    public int GoldGainPerLevel = 5;
    public float GoldInflation = 1.2f;
    public int EnemyBaseHealth = 0;
    public int EnemyBaseDamage = 0;
    public float GrowthRate;
    public SkipRewardData[] allSkipeRewards;
    public RuneData[] allRunes;
    public HeroData[] allHeroes;
    public int ExperiencePerKill;
    public int ExperienceToLevelUp;
    public int HealthGainPerLevel;
    public int[] Rarity;


}
