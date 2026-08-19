using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeroInfoScreen : Singleton<HeroInfoScreen>
{
    public GameObject ShopWindow;
    public CanvasGroup bgCanvasGroup;
    public Image HeroPortrait;
    public Transform QuestParent;
    public GameObject QuestTemplate;
    public Image RewardProgressBar;
    public TMP_Text RewardProgressLabel;
    public Button ClaimButton;

    public Vector3 startPosition;

    public void Init()
    {
        if (ShopWindow != null)
            startPosition = ShopWindow.GetComponent<RectTransform>().anchoredPosition;

        if (QuestTemplate != null)
            QuestTemplate.SetActive(false);

        if (ClaimButton != null)
        {
            ClaimButton.onClick.RemoveAllListeners();
            ClaimButton.onClick.AddListener(ClickClaimReward);
        }
    }

    public void ShowWindow()
    {
        UpdateUI();

        SoundManager.TryPlay(SoundType.WindowOpen);
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);

        bgCanvasGroup.gameObject.SetActive(true);
        bgCanvasGroup.alpha = 0;
        LeanTween.alphaCanvas(bgCanvasGroup, 1f, 0.25f).setEaseOutQuad();

        ShopWindow.SetActive(true);
        Vector2 targetPos = ShopWindow.GetComponent<RectTransform>().anchoredPosition;
        ShopWindow.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPos.x, -Screen.height);

        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack();
    }

    public void HideWindow(System.Action onComplete=null)
    {
        SoundManager.TryPlay(SoundType.WindowClose);
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);

        bgCanvasGroup.alpha = 1;
        LeanTween.alphaCanvas(bgCanvasGroup, 0f, 0.25f).setEaseInQuad();

        Vector2 hidePos = new Vector2(ShopWindow.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                if(onComplete != null)
                    onComplete.Invoke();
                ShopWindow.SetActive(false);
                ShopWindow.GetComponent<RectTransform>().anchoredPosition = startPosition;
                bgCanvasGroup.gameObject.SetActive(false);
            });
    }

    public void UpdateUI()
    {
        SetHeroPortrait();
        DailyQuestManager.Instance.PopulateQuestItems(QuestParent, QuestTemplate);
        DailyQuestManager.Instance.UpdateRewardUI(RewardProgressBar, RewardProgressLabel, ClaimButton);
    }

    public void ClickHeroSelectionScreen()
    {
        PlayButtonFeedback();
        HideWindow(()=>{HeroSelectionManager.Instance.ShowWindow();});

    }

    public void ClickInventory()
    {
        PlayButtonFeedback();
        InventoryOverviewManager.Instance.ShowWindow();
    }

    public void ClickDailyQuests()
    {
        PlayButtonFeedback();
        DailyQuestManager.Instance.ShowWindow();
    }

    public void ClickJoinDiscord()
    {
        PlayButtonFeedback();
        UIManager.Instance.ClickDiscord();
    }

    public void ClickBuyFullGame()
    {
        PlayButtonFeedback();
        UIManager.Instance.ClickBuyPopupWindow();
    }

    public void ClickStartBattle()
    {
        PlayButtonFeedback();
        HideWindow(()=>{GameManager.Instance.RunPreGameSetup();});
        if(TutorialController.Instance.HasRunTutorial() == false)
        {
            if(UIManager.Instance.PotionSlotParent.childCount>0)
                PotionManager.Instance.SellPotion(UIManager.Instance.PotionSlotParent.GetChild(0).GetComponent<Potion>());
        }

        HighscoreManager.Instance.UpdateMaxLevel(GameData.CurrentRound);
        DailyQuestManager.Instance.AddProgress(DailyQuestType.PlayRuns);
        DailyQuestManager.Instance.SetProgressIfHigher(DailyQuestType.ReachLevel, GameData.CurrentRound);


        
        
        // LevelSelectionManager.Instance.ShowWindow();
    }

    private void ClickClaimReward()
    {
        PlayButtonFeedback();
        DailyQuestManager.Instance.ClaimArtifactReward();
        UpdateUI();
    }

    private void SetHeroPortrait()
    {
        if (HeroPortrait == null ||
            GameManager.Instance == null ||
            GameManager.Instance.TheHero == null ||
            GameManager.Instance.TheHero.HeroPortraits == null ||
            GameData.HeroSelected < 0 ||
            GameData.HeroSelected >= GameManager.Instance.TheHero.HeroPortraits.Count)
            return;

        Image selectedPortrait = GameManager.Instance.TheHero.HeroPortraits[GameData.HeroSelected].GetComponent<Image>();
        if (selectedPortrait != null)
            HeroPortrait.sprite = selectedPortrait.sprite;
    }

    private void PlayButtonFeedback()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        SoundManager.TryPlay(SoundType.ButtonTap);
    }
}
