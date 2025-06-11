using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hero : MonoBehaviour
{
    public float Health = 100;
    public float MaxHealth = 100;
    public float CurrentLifeStealProc = 0;
    // Start is called before the first frame update
    void Start()
    {
        
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
    // Update is called once per frame
    void Update()
    {
    
    }
}
