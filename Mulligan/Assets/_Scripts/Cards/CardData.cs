using System.Collections.Generic;
using Unity.VisualScripting;
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
    DoubleSynergies,

    DestroyUnitInHand, // Done
    DodgeEnemyAttack, // Done
    RaceHasExtraDamage, 
    GainGoldAfterLevel, // Done
    GetPotion, // Done
    AttackingMagesPlusDamage,
    BardInHandAttackingUnitsPlusDamage,
    ProcHPinDamage
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
public enum SkipRewardType
{
    DoubleGold,
    RandomPotions,
    DisableBossDebuff,
    UncommonArtifact,
    RareArtifact,
    ArmoryUpgrade,
    IncreaseMaxHP,
    FullHeal,
    MarketFreeNextRound,
    AddRuneToHero,
    ExtraAttacksNextRound
}
public enum UpgradeType
{
    Enchantment,
    Charms,
    RankUpgrade
}
// public enum RuneRarity { Normal, Rare }
[System.Serializable]
public class RuneData
{
    public string name;
    public RuneType type;
    public RarityType rarity;
    public string description;
    public string GetRarityText()
    {
       RarityType r =  (RarityType)rarity;
        return "\n\n<color=#"+UIManager.Instance.GetTextColor((int)rarity).ToHexString()+">"+r.ToString()+"</color>";
    }
}
public enum RuneType
{
    RerollBonus,            // +1 or +2 reroll per turn
    HeroAegis,              // Resurrect with 1 HP or full HP
    MarketDiscount,         // 25% or 50% discount in Market
    BossDoubleGold,         // Double gold from boss rewards
    PotionRetriggerChance,  // 10% or 20% chance to retrigger potion effects
    FreeMarketReroll        // First reroll is free each turn
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
    public string sprite_portrait;
    public List<int> RankUpgrades;
}
public enum BossAbilityEnum
{
    None,
    DisableHumanUnits,
    DisableRerolls,
    DisablePotions,
    DisableOrcUnits,
    DisableElfUnits,
    DisableUndeadUnits,

}
public enum RarityType
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3
}
[System.Serializable]
public class RaceData
{
    public CardRace theRace;
    public string sprite_theSprite;
    public Color theColor;
}
[System.Serializable]
public class ClassData
{
    public CardClass theClass;
    public string sprite_theSprite;
    public Color theColor;
}
[System.Serializable]
public class ArtifactData
{
    public string name;
    [TextArea] public string description;
    public string sprite_icon;

    public ArtifactEffectType effect;
    public int value; // if needed (e.g. +2 crit, +20 dmg)
    public int rarity;

    public string GetRarityText()
    {
       RarityType r =  (RarityType)rarity;
        return "\n\n<color=#"+UIManager.Instance.GetTextColor(rarity).ToHexString()+">"+r.ToString()+"</color>";
    }
}
[System.Serializable]
public class PotionCardData
{
    public string name;
    public PotionEffectType effectType;
    [TextArea] public string description;
    public string sprite_icon;

    public float value; // if needed (e.g. +2 crit, +20 dmg)
    public int rarity;
    public string GetRarityText()
    {
       RarityType r =  (RarityType)rarity;
        return "\n\n<color=#"+UIManager.Instance.GetTextColor(rarity).ToHexString()+">"+r.ToString()+"</color>";
    }
}
[System.Serializable]
public class UpgradeCardData
{
    public string name;
    [TextArea] public string description;
    public string sprite_icon;

    public UpgradeEffect effect;
    public UpgradeType type;
    public int value; // if needed (e.g. +2 crit, +20 dmg)
    public int rarity;
    public string GetRarityText()
    {
       RarityType r =  (RarityType)rarity;
        return "\n\n<color=#"+UIManager.Instance.GetTextColor(rarity).ToHexString()+">"+r.ToString()+"</color>";
    }
}
[System.Serializable]
public class BossData
{
    public string name;
    public string description;
    public string sprite_theSprite;

    public List<BossAbilityEnum> abilities;
    public float baseHP;
    public float baseDamage;
}
[System.Serializable]
public class EnemyData
{
    public string name;
    public string sprite_theSprite;

    public float baseHP;
    public float baseDamage;
}
[System.Serializable]
public class SkipRewardData
{
    public SkipRewardType type;
    public string title;
    [TextArea] public string description;
}

[System.Serializable]
public class HeroData
{
    public string heroName;
    public int startingHP;

    public HeroTrait startingTrait;        // e.g. +1 Attack or +1 Reroll
    public StartingItemType startingItem;  // e.g. Random Artifact or Potion

    public string description;             // For UI display
    public string portrait;                // Optional
}

public enum HeroTrait
{
    BonusAttack,
    BonusReroll,
    ExtraGold,
    None
}

public enum StartingItemType
{
    RandomArtifact,
    RandomPotion,
    None
}