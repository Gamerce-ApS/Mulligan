using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hero : MonoBehaviour
{
    public float Health = 100;
    public float MaxHealth = 100;
    public float CurrentLifeStealProc = 0;
    public TMPro.TMP_Text healthLabel;
    private Image image;
    private Color originalColor;
    public Image bar;

    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
        originalColor = image.color;
    }
    public void Init(int aHealth)
    {
        Health = aHealth;
        healthLabel.text = Health.ToString();
        MaxHealth = Health;
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

    }

    public void AddMaxHPPercent(float aValue)
    {
        MaxHealth *= aValue;
    }
    public void HealPercent(float percent)
    {
        int healAmount = Mathf.RoundToInt(MaxHealth * percent);
        Health += healAmount; // assuming you have a Heal(int) method
    }
    public void ReduceMaxHPPercent(float percent)
    {
        MaxHealth = Mathf.Max(1, MaxHealth - Mathf.RoundToInt(MaxHealth * percent));
        Health = Mathf.Min(Health, MaxHealth);
    }
    public void Attack(int aDamage)
    {
        float attackDuration = 0.4f;

            Vector3 originalPos = transform.position;
            Vector3 targetPos = GameManager.Instance.TheEnemy.transform.position;

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
            GameManager.Instance.TheEnemy.DoDamage(aDamage);
            Health += aDamage * (CurrentLifeStealProc);
            CurrentLifeStealProc = 0;
        });

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
}
