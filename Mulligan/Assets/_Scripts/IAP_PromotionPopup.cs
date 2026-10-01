using UnityEngine;
using UnityEngine.UI;

public class IAP_PromotionPopup : MonoBehaviour
{
    [Header("References")]
    public Image Background;
    public RectTransform Banner;
    public CanvasGroup PopupCanvasGroup;

    [Header("Animation")]
    public float BackgroundFadeDuration = 0.25f;
    public float BannerMoveDuration = 0.5f;
    public float BannerStartDistance = 1200f;
    public float VisibleDuration = 2f;
    public float FadeOutDuration = 0.3f;

    private Vector2 bannerTargetPosition;
    private Color backgroundTargetColor;
    private bool initialized;
    private bool showBackgroundOnlyOnEnable;

    private void Awake()
    {
        CacheInitialState();
    }

    private void OnEnable()
    {
        CacheInitialState();

        if (showBackgroundOnlyOnEnable)
        {
            showBackgroundOnlyOnEnable = false;
            PlayBackgroundAnimation();
        }
        else
        {
            PlayAnimation();
        }
    }

    private void CacheInitialState()
    {
        if (initialized)
            return;

        if (PopupCanvasGroup == null)
            PopupCanvasGroup = GetComponent<CanvasGroup>();

        if (Banner != null)
            bannerTargetPosition = Banner.anchoredPosition;

        if (Background != null)
            backgroundTargetColor = Background.color;

        initialized = true;
    }

    public void PlayAnimation()
    {
        ShowBackground();
        ShowBanner();
    }

    public void ShowBackground()
    {
        transform.SetAsLastSibling();

        if (gameObject.activeSelf == false)
        {
            showBackgroundOnlyOnEnable = true;
            gameObject.SetActive(true);
            return;
        }

        PlayBackgroundAnimation();
    }

    public void ShowBanner()
    {
        if (gameObject.activeSelf == false)
            ShowBackground();

        if (Banner == null)
        {
            ScheduleFadeOut();
            return;
        }

        LeanTween.cancel(Banner.gameObject);
        Banner.gameObject.SetActive(true);
        Banner.anchoredPosition = bannerTargetPosition + Vector2.down * BannerStartDistance;
        LeanTween.move(Banner, bannerTargetPosition, BannerMoveDuration)
            .setEaseOutBack()
            .setIgnoreTimeScale(true)
            .setOnComplete(ScheduleFadeOut);
    }

    public void Hide()
    {
        showBackgroundOnlyOnEnable = false;
        CancelAnimation();
        gameObject.SetActive(false);
    }

    private void PlayBackgroundAnimation()
    {
        CancelAnimation();

        if (PopupCanvasGroup != null)
        {
            PopupCanvasGroup.alpha = 1f;
            PopupCanvasGroup.interactable = true;
            PopupCanvasGroup.blocksRaycasts = true;
        }

        if (Background != null)
        {
            Color color = backgroundTargetColor;
            color.a = 0f;
            Background.color = color;

            LeanTween.value(Background.gameObject, 0f, backgroundTargetColor.a, BackgroundFadeDuration)
                .setEaseOutQuad()
                .setIgnoreTimeScale(true)
                .setOnUpdate((float alpha) =>
                {
                    if (Background == null)
                        return;

                    Color updatedColor = backgroundTargetColor;
                    updatedColor.a = alpha;
                    Background.color = updatedColor;
                });
        }

        if (Banner != null)
        {
            Banner.gameObject.SetActive(false);
            Banner.anchoredPosition = bannerTargetPosition + Vector2.down * BannerStartDistance;
        }
    }

    private void ScheduleFadeOut()
    {
        LeanTween.delayedCall(gameObject, VisibleDuration, FadeOut)
            .setIgnoreTimeScale(true);
    }

    private void FadeOut()
    {
        if (PopupCanvasGroup == null)
        {
            FinishAnimation();
            return;
        }

        PopupCanvasGroup.interactable = false;
        PopupCanvasGroup.blocksRaycasts = false;

        LeanTween.alphaCanvas(PopupCanvasGroup, 0f, FadeOutDuration)
            .setEaseInQuad()
            .setIgnoreTimeScale(true)
            .setOnComplete(FinishAnimation);
    }

    private void FinishAnimation()
    {
        gameObject.SetActive(false);
    }

    private void CancelAnimation()
    {
        LeanTween.cancel(gameObject);

        if (Background != null)
            LeanTween.cancel(Background.gameObject);

        if (Banner != null)
            LeanTween.cancel(Banner.gameObject);
    }

    private void OnDisable()
    {
        CancelAnimation();
    }
}
