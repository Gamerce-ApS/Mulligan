using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardContainer : Singleton<CardContainer>
{
    private CardData[] CardsDataList = null;
    private RaceData[] RaceDataList = null;
    private ClassData[] ClassDataList = null;

    
    public List<CardInstance> CurrentDeck = new List<CardInstance>();
    public List<CardInstance> DiscardDeck = new List<CardInstance>();

    public void Init()
    {
        CardsDataList = CardLoader.LoadAllCards().allCards;
        RaceDataList = CardLoader.LoadAllCards().raceData;
        ClassDataList = CardLoader.LoadAllCards().classData;
        CurrentDeck.Clear();
        foreach (var data in CardsDataList)
        {
            CurrentDeck.Add(new CardInstance(data));
            //CurrentDeck.Add(new CardInstance(data));
            //CurrentDeck.Add(new CardInstance(data));
        }

        CurrentDeck.Shuffle();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public CardData GetRandomCardData()
    {
        return CardsDataList[Random.Range(0, CardsDataList.Length - 1)]; ;
    }
    public CardInstance DrawCard()
    {
        if(CurrentDeck.Count<=0)
        {
            Shuffel();
        }
        CardInstance ins = CurrentDeck[0];
        CurrentDeck.RemoveAt(0);
        return ins;
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
        foreach(var r in RaceDataList)
        {
            if (r.theRace == aRace)
                return r.theSprite;
        }
        return null;
    }
    public Sprite GetSpriteForClass(CardClass aClass)
    {
        foreach (var r in ClassDataList)
        {
            if (r.theClass == aClass)
                return r.theSprite;
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


}
