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
        public int StatingGold = 0;
        public int GoldGainPerLevel = 5;
        public float GoldInflation = 1.2f;
        public int StatingHealth = 0;
        public int EnemyBaseHealth = 0;
        public int EnemyBaseDamage = 0;
        public float GrowthRate;

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
        StatingHealth = aData.StatingHealth;
        EnemyBaseHealth = aData.EnemyBaseHealth;
        EnemyBaseDamage = aData.EnemyBaseDamage;
        GrowthRate= aData.GrowthRate;
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
    public int StatingHealth = 0;
    public int EnemyBaseHealth = 0;
    public int EnemyBaseDamage = 0;
    public float GrowthRate;
}
