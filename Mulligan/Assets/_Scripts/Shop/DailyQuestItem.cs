using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyQuestItem : MonoBehaviour
{
    public TMP_Text QuestText;
    public TMP_Text ProgressText;
    public Image ProgressBar;

    public void Init(string questText, int progress, int target, bool completed)
    {
        if (QuestText != null)
            QuestText.text = questText;

        int clampedProgress = Mathf.Clamp(progress, 0, target);

        if (ProgressText != null)
            ProgressText.text = completed ? "Done" : clampedProgress + " / " + target;

        if (ProgressBar != null)
            ProgressBar.fillAmount = target <= 0 ? 1f : (float)clampedProgress / target;
    }
}
