using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

public class ArtifactManager : Singleton<ArtifactManager>
{
    public List<ArtifactData> ActiveArtifacts = new List<ArtifactData>(5);

    [Header("Tavern Tales VFX")]
    public Vector2 TavernTalesDamageTargetOffset = Vector2.zero;
    public float TavernTalesBardShakeScale = 1.15f;
    public float TavernTalesBardShakeDuration = 0.4f;

    private readonly Dictionary<ArtifactData, int> boneCollectorDestroyedUnits = new Dictionary<ArtifactData, int>();
    private readonly Dictionary<ArtifactData, int> gravekeeperUndeadRerolls = new Dictionary<ArtifactData, int>();
    private readonly Dictionary<ArtifactData, int> explosiveArrowHunterAttacks = new Dictionary<ArtifactData, int>();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void AddRandomArtifact()
    {
        if (ActiveArtifacts.Count >= GameManager.Instance.TheHero.myHeroData.ArtifactSlots)
        {
            Debug.Log("Artifact slots are full.");
            return;
        }

        var all = CardContainer.Instance.GetUnlockedArtifacts();

        if (all == null || all.Count == 0)
        {
            Debug.LogWarning("No artifacts available to choose from.");
            return;
        }

        // Filter out already equipped ones
        List<ArtifactData> available = new List<ArtifactData>();
        foreach (var artifact in all)
        {
            if (!ActiveArtifacts.Contains(artifact))
            {
                available.Add(artifact);
            }
        }

        if (available.Count == 0)
        {
            Debug.Log("All artifacts are already equipped.");
            return;
        }

        // Pick random one
        // ArtifactData selected = available[Random.Range(0, available.Count)];
        ArtifactData selected = PickArtifactByRarity(available);
        if (selected == null)
            return;

        DailyQuestManager.Instance.RollUnlockedRaceForArtifact(selected);
        AddActiveArtifact(selected);
        SoundManager.TryPlay(SoundType.ArtifactObtained);

        // Update UI
        UIManager.Instance.UpdateArtifactSlotsUI();

        Debug.Log("Added artifact: " + selected.name);
    }
    public ArtifactData GetRandom()
    {

        var all = CardContainer.Instance.GetUnlockedArtifacts();
        if (all == null || all.Count == 0)
        {
            if (TutorialController.Instance.HasRunTutorial()== false && TutorialController.Instance.LastStepPlayed == "Step1_Gold")
                return CardContainer.Instance.ArtifactDataList.ToList().Find(c=> c.name == "+20 Dmg");

            Debug.LogWarning("No artifacts available to choose from.");
            return null;
        }

        // Filter out already equipped ones
        List<ArtifactData> available = new List<ArtifactData>();
        foreach (var artifact in all)
        {
            if (!ActiveArtifacts.Contains(artifact))
            {
                available.Add(artifact);
            }
        }

        if (available.Count == 0)
        {
            Debug.Log("All artifacts are already equipped.");
            return null;
        }

        // Pick random one
        // ArtifactData selected = available[Random.Range(0, available.Count)];
        ArtifactData selected = PickArtifactByRarity(available);
        DailyQuestManager.Instance.RollUnlockedRaceForArtifact(selected);

        if (TutorialController.Instance.HasRunTutorial()== false && TutorialController.Instance.LastStepPlayed == "Step1_Gold")
        {
             selected = available.Find(c=> c.name == "+20 Dmg");
             if (selected == null)
                selected = CardContainer.Instance.ArtifactDataList.ToList().Find(c=> c.name == "+20 Dmg");
        }

        return selected;
    }

    public bool HasAvailableArtifactChoices()
    {
        return GetAvailableArtifactChoices().Count > 0;
    }

    public List<ArtifactData> GetRandomArtifactChoices(int amount)
    {
        List<ArtifactData> choices = new List<ArtifactData>();
        if (amount <= 0)
            return choices;

        List<ArtifactData> available = GetAvailableArtifactChoices();
        while (choices.Count < amount && available.Count > 0)
        {
            ArtifactData selected = PickArtifactByRarity(available);
            if (selected == null)
                break;

            DailyQuestManager.Instance.RollUnlockedRaceForArtifact(selected);
            choices.Add(selected);
            available.Remove(selected);
        }

        return choices;
    }

