using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData
{
    public static int CurrentGold
    {
        get { return PlayerPrefs.GetInt("CurrentGold", 0); }
        set { PlayerPrefs.SetInt("CurrentGold", value); UIManager.Instance.UpdateLabels(); }
    }
    public static int CurrentRound
    {
        get { return PlayerPrefs.GetInt("CurrentRound", 0); }
        set { PlayerPrefs.SetInt("CurrentRound", value); UIManager.Instance.UpdateLabels(); }
    }
    public static int CurrentAttacks
    {
        get { return PlayerPrefs.GetInt("CurrentAttacks", 0); }
        set { PlayerPrefs.SetInt("CurrentAttacks", value); UIManager.Instance.UpdateLabels(); }
    }
    public static int CurrentReRolls
    {
        get { return PlayerPrefs.GetInt("CurrentReRolls", 0); }
        set { PlayerPrefs.SetInt("CurrentReRolls", value); UIManager.Instance.UpdateLabels(); }
    }
    public static int PotionsUsed
    {
        get { return PlayerPrefs.GetInt("PotionsUsed", 0); }
        set { PlayerPrefs.SetInt("PotionsUsed", value); UIManager.Instance.UpdateLabels(); }
    }

    public static int CurrentArmySize
    {
        get { return PlayerPrefs.GetInt("CurrentArmySize", 0); }
        set { PlayerPrefs.SetInt("CurrentArmySize", value); UIManager.Instance.UpdateLabels(); }
    }
    public static int BossDebuffDisabledThisTurn
    {
        get { return PlayerPrefs.GetInt("BossDebuffDisabledThisTurn", 0); }
        set { PlayerPrefs.SetInt("BossDebuffDisabledThisTurn", value); UIManager.Instance.UpdateLabels(); }
    }
    public static int GlobalDamageMultiplier
    {
        get { return PlayerPrefs.GetInt("GlobalDamageMultiplier", 1); }
        set { PlayerPrefs.SetInt("GlobalDamageMultiplier", value); UIManager.Instance.UpdateLabels(); }
    }
    
}
