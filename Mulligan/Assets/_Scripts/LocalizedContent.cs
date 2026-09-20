using System;

public static class LocalizationKeys
{
    public static string UnitName(CardData data)
    {
        return "unit." + LocalizationService.Slug(data != null ? data.sprite_portrait : "unknown") + ".name";
    }

    public static string Artifact(ArtifactData data, string field)
    {
        if (data == null)
            return "artifact.unknown." + field;

        return "artifact." + LocalizationService.Slug(data.name) + "." +
               LocalizationService.Slug(data.effect.ToString()) + "." + LocalizationService.Number(data.value) + "." + LocalizationService.Number(data.rarity) + "." + field;
    }

    public static string Potion(PotionCardData data, string field)
    {
        if (data == null)
            return "potion.unknown." + field;

        return "potion." + LocalizationService.Slug(data.name) + "." +
               LocalizationService.Slug(data.effectType.ToString()) + "." +
               LocalizationService.Slug(LocalizationService.Number(data.value)) + "." + LocalizationService.Number(data.rarity) + "." + field;
    }

    public static string Upgrade(UpgradeCardData data, string field)
    {
        if (data == null)
            return "upgrade.unknown." + field;

        return "upgrade." + LocalizationService.Slug(data.name) + "." +
               LocalizationService.Slug(data.effect.ToString()) + "." +
               LocalizationService.Slug(data.type.ToString()) + "." + LocalizationService.Number(data.value) + "." + LocalizationService.Number(data.rarity) + "." + field;
    }

    public static string Rune(RuneData data, string field)
    {
        if (data == null)
            return "rune.unknown." + field;

        return "rune." + LocalizationService.Slug(data.name) + "." +
               LocalizationService.Slug(data.type.ToString()) + "." + LocalizationService.Number((int)data.rarity) + "." + field;
    }

    public static string Boss(BossData data, string field)
    {
        if (data == null)
            return "boss.unknown." + field;

        return "boss." + LocalizationService.Slug(data.sprite_theSprite) + "." +
               LocalizationService.Slug(data.name) + "." + field;
    }

    public static string Enemy(EnemyData data)
    {
        return "enemy." + LocalizationService.Slug(data != null ? data.sprite_theSprite : "unknown") + ".name";
    }

    public static string Hero(int heroIndex, string field)
    {
        return "hero." + LocalizationService.Number(heroIndex) + "." + field;
    }

    public static string SkipReward(SkipRewardData data, string field)
    {
        return "skip_reward." + LocalizationService.Slug(data != null ? data.type.ToString() : "unknown") + "." + field;
    }

    public static string DailyQuest(DailyQuestDefinition definition)
    {
        if (definition == null)
            return "daily_quest.unknown";

        string key = "daily_quest." + LocalizationService.Slug(definition.Type.ToString());
        if (definition.Type == DailyQuestType.PlayRaceUnits)
            key += "." + LocalizationService.Slug(definition.Race.ToString());
        else if (definition.Type == DailyQuestType.PlayClassUnits)
            key += "." + LocalizationService.Slug(definition.Class.ToString());

        return key + "." + LocalizationService.Number(definition.TargetAmount);
    }

    public static string Tutorial(string stepId)
    {
        return "tutorial." + LocalizationService.Slug(stepId) + ".dialogue";
    }
}

public static class LocalizedContent
{
    public static string UnitName(CardData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.UnitName(data), data.cardName);
    }

    public static string ArtifactName(ArtifactData data)
    {
        if (data == null)
            return string.Empty;

        if (data.name.Contains("RandomRace"))
        {
            string fallback = data.name.Replace("RandomRace", "{0}");
            return LocalizationService.Format(LocalizationKeys.Artifact(data, "name"), fallback, Race(data.RandomRace));
        }

        return LocalizationService.Get(LocalizationKeys.Artifact(data, "name"), data.name);
    }

    public static string ArtifactDescription(ArtifactData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.Artifact(data, "description"), data.description);
    }

    public static string PotionName(PotionCardData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.Potion(data, "name"), data.name);
    }

    public static string PotionDescription(PotionCardData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.Potion(data, "description"), data.description);
    }

    public static string UpgradeName(UpgradeCardData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.Upgrade(data, "name"), data.name);
    }

    public static string UpgradeDescription(UpgradeCardData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.Upgrade(data, "description"), data.description);
    }

    public static string RuneName(RuneData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.Rune(data, "name"), data.name);
    }

    public static string RuneDescription(RuneData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.Rune(data, "description"), data.description);
    }

    public static string BossName(BossData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.Boss(data, "name"), data.name);
    }

    public static string BossDescription(BossData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.Boss(data, "description"), data.description);
    }

    public static string EnemyName(EnemyData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.Enemy(data), data.name);
    }

    public static string HeroName(HeroData data, int heroIndex)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.Hero(heroIndex, "name"), data.heroName);
    }

    public static string HeroDescription(HeroData data, int heroIndex)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.Hero(heroIndex, "description"), data.description);
    }

    public static string HeroStartingItems(HeroData data, int heroIndex)
    {
        return LocalizationService.Get(
            LocalizationKeys.Hero(heroIndex, "starting_items"),
            GetHeroStartingItemsFallback(data));
    }

    public static string GetHeroStartingItemsFallback(HeroData data)
    {
        if (data == null)
            return "None";

        if (data.heroName == "Dwarf")
            return "Rune of Rare Chance\nArtifact: +2 Gold";
        if (data.heroName == "Warlock")
            return "Rune of Attack\nArtifact: Rank Up";
        if (data.heroName == "Goblin")
            return "2x Potions";
        if (data.startingItem == StartingItemType.RandomArtifact)
            return "Random Artifact";
        if (data.startingItem == StartingItemType.RandomPotion)
            return "Random Potion";
        if (data.startingTrait == HeroTrait.BonusAttack)
            return "+1 Attack";
        if (data.startingTrait == HeroTrait.BonusReroll)
            return "+1 Reroll";
        if (data.startingTrait == HeroTrait.ExtraGold)
            return "Extra Gold";
        if (string.IsNullOrEmpty(data.description) == false)
            return data.description;

        return "None";
    }

    public static string SkipRewardTitle(SkipRewardData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.SkipReward(data, "title"), data.title);
    }

    public static string SkipRewardDescription(SkipRewardData data)
    {
        return data == null ? string.Empty : LocalizationService.Get(LocalizationKeys.SkipReward(data, "description"), data.description);
    }

    public static string Race(CardRace race)
    {
        return LocalizationService.Get("race." + LocalizationService.Slug(race.ToString()), race.ToString());
    }

    public static string Class(CardClass cardClass)
    {
        return LocalizationService.Get("class." + LocalizationService.Slug(cardClass.ToString()), cardClass.ToString());
    }

    public static string Rarity(RarityType rarity)
    {
        return LocalizationService.Get("rarity." + LocalizationService.Slug(rarity.ToString()), rarity.ToString());
    }

    public static string UpgradeType(UpgradeType type)
    {
        return LocalizationService.Get("upgrade_type." + LocalizationService.Slug(type.ToString()), type.ToString());
    }

    public static string Tutorial(string stepId, string fallback)
    {
        return LocalizationService.Get(LocalizationKeys.Tutorial(stepId), fallback);
    }
}
