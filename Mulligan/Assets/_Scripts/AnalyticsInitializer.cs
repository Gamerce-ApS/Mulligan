using System;
using GameAnalyticsSDK;
using Unity.Services.Core;
using Unity.Services.Analytics;
using UnityEngine;

public class AnalyticsInitializer : MonoBehaviour
{
    async void Awake()
    {
        DontDestroyOnLoad(gameObject);

        try
        {
            await UnityServices.InitializeAsync();

            AnalyticsService.Instance.StartDataCollection();

            if (GameAnalytics.Initialized == false)
                GameAnalytics.Initialize();

            Debug.Log("Unity Analytics initialized");
        }
        catch (Exception e)
        {
            Debug.LogError("Unity Analytics init failed: " + e);
        }
    }
}
