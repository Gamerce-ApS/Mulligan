using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class TextLocaliser : MonoBehaviour
{
    public string TextID;
    public TMP_Text TargetText;
    [TextArea(1, 6)] public string DefaultText;

    private void Reset()
    {
        TargetText = GetComponent<TMP_Text>();
        CaptureDefaultText();
    }

    private void Awake()
    {
        if (TargetText == null)
            TargetText = GetComponent<TMP_Text>();

        CaptureDefaultText();
    }

    private void OnEnable()
    {
        LocalizationService.LanguageChanged += RefreshText;
        RefreshText();
    }

    private void OnDisable()
    {
        LocalizationService.LanguageChanged -= RefreshText;
    }

    public void RefreshText()
    {
        if (TargetText == null || string.IsNullOrWhiteSpace(TextID))
            return;

        TargetText.text = LocalizationService.Get(TextID, DefaultText);
    }

    private void CaptureDefaultText()
    {
        if (TargetText != null && string.IsNullOrEmpty(DefaultText))
            DefaultText = TargetText.text;
    }
}
