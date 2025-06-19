using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;

public static class UnityHelper
{
    public static void RunAfterDelay(MonoBehaviour owner, float delay, Action action)
    {
        owner.StartCoroutine(DelayedExecution(delay, action));
    }

    private static IEnumerator DelayedExecution(float delay, Action action)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.deltaTime; // Scaled time
            yield return null;
        }
        action?.Invoke();
    }
    // Shuffles the list in-place
    public static void Shuffle<T>(this IList<T> list)
    {
        int count = list.Count;
        for (int i = 0; i < count - 1; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    public static T GetRandom<T>(this IList<T> list)
    {
        if (list == null || list.Count == 0)
            throw new InvalidOperationException("Cannot get a random element from an empty or null list.");

        int index = UnityEngine.Random.Range(0, list.Count);
        return list[index];
    }
    public static List<T> ToList<T>(this T[] array)
    {
        return new List<T>(array);
    }
    public static void AnimateTMPColorTransition(TMPro.TMP_Text label, Color fromColor, Color toColor, float duration, bool destroyAfter = false)
    {
        if (label == null) return;

        label.color = fromColor;

        LeanTween.value(label.gameObject, 0f, 1f, duration)
            .setOnUpdate((float t) =>
            {
                if (label == null) return;
                label.color = Color.Lerp(fromColor, toColor, t);
            })
            .setOnComplete(() =>
            {
                if (destroyAfter && label != null)
                    GameObject.Destroy(label.gameObject);
            });
    }
}

