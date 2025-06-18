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


}

