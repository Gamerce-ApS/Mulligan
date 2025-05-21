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
    public static List<T> ToList<T>(this T[] array)
    {
        return new List<T>(array);
    }
}

