using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmoryManager : Singleton<ArmoryManager>
{

    public GameObject UnitPackPrefab;

    public Transform UnitPackParent;

    public GameObject ShopWindow;

    public void PopulateShop()
    {
        for (int i = UnitPackParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(UnitPackParent.GetChild(i).gameObject);
        }

        GameObject go = GameObject.Instantiate(UnitPackPrefab, UnitPackParent);
        go.GetComponent< Card>().Init(new CardInstance(CardContainer.Instance.GetRandomCardData()));
        go.GetComponent<Card>().OnClick += ClickOnCard;
        go = GameObject.Instantiate(UnitPackPrefab, UnitPackParent);
        go.GetComponent<Card>().Init(new CardInstance(CardContainer.Instance.GetRandomCardData()));
        go.GetComponent<Card>().OnClick += ClickOnCard;
        go = GameObject.Instantiate(UnitPackPrefab, UnitPackParent);
        go.GetComponent<Card>().Init(new CardInstance(CardContainer.Instance.GetRandomCardData()));
        go.GetComponent<Card>().OnClick += ClickOnCard;

    }
    // Start is called before the first frame update
    void Start()
    {
        startPosition = ShopWindow.GetComponent<RectTransform>().anchoredPosition;

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
    public void ClickReRoll()
    {
        PopulateShop();
    }
    public void ClickSkip()
    {
        HideWindow();
    }
    public void ClickOnCard(Card aCard)
    {
        for (int i = UnitPackParent.childCount - 1; i >= 0; i--)
        {
            if(UnitPackParent.GetChild(i).GetComponent<Card>() != aCard)
            {
                RectTransform go = UnitPackParent.GetChild(i).GetComponent<RectTransform>();
                LeanTween.move(go, go.anchoredPosition-new Vector2(0,1000), 0.5f)
                  .setEaseInOutCirc()
                  .setOnComplete(() =>
                  {
                      UnityHelper.RunAfterDelay(this, 0.75f, () =>
                      {
                          DestroyImmediate(go.gameObject);
                      });
      
                  });
            }    
        }
        UnityHelper.RunAfterDelay(this, 0.75f, () =>
        {
            HideWindow();
        });
        UnityHelper.RunAfterDelay(this, 1.5f, () =>
        {
            DestroyImmediate(aCard.gameObject);
        });
        CardContainer.Instance.CurrentDeck.Add(new CardInstance(aCard.cardInstance.data));
    }
}