    private List<ArtifactData> GetAvailableArtifactChoices()
    {
        List<ArtifactData> unlockedArtifacts = CardContainer.Instance.GetUnlockedArtifacts();
        if (unlockedArtifacts == null)
            return new List<ArtifactData>();

        return unlockedArtifacts
            .Where(artifact => artifact != null && ActiveArtifacts.Contains(artifact) == false)
            .ToList();
    }

    public void AddArtifact(ArtifactEffectType aType)
    {
        if (ActiveArtifacts.Count >= GameManager.Instance.TheHero.myHeroData.ArtifactSlots)
        {
            Debug.Log("Artifact slots are full.");
            return;
        }

        var all = CardContainer.Instance.GetUnlockedArtifacts();
        if (all == null || all.Count == 0)
        {
            Debug.LogWarning("No artifacts available to choose from.");
            return;
        }

        // Filter out already equipped ones
        List<ArtifactData> available = new List<ArtifactData>();
        foreach (var artifact in all)
        {
            if (!ActiveArtifacts.Contains(artifact) && aType== artifact.effect)
            {
                DailyQuestManager.Instance.RollUnlockedRaceForArtifact(artifact);
                AddActiveArtifact(artifact);
                SoundManager.TryPlay(SoundType.ArtifactObtained);

                // Update UI
                UIManager.Instance.UpdateArtifactSlotsUI();

                Debug.Log("Added artifact: " + artifact.name);
                return;
            }
        }

        if (available.Count == 0)
        {
            Debug.Log("All artifacts are already equipped.");
            return;
        }




    }
    public void SellArtifact(Artifact aArtifact)
    {
        boneCollectorDestroyedUnits.Remove(aArtifact.ArtifactData);
        gravekeeperUndeadRerolls.Remove(aArtifact.ArtifactData);
        explosiveArrowHunterAttacks.Remove(aArtifact.ArtifactData);
        ActiveArtifacts.Remove(aArtifact.ArtifactData);
        Destroy(aArtifact.gameObject);
        SoundManager.TryPlay(SoundType.ArtifactSold);
        UIManager.Instance.UpdateArtifactSlotsUI(); // updates visuals
        GameManager.Instance.AddGold(3);
        GameManager.Instance.TheHero.RefreshBar();


    }

    public bool ReorderArtifact(ArtifactData artifact, int targetSlotIndex)
    {
        if (artifact == null || GameManager.Instance.TheHero == null)
            return false;

        int sourceIndex = ActiveArtifacts.IndexOf(artifact);
        int unlockedSlots = GameManager.Instance.TheHero.myHeroData.ArtifactSlots;

        if (sourceIndex < 0 || targetSlotIndex < 0 || targetSlotIndex >= unlockedSlots)
            return false;

        if (targetSlotIndex < ActiveArtifacts.Count)
        {
            if (sourceIndex == targetSlotIndex)
                return false;

            ArtifactData targetArtifact = ActiveArtifacts[targetSlotIndex];
            ActiveArtifacts[targetSlotIndex] = artifact;
            ActiveArtifacts[sourceIndex] = targetArtifact;
        }
        else
        {
            if (sourceIndex == ActiveArtifacts.Count - 1)
                return false;

            ActiveArtifacts.RemoveAt(sourceIndex);
            ActiveArtifacts.Add(artifact);
        }

        UIManager.Instance.UpdateArtifactSlotsUI();
        return true;
    }

    public void AddArtifact(ArtifactData artifact)
    {
        AddArtifact(artifact, true);
    }

