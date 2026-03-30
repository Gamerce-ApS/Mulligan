using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

public class HeroSelectionManager : Singleton<HeroSelectionManager>
{

    public GameObject ShopWindow;



    public List<GameObject> HeroNormal;
    public List<GameObject> HeroPortrait;
    public List<GameObject> HeroSelected;
    public List<GameObject> HeroLock;
    public int selectedHero = -1;
    public Vector3 OriginalScale;
    public TMPro.TMP_Text NameLabel;
    public TMPro.TMP_Text HPLabel;
    public TMPro.TMP_Text ArtifactSlotsLabel;
    public TMPro.TMP_Text PotionSlotsLabel;
    public TMPro.TMP_Text GolfLabel;
     public TMPro.TMP_Text RuneLabel;
    // Start is called before the first frame update
    void Awake()
    {
        startPosition = ShopWindow.GetComponent<RectTransform>().anchoredPosition;
        OriginalScale = HeroNormal[0].transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public Vector3 startPosition;
    System.Action OnHideShop=null;
    public CanvasGroup bgCanvasGroup;
    public void ShowWindow(System.Action onComplete = null)
    {

        bgCanvasGroup.gameObject.SetActive(true);
        bgCanvasGroup.alpha = 0;
        LeanTween.alphaCanvas(bgCanvasGroup, 1f, 0.25f).setEaseOutQuad();

        OnHideShop = onComplete;
        ShopWindow.SetActive(true);
        // Store the target position
        Vector2 targetPos = startPosition;

        // Start below the screen
        ShopWindow.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPos.x, -Screen.height*2);

        // Animate to its original position
        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), targetPos, 0.5f).setEaseOutBack();

        RefreshUI();




    }
    public void RefreshUI()
    {
        if(IAPManager.Instance.IsFullGameUnlocked == false)
        {
            for(int i = 1; i < HeroLock.Count;i++)
            {
                HeroLock[i].SetActive(true);
                HeroLock[i].transform.parent.GetChild(0).GetComponent<Image>().color = new Color(0.5f,0.5f,0.5f,1);
            }
            
            
        }else
        {
            for(int i = 0; i < HeroLock.Count;i++)
            {
                HeroLock[i].SetActive(false);
                HeroLock[i].transform.parent.GetChild(0).GetComponent<Image>().color = new Color(1,1,1,1);

            }
            
        }
    }
    public void HideWindow()
    {
        bgCanvasGroup.alpha = 1;
        LeanTween.alphaCanvas(bgCanvasGroup, 0f, 0.25f).setEaseInQuad();

        // Move downward off the screen
        Vector2 hidePos = new Vector2(ShopWindow.GetComponent<RectTransform>().anchoredPosition.x, -Screen.height);

        // Animate down
        LeanTween.move(ShopWindow.GetComponent<RectTransform>(), hidePos, 0.4f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                OnHideShop?.Invoke();
                ShopWindow.SetActive(false);
                ShopWindow.GetComponent<RectTransform>().anchoredPosition = startPosition;
                bgCanvasGroup.gameObject.SetActive(false);
            });
    }
    public void ClickHero(int id)
    {
        // if (selectedHero == id)
        //     return; // Don't reselect the same hero
        if(IAPManager.Instance.IsFullGameUnlocked == false&& id!= 0)
        {
            UIManager.Instance.ClickBuyPopupWindow();
            return;  
        }

        if(LeanTween.isTweening())
        return;

        for (int i = 0; i < HeroNormal.Count; i++)
        {
            if (i == id)
            {
                SetCharacterData(id);
                
                HeroPortrait[i].SetActive(true);
                HeroNormal[i].transform.GetChild(0).gameObject.SetActive(true);

                // Reset scale before animating
                OriginalScale = HeroPortrait[i].transform.localScale;
                 HeroPortrait[i].transform.localScale = OriginalScale * 0.9f;
            GameObject ports = HeroPortrait[i];
                // Animate pop-in effect
                LeanTween.scale(ports, OriginalScale * 1.05f, 0.15f)
                    .setEaseOutBack()
                    .setOnComplete(() =>
                    {
                        LeanTween.scale(ports, OriginalScale, 0.1f).setEaseInOutSine();
                    });
             }
            else
            {
                HeroPortrait[i].SetActive(false);
                HeroNormal[i].transform.GetChild(0).gameObject.SetActive(false);
 
            }
        }

        selectedHero = id;
        GameData.HeroSelected = id;
    }
    public void SetCharacterData(int aID)
    {
        HeroData data= CardContainer.Instance.HeroDataList[aID];
        if(aID == 0)
        {
            NameLabel.text=data.heroName.ToString();
            HPLabel.text=data.startingHP.ToString();
            ArtifactSlotsLabel.text=data.ArtifactSlots.ToString();
            PotionSlotsLabel.text=data.PotionSlots.ToString();
            GolfLabel.text= CardContainer.Instance.StatingGold.ToString();
            RuneLabel.text="None";
        }
         if(aID == 1)
        {
            NameLabel.text=data.heroName.ToString();
            HPLabel.text=data.startingHP.ToString();
            ArtifactSlotsLabel.text=data.ArtifactSlots.ToString();
            PotionSlotsLabel.text=data.PotionSlots.ToString();
            GolfLabel.text= CardContainer.Instance.StatingGold.ToString();
            RuneLabel.text="2x rare items \n + Gold each level";
        }
         if(aID == 2)
        {
            NameLabel.text=data.heroName.ToString();
            HPLabel.text=data.startingHP.ToString();
            ArtifactSlotsLabel.text=data.ArtifactSlots.ToString();
            PotionSlotsLabel.text=data.PotionSlots.ToString();
            GolfLabel.text= CardContainer.Instance.StatingGold.ToString();
            RuneLabel.text="+1 attack \n+Rank ranom units";
        }
         if(aID == 3)
        {
    
            NameLabel.text=data.heroName.ToString();
            HPLabel.text=data.startingHP.ToString();
            ArtifactSlotsLabel.text=data.ArtifactSlots.ToString();
            PotionSlotsLabel.text=data.PotionSlots.ToString();
            GolfLabel.text= CardContainer.Instance.StatingGold.ToString();
            RuneLabel.text="2x Potions";
        }
   
    }
    public void ClickPlay()
    {
        if(selectedHero == -1)
        {
            UIManager.Instance.ShowTooltip("You need to selected a hero!");
            return;
        }
        if(TutorialController.Instance.HasRunTutorial() == false)
        {
            if(UIManager.Instance.PotionSlotParent.childCount>0)
                PotionManager.Instance.SellPotion(UIManager.Instance.PotionSlotParent.GetChild(0).GetComponent<Potion>());
        }

        HideWindow();
    }
    public void ClickLocked()
    {
        UIManager.Instance.ShowTooltip("Locked heroes");

    }
  
}
