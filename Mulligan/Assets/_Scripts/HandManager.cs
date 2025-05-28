using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandManager : Singleton<HandManager>
{
    //TODO
    // Reads deck from CardContainer
    // Pulls cards and populates it in the hand

    public List<CardInstance> CurrentHand = new List<CardInstance>();
    public List<CardInstance> PlayedHand = new List<CardInstance>();

    public GameObject CardPrefab;
    public Transform PlayAreaTransform;
    // Start is called before the first frame update
    public void Init()
    {
        DrawHand();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void RankUpRandom()
    {
        if (CurrentHand.Count > 0)
        {
            var randomCard = CurrentHand[Random.Range(0, CurrentHand.Count)];
            randomCard.UpgradeRank();
            UIManager.Instance.ShowTooltip("1 Unit Ranked Up!");
        }
    }
    public void DrawHand()
    {
        for(int i = 0; i< 8;i++)
        {
            CardInstance cardInstance = CardContainer.Instance.DrawCard();
            CurrentHand.Add(cardInstance);

            Card c = GameObject.Instantiate(CardPrefab, ArcCardLayout.Instance.transform).GetComponent<Card>();
            c.gameObject.SetActive(true);
            cardInstance.CardGO = c;
            c.Init(cardInstance);
        }

        ArcCardLayout.Instance.UpdateCardLayout();
    }
    public void PlayHand()
    {
        for (int i = CurrentHand.Count - 1; i >= 0; i--)
        {
            var cardInstance = CurrentHand[i];
            Card cardGO = cardInstance.CardGO;

            if (cardGO.isSelected)
            {
                PlayedHand.Add(cardInstance);
                CurrentHand.RemoveAt(i); // remove from hand list
            }
        }

        for (int i = 0; i < PlayedHand.Count; i++)
        {
            AnimateCardToPlayArea(PlayedHand[i].CardGO.gameObject, PlayedHand.Count-i, PlayedHand.Count);
        }
        UIManager.Instance.ShiftUI(-250f);
        ArcCardLayout.Instance.FillEmpty();

        int totalDmg;
        List<CardInstance> boostedCards = EvaluatorManager.Instance.EvaluateHand(PlayedHand, out totalDmg);


        UIManager.Instance.DamageReset();

        //Delay so lifting works
        UnityHelper.RunAfterDelay(this, 0.6f, () =>
        {
            for (int i = 0; i < boostedCards.Count; i++)
            {
                boostedCards[i].CardGO.LiftCard();
            }

            // Delay after the lift start evaluation
            UnityHelper.RunAfterDelay(this, 0.6f, () =>
            {
                // Begin chain
                if (boostedCards.Count > 0)
                    EvaluatorManager.Instance.PlayBoostedCardsSequentially(boostedCards,-1);
                else
                    DiscardHand(); // nothing boosted, skip straight to discard

            });
        });


        // Show total damage somewhere
        //UIManager.Instance.ShowDamage(totalDmg);

    }
    public void DiscardHand()
    {
        Vector3 discardTarget = UIManager.Instance.DiscardPileIcon.transform.position; // or anywhere off-screen

        for (int i = 0; i < PlayedHand.Count; i++)
        {
            PlayedHand[i].CardGO.FlyAwayAndDiscard(discardTarget, i * 0.1f, PlayedHand[i]); // staggered delay
        }
        UnityHelper.RunAfterDelay(this, 1.0f, () =>
        {
            UIManager.Instance.ShiftUI(250f, () => { PlayedHand.Clear(); RefillHand(); GameManager.Instance.FinishRound(); });
            
        });
    }
    public void RefillHand()
    {
        int needed = 8 - CurrentHand.Count;

        StartCoroutine(AnimateRefillHand(needed));
    }
    private IEnumerator AnimateRefillHand(int numberToDraw)
    {
        Vector3 worldSpawn = UIManager.Instance.DiscardPileIcon.transform.position;

        // 1. Generate empty slots
        ArcCardLayout.Instance.FillEmpty();
        yield return new WaitForEndOfFrame();

        // 2. Collect the empty slots
        List<Transform> emptySlots = new List<Transform>();
        foreach (Transform child in ArcCardLayout.Instance.transform)
        {
            if (child.name == "EmptySlot")
                emptySlots.Add(child);
        }

        for (int i = 0; i < numberToDraw && i < emptySlots.Count; i++)
        {
            Transform slot = emptySlots[i];

            // 3. Draw and create real card (not yet parented to layout)
            CardInstance cardInstance = CardContainer.Instance.DrawCard();
            CurrentHand.Add(cardInstance);

            GameObject realCard = Instantiate(CardPrefab, UIManager.Instance.thCanvas.transform); // stays in Canvas space
            RectTransform cardRT = realCard.GetComponent<RectTransform>();
            cardRT.position = worldSpawn;
            cardRT.localScale = Vector3.zero;

            Card cardScript = realCard.GetComponent<Card>();
            cardInstance.CardGO = cardScript;
            cardScript.Init(cardInstance);

            // 4. Animate to slot
            LeanTween.scale(realCard, new Vector3(0.9f, 0.9f, 0.9f), 0.3f).setEaseOutBack();
            LeanTween.rotateLocal(realCard, slot.rotation.eulerAngles, 0.4f).setEaseOutCubic();
            LeanTween.move(realCard, slot.position, 0.4f).setEaseOutCubic().setOnComplete(() =>
            {
                // Destroy slot
                int siblingIndex = slot.transform.GetSiblingIndex();
                Destroy(slot.gameObject);

                // Re-parent into layout
                cardRT.SetParent(ArcCardLayout.Instance.transform, true);
                cardRT.localScale = Vector3.one;
                cardRT.transform.SetSiblingIndex(siblingIndex);
            });

            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(0.6f);
        ArcCardLayout.Instance.UpdateCardLayout(); // fan them out
    }


    private void AnimateCardToPlayArea(GameObject card, int index, int total)
    {
        RectTransform cardRT = card.GetComponent<RectTransform>();

        Vector3 worldPos = cardRT.position;
        cardRT.SetParent(PlayAreaTransform, false);
        cardRT.position = worldPos;

        float spacing = 275f;
        float totalWidth = spacing * (total - 1);
        Vector2 targetAnchoredPos = new Vector2(index * spacing - totalWidth / 2f, 0f);

        float duration = 0.4f;

        LeanTween.value(card, cardRT.anchoredPosition, targetAnchoredPos, duration)
            .setEaseOutCubic()
            .setOnUpdate((Vector2 val) => { cardRT.anchoredPosition = val; });

        LeanTween.rotateZ(cardRT.gameObject, 0f, duration);
    }
    public int SelectedCardCount()
    {
        int count = 0;
        foreach (var card in CurrentHand)
        {
            if (card.CardGO != null && card.CardGO.isSelected)
                count++;
        }
        return count;
    }

}