    public void AddArtifact(ArtifactData artifact, bool rollUnlockedRace)
    {
        if (artifact == null)
            return;

        if (ActiveArtifacts.Count >= GameManager.Instance.TheHero.myHeroData.ArtifactSlots) return;

        if (rollUnlockedRace)
            DailyQuestManager.Instance.RollUnlockedRaceForArtifact(artifact);
        AddActiveArtifact(artifact);
        SoundManager.TryPlay(SoundType.ArtifactObtained);
        UIManager.Instance.UpdateArtifactSlotsUI(); // updates visuals

        if(TutorialController.Instance.LastStepPlayed=="Step2_Shop3")
        {
            TutorialController.Instance.ShowNextStep();
        }
    }

    public bool HasArtifact(ArtifactEffectType effectType)
    {
        return ActiveArtifacts.Exists(a => a.effect == effectType && IsArtifactMutedByBoss(a) == false);
    }

    public int GetArtifactValue(ArtifactEffectType effectType)
    {
        int total = 0;
        foreach (var artifact in ActiveArtifacts)
        {
            if (IsArtifactMutedByBoss(artifact))
                continue;

            if (artifact.effect == effectType)
                total += artifact.value;
        }
        return total;
    }

    public int GetBoneCollectorDestroyedUnits(ArtifactData artifact)
    {
        if (artifact == null || artifact.effect != ArtifactEffectType.UndeadPermanentAttackPerDestroyedUnit)
            return 0;

        return boneCollectorDestroyedUnits.TryGetValue(artifact, out int amount) ? amount : 0;
    }

    public float GetGravekeeperCritMultiplier(ArtifactData artifact)
    {
        if (artifact == null || artifact.effect != ArtifactEffectType.CritMultiplierPerUndeadRerolled)
            return 1f;

        int rerolledUndead = gravekeeperUndeadRerolls.TryGetValue(artifact, out int amount) ? amount : 0;
        int multiplierStepInTenths = Mathf.Max(1, artifact.value);
        return 1f + rerolledUndead * multiplierStepInTenths * 0.1f;
    }

    public int GetExplosiveArrowHunterAttacks(ArtifactData artifact)
    {
        if (artifact == null || artifact.effect != ArtifactEffectType.ExplosiveArrow)
            return 0;

        return explosiveArrowHunterAttacks.TryGetValue(artifact, out int amount) ? amount : 0;
    }

    public void OnUnitsRerolled(List<CardInstance> rerolledCards)
    {
        if (rerolledCards == null || rerolledCards.Count == 0)
            return;

        int undeadAmount = rerolledCards.Count(card =>
            card != null && card.data != null && card.data.race == CardRace.Undead);
        if (undeadAmount == 0)
            return;

        foreach (var artifact in ActiveArtifacts)
        {
            if (artifact == null ||
                artifact.effect != ArtifactEffectType.CritMultiplierPerUndeadRerolled ||
                IsArtifactMutedByBoss(artifact))
                continue;

            int currentAmount = gravekeeperUndeadRerolls.TryGetValue(artifact, out int amount) ? amount : 0;
            gravekeeperUndeadRerolls[artifact] = currentAmount + undeadAmount;

            Artifact visual = UIManager.Instance.GetVisualArtifact(artifact);
            if (visual != null)
                visual.RefreshCounter();
        }
    }

    private void AddActiveArtifact(ArtifactData artifact)
    {
        ActiveArtifacts.Add(artifact);

        if (artifact.effect == ArtifactEffectType.UndeadPermanentAttackPerDestroyedUnit)
            boneCollectorDestroyedUnits[artifact] = 0;

        if (artifact.effect == ArtifactEffectType.CritMultiplierPerUndeadRerolled)
            gravekeeperUndeadRerolls[artifact] = 0;

        if (artifact.effect == ArtifactEffectType.ExplosiveArrow)
            explosiveArrowHunterAttacks[artifact] = 0;
    }

    public void OnHunterDamageAdded(CardInstance card, System.Action onComplete)
    {
        if (card == null || card.data == null || card.data.cardClass != CardClass.Archer)
        {
            onComplete?.Invoke();
            return;
        }

        List<ArtifactData> explosiveArrows = ActiveArtifacts
            .Where(artifact => artifact != null &&
                               artifact.effect == ArtifactEffectType.ExplosiveArrow &&
                               IsArtifactMutedByBoss(artifact) == false)
            .ToList();

        TriggerExplosiveArrow(explosiveArrows, 0, onComplete);
    }

