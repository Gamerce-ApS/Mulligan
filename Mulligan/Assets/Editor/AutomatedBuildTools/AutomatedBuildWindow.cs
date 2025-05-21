using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using UnityEditor;


public class AutomatedBuildWindow : EditorWindow
{
    private string _assetSavePath;
    private string _assetName;
    private static AutomatedBuildSettings _automatedBuildSettingsData;
    private const string SETTINGS_DATA_PATH = "Assets/Resources/";
    private const string SETTINGS_FILE_NAME = "AutomatedBuildSettings";
    private const string SETTINGS_RESOURCE_EXTENSION = ".asset";
    private List<string> _logOutput = new List<string>();
    private Vector2 _logScrollPos = Vector2.zero;
    private ReorderableList _logConsoleList;
    private string _buildLogFileName = string.Empty;
    private int selectedTab = 0;

    public const string DEBUG_CONFIG_ASSET_PATH = "Assets/BuildConfigurations/Debug.asset";
    public const string RELEASE_CONFIG_ASSET_PATH = "Assets/BuildConfigurations/Release.asset";

    public enum GradleTask
    {
        Assemble,
        Build,
        Check,
        Clean
    }

    [MenuItem("BuildTool/Teamcity Builder")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        var window = (AutomatedBuildWindow)GetWindow(typeof(AutomatedBuildWindow));
        window.Show();


    }


    // ReSharper disable once InconsistentNaming
    void OnGUI()
    {
        selectedTab = Tabs(new[] { "My build", "Unity", "Teamcity" }, selectedTab);

        if (selectedTab == 0)
        {
            DrawBuildSettings();
        }
        else if (selectedTab == 1)
        {
            DrawIosSettings();
        }
        else if (selectedTab == 2)
        {
            DrawTeamCityProjects();
        }


    }
    public void DrawBuildSettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        FindSettingsResourceFile(ref _automatedBuildSettingsData);
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.Label("Project:", EditorStyles.boldLabel);
            GUILayout.Label("This will create the app on teamcity");
            EditorGUILayout.TextField("Project Name (Boundle):", Application.identifier, GUILayout.Width(400));
            _automatedBuildSettingsData.gitPath = EditorGUILayout.TextField("Git Path:", _automatedBuildSettingsData.gitPath, GUILayout.Width(400));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create", GUILayout.Width(200), GUILayout.Height(30)))
            {
                if (_automatedBuildSettingsData.gitPath.Length > 3)
                {
                    Utility.StartBackgroundTask(Create());
                    errorComment = "";
                }
                else
                {
                    errorComment = "Missing git path";
                }
            }
            if (GUILayout.Button("Check", GUILayout.Width(200), GUILayout.Height(30)))
            {

            }
            GUILayout.EndHorizontal();

            GUI.color = Color.red;
            GUILayout.Label(errorComment);
            GUI.color = Color.white;

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.Label("Appstore:", EditorStyles.boldLabel);
            GUILayout.Label("This will create the app on appstore");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable create", GUILayout.Width(200), GUILayout.Height(30)))
            {
                Utility.StartBackgroundTask(Edit("env.ShouldCreateApp", "1"));
                Utility.StartBackgroundTask(Edit("env.AppName", _automatedBuildSettingsData.appName));
                Utility.StartBackgroundTask(Edit("env.SKU", _automatedBuildSettingsData.appSKU));
                Utility.StartBackgroundTask(Edit("env.BUNDLE_IDENTIFIER", _automatedBuildSettingsData.app_identifier));


            }
            if (GUILayout.Button("Disable create", GUILayout.Width(200), GUILayout.Height(30)))
            {
                Utility.StartBackgroundTask(Edit("env.ShouldCreateApp", "0"));
            }
            GUILayout.EndHorizontal();




            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.Label("Settings:", EditorStyles.boldLabel);
            _automatedBuildSettingsData.app_identifier = EditorGUILayout.TextField("app_identifier:", _automatedBuildSettingsData.app_identifier, GUILayout.Width(400));
            _automatedBuildSettingsData.apple_id = EditorGUILayout.TextField("apple_id:", _automatedBuildSettingsData.apple_id, GUILayout.Width(400));
            _automatedBuildSettingsData.itc_team_id = EditorGUILayout.TextField("itc_team_id:", _automatedBuildSettingsData.itc_team_id, GUILayout.Width(400));
            _automatedBuildSettingsData.team_id = EditorGUILayout.TextField("team_id:", _automatedBuildSettingsData.team_id, GUILayout.Width(400));

