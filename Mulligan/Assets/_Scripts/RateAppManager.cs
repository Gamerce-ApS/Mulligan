using System;
using System.Collections;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Review;
#endif
using UnityEngine;

public class RateAppManager : Singleton<RateAppManager>
{
    private const string TotalWinsKey = "RateApp_TotalWins";
    private const string LastPromptWinKey = "RateApp_LastPromptWin";
    private const string LastPromptTicksKey = "RateApp_LastPromptTicks";

    public bool ReviewsEnabled = true;
    public bool OnlyAfterTutorial = true;
    public int WinsBeforeFirstPrompt = 3;
    public int WinsBetweenPrompts = 8;
    public int DaysBetweenPrompts = 3;
    public float PromptDelay = 1.5f;
    public bool OpenStorePageIfNativeReviewUnavailable = false;

    [Header("Store Links")]
    public string IOSAppStoreId = "6760552140";
    public string AndroidPackageNameOverride = "";

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
            DontDestroyOnLoad(gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstanceIfMissing()
    {
        if (FindObjectOfType<RateAppManager>() != null)
            return;

        GameObject go = new GameObject("RateAppManager");
        go.AddComponent<RateAppManager>();
    }

    public void RegisterWinAndMaybeRequestReview()
    {
        if (ReviewsEnabled == false)
            return;

        if (OnlyAfterTutorial && TutorialController.Instance.HasRunTutorial() == false)
            return;

        int totalWins = PlayerPrefs.GetInt(TotalWinsKey, 0) + 1;
        PlayerPrefs.SetInt(TotalWinsKey, totalWins);

        if (ShouldRequestReview(totalWins) == false)
        {
            PlayerPrefs.Save();
            return;
        }

        PlayerPrefs.SetInt(LastPromptWinKey, totalWins);
        PlayerPrefs.SetString(LastPromptTicksKey, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();

        StartCoroutine(RequestReviewAfterDelay());
    }

    public void RequestReview()
    {
        if (ReviewsEnabled == false)
            return;

#if UNITY_IOS && !UNITY_EDITOR
        bool requested = UnityEngine.iOS.Device.RequestStoreReview();
        if (requested == false && OpenStorePageIfNativeReviewUnavailable)
            OpenStorePageFallback();
#elif UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(RequestAndroidReview());
#else
        if (OpenStorePageIfNativeReviewUnavailable)
            OpenStorePageFallback();
#endif
    }

    public void OpenStorePageFallback()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (string.IsNullOrEmpty(IOSAppStoreId))
        {
            Debug.LogWarning("RateAppManager: IOSAppStoreId is missing.");
            return;
        }

        Application.OpenURL("itms-apps://itunes.apple.com/app/id" + IOSAppStoreId);
#elif UNITY_ANDROID && !UNITY_EDITOR
        string packageName = string.IsNullOrEmpty(AndroidPackageNameOverride) ? Application.identifier : AndroidPackageNameOverride;
        Application.OpenURL("https://play.google.com/store/apps/details?id=" + packageName);
#else
        Debug.Log("RateAppManager: Store page fallback is only available on device builds.");
#endif
    }

    private bool ShouldRequestReview(int totalWins)
    {
        if (totalWins < WinsBeforeFirstPrompt)
            return false;

        int lastPromptWin = PlayerPrefs.GetInt(LastPromptWinKey, 0);
        if (lastPromptWin > 0 && totalWins - lastPromptWin < WinsBetweenPrompts)
            return false;

        string lastPromptTicks = PlayerPrefs.GetString(LastPromptTicksKey, "");
        if (string.IsNullOrEmpty(lastPromptTicks) == false && long.TryParse(lastPromptTicks, out long ticks))
        {
            DateTime lastPromptTime = new DateTime(ticks, DateTimeKind.Utc);
            if ((DateTime.UtcNow - lastPromptTime).TotalDays < DaysBetweenPrompts)
                return false;
        }

        return true;
    }

    private IEnumerator RequestReviewAfterDelay()
    {
        yield return new WaitForSecondsRealtime(PromptDelay);
        RequestReview();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator RequestAndroidReview()
    {
        ReviewManager reviewManager = new ReviewManager();
        var requestFlowOperation = reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;

        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            if (OpenStorePageIfNativeReviewUnavailable)
                OpenStorePageFallback();
            yield break;
        }

        PlayReviewInfo playReviewInfo = requestFlowOperation.GetResult();
        var launchFlowOperation = reviewManager.LaunchReviewFlow(playReviewInfo);
        yield return launchFlowOperation;

        if (launchFlowOperation.Error != ReviewErrorCode.NoError && OpenStorePageIfNativeReviewUnavailable)
            OpenStorePageFallback();
    }
#endif
}
