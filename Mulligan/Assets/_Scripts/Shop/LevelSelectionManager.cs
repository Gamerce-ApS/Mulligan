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

    public List<GameObject> Buttons;
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
    public void ShowWindow(System.Action onComplete = null)
    {



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

        //Is boss level
        if (GameData.CurrentRound % 4 != 0)
        {
            BackgroundImage.sprite = NormalGameBG.GetRandom();
            CurrentLevel.text = "0" + GameData.CurrentRound.ToString();
            Buttons[0].SetActive(true);
            Buttons[1].SetActive(true);
            Buttons[2].SetActive(false);
        }
        else
        {
            BackgroundImage.sprite = BossGameBG.GetRandom();
            CurrentLevel.text = "";
            Buttons[0].SetActive(false);
            Buttons[1].SetActive(false);
            Buttons[2].SetActive(true);
        }
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
    public void ClickSkip()
    {
        GameData.CurrentRound++;
        RefreshUI();
    }
    public void ClickPlay()
    {
        HideWindow();
    }

}