            _automatedBuildSettingsData.appName = EditorGUILayout.TextField("App Name:", _automatedBuildSettingsData.appName, GUILayout.Width(400));
            _automatedBuildSettingsData.appSKU = EditorGUILayout.TextField("SKU:", _automatedBuildSettingsData.appSKU, GUILayout.Width(400));


            _automatedBuildSettingsData.motherappLinkName = EditorGUILayout.TextField("MotherappLinkNam:", _automatedBuildSettingsData.motherappLinkName, GUILayout.Width(400));
            _automatedBuildSettingsData.appLinkName = EditorGUILayout.TextField("AppLinkName:", _automatedBuildSettingsData.appLinkName, GUILayout.Width(400));


            if (GUILayout.Button("Save (Appfile & Fastfile)", GUILayout.Width(400), GUILayout.Height(30)))
            {


                string appFileTemplate = File.ReadAllText(Application.dataPath + @"/Editor/AutomatedBuildTools/fastlane/AppFileTemplate");
                appFileTemplate = appFileTemplate.Replace("#app_identifier#", _automatedBuildSettingsData.app_identifier);
                appFileTemplate = appFileTemplate.Replace("#apple_id#", _automatedBuildSettingsData.apple_id);
                appFileTemplate = appFileTemplate.Replace("#itc_team_id#", _automatedBuildSettingsData.itc_team_id);
                appFileTemplate = appFileTemplate.Replace("#team_id#", _automatedBuildSettingsData.team_id);
                File.WriteAllText(Application.dataPath + @"/Editor/AutomatedBuildTools/fastlane/AppFile", appFileTemplate);

                string fastFileTemplate = File.ReadAllText(Application.dataPath + @"/Editor/AutomatedBuildTools/fastlane/FastFileTemplate");
                fastFileTemplate = fastFileTemplate.Replace("#Application.version#", Application.version);
                fastFileTemplate = fastFileTemplate.Replace("#itc_provider#", _automatedBuildSettingsData.team_id);
                File.WriteAllText(Application.dataPath + @"/Editor/AutomatedBuildTools/fastlane/FastFile", fastFileTemplate);





            }



            //if (GUILayout.Button("Edits project", GUILayout.Width(400), GUILayout.Height(30)))
            //{

