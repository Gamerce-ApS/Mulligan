using UnityEditor;
using UnityEngine;

public static class MulliganRushEditorTools
{
    [MenuItem("Tools/Mulligan Rush/Completet tutorial")]
    public static void CompleteTutorial()
    {
        PlayerPrefs.SetInt("HasRunTutorial", 1);
        PlayerPrefs.Save();
        Debug.Log("Mulligan Rush: Tutorial marked as completed.");
    }
}
