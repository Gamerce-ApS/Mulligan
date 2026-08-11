using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnlockManager : Singleton<UnlockManager>
{
    public GameObject ShopWindow;
    public CanvasGroup bgCanvasGroup;
    public Transform UnlockParent;
    public Transform CardBackgroundParent;

    public GameObject ArtifactPrefab;
    public GameObject PotionPrefab;
    public GameObject RunePrefab;
    public GameObject UnitUpgradePrefab;
    public GameObject CardBackgroundPrefab;

    public float ShopCardScale = 1.261564f;
    public float UnitUpgradeCardScale = 0.9070403f;
    public float CardBackgroundScale = 1f;

    public Vector3 startPosition;
    private List<RectTransform> cardWrappers = new List<RectTransform>();
    private List<RectTransform> cardBackgrounds = new List<RectTransform>();

    public void Init()
    {
        startPosition = ShopWindow.GetComponent<RectTransform>().anchoredPosition;
    }

    void Update()
    {
        if (ShopWindow == null || ShopWindow.activeSelf == false)
            return;

        SyncCardBackgrounds();

        if (UIManager.Instance.currentTransform == null || Input.GetMouseButtonDown(0) == false)
            return;

        if (IsPointerOverUnlockCard())
            return;

        UIManager.Instance.HideCardInfoPopup();
    }

    public bool HasUnlocksToReveal()
    {
        return GetUnlockedArtifactsToReveal().Count > 0 ||
               GetUnlockedPotionsToReveal().Count > 0 ||
               GetUnlockedRunesToReveal().Count > 0 ||
               GetUnlockedUpgradesToReveal().Count > 0;
    }

    public void PopulateUnlocks()
    {
        cardWrappers.Clear();
        cardBackgrounds.Clear();

        for (int i = UnlockParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(UnlockParent.GetChild(i).gameObject);
        }

        if (CardBackgroundParent != null)
        {
            for (int i = CardBackgroundParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(CardBackgroundParent.GetChild(i).gameObject);
            }
        }

        foreach (var artifact in GetUnlockedArtifactsToReveal())
        {
            SpawnArtifact(artifact);
        }

        foreach (var potion in GetUnlockedPotionsToReveal())
        {
            SpawnPotion(potion);
        }

        foreach (var rune in GetUnlockedRunesToReveal())
        {
            SpawnRune(rune);
        }

        foreach (var upgrade in GetUnlockedUpgradesToReveal())
        {
            SpawnUpgrade(upgrade);
        }

        Canvas.ForceUpdateCanvases();
        RectTransform unlockRect = UnlockParent.GetComponent<RectTransform>();
        if (unlockRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(unlockRect);

        SyncCardBackgrounds();
    }

    public void ShowWindow()
    {
        if (HasUnlocksToReveal() == false)
            return;

        VibrationsManager.TryVibrate(VibrationType.Success);
        PopulateUnlocks();

        bgCanvasGroup.gameObject.SetActive(true);
        bgCanvasGroup.alpha = 0;
        LeanTween.alphaCanvas(bgCanvasGroup, 1f, 0.25f).setEaseOutQuad();

        ShopWindow.SetActive(true);
        Vector2 targetPos = ShopWindow.GetComponent<RectTransform>().anchoredPosition;
        ShopWindow.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPos.x, -Screen.height);

        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack();
    }

    public void HideWindow()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
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

            GameData.FirstBossCompletedThisRun = 0;
    }

    public void DebugCompleteFirstBossAndShow()
    {
        GameData.CompletedFirstBossAmount++;
        GameData.FirstBossCompletedThisRun = 1;
        PlayerPrefs.Save();
        ShowWindow();
    }

    private List<ArtifactData> GetUnlockedArtifactsToReveal()
    {
        if (CardContainer.Instance.ArtifactDataList == null)
            return new List<ArtifactData>();

        return CardContainer.Instance.ArtifactDataList
            .Where(c => c != null && ShouldReveal(c.UnlockRun))
            .ToList();
    }

    private List<PotionCardData> GetUnlockedPotionsToReveal()
    {
        if (CardContainer.Instance.PotionDataList == null)
            return new List<PotionCardData>();

        return CardContainer.Instance.PotionDataList
            .Where(c => c != null && ShouldReveal(c.UnlockRun))
            .ToList();
    }

    private List<RuneData> GetUnlockedRunesToReveal()
    {
        if (CardContainer.Instance.RuneDataList == null)
            return new List<RuneData>();

        return CardContainer.Instance.RuneDataList
            .Where(c => c != null && ShouldReveal(c.UnlockRun))
            .ToList();
    }

    private List<UpgradeCardData> GetUnlockedUpgradesToReveal()
    {
        CardDataObject dataList = CardLoader.LoadAllCards();
        if (dataList == null || dataList.allUpgradeCards == null)
            return new List<UpgradeCardData>();

        return dataList.allUpgradeCards
            .Where(c => c != null && ShouldReveal(c.UnlockRun))
            .ToList();
    }

    private bool ShouldReveal(int unlockRun)
    {
        return unlockRun > 0 &&
               unlockRun > GameData.UnlockProgressForThisRun &&
               unlockRun <= GameData.CompletedFirstBossAmount;
    }

    private void SpawnArtifact(ArtifactData data)
    {
        GameObject visual = SpawnWrappedVisual(ArtifactPrefab, ShopCardScale);
        if (visual == null)
            return;

        ShopCard shopCard = visual.GetComponent<ShopCard>();
        if (shopCard != null)
        {
            shopCard.Init(data);
            shopCard.CanBeDraged = false;
            HidePrice(shopCard);
        }

        string artifactName = data.name;
        if (artifactName.Contains("RandomRace"))
            artifactName = artifactName.Replace("RandomRace", data.RandomRace.ToString());

        InitWrapper(visual, artifactName, data.description + data.GetRarityText());
        DisableChildRaycasts(visual);
    }

    private void SpawnPotion(PotionCardData data)
    {
        GameObject visual = SpawnWrappedVisual(PotionPrefab, ShopCardScale);
        if (visual == null)
            return;

        ShopCard shopCard = visual.GetComponent<ShopCard>();
        if (shopCard != null)
        {
            shopCard.Init(data);
            shopCard.CanBeDraged = false;
            HidePrice(shopCard);
        }

        InitWrapper(visual, data.name, data.description + data.GetRarityText());
        DisableChildRaycasts(visual);
    }

    private void SpawnRune(RuneData data)
    {
        GameObject visual = SpawnWrappedVisual(RunePrefab, ShopCardScale);
        if (visual == null)
            return;

        ShopCard shopCard = visual.GetComponent<ShopCard>();
        if (shopCard != null)
        {
            shopCard.Init(data);
            shopCard.CanBeDraged = false;
            HidePrice(shopCard);
        }

        InitWrapper(visual, data.name, data.description + data.GetRarityText());
        DisableChildRaycasts(visual);
    }

    private void SpawnUpgrade(UpgradeCardData data)
    {
        GameObject visual = SpawnWrappedVisual(UnitUpgradePrefab, UnitUpgradeCardScale);
        if (visual == null)
            return;

        Card card = visual.GetComponent<Card>();
        if (card != null)
        {
            card.Init(new CardInstance(data));
            card.myType = CardTypeEnum.UnitUpgradeCard;
            card.allowDrag = false;
        }

        InitWrapper(visual, data.name, data.description + data.GetRarityText());
        DisableChildRaycasts(visual);
    }

    private GameObject SpawnWrappedVisual(GameObject prefab, float displayScale)
    {
        if (prefab == null || UnlockParent == null)
            return null;

        GameObject wrapper = new GameObject("UnlockContentCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(UnlockContentCard));
        wrapper.transform.SetParent(UnlockParent, false);

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

            GameObject background = SpawnCardBackground();
            SetupCardBackground(background, wrapperRect);
            AddCardBackground(wrapperRect, background);

            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localPosition = Vector3.zero;
            cardRect.localRotation = Quaternion.identity;
        }

        return visual;
    }

    private GameObject SpawnCardBackground()
    {
        if (CardBackgroundPrefab == null || CardBackgroundParent == null)
            return null;

        GameObject background = Instantiate(CardBackgroundPrefab, CardBackgroundParent);
        background.SetActive(true);

        foreach (var graphic in background.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        CanvasGroup canvasGroup = background.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        return background;
    }

    private void AddCardBackground(RectTransform wrapperRect, GameObject background)
    {
        if (wrapperRect == null || background == null)
            return;

        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        if (backgroundRect == null)
            return;

        cardWrappers.Add(wrapperRect);
        cardBackgrounds.Add(backgroundRect);
    }

    private void SetupCardBackground(GameObject background, RectTransform wrapperRect)
    {
        if (background == null)
            return;

        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        if (backgroundRect == null)
            return;

        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.position = wrapperRect.position;
        backgroundRect.localRotation = Quaternion.identity;
        backgroundRect.sizeDelta = GetSquareBackgroundSize(wrapperRect);
    }

    private void SyncCardBackgrounds()
    {
        for (int i = 0; i < cardWrappers.Count && i < cardBackgrounds.Count; i++)
        {
            if (cardWrappers[i] == null || cardBackgrounds[i] == null)
                continue;

            cardBackgrounds[i].position = cardWrappers[i].position;
            // cardBackgrounds[i].sizeDelta = cardWrappers[i].sizeDelta;
        }
    }

    private Vector2 GetSquareBackgroundSize(RectTransform wrapperRect)
    {
        float size = Mathf.Max(wrapperRect.sizeDelta.x, wrapperRect.sizeDelta.y) * CardBackgroundScale;
        return new Vector2(size, size);
    }

    private void InitWrapper(GameObject visual, string title, string description)
    {
        UnlockContentCard unlockCard = visual.GetComponentInParent<UnlockContentCard>();
        if (unlockCard != null)
            unlockCard.Init(title, description, visual.transform);
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

    private bool IsPointerOverUnlockCard()
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