            //}
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.Label("Buildlog:", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(400), GUILayout.Height(30)))
            {

                Utility.StartBackgroundTask(GetBuildLog());
            }





            GUILayout.EndHorizontal();




            if (buildLogXML != null)
            {
                XmlNodeList nodeList;
                XmlNode root = buildLogXML.DocumentElement;

                //nodeList = root.SelectNodes("descendant::page[@id='1']");  //this selects all nodes with page id="1"
                //nodeList = root.ChildNodes;
                //nodeList = nodeList[0].ChildNodes;  //this rebuilds the list using all the childnodes from the nodes with page id ="1"
                nodeList = root.ChildNodes;
                currentScroll = GUILayout.BeginScrollView(currentScroll);
                for (int i = 0; i < nodeList.Count; i++)
                {
                    //print(nodeList[i].Attributes["id"].Value);
                    //print(nodeList[i].InnerText);
                    //GUILayout.Label(nodeList[i].Attributes[0].Value);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(nodeList[i].Attributes[2].Value + ": " + nodeList[i].Attributes[1].Value + " " + nodeList[i].Attributes[3].Value + " " + nodeList[i].Attributes[4].Value);

                    GUILayout.EndHorizontal();

                }
                GUILayout.EndScrollView();
            }


            GUILayout.FlexibleSpace();

            GUI.color = Color.green;
            if (GUILayout.Button("Build", GUILayout.Height(50)))
            {
                Utility.StartBackgroundTask(StartBuild(Application.identifier.Replace(".", "_")));
            }
            GUI.color = Color.white;

        }
    }
    private void DrawIosSettings()
    {
        EditorGUILayout.Space();
        FindSettingsResourceFile(ref _automatedBuildSettingsData);
        {
            EditorGUILayout.LabelField("Build settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("Box");

            #region projectExportPath

            EditorGUILayout.BeginHorizontal();
            _automatedBuildSettingsData.iosProjectExportPath = EditorGUILayout.TextField("Project export path:", _automatedBuildSettingsData.iosProjectExportPath);
            if (GUILayout.Button("Browse", GUILayout.Width(100)))
            {
                var path = EditorUtility.OpenFolderPanel("Select project export path", Application.dataPath, "");
                _automatedBuildSettingsData.iosProjectExportPath = ConvertFromAbsoluteToRelativePath(path);
                GUI.FocusControl(string.Empty);
            }
            EditorGUILayout.EndHorizontal();

            #endregion

            #region gradle build file path

            EditorGUILayout.BeginHorizontal();
            _automatedBuildSettingsData.iosGradleRootFolder = EditorGUILayout.TextField("Gradle root folder:", _automatedBuildSettingsData.iosGradleRootFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(100)))
            {
                var path = EditorUtility.OpenFolderPanel("Select gradle root folder", Application.dataPath, "");
                _automatedBuildSettingsData.iosGradleRootFolder = ConvertFromAbsoluteToRelativePath(path);
                GUI.FocusControl(string.Empty);
            }
            EditorGUILayout.EndHorizontal();

            #endregion

            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();

            EditorGUILayout.Space();

            if (GUILayout.Button("Build", GUILayout.Width(100), GUILayout.Height(30)))
            {
                SaveSettings();
                IosBuildAutomated();
            }

            EditorGUILayout.Space();


        }




    }
    public string errorComment;
    private IEnumerator Create()
    {
        using (UnityWebRequest w = UnityWebRequest.Get("http://80.199.157.146:8111/app/rest/projects/IsItCakeGit/buildTypes"))
        {

            string createTemplate = _automatedBuildSettingsData.jsonFile.text;
            createTemplate = createTemplate.Replace("<!project_name!>", Application.identifier);
            createTemplate = createTemplate.Replace("<!project_id!>", Application.identifier).Replace(".", "_");


            w.method = "POST";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(createTemplate);
            w.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            w.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            w.SetRequestHeader("Content-Type", "application/json");
            w.SetRequestHeader("Authorization", "Bearer " + "eyJ0eXAiOiAiVENWMiJ9.eG9KcGVPVkxvSjN0MF9fQlloYi1fM2FIaVln.ZjNkMDdkOWItN2FmZi00NmM3LThkYWUtYzhlNzE3OWIzN2Ew");
            yield return w.SendWebRequest();

            while (w.isDone == false)
                yield return null;

            Debug.Log(w.downloadHandler.text);

            Utility.StartBackgroundTask(Edit());
        }
    }
    private IEnumerator Edit()
    {
        using (UnityWebRequest w = UnityWebRequest.Get("http://80.199.157.146:8111/app/rest/buildTypes/" + Application.identifier.Replace(".", "_") + "/parameters"))
        {

            string templateGit = _automatedBuildSettingsData.jsonFile2.text;

            templateGit = templateGit.Replace("<!project_gitpath!>", _automatedBuildSettingsData.gitPath);
            w.method = "POST";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(templateGit);
            w.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            w.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            w.SetRequestHeader("Content-Type", "application/json");
            w.SetRequestHeader("Authorization", "Bearer " + "eyJ0eXAiOiAiVENWMiJ9.eG9KcGVPVkxvSjN0MF9fQlloYi1fM2FIaVln.ZjNkMDdkOWItN2FmZi00NmM3LThkYWUtYzhlNzE3OWIzN2Ew");
            yield return w.SendWebRequest();

            while (w.isDone == false)
                yield return null;

            Debug.Log(w.downloadHandler.text);
        }
    }
    private IEnumerator Edit(string name, string value)
    {
        using (UnityWebRequest w = UnityWebRequest.Get("http://80.199.157.146:8111/app/rest/buildTypes/" + Application.identifier.Replace(".", "_") + "/parameters"))
        {

            string templateGit = _automatedBuildSettingsData.VariableTemplate.text;

            templateGit = templateGit.Replace("<!variable_name!>", name);
            templateGit = templateGit.Replace("<!variable_value!>", value);

            w.method = "POST";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(templateGit);
            w.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            w.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            w.SetRequestHeader("Content-Type", "application/json");
            w.SetRequestHeader("Authorization", "Bearer " + "eyJ0eXAiOiAiVENWMiJ9.eG9KcGVPVkxvSjN0MF9fQlloYi1fM2FIaVln.ZjNkMDdkOWItN2FmZi00NmM3LThkYWUtYzhlNzE3OWIzN2Ew");
            yield return w.SendWebRequest();

            while (w.isDone == false)
                yield return null;




            Debug.Log(w.downloadHandler.text);
        }
    }
    private IEnumerator GetProjects()
    {
        using (UnityWebRequest w = UnityWebRequest.Get("http://80.199.157.146:8111/app/rest/projects/IsItCakeGit"))
        {




            w.method = "GET";

            w.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            w.SetRequestHeader("Content-Type", "application/json");
            w.SetRequestHeader("Authorization", "Bearer " + "eyJ0eXAiOiAiVENWMiJ9.eG9KcGVPVkxvSjN0MF9fQlloYi1fM2FIaVln.ZjNkMDdkOWItN2FmZi00NmM3LThkYWUtYzhlNzE3OWIzN2Ew");
            yield return w.SendWebRequest();

            while (w.isDone == false)
                yield return null;

            Debug.Log(w.downloadHandler.text);

            xmlDoc = new XmlDocument();
            string text = w.downloadHandler.text;
            xmlDoc.Load(new StringReader(text));

        }
    }
    private IEnumerator GetBuildLog()
    {
        using (UnityWebRequest w = UnityWebRequest.Get("http://80.199.157.146:8111/app/rest/builds?locator=buildType:" + Application.identifier.Replace(".", "_")))
        {
            w.method = "GET";

            w.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            w.SetRequestHeader("Content-Type", "application/json");
            w.SetRequestHeader("Authorization", "Bearer " + "eyJ0eXAiOiAiVENWMiJ9.eG9KcGVPVkxvSjN0MF9fQlloYi1fM2FIaVln.ZjNkMDdkOWItN2FmZi00NmM3LThkYWUtYzhlNzE3OWIzN2Ew");
            yield return w.SendWebRequest();

            while (w.isDone == false)
                yield return null;

            Debug.Log(w.downloadHandler.text);

            buildLogXML = new XmlDocument();
            string text = w.downloadHandler.text;
            buildLogXML.Load(new StringReader(text));

        }
    }
    private IEnumerator StartBuild(string aBuild)
    {
        using (UnityWebRequest w = UnityWebRequest.Get("http://80.199.157.146:8111/app/rest/buildQueue"))
        {

            string json = "{\"buildType\": {\"id\": \"" + aBuild + "\"}}";

            //string createTemplate = _automatedBuildSettingsData.jsonFile.text;
            //createTemplate = createTemplate.Replace("<!project_name!>", Application.identifier);
            //createTemplate = createTemplate.Replace("<!project_id!>", Application.identifier).Replace(".", "_");


            w.method = "POST";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            w.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            w.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            w.SetRequestHeader("Content-Type", "application/json");
            w.SetRequestHeader("Authorization", "Bearer " + "eyJ0eXAiOiAiVENWMiJ9.eG9KcGVPVkxvSjN0MF9fQlloYi1fM2FIaVln.ZjNkMDdkOWItN2FmZi00NmM3LThkYWUtYzhlNzE3OWIzN2Ew");
            yield return w.SendWebRequest();

            while (w.isDone == false)
                yield return null;

            Debug.Log(w.downloadHandler.text);

            Utility.StartBackgroundTask(Edit());
        }
    }
    public static XmlDocument buildLogXML = null;



    public static XmlDocument xmlDoc = null;
    public void DrawTeamCityProjects()
    {


        if (GUILayout.Button("Refresh", GUILayout.Width(400), GUILayout.Height(30)))
        {



            Utility.StartBackgroundTask(GetProjects());




        }





        if (xmlDoc != null)
        {
            XmlNodeList nodeList;
            XmlNode root = xmlDoc.DocumentElement;

            //nodeList = root.SelectNodes("descendant::page[@id='1']");  //this selects all nodes with page id="1"
            //nodeList = root.ChildNodes;
            //nodeList = nodeList[0].ChildNodes;  //this rebuilds the list using all the childnodes from the nodes with page id ="1"
            nodeList = root.SelectNodes("/project/buildTypes")[0].ChildNodes;
            currentScroll = GUILayout.BeginScrollView(currentScroll);
            for (int i = 0; i < nodeList.Count; i++)
            {
                //print(nodeList[i].Attributes["id"].Value);
                //print(nodeList[i].InnerText);
                //GUILayout.Label(nodeList[i].Attributes[0].Value);
                GUILayout.Label(nodeList[i].Attributes[0].Value);


            }
            GUILayout.EndScrollView();
        }

    }

    public Vector2 currentScroll = new Vector2(0, 0);
    void CreateSettingsFile()
    {
        _assetSavePath = SETTINGS_DATA_PATH + SETTINGS_FILE_NAME + SETTINGS_RESOURCE_EXTENSION;
        _assetName = SETTINGS_FILE_NAME;
        MethodInfo method = typeof(AutomatedBuildWindow).GetMethod("CreateAsset");
        MethodInfo generic = method.MakeGenericMethod(typeof(AutomatedBuildSettings));
        generic.Invoke(this, new object[] { _assetName });

    }

    /// <summary>
    //	This makes it easy to create, name and place unique new ScriptableObject asset files.
    /// </summary>

    public void CreateAsset<T>(string assetName) where T : ScriptableObject
    {
        var asset = CreateInstance<T>();

        var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(_assetSavePath);

        AssetDatabase.CreateAsset(asset, assetPathAndName);
        AssetDatabase.SaveAssets();
    }


    void OnDisable()
    {
        SaveSettings();
    }


    void OnDestroy()
    {
        SaveSettings();
    }

    void SaveSettings()
    {
        EditorUtility.SetDirty(_automatedBuildSettingsData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }


    void OnEnable()
    {
        //CreateSettingsFile();
        //try to search for a settings file, otherwise just create a new one
        LoadAutonatedBuildSettings();
        _logConsoleList = new ReorderableList(_logOutput, typeof(string), false, false, false, false);

        AutomatedBuildSettings buildSettings = null;

    }


    void LoadAutonatedBuildSettings()
    {
        if (!FindSettingsResourceFile(ref _automatedBuildSettingsData))
        {
            CreateSettingsFile();
            FindSettingsResourceFile(ref _automatedBuildSettingsData);
        }
    }

    static bool FindSettingsResourceFile(ref AutomatedBuildSettings buildSettings)
    {
        //we already have a cached version of the build settings in the tool
        if (buildSettings != null) return true;

        //trying to find build settings
        var settingsFilePathGuids = AssetDatabase.FindAssets(SETTINGS_FILE_NAME).ToList();
        var settingsFilePathGuid = string.Empty;
        foreach (var filePathGuid in from filePathGuid in settingsFilePathGuids let filePath = AssetDatabase.GUIDToAssetPath(filePathGuid) where filePath.Contains(".asset") select filePathGuid)
        {
            settingsFilePathGuid = filePathGuid;
        }

        if (string.IsNullOrEmpty(settingsFilePathGuid)) return false;
        buildSettings = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(settingsFilePathGuid), typeof(AutomatedBuildSettings)) as AutomatedBuildSettings;
        return true;
    }

    #region methods to handle relative paths

    private static string ConvertFromAbsoluteToRelativePath(string filePath)
    {
        var absolutePath = filePath;
        var relativeReference = Application.dataPath;

        var relativePath = string.Empty;

        absolutePath = absolutePath.Replace("\\" + "", Path.DirectorySeparatorChar + "");
        relativeReference = relativeReference.Replace("\\" + "", Path.DirectorySeparatorChar + "");

        absolutePath = absolutePath.Replace("//" + "", Path.DirectorySeparatorChar + "");
        relativeReference = relativeReference.Replace("//" + "", Path.DirectorySeparatorChar + "");

        absolutePath = absolutePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        relativeReference = relativeReference.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        var absolutePathFolderList = absolutePath.Split(Path.DirectorySeparatorChar);
        var relativeReferenceFolderList = relativeReference.Split(Path.DirectorySeparatorChar);

        if (absolutePath.Contains(relativeReference))
        {
            //this is a subfolder of the Assets folder
            relativePath = absolutePath.Replace(relativeReference, "");

            //Debug.Log(relativePath);

            return relativePath;
        }

        //this is not a subfolder of the Assets folder
        var startRelativeDepth = 0;
        for (var i = 0; i < absolutePathFolderList.Count(); i++)
        {
            if (String.Compare(absolutePathFolderList[i], relativeReferenceFolderList[i], StringComparison.Ordinal) != 0)
            {
                startRelativeDepth = i;
                break;
            }
        }

        var numberOfLevels = relativeReferenceFolderList.Count() - startRelativeDepth;

        for (var i = 0; i < numberOfLevels; i++)
        {
            relativePath += ".." + Path.DirectorySeparatorChar;
        }

        for (var i = startRelativeDepth; i < absolutePathFolderList.Count(); i++)
        {
            if (i < absolutePathFolderList.Count() - 1)
            {
                relativePath += absolutePathFolderList[i] + Path.DirectorySeparatorChar;
            }
            else
            {
                relativePath += absolutePathFolderList[i];
            }
        }

        //Debug.Log(relativePath);

        return relativePath;
    }

    private static string ConvertFromRelativeToAbsolute(string filePath)
    {
        var absolutePath = string.Empty;

        var relativePath = filePath;
        var relativeReference = Application.dataPath;

        relativePath = relativePath.Replace("\\", Path.DirectorySeparatorChar + "");
        relativeReference = relativeReference.Replace("\\", Path.DirectorySeparatorChar + "");

        relativePath = relativePath.Replace("//", Path.DirectorySeparatorChar + "");
        relativeReference = relativeReference.Replace("//", Path.DirectorySeparatorChar + "");

        relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        relativeReference = relativeReference.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        var relativePathFolderList = relativePath.Split(Path.DirectorySeparatorChar).ToList();
        var relativeReferenceFolderList = relativeReference.Split(Path.DirectorySeparatorChar).ToList();

        var levelsUp = relativePathFolderList.Count(s => String.Compare(s, "..", StringComparison.Ordinal) == 0);

        if (levelsUp == 0)
        {
            //it's a subfolder
            absolutePath = Application.dataPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) + relativePath;

            //Debug.Log(absolutePath);

            return absolutePath;
        }

        //it's not in the Assets folder
        for (var i = 0; i < relativeReferenceFolderList.Count() - levelsUp; i++)
        {
            absolutePath += relativeReferenceFolderList[i] + Path.DirectorySeparatorChar;
        }

        relativePathFolderList.RemoveAll(s => String.Compare(s, "..", StringComparison.Ordinal) == 0);
        for (var i = 0; i < relativePathFolderList.Count(); i++)
        {
            if (i == 0)
            {
                absolutePath += relativePathFolderList[i];
            }
            else
            {
                absolutePath += Path.DirectorySeparatorChar + relativePathFolderList[i];
            }
        }

        //Debug.Log(absolutePath);

        return absolutePath;
    }

    #endregion


    /// <summary>
    /// This is called by the editor tool to export the eclipse project and the build the apk with all the associated libraries. This is ment to be used only in the local automated build.
    /// </summary>
    public void IosBuildAutomated()
    {
        //BuildIos();
        BuildAndroid();
        //start the gradle task
    }


    static void ParseExtraCommandLineArgs()
    {
        var commandLineArgs = Environment.GetCommandLineArgs();
        foreach (var commandLineArg in commandLineArgs)
        {
            if (commandLineArg.ToLower().Contains("enabledevbuild"))
            {
                Debug.Log("Extra commnad line argument:" + commandLineArg);
                SetDevBuild(commandLineArg);
            }
        }
    }

    private static BuildOptions _buildOptions = BuildOptions.None;

    static void SetDevBuild(string commandLineArg)
    {
        var devBuild = false;
        devBuild = commandLineArg.ToLower().Contains("true");

        if (devBuild)
        {
            Debug.Log("Creating a development build");
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                if (_buildOptions == BuildOptions.None)
                {
                    _buildOptions = BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler;
                }
                else
                {
                    _buildOptions |= BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler;
                }

                PlayerSettings.strippingLevel = StrippingLevel.StripAssemblies;
                PlayerSettings.stripEngineCode = true;
                PlayerSettings.SetPropertyInt("ScriptingBackend", (int)ScriptingImplementation.IL2CPP, BuildTargetGroup.iOS);
            }
            else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                _buildOptions = BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.AcceptExternalModificationsToPlayer;
            }
        }
    }


    static void BuildAndroid()
    {
        string[] defaultScene = {
            "Assets/Scenes/main.unity",
            };

        BuildPipeline.BuildPlayer(defaultScene, "FireFighterBlaze_.apk",
            BuildTarget.Android, BuildOptions.None);
    }

    /// <summary>
    /// Performs an ios build and copies the gradle files. Called by the CI server.
    /// </summary>
    public static void BuildIos()
    {

        ParseExtraCommandLineArgs();

        if (FindSettingsResourceFile(ref _automatedBuildSettingsData))
        {
            var editorBuildScenes = EditorBuildSettings.scenes;
            var scenes = (from editorBuildSettingsScene in editorBuildScenes
                          where editorBuildSettingsScene.enabled && !string.IsNullOrEmpty(editorBuildSettingsScene.path)
                          select editorBuildSettingsScene.path);

            var parsedProjectExportPath = ConvertFromRelativeToAbsolute(_automatedBuildSettingsData.iosProjectExportPath);
            var parsedGradleRootFolder = ConvertFromRelativeToAbsolute(_automatedBuildSettingsData.iosGradleRootFolder);

            //clean up the project export path
            DirectoryInfo directoryInfo = new DirectoryInfo(parsedProjectExportPath);


            if (!Directory.Exists(parsedProjectExportPath))
                Directory.CreateDirectory(parsedProjectExportPath);

            foreach (var dir in directoryInfo.GetDirectories())
            {
                RecursiveForceDelete(dir);
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
                //switch to the correct build target
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.iOS);
            }

            Debug.Log("Build options IOS: " + _buildOptions);

            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.connectProfiler = true;
            EditorUserBuildSettings.allowDebugging = true;


            //ios export project path
            BuildPipeline.BuildPlayer(scenes.ToArray(), parsedProjectExportPath, BuildTarget.iOS, BuildOptions.None);

            var sourcePath = parsedGradleRootFolder;
            var destinationPath = parsedProjectExportPath;


            Debug.Log("sourcePath" + sourcePath);
            Debug.Log("destinationPath" + destinationPath);

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                if (!dirPath.Contains(".svn"))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));


                }
            }
            Directory.CreateDirectory(destinationPath + "/fastlane");

            File.Copy(sourcePath + "/Gemfile", destinationPath + "/Gemfile", true);

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories).Where(s => !s.Contains(".meta")))
            {
                if (!newPath.Contains(".svn"))
                {
                    Debug.Log(newPath);
                    Debug.Log(newPath.Replace(sourcePath, destinationPath + "/fastlane"));

                    File.Copy(newPath, newPath.Replace(sourcePath, destinationPath + "/fastlane"), true);

                    File.SetAttributes(destinationPath + "/fastlane", FileAttributes.Normal);
                }
            }
        }
    }



    /// <summary>
    /// Recurse through the directory's contents and delete all files and directories,
    /// first setting the read only attribute on the files to false first.
    /// </summary>
    /// <param name="dir"></param>
    private static void RecursiveForceDelete(DirectoryInfo dir)
    {
        foreach (FileInfo file in dir.GetFiles())
        {
            try
            {
                file.IsReadOnly = false;
                file.Delete();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to delete file: " + file.FullName + ". Please make sure it's not open before building.\n" + e.Message);
            }
        }
        foreach (DirectoryInfo directory in dir.GetDirectories())
        {
            RecursiveForceDelete(directory);
        }
        try
        {
            dir.Delete();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to delete directory: " + dir.FullName + ". exception:\n" + e.Message);
        }
    }


    private void StartGradleTaskWindows(string workingDirectory = "")
    {
        Process process = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Normal,
            FileName = @workingDirectory + Path.DirectorySeparatorChar + "gradlew.bat",
            WorkingDirectory = @workingDirectory,
            Arguments = @"assemble",
            UseShellExecute = false,
            CreateNoWindow = false
        };

        try
        {
            // Start the process.
            process.StartInfo = startInfo;
            process.Start();

            // Display the process statistics until 
            // the user closes the program. 
            do
            {
                if (!process.HasExited)
                {
                    // Refresh the current process property values.
                    process.Refresh();
                    //Debug.Log("Process refresh");
                }
            } while (!process.WaitForExit(1000));

        }
        finally
        {
            process.Close();
            process.Dispose();
        }
    }
    /// <summary>
    /// Creates tabs from buttons, with their bottom edge removed by the magic of Haxx
    /// </summary>
    /// <remarks> 
    /// The line will be misplaced if other elements is drawn before this
    /// </remarks> 
    /// <returns>Selected tab</returns>
    public static int Tabs(string[] options, int selected)
    {
        const float darkGray = 0.4f;
        const float lightGray = 0.9f;
        const float startSpace = 10;

        GUILayout.Space(startSpace);
        Color storeColor = GUI.backgroundColor;
        Color highlightCol = new Color(darkGray, darkGray, darkGray);
        Color bgCol = new Color(lightGray, lightGray, lightGray);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { padding = { bottom = 8 } };

        GUILayout.BeginHorizontal();
        {   //Create a row of buttons
            for (int i = 0; i < options.Length; ++i)
            {
                GUI.backgroundColor = i == selected ? highlightCol : bgCol;
                if (GUILayout.Button(options[i], buttonStyle))
                {
                    selected = i; //Tab click
                }
            }
        }
        GUILayout.EndHorizontal();
        //Restore color
        GUI.backgroundColor = storeColor;
        //Draw a line over the bottom part of the buttons (ugly haxx)
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, highlightCol);
        texture.Apply();
        GUI.DrawTexture(new Rect(0, buttonStyle.lineHeight + buttonStyle.border.top + buttonStyle.margin.top + startSpace, UnityEngine.Screen.width, 4), texture);

        return selected;
    }




}
