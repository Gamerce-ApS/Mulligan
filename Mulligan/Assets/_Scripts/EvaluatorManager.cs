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
            EvaluateAttackPost(() => LastCardEvaluatedDoDamgge());
            return;

        }

        // Pre Evaluation
        if (index == -1)
        {
            EvaluateAttackPre(() =>  PlayBoostedCardsSequentially(boostedCards, index + 1));
            return;
        }

        var card = boostedCards[index];
        EvaluateCard(card, () =>
        {
            PlayBoostedCardsSequentially(boostedCards, index + 1);
        });

    }
    public void EvaluateAttackPost(System.Action onComplete)
    {
        Queue<System.Action<System.Action>> steps = new();

        // Step 2: Apply artifacts
        steps.Enqueue(next =>
        {

            // 🔁 Retrigger attacking units
            if (ArtifactManager.Instance.HasArtifact(ArtifactEffectType.RetriggerAttacks))
            {
                Debug.Log("Retriggering attacking units from artifact.");
                //foreach (var card in HandManager.Instance.PlayedHand)
                //{
                //    // Re-evaluate each attacking card again
                //    EvaluateCard(card, () =>
                //    {
                //        Debug.Log("Retriggered attack completed.");
                //    });
                //}

            }else
            {
            }

            LeanTween.delayedCall(gameObject, 0.5f, next); // ✅ continue the sequence


        });

        steps.Enqueue(next =>
        {
            EvaluateArtifactsPost(next);
        });

        // Step 3: Done
        steps.Enqueue(_ => onComplete?.Invoke());

        RunNextStep(steps);
    }
    public void EvaluateAttackPre(System.Action onComplete)
    {
        Queue<System.Action<System.Action>> steps = new();

        // Step 1: Apply synergy crit
        steps.Enqueue(next =>
        {
            int synergyCritBonus = GetGlobalCritMultiplier(HandManager.Instance.PlayedHand);
            UIManager.Instance.AddCritical(synergyCritBonus);
            LeanTween.delayedCall(gameObject, 1.0f, next); // ✅ continue the sequence
        });

        // Step 2: Apply artifacts
        steps.Enqueue(next =>
        {
            foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
            {
                if (artifact.effect == ArtifactEffectType.AddMaxHP)
                {
                    GameManager.Instance.TheHero.AddMaxHPPercent(artifact.value);
                }
            }
            next(); // ✅ properly proceed to the next step
        });

        // Step 3: Done
        steps.Enqueue(_ => onComplete?.Invoke());

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
            int synergyBonus = GetSynergyDamage(aCard,HandManager.Instance.PlayedHand);
            aCard.CardGO.AddDamage(synergyBonus, next);
        });

        // Step 4: Artifact Bonuses (currently 0)
        steps.Enqueue(next => EvaluateArtifactsForCard(aCard, next));

        // Step 5: Total Damage
        steps.Enqueue(next => aCard.CardGO.AddToTotalDamage(next));

        // Step 6: Done
        steps.Enqueue(_ => onComplete.Invoke());

        RunNextStep(steps);

    }
    public void FinisLevel()
    {
        // ❤️ Heal 10% HP After Level
        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            if (artifact.effect == ArtifactEffectType.HealAfterLevel)
            {
                float healPercent = artifact.value / 100f;
                GameManager.Instance.TheHero.HealPercent(healPercent);
                UIManager.Instance.ShowTooltip($"Healed {artifact.value}% HP from artifact");
            }
        }
    }
    public void StartLevel()
    {
        // Called when level Start
            GameData.CurrentArmySize = 0;
            foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
            {
                switch (artifact.effect)
                {
                    case ArtifactEffectType.AddReroll:
                        GameData.CurrentReRolls += artifact.value;
                        UIManager.Instance.ShowTooltip($"+{artifact.value} Reroll");
                        break;

                    case ArtifactEffectType.AddArmySize:
                        GameData.CurrentArmySize += artifact.value;
                        UIManager.Instance.ShowTooltip($"+{artifact.value} Army Size");
                        break;

                    case ArtifactEffectType.AttackPerLevel:
                        GameData.CurrentAttacks += artifact.value;
                        UIManager.Instance.ShowTooltip($"+{artifact.value} Attack");
                        break;

                    case ArtifactEffectType.RankRandomUnit:
                        HandManager.Instance.RankUpRandom();
                        break;
                }
            }
       


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
    public int GetSynergyDamage(CardInstance card, List<CardInstance> aHand)
    {
        // Count synergies in current PlayedHand
        int raceCount = 0;
        int classCount = 0;

        foreach (var c in aHand)
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

    public int GetGlobalCritMultiplier(List<CardInstance> aHand)
    {
        Dictionary<CardRace, int> raceCounts = new();
        Dictionary<CardClass, int> classCounts = new();

        foreach (var card in aHand)
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

        return critTriggered * 3;
    }

    private void RunNextStep(Queue<System.Action<System.Action>> steps)
    {
        if (steps.Count == 0) return;

        var step = steps.Dequeue();
        step(() => RunNextStep(steps));
    }

    public void EvaluateArtifactsForCard(CardInstance card, System.Action onComplete)
    {
        Queue<System.Action<System.Action>> steps = new();

        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            steps.Enqueue(next =>
            {
                switch (artifact.effect)
                {
                    // No artifacts affecting individual cards yet
                    default:
                        next();
                        break;
                }
            });
        }

        steps.Enqueue(_ => onComplete.Invoke());

        RunNextStep(steps);
    }
    //public void EvaluateArtifactsPost(System.Action onComplete)

    public void EvaluateArtifactsPost(System.Action onComplete)
    {
        Queue<System.Action<System.Action>> steps = new();

        foreach (var artifactData in ArtifactManager.Instance.ActiveArtifacts)
        {
            steps.Enqueue(next =>
            {
                Artifact visual = UIManager.Instance.GetVisualArtifact(artifactData);
                if (visual == null)
                {
                    next();
                    return;
                }

                switch (artifactData.effect)
                {
                    case ArtifactEffectType.AddDamageFlat:
                        visual.AddDamage(artifactData.value, () =>
                        {
                            visual.AddToTotalDamage(() =>
                            {
                                next();
                            });
                        });
                        break;

                    case ArtifactEffectType.DamagePerGold:
                        if(GameData.CurrentGold ==0)
                        {
                            next();
                            break;
                        }
                        int dmg = GameData.CurrentGold * artifactData.value;
                        visual.AddDamage(dmg, () =>
                        {
                            visual.AddToTotalDamage(() =>
                            {
                                next();
                            });
                        });
                        break;

                    case ArtifactEffectType.AddCritFlat:
                        visual.AddDamage(artifactData.value, () =>
                        {
                            visual.AddToTotalDamage(() =>
                            {
                                next();
                            });
                        },true);
              
                        break;

                    case ArtifactEffectType.CritPerPotionUsed:
                        int crit = GameData.PotionsUsed * artifactData.value;
                        UIManager.Instance.AddCritical(crit);
                        next();
                        break;

                    case ArtifactEffectType.RetriggerAttacks:
                        // logic handled elsewhere
                        next();
                        break;

                    default:
                        next();
                        break;
                }
            });
        }

        steps.Enqueue(_ => onComplete?.Invoke());
        RunNextStep(steps);
    }




}
