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
        public PotionCardData potion;
        public RuneData rune;
        public UpgradeCardData upgrade;
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

    public float ArtifactCardScale = 1.261564f;
    public float PotionCardScale = 1.261564f;
    public float RuneCardScale = 1.261564f;
    public float UnitUpgradeCardScale = 0.9070403f;
    public float LockedCardScale = 1f;

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

        if (IsPointerOverInventoryCard())
            return;

        UIManager.Instance.HideCardInfoPopup();
    }

    public void PopulateInventory()
    {
        for (int i = InventoryParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(InventoryParent.GetChild(i).gameObject);
        }

        List<InventoryContent> allContent = GetAllContent();
        List<InventoryContent> sorted = allContent
            .OrderBy(c => c.type)
            .ThenBy(c => c.sortOrder)
            .ToList();

        int unlockedCount = allContent.Count(c => IsInventoryUnlocked(c.UnlockRun));
        if (CountLabel != null)
            CountLabel.text = unlockedCount + " / " + allContent.Count;

        foreach (var content in sorted)
        {
            if (IsInventoryUnlocked(content.UnlockRun))
                SpawnUnlocked(content);
            else
                SpawnLocked(content);
        }
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

        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack();
    }

    public void HideWindow()
    {
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

                string artifactName = artifact.name;
                if (artifactName.Contains("RandomRace"))
                    artifactName = artifactName.Replace("RandomRace", artifact.RandomRace.ToString());

                allContent.Add(new InventoryContent
                {
                    type = InventoryContentType.Artifact,
                    UnlockRun = artifact.UnlockRun,
                    rarity = artifact.rarity,
                    sortOrder = i,
                    name = artifactName,
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

    private bool IsInventoryUnlocked(int unlockRun)
    {
        return unlockRun <= GameData.CompletedFirstBossAmount;
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

        ShopCard shopCard = visual.GetComponent<ShopCard>();
        if (shopCard != null)
        {
            shopCard.Init(content.artifact);
            shopCard.CanBeDraged = false;
            HidePrice(shopCard);
        }

        InitWrapper(visual, content.name, content.description);
        HideUnusedArtifactPreview(visual);
        DisableChildRaycasts(visual);
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
    }

    private void SpawnLocked(InventoryContent content)
    {
        GameObject visual = SpawnWrappedVisual(LockedTemplate, LockedCardScale, false);
        if (visual == null)
            return;

        visual.name = "Locked " + content.type + " " + content.UnlockRun;
        DisableChildRaycasts(visual);
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
