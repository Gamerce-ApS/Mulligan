using UnityEngine;

public class HighscoreManager : Singleton<HighscoreManager>
{
    private const string MaxLevelKey = "Highscore_MaxLevel_Hero_";
    private const string MaxDamageKey = "Highscore_MaxDamage_Hero_";

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
            DontDestroyOnLoad(gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstanceIfMissing()
    {
        if (FindObjectOfType<HighscoreManager>() != null)
            return;

        GameObject go = new GameObject("HighscoreManager");
        go.AddComponent<HighscoreManager>();
    }

    public void UpdateMaxLevel(int level)
    {
        int heroIndex = GameData.HeroSelected;
        if (level <= GetMaxLevel(heroIndex))
            return;

        PlayerPrefs.SetInt(GetMaxLevelKey(heroIndex), level);
        PlayerPrefs.Save();
    }

    public void UpdateMaxDamage(int damage)
    {
        int heroIndex = GameData.HeroSelected;
        if (damage <= GetMaxDamage(heroIndex))
            return;

        PlayerPrefs.SetInt(GetMaxDamageKey(heroIndex), damage);
        PlayerPrefs.Save();
    }

    public int GetMaxLevel(int heroIndex)
    {
        return PlayerPrefs.GetInt(GetMaxLevelKey(heroIndex), 0);
    }

    public int GetMaxDamage(int heroIndex)
    {
        return PlayerPrefs.GetInt(GetMaxDamageKey(heroIndex), 0);
    }

    private string GetMaxLevelKey(int heroIndex)
    {
        return MaxLevelKey + heroIndex;
    }

    private string GetMaxDamageKey(int heroIndex)
    {
        return MaxDamageKey + heroIndex;
    }
}
