using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeckOverviewManager : Singleton<DeckOverviewManager>
{
    public GameObject UnitPrefab;
    public Transform UnitParent;
    public GameObject SynergiTemplate;
    public Transform RaceOverviewParent;
    public Transform ClassOverviewParent;

    public GameObject ShopWindow;

    public Vector3 startPosition;
    public CanvasGroup bgCanvasGroup;

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

        if (IsPointerOverOverviewCard())
            return;

        UIManager.Instance.HideCardInfoPopup();
    }

    public void PopulateDeck()
    {
        for (int i = UnitParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(UnitParent.GetChild(i).gameObject);
        }

        List<CardInstance> cards = new List<CardInstance>();
        cards.AddRange(CardContainer.Instance.CurrentDeck);
        cards.AddRange(CardContainer.Instance.DiscardDeck);
        cards.AddRange(HandManager.Instance.CurrentHand);
        cards.RemoveAll(card => card.data == null);
        cards.Sort((a, b) =>
        {
            int raceCompare = a.data.race.CompareTo(b.data.race);
            if (raceCompare != 0)
                return raceCompare;

            int classCompare = a.data.cardClass.CompareTo(b.data.cardClass);
            if (classCompare != 0)
                return classCompare;

            return string.Compare(a.data.cardName, b.data.cardName, System.StringComparison.Ordinal);
        });

        PopulateCardTypeOverview(cards);

        foreach (var cardInstance in cards)
        {
            GameObject wrapper = new GameObject("DeckOverviewCard", typeof(RectTransform), typeof(Image), typeof(DeckOverviewCard));
            wrapper.transform.SetParent(UnitParent, false);

            Image image = wrapper.GetComponent<Image>();
            image.color = new Color(1, 1, 1, 0);
            image.raycastTarget = true;

            GameObject go = GameObject.Instantiate(UnitPrefab, wrapper.transform);
            RectTransform cardRect = go.GetComponent<RectTransform>();
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localRotation = Quaternion.identity;

            Card card = go.GetComponent<Card>();
            card.Init(cardInstance);
            card.myType = CardTypeEnum.UnitSelectCard;
            card.allowDrag = false;
            go.transform.localScale = new Vector3(0.7431874f, 0.7431874f, 0.7431874f);

            foreach (var graphic in go.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

            CanvasGroup canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }

            wrapper.GetComponent<DeckOverviewCard>().Init(card);
        }
    }

    private void PopulateCardTypeOverview(List<CardInstance> cards)
    {
        ClearOverviewParent(RaceOverviewParent);
        ClearOverviewParent(ClassOverviewParent);

        if (SynergiTemplate == null)
            SynergiTemplate = UIManager.Instance.SynergiTemplate;

        if (SynergiTemplate == null || RaceOverviewParent == null || ClassOverviewParent == null)
            return;

        Dictionary<CardRace, int> raceCounts = new Dictionary<CardRace, int>();
        Dictionary<CardClass, int> classCounts = new Dictionary<CardClass, int>();

        foreach (var card in cards)
        {
            if (!raceCounts.ContainsKey(card.data.race)) raceCounts[card.data.race] = 0;
            raceCounts[card.data.race]++;

            if (!classCounts.ContainsKey(card.data.cardClass)) classCounts[card.data.cardClass] = 0;
            classCounts[card.data.cardClass]++;
        }

        foreach (CardRace race in System.Enum.GetValues(typeof(CardRace)))
        {
            if (race == CardRace.END || !raceCounts.ContainsKey(race))
                continue;

            CreateOverviewCountItem(
                RaceOverviewParent,
                "Race: " + race,
                raceCounts[race],
                CardContainer.Instance.GetSpriteForRace(race),
                true
            );
        }

        foreach (CardClass cardClass in System.Enum.GetValues(typeof(CardClass)))
        {
            if (!classCounts.ContainsKey(cardClass))
                continue;

            CreateOverviewCountItem(
                ClassOverviewParent,
                "Class: " + cardClass,
                classCounts[cardClass],
                CardContainer.Instance.GetSpriteForClass(cardClass),
                false
            );
        }
    }

    private void ClearOverviewParent(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private void CreateOverviewCountItem(Transform parent, string itemName, int count, Sprite iconSprite, bool isRace)
    {
        GameObject item = GameObject.Instantiate(SynergiTemplate, parent);
        item.SetActive(true);
        item.name = itemName;
        item.transform.localScale = SynergiTemplate.transform.localScale;

        TMPro.TMP_Text countText = item.GetComponentInChildren<TMPro.TMP_Text>();
        Image iconRace = item.transform.Find("IconRace")?.GetComponent<Image>();
        Image iconClass = item.transform.Find("IconClass")?.GetComponent<Image>();

        if (countText != null)
            countText.text = count.ToString();

        if (iconRace != null)
        {
            iconRace.enabled = isRace;
            if (isRace)
                iconRace.sprite = iconSprite;
        }

        if (iconClass != null)
        {
            iconClass.enabled = !isRace;
            if (!isRace)
                iconClass.sprite = iconSprite;
        }

        Transform glow = item.transform.Find("Glow");
        if (glow != null)
            glow.gameObject.SetActive(false);
    }

    private bool IsPointerOverOverviewCard()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result.gameObject.GetComponentInParent<DeckOverviewCard>() != null)
                return true;
        }

        return false;
    }

    public void ShowWindow()
    {
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
        SoundManager.TryPlay(SoundType.InventoryDeckOverviewOpen);
        PopulateDeck();

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

}
