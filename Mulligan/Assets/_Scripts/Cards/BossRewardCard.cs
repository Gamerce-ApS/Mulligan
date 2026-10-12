using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BossRewardCard : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public TMP_Text NameLabel;
    public TMP_Text DescriptionLabel;
    public TMP_Text RarityLabel;
    public Image Icon;
    public bool HoldToShowInfo = true;
    public float HoldInfoDuration = 0.5f;

    public BossRewardData RewardData { get; private set; }
    public Action<BossRewardCard> OnClick;

    private Func<string> targetNameGetter;
    private bool isHolding;
    private bool longPressTriggered;
    private float holdTimer;

    private void OnEnable()
    {
        LocalizationService.LanguageChanged += RefreshLocalizedText;
    }

    private void OnDisable()
    {
        LocalizationService.LanguageChanged -= RefreshLocalizedText;
        ResetHold();
    }

    public void Init(BossRewardData data, Func<string> aTargetNameGetter, Action<BossRewardCard> onClick)
    {
        RewardData = data;
        targetNameGetter = aTargetNameGetter;
        OnClick = onClick;

        if (Icon != null)
        {
            Icon.sprite = data != null && string.IsNullOrWhiteSpace(data.sprite_icon) == false
                ? Resources.Load<Sprite>(data.sprite_icon)
                : null;
            Icon.gameObject.SetActive(Icon.sprite != null);
            Icon.raycastTarget = false;
        }

        RefreshLocalizedText();
    }

    private void Update()
    {
        if (isHolding == false || HoldToShowInfo == false || longPressTriggered)
            return;

        holdTimer += Time.unscaledDeltaTime;
        if (holdTimer < HoldInfoDuration)
            return;

        longPressTriggered = true;
        ShowInfo();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        longPressTriggered = false;
        holdTimer = 0f;
        SoundManager.TryPlay(SoundType.CardTap);
        VibrationsManager.TryVibrate(VibrationType.CardTap);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        holdTimer = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHold();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (longPressTriggered)
        {
            longPressTriggered = false;
            return;
        }

        OnClick?.Invoke(this);
    }

    private void RefreshLocalizedText()
    {
        if (RewardData == null)
            return;

        string targetName = targetNameGetter != null ? targetNameGetter() : null;
        object[] formatValues = string.IsNullOrEmpty(targetName)
            ? Array.Empty<object>()
            : new object[] { targetName };

        if (NameLabel != null)
        {
            NameLabel.text = LocalizedContent.BossRewardName(RewardData, formatValues);
            NameLabel.color = UIManager.Instance.GetTextColor(RewardData.rarity);
        }

        if (DescriptionLabel != null)
            DescriptionLabel.text = LocalizedContent.BossRewardDescription(RewardData, formatValues);

        if (RarityLabel != null)
        {
            RarityType rarity = (RarityType)Mathf.Clamp(RewardData.rarity, 0, 3);
            RarityLabel.text = LocalizedContent.Rarity(rarity);
            RarityLabel.color = UIManager.Instance.GetTextColor((int)rarity);
        }
    }

    private void ShowInfo()
    {
        if (RewardData == null || UIManager.Instance == null)
            return;

        UIManager.Instance.ShowCardInfoPopup(
            () => GetDisplayName(),
            () => GetDisplayDescription(),
            () => "",
            transform);
    }

    private string GetDisplayName()
    {
        string targetName = targetNameGetter != null ? targetNameGetter() : null;
        return string.IsNullOrEmpty(targetName)
            ? LocalizedContent.BossRewardName(RewardData)
            : LocalizedContent.BossRewardName(RewardData, targetName);
    }

    private string GetDisplayDescription()
    {
        string targetName = targetNameGetter != null ? targetNameGetter() : null;
        string description = string.IsNullOrEmpty(targetName)
            ? LocalizedContent.BossRewardDescription(RewardData)
            : LocalizedContent.BossRewardDescription(RewardData, targetName);
        RarityType rarity = (RarityType)Mathf.Clamp(RewardData.rarity, 0, 3);
        string rarityColor = ColorUtility.ToHtmlStringRGB(UIManager.Instance.GetTextColor((int)rarity));
        return description + "\n\n<color=#" + rarityColor + ">" +
               LocalizedContent.Rarity(rarity) + "</color>";
    }

    private void ResetHold()
    {
        isHolding = false;
        holdTimer = 0f;
        longPressTriggered = false;
    }
}
