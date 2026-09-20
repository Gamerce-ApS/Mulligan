using System.Collections.Generic;

public struct LocalizationSourceText
{
    public string TextID;
    public string English;

    public LocalizationSourceText(string textId, string english)
    {
        TextID = textId;
        English = english;
    }
}

public static class LocalizationDefaultTexts
{
    public static IEnumerable<LocalizationSourceText> GetAll()
    {
        yield return new LocalizationSourceText("ui.common.loading", "Loading{0}");
        yield return new LocalizationSourceText("ui.common.level", "Level {0}");
        yield return new LocalizationSourceText("ui.common.done", "Done");
        yield return new LocalizationSourceText("ui.common.none", "None");
        yield return new LocalizationSourceText("ui.common.hit", "{0} Hit");
        yield return new LocalizationSourceText("ui.common.unit", "Unit");
        yield return new LocalizationSourceText("ui.shop.unit_upgrade_pack", "Unit Upgrade Pack");
        yield return new LocalizationSourceText("ui.shop.unit_upgrade_pack_description", "Allows you to upgrade your units with Charms, Enchantments or Rank up");
        yield return new LocalizationSourceText("ui.tooltip.not_enough_gold", "Not enough gold!");
        yield return new LocalizationSourceText("ui.tooltip.no_slots", "No slots!");
        yield return new LocalizationSourceText("ui.tooltip.potion_slots_full", "Potion slots are full.");
        yield return new LocalizationSourceText("ui.tooltip.hero_owned", "You already own this hero!");
        yield return new LocalizationSourceText("ui.tooltip.hero_locked", "Unlock this hero first!");
        yield return new LocalizationSourceText("ui.tooltip.select_hero", "You need to select a hero!");
        yield return new LocalizationSourceText("ui.tooltip.achievement_completed", "Achievement completed!");
        yield return new LocalizationSourceText("ui.tooltip.artifact_sold", "Artifact sold!");
        yield return new LocalizationSourceText("ui.tooltip.potion_sold", "Potion sold!");
        yield return new LocalizationSourceText("ui.tooltip.click_orcs", "Click on ORCs");
        yield return new LocalizationSourceText("ui.tooltip.click_warriors", "Click on Warriors");
        yield return new LocalizationSourceText("ui.tooltip.click_correct_cards", "Click on correct cards!");
        yield return new LocalizationSourceText("ui.tooltip.select_reroll_card", "Select a card to reroll");
        yield return new LocalizationSourceText("ui.tooltip.select_two_reroll_cards", "Select 2 cards to reroll");
        yield return new LocalizationSourceText("ui.tooltip.rerolls_disabled", "Rerolls disabled!");
        yield return new LocalizationSourceText("ui.tooltip.new_unlocks", "New unlocks!");
        yield return new LocalizationSourceText("ui.tooltip.gold_stolen", "Gold stolen!");
        yield return new LocalizationSourceText("ui.tooltip.dodged_attack", "Dodged Attack!");
        yield return new LocalizationSourceText("ui.tooltip.unit_ranked_up", "1 Unit Ranked Up!");
        yield return new LocalizationSourceText("ui.tooltip.only_four_cards", "Only 4 cards can be selected.");
        yield return new LocalizationSourceText("ui.tooltip.select_unit_upgrade", "Click on a Unit and an Upgrade!");
        yield return new LocalizationSourceText("ui.tooltip.select_unit_upgrade_card", "Click on a Unit and an Upgrade card!");
        yield return new LocalizationSourceText("ui.tooltip.unit_destroyed", "{0} Destroyed");
        yield return new LocalizationSourceText("ui.tooltip.unit_duplicated", "{0} Duplicated");
        yield return new LocalizationSourceText("ui.tooltip.gold_from_artifact", "+{0} Gold from artifact");
        yield return new LocalizationSourceText("ui.tooltip.healed_from_artifact", "Healed {0}% HP from artifact");
        yield return new LocalizationSourceText("ui.tooltip.random_unit_destroyed", "Destroyed random unit in hand");
        yield return new LocalizationSourceText("ui.tooltip.random_potion_added", "Added random potion!");
        yield return new LocalizationSourceText("ui.tooltip.reroll_added", "+{0} Reroll");
        yield return new LocalizationSourceText("ui.tooltip.army_size_added", "+{0} Army Size");
        yield return new LocalizationSourceText("ui.tooltip.attack_added", "+{0} Attack");
        yield return new LocalizationSourceText("ui.tooltip.retriggered_unit", "Retriggered: {0}");
        yield return new LocalizationSourceText("ui.tooltip.potion_added_for_unit", "Added potion: {0}");
        yield return new LocalizationSourceText("ui.tooltip.healed_max_health", "Healed 10% of max health!");
        yield return new LocalizationSourceText("ui.tooltip.level_up", "Level Up! Max HP increased to {0}");
        yield return new LocalizationSourceText("ui.tooltip.all_artifacts_unlocked", "All artifacts unlocked");
        yield return new LocalizationSourceText("ui.tooltip.coming_soon", "Coming soon!");
        yield return new LocalizationSourceText("ui.tooltip.hero_info_coming_soon", "Hero info coming soon!");
        yield return new LocalizationSourceText("ui.tooltip.potion_crit", "+{0} Crit to {1}");
        yield return new LocalizationSourceText("ui.tooltip.potion_damage", "+{0} Damage to {1}");
        yield return new LocalizationSourceText("ui.tooltip.potion_faceless", "{0} becomes Faceless");
        yield return new LocalizationSourceText("ui.tooltip.potion_suicide_boost", "{0} gains {1} 4x Damage but will explode");
        yield return new LocalizationSourceText("ui.tooltip.potion_disable_debuff", "Boss debuff disabled this turn");
        yield return new LocalizationSourceText("ui.tooltip.potion_heal", "Hero healed {0}% HP");
        yield return new LocalizationSourceText("ui.tooltip.potion_boost_lose_hp", "All damage x5 this turn, lose 10% max HP");
        yield return new LocalizationSourceText("ui.tooltip.potion_retrigger_upgrades", "Retriggered upgrades for {0}");
        yield return new LocalizationSourceText("ui.tooltip.potion_retriggered", "Retriggered Potion!");
        yield return new LocalizationSourceText("ui.tooltip.rune_rerolls", "+{0} Reroll per turn");
        yield return new LocalizationSourceText("ui.tooltip.rune_revive", "Revive once with 1 HP");
        yield return new LocalizationSourceText("ui.tooltip.rune_market_discount", "{0}% discount in the Market");
        yield return new LocalizationSourceText("ui.tooltip.rune_double_gold", "Bosses drop double Gold");
        yield return new LocalizationSourceText("ui.tooltip.rune_retrigger_potions", "{0}% chance to retrigger Potions");
        yield return new LocalizationSourceText("ui.tooltip.rune_free_market_reroll", "First Market reroll each turn is free");
        yield return new LocalizationSourceText("ui.tooltip.rune_attacks", "+{0} Attacks per turn");
        yield return new LocalizationSourceText("ui.tooltip.rune_artifact_slot", "Artifact slot unlocked!");
        yield return new LocalizationSourceText("ui.daily.new_quests_in", "New quests in {0}h {1}m");
        yield return new LocalizationSourceText("ui.daily.play_race_units", "Play {0} {1} units");
        yield return new LocalizationSourceText("ui.daily.play_class_units", "Play {0} {1} units");
        yield return new LocalizationSourceText("ui.daily.reach_level", "Reach level {0}");
        yield return new LocalizationSourceText("ui.daily.single_attack_damage", "Deal {0} damage in one attack");
        yield return new LocalizationSourceText("ui.defeat.reached", "You reached World {0}, Level {1}");
        yield return new LocalizationSourceText("ui.tutorial.click_continue", "Click to continue");
        yield return new LocalizationSourceText("ui.highscore.level", "Level {0}");
        yield return new LocalizationSourceText("ui.highscore.hit", "{0} Hit");
        yield return new LocalizationSourceText("ui.highscore.enter_name", "Enter a name");
        yield return new LocalizationSourceText("ui.highscore.could_not_connect", "Could not connect");
        yield return new LocalizationSourceText("ui.highscore.name_unavailable", "Name unavailable");
        yield return new LocalizationSourceText("ui.damage.critical", "+{0} Critical");
        yield return new LocalizationSourceText("ui.damage.gold", "+{0} Gold");
        yield return new LocalizationSourceText("ui.card.temp_crit", "\n+{0} Crit");
        yield return new LocalizationSourceText("ui.card.temp_damage", "\n+{0} Damage");
        yield return new LocalizationSourceText("ui.hero.starting_items", "<color={0}>Starting Items:</color>");
        yield return new LocalizationSourceText("ui.hero.health", "<color={0}>Health:</color> {1}");
        yield return new LocalizationSourceText("ui.hero.artifact_slots", "<color={0}>Artifact Slots:</color> {1}");
        yield return new LocalizationSourceText("ui.hero.potion_slots", "<color={0}>Potion Slots:</color> {1}");
        yield return new LocalizationSourceText("ui.hero.starting_gold", "<color={0}>Starting Gold:</color> {1}");
        yield return new LocalizationSourceText("ui.synergy.title", "Synergies");
        yield return new LocalizationSourceText("ui.synergy.description", "2 units: 2X damage\n\n4 units: 3X Critical");
        yield return new LocalizationSourceText("ui.runes.title", "Hero Runes");
        yield return new LocalizationSourceText("ui.victory.message_0", "- You did great!");
        yield return new LocalizationSourceText("ui.victory.message_1", "- Victory is yours!");
        yield return new LocalizationSourceText("ui.victory.message_2", "- The crowd goes wild!");
        yield return new LocalizationSourceText("ui.victory.message_3", "- Another step to glory!");
        yield return new LocalizationSourceText("ui.victory.message_4", "- You're unstoppable!");
        yield return new LocalizationSourceText("ui.victory.message_5", "- Hero of the realm!");
        yield return new LocalizationSourceText("ui.victory.message_6", "- You crushed it!");
    }
}
