using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardInstance
{
    public CardData data;
    public int currentRank;
    public Card CardGO=null;

    public CardInstance(CardData data)
    {
        this.data = data;
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

    public void UpgradeRank()
    {
        if (currentRank < data.RankUpgrades.Count - 1)
            currentRank++;

        if (CardGO != null)
            CardGO.UpdateCardUI();
    }

}

