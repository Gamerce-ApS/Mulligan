using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    //TODO
    //Handles all clicks from buttons

    public RectTransform UIShiftGroup;
    public GameObject DamageFloatPrefab;
    public GameObject DamageLabel;
    public GameObject CriticalLabel;
    public GameObject DiscardPileIcon;

    public Canvas thCanvas;

    // Start is called before the first frame update
    public void Init()
    {
        DamageLabel.GetComponent<TMPro.TMP_Text>().text = "0";
        CriticalLabel.GetComponent<TMPro.TMP_Text>().text = "0";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ClickPlayHand()
    {
        HandManager.Instance.PlayHand();
    }
    public void ClickReRoll()
    {

    }
    public void AddDamage(float aDamage)
    {
        DamageLabel.GetComponent<TMPro.TMP_Text>().text = (float.Parse(DamageLabel.GetComponent<TMPro.TMP_Text>().text) + aDamage).ToString();
        LeanTween.scale(DamageLabel, Vector3.one * 1.3f, 0.5f).setEasePunch();
    }
    public void AddCritical(float aDamage)
    {
        CriticalLabel.GetComponent<TMPro.TMP_Text>().text = (float.Parse(CriticalLabel.GetComponent<TMPro.TMP_Text>().text) + aDamage).ToString();
        LeanTween.scale(CriticalLabel, Vector3.one * 1.3f, 0.5f).setEasePunch();
    }
    public void ShiftUI(float offsetY, System.Action onComplete=null)
    {
        if (UIShiftGroup == null) return;

        Vector2 startPos = UIShiftGroup.anchoredPosition;
        Vector2 targetPos = startPos + new Vector2(0, offsetY);

        LeanTween.value(gameObject, startPos, targetPos, 0.3f)
            .setEaseOutQuad()
            .setOnUpdate((Vector2 val) => {
                UIShiftGroup.anchoredPosition = val;
            }).setOnComplete(onComplete);
    }
    public GameObject SynergiTemplate;

    public void ShowSynergies()
    {
        Transform parent = SynergiTemplate.transform.parent;

        // 1. Destroy all old synergy UIs (except the template)
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (child != SynergiTemplate)
            {
                Destroy(child);
            }
        }

        // 2. Count synergies from selected cards
        Dictionary<CardRace, int> raceCounts = new();
        Dictionary<CardClass, int> classCounts = new();

        foreach (var cardInstance in HandManager.Instance.CurrentHand)
        {
            if (cardInstance.CardGO == null || !cardInstance.CardGO.isSelected) continue;

            var data = cardInstance.data;

            if (!raceCounts.ContainsKey(data.race)) raceCounts[data.race] = 0;
            raceCounts[data.race]++;

            if (!classCounts.ContainsKey(data.cardClass)) classCounts[data.cardClass] = 0;
            classCounts[data.cardClass]++;
        }

        // 3. Create UI items for each synergy
        foreach (var kvp in raceCounts)
        {
            CreateSynergyItem($"Race: {kvp.Key}", kvp.Value, 4, CardContainer.Instance.GetSpriteForRace(kvp.Key),true);
        }

        foreach (var kvp in classCounts)
        {
            CreateSynergyItem($"Class: {kvp.Key}", kvp.Value, 4, CardContainer.Instance.GetSpriteForClass(kvp.Key),false);
        }
    }

    private void CreateSynergyItem(string key, int count, int max, Sprite iconSprite, bool isRace)
    {
        if (count == 0) return;

        GameObject item = Instantiate(SynergiTemplate, SynergiTemplate.transform.parent);
        item.SetActive(true);
        item.name = key; // So "Race: Orc" or "Class: Mage"

        TMPro.TMP_Text countText = item.GetComponentInChildren<TMPro.TMP_Text>();
        UnityEngine.UI.Image iconRace = item.transform.Find("IconRace")?.GetComponent<UnityEngine.UI.Image>();
        UnityEngine.UI.Image iconClass = item.transform.Find("IconClass")?.GetComponent<UnityEngine.UI.Image>();

        // Set correct icon
        if (isRace)
        {
            iconRace.enabled = true;
            iconClass.enabled = false;
            if (iconRace != null) iconRace.sprite = iconSprite;
        }
        else
        {
            iconRace.enabled = false;
            iconClass.enabled = true;
            if (iconClass != null) iconClass.sprite = iconSprite;
        }

        // Format synergy text
        string displayText = "";
        if (count == 1)
            displayText = "1/2";
        else if (count == 2)
            displayText = "2";
        else if (count == 3)
            displayText = "3/4";
        else
            displayText = "4";

        countText.text = displayText;

        // Highlight if full synergy (2 or 4)
        bool isFull = (count == 2 || count >= 4);
        if (isFull)
        {
            countText.color = new Color(1f, 0.84f, 0.2f); // Gold color

            // Scale pulse
            Vector3 originalScale = SynergiTemplate.transform.localScale;
            item.transform.localScale = originalScale;
            LeanTween.scale(item, originalScale * 1.3f, 0.5f).setEasePunch();

            // Enable glow
            var glow = item.transform.Find("Glow");
            if (glow != null) glow.gameObject.SetActive(true);

            if (glow != null)
            {
                glow.gameObject.SetActive(true);
                CanvasGroup cg = glow.GetComponent<CanvasGroup>() ?? glow.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0;
                LeanTween.alphaCanvas(cg, 1f, 0.3f).setEaseOutCubic();
            }
        }
        else
        {
            countText.color = new Color32(0xFA, 0xE3, 0xBC, 255); ;
            item.transform.localScale = SynergiTemplate.transform.localScale;

            // Disable glow
            var glow = item.transform.Find("Glow");
            if (glow != null) glow.gameObject.SetActive(false);
        }

 
    }
    public void PulseSynergyItem(string keyName)
    {
        Transform parent = SynergiTemplate.transform.parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (child == SynergiTemplate) continue;

            if (child.name.Contains(keyName)) // match based on synergy key
            {
                LeanTween.scale(child, child.transform.localScale * 1.3f, 0.5f).setEasePunch();
            }
        }
    }
    public void ClearSynergies()
    {
        Transform parent = SynergiTemplate.transform.parent;

        // 1. Destroy all old synergy UIs (except the template)
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (child != SynergiTemplate)
            {
                Destroy(child);
            }
        }
    }




}
