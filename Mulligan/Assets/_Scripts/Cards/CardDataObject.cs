using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "CardContainer", menuName = "Card Game/Card Container")]
    public class CardDataObject : ScriptableObject
    {
        public CardData[] allCards;
        public RaceData[] raceData;
        public ClassData[] classData;

    
}

