using UnityEngine;
using UnityEngine.EventSystems;

public class DeckOverviewCard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Card Card;
    private bool isHolding = false;
    private float holdTimer = 0f;
    private Vector2 pointerDownPosition;
    private bool showedInfoThisPress = false;

    public void Init(Card card)
    {
        Card = card;
    }

    void Update()
    {
        if (isHolding == false)
            return;

        if (Vector2.Distance(pointerDownPosition, Input.mousePosition) > 10f)
        {
            isHolding = false;
            holdTimer = 0f;
            return;
        }

        holdTimer += Time.deltaTime;
        if (holdTimer > 0.4f)
        {
            isHolding = false;
            showedInfoThisPress = true;
            UIManager.Instance.ShowCardInfoPopup(
                Card.NameLabel.text,
                Card.GetDescription(),
                "",
                Card.transform
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        showedInfoThisPress = false;
        isHolding = true;
        holdTimer = 0f;
        pointerDownPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        holdTimer = 0f;

        if (showedInfoThisPress)
            return;

        if (UIManager.Instance.currentTransform != null)
        {
            UIManager.Instance.HideCardInfoPopup();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHolding = false;
        holdTimer = 0f;
    }
}
