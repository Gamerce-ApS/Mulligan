using System.Collections;
using System.Collections.Generic;
using Singular;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
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


    // Start is called before the first frame update
    void Start()
    {

        myGameStates = GameStates.Loading;
        GameDataLoader.Instance.LoadGameData(() =>
        {

            Application.targetFrameRate = 60;
            GameData.UnlockProgressForThisRun = GameData.CompletedFirstBossAmount;
            GameData.FirstBossCompletedThisRun = 0;
            CardContainer.Instance.Init();
            HandManager.Instance.Init();
            UIManager.Instance.Init();
            UnitUpgradeManager.Instance.Init();
            DeckOverviewManager.Instance.Init();
            UnlockManager.Instance.Init();
            ShopManager.Instance.PopulateShop();
            StartGame();

        });
        SingularSDK.Event("StartEvent");


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

        //  #if UNITY_EDITOR
        //      UIManager.Instance.ClickPlayFullGame();
        //      PlayerPrefs.SetInt(IAPManager.FullGameUnlockedKey, 1);
        //      #else
        UIManager.Instance.SplashScreen.SetActive(true);

        if(IAPManager.Instance.IsFullGameUnlocked)// owns game
        {
            UIManager.Instance.SplashScreenButtons[0].SetActive(true);
            UIManager.Instance.SplashScreenButtons[1].SetActive(false);
            UIManager.Instance.SplashScreenButtons[2].SetActive(false); 
        }else // trial
        {
            UIManager.Instance.SplashScreenButtons[0].SetActive(false);
            UIManager.Instance.SplashScreenButtons[1].SetActive(true);
            UIManager.Instance.SplashScreenButtons[2].SetActive(true); 
        }
        //   #endif
    }
    public void ShowHeroSelection()
    {
        HeroSelectionManager.Instance.ShowWindow(() =>
        {
            myGameStates = GameStates.Pre_Game;
            TheHero.Init(CardContainer.Instance.HeroDataList[GameData.HeroSelected]);
            AnalyticsService.Instance.RecordEvent("Started_Game_With_Hero"+GameData.HeroSelected);
            LevelSelectionManager.Instance.ShowWindow(() =>
            {
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
        });
    }
    public void WinGame()
    {
        VibrationsManager.TryVibrate(VibrationType.Success);
        AnalyticsService.Instance.RecordEvent("WonRound_"+GameData.CurrentRound);
        RateAppManager.Instance.RegisterWinAndMaybeRequestReview();
        if (GameData.CurrentRound == 4 && GameData.FirstBossCompletedThisRun == 0)
        {
            GameData.CompletedFirstBossAmount++;
            GameData.FirstBossCompletedThisRun = 1;
            PlayerPrefs.Save();
        }
        GameData.CurrentGold = Mathf.RoundToInt(((float)GameData.CurrentGold * CardContainer.Instance.GoldInflation)); //TODO. Interest is based on even numbers.

        if (GameData.CurrentRound % 4 == 0)
        {
            GameData.CurrentGold += (int)(CardContainer.Instance.GoldGainPerLevel * GameManager.Instance.BossGoldMultiplier);
                    CardContainer.Instance.CompleteBoss();

        }
        else
        {
            GameData.CurrentGold += CardContainer.Instance.GoldGainPerLevel;
        }
        if (RuneManager.Instance.ActiveRunes.Find(c => c.type == RuneType.RuneOfGold) != null)
        {
            GameData.CurrentGold += 2;
        }
        if (RuneManager.Instance.ActiveRunes.Find(c => c.type == RuneType.RuneOfGold2X) != null)
        {
            GameData.CurrentGold += 5;
        }

        GameData.CurrentAttacks = 4 + TheHero.GetAttackModifier();
        GameData.CurrentReRolls = 2 + TheHero.GetRollsModifier();
        GameData.CurrentRound++;
        HighscoreManager.Instance.UpdateMaxLevel(GameData.CurrentRound);
        LeanTween.delayedCall(gameObject, 1f, () =>
        {
            myGameStates = GameStates.Post_Game;
            UIManager.Instance.ShowVictoryScreen(() =>
            {
                ArmoryManager.Instance.ShowWindow(() =>
                {
                    TheEnemy.gameObject.SetActive(false);
                    ArcCardLayout.Instance.transform.gameObject.SetActive(false);
                    ShopManager.Instance.ShowShopWindow(() =>
                    {
                        LevelSelectionManager.Instance.ShowWindow(() =>
                        {
                            ArcCardLayout.Instance.transform.gameObject.SetActive(true);
                            TheEnemy.gameObject.SetActive(true);
                            TheEnemy.Init(GameData.CurrentRound);
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


        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            if (artifact.effect == ArtifactEffectType.GoldOnLose &&
            GameData.CurrentAttacks <= 0 &&
            GameManager.Instance.TheEnemy.Health > 0)
            {
                GameData.CurrentGold += artifact.value;
                UIManager.Instance.ShowTooltip($"+{artifact.value} Gold from artifact");
            }
        }

        LeanTween.delayedCall(gameObject, 0.5f, () =>
        {
            UIManager.Instance.ShowLoseScreen(() =>
            {
                if (UnlockManager.Instance.HasUnlocksToReveal())
                    UnlockManager.Instance.ShowWindow();
            });

        });

        if(TutorialController.Instance.HasRunTutorial() == false)
        {
            PlayerPrefs.SetInt("HasRunTutorial", 1);
            AnalyticsService.Instance.RecordEvent("tutorial_finished");
            AnalyticsService.Instance.RecordEvent("LostGameInTutorial");

        }

    }

    public void FinishRound()
    {
        GameData.CurrentAttacks--;
        EvaluatorManager.Instance.FinisLevel();
        myGameStates = GameStates.Game;


        if (TheEnemy.Health <= 0)
        {
            WinGame();
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
            PotionManager.Instance.AddPotion(PotionEffectType.HealHero);
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
            GameData.CurrentGold += 100;
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
            UnlockManager.Instance.DebugCompleteFirstBossAndShow();
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
            TheEnemy.Init(GameData.CurrentRound);
            GameData.CurrentRound++;
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            //    RewardManager.Instance.ShowWindow();
        }


    }

    // Debug functions
    public void AddGold(int aValue)
    {
        GameData.CurrentGold += aValue;
    }
    public void DisableBossDebuffForTurn()
    {
        GameData.BossDebuffDisabledThisTurn = 1;
    }
}
