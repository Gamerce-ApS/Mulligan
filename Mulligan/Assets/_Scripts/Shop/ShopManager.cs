using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : Singleton<ShopManager>
{
    public GameObject RunePrefab;
    public GameObject PotionPrefab;
    public GameObject ArtifactPrefab;
    public GameObject UnitPackPrefab;

    public Transform RuneParent;
    public Transform ArtifactParent;
    public Transform PotionParent;
    public Transform UnitPackParent;

    public GameObject HeroRunePrefab;
    public Transform HeroRuneParent;

    public GameObject ShopWindow;
    public bool SetEverythingFreeNextRound = false;
    public GameObject BattleButton;

    public void PopulateShop()
    {

        Clear();


        // go = GameObject.Instantiate(RunePrefab, RuneParent);
        // go.GetComponent<ShopCard>().Init(RuneManager.Instance.GetRandom());

        //GameObject.Instantiate(PotionPrefab, ArtifactParent);
        //GameObject.Instantiate(PotionPrefab, ArtifactParent);

        GameObject go = GameObject.Instantiate(ArtifactPrefab, ArtifactParent);
        go.GetComponent<ShopCard>().Init(ArtifactManager.Instance.GetRandom());
        // go = GameObject.Instantiate(ArtifactPrefab, ArtifactParent);
        // go.GetComponent<ShopCard>().Init(ArtifactManager.Instance.GetRandom());
        //go = GameObject.Instantiate(ArtifactPrefab, ArtifactParent);
        //go.GetComponent<ShopCard>().Init(ArtifactManager.Instance.GetRandom());
        //go = GameObject.Instantiate(ArtifactPrefab, ArtifactParent);
        //go.GetComponent<ShopCard>().Init(ArtifactManager.Instance.GetRandom());
        go = GameObject.Instantiate(PotionPrefab, PotionParent);
        go.GetComponent<ShopCard>().Init(PotionManager.Instance.GetRandom());

        if (TutorialController.Instance.HasRunTutorial())
        {
            go = GameObject.Instantiate(RunePrefab, RuneParent);
            go.GetComponent<ShopCard>().Init(RuneManager.Instance.GetRandom());

            go = GameObject.Instantiate(PotionPrefab, PotionParent);
            go.GetComponent<ShopCard>().Init(PotionManager.Instance.GetRandom());
            go = GameObject.Instantiate(UnitPackPrefab, UnitPackParent);
            go.GetComponent<ShopCard>().Init(3);
        }


        // go = GameObject.Instantiate(UnitPackPrefab, UnitPackParent);
        // go.GetComponent<ShopCard>().Init(3);



        if (TutorialController.Instance.HasRunTutorial() == false)
        {
          if (UIManager.Instance.PotionSlotParent.childCount > 0)
                PotionManager.Instance.SellPotion(UIManager.Instance.PotionSlotParent.GetChild(0).GetComponent<Potion>());

            if(TutorialController.Instance.LastStepPlayed == "Step3_Potion")
            {
                go = GameObject.Instantiate(UnitPackPrefab, UnitPackParent);
                go.GetComponent<ShopCard>().Init(3);
                TutorialController.Instance.ShowStepById("Step4_Shop1");
            }
            if(TutorialController.Instance.LastStepPlayed == "Step5_boss2")
            {
                go = GameObject.Instantiate(RunePrefab, RuneParent);
                go.GetComponent<ShopCard>().Init(RuneManager.Instance.GetRandom());
                TutorialController.Instance.ShowStepById("Step6_shop1");
            }

            
            
        }
                RefreshHeroRunes();

    }
    public void RefreshHeroRunes()
    {
        HeroRuneParent.DestroyAllChildren();
        for (int i = 0; i < RuneManager.Instance.ActiveRunes.Count; i++)
        {
            GameObject go2 = GameObject.Instantiate(HeroRunePrefab, HeroRuneParent);
            go2.GetComponent<ShopCard>().Init(RuneManager.Instance.ActiveRunes[i]);
            go2.GetComponent<ShopCard>().CanBeDraged = false;
            go2.transform.localScale = new Vector3(0.6656631f, 0.6656631f, 0.6656631f);
        }
    }
    public bool hasRerolledForFree = false;
    public void ClickReRoll()
    {
        if (GameManager.Instance.HasFreeReroll && hasRerolledForFree == false)
        {
            PopulateShop();
            hasRerolledForFree = true;
        }
        else if (GameData.CurrentGold >= 5)
        {
            GameData.CurrentGold -= 5;
            PopulateShop();
        }
        else
        {
            UIManager.Instance.ShowTooltip("Not enough gold!");
        }
    }

    public void Clear()
    {
        UnitPackParent.DestroyAllChildren();
        ArtifactParent.DestroyAllChildren();
        RuneParent.DestroyAllChildren();
        PotionParent.DestroyAllChildren();
        HeroRuneParent.DestroyAllChildren();
    }
    // Start is called before the first frame update
    void Start()
    {
        startPosition = ShopWindow.GetComponent<RectTransform>().anchoredPosition;

        //PopulateShop();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public Vector3 startPosition;
    System.Action OnHideShop = null;
    public CanvasGroup bgCanvasGroup;
    public void ShowShopWindow(System.Action onComplete = null)
    {

        PopulateShop();

        bgCanvasGroup.gameObject.SetActive(true);
        bgCanvasGroup.alpha = 0;
        LeanTween.alphaCanvas(bgCanvasGroup, 1f, 0.25f).setEaseOutQuad();

        OnHideShop = onComplete;
        ShopWindow.SetActive(true);
        // Store the target position
        Vector2 targetPos = ShopWindow.GetComponent<RectTransform>().anchoredPosition;

        // Start below the screen
        ShopWindow.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPos.x, -Screen.height);

        // Animate to its original position
        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack();

        hasRerolledForFree = false;

        if (TutorialController.Instance.HasRunTutorial() == false)
            if (TutorialController.Instance.LastStepPlayed == "Step1_Gold")
            {  
                TutorialController.Instance.ShowStepById("Step2_Shop");   
            }
    }
    public void HideShopWindow()
    {
        if (TutorialController.Instance.LastStepPlayed == "Step2_Shop4_ClickBattle")
            TutorialController.Instance.HideTutorial();

        bgCanvasGroup.alpha = 1;
        LeanTween.alphaCanvas(bgCanvasGroup, 0f, 0.25f).setEaseInQuad();

        // Move downward off the screen
        Vector2 hidePos = new Vector2(ShopWindow.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        // Animate down
        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                OnHideShop?.Invoke();
                ShopWindow.SetActive(false);
                ShopWindow.GetComponent<RectTransform>().anchoredPosition = startPosition;
                bgCanvasGroup.gameObject.SetActive(false);
            });
        SetEverythingFreeNextRound = false;
    }
}
