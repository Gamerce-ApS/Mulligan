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
    public TMPro.TMP_Text CounterLabel;
    public GameObject mutedGO;
    public bool isMuted = false;
    public GameObject ActiveEffect = null;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        LocalizationService.LanguageChanged += RefreshLocalizedText;
    }

    private void OnDisable()
    {
        LocalizationService.LanguageChanged -= RefreshLocalizedText;
    }
    public void SetMuted(bool mute)
    {
        isMuted = mute;
        mutedGO.SetActive(isMuted);
        RefreshActiveEffect();
    }
    public void Init(ArtifactData aData)
    {
        ArtifactData = aData;

        NameLabel.text = LocalizedContent.ArtifactName(aData);
        NameLabel.color = UIManager.Instance.GetTextColor(aData.rarity);
        RefreshCounter();
    }
    public void RefreshCounter()
    {
        RefreshActiveEffect();

        if (CounterLabel == null)
            return;

        GameObject counterObject = CounterLabel.transform.parent != null ? CounterLabel.transform.parent.gameObject : CounterLabel.gameObject;
        counterObject.SetActive(false);

        if (ArtifactData == null)
            return;

        bool showCounter = true;
        int counter = 0;
        string counterText = null;

        switch (ArtifactData.effect)
        {
            case ArtifactEffectType.DamagePerGold:
                counter = GameData.CurrentGold * ArtifactData.value;
                break;
            case ArtifactEffectType.CritPerPotionUsed:
                counter = GameData.PotionsUsed;
                break;
            case ArtifactEffectType.CritPerSkippedLevel:
                counter = GameData.SkippedLevels;
                break;
            case ArtifactEffectType.CritPerUpgradedUnit:
                counter = GameData.UpgradedUnits;
                break;
            case ArtifactEffectType.ProcHPinDamage:
                if (GameManager.Instance.TheHero == null)
                    return;

                counter = Mathf.RoundToInt((ArtifactData.value / 100f) * GameManager.Instance.TheHero.MaxHealth);
                break;
            case ArtifactEffectType.UndeadPermanentAttackPerDestroyedUnit:
                counter = ArtifactManager.Instance.GetBoneCollectorDestroyedUnits(ArtifactData);
                break;
            case ArtifactEffectType.CritMultiplierPerUndeadRerolled:
                counterText = ArtifactManager.Instance.GetGravekeeperCritMultiplier(ArtifactData)
                    .ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "";
                break;
            default:
                showCounter = false;
                break;
        }

        if (showCounter == false)
            return;

        CounterLabel.text = counterText ?? counter.ToString();
        counterObject.SetActive(true);
    }

    private void RefreshActiveEffect()
    {
        if (ActiveEffect == null)
            return;

        bool isActive = false;

        if (ArtifactData != null &&
            ArtifactData.effect == ArtifactEffectType.DoubleOrcAttackBelowHalfHealth &&
            ArtifactData.value > 1 &&
            isMuted == false &&
            ArtifactManager.Instance.ActiveArtifacts.Contains(ArtifactData) &&
            ArtifactManager.Instance.IsArtifactMutedByBoss(ArtifactData) == false)
        {
            Hero hero = GameManager.Instance.TheHero;
            isActive = hero != null && hero.MaxHealth > 0 && hero.Health < hero.MaxHealth * 0.5f;
        }

        ActiveEffect.SetActive(isActive);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if(isMuted)
        return;
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
        if(isSelected)
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
                    () => GetArtifactName(),
                    () => LocalizedContent.ArtifactDescription(ArtifactData) + ArtifactData.GetRarityText(),
                    () => "",
                    transform);
            }
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
 if(isMuted)
        return;
        {
            isDragging = false;
 

            rectTransform.anchoredPosition = originalAnchoredPos;

        }




    }
    public void OnPointerDown(PointerEventData eventData)
    {
        VibrationsManager.TryVibrate(VibrationType.Tap);
        SoundManager.TryPlay(SoundType.Tap);
        isHolding = true;
        holdTimer = 0f;
    }
    public string GetArtifactName()
    {
        return LocalizedContent.ArtifactName(ArtifactData);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        holdTimer = 0f;
   

        if(isDragging == false)
        {
            if (UIManager.Instance.currentTransform == transform)
            {
                UIManager.Instance.HideCardInfoPopup();
            }
            else
            {
                UIManager.Instance.ShowCardInfoPopup(
                     () => GetArtifactName(),
                     () => LocalizedContent.ArtifactDescription(ArtifactData) + ArtifactData.GetRarityText(),
                     () => "",
                     transform);
            }
        }
        else
        {
            UIManager.Instance.HideCardInfoPopup();
            UIManager.Instance.UpdateArtifactSlotsUI();


            if (IsOverSellSlot())
            {
                ArtifactManager.Instance.SellArtifact(this); // Add logic here

                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.artifact_sold", "Artifact sold!"));

            }
            else
            {

            }

            UIManager.Instance.SellItemArea.gameObject.SetActive(false);
        }



    }

    private void RefreshLocalizedText()
    {
        if (ArtifactData != null && NameLabel != null)
            NameLabel.text = LocalizedContent.ArtifactName(ArtifactData);
    }


    public GameObject DmgNumber = null;
    bool isCriticalBonus = false;
    private int damageNumberAmount = 0;
    public void AddDamage(int damageAmount, System.Action onComplete, bool isCrit = false)
    {
        if (damageAmount == 0)
        {
            onComplete?.Invoke();
            return;
        }
        SoundManager.TryPlay(SoundType.ArtifactTrigger);

        // Pulse
        LeanTween.scale(gameObject, Vector3.one * 1.7f, 0.6f)
        .setEasePunch();
        isCriticalBonus = isCrit;
        int totalD = damageAmount;
        // 3. Create damage number above the card
        if (DmgNumber == null)
        {
            DmgNumber = Instantiate(UIManager.Instance.DamageFloatPrefab, transform.position, Quaternion.identity, transform);
            DmgNumber.GetComponent<TMPro.TMP_Text>().text = "0";
            damageNumberAmount = 0;
        }
        else
        {
            DmgNumber.transform.position = transform.position;
        }

        RectTransform dmgRT = DmgNumber.GetComponent<RectTransform>();
        TMPro.TMP_Text dmgText = DmgNumber.GetComponent<TMPro.TMP_Text>();



        totalD = damageNumberAmount + damageAmount;
        damageNumberAmount = totalD;

        if (isCrit)
        {
            dmgText.text = LocalizationService.Format("ui.damage.critical", "+{0} Critical", totalD);
            DmgNumber.GetComponent<TMPro.TMP_Text>().fontSize = 50;
            dmgRT.anchoredPosition -= new Vector2(0, 165f+120f);

        }
        else
        {
            dmgRT.anchoredPosition -= new Vector2(0, 165f+120f);
            dmgText.text = "+" + totalD;
        }




        // 4. Animate punch scale in
        dmgRT.localScale = Vector3.zero;
        LeanTween.scale(DmgNumber, Vector3.one * 1.3f, 0.3f).setEaseOutBack();

        LeanTween.delayedCall(DmgNumber, 1.0f, () =>
        {
            onComplete?.Invoke();
        });
    }
    public void Shake()
    {
        // Pulse
        LeanTween.scale(gameObject, Vector3.one * 1.7f, 0.6f)
        .setEasePunch();
  
    }

    private float criticalMultiplierAmount = 1f;

    public void AddCriticalMultiplier(float multiplier, System.Action onComplete)
    {
        if (multiplier <= 1f)
        {
            onComplete?.Invoke();
            return;
        }

        SoundManager.TryPlay(SoundType.ArtifactTrigger);
        LeanTween.scale(gameObject, Vector3.one * 1.7f, 0.6f).setEasePunch();
        criticalMultiplierAmount = multiplier;

        DmgNumber = Instantiate(UIManager.Instance.DamageFloatPrefab, transform.position, Quaternion.identity, transform);
        RectTransform dmgRT = DmgNumber.GetComponent<RectTransform>();
        TMPro.TMP_Text dmgText = DmgNumber.GetComponent<TMPro.TMP_Text>();
        dmgText.text = multiplier.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "X";
        dmgText.fontSize = 50;
        dmgRT.anchoredPosition -= new Vector2(0, 165f + 120f);
        dmgRT.localScale = Vector3.zero;
        LeanTween.scale(DmgNumber, Vector3.one * 1.3f, 0.3f).setEaseOutBack();
        LeanTween.delayedCall(DmgNumber, 1.0f, () => onComplete?.Invoke());
    }

    public void MultiplyCritical(System.Action onComplete)
    {
        if (DmgNumber == null)
        {
            onComplete?.Invoke();
            return;
        }

        LeanTween.delayedCall(DmgNumber, 0.75f, () =>
        {
            LeanTween.move(DmgNumber, UIManager.Instance.CriticalLabel.transform.position, 0.25f)
                .setEaseInCubic()
                .setOnComplete(() =>
                {
                    UIManager.Instance.MultiplyCritical(criticalMultiplierAmount);
                    Destroy(DmgNumber);
                    DmgNumber = null;
                    criticalMultiplierAmount = 1f;
                    onComplete?.Invoke();
                });
        });
    }



    public void AddToTotalDamage(System.Action onComplete)
    {
        if (DmgNumber == null)
        {
            onComplete?.Invoke();
            return;
        }

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
                    int amount = damageNumberAmount;
                    Destroy(DmgNumber);
                    DmgNumber = null;
                    damageNumberAmount = 0;

                    // After animation add to the total
                    if (isCriticalBonus)
                        UIManager.Instance.AddCritical(amount);
                    else
                        UIManager.Instance.AddDamage(amount);

                    onComplete?.Invoke();
                });
        });
    }
    public void DestroyDamageNumber()
    {
        Destroy(DmgNumber);
        DmgNumber = null;
        damageNumberAmount = 0;
        criticalMultiplierAmount = 1f;
    }
}
