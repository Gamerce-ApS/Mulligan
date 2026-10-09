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
    public int permanentDamageBonus = 0;
    public bool WillExplodeAfterAttack = false;
    public bool IsFacelessThisTurn = false;

    public bool isMuted = false;
    [System.NonSerialized] private bool isDestroyed = false;

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
        if(tempDamageBonus>0  || tempCritBonus>0 || WillExplodeAfterAttack || IsFacelessThisTurn || appliedUpgrades.Count>0)
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
        int rankBaseDamage = GetRankBaseDamage();
        int damage = rankBaseDamage + tempDamageBonus + permanentDamageBonus;
        return damage * GameData.GlobalDamageMultiplier;
    }

    private int GetRankBaseDamage()
    {
        if (data == null)
            return 0;

        if (currentRank <= 0 || data.RankUpgrades == null || data.RankUpgrades.Count == 0)
            return data.damage;

        int rankIndex = Mathf.Clamp(currentRank - 1, 0, data.RankUpgrades.Count - 1);
        return data.RankUpgrades[rankIndex];
    }
    public void AddPermanentDamage(int amount)
    {
        if (amount <= 0)
            return;

        permanentDamageBonus += amount;
        if (CardGO != null)
            CardGO.UpdateCardUI();
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
    
    public void UpgradeRank(int amount = 1)
    {
        if (data == null || amount <= 0 || data.RankUpgrades == null || data.RankUpgrades.Count == 0)
            return;

        int originalRank = currentRank;
        int previousRank = Mathf.Clamp(currentRank, 0, data.RankUpgrades.Count);
        int newRank = Mathf.Min(previousRank + amount, data.RankUpgrades.Count);
        currentRank = newRank;

        if (newRank <= previousRank)
        {
            if (originalRank != currentRank && CardGO != null)
                CardGO.UpdateCardUI();
            return;
        }

        SoundManager.TryPlay(SoundType.RankUp);

        if (CardGO != null)
            CardGO.UpdateCardUI();
    }

    public void ApplyUpgrade(UpgradeCardData upgrade)
    {
        if (upgrade.effect == UpgradeEffect.RankUpgrade_Normal)
        {
            UpgradeRank(upgrade.value);
            return;
        }
       
        if (!appliedUpgrades.Contains(upgrade))
        {
            appliedUpgrades.Add(upgrade);
        }

        if (CardGO != null)
            CardGO.UpdateCardUI();

  
    }

    public CardInstance CreateCopyForDeck()
    {
        CardInstance copy = new CardInstance(data);
        copy.currentRank = currentRank;
        copy.permanentDamageBonus = permanentDamageBonus;
        copy.appliedUpgrades = new List<UpgradeCardData>(appliedUpgrades);
        return copy;
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
    public void Destroy(bool destroyVisual = true)
    {
        if (isDestroyed)
            return;

        if (destroyVisual && CardGO != null)
        {
            CardRenderTextureCapture textureCapture = UnityEngine.Object.FindObjectOfType<CardRenderTextureCapture>();
            if (textureCapture != null)
                textureCapture.CaptureAndPlayDestroy(CardGO);
        }

        isDestroyed = true;
        DailyQuestManager.Instance.AddProgress(DailyQuestType.DestroyUnits);
        HandManager.Instance.CurrentHand.Remove(this);
        HandManager.Instance.PlayedHand.Remove(this);   
        CardContainer.Instance.DiscardDeck.Remove(this);
        CardContainer.Instance.CurrentDeck.Remove(this);
        CardContainer.Instance.TutorialDeck.Remove(this);

        ArtifactManager.Instance.OnUnitDestroyed(this);

        if (CardGO != null)
        {
            if (destroyVisual)
                GameObject.Destroy(CardGO.gameObject);
            CardGO = null;
        }
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
