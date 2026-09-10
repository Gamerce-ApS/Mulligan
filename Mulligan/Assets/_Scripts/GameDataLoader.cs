using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using GameAnalyticsSDK;

public class GameDataLoader : Singleton<GameDataLoader>
{
    public string jsonUrl = "http://gamerce.net/mulligan/data.json";
    public bool useLocalData = false;
    public GameObject LoadingWindow;
    public TMPro.TMP_Text LoadingLabel;
    public float LoadingTextInterval = 0.35f;

    private Coroutine loadingTextRoutine;
    void Start()
    {

    }
    public void LoadGameData(System.Action onComplete)
    {
        if(useLocalData)
        {
            TrackDesign("data:load:local");
            PlayerPrefs.DeleteKey("GameData");
            HideLoadingWindow();
            onComplete.Invoke();
            return;
        }
        StartCoroutine(LoadJsonFromServer(onComplete));
    }
    IEnumerator LoadJsonFromServer(System.Action onComplete)
    {
        ShowLoadingWindow();
        TrackDesign("data:load:start");
        TrackCacheState();
        float startTime = Time.realtimeSinceStartup;
        UnityWebRequest www = UnityWebRequest.Get(jsonUrl);
        www.timeout = 10;
        yield return www.SendWebRequest();

        float elapsed = Time.realtimeSinceStartup - startTime;
        if (www.result != UnityWebRequest.Result.Success)
        {
            TrackDesign("data:load:fail", elapsed);
            TrackLoadFailReason(www);
            Debug.LogError("Failed to load game data: " + www.error);
            HideLoadingWindow();
            onComplete.Invoke();
            yield break;
        }

        string json = www.downloadHandler.text;
        PlayerPrefs.SetString("GameData", json);
        PlayerPrefs.Save();
        TrackDesign("data:load:success", elapsed);
        yield return null;
        Debug.Log("Game data loaded and applied.");



        HideLoadingWindow();
        onComplete.Invoke();
    }

    private void ShowLoadingWindow()
    {
        if (LoadingWindow != null)
            LoadingWindow.SetActive(true);

        if (LoadingLabel != null)
            LoadingLabel.text = "Loading.";

        if (loadingTextRoutine != null)
            StopCoroutine(loadingTextRoutine);

        loadingTextRoutine = StartCoroutine(AnimateLoadingText());
    }

    private void HideLoadingWindow()
    {
        if (loadingTextRoutine != null)
        {
            StopCoroutine(loadingTextRoutine);
            loadingTextRoutine = null;
        }

        if (LoadingWindow != null)
            LoadingWindow.SetActive(false);
    }

    private IEnumerator AnimateLoadingText()
    {
        int dotCount = 1;

        while (true)
        {
            if (LoadingLabel != null)
                LoadingLabel.text = "Loading" + new string('.', dotCount);

            dotCount++;
            if (dotCount > 3)
                dotCount = 1;

            yield return new WaitForSecondsRealtime(LoadingTextInterval);
        }
    }

    private void TrackCacheState()
    {
        if (PlayerPrefs.HasKey("GameData"))
            TrackDesign("data:load:cached_exists");
        else
            TrackDesign("data:load:no_cache");
    }

    private void TrackLoadFailReason(UnityWebRequest www)
    {
        if (www == null)
        {
            TrackDesign("data:load:fail:unknown");
            return;
        }

        if (!string.IsNullOrEmpty(www.error) && www.error.ToLower().Contains("timeout"))
            TrackDesign("data:load:fail:timeout");
        else if (www.result == UnityWebRequest.Result.ConnectionError)
            TrackDesign("data:load:fail:connection");
        else if (www.result == UnityWebRequest.Result.ProtocolError)
            TrackDesign("data:load:fail:protocol");
        else if (www.result == UnityWebRequest.Result.DataProcessingError)
            TrackDesign("data:load:fail:data_processing");
        else
            TrackDesign("data:load:fail:unknown");
    }

    private void TrackDesign(string eventName)
    {
        if (GameAnalytics.Initialized == false)
            GameAnalytics.Initialize();

        GameAnalytics.NewDesignEvent(eventName);
    }

    private void TrackDesign(string eventName, float value)
    {
        if (GameAnalytics.Initialized == false)
            GameAnalytics.Initialize();

        GameAnalytics.NewDesignEvent(eventName, value);
    }
}
