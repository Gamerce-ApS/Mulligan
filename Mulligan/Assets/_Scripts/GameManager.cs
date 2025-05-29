using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public Enemy TheEnemy;
    public Hero TheHero;


    // Start is called before the first frame update
    void Start()
    {

        CardContainer.Instance.Init();
        HandManager.Instance.Init();
        UIManager.Instance.Init();




            StartGame();

    }

    public void StartGame()
    {
        GameData.CurrentGold = 0;
        GameData.CurrentAttacks = 4;
        GameData.CurrentReRolls = 2;
        GameData.CurrentRound = 1;
        LevelSelectionManager.Instance.ShowWindow(() => {
            TheEnemy.Init(250 * GameData.CurrentRound);

        });

    }
    public void WinGame()
    {
        GameData.CurrentGold = (int)((float)GameData.CurrentGold * 1.2f);
        GameData.CurrentGold += 5;
        GameData.CurrentAttacks = 4;
        GameData.CurrentReRolls = 2;
        GameData.CurrentRound++;
        LeanTween.delayedCall(gameObject, 1f, () =>
        {
            UIManager.Instance.ShowVictoryScreen(() => {
                ArmoryManager.Instance.ShowWindow(() => {
                    TheEnemy.gameObject.SetActive(false);
                    ArcCardLayout.Instance.transform.gameObject.SetActive(false);
                    ShopManager.Instance.ShowShopWindow(() => {
                        LevelSelectionManager.Instance.ShowWindow(() => {
                            ArcCardLayout.Instance.transform.gameObject.SetActive(true);
                            TheEnemy.gameObject.SetActive(true);
                            TheEnemy.Init(250 * GameData.CurrentRound);
                            EvaluatorManager.Instance.StartLevel();
                        });

                    });

  
                });
     
             });
        });


    }
    public void LostGame()
    {


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

        LeanTween.delayedCall(gameObject, 1f, () =>
        {
            StartGame();
            SceneManager.LoadScene(0);
        });



    }
    public void FinishRound()
    {
        GameData.CurrentAttacks--;
        EvaluatorManager.Instance.FinisLevel();

        if ( TheEnemy.Health < 0)
        {
            WinGame();
        }
        else if(GameData.CurrentAttacks <=0)
        {
            LostGame();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.S))
        {
            ArtifactManager.Instance.AddRandomArtifact();
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            ArtifactManager.Instance.AddArtifact(ArtifactEffectType.AddCritFlat);
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            HandManager.Instance.RankUpRandom();
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            TheHero.Attack(500);
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

    }

    // Debug functions
    public void AddGold(int aValue)
    {
        GameData.CurrentGold += aValue;
    }
}
