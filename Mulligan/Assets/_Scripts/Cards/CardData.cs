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
public enum PotionEffectType
{
    CritBonus, // Done
    DamageBonus, // Done
    RandomDamage, // Done
    FacelessSingle, // <-- TODO
    FacelessMultiple, // <-- TODO
    DrawExtra, // <-- SKIP
    SuicideBoost, // Done
    DisableDebuff,  // <-- TODO
    HealHero, // Done
    BoostAndLoseHP, // Done
    RetriggerUpgrades // <-- TODO
}
public enum UpgradeEffect
{
    Enchantment_Crit, // Done
    Enchantment_PlusOneClass,
    Enchantment_Changeling,
    Enchantment_Retrigger,// Done
    Enchantment_LifeSteal,// Done
    Charms_Gold,// Done
    Charms_Potion,
    Charms_Heal,
    RankUpgrade_Normal // Done
}
public enum UpgradeType
{
    Enchantment,
    Charms,
    RankUpgrade
}
public enum CardTypeEnum
{
    UnitCard,
    UnitSelectCard,
    ArtifactCard,
    UnitPackCard,
    UnitUpgradeCard,
    PotionCard,
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
[System.Serializable]
public class PotionCardData
{
    public string name;
    public PotionEffectType effectType;
    [TextArea] public string description;
    public Sprite icon;
    public int value; // if needed (e.g. +2 crit, +20 dmg)
}
[System.Serializable]
public class UpgradeCardData
{
    public string name;
    [TextArea] public string description;
    public Sprite icon;

    public UpgradeEffect effect;
    public UpgradeType type;
    public int value; // if needed (e.g. +2 crit, +20 dmg)
}