    private void TriggerExplosiveArrow(List<ArtifactData> artifacts, int index, System.Action onComplete)
    {
        if (index >= artifacts.Count)
        {
            onComplete?.Invoke();
            return;
        }

        const int attacksRequired = 3;
        ArtifactData artifact = artifacts[index];
        int attackCount = GetExplosiveArrowHunterAttacks(artifact) + 1;
        bool dealsDamage = attackCount >= attacksRequired;
        explosiveArrowHunterAttacks[artifact] = dealsDamage ? 0 : attackCount;

        Artifact visual = UIManager.Instance.GetVisualArtifact(artifact);
        if (visual != null)
        {
            visual.RefreshCounter();

            CardRenderTextureCapture capture = CardRenderTextureCapture.Instance;
            if (capture != null)
                capture.PlayArtifactActivateVFXAtUI(visual.transform as RectTransform);
        }

        if (dealsDamage == false)
        {
            TriggerArtifact(artifact);
            TriggerExplosiveArrow(artifacts, index + 1, onComplete);
            return;
        }

        if (artifact.value <= 0)
        {
            TriggerExplosiveArrow(artifacts, index + 1, onComplete);
            return;
        }

        if (visual == null)
        {
            UIManager.Instance.AddDamage(artifact.value);
            TriggerExplosiveArrow(artifacts, index + 1, onComplete);
            return;
        }

        visual.AddDamage(artifact.value, () =>
        {
            visual.AddToTotalDamage(() =>
            {
                TriggerExplosiveArrow(artifacts, index + 1, onComplete);
            });
        });
    }

    public bool IsArtifactMutedByBoss(ArtifactData artifact)
    {
        if (artifact == null || GameManager.Instance.AreArtifactsMutedByBoss() == false)
            return false;

        int index = ActiveArtifacts.IndexOf(artifact);
        return index >= 0 && index < 2;
    }

    public int GetEffectiveAttack(CardInstance card, int additionalPermanentDamage = 0)
    {
        if (card == null || card.data == null)
            return 0;

        int attack = card.GetDamage() + additionalPermanentDamage * GameData.GlobalDamageMultiplier;
        if (card.data.race != CardRace.Orc)
            return attack;

        Hero hero = GameManager.Instance.TheHero;
        if (hero == null || hero.MaxHealth <= 0 || hero.Health >= hero.MaxHealth * 0.5f)
            return attack;

        int multiplier = GetArtifactValue(ArtifactEffectType.DoubleOrcAttackBelowHalfHealth);
        return multiplier > 1 ? attack * multiplier : attack;
    }

    public int GetTavernTalesBonus(List<CardInstance> cardsBeingPlayed)
    {
        int artifactValue = GetArtifactValue(ArtifactEffectType.BardInHandAttackingUnitsPlusDamage);
        if (artifactValue <= 0)
            return 0;

        return artifactValue * GetBardsKeptInHandCount(cardsBeingPlayed);
    }

    private int GetBardsKeptInHandCount(List<CardInstance> cardsBeingPlayed)
    {
        return GetBardsKeptInHand(cardsBeingPlayed).Count;
    }

    private List<CardInstance> GetBardsKeptInHand(List<CardInstance> cardsBeingPlayed)
    {
        return HandManager.Instance.CurrentHand.Where(card =>
            card != null && card.data != null && card.isMuted == false &&
            card.data.cardClass == CardClass.Bard &&
            (cardsBeingPlayed == null || cardsBeingPlayed.Contains(card) == false))
            .ToList();
    }

