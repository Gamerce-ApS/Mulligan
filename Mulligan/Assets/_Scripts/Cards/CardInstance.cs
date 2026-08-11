using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
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

    public bool isMuted = false;

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
    public bool IsSpecial()
    {
        if(tempDamageBonus>0 || tempCritBonus>0 || WillExplodeAfterAttack || IsFacelessThisTurn || appliedUpgrades.Count>0)
    return true;

        return false;
    }
    public void SetMuted(bool mute)
    {
        isMuted = mute;
        CardGO.mutedGO.SetActive(isMuted);
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
        {
            currentRank++;
            SoundManager.TryPlay(SoundType.RankUp);
        }

        if (CardGO != null)
            CardGO.UpdateCardUI();
    }

    public void ApplyUpgrade(UpgradeCardData upgrade)
    {
        if (upgrade.effect == UpgradeEffect.RankUpgrade_Normal)
        {
            for(int i = 0; i < upgrade.value;i++)
            UpgradeRank();
            return;
        }
       
        if (!appliedUpgrades.Contains(upgrade))
        {
            appliedUpgrades.Add(upgrade);
        }

        if (CardGO != null)
            CardGO.UpdateCardUI();

  
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
    public bool GetIsAnyClass()
    {
        if (IsFacelessThisTurn)
            return true;

        foreach (var upgrade in appliedUpgrades)
        {
            if (upgrade.effect == UpgradeEffect.Enchantment_PlusOneClass)
                return true;
        }


        return false;
    }
    public bool GetIsAnyRace()
    {
        foreach (var upgrade in appliedUpgrades)
        {
            if (upgrade.effect == UpgradeEffect.Enchantment_Changeling)
                return true;
        }
        return false;
    }
    public void BecomeFacelessThisTurn()
    {
        IsFacelessThisTurn = true;
        if (CardGO != null)
        {
            // Optional: visual cue
            LeanTween.scale(CardGO.gameObject, Vector3.one * 1.15f, 0.3f).setEasePunch();
        }
        CardGO.AnyClass.SetActive(true);
    }
    public void Destroy()
    {
        HandManager.Instance.CurrentHand.Remove(this);
        HandManager.Instance.PlayedHand.Remove(this);   
        CardContainer.Instance.DiscardDeck.Remove(this);
        CardContainer.Instance.CurrentDeck.Remove(this);
        GameObject.Destroy(CardGO.gameObject);
    }
    public void TurnEnded(System.Action onComplete)
    {
        tempCritBonus = 0;
        tempDamageBonus = 0;
        if(IsFacelessThisTurn)
        {
            CardGO.AnyClass.SetActive(false);
            IsFacelessThisTurn = false;
        }
  
        CardGO.UpdateCardUI();
        onComplete?.Invoke();

        // if (WillExplodeAfterAttack)
        // {
        //     // Destroy
        //     currentRank = 0;
        //     appliedUpgrades.Clear();
        //     if(CardGO != null)
        //     UnityHelper.RunAfterDelay(CardGO, 1.5f, () =>
        //     {
        //         GameObject.Destroy(CardGO);
        //         CardContainer.Instance.DiscardDeck.Remove(this);
        //         CardContainer.Instance.CurrentDeck.Remove(this);
                

        //     });
     

        // }
    }

}
