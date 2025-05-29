using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{

    public ArtifactData ArtifactData;
    public int Price = 30;

    private Transform originalParent;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Vector2 originalAnchoredPos;
    public bool isSelected = false;
    private bool isDragging = false;
    private float holdTimer = 0f;
    private bool isHolding = false;
    public TMPro.TMP_Text PriceLabel;

    public void Init(ArtifactData aData)
    {
        //NameLabel.text = aData.name;
        ArtifactData = aData;
        Price = 6;

        PriceLabel.text = Price.ToString();
    }
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

    }

    public void OnBeginDrag(PointerEventData eventData)
    {


        if (isSelected)
            rectTransform.anchoredPosition = originalAnchoredPos;


        isDragging = true;
        isSelected = false;

        originalAnchoredPos = rectTransform.anchoredPosition;
        UIManager.Instance.HideCardInfoPopup();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, canvas.worldCamera, out globalMousePos))
        {
            rectTransform.position = globalMousePos;
        }

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        {
            isDragging = false;


            rectTransform.anchoredPosition = originalAnchoredPos;

        }


        canvasGroup.blocksRaycasts = true;

        if (IsOverArtifactSlot())
        {
            if (GameData.CurrentGold >= Price)
            {
                GameData.CurrentGold -= Price;
                UIManager.Instance.UpdateLabels();

                ArtifactManager.Instance.AddArtifact(ArtifactData); // Add logic here
                canvasGroup.blocksRaycasts = true;
                //Destroy(gameObject);
                canvasGroup.alpha = 0;
                enabled = false;
            }
            else
            {
                UIManager.Instance.ShowTooltip("Not enough gold!");
                ReturnToShop();
            }
        }
        else
        {
            ReturnToShop();
        }
    }

    private bool IsOverArtifactSlot()
    {
            if (RectTransformUtility.RectangleContainsScreenPoint(UIManager.Instance.BuyItemArea, Input.mousePosition,Camera.main))
            {
                return true;
            }

        return false;
    }

    private void ReturnToShop()
    {
        rectTransform.anchoredPosition = originalAnchoredPos;
        UIManager.Instance.HideCardInfoPopup();
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return;

 


        if (!isSelected)
        {
            UIManager.Instance.ShowCardInfoPopup(
              ArtifactData.name,
              ArtifactData.description,
              ArtifactData.icon,
              transform
          );
            isSelected = true;
        }
        else
        {
            //rectTransform.anchoredPosition = originalAnchoredPos;
            isSelected = false;
            UIManager.Instance.HideCardInfoPopup();
        }

    }

    void Update()
    {
        //if (isHolding)
        //{
        //    holdTimer += Time.deltaTime;
        //    if (holdTimer > 0.6f) // 400 ms hold
        //    {
        //        isHolding = false;
        //        UIManager.Instance.ShowCardInfoPopup(
        //            ArtifactData.name,
        //            ArtifactData.description,
        //            ArtifactData.icon,
        //            transform
        //        );
        //    }
        //}
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
    }
}
