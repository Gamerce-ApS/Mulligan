using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

public class RuneManager : Singleton<RuneManager>
{
    public List<RuneData> ActiveRunes = new List<RuneData>(5);
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void TriggerRunes(RuneData aRune, Card targetCard = null)
    {
        SoundManager.TryPlay(SoundType.RuneTrigger);

        switch (aRune.type)
        {
            case RuneType.RerollBonus:
                // int rerolls = (aRune.rarity == RuneRarity.Rare) ? 2 : 1;
                GameManager.Instance.BonusRerolls += 1;
                UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.rune_rerolls", "+{0} Reroll per turn", 1));
                break;
                            case RuneType.RerollBonus2X:
                // int rerolls = (aRune.rarity == RuneRarity.Rare) ? 2 : 1;
                GameManager.Instance.BonusRerolls += 2;
                UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.rune_rerolls", "+{0} Reroll per turn", 2));
                break;

            case RuneType.HeroAegis:
                // if (aRune.rarity == RuneRarity.Rare)
                // {
                //     GameManager.Instance.ReviveFullHP = true;
                //     UIManager.Instance.ShowTooltip("Revive once with full HP");
                // }
                // else
                // {
                GameManager.Instance.ReviveWith1HP = true;
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.rune_revive", "Revive once with 1 HP"));
                // }
                break;

            case RuneType.MarketDiscount:
                // float discount = (aRune.rarity == RuneRarity.Rare) ? 0.5f : 0.25f;
                GameManager.Instance.MarketDiscountModifier = 0.25f;
                UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.rune_market_discount", "{0}% discount in the Market", 25));
                break;
                            case RuneType.MarketDiscount2X:
                // float discount = (aRune.rarity == RuneRarity.Rare) ? 0.5f : 0.25f;
                GameManager.Instance.MarketDiscountModifier = 0.5f;
                UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.rune_market_discount", "{0}% discount in the Market", 50));
                break;

            case RuneType.BossDoubleGold:
                GameManager.Instance.BossGoldMultiplier = 2f;
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.rune_double_gold", "Bosses drop double Gold"));
                break;

            case RuneType.PotionRetriggerChance:
                // float retriggerChance = (aRune.rarity == RuneRarity.Rare) ? 0.20f : 0.10f;
                GameManager.Instance.PotionRetriggerChance = 0.1f;
                UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.rune_retrigger_potions", "{0}% chance to retrigger Potions", 10));
                break;

            case RuneType.FreeMarketReroll:
                GameManager.Instance.HasFreeReroll = true;
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.rune_free_market_reroll", "First Market reroll each turn is free"));
                break;
            case RuneType.RuneOfAttack:
                GameManager.Instance.BonusAttacks += 1;
                UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.rune_attacks", "+{0} Attacks per turn", 1));
                break;
            case RuneType.RuneOfAttack2X:
                GameManager.Instance.BonusAttacks += 2;
                UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.rune_attacks", "+{0} Attacks per turn", 2));
                break;

            case RuneType.RuneOfArtifact:
                GameManager.Instance.TheHero.myHeroData.ArtifactSlots++;
                ShopManager.Instance.RefreshArtifactSlots();
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.rune_artifact_slot", "Artifact slot unlocked!"));
                break;
            case RuneType.RuneOfArtifact2X:
                GameManager.Instance.TheHero.myHeroData.ArtifactSlots++;
                GameManager.Instance.TheHero.myHeroData.ArtifactSlots++;
                ShopManager.Instance.RefreshArtifactSlots();
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.rune_artifact_slot", "Artifact slot unlocked!"));
                break;

            case RuneType.RuneOfRareChance:
                // CardContainer.Instance.Rarity[0] -= CardContainer.Instance.Rarity[1];
                CardContainer.Instance.Rarity[1] *= 2;

                break;
            case RuneType.RuneOfEpicChance:
                // CardContainer.Instance.Rarity[0] -= CardContainer.Instance.Rarity[2];
                CardContainer.Instance.Rarity[2] *= 2;


                break;
            default:
                Debug.LogWarning("Unhandled rune type: " + aRune.type);
                break;
        }

        UIManager.Instance.UpdateArtifactSlotsUI();

    }




