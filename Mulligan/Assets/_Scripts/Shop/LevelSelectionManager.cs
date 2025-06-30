using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionManager : Singleton<LevelSelectionManager>
{

    public List<Transform> LevelPositions;
    public GameObject buttonsParent;
    public GameObject ShopWindow;

    public List<Sprite> NormalGameBG;
    public List<Sprite> BossGameBG;
    public Image BackgroundImage;
    public TMPro.TMP_Text CurrentLevel;
    public TMPro.TMP_Text RewardText;

    public List<GameObject> Buttons;

    public List<GameObject> LevelParent;
    public List<GameObject> BossParent;

    // Start is called before the first frame update
    void Awake()
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
    public SkipRewardData CurrentRewardData = null;
    public void ShowWindow(System.Action onComplete = null)
    {
        CurrentRewardData = RewardManager.Instance.GetRandom();
        //RewardText.text = CurrentRewardData.title;


        bgCanvasGroup.gameObject.SetActive(true);
        bgCanvasGroup.alpha = 0;
        LeanTween.alphaCanvas(bgCanvasGroup, 1f, 0.25f).setEaseOutQuad();

        OnHideShop = onComplete;
        ShopWindow.SetActive(true);
        // Store the target position
        Vector2 targetPos = startPosition;

        // Start below the screen
        ShopWindow.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPos.x, -Screen.height*2);

        // Animate to its original position
        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack();

        RefreshUI();




    }

    public void RefreshUI()
    {
        //buttonsParent.transform.position = LevelPositions[GameData.CurrentRound].transform.position;
        foreach (var a in LevelParent)
            a.SetActive(false);
        foreach (var a in BossParent)
            a.SetActive(false);
        //Is boss level
        if (GameData.CurrentRound % 4 != 0)
        {
            int index = (GameData.CurrentRound - 1) % LevelParent.Count;
            GameObject parent = LevelParent[index];

            parent.SetActive(true);
            //BackgroundImage.sprite = NormalGameBG.GetRandom();

            TMPro.TMP_Text rewardT = parent.transform.Find("reward").GetChild(0).GetChild(0).GetComponent<TMPro.TMP_Text>();
            rewardT.text = CurrentRewardData.title;
            TMPro.TMP_Text levleT = parent.transform.Find("PlayButton").GetChild(1).GetComponent<TMPro.TMP_Text>();

            if (GameData.CurrentRound<9)
                levleT.text = "0" + GameData.CurrentRound.ToString();
            else
                levleT.text = GameData.CurrentRound.ToString();
        }
        else
        {
            BossParent.GetRandom().SetActive(true);

            //BackgroundImage.sprite = BossGameBG.GetRandom();
            //CurrentLevel.text = "";

        }
    }
    public void HideWindow(bool callback = true)
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
                if(callback)
                OnHideShop?.Invoke();
                ShopWindow.SetActive(false);
                ShopWindow.GetComponent<RectTransform>().anchoredPosition = startPosition;
                bgCanvasGroup.gameObject.SetActive(false);
            });
    }
    public void ClickSkip()
    {
        GameData.CurrentRound++;
        //RefreshUI();
        HideWindow(false);
        LeanTween.delayedCall(gameObject, 0.3f, () =>
        {
            RewardManager.Instance.ShowWindow(()=> { ShowWindow(OnHideShop); });
        });
        GameData.SkippedLevels++;
    }
    public void ClickPlay()
    {
        HideWindow();
    }

}
