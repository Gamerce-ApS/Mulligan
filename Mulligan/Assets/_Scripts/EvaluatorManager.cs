using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
        Time.timeScale = 1; 
        UnityHelper.RunAfterDelay(this, 0.75f, () =>
        {
            string totalDamage = UIManager.Instance.DamageLabel.GetComponent<TMPro.TMP_Text>().text;
            string totalCritical = UIManager.Instance.CriticalLabel.GetComponent<TMPro.TMP_Text>().text;
            UIManager.Instance.DamageLabel.GetComponent<TMPro.TMP_Text>().text = "0";
            UIManager.Instance.CriticalLabel.GetComponent<TMPro.TMP_Text>().text = "1";
            int crit = int.Parse(totalCritical);
            if (crit == 0) crit = 1;
            int damage = int.Parse(totalDamage) * crit;
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
        steps.Enqueue(next =>
        {
            EvaluateUpgradesPost(next);
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

        // Step 1: Base Damage
        steps.Enqueue(next => aCard.CardGO.AddDamage(aCard.GetDamage(), next));

        // Step 2.5: Card Bonuses damage
        //steps.Enqueue(next => aCard.CardGO.AddDamage(aCard.GetDamageBonus(), next));

        // Step 3: Synergy Damage Bonuses
        steps.Enqueue(next => {
            int synergyBonus = GetSynergyDamage(aCard,HandManager.Instance.PlayedHand,true);
            aCard.CardGO.AddDamage(synergyBonus, next,false,false,false, GetSynergyIconForCard(aCard, HandManager.Instance.PlayedHand) );

        });

        // Step 4: Artifact Bonuses (currently 0)
        steps.Enqueue(next => EvaluateArtifactsForCard(aCard, next));


        steps.Enqueue(next => aCard.CardGO.AddDamage(aCard.CardGO.GetTotalDamage(), next,false,false,true));

        // Step 5: Total Damage move
        steps.Enqueue(next => aCard.CardGO.AddToTotalDamage(next));


        // Step 6: Add crit from upgrades
        if(aCard.GetUpgradeCritBonus()+ aCard.GetCritBonus() > 0  )
        {
            steps.Enqueue(next => aCard.CardGO.AddDamage(aCard.GetUpgradeCritBonus(), next, true));
            steps.Enqueue(next => aCard.CardGO.AddDamage(aCard.GetCritBonus(), next, true));


                steps.Enqueue(next => aCard.CardGO.AddDamage(aCard.CardGO.GetTotalCrit(), next, true, false, true));

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
    public void FinisLevel()
    {
        if ( GameManager.Instance.TheEnemy.Health > 0)
            return;
        // ❤️ Heal 10% HP After Level
        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            if (ArtifactManager.Instance.IsArtifactMutedByBoss(artifact))
                continue;

            if (artifact.effect == ArtifactEffectType.HealAfterLevel)
            {
                float healPercent = artifact.value / 100f;
                GameManager.Instance.TheHero.HealPercent(healPercent);
                UIManager.Instance.ShowTooltip($"Healed {artifact.value}% HP from artifact");
            }
            if(artifact.effect == ArtifactEffectType.DestroyUnitInHand)
            {
                if(artifact.value < Random.Range(0,100))
                {
                    Vector3 discardTarget = UIManager.Instance.DiscardPileIcon.transform.position; // or anywhere off-screen
                    CardInstance ins = HandManager.Instance.CurrentHand.GetRandom();
                    ins.CardGO.FlyAwayAndDiscard(discardTarget,0.1f,ins);

                UIManager.Instance.ShowTooltip($"Destroyed Random Unit in hand");   
                }
            }
            if(artifact.effect == ArtifactEffectType.GainGoldAfterLevel)
            {
                GameManager.Instance.AddGold(artifact.value);
                UIManager.Instance.ShowTooltip($"+ {artifact.value}% Gold from artifact");
            }
            if(artifact.effect == ArtifactEffectType.GetPotion)
            {
                UIManager.Instance.ShowTooltip($"Added random potion!");
                PotionManager.Instance.AddRandomPotion(artifact.value);
            }
            
        }
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

            if(card.GetIsAnyRace() == false && card.isMuted == false)
            raceCounts[data.race]++;

            if (!classCounts.ContainsKey(data.cardClass)) classCounts[data.cardClass] = 0;

            if (card.GetIsAnyClass() == false && card.isMuted == false)
                classCounts[data.cardClass]++;

        }
        foreach (var cardInstance in playedCards)
        {
            if (cardInstance.GetIsAnyRace() && cardInstance.isMuted == false)
            {
                // ✅ Safe: iterate over a copy of the keys
                foreach (var key in raceCounts.Keys.ToList())
                {
                    raceCounts[key]++;
                }
            }
            if (cardInstance.GetIsAnyClass() && cardInstance.isMuted == false)
            {
                foreach (var key in classCounts.Keys.ToList())
                {
                    classCounts[key]++;
                }
            }
        }


        foreach (var card in playedCards)
        {
            var data = card.data;

            bool isBoosted = raceCounts[data.race] >= 2 || classCounts[data.cardClass] >= 2;

            if (isBoosted && card.isMuted == false)
            {
                boosted.Add(card);
                totalDamage += card.GetDamage(); // include ranks, bonuses, etc.
            }
        }

        return boosted;
    }
    public int GetArtifactBonusDamage(CardInstance card)
    {
        int bonusDmg = 0;
        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            if (ArtifactManager.Instance.IsArtifactMutedByBoss(artifact))
                continue;

            if(artifact.effect == ArtifactEffectType.RaceHasExtraDamage)
            {
                if(artifact.RandomRace == card.data.race)
                {
                    bonusDmg+=artifact.value;
                }
            }
        }
        return bonusDmg;
    }
    public int GetSynergyDamage(CardInstance card, List<CardInstance> aHand, bool isCombat = false,bool popUI = true)
    {
        // Count synergies in current PlayedHand
        int raceCount = 0;
        int classCount = 0;

        foreach (var c in aHand)
        {
            if (c.data.race == card.data.race || c.GetIsAnyRace()) raceCount++;
            if (c.data.cardClass == card.data.cardClass || c.GetIsAnyClass()) classCount++;
        }

        int bonus = 0;

        if (raceCount == 2 || raceCount == 3 )
        {
            bonus += card.GetDamage(); // Double race damage
            if(popUI)
            UIManager.Instance.PulseSynergyItem(card.data.race.ToString(), isCombat);
        }

        if (classCount == 2 || classCount == 3)
        {
            bonus += card.GetDamage(); // Double class damage
            if(popUI)
            UIManager.Instance.PulseSynergyItem(card.data.cardClass.ToString(), isCombat);
        }

        return bonus;
    }
    public string GetSynergyIconForCard(CardInstance card, List<CardInstance> aHand)
    {
        // Count synergies in current PlayedHand
        int raceCount = 0;
        int classCount = 0;

        foreach (var c in aHand)
        {
            if (c.data.race == card.data.race || c.GetIsAnyRace()) raceCount++;
            if (c.data.cardClass == card.data.cardClass || c.GetIsAnyClass()) classCount++;
        }


        if (raceCount == 2 || raceCount == 3)
        {
            return CardContainer.Instance.GetSpriteForRace(card.data.race).name;
        }

        if (classCount == 2 || classCount == 3)
        {
            return CardContainer.Instance.GetSpriteForClass(card.data.cardClass).name;
        }

        return "";
    }
    public void ApplyGlobalDamageMultiplier(int multiplier)
    {
        GameData.GlobalDamageMultiplier = multiplier;
    }
    public int GetGlobalCritMultiplier(List<CardInstance> aHand)
    {
        Dictionary<CardRace, int> raceCounts = new();
        Dictionary<CardClass, int> classCounts = new();

        foreach (var card in aHand)
        {
            if (!raceCounts.ContainsKey(card.data.race))
                raceCounts[card.data.race] = 0;
            if (card.GetIsAnyRace() == false)
                raceCounts[card.data.race]++;

            if (!classCounts.ContainsKey(card.data.cardClass))
                classCounts[card.data.cardClass] = 0;
            if (card.GetIsAnyClass() == false)
                classCounts[card.data.cardClass]++;
        }
        foreach (var cardInstance in aHand)
        {
            if (cardInstance.GetIsAnyRace())
            {
                // ✅ Safe: iterate over a copy of the keys
                foreach (var key in raceCounts.Keys.ToList())
                {
                    raceCounts[key]++;
                }
            }
            if (cardInstance.GetIsAnyClass())
            {
                foreach (var key in classCounts.Keys.ToList())
                {
                    classCounts[key]++;
                }
            }
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

        return (critTriggered * 3);
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
                if (ArtifactManager.Instance.IsArtifactMutedByBoss(artifact))
                {
                    next();
                    return;
                }

                Artifact visual = UIManager.Instance.GetVisualArtifact(artifact);
                if (visual == null)
                {
                    next();
                    return;
                }
                
                switch (artifact.effect)
                {
                    // No artifacts affecting individual cards yet
                        case ArtifactEffectType.RaceHasExtraDamage:
                        
                    
                            if( card.data.race == artifact.RandomRace)
                            {
                                CardInstance c = card;
                                int dmg = artifact.value;
                                visual.Shake();
                                card.CardGO.AddDamage(dmg, () =>
                                {
                                    next();
                                }); 
                                
                            }else
                            {
                                next();
                            }
                        
                       
                        break;
                    default:
                        next();
                        break;
                }
            });
        }

        steps.Enqueue(_ => onComplete.Invoke());

        RunNextStep(steps);
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
                        UIManager.Instance.ShowTooltip($"Retriggered: {card.data.cardName}");
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
                        UIManager.Instance.ShowTooltip($"Added potion: {card.data.cardName}");
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
                        UIManager.Instance.ShowTooltip($"Healed 10% of max health!");
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
                            if(TutorialController.Instance.HasRunTutorial() == false && TutorialController.Instance.LastStepPlayed == "Step2_Shop4_ClickBattle")
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
                        visual.AddDamage( (int)((artifactData.value/100f) * GameManager.Instance.TheHero.MaxHealth), () =>
                        {
                            visual.AddToTotalDamage(() =>
                            {
                                next();
                            });
                        },false);
              
                        break;

                    case ArtifactEffectType.CritPerPotionUsed:
                        int crit = GameData.PotionsUsed * artifactData.value;

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
