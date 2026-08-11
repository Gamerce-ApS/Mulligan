using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class CardContainer : Singleton<CardContainer>
{
    private CardData[] CardsDataList = null;
    private RaceData[] RaceDataList = null;
    private ClassData[] ClassDataList = null;
    public EnemyData[] EnemyDataList = null;
    public BossData[] BossDataList = null;
    public SkipRewardData[] SkipDataList = null;
    public RuneData[] RuneDataList = null;
    public PotionCardData[] PotionDataList = null;
    public HeroData[] HeroDataList = null;
    public ArtifactData[] ArtifactDataList = null;

    public int StatingGold = 0;
    public int GoldGainPerLevel = 5;
    public float GoldInflation = 1.2f;
    public int EnemyBaseHealth = 0;
    public int EnemyBaseDamage = 0;
    public float GrowthRate = 0.1f;
    public float GrowthRateEXP = 0.1f;
    public float GrowthRateDMG = 0.1f;
    public float GrowthRateDMGEXP = 0.1f;
    public int ExperiencePerKill;
    public int ExperienceToLevelUp;
    public int HealthGainPerLevel;
    public int[] Rarity;

    public List<CardInstance> CurrentDeck = new List<CardInstance>();
    public List<CardInstance> DiscardDeck = new List<CardInstance>();
    public List<CardInstance> TutorialDeck = new List<CardInstance>();

    public List<EnemyData> myEnemiesList;
    public List<BossData> myBossList;


    public void Init()
    {
        CardsDataList = CardLoader.LoadAllCards().allCards;
        RaceDataList = CardLoader.LoadAllCards().raceData;
        ClassDataList = CardLoader.LoadAllCards().classData;
        EnemyDataList = CardLoader.LoadAllCards().allEnemies;
        BossDataList = CardLoader.LoadAllCards().allBosses;
        PotionDataList = CardLoader.LoadAllCards().allPotions;
        HeroDataList = CardLoader.LoadAllCards().allHeroes;
        StatingGold = CardLoader.LoadAllCards().StatingGold;
        GoldGainPerLevel = CardLoader.LoadAllCards().GoldGainPerLevel;
        GoldInflation = CardLoader.LoadAllCards().GoldInflation;
        EnemyBaseHealth = CardLoader.LoadAllCards().EnemyBaseHealth;
        EnemyBaseDamage = CardLoader.LoadAllCards().EnemyBaseDamage;
        GrowthRate = CardLoader.LoadAllCards().GrowthRate;
        GrowthRateEXP = CardLoader.LoadAllCards().GrowthRateEXP;
        GrowthRateDMG = CardLoader.LoadAllCards().GrowthRateDMG;
        GrowthRateDMGEXP = CardLoader.LoadAllCards().GrowthRateDMGEXP;
        SkipDataList = CardLoader.LoadAllCards().allSkipeRewards;
        RuneDataList = CardLoader.LoadAllCards().allRunes;
        ArtifactDataList = CardLoader.LoadAllCards().allArtifacts;

        ExperiencePerKill = CardLoader.LoadAllCards().ExperiencePerKill;
        ExperienceToLevelUp = CardLoader.LoadAllCards().ExperienceToLevelUp;
        HealthGainPerLevel = CardLoader.LoadAllCards().HealthGainPerLevel;
        Rarity = CardLoader.LoadAllCards().Rarity;
        CurrentDeck.Clear();
        List<CardData> startingCards = GetUnlockedCards();
        if (startingCards.Count == 0)
        {
            Debug.LogWarning("No unlocked units available. Falling back to all units.");
            startingCards = CardsDataList.ToList();
        }

        foreach (var data in startingCards)
        {
            CurrentDeck.Add(new CardInstance(data));
            //CurrentDeck.Add(new CardInstance(data));
            //CurrentDeck.Add(new CardInstance(data));
        }

        CurrentDeck.Shuffle();
        foreach (var a in ArtifactDataList)
        {
            a.RandomRace = (CardRace)Random.Range(0, (int)CardRace.END);
        }

        string json = JsonUtility.ToJson(CardLoader.LoadAllCards(), true);

        // // Update Data from Online
        // GUIUtility.systemCopyBuffer = json;
        // CardDataObject asset = AssetDatabase.LoadAssetAtPath<CardDataObject>("Assets/Resources/CardContainer.asset");
        // asset.LoadFromJson(json);
        // #if UNITY_EDITOR
        // EditorUtility.SetDirty(asset);
        // AssetDatabase.SaveAssets();
        // #endif

        for (int i = 0; i < 30; i++)
        {
            List<EnemyData> d = EnemyDataList.ToList();
            TutorialController.Instance.myEnemiesList.AddRange(d);
            d.Shuffle();
            myEnemiesList.AddRange(d);
        }
        for (int i = 0; i < 30; i++)
        {
            List<BossData> d = BossDataList.ToList();
            TutorialController.Instance.myBossList.AddRange(d);
            d.Shuffle();


            myBossList.AddRange(d);
        }


        for (int i = 0; i < 5; i++)
        {
            if (myBossList[0].abilities.Contains(BossAbilityEnum.Evasion) || myBossList[0].baseDamage >= 999)
            {
                myBossList.RemoveAt(0);
            }
        }



        //  myBossList[0] = BossDataList[3];
        // for(int i = 0; i < 100;i++)
        //     myEnemiesList.Add(GetRandomEnemy());
        // for(int i = 0; i < 100;i++)
        //     myBossList.Add(GetRandomBoss());


        AddTutorialDeck();

    }
    public void AddTutorialDeck()
    {
        TutorialDeck.Clear();
        AddTutorialCard(CardRace.Orc, CardClass.Warrior);
        AddTutorialCard(CardRace.Elf, CardClass.Mage);
        AddTutorialCard(CardRace.Orc, CardClass.Cleric);
        AddTutorialCard(CardRace.Orc, CardClass.Mage);
        AddTutorialCard(CardRace.Orc, CardClass.Archer);
        AddTutorialCard(CardRace.Undead, CardClass.Warrior);
        AddTutorialCard(CardRace.Human, CardClass.Peasant);
        AddTutorialCard(CardRace.Human, CardClass.Cleric);

        AddTutorialCard(CardRace.Orc, CardClass.Mage);
        AddTutorialCard(CardRace.Troll, CardClass.Warrior);
        AddTutorialCard(CardRace.Undead, CardClass.Cleric);
        AddTutorialCard(CardRace.Elf, CardClass.Archer);

        AddTutorialCard(CardRace.Human, CardClass.Warrior);

        AddTutorialCard(CardRace.Human, CardClass.Warrior);
        AddTutorialCard(CardRace.Undead, CardClass.Bard);
        AddTutorialCard(CardRace.Dwarf, CardClass.Mage);
        AddTutorialCard(CardRace.Orc, CardClass.Warrior);
        AddTutorialCard(CardRace.Human, CardClass.Warrior);
        AddTutorialCard(CardRace.Troll, CardClass.Warrior);
        AddTutorialCard(CardRace.Human, CardClass.Cleric);
        AddTutorialCard(CardRace.Dwarf, CardClass.Cleric);
        foreach (var card in CurrentDeck)
        {
            if (card.data != null)
                TutorialDeck.Add(new CardInstance(card.data));
        }
    }
    private void AddTutorialCard(CardRace race, CardClass cardClass)
    {
        CardInstance card = GetCardFromRace(race, cardClass);
        if (card != null && card.data != null)
            TutorialDeck.Add(card);
    }
    public CardInstance GetCardFromRace(CardRace aRace, CardClass aClass)
    {
        var candidates = GetUnlockedCards()
        .Where(c => c.race == aRace && c.cardClass == aClass)
        .ToList();

        if (TutorialController.Instance.HasRunTutorial() == false && candidates.Count == 0)
        {
            candidates = CardsDataList
            .Where(c => c.race == aRace && c.cardClass == aClass)
            .ToList();
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("No unlocked cards found for " + aRace + " " + aClass);
            return null;
        }

        var candidate = candidates.GetRandom();
        if (candidate == null)
            return null;

        return new CardInstance(candidate);
    }
    public bool IsContentUnlocked(int unlockRun)
    {
        return unlockRun <= GameData.UnlockProgressForThisRun;
    }
    public List<CardData> GetUnlockedCards()
    {
        if (CardsDataList == null)
            return new List<CardData>();

        return CardsDataList.Where(c => c != null && IsContentUnlocked(c.UnlockRun)).ToList();
    }
    public List<ArtifactData> GetUnlockedArtifacts()
    {
        if (ArtifactDataList == null)
            return new List<ArtifactData>();

        return ArtifactDataList.Where(c => c != null && IsContentUnlocked(c.UnlockRun)).ToList();
    }
    public List<PotionCardData> GetUnlockedPotions()
    {
        if (PotionDataList == null)
            return new List<PotionCardData>();

        return PotionDataList.Where(c => c != null && IsContentUnlocked(c.UnlockRun)).ToList();
    }
    public List<RuneData> GetUnlockedRunes()
    {
        if (RuneDataList == null)
            return new List<RuneData>();

        return RuneDataList.Where(c => c != null && IsContentUnlocked(c.UnlockRun)).ToList();
    }
    public List<UpgradeCardData> GetUnlockedUpgrades()
    {
        CardDataObject dataList = CardLoader.LoadAllCards();
        if (dataList == null || dataList.allUpgradeCards == null)
            return new List<UpgradeCardData>();

        return dataList.allUpgradeCards.Where(c => c != null && IsContentUnlocked(c.UnlockRun)).ToList();
    }
    public void CompleteBoss()
    {
        myEnemiesList.RemoveAt(0);
        myEnemiesList.RemoveAt(0);
        myEnemiesList.RemoveAt(0);
        myBossList.RemoveAt(0);
    }
    // Update is called once per frame
    void Update()
    {

    }
    public EnemyData GetRandomEnemy()
    {
        return EnemyDataList[Random.Range(0, EnemyDataList.Length)]; ;
    }
    public BossData GetRandomBoss()
    {
        return BossDataList[Random.Range(0, BossDataList.Length)]; ;
    }
    public CardData GetRandomCardData()
    {
        List<CardData> available = GetUnlockedCards();
        if (available.Count == 0)
        {
            Debug.LogWarning("No unlocked cards available.");
            return null;
        }

        return available[Random.Range(0, available.Count)];
    }
    public CardInstance GetRandomCardFromDecks()
    {
        List<CardInstance> allC = new List<CardInstance>();
        allC.AddRange(CurrentDeck);
        allC.AddRange(DiscardDeck);
        allC.AddRange(HandManager.Instance.CurrentHand);
        allC.RemoveAll(c => c == null || c.data == null);
        if (allC.Count == 0)
        {
            Debug.LogWarning("No cards available from deck, discard, or hand.");
            return null;
        }
        return allC[Random.Range(0, allC.Count)];

    }
    public CardInstance DrawCard()
    {


        if (TutorialController.Instance.HasRunTutorial())
        {
            if (CurrentDeck.Count <= 0)
            {
                Shuffel();
            }
            if (CurrentDeck.Count <= 0)
            {
                Debug.LogWarning("No cards available to draw.");
                return null;
            }
            CardInstance ins = CurrentDeck[0];
            CurrentDeck.RemoveAt(0);
            return ins;

        }
        else
        {
            if (TutorialDeck.Count <= 0)
            {
                AddTutorialDeck();
                Shuffel();
            }
            if (TutorialDeck.Count <= 0)
            {
                Debug.LogWarning("No tutorial cards available to draw.");
                return null;
            }
            CardInstance ins = TutorialDeck[0];
            TutorialDeck.RemoveAt(0);
            return ins;
        }

    }
    public void DiscardCard(CardInstance aCard)
    {
        DiscardDeck.Add(aCard);
    }
    public void Shuffel()
    {
        foreach (var a in DiscardDeck)
            CurrentDeck.Add(a);
        DiscardDeck.Clear();

        //foreach (var a in HandManager.Instance.CurrentHand)
        //    CurrentDeck.Add(a);
        //HandManager.Instance.CurrentHand.Clear();

        CurrentDeck.Shuffle();
    }
    public Sprite GetSpriteForRace(CardRace aRace)
    {
        foreach (var r in RaceDataList)
        {
            if (r.theRace == aRace)
            {
                return Resources.Load<Sprite>("" + r.sprite_theSprite);
            }
            //return r.theSprite;
        }
        return null;
    }
    public Sprite GetSpriteForClass(CardClass aClass)
    {
        foreach (var r in ClassDataList)
        {
            if (r.theClass == aClass)
            {
                return Resources.Load<Sprite>("" + r.sprite_theSprite);
            }
            //return r.theSprite;
        }
        return null;
    }
    public Color GetColorForRace(CardRace aRace)
    {
        foreach (var r in RaceDataList)
        {
            if (r.theRace == aRace)
            {
                Color col = r.theColor;
                col.a = 1;
                return col;
            }

        }
        return Color.white;
    }
    public RarityType GetRandomRarity()
    {
        int totalWeight = 0;

        for (int i = 0; i < Rarity.Length; i++)
            totalWeight += Rarity[i];

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < Rarity.Length; i++)
        {
            cumulative += Rarity[i];

            if (roll < cumulative)
                return (RarityType)i;
        }

        return RarityType.Common;
    }
    //public void FetchLiveEnemies()
    //{
    //    StartCoroutine(GoogleSheetLoader.LoadCSV("https://docs.google.com/spreadsheets/d/e/2PACX-1vSKJVabfOQukbUoDYA8NCMwUR3b-6jiWQ9TWL31DJc1MRxfHMQCtFyEkr_-NFPNlfsvDwgkQyKllT-q/pub?output=csv", ParseData, (list) =>
    //    {
    //        cardDataObject.name = list.ToArray();
    //        Debug.Log("Live enemy data updated!");
    //    }));
    //}

    //EnemyData ParseEnemy(string[] row)
    //{
    //    return new EnemyData
    //    {
    //        name = row[0],
    //        baseHP = int.Parse(row[1]),
    //        baseDamage = int.Parse(row[2]),
    //        //theSprite = Resources.Load<Sprite>("Enemies/" + row[3])
    //    };
    //}


}
