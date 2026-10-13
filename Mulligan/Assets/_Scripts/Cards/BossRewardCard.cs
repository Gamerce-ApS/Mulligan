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
    [Header("Backside Text")]
    public TMP_Text BacksideNameLabel;
    public TMP_Text BacksideDescriptionLabel;
    public TMP_Text BacksideRarityLabel;
    [Header("Card Flip")]
    public GameObject FrontsideGameObject;
    public GameObject BacksideGameObject;
    public float FlipDuration = 0.3f;

    public bool IsFlipped { get; private set; }
    public bool IsFlipping { get; private set; }
    private Quaternion neutralRotation = Quaternion.identity;
    private Coroutine flipRoutine;
    public bool HoldToShowInfo = true;
    public float HoldInfoDuration = 0.5f;

    public BossRewardData RewardData { get; private set; }
    public Action<BossRewardCard> OnClick;

    private Func<string> targetNameGetter;
    private bool isHolding;
    private bool longPressTriggered;
    private float holdTimer;

    private void Awake()
    {
        neutralRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        LocalizationService.LanguageChanged += RefreshLocalizedText;
    }

    private void OnDisable()
    {
        LocalizationService.LanguageChanged -= RefreshLocalizedText;
        ResetHold();
        ResetFlip();
    }

    public void Init(BossRewardData data, Func<string> aTargetNameGetter, Action<BossRewardCard> onClick)
    {
        RewardData = data;
        targetNameGetter = aTargetNameGetter;
        OnClick = onClick;
        neutralRotation = transform.localRotation;
        ResetFlip();

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
        if (IsFlipping)
            return;
        if (longPressTriggered)
        {
            longPressTriggered = false;
            return;
        }

        OnClick?.Invoke(this);
    }

    public void SetFlipped(bool flipped)
    {
        if (IsFlipped == flipped && IsFlipping == false)
            return;

        if (flipRoutine != null)
            StopCoroutine(flipRoutine);
        flipRoutine = StartCoroutine(Flip(flipped));
    }

    private System.Collections.IEnumerator Flip(bool flipped)
    {
        IsFlipping = true;
        float halfDuration = Mathf.Max(0.01f, FlipDuration * 0.5f);
        Quaternion startingRotation = transform.localRotation;
        Quaternion edgeRotation = neutralRotation * Quaternion.Euler(0f, 90f, 0f);
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localRotation = Quaternion.Slerp(startingRotation, edgeRotation,
                Mathf.SmoothStep(0f, 1f, elapsed / halfDuration));
            yield return null;
        }

        IsFlipped = flipped;
        SetVisibleSide(flipped);
        startingRotation = neutralRotation * Quaternion.Euler(0f, -90f, 0f);
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localRotation = Quaternion.Slerp(startingRotation, neutralRotation,
                Mathf.SmoothStep(0f, 1f, elapsed / halfDuration));
            yield return null;
        }

        transform.localRotation = neutralRotation;
        IsFlipping = false;
        flipRoutine = null;
    }

    private void ResetFlip()
    {
        if (flipRoutine != null)
            StopCoroutine(flipRoutine);
        flipRoutine = null;
        IsFlipped = false;
        IsFlipping = false;
        transform.localRotation = neutralRotation;
        SetVisibleSide(false);
    }

    private void SetVisibleSide(bool backside)
    {
        if (FrontsideGameObject != null && FrontsideGameObject != gameObject)
            FrontsideGameObject.SetActive(!backside);
        else
        {
            if (NameLabel != null) NameLabel.gameObject.SetActive(!backside);
            if (DescriptionLabel != null) DescriptionLabel.gameObject.SetActive(!backside);
            if (RarityLabel != null) RarityLabel.gameObject.SetActive(!backside);
            if (Icon != null) Icon.gameObject.SetActive(!backside && Icon.sprite != null);
        }

        if (BacksideGameObject != null && BacksideGameObject != gameObject)
            BacksideGameObject.SetActive(backside);
    }

    private void RefreshLocalizedText()
    {
        if (RewardData == null)
            return;

        string targetName = targetNameGetter != null ? targetNameGetter() : null;
        object[] formatValues = string.IsNullOrEmpty(targetName)
            ? Array.Empty<object>()
            : new object[] { targetName };

        string rewardName = LocalizedContent.BossRewardName(RewardData, formatValues);
        string description = LocalizedContent.BossRewardDescription(RewardData, formatValues);
        RarityType rarity = (RarityType)Mathf.Clamp(RewardData.rarity, 0, 3);
        string rarityText = LocalizedContent.Rarity(rarity);
        Color rarityColor = UIManager.Instance.GetTextColor((int)rarity);

        if (NameLabel != null)
        {
            NameLabel.text = rewardName;
            NameLabel.color = rarityColor;
        }

        if (DescriptionLabel != null)
            DescriptionLabel.text = description;

        if (RarityLabel != null)
        {
            RarityLabel.text = rarityText;
            RarityLabel.color = rarityColor;
        }

        if (BacksideNameLabel != null)
        {
            BacksideNameLabel.text = rewardName;
            BacksideNameLabel.color = rarityColor;
        }

        if (BacksideDescriptionLabel != null)
            BacksideDescriptionLabel.text = description;

        if (BacksideRarityLabel != null)
        {
            BacksideRarityLabel.text = rarityText;
            BacksideRarityLabel.color = rarityColor;
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
