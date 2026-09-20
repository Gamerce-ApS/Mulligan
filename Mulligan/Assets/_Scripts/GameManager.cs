using System.Collections;
using System.Collections.Generic;
using GameAnalyticsSDK;
using Singular;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Event = UnityEngine.Event;

public class GameManager : Singleton<GameManager>
{
    public const string FirstDefeatBuyPopupSkippedKey = "FirstDefeatBuyPopupSkipped";
    private const int RoundAfterTutorial = 5;
    public static bool OpenHeroSelectionAfterDefeat = false;

    public enum GameStates
    {
        Loading,
        Pre_Game,
        Game,
        Evaluation,
        Post_Game
    };
    public GameStates myGameStates;

    public Enemy TheEnemy;
    public Hero TheHero;
    public bool DisableBossDebuffNextRound = false;
    public bool BonusAttacksNextRound = false;
    private bool drawFreshHandAfterTutorial = false;

    //Runes Effects
    public int BonusRerolls = 0;
    public int BonusAttacks = 0;
    public bool ReviveFullHP = false;
    public bool ReviveWith1HP = false;
    public float MarketDiscountModifier = 1;
    public float BossGoldMultiplier = 1;
    public float PotionRetriggerChance = 0;
    public bool HasFreeReroll = false;

    public List<TMPro.TMP_SpriteAsset> TextSprites = new List<TMPro.TMP_SpriteAsset>();
    public bool ShowDailyQuestDebugButtons = true;


