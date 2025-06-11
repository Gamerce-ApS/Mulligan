using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitUpgradeManager : Singleton<UnitUpgradeManager>
{

    public GameObject UnitPrefab;
    public Transform UnitParent;

    public GameObject UnitUpgradePrefab;
    public Transform UnitUpgradeParent;

    public GameObject ShopWindow;

    public GameObject InfoBoxPopup;

    public void PopulateShop()
    {
        for (int i = UnitParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(UnitParent.GetChild(i).gameObject);
        }

        for(int i = 0; i < 8;i++)
        {
            GameObject go = GameObject.Instantiate(UnitPrefab, UnitParent);
            go.GetComponent<Card>().Init(CardContainer.Instance.GetRandomCardFromDecks());
            go.GetComponent<Card>().OnClick += ClickOnCard;
            go.GetComponent<Card>().myType = CardTypeEnum.UnitSelectCard;
            go.GetComponent<Card>().allowDrag = false;
            go.transform.localScale = new Vector3(0.7431874f, 0.7431874f, 0.7431874f);
        }


        for (int i = UnitUpgradeParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(UnitUpgradeParent.GetChild(i).gameObject);
        }

        List<UpgradeCardData> list = new List<UpgradeCardData>(DataList.allUpgradeCards);
        list.Shuffle();

        for (int i = 0; i < 3; i++)
        {
            
            GameObject go = GameObject.Instantiate(UnitUpgradePrefab, UnitUpgradeParent);
            go.GetComponent<Card>().Init(new CardInstance(list[i]));
            go.GetComponent<Card>().OnClick += ClickOnCard;
            go.GetComponent<Card>().myType = CardTypeEnum.UnitUpgradeCard;
            go.GetComponent<Card>().allowDrag = false;

            //go.transform.localScale = new Vector3(0.7431874f, 0.7431874f, 0.7431874f);
        }
    }

    CardDataObject DataList;
    // Start is called before the first frame update
    void Start()
    {
        DataList = CardLoader.LoadAllCards();


        startPosition = ShopWindow.GetComponent<RectTransform>().anchoredPosition;
        UnityHelper.RunAfterDelay(this, 0.6f, () =>
        {
            PopulateShop();
        });


    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public Vector3 startPosition;
    System.Action OnHideShop=null;
    public CanvasGroup bgCanvasGroup;
    public void ShowWindow(System.Action onComplete = null)
    {

        PopulateShop();

        bgCanvasGroup.gameObject.SetActive(true);
        bgCanvasGroup.alpha = 0;
        LeanTween.alphaCanvas(bgCanvasGroup, 1f, 0.25f).setEaseOutQuad();

        OnHideShop = onComplete;
        ShopWindow.SetActive(true);
        // Store the target position
        Vector2 targetPos = ShopWindow.GetComponent<RectTransform>().anchoredPosition;

        // Start below the screen
        ShopWindow.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPos.x, -Screen.height);

        // Animate to its original position
        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack();
    }
    public void HideWindow()
    {
        bgCanvasGroup.alpha = 1;
        LeanTween.alphaCanvas(bgCanvasGroup, 0f, 0.25f).setEaseInQuad();

        // Move downward off the screen
        Vector2 hidePos = new Vector2(ShopWindow.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        // Animate down
        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                OnHideShop?.Invoke();
                ShopWindow.SetActive(false);
                ShopWindow.GetComponent<RectTransform>().anchoredPosition = startPosition;
                bgCanvasGroup.gameObject.SetActive(false);
            });
    }
    public void ClickContinue()
    {
        UIManager.Instance.HideCardInfoPopup();

        Card targetUnitCard = null;
        Card upgradeCard = null;

        for (int i = UnitParent.childCount - 1; i >= 0; i--)
        {
            if (UnitParent.GetChild(i).GetComponent<Card>().isSelected == false)
            {
                RectTransform go = UnitParent.GetChild(i).GetComponent<RectTransform>();
                LeanTween.move(go, go.anchoredPosition - new Vector2(0, 1000), 0.5f)
                  .setEaseInOutCirc()
                  .setOnComplete(() =>
                  {
                      UnityHelper.RunAfterDelay(this, 0.75f, () =>
                      {
                          //DestroyImmediate(go.gameObject);
                      });
                  });
            }
            else
                targetUnitCard = UnitParent.GetChild(i).GetComponent<Card>();
        }
        for (int i = UnitUpgradeParent.childCount - 1; i >= 0; i--)
        {
            var card = UnitUpgradeParent.GetChild(i).GetComponent<Card>();
            if (card.isSelected)
                upgradeCard = card;
        }

        for (int i = UnitUpgradeParent.childCount - 1; i >= 0; i--)
        {
            if (UnitUpgradeParent.GetChild(i).GetComponent<Card>().isSelected == false)
            {
                RectTransform go = UnitUpgradeParent.GetChild(i).GetComponent<RectTransform>();
                LeanTween.move(go, go.anchoredPosition - new Vector2(0, 1000), 0.5f)
                  .setEaseInOutCirc()
                  .setOnComplete(() =>
                  {
                      UnityHelper.RunAfterDelay(this, 0.75f, () =>
                      {
                          //DestroyImmediate(go.gameObject);
                      });
                  });
            }
            else
            {
                GameObject go = UnitUpgradeParent.GetChild(i).gameObject;
                LeanTween.move(go, targetUnitCard.transform.position, 0.5f)
                  .setEaseInOutCirc()
                  .setOnComplete(() =>
                  {
                      UnityHelper.RunAfterDelay(this, 0.25f, () =>
                      {
                          //DestroyImmediate(go.gameObject);
                      });
                  }).setDelay(1f);
            }
        }
        UnityHelper.RunAfterDelay(this, 1.25f, () =>
        {
            HideWindow();
        });
        //UnityHelper.RunAfterDelay(this, 1.5f, () =>
        //{
        //    DestroyImmediate(aCard.gameObject);
        //});
        //CardContainer.Instance.CurrentDeck.Add(new CardInstance(aCard.cardInstance.data));


        if (targetUnitCard != null && upgradeCard != null)
        {
            targetUnitCard.cardInstance.ApplyUpgrade(upgradeCard.cardInstance.upgradeData);
        }

    }
    public void ClickSkip()
    {
        HideWindow();
    }
    public int GetAmountSelected(CardTypeEnum aType)
    {
        int total = 0;
        for (int i = UnitParent.childCount - 1; i >= 0; i--)
        {
            Card c = UnitParent.GetChild(i).GetComponent<Card>();
            if (c.myType == aType && c.isSelected)
                total++;
        }
        for (int i = UnitUpgradeParent.childCount - 1; i >= 0; i--)
        {
            Card c = UnitUpgradeParent.GetChild(i).GetComponent<Card>();
            if (c.myType == aType && c.isSelected)
                total++;
        }
        return total;
    }
    public void DeselectCardOfType(CardTypeEnum aType)
    {

        for (int i = UnitParent.childCount - 1; i >= 0; i--)
        {
            Card c = UnitParent.GetChild(i).GetComponent<Card>();
            if (c.myType == aType && c.isSelected)
            {
                c.rectTransform.anchoredPosition = c.originalAnchoredPos;
                c.rectTransform.rotation = c.originalRotation;
                c.isSelected = false;
            }
        }
        for (int i = UnitUpgradeParent.childCount - 1; i >= 0; i--)
        {
            Card c = UnitUpgradeParent.GetChild(i).GetComponent<Card>();
            if (c.myType == aType && c.isSelected)
            {
                c.rectTransform.anchoredPosition = c.originalAnchoredPos;
                c.rectTransform.rotation = c.originalRotation;
                c.isSelected = false;
            }
        }

    }
    public void ClickOnCard(Card aCard)
    {
        int selectedUnit = GetAmountSelected(CardTypeEnum.UnitSelectCard);
        int selectedUpgrades = GetAmountSelected(CardTypeEnum.UnitUpgradeCard);


        

        if (aCard.isSelected == false)
        {
            DeselectCardOfType(aCard.myType);
            //if (aCard.myType == CardTypeEnum.UnitUpgradeCard && selectedUpgrades > 0)
            //{
            //    //UIManager.Instance.ShowTooltip("Only select 1 upgrade card");
            //    return;
            //}
            //if (aCard.myType == CardTypeEnum.UnitSelectCard && selectedUnit > 0)
            //{
            //    //UIManager.Instance.ShowTooltip("Only select 1 unit to upgrade");
            //    return;
            //}
            aCard.originalAnchoredPos = aCard.rectTransform.anchoredPosition;
            LeanTween.scale(aCard.gameObject, aCard.transform.localScale * 1.3f, 0.5f).setEasePunch();
            aCard.originalRotation = aCard.rectTransform.rotation;
            aCard.rectTransform.anchoredPosition += new Vector2(0, 60f); // Lift
            aCard.rectTransform.rotation = Quaternion.identity;
            aCard.isSelected = true;

            
            if (aCard.myType == CardTypeEnum.UnitUpgradeCard)
            {
                UIManager.Instance.ShowCardInfoPopup(
              aCard.cardInstance.upgradeData.name,
              aCard.cardInstance.upgradeData.description,
              aCard.cardInstance.upgradeData.type.ToString(),
              InfoBoxPopup.transform);
            }
        }
        else
        {

            aCard.rectTransform.anchoredPosition = aCard.originalAnchoredPos;
            aCard.rectTransform.rotation = aCard.originalRotation;
            aCard.isSelected = false;

            if (aCard.myType == CardTypeEnum.UnitUpgradeCard)
                UIManager.Instance.HideCardInfoPopup();
        }

        UpdateUI();
    }
    public void UpdateUI()
    {

    }
}
