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

    public GameObject ArtifactPrefab;
    public GameObject PotionPrefab;
    public GameObject RunePrefab;
    public GameObject UnitUpgradePrefab;

    public float ShopCardScale = 1.261564f;
    public float UnitUpgradeCardScale = 0.9070403f;

    public Vector3 startPosition;

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
        for (int i = UnlockParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(UnlockParent.GetChild(i).gameObject);
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
            shopCard.PriceLabel.gameObject.SetActive(false);
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
