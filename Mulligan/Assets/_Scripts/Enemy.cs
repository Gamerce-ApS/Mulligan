using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public int Health = 100;
    public float MaxHealth = 100;

    public TMPro.TMP_Text healthLabel;
    public TMPro.TMP_Text dmgLabel;
    public Image image;
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


        // image = GetComponent<Image>();
        originalColor = image.color;

    }
    public void Init(int aRound)
    {
        ActiveAbbilities.Clear();
        UIManager.Instance.MutePotions(false);
        UIManager.Instance.MuteArtifacts(false);
        
        GetComponent<CanvasGroup>().alpha = 0;
        if (aRound % 4 == 0)
        {
            BossData d = CardContainer.Instance.myBossList[0];
            if(TutorialController.Instance.HasRunTutorial() == false)
            {
                d = TutorialController.Instance.GetCurrentBoss();
            }
            float baseDamage = d.baseDamage;
            float baseHp = d.baseHP;
            if (TutorialController.Instance.HasRunTutorial() == false)
            {
                baseHp = GetTutorialBossBaseHPForTarget(105, aRound);
                baseDamage = GetTutorialBossBaseDamageForTarget(18, aRound);
            }

            SetupEnemyForLevel(baseHp , baseDamage, aRound);
            //image.sprite = d.theSprite;
            image.sprite = Resources.Load<Sprite>("" +d.sprite_theSprite);

            ActiveAbbilities.AddRange(d.abilities);

            if(ActiveAbbilities.Contains(BossAbilityEnum.Disable2RandomRace))
            {
                List<BossAbilityEnum> rList = new List<BossAbilityEnum>();
                rList.Add(BossAbilityEnum.DisableHumanUnits);
                rList.Add(BossAbilityEnum.DisableElfUnits);
                rList.Add(BossAbilityEnum.DisableDwarfesUnits);
                rList.Add(BossAbilityEnum.DisableOrcUnits);
                rList.Add(BossAbilityEnum.DisableTrollUnits);
                rList.Add(BossAbilityEnum.DisableUndeadUnits);
                rList.Shuffle();
                ActiveAbbilities.Add(rList[0]);
                ActiveAbbilities.Add(rList[1]);
                
            }
             if(ActiveAbbilities.Contains(BossAbilityEnum.Disable2RandomClass))
            {
                    List<BossAbilityEnum> rList = new List<BossAbilityEnum>();
                rList.Add(BossAbilityEnum.DisablePeassantUnits);
                rList.Add(BossAbilityEnum.DisableBardUnits);
                rList.Add(BossAbilityEnum.DisableClericUnits);
                rList.Add(BossAbilityEnum.DisableWarriorUnits);
                rList.Add(BossAbilityEnum.DisableMageUnits);
                rList.Add(BossAbilityEnum.DisableArcherUnits);
                rList.Add(BossAbilityEnum.DisableWarlockUnits);
                rList.Shuffle();
                ActiveAbbilities.Add(rList[0]);
                ActiveAbbilities.Add(rList[1]);
            }
            
            // UIManager.Instance.ShowBossIntroScreen(d,()=> { PlayEnterAnimation(); });
            PlayEnterAnimation();

            if(TutorialController.Instance.LastStepPlayed=="Step4_Shop3")
            {
                TutorialController.Instance.ShowStepById("Step5_boss1");
            }
        }
        else
        {
            int loopedIndex = (GameData.CurrentRound-1) % 4;
            EnemyData d = CardContainer.Instance.myEnemiesList[loopedIndex];
            if(TutorialController.Instance.HasRunTutorial() == false)
            {
                d = TutorialController.Instance.myEnemiesList[loopedIndex];
            }

            SetupEnemyForLevel(d.baseHP, d.baseDamage, aRound);
            //image.sprite = d.theSprite;
            image.sprite = Resources.Load<Sprite>("" + d.sprite_theSprite);

            PlayEnterAnimation();
            CanvasGroup cg = GetComponent<CanvasGroup>();
            cg.alpha = 1;
        }

        gameObject.SetActive(true); // or Destroy(gameObject)

        //Health = aHealth;
        //MaxHealth = aHealth;

        //  ActiveAbbilities.Add(BossAbilityEnum.Evasion);

        HandManager.Instance.HandleMutedCards();
    }
    // Example function to calculate scaled stats for a given enemy/boss at a certain level.
 public void SetupEnemyForLevel(float baseHp, float baseDmg, int level)
{
    float linearGrowth = CardContainer.Instance.GrowthRate;
    float expGrowth = CardContainer.Instance.GrowthRateEXP;
    float linearGrowthDMG = CardContainer.Instance.GrowthRateDMG;
    float expGrowthDMG = CardContainer.Instance.GrowthRateDMGEXP;

    float linearMultiplier = 1f + (level - 1) * linearGrowth;
    float expMultiplier = Mathf.Pow(1f + expGrowth, level - 1);

    float linearMultiplierDMG = 1f + (level - 1) * linearGrowthDMG;
    float expMultiplierDMG = Mathf.Pow(1f + expGrowthDMG, level - 1);

    float hpMultiplier = linearMultiplier * expMultiplier;
    float dmgMultiplier = linearMultiplierDMG*expMultiplierDMG;

    int scaledHP = Mathf.RoundToInt(CardContainer.Instance.EnemyBaseHealth * baseHp * hpMultiplier);
    int scaledDamage = Mathf.RoundToInt(CardContainer.Instance.EnemyBaseDamage * baseDmg * dmgMultiplier);

    Health = scaledHP;
    MaxHealth = Health;
    Damage = scaledDamage;

    healthLabel.text = Health.ToString();
    dmgLabel.text = Damage.ToString();
    bar.fillAmount = (float)Health / MaxHealth;
}

    private float GetTutorialBossBaseDamageForTarget(int targetDamage, int level)
    {
        float linearGrowthDMG = CardContainer.Instance.GrowthRateDMG;
        float expGrowthDMG = CardContainer.Instance.GrowthRateDMGEXP;

        float linearMultiplierDMG = 1f + (level - 1) * linearGrowthDMG;
        float expMultiplierDMG = Mathf.Pow(1f + expGrowthDMG, level - 1);
        float dmgMultiplier = linearMultiplierDMG * expMultiplierDMG;
        float baseDamage = CardContainer.Instance.EnemyBaseDamage * dmgMultiplier;

        if (baseDamage <= 0)
            return 0;

        return targetDamage / baseDamage;
    }

    private float GetTutorialBossBaseHPForTarget(int targetHP, int level)
    {
        float linearGrowth = CardContainer.Instance.GrowthRate;
        float expGrowth = CardContainer.Instance.GrowthRateEXP;

        float linearMultiplier = 1f + (level - 1) * linearGrowth;
        float expMultiplier = Mathf.Pow(1f + expGrowth, level - 1);
        float hpMultiplier = linearMultiplier * expMultiplier;
        float baseHP = CardContainer.Instance.EnemyBaseHealth * hpMultiplier;

        if (baseHP <= 0)
            return 0;

        return targetHP / baseHP;
    }

    public void Attack(int aDamage=0)
    {
        SoundManager.TryPlay(SoundType.EnemyAttack);

        int dmg = (int)(Damage);

        if (aDamage > 0)
            dmg = aDamage;

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
            GameManager.Instance.TheHero.DoDamage(dmg);

            if (GameManager.Instance.TheHero.Health <= 0)
            {
                if(GameManager.Instance.ReviveFullHP )
                {
                    GameManager.Instance.ReviveFullHP = false;
                    GameManager.Instance.TheHero.Health = GameManager.Instance.TheHero.MaxHealth;
                }
                else if(GameManager.Instance.ReviveWith1HP)
                {
                    GameManager.Instance.ReviveWith1HP = false;
                    GameManager.Instance.TheHero.Health = 1;
                }
                else
                {
                    GameManager.Instance.LostGame();
                }
            }


            if (TutorialController.Instance.LastStepPlayed == "Step3_Artifact")
            {
                UnityHelper.RunAfterDelay(this, 0.75f, () =>
                {
                TutorialController.Instance.ShowStepById("Step3_Potion"); 
                 });
            }

             if (GameManager.Instance.TheEnemy.ActiveAbbilities.Contains(BossAbilityEnum.Steal10GoldAttacking))
            {
                GameData.CurrentGold -= 10;
                if(GameData.CurrentGold <0)
                GameData.CurrentGold =0;
                UIManager.Instance.ShowTooltip("Gold stolen!");
            }
        });

    }
    public void DoDamage(int aDamage)
    {
       if (GameManager.Instance.TheEnemy.ActiveAbbilities.Contains(BossAbilityEnum.Evasion))
            {
               
                    if (50 < Random.Range(0, 100))
                    {
                        UIManager.Instance.ShowTooltip($"Dodged Attack!");
                        SoundManager.TryPlay(SoundType.Dodge);
                        return;
                    }
                
            }

        Health -= aDamage;
        VibrationsManager.TryVibrate(VibrationType.EnemyDamage);
        SoundManager.TryPlay(SoundType.EnemyDamage);
        if(TutorialController.Instance.HasRunTutorial()== false)
        {
            if(TutorialController.Instance.LastStepPlayed=="Step3_Artifact")
            {
                if(Health<=0)
                    Health = 1;
            }
        }
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

            if(TutorialController.Instance.LastStepPlayed =="Step1_Synergies2")
            {
                LeanTween.delayedCall(gameObject, 0.75f, () =>
                {
                    TutorialController.Instance.ShowStepById("Step1_Gold");
                });
      
            }
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
        SoundManager.TryPlay(SoundType.EnemyDeath);

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
