using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

public class HeroSelectionManager : Singleton<HeroSelectionManager>
{

    public GameObject ShopWindow;



    public List<GameObject> HeroNormal;
    public List<GameObject> HeroPortrait;
    public List<GameObject> HeroSelected;
    public List<GameObject> HeroLock;
    public int selectedHero = -1;
    public Vector3 OriginalScale;
    public TMPro.TMP_Text NameLabel;
    public TMPro.TMP_Text HPLabel;
    public TMPro.TMP_Text ArtifactSlotsLabel;
    public TMPro.TMP_Text PotionSlotsLabel;
    public TMPro.TMP_Text GolfLabel;
    public TMPro.TMP_Text RuneLabel;
    public TMPro.TMP_Text MaxLevelLabel;
    public TMPro.TMP_Text MaxDamageLabel;
    public GameObject BuyHeroButton;
    private bool subscribedToIapInitialized = false;
    // Start is called before the first frame update
    void Awake()
    {
        startPosition = ShopWindow.GetComponent<RectTransform>().anchoredPosition;
        OriginalScale = HeroNormal[0].transform.localScale;
        LocalizationService.LanguageChanged += HandleLanguageChanged;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public Vector3 startPosition;
    System.Action OnHideShop=null;
    public CanvasGroup bgCanvasGroup;
    public void ShowWindow(System.Action onComplete = null)
    {
        SoundManager.TryPlay(SoundType.WindowOpen);
        SubscribeToIapInitialized();

        bgCanvasGroup.gameObject.SetActive(true);
        bgCanvasGroup.alpha = 0;
        LeanTween.alphaCanvas(bgCanvasGroup, 1f, 0.25f).setEaseOutQuad();

        OnHideShop = onComplete;
        ShopWindow.SetActive(true);
        // Store the target position
        Vector2 targetPos = startPosition;

        // Start below the screen
        ShopWindow.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPos.x, -Screen.height*2);

        // Animate to its original position
        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack();

        RefreshUI();
        SelectHero(GameData.HeroSelected, false);




    }
    public void RefreshUI()
    {
        for(int i = 0; i < HeroLock.Count;i++)
        {
            bool isUnlocked = IAPManager.Instance == null || IAPManager.Instance.IsHeroUnlocked(i);
            HeroLock[i].SetActive(isUnlocked == false);
            SetLockRaycasts(HeroLock[i], false);

            if (HeroLock[i].transform.parent.childCount > 0)
            {
                Image portrait = HeroLock[i].transform.parent.GetChild(0).GetComponent<Image>();
                // if (portrait != null)
                //     portrait.color = isUnlocked ? new Color(1,1,1,1) : new Color(0.5f,0.5f,0.5f,1);
            }
        }

        RefreshHighscoreUI(selectedHero >= 0 ? selectedHero : GameData.HeroSelected);
        RefreshBuyHeroButton();
    }
    public void HideWindow(System.Action onCompletet=null)
    {
        SoundManager.TryPlay(SoundType.WindowClose);

        bgCanvasGroup.alpha = 1;
        LeanTween.alphaCanvas(bgCanvasGroup, 0f, 0.25f).setEaseInQuad();

        // Move downward off the screen
        Vector2 hidePos = new Vector2(ShopWindow.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        // Animate down
        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                if(onCompletet != null)
                onCompletet.Invoke();
                OnHideShop?.Invoke();
                ShopWindow.SetActive(false);
                ShopWindow.GetComponent<RectTransform>().anchoredPosition = startPosition;
                bgCanvasGroup.gameObject.SetActive(false);
            });
    }
    public void ClickHero(int id)
    {
        // if (selectedHero == id)
        //     return; // Don't reselect the same hero
        if(LeanTween.isTweening())
        return;

        SelectHero(id, true);
    }
    private void SelectHero(int id, bool playFeedback)
    {
        if (id < 0 || id >= HeroNormal.Count)
            id = 0;

        if (playFeedback)
        {
            VibrationsManager.TryVibrate(VibrationType.CardTap);
            SoundManager.TryPlay(SoundType.CardTap);
        }

        for (int i = 0; i < HeroNormal.Count; i++)
        {
            if (i == id)
            {
                SetCharacterData(id);
                
                HeroPortrait[i].SetActive(true);
                HeroNormal[i].transform.GetChild(0).gameObject.SetActive(true);

                // Reset scale before animating
                OriginalScale = HeroPortrait[i].transform.localScale;
                if (playFeedback)
                {
                    HeroPortrait[i].transform.localScale = OriginalScale * 0.9f;
                    GameObject ports = HeroPortrait[i];
                    // Animate pop-in effect
                    LeanTween.scale(ports, OriginalScale * 1.05f, 0.15f)
                        .setEaseOutBack()
                        .setOnComplete(() =>
                        {
                            LeanTween.scale(ports, OriginalScale, 0.1f).setEaseInOutSine();
                        });
                }
             }
            else
            {
                HeroPortrait[i].SetActive(false);
                HeroNormal[i].transform.GetChild(0).gameObject.SetActive(false);
 
            }
        }

        selectedHero = id;
        if (IAPManager.Instance == null || IAPManager.Instance.IsHeroUnlocked(id))
            GameData.HeroSelected = id;

        RefreshHighscoreUI(id);
        RefreshBuyHeroButton();
    }

    private void OnDestroy()
    {
        LocalizationService.LanguageChanged -= HandleLanguageChanged;

        if (subscribedToIapInitialized && IAPManager.Instance != null)
            IAPManager.Instance.OnIAPInitialized -= RefreshBuyHeroButton;
    }

    public void SetCharacterData(int aID)
    {
        HeroData data= CardContainer.Instance.HeroDataList[aID];
        if(aID == 0)
        {
            NameLabel.text=LocalizedContent.HeroName(data, aID);
            HPLabel.text=data.startingHP.ToString();
            ArtifactSlotsLabel.text=data.ArtifactSlots.ToString();
            PotionSlotsLabel.text=data.PotionSlots.ToString();
            GolfLabel.text= CardContainer.Instance.StatingGold.ToString();
            RuneLabel.text=LocalizedContent.HeroStartingItems(data, aID);
        }
         if(aID == 1)
        {
            NameLabel.text=LocalizedContent.HeroName(data, aID);
            HPLabel.text=data.startingHP.ToString();
            ArtifactSlotsLabel.text=data.ArtifactSlots.ToString();
            PotionSlotsLabel.text=data.PotionSlots.ToString();
            GolfLabel.text= CardContainer.Instance.StatingGold.ToString();
            RuneLabel.text=LocalizedContent.HeroStartingItems(data, aID);
        }
         if(aID == 2)
        {
            NameLabel.text=LocalizedContent.HeroName(data, aID);
            HPLabel.text=data.startingHP.ToString();
            ArtifactSlotsLabel.text=data.ArtifactSlots.ToString();
            PotionSlotsLabel.text=data.PotionSlots.ToString();
            GolfLabel.text= CardContainer.Instance.StatingGold.ToString();
            RuneLabel.text=LocalizedContent.HeroStartingItems(data, aID);
        }
         if(aID == 3)
        {
    
            NameLabel.text=LocalizedContent.HeroName(data, aID);
            HPLabel.text=data.startingHP.ToString();
            ArtifactSlotsLabel.text=data.ArtifactSlots.ToString();
            PotionSlotsLabel.text=data.PotionSlots.ToString();
            GolfLabel.text= CardContainer.Instance.StatingGold.ToString();
            RuneLabel.text=LocalizedContent.HeroStartingItems(data, aID);
        }
        RefreshHighscoreUI(aID);
   
    }
    public void RefreshHighscoreUI(int heroIndex)
    {
        if (MaxLevelLabel != null)
            MaxLevelLabel.text = "" + HighscoreManager.Instance.GetMaxLevel(heroIndex).ToString();

        if (MaxDamageLabel != null)
            MaxDamageLabel.text = "" + HighscoreManager.Instance.GetMaxDamage(heroIndex).ToString();
    }
    public void ClickPlay()
    {
        if(selectedHero == -1)
        {
            UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.select_hero", "You need to select a hero!"));
            return;
        }

        if (IAPManager.Instance != null && IAPManager.Instance.IsHeroUnlocked(selectedHero) == false)
        {
            UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.hero_locked", "Unlock this hero first!"));
            RefreshBuyHeroButton();
            return;
        }

        GameData.HeroSelected = selectedHero;
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        SoundManager.TryPlay(SoundType.Success);

        

        // if(TutorialController.Instance.HasRunTutorial() == false)
        // {
        //     if(UIManager.Instance.PotionSlotParent.childCount>0)
        //         PotionManager.Instance.SellPotion(UIManager.Instance.PotionSlotParent.GetChild(0).GetComponent<Potion>());
        // }

        // HighscoreManager.Instance.UpdateMaxLevel(GameData.CurrentRound);
        // DailyQuestManager.Instance.AddProgress(DailyQuestType.PlayRuns);
        // DailyQuestManager.Instance.SetProgressIfHigher(DailyQuestType.ReachLevel, GameData.CurrentRound);
        
        HideWindow(()=>{HeroInfoScreen.Instance.ShowWindow();});
    }
    public void ClickLocked()
    {
        RefreshBuyHeroButton();

    }

    public void ClickedBuyHeroButton()
    {
        if (selectedHero <= 0)
            return;

        if (IAPManager.Instance == null)
            return;

        if (IAPManager.Instance.IsHeroUnlocked(selectedHero))
        {
            UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.hero_owned", "You already own this hero!"));
            RefreshBuyHeroButton();
            return;
        }

        UIManager.Instance.ClickBuyHero(selectedHero);
    }

    public void RefreshBuyHeroButton()
    {
        if (BuyHeroButton == null)
            return;

        bool showBuyButton = selectedHero > 0 && IAPManager.Instance != null && IAPManager.Instance.IsHeroUnlocked(selectedHero) == false;
        BuyHeroButton.SetActive(showBuyButton);

        if (showBuyButton == false)
            return;

        TMPro.TMP_Text priceLabel = BuyHeroButton.transform.childCount > 0
            ? BuyHeroButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>()
            : null;

        if (priceLabel != null)
            priceLabel.text = IAPManager.Instance.GetLocalizedHeroPrice(selectedHero);
    }

    private void SubscribeToIapInitialized()
    {
        if (subscribedToIapInitialized || IAPManager.Instance == null)
            return;

        IAPManager.Instance.OnIAPInitialized += RefreshBuyHeroButton;
        subscribedToIapInitialized = true;
    }

    private void SetLockRaycasts(GameObject lockObject, bool value)
    {
        if (lockObject == null)
            return;

        Graphic[] graphics = lockObject.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
            graphic.raycastTarget = value;
    }
    public void ClickTalent()
    {
        UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.coming_soon", "Coming soon!"));
    }
    public void ClickedHighScore()
    {
       UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.coming_soon", "Coming soon!"));
    }
    public void ClickBack()
    {
        HideWindow(()=>{

            HeroInfoScreen.Instance.ShowWindow();

        });

    }

    private void HandleLanguageChanged()
    {
        int heroIndex = selectedHero >= 0 ? selectedHero : GameData.HeroSelected;
        if (CardContainer.Instance != null && CardContainer.Instance.HeroDataList != null &&
            heroIndex >= 0 && heroIndex < CardContainer.Instance.HeroDataList.Length)
        {
            SetCharacterData(heroIndex);
        }
    }
}
