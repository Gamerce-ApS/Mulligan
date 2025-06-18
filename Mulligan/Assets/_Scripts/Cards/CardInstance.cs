using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardInstance
{
    public CardData data=null;
    public UpgradeCardData upgradeData=null;
    public PotionCardData potionData=null; // ADD THIS
    public int currentRank;
    public Card CardGO=null;
    public List<UpgradeCardData> appliedUpgrades = new List<UpgradeCardData>();

    public int tempCritBonus = 0;
    public int tempDamageBonus = 0;
    public bool WillExplodeAfterAttack = false;
    public bool IsFacelessThisTurn = false;

    public CardInstance(CardData data)
    {
        this.data = data;
        this.currentRank = 0;
    }
    public CardInstance(UpgradeCardData data)
    {
        this.upgradeData = data;
        this.currentRank = 0;
    }
    public int GetDamage()
    {
        if(currentRank == 0)
            return (data.damage+ tempDamageBonus) * GameData.GlobalDamageMultiplier;

        if (data.RankUpgrades != null && currentRank-1 < data.RankUpgrades.Count)
            return (data.RankUpgrades[currentRank-1]+ tempDamageBonus) * GameData.GlobalDamageMultiplier;
        return data.damage * GameData.GlobalDamageMultiplier;
    }
    //public int GetDamageBonus()
    //{
    //    return tempDamageBonus;
    //}
    public int GetCritBonus()
    {
        return tempCritBonus;
    }
    public int GetUpgradeCritBonus()
    {
        foreach (var upgrade in appliedUpgrades)
        {
            switch (upgrade.effect)
            {
                case UpgradeEffect.Enchantment_Crit:
                    return upgrade.value;
            }
        }
        return 0;
    }
    public int GetUpgradeGold()
    {
        foreach (var upgrade in appliedUpgrades)
        {
            switch (upgrade.effect)
            {
                case UpgradeEffect.Charms_Gold:
                    return upgrade.value;
            }
        }
        return 0;
    }
    
    public void UpgradeRank()
    {
        if (currentRank < data.RankUpgrades.Count - 1)
            currentRank++;

        if (CardGO != null)
            CardGO.UpdateCardUI();
    }

    public void ApplyUpgrade(UpgradeCardData upgrade)
    {
        if (upgrade.effect == UpgradeEffect.RankUpgrade_Normal)
        {
            UpgradeRank();
            return;
        }
        if (!appliedUpgrades.Contains(upgrade))
        {
            appliedUpgrades.Add(upgrade);
        }
    }

    public void EvaluateUpgrades(System.Action onComplete)
    {
        foreach (var upgrade in appliedUpgrades)
        {
            switch (upgrade.effect)
            {
                case UpgradeEffect.Enchantment_LifeSteal:
                    GameManager.Instance.TheHero.CurrentLifeStealProc += upgrade.value;
                    break;
            }
        }

        onComplete.Invoke();
    }
    public void BecomeFacelessThisTurn()
    {
        IsFacelessThisTurn = true;
        //if (CardGO != null)
        //{
        //    // Optional: visual cue
        //    LeanTween.scale(CardGO.gameObject, Vector3.one * 1.15f, 0.3f).setEasePunch();
        //}
    }
    public void TurnEnded(System.Action onComplete)
    {
        tempCritBonus = 0;
        tempDamageBonus = 0;
        IsFacelessThisTurn = false;
        if(WillExplodeAfterAttack)
        {
            // Destroy
            currentRank = 0;
            appliedUpgrades.Clear();
        }
        onComplete?.Invoke();
    }

}