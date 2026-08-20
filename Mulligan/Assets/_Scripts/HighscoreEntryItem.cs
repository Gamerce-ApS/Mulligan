using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HighscoreEntryItem : MonoBehaviour
{
    public TMP_Text Rank;
    public TMP_Text PlayerName;
    public Image HeroPortrait;
    public TMP_Text Level;
    public TMP_Text TopHit;

    public void Init(LeaderboardEntryData data)
    {
        if (data == null)
            return;

        Init(data.Rank, data.PlayerName, data.HeroIndex, data.LevelReached, data.TopHit);
    }

    public void Init(int rank, string playerName, int heroIndex, int level, long topHit)
    {
        if (Rank != null)
            Rank.text = "#" + rank;

        if (PlayerName != null)
            PlayerName.text = playerName;

        if (Level != null)
            Level.text = level.ToString();

        if (TopHit != null)
            TopHit.text = topHit.ToString();

        if (HeroPortrait != null)
            HighscoreManager.Instance.ApplyHeroPortrait(HeroPortrait, heroIndex);
    }
}
