using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class AutomatedBuildSettings : ScriptableObject
{
    //#region Android
    //#region Android signing credentials
    //public string keyStorePath = string.Empty;
    //public string keyStorePassword = string.Empty;
    //public string keyAliasName = string.Empty;
    //public string keyAliasPassword = string.Empty;
    //#endregion

    //public bool useApkExpansionFiles = false;

    //public string projectExportPath = string.Empty;
    //public string gradleBuildFilePath = string.Empty;
    //public string gradleRootFolder = string.Empty;

    //public string unityApplicationDataPath = string.Empty;
    //public AutomatedBuildWindow.GradleTask gradleTask = AutomatedBuildWindow.GradleTask.Assemble;
    //#endregion

    #region Ios

    public string iosProjectExportPath = string.Empty;
    public string iosGradleRootFolder = string.Empty;
    public string gitPath = string.Empty;
    public string appName = string.Empty;
    public string appSKU = string.Empty;

    public string motherappLinkName = string.Empty;
    public string appLinkName = string.Empty;


    public TextAsset jsonFile;
    public TextAsset jsonFile2;
    public TextAsset offlienDataTest;
    public TextAsset VariableTemplate;

    

    public string app_identifier = string.Empty;
    public string apple_id = string.Empty;
    public string itc_team_id = string.Empty;
    public string team_id = string.Empty;


    #endregion
}



public static class Utility
{
    public static void StartBackgroundTask(IEnumerator update, Action end = null)
    {
        EditorApplication.CallbackFunction closureCallback = null;

        closureCallback = () =>
        {
            try
            {
                if (update.MoveNext() == false)
                {
                    if (end != null)
                        end();
                    EditorApplication.update -= closureCallback;
                }
            }
            catch (Exception ex)
            {
                if (end != null)
                    end();
                Debug.LogException(ex);
                EditorApplication.update -= closureCallback;
            }
        };

        EditorApplication.update += closureCallback;
    }
}