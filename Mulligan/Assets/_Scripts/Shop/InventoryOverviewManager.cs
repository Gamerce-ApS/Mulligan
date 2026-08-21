using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryOverviewManager : Singleton<InventoryOverviewManager>
{
    private enum InventoryContentType
    {
        Artifact,
        Potion,
        Rune,
        Upgrade
    }

    private class InventoryContent
    {
        public InventoryContentType type;
        public int UnlockRun;
        public int rarity;
        public int sortOrder;
        public string name;
        public string description;
        public ArtifactData artifact;
        public bool hasArtifactRace;
        public CardRace artifactRace;
        public PotionCardData potion;
        public RuneData rune;
        public UpgradeCardData upgrade;
    }

    private class NewInventoryReveal
    {
        public string id;
        public RectTransform wrapper;
        public GameObject visual;
        public GameObject lockedVisual;
        public GameObject badge;
        public GameObject glow;
        public UnlockContentCard infoCard;
    }

    public GameObject ShopWindow;
    public CanvasGroup bgCanvasGroup;
    public Transform InventoryParent;
    public TMPro.TMP_Text CountLabel;

    public GameObject ArtifactPrefab;
    public GameObject PotionPrefab;
    public GameObject RunePrefab;
    public GameObject UnitUpgradePrefab;
    public GameObject LockedTemplate;

    [Header("New Unlock Reveal")]
    public ScrollRect InventoryScrollRect;
    public Transform AnimationLayer;
    public CanvasGroup RevealFocusCanvasGroup;
    public GameObject NewBadgePrefab;
    public GameObject NewGlowPrefab;
    public float RevealIntroDelay = 0.35f;
    public float FocusFadeDuration = 0.15f;
    public float ScrollDuration = 0.35f;
    public float FlyDuration = 0.55f;
    public float FullscreenHoldDelay = 0.35f;
    public float FullscreenRevealScale = 2.2f;
    public float BetweenRevealDelay = 0.15f;
    public Vector2 FlyStartOffset = Vector2.zero;

    public float ArtifactCardScale = 1.261564f;
    public float PotionCardScale = 1.261564f;
    public float RuneCardScale = 1.261564f;
    public float UnitUpgradeCardScale = 0.9070403f;
    public float LockedCardScale = 1f;

    public Vector3 startPosition;
    private const string SeenInventoryKeyPrefix = "InventorySeen_";
    private const string PendingInventoryKeyPrefix = "InventoryPending_";
    private List<NewInventoryReveal> newReveals = new List<NewInventoryReveal>();
    private Coroutine revealCoroutine = null;
    private GameObject activeFlyingCard = null;

    public void Init()
    {
        startPosition = ShopWindow.GetComponent<RectTransform>().anchoredPosition;
    }

    void Update()
    {
        if (ShopWindow == null || ShopWindow.activeSelf == false)
            return;

        if (UIManager.Instance.currentTransform == null || Input.GetMouseButtonDown(0) == false)
            return;

        if (IsPointerOverInventoryCard())
            return;

        UIManager.Instance.HideCardInfoPopup();
    }

    public void PopulateInventory()
    {
        StopRevealSequence();
        newReveals.Clear();

        for (int i = InventoryParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(InventoryParent.GetChild(i).gameObject);
        }

        List<InventoryContent> allContent = GetAllContent();
        List<InventoryContent> sorted = allContent
            .OrderBy(c => c.type)
            .ThenBy(c => c.sortOrder)
            .ToList();

        int unlockedCount = allContent.Count(c => IsInventoryUnlocked(c));
        if (CountLabel != null)
            CountLabel.text = unlockedCount + " / " + allContent.Count;

        foreach (var content in sorted)
        {
            if (IsInventoryUnlocked(content))
                SpawnUnlocked(content);
            else
                SpawnLocked(content);
        }

        RebuildInventoryLayout();
    }

    public void ShowWindow()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        SoundManager.TryPlay(SoundType.InventoryDeckOverviewOpen);
        PopulateInventory();

        bgCanvasGroup.gameObject.SetActive(true);
        bgCanvasGroup.alpha = 0;
        LeanTween.alphaCanvas(bgCanvasGroup, 1f, 0.25f).setEaseOutQuad();

        ShopWindow.SetActive(true);
        Vector2 targetPos = ShopWindow.GetComponent<RectTransform>().anchoredPosition;
        ShopWindow.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPos.x, -Screen.height);

        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack()
            .setOnComplete(() =>
            {
                StartRevealSequence();
            });
    }

    public void HideWindow()
    {
        StopRevealSequence();
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        SoundManager.TryPlay(SoundType.WindowClose);
        UIManager.Instance.HideCardInfoPopup();

        bgCanvasGroup.alpha = 1;
        LeanTween.alphaCanvas(bgCanvasGroup, 0f, 0.25f).setEaseInQuad();

        Vector2 hidePos = new Vector2(ShopWindow.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                ShopWindow.SetActive(false);
                ShopWindow.GetComponent<RectTransform>().anchoredPosition = startPosition;
                bgCanvasGroup.gameObject.SetActive(false);
            });
    }

    private List<InventoryContent> GetAllContent()
    {
        List<InventoryContent> allContent = new List<InventoryContent>();

        if (CardContainer.Instance.ArtifactDataList != null)
        {
            for (int i = 0; i < CardContainer.Instance.ArtifactDataList.Length; i++)
            {
                var artifact = CardContainer.Instance.ArtifactDataList[i];
                if (artifact == null)
                    continue;

                if (ShouldShowArtifactForEachRace(artifact))
                {
                    foreach (CardRace race in GetPlayableRaces())
                    {
                        allContent.Add(new InventoryContent
                        {
                            type = InventoryContentType.Artifact,
                            UnlockRun = artifact.UnlockRun,
                            rarity = artifact.rarity,
                            sortOrder = i,
                            name = GetArtifactName(artifact, race),
                            description = artifact.description + artifact.GetRarityText(),
                            artifact = artifact,
                            hasArtifactRace = true,
                            artifactRace = race
                        });
                    }

                    continue;
                }

                allContent.Add(new InventoryContent
                {
                    type = InventoryContentType.Artifact,
                    UnlockRun = artifact.UnlockRun,
                    rarity = artifact.rarity,
                    sortOrder = i,
                    name = GetArtifactName(artifact, artifact.RandomRace),
                    description = artifact.description + artifact.GetRarityText(),
                    artifact = artifact
                });
            }
        }

        if (CardContainer.Instance.PotionDataList != null)
        {
            for (int i = 0; i < CardContainer.Instance.PotionDataList.Length; i++)
            {
                var potion = CardContainer.Instance.PotionDataList[i];
                if (potion == null)
                    continue;

                allContent.Add(new InventoryContent
                {
                    type = InventoryContentType.Potion,
                    UnlockRun = potion.UnlockRun,
                    rarity = potion.rarity,
                    sortOrder = i,
                    name = potion.name,
                    description = potion.description + potion.GetRarityText(),
                    potion = potion
                });
            }
        }

        if (CardContainer.Instance.RuneDataList != null)
        {
            for (int i = 0; i < CardContainer.Instance.RuneDataList.Length; i++)
            {
                var rune = CardContainer.Instance.RuneDataList[i];
                if (rune == null)
                    continue;

                allContent.Add(new InventoryContent
                {
                    type = InventoryContentType.Rune,
                    UnlockRun = rune.UnlockRun,
                    rarity = (int)rune.rarity,
                    sortOrder = i,
                    name = rune.name,
                    description = rune.description + rune.GetRarityText(),
                    rune = rune
                });
            }
        }

        CardDataObject dataList = CardLoader.LoadAllCards();
        if (dataList != null && dataList.allUpgradeCards != null)
        {
            for (int i = 0; i < dataList.allUpgradeCards.Length; i++)
            {
                var upgrade = dataList.allUpgradeCards[i];
                if (upgrade == null)
                    continue;

                allContent.Add(new InventoryContent
                {
                    type = InventoryContentType.Upgrade,
                    UnlockRun = upgrade.UnlockRun,
                    rarity = upgrade.rarity,
                    sortOrder = i,
                    name = upgrade.name,
                    description = upgrade.description + upgrade.GetRarityText(),
                    upgrade = upgrade
                });
            }
        }

        return allContent;
    }

    private bool IsInventoryUnlocked(InventoryContent content)
    {
        if (content == null || content.UnlockRun > GameData.CompletedFirstBossAmount)
            return false;

        if (content.type == InventoryContentType.Artifact)
        {
            DailyQuestManager dailyQuestManager = FindObjectOfType<DailyQuestManager>();
            if (dailyQuestManager != null && IsArtifactUnlockedByDailyQuest(content, dailyQuestManager) == false)
                return false;
        }

        return true;
    }

    private void SpawnUnlocked(InventoryContent content)
    {
        switch (content.type)
        {
            case InventoryContentType.Artifact:
                SpawnArtifact(content);
                break;
            case InventoryContentType.Potion:
                SpawnPotion(content);
                break;
            case InventoryContentType.Rune:
                SpawnRune(content);
                break;
            case InventoryContentType.Upgrade:
                SpawnUpgrade(content);
                break;
        }
    }

    private void SpawnArtifact(InventoryContent content)
    {
        GameObject visual = SpawnWrappedVisual(ArtifactPrefab, ArtifactCardScale);
        if (visual == null)
            return;

        ArtifactData displayArtifact = GetDisplayArtifact(content);
        ShopCard shopCard = visual.GetComponent<ShopCard>();
        if (shopCard != null)
        {
            shopCard.Init(displayArtifact);
            shopCard.CanBeDraged = false;
            HidePrice(shopCard);
        }

        InitWrapper(visual, content.name, content.description);
        HideUnusedArtifactPreview(visual);
        DisableChildRaycasts(visual);
        RegisterNewReveal(content, visual);
    }

    private bool ShouldShowArtifactForEachRace(ArtifactData artifact)
    {
        return artifact != null &&
               artifact.effect == ArtifactEffectType.RaceHasExtraDamage &&
               artifact.name.Contains("RandomRace") &&
               (artifact.value == 20 || artifact.value == 40);
    }

    private bool IsArtifactUnlockedByDailyQuest(InventoryContent content, DailyQuestManager dailyQuestManager)
    {
        if (content == null || dailyQuestManager == null)
            return true;

        if (content.hasArtifactRace)
            return dailyQuestManager.IsArtifactRaceAvailable(content.artifact, content.artifactRace);

        return dailyQuestManager.IsArtifactAvailable(content.artifact);
    }

    private ArtifactData GetDisplayArtifact(InventoryContent content)
    {
        if (content == null || content.artifact == null)
            return null;

        if (content.hasArtifactRace == false)
            return content.artifact;

        ArtifactData artifact = content.artifact;
        return new ArtifactData
        {
            name = artifact.name,
            UnlockRun = artifact.UnlockRun,
            description = artifact.description,
            sprite_icon = artifact.sprite_icon,
            effect = artifact.effect,
            value = artifact.value,
            rarity = artifact.rarity,
            RandomRace = content.artifactRace
        };
    }

    private string GetArtifactName(ArtifactData artifact, CardRace race)
    {
        if (artifact == null)
            return "";

        if (artifact.name.Contains("RandomRace"))
            return artifact.name.Replace("RandomRace", race.ToString());

        return artifact.name;
    }

    private List<CardRace> GetPlayableRaces()
    {
        List<CardRace> races = new List<CardRace>();
        foreach (CardRace race in System.Enum.GetValues(typeof(CardRace)))
        {
            if (race != CardRace.END)
                races.Add(race);
        }

        return races;
    }

    private void SpawnPotion(InventoryContent content)
    {
        GameObject visual = SpawnWrappedVisual(PotionPrefab, PotionCardScale);
        if (visual == null)
            return;

        ShopCard shopCard = visual.GetComponent<ShopCard>();
        if (shopCard != null)
        {
            shopCard.Init(content.potion);
            shopCard.CanBeDraged = false;
            HidePrice(shopCard);
        }

        InitWrapper(visual, content.name, content.description);
        DisableChildRaycasts(visual);
        RegisterNewReveal(content, visual);
    }

    private void SpawnRune(InventoryContent content)
    {
        GameObject visual = SpawnWrappedVisual(RunePrefab, RuneCardScale);
        if (visual == null)
            return;

        ShopCard shopCard = visual.GetComponent<ShopCard>();
        if (shopCard != null)
        {
            shopCard.Init(content.rune);
            shopCard.CanBeDraged = false;
            HidePrice(shopCard);
        }

        InitWrapper(visual, content.name, content.description);
        DisableChildRaycasts(visual);
        RegisterNewReveal(content, visual);
    }

    private void SpawnUpgrade(InventoryContent content)
    {
        GameObject visual = SpawnWrappedVisual(UnitUpgradePrefab, UnitUpgradeCardScale);
        if (visual == null)
            return;

        Card card = visual.GetComponent<Card>();
        if (card != null)
        {
            card.Init(new CardInstance(content.upgrade));
            card.myType = CardTypeEnum.UnitUpgradeCard;
            card.allowDrag = false;
        }

        InitWrapper(visual, content.name, content.description);
        DisableChildRaycasts(visual);
        RegisterNewReveal(content, visual);
    }

    private void SpawnLocked(InventoryContent content)
    {
        GameObject visual = SpawnWrappedVisual(LockedTemplate, LockedCardScale, false);
        if (visual == null)
            return;

        visual.name = "Locked " + content.type + " " + content.UnlockRun;
        DisableChildRaycasts(visual);
    }

    private void RegisterNewReveal(InventoryContent content, GameObject visual)
    {
        if (content == null || visual == null || visual.transform.parent == null)
            return;

        string id = GetInventoryContentId(content);
        if (ShouldRevealInventoryContent(content, id) == false)
            return;

        RectTransform wrapper = visual.transform.parent.GetComponent<RectTransform>();
        if (wrapper == null)
            return;

        GameObject lockedVisual = SpawnLockedVisualForReveal(wrapper);
        visual.SetActive(false);

        GameObject glow = SpawnNewRevealDecoration(NewGlowPrefab, wrapper, true);
        GameObject badge = SpawnNewRevealDecoration(NewBadgePrefab, wrapper, false);

        UnlockContentCard infoCard = wrapper.GetComponent<UnlockContentCard>();
        if (infoCard != null)
            infoCard.IsInteractable = false;

        newReveals.Add(new NewInventoryReveal
        {
            id = id,
            wrapper = wrapper,
            visual = visual,
            lockedVisual = lockedVisual,
            badge = badge,
            glow = glow,
            infoCard = infoCard
        });
    }

    private GameObject SpawnLockedVisualForReveal(RectTransform wrapper)
    {
        if (LockedTemplate == null || wrapper == null)
            return null;

        GameObject lockedVisual = Instantiate(LockedTemplate, wrapper);
        lockedVisual.SetActive(true);

        RectTransform lockedRect = lockedVisual.GetComponent<RectTransform>();
        if (lockedRect != null)
        {
            Vector3 scale = lockedRect.localScale;
            lockedRect.localScale = new Vector3(scale.x * LockedCardScale, scale.y * LockedCardScale, scale.z * LockedCardScale);
            lockedRect.anchorMin = new Vector2(0.5f, 0.5f);
            lockedRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockedRect.pivot = new Vector2(0.5f, 0.5f);
            lockedRect.anchoredPosition = Vector2.zero;
            lockedRect.localPosition = Vector3.zero;
            lockedRect.localRotation = Quaternion.identity;
        }

        DisableChildRaycasts(lockedVisual);
        lockedVisual.transform.SetAsLastSibling();
        return lockedVisual;
    }

    private GameObject SpawnNewRevealDecoration(GameObject prefab, RectTransform wrapper, bool setFirstSibling)
    {
        if (prefab == null || wrapper == null)
            return null;

        GameObject go = Instantiate(prefab, wrapper);
        go.SetActive(true);

        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
        }

        foreach (var graphic in go.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        CanvasGroup canvasGroup = go.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        if (setFirstSibling)
            go.transform.SetAsFirstSibling();
        else
            go.transform.SetAsLastSibling();

        return go;
    }

    private void StartRevealSequence()
    {
        if (newReveals.Count == 0 || ShopWindow == null || ShopWindow.activeSelf == false)
            return;

        revealCoroutine = StartCoroutine(PlayNewRevealSequence());
    }

    private void StopRevealSequence()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        foreach (var reveal in newReveals)
        {
            if (reveal != null && reveal.infoCard != null)
                reveal.infoCard.IsInteractable = true;
        }

        if (activeFlyingCard != null)
        {
            Destroy(activeFlyingCard);
            activeFlyingCard = null;
        }

        HideRevealFocus(true);
    }

    private IEnumerator PlayNewRevealSequence()
    {
        yield return new WaitForSeconds(RevealIntroDelay);

        RebuildInventoryLayout();

        foreach (var reveal in newReveals.ToList())
        {
            if (reveal == null || reveal.wrapper == null || reveal.visual == null)
                continue;

            if (reveal.infoCard != null)
                reveal.infoCard.IsInteractable = false;

            ScrollToReveal(reveal.wrapper);
            yield return new WaitForSeconds(ScrollDuration);

            GameObject flyingCard = CreateFlyingCard(reveal.visual);
            activeFlyingCard = flyingCard;
            if (flyingCard != null)
            {
                RectTransform flyingRect = flyingCard.GetComponent<RectTransform>();
                RectTransform targetRect = reveal.visual.GetComponent<RectTransform>();

                if (flyingRect != null && targetRect != null)
                {
                    Vector3 targetPosition = targetRect.position;
                    Vector3 startPosition = GetRevealStartPosition(targetPosition);
                    Vector3 targetScale = targetRect.localScale;

                    flyingRect.position = startPosition;
                    flyingRect.localScale = targetScale * FullscreenRevealScale;
                    flyingRect.localRotation = targetRect.localRotation;

                    ShowRevealFocus();
                    yield return new WaitForSeconds(FullscreenHoldDelay);

                    HideRevealFocus();
                    LeanTween.move(flyingCard, targetPosition, FlyDuration).setEaseOutCubic();
                    LeanTween.scale(flyingCard, targetScale, FlyDuration).setEaseOutBack();
                    SoundManager.TryPlay(SoundType.Unlock);
                    VibrationsManager.TryVibrate(VibrationType.Success);
                    yield return new WaitForSeconds(FlyDuration);
                }

                Destroy(flyingCard);
                activeFlyingCard = null;
            }

            if (reveal.lockedVisual != null)
                Destroy(reveal.lockedVisual);

            if (reveal.visual != null)
                reveal.visual.SetActive(true);

            PulseRevealTarget(reveal);
            MarkInventoryContentSeen(reveal.id);
            ClearPendingInventoryContent(reveal.id);

            if (reveal.badge != null)
                Destroy(reveal.badge);

            if (reveal.infoCard != null)
                reveal.infoCard.IsInteractable = true;

            yield return new WaitForSeconds(BetweenRevealDelay);
        }

        PlayerPrefs.Save();
        HideRevealFocus();
        revealCoroutine = null;
    }

    private void ShowRevealFocus()
    {
        if (RevealFocusCanvasGroup == null)
            return;

        RevealFocusCanvasGroup.gameObject.SetActive(true);
        LeanTween.cancel(RevealFocusCanvasGroup.gameObject);
        RevealFocusCanvasGroup.alpha = 0f;
        RevealFocusCanvasGroup.blocksRaycasts = false;
        LeanTween.alphaCanvas(RevealFocusCanvasGroup, 1f, FocusFadeDuration).setEaseOutQuad();
    }

    private void HideRevealFocus(bool instant = false)
    {
        if (RevealFocusCanvasGroup == null)
            return;

        LeanTween.cancel(RevealFocusCanvasGroup.gameObject);

        if (instant)
        {
            RevealFocusCanvasGroup.alpha = 0f;
            RevealFocusCanvasGroup.gameObject.SetActive(false);
            return;
        }

        LeanTween.alphaCanvas(RevealFocusCanvasGroup, 0f, FocusFadeDuration)
            .setEaseInQuad()
            .setOnComplete(() =>
            {
                if (RevealFocusCanvasGroup != null)
                    RevealFocusCanvasGroup.gameObject.SetActive(false);
            });
    }

    private GameObject CreateFlyingCard(GameObject visual)
    {
        if (visual == null)
            return null;

        Transform parent = AnimationLayer != null ? AnimationLayer : ShopWindow.transform;
        GameObject flyingCard = Instantiate(visual, parent);
        flyingCard.SetActive(true);
        flyingCard.name = "Flying New " + visual.name;
        DisableChildRaycasts(flyingCard);

        UnlockContentCard infoCard = flyingCard.GetComponentInChildren<UnlockContentCard>(true);
        if (infoCard != null)
            infoCard.IsInteractable = false;

        return flyingCard;
    }

    private Vector3 GetRevealStartPosition(Vector3 targetPosition)
    {
        Transform startParent = AnimationLayer != null ? AnimationLayer : ShopWindow.transform;
        if (startParent == null)
            return targetPosition + (Vector3)FlyStartOffset;

        RectTransform rectTransform = startParent.GetComponent<RectTransform>();
        if (rectTransform != null)
            return rectTransform.TransformPoint(rectTransform.rect.center + FlyStartOffset);

        return startParent.position + (Vector3)FlyStartOffset;
    }

    private void PulseRevealTarget(NewInventoryReveal reveal)
    {
        if (reveal == null || reveal.wrapper == null)
            return;

        LeanTween.scale(reveal.wrapper.gameObject, reveal.wrapper.localScale * 1.12f, 0.25f)
            .setEasePunch();

        if (reveal.glow != null)
        {
            CanvasGroup canvasGroup = reveal.glow.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = reveal.glow.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 1f;
            LeanTween.alphaCanvas(canvasGroup, 0f, 1.2f)
                .setDelay(0.35f)
                .setOnComplete(() =>
                {
                    if (reveal.glow != null)
                        Destroy(reveal.glow);
                });
        }
    }

    private void ScrollToReveal(RectTransform wrapper)
    {
        if (InventoryScrollRect == null || wrapper == null || InventoryParent == null)
            return;

        if (InventoryScrollRect.vertical == false)
            return;

        int childCount = InventoryParent.childCount;
        if (childCount <= 1)
            return;

        int index = wrapper.GetSiblingIndex();
        int columns = GetInventoryGridColumnCount();
        int row = index / columns;
        int rowCount = Mathf.Max(1, Mathf.CeilToInt((float)childCount / columns));

        float target = rowCount <= 1 ? 1f : 1f - ((float)row / (rowCount - 1));
        target = Mathf.Clamp01(target);

        LeanTween.value(gameObject, InventoryScrollRect.verticalNormalizedPosition, target, ScrollDuration)
            .setEaseInOutCubic()
            .setOnUpdate((float value) =>
            {
                if (InventoryScrollRect != null)
                    InventoryScrollRect.verticalNormalizedPosition = value;
            });
    }

    private int GetInventoryGridColumnCount()
    {
        GridLayoutGroup grid = InventoryParent != null ? InventoryParent.GetComponent<GridLayoutGroup>() : null;
        if (grid == null)
            return 1;

        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            return Mathf.Max(1, grid.constraintCount);

        RectTransform parentRect = InventoryParent.GetComponent<RectTransform>();
        if (parentRect == null || grid.cellSize.x <= 0)
            return 1;

        float availableWidth = parentRect.rect.width - grid.padding.left - grid.padding.right + grid.spacing.x;
        float cellWidth = grid.cellSize.x + grid.spacing.x;
        return Mathf.Max(1, Mathf.FloorToInt(availableWidth / cellWidth));
    }

    private void RebuildInventoryLayout()
    {
        Canvas.ForceUpdateCanvases();

        RectTransform rect = InventoryParent != null ? InventoryParent.GetComponent<RectTransform>() : null;
        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        Canvas.ForceUpdateCanvases();
    }

    private string GetInventoryContentId(InventoryContent content)
    {
        if (content == null)
            return "";

        switch (content.type)
        {
            case InventoryContentType.Artifact:
                if (content.artifact == null)
                    return "";

                string artifactId = "Artifact|" + content.artifact.name + "|" + content.artifact.effect + "|" + content.artifact.value;
                if (content.hasArtifactRace)
                    artifactId += "|" + content.artifactRace;
                return artifactId;

            case InventoryContentType.Potion:
                if (content.potion == null)
                    return "";
                return "Potion|" + content.potion.name + "|" + content.potion.effectType + "|" + content.potion.value;

            case InventoryContentType.Rune:
                if (content.rune == null)
                    return "";
                return "Rune|" + content.rune.name + "|" + content.rune.type;

            case InventoryContentType.Upgrade:
                if (content.upgrade == null)
                    return "";
                return "Upgrade|" + content.upgrade.name + "|" + content.upgrade.effect + "|" + content.upgrade.type + "|" + content.upgrade.value;
        }

        return "";
    }

    private bool ShouldRevealInventoryContent(InventoryContent content, string id)
    {
        if (content == null || string.IsNullOrEmpty(id) || HasSeenInventoryContent(id))
            return false;

        if (HasPendingInventoryContent(id))
            return true;

        return content.UnlockRun > 0 &&
               content.UnlockRun > GameData.UnlockProgressForThisRun &&
               content.UnlockRun <= GameData.CompletedFirstBossAmount;
    }

    private bool HasSeenInventoryContent(string id)
    {
        if (string.IsNullOrEmpty(id))
            return true;

        return PlayerPrefs.GetInt(SeenInventoryKeyPrefix + id, 0) == 1;
    }

    private void MarkInventoryContentSeen(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        PlayerPrefs.SetInt(SeenInventoryKeyPrefix + id, 1);
        PlayerPrefs.Save();
    }

    private bool HasPendingInventoryContent(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        return PlayerPrefs.GetInt(PendingInventoryKeyPrefix + id, 0) == 1;
    }

    private void ClearPendingInventoryContent(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        PlayerPrefs.DeleteKey(PendingInventoryKeyPrefix + id);
    }

    public void MarkArtifactAsNew(ArtifactData artifact)
    {
        if (artifact == null)
            return;

        string id = "Artifact|" + artifact.name + "|" + artifact.effect + "|" + artifact.value;
        if (ShouldShowArtifactForEachRace(artifact))
            id += "|" + artifact.RandomRace;

        PlayerPrefs.SetInt(PendingInventoryKeyPrefix + id, 1);
        PlayerPrefs.Save();
    }

    private GameObject SpawnWrappedVisual(GameObject prefab, float displayScale, bool addInfoComponent = true)
    {
        if (prefab == null || InventoryParent == null)
            return null;

        GameObject wrapper = new GameObject("InventoryOverviewCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        if (addInfoComponent)
            wrapper.AddComponent<UnlockContentCard>();

        wrapper.transform.SetParent(InventoryParent, false);

        Image image = wrapper.GetComponent<Image>();
        image.color = new Color(1, 1, 1, 0);
        image.raycastTarget = true;

        GameObject visual = Instantiate(prefab, wrapper.transform);
        RectTransform cardRect = visual.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            Vector2 size = cardRect.sizeDelta;
            Vector3 scale = cardRect.localScale;
            cardRect.localScale = new Vector3(scale.x * displayScale, scale.y * displayScale, scale.z * displayScale);

            RectTransform wrapperRect = wrapper.GetComponent<RectTransform>();
            wrapperRect.sizeDelta = new Vector2(size.x * cardRect.localScale.x, size.y * cardRect.localScale.y);

            LayoutElement layoutElement = wrapper.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = wrapperRect.sizeDelta.x;
            layoutElement.preferredHeight = wrapperRect.sizeDelta.y;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;

            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localPosition = Vector3.zero;
            cardRect.localRotation = Quaternion.identity;
        }

        return visual;
    }

    private void InitWrapper(GameObject visual, string title, string description)
    {
        if (visual == null || visual.transform.parent == null)
            return;

        UnlockContentCard unlockCard = visual.transform.parent.GetComponent<UnlockContentCard>();
        if (unlockCard == null)
            unlockCard = visual.transform.parent.gameObject.AddComponent<UnlockContentCard>();

        if (unlockCard != null)
        {
            unlockCard.AllowLongPress = false;
            unlockCard.Init(title, description, visual.transform);
        }
    }

    private void DisableChildRaycasts(GameObject visual)
    {
        foreach (var graphic in visual.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        CanvasGroup canvasGroup = visual.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void HidePrice(ShopCard shopCard)
    {
        if (shopCard.PriceLabel != null)
            shopCard.PriceLabel.transform.parent.gameObject.SetActive(false);
    }

    private void HideUnusedArtifactPreview(GameObject visual)
    {
        Transform unusedPreview = visual.transform.Find("cards");
        if (unusedPreview != null)
            unusedPreview.gameObject.SetActive(false);
    }

    private bool IsPointerOverInventoryCard()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result.gameObject.GetComponentInParent<UnlockContentCard>() != null)
                return true;
        }

        return false;
    }
}
