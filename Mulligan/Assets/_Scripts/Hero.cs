using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hero : MonoBehaviour
{
    public float Health = 100;
    public int Experience = 0;
    public int Level = 0;
    public float MaxHealth = 100;
    public float CurrentLifeStealProc = 0;
    public TMPro.TMP_Text healthLabel;
    public Image image;
    private Color originalColor;
    public Image bar;
    public List<GameObject> HeroPortraits;
    HeroData myHeroData;
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
        RefreshBar();
    }
    public void Init(HeroData aData)
    {
        myHeroData = aData;

        Health = aData.startingHP;
        healthLabel.text = Health.ToString();
        MaxHealth = Health;

        if( aData.startingItem == StartingItemType.RandomArtifact)
        {
            ArtifactManager.Instance.AddRandomArtifact();
        }
        else if (aData.startingItem == StartingItemType.RandomPotion)
        {
            PotionManager.Instance.AddRandomPotion();
        }

        GameData.CurrentAttacks += GetAttackModifier();
        GameData.CurrentReRolls += GetRollsModifier();
        
        // image.sprite =  Resources.Load<Sprite>("" + aData.portrait);
        // image.sprite = HeroPortraits[GameData.HeroSelected];
        foreach(var c in HeroPortraits)
            c.SetActive(false);
        HeroPortraits[GameData.HeroSelected].SetActive(true);
        Experience = 0;
        Level = 1;
        RefreshBar();
    }
    public int GetAttackModifier()
    {
        if (myHeroData.startingTrait == HeroTrait.BonusAttack)
        {
            return 1;
        }
        return 0;
    }
    public int GetRollsModifier()
    {
        int modifier = 0;
        if (myHeroData.startingTrait == HeroTrait.BonusReroll)
        {
            modifier = 1;
        }
        modifier+=GameManager.Instance.BonusRerolls;

        return modifier;
    }
    public void RefreshBar()
    {
        bar.fillAmount = Health / MaxHealth;
        healthLabel.text = Health.ToString();
    }
    public bool DodgeCheck()
    {
        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            if (artifact.effect == ArtifactEffectType.DodgeEnemyAttack)
            {
                if(artifact.value < Random.Range(0,100))
                {
                    UIManager.Instance.ShowTooltip($"Dodged Attack!");  
                    return true; 
                }
            }
        
        }
        return false;
    }
    public void DoDamage(int aDamage)
    {
        if(DodgeCheck())
        {
            return;
        }

        Health -= aDamage;
        bar.fillAmount = Health / MaxHealth;
        healthLabel.text = Health.ToString();

        RefreshBar();

        LeanTween.scale(healthLabel.gameObject, Vector3.one * 1.3f, 0.5f).setEasePunch();

        LeanTween.scale(gameObject, Vector3.one * 1.2f, 0.5f).setOnComplete(() =>
            {
                gameObject.transform.localScale = Vector3.one;
            }).setEasePunch();




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
        RefreshBar();
    }
    public void HealPercent(float percent)
    {
        int healAmount = Mathf.RoundToInt(MaxHealth * percent);
        Health += healAmount; // assuming you have a Heal(int) method
        RefreshBar();
    }
    public void ReduceMaxHPPercent(float percent)
    {
        MaxHealth = Mathf.Max(1, MaxHealth - Mathf.RoundToInt(MaxHealth * percent));
        Health = Mathf.Min(Health, MaxHealth);
        RefreshBar();
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
            LeanTween.scale(gameObject, Vector3.one * 1.3f, 0.15f).setOnComplete(() =>
            {
                gameObject.transform.localScale = Vector3.one;
            }).setEasePunch();
            GameManager.Instance.TheEnemy.DoDamage(aDamage);
            Health += aDamage * (CurrentLifeStealProc);
            RefreshBar();
            CurrentLifeStealProc = 0;
            GameData.PotionsUsed = 0;
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
