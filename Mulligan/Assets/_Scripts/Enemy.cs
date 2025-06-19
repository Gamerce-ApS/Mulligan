using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public int Health = 100;
    public float MaxHealth = 100;

    public TMPro.TMP_Text healthLabel;

    private Image image;
    private Color originalColor;
    public Image bar;
    public float Damage=0;

    private Vector2 originalAnchoredPosition;
    private Quaternion originalRotation;
    private bool initialized = false;

    public List<BossAbilityEnum> ActiveAbbilities = new List<BossAbilityEnum>();

    // Start is called before the first frame update
    void Start()
    {
        if (!initialized)
        {
            RectTransform rt = GetComponent<RectTransform>();
            originalAnchoredPosition = rt.anchoredPosition;
            originalRotation = rt.localRotation;
            initialized = true;
        }


        image = GetComponent<Image>();
        originalColor = image.color;

    }
    public void Init(int aRound)
    {
        ActiveAbbilities.Clear();

        GetComponent<CanvasGroup>().alpha = 0;
        if (aRound % 4 == 0)
        {
            BossData d = CardContainer.Instance.GetRandomBoss();
            SetupEnemyForLevel(d.baseDamage, d.baseHP, aRound);
            //image.sprite = d.theSprite;
            image.sprite = Resources.Load<Sprite>("" +d.sprite_theSprite);

            ActiveAbbilities.AddRange(d.abilities);
            
            UIManager.Instance.ShowBossIntroScreen(d,()=> { PlayEnterAnimation(); });
        }
        else
        {
            EnemyData d = CardContainer.Instance.GetRandomEnemy();
            SetupEnemyForLevel(d.baseDamage, d.baseHP, aRound);
            //image.sprite = d.theSprite;
            image.sprite = Resources.Load<Sprite>("" + d.sprite_theSprite);

            PlayEnterAnimation();
            CanvasGroup cg = GetComponent<CanvasGroup>();
            cg.alpha = 1;
        }

        gameObject.SetActive(true); // or Destroy(gameObject)

        //Health = aHealth;
        //MaxHealth = aHealth;

        

        HandManager.Instance.HandleMutedCards();
    }
    // Example function to calculate scaled stats for a given enemy/boss at a certain level.
    public void SetupEnemyForLevel(int baseHp, int baseDmg, int level)
    {
        // Example scaling: 10% increase in stats per level (adjust factor as needed)
        float growthRate = CardContainer.Instance.GrowthRate;  // 10% per level

        // Calculate multiplier based on level (level 1 => 1.0, level 2 => 1.1, level 3 => 1.2, etc.)
        float statMultiplier = 1f + (level - 1) * growthRate;

        int scaledHP = Mathf.RoundToInt(CardContainer.Instance.EnemyBaseHealth*baseHp * statMultiplier);
        int scaledDamage = Mathf.RoundToInt(baseDmg * statMultiplier);

        // Apply these values to the enemy instance (for example, to its health component)
        Health = scaledHP;
        MaxHealth = Health;
        Damage = scaledDamage;
        healthLabel.text = Health.ToString();

    }

    public void Attack(int aDamage=0)
    {
        float attackDuration = 0.4f;

        Vector3 originalPos = transform.position;
        Vector3 targetPos = GameManager.Instance.TheHero.transform.position;

        float overshoot = 17.5f; // how far past the enemy it flies (optional)

        // Optional: slight offset to go "through" the target
        Vector3 direction = (targetPos - originalPos).normalized;
        Vector3 attackTargetPos = targetPos - direction * overshoot;

        // 1. Fly to target
        LeanTween.move(gameObject, attackTargetPos, attackDuration)
            .setEaseOutCubic()
            .setOnComplete(() =>
            {


                    // 3. Return to start
                    LeanTween.move(gameObject, originalPos, attackDuration)
                                .setEaseInCubic();


            });

        UnityHelper.RunAfterDelay(this, 0.45f, () =>
        {
            // 2. Optional: impact punch
            LeanTween.scale(gameObject, Vector3.one * 1.3f, 0.15f)
                        .setEasePunch();
            GameManager.Instance.TheHero.DoDamage(aDamage);

        });

    }
    public void DoDamage(int aDamage)
    {
        Health -= aDamage;

        bar.fillAmount = Health / MaxHealth;
        healthLabel.text = Health.ToString();
        LeanTween.scale(healthLabel.gameObject, Vector3.one * 1.3f, 0.5f).setEasePunch();

        LeanTween.scale(gameObject, Vector3.one * 1.2f, 0.5f).setEasePunch();




        // Red flash
        Color flashColor = new Color(1f, 0f, 0f, 1f); // Pure red

      if (image != null)
        {
            image.color = flashColor;
            LeanTween.value(gameObject, flashColor, originalColor, 0.3f)
                .setOnUpdate((Color val) => image.color = val)
                .setEaseOutCubic();
        }


        ShowFloatingDamage(aDamage);
        // make text float in red showing how much damage was done instead of the float 

        if (Health < 0)
        {
            LeanTween.delayedCall(gameObject, 0.5f, () =>
            {
                PlayDeathAnimation(() => { });
            });
        }
    }
    private void ShowFloatingDamage(int damageAmount)
    {
        // Clone the health label
        TMPro.TMP_Text floatingLabel = Instantiate(healthLabel, healthLabel.transform.parent);
        floatingLabel.text = "-" + damageAmount.ToString();
        floatingLabel.color = new Color(1f, 0.2f, 0.2f, 1f); // Red with full alpha
        floatingLabel.transform.localPosition = healthLabel.transform.localPosition + new Vector3(-10, 150, 0); // start same position
        floatingLabel.transform.localScale = Vector3.one;

        // Animate upward + fade
        LeanTween.moveLocalY(floatingLabel.gameObject, floatingLabel.transform.localPosition.y + 120f, 1.2f).setEaseOutCubic();
        LeanTween.value(floatingLabel.gameObject, 1f, 0f, 1.2f)
            .setOnUpdate((float val) =>
            {
                Color c = floatingLabel.color;
                c.a = val;
                floatingLabel.color = c;
            })
            .setOnComplete(() =>
            {
                Destroy(floatingLabel.gameObject);
            });

        // Optional pop-in scale
        floatingLabel.transform.localScale = Vector3.zero;
        LeanTween.scale(floatingLabel.gameObject, Vector3.one, 0.2f).setEaseOutBack();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    public void PlayDeathAnimation(System.Action onComplete = null)
    {
        RectTransform rt = GetComponent<RectTransform>();
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();

        // 1. Rotate to lay flat (like it toppled over)
        LeanTween.rotateZ(gameObject, -90f, 0.3f).setEaseInBack();

        // 2. After short delay, fly out sideways and fade out
        LeanTween.delayedCall(gameObject, 0.35f, () =>
        {
            // Move to right (or left: use negative X)
            Vector3 target = rt.anchoredPosition + new Vector2(1000f, 0f);
            LeanTween.move(rt, target, 0.6f).setEaseInBack();

            LeanTween.alphaCanvas(cg, 0f, 0.5f).setOnComplete(() =>
            {
                gameObject.SetActive(false); // or Destroy(gameObject)
                onComplete?.Invoke();
            });
        });
    }

    public void PlayEnterAnimation()
    {
        RectTransform rt = GetComponent<RectTransform>();
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        // Reset position and rotation
        rt.anchoredPosition = originalAnchoredPosition + new Vector2(800f, 0f); // enter from right
        rt.localRotation = Quaternion.identity;
        cg.alpha = 0f;

        gameObject.SetActive(true);

        // Animate
        LeanTween.alphaCanvas(cg, 1f, 0.3f);
        LeanTween.move(rt, originalAnchoredPosition, 0.5f).setEaseOutBack();
    }


}
