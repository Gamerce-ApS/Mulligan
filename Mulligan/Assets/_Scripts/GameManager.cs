using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
