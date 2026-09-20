using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Card : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Transform originalParent;
    public Vector2 originalAnchoredPos;
    public bool isSelected = false;
    private bool isDragging = false;

    private GameObject placeholder;
    private LayoutElement layoutElement;

    public TMPro.TMP_Text NameLabel;
    public TMPro.TMP_Text DamageLabel;
    public TMPro.TMP_Text RankLabel;
    public Image Portrait_BG;
    public Image Portrait;
    public Image RaceIcon;
    public Image ClassIcon;
    public Image bg;
    public List<Sprite> myCardBG;

    private float holdTimer = 0f;
    private bool isHolding = false;
    public CardInstance cardInstance;
    public GameObject mutedGO;

    public System.Action<Card> OnClick = null;
    public List<GameObject> enhancedGO;
    public CardTypeEnum myType;
    public bool allowDrag = true;

    public GameObject AnyRace;
    public GameObject AnyClass;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        LocalizationService.LanguageChanged += RefreshLocalizedText;
    }

    private void OnDisable()
    {
        LocalizationService.LanguageChanged -= RefreshLocalizedText;
    }
    public void Init(CardInstance aCardInstance)
    {
        cardInstance = aCardInstance;
        if(aCardInstance.data != null)
        {
            Init(aCardInstance.data);
        }else if(aCardInstance.upgradeData != null)
        {
            Init(aCardInstance.upgradeData);
        }

        RankLabel.text = aCardInstance.currentRank.ToString();

        if (aCardInstance.currentRank == 0)
            RankLabel.transform.parent.gameObject.SetActive(false);
        else
        {
            RankLabel.transform.parent.gameObject.SetActive(true);
        }

        if(enhancedGO !=null)
        {
            if(enhancedGO.Count>0)
            {
                int upgradeAmount = aCardInstance.appliedUpgrades.Count;
                if (upgradeAmount > 3)
                    upgradeAmount = 3;

                if (aCardInstance.IsSpecial() && upgradeAmount == 0)
                    upgradeAmount = 1;

                if (upgradeAmount > 0)
                    enhancedGO[upgradeAmount - 1].SetActive(true);
                else
                {
                    foreach (var u in enhancedGO)
                        u.SetActive(false);
                }
            }
        }


    }
    public void Init(CardData aData)
    {
        // Sett image, name, type and so on
        NameLabel.text = FormatCardName(LocalizedContent.UnitName(aData));

        DamageLabel.text = aData.damage.ToString();
        DamageLabel.text = (cardInstance.GetDamage()).ToString();
        Portrait.sprite = Resources.Load<Sprite>("" + cardInstance.data.sprite_portrait);
        SetupAnimatedPortrait(aData);
        //Portrait.sprite = cardInstance.data.portrait;

        RaceIcon.sprite = CardContainer.Instance.GetSpriteForRace(aData.race);
        ClassIcon.sprite = CardContainer.Instance.GetSpriteForClass(aData.cardClass);
        Portrait_BG.color = CardContainer.Instance.GetColorForRace(aData.race);

        
        bg.sprite = myCardBG[(int)aData.race];
    }

    private void SetupAnimatedPortrait(CardData aData)
    {
        if (Portrait == null)
            return;

        AnimatedPortraitSlot slot = Portrait.GetComponent<AnimatedPortraitSlot>();
        if (CardContainer.Instance.UseAnimatedPortraits == false)
        {
            if (slot != null)
                slot.ClearPortrait();
            return;
        }

        AnimatedCardPortraitData portraitData = CardContainer.Instance.GetAnimatedCardPortraitData(aData.sprite_portrait);

        if (portraitData == null || portraitData.PortraitPrefab == null)
        {
            if (slot != null)
                slot.ClearPortrait();
            return;
        }

        if (slot == null)
            slot = Portrait.gameObject.AddComponent<AnimatedPortraitSlot>();

        slot.ShowPortrait(portraitData.PortraitPrefab, portraitData.Offset, portraitData.Scale);
    }
    public void Init(UpgradeCardData aData)
    {
        NameLabel.text = LocalizedContent.UpgradeName(aData);
        NameLabel.color = UIManager.Instance.GetTextColor(aData.rarity);
    }
    public void UpdateCardUI()
    {
        if (cardInstance.currentRank == 0)
            RankLabel.transform.parent.gameObject.SetActive(false);
        else
        {
            RankLabel.text = cardInstance.currentRank.ToString();
            RankLabel.transform.parent.gameObject.SetActive(true);
        }


        if (enhancedGO != null)
        {
            if (enhancedGO.Count > 0)
            {
                int upgradeAmount = cardInstance.appliedUpgrades.Count;
                if (upgradeAmount > 3)
                    upgradeAmount = 3;

                if (cardInstance.IsSpecial() && upgradeAmount == 0)
                    upgradeAmount = 1;

                if (upgradeAmount > 0)
                    enhancedGO[upgradeAmount - 1].SetActive(true);
                else
                {
                    foreach (var u in enhancedGO)
                        u.SetActive(false);
                }
            }
        }


        //if (cardInstance.appliedUpgrades.Count > 0 || cardInstance.IsSpecial())
        //    enhancedGO.SetActive(true);
        //else
        //    enhancedGO.SetActive(false);

        DamageLabel.text = (cardInstance.GetDamage()).ToString();



        foreach(var u in cardInstance.appliedUpgrades)
        {
            if(u.effect == UpgradeEffect.Enchantment_Changeling)
            {
                AnyRace.SetActive(true);
            }
            if (u.effect == UpgradeEffect.Enchantment_PlusOneClass)
            {
                AnyClass.SetActive(true);
            }
        }

    }
    void Update()
    {
        if (isHolding)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer > 0.4f) // 400 ms hold
            {
                isHolding = false;
                UIManager.Instance.ShowCardInfoPopup(
                    () => cardInstance.data != null
                        ? LocalizedContent.UnitName(cardInstance.data)
                        : LocalizedContent.UpgradeName(cardInstance.upgradeData),
                    () => GetDescription(),
                    () => "",
                    transform);
            }
        }
    }
    public string GetDescription()
    {
        string upgradeString = "";
        for(int i= 0; i < cardInstance.appliedUpgrades.Count;i++)
        {
            var upg = cardInstance.appliedUpgrades[i];
            string localizedDescription = LocalizedContent.UpgradeDescription(upg);
            if (string.IsNullOrEmpty(localizedDescription) == false)
            {
                if(i != 0)
              upgradeString += "\n";
              upgradeString += localizedDescription.Replace("\n", " ");

            }
        }
        if(cardInstance.tempCritBonus>0)
        upgradeString += "<color=\"red\">" + LocalizationService.Format("ui.card.temp_crit", "\n+{0} Crit", cardInstance.tempCritBonus) + "</color>";
        if(cardInstance.tempDamageBonus>0)
        upgradeString += "<color=\"red\">" + LocalizationService.Format("ui.card.temp_damage", "\n+{0} Damage", cardInstance.tempDamageBonus) + "</color>";


        //upgradeString = upgradeString.Replace(" ", "");
        return upgradeString;
    }

    private void RefreshLocalizedText()
    {
        if (NameLabel == null || cardInstance == null)
            return;

        if (cardInstance.data != null)
            NameLabel.text = FormatCardName(LocalizedContent.UnitName(cardInstance.data));
        else if (cardInstance.upgradeData != null)
            NameLabel.text = LocalizedContent.UpgradeName(cardInstance.upgradeData);
    }

    private string FormatCardName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "";

        int firstSpace = displayName.IndexOf(' ');
        if (firstSpace <= 0 || firstSpace >= displayName.Length - 1)
            return displayName;

        return displayName.Substring(0, firstSpace) + "\n" + displayName.Substring(firstSpace + 1);
    }

    public Quaternion originalRotation;
    public void OnPointerClick(PointerEventData eventData)
    {
        if(TutorialController.Instance.myCurrentAction == TutorialController.TutorialActionsEnum.SELECT_ORCS)
        {
            if( cardInstance.data.race != CardRace.Orc)
            {
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.click_orcs", "Click on ORCs"));
                return;
            }
            if(HandManager.Instance.SelectedCardCount()>=3)
            {
                TutorialController.Instance.ShowNextStep();
            }
        }
        if(TutorialController.Instance.myCurrentAction == TutorialController.TutorialActionsEnum.CLICK_ReRollCards)
        {
            if( HandManager.Instance.CurrentHand[0] == cardInstance ||  HandManager.Instance.CurrentHand[2] == cardInstance )
            {
                // Wait until the click has actually toggled selection before advancing.
            }else
            {
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.click_correct_cards", "Click on correct cards!"));
                return;
            } 
        }
        if(TutorialController.Instance.myCurrentAction == TutorialController.TutorialActionsEnum.SELECT_WARRIORS)
        {
            if( cardInstance.data.cardClass != CardClass.Warrior)
            {
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.click_warriors", "Click on Warriors"));
                return;
            }
            if(HandManager.Instance.SelectedCardCount()>=3)
            {
                TutorialController.Instance.ShowNextStep();
            }
        }


        

        if (GameManager.Instance.myGameStates != GameManager.GameStates.Game && ArmoryManager.Instance.ShopWindow.activeSelf == false&& UnitUpgradeManager.Instance.ShopWindow.activeSelf == false)
            return;
        if (isDragging) return;
        if (OnClick != null)
        {
            OnClick.Invoke(this);



            return;
        }

        if (!isSelected)
        {
            if (HandManager.Instance.SelectedCardCount() >= 4)
            {
                LeanTween.scale(gameObject, transform.localScale * 1.1f, 0.2f).setEasePunch();
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.only_four_cards", "Only 4 cards can be selected."));
                return;
            }
            //if (cardInstance.isMuted)
            //{
            //    LeanTween.scale(gameObject, transform.localScale * 1.1f, 0.2f).setEasePunch();
            //    UIManager.Instance.ShowTooltip("Card is muted.");
            //    return;
            //}
            originalAnchoredPos = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition += new Vector2(0, 70f); // Lift
            isSelected = true;
        }
        else
        {
            rectTransform.anchoredPosition = originalAnchoredPos;
            isSelected = false;
        }

        UIManager.Instance.ShowSynergies();
        UIManager.Instance.RefreshPreDamage();

        if (TutorialController.Instance.myCurrentAction == TutorialController.TutorialActionsEnum.CLICK_ReRollCards &&
            HasSelectedTutorialRerollCards())
        {
            TutorialController.Instance.ShowNextStep();
        }
    }

    private bool HasSelectedTutorialRerollCards()
    {
        if (HandManager.Instance.CurrentHand.Count <= 2)
            return false;

        CardInstance firstCard = HandManager.Instance.CurrentHand[0];
        CardInstance thirdCard = HandManager.Instance.CurrentHand[2];

        return firstCard != null &&
               thirdCard != null &&
               firstCard.CardGO != null &&
               thirdCard.CardGO != null &&
               firstCard.CardGO.isSelected &&
               thirdCard.CardGO.isSelected;
    }

    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!allowDrag)
            return;
        if (GameManager.Instance.myGameStates != GameManager.GameStates.Game)
            return;

        if(isSelected)
            rectTransform.anchoredPosition = originalAnchoredPos;


        isDragging = true;
        isSelected = false;
        SoundManager.TryPlay(SoundType.CardMove);

        originalParent = transform.parent;
        originalAnchoredPos = rectTransform.anchoredPosition;

        // Create placeholder to keep grid position
        placeholder = new GameObject("Card Placeholder");
        layoutElement = placeholder.AddComponent<LayoutElement>();

        // Match size to the card
        LayoutElement thisLayout = GetComponent<LayoutElement>();
        if (thisLayout != null)
        {
            layoutElement.preferredWidth = thisLayout.preferredWidth;
            layoutElement.preferredHeight = thisLayout.preferredHeight;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
        }

        placeholder.transform.SetParent(originalParent);
        placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex());

        // Move card out to canvas (visually)
        transform.SetParent(canvas.transform, true);
        canvasGroup.blocksRaycasts = false;
        UIManager.Instance.HideCardInfoPopup();

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!allowDrag)
            return;
        if (GameManager.Instance.myGameStates != GameManager.GameStates.Game)
            return;
        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, canvas.worldCamera, out globalMousePos))
        {
            rectTransform.position = globalMousePos;
        }
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        VibrationsManager.TryVibrate(VibrationType.CardTap);
        SoundManager.TryPlay(SoundType.CardTap);
        isHolding = true;
        holdTimer = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        holdTimer = 0f;

        if(myType == CardTypeEnum.UnitCard)
        {
            if (UIManager.Instance.currentTransform != transform)
            {
                UIManager.Instance.HideCardInfoPopup();
            }
            UIManager.Instance.currentTransform = null;
        }

    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!allowDrag)
            return;
        if (GameManager.Instance.myGameStates != GameManager.GameStates.Game)
            return;

        {
            isDragging = false;
 
            // Return to grid in same position as placeholder
            int returnIndex = placeholder.transform.GetSiblingIndex();
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(returnIndex);
            transform.localScale = Vector3.one;
            rectTransform.anchoredPosition = originalAnchoredPos;

            // Clean up placeholder
            Destroy(placeholder);
            canvasGroup.blocksRaycasts = true;
        }
    }
    public void LiftCard()
    {
        float liftHeight = 50f;
        float scaleTime = 0.2f;

        RectTransform rt = GetComponent<RectTransform>();
        Vector2 startAnchored = rt.anchoredPosition;
        Vector2 targetAnchored = startAnchored + new Vector2(0, liftHeight);

        LeanTween.value(gameObject, startAnchored, targetAnchored, scaleTime)
        .setEaseOutCubic()
        .setOnUpdate((Vector2 val) => rt.anchoredPosition = val);

    }
    public void PlayBoostAnimation( int damageAmount, Transform targetLabel, System.Action onComplete = null)
    {
        float delay =  0.0f; // spread delay per card
        LeanTween.delayedCall(gameObject, delay, () =>
        {
            // Lift
            LeanTween.delayedCall(gameObject, delay, () =>
            {
                // Pulse
                LeanTween.scale(gameObject, Vector3.one * 1.3f, 0.5f)
                .setEasePunch();

            // 3. Create damage number above the card
            GameObject dmgObj = Instantiate(UIManager.Instance.DamageFloatPrefab, transform.position, Quaternion.identity, transform.parent);
            RectTransform dmgRT = dmgObj.GetComponent<RectTransform>();
            TMPro.TMP_Text dmgText = dmgObj.GetComponent<TMPro.TMP_Text>();
            dmgText.text = "+" + damageAmount;

            dmgRT.anchoredPosition += new Vector2(0, 250f);

            // 4. Animate punch scale in
            dmgRT.localScale = Vector3.zero;
            LeanTween.scale(dmgObj, Vector3.one*1.2f, 0.3f).setEaseOutBack();


            // 5. Wait, then fly to damage label
            LeanTween.delayedCall(dmgObj, 0.75f, () =>
            {
                Vector3 worldTarget = targetLabel.position;

                LeanTween.move(dmgObj, worldTarget, 0.25f)
                    .setEaseInCubic()
                    .setOnComplete(() =>
                    {
                        Destroy(dmgObj);
                        onComplete?.Invoke();
                    });
            });

            });
        });
        UIManager.Instance.HideCardInfoPopup();

    }
    public int GetCurrentDamageLabelNumber()
    {
        if (DmgNumber == null)
            return 0;

        return currentDamageNumberAmount;

    }
    public GameObject DmgNumber = null;
    private int currentDamageNumberAmount = 0;
    public void AddDamage(int damageAmount, System.Action onComplete, bool isCrit = false, bool isGold = false,bool isTotal = false,string SynergySpriteName = "")
    {
        if(damageAmount==0)
        {
            onComplete?.Invoke();
            return;
        }
        if(isTotal)
        { 
            if(isCrit)
            {
                if (GetTotalCrit() == GetCurrentDamageLabelNumber())
                {
                    onComplete?.Invoke();
                    return;
                }
            }
            else if(isGold)
            {

            }
            else
            {
                if (GetTotalDamage() == GetCurrentDamageLabelNumber())
                {
                    onComplete?.Invoke();
                    return;
                }
            }
        }

        SoundManager.TryPlay(SoundType.CardDamageNumber);

        // Pulse
        LeanTween.scale(gameObject, Vector3.one * 1.3f, 0.5f)
        .setEasePunch();

        int totalD = damageAmount;
        // 3. Create damage number above the card
        if (DmgNumber == null)
        {
            DmgNumber = Instantiate(UIManager.Instance.DamageFloatPrefab, transform.position, Quaternion.identity, transform.parent);
            DmgNumber.GetComponent<TMPro.TMP_Text>().text = "0";
        }
        else
        {
            DmgNumber.transform.position = transform.position;
        }

        RectTransform dmgRT = DmgNumber.GetComponent<RectTransform>();
        TMPro.TMP_Text dmgText = DmgNumber.GetComponent<TMPro.TMP_Text>();


        //totalD = int.Parse(dmgText.text.Replace("+", "")) + damageAmount;
        totalD = damageAmount;
        currentDamageNumberAmount = totalD;

        if (isCrit)
        {
            if (isTotal)
            {
                dmgText.text = "<color=#FFD700>" + LocalizationService.Format("ui.damage.critical", "+{0} Critical", totalD) + "</color>"; // gold/yellow + label
            }
            else
                dmgText.text = LocalizationService.Format("ui.damage.critical", "+{0} Critical", totalD);
        }else if(isGold)
        {
            dmgText.text = LocalizationService.Format("ui.damage.gold", "+{0} Gold", totalD);
        }
        else
        {
            if (isTotal)
            {
                dmgText.text = "<color=#FFD700>+" + totalD + "</color>"; // gold/yellow + label
            }
            else
            dmgText.text = "+" + totalD;
        }
 

        dmgRT.anchoredPosition += new Vector2(0, 185f);

        // 4. Animate punch scale in
        dmgRT.localScale = Vector3.zero;
        LeanTween.scale(DmgNumber, Vector3.one * 1.2f, 0.3f).setEaseOutBack();

        LeanTween.delayedCall(DmgNumber, 1.0f, () =>
        {
            onComplete?.Invoke();
        });

        dmgText.text = "<voffset=-26>" + dmgText.text + "</voffset>";

        if (SynergySpriteName != "")
        {
            dmgText.spriteAsset = GameManager.Instance.GetTextSpriteForSprite(SynergySpriteName);

            dmgText.text += "    <sprite=0>";
        }
    }



    public int GetTotalDamage()
    {
        int TotalDamage = 0;
        TotalDamage = cardInstance.GetDamage();
        int synergyBonus = EvaluatorManager.Instance.GetSynergyDamage(cardInstance, HandManager.Instance.PlayedHand,false,false);
        TotalDamage += synergyBonus;
        TotalDamage+=EvaluatorManager.Instance.GetArtifactBonusDamage(cardInstance);


        return TotalDamage;
    }
    public int GetTotalCrit()
    {
        return cardInstance.GetUpgradeCritBonus() + cardInstance.GetCritBonus();
    }
    
    public void AddToTotalDamage(System.Action onComplete, bool isCrit = false, bool isGold = false)
    {
        // 5. Wait, then fly to damage label
        LeanTween.delayedCall(DmgNumber, 0.75f, () =>
        {
            Vector3 worldTarget = UIManager.Instance.DamageLabel.transform.position;
            if(isCrit)
                worldTarget = UIManager.Instance.CriticalLabel.transform.position;
            if (isGold)
                worldTarget = UIManager.Instance.GoldLabel.transform.position;

            LeanTween.move(DmgNumber, worldTarget, 0.25f)
                .setEaseInCubic()
                .setOnComplete(() =>
                {
                    int amount = currentDamageNumberAmount;
                    Destroy(DmgNumber);
                    DmgNumber = null;
                    currentDamageNumberAmount = 0;
                    // After animation add to the total
                    if (isCrit)
                        UIManager.Instance.AddCritical(amount);
                    else if(isGold)
                    {
                        GameManager.Instance.AddGold(amount);
                    }
                    else
                        UIManager.Instance.AddDamage(amount);

                    onComplete?.Invoke();
                });
        });
    }


    public void FlyAwayAndDiscard(Vector3 flyTargetWorld, float delay,CardInstance cInstance)
    {
        SoundManager.TryPlay(SoundType.CardDiscard);

        float flyTime = 0.5f;
        float rotateAngle = 360f;

        // Disable interactions
        canvasGroup.blocksRaycasts = false;

        // Animate after optional delay
        LeanTween.delayedCall(gameObject, delay, () =>
        {
            // Rotate and move to target
            LeanTween.move(gameObject, flyTargetWorld, flyTime)
                .setEaseInCubic();

            LeanTween.rotateZ(gameObject, rotateAngle, flyTime)
                .setEaseInOutCubic();

            // Scale down
            LeanTween.scale(gameObject, Vector3.zero, flyTime)
                .setEaseInBack();

            // After animation, add to discard pile and destroy visual
            LeanTween.delayedCall(gameObject, flyTime, () =>
            {
                CardContainer.Instance.DiscardCard(cInstance);
                Destroy(gameObject);

                    if (cardInstance.WillExplodeAfterAttack)
                    {
                            DailyQuestManager.Instance.AddProgress(DailyQuestType.DestroyUnits);
                            CardContainer.Instance.DiscardDeck.Remove(cardInstance);
                            CardContainer.Instance.CurrentDeck.Remove(cardInstance);
                    }
            });
        });
    }


}