    // Start is called before the first frame update
    void Start()
    {

        myGameStates = GameStates.Loading;
        TrackDesign("boot:data_load:request");
        GameDataLoader.Instance.LoadGameData(() =>
        {

            TrackDesign("boot:data_load:complete");
            TrackDesign("boot:managers_init:start");
            Application.targetFrameRate = 60;
            GameData.UnlockProgressForThisRun = GameData.CompletedFirstBossAmount;
            GameData.FirstBossCompletedThisRun = 0;
            CardContainer.Instance.Init();
            HandManager.Instance.Init();
            UIManager.Instance.Init();
            SettingsManager.Instance.Init();
            UnitUpgradeManager.Instance.Init();
            DeckOverviewManager.Instance.Init();
            UnlockManager.Instance.Init();
            InventoryOverviewManager.Instance.Init();
            HeroInfoScreen.Instance.Init();
            LocalNotificationManager.Instance.Init();
            HighscoreManager.Instance.Init();
            DailyQuestManager.Instance.Init();
            ShopManager.Instance.PopulateShop();
            TrackDesign("boot:managers_init:complete");
            TrackDesign("boot:start_game");
            StartGame();

        });
        SingularSDK.Event("StartEvent");
        if (GameAnalytics.Initialized == false)
            GameAnalytics.Initialize();
        TrackDesign("app:start");

        

    }
    public TMPro.TMP_SpriteAsset GetTextSpriteForSprite(string aSpriteName)
    {
        foreach (var e in TextSprites)
        {
            if (e.name == aSpriteName) return e;
        }
        return null;
    }
    public void StartGame()
    {
        TheHero.Init(100);
        GameData.CurrentGold = CardContainer.Instance.StatingGold;
        GameData.CurrentAttacks = 4;
        GameData.CurrentReRolls = 2;
        GameData.CurrentRound = 1;
        GameData.UnlockProgressForThisRun = GameData.CompletedFirstBossAmount;
        GameData.FirstBossCompletedThisRun = 0;
        GameData.SkippedLevels = 0;
        GameData.PotionsUsed = 0;
        GameData.UpgradedUnits = 0;

        if (TutorialController.Instance.HasRunTutorial() == false)
        {
            TrackDesign("boot:start_game:tutorial_required");
            StartTutorialGameDirect();
            return;
        }

        if (OpenHeroSelectionAfterDefeat)
        {
            OpenHeroSelectionAfterDefeat = false;
            UIManager.Instance.SplashScreen.SetActive(false);
            SoundManager.TryPlayMenuMusic();
            ShowHeroSelection();
            ShowBuyPopupAfterDefeatIfNeeded();
            return;
        }

        //  #if UNITY_EDITOR
        //      UIManager.Instance.ClickPlayFullGame();
        //      PlayerPrefs.SetInt(IAPManager.FullGameUnlockedKey, 1);
        //      #else
        UIManager.Instance.SplashScreen.SetActive(true);
        TrackDesign("boot:start_game:splash");

        if(IAPManager.Instance.IsFullGameUnlocked)// owns game
        {
            UIManager.Instance.SplashScreenButtons[0].SetActive(true);
            UIManager.Instance.SplashScreenButtons[1].SetActive(true);
            UIManager.Instance.SplashScreenButtons[2].SetActive(false); 
        }else // trial
        {
            UIManager.Instance.SplashScreenButtons[0].SetActive(true);
            UIManager.Instance.SplashScreenButtons[1].SetActive(true);
            UIManager.Instance.SplashScreenButtons[2].SetActive(false); 
        }
        //   #endif
        SoundManager.TryPlayMenuMusic();
    }
    private void StartTutorialGameDirect()
    {
        TrackDesign("boot:tutorial_direct:start");
        GameData.HeroSelected = 0;
        UIManager.Instance.SplashScreen.SetActive(false);
        myGameStates = GameStates.Pre_Game;
        TheHero.Init(CardContainer.Instance.HeroDataList[GameData.HeroSelected]);
        HighscoreManager.Instance.StartRun(GameData.HeroSelected);
        TrackRunStart();
        AnalyticsService.Instance.RecordEvent("Started_Game_With_Hero"+GameData.HeroSelected);
        GameAnalytics.NewDesignEvent("run:start:hero_" + GameData.HeroSelected);
        TrackDesign("boot:tutorial_direct:level_selection_open");
        LevelSelectionManager.Instance.ShowWindow(() =>
        {
            SoundManager.TryPlayCombatMusic();
            TheEnemy.Init(GameData.CurrentRound);
            myGameStates = GameStates.Game;
            TrackDesign("boot:tutorial_direct:combat_ready");
            TutorialController.Instance.StartTutorial();
        });
    }
    private void ShowBuyPopupAfterDefeatIfNeeded()
    {
        if (IAPManager.Instance.IsFullGameUnlocked)
            return;

        if (PlayerPrefs.GetInt(FirstDefeatBuyPopupSkippedKey, 0) == 0)
        {
            PlayerPrefs.SetInt(FirstDefeatBuyPopupSkippedKey, 1);
            PlayerPrefs.Save();
            GameAnalytics.NewDesignEvent("paywall:first_loss_skipped");
            return;
        }

        UnityHelper.RunAfterDelay(this, 0.5f, () =>
        {
            UIManager.Instance.ClickBuyPopupWindow();
        });
    }
    public void ShowHeroSelection()
    {
        HeroSelectionManager.Instance.ShowWindow(() =>
        {
     
        });
    }
    public void RunPreGameSetup()
    {
        myGameStates = GameStates.Pre_Game;
        TheHero.Init(CardContainer.Instance.HeroDataList[GameData.HeroSelected]);
        HighscoreManager.Instance.StartRun(GameData.HeroSelected);
        TrackRunStart();
        AnalyticsService.Instance.RecordEvent("Started_Game_With_Hero"+GameData.HeroSelected);
        GameAnalytics.NewDesignEvent("run:start:hero_" + GameData.HeroSelected);
        LevelSelectionManager.Instance.ShowWindow(() =>
        {
            SoundManager.TryPlayCombatMusic();
            TheEnemy.Init(GameData.CurrentRound);
            GameManager.Instance.myGameStates = GameManager.GameStates.Game;

            if (GameManager.Instance.BonusAttacksNextRound)
            {
                GameData.CurrentAttacks += 2;
                GameManager.Instance.BonusAttacksNextRound = false;
            }
            if (TutorialController.Instance.HasRunTutorial() == false)
                TutorialController.Instance.StartTutorial();
        });
    }
    public void WinGame(RoundRewardResult roundRewardResult = null)
    {
        if (roundRewardResult == null)
            roundRewardResult = new RoundRewardResult();

        VibrationsManager.TryVibrate(VibrationType.Success);
        AnalyticsService.Instance.RecordEvent("WonRound_"+GameData.CurrentRound);
        GameAnalytics.NewDesignEvent("round:win:" + GameData.CurrentRound);
        GameAnalytics.NewDesignEvent("round:win", GameData.CurrentRound);
        RateAppManager.Instance.RegisterWinAndMaybeRequestReview();
        bool completedTutorialThisRound = TutorialController.Instance.HasRunTutorial() == false && GameData.CurrentRound % 4 == 0;
        if (GameData.CurrentRound == 4 && GameData.FirstBossCompletedThisRun == 0)
        {
            GameData.CompletedFirstBossAmount++;
            GameData.FirstBossCompletedThisRun = 1;
            PlayerPrefs.Save();
        }
        GameData.CurrentGold = Mathf.RoundToInt(((float)GameData.CurrentGold * CardContainer.Instance.GoldInflation)); //TODO. Interest is based on even numbers.
        int goldGained = 0;
        if (GameData.CurrentRound % 4 == 0)
        {
            goldGained += (int)(CardContainer.Instance.GoldGainPerLevel * GameManager.Instance.BossGoldMultiplier);
            DailyQuestManager.Instance.AddProgress(DailyQuestType.DefeatBosses);
            CardContainer.Instance.CompleteBoss();
            if(TutorialController.Instance.HasRunTutorial() == false)
            {
                PlayerPrefs.SetInt("HasRunTutorial", 1);
                if (LocalNotificationManager.Instance != null)
                    LocalNotificationManager.Instance.RequestPermissionAfterTutorial();
                CardContainer.Instance.ResetDeckAfterTutorial();
                RequestFreshHandAfterTutorial();
                AnalyticsService.Instance.RecordEvent("tutorial_finished");
                GameAnalytics.NewDesignEvent("tutorial:finished");
                GameAnalytics.NewDesignEvent("tutorial:continued_to_run");
                GameData.CurrentRunStartedAfterTutorial = 1;
            }

    

        }
        else
        {
            goldGained += CardContainer.Instance.GoldGainPerLevel;
        }
        if (RuneManager.Instance.ActiveRunes.Find(c => c.type == RuneType.RuneOfGold) != null)
        {
            goldGained += 2;
        }
        if (RuneManager.Instance.ActiveRunes.Find(c => c.type == RuneType.RuneOfGold2X) != null)
        {
            goldGained += 5;
        }
        AddGold(goldGained);
        int goldGainedThisRound = goldGained + roundRewardResult.GoldGained;

        GameData.CurrentAttacks = 4 + TheHero.GetAttackModifier();
        GameData.CurrentReRolls = 2 + TheHero.GetRollsModifier();
        if (completedTutorialThisRound)
        {
            GameData.CurrentRound = RoundAfterTutorial;
        }
        else
        {
            GameData.CurrentRound++;
            if(TutorialController.Instance.HasRunTutorial() == false)
            {
                GameData.CurrentRound++;
                GameData.CurrentRound++;
            }
        }
        DailyQuestManager.Instance.SetProgressIfHigher(DailyQuestType.ReachLevel, GameData.CurrentRound);
        HighscoreManager.Instance.UpdateMaxLevel(GameData.CurrentRound);
        LeanTween.delayedCall(gameObject, 1f, () =>
        {
            myGameStates = GameStates.Post_Game;
            UIManager.Instance.ShowVictoryScreen(goldGainedThisRound, roundRewardResult.HealthGained, () =>
            {
                ArmoryManager.Instance.ShowWindow(() =>
                {
                    TheEnemy.gameObject.SetActive(false);
                    ArcCardLayout.Instance.transform.gameObject.SetActive(false);
                    ShopManager.Instance.ShowShopWindow(() =>
                    {
                        LevelSelectionManager.Instance.ShowWindow(() =>
                        {
                            SoundManager.TryPlayCombatMusic();
                            ArcCardLayout.Instance.transform.gameObject.SetActive(true);
                            TheEnemy.gameObject.SetActive(true);
                            TheEnemy.Init(GameData.CurrentRound);
                            DrawFreshHandAfterTutorialIfNeeded();
                            EvaluatorManager.Instance.StartLevel();
                            GameManager.Instance.myGameStates = GameManager.GameStates.Game;

                        });

                    });

                });

     

            });
        });


    }
    public void LostGame()
    {
        VibrationsManager.TryVibrate(VibrationType.Error);
        AnalyticsService.Instance.RecordEvent("LostRound_"+GameData.CurrentRound);
        GameAnalytics.NewDesignEvent("round:loss:" + GameData.CurrentRound);
        GameAnalytics.NewDesignEvent("round:loss", GameData.CurrentRound);
        TrackRunEnd();
        HighscoreManager.Instance.SubmitCurrentRun();


        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            if (ArtifactManager.Instance.IsArtifactMutedByBoss(artifact))
                continue;

            if (artifact.effect == ArtifactEffectType.GoldOnLose &&
            GameData.CurrentAttacks <= 0 &&
            GameManager.Instance.TheEnemy.Health > 0)
            {
                AddGold(artifact.value);
                UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.gold_from_artifact", "+{0} Gold from artifact", artifact.value));
            }
        }