    public void AddRandomRune()
    {


        var all = CardContainer.Instance.GetUnlockedRunes();
        if (all == null || all.Count == 0)
        {
            Debug.LogWarning("No runes available to choose from.");
            return;
        }

        // Filter out already equipped ones
        List<RuneData> available = new List<RuneData>();
        foreach (var artifact in all)
        {
            if (!ActiveRunes.Contains(artifact))
            {
                available.Add(artifact);
            }
        }

        if (available.Count == 0)
        {
            Debug.Log("All artifacts are already equipped.");
            return;
        }

        // Pick random one
        RuneData selected = available[Random.Range(0, available.Count)];
        if (selected == null)
            return;

        ActiveRunes.Add(selected);
        SoundManager.TryPlay(SoundType.RuneObtained);

        // Update UI
        UIManager.Instance.UpdateArtifactSlotsUI();
        TriggerRunes(selected);
        Debug.Log("Added rune: " + selected.name);
    }
    public RuneData GetRandom()
    {

        var all = CardContainer.Instance.GetUnlockedRunes();
        if (all == null || all.Count == 0)
        {
            Debug.LogWarning("No runes available to choose from.");
            return null;
        }

        // Filter out already equipped ones
        List<RuneData> available = new List<RuneData>();
        foreach (var artifact in all)
        {
            if (!ActiveRunes.Contains(artifact))
            {
                available.Add(artifact);
            }
        }

        if (available.Count == 0)
        {
            Debug.Log("All runes are already equipped.");
            return null;
        }

        // Pick random one
        // RuneData selected = available[Random.Range(0, available.Count)];
        RuneData selected = PickRuneByRarity(available);
        return selected;
    }
    public void AddRune(RuneData artifact)
    {
        if (artifact == null)
            return;

        ActiveRunes.Add(artifact);
        SoundManager.TryPlay(SoundType.RuneObtained);
        UIManager.Instance.UpdateArtifactSlotsUI(); // updates visuals
        ShopManager.Instance.RefreshHeroRunes();
        TriggerRunes(artifact);
    }

    public void AddRune(RuneType aType)
    {
     

        var all = CardContainer.Instance.GetUnlockedRunes();

        // Filter out already equipped ones
        List<RuneData> available = new List<RuneData>();
        foreach (var artifact in all)
        {
            if (!ActiveRunes.Contains(artifact) && aType== artifact.type)
            {
                ActiveRunes.Add(artifact);
                SoundManager.TryPlay(SoundType.RuneObtained);

                // Update UI
                UIManager.Instance.UpdateArtifactSlotsUI();

                Debug.Log("Added Rune: " + artifact.name);
                return;
            }
        }

        if (available.Count == 0)
        {
            Debug.Log("All runes are already equipped.");
            return;
        }




    }
    public string GetActiveRunesInfo()
    {
        string tot = "";
        foreach (var r in ActiveRunes)
        {
            tot += LocalizedContent.RuneName(r) + "\n";
        }
        return tot;
    }
    private RuneData PickRuneByRarity(List<RuneData> available)
    {
        if (available == null || available.Count == 0)
            return null;

        RarityType rolled = CardContainer.Instance.GetRandomRarity();

        // Exact rarity first
        var pool = available
            .Where(a => (RarityType)a.rarity == rolled)
            .ToList();

        if (pool.Count > 0)
            return pool[Random.Range(0, pool.Count)];

        // Fallback downward
        for (int r = (int)rolled - 1; r >= 0; r--)
        {
            pool = available
                .Where(a => (int)a.rarity == r)
                .ToList();

            if (pool.Count > 0)
                return pool[Random.Range(0, pool.Count)];
        }

        // Fallback upward
        for (int r = (int)rolled + 1; r <= 3; r++)
        {
            pool = available
                .Where(a => (int)a.rarity == r)
                .ToList();

            if (pool.Count > 0)
                return pool[Random.Range(0, pool.Count)];
        }

        // Final fallback
        return available[Random.Range(0, available.Count)];
    }






}
