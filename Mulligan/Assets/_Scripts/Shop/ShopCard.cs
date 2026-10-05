using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{

    public ArtifactData ArtifactData;
    public PotionCardData PotionData;
    public RuneData RuneData;

    public int Price = 30;

    private Transform originalParent;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Vector2 originalAnchoredPos;
    public bool isSelected = false;
    private bool isDragging = false;
    private float holdTimer = 0f;
    private bool isHolding = false;
    public TMPro.TMP_Text PriceLabel;
    public TMPro.TMP_Text NameLabel;
    public bool CanBeDraged = true;
    public bool HoldToShowInfo = false;
    public float HoldInfoDuration = 0.5f;
    public ShopItemType PackType = ShopItemType.UnitUpgradePack;
    public System.Action<ShopCard> OnClick;
    private bool longPressTriggered = false;
    private Vector2 pointerDownPosition;

    private void OnEnable()
    {
        LocalizationService.LanguageChanged += RefreshLocalizedText;
    }

    private void OnDisable()
    {
        LocalizationService.LanguageChanged -= RefreshLocalizedText;
    }
    public void Init(ArtifactData aData)
    {
        NameLabel.text = LocalizedContent.ArtifactName(aData);
        NameLabel.color = UIManager.Instance.GetTextColor(aData.rarity);

        ArtifactData = aData;
        Price = CardContainer.Instance.GetShopPrice(ShopItemType.Artifact, (RarityType)aData.rarity);
        PotionData = null;
        RuneData = null;

        Price = (int)(Price * (1-GameManager.Instance.MarketDiscountModifier));
        if (ShopManager.Instance.SetEverythingFreeNextRound)
            Price = 0;

        PriceLabel.text = Price.ToString();
    }
    public void Init(RuneData aData)
    {
        NameLabel.text = LocalizedContent.RuneName(aData);
        NameLabel.color = UIManager.Instance.GetTextColor((int)aData.rarity);

        RuneData = aData;
        Price = CardContainer.Instance.GetShopPrice(ShopItemType.Rune, aData.rarity);
        PotionData = null;
        ArtifactData = null;

        Price = (int)(Price * (1-GameManager.Instance.MarketDiscountModifier));
        if (ShopManager.Instance.SetEverythingFreeNextRound)
            Price = 0;

        PriceLabel.text = Price.ToString();
    }
    public void Init(PotionCardData aData)
    {
        ArtifactData = null;
        RuneData = null;
        NameLabel.text = LocalizedContent.PotionName(aData);
        NameLabel.color = UIManager.Instance.GetTextColor((int)aData.rarity);

        PotionData = aData;
        Price = CardContainer.Instance.GetShopPrice(ShopItemType.Potion, (RarityType)aData.rarity);
        Price = (int)(Price * (1-GameManager.Instance.MarketDiscountModifier));
        if (ShopManager.Instance.SetEverythingFreeNextRound)
            Price = 0;

        PriceLabel.text = Price.ToString();
    }
    public void Init(int aCost)
    {
        Init(aCost, ShopItemType.UnitUpgradePack);
    }

    public void Init(int aCost, ShopItemType packType)
    {
        // NameLabel.text = "Army Upgrade";
        ArtifactData = null;
        PotionData = null;
        RuneData = null;
        PackType = packType;
        if (NameLabel != null)
        {
            NameLabel.text = packType == ShopItemType.ArtifactPack
                ? LocalizationService.Get("ui.shop.artifact_pack", "Artifact Pack")
                : LocalizationService.Get("ui.shop.unit_upgrade_pack", "Unit Upgrade Pack");
        }
        Price = aCost;
        Price = (int)(Price * (1-GameManager.Instance.MarketDiscountModifier));
        if (ShopManager.Instance.SetEverythingFreeNextRound)
            Price = 0;
        PriceLabel.text = Price.ToString();
    }
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if(CanBeDraged == false)
        return;

        if (isSelected)
            rectTransform.anchoredPosition = originalAnchoredPos;


        isDragging = true;
        isSelected = false;

        VibrationsManager.TryVibrate(VibrationType.CardTap);
        SoundManager.TryPlay(SoundType.ShopItemDragStart);
        originalAnchoredPos = rectTransform.anchoredPosition;
        UIManager.Instance.HideCardInfoPopup();
        UIManager.Instance.BuyItemArea.gameObject.SetActive(true);

    }

    public void OnDrag(PointerEventData eventData)
    {
        if(CanBeDraged == false)
        return;
        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, canvas.worldCamera, out globalMousePos))
        {
            rectTransform.position = globalMousePos;
        }

    }

    public void OnEndDrag(PointerEventData eventData)
    {
       if(CanBeDraged == false)
        return;
        VibrationsManager.TryVibrate(VibrationType.Tap);
        UIManager.Instance.BuyItemArea.gameObject.SetActive(false);
        {
            isDragging = false;


            rectTransform.anchoredPosition = originalAnchoredPos;

        }


        canvasGroup.blocksRaycasts = true;

        bool slotFull = false;
        if(ArtifactData != null && ArtifactManager.Instance.ActiveArtifacts.Count >= GameManager.Instance.TheHero.myHeroData.ArtifactSlots)
            slotFull = true;
        if(ArtifactData == null && PotionData == null && RuneData == null && PackType == ShopItemType.ArtifactPack &&
           ArtifactManager.Instance.ActiveArtifacts.Count >= GameManager.Instance.TheHero.myHeroData.ArtifactSlots)
            slotFull = true;
        if(PotionData != null && PotionManager.Instance.ActivePotions.Count >= GameManager.Instance.TheHero.myHeroData.PotionSlots)
            slotFull = true;
     if(RuneData != null && RuneManager.Instance.ActiveRunes.Count >= 6)
            slotFull = true;

        bool noArtifactChoices = ArtifactData == null && PotionData == null && RuneData == null &&
                                 PackType == ShopItemType.ArtifactPack &&
                                 ArtifactManager.Instance.HasAvailableArtifactChoices() == false;
        ArtifactPackManager artifactPackManager = null;
        bool artifactPackMissing = false;
        if (ArtifactData == null && PotionData == null && RuneData == null && PackType == ShopItemType.ArtifactPack)
        {
            artifactPackManager = FindObjectOfType<ArtifactPackManager>();
            artifactPackMissing = artifactPackManager == null || artifactPackManager.IsConfigured == false;
        }


        if (IsOverSellSlot() && slotFull == false && noArtifactChoices == false && artifactPackMissing == false)
        {
            if (GameData.CurrentGold >= Price)
            {
                GameData.CurrentGold -= Price;
                VibrationsManager.TryVibrate(VibrationType.Success);
                SoundManager.TryPlay(SoundType.ShopPurchase);
                UIManager.Instance.UpdateLabels();

                if(ArtifactData != null)
                    ArtifactManager.Instance.AddArtifact(ArtifactData); // Add logic here
                else if (PotionData != null)
                    PotionManager.Instance.AddPotion(PotionData); // Add logic here
                else if (RuneData != null)
                    RuneManager.Instance.AddRune(RuneData); // Add logic here
                else if (PackType == ShopItemType.ArtifactPack)
                {
                    artifactPackManager.ShowWindow();
                }
                else
                {
                    UnitUpgradeManager.Instance.ShowWindow();
                }
                canvasGroup.blocksRaycasts = true;
                //Destroy(gameObject);
                canvasGroup.alpha = 0;
                enabled = false;
            }
            else
            {
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.not_enough_gold", "Not enough gold!"));
                SoundManager.TryPlay(SoundType.ShopItemDropCancel);
                ReturnToShop();
            }
        }
        else
        {
            if (noArtifactChoices)
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.no_artifacts_available", "No Artifacts available!"));
            else if (artifactPackMissing)
                Debug.LogWarning("Artifact Pack purchase needs a configured ArtifactPackManager in the scene.");
            else if(slotFull)
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.no_slots", "No slots!"));
            SoundManager.TryPlay(SoundType.ShopItemDropCancel);
            ReturnToShop();
        }
    }

    private bool IsOverSellSlot()
    {
            if (RectTransformUtility.RectangleContainsScreenPoint(UIManager.Instance.BuyItemArea, Input.mousePosition,Camera.main))
            {
                return true;
            }

        return false;
    }

    private void ReturnToShop()
    {
        rectTransform.anchoredPosition = originalAnchoredPos;
        UIManager.Instance.HideCardInfoPopup();
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging || longPressTriggered) return;

        if (OnClick != null)
        {
            OnClick(this);
            return;
        }

        if (!isSelected)
        {
            if(ArtifactData !=null)
            {
                    UIManager.Instance.ShowCardInfoPopup(
                   () => LocalizedContent.ArtifactName(ArtifactData),
                   () => LocalizedContent.ArtifactDescription(ArtifactData) + ArtifactData.GetRarityText(),
                   () => "",
                   transform);
            }
            else if (PotionData != null)
            {
                UIManager.Instance.ShowCardInfoPopup(
                   () => LocalizedContent.PotionName(PotionData),
                   () => LocalizedContent.PotionDescription(PotionData) + PotionData.GetRarityText(),
                   () => "",
                   transform);
            }
            else if (RuneData != null)
            {
                UIManager.Instance.ShowCardInfoPopup(
                   () => LocalizedContent.RuneName(RuneData),
                   () => LocalizedContent.RuneDescription(RuneData) + RuneData.GetRarityText(),
                   () => "",
                   transform);
            }
            else
            {
                if (PackType == ShopItemType.ArtifactPack)
                {
                    UIManager.Instance.ShowCardInfoPopup(
                               () => LocalizationService.Get("ui.shop.artifact_pack", "Artifact Pack"),
                               () => LocalizationService.Get("ui.shop.artifact_pack_description", "Choose one of three random Artifacts"),
                               () => "",
                               transform);
                    isSelected = true;
                    return;
                }

                UIManager.Instance.ShowCardInfoPopup(
                           () => LocalizationService.Get("ui.shop.unit_upgrade_pack", "Unit Upgrade Pack"),
                           () => LocalizationService.Get("ui.shop.unit_upgrade_pack_description", "Allows you to upgrade your units with Charms, Enchantments or Rank up"),
                           () => "",
                           transform);
            }

            isSelected = true;
        }
        else
        {
            //rectTransform.anchoredPosition = originalAnchoredPos;
            isSelected = false;
            UIManager.Instance.HideCardInfoPopup();
        }

    }

    void Update()
    {
        if (HoldToShowInfo == false || isHolding == false || isDragging)
            return;

        if (Vector2.Distance(pointerDownPosition, Input.mousePosition) > 10f)
        {
            isHolding = false;
            holdTimer = 0f;
            return;
        }

        holdTimer += Time.deltaTime;
        if (holdTimer < HoldInfoDuration)
            return;

        isHolding = false;
        longPressTriggered = true;
        ShowCurrentInfo();
    }

    private void ShowCurrentInfo()
    {
        if (ArtifactData != null)
        {
            UIManager.Instance.ShowCardInfoPopup(
                () => LocalizedContent.ArtifactName(ArtifactData),
                () => LocalizedContent.ArtifactDescription(ArtifactData) + ArtifactData.GetRarityText(),
                () => "",
                transform);
        }
        else if (PotionData != null)
        {
            UIManager.Instance.ShowCardInfoPopup(
                () => LocalizedContent.PotionName(PotionData),
                () => LocalizedContent.PotionDescription(PotionData) + PotionData.GetRarityText(),
                () => "",
                transform);
        }
        else if (RuneData != null)
        {
            UIManager.Instance.ShowCardInfoPopup(
                () => LocalizedContent.RuneName(RuneData),
                () => LocalizedContent.RuneDescription(RuneData) + RuneData.GetRarityText(),
                () => "",
                transform);
        }
    }

    private void RefreshLocalizedText()
    {
        if (NameLabel == null)
            return;

        if (ArtifactData != null)
            NameLabel.text = LocalizedContent.ArtifactName(ArtifactData);
        else if (PotionData != null)
            NameLabel.text = LocalizedContent.PotionName(PotionData);
        else if (RuneData != null)
            NameLabel.text = LocalizedContent.RuneName(RuneData);
        else if (PackType == ShopItemType.ArtifactPack)
            NameLabel.text = LocalizationService.Get("ui.shop.artifact_pack", "Artifact Pack");
        else
            NameLabel.text = LocalizationService.Get("ui.shop.unit_upgrade_pack", "Unit Upgrade Pack");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        VibrationsManager.TryVibrate(VibrationType.Tap);
        SoundManager.TryPlay(SoundType.Tap);
        longPressTriggered = false;
        isHolding = true;
        holdTimer = 0f;
        pointerDownPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        holdTimer = 0f;

        if (HoldToShowInfo)
            return;

        UIManager.Instance.HideCardInfoPopup();
    }
}
