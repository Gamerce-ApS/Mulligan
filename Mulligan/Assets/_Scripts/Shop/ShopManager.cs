using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : Singleton<ShopManager>
{
    public GameObject RunePrefab;
    public GameObject PotionPrefab;
    public GameObject ArtifactPrefab;
    public GameObject UnitPackPrefab;

    public Transform RuneParent;
    public Transform ArtifactParent;
    public Transform PotionParent;
    public Transform UnitPackParent;

    public GameObject ShopWindow;
    public bool SetEverythingFreeNextRound = false;

    public void PopulateShop()
    {

        Clear();

        GameObject go = GameObject.Instantiate(RunePrefab, RuneParent);
        go.GetComponent<ShopCard>().Init(RuneManager.Instance.GetRandom());
        // go = GameObject.Instantiate(RunePrefab, RuneParent);
        // go.GetComponent<ShopCard>().Init(RuneManager.Instance.GetRandom());

        //GameObject.Instantiate(PotionPrefab, ArtifactParent);
        //GameObject.Instantiate(PotionPrefab, ArtifactParent);

        go = GameObject.Instantiate(ArtifactPrefab, ArtifactParent);
        go.GetComponent< ShopCard>().Init( ArtifactManager.Instance.GetRandom() );
        // go = GameObject.Instantiate(ArtifactPrefab, ArtifactParent);
        // go.GetComponent<ShopCard>().Init(ArtifactManager.Instance.GetRandom());
        //go = GameObject.Instantiate(ArtifactPrefab, ArtifactParent);
        //go.GetComponent<ShopCard>().Init(ArtifactManager.Instance.GetRandom());
        //go = GameObject.Instantiate(ArtifactPrefab, ArtifactParent);
        //go.GetComponent<ShopCard>().Init(ArtifactManager.Instance.GetRandom());
        go = GameObject.Instantiate(PotionPrefab, PotionParent);
        go.GetComponent<ShopCard>().Init(PotionManager.Instance.GetRandom());
        go = GameObject.Instantiate(PotionPrefab, PotionParent);
        go.GetComponent<ShopCard>().Init(PotionManager.Instance.GetRandom());


        go = GameObject.Instantiate(UnitPackPrefab, UnitPackParent);
        go.GetComponent<ShopCard>().Init(3);

        // go = GameObject.Instantiate(UnitPackPrefab, UnitPackParent);
        // go.GetComponent<ShopCard>().Init(3);



    }
    public bool hasRerolledForFree = false;
    public void ClickReRoll()
    {
        if(GameManager.Instance.HasFreeReroll && hasRerolledForFree == false)
        {
            PopulateShop();
            hasRerolledForFree = true;
        }
        else if(GameData.CurrentGold>=5)
        {
            GameData.CurrentGold -= 5;
            PopulateShop();
        }
        else
        {
            UIManager.Instance.ShowTooltip("Not enough gold!");
        }
    }

    public void Clear()
    {
        UnitPackParent.DestroyAllChildren();
        ArtifactParent.DestroyAllChildren();
        RuneParent.DestroyAllChildren();
        PotionParent.DestroyAllChildren();
    }
    // Start is called before the first frame update
    void Start()
    {
        startPosition = ShopWindow.GetComponent<RectTransform>().anchoredPosition;

        //PopulateShop();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public Vector3 startPosition;
    System.Action OnHideShop=null;
    public CanvasGroup bgCanvasGroup;
    public void ShowShopWindow(System.Action onComplete = null)
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

        hasRerolledForFree = false;
    }
    public void HideShopWindow()
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
        SetEverythingFreeNextRound = false;
    }
}
