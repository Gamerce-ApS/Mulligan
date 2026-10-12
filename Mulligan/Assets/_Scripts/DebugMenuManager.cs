using System;
using UnityEngine;

public class DebugMenuManager : Singleton<DebugMenuManager>
{
    private enum DebugPage
    {
        Main,
        Artifacts,
        Potions
    }

    private const int ActivationTapCount = 3;
    private const float ActivationTapTimeout = 1.5f;
    private const int ItemsPerPage = 7;
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    public int GoldToGive = 100;
    public float CornerTapSize = 150f;

    public bool DebugModeActive { get; private set; }
    public bool MenuOpen { get; private set; }

    private DebugPage currentPage = DebugPage.Main;
    private int itemPage;
    private int activationTaps;
    private float lastActivationTapTime;
    private string statusMessage = "";

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle itemButtonStyle;
    private GUIStyle statusStyle;
    private GUIStyle cornerStyle;

    public static DebugMenuManager EnsureExists(GameObject host)
    {
        DebugMenuManager manager = FindObjectOfType<DebugMenuManager>(true);
        if (manager != null)
            return manager;

        if (host == null)
        {
            GameObject managerObject = new GameObject("DebugMenuManager");
            return managerObject.AddComponent<DebugMenuManager>();
        }

        return host.AddComponent<DebugMenuManager>();
    }

    private void Update()
    {
        if (MenuOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseMenu();
    }

    public void CornerPressed()
    {
        if (DebugModeActive)
        {
            ToggleMenu();
            return;
        }

        if (SettingsManager.Instance == null || SettingsManager.Instance.IsOpen == false)
            return;

        float now = Time.unscaledTime;
        if (now - lastActivationTapTime > ActivationTapTimeout)
            activationTaps = 0;

        lastActivationTapTime = now;
        activationTaps++;

        if (activationTaps < ActivationTapCount)
            return;

        activationTaps = 0;
        DebugModeActive = true;
        MenuOpen = true;
        currentPage = DebugPage.Main;
        statusMessage = "Debug mode enabled for this session.";
        SettingsManager.Instance.HideWindow();
        UIManager.Instance?.HideCardInfoPopup();
        SoundManager.TryPlay(SoundType.Success);
        VibrationsManager.TryVibrate(VibrationType.Success);
    }

    public void ToggleMenu()
    {
        if (DebugModeActive == false)
            return;

        MenuOpen = !MenuOpen;
        if (MenuOpen)
        {
            currentPage = DebugPage.Main;
            itemPage = 0;
            UIManager.Instance?.HideCardInfoPopup();
        }
    }

    public void CloseMenu()
    {
        MenuOpen = false;
        currentPage = DebugPage.Main;
        itemPage = 0;
    }

    public void DebugSetEnemyToOneHit()
    {
        if (GameManager.Instance == null || GameManager.Instance.TheEnemy == null ||
            GameManager.Instance.TheEnemy.gameObject.activeInHierarchy == false)
        {
            SetStatus("No active enemy.");
            return;
        }

        GameManager.Instance.TheEnemy.DebugSetOneHitHealth();
        SetStatus("Enemy HP set to 1. Your next damaging hit can defeat it.");
    }

    public void DebugOpenShop()
    {
        if (ShopManager.Instance == null || ShopManager.Instance.ShopWindow == null)
        {
            SetStatus("Shop is not available.");
            return;
        }

        CloseMenu();
        if (ShopManager.Instance.ShopWindow.activeSelf == false)
            ShopManager.Instance.ShowShopWindow();
    }

    public void DebugGiveGold()
    {
        int amount = Mathf.Max(1, GoldToGive);
        GameManager.Instance.AddGold(amount);
        SetStatus("Added " + amount + " Gold.");
    }

    public void DebugUnlockAllItems()
    {
        int highestUnlockRun = GameData.CompletedFirstBossAmount;

        if (CardContainer.Instance.ArtifactDataList != null)
        {
            foreach (ArtifactData artifact in CardContainer.Instance.ArtifactDataList)
            {
                if (artifact != null)
                    highestUnlockRun = Mathf.Max(highestUnlockRun, artifact.UnlockRun);
            }
        }

        if (CardContainer.Instance.PotionDataList != null)
        {
            foreach (PotionCardData potion in CardContainer.Instance.PotionDataList)
            {
                if (potion != null)
                    highestUnlockRun = Mathf.Max(highestUnlockRun, potion.UnlockRun);
            }
        }

        GameData.CompletedFirstBossAmount = highestUnlockRun;
        GameData.UnlockProgressForThisRun = highestUnlockRun;

        if (DailyQuestManager.Instance != null)
            DailyQuestManager.Instance.DebugUnlockAllArtifactRewards();

        PlayerPrefs.Save();
        RefreshInventoryIfOpen();
        SetStatus("All Artifacts and Potions unlocked.");
    }

    public void DebugAddArtifact(int index)
    {
        ArtifactData[] artifacts = CardContainer.Instance.ArtifactDataList;
        if (artifacts == null || index < 0 || index >= artifacts.Length || artifacts[index] == null)
        {
            SetStatus("Artifact is not available.");
            return;
        }

        if (GameManager.Instance.TheHero == null || GameManager.Instance.TheHero.myHeroData == null)
        {
            SetStatus("Start a run before adding an Artifact.");
            return;
        }

        if (ArtifactManager.Instance.ActiveArtifacts.Count >= GameManager.Instance.TheHero.myHeroData.ArtifactSlots)
        {
            SetStatus("Artifact slots are full.");
            return;
        }

        ArtifactManager.Instance.AddArtifact(artifacts[index]);
        SetStatus("Added Artifact: " + LocalizedContent.ArtifactName(artifacts[index]));
    }

    public void DebugAddPotion(int index)
    {
        PotionCardData[] potions = CardContainer.Instance.PotionDataList;
        if (potions == null || index < 0 || index >= potions.Length || potions[index] == null)
        {
            SetStatus("Potion is not available.");
            return;
        }

        if (GameManager.Instance.TheHero == null || GameManager.Instance.TheHero.myHeroData == null)
        {
            SetStatus("Start a run before adding a Potion.");
            return;
        }

        if (PotionManager.Instance.ActivePotions.Count >= GameManager.Instance.TheHero.myHeroData.PotionSlots)
        {
            SetStatus("Potion slots are full.");
            return;
        }

        PotionManager.Instance.AddPotion(potions[index]);
        SetStatus("Added Potion: " + LocalizedContent.PotionName(potions[index]));
    }

    private void OnGUI()
    {
        bool settingsOpen = SettingsManager.Instance != null && SettingsManager.Instance.IsOpen;
        if (DebugModeActive == false && settingsOpen == false)
            return;

        Matrix4x4 originalMatrix = GUI.matrix;
        float scale = Mathf.Max(0.01f, Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight));
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float screenWidth = Screen.width / scale;
        float screenHeight = Screen.height / scale;
        Rect safeArea = GetScaledSafeArea(scale);

        BuildStyles();
        Color originalColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.01f);
        if (GUI.Button(new Rect(safeArea.x, safeArea.y, CornerTapSize, CornerTapSize), GUIContent.none, cornerStyle))
            CornerPressed();
        GUI.color = originalColor;

