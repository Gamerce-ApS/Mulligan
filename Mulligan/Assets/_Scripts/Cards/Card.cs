using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Card : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Transform originalParent;
    private Vector2 originalAnchoredPos;
    public bool isSelected = false;
    private bool isDragging = false;

    private GameObject placeholder;
    private LayoutElement layoutElement;

    public TMPro.TMP_Text NameLabel;
    public TMPro.TMP_Text DamageLabel;
    public TMPro.TMP_Text RankLabel;
    public Image Portrait_BG;
    public Image Portrait;
    public Image RaceIcon;
    public Image ClassIcon;

    private float holdTimer = 0f;
    private bool isHolding = false;
    public CardInstance cardInstance;

    public System.Action<Card> OnClick = null;
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }
    public void Init(CardInstance aCardInstance)
    {
        cardInstance = aCardInstance;
        // Sett image, name, type and so on
        NameLabel.text = aCardInstance.data.cardName;

        string[] splitName = aCardInstance.data.cardName.Split(" ");
        NameLabel.text = splitName[0] + "\n" + splitName[1];

        DamageLabel.text = aCardInstance.data.damage.ToString();
        DamageLabel.text = aCardInstance.GetDamage().ToString();

        Portrait.sprite = aCardInstance.data.portrait;
        RaceIcon.sprite = CardContainer.Instance.GetSpriteForRace(aCardInstance.data.race);
        ClassIcon.sprite = CardContainer.Instance.GetSpriteForClass(aCardInstance.data.cardClass);
        Portrait_BG.color = CardContainer.Instance.GetColorForRace(aCardInstance.data.race);
        RankLabel.text = aCardInstance.currentRank.ToString();

        if (aCardInstance.currentRank == 0)
            RankLabel.transform.parent.gameObject.SetActive(false);
        else
        {
            RankLabel.transform.parent.gameObject.SetActive(true);
        }


    }
    public void UpdateCardUI()
    {
        if (cardInstance.currentRank == 0)
            RankLabel.transform.parent.gameObject.SetActive(false);
        else
        {
            RankLabel.text = cardInstance.currentRank.ToString();
            RankLabel.transform.parent.gameObject.SetActive(true);
        }

        DamageLabel.text = cardInstance.GetDamage().ToString();

    }
    void Update()
    {
        if (isHolding)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer > 0.4f) // 400 ms hold
            {
                isHolding = false;
                UIManager.Instance.ShowCardInfoPopup(
                    NameLabel.text,
                    "Description",
                    Portrait.sprite,
                    transform
                );
            }
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if(OnClick != null)
        {
            OnClick.Invoke(this);
            originalAnchoredPos = rectTransform.anchoredPosition;
            LeanTween.scale(gameObject, transform.localScale * 1.3f, 0.5f).setEasePunch();

            rectTransform.anchoredPosition += new Vector2(0, 20f); // Lift
            rectTransform.rotation = Quaternion.identity;
            return;
        }
        if (isDragging) return;

        if (!isSelected)
        {
            if (HandManager.Instance.SelectedCardCount() >= 4)
            {
                LeanTween.scale(gameObject, transform.localScale * 1.1f, 0.2f).setEasePunch();
                UIManager.Instance.ShowTooltip("Only 4 cards can be selected.");
                return;
            }

            originalAnchoredPos = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition += new Vector2(0, 70f); // Lift
            isSelected = true;
        }
        else
        {
            rectTransform.anchoredPosition = originalAnchoredPos;
            isSelected = false;
        }

        UIManager.Instance.ShowSynergies();
        UIManager.Instance.RefreshPreDamage();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {

        if(isSelected)
            rectTransform.anchoredPosition = originalAnchoredPos;


        isDragging = true;
        isSelected = false;

        originalParent = transform.parent;
        originalAnchoredPos = rectTransform.anchoredPosition;

        // Create placeholder to keep grid position
        placeholder = new GameObject("Card Placeholder");
        layoutElement = placeholder.AddComponent<LayoutElement>();

        // Match size to the card
        LayoutElement thisLayout = GetComponent<LayoutElement>();
        if (thisLayout != null)
        {
            layoutElement.preferredWidth = thisLayout.preferredWidth;
            layoutElement.preferredHeight = thisLayout.preferredHeight;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
        }

        placeholder.transform.SetParent(originalParent);
        placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex());

        // Move card out to canvas (visually)
        transform.SetParent(canvas.transform, true);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, canvas.worldCamera, out globalMousePos))
        {
            rectTransform.position = globalMousePos;
        }
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
    public void OnEndDrag(PointerEventData eventData)
    {

        {
            isDragging = false;
 
            // Return to grid in same position as placeholder
            int returnIndex = placeholder.transform.GetSiblingIndex();
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(returnIndex);
            transform.localScale = Vector3.one;
            rectTransform.anchoredPosition = originalAnchoredPos;

            // Clean up placeholder
            Destroy(placeholder);
            canvasGroup.blocksRaycasts = true;
        }
    }
    public void LiftCard()
    {
        float liftHeight = 50f;
        float scaleTime = 0.2f;

        RectTransform rt = GetComponent<RectTransform>();
        Vector2 startAnchored = rt.anchoredPosition;
        Vector2 targetAnchored = startAnchored + new Vector2(0, liftHeight);

        LeanTween.value(gameObject, startAnchored, targetAnchored, scaleTime)
        .setEaseOutCubic()
        .setOnUpdate((Vector2 val) => rt.anchoredPosition = val);
    }
    public void PlayBoostAnimation( int damageAmount, Transform targetLabel, System.Action onComplete = null)
    {
        float delay =  0.0f; // spread delay per card
        LeanTween.delayedCall(gameObject, delay, () =>
        {
            // Lift
            LeanTween.delayedCall(gameObject, delay, () =>
            {
                // Pulse
                LeanTween.scale(gameObject, Vector3.one * 1.3f, 0.5f)
                .setEasePunch();

            // 3. Create damage number above the card
            GameObject dmgObj = Instantiate(UIManager.Instance.DamageFloatPrefab, transform.position, Quaternion.identity, transform.parent);
            RectTransform dmgRT = dmgObj.GetComponent<RectTransform>();
            TMPro.TMP_Text dmgText = dmgObj.GetComponent<TMPro.TMP_Text>();
            dmgText.text = "+" + damageAmount;

            dmgRT.anchoredPosition += new Vector2(0, 250f);

            // 4. Animate punch scale in
            dmgRT.localScale = Vector3.zero;
            LeanTween.scale(dmgObj, Vector3.one*1.2f, 0.3f).setEaseOutBack();


            // 5. Wait, then fly to damage label
            LeanTween.delayedCall(dmgObj, 0.75f, () =>
            {
                Vector3 worldTarget = targetLabel.position;

                LeanTween.move(dmgObj, worldTarget, 0.25f)
                    .setEaseInCubic()
                    .setOnComplete(() =>
                    {
                        Destroy(dmgObj);
                        onComplete?.Invoke();
                    });
            });

            });
        });
    }

    public GameObject DmgNumber = null;
    public void AddDamage(int damageAmount, System.Action onComplete, bool isCrit = false)
    {
        if(damageAmount==0)
        {
            onComplete?.Invoke();
            return;
        }
        // Pulse
        LeanTween.scale(gameObject, Vector3.one * 1.3f, 0.5f)
        .setEasePunch();





        int totalD = damageAmount;
        // 3. Create damage number above the card
        if (DmgNumber == null)
        {
            DmgNumber = Instantiate(UIManager.Instance.DamageFloatPrefab, transform.position, Quaternion.identity, transform.parent);
            DmgNumber.GetComponent<TMPro.TMP_Text>().text = "0";
        }
        else
        {
            DmgNumber.transform.position = transform.position;
        }

        RectTransform dmgRT = DmgNumber.GetComponent<RectTransform>();
        TMPro.TMP_Text dmgText = DmgNumber.GetComponent<TMPro.TMP_Text>();


        totalD = int.Parse(dmgText.text.Replace("+", "")) + damageAmount;

        if (isCrit)
        {
            dmgText.text = "+" + totalD + " Critical";
        }
        else
            dmgText.text = "+" + totalD;

        dmgRT.anchoredPosition += new Vector2(0, 250f);

        // 4. Animate punch scale in
        dmgRT.localScale = Vector3.zero;
        LeanTween.scale(DmgNumber, Vector3.one * 1.2f, 0.3f).setEaseOutBack();

        LeanTween.delayedCall(DmgNumber, 1.0f, () =>
        {
            onComplete?.Invoke();
        });
    }



    
    public void AddToTotalDamage(System.Action onComplete)
    {
        // 5. Wait, then fly to damage label
        LeanTween.delayedCall(DmgNumber, 0.75f, () =>
        {
            Vector3 worldTarget = UIManager.Instance.DamageLabel.transform.position;

            LeanTween.move(DmgNumber, worldTarget, 0.25f)
                .setEaseInCubic()
                .setOnComplete(() =>
                {
                    string amount = DmgNumber.GetComponent<TMPro.TMP_Text>().text;
                    Destroy(DmgNumber);
                    DmgNumber = null;

                    // After animation add to the total
                    UIManager.Instance.AddDamage(int.Parse(amount));

                    onComplete?.Invoke();
                });
        });
    }
    public void FlyAwayAndDiscard(Vector3 flyTargetWorld, float delay,CardInstance cInstance)
    {
        float flyTime = 0.5f;
        float rotateAngle = 360f;

        // Disable interactions
        canvasGroup.blocksRaycasts = false;

        // Animate after optional delay
        LeanTween.delayedCall(gameObject, delay, () =>
        {
            // Rotate and move to target
            LeanTween.move(gameObject, flyTargetWorld, flyTime)
                .setEaseInCubic();

            LeanTween.rotateZ(gameObject, rotateAngle, flyTime)
                .setEaseInOutCubic();

            // Scale down
            LeanTween.scale(gameObject, Vector3.zero, flyTime)
                .setEaseInBack();

            // After animation, add to discard pile and destroy visual
            LeanTween.delayedCall(gameObject, flyTime, () =>
            {
                CardContainer.Instance.DiscardCard(cInstance);
                Destroy(gameObject);
            });
        });
    }

}
