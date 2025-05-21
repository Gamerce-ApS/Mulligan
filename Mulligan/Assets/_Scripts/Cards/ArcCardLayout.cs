using System.Collections.Generic;
using UnityEngine;

public class ArcCardLayout : Singleton<ArcCardLayout>
{
    public float radius = 300f;
    public float maxAngle = 30f; // Total arc angle (e.g. 30 degrees)

    public void UpdateCardLayout()
    {
        int childCount = transform.childCount;
        if (childCount == 0) return;

        // Clamp max spread: use smaller angle for fewer cards
        float dynamicAngle = Mathf.Lerp(5f, maxAngle, Mathf.InverseLerp(1, 8, childCount));
        float angleStep = (childCount > 1) ? dynamicAngle / (childCount - 1) : 0f;
        float startAngle = -dynamicAngle / 2f;

        for (int i = 0; i < childCount; i++)
        {
            Transform card = transform.GetChild(i);

            float angle = startAngle + angleStep * i;
            float radians = angle * Mathf.Deg2Rad;

            Vector2 offset = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * radius;

            Vector3 targetRotation = new Vector3(0f, 0f, -angle);

                card.localPosition = offset;
                card.localRotation = Quaternion.Euler(0, 0, -angle);

  



        }
    }
    [ContextMenu("Update Arc Layout")]
    private void ContextMenuUpdate()
    {
        UpdateCardLayout();
    }
    private void OnEnable()
    {
        UpdateCardLayout();
    }

    [ContextMenu("Fill Empty Slots")]
    public void FillEmpty()
    {
        int desiredCount = 8;

        // STEP 1: Cache all existing non-empty cards
        List<Transform> existingCards = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name != "EmptySlot")
            {
                existingCards.Add(child);
            }
        }

        // STEP 2: Detach existingCards before clearing
        foreach (var card in existingCards)
        {
            card.SetParent(null); // detach safely
        }

        // STEP 3: Clear all children (empty slots or old structure)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // STEP 4: Calculate padding
        int numCards = existingCards.Count;
        int numEmpty = desiredCount - numCards;
        int before = numEmpty / 2;
        int after = numEmpty - before;

        // STEP 5: Add empty slots before
        for (int i = 0; i < before; i++)
        {
            CreateEmptySlot();
        }

        // STEP 6: Add the actual cards
        foreach (var card in existingCards)
        {
            card.SetParent(transform);
            card.SetAsLastSibling();
        }

        // STEP 7: Add empty slots after
        for (int i = 0; i < after; i++)
        {
            CreateEmptySlot();
        }

            UpdateCardLayout(); // optional


    }

    private void CreateEmptySlot()
    {
        GameObject empty = new GameObject("EmptySlot");
        RectTransform rt = empty.AddComponent<RectTransform>();
        rt.SetParent(transform);
        rt.localScale = Vector3.one;
        rt.localPosition = Vector3.zero;
        rt.sizeDelta = new Vector2(150, 200); // visually match cards
    }


}
