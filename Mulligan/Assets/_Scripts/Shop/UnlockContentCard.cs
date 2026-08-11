using UnityEngine;
using UnityEngine.EventSystems;

public class UnlockContentCard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Transform PopupTarget;
    private string title;
    private string description;
    private bool isHolding = false;
    private float holdTimer = 0f;
    private Vector2 pointerDownPosition;
    private bool showedInfoThisPress = false;

    public void Init(string aTitle, string aDescription, Transform aPopupTarget)
    {
        title = aTitle;
        description = aDescription;
        PopupTarget = aPopupTarget;
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
            ShowInfo();
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

        if (UIManager.Instance.currentTransform == PopupTarget)
            UIManager.Instance.HideCardInfoPopup();
        else
            ShowInfo();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHolding = false;
        holdTimer = 0f;
    }

    private void ShowInfo()
    {
        UIManager.Instance.ShowCardInfoPopup(
            title,
            description,
            "",
            PopupTarget
        );
    }
}
