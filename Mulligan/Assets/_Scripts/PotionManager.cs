using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

public class PotionManager : Singleton<PotionManager>
{
    public List<PotionCardData> ActivePotions = new List<PotionCardData>(5);
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
        int EffectMultiplier = 1;
        if(ArtifactManager.Instance.HasArtifact(ArtifactEffectType.DoublePotionEffects))
            EffectMultiplier = 2;


        switch (potion.effectType)
        {
            case PotionEffectType.CritBonus:
                if (targetCard != null)
                {
                    targetCard.cardInstance.tempCritBonus += (int)potion.value* EffectMultiplier;
                    targetCard.UpdateCardUI();
                    UIManager.Instance.ShowTooltip($"+{potion.value} Crit to {targetCard.cardInstance.data.cardName}");
                }
                break;

            case PotionEffectType.DamageBonus:
                if (targetCard != null)
                {
                    targetCard.cardInstance.tempDamageBonus += (int)potion.value * EffectMultiplier;
                    targetCard.UpdateCardUI();
                    UIManager.Instance.ShowTooltip($"+{potion.value} Damage to {targetCard.cardInstance.data.cardName}");
                }
                break;

            case PotionEffectType.RandomDamage:
                var allUnits = HandManager.Instance.CurrentHand.Where(ci => ci.CardGO != null).ToList();
                allUnits.Shuffle();
                foreach (var unit in allUnits.Take(3))
                {
                    unit.tempDamageBonus += (int)potion.value * EffectMultiplier;
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
                case PotionEffectType.DestroyUnit:
                if (targetCard != null)
                {
                    targetCard.cardInstance.Destroy();
                    UIManager.Instance.ShowTooltip($"{targetCard.cardInstance.data.cardName} Destroyed");
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

                if(EffectMultiplier==2)
                {
                    foreach (var unit in list.Take(2))
                    {
                        unit.BecomeFacelessThisTurn();
                        UIManager.Instance.ShowTooltip($"{unit.data.cardName} becomes Faceless");
                    }
                }
                break;

            case PotionEffectType.SuicideBoost:
                if (targetCard != null)
                {
                    targetCard.cardInstance.tempDamageBonus += targetCard.cardInstance.GetDamage() * (int)potion.value * EffectMultiplier;
                    targetCard.cardInstance.WillExplodeAfterAttack = true;
                    targetCard.UpdateCardUI();
                    UIManager.Instance.ShowTooltip($"{targetCard.cardInstance.data.cardName} gains {potion.value} 4x Damage but will explode");
                }
                break;

            case PotionEffectType.DisableDebuff:
                GameManager.Instance.DisableBossDebuffForTurn();
                UIManager.Instance.ShowTooltip($"Boss debuff disabled this turn");
                break;

            case PotionEffectType.HealHero:
                GameManager.Instance.TheHero.HealPercent(potion.value * EffectMultiplier);
                UIManager.Instance.ShowTooltip("Hero healed "+ potion.value * EffectMultiplier+ "% HP");

                GameManager.Instance.TheHero.HealEffect.SetActive(false);
                GameManager.Instance.TheHero.HealEffect.SetActive(true);
                break;

            case PotionEffectType.BoostAndLoseHP:
                //EvaluatorManager.Instance.ApplyGlobalDamageMultiplier(5);

                var allUnits2 = HandManager.Instance.CurrentHand.Where(ci => ci.CardGO != null).ToList();
                foreach (var unit in allUnits2)
                {
                    unit.tempDamageBonus = (unit.GetDamage())* (int)potion.value;
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
        GameData.PotionsUsed++;


        if(GameManager.Instance.PotionRetriggerChance>0)
        {
            if(Random.Range(0,100) < GameManager.Instance.PotionRetriggerChance *100f)
            {
                LeanTween.delayedCall(gameObject, 0.5f, () =>
                {
                    UIManager.Instance.ShowTooltip($"Retriggered Potion!");
                    LeanTween.delayedCall(gameObject, 0.5f, () =>
                    {
                        TriggerPotion(potion, targetCard);
                    });
                });

                return;
            }
        }

        // Remove used potion
        ActivePotions.Remove(potion);
        UIManager.Instance.UpdateArtifactSlotsUI();
    }

public void SellPotion(Potion aPotion)
    {
        ActivePotions.Remove(aPotion.PotionData);
        Destroy(aPotion.gameObject);
        UIManager.Instance.UpdatePotionsSlotsUI(); // updates visuals
        GameData.CurrentGold += 1;



    }
    public PotionCardData AddRandomPotion(int rarity = -1)
    {
        if (ActivePotions.Count >= GameManager.Instance.TheHero.myHeroData.PotionSlots)
        {
            Debug.Log("Potions slots are full.");
            return null;
        }

        var all = CardContainer.Instance.PotionDataList;
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
            Debug.Log("All artifacts are already equipped.");
            return null;
        }



        // Pick random one
        // PotionCardData randomFromAll = available[Random.Range(0, available.Count)];
        PotionCardData selected = PickPotionWeightedByRarity(available);

        if( rarity != -1)
        {
            selected = PickPotionByRarity(available,rarity);
        }
        
        ActivePotions.Add(selected);

        // Update UI
        UIManager.Instance.UpdateArtifactSlotsUI();

        Debug.Log("Added artifact: " + selected.name);

        return selected;
    }
    public PotionCardData GetRandom()
    {

        var all = CardContainer.Instance.PotionDataList;
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
     if (TutorialController.Instance.HasRunTutorial()== false)
        {
             return available.Find(c=> c.name == "Healing");
        }
        return PickPotionWeightedByRarity(available);
        // // Pick random one
        // PotionCardData selected = available[Random.Range(0, available.Count)];

        // return selected;
    }
    public void AddPotion(PotionEffectType aType)
    {
        if (ActivePotions.Count >= GameManager.Instance.TheHero.myHeroData.PotionSlots)
        {
            Debug.Log("potion slots are full.");
            UIManager.Instance.ShowTooltip("potion slots are full.");

            return;
        }

        var all = CardContainer.Instance.PotionDataList;

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
        if(TutorialController.Instance.LastStepPlayed=="Step2_Shop4_Potion2")
        {
            TutorialController.Instance.ShowNextStep();
        }
        if (ActivePotions.Count >= GameManager.Instance.TheHero.myHeroData.PotionSlots)
        {
            UIManager.Instance.ShowTooltip("potion slots are full.");
            return;
        }


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

    public float GetArtifactValue(PotionEffectType effectType)
    {
        float total = 0;
        foreach (var artifact in ActivePotions)
        {
            if (artifact.effectType == effectType)
                total += artifact.value;
        }
        return total;
    }
    private PotionCardData PickPotionWeightedByRarity(List<PotionCardData> available)
    {
        if (available == null || available.Count == 0)
            return null;

        int rolled = (int)CardContainer.Instance.GetRandomRarity();

        // 1) Try exact rolled rarity
        var pool = available.Where(p => p.rarity == rolled).ToList();
        if (pool.Count > 0)
            return pool[Random.Range(0, pool.Count)];

        // 2) Fallback: go downwards first (feels fair)
        for (int r = rolled - 1; r >= 0; r--)
        {
            pool = available.Where(p => p.rarity == r).ToList();
            if (pool.Count > 0)
                return pool[Random.Range(0, pool.Count)];
        }

        // 3) Fallback: then upwards
        for (int r = rolled + 1; r < 4; r++)
        {
            pool = available.Where(p => p.rarity == r).ToList();
            if (pool.Count > 0)
                return pool[Random.Range(0, pool.Count)];
        }

        // 4) Last fallback
        return available[Random.Range(0, available.Count)];
    }
    private PotionCardData PickPotionByRarity(List<PotionCardData> available,int rarity)
    {
        var pool = available.Where(p => p.rarity == rarity).ToList();
        return pool[Random.Range(0, pool.Count)];
    }

}
