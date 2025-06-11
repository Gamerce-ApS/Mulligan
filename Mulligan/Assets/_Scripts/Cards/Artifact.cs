using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Artifact : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Vector2 originalAnchoredPos;
    public bool isSelected = false;
    private bool isDragging = false;
    private float holdTimer = 0f;
    private bool isHolding = false;
    public ArtifactData ArtifactData;
    public TMPro.TMP_Text NameLabel;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }
    public void Init(ArtifactData aData)
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
                    ArtifactData.name,
                    ArtifactData.description,
                    "",
                    transform
                );
            }
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {

        {
            isDragging = false;
 

            rectTransform.anchoredPosition = originalAnchoredPos;

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
    

    public GameObject DmgNumber = null;
    bool isCriticalBonus = false;
    public void AddDamage(int damageAmount, System.Action onComplete, bool isCrit = false)
    {
        if (damageAmount == 0)
        {
            onComplete?.Invoke();
            return;
        }
        // Pulse
        LeanTween.scale(gameObject, Vector3.one * 1.3f, 0.5f)
        .setEasePunch();
        isCriticalBonus = isCrit;
        int totalD = damageAmount;
        // 3. Create damage number above the card
        if (DmgNumber == null)
        {
            DmgNumber = Instantiate(UIManager.Instance.DamageFloatPrefab, transform.position, Quaternion.identity, transform);
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
            DmgNumber.GetComponent<TMPro.TMP_Text>().fontSize = 50;
            dmgRT.anchoredPosition -= new Vector2(0, 165f);

        }
        else
        {
            dmgRT.anchoredPosition -= new Vector2(0, 175f);
            dmgText.text = "+" + totalD;
        }




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

            if(isCriticalBonus)
                 worldTarget = UIManager.Instance.CriticalLabel.transform.position;


            LeanTween.move(DmgNumber, worldTarget, 0.25f)
                .setEaseInCubic()
                .setOnComplete(() =>
                {
                    string amount = DmgNumber.GetComponent<TMPro.TMP_Text>().text;
                    Destroy(DmgNumber);
                    DmgNumber = null;

                    // After animation add to the total
                    if (isCriticalBonus)
                        UIManager.Instance.AddCritical(int.Parse(amount.Replace(" Critical", "")));
                    else
                        UIManager.Instance.AddDamage(int.Parse(amount));

                    onComplete?.Invoke();
                });
        });
    }
}