        if (DebugModeActive == false || MenuOpen == false)
        {
            GUI.matrix = originalMatrix;
            return;
        }

        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0f, 0f, screenWidth, screenHeight), Texture2D.whiteTexture);
        GUI.color = originalColor;

        float panelWidth = Mathf.Min(1050f, screenWidth - 100f);
        float panelHeight = Mathf.Min(920f, screenHeight - 80f);
        Rect panelRect = new Rect(
            (screenWidth - panelWidth) * 0.5f,
            (screenHeight - panelHeight) * 0.5f,
            panelWidth,
            panelHeight);

        GUILayout.BeginArea(panelRect, GUI.skin.window);
        DrawHeader();

        if (currentPage == DebugPage.Main)
            DrawMainPage();
        else
            DrawItemPage();

        GUILayout.EndArea();
        GUI.matrix = originalMatrix;
    }

    private void DrawHeader()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(GetPageTitle(), titleStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("X", buttonStyle, GUILayout.Width(90f), GUILayout.Height(65f)))
            CloseMenu();
        GUILayout.EndHorizontal();

        GUILayout.Space(10f);
        GUILayout.Label("Device testing tools", subtitleStyle);
        GUILayout.Space(18f);
    }

    private void DrawMainPage()
    {
        if (GUILayout.Button("Enemy HP = 1 (One Blow)", buttonStyle, GUILayout.Height(76f)))
            DebugSetEnemyToOneHit();

        if (GUILayout.Button("Open Shop", buttonStyle, GUILayout.Height(76f)))
            DebugOpenShop();

        if (GUILayout.Button("Give " + Mathf.Max(1, GoldToGive) + " Gold", buttonStyle, GUILayout.Height(76f)))
            DebugGiveGold();

        if (GUILayout.Button("Get Specific Artifact", buttonStyle, GUILayout.Height(76f)))
            OpenPage(DebugPage.Artifacts);

        if (GUILayout.Button("Get Specific Potion", buttonStyle, GUILayout.Height(76f)))
            OpenPage(DebugPage.Potions);

        if (GUILayout.Button("Unlock All Artifacts + Potions", buttonStyle, GUILayout.Height(76f)))
            DebugUnlockAllItems();

        GUILayout.FlexibleSpace();
        GUILayout.Label(statusMessage, statusStyle, GUILayout.Height(70f));
    }

    private void DrawItemPage()
    {
        int itemCount = GetItemCount();
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(itemCount / (float)ItemsPerPage));
        itemPage = Mathf.Clamp(itemPage, 0, pageCount - 1);
        int startIndex = itemPage * ItemsPerPage;
        int endIndex = Mathf.Min(startIndex + ItemsPerPage, itemCount);

        for (int i = startIndex; i < endIndex; i++)
        {
            if (GUILayout.Button(GetItemLabel(i), itemButtonStyle, GUILayout.Height(82f)))
            {
                if (currentPage == DebugPage.Artifacts)
                    DebugAddArtifact(i);
                else
                    DebugAddPotion(i);
            }
        }

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Back", buttonStyle, GUILayout.Width(180f), GUILayout.Height(65f)))
            OpenPage(DebugPage.Main);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("<", buttonStyle, GUILayout.Width(100f), GUILayout.Height(65f)))
            itemPage = Mathf.Max(0, itemPage - 1);
        GUILayout.Label((itemPage + 1) + " / " + pageCount, subtitleStyle, GUILayout.Width(150f), GUILayout.Height(65f));
        if (GUILayout.Button(">", buttonStyle, GUILayout.Width(100f), GUILayout.Height(65f)))
            itemPage = Mathf.Min(pageCount - 1, itemPage + 1);
        GUILayout.EndHorizontal();
        GUILayout.Label(statusMessage, statusStyle, GUILayout.Height(55f));
    }

    private void OpenPage(DebugPage page)
    {
        currentPage = page;
        itemPage = 0;
        statusMessage = "";
        SoundManager.TryPlay(SoundType.ButtonTap);
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
    }

    private string GetPageTitle()
    {
        switch (currentPage)
        {
            case DebugPage.Artifacts:
                return "Select Artifact";
            case DebugPage.Potions:
                return "Select Potion";
            default:
                return "Debug Menu";
        }
    }

    private int GetItemCount()
    {
        if (currentPage == DebugPage.Artifacts)
            return CardContainer.Instance.ArtifactDataList != null ? CardContainer.Instance.ArtifactDataList.Length : 0;

        return CardContainer.Instance.PotionDataList != null ? CardContainer.Instance.PotionDataList.Length : 0;
    }

    private string GetItemLabel(int index)
    {
        if (currentPage == DebugPage.Artifacts)
        {
            ArtifactData artifact = CardContainer.Instance.ArtifactDataList[index];
            if (artifact == null)
                return "Missing Artifact";

            return LocalizedContent.ArtifactName(artifact) + "\n" +
                   (RarityType)Mathf.Clamp(artifact.rarity, 0, 3) + " | " + artifact.effect + " | " + artifact.value;
        }

        PotionCardData potion = CardContainer.Instance.PotionDataList[index];
        if (potion == null)
            return "Missing Potion";

        return LocalizedContent.PotionName(potion) + "\n" +
               (RarityType)Mathf.Clamp(potion.rarity, 0, 3) + " | " + potion.effectType + " | " + potion.value;
    }

    private void SetStatus(string message)
    {
        statusMessage = message;
        SoundManager.TryPlay(SoundType.ButtonTap);
        VibrationsManager.TryVibrate(VibrationType.ButtonTap);
    }

    private void RefreshInventoryIfOpen()
    {
        if (InventoryOverviewManager.Instance != null &&
            InventoryOverviewManager.Instance.ShopWindow != null &&
            InventoryOverviewManager.Instance.ShopWindow.activeSelf)
        {
            InventoryOverviewManager.Instance.PopulateInventory();
        }
    }

    private Rect GetScaledSafeArea(float scale)
    {
        Rect safeArea = Screen.safeArea;
        return new Rect(
            safeArea.x / scale,
            (Screen.height - safeArea.yMax) / scale,
            safeArea.width / scale,
            safeArea.height / scale);
    }

    private void BuildStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 42,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        titleStyle.normal.textColor = Color.white;

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            alignment = TextAnchor.MiddleCenter
        };
        subtitleStyle.normal.textColor = new Color(0.82f, 0.84f, 0.88f);

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };

        itemButtonStyle = new GUIStyle(buttonStyle)
        {
            fontSize = 23,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(24, 18, 8, 8)
        };

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        statusStyle.normal.textColor = new Color(0.95f, 0.78f, 0.28f);

        cornerStyle = new GUIStyle(GUI.skin.button);
    }
}
