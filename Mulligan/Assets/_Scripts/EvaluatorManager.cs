using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvaluatorManager  : Singleton<EvaluatorManager>
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void LastCardEvaluatedDoDamgge()
    {
        UnityHelper.RunAfterDelay(this, 0.75f, () =>
        {
            string totalDamage = UIManager.Instance.DamageLabel.GetComponent<TMPro.TMP_Text>().text;
            string totalCritical = UIManager.Instance.CriticalLabel.GetComponent<TMPro.TMP_Text>().text;
            UIManager.Instance.DamageLabel.GetComponent<TMPro.TMP_Text>().text = "0";
            UIManager.Instance.CriticalLabel.GetComponent<TMPro.TMP_Text>().text = "0";
            int crit = int.Parse(totalCritical);
            if (crit == 0) crit = 1;
            GameManager.Instance.TheHero.Attack(int.Parse(totalDamage) * crit);
            UnityHelper.RunAfterDelay(this, 0.5f, () =>
            {
                HandManager.Instance.DiscardHand();
                UIManager.Instance.ClearSynergies();
            });
        });
    }
    public void PlayBoostedCardsSequentially(List<CardInstance> boostedCards, int index = 0)
    {
        if (index >= boostedCards.Count)
        {
            LastCardEvaluatedDoDamgge();
            return;
        }

        // Pre Evaluation
        if (index == -1)
        {
            EvaluatePre(() =>  PlayBoostedCardsSequentially(boostedCards, index + 1));
            return;
        }

        var card = boostedCards[index];
        EvaluateCard(card, () =>
        {
            PlayBoostedCardsSequentially(boostedCards, index + 1);
        });

    }
    public void EvaluatePre(System.Action onComplete)
    {
        Queue<System.Action<System.Action>> steps = new();

        steps.Enqueue(next => {
            int synergyCritBonus = GetGlobalCritMultiplier();
            UIManager.Instance.AddCritical(synergyCritBonus);
            LeanTween.delayedCall(gameObject, 1.0f, () =>
            {
                onComplete?.Invoke();
            });

        });

        // Step 6: Done
        steps.Enqueue(_ => onComplete.Invoke());

        RunNextStep(steps);
    }
    public void EvaluateCard(CardInstance aCard, System.Action onComplete)
    {
        Queue<System.Action<System.Action>> steps = new();

        // Step 1: Base Damage
        steps.Enqueue(next => aCard.CardGO.AddDamage(aCard.GetDamage(), next));

        // Step 2: Card Bonuses (currently 0)
        steps.Enqueue(next => aCard.CardGO.AddDamage(0, next));

        // Step 3: Synergy Damage Bonuses
        steps.Enqueue(next => {
            int synergyBonus = GetSynergyDamage(aCard);
            aCard.CardGO.AddDamage(synergyBonus, next);
        });

        // Step 4: Artifact Bonuses (currently 0)
        steps.Enqueue(next => aCard.CardGO.AddDamage(0, next));

        // Step 5: Total Damage
        steps.Enqueue(next => aCard.CardGO.AddToTotalDamage(next));

        // Step 6: Done
        steps.Enqueue(_ => onComplete.Invoke());

        RunNextStep(steps);

    }
    public List<CardInstance> EvaluateHand(List<CardInstance> playedCards, out int totalDamage)
    {
        Dictionary<CardRace, int> raceCounts = new Dictionary<CardRace, int>();
        Dictionary<CardClass, int> classCounts = new Dictionary<CardClass, int>();
        List<CardInstance> boosted = new List<CardInstance>();

        totalDamage = 0;

        foreach (var card in playedCards)
        {
            var data = card.data;

            if (!raceCounts.ContainsKey(data.race)) raceCounts[data.race] = 0;
            raceCounts[data.race]++;

            if (!classCounts.ContainsKey(data.cardClass)) classCounts[data.cardClass] = 0;
            classCounts[data.cardClass]++;
        }

        foreach (var card in playedCards)
        {
            var data = card.data;

            bool isBoosted = raceCounts[data.race] >= 2 || classCounts[data.cardClass] >= 2;

            if (isBoosted)
            {
                boosted.Add(card);
                totalDamage += card.GetDamage(); // include ranks, bonuses, etc.
            }
        }

        return boosted;
    }
    public int GetSynergyDamage(CardInstance card)
    {
        // Count synergies in current PlayedHand
        int raceCount = 0;
        int classCount = 0;

        foreach (var c in HandManager.Instance.PlayedHand)
        {
            if (c.data.race == card.data.race) raceCount++;
            if (c.data.cardClass == card.data.cardClass) classCount++;
        }

        int bonus = 0;

        if (raceCount == 2 || raceCount == 3 )
        {
            bonus += card.GetDamage(); // Double race damage
            UIManager.Instance.PulseSynergyItem(card.data.race.ToString());
        }

        if (classCount == 2 || classCount == 3)
        {
            bonus += card.GetDamage(); // Double class damage
            UIManager.Instance.PulseSynergyItem(card.data.cardClass.ToString());
        }

        return bonus;
    }

    public int GetGlobalCritMultiplier()
    {
        Dictionary<CardRace, int> raceCounts = new();
        Dictionary<CardClass, int> classCounts = new();

        foreach (var card in HandManager.Instance.PlayedHand)
        {
            if (!raceCounts.ContainsKey(card.data.race))
                raceCounts[card.data.race] = 0;
            raceCounts[card.data.race]++;

            if (!classCounts.ContainsKey(card.data.cardClass))
                classCounts[card.data.cardClass] = 0;
            classCounts[card.data.cardClass]++;
        }

        int critTriggered = 0;

        foreach (var kvp in raceCounts)
        {
            if (kvp.Value >= 4)
            {
                critTriggered++;
                UIManager.Instance.PulseSynergyItem(kvp.Key.ToString()); // ✅ Use actual race name
            }
        }

        foreach (var kvp in classCounts)
        {
            if (kvp.Value >= 4)
            {
                critTriggered++;
                UIManager.Instance.PulseSynergyItem(kvp.Key.ToString()); // ✅ Use actual class name
            }
        }

        return critTriggered * 2;
    }

    private void RunNextStep(Queue<System.Action<System.Action>> steps)
    {
        if (steps.Count == 0) return;

        var step = steps.Dequeue();
        step(() => RunNextStep(steps));
    }

}
