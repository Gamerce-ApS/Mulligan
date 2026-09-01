using System;
using System.Collections.Generic;
using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;
using UnityEngine;

public class EventTests : MonoBehaviour, IGameAnalyticsATTListener
{
    // --- Navigation & UI State ---
    private enum Tab
    {
        StatusAndIdentity,
        SendEvents,
        RemoteConfigs,
        CustomDimensions,
        LiveLogs
    }

    private Tab currentTab = Tab.StatusAndIdentity;
    private Vector2 mainScrollPos;
    private Vector2 logScrollPos;
    private Vector2 consoleScrollPos;
    private int lastConsoleMessageCount = -1;

    // --- Tab-specific Inputs ---

    // Identity Inputs
    private string customIdInput = "my_custom_id";
    private string externalUserIdInput = "external_user_id_1";

    // Design Event Inputs
    private string designEventName = "Weapons:Legendary:Sword";
    private float designEventValue = 7.5f;
    private bool designHasValue = true;

    // Business Event Inputs
    private string bizCurrency = "USD";
    private int bizAmount = 99; // in cents
    private string bizItemType = "Weapons";
    private string bizItemId = "legendary_sword_01";
    private string bizCartType = "shop_main";

    // Progression Event Inputs
    private GAProgressionStatus progStatus = GAProgressionStatus.Start;
    private string prog01 = "World_01";
    private string prog02 = "Stage_01";
    private string prog03 = "SavingPrincess";
    private int progScore = 0;
    private bool progHasScore = false;

    // Resource Event Inputs
    private GAResourceFlowType resourceFlow = GAResourceFlowType.Sink;
    private string resourceCurrency = "silver";
    private float resourceAmount = 1000f;
    private string resourceItemType = "Shop";
    private string resourceItemId = "Minigun";

    // Ad Event Inputs
    private GAAdAction adAction = GAAdAction.Clicked;
    private GAAdType adType = GAAdType.Interstitial;
    private string adNetwork = "admob";
    private string adPlacement = "after_level";
    private long adDuration = 5000; // ms
    private bool adHasDuration = false;
    private GAAdError adError = GAAdError.NoFill;
    private bool adHasError = false;

    // Error Event Inputs
    private GAErrorSeverity errorSeverity = GAErrorSeverity.Info;
    private string errorMessage = "GA Test Exception: something went wrong";

    // Custom Dimensions Inputs
    private string customDim01 = "ninja";
    private string customDim02 = "dolphin";
    private string customDim03 = "alliance";

    // Remote Config Inputs
    private string remoteConfigKey = "my_key";
    private string remoteConfigFallback = "fallback_value";
    private string remoteConfigResult = "";

    // Logging Toggles
    private bool infoLogEnabled = false;
    private bool verboseLogEnabled = false;

    // Common Custom Fields (Key-Value pairs for testing custom fields on events)
    private string customFieldKey1 = "test";
    private string customFieldValue1 = "hello_world";
    private string customFieldKey2 = "score";
    private float customFieldValue2 = 100f;
    private bool includeCustomFields = true;

    // --- GUI Styles (Lazily initialized) ---
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle sidebarStyle;
    private GUIStyle contentStyle;
    private GUIStyle buttonStyle;
    private GUIStyle activeTabStyle;
    private GUIStyle logEntryStyle;
    private GUIStyle statusValueStyle;
    private GUIStyle warningBoxStyle;

    private bool stylesInitialized = false;

    // --- Unity Lifecycle ---

    void Start()
    {
        // Routes background-thread logs (native C++ SDK) into GA_Debug.Messages.
        ThreadedLogCapture.Ensure(gameObject);

        Debug.Log("persistentDataPath=" + Application.persistentDataPath);

        if (GameAnalytics.SettingsGA != null)
        {
            infoLogEnabled = GameAnalytics.SettingsGA.InfoLogBuild;
            verboseLogEnabled = GameAnalytics.SettingsGA.VerboseLogBuild;
        }

        GameAnalytics.OnRemoteConfigsUpdatedEvent += OnRemoteConfigsUpdated;
        Debug.Log("IsRemoteConfigsReady: " + GameAnalytics.IsRemoteConfigsReady());

        try
        {
            GameAnalyticsILRD.SubscribeHyperBidImpressions();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GameAnalyticsILRD.SubscribeHyperBidImpressions failed, continuing without it: " + ex.Message);
        }

        // Default custom fields initialization
        var globalFields = new Dictionary<string, object>
        {
            { "test", 666 },
            { "test_2", "global_hello_world" }
        };
        GameAnalytics.SetGlobalCustomEventFields(globalFields);

        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            GameAnalytics.RequestTrackingAuthorization(this);
        }
        else
        {
            GameAnalytics.Initialize();
            GameAnalytics.StartSession();
        }

