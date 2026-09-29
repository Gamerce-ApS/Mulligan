using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardRenderTextureCapture : Singleton<CardRenderTextureCapture>
{
    [Header("Output")]
    public int TextureWidth = 512;
    public int TextureHeight = 768;
    public int Padding = 8;
    public FilterMode FilterMode = FilterMode.Bilinear;

    [Header("Material Output")]
    public Material TargetMaterial;
    public string TexturePropertyName = "_MainTex";
    public Texture CapturedTexture;

    [Header("Destroy VFX")]
    public GameObject DestroyCardVFX;
    public Camera VFXWorldCamera;

    [Header("Merge VFX")]
    public GameObject MergeCardVFX;
    public SpriteRenderer MergeCardSpriteRenderer;
    public float MergeSpritePixelsPerUnit = 100f;
    [Min(0f)] public float MergeVFXShowBeforeArrival = 0.2f;

    [Header("Capture")]
    [Range(0, 31)] public int CaptureLayer = 31;

    private readonly HashSet<RenderTexture> capturedTextures = new HashSet<RenderTexture>();
    private GameObject captureRoot;
    private Camera captureCamera;
    private Canvas captureCanvas;
    private Texture2D mergeCardTexture;
    private Sprite mergeCardSprite;
    private Card mergeFollowCard;

    protected override void Awake()
    {
        base.Awake();
    }

    public RenderTexture CaptureCard(Card card)
    {
        if (card == null)
        {
            Debug.LogWarning("Cannot capture a null card.");
            return null;
        }

        if (card.bg == null || card.bg.transform.parent == null)
        {
            Debug.LogWarning("Card capture requires the card background and its visual parent.", card);
            return null;
        }

        RectTransform visualRoot = card.bg.transform.parent as RectTransform;
        if (visualRoot == null || visualRoot.rect.width <= 0f || visualRoot.rect.height <= 0f)
        {
            Debug.LogWarning("Card capture could not find a valid visual RectTransform.", card);
            return null;
        }

        EnsureCaptureSetup();

        int outputWidth = Mathf.Max(1, TextureWidth);
        int outputHeight = Mathf.Max(1, TextureHeight);
        RenderTexture texture = CreateCaptureTexture(card);
        GameObject visualClone = null;
        RenderTexture previousActiveTexture = RenderTexture.active;

        try
        {
            captureCamera.targetTexture = texture;
            captureCamera.aspect = outputWidth / (float)outputHeight;
            Canvas.ForceUpdateCanvases();

            visualClone = Instantiate(visualRoot.gameObject, captureCanvas.transform, false);
            visualClone.name = card.gameObject.name + " Capture";
            visualClone.SetActive(true);
            SetLayerRecursively(visualClone, CaptureLayer);

            RectTransform cloneRect = visualClone.GetComponent<RectTransform>();
            cloneRect.anchorMin = new Vector2(0.5f, 0.5f);
            cloneRect.anchorMax = new Vector2(0.5f, 0.5f);
            cloneRect.pivot = new Vector2(0.5f, 0.5f);
            cloneRect.anchoredPosition = Vector2.zero;
            cloneRect.localRotation = Quaternion.identity;

            float availableWidth = Mathf.Max(1f, outputWidth - Padding * 2f);
            float availableHeight = Mathf.Max(1f, outputHeight - Padding * 2f);
            float fitScale = Mathf.Min(
                availableWidth / Mathf.Abs(visualRoot.rect.width),
                availableHeight / Mathf.Abs(visualRoot.rect.height));
            cloneRect.localScale = Vector3.one * fitScale;

            foreach (TMP_Text label in visualClone.GetComponentsInChildren<TMP_Text>(true))
                label.ForceMeshUpdate(true);

            Canvas.ForceUpdateCanvases();
            captureCamera.Render();

            CapturedTexture = texture;
            ApplyTextureToMaterial(texture);
            return texture;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, card);
            captureCamera.targetTexture = null;
            ReleaseTexture(texture);
            return null;
        }
        finally
        {
            captureCamera.targetTexture = null;
            RenderTexture.active = previousActiveTexture;

            if (visualClone != null)
            {
                visualClone.SetActive(false);
                Destroy(visualClone);
            }
        }
    }

    public void ReleaseTexture(RenderTexture texture)
    {
        if (texture == null || capturedTextures.Remove(texture) == false)
            return;

        if (TargetMaterial != null &&
            string.IsNullOrWhiteSpace(TexturePropertyName) == false &&
            TargetMaterial.HasProperty(TexturePropertyName) &&
            TargetMaterial.GetTexture(TexturePropertyName) == texture)
        {
            TargetMaterial.SetTexture(TexturePropertyName, null);
        }

        if (CapturedTexture == texture)
            CapturedTexture = null;

        if (texture.IsCreated())
            texture.Release();

        Destroy(texture);
    }

    public RenderTexture CaptureAndPlayDestroy(Card card)
    {
        RenderTexture texture = CaptureCard(card);
        if (texture != null)
            PositionAndRestartVFX(DestroyCardVFX, card);

        return texture;
    }

    public RenderTexture CaptureAndPlayMerge(Card card)
    {
        RenderTexture texture = CaptureCard(card);
        if (texture == null || MergeCardVFX == null)
            return texture;

        MergeCardVFX.SetActive(false);
        PositionVFX(MergeCardVFX, card);
        ApplyTextureToMergeSprite(texture);
        mergeFollowCard = card;
        MergeCardVFX.SetActive(true);

        SpriteRenderer spriteRenderer = GetMergeSpriteRenderer();
        if (spriteRenderer != null && mergeCardSprite != null)
            spriteRenderer.sprite = mergeCardSprite;

        return texture;
    }

    public void StopMergeVFX()
    {
        mergeFollowCard = null;

        if (MergeCardVFX != null)
            MergeCardVFX.SetActive(false);
    }

    private void LateUpdate()
    {
        if (mergeFollowCard == null || MergeCardVFX == null || MergeCardVFX.activeSelf == false)
            return;

        if (mergeFollowCard.gameObject.activeInHierarchy == false)
        {
            StopMergeVFX();
            return;
        }

        PositionVFX(MergeCardVFX, mergeFollowCard);
    }

    private void ApplyTextureToMaterial(RenderTexture texture)
    {
        if (TargetMaterial == null)
            return;

        if (string.IsNullOrWhiteSpace(TexturePropertyName) ||
            TargetMaterial.HasProperty(TexturePropertyName) == false)
        {
            Debug.LogWarning(
                $"Material '{TargetMaterial.name}' does not have texture property '{TexturePropertyName}'.",
                this);
            return;
        }

        TargetMaterial.SetTexture(TexturePropertyName, texture);
    }

    private void ApplyTextureToMergeSprite(RenderTexture texture)
    {
        SpriteRenderer spriteRenderer = GetMergeSpriteRenderer();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("Merge card VFX requires a SpriteRenderer.", this);
            return;
        }

        RenderTexture previousActiveTexture = RenderTexture.active;

        try
        {
            RenderTexture.active = texture;

            Texture2D cardTexture = new Texture2D(
                texture.width,
                texture.height,
                TextureFormat.RGBA32,
                false);
            cardTexture.name = "Merge Card Texture";
            cardTexture.filterMode = FilterMode;
            cardTexture.wrapMode = TextureWrapMode.Clamp;
            cardTexture.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0, false);
            cardTexture.Apply(false, false);

            Sprite cardSprite = Sprite.Create(
                cardTexture,
                new Rect(0f, 0f, cardTexture.width, cardTexture.height),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(1f, MergeSpritePixelsPerUnit));
            cardSprite.name = "Merge Card Sprite";

            ClearMergeSprite();
            mergeCardTexture = cardTexture;
            mergeCardSprite = cardSprite;
            spriteRenderer.sprite = mergeCardSprite;
        }
        finally
        {
            RenderTexture.active = previousActiveTexture;
        }
    }

    private void ClearMergeSprite()
    {
        SpriteRenderer spriteRenderer = GetMergeSpriteRenderer();
        if (spriteRenderer != null && spriteRenderer.sprite == mergeCardSprite)
            spriteRenderer.sprite = null;

        if (mergeCardSprite != null)
            Destroy(mergeCardSprite);

        if (mergeCardTexture != null)
            Destroy(mergeCardTexture);

        mergeCardSprite = null;
        mergeCardTexture = null;
    }

    private SpriteRenderer GetMergeSpriteRenderer()
    {
        if (MergeCardSpriteRenderer != null)
            return MergeCardSpriteRenderer;

        if (MergeCardVFX != null)
            MergeCardSpriteRenderer = MergeCardVFX.GetComponentInChildren<SpriteRenderer>(true);

        return MergeCardSpriteRenderer;
    }

    private void PositionAndRestartVFX(GameObject vfx, Card card)
    {
        if (vfx == null || card == null)
            return;

        vfx.SetActive(false);
        PositionVFX(vfx, card);
        vfx.SetActive(true);
    }

    private void PositionVFX(GameObject vfx, Card card)
    {
        if (vfx == null || card == null)
            return;

        Camera worldCamera = VFXWorldCamera != null ? VFXWorldCamera : Camera.main;
        if (worldCamera == null)
        {
            Debug.LogWarning("Card VFX requires a world camera.", this);
            return;
        }

        Canvas cardCanvas = card.GetComponentInParent<Canvas>();
        Camera uiCamera = cardCanvas != null && cardCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? cardCanvas.worldCamera
            : null;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, card.rectTransform.position);
        float worldDepth = worldCamera.WorldToScreenPoint(vfx.transform.position).z;

        if (worldDepth <= worldCamera.nearClipPlane)
            worldDepth = worldCamera.nearClipPlane + 1f;

        Vector3 vfxPosition = worldCamera.ScreenToWorldPoint(new Vector3(
            screenPosition.x,
            screenPosition.y,
            worldDepth));

        vfx.transform.position = vfxPosition;
    }

    private void EnsureCaptureSetup()
    {
        if (captureRoot != null)
        {
            SetLayerRecursively(captureRoot, CaptureLayer);
            captureCamera.cullingMask = 1 << CaptureLayer;
            return;
        }

        captureRoot = new GameObject("Card Capture Runtime");
        captureRoot.transform.SetParent(transform, false);
        captureRoot.layer = CaptureLayer;

        GameObject cameraObject = new GameObject("Capture Camera", typeof(Camera));
        cameraObject.transform.SetParent(captureRoot.transform, false);
        cameraObject.layer = CaptureLayer;
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);

        captureCamera = cameraObject.GetComponent<Camera>();
        captureCamera.enabled = false;
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = Color.clear;
        captureCamera.cullingMask = 1 << CaptureLayer;
        captureCamera.orthographic = true;
        captureCamera.orthographicSize = 5f;
        captureCamera.nearClipPlane = 0.01f;
        captureCamera.farClipPlane = 100f;
        captureCamera.allowHDR = false;
        captureCamera.allowMSAA = false;

        GameObject canvasObject = new GameObject(
            "Capture Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        canvasObject.transform.SetParent(captureRoot.transform, false);
        canvasObject.layer = CaptureLayer;

        captureCanvas = canvasObject.GetComponent<Canvas>();
        captureCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        captureCanvas.worldCamera = captureCamera;
        captureCanvas.planeDistance = 1f;
        captureCanvas.pixelPerfect = false;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasScaler.scaleFactor = 1f;
        canvasScaler.referencePixelsPerUnit = 100f;
    }

    private RenderTexture CreateCaptureTexture(Card card)
    {
        RenderTexture texture = new RenderTexture(
            Mathf.Max(1, TextureWidth),
            Mathf.Max(1, TextureHeight),
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default)
        {
            name = card.gameObject.name + " Card Capture",
            filterMode = FilterMode,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            antiAliasing = 1
        };

        texture.Create();
        capturedTextures.Add(texture);
        return texture;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void OnValidate()
    {
        TextureWidth = Mathf.Max(1, TextureWidth);
        TextureHeight = Mathf.Max(1, TextureHeight);
        Padding = Mathf.Clamp(Padding, 0, Mathf.Min(TextureWidth, TextureHeight) / 2);
        CaptureLayer = Mathf.Clamp(CaptureLayer, 0, 31);
        MergeSpritePixelsPerUnit = Mathf.Max(1f, MergeSpritePixelsPerUnit);
        MergeVFXShowBeforeArrival = Mathf.Max(0f, MergeVFXShowBeforeArrival);
    }

    private void OnDestroy()
    {
        mergeFollowCard = null;

        List<RenderTexture> texturesToRelease = new List<RenderTexture>(capturedTextures);
        foreach (RenderTexture texture in texturesToRelease)
            ReleaseTexture(texture);

        ClearMergeSprite();

        if (captureRoot != null)
            Destroy(captureRoot);
    }
}
