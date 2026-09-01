using System;
using UnityEngine;

public class GAIronSourceIntegration
{
#if (gameanalytics_levelplay_enabled || gameanalytics_ironsource_enabled) && !(UNITY_EDITOR)
    private static bool _subscribed = false;
#endif

    public static void ListenForImpressions(Action<string> callback)
    {
#if UNITY_EDITOR
#elif gameanalytics_levelplay_enabled
        if (_subscribed)
        {
            Debug.Log("Ignoring duplicate gameanalytics subscription");
            return;
        }

        // LevelPlay.OnImpressionDataReady fires on a background thread, forward to the Unity main thread
        System.Threading.SynchronizationContext unityContext = System.Threading.SynchronizationContext.Current;
        Unity.Services.LevelPlay.LevelPlay.OnImpressionDataReady += (impressionData) =>
        {
            if (impressionData == null)
            {
                return;
            }

            string allData = impressionData.AllData;
            if (unityContext != null)
            {
                unityContext.Post(_ => callback(allData), null);
            }
            else
            {
                Debug.LogWarning("GameAnalytics: no main thread context captured, dropping LevelPlay impression");
            }
        };
        _subscribed = true;
#elif gameanalytics_ironsource_enabled
        if (_subscribed)
        {
            Debug.Log("Ignoring duplicate gameanalytics subscription");
            return;
        }

        IronSourceEvents.onImpressionDataReadyEvent += (arg1) => callback(arg1.allData);
        _subscribed = true;
#else
        Debug.LogWarning("GameAnalytics: no IronSource/LevelPlay SDK detected in the project, ILRD impressions will not be sent");
#endif

    }
}
