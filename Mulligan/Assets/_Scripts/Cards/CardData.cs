using System.Collections.Generic;
using UnityEngine;

public enum CardRace
{
    Human,
    Elf,
    Orc,
    Undead,
    // Add more as needed
}

public enum CardClass
{
    Warrior,
    Mage,
    Rogue,
    Archer,
    Cleric,
    // Add more as needed
}



[System.Serializable]
public class CardData
{
    public string cardName;
    public int damage;
    public CardRace race;
    public CardClass cardClass;
    public Sprite portrait;
    public List<int> RankUpgrades;
}

[System.Serializable]
public class RaceData
{
    public CardRace theRace;
    public Sprite theSprite;
    public Color theColor;
}
[System.Serializable]
public class ClassData
{
    public CardClass theClass;
    public Sprite theSprite;
    public Color theColor;
}