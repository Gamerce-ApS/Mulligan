using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class GameDataLoader : Singleton<GameDataLoader>
{
    public string jsonUrl = "http://gamerce.net/mulligan/data.json";
    public CardDataExportWrapper cardDataObject;

    void Start()
    {

    }
    public void LoadGameData(System.Action onComplete)
    {
        StartCoroutine(LoadJsonFromServer(onComplete));
    }
    IEnumerator LoadJsonFromServer(System.Action onComplete)
    {
        UnityWebRequest www = UnityWebRequest.Get(jsonUrl);
        www.timeout = 10;
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load game data: " + www.error);
            onComplete.Invoke();
            yield break;
        }

        string json = www.downloadHandler.text;
        PlayerPrefs.SetString("GameData", json);
        PlayerPrefs.Save();
        yield return null;
        Debug.Log("Game data loaded and applied.");



        onComplete.Invoke();
    }
}
