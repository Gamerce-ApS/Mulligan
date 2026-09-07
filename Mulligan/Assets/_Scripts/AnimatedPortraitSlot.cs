using UnityEngine;
using UnityEngine.UI;

public class AnimatedPortraitSlot : MonoBehaviour
{
    public Vector2 Offset = Vector2.zero;
    public float Scale = 1f;
    public bool HideStaticImageWhenAnimated = true;

    private GameObject currentPortrait;
    private RectTransform currentPortraitRect;
    private AnimatedPortrait currentAnimatedPortrait;
    private Vector2 currentSourceSize;
    private Vector2 lastTargetSize;
    private float baseBreathingAmountMultiplier = 1f;
    private float baseBreathingSpeedMultiplier = 1f;
    private float baseHeadPositionMultiplier = 1f;
    private float baseHeadPositionSpeedMultiplier = 1f;
    private float baseHeadRotationMultiplier = 1f;
    private float baseHeadRotationSpeedMultiplier = 1f;
    private Image staticImage;
    private Color originalImageColor;
    private bool hasOriginalColor = false;

    void Awake()
    {
        CacheStaticImage();
    }

    void OnEnable()
    {
        ApplyPortraitLayout();
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

        currentPortraitRect = currentPortrait.GetComponent<RectTransform>();
        currentSourceSize = GetSourceSize(currentPortrait);
        ApplyPortraitLayout();

        DisableRaycasts(currentPortrait);

        currentAnimatedPortrait = currentPortrait.GetComponent<AnimatedPortrait>();
        if (currentAnimatedPortrait != null)
        {
            baseBreathingAmountMultiplier = currentAnimatedPortrait.BreathingAmountMultiplier;
            baseBreathingSpeedMultiplier = currentAnimatedPortrait.BreathingSpeedMultiplier;
            baseHeadPositionMultiplier = currentAnimatedPortrait.HeadPositionMultiplier;
            baseHeadPositionSpeedMultiplier = currentAnimatedPortrait.HeadPositionSpeedMultiplier;
            baseHeadRotationMultiplier = currentAnimatedPortrait.HeadRotationMultiplier;
            baseHeadRotationSpeedMultiplier = currentAnimatedPortrait.HeadRotationSpeedMultiplier;
            ApplyGlobalAnimationTweaks();
            currentAnimatedPortrait.PlayIdle();
        }
    }

    public void ClearPortrait()
    {
        if (currentPortrait != null)
            Destroy(currentPortrait);

        currentPortrait = null;
        currentPortraitRect = null;
        currentAnimatedPortrait = null;
        currentSourceSize = Vector2.zero;
        lastTargetSize = Vector2.zero;
        RestoreStaticImage();
    }

    void LateUpdate()
    {
        if (currentPortraitRect == null)
            return;

        Vector2 targetSize = GetTargetSize();
        if (targetSize != lastTargetSize || IsPortraitLayoutDirty())
            ApplyPortraitLayout();

        ApplyGlobalAnimationTweaks();
    }

    void OnRectTransformDimensionsChange()
    {
        if (currentPortraitRect != null)
            ApplyPortraitLayout();
    }

    private void ApplyPortraitLayout()
    {
        if (currentPortraitRect == null)
            return;

        if (currentSourceSize.x <= 0f || currentSourceSize.y <= 0f)
            currentSourceSize = GetSourceSize(currentPortrait);

        float fitScale = GetFitScale(currentSourceSize);
        Vector3 targetScale = Vector3.one * Mathf.Max(0.01f, Scale) * fitScale;
        currentPortraitRect.anchorMin = new Vector2(0.5f, 0.5f);
        currentPortraitRect.anchorMax = new Vector2(0.5f, 0.5f);
        currentPortraitRect.pivot = new Vector2(0.5f, 0.5f);
        currentPortraitRect.anchoredPosition = Offset;
        currentPortraitRect.localScale = targetScale;
        currentPortraitRect.localRotation = Quaternion.identity;
        currentPortraitRect.sizeDelta = currentSourceSize;
        lastTargetSize = GetTargetSize();
    }

    private bool IsPortraitLayoutDirty()
    {
        if (currentPortraitRect == null)
            return false;

        Vector3 targetScale = Vector3.one * Mathf.Max(0.01f, Scale) * GetFitScale(currentSourceSize);
        if ((currentPortraitRect.localScale - targetScale).sqrMagnitude > 0.0001f)
            return true;

        if ((currentPortraitRect.anchoredPosition - Offset).sqrMagnitude > 0.0001f)
            return true;

        if ((currentPortraitRect.sizeDelta - currentSourceSize).sqrMagnitude > 0.0001f)
            return true;

        return false;
    }

    private void ApplyGlobalAnimationTweaks()
    {
        if (currentAnimatedPortrait == null || CardContainer.Instance == null)
            return;

        currentAnimatedPortrait.BreathingAmountMultiplier =
            baseBreathingAmountMultiplier * CardContainer.Instance.AnimatedPortraitBreathingAmountMultiplier;
        currentAnimatedPortrait.BreathingSpeedMultiplier =
            baseBreathingSpeedMultiplier * CardContainer.Instance.AnimatedPortraitBreathingSpeedMultiplier;
        currentAnimatedPortrait.HeadPositionMultiplier =
            baseHeadPositionMultiplier * CardContainer.Instance.AnimatedPortraitHeadPositionMultiplier;
        currentAnimatedPortrait.HeadPositionSpeedMultiplier =
            baseHeadPositionSpeedMultiplier * CardContainer.Instance.AnimatedPortraitHeadPositionSpeedMultiplier;
        currentAnimatedPortrait.HeadRotationMultiplier =
            baseHeadRotationMultiplier * CardContainer.Instance.AnimatedPortraitHeadRotationMultiplier;
        currentAnimatedPortrait.HeadRotationSpeedMultiplier =
            baseHeadRotationSpeedMultiplier * CardContainer.Instance.AnimatedPortraitHeadRotationSpeedMultiplier;
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

        Vector2 targetSize = GetTargetSize();
        if (targetSize.x <= 0f || targetSize.y <= 0f)
            return 1f;

        return Mathf.Min(targetSize.x / sourceSize.x, targetSize.y / sourceSize.y);
    }

    private Vector2 GetTargetSize()
    {
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null)
            return Vector2.one;

        Vector2 rectSize = parentRect.rect.size;
        if (rectSize.x > 0f && rectSize.y > 0f)
            return rectSize;

        if (parentRect.sizeDelta.x > 0f && parentRect.sizeDelta.y > 0f)
            return parentRect.sizeDelta;

        return Vector2.one;
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
