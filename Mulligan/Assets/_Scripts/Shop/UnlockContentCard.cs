using UnityEngine;
using UnityEngine.EventSystems;

public class UnlockContentCard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private const float TapMoveThreshold = 10f;

    public Transform PopupTarget;
    public bool AllowLongPress = true;
    public bool IsInteractable = true;
    private string title;
    private string description;
    private bool isHolding = false;
    private float holdTimer = 0f;
    private Vector2 pointerDownPosition;
    private bool showedInfoThisPress = false;
    private bool pointerMovedTooFar = false;

    public void Init(string aTitle, string aDescription, Transform aPopupTarget)
    {
        title = aTitle;
        description = aDescription;
        PopupTarget = aPopupTarget;
    }

    void Update()
    {
        if (IsInteractable == false)
        {
            isHolding = false;
            holdTimer = 0f;
            return;
        }

        if (isHolding == false)
            return;

        if (Vector2.Distance(pointerDownPosition, Input.mousePosition) > TapMoveThreshold)
        {
            pointerMovedTooFar = true;
            isHolding = false;
            holdTimer = 0f;
            return;
        }

        if (AllowLongPress == false)
            return;

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
        if (IsInteractable == false)
            return;

        showedInfoThisPress = false;
        pointerMovedTooFar = false;
        isHolding = true;
        holdTimer = 0f;
        pointerDownPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (IsInteractable == false)
            return;

        isHolding = false;
        holdTimer = 0f;

        if (showedInfoThisPress)
            return;

        if (pointerMovedTooFar || Vector2.Distance(pointerDownPosition, eventData.position) > TapMoveThreshold)
            return;

        if (UIManager.Instance.currentTransform == PopupTarget)
            UIManager.Instance.HideCardInfoPopup();
        else
            ShowInfo();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsInteractable == false)
            return;

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
