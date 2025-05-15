using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.Linq;

#if UNITY_IOS || UNITY_TVOS
using UnityEditor.iOS.Xcode;
// Create specific aliases for iOS.Xcode imports.
// Unity Editor on macOS can report a conflict with other plugins
using PlistDocument = UnityEditor.iOS.Xcode.PlistDocument;
using PlistElementDict = UnityEditor.iOS.Xcode.PlistElementDict;
#endif




public class UpdateXcodeBuildSystemPostProcessor : MonoBehaviour
{
    [PostProcessBuild(99)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS || buildTarget == BuildTarget.tvOS)
        {
            UpdateXcodeBuildSystem(path);
        }
    }

    private static void UpdateXcodeBuildSystem(string projectPath)
    {
		
		#if UNITY_IOS || UNITY_TVOS
        string workspaceSettingsPath = Path.Combine(projectPath,
            "Unity-iPhone.xcodeproj/project.xcworkspace/xcshareddata/" +
            "WorkspaceSettings.xcsettings");

        if (File.Exists(workspaceSettingsPath))
        {
            // Read the plist document, and find the root element
            PlistDocument workspaceSettings = new PlistDocument();
            workspaceSettings.ReadFromFile(workspaceSettingsPath);
            PlistElementDict root = workspaceSettings.root;

            // Modify the document as necessary.
            bool workspaceSettingsChanged = false;
            // Remove the BuildSystemType entry because it specifies the
            // legacy Xcode build system, which is deprecated

            if (root.values.ContainsKey("BuildSystemType"))
            {
                root.values.Remove("BuildSystemType");
                workspaceSettingsChanged = true;
            }

            // If actual changes to the document occurred, write the result
            // back to disk.
            if (workspaceSettingsChanged)
            {
                Debug.Log("UpdateXcodeBuildSystem: Writing updated " +
                    "workspace settings to disk.");

                try
                {
                    workspaceSettings.WriteToFile(workspaceSettingsPath);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(string.Format("UpdateXcodeBuildSystem: " +
                        "Exception occurred writing workspace settings to " +
                        "disk: \n{0}",
                        e.Message));
                    throw;
                }
            }
            else
            {
                Debug.Log("UpdateXcodeBuildSystem: workspace settings did " +
                    "not require modifications.");
            }
        }
        else
        {
            Debug.LogWarningFormat("UpdateXcodeBuildSystem: could not find " +
                "workspace settings files [{0}]",
                workspaceSettingsPath);
        }

        // Get the path to the Xcode project
        string pbxProjectPath = PBXProject.GetPBXProjectPath(projectPath);
        var pbxProject = new PBXProject();

        // Open the Xcode project
        pbxProject.ReadFromFile(pbxProjectPath);

        // Get the UnityFramework target GUID
        string unityFrameworkTargetGuid =
            pbxProject.GetUnityFrameworkTargetGuid();

        // Modify the Swift version in the UnityFramework target to a
        // compatible string
        pbxProject.SetBuildProperty(unityFrameworkTargetGuid,
            "SWIFT_VERSION", "5.0");

#if UNITY_2019_3_OR_NEWER
        string targetGUID = pbxProject.GetUnityMainTargetGuid();
        
#else
        string targetName = PBXProject.GetUnityTargetName();
        string targetGUID = project.TargetGuidByName(targetName);
#endif

        var token = pbxProject.GetBuildPropertyForAnyConfig(targetGUID, "USYM_UPLOAD_AUTH_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            token = "FakeToken";
        }
        pbxProject.SetBuildProperty(targetGUID, "USYM_UPLOAD_AUTH_TOKEN", token);

        string target = pbxProject.GetUnityMainTargetGuid();
        pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
        target = pbxProject.TargetGuidByName(PBXProject.GetUnityTestTargetName());
        pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
        target = pbxProject.GetUnityFrameworkTargetGuid();
        pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");


        // Write out the Xcode project
        pbxProject.WriteToFile(pbxProjectPath);

        Debug.Log("UpdateXcodeBuildSystem: update Swift version in Xcode " +
            "project.");




#endif



    }
    static bool FindSettingsResourceFile(ref AutomatedBuildSettings buildSettings)
    {
        Debug.Log("FindSettingsResourceFile");
        string SETTINGS_FILE_NAME = "AutomatedBuildSettings";
        //we already have a cached version of the build settings in the tool
        //if (buildSettings != null) return true;

        //trying to find build settings
        var settingsFilePathGuids = AssetDatabase.FindAssets(SETTINGS_FILE_NAME).ToList();
        var settingsFilePathGuid = string.Empty;
        foreach (var filePathGuid in from filePathGuid in settingsFilePathGuids let filePath = AssetDatabase.GUIDToAssetPath(filePathGuid) where filePath.Contains(".asset") select filePathGuid)
        {
            settingsFilePathGuid = filePathGuid;
        }

        if (string.IsNullOrEmpty(settingsFilePathGuid)) return false;
        buildSettings = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(settingsFilePathGuid), typeof(AutomatedBuildSettings)) as AutomatedBuildSettings;

        Debug.Log("FOUND FILE!!");
        return true;
    }
    [PostProcessBuild]
    public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
    {

    //    if (buildTarget == BuildTarget.iOS)
    //    {
    //        Debug.Log("Starts!!!");
    //        AutomatedBuildSettings _automatedBuildSettingsData = new AutomatedBuildSettings();
    //FindSettingsResourceFile(ref _automatedBuildSettingsData);
    //        {

    //            Debug.Log("FoundSettingsFile");
    //            // Get plist
    //            string plistPath = pathToBuiltProject + "/Info.plist";
    //            PlistDocument plist = new PlistDocument();
    //            plist.ReadFromString(File.ReadAllText(plistPath));

    //            // Get root
    //            PlistElementDict rootDict = plist.root;

    //            // Change value of CFBundleVersion in Xcode plist


    //            PlistElementArray bgModes = rootDict.CreateArray("LSApplicationQueriesSchemes");
    //            bgModes.AddString(_automatedBuildSettingsData.motherappLinkName);
    //            bgModes.AddString("bek2");
    //            bgModes.AddString("billetrille");
    //            bgModes.AddString("ChristmasGame");
    //            bgModes.AddString("dressup");
    //            bgModes.AddString("madmagician");
                

    //            PlistElementArray test = rootDict.CreateArray("CFBundleURLTypes");

    //            //var dict = test.AddDict();
    //            //dict.CreateArray("kidsplatform");


    //            var urlTypes = plist.root["CFBundleURLTypes"].AsArray();
    //            var dict = urlTypes.AddDict();
    //            dict.SetString("CFBundleTypeRole", "Editor");
    //            dict.SetString("CFBundleURLName", _automatedBuildSettingsData.app_identifier);
    //            var array = dict.CreateArray("CFBundleURLSchemes");
    //            array.AddString(_automatedBuildSettingsData.appLinkName);











    //            // Write to file
    //            File.WriteAllText(plistPath, plist.WriteToString());





        //    }

 


        //}
    }


}
