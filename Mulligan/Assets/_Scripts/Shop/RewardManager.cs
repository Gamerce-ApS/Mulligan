using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardManager : Singleton<RewardManager>
{

    public GameObject Window;
    public Image bg;

    public TMPro.TMP_Text Title;

    public Artifact ArtifactReward;
    public Potion Potion1;
    public Potion Potion2;



    // Start is called before the first frame update
    void Awake()
    {
        startPosition = Window.GetComponent<RectTransform>().anchoredPosition;

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
        if (LevelSelectionManager.Instance.CurrentRewardData == null)
            LevelSelectionManager.Instance.CurrentRewardData = GetRandom();

        Title.text = LevelSelectionManager.Instance.CurrentRewardData.title;

        OnHideShop = onComplete;

        bgCanvasGroup.alpha = 0;
        bgCanvasGroup.gameObject.SetActive(true);


        bg.GetComponent<CanvasGroup>().alpha = 1f;

        // Fade in
        LeanTween.alphaCanvas(bgCanvasGroup.GetComponent<CanvasGroup>(), 1f, 0.3f).setEaseOutQuad().setDelay(0.1f).setOnComplete(() =>
        {
            // Animate children
            int i = 0;
            foreach (Transform child in Window.transform)
            {
                if (child.name == "ignore" || child == bg.transform)
                {
                    //child.gameObject.SetActive(true);

                }
                else
                {
                    child.gameObject.SetActive(true);

                    Vector3 targetScale = child.localScale;
                    child.localScale = Vector3.zero;
                    LeanTween.scale(child.gameObject, targetScale, 0.5f).setEaseOutBack().setDelay(i * 0.1f);
                }

                i++;
            }
        });
        foreach (Transform child in Window.transform)
        {
            if (child.name != "ignore" || child != bg.transform)
                child.gameObject.SetActive(false);
        }

 

        if(LevelSelectionManager.Instance.CurrentRewardData.type == SkipRewardType.RareArtifact || LevelSelectionManager.Instance.CurrentRewardData.type == SkipRewardType.UncommonArtifact)
        {
            a1 = ArtifactManager.Instance.GetRandom();
            ArtifactReward.Init(a1);
            ArtifactReward.ArtifactData = a1;
            ArtifactReward.gameObject.SetActive(true);

            Vector3 targetScale = ArtifactReward.transform.localScale;
            ArtifactReward.transform.localScale = Vector3.zero;
            LeanTween.scale(ArtifactReward.transform.gameObject, targetScale, 0.5f).setEaseOutBack().setDelay(1.0f);

        }
        if (LevelSelectionManager.Instance.CurrentRewardData.type == SkipRewardType.RandomPotions)
        {
            p1 = PotionManager.Instance.GetRandom();
            p2 = PotionManager.Instance.GetRandom();
            
            Potion1.Init(p1);
            Potion1.PotionData = p1;
            Potion2.Init(p2);
            Potion2.PotionData = p2;
            Potion1.gameObject.SetActive(true);
            Potion2.gameObject.SetActive(true);

            Vector3 targetScale = Potion1.transform.localScale;
            Potion1.transform.localScale = Vector3.zero;
            LeanTween.scale(Potion1.transform.gameObject, targetScale, 0.5f).setEaseOutBack().setDelay(1.0f);

            targetScale = Potion2.transform.localScale;
            Potion2.transform.localScale = Vector3.zero;
            LeanTween.scale(Potion2.transform.gameObject, targetScale, 0.5f).setEaseOutBack().setDelay(1.0f);

        }
    }
    public PotionCardData p1;
    public PotionCardData p2;
    public ArtifactData a1;
    public void HideWindow()
    {
        if(LevelSelectionManager.Instance.CurrentRewardData != null)
            ApplyReward(LevelSelectionManager.Instance.CurrentRewardData.type);

        bgCanvasGroup.alpha = 1;
        LeanTween.alphaCanvas(bgCanvasGroup, 0f, 0.25f).setEaseInQuad();

        // Move downward off the screen
        Vector2 hidePos = new Vector2(Window.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        // Animate down
        LeanTween.move(Window.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                OnHideShop?.Invoke();
                Window.SetActive(false);
                Window.GetComponent<RectTransform>().anchoredPosition = startPosition;
                bgCanvasGroup.gameObject.SetActive(false);
            });
    }
    public void ClickSkip()
    {

    }
    public void ClickPlay()
    {
        HideWindow();
    }
    public SkipRewardData GetRandom()
    {

        var all = CardContainer.Instance.SkipDataList;
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("No artifacts available to choose from.");
            return null;
        }

        // Pick random one
        SkipRewardData selected = all[Random.Range(0, all.Length)];

        return selected;
    }
    public void ApplyReward(SkipRewardType type)
    {
        switch (type)
        {
            case SkipRewardType.DoubleGold:
                GameManager.Instance.AddGold(GameData.CurrentGold);
                break;

            case SkipRewardType.RandomPotions:
                PotionManager.Instance.AddPotion(p1);
                PotionManager.Instance.AddPotion(p2);

                break;

            case SkipRewardType.DisableBossDebuff:
                GameManager.Instance.DisableBossDebuffNextRound = true;
                break;

            case SkipRewardType.UncommonArtifact:
                ArtifactManager.Instance.AddArtifact(a1);
                //ArtifactManager.Instance.AddRandomArtifact();
                break;

            case SkipRewardType.RareArtifact:
                ArtifactManager.Instance.AddArtifact(a1);
                break;

            case SkipRewardType.ArmoryUpgrade:
                UnitUpgradeManager.Instance.ShowWindow();
                break;

            case SkipRewardType.IncreaseMaxHP:
                GameManager.Instance.TheHero.AddMaxHPPercent(1.25f);
                break;

            case SkipRewardType.FullHeal:
                GameManager.Instance.TheHero.Health = GameManager.Instance.TheHero.MaxHealth;
                GameManager.Instance.TheHero.RefreshBar();
                break;

            case SkipRewardType.MarketFreeNextRound:
                ShopManager.Instance.SetEverythingFreeNextRound=true;
                break;

            case SkipRewardType.AddRuneToHero:
                //Hero.Instance.AddRandomRune();
                break;

            case SkipRewardType.ExtraAttacksNextRound:
                GameManager.Instance.BonusAttacksNextRound=true;
                break;

            default:
                Debug.LogWarning("Skip reward not implemented: " + type);
                break;
        }
    }
}
