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
        switch (aRune.type)
        {
            case RuneType.RerollBonus:
                int rerolls = (aRune.rarity == RuneRarity.Rare) ? 2 : 1;
                GameManager.Instance.BonusRerolls += rerolls;
                UIManager.Instance.ShowTooltip($"+{rerolls} Reroll{(rerolls > 1 ? "s" : "")} per turn");
                break;

            case RuneType.HeroAegis:
                if (aRune.rarity == RuneRarity.Rare)
                {
                    GameManager.Instance.ReviveFullHP = true;
                    UIManager.Instance.ShowTooltip("Revive once with full HP");
                }
                else
                {
                    GameManager.Instance.ReviveWith1HP = true;
                    UIManager.Instance.ShowTooltip("Revive once with 1 HP");
                }
                break;

            case RuneType.MarketDiscount:
                float discount = (aRune.rarity == RuneRarity.Rare) ? 0.5f : 0.25f;
                GameManager.Instance.MarketDiscountModifier = discount;
                UIManager.Instance.ShowTooltip($"{(int)(discount * 100)}% discount in the Market");
                break;

            case RuneType.BossDoubleGold:
                GameManager.Instance.BossGoldMultiplier = 2f;
                UIManager.Instance.ShowTooltip("Bosses drop double Gold");
                break;

            case RuneType.PotionRetriggerChance:
                float retriggerChance = (aRune.rarity == RuneRarity.Rare) ? 0.20f : 0.10f;
                GameManager.Instance.PotionRetriggerChance = retriggerChance;
                UIManager.Instance.ShowTooltip($"{(int)(retriggerChance * 100)}% chance to retrigger Potions");
                break;

            case RuneType.FreeMarketReroll:
                GameManager.Instance.HasFreeReroll = true;
                UIManager.Instance.ShowTooltip("First Market reroll each turn is free");
                break;

            default:
                Debug.LogWarning("Unhandled rune type: " + aRune.type);
                break;
        }

        UIManager.Instance.UpdateArtifactSlotsUI();
    }




    public void AddRandomRune()
    {


        var all = CardContainer.Instance.RuneDataList;
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("No artifacts available to choose from.");
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

        ActiveRunes.Add(selected);

        // Update UI
        UIManager.Instance.UpdateArtifactSlotsUI();
        TriggerRunes(selected);
        Debug.Log("Added rune: " + selected.name);
    }
    public RuneData GetRandom()
    {

        var all = CardContainer.Instance.RuneDataList;
        if (all == null || all.Length == 0)
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
        RuneData selected = available[Random.Range(0, available.Count)];

        return selected;
    }

    public void AddRune(RuneData artifact)
    {

        ActiveRunes.Add(artifact);
        UIManager.Instance.UpdateArtifactSlotsUI(); // updates visuals

        TriggerRunes(artifact);
    }





 

}
