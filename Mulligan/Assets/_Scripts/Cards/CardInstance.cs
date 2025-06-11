using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardInstance
{
    public CardData data=null;
    public UpgradeCardData upgradeData=null;
    public int currentRank;
    public Card CardGO=null;
    public List<UpgradeCardData> appliedUpgrades = new List<UpgradeCardData>();

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
            return data.damage;

        if (data.RankUpgrades != null && currentRank-1 < data.RankUpgrades.Count)
            return data.RankUpgrades[currentRank-1];
        return data.damage;
    }
    public int GetUpgradeDamageBonus()
    {
        return 0;
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

}

