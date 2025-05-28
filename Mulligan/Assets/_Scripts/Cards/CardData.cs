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
public enum ArtifactEffectType
{
    AddCritFlat, // Done
    AddDamageFlat, // Done
    CritPerUpgradedUnit, // Needs testing
    CritPerSkippedLevel,
    DamagePerGold, // Needs testing
    CritPerPotionUsed,
    AddReroll, // Done
    AddArmySize,
    AttackPerLevel, // Done
    RankRandomUnit, // Done
    DoublePotionEffects,
    CopyRightArtifact,
    RetriggerAttacks,
    GoldOnLose,
    HealAfterLevel, // Done
    AddMaxHP, // Done
    DoubleSynergies
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
[System.Serializable]
public class ArtifactData
{
    public string name;
    [TextArea] public string description;
    public Sprite icon;

    public ArtifactEffectType effect;
    public int value; // if needed (e.g. +2 crit, +20 dmg)
}