    public void ApplyAttackStartEffects(List<CardInstance> attackingCards, System.Action onComplete)
    {
        if (attackingCards == null || attackingCards.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        List<CardInstance> validAttackers = attackingCards
            .Where(card => card != null && card.data != null && card.isMuted == false)
            .Distinct()
            .ToList();

        if (validAttackers.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        List<ArtifactData> tavernTalesArtifacts = ActiveArtifacts
            .Where(artifact => artifact != null &&
                               artifact.effect == ArtifactEffectType.BardInHandAttackingUnitsPlusDamage &&
                               IsArtifactMutedByBoss(artifact) == false &&
                               artifact.value > 0)
            .ToList();

        List<CardInstance> bardsInHand = GetBardsKeptInHand(attackingCards);
        int bonusPerBard = tavernTalesArtifacts.Sum(artifact => artifact.value);

        if (tavernTalesArtifacts.Count == 0 || bardsInHand.Count == 0 || bonusPerBard <= 0)
        {
            onComplete?.Invoke();
            return;
        }

        foreach (var artifact in tavernTalesArtifacts)
            TriggerArtifact(artifact);

        StartCoroutine(PlayTavernTalesBoost(
            bardsInHand,
            validAttackers,
            bonusPerBard,
            onComplete));
    }

    private IEnumerator PlayTavernTalesBoost(List<CardInstance> bardsInHand,
        List<CardInstance> attackingCards, int bonusPerBard, System.Action onComplete)
    {
        const float popDuration = 0.2f;
        const float flyDuration = 0.4f;
        List<GameObject> damageNumbers = new List<GameObject>();
        List<RectTransform> damageTargets = new List<RectTransform>();

        foreach (var bard in bardsInHand)
        {
            if (bard.CardGO != null)
                bard.CardGO.Shake(TavernTalesBardShakeScale, TavernTalesBardShakeDuration);
        }

        bool canAnimate = UIManager.Instance.DamageFloatPrefab != null &&
                          UIManager.Instance.thCanvas != null;

        if (canAnimate)
        {
            foreach (var bard in bardsInHand)
            {
                if (bard.CardGO == null)
                    continue;

                foreach (var attacker in attackingCards)
                {
                    if (attacker.CardGO == null)
                        continue;

                    RectTransform target = attacker.CardGO.DamageLabel != null
                        ? attacker.CardGO.DamageLabel.rectTransform
                        : attacker.CardGO.rectTransform;
                    GameObject damageNumber = Instantiate(
                        UIManager.Instance.DamageFloatPrefab,
                        bard.CardGO.transform.position,
                        Quaternion.identity,
                        UIManager.Instance.thCanvas.transform);

                    TMPro.TMP_Text damageText = damageNumber.GetComponent<TMPro.TMP_Text>();
                    if (damageText != null)
                    {
                        damageText.text = "+" + bonusPerBard;
                        damageText.raycastTarget = false;
                    }

                    damageNumber.transform.localScale = Vector3.zero;
                    LeanTween.scale(damageNumber, Vector3.one * 1.15f, popDuration).setEaseOutBack();
                    damageNumbers.Add(damageNumber);
                    damageTargets.Add(target);
                }
            }
        }

        if (damageNumbers.Count > 0)
        {
            yield return new WaitForSeconds(popDuration);

            for (int i = 0; i < damageNumbers.Count; i++)
            {
                if (damageNumbers[i] == null || damageTargets[i] == null)
                    continue;

                Vector3 targetPosition = GetPositionOnEffectCanvas(damageTargets[i]);
                LeanTween.move(damageNumbers[i], targetPosition, flyDuration).setEaseInCubic();
                LeanTween.scale(damageNumbers[i], Vector3.one * 0.7f, flyDuration).setEaseInCubic();
            }

            yield return new WaitForSeconds(flyDuration);

            foreach (var damageNumber in damageNumbers)
            {
                if (damageNumber != null)
                    Destroy(damageNumber);
            }
        }

        int totalBonus = bonusPerBard * bardsInHand.Count;
        foreach (var attacker in attackingCards)
        {
            attacker.AddPermanentDamage(totalBonus);

            if (attacker.CardGO != null)
                PulseDamageLabel(attacker.CardGO);
        }

        onComplete?.Invoke();
    }

    private Vector3 GetPositionOnEffectCanvas(RectTransform target)
    {
        if (target == null || UIManager.Instance.thCanvas == null)
            return target != null ? target.position : Vector3.zero;

        Canvas sourceCanvas = target.GetComponentInParent<Canvas>();
        Camera sourceCamera = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? sourceCanvas.worldCamera
            : null;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(sourceCamera, target.position);

        Canvas effectCanvas = UIManager.Instance.thCanvas;
        screenPosition += TavernTalesDamageTargetOffset * effectCanvas.scaleFactor;
        RectTransform effectCanvasRect = effectCanvas.transform as RectTransform;
        Camera effectCamera = effectCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? effectCanvas.worldCamera
            : null;

        if (effectCanvasRect != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(
                effectCanvasRect,
                screenPosition,
                effectCamera,
                out Vector3 worldPosition))
        {
            return worldPosition;
        }

        return target.position;
    }

    private void PulseDamageLabel(Card card)
    {
        if (card == null || card.DamageLabel == null)
            return;

        GameObject labelObject = card.DamageLabel.gameObject;
        Vector3 originalScale = labelObject.transform.localScale;
        LeanTween.cancel(labelObject);
        labelObject.transform.localScale = originalScale;
        LeanTween.scale(labelObject, originalScale * 1.3f, 0.3f)
            .setEasePunch()
            .setOnComplete(() =>
            {
                if (labelObject != null)
                    labelObject.transform.localScale = originalScale;
            });
    }

    public void TriggerWaaaghForCard(CardInstance card)
    {
        if (card == null || card.data == null || card.data.race != CardRace.Orc)
            return;

        Hero hero = GameManager.Instance.TheHero;
        if (hero == null || hero.MaxHealth <= 0 || hero.Health >= hero.MaxHealth * 0.5f)
            return;

        if (GetArtifactValue(ArtifactEffectType.DoubleOrcAttackBelowHalfHealth) <= 1)
            return;

        foreach (var artifact in ActiveArtifacts)
        {
            if (artifact != null &&
                artifact.effect == ArtifactEffectType.DoubleOrcAttackBelowHalfHealth &&
                IsArtifactMutedByBoss(artifact) == false)
            {
                TriggerArtifact(artifact);
            }
        }
    }

    public void ApplyAfterAttackEffects(List<CardInstance> attackingCards)
    {
        if (attackingCards == null || attackingCards.Count == 0)
            return;

        List<CardInstance> warriors = attackingCards
            .Where(card => card != null && card.data != null && card.isMuted == false &&
                           card.data.cardClass == CardClass.Warrior)
            .Distinct()
            .ToList();

        if (warriors.Count == 0)
            return;

        foreach (var artifact in ActiveArtifacts)
        {
            if (artifact == null ||
                artifact.effect != ArtifactEffectType.WarriorPermanentAttackOnAttack ||
                IsArtifactMutedByBoss(artifact) || artifact.value <= 0)
                continue;

            foreach (var warrior in warriors)
            {
                warrior.AddPermanentDamage(artifact.value);

                if (warrior.CardGO != null)
                    warrior.CardGO.Shake();
            }

            TriggerArtifact(artifact);
        }
    }

    public void ApplyHolySymbolEffects(System.Action onComplete)
    {
        List<ArtifactData> holySymbols = ActiveArtifacts
            .Where(artifact => artifact != null &&
                               artifact.effect == ArtifactEffectType.ClericsInHandHealOnAttack &&
                               IsArtifactMutedByBoss(artifact) == false)
            .ToList();

        if (holySymbols.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        List<CardInstance> clerics = HandManager.Instance.CurrentHand
            .Where(card => card != null && card.data != null && card.isMuted == false &&
                           card.data.cardClass == CardClass.Cleric)
            .ToList();

        int healAmount = clerics.Sum(card => GetEffectiveAttack(card)) * holySymbols.Count;

        if (healAmount <= 0)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(PlayHolySymbolHeal(clerics, holySymbols, healAmount, onComplete));
    }

    private IEnumerator PlayHolySymbolHeal(List<CardInstance> clerics, List<ArtifactData> holySymbols,
        int healAmount, System.Action onComplete)
    {
        const float popDuration = 0.2f;
        const float flyDuration = 0.4f;
        const float totalHoldDuration = 1.5f;
        Color healColor = new Color(0.25f, 1f, 0.35f, 1f);
        Hero hero = GameManager.Instance.TheHero;
        List<GameObject> healNumbers = new List<GameObject>();

        foreach (var cleric in clerics)
        {
            if (cleric.CardGO != null)
                cleric.CardGO.Shake();
        }

        if (hero == null || UIManager.Instance.DamageFloatPrefab == null || UIManager.Instance.thCanvas == null)
        {
            ApplyHolySymbolHeal(holySymbols, healAmount);
            onComplete?.Invoke();
            yield break;
        }

        foreach (var cleric in clerics)
        {
            if (cleric.CardGO == null)
                continue;

            int clericHeal = GetEffectiveAttack(cleric) * holySymbols.Count;
            GameObject healNumber = Instantiate(
                UIManager.Instance.DamageFloatPrefab,
                cleric.CardGO.transform.position,
                Quaternion.identity,
                UIManager.Instance.thCanvas.transform);

            TMPro.TMP_Text healText = healNumber.GetComponent<TMPro.TMP_Text>();
            if (healText != null)
            {
                healText.text = "+" + clericHeal;
                healText.color = healColor;
                healText.raycastTarget = false;
            }

            healNumber.transform.localScale = Vector3.zero;
            LeanTween.scale(healNumber, Vector3.one * 1.15f, popDuration).setEaseOutBack();
            healNumbers.Add(healNumber);
        }

        yield return new WaitForSeconds(popDuration);

        Vector3 healTarget = hero.healthLabel != null ? hero.healthLabel.transform.position : hero.transform.position;
        foreach (var healNumber in healNumbers)
        {
            if (healNumber == null)
                continue;

            LeanTween.move(healNumber, healTarget, flyDuration).setEaseInCubic();
            LeanTween.scale(healNumber, Vector3.one * 0.7f, flyDuration).setEaseInCubic();
        }

        yield return new WaitForSeconds(flyDuration);

        foreach (var healNumber in healNumbers)
        {
            if (healNumber != null)
                Destroy(healNumber);
        }

        ApplyHolySymbolHeal(holySymbols, healAmount);

        GameObject totalNumber = Instantiate(
            UIManager.Instance.DamageFloatPrefab,
            healTarget,
            Quaternion.identity,
            UIManager.Instance.thCanvas.transform);
        TMPro.TMP_Text totalText = totalNumber.GetComponent<TMPro.TMP_Text>();
        if (totalText != null)
        {
            totalText.text = "+" + healAmount;
            totalText.color = healColor;
            totalText.raycastTarget = false;
        }

        totalNumber.transform.localScale = Vector3.zero;
        LeanTween.scale(totalNumber, Vector3.one * 1.35f, popDuration).setEaseOutBack();
        yield return new WaitForSeconds(popDuration + totalHoldDuration);

        CanvasGroup canvasGroup = totalNumber.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = totalNumber.AddComponent<CanvasGroup>();

        LeanTween.alphaCanvas(canvasGroup, 0f, 0.2f).setOnComplete(() =>
        {
            if (totalNumber != null)
                Destroy(totalNumber);
        });

        yield return new WaitForSeconds(0.2f);
        onComplete?.Invoke();
    }

    private void ApplyHolySymbolHeal(List<ArtifactData> holySymbols, int healAmount)
    {
        GameManager.Instance.TheHero.HealHPPoints(healAmount);

        foreach (var artifact in holySymbols)
            TriggerArtifact(artifact);

        if (GameManager.Instance.TheHero.HealEffect != null)
        {
            GameManager.Instance.TheHero.HealEffect.SetActive(false);
            GameManager.Instance.TheHero.HealEffect.SetActive(true);
        }
    }

    public void ApplyGravekeeperCritMultiplier(System.Action onComplete)
    {
        List<ArtifactData> gravekeepers = ActiveArtifacts
            .Where(artifact => artifact != null &&
                               artifact.effect == ArtifactEffectType.CritMultiplierPerUndeadRerolled &&
                               IsArtifactMutedByBoss(artifact) == false &&
                               GetGravekeeperCritMultiplier(artifact) > 1f)
            .ToList();

        ApplyGravekeeperCritMultiplier(gravekeepers, 0, onComplete);
    }

    private void ApplyGravekeeperCritMultiplier(List<ArtifactData> gravekeepers, int index,
        System.Action onComplete)
    {
        if (index >= gravekeepers.Count)
        {
            onComplete?.Invoke();
            return;
        }

        ArtifactData artifact = gravekeepers[index];
        float multiplier = GetGravekeeperCritMultiplier(artifact);
        Artifact visual = UIManager.Instance.GetVisualArtifact(artifact);

        if (visual == null)
        {
            UIManager.Instance.MultiplyCritical(multiplier);
            ApplyGravekeeperCritMultiplier(gravekeepers, index + 1, onComplete);
            return;
        }

        visual.AddCriticalMultiplier(multiplier, () =>
        {
            visual.MultiplyCritical(() =>
            {
                ApplyGravekeeperCritMultiplier(gravekeepers, index + 1, onComplete);
            });
        });
    }

    public void OnUnitDestroyed(CardInstance destroyedUnit)
    {
        List<ArtifactData> boneCollectors = ActiveArtifacts
            .Where(artifact => artifact != null &&
                               artifact.effect == ArtifactEffectType.UndeadPermanentAttackPerDestroyedUnit &&
                               IsArtifactMutedByBoss(artifact) == false)
            .ToList();

        if (boneCollectors.Count == 0)
            return;

        foreach (var artifact in boneCollectors)
        {
            boneCollectorDestroyedUnits[artifact] = GetBoneCollectorDestroyedUnits(artifact) + 1;

            Artifact visual = UIManager.Instance.GetVisualArtifact(artifact);
            if (visual != null)
                visual.RefreshCounter();
        }

        List<CardInstance> undeadCards = CardContainer.Instance.GetAllOwnedCards()
            .Where(card => card != destroyedUnit && card.data.race == CardRace.Undead)
            .ToList();

        if (undeadCards.Count == 0)
            return;

        int bonus = boneCollectors.Sum(artifact => artifact.value);
        if (bonus <= 0)
            return;

        foreach (var card in undeadCards)
            card.AddPermanentDamage(bonus);

        foreach (var artifact in boneCollectors)
            TriggerArtifact(artifact);
    }

    private void TriggerArtifact(ArtifactData artifact)
    {
        Artifact visual = UIManager.Instance.GetVisualArtifact(artifact);
        if (visual != null)
            visual.Shake();

        SoundManager.TryPlay(SoundType.ArtifactTrigger);
    }



    [System.Serializable]
    public class ArtifactJsonData
    {
        public string name;
        public string description;
        public int value;
        public ArtifactEffectType effect;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }

    private string WrapArray(string rawJson)
    {
        return "{ \"items\": " + rawJson + " }";
    }
    private ArtifactData PickArtifactByRarity(List<ArtifactData> available)
{
    if (available == null || available.Count == 0)
        return null;

    RarityType rolled = CardContainer.Instance.GetRandomRarity();

    // Exact rarity first
    var pool = available
        .Where(a => (RarityType)a.rarity == rolled)
        .ToList();

    if (pool.Count > 0)
        return pool[Random.Range(0, pool.Count)];

    // Fallback downward
    for (int r = (int)rolled - 1; r >= 0; r--)
    {
        pool = available
            .Where(a => a.rarity == r)
            .ToList();

        if (pool.Count > 0)
            return pool[Random.Range(0, pool.Count)];
    }

    // Fallback upward
    for (int r = (int)rolled + 1; r <= 3; r++)
    {
        pool = available
            .Where(a => a.rarity == r)
            .ToList();

        if (pool.Count > 0)
            return pool[Random.Range(0, pool.Count)];
    }

    // Final fallback
    return available[Random.Range(0, available.Count)];
}

}
