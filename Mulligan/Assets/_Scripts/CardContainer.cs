using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        SkipDataList = CardLoader.LoadAllCards().allSkipeRewards;
        RuneDataList = CardLoader.LoadAllCards().allRunes;
        ArtifactDataList = CardLoader.LoadAllCards().allArtifacts;

        ExperiencePerKill = CardLoader.LoadAllCards().ExperiencePerKill;
        ExperienceToLevelUp = CardLoader.LoadAllCards().ExperienceToLevelUp;
        HealthGainPerLevel = CardLoader.LoadAllCards().HealthGainPerLevel;
        Rarity = CardLoader.LoadAllCards().Rarity;
        CurrentDeck.Clear();
        foreach (var data in CardsDataList)
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
        GUIUtility.systemCopyBuffer = json;

        for (int i = 0; i < 20; i++)
        {
            List<EnemyData> d = EnemyDataList.ToList();
            d.Shuffle();
            myEnemiesList.AddRange(d);
        }
        for (int i = 0; i < 20; i++)
        {
            List<BossData> d = BossDataList.ToList();
            d.Shuffle();
            myBossList.AddRange(d);
        }
        // for(int i = 0; i < 100;i++)
        //     myEnemiesList.Add(GetRandomEnemy());
        // for(int i = 0; i < 100;i++)
        //     myBossList.Add(GetRandomBoss());


        TutorialDeck.Clear();
        TutorialDeck.Add(GetCardFromRace(CardRace.Orc,CardClass.Warrior));
        TutorialDeck.Add(GetCardFromRace(CardRace.Elf,CardClass.Warrior));
        TutorialDeck.Add(GetCardFromRace(CardRace.Orc,CardClass.Cleric));
        TutorialDeck.Add(GetCardFromRace(CardRace.Orc,CardClass.Mage));
        TutorialDeck.Add(GetCardFromRace(CardRace.Orc,CardClass.Archer));
        TutorialDeck.Add(GetCardFromRace(CardRace.Undead,CardClass.Warrior));
        TutorialDeck.Add(GetCardFromRace(CardRace.Human,CardClass.Warrior));
        TutorialDeck.Add(GetCardFromRace(CardRace.Human,CardClass.Cleric));
       
        TutorialDeck.Add(GetCardFromRace(CardRace.Orc,CardClass.Mage));
        TutorialDeck.Add(GetCardFromRace(CardRace.Human,CardClass.Warrior));
        TutorialDeck.Add(GetCardFromRace(CardRace.Undead,CardClass.Cleric));
        TutorialDeck.Add(GetCardFromRace(CardRace.Elf,CardClass.Archer));

        TutorialDeck.Add(GetCardFromRace(CardRace.Human,CardClass.Warrior));

        TutorialDeck.Add(GetCardFromRace(CardRace.Human,CardClass.Warrior));
                TutorialDeck.Add(GetCardFromRace(CardRace.Human,CardClass.Warrior));
                TutorialDeck.Add(GetCardFromRace(CardRace.Human,CardClass.Warrior));
                TutorialDeck.Add(GetCardFromRace(CardRace.Human,CardClass.Warrior));
                TutorialDeck.Add(GetCardFromRace(CardRace.Human,CardClass.Warrior));
                TutorialDeck.Add(GetCardFromRace(CardRace.Human,CardClass.Warrior));
        TutorialDeck.Add(GetCardFromRace(CardRace.Human,CardClass.Cleric));
                TutorialDeck.Add(GetCardFromRace(CardRace.Human,CardClass.Cleric));
        TutorialDeck.AddRange(CurrentDeck);

    }
    public CardInstance GetCardFromRace(CardRace aRace,CardClass aClass)
    {
        var candidates = CardsDataList
        .Where(c => c.race == aRace && c.cardClass == aClass)
        .ToList().GetRandom();
        return new CardInstance(candidates);
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
        return CardsDataList[Random.Range(0, CardsDataList.Length)]; ;
    }
    public CardInstance GetRandomCardFromDecks()
    {
        List<CardInstance> allC = new List<CardInstance>();
        allC.AddRange(CurrentDeck);
        allC.AddRange(DiscardDeck);
        allC.AddRange(HandManager.Instance.CurrentHand);
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
            CardInstance ins = CurrentDeck[0];
            CurrentDeck.RemoveAt(0);
            return ins;

        }
        else
        {
            if (TutorialDeck.Count <= 0)
            {
                Shuffel();
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
