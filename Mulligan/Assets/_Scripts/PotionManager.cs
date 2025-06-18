using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

public class PotionManager : Singleton<PotionManager>
{
    public List<PotionCardData> ActivePotions = new List<PotionCardData>(5);
    public CardDataObject cardDataObject;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void TriggerPotion(PotionCardData potion, Card targetCard = null)
    {
        switch (potion.effectType)
        {
            case PotionEffectType.CritBonus:
                if (targetCard != null)
                {
                    targetCard.cardInstance.tempCritBonus += potion.value;
                    targetCard.UpdateCardUI();
                    UIManager.Instance.ShowTooltip($"+{potion.value} Crit to {targetCard.cardInstance.data.cardName}");
                }
                break;

            case PotionEffectType.DamageBonus:
                if (targetCard != null)
                {
                    targetCard.cardInstance.tempDamageBonus += potion.value;
                    targetCard.UpdateCardUI();
                    UIManager.Instance.ShowTooltip($"+{potion.value} Damage to {targetCard.cardInstance.data.cardName}");
                }
                break;

            case PotionEffectType.RandomDamage:
                var allUnits = HandManager.Instance.CurrentHand.Where(ci => ci.CardGO != null).ToList();
                allUnits.Shuffle();
                foreach (var unit in allUnits.Take(3))
                {
                    unit.tempDamageBonus += potion.value;
                    unit.CardGO.UpdateCardUI();
                    //unit.CardGO?.PlayBoostAnimation(potion.value, UIManager.Instance.DamageLabel.transform);
                }
                break;

            case PotionEffectType.FacelessSingle:
                if (targetCard != null)
                {
                    targetCard.cardInstance.BecomeFacelessThisTurn();
                    UIManager.Instance.ShowTooltip($"{targetCard.cardInstance.data.cardName} becomes Faceless");
                }
                break;

            case PotionEffectType.FacelessMultiple:
                var list = HandManager.Instance.CurrentHand.Where(ci => ci.CardGO != null).ToList();
                list.Shuffle();
                foreach (var unit in list.Take(2))
                {
                    unit.BecomeFacelessThisTurn();
                    UIManager.Instance.ShowTooltip($"{unit.data.cardName} becomes Faceless");
                }
                break;

            case PotionEffectType.SuicideBoost:
                if (targetCard != null)
                {
                    targetCard.cardInstance.currentRank += potion.value;
                    targetCard.cardInstance.WillExplodeAfterAttack = true;
                    targetCard.UpdateCardUI();
                    UIManager.Instance.ShowTooltip($"{targetCard.cardInstance.data.cardName} gains {potion.value} Ranks but will explode");
                }
                break;

            case PotionEffectType.DisableDebuff:
                GameManager.Instance.DisableBossDebuffForTurn();
                UIManager.Instance.ShowTooltip($"Boss debuff disabled this turn");
                break;

            case PotionEffectType.HealHero:
                GameManager.Instance.TheHero.HealPercent(0.2f);
                UIManager.Instance.ShowTooltip("Hero healed 20% HP");
                break;

            case PotionEffectType.BoostAndLoseHP:
                //EvaluatorManager.Instance.ApplyGlobalDamageMultiplier(5);

                var allUnits2 = HandManager.Instance.CurrentHand.Where(ci => ci.CardGO != null).ToList();
                foreach (var unit in allUnits2)
                {
                    unit.tempDamageBonus = (unit.GetDamage())* potion.value;
                    unit.CardGO.UpdateCardUI();
                }


                GameManager.Instance.TheHero.ReduceMaxHPPercent(0.1f);
                UIManager.Instance.ShowTooltip("All damage x5 this turn, lose 10% max HP");
                break;

            case PotionEffectType.RetriggerUpgrades:
                if (targetCard != null)
                {
                    //targetCard.cardInstance.TriggerAllUpgrades();
                    UIManager.Instance.ShowTooltip($"Retriggered upgrades for {targetCard.cardInstance.data.cardName}");
                }
                break;

            default:
                Debug.LogWarning("Unhandled potion type: " + potion.effectType);
                break;
        }

        // Remove used potion
        ActivePotions.Remove(potion);
        UIManager.Instance.UpdateArtifactSlotsUI();
    }