        LeanTween.delayedCall(gameObject, 0.5f, () =>
        {
            UIManager.Instance.ShowLoseScreen(() =>
            {
                LeanTween.delayedCall(gameObject, 2f, () =>
                {
                        if (UnlockManager.Instance.HasUnlocksToReveal())
                            UnlockManager.Instance.ShowWindow();
                });
            });

        });

        if(TutorialController.Instance.HasRunTutorial() == false)
        {
            AnalyticsService.Instance.RecordEvent("LostGameInTutorial");
            GameAnalytics.NewDesignEvent("tutorial:lost");

        }

    }

    private void TrackRunStart()
    {
        GameData.TotalRunsStarted++;
        GameData.CurrentRunNumber = GameData.TotalRunsStarted;
        GameData.CurrentRunStartedAfterTutorial = TutorialController.Instance.HasRunTutorial() ? 1 : 0;
        PlayerPrefs.Save();

        GameAnalytics.NewDesignEvent("run:start", GameData.CurrentRunNumber);
        GameAnalytics.NewDesignEvent("run:start:number_" + GameData.CurrentRunNumber);
    }

    private void TrackRunEnd()
    {
        GameData.TotalRunsFinished++;
        PlayerPrefs.Save();

        GameAnalytics.NewDesignEvent("run:end", GameData.CurrentRound);
        GameAnalytics.NewDesignEvent("run:end:number_" + GameData.CurrentRunNumber);

        if (GameData.CurrentRunNumber == 1)
            GameAnalytics.NewDesignEvent("run:end:tutorial_first_run", GameData.CurrentRound);
        else
            GameAnalytics.NewDesignEvent("run:end:normal", GameData.CurrentRound);
    }

    private void TrackDesign(string eventName)
    {
        if (GameAnalytics.Initialized == false)
            GameAnalytics.Initialize();

        GameAnalytics.NewDesignEvent(eventName);
    }

    public void RequestFreshHandAfterTutorial()
    {
        drawFreshHandAfterTutorial = true;
    }

    private void DrawFreshHandAfterTutorialIfNeeded()
    {
        if (drawFreshHandAfterTutorial == false)
            return;

        drawFreshHandAfterTutorial = false;
        HandManager.Instance.DrawHand();
        HandManager.Instance.HandleMutedCards();
    }

    public void FinishRound()
    {
        GameData.CurrentAttacks--;
        RoundRewardResult roundRewardResult = EvaluatorManager.Instance.FinisLevel();
        myGameStates = GameStates.Game;


        if (TheEnemy.Health <= 0)
        {
            WinGame(roundRewardResult);
        }
        else if (GameData.CurrentAttacks <= 0 && TutorialController.Instance.HasRunTutorial() == true)
        {
            LostGame();
        }
        else
        {
            TheEnemy.Attack(0);

        }
        UnityHelper.RunAfterDelay(this, 0.5f, () =>
        {
                HandManager.Instance.HandleMutedCards();  
        });
  
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Y))
        {
            DebugProgressDailyQuest();
        }

        if (Input.GetKeyUp(KeyCode.S))
        {
            ArtifactManager.Instance.AddRandomArtifact();
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            ArtifactManager.Instance.AddArtifact(ArtifactEffectType.CritPerUpgradedUnit);
        }
        if (Input.GetKeyUp(KeyCode.O))
        {
            ArtifactManager.Instance.AddArtifact(ArtifactEffectType.RaceHasExtraDamage);
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            //HandManager.Instance.RankUpRandom();
            HandManager.Instance.GiveRandomUpgrade();

        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            TheHero.Attack(500);
            GameManager.Instance.FinishRound();
        }
        // if (Input.GetKeyUp(KeyCode.I))
        // {
        //     TheHero.Attack(TheEnemy.Health-1);
        // }
        if (Input.GetKeyUp(KeyCode.Q))
        {
            TheEnemy.Attack(25);
        }
        if (Input.GetKeyUp(KeyCode.G))
        {
            AddGold(100);
        }
        if (Input.GetKeyUp(KeyCode.K))
        {
            UIManager.Instance.PlayDeckPileDrawAnimation();
        }
        if (Input.GetKeyUp(KeyCode.X))
        {
            ShopManager.Instance.ShowShopWindow();
        }
        if (Input.GetKeyUp(KeyCode.Z))
        {
            ArmoryManager.Instance.ShowWindow();
        }
        if (Input.GetKeyUp(KeyCode.C))
        {
            LevelSelectionManager.Instance.ShowWindow();
        }
        if (Input.GetKeyUp(KeyCode.V))
        {
            UnitUpgradeManager.Instance.ShowWindow();
        }
        if (Input.GetKeyUp(KeyCode.U))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                DebugResetDailyQuests();
            else
                DebugUnlockAllArtifacts();
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            DeckOverviewManager.Instance.ShowWindow();
        }
        if (Input.GetKeyUp(KeyCode.F))
        {
            PotionManager.Instance.AddRandomPotion();
        }
        if (Input.GetKeyUp(KeyCode.P))
        {
            HeroInfoScreen.Instance.ShowWindow();
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
                    GameData.CompletedFirstBossAmount++;
        GameData.FirstBossCompletedThisRun = 1;
            LostGame();
        }
        


    }

    void OnGUI()
    {
        // if (ShowDailyQuestDebugButtons == false)
        //     return;

        // if (DailyQuestManager.Instance == null ||
        //     DailyQuestManager.Instance.ShopWindow == null ||
        //     DailyQuestManager.Instance.ShopWindow.activeSelf == false)
        //     return;

        // int width = 340;
        // int height = 80;
        // int padding = 20;
        // int y = 120;

        // GUI.skin.button.fontSize = 28;

        // if (GUI.Button(new Rect(padding, y, width, height), "Daily Quest +"))
        // {
        //     DailyQuestManager.Instance.SuppressHideForDebugClick();
        //     DebugProgressDailyQuest();
        //     DailyQuestManager.Instance.UpdateUI();
        //     Event.current.Use();
        // }

        // if (GUI.Button(new Rect(padding, y + height + padding, width, height), "New Daily Quests"))
        // {
        //     DailyQuestManager.Instance.SuppressHideForDebugClick();
        //     DebugResetDailyQuests();
        //     DailyQuestManager.Instance.UpdateUI();
        //     Event.current.Use();
        // }
    }

    private void DebugProgressDailyQuest()
    {
        DailyQuestManager.Instance.DebugProgressActiveQuest(Random.Range(0, 3));
    }

    private void DebugResetDailyQuests()
    {
        DailyQuestManager.Instance.DebugResetDailyQuests();
    }

    private void DebugUnlockAllArtifacts()
    {
        int highestUnlockRun = GameData.CompletedFirstBossAmount;

        if (CardContainer.Instance.ArtifactDataList != null)
        {
            foreach (var artifact in CardContainer.Instance.ArtifactDataList)
            {
                if (artifact != null && artifact.UnlockRun > highestUnlockRun)
                    highestUnlockRun = artifact.UnlockRun;
            }
        }

        GameData.CompletedFirstBossAmount = highestUnlockRun;
        GameData.UnlockProgressForThisRun = highestUnlockRun;

        if (DailyQuestManager.Instance != null)
            DailyQuestManager.Instance.DebugUnlockAllArtifactRewards();

        PlayerPrefs.Save();

        if (InventoryOverviewManager.Instance != null &&
            InventoryOverviewManager.Instance.ShopWindow != null &&
            InventoryOverviewManager.Instance.ShopWindow.activeSelf)
        {
            InventoryOverviewManager.Instance.PopulateInventory();
        }

        UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.all_artifacts_unlocked", "All artifacts unlocked"));
    }

    public bool AreArtifactsMutedByBoss()
    {
        return TheEnemy != null &&
               TheEnemy.ActiveAbbilities.Contains(BossAbilityEnum.Disable2Artifacts) &&
               (myGameStates == GameStates.Game || myGameStates == GameStates.Evaluation);
    }

    // Debug functions
    public void AddGold(int aValue)
    {
        if (aValue <= 0)
            return;

        GameData.CurrentGold += aValue;
        SoundManager.TryPlay(SoundType.Gold);
        DailyQuestManager.Instance.AddProgress(DailyQuestType.EarnGold, aValue);
    }
    public void DisableBossDebuffForTurn()
    {
        GameData.BossDebuffDisabledThisTurn = 1;
    }
}
