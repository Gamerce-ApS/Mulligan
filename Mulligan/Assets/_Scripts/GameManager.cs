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
        //if(GameData.CurrentRound ==0)
            StartGame();
    }

    public void StartGame()
    {
        GameData.CurrentGold = 0;
        GameData.CurrentAttacks = 4;
        GameData.CurrentReRolls = 2;
        GameData.CurrentRound = 1;
        TheEnemy.Init(250* GameData.CurrentRound);
    }
    public void WinGame()
    {
        GameData.CurrentGold += 5;
        GameData.CurrentAttacks = 4;
        GameData.CurrentReRolls = 2;
        GameData.CurrentRound++;
        TheEnemy.Init(250 * GameData.CurrentRound);
    }
    public void LostGame()
    {
        StartGame();
        SceneManager.LoadScene(0);
    }
    public void FinishRound()
    {
        GameData.CurrentAttacks--;
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
        
    }
}