    public void AddRandomPotion()
    {
        if (ActivePotions.Count >= 6)
        {
            Debug.Log("Artifact slots are full.");
            return;
        }

        var all = cardDataObject.allPotions;
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("No artifacts available to choose from.");
            return;
        }

        // Filter out already equipped ones
        List<PotionCardData> available = new List<PotionCardData>();
        foreach (var artifact in all)
        {
            if (!ActivePotions.Contains(artifact))
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
        PotionCardData selected = available[Random.Range(0, available.Count)];

        ActivePotions.Add(selected);

        // Update UI
        UIManager.Instance.UpdateArtifactSlotsUI();

        Debug.Log("Added artifact: " + selected.name);
    }
    public PotionCardData GetRandom()
    {

        var all = cardDataObject.allPotions;
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("No artifacts available to choose from.");
            return null;
        }

        // Filter out already equipped ones
        List<PotionCardData> available = new List<PotionCardData>();
        foreach (var artifact in all)
        {
            if (!ActivePotions.Contains(artifact))
            {
                available.Add(artifact);
            }
        }

        if (available.Count == 0)
        {
            Debug.Log("All potions are already equipped.");
            return null;
        }

        // Pick random one
        PotionCardData selected = available[Random.Range(0, available.Count)];

        return selected;
    }
    public void AddPotion(PotionEffectType aType)
    {
        if (ActivePotions.Count >= 2)
        {
            Debug.Log("potion slots are full.");
            return;
        }

        var all = cardDataObject.allPotions;
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("No potions available to choose from.");
            return;
        }

        // Filter out already equipped ones
        List<PotionCardData> available = new List<PotionCardData>();
        foreach (var artifact in all)
        {
            if (!ActivePotions.Contains(artifact) && aType== artifact.effectType)
            {
                ActivePotions.Add(artifact);

                // Update UI
                UIManager.Instance.UpdateArtifactSlotsUI();

                Debug.Log("Added artifact: " + artifact.name);
                return;
            }
        }

        if (available.Count == 0)
        {
            Debug.Log("All artifacts are already equipped.");
            return;
        }
    }
    public void AddPotion(PotionCardData artifact)
    {
        if (ActivePotions.Count >= 5) return;

        ActivePotions.Add(artifact);
        UIManager.Instance.UpdateArtifactSlotsUI(); // updates visuals
    }
    public void RemovePotion(PotionCardData artifact)
    {

        ActivePotions.Remove(artifact);
        UIManager.Instance.UpdateArtifactSlotsUI(); // updates visuals
    }
    public bool HasArtifact(PotionEffectType effectType)
    {
        return ActivePotions.Exists(a => a.effectType == effectType);
    }

    public int GetArtifactValue(PotionEffectType effectType)
    {
        int total = 0;
        foreach (var artifact in ActivePotions)
        {
            if (artifact.effectType == effectType)
                total += artifact.value;
        }
        return total;
    }

    [ContextMenu("Load Artifacts from JSON")]
    public void LoadArtifactsFromJsonFile()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "potions_colored.json");

        if (!File.Exists(path))
        {
            Debug.LogError("Artifact JSON not found at: " + path);
            return;
        }

        string json = File.ReadAllText(path);

        try
        {
            var loaded = JsonConvert.DeserializeObject<List<PotionCardData>>(json);

            if (cardDataObject != null)
            {
                cardDataObject.allPotions = loaded.ToArray();
                Debug.Log($"Loaded {loaded.Count} artifacts into CardDataObject.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to parse artifacts: " + ex.Message);
        }
    }

}
