using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardManager : Singleton<RewardManager>
{

    public GameObject Window;
    public Image bg;

    // Start is called before the first frame update
    void Awake()
    {
        startPosition = Window.GetComponent<RectTransform>().anchoredPosition;

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
        OnHideShop = onComplete;

        bgCanvasGroup.alpha = 0;
        bgCanvasGroup.gameObject.SetActive(true);
        //bgCanvasGroup.alpha = 0;
        //LeanTween.alphaCanvas(bgCanvasGroup, 1f, 0.25f).setEaseOutQuad();

        //OnHideShop = onComplete;
        //Window.SetActive(true);
        //// Store the target position
        //Vector2 targetPos = startPosition;

        //// Start below the screen
        //Window.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPos.x, -Screen.height*2);

        //// Animate to its original position
        //LeanTween.move(Window.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack();



        bg.GetComponent<CanvasGroup>().alpha = 1f;


        // Pick a fun message

        // Fade in
        LeanTween.alphaCanvas(bgCanvasGroup.GetComponent<CanvasGroup>(), 1f, 0.3f).setEaseOutQuad().setDelay(0.1f).setOnComplete(() =>
        {
            // Animate children
            int i = 0;
            foreach (Transform child in Window.transform)
            {
                if (child.name == "ignore" || child == bg.transform)
                {
                    child.gameObject.SetActive(true);

                }
                else
                {
                    child.gameObject.SetActive(true);

                    Vector3 targetScale = child.localScale;
                    child.localScale = Vector3.zero;
                    LeanTween.scale(child.gameObject, targetScale, 0.5f).setEaseOutBack().setDelay(i * 0.1f);
                }

                i++;
            }
        });
        foreach (Transform child in Window.transform)
        {
            if (child.name != "ignore" || child != bg.transform)
                child.gameObject.SetActive(false);
        }


    }

    public void HideWindow()
    {
        bgCanvasGroup.alpha = 1;
        LeanTween.alphaCanvas(bgCanvasGroup, 0f, 0.25f).setEaseInQuad();

        // Move downward off the screen
        Vector2 hidePos = new Vector2(Window.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        // Animate down
        LeanTween.move(Window.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                OnHideShop?.Invoke();
                Window.SetActive(false);
                Window.GetComponent<RectTransform>().anchoredPosition = startPosition;
                bgCanvasGroup.gameObject.SetActive(false);
            });
    }
    public void ClickSkip()
    {

    }
    public void ClickPlay()
    {
        HideWindow();
    }

}
