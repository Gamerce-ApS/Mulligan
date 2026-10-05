using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactPackManager : Singleton<ArtifactPackManager>
{
    private class ArtifactChoice
    {
        public ArtifactData Data;
        public ShopCard Card;
        public RectTransform RectTransform;
        public Vector2 TargetPosition;
        public Vector3 TargetScale;
    }

    [Header("Window")]
    public GameObject ShopWindow;
    public CanvasGroup bgCanvasGroup;
    public Transform ArtifactParent;
    public GameObject ArtifactTemplate;

    [Header("Animation")]
    public Vector2 ArtifactEnterOffset = new Vector2(0f, -1000f);
    public Vector2 ArtifactExitOffset = new Vector2(0f, -1000f);
    public Vector2 SelectedCenterOffset = Vector2.zero;
    public float BackgroundFadeDuration = 0.25f;
    public float ArtifactEnterDuration = 0.5f;
    public float ArtifactEnterStagger = 0.1f;
    public float OtherArtifactsExitDuration = 0.35f;
    public float SelectedCenterDuration = 0.4f;
    public float SelectedHoldDuration = 0.8f;
    [Tooltip("Duration for the selected artifact to move from the center to its artifact slot.")]
    public float SelectedExitDuration = 0.4f;
    public float SelectedScale = 1.15f;

    private readonly List<ArtifactChoice> choices = new List<ArtifactChoice>();
    private System.Action onHide;
    private bool acceptingInput;
    private bool isClosing;
    private CanvasGroup destinationSlotCanvasGroup;
    private float destinationSlotOriginalAlpha = 1f;

    public bool IsConfigured => ShopWindow != null &&
                                bgCanvasGroup != null &&
                                ArtifactParent != null &&
                                ArtifactTemplate != null;

    public void Init()
    {
        acceptingInput = false;
        isClosing = false;
    }

    public void ShowWindow(System.Action onComplete = null)
    {
        if (IsConfigured == false)
        {
            Debug.LogWarning("ArtifactPackManager is missing window, parent, or artifact template references.");
            return;
        }

        if (ShopWindow.activeSelf || isClosing)
            return;

        if (ArtifactManager.Instance.ActiveArtifacts.Count >= GameManager.Instance.TheHero.myHeroData.ArtifactSlots)
        {
            UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.no_slots", "No slots!"));
            return;
        }

        List<ArtifactData> artifacts = ArtifactManager.Instance.GetRandomArtifactChoices(3);
        if (artifacts.Count == 0)
        {
            UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.no_artifacts_available", "No Artifacts available!"));
            return;
        }

        ClearChoices();
        onHide = onComplete;
        acceptingInput = false;
        isClosing = false;

        bgCanvasGroup.gameObject.SetActive(true);
        bgCanvasGroup.alpha = 0f;
        ShopWindow.SetActive(true);

        SpawnChoices(artifacts);
        if (choices.Count == 0)
        {
            ShopWindow.SetActive(false);
            if (bgCanvasGroup.gameObject != ShopWindow)
                bgCanvasGroup.gameObject.SetActive(false);
            return;
        }

        PrepareChoicePositions();

        LeanTween.alphaCanvas(bgCanvasGroup, 1f, BackgroundFadeDuration).setEaseOutQuad();
        SoundManager.TryPlay(SoundType.WindowOpen);

        for (int i = 0; i < choices.Count; i++)
        {
            ArtifactChoice choice = choices[i];
            choice.RectTransform.anchoredPosition = choice.TargetPosition + ArtifactEnterOffset;
            LeanTween.move(choice.RectTransform, choice.TargetPosition, ArtifactEnterDuration)
                .setEaseOutBack()
                .setDelay(i * ArtifactEnterStagger);
        }

        float inputDelay = ArtifactEnterDuration + Mathf.Max(0, choices.Count - 1) * ArtifactEnterStagger;
        LeanTween.delayedCall(ShopWindow, inputDelay, () =>
        {
            if (ShopWindow.activeSelf && isClosing == false)
                acceptingInput = true;
        });
    }

    public void HideWindow()
    {
        if (ShopWindow == null || ShopWindow.activeSelf == false || isClosing)
            return;

        acceptingInput = false;
        isClosing = true;
        SoundManager.TryPlay(SoundType.WindowClose);

        foreach (ArtifactChoice choice in choices)
        {
            LeanTween.cancel(choice.Card.gameObject);
            LeanTween.move(choice.RectTransform, choice.RectTransform.anchoredPosition + ArtifactExitOffset, OtherArtifactsExitDuration)
                .setEaseInBack();
        }

        FadeOutAndClose(Mathf.Max(OtherArtifactsExitDuration, BackgroundFadeDuration));
    }

    private void SpawnChoices(List<ArtifactData> artifacts)
    {
        foreach (ArtifactData artifact in artifacts)
        {
            GameObject visual = Instantiate(ArtifactTemplate, ArtifactParent);
            visual.SetActive(true);

            ShopCard shopCard = visual.GetComponent<ShopCard>();
            if (shopCard == null)
            {
                Debug.LogWarning("Artifact Pack template needs a ShopCard component.");
                Destroy(visual);
                continue;
            }

            shopCard.Init(artifact);
            shopCard.CanBeDraged = false;
            shopCard.HoldToShowInfo = true;
            shopCard.OnClick = SelectArtifact;
            HidePrice(shopCard);

            choices.Add(new ArtifactChoice
            {
                Data = artifact,
                Card = shopCard,
                RectTransform = visual.GetComponent<RectTransform>(),
                TargetScale = visual.transform.localScale
            });
        }
    }

    private void PrepareChoicePositions()
    {
        Canvas.ForceUpdateCanvases();

        RectTransform parentRect = ArtifactParent as RectTransform;
        if (parentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

        Canvas.ForceUpdateCanvases();

        foreach (ArtifactChoice choice in choices)
        {
            choice.TargetPosition = choice.RectTransform.anchoredPosition;

            LayoutElement layoutElement = choice.Card.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = choice.Card.gameObject.AddComponent<LayoutElement>();

            layoutElement.ignoreLayout = true;
        }
    }

    private void SelectArtifact(ShopCard selectedCard)
    {
        if (acceptingInput == false || isClosing)
            return;

        ArtifactChoice selectedChoice = choices.Find(choice => choice.Card == selectedCard);
        if (selectedChoice == null)
            return;

        acceptingInput = false;
        isClosing = true;
        UIManager.Instance.HideCardInfoPopup();

        foreach (ArtifactChoice choice in choices)
            choice.Card.OnClick = null;

        ArtifactManager.Instance.AddArtifact(selectedChoice.Data, false);
        VibrationsManager.TryVibrate(VibrationType.Success);

        RectTransform destinationSlot = GetDestinationSlot(selectedChoice.Data);
        HideDestinationSlot(destinationSlot);

        Vector2 centerPosition = GetChoiceCenterPosition();
        selectedChoice.RectTransform.SetAsLastSibling();

        foreach (ArtifactChoice choice in choices)
        {
            LeanTween.cancel(choice.Card.gameObject);
            if (choice == selectedChoice)
                continue;

            LeanTween.move(choice.RectTransform, choice.TargetPosition + ArtifactExitOffset, OtherArtifactsExitDuration)
                .setEaseInBack();
        }

        LeanTween.move(selectedChoice.RectTransform, centerPosition, SelectedCenterDuration)
            .setEaseInOutQuad();
        LeanTween.scale(selectedChoice.Card.gameObject, selectedChoice.TargetScale * SelectedScale, SelectedCenterDuration)
            .setEaseOutBack();

        LeanTween.delayedCall(selectedChoice.Card.gameObject, SelectedCenterDuration + SelectedHoldDuration, () =>
        {
            SoundManager.TryPlay(SoundType.WindowClose);
            LeanTween.alphaCanvas(bgCanvasGroup, 0f, BackgroundFadeDuration).setEaseInQuad();

            if (destinationSlot == null)
            {
                LeanTween.move(selectedChoice.RectTransform, centerPosition + ArtifactExitOffset, SelectedExitDuration)
                    .setEaseInBack();
                LeanTween.delayedCall(ShopWindow, Mathf.Max(SelectedExitDuration, BackgroundFadeDuration), CompleteClose);
                return;
            }

            Vector3 destinationScale = GetScaleForDestination(selectedChoice.RectTransform, destinationSlot);
            LeanTween.move(selectedChoice.Card.gameObject, destinationSlot.position, SelectedExitDuration)
                .setEaseInOutCubic()
                .setOnComplete(() =>
                {
                    ShowDestinationSlot();
                    CompleteClose();
                });
            LeanTween.scale(selectedChoice.Card.gameObject, destinationScale, SelectedExitDuration)
                .setEaseInOutCubic();
        });
    }

    private RectTransform GetDestinationSlot(ArtifactData artifact)
    {
        Canvas.ForceUpdateCanvases();

        RectTransform slotParent = UIManager.Instance.ArtifactSlotParent as RectTransform;
        if (slotParent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotParent);

        Canvas.ForceUpdateCanvases();

        foreach (Transform child in UIManager.Instance.ArtifactSlotParent)
        {
            Artifact visual = child.GetComponent<Artifact>();
            if (visual != null && visual.ArtifactData == artifact)
                return visual.GetComponent<RectTransform>();
        }

        return null;
    }

    private void HideDestinationSlot(RectTransform destinationSlot)
    {
        ShowDestinationSlot();

        if (destinationSlot == null)
            return;

        destinationSlotCanvasGroup = destinationSlot.GetComponent<CanvasGroup>();
        if (destinationSlotCanvasGroup == null)
            destinationSlotCanvasGroup = destinationSlot.gameObject.AddComponent<CanvasGroup>();

        destinationSlotOriginalAlpha = destinationSlotCanvasGroup.alpha;
        destinationSlotCanvasGroup.alpha = 0f;
    }

    private void ShowDestinationSlot()
    {
        if (destinationSlotCanvasGroup != null)
            destinationSlotCanvasGroup.alpha = destinationSlotOriginalAlpha;

        destinationSlotCanvasGroup = null;
        destinationSlotOriginalAlpha = 1f;
    }

    private Vector3 GetScaleForDestination(RectTransform source, RectTransform destination)
    {
        float sourceWidth = source.rect.width * Mathf.Abs(source.lossyScale.x);
        float sourceHeight = source.rect.height * Mathf.Abs(source.lossyScale.y);
        float destinationWidth = destination.rect.width * Mathf.Abs(destination.lossyScale.x);
        float destinationHeight = destination.rect.height * Mathf.Abs(destination.lossyScale.y);

        if (sourceWidth <= 0f || sourceHeight <= 0f || destinationWidth <= 0f || destinationHeight <= 0f)
            return source.localScale;

        float scaleRatio = Mathf.Min(destinationWidth / sourceWidth, destinationHeight / sourceHeight);
        return source.localScale * scaleRatio;
    }

    private Vector2 GetChoiceCenterPosition()
    {
        if (choices.Count == 0)
            return SelectedCenterOffset;

        Vector2 center = Vector2.zero;
        foreach (ArtifactChoice choice in choices)
            center += choice.TargetPosition;

        return center / choices.Count + SelectedCenterOffset;
    }

    private void FadeOutAndClose(float closeDelay)
    {
        LeanTween.alphaCanvas(bgCanvasGroup, 0f, BackgroundFadeDuration).setEaseInQuad();
        LeanTween.delayedCall(ShopWindow, closeDelay, CompleteClose);
    }

    private void CompleteClose()
    {
        ShowDestinationSlot();

        System.Action callback = onHide;
        onHide = null;

        ShopWindow.SetActive(false);
        if (bgCanvasGroup.gameObject != ShopWindow)
            bgCanvasGroup.gameObject.SetActive(false);

        ClearChoices();
        acceptingInput = false;
        isClosing = false;
        callback?.Invoke();
    }

    private void ClearChoices()
    {
        foreach (ArtifactChoice choice in choices)
        {
            if (choice.Card != null)
            {
                LeanTween.cancel(choice.Card.gameObject);
                Destroy(choice.Card.gameObject);
            }
        }

        choices.Clear();
    }

    private void HidePrice(ShopCard shopCard)
    {
        if (shopCard.PriceLabel == null)
            return;

        GameObject priceObject = shopCard.PriceLabel.transform.parent != null
            ? shopCard.PriceLabel.transform.parent.gameObject
            : shopCard.PriceLabel.gameObject;
        priceObject.SetActive(false);
    }
}
