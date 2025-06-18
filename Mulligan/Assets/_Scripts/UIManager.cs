using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    public TMPro.TMP_Text AttackLabel;
    public TMPro.TMP_Text ReRollLabel;
    public TMPro.TMP_Text RoundsLabel;
    public TMPro.TMP_Text WorldLabel;
    public TMPro.TMP_Text GoldLabel;

    public Canvas thCanvas;

    public GameObject ArtifactSlotTemplate; // prefab with icon image
    public Transform ArtifactSlotParent; // grid/horizontal layout holder


    public GameObject PotionSlotTemplate; // prefab with icon image
    public Transform PotionSlotParent; // grid/horizontal layout holder


    public RectTransform BuyItemArea;

    // Start is called before the first frame update
    public void Init()
    {
        DamageReset();
    }
    public void DamageReset()
    {
        DamageLabel.GetComponent<TMPro.TMP_Text>().text = "0";
        CriticalLabel.GetComponent<TMPro.TMP_Text>().text = "0";
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    public void UpdateLabels()
    {
        AttackLabel.text = GameData.CurrentAttacks.ToString();
        ReRollLabel.text = GameData.CurrentReRolls.ToString();
        RoundsLabel.text = "Round "+GameData.CurrentRound.ToString();
        WorldLabel.text = "1/8";
        GoldLabel.text = GameData.CurrentGold.ToString();

    }
    public void ClickPlayHand()
    {
        HandManager.Instance.PlayHand();
        UIManager.Instance.HideCardInfoPopup();
    }
    public void ClickReRoll()
    {
        if(GameData.CurrentReRolls >0)
        {
            GameData.CurrentReRolls--;
        }
    }
    public void ClickContinueFromShop()
    {
        ShopManager.Instance.HideShopWindow();
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
    public void RefreshPreDamage()
    {
        Dictionary<CardRace, int> raceCounts = new();
        Dictionary<CardClass, int> classCounts = new();
        List<CardInstance> selectedCards = new List<CardInstance>();
        foreach (var cardInstance in HandManager.Instance.CurrentHand)
        {
            if (cardInstance.CardGO == null || !cardInstance.CardGO.isSelected) continue;
            selectedCards.Add(cardInstance);
            var data = cardInstance.data;

            if (!raceCounts.ContainsKey(data.race)) raceCounts[data.race] = 0;
            raceCounts[data.race]++;

            if (!classCounts.ContainsKey(data.cardClass)) classCounts[data.cardClass] = 0;
            classCounts[data.cardClass]++;
        }


        int totalDmg;
        List<CardInstance> boostedCards = EvaluatorManager.Instance.EvaluateHand(selectedCards, out totalDmg);

        foreach(var card in boostedCards)
        {
            int synergyDMG = EvaluatorManager.Instance.GetSynergyDamage(card, selectedCards);
            totalDmg += synergyDMG;
        }

        TMPro.TMP_Text text = DamageLabel.GetComponent<TMPro.TMP_Text>();
        int prevValue = int.Parse(text.text);
        if(totalDmg < prevValue)
            UnityHelper.AnimateTMPColorTransition(text, new Color(1f, 0.2f, 0.2f, 1f), new Color(0.866f, 0.757f, 0.573f, 1f), 0.5f);
        else if(totalDmg > prevValue)
            UnityHelper.AnimateTMPColorTransition(text, new Color(0.18f, 0.70f, 0.14f, 1f), new Color(0.866f, 0.757f, 0.573f, 1f), 0.5f);

        DamageLabel.GetComponent<TMPro.TMP_Text>().text = (totalDmg).ToString();
        if(totalDmg != 0)
        LeanTween.scale(DamageLabel, Vector3.one * 1.3f, 0.5f).setEasePunch();


        int crit = EvaluatorManager.Instance.GetGlobalCritMultiplier(selectedCards);
        CriticalLabel.GetComponent<TMPro.TMP_Text>().text = (crit).ToString();
        if(crit != 0)
        LeanTween.scale(CriticalLabel, Vector3.one * 1.3f, 0.5f).setEasePunch();
    }
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
    public GameObject TooltipPrefab;
    public void ShowTooltip(string message)
    {
        GameObject tooltip = Instantiate(TooltipPrefab, thCanvas.transform);
        TMPro.TMP_Text text = tooltip.GetComponentInChildren<TMPro.TMP_Text>();
        RectTransform rt = tooltip.GetComponent<RectTransform>();

        text.text = message;

        // Position it in center-bottom or wherever you want
        rt.anchoredPosition = new Vector2(0, -300); // adjust to your canvas
        rt.localScale = Vector3.one;

        CanvasGroup cg = tooltip.GetComponent<CanvasGroup>() ?? tooltip.AddComponent<CanvasGroup>();
        cg.alpha = 0;

        // Fade in + float up
        LeanTween.value(tooltip, 0f, 1f, 0.2f)
            .setOnUpdate((float val) => cg.alpha = val);

        LeanTween.moveY(rt, rt.anchoredPosition.y + 40f, 1f).setEaseOutCubic();

        // Fade out
        LeanTween.delayedCall(tooltip, 1f, () =>
        {
            LeanTween.value(tooltip, 1f, 0f, 0.3f)
                .setOnUpdate((float val) => cg.alpha = val)
                .setOnComplete(() => Destroy(tooltip));
        });
    }
    public void UpdateArtifactSlotsUI()
    {
        foreach (Transform child in ArtifactSlotParent)
        {
            if (child != ArtifactSlotTemplate.transform)
                Destroy(child.gameObject);
        }

        foreach (var artifact in ArtifactManager.Instance.ActiveArtifacts)
        {
            Artifact slot = Instantiate(ArtifactSlotTemplate, ArtifactSlotParent).GetComponent<Artifact>();
            slot.gameObject.SetActive(true);
            slot.ArtifactData = artifact;
            slot.Init(artifact);
            //slot.transform.Find("Icon").GetComponent<Image>().sprite = artifact.icon;
            // Optionally add tooltip or highlight here
        }
        UpdatePotionsSlotsUI();
    }
    public void UpdatePotionsSlotsUI()
    {
        foreach (Transform child in PotionSlotParent)
        {
            if (child != PotionSlotTemplate.transform)
                Destroy(child.gameObject);
        }

        foreach (var pot in PotionManager.Instance.ActivePotions)
        {
            Potion slot = Instantiate(PotionSlotTemplate, PotionSlotParent).GetComponent<Potion>();
            slot.gameObject.SetActive(true);
            slot.PotionData = pot;
            slot.Init(pot);
            //slot.transform.Find("Icon").GetComponent<Image>().sprite = artifact.icon;
            // Optionally add tooltip or highlight here
        }
    }
    public Artifact GetVisualArtifact(ArtifactData aArtifactData)
    {
        foreach (Transform child in ArtifactSlotParent)
        {
            if (child.GetComponent<Artifact>().ArtifactData == aArtifactData)
                return child.GetComponent<Artifact>();

        }
        return null;
    }
    public GameObject CardInfoPopupPrefab;
    private GameObject activeInfoPopup;
    public Transform currentTransform;
    public void ShowCardInfoPopup(string title, string description, string text2, Transform target)
    {
        if (activeInfoPopup != null) Destroy(activeInfoPopup);

        currentTransform = target;
        GameObject popup = Instantiate(CardInfoPopupPrefab, thCanvas.transform);
        activeInfoPopup = popup;

        TMP_Text titleText = popup.transform.Find("Title").GetComponent<TMP_Text>();
        TMP_Text descText = popup.transform.Find("Description").GetComponent<TMP_Text>();
        TMP_Text text2Text = popup.transform.Find("Text2").GetComponent<TMP_Text>();

        //Image iconImage = popup.transform.Find("Icon").GetComponent<Image>();

        title = title.Replace("\n", " ");
        titleText.text = title;
        descText.text = description;
        text2Text.text = text2;
        //iconImage.sprite = icon;

        RectTransform popupRT = popup.GetComponent<RectTransform>();

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, target.position);
        popupRT.transform.position = target.transform.position;

        if(screenPos.y < Screen.height * 0.5f)
            popupRT.transform.position += new Vector3(0, 35,0);
        else
            popupRT.transform.position += new Vector3(0, -30, 0);

        // Fade in
        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        cg.alpha = 0;
        LeanTween.alphaCanvas(cg, 1f, 0.2f);
    }
    public void HideCardInfoPopup()
    {
        if (activeInfoPopup != null)
        {
            Destroy(activeInfoPopup);
            activeInfoPopup = null;
        }
        currentTransform = null;
    }
    private string[] funMessages = new string[]
{
        "- You did great!",
        "- Victory is yours!",
        "- The crowd goes wild!",
        "- Another step to glory!",
        "- You're unstoppable!",
        "- Hero of the realm!",
        "- You crushed it!"
};
    public TMP_Text VictoryFunText;
    public GameObject VictoryParent;
    public void ShowVictoryScreen(System.Action onComplete)
    {
        VictoryParent.GetComponent<CanvasGroup>().alpha = 0f;
        VictoryParent.SetActive(true);

        // Pick a fun message
        VictoryFunText.text = funMessages[UnityEngine.Random.Range(0, funMessages.Length)];

        // Fade in
        LeanTween.alphaCanvas(VictoryParent.GetComponent<CanvasGroup>(), 1f, 0.3f).setEaseOutQuad().setOnComplete(() =>
        {
            // Animate children
            StartCoroutine(AnimateChildrenIn(onComplete));
        });
        foreach (Transform child in VictoryParent.transform)
        {
            child.gameObject.SetActive(false);
        }

    }
    private IEnumerator AnimateChildrenIn(System.Action onComplete)
    {
        int i = 0;
        foreach (Transform child in VictoryParent.transform)
        {
            child.gameObject.SetActive(true);
            Vector3 targetScale = child.localScale;
            child.localScale = Vector3.zero;
            LeanTween.scale(child.gameObject, targetScale, 0.5f).setEaseOutBack().setDelay(i * 0.1f);
            i++;
        }

        yield return new WaitForSeconds(2.5f);

        onComplete?.Invoke();
        LeanTween.alphaCanvas(VictoryParent.GetComponent<CanvasGroup>(), 0f, 0.3f).setEaseOutQuad().setOnComplete(() =>
        {

            VictoryParent.SetActive(false);
        });

    }

}
