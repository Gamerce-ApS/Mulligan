using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Singular;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    //TODO
    //Handles all clicks from buttons

    public RectTransform UIShiftGroup;
    public GameObject DamageFloatPrefab;
    public GameObject DamageLabel;
    public GameObject CriticalLabel;
    public GameObject DiscardPileIcon;
    public GameObject DeckPileIcon;
    public float DeckPileScaleMultiplier = 1.15f;
    public float DeckPileScaleUpTime = 0.12f;
    public float DeckPileScaleDownTime = 0.2f;

    public TMPro.TMP_Text AttackLabel;
    public TMPro.TMP_Text ReRollLabel;
    public TMPro.TMP_Text RoundsLabel;
    public TMPro.TMP_Text WorldLabel;
    public TMPro.TMP_Text GoldLabel;
    public GameObject SplashScreen;
    public GameObject BuyPopupWindow;
    public GameObject AttackButton;
    public GameObject ReRollButton;





    //Hero info
    public TMPro.TMP_Text GoldGainedText;
    public TMPro.TMP_Text HealthGainedText;
    public TMPro.TMP_Text LevelText;
    public TMPro.TMP_Text LostText;

    public Image XpBar;

    public Canvas thCanvas;

    public GameObject ArtifactSlotTemplate; // prefab with icon image
    public Transform ArtifactSlotParent; // grid/horizontal layout holder


    public GameObject PotionSlotTemplate; // prefab with icon image
    public Transform PotionSlotParent; // grid/horizontal layout holder


    public RectTransform BuyItemArea;
    public RectTransform SellItemArea;


    private Vector3 DamageLabelOriginalScale;
    private Vector3 CriticalLabelOriginalScale;
    private Vector3 DeckPileOriginalScale;
    public Transform SynergiButtonInfo;
    public Transform HeroButtonInfo;
    public List<Color> myRarityColors;

    public List<GameObject> PotionBackground;
    public List<GameObject> ArtifactBackground;

    public List<GameObject> SplashScreenButtons;


    // Start is called before the first frame update
    public void Init()
    {
        DamageLabelOriginalScale = DamageLabel.transform.localScale;
        CriticalLabelOriginalScale = CriticalLabel.transform.localScale;
        if (DeckPileIcon != null)
            DeckPileOriginalScale = DeckPileIcon.transform.localScale;

        DamageReset();

        OriginalsynergyTextGO = synergyTextGO.transform.localScale;

        SpeedLabel.text = PlayerPrefs.GetString("GameSpeed", "1X");

        if (PlayerPrefs.HasKey("GameSpeed") == false)
        {
            PlayerPrefs.SetString("GameSpeed", "2X");

        }

    }
    public void DamageReset()
    {
        DamageLabel.GetComponent<TMPro.TMP_Text>().text = "0";
        CriticalLabel.GetComponent<TMPro.TMP_Text>().text = "1";
        DamageLabel.transform.localScale = DamageLabelOriginalScale;
        CriticalLabel.transform.localScale = CriticalLabelOriginalScale;

    }
    // Update is called once per frame
    void Update()
    {

    }
    public Vector3 GetDeckPilePosition()
    {
        if (DeckPileIcon != null)
            return DeckPileIcon.transform.position;

        return DiscardPileIcon.transform.position;
    }
    public void PlayDeckPileDrawAnimation()
    {
        if (DeckPileIcon == null)
            return;

        if (DeckPileOriginalScale == Vector3.zero)
            DeckPileOriginalScale = DeckPileIcon.transform.localScale;

        LeanTween.cancel(DeckPileIcon);
        DeckPileIcon.transform.localScale = DeckPileOriginalScale;

        Vector3 targetScale = DeckPileOriginalScale * DeckPileScaleMultiplier;
        LeanTween.scale(DeckPileIcon, targetScale, DeckPileScaleUpTime)
            .setEaseOutBack()
            .setOnComplete(() =>
            {
                LeanTween.scale(DeckPileIcon, DeckPileOriginalScale, DeckPileScaleDownTime)
                    .setEaseOutQuad();
            });
    }
    public void UpdateLabels()
    {
        AttackLabel.text = GameData.CurrentAttacks.ToString();
        ReRollLabel.text = GameData.CurrentReRolls.ToString();
        RoundsLabel.text = "Level " + GameData.CurrentRound.ToString();
        int totalWorlds = 8; // or however many worlds you have
        int currentWorld = (GameData.CurrentRound - 1) / 4 + 1;
        WorldLabel.text = $"{currentWorld}/{totalWorlds}";
        GoldLabel.text = GameData.CurrentGold.ToString();

    }
    public void ClickPlayHand()
    {
        if (TutorialController.Instance.myCurrentAction == TutorialController.TutorialActionsEnum.SELECT_ORCS)
        {
            if (HandManager.Instance.SelectedCardCount() <= 3)
            {
                UIManager.Instance.ShowTooltip("Click on ORCs");
                return;
            }
        }


        HandManager.Instance.PlayHand();
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        UIManager.Instance.HideCardInfoPopup();

        if (TutorialController.Instance.myCurrentAction == TutorialController.TutorialActionsEnum.CLICK_ATTACK)
        {
            TutorialController.Instance.ShowNextStep();
        }
    }
    public void ClickReRoll()
    {
        if (TutorialController.Instance.myCurrentAction == TutorialController.TutorialActionsEnum.SELECT_ORCS)
        {
            if (HandManager.Instance.SelectedCardCount() <= 3)
            {
                UIManager.Instance.ShowTooltip("Click on ORCs");
                return;
            }
        }
        if (GameManager.Instance.TheEnemy.ActiveAbbilities.Contains(BossAbilityEnum.DisableRerolls))
        {
            UIManager.Instance.ShowTooltip("ReRolls disabled!");
            return;
        }

        if (GameData.CurrentReRolls > 0)
        {
            HandManager.Instance.ReRollHand();
            VibrationsManager.TryVibrate(VibrationType.ButtonTap);
            UIManager.Instance.HideCardInfoPopup();

            GameData.CurrentReRolls--;
        }
        if (TutorialController.Instance.myCurrentAction == TutorialController.TutorialActionsEnum.CLICK_REROLL)
        {
            TutorialController.Instance.ShowNextStep();
        }
    }
    public void ClickContinueFromShop()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        ShopManager.Instance.HideShopWindow();
    }
    public void ClickContinueFromDefeate()
    {
        if(GameData.FirstBossCompletedThisRun == 1)
        return;

        GameManager.Instance.StartGame();
        SceneManager.LoadScene(0);
        if (IAPManager.Instance.IsFullGameUnlocked == false)
        {
            UnityHelper.RunAfterDelay(IAPManager.Instance, 0.5f, () =>
            {
                UIManager.Instance.ClickBuyPopupWindow();
            });
        }

    }
    public void AddDamage(float aDamage)
    {
        DamageLabel.GetComponent<TMPro.TMP_Text>().text = (float.Parse(DamageLabel.GetComponent<TMPro.TMP_Text>().text) + aDamage).ToString();
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        LeanTween.scale(DamageLabel, Vector3.one * 1.3f, 0.5f).setEasePunch().setOnComplete(() =>
        {
            DamageLabel.transform.localScale = DamageLabelOriginalScale;
        });
    }
    public void AddCritical(float aDamage)
    {
        CriticalLabel.GetComponent<TMPro.TMP_Text>().text = (float.Parse(CriticalLabel.GetComponent<TMPro.TMP_Text>().text) + aDamage).ToString();
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        LeanTween.scale(CriticalLabel, Vector3.one * 1.3f, 0.5f).setEasePunch().setOnComplete(() =>
        {
            CriticalLabel.transform.localScale = CriticalLabelOriginalScale;
        });
    }



    public void ShiftUI(float offsetY, System.Action onComplete = null)
    {
        if (UIShiftGroup == null) return;

        Vector2 startPos = UIShiftGroup.anchoredPosition;
        Vector2 targetPos = startPos + new Vector2(0, offsetY);

        LeanTween.value(gameObject, startPos, targetPos, 0.3f)
            .setEaseOutQuad()
            .setOnUpdate((Vector2 val) =>
            {
                UIShiftGroup.anchoredPosition = val;
            }).setOnComplete(onComplete);
    }
    public GameObject SynergiTemplate;
    public void RefreshPreDamage()
    {
        Dictionary<CardRace, int> raceCounts = new();
        Dictionary<CardClass, int> classCounts = new();
        List<CardInstance> selectedCards = new List<CardInstance>();
        foreach (var cardInstance in HandManager.Instance.CurrentHand)
        {
            if (cardInstance.CardGO == null || !cardInstance.CardGO.isSelected) continue;
            selectedCards.Add(cardInstance);
            var data = cardInstance.data;

            if (!raceCounts.ContainsKey(data.race)) raceCounts[data.race] = 0;
            raceCounts[data.race]++;

            if (!classCounts.ContainsKey(data.cardClass)) classCounts[data.cardClass] = 0;
            classCounts[data.cardClass]++;
        }


        int totalDmg;
        List<CardInstance> boostedCards = EvaluatorManager.Instance.EvaluateHand(selectedCards, out totalDmg);

        foreach (var card in boostedCards)
        {
            int synergyDMG = EvaluatorManager.Instance.GetSynergyDamage(card, selectedCards);
            totalDmg += synergyDMG;
        }

        TMPro.TMP_Text text = DamageLabel.GetComponent<TMPro.TMP_Text>();
        int prevValue = int.Parse(text.text);
        if (totalDmg < prevValue)
            UnityHelper.AnimateTMPColorTransition(text, new Color(1f, 0.2f, 0.2f, 1f), new Color(0.866f, 0.757f, 0.573f, 1f), 0.5f);
        else if (totalDmg > prevValue)
            UnityHelper.AnimateTMPColorTransition(text, new Color(0.18f, 0.70f, 0.14f, 1f), new Color(0.866f, 0.757f, 0.573f, 1f), 0.5f);

        DamageLabel.GetComponent<TMPro.TMP_Text>().text = (totalDmg).ToString();
        if (totalDmg != 0)
            LeanTween.scale(DamageLabel, Vector3.one * 1.3f, 0.5f).setEasePunch();


        int crit = EvaluatorManager.Instance.GetGlobalCritMultiplier(selectedCards);
        CriticalLabel.GetComponent<TMPro.TMP_Text>().text = (crit + 1).ToString();
        if (crit != 0)
            LeanTween.scale(CriticalLabel, Vector3.one * 1.3f, 0.5f).setEasePunch();
    }

    public void ShowSynergies()
    {
        Transform parent = SynergiTemplate.transform.parent;

        // 1. Destroy all old synergy UIs (except the template)
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (child != SynergiTemplate)
            {
                Destroy(child);
            }
        }

        // 2. Count synergies from selected cards
        Dictionary<CardRace, int> raceCounts = new();
        Dictionary<CardClass, int> classCounts = new();

        foreach (var cardInstance in HandManager.Instance.CurrentHand)
        {
            if (cardInstance.CardGO == null || !cardInstance.CardGO.isSelected) continue;

            var data = cardInstance.data;
            if (cardInstance.GetIsAnyClass())
            {

            }
            else
            {
                if (!classCounts.ContainsKey(data.cardClass)) classCounts[data.cardClass] = 0;
                classCounts[data.cardClass]++;
            }

            if (cardInstance.GetIsAnyRace())
            {

            }
            else
            {
                if (!raceCounts.ContainsKey(data.race)) raceCounts[data.race] = 0;
                raceCounts[data.race]++;
            }
        }
        foreach (var cardInstance in HandManager.Instance.CurrentHand)
        {
            if (cardInstance.CardGO == null || !cardInstance.CardGO.isSelected)
                continue;

            if (cardInstance.GetIsAnyRace())
            {
                // ✅ Safe: iterate over a copy of the keys
                foreach (var key in raceCounts.Keys.ToList())
                {
                    raceCounts[key]++;
                }
            }

            if (cardInstance.GetIsAnyClass())
            {
                foreach (var key in classCounts.Keys.ToList())
                {
                    classCounts[key]++;
                }
            }
        }



        // 3. Create UI items for each synergy
        foreach (var kvp in raceCounts)
        {
            CreateSynergyItem($"Race: {kvp.Key}", kvp.Value, 4, CardContainer.Instance.GetSpriteForRace(kvp.Key), true);
        }

        foreach (var kvp in classCounts)
        {
            CreateSynergyItem($"Class: {kvp.Key}", kvp.Value, 4, CardContainer.Instance.GetSpriteForClass(kvp.Key), false);
        }

        synergyTextGO.transform.localScale = OriginalsynergyTextGO;
    }

    private void CreateSynergyItem(string key, int count, int max, Sprite iconSprite, bool isRace)
    {
        if (count == 0) return;

        GameObject item = Instantiate(SynergiTemplate, SynergiTemplate.transform.parent);
        item.SetActive(true);
        item.name = key; // So "Race: Orc" or "Class: Mage"

        TMPro.TMP_Text countText = item.GetComponentInChildren<TMPro.TMP_Text>();
        UnityEngine.UI.Image iconRace = item.transform.Find("IconRace")?.GetComponent<UnityEngine.UI.Image>();
        UnityEngine.UI.Image iconClass = item.transform.Find("IconClass")?.GetComponent<UnityEngine.UI.Image>();

        // Set correct icon
        if (isRace)
        {
            iconRace.enabled = true;
            iconClass.enabled = false;
            if (iconRace != null) iconRace.sprite = iconSprite;
        }
        else
        {
            iconRace.enabled = false;
            iconClass.enabled = true;
            if (iconClass != null) iconClass.sprite = iconSprite;
        }

        // Format synergy text
        string displayText = "";
        if (count == 1)
            displayText = "1/2";
        else if (count == 2)
            displayText = "2";
        else if (count == 3)
            displayText = "3/4";
        else
            displayText = "4";

        countText.text = displayText;

        // Highlight if full synergy (2 or 4)
        bool isFull = (count == 2 || count >= 4);
        if (isFull)
        {
            countText.color = new Color(1f, 0.84f, 0.2f); // Gold color

            // Scale pulse
            Vector3 originalScale = SynergiTemplate.transform.localScale;
            item.transform.localScale = originalScale;
            LeanTween.scale(item, originalScale * 1.3f, 0.5f).setEasePunch();

            // Enable glow
            var glow = item.transform.Find("Glow");
            if (glow != null) glow.gameObject.SetActive(true);

            if (glow != null)
            {
                glow.gameObject.SetActive(true);
                CanvasGroup cg = glow.GetComponent<CanvasGroup>() ?? glow.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0;
                LeanTween.alphaCanvas(cg, 1f, 0.3f).setEaseOutCubic();
            }
        }
        else
        {
            countText.color = new Color32(0xFA, 0xE3, 0xBC, 255); ;
            item.transform.localScale = SynergiTemplate.transform.localScale;

            // Disable glow
            var glow = item.transform.Find("Glow");
            if (glow != null) glow.gameObject.SetActive(false);
        }


    }
    public GameObject synergyTextGO;
    public Vector3 OriginalsynergyTextGO;
    public void PulseSynergyItem(string keyName, bool combat = false)
    {
        Transform parent = SynergiTemplate.transform.parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (child == SynergiTemplate) continue;

            if (child.name.Contains(keyName)) // match based on synergy key
            {
                if (combat == false)
                    LeanTween.scale(child, child.transform.localScale * 1.4f, 0.5f).setEasePunch();
                else
                {
                    Transform ch = child.transform;
                    Vector3 orignialScale = child.transform.localScale;
                    LeanTween.scale(child, child.transform.localScale * 2.6f, 0.7f).setEasePunch().setOnComplete(() => { ch.transform.localScale = orignialScale; }); ;
                    LeanTween.scale(synergyTextGO, synergyTextGO.transform.localScale * 2.6f, 0.7f).setEasePunch().setOnComplete(() =>
                    {
                        synergyTextGO.transform.localScale = OriginalsynergyTextGO;
                    });

                }

            }
        }
    }
    public void ClearSynergies()
    {
        Transform parent = SynergiTemplate.transform.parent;

        // 1. Destroy all old synergy UIs (except the template)
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (child != SynergiTemplate)
            {
                Destroy(child);
            }
        }
    }
    public GameObject TooltipPrefab;
    public void ShowTooltip(string message)
    {
        GameObject tooltip = Instantiate(TooltipPrefab, thCanvas.transform);
        TMPro.TMP_Text text = tooltip.GetComponentInChildren<TMPro.TMP_Text>();
        RectTransform rt = tooltip.GetComponent<RectTransform>();

        text.text = message;

        // Position it in center-bottom or wherever you want
        rt.anchoredPosition = new Vector2(0, -300); // adjust to your canvas
        rt.localScale = Vector3.one;

        CanvasGroup cg = tooltip.GetComponent<CanvasGroup>() ?? tooltip.AddComponent<CanvasGroup>();
        cg.alpha = 0;

        // Fade in + float up
        LeanTween.value(tooltip, 0f, 1f, 0.2f)
            .setOnUpdate((float val) => cg.alpha = val);

        LeanTween.moveY(rt, rt.anchoredPosition.y + 40f, 1f).setEaseOutCubic();

        // Fade out
        LeanTween.delayedCall(tooltip, 1f, () =>
        {
            LeanTween.value(tooltip, 1f, 0f, 0.3f)
                .setOnUpdate((float val) => cg.alpha = val)
                .setOnComplete(() => Destroy(tooltip));
        });
    }
    public void UpdateArtifactSlotsUI()
    {
        foreach (Transform child in ArtifactSlotParent)
        {
            if (child != ArtifactSlotTemplate.transform)
                Destroy(child.gameObject);
        }

        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            Artifact slot = Instantiate(ArtifactSlotTemplate, ArtifactSlotParent).GetComponent<Artifact>();
            slot.gameObject.SetActive(true);
            slot.ArtifactData = artifact;
            slot.Init(artifact);
            //slot.transform.Find("Icon").GetComponent<Image>().sprite = artifact.icon;
            // Optionally add tooltip or highlight here
        }
        UpdatePotionsSlotsUI();
        GameManager.Instance.TheHero.RefreshBar();
    }
    public void UpdatePotionsSlotsUI()
    {
        foreach (Transform child in PotionSlotParent)
        {
            if (child != PotionSlotTemplate.transform)
                Destroy(child.gameObject);
        }

        foreach (var pot in PotionManager.Instance.ActivePotions)
        {
            Potion slot = Instantiate(PotionSlotTemplate, PotionSlotParent).GetComponent<Potion>();
            slot.gameObject.SetActive(true);
            slot.PotionData = pot;
            slot.Init(pot);
            //slot.transform.Find("Icon").GetComponent<Image>().sprite = artifact.icon;
            // Optionally add tooltip or highlight here
        }
    }
    public void MutePotions(bool aValue)
    {
        foreach (Transform child in PotionSlotParent)
        {
            if (child != PotionSlotTemplate.transform)
            {
                child.GetComponent<Potion>().SetMuted(aValue);
            }
        }
    }
    public void MuteArtifacts(bool aValue)
    {
        int count = 0;
        foreach (Transform child in ArtifactSlotParent)
        {
            if (count <= 1)
                if (child != ArtifactSlotTemplate.transform)
                {
                    child.GetComponent<Artifact>().SetMuted(aValue);
                    count++;
                }
        }
    }
    public Artifact GetVisualArtifact(ArtifactData aArtifactData)
    {
        foreach (Transform child in ArtifactSlotParent)
        {
            if (child.GetComponent<Artifact>().ArtifactData == aArtifactData)
                return child.GetComponent<Artifact>();

        }
        return null;
    }
    public GameObject CardInfoPopupPrefab;
    private GameObject activeInfoPopup;
    public Transform currentTransform;
    public void ShowCardInfoPopup(string title, string description, string text2, Transform target)
    {
        if (activeInfoPopup != null) Destroy(activeInfoPopup);

        currentTransform = target;
        GameObject popup = Instantiate(CardInfoPopupPrefab, thCanvas.transform);
        activeInfoPopup = popup;

        TMP_Text titleText = popup.transform.Find("Title").GetComponent<TMP_Text>();
        TMP_Text descText = popup.transform.Find("Description").GetComponent<TMP_Text>();
        TMP_Text text2Text = popup.transform.Find("Text2").GetComponent<TMP_Text>();

        //Image iconImage = popup.transform.Find("Icon").GetComponent<Image>();

        title = title.Replace("\n", " ");
        titleText.text = title;
        descText.text = description;
        text2Text.text = text2;
        //iconImage.sprite = icon;

        RectTransform popupRT = popup.GetComponent<RectTransform>();

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, target.position);
        popupRT.transform.position = target.transform.position;

        if (screenPos.y < Screen.height * 0.5f)
            popupRT.transform.position += new Vector3(0, 35, 0);
        else
            popupRT.transform.position += new Vector3(0, -30, 0);

        // Fade in
        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        cg.alpha = 0;
        LeanTween.alphaCanvas(cg, 1f, 0.2f);


        // if(target.GetComponent<Artifact>() != null)
        // {
        //     activeInfoPopup.transform.Find("SellItemArea").gameObject.SetActive(true);
        //     activeInfoPopup.transform.Find("SellItemArea").GetComponent<Button>().onClick.RemoveAllListeners();
        //     Artifact art = target.GetComponent<Artifact>();
        //     activeInfoPopup.transform.Find("SellItemArea").GetComponent<Button>().onClick.AddListener(() => {
        //         ArtifactManager.Instance.SellArtifact(art);
        //         UIManager.Instance.ShowTooltip("Artifact sold!");
        //         HideCardInfoPopup();
        //     });


        // }
        // else
        //     activeInfoPopup.transform.Find("SellItemArea").gameObject.SetActive(false);
    }
    public void HideCardInfoPopup()
    {
        if (activeInfoPopup != null)
        {
            Destroy(activeInfoPopup);
            activeInfoPopup = null;
        }
        currentTransform = null;
    }
    private string[] funMessages = new string[]
{
        "- You did great!",
        "- Victory is yours!",
        "- The crowd goes wild!",
        "- Another step to glory!",
        "- You're unstoppable!",
        "- Hero of the realm!",
        "- You crushed it!"
};
    public TMP_Text VictoryFunText;
    public GameObject VictoryParent;
    public void ShowVictoryScreen(System.Action onComplete)
    {
        VictoryParent.GetComponent<CanvasGroup>().alpha = 0f;
        VictoryParent.SetActive(true);

        // Pick a fun message
        VictoryFunText.text = funMessages[UnityEngine.Random.Range(0, funMessages.Length)];

        // Fade in
        LeanTween.alphaCanvas(VictoryParent.GetComponent<CanvasGroup>(), 1f, 0.3f).setEaseOutQuad().setOnComplete(() =>
        {
            // Animate children
            StartCoroutine(AnimateChildrenIn(onComplete, VictoryParent.transform));
        });
        foreach (Transform child in VictoryParent.transform)
        {
            child.gameObject.SetActive(false);
        }

        // 1. Add EXP
        var hero = GameManager.Instance.TheHero;
        hero.Experience += CardContainer.Instance.ExperiencePerKill;

        // 2. Check for level-up
        while (hero.Experience >= CardContainer.Instance.ExperienceToLevelUp)
        {
            hero.Experience -= CardContainer.Instance.ExperienceToLevelUp;
            hero.Level++;

            // 3. Increase max HP
            hero.MaxHealth += CardContainer.Instance.HealthGainPerLevel;
            hero.HealHPPoints(CardContainer.Instance.HealthGainPerLevel);
            hero.RefreshBar();
            HealthGainedText.text = "+ " + CardContainer.Instance.HealthGainPerLevel.ToString();

            LeanTween.delayedCall(gameObject, 1.5f, () =>
            {
                ShowTooltip($"Level Up! Max HP increased to {hero.MaxHealth}");
            });
            // 4. Show level-up tooltip



        }
        LevelText.text = "Level " + hero.Level.ToString();
        XpBar.fillAmount = (float)hero.Experience / (float)CardContainer.Instance.ExperienceToLevelUp;
        GoldGainedText.text = "+ " + CardContainer.Instance.GoldGainPerLevel.ToString();



        //        public TMPro.TMP_Text GoldGainedText;
        //public TMPro.TMP_Text HealthGainedText;
        //public TMPro.TMP_Text LevelText;
        //public Image XpBar;


    }
    public GameObject LoseParent;
    public void ShowLoseScreen(System.Action onComplete)
    {

        int currentWorld = (GameData.CurrentRound - 1) / 4 + 1;
        LostText.text = "You reached World " + currentWorld + ", Level " + GameData.CurrentRound.ToString();
        LoseParent.GetComponent<CanvasGroup>().alpha = 0f;
        LoseParent.SetActive(true);



        // Fade in
        LeanTween.alphaCanvas(LoseParent.GetComponent<CanvasGroup>(), 1f, 0.3f).setEaseOutQuad().setOnComplete(() =>
        {
            // Animate children
            StartCoroutine(AnimateChildrenIn(onComplete, LoseParent.transform, -1));
        });
        foreach (Transform child in LoseParent.transform)
        {
            child.gameObject.SetActive(false);
        }

    }
    private IEnumerator AnimateChildrenIn(System.Action onComplete, Transform parent, float FadeOutAfterTime = 3.5f)
    {
        int i = 0;
        foreach (Transform child in parent)
        {
            if (child.name == "ignore")
            {
                child.gameObject.SetActive(true);
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

        yield return new WaitForSeconds(FadeOutAfterTime);

        onComplete?.Invoke();
        if (FadeOutAfterTime != -1)
        {
            LeanTween.alphaCanvas(parent.GetComponent<CanvasGroup>(), 0f, 0.3f).setEaseOutQuad().setOnComplete(() =>
            {
                parent.gameObject.SetActive(false);
            });
        }


    }

    public TMP_Text BossNameText;
    public TMP_Text BossAbilityText;
    public Image BossImage;
    public GameObject BossParent;
    public void ShowBossIntroScreen(BossData boss, System.Action onComplete)
    {
        BossParent.GetComponent<CanvasGroup>().alpha = 0f;
        BossParent.SetActive(true);

        // Pick a fun message
        BossNameText.text = boss.name;
        BossAbilityText.text = boss.description;
        //BossImage.sprite = boss.theSprite;
        BossImage.sprite = Resources.Load<Sprite>("" + boss.sprite_theSprite);



        // Fade in
        LeanTween.alphaCanvas(BossParent.GetComponent<CanvasGroup>(), 1f, 0.3f).setEaseOutQuad().setOnComplete(() =>
        {
            // Animate children
            StartCoroutine(AnimateChildrenIn(onComplete, BossParent.transform, 4));
        });
        foreach (Transform child in BossParent.transform)
        {
            child.gameObject.SetActive(false);
        }

    }

    public void ClickShowSynergiTooltip()
    {
        if (currentTransform == SynergiButtonInfo)
            HideCardInfoPopup();
        else
        {
            UIManager.Instance.ShowCardInfoPopup(
                "Synergies",
                "2 units: 2X damage \n\n4 units: 3X Critical",
                "",
                SynergiButtonInfo
            );
        }

    }
    public void ClickShowRunes()
    {
        if (currentTransform == HeroButtonInfo)
            HideCardInfoPopup();
        else
        {
            UIManager.Instance.ShowCardInfoPopup(
                "Hero Runes",
                RuneManager.Instance.GetActiveRunesInfo(),
                "",
                HeroButtonInfo
            );
        }

    }
    public int currentSpeed = 0;
    public TMPro.TMP_Text SpeedLabel;
    public void ClickSpeedButton()
    {
        currentSpeed++;
        if (currentSpeed > 2)
            currentSpeed = 0;
        if (currentSpeed == 0)
        {
            SpeedLabel.text = "1X";
        }
        else if (currentSpeed == 1)
        {
            SpeedLabel.text = "2X";

        }
        else if (currentSpeed == 2)
        {
            SpeedLabel.text = "3X";
        }
        PlayerPrefs.SetString("GameSpeed", SpeedLabel.text);
        LoadSpeed();
    }
    public void LoadSpeed()
    {
        string speed = PlayerPrefs.GetString("GameSpeed", "1X");

        if (speed == "1X")
        {
            Time.timeScale = 1;
        }
        else if (speed == "2X")
        {
            Time.timeScale = 2f;
        }
        else if (speed == "3X")
        {
            Time.timeScale = 3f;
        }
        SpeedLabel.text = speed;
    }
    public Color GetTextColor(int aRarity)
    {
        return myRarityColors[aRarity];
    }
    public void ClickTryForFree()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        SingularSDK.Event("ClickTryForFree");
        // PlayerPrefs.SetInt("HasRunTutorial", 1);
        Vector2 hidePos = new Vector2(SplashScreen.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        // Animate down
        LeanTween.move(SplashScreen.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                SplashScreen.SetActive(false);
                // if (TutorialController.Instance.HasRunTutorial() == false)
                // {
                // GameManager.Instance.ShowHeroSelection();
                // // UnityHelper.RunAfterDelay(this, 0.01f, () =>
                // // {
                // HeroSelectionManager.Instance.ClickHero(0);
                // // UnityHelper.RunAfterDelay(this, 0.01f, () =>
                // // {
                // HeroSelectionManager.Instance.HeroPortrait[0].SetActive(true);
                // HeroSelectionManager.Instance.HeroNormal[0].transform.GetChild(0).gameObject.SetActive(true);
                // HeroSelectionManager.Instance.selectedHero = 0;
                // HeroSelectionManager.Instance.ClickPlay();

                // });
                // });

                // }
                // else
                {
                    GameManager.Instance.ShowHeroSelection();
                    UnityHelper.RunAfterDelay(this, 0.5f, () =>
                    {
                        HeroSelectionManager.Instance.ClickHero(0);
                    });
                }

            });


    }
    public void ClickBuyPopupWindow()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        BuyPopupWindow.SetActive(true);
        BuyPopupWindow.GetComponent<CanvasGroup>().alpha = 0;
        LeanTween.alphaCanvas(BuyPopupWindow.GetComponent<CanvasGroup>(), 1f, 0.25f).setEaseOutQuad();

        GameObject g = BuyPopupWindow.transform.GetChild(0).gameObject;
        // Store the target position
        Vector2 targetPos = g.GetComponent<RectTransform>().anchoredPosition;
        // Start below the screen
        g.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPos.x, -Screen.height);
        // Animate to its original position
        LeanTween.move(g.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack();
    }

    public void ClickClosePopupWindow()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        BuyPopupWindow.GetComponent<CanvasGroup>().alpha = 1;
        LeanTween.alphaCanvas(BuyPopupWindow.GetComponent<CanvasGroup>(), 0f, 0.25f).setEaseInQuad();
        GameObject g = BuyPopupWindow.transform.GetChild(0).gameObject;

        // Move downward off the screen
        Vector2 hidePos = new Vector2(g.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);
        Vector3 startPos = g.GetComponent<RectTransform>().anchoredPosition;

        // Animate down
        LeanTween.move(g.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                BuyPopupWindow.SetActive(false);
                // g.SetActive(false);
                g.GetComponent<RectTransform>().anchoredPosition = startPos;
            });

    }
    public void ClickBuy()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        SingularSDK.Event("ClickBuy");

        IAPManager.Instance.BuyFullGame(() =>
        {
            UIManager.Instance.SplashScreenButtons[0].SetActive(true);
            UIManager.Instance.SplashScreenButtons[1].SetActive(false);
            UIManager.Instance.SplashScreenButtons[2].SetActive(false);
            HeroSelectionManager.Instance.RefreshUI();
            ClickClosePopupWindow();
        });

        // PlayerPrefs.SetInt("HasRunTutorial", 1);
        // Vector2 hidePos = new Vector2(SplashScreen.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        // // Animate down
        // LeanTween.move(SplashScreen.GetComponent<RectTransform>(), hidePos, 0.4f)
        //     .setEaseInBack()
        //     .setOnComplete(() =>
        //     {
        //         SplashScreen.SetActive(false);


        //         GameManager.Instance.ShowHeroSelection();
        //         UnityHelper.RunAfterDelay(this, 0.5f, () =>
        //         {
        //             HeroSelectionManager.Instance.ClickHero(0);
        //         });


        //     });

    }
    public void ClickTutorial()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        PlayerPrefs.SetInt("HasRunTutorial", 0);

        Vector2 hidePos = new Vector2(SplashScreen.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        // Animate down
        LeanTween.move(SplashScreen.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                SplashScreen.SetActive(false);
                GameManager.Instance.ShowHeroSelection();
                HeroSelectionManager.Instance.HeroPortrait[0].SetActive(true);
                HeroSelectionManager.Instance.HeroNormal[0].transform.GetChild(0).gameObject.SetActive(true);
                HeroSelectionManager.Instance.ClickHero(0);
                HeroSelectionManager.Instance.selectedHero = 0;
                HeroSelectionManager.Instance.ClickPlay();
            });
    }
    public void ClickPlayFullGame()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        PlayerPrefs.SetInt("HasRunTutorial", 1);
        // PlayerPrefs.SetInt(IAPManager.FullGameUnlockedKey, 1);
        Vector2 hidePos = new Vector2(SplashScreen.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        // Animate down
        LeanTween.move(SplashScreen.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                SplashScreen.SetActive(false);


                GameManager.Instance.ShowHeroSelection();
                UnityHelper.RunAfterDelay(this, 0.5f, () =>
                {
                    HeroSelectionManager.Instance.ClickHero(0);
                });


            });
    }
    public void ClickRestore()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        IAPManager.Instance.RestorePurchases();
    }
    public void ClickDiscord()
    {
        string inviteUrl = "https://discord.gg/fZwm99B89G";
        Application.OpenURL(inviteUrl);

    }

}