        Debug.Log("User ID: " + GameAnalytics.GetUserId());
        GA_Debug.EnabledLog();
    }

    private void OnRemoteConfigsUpdated()
    {
        Debug.Log("OnRemoteConfigsUpdated callback fired!");
        remoteConfigResult = "Remote Configs Updated! Check keys.";
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.15f, 0.75f, 1f) }
        };

        subHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
        };

        sidebarStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 10, 10)
        };

        contentStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(15, 15, 15, 15)
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 35
        };

        activeTabStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 35,
            normal = { textColor = new Color(0.15f, 0.85f, 1f) }
        };

        logEntryStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            wordWrap = true,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };

        statusValueStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.2f, 0.9f, 0.3f) }
        };

        warningBoxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(12, 12, 12, 12),
            normal = { background = Texture2D.whiteTexture }
        };

        stylesInitialized = true;
    }

    private string lastOnGUIError = null;

    void OnGUI()
    {
        try
        {
            DrawDashboard();
            lastOnGUIError = null;
        }
        catch (Exception ex)
        {
            lastOnGUIError = ex.ToString();
            Debug.LogError("EventTests.OnGUI threw, dashboard hidden this frame: " + lastOnGUIError);
        }

        if (lastOnGUIError != null)
        {
            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
            GUILayout.Label("Dashboard failed to draw:\n" + lastOnGUIError);
            GUILayout.EndArea();
        }
    }

    private void DrawDashboard()
    {
        InitializeStyles();

        // Title Bar
        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, 40));
        GUILayout.BeginHorizontal("box");
        GUILayout.Label("📊 GameAnalytics Modern Dashboard", headerStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label("SDK Version: " + (GameAnalytics.GetUserId() != null ? "Active" : "Not Init"), statusValueStyle);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        // Left Panel: Sidebar (Status & Session Controls)
        GUILayout.BeginArea(new Rect(10, 60, 260, Screen.height - 70));
        GUILayout.BeginVertical(sidebarStyle, GUILayout.ExpandHeight(true));

        GUILayout.Label("System Status", subHeaderStyle);
        GUILayout.Space(5);

        DrawStatusRow("Initialized:", GameAnalytics.Initialized ? "YES" : "NO", GameAnalytics.Initialized);
        DrawStatusRow("Remote Configs:", GameAnalytics.IsRemoteConfigsReady() ? "READY" : "NOT READY", GameAnalytics.IsRemoteConfigsReady());
        
        GUILayout.Space(10);
        GUILayout.Label("Current Identity", subHeaderStyle);
        GUILayout.Space(5);

        DrawIdentityRow("User ID:", GameAnalytics.GetUserId());
        DrawIdentityRow("Ext User ID:", GameAnalytics.GetExternalUserId());
        DrawIdentityRow("AB Test ID:", GameAnalytics.GetABTestingId());
        DrawIdentityRow("AB Variant:", GameAnalytics.GetABTestingVariantId());

        GUILayout.Space(10);
        GUILayout.Label("Logging", subHeaderStyle);
        GUILayout.Space(5);

        bool newInfoLogEnabled = GUILayout.Toggle(infoLogEnabled, "Info Log");
        if (newInfoLogEnabled != infoLogEnabled)
        {
            infoLogEnabled = newInfoLogEnabled;
            GA_Setup.SetInfoLog(infoLogEnabled);
        }

        bool newVerboseLogEnabled = GUILayout.Toggle(verboseLogEnabled, "Verbose Log");
        if (newVerboseLogEnabled != verboseLogEnabled)
        {
            verboseLogEnabled = newVerboseLogEnabled;
            GA_Setup.SetVerboseLog(verboseLogEnabled);
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Initialize SDK", buttonStyle))
        {
            GameAnalytics.Initialize();
            GameAnalytics.StartSession();
        }

        if (GUILayout.Button("Force Start Session", buttonStyle))
        {
            GameAnalytics.StartSession();
        }

        if (GUILayout.Button("Quit Game", buttonStyle))
        {
            Application.Quit();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();

        // Right Panel: Nav Tabs and Main Content Form
        GUILayout.BeginArea(new Rect(280, 60, Screen.width - 290, Screen.height - 70));
        GUILayout.BeginVertical();

        // Tab Navigation Bar
        GUILayout.BeginHorizontal();
        DrawTabButton("Identity & Setup", Tab.StatusAndIdentity);
        DrawTabButton("Send Events", Tab.SendEvents);
        DrawTabButton("Remote Configs", Tab.RemoteConfigs);
        DrawTabButton("Custom Dimensions", Tab.CustomDimensions);
        DrawTabButton("Live Logs (" + (GA_Debug.Messages != null ? GA_Debug.Messages.Count : 0) + ")", Tab.LiveLogs);
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Content Area Scroll
        mainScrollPos = GUILayout.BeginScrollView(mainScrollPos, contentStyle, GUILayout.ExpandHeight(true));

        // Auto-Setup banner if Platforms list is completely empty
        if (GameAnalytics.SettingsGA == null || GameAnalytics.SettingsGA.Platforms.Count == 0)
        {
            DrawAutoSetupBanner();
        }

        switch (currentTab)
        {
            case Tab.StatusAndIdentity:
                DrawIdentityTab();
                break;
            case Tab.SendEvents:
                DrawEventsTab();
                break;
            case Tab.RemoteConfigs:
                DrawRemoteConfigsTab();
                break;
            case Tab.CustomDimensions:
                DrawCustomDimensionsTab();
                break;
            case Tab.LiveLogs:
                DrawLiveLogsTab();
                break;
        }

        GUILayout.EndScrollView();

        // Direct Quick Console Logger Preview at bottom of right panel
        GUILayout.BeginVertical("box", GUILayout.Height(Mathf.Max(180f, Screen.height * 0.25f)));
        GUILayout.Label("Console Output:", subHeaderStyle);
        var messages = GA_Debug.Messages;
        if (messages != null && messages.Count > 0)
        {
            // Snap to the newest entry whenever a new message arrives.
            if (messages.Count != lastConsoleMessageCount)
            {
                consoleScrollPos.y = float.MaxValue;
                lastConsoleMessageCount = messages.Count;
            }
            consoleScrollPos = GUILayout.BeginScrollView(consoleScrollPos, GUILayout.ExpandHeight(true));
            for (int i = 0; i < messages.Count; i++)
            {
                GUILayout.Label($"{i + 1}. {messages[i]}", logEntryStyle);
            }
            GUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("No logs recorded yet. Perform actions to see SDK output.", logEntryStyle);
        }
        GUILayout.EndVertical();

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    // --- Navigation UI Helpers ---

    private void DrawTabButton(string label, Tab tab)
    {
        bool isActive = currentTab == tab;
        if (GUILayout.Button(label, isActive ? activeTabStyle : buttonStyle, GUILayout.ExpandWidth(true)))
        {
            currentTab = tab;
        }
    }

    private void DrawStatusRow(string label, string val, bool active)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(110));
        GUI.color = active ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.3f, 0.2f);
        GUILayout.Label(val, statusValueStyle, GUILayout.ExpandWidth(true));
        GUI.color = Color.white;
        GUILayout.EndHorizontal();
    }

    private void DrawIdentityRow(string label, string val)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(90));
        string cleanVal = string.IsNullOrEmpty(val) ? "None" : val;
        GUILayout.Label(cleanVal, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
    }

    private void DrawAutoSetupBanner()
    {
        GUI.color = new Color(0.9f, 0.45f, 0.15f);
        GUILayout.BeginVertical(warningBoxStyle);
        GUI.color = Color.white;

        GUILayout.Label("⚠️ Quick Setup Required!", subHeaderStyle);
        GUILayout.Label("The GameAnalytics settings asset has no platforms or game keys configured. The SDK requires at least one platform (with a game key/secret) to initialize and validate events.", logEntryStyle);
        GUILayout.Space(8);

        if (GUILayout.Button("👉 Automatically Add & Configure Default Testing Platforms", buttonStyle))
        {
            SetupTestingPlatforms();
        }

        GUILayout.EndVertical();
        GUILayout.Space(15);
    }

    private void SetupTestingPlatforms()
    {
        try
        {
            var settings = GameAnalytics.SettingsGA;
            if (settings != null)
            {
                // Clear any existing (just in case)
                settings.Platforms.Clear();

                // Add active editor platforms & mobile targets
                RuntimePlatform activeStandalone = Application.platform == RuntimePlatform.WindowsEditor ? RuntimePlatform.WindowsPlayer : RuntimePlatform.OSXPlayer;
                
                settings.AddPlatform(activeStandalone);
                settings.AddPlatform(RuntimePlatform.Android);
                settings.AddPlatform(RuntimePlatform.IPhonePlayer);

                // Add test dummy game keys and secret keys (32-char hex string & 40-char hex string)
                string testGameKey = "1234567890abcdef1234567890abcdef";
                string testSecretKey = "1234567890abcdef1234567890abcdef12345678";

                for (int i = 0; i < settings.Platforms.Count; i++)
                {
                    GameAnalyticsSDK.Setup.Settings.UpdateKeys(i, testGameKey, testSecretKey);
                }

                // Add resource event configurations so sending resources won't fail validation
                settings.ResourceCurrencies.Clear();
                settings.ResourceCurrencies.Add("silver");
                settings.ResourceCurrencies.Add("gold");

                settings.ResourceItemTypes.Clear();
                settings.ResourceItemTypes.Add("Shop");
                settings.ResourceItemTypes.Add("Lootbox");

                // Add custom dimensions configurations so validation is happy
                settings.CustomDimensions01.Clear();
                settings.CustomDimensions01.Add("ninja");
                settings.CustomDimensions02.Clear();
                settings.CustomDimensions02.Add("dolphin");
                settings.CustomDimensions03.Clear();
                settings.CustomDimensions03.Add("alliance");

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(settings);
                UnityEditor.AssetDatabase.SaveAssets();
#endif

                Debug.Log("Successfully configured default platforms, resource types, custom dimensions, and dummy keys!");
                
                // Reinitialize SDK with new settings
                GameAnalytics.Initialize();
                GameAnalytics.StartSession();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error configuring testing platforms automatically: " + ex.Message);
        }
    }

    private Dictionary<string, object> BuildCustomFields()
    {
        if (!includeCustomFields) return null;

        var dict = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(customFieldKey1))
        {
            dict.Add(customFieldKey1, customFieldValue1);
        }
        if (!string.IsNullOrEmpty(customFieldKey2))
        {
            dict.Add(customFieldKey2, customFieldValue2);
        }
        return dict.Count > 0 ? dict : null;
    }

    private void DrawCustomFieldsSetup()
    {
        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        includeCustomFields = GUILayout.Toggle(includeCustomFields, "Include Custom Event Fields on Send");
        if (includeCustomFields)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Field 1 Key:", GUILayout.Width(80));
            customFieldKey1 = GUILayout.TextField(customFieldKey1, GUILayout.Width(100));
            GUILayout.Label("Val (Str):", GUILayout.Width(60));
            customFieldValue1 = GUILayout.TextField(customFieldValue1, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Field 2 Key:", GUILayout.Width(80));
            customFieldKey2 = GUILayout.TextField(customFieldKey2, GUILayout.Width(100));
            GUILayout.Label("Val (Num):", GUILayout.Width(60));
            string rawNum = GUILayout.TextField(customFieldValue2.ToString(), GUILayout.ExpandWidth(true));
            float.TryParse(rawNum, out customFieldValue2);
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }

    // --- Tab Contents ---

    private void DrawIdentityTab()
    {
        GUILayout.Label("Identity & Session Setup", subHeaderStyle);
        GUILayout.Label("Manage Custom IDs and External User IDs before or after SDK initialization.", GUI.skin.label);
        GUILayout.Space(10);

        GUILayout.BeginVertical("box");
        GUILayout.Label("Custom User ID (Needs to be checked in GameAnalytics Settings first)", GUI.skin.label);
        GUILayout.BeginHorizontal();
        customIdInput = GUILayout.TextField(customIdInput, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Set Custom ID", GUILayout.Width(140)))
        {
            GameAnalytics.SetCustomId(customIdInput);
            Debug.Log("Custom ID set to: " + customIdInput);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical("box");
        GUILayout.Label("External User ID", GUI.skin.label);
        GUILayout.BeginHorizontal();
        externalUserIdInput = GUILayout.TextField(externalUserIdInput, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Set External User ID", GUILayout.Width(140)))
        {
            GameAnalytics.SetExternalUserId(externalUserIdInput);
            Debug.Log("External User ID set to: " + externalUserIdInput);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(15);
        GUILayout.Label("Global Custom Event Fields", subHeaderStyle);
        if (GUILayout.Button("Apply Test Global Custom Fields (test=666, test_2=global_hello_world)"))
        {
            var globalFields = new Dictionary<string, object>
            {
                { "test", 666 },
                { "test_2", "global_hello_world" }
            };
            GameAnalytics.SetGlobalCustomEventFields(globalFields);
            Debug.Log("Global Custom Event Fields set.");
        }
    }

    private enum EventType { Design, Business, Progression, Resource, Ad, Error }
    private EventType selectedEventType = EventType.Design;

    private void DrawEventsTab()
    {
        GUILayout.Label("Send Events Interactive Console", subHeaderStyle);
        GUILayout.Label("Configure event payloads dynamically and test how the GameAnalytics SDK handles validation.", GUI.skin.label);
        GUILayout.Space(10);

        // Sub-tabs for event types
        GUILayout.BeginHorizontal();
        DrawEventSubTab("Design", EventType.Design);
        DrawEventSubTab("Business", EventType.Business);
        DrawEventSubTab("Progression", EventType.Progression);
        DrawEventSubTab("Resource", EventType.Resource);
        DrawEventSubTab("Ad", EventType.Ad);
        DrawEventSubTab("Error", EventType.Error);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginVertical("box");
        switch (selectedEventType)
        {
            case EventType.Design:
                DrawDesignEventForm();
                break;
            case EventType.Business:
                DrawBusinessEventForm();
                break;
            case EventType.Progression:
                DrawProgressionEventForm();
                break;
            case EventType.Resource:
                DrawResourceEventForm();
                break;
            case EventType.Ad:
                DrawAdEventForm();
                break;
            case EventType.Error:
                DrawErrorEventForm();
                break;
        }
        GUILayout.EndVertical();

        DrawCustomFieldsSetup();
    }

    private void DrawEventSubTab(string label, EventType type)
    {
        if (GUILayout.Toggle(selectedEventType == type, label, GUI.skin.button, GUILayout.ExpandWidth(true)))
        {
            selectedEventType = type;
        }
    }

    private void DrawDesignEventForm()
    {
        GUILayout.Label("🎯 Design Event Configuration", subHeaderStyle);
        GUILayout.Space(5);
        GUILayout.Label("Design events are used to track specific user behaviors (e.g. Weapon equipped, button clicked). Supports 1 to 5 parts separated by colons (e.g. category:subCategory:item).");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Event String:", GUILayout.Width(100));
        designEventName = GUILayout.TextField(designEventName, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        designHasValue = GUILayout.Toggle(designHasValue, "Include Numeric Value", GUILayout.Width(180));
        if (designHasValue)
        {
            string rawValue = GUILayout.TextField(designEventValue.ToString(), GUILayout.ExpandWidth(true));
            float.TryParse(rawValue, out designEventValue);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (GUILayout.Button("Send Design Event", buttonStyle))
        {
            var fields = BuildCustomFields();
            if (designHasValue)
            {
                GameAnalytics.NewDesignEvent(designEventName, designEventValue, fields);
                Debug.Log($"Design Event Sent: {designEventName} Value: {designEventValue}");
            }
            else
            {
                GameAnalytics.NewDesignEvent(designEventName, fields);
                Debug.Log($"Design Event Sent: {designEventName}");
            }
        }
    }

    private void DrawBusinessEventForm()
    {
        GUILayout.Label("💰 Business Event Configuration", subHeaderStyle);
        GUILayout.Space(5);
        GUILayout.Label("Business events track real-money in-app purchases. Currency is a 3-letter ISO code. Amount is defined in cents.");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Currency (ISO):", GUILayout.Width(120));
        bizCurrency = GUILayout.TextField(bizCurrency, GUILayout.Width(80));
        GUILayout.Label("Amount (in cents):", GUILayout.Width(120));
        string rawAmount = GUILayout.TextField(bizAmount.ToString(), GUILayout.ExpandWidth(true));
        int.TryParse(rawAmount, out bizAmount);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Item Type:", GUILayout.Width(120));
        bizItemType = GUILayout.TextField(bizItemType, GUILayout.Width(150));
        GUILayout.Label("Item ID:", GUILayout.Width(80));
        bizItemId = GUILayout.TextField(bizItemId, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Cart Type (Optional):", GUILayout.Width(120));
        bizCartType = GUILayout.TextField(bizCartType, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (GUILayout.Button("Send Business Event", buttonStyle))
        {
            var fields = BuildCustomFields();
            GameAnalytics.NewBusinessEvent(bizCurrency, bizAmount, bizItemType, bizItemId, bizCartType, fields);
            Debug.Log($"Business Event Sent: {bizCurrency} {bizAmount} Item: {bizItemId}");
        }
    }

    private void DrawProgressionEventForm()
    {
        GUILayout.Label("🏆 Progression Event Configuration", subHeaderStyle);
        GUILayout.Space(5);
        GUILayout.Label("Progression events track player journey through levels/stages. Status can be Start, Complete, or Fail.");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Progression Status:", GUILayout.Width(130));
        if (GUILayout.Button(progStatus.ToString(), GUILayout.Width(120)))
        {
            progStatus = (GAProgressionStatus)(((int)progStatus + 1) % Enum.GetValues(typeof(GAProgressionStatus)).Length);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Progression 01:", GUILayout.Width(110));
        prog01 = GUILayout.TextField(prog01, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Progression 02 (Opt):", GUILayout.Width(110));
        prog02 = GUILayout.TextField(prog02, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Progression 03 (Opt):", GUILayout.Width(110));
        prog03 = GUILayout.TextField(prog03, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        progHasScore = GUILayout.Toggle(progHasScore, "Include Score", GUILayout.Width(180));
        if (progHasScore)
        {
            string rawScore = GUILayout.TextField(progScore.ToString(), GUILayout.ExpandWidth(true));
            int.TryParse(rawScore, out progScore);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (GUILayout.Button("Send Progression Event", buttonStyle))
        {
            var fields = BuildCustomFields();
            string p2 = string.IsNullOrEmpty(prog02) ? null : prog02;
            string p3 = string.IsNullOrEmpty(prog03) ? null : prog03;

            if (progHasScore)
            {
                GameAnalytics.NewProgressionEvent(progStatus, prog01, p2, p3, progScore, fields);
                Debug.Log($"Progression Event Sent: {progStatus} {prog01} (Score: {progScore})");
            }
            else
            {
                GameAnalytics.NewProgressionEvent(progStatus, prog01, p2, p3, fields);
                Debug.Log($"Progression Event Sent: {progStatus} {prog01}");
            }
        }
    }

    private void DrawResourceEventForm()
    {
        GUILayout.Label("💎 Resource Event Configuration", subHeaderStyle);
        GUILayout.Space(5);
        GUILayout.Label("Resource events track virtually anything representing currency (Gold, Gems, Coins). Currencies and item types must be configured in settings.");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Flow Type:", GUILayout.Width(110));
        if (GUILayout.Button(resourceFlow.ToString(), GUILayout.Width(120)))
        {
            resourceFlow = (GAResourceFlowType)(((int)resourceFlow + 1) % Enum.GetValues(typeof(GAResourceFlowType)).Length);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Currency:", GUILayout.Width(110));
        resourceCurrency = GUILayout.TextField(resourceCurrency, GUILayout.Width(120));
        GUILayout.Label("Amount:", GUILayout.Width(80));
        string rawAmount = GUILayout.TextField(resourceAmount.ToString(), GUILayout.ExpandWidth(true));
        float.TryParse(rawAmount, out resourceAmount);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Item Type:", GUILayout.Width(110));
        resourceItemType = GUILayout.TextField(resourceItemType, GUILayout.Width(150));
        GUILayout.Label("Item ID:", GUILayout.Width(80));
        resourceItemId = GUILayout.TextField(resourceItemId, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (GUILayout.Button("Send Resource Event", buttonStyle))
        {
            var fields = BuildCustomFields();
            GameAnalytics.NewResourceEvent(resourceFlow, resourceCurrency, resourceAmount, resourceItemType, resourceItemId, fields);
            Debug.Log($"Resource Event Sent: {resourceFlow} {resourceCurrency} {resourceAmount}");
        }
    }

    private void DrawAdEventForm()
    {
        GUILayout.Label("📺 Ad Event Configuration", subHeaderStyle);
        GUILayout.Space(5);
        GUILayout.Label("Ad events track ad-networks lifecycle and impressions.");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Ad Action:", GUILayout.Width(100));
        if (GUILayout.Button(adAction.ToString(), GUILayout.Width(140)))
        {
            adAction = (GAAdAction)(((int)adAction + 1) % Enum.GetValues(typeof(GAAdAction)).Length);
        }
        GUILayout.Label("Ad Type:", GUILayout.Width(100));
        if (GUILayout.Button(adType.ToString(), GUILayout.ExpandWidth(true)))
        {
            adType = (GAAdType)(((int)adType + 1) % Enum.GetValues(typeof(GAAdType)).Length);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Ad Network:", GUILayout.Width(100));
        adNetwork = GUILayout.TextField(adNetwork, GUILayout.Width(140));
        GUILayout.Label("Placement:", GUILayout.Width(100));
        adPlacement = GUILayout.TextField(adPlacement, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        adHasDuration = GUILayout.Toggle(adHasDuration, "Include Duration (ms)", GUILayout.Width(160));
        if (adHasDuration)
        {
            string rawDuration = GUILayout.TextField(adDuration.ToString(), GUILayout.ExpandWidth(true));
            long.TryParse(rawDuration, out adDuration);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        adHasError = GUILayout.Toggle(adHasError, "Include Ad Error Code", GUILayout.Width(160));
        if (adHasError)
        {
            if (GUILayout.Button(adError.ToString(), GUILayout.ExpandWidth(true)))
            {
                adError = (GAAdError)(((int)adError + 1) % Enum.GetValues(typeof(GAAdError)).Length);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (GUILayout.Button("Send Ad Event", buttonStyle))
        {
            if (adHasError)
            {
                GameAnalytics.NewAdEvent(adAction, adType, adNetwork, adPlacement, adError);
                Debug.Log($"Ad Event Sent with Error: {adAction} Type: {adType} Network: {adNetwork} Error: {adError}");
            }
            else if (adHasDuration)
            {
                GameAnalytics.NewAdEvent(adAction, adType, adNetwork, adPlacement, adDuration);
                Debug.Log($"Ad Event Sent with Duration: {adAction} Type: {adType} Network: {adNetwork} Duration: {adDuration}");
            }
            else
            {
                GameAnalytics.NewAdEvent(adAction, adType, adNetwork, adPlacement);
                Debug.Log($"Ad Event Sent: {adAction} Type: {adType} Network: {adNetwork}");
            }
        }
    }

    private void DrawErrorEventForm()
    {
        GUILayout.Label("⚠️ Error Event Configuration", subHeaderStyle);
        GUILayout.Space(5);
        GUILayout.Label("Error events are ideal for capturing run-time bugs and exceptions.");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Severity:", GUILayout.Width(100));
        if (GUILayout.Button(errorSeverity.ToString(), GUILayout.Width(120)))
        {
            errorSeverity = (GAErrorSeverity)(((int)errorSeverity + 1) % Enum.GetValues(typeof(GAErrorSeverity)).Length);
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Exception Message / Stacktrace:", GUI.skin.label);
        errorMessage = GUILayout.TextArea(errorMessage, GUILayout.Height(60));

        GUILayout.Space(10);
        if (GUILayout.Button("Send Error Event", buttonStyle))
        {
            var fields = BuildCustomFields();
            GameAnalytics.NewErrorEvent(errorSeverity, errorMessage, fields);
            Debug.Log($"Error Event Sent: Severity {errorSeverity} Message: {errorMessage}");
        }
    }

    private void DrawRemoteConfigsTab()
    {
        GUILayout.Label("📡 Remote Configs & AB Testing", subHeaderStyle);
        GUILayout.Label("GameAnalytics supports dynamic key-value remote configurations and active A/B tests campaigns setup in your dashboard.", GUI.skin.label);
        GUILayout.Space(15);

        GUILayout.Label("Is Remote Configs Ready: " + GameAnalytics.IsRemoteConfigsReady());
        
        GUILayout.BeginVertical("box");
        GUILayout.Label("Config Single Key Lookup", subHeaderStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Key Name:", GUILayout.Width(80));
        remoteConfigKey = GUILayout.TextField(remoteConfigKey, GUILayout.Width(120));
        GUILayout.Label("Fallback (Default):", GUILayout.Width(120));
        remoteConfigFallback = GUILayout.TextField(remoteConfigFallback, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Get Remote Value As String", buttonStyle))
        {
            remoteConfigResult = GameAnalytics.GetRemoteConfigsValueAsString(remoteConfigKey, remoteConfigFallback);
            Debug.Log($"Fetched Config: key={remoteConfigKey} val={remoteConfigResult}");
        }
        
        GUILayout.Label("Lookup Result: " + remoteConfigResult, statusValueStyle);
        GUILayout.EndVertical();

        GUILayout.Space(15);

        GUILayout.BeginVertical("box");
        GUILayout.Label("Dump Full Config Payload", subHeaderStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Dump as String", GUILayout.ExpandWidth(true)))
        {
            string payload = GameAnalytics.GetRemoteConfigsContentAsString();
            Debug.Log("Full Remote Configs Content (String):\n" + payload);
        }
        if (GUILayout.Button("Dump as JSON Dictionary", GUILayout.ExpandWidth(true)))
        {
            string payload = GameAnalytics.GetRemoteConfigsContentAsJSON();
            Debug.Log("Full Remote Configs Content (JSON):\n" + payload);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void DrawCustomDimensionsTab()
    {
        GUILayout.Label("🏷️ Custom User Dimensions", subHeaderStyle);
        GUILayout.Label("Define custom user segment classifications (e.g. Player character class, clan, status) declared in GameAnalytics Settings.", GUI.skin.label);
        GUILayout.Space(15);

        GUILayout.BeginVertical("box");
        GUILayout.Label("Custom Dimension 01 Segment Setup", subHeaderStyle);
        GUILayout.BeginHorizontal();
        customDim01 = GUILayout.TextField(customDim01, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Apply 01", GUILayout.Width(100)))
        {
            GameAnalytics.SetCustomDimension01(customDim01);
            Debug.Log("Set Custom Dimension 01: " + customDim01);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical("box");
        GUILayout.Label("Custom Dimension 02 Segment Setup", subHeaderStyle);
        GUILayout.BeginHorizontal();
        customDim02 = GUILayout.TextField(customDim02, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Apply 02", GUILayout.Width(100)))
        {
            GameAnalytics.SetCustomDimension02(customDim02);
            Debug.Log("Set Custom Dimension 02: " + customDim02);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical("box");
        GUILayout.Label("Custom Dimension 03 Segment Setup", subHeaderStyle);
        GUILayout.BeginHorizontal();
        customDim03 = GUILayout.TextField(customDim03, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Apply 03", GUILayout.Width(100)))
        {
            GameAnalytics.SetCustomDimension03(customDim03);
            Debug.Log("Set Custom Dimension 03: " + customDim03);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void DrawLiveLogsTab()
    {
        GUILayout.Label("📝 Live GameAnalytics Execution Logs", subHeaderStyle);
        GUILayout.Label("Below is the history of standard output and GameAnalytics specific engine validation calls.", GUI.skin.label);
        GUILayout.Space(10);

        var messages = GA_Debug.Messages;
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Internal Messages", GUILayout.Width(180)))
        {
            if (messages != null) messages.Clear();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        logScrollPos = GUILayout.BeginScrollView(logScrollPos, "box", GUILayout.ExpandHeight(true));
        if (messages != null && messages.Count > 0)
        {
            for (int i = 0; i < messages.Count; i++)
            {
                GUILayout.Label($"{i + 1}. {messages[i]}", logEntryStyle);
            }
        }
        else
        {
            GUILayout.Label("No messages currently captured. Turn on SDK debugging or execute events.", logEntryStyle);
        }
        GUILayout.EndScrollView();
    }

    // --- ATT Listener Callbacks ---

    public void GameAnalyticsATTListenerNotDetermined()
    {
        Debug.Log("GameAnalyticsATTListenerNotDetermined");
        GameAnalytics.Initialize();
    }

    public void GameAnalyticsATTListenerRestricted()
    {
        Debug.Log("GameAnalyticsATTListenerRestricted");
        GameAnalytics.Initialize();
    }

    public void GameAnalyticsATTListenerDenied()
    {
        Debug.Log("GameAnalyticsATTListenerDenied");
        GameAnalytics.Initialize();
    }

    public void GameAnalyticsATTListenerAuthorized()
    {
        Debug.Log("GameAnalyticsATTListenerAuthorized");
        GameAnalytics.Initialize();
    }
}
