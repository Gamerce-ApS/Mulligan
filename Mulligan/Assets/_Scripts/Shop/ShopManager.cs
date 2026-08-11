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
    public TMPro.TMP_Text ReRollCostLabel;
    public void PopulateShop()
    {

        Clear();


        // go = GameObject.Instantiate(RunePrefab, RuneParent);
        // go.GetComponent<ShopCard>().Init(RuneManager.Instance.GetRandom());

        //GameObject.Instantiate(PotionPrefab, ArtifactParent);
        //GameObject.Instantiate(PotionPrefab, ArtifactParent);

        SpawnArtifactCard();
        // go = GameObject.Instantiate(ArtifactPrefab, ArtifactParent);
        // go.GetComponent<ShopCard>().Init(ArtifactManager.Instance.GetRandom());
        //go = GameObject.Instantiate(ArtifactPrefab, ArtifactParent);
        //go.GetComponent<ShopCard>().Init(ArtifactManager.Instance.GetRandom());
        //go = GameObject.Instantiate(ArtifactPrefab, ArtifactParent);
        //go.GetComponent<ShopCard>().Init(ArtifactManager.Instance.GetRandom());
        SpawnPotionCard();

        if (TutorialController.Instance.HasRunTutorial())
        {
            SpawnRuneCard();

            SpawnPotionCard();
            GameObject go = GameObject.Instantiate(UnitPackPrefab, UnitPackParent);
            go.GetComponent<ShopCard>().Init(3);
        }


        // go = GameObject.Instantiate(UnitPackPrefab, UnitPackParent);
        // go.GetComponent<ShopCard>().Init(3);



        if (TutorialController.Instance.HasRunTutorial() == false)
        {
            if (UIManager.Instance.PotionSlotParent.childCount > 0 && tutorialShopHelp == false)
                PotionManager.Instance.SellPotion(UIManager.Instance.PotionSlotParent.GetChild(0).GetComponent<Potion>());

            if( tutorialShopHelp == true)
            {
                SpawnPotionCard();
            }
            if (TutorialController.Instance.LastStepPlayed == "Step4_Shop3" || tutorialShopHelpArmory)
            {
                tutorialShopHelpArmory = true;
                GameObject go = GameObject.Instantiate(UnitPackPrefab, UnitPackParent);
                go.GetComponent<ShopCard>().Init(3);
            }

            if (TutorialController.Instance.LastStepPlayed == "Step3_Potion")
            {
                GameObject go = GameObject.Instantiate(UnitPackPrefab, UnitPackParent);
                go.GetComponent<ShopCard>().Init(3);
                TutorialController.Instance.ShowStepById("Step4_Shop1");
                tutorialShopHelp = true;
            }
            
            
            if (TutorialController.Instance.LastStepPlayed == "Step5_boss2")
            {
                SpawnRuneCard();
                TutorialController.Instance.ShowStepById("Step6_shop1");
            }
          


        }
        RefreshHeroRunes();
        RefreshArtifactSlots();
RefreshPotionSlots();


    }
    bool tutorialShopHelp = false;
    bool tutorialShopHelpArmory = false;

    private void SpawnArtifactCard()
    {
        ArtifactData artifact = ArtifactManager.Instance.GetRandom();
        if (artifact == null)
            return;

        GameObject go = GameObject.Instantiate(ArtifactPrefab, ArtifactParent);
        go.GetComponent<ShopCard>().Init(artifact);
    }

    private void SpawnPotionCard()
    {
        PotionCardData potion = PotionManager.Instance.GetRandom();
        if (potion == null)
            return;

        GameObject go = GameObject.Instantiate(PotionPrefab, PotionParent);
        go.GetComponent<ShopCard>().Init(potion);
    }

    private void SpawnRuneCard()
    {
        RuneData rune = RuneManager.Instance.GetRandom();
        if (rune == null)
            return;

        GameObject go = GameObject.Instantiate(RunePrefab, RuneParent);
        go.GetComponent<ShopCard>().Init(rune);
    }

    public void RefreshArtifactSlots()
    {
        for (int i = 0; i < UIManager.Instance.ArtifactBackground.Count; i++)
        {
            UIManager.Instance.ArtifactBackground[i].transform.GetChild(0).gameObject.SetActive(false);
            if (i < GameManager.Instance.TheHero.myHeroData.ArtifactSlots)
                UIManager.Instance.ArtifactBackground[i].SetActive(true);
            else
                UIManager.Instance.ArtifactBackground[i].SetActive(false);
        }
        //Activate the last slot in shop
        if(IAPManager.Instance.IsFullGameUnlocked)
        if (GameManager.Instance.TheHero.myHeroData.ArtifactSlots < UIManager.Instance.ArtifactBackground.Count)
        {
            UIManager.Instance.ArtifactBackground[GameManager.Instance.TheHero.myHeroData.ArtifactSlots].gameObject.SetActive(true);
            UIManager.Instance.ArtifactBackground[GameManager.Instance.TheHero.myHeroData.ArtifactSlots].transform.GetChild(0).gameObject.SetActive(true);
        }
    }
    public void RefreshPotionSlots()
    {
        for (int i = 0; i < UIManager.Instance.PotionBackground.Count; i++)
        {
            UIManager.Instance.PotionBackground[i].transform.GetChild(0).gameObject.SetActive(false);
            if (i < GameManager.Instance.TheHero.myHeroData.PotionSlots)
                UIManager.Instance.PotionBackground[i].SetActive(true);
            else
                UIManager.Instance.PotionBackground[i].SetActive(false);
        }
        //Activate the last slot in shop
        if(IAPManager.Instance.IsFullGameUnlocked)
        if (GameManager.Instance.TheHero.myHeroData.PotionSlots < UIManager.Instance.PotionBackground.Count&& TutorialController.Instance.HasRunTutorial() == true)
        {
            UIManager.Instance.PotionBackground[GameManager.Instance.TheHero.myHeroData.PotionSlots].gameObject.SetActive(true);
            UIManager.Instance.PotionBackground[GameManager.Instance.TheHero.myHeroData.PotionSlots].transform.GetChild(0).gameObject.SetActive(true);
        }
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
            VibrationsManager.TryVibrate(VibrationType.ButtonTap);
            hasRerolledForFree = true;
            ReRollCostLabel.text ="5";

        }
        else if (GameData.CurrentGold >= 5)
        {
            GameData.CurrentGold -= 5;
            PopulateShop();
            VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        }
        else
        {
            UIManager.Instance.ShowTooltip("Not enough gold!");
        }
    }
    public void ClickUnlockSlot()
    {
        if (GameData.CurrentGold >= 20)
        {
            GameData.CurrentGold -= 20;
            GameManager.Instance.TheHero.myHeroData.ArtifactSlots++;
            RefreshArtifactSlots();
        }
        else
        {
            UIManager.Instance.ShowTooltip("Not enough gold!");
        }
    }
        public void ClickUnlockPotionSlot()
    {
        if (GameData.CurrentGold >= 20)
        {
            GameData.CurrentGold -= 20;
            GameManager.Instance.TheHero.myHeroData.PotionSlots++;
            RefreshPotionSlots();
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

        UIManager.Instance.MutePotions(false);
        UIManager.Instance.MuteArtifacts(false);

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


        if (GameManager.Instance.HasFreeReroll && hasRerolledForFree == false)
        {
            ReRollCostLabel.text = "";
        }else
        {
            ReRollCostLabel.text="5";
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

        if (GameManager.Instance.TheHero.myHeroData.ArtifactSlots < UIManager.Instance.ArtifactBackground.Count&& TutorialController.Instance.HasRunTutorial() == true)
        {
            UIManager.Instance.ArtifactBackground[GameManager.Instance.TheHero.myHeroData.ArtifactSlots].gameObject.SetActive(false);
            UIManager.Instance.ArtifactBackground[GameManager.Instance.TheHero.myHeroData.ArtifactSlots].transform.GetChild(0).gameObject.SetActive(false);
        }

        if (GameManager.Instance.TheHero.myHeroData.PotionSlots < UIManager.Instance.PotionBackground.Count&& TutorialController.Instance.HasRunTutorial() == true)
        {
            UIManager.Instance.PotionBackground[GameManager.Instance.TheHero.myHeroData.PotionSlots].gameObject.SetActive(false);
            UIManager.Instance.PotionBackground[GameManager.Instance.TheHero.myHeroData.PotionSlots].transform.GetChild(0).gameObject.SetActive(false);
        }

    }
}
