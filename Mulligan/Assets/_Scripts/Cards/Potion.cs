using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Potion : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Vector2 originalAnchoredPos;
    public bool isSelected = false;
    private bool isDragging = false;
    private float holdTimer = 0f;
    private bool isHolding = false;
    public PotionCardData PotionData;
    public TMPro.TMP_Text NameLabel;
    private Card hoveredCard = null;
    private Coroutine shakeCoroutine = null;
    public GameObject mutedGO;
    public bool isMuted = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }
    public void SetMuted(bool mute)
    {
        isMuted = mute;
        mutedGO.SetActive(isMuted);
    }
    public void Init(PotionCardData aData)
    {
        NameLabel.text = aData.name;
        NameLabel.color = UIManager.Instance.GetTextColor(aData.rarity);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return;

        if (!isSelected)
        {
            //originalAnchoredPos = rectTransform.anchoredPosition;
            //rectTransform.anchoredPosition += new Vector2(0, 70f); // Lift
            isSelected = true;
        }
        else
        {
            //rectTransform.anchoredPosition = originalAnchoredPos;
            isSelected = false;
        }

    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if(isMuted)
        return;
        // Debug.Log("Potion BeginDrag");
        if (isSelected)
            rectTransform.anchoredPosition = originalAnchoredPos;


        isDragging = true;
        isSelected = false;

        originalAnchoredPos = rectTransform.anchoredPosition;
        if(ShopManager.Instance.ShopWindow.activeSelf)
            UIManager.Instance.SellItemArea.gameObject.SetActive(true);
    }
    private bool IsOverSellSlot()
    {
        if(ShopManager.Instance.ShopWindow.activeSelf == false)
            return false;
        if (RectTransformUtility.RectangleContainsScreenPoint(UIManager.Instance.SellItemArea, Input.mousePosition, Camera.main))
        {
            return true;
        }

        return false;
    }
    public void OnDrag(PointerEventData eventData)
    {
             if(isMuted)
        return;
        // Debug.Log("Potion Drag");
        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, canvas.worldCamera, out globalMousePos))
        {
            rectTransform.position = globalMousePos;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        Card detectedCard = null;

        foreach (var result in raycastResults)
        {
            Card card = result.gameObject.GetComponent<Card>();
            if (card != null && card.myType == CardTypeEnum.UnitCard)
            {
                detectedCard = card;
                break;
            }
        }

        if (detectedCard != hoveredCard)
        {
            // Stop old shake
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }

            if (hoveredCard != null)
            {
                hoveredCard.transform.rotation = Quaternion.identity;
            }

            hoveredCard = detectedCard;

            if (hoveredCard != null)
            {
                shakeCoroutine = StartCoroutine(ShakeCard(hoveredCard.gameObject));
            }
        }

        UIManager.Instance.HideCardInfoPopup();

    }
    private IEnumerator ShakeCard(GameObject target)
    {
        float shakeAmount = 5f;
        float shakeSpeed = 10f;

        while (true)
        {
            float z = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            target.transform.rotation = Quaternion.Euler(0, 0, z);
            yield return null;
        }
    }
    void Update()
    {
        if (isHolding)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer > 0.6f) // 400 ms hold
            {
                isHolding = false;
                UIManager.Instance.ShowCardInfoPopup(
                    PotionData.name,
                    PotionData.description + PotionData.GetRarityText(),
                    "",
                    transform
                );
            }
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
             if(isMuted)
        return;
        // Debug.Log("Potion EndDrag");

        isDragging = false;


    Vector2 myScreenPos = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
    Vector2 heroScreenPos = RectTransformUtility.WorldToScreenPoint(null, GameManager.Instance.TheHero.GetComponent<RectTransform>().position);
float baseThreshold = 75f; // what feels good in editor
float scaleFactor = Screen.height / 1792f; // reference resolution
float threshold = baseThreshold * scaleFactor;

        rectTransform.anchoredPosition = originalAnchoredPos;

        if (shakeCoroutine != null && hoveredCard != null)
        {
            StopCoroutine(shakeCoroutine);
            hoveredCard.transform.rotation = Quaternion.identity;
            shakeCoroutine = null;
        }

        if (hoveredCard != null)
        {
            ApplyPotionToCard(PotionData, hoveredCard);
        }else if(PotionData.effectType == PotionEffectType.HealHero)
        {
            float dist = Vector2.Distance( myScreenPos, heroScreenPos);
            Debug.Log("Dist:" + dist);
            if(dist< threshold)
            {
                ApplyPotionToCard(PotionData, null);
   
                

            }
        }
        UIManager.Instance.HideCardInfoPopup();
        UIManager.Instance.UpdateArtifactSlotsUI();
    }

    private void ApplyPotionToCard(PotionCardData potion, Card target)
    {
        //PotionEffectEvaluator.ApplyPotion(potion, target); // or however you apply effects
        PotionManager.Instance.TriggerPotion(potion, target);
        // Optional: play animation
        LeanTween.scale(gameObject, Vector3.zero, 0.5f)
            .setEaseInBack()
            .setOnComplete(() => Destroy(gameObject));

        // PotionManager.Instance.RemovePotion(PotionData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Debug.Log("Potion PointerDown");
        isHolding = true;
        holdTimer = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {

        // Debug.Log("Potion PointerUp");
        // OnEndDrag(eventData);
        isHolding = false;
        holdTimer = 0f;
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        if (hoveredCard != null)
        {
            hoveredCard.transform.rotation = Quaternion.identity;
        }
        //UIManager.Instance.UpdateArtifactSlotsUI();



        if (isDragging == false)
        {
            if (UIManager.Instance.currentTransform == transform)
            {
                UIManager.Instance.HideCardInfoPopup();
            }
            else
            {
                UIManager.Instance.ShowCardInfoPopup(
                         PotionData.name,
                         PotionData.description + PotionData.GetRarityText(),
                         "",
                         transform
                     );
            }
        }
        else
        {
            UIManager.Instance.HideCardInfoPopup();
            // UIManager.Instance.UpdateArtifactSlotsUI();


            if (IsOverSellSlot())
            {
                PotionManager.Instance.SellPotion(this); // Add logic here

                UIManager.Instance.ShowTooltip("Potion sold!");

            }
            else
            {

            }

            UIManager.Instance.SellItemArea.gameObject.SetActive(false);
        }


    }



}
