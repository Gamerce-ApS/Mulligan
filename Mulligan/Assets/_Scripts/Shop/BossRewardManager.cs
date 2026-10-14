using System;
using System.Collections.Generic;
using System.Linq;
using GameAnalyticsSDK;
using UnityEngine;
using UnityEngine.UI;

public class BossRewardResult
{
    public int GoldGained;
    public int HealthGained;
}

public class BossRewardManager : Singleton<BossRewardManager>
{
    private class BossRewardChoice
    {
        public BossRewardData Data;
        public BossRewardCard Card;
        public RectTransform RectTransform;
        public Vector2 TargetPosition;
        public Vector3 TargetScale;
        public bool HasPowerTarget;
        public bool TargetsRace;
        public CardRace Race;
        public CardClass Class;
    }

    [Header("Window")]
    public GameObject ShopWindow;
    public CanvasGroup bgCanvasGroup;
    public Transform RewardParent;
    public GameObject RewardTemplate;
    public TMPro.TMP_Text TapAgainToSelectText;

    [Header("Animation")]
    public Vector2 RewardEnterOffset = new Vector2(0f, -1000f);
    public Vector2 RewardExitOffset = new Vector2(0f, -1000f);
    public Vector2 SelectedCenterOffset = Vector2.zero;
    public float BackgroundFadeDuration = 0.25f;
    public float RewardEnterDuration = 0.5f;
    public float RewardEnterStagger = 0.1f;
    public float OtherRewardsExitDuration = 0.35f;
    public float SelectedCenterDuration = 0.4f;
    public float SelectedHoldDuration = 0.8f;
    public float SelectedExitDuration = 0.4f;
    public float SelectedScale = 1.15f;

    private readonly List<BossRewardChoice> choices = new List<BossRewardChoice>();
    private Action<BossRewardResult> onHide;
    private bool acceptingInput;
    private bool isClosing;
    private BossRewardCard flippedCard;

    private void OnEnable()
    {
        LocalizationService.LanguageChanged += UpdateSelectionHint;
    }

    private void OnDisable()
    {
        LocalizationService.LanguageChanged -= UpdateSelectionHint;
    }

