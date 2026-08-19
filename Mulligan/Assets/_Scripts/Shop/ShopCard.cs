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
    public void Init(ArtifactData aData)
    {
        string artifactName = aData.name;
        if( artifactName.Contains("RandomRace"))
        {
            artifactName = artifactName.Replace("RandomRace",aData.RandomRace.ToString());
        }
        NameLabel.text = artifactName;
        NameLabel.color = UIManager.Instance.GetTextColor(aData.rarity);

        ArtifactData = aData;
        Price = 6;
        PotionData = null;
        RuneData = null;

        if (ShopManager.Instance.SetEverythingFreeNextRound)
            Price = 0;

        Price = (int)(Price * (1-GameManager.Instance.MarketDiscountModifier));

        PriceLabel.text = Price.ToString();
    }
    public void Init(RuneData aData)
    {
        NameLabel.text = aData.name;
        NameLabel.color = UIManager.Instance.GetTextColor((int)aData.rarity);

        RuneData = aData;
        Price = 12;
        PotionData = null;
        ArtifactData = null;

        if (ShopManager.Instance.SetEverythingFreeNextRound)
            Price = 0;
        Price = (int)(Price * (1-GameManager.Instance.MarketDiscountModifier));

        PriceLabel.text = Price.ToString();
    }
    public void Init(PotionCardData aData)
    {
        ArtifactData = null;
        RuneData = null;
        NameLabel.text = aData.name;
        NameLabel.color = UIManager.Instance.GetTextColor((int)aData.rarity);

        PotionData = aData;
        Price = 3;
        if (ShopManager.Instance.SetEverythingFreeNextRound)
            Price = 0;
        Price = (int)(Price * (1-GameManager.Instance.MarketDiscountModifier));

        PriceLabel.text = Price.ToString();
    }
    public void Init(int aCost)
    {
        // NameLabel.text = "Army Upgrade";
        ArtifactData = null;
        PotionData = null;
        RuneData = null;
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
        if(PotionData != null && PotionManager.Instance.ActivePotions.Count >= GameManager.Instance.TheHero.myHeroData.PotionSlots)
            slotFull = true;
     if(RuneData != null && RuneManager.Instance.ActiveRunes.Count >= 6)
            slotFull = true;


        if (IsOverSellSlot() && slotFull == false)
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
                UIManager.Instance.ShowTooltip("Not enough gold!");
                SoundManager.TryPlay(SoundType.ShopItemDropCancel);
                ReturnToShop();
            }
        }
        else
        {
            if( ArtifactManager.Instance.ActiveArtifacts.Count >= GameManager.Instance.TheHero.myHeroData.ArtifactSlots)
                UIManager.Instance.ShowTooltip("No slots!");
            else if( PotionManager.Instance.ActivePotions.Count >= GameManager.Instance.TheHero.myHeroData.PotionSlots)
                UIManager.Instance.ShowTooltip("No slots!");
            else if( RuneManager.Instance.ActiveRunes.Count >= 6)
                UIManager.Instance.ShowTooltip("No slots!");
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
        if (isDragging) return;

        if (!isSelected)
        {
            if(ArtifactData !=null)
            {
                    UIManager.Instance.ShowCardInfoPopup(
                   NameLabel.text,
                   ArtifactData.description+ ArtifactData.GetRarityText(),
                   "",
                   transform
               );
            }
            else if (PotionData != null)
            {
                UIManager.Instance.ShowCardInfoPopup(
                   PotionData.name,
                   PotionData.description+ PotionData.GetRarityText(),
                   "",
                   transform
                          );
            }
            else if (RuneData != null)
            {
                UIManager.Instance.ShowCardInfoPopup(
                   RuneData.name,
                   RuneData.description+ RuneData.GetRarityText(),
                   "",
                   transform
                          );
            }
            else
            {
                UIManager.Instance.ShowCardInfoPopup(
                           "Unit Upgrade Pack",
                           "Allows you to upgrade your units with Charms, Enchantments or Rank up",
                           "",
                           transform
                       );
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

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        VibrationsManager.TryVibrate(VibrationType.Tap);
        SoundManager.TryPlay(SoundType.Tap);
        isHolding = true;
        holdTimer = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        holdTimer = 0f;
        UIManager.Instance.HideCardInfoPopup();
    }
}
