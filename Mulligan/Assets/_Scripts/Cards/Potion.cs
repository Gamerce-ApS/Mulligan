using System.Collections;
using System.Collections.Generic;
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

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }
    public void Init(PotionCardData aData)
    {
        NameLabel.text = aData.name;
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

        if(isSelected)
            rectTransform.anchoredPosition = originalAnchoredPos;


        isDragging = true;
        isSelected = false;

        originalAnchoredPos = rectTransform.anchoredPosition;

    }

    public void OnDrag(PointerEventData eventData)
    {
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
                hoveredCard.transform.rotation = Quaternion.identity;
                shakeCoroutine = null;
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
                    PotionData.description,
                    "",
                    transform
                );
            }
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {


            isDragging = false;
 

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
        }
        UIManager.Instance.HideCardInfoPopup();

    }

    private void ApplyPotionToCard(PotionCardData potion, Card target)
    {
        //PotionEffectEvaluator.ApplyPotion(potion, target); // or however you apply effects
        PotionManager.Instance.TriggerPotion(potion,target);
        // Optional: play animation
        LeanTween.scale(gameObject, Vector3.zero, 0.5f)
            .setEaseInBack()
            .setOnComplete(() => Destroy(gameObject));

        PotionManager.Instance.RemovePotion(PotionData);
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        holdTimer = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        holdTimer = 0f;
        UIManager.Instance.HideCardInfoPopup();
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            hoveredCard.transform.rotation = Quaternion.identity;
            shakeCoroutine = null;
        }

    }
    


}
