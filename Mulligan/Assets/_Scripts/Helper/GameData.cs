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
    public static int SkippedLevels
    {
        get { return PlayerPrefs.GetInt("SkippedLevels", 0); }
        set { PlayerPrefs.SetInt("SkippedLevels", value); UIManager.Instance.UpdateLabels(); }
    }
    public static int UpgradedUnits
    {
        get { return PlayerPrefs.GetInt("UpgradedUnits", 0); }
        set { PlayerPrefs.SetInt("UpgradedUnits", value); UIManager.Instance.UpdateLabels(); }
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
    public static int HeroSelected
    {
        get { 
            if(TutorialController.Instance.HasRunTutorial() ==false) return 0;
            return PlayerPrefs.GetInt("HeroSelected", 1);
             }
        set { PlayerPrefs.SetInt("HeroSelected", value); UIManager.Instance.UpdateLabels(); }
    }
    public static int CompletedFirstBossAmount
    {
        get { return PlayerPrefs.GetInt("CompletedFirstBossAmount", 0); }
        set { PlayerPrefs.SetInt("CompletedFirstBossAmount", value); }
    }
    public static int UnlockProgressForThisRun
    {
        get { return PlayerPrefs.GetInt("UnlockProgressForThisRun", 0); }
        set { PlayerPrefs.SetInt("UnlockProgressForThisRun", value); }
    }
    public static int FirstBossCompletedThisRun
    {
        get { return PlayerPrefs.GetInt("FirstBossCompletedThisRun", 0); }
        set { PlayerPrefs.SetInt("FirstBossCompletedThisRun", value); }
    }
    public static long DailyQuestNextResetUtcTicks
    {
        get { return long.Parse(PlayerPrefs.GetString("DailyQuestNextResetUtcTicks", "0")); }
        set { PlayerPrefs.SetString("DailyQuestNextResetUtcTicks", value.ToString()); }
    }
    public static int CompletedQuestsTowardsReward
    {
        get { return PlayerPrefs.GetInt("CompletedQuestsTowardsReward", 0); }
        set { PlayerPrefs.SetInt("CompletedQuestsTowardsReward", value); }
    }
    public static int DailyQuestArtifactRewardIndex
    {
        get { return PlayerPrefs.GetInt("DailyQuestArtifactRewardIndex", 0); }
        set { PlayerPrefs.SetInt("DailyQuestArtifactRewardIndex", value); }
    }




}
