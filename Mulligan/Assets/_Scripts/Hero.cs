using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hero : MonoBehaviour
{
    public float Health = 100;
    public int Experience = 0;
    public int Level = 0;
    public float MaxHealth
    {
        get
        {
            float v = 1;
            foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
            {
                if (ArtifactManager.Instance.IsArtifactMutedByBoss(artifact))
                    continue;

                if (artifact.effect == ArtifactEffectType.AddMaxHP)
                {
                    v += artifact.value / 100f;
                }
            }
            return MaxHealthV * v;
        }
        set
        {
            MaxHealthV = value;
        }
    }
    public float MaxHealthV = 100;
    public float CurrentLifeStealProc = 0;
    public TMPro.TMP_Text healthLabel;
    public Image image;
    private Color originalColor;
    public Image bar;
    public List<GameObject> HeroPortraits;
    public HeroData myHeroData;
    public GameObject HealEffect;
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
        SetStartingItems();
        // if (aData.startingItem == StartingItemType.RandomArtifact)
        // {
        //     ArtifactManager.Instance.AddRandomArtifact();
        // }
        // else if (aData.startingItem == StartingItemType.RandomPotion)
        // {
        //     if (TutorialController.Instance.HasRunTutorial() == true)
        //         PotionManager.Instance.AddRandomPotion();
        // }

        GameData.CurrentAttacks += GetAttackModifier();
        GameData.CurrentReRolls += GetRollsModifier();

        // image.sprite =  Resources.Load<Sprite>("" + aData.portrait);
        // image.sprite = HeroPortraits[GameData.HeroSelected];
        foreach (var c in HeroPortraits)
            c.SetActive(false);
        HeroPortraits[GameData.HeroSelected].SetActive(true);
        Experience = 0;
        Level = 1;
        RefreshBar();
        for (int i = 0; i < UIManager.Instance.PotionBackground.Count; i++)
        {
            if (i < aData.PotionSlots)
            {
                UIManager.Instance.PotionBackground[i].SetActive(true);
                UIManager.Instance.PotionBackground[i].transform.GetChild(0).gameObject.SetActive(false);

            }
            else
                UIManager.Instance.PotionBackground[i].SetActive(false);
        }
        for (int i = 0; i < UIManager.Instance.ArtifactBackground.Count; i++)
        {
            if (i < aData.ArtifactSlots)
            {
                UIManager.Instance.ArtifactBackground[i].SetActive(true);
                UIManager.Instance.ArtifactBackground[i].transform.GetChild(0).gameObject.SetActive(false);
            }
            else
                UIManager.Instance.ArtifactBackground[i].SetActive(false);
        }

    }
    public void SetStartingItems()
    {
        if (TutorialController.Instance.HasRunTutorial() == true)
        {
            if( myHeroData.heroName == "WARRIOR")
            {
                
            }else  if( myHeroData.heroName == "Dwarf")
            {
                RuneManager.Instance.AddRune(RuneType.RuneOfRareChance);
                ArtifactManager.Instance.AddArtifact(ArtifactEffectType.GainGoldAfterLevel);
  
            }else  if( myHeroData.heroName == "Warlock")
            {
                RuneManager.Instance.AddRune(RuneType.RuneOfAttack);
                ArtifactManager.Instance.AddArtifact(ArtifactEffectType.RankRandomUnit);

 
            }else  if( myHeroData.heroName == "Goblin")
            {
                PotionManager.Instance.AddRandomPotion();
                PotionManager.Instance.AddRandomPotion();
            }
        }
    }
    public int GetAttackModifier()
    {
        int modifier = 0;
        if (myHeroData.startingTrait == HeroTrait.BonusAttack)
        {
            modifier = 0;
        }
        modifier += GameManager.Instance.BonusAttacks;
        return modifier;
    }
    public int GetRollsModifier()
    {
        int modifier = 0;
        if (myHeroData.startingTrait == HeroTrait.BonusReroll)
        {
            modifier = 1;
        }
        modifier += GameManager.Instance.BonusRerolls;

        return modifier;
    }
    public void RefreshBar()
    {
        if (Health > MaxHealth)
            Health = MaxHealth;

        bar.fillAmount = Health / MaxHealth;
        healthLabel.text = Health.ToString();
        UIManager.Instance.RefreshArtifactCounters();
    }
    public bool DodgeCheck()
    {
        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            if (ArtifactManager.Instance.IsArtifactMutedByBoss(artifact))
                continue;

            if (artifact.effect == ArtifactEffectType.DodgeEnemyAttack)
            {
                if (Random.Range(0, 100) < artifact.value)
                {
                    UIManager.Instance.ShowTooltip($"Dodged Attack!");
                    SoundManager.TryPlay(SoundType.Dodge);
                    return true;
                }
            }

        }
        return false;
    }
    public void DoDamage(int aDamage)
    {
        
        if (DodgeCheck())
        {
            return;
        }
        // if (TutorialController.Instance.HasRunTutorial() == false)
        // {
        //     if(Health-aDamage <=1)
        //         aDamage = 1;
        // }
        Health -= aDamage;
        VibrationsManager.TryVibrate(VibrationType.PlayerDamage);
        SoundManager.TryPlay(SoundType.PlayerDamage);
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
        float beforeHealth = Health;
        Health += healAmount; // assuming you have a Heal(int) method
        if (Health > MaxHealth)
            Health = MaxHealth;
        DailyQuestManager.Instance.AddProgress(DailyQuestType.Heal, Mathf.RoundToInt(Health - beforeHealth));
        RefreshBar();
    }
    public void HealHPPoints(float aValue)
    {
        float beforeHealth = Health;
        Health += aValue; // assuming you have a Heal(int) method
        if (Health > MaxHealth)
            Health = MaxHealth;
        DailyQuestManager.Instance.AddProgress(DailyQuestType.Heal, Mathf.RoundToInt(Health - beforeHealth));
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
        UnityHelper.RunAfterDelay(this, 1.25f, () =>
                  {
                      if (TutorialController.Instance.myCurrentAction == TutorialController.TutorialActionsEnum.CLICK_ATTACK)
                      {
                          TutorialController.Instance.ShowStepById("Step1_Enemy");
                          Time.timeScale = 0;
                      }
                  });


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
            VibrationsManager.TryVibrate(VibrationType.PlayerDamage);
            GameManager.Instance.TheEnemy.DoDamage(aDamage);
            Health += Mathf.RoundToInt( aDamage * (CurrentLifeStealProc/100f));
            RefreshBar();
            CurrentLifeStealProc = 0;
            // GameData.PotionsUsed = 0;

            // UnityHelper.RunAfterDelay(this, 0.0f, () =>
            // {

            // });
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
