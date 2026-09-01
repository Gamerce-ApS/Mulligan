using UnityEngine;
using UnityEngine.UI;

public class AnimatedPortraitSlot : MonoBehaviour
{
    public Vector2 Offset = Vector2.zero;
    public float Scale = 1f;
    public bool HideStaticImageWhenAnimated = true;

    private GameObject currentPortrait;
    private Image staticImage;
    private Color originalImageColor;
    private bool hasOriginalColor = false;

    void Awake()
    {
        CacheStaticImage();
    }

    public void ShowPortrait(GameObject portraitPrefab, Vector2 offset, float scale)
    {
        ClearPortrait();

        Offset = offset;
        Scale = scale;

        if (portraitPrefab == null)
        {
            RestoreStaticImage();
            return;
        }

        CacheStaticImage();
        SetStaticImageVisible(HideStaticImageWhenAnimated == false);

        currentPortrait = Instantiate(portraitPrefab, transform);
        currentPortrait.name = portraitPrefab.name;

        RectTransform rectTransform = currentPortrait.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector2 sourceSize = GetSourceSize(currentPortrait);
            float fitScale = GetFitScale(sourceSize);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = offset;
            rectTransform.localScale = Vector3.one * Mathf.Max(0.01f, scale) * fitScale;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.sizeDelta = sourceSize;
        }

        DisableRaycasts(currentPortrait);

        AnimatedPortrait animatedPortrait = currentPortrait.GetComponent<AnimatedPortrait>();
        if (animatedPortrait != null)
            animatedPortrait.PlayIdle();
    }

    public void ClearPortrait()
    {
        if (currentPortrait != null)
            Destroy(currentPortrait);

        currentPortrait = null;
        RestoreStaticImage();
    }

    private Vector2 GetSourceSize(GameObject portrait)
    {
        AnimatedPortrait animatedPortrait = portrait.GetComponent<AnimatedPortrait>();
        if (animatedPortrait != null && animatedPortrait.SourceWidth > 0 && animatedPortrait.SourceHeight > 0)
            return new Vector2(animatedPortrait.SourceWidth, animatedPortrait.SourceHeight);

        RectTransform rectTransform = portrait.GetComponent<RectTransform>();
        if (rectTransform != null && rectTransform.sizeDelta.x > 0f && rectTransform.sizeDelta.y > 0f)
            return rectTransform.sizeDelta;

        return GetTargetSize();
    }

    private float GetFitScale(Vector2 sourceSize)
    {
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null || sourceSize.x <= 0f || sourceSize.y <= 0f)
            return 1f;

        Vector2 targetSize = parentRect.rect.size;
        if (targetSize.x <= 0f || targetSize.y <= 0f)
            return 1f;

        return Mathf.Min(targetSize.x / sourceSize.x, targetSize.y / sourceSize.y);
    }

    private Vector2 GetTargetSize()
    {
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null)
            return Vector2.one;

        return parentRect.rect.size;
    }

    private void DisableRaycasts(GameObject root)
    {
        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }

    private void CacheStaticImage()
    {
        if (staticImage != null)
            return;

        staticImage = GetComponent<Image>();
        if (staticImage != null && hasOriginalColor == false)
        {
            originalImageColor = staticImage.color;
            hasOriginalColor = true;
        }
    }

    private void RestoreStaticImage()
    {
        CacheStaticImage();
        if (staticImage != null && hasOriginalColor)
            staticImage.color = originalImageColor;
    }

    private void SetStaticImageVisible(bool visible)
    {
        if (staticImage == null)
            return;

        Color color = staticImage.color;
        color.a = visible ? originalImageColor.a : 0f;
        staticImage.color = color;
    }
}