    private void Update()
    {
        if (acceptingInput == false || isClosing || flippedCard == null)
            return;

        Vector2 pointerPosition;
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began)
                return;
            pointerPosition = touch.position;
        }
        else
        {
            if (Input.GetMouseButtonDown(0) == false)
                return;
            pointerPosition = Input.mousePosition;
        }

        foreach (BossRewardChoice choice in choices)
        {
            Canvas canvas = choice.Card.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(choice.RectTransform, pointerPosition, eventCamera))
                return;
        }

        flippedCard.SetFlipped(false);
        flippedCard = null;
        UpdateSelectionHint();
    }

    private void UpdateSelectionHint()
    {
        if (TapAgainToSelectText == null)
            return;

        TapAgainToSelectText.text = LocalizationService.Get("ui.boss_reward.tap_again_to_select", "Tap again to select");
        TapAgainToSelectText.gameObject.SetActive(flippedCard != null && isClosing == false);
    }

    public bool IsConfigured => ShopWindow != null &&
                                bgCanvasGroup != null &&
                                RewardParent != null &&
                                RewardTemplate != null;

    public void Init()
    {
        acceptingInput = false;
        isClosing = false;
        UpdateSelectionHint();
    }

    public void ShowWindow(Action<BossRewardResult> onComplete = null)
    {
        if (IsConfigured == false)
        {
            Debug.LogWarning("BossRewardManager is missing window, background, parent, or reward template references.");
            onComplete?.Invoke(new BossRewardResult());
            return;
        }

        if (ShopWindow.activeSelf || isClosing)
        {
            onComplete?.Invoke(new BossRewardResult());
            return;
        }

        List<BossRewardChoice> rolledChoices = GetRandomChoices(3);
        if (rolledChoices.Count == 0)
        {
            Debug.LogWarning("No eligible Boss Rewards are available.");
            onComplete?.Invoke(new BossRewardResult());
            return;
        }

        if (rolledChoices.Count < 3)
            Debug.LogWarning("Fewer than three unique eligible Boss Rewards are available.");

        ClearChoices();
        choices.AddRange(rolledChoices);
        onHide = onComplete;
        acceptingInput = false;
        isClosing = false;

        bgCanvasGroup.gameObject.SetActive(true);
        bgCanvasGroup.alpha = 0f;
        ShopWindow.SetActive(true);

        SpawnChoices();
        if (choices.Count == 0)
        {
            CompleteClose(new BossRewardResult(), null);
            return;
        }

        PrepareChoicePositions();
        LeanTween.alphaCanvas(bgCanvasGroup, 1f, BackgroundFadeDuration).setEaseOutQuad();
        SoundManager.TryPlay(SoundType.WindowOpen);

        for (int i = 0; i < choices.Count; i++)
        {
            BossRewardChoice choice = choices[i];
            choice.RectTransform.anchoredPosition = choice.TargetPosition + RewardEnterOffset;
            LeanTween.move(choice.RectTransform, choice.TargetPosition, RewardEnterDuration)
                .setEaseOutBack()
                .setDelay(i * RewardEnterStagger);
        }

        float inputDelay = RewardEnterDuration + Mathf.Max(0, choices.Count - 1) * RewardEnterStagger;
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
        UpdateSelectionHint();
        SoundManager.TryPlay(SoundType.WindowClose);

        foreach (BossRewardChoice choice in choices)
        {
            if (choice.Card == null)
                continue;

            LeanTween.cancel(choice.Card.gameObject);
            LeanTween.move(choice.RectTransform, choice.RectTransform.anchoredPosition + RewardExitOffset, OtherRewardsExitDuration)
                .setEaseInBack();
        }

        LeanTween.alphaCanvas(bgCanvasGroup, 0f, BackgroundFadeDuration).setEaseInQuad();
        float delay = Mathf.Max(OtherRewardsExitDuration, BackgroundFadeDuration);
        LeanTween.delayedCall(ShopWindow, delay, () => CompleteClose(new BossRewardResult(), null));
    }

    public void DebugShowWindow()
    {
        ShowWindow();
    }

    private List<BossRewardChoice> GetRandomChoices(int amount)
    {
        List<BossRewardChoice> result = new List<BossRewardChoice>();
        if (amount <= 0 || CardContainer.Instance.BossRewardDataList == null)
            return result;

        List<BossRewardData> available = CardContainer.Instance.BossRewardDataList
            .Where(IsRewardEligible)
            .ToList();

        while (result.Count < amount && available.Count > 0)
        {
            BossRewardData selected = PickByRarity(available);
            if (selected == null)
                break;

            BossRewardChoice choice = new BossRewardChoice { Data = selected };
            ConfigurePowerTarget(choice);
            result.Add(choice);

            available.RemoveAll(reward => reward == null || reward.effect == selected.effect);
        }

        return result;
    }

    private bool IsRewardEligible(BossRewardData reward)
    {
        if (reward == null || reward.rarity < 0 || reward.rarity > 3)
            return false;

        switch (reward.effect)
        {
            case BossRewardEffect.Cleanup:
                return CardContainer.Instance.GetAllOwnedCards()
                    .Any(card => card.data.cardClass == CardClass.Peasant);

            case BossRewardEffect.RandomArtifact:
                return HasArtifactRoom() && ArtifactManager.Instance.HasAvailableArtifactChoices();

            case BossRewardEffect.ArtifactPack:
                ArtifactPackManager artifactPackManager = FindObjectOfType<ArtifactPackManager>(true);
                return HasArtifactRoom() &&
                       ArtifactManager.Instance.HasAvailableArtifactChoices() &&
                       artifactPackManager != null &&
                       artifactPackManager.IsConfigured;

            case BossRewardEffect.Healing:
                return GameManager.Instance.TheHero != null &&
                       GameManager.Instance.TheHero.Health < GameManager.Instance.TheHero.MaxHealth &&
                       GetHealingPotion(reward.rarity) != null;

            default:
                return true;
        }
    }

    private bool HasArtifactRoom()
    {
        return GameManager.Instance.TheHero != null &&
               GameManager.Instance.TheHero.myHeroData != null &&
               ArtifactManager.Instance.ActiveArtifacts.Count < GameManager.Instance.TheHero.myHeroData.ArtifactSlots;
    }

    private BossRewardData PickByRarity(List<BossRewardData> available)
    {
        if (available == null || available.Count == 0)
            return null;

        int rolled = (int)CardContainer.Instance.GetRandomRarity();
        List<BossRewardData> pool = available.Where(reward => reward.rarity == rolled).ToList();
        if (pool.Count > 0)
            return pool[UnityEngine.Random.Range(0, pool.Count)];

        for (int rarity = rolled - 1; rarity >= 0; rarity--)
        {
            pool = available.Where(reward => reward.rarity == rarity).ToList();
            if (pool.Count > 0)
                return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        for (int rarity = rolled + 1; rarity <= 3; rarity++)
        {
            pool = available.Where(reward => reward.rarity == rarity).ToList();
            if (pool.Count > 0)
                return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        return available[UnityEngine.Random.Range(0, available.Count)];
    }

    private void ConfigurePowerTarget(BossRewardChoice choice)
    {
        if (choice.Data.effect != BossRewardEffect.PermanentPower)
            return;

        choice.HasPowerTarget = true;
        choice.TargetsRace = UnityEngine.Random.value < 0.5f;
        if (choice.TargetsRace)
        {
            choice.Race = (CardRace)UnityEngine.Random.Range(0, (int)CardRace.END);
        }
        else
        {
            Array classes = Enum.GetValues(typeof(CardClass));
            choice.Class = (CardClass)classes.GetValue(UnityEngine.Random.Range(0, classes.Length));
        }
    }

    private void SpawnChoices()
    {
        List<BossRewardChoice> invalidChoices = new List<BossRewardChoice>();
        for (int i = 0; i < choices.Count; i++)
        {
            BossRewardChoice choice = choices[i];
            GameObject visual = Instantiate(RewardTemplate, RewardParent);
            visual.SetActive(true);

            BossRewardCard card = visual.GetComponent<BossRewardCard>();
            RectTransform rectTransform = visual.GetComponent<RectTransform>();
            if (card == null || rectTransform == null)
            {
                Debug.LogWarning("Boss Reward template needs BossRewardCard and RectTransform components.");
                Destroy(visual);
                invalidChoices.Add(choice);
                continue;
            }

            choice.Card = card;
            choice.RectTransform = rectTransform;
            choice.TargetScale = visual.transform.localScale;
            card.HoldToShowInfo = false;
            card.Init(choice.Data, () => GetTargetName(choice), HandleRewardTap);
        }

        choices.RemoveAll(choice => invalidChoices.Contains(choice));
    }

    private void PrepareChoicePositions()
    {
        Canvas.ForceUpdateCanvases();
        RectTransform parentRect = RewardParent as RectTransform;
        if (parentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        Canvas.ForceUpdateCanvases();

        foreach (BossRewardChoice choice in choices)
        {
            choice.TargetPosition = choice.RectTransform.anchoredPosition;
            LayoutElement layoutElement = choice.Card.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = choice.Card.gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
        }
    }

    private void HandleRewardTap(BossRewardCard card)
    {
        if (acceptingInput == false || isClosing || card.IsFlipping)
            return;

        if (flippedCard == card && card.IsFlipped)
        {
            SelectReward(card);
            return;
        }

        if (flippedCard != null)
            flippedCard.SetFlipped(false);

        UIManager.Instance.HideCardInfoPopup();
        flippedCard = card;
        card.SetFlipped(true);
        UpdateSelectionHint();
    }

    private void SelectReward(BossRewardCard selectedCard)
    {
        if (acceptingInput == false || isClosing)
            return;

        BossRewardChoice selectedChoice = choices.Find(choice => choice.Card == selectedCard);
        if (selectedChoice == null)
            return;

        acceptingInput = false;
        isClosing = true;
        UpdateSelectionHint();
        UIManager.Instance.HideCardInfoPopup();
        GameAnalytics.NewDesignEvent("boss_reward:selected:" + selectedChoice.Data.name);
        BossRewardResult result = ApplyReward(selectedChoice);

        foreach (BossRewardChoice choice in choices)
            choice.Card.OnClick = null;

        selectedChoice.RectTransform.SetAsLastSibling();
        Vector2 centerPosition = GetChoiceCenterPosition();
        foreach (BossRewardChoice choice in choices)
        {
            LeanTween.cancel(choice.Card.gameObject);
            if (choice == selectedChoice)
                continue;

            LeanTween.move(choice.RectTransform, choice.TargetPosition + RewardExitOffset, OtherRewardsExitDuration)
                .setEaseInBack();
        }

        LeanTween.move(selectedChoice.RectTransform, centerPosition, SelectedCenterDuration).setEaseInOutQuad();
        LeanTween.scale(selectedChoice.Card.gameObject, selectedChoice.TargetScale * SelectedScale, SelectedCenterDuration)
            .setEaseOutBack();

        LeanTween.delayedCall(selectedChoice.Card.gameObject, SelectedCenterDuration + SelectedHoldDuration, () =>
        {
            SoundManager.TryPlay(SoundType.WindowClose);
            LeanTween.alphaCanvas(bgCanvasGroup, 0f, BackgroundFadeDuration).setEaseInQuad();
            LeanTween.move(selectedChoice.RectTransform, centerPosition + RewardExitOffset, SelectedExitDuration)
                .setEaseInBack();
            LeanTween.delayedCall(ShopWindow, Mathf.Max(SelectedExitDuration, BackgroundFadeDuration), () =>
                CompleteClose(result, selectedChoice));
        });

        SoundManager.TryPlay(SoundType.Success);
        VibrationsManager.TryVibrate(VibrationType.Success);
    }

    private BossRewardResult ApplyReward(BossRewardChoice choice)
    {
        BossRewardResult result = new BossRewardResult();
        int amount = Mathf.RoundToInt(choice.Data.value);

        switch (choice.Data.effect)
        {
            case BossRewardEffect.BlessedHealing:
                GameManager.Instance.BossRewardHealingBonus += choice.Data.value;
                break;

            case BossRewardEffect.PermanentPower:
                foreach (CardInstance card in CardContainer.Instance.GetAllOwnedCards())
                {
                    bool matches = choice.TargetsRace
                        ? card.data.race == choice.Race
                        : card.data.cardClass == choice.Class;
                    if (matches)
                        card.AddPermanentDamage(amount);
                }
                break;

            case BossRewardEffect.GoldenOpportunity:
                GameManager.Instance.BossRewardGoldPerLevel += amount;
                break;

            case BossRewardEffect.Cleanup:
                List<CardInstance> peasants = CardContainer.Instance.GetAllOwnedCards()
                    .Where(card => card.data.cardClass == CardClass.Peasant)
                    .ToList();
                if (peasants.Count > 0)
                    peasants[UnityEngine.Random.Range(0, peasants.Count)].Destroy();
                break;

            case BossRewardEffect.Investment:
                GameManager.Instance.BossRewardInterestCapBonus += amount;
                break;

            case BossRewardEffect.BonusReroll:
                GameManager.Instance.BossRewardBonusRerolls += amount;
                GameData.CurrentReRolls += amount;
                break;

            case BossRewardEffect.Gold:
                GameManager.Instance.AddGold(amount);
                result.GoldGained = amount;
                break;

            case BossRewardEffect.RandomArtifact:
                ArtifactData artifact = ArtifactManager.Instance.GetRandom();
                if (artifact != null)
                    ArtifactManager.Instance.AddArtifact(artifact, false);
                break;

            case BossRewardEffect.Healing:
                PotionCardData potion = GetHealingPotion(choice.Data.rarity);
                if (potion != null)
                {
                    SoundManager.TryPlay(SoundType.PotionHeal);
                    result.HealthGained = PotionManager.Instance.ApplyHealingPotionEffect(potion);
                }
                break;
        }

        return result;
    }

    private PotionCardData GetHealingPotion(int rarity)
    {
        if (CardContainer.Instance.PotionDataList == null)
            return null;

        return CardContainer.Instance.PotionDataList.FirstOrDefault(potion =>
            potion != null && potion.effectType == PotionEffectType.HealHero && potion.rarity == rarity);
    }

    private string GetTargetName(BossRewardChoice choice)
    {
        if (choice == null || choice.HasPowerTarget == false)
            return null;

        return choice.TargetsRace
            ? LocalizedContent.Race(choice.Race)
            : LocalizedContent.Class(choice.Class);
    }

    private Vector2 GetChoiceCenterPosition()
    {
        if (choices.Count == 0)
            return SelectedCenterOffset;

        Vector2 center = Vector2.zero;
        foreach (BossRewardChoice choice in choices)
            center += choice.TargetPosition;
        return center / choices.Count + SelectedCenterOffset;
    }

    private void CompleteClose(BossRewardResult result, BossRewardChoice selectedChoice)
    {
        Action<BossRewardResult> callback = onHide;
        onHide = null;

        if (ShopWindow != null)
            ShopWindow.SetActive(false);
        if (bgCanvasGroup != null && bgCanvasGroup.gameObject != ShopWindow)
            bgCanvasGroup.gameObject.SetActive(false);

        ClearChoices();
        acceptingInput = false;
        isClosing = false;

        if (selectedChoice != null && selectedChoice.Data.effect == BossRewardEffect.ArtifactPack)
        {
            ArtifactPackManager artifactPackManager = FindObjectOfType<ArtifactPackManager>(true);
            if (artifactPackManager != null && artifactPackManager.IsConfigured)
            {
                artifactPackManager.ShowWindow(() => callback?.Invoke(result));
                return;
            }
        }

        callback?.Invoke(result);
    }

    private void ClearChoices()
    {
        flippedCard = null;
        UpdateSelectionHint();
        foreach (BossRewardChoice choice in choices)
        {
            if (choice.Card == null)
                continue;

            LeanTween.cancel(choice.Card.gameObject);
            Destroy(choice.Card.gameObject);
        }
        choices.Clear();
    }
}
