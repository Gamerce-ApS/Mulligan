using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class RoundRewardResult
{
    public int GoldGained = 0;
    public int HealthGained = 0;
}

public class SynergyEvaluationResult
{
    public Dictionary<CardRace, int> RaceCounts = new Dictionary<CardRace, int>();
    public Dictionary<CardClass, int> ClassCounts = new Dictionary<CardClass, int>();
    public int TotalCritBonus = 0;
}

public class EvaluatorManager  : Singleton<EvaluatorManager>
{
    private class RaceArtifactDamageTrigger
    {
        public Artifact Visual;
        public int Damage;
    }

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
        Time.timeScale = 1; 
        UnityHelper.RunAfterDelay(this, 0.75f, () =>
        {
            string totalDamage = UIManager.Instance.DamageLabel.GetComponent<TMPro.TMP_Text>().text;
            float totalCritical = UIManager.Instance.GetCriticalValue();
            UIManager.Instance.DamageLabel.GetComponent<TMPro.TMP_Text>().text = "0";
            UIManager.Instance.CriticalLabel.GetComponent<TMPro.TMP_Text>().text = "1";
            if (totalCritical <= 0f)
                totalCritical = 1f;
            int damage = Mathf.RoundToInt(int.Parse(totalDamage) * totalCritical);
            HighscoreManager.Instance.UpdateMaxDamage(damage);
            DailyQuestManager.Instance.AddProgress(DailyQuestType.DealDamage, damage);
            DailyQuestManager.Instance.SetProgressIfHigher(DailyQuestType.SingleAttackDamage, damage);
            GameManager.Instance.TheHero.Attack(damage);
            UnityHelper.RunAfterDelay(this, 0.5f, () =>
            {
                HandManager.Instance.DiscardHand();
                UIManager.Instance.ClearSynergies();
                GameData.GlobalDamageMultiplier = 1;
                HandManager.Instance.ResetTempDamage();
            });
        });
    }
    public void PlayBoostedCardsSequentially(List<CardInstance> boostedCards, int index = 0)
    {
        if (index >= boostedCards.Count)
        {
            EvaluateAttackPost(boostedCards, () => LastCardEvaluatedDoDamgge());
            return;

        }

        // Pre Evaluation
        if (index == -1)
        {
            EvaluateAttackPre(boostedCards, () => PlayBoostedCardsSequentially(boostedCards, index + 1));
            return;
        }

        var card = boostedCards[index];
        EvaluateCard(card, () =>
        {
            PlayBoostedCardsSequentially(boostedCards, index + 1);
        });

    }
    public void EvaluateAttackPost(List<CardInstance> attackingCards, System.Action onComplete)
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
        steps.Enqueue(next =>
        {
            EvaluateUpgradesPost(next);
        });
        steps.Enqueue(next =>
        {
            ArtifactManager.Instance.ApplyGravekeeperCritMultiplier(next);
        });
        steps.Enqueue(next =>
        {
            ArtifactManager.Instance.ApplyAfterAttackEffects(attackingCards);
            next();
        });
        steps.Enqueue(next =>
        {
            ArtifactManager.Instance.ApplyHolySymbolEffects(next);
        });
        // Step 3: Done
        steps.Enqueue(_ => onComplete?.Invoke());

        RunNextStep(steps);
    }
    public void EvaluateAttackPre(List<CardInstance> attackingCards, System.Action onComplete)
    {
        Queue<System.Action<System.Action>> steps = new();

        steps.Enqueue(next =>
        {
            ArtifactManager.Instance.ApplyAttackStartEffects(attackingCards, next);
        });

        // Step 2: Apply artifacts
        steps.Enqueue(next =>
        {
            foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
            {
                // if (artifact.effect == ArtifactEffectType.AddMaxHP)
                // {
                //     GameManager.Instance.TheHero.AddMaxHPPercent(artifact.value);
                // }
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
        List<RaceArtifactDamageTrigger> raceArtifactTriggers = new List<RaceArtifactDamageTrigger>();

        steps.Enqueue(next =>
        {
            ArtifactManager.Instance.TriggerWaaaghForCard(aCard);
            next();
        });

        // Step 1: Show the unit and matching race artifact damage at the same time.
        steps.Enqueue(next => PrepareCardAndRaceArtifactDamage(aCard, triggers =>
        {
            raceArtifactTriggers = triggers;
            next();
        }));


        steps.Enqueue(next => aCard.CardGO.AddDamage(aCard.CardGO.GetTotalDamage(), next,false,false,true));

        // Step 5: Move the unit and all matching artifact numbers at the same time.
        steps.Enqueue(next => MoveCardAndRaceArtifactDamageToTotal(aCard, raceArtifactTriggers, next));

        steps.Enqueue(next => ArtifactManager.Instance.OnHunterDamageAdded(aCard, next));


        // Step 6: Temporary crit still resolves with the individual card.
        if(aCard.GetCritBonus() > 0)
        {
            steps.Enqueue(next => aCard.CardGO.AddDamage(aCard.GetCritBonus(), next, true));
            steps.Enqueue(next => aCard.CardGO.AddToTotalDamage(next, true));
        }

        if (aCard.GetUpgradeGold() > 0)
        {
            steps.Enqueue(next => aCard.CardGO.AddDamage(aCard.GetUpgradeGold(), next, false,true));
            steps.Enqueue(next => aCard.CardGO.AddToTotalDamage(next, false,true));
        }
        steps.Enqueue(next => aCard.EvaluateUpgrades(next));

        steps.Enqueue(next => aCard.TurnEnded(next));

        

        // Step 7: Done
        steps.Enqueue(_ => onComplete.Invoke());


        RunNextStep(steps);

    }
    public RoundRewardResult FinisLevel()
    {
        RoundRewardResult result = new RoundRewardResult();

        if ( GameManager.Instance.TheEnemy.Health > 0)
            return result;

        // ❤️ Heal 10% HP After Level
        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            if (ArtifactManager.Instance.IsArtifactMutedByBoss(artifact))
                continue;

            if (artifact.effect == ArtifactEffectType.HealAfterLevel)
            {
                float healPercent = artifact.value / 100f;
                float healthBefore = GameManager.Instance.TheHero.Health;
                GameManager.Instance.TheHero.HealPercent(healPercent);
                result.HealthGained += Mathf.RoundToInt(GameManager.Instance.TheHero.Health - healthBefore);
                UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.healed_from_artifact", "Healed {0}% HP from artifact", artifact.value));
            }
            if(artifact.effect == ArtifactEffectType.DestroyUnitInHand)
            {
                if(artifact.value < Random.Range(0,100))
                {
                    Vector3 discardTarget = UIManager.Instance.DiscardPileIcon.transform.position; // or anywhere off-screen
                    CardInstance ins = HandManager.Instance.CurrentHand.GetRandom();
                    ins.CardGO.FlyAwayAndDiscard(discardTarget,0.1f,ins, true);

                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.random_unit_destroyed", "Destroyed random unit in hand"));
                }
            }
            if(artifact.effect == ArtifactEffectType.GainGoldAfterLevel)
            {
                GameManager.Instance.AddGold(artifact.value);
                result.GoldGained += artifact.value;
                UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.gold_from_artifact", "+{0} Gold from artifact", artifact.value));
            }
            if(artifact.effect == ArtifactEffectType.GetPotion)
            {
                UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.random_potion_added", "Added random potion!"));
                PotionManager.Instance.AddRandomPotion(artifact.value);
            }
            
        }

        return result;
    }
    public void StartLevel()
    {

        // Called when level Start
            GameData.CurrentArmySize = 0;
            foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
            {
                if (ArtifactManager.Instance.IsArtifactMutedByBoss(artifact))
                    continue;

                switch (artifact.effect)
                {
                    case ArtifactEffectType.AddReroll:
                        GameData.CurrentReRolls += artifact.value;
                        UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.reroll_added", "+{0} Reroll", artifact.value));
                        break;

                    case ArtifactEffectType.AddArmySize:
                        GameData.CurrentArmySize += artifact.value;
                        UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.army_size_added", "+{0} Army Size", artifact.value));
                        break;

                    case ArtifactEffectType.AttackPerLevel:
                        GameData.CurrentAttacks += artifact.value;
                        UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.attack_added", "+{0} Attack", artifact.value));
                        break;

                    case ArtifactEffectType.RankRandomUnit:
                        HandManager.Instance.RankUpRandom();
                        break;
                }
            }
       



    }
    public List<CardInstance> EvaluateHand(List<CardInstance> playedCards, out int totalDamage)
    {
        totalDamage = 0;
        List<CardInstance> attackingCards = new List<CardInstance>();

        if (playedCards == null)
            return attackingCards;

        foreach (CardInstance card in playedCards)
        {
            if (card == null || card.data == null || card.isMuted)
                continue;

            attackingCards.Add(card);
            totalDamage += GetEffectiveAttack(card);
        }

        return attackingCards;
    }

    public SynergyEvaluationResult EvaluateSynergies(List<CardInstance> cards)
    {
        SynergyEvaluationResult result = new SynergyEvaluationResult();
        if (cards == null)
            return result;

        List<CardInstance> validCards = cards
            .Where(card => card != null && card.data != null && card.isMuted == false)
            .ToList();

        foreach (CardInstance card in validCards)
        {
            if (!result.RaceCounts.ContainsKey(card.data.race))
                result.RaceCounts[card.data.race] = 0;
            if (card.GetIsAnyRace() == false)
                result.RaceCounts[card.data.race]++;

            if (!result.ClassCounts.ContainsKey(card.data.cardClass))
                result.ClassCounts[card.data.cardClass] = 0;
            if (card.GetIsAnyClass() == false)
                result.ClassCounts[card.data.cardClass]++;
        }

        foreach (CardInstance card in validCards)
        {
            if (card.GetIsAnyRace())
            {
                foreach (CardRace race in result.RaceCounts.Keys.ToList())
                    result.RaceCounts[race]++;
            }

            if (card.GetIsAnyClass())
            {
                foreach (CardClass cardClass in result.ClassCounts.Keys.ToList())
                    result.ClassCounts[cardClass]++;
            }
        }

        result.TotalCritBonus = result.RaceCounts.Values.Sum(GetSynergyCritBonus) +
                                result.ClassCounts.Values.Sum(GetSynergyCritBonus);
        return result;
    }

    public int GetSynergyCritBonus(int count)
    {
        if (count >= 4)
            return 4;
        if (count >= 2)
            return 1;
        return 0;
    }
    public int GetEffectiveAttack(CardInstance card, int additionalPermanentDamage = 0)
    {
        if (card == null)
            return 0;

        if (ArtifactManager.Instance == null)
            return card.GetDamage() + additionalPermanentDamage * GameData.GlobalDamageMultiplier;

        return ArtifactManager.Instance.GetEffectiveAttack(card, additionalPermanentDamage);
    }
    public int GetArtifactBonusDamage(CardInstance card)
    {
        int bonusDmg = 0;
        if (card == null || card.data == null || ArtifactManager.Instance == null)
            return bonusDmg;

        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            if (IsRaceDamageArtifactForCard(artifact, card))
                bonusDmg += artifact.value;
        }
        return bonusDmg;
    }
    public void ApplyGlobalDamageMultiplier(int multiplier)
    {
        GameData.GlobalDamageMultiplier = multiplier;
    }
    public int GetGlobalCritMultiplier(List<CardInstance> aHand)
    {
        return EvaluateSynergies(aHand).TotalCritBonus;
    }

    public int GetStartingCritical(List<CardInstance> attackingCards)
    {
        int critical = 1 + GetGlobalCritMultiplier(attackingCards);
        if (attackingCards == null)
            return critical;

        foreach (CardInstance card in attackingCards)
        {
            if (card == null || card.data == null || card.isMuted)
                continue;

            critical += card.GetUpgradeCritBonus();
        }

        return critical;
    }

    private void RunNextStep(Queue<System.Action<System.Action>> steps)
    {
        if (steps.Count == 0) return;

        var step = steps.Dequeue();
        step(() => RunNextStep(steps));
    }

    private bool IsRaceDamageArtifactForCard(ArtifactData artifact, CardInstance card)
    {
        return artifact != null &&
               card != null &&
               card.data != null &&
               artifact.effect == ArtifactEffectType.RaceHasExtraDamage &&
               artifact.RandomRace == card.data.race &&
               ArtifactManager.Instance.IsArtifactMutedByBoss(artifact) == false;
    }

    private void PrepareCardAndRaceArtifactDamage(
        CardInstance card,
        System.Action<List<RaceArtifactDamageTrigger>> onComplete)
    {
        List<RaceArtifactDamageTrigger> triggers = new List<RaceArtifactDamageTrigger>();
        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            if (IsRaceDamageArtifactForCard(artifact, card) == false || artifact.value == 0)
                continue;

            triggers.Add(new RaceArtifactDamageTrigger
            {
                Visual = UIManager.Instance.GetVisualArtifact(artifact),
                Damage = artifact.value
            });
        }

        int pendingNumbers = 1 + triggers.Count(trigger => trigger.Visual != null);

        void FinishNumber()
        {
            pendingNumbers--;
            if (pendingNumbers <= 0)
                onComplete?.Invoke(triggers);
        }

        card.CardGO.AddDamage(GetEffectiveAttack(card), FinishNumber);

        foreach (RaceArtifactDamageTrigger trigger in triggers)
        {
            if (trigger.Visual == null)
                continue;

            trigger.Visual.AddDamage(trigger.Damage, FinishNumber);
        }
    }

    private void MoveCardAndRaceArtifactDamageToTotal(
        CardInstance card,
        List<RaceArtifactDamageTrigger> triggers,
        System.Action onComplete)
    {
        int pendingAnimations = 1 + triggers.Count;

        void FinishAnimation()
        {
            pendingAnimations--;
            if (pendingAnimations <= 0)
                onComplete?.Invoke();
        }

        card.CardGO.AddToTotalDamage(FinishAnimation);

        foreach (RaceArtifactDamageTrigger trigger in triggers)
        {
            if (trigger.Visual != null && trigger.Visual.DmgNumber != null)
            {
                trigger.Visual.AddToTotalDamage(FinishAnimation);
                continue;
            }

            // Preserve gameplay damage if an active artifact visual is unavailable.
            LeanTween.delayedCall(gameObject, 1f, () =>
            {
                UIManager.Instance.AddDamage(trigger.Damage);
                FinishAnimation();
            });
        }
    }
    public void EvaluateUpgradesPost(System.Action onComplete)
    {
        HashSet<CardInstance> alreadyRetriggered = new();
        Queue<System.Action<System.Action>> steps = new();

        foreach (var card in HandManager.Instance.PlayedHand)
        {
            if (alreadyRetriggered.Contains(card)) continue;

            foreach (var upgrade in card.appliedUpgrades)
            {
                if (upgrade.effect == UpgradeEffect.Enchantment_Retrigger)
                {
                    alreadyRetriggered.Add(card);
                    steps.Enqueue(next =>
                    {
                        Debug.Log("Retriggering card from upgrade: " + card.data.cardName);
                        UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.retriggered_unit", "Retriggered: {0}", LocalizedContent.UnitName(card.data)));
                        EvaluateCard(card, () =>
                        {
                            Debug.Log("Retrigger complete for: " + card.data.cardName);
                            next();
                        });
                    });
                }
                if (upgrade.effect == UpgradeEffect.Charms_Potion)
                {
                    steps.Enqueue(next =>
                    {
                        UIManager.Instance.ShowTooltip(LocalizationService.Format("ui.tooltip.potion_added_for_unit", "Added potion: {0}", LocalizedContent.UnitName(card.data)));
                        PotionManager.Instance.AddRandomPotion();
                        UnityHelper.RunAfterDelay(this, 0.5f, () =>
                        {
                            next();
                        });
                    });
                }
                if (upgrade.effect == UpgradeEffect.Charms_Heal)
                {
                    steps.Enqueue(next =>
                    {
                        UIManager.Instance.ShowTooltip(LocalizationService.Get("ui.tooltip.healed_max_health", "Healed 10% of max health!"));
                        GameManager.Instance.TheHero.HealPercent(0.1f);
                        UnityHelper.RunAfterDelay(this, 0.5f, () =>
                        {
                            next();
                        });
                    });
                }
            }
        }

        steps.Enqueue(_ => onComplete?.Invoke());

        RunNextStep(steps);
    }
    public void EvaluateArtifactsPost(System.Action onComplete)
    {
        Queue<System.Action<System.Action>> steps = new();

        foreach (var artifactData in ArtifactManager.Instance.ActiveArtifacts)
        {
            steps.Enqueue(next =>
            {
                if (ArtifactManager.Instance.IsArtifactMutedByBoss(artifactData))
                {
                    next();
                    return;
                }

                Artifact visual = UIManager.Instance.GetVisualArtifact(artifactData);
                if (visual == null)
                {
                    next();
                    return;
                }
                if(visual.isMuted)
                {
                    next();
                    return;
                }

                switch (artifactData.effect)
                {
                    case ArtifactEffectType.AddDamageFlat:
                        visual.AddDamage(artifactData.value, () =>
                        {
                            if(TutorialController.Instance.ShouldShowArtifactTriggeredStep())
                                {
                                    TutorialController.Instance.ShowStepById("Step3_Artifact");
                                }
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
                    case ArtifactEffectType.ProcHPinDamage:
                        int hpDamage = (int)((artifactData.value/100f) * GameManager.Instance.TheHero.MaxHealth);
                        if (hpDamage == 0)
                        {
                            next();
                            break;
                        }
                        visual.AddDamage(hpDamage, () =>
                        {
                            visual.AddToTotalDamage(() =>
                            {
                                next();
                            });
                        },false);
              
                        break;

                    case ArtifactEffectType.CritPerPotionUsed:
                        int crit = GameData.PotionsUsed * artifactData.value;
                        if (crit == 0)
                        {
                            next();
                            break;
                        }

                        visual.AddDamage(crit, () =>
                        {
                            visual.AddToTotalDamage(() =>
                            {
                                next();
                            });
                        }, true);

                        //UIManager.Instance.AddCritical(crit);
                        // next();
                        break;
                    case ArtifactEffectType.CritPerSkippedLevel:
                        int crit2 = GameData.SkippedLevels * artifactData.value;
                        if (crit2 == 0)
                        {
                            next();
                            break;
                        }

                        visual.AddDamage(crit2, () =>
                        {
                            visual.AddToTotalDamage(() =>
                            {
                                next();
                            });
                        }, true);

                        //UIManager.Instance.AddCritical(crit);
                        // next();
                        break;
                    case ArtifactEffectType.CritPerUpgradedUnit:
                        int crit3 = GameData.UpgradedUnits * artifactData.value;
                        if (crit3 == 0)
                        {
                            next();
                            break;
                        }

                        visual.AddDamage(crit3, () =>
                        {
                            visual.AddToTotalDamage(() =>
                            {
                                next();
                            });
                        }, true);

                        //UIManager.Instance.AddCritical(crit);
                        // next();
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
