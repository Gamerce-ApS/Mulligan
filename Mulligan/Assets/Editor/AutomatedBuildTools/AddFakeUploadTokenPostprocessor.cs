#if UNITY_IOS

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

/// <summary>
/// Adding fake USYM_UPLOAD_AUTH_TOKEN if not already set, to avoid cli build errors
/// see https://forum.unity.com/threads/ios-build-is-failing-seems-like-a-fastlane-problem-not-sure-how-to-proceed.682201/#post-4701557
/// </summary>
public class AddFakeUploadTokenPostprocessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 100;

    public void OnPostprocessBuild(BuildReport report)
    {
        var pathToBuiltProject = report.summary.outputPath;
        var target = report.summary.platform;
        if (target != BuildTarget.iOS)
        {
            return;
        }

        Debug.LogFormat("Postprocessing build at \"{0}\" for target {1}", pathToBuiltProject, target);
        PBXProject project = new PBXProject();
        string pbxFilename = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
        project.ReadFromFile(pbxFilename);

#if UNITY_2019_3_OR_NEWER
        string targetGUID = project.GetUnityMainTargetGuid();
        
#else
        string targetName = PBXProject.GetUnityTargetName();
        string targetGUID = project.TargetGuidByName(targetName);
#endif

        var token = project.GetBuildPropertyForAnyConfig(targetGUID, "USYM_UPLOAD_AUTH_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            token = "FakeToken";
        }

        

        project.SetBuildProperty(targetGUID, "USYM_UPLOAD_AUTH_TOKEN", token);
        if(!string.IsNullOrEmpty (project.TargetGuidByName("Debug")) && !string.IsNullOrEmpty(project.GetBuildPropertyForConfig(targetGUID, "USYM_UPLOAD_AUTH_TOKEN")))
            project.SetBuildProperty(project.TargetGuidByName("Debug"), "USYM_UPLOAD_AUTH_TOKEN", token);
        if (!string.IsNullOrEmpty(project.TargetGuidByName("Release")) && !string.IsNullOrEmpty(project.GetBuildPropertyForConfig(targetGUID, "USYM_UPLOAD_AUTH_TOKEN")))
            project.SetBuildProperty(project.TargetGuidByName("Release"), "USYM_UPLOAD_AUTH_TOKEN", token);
        if (!string.IsNullOrEmpty(project.TargetGuidByName("ReleaseForRunning")) && !string.IsNullOrEmpty(project.GetBuildPropertyForConfig(targetGUID, "USYM_UPLOAD_AUTH_TOKEN")))
            project.SetBuildProperty(project.TargetGuidByName("ReleaseForRunning"), "USYM_UPLOAD_AUTH_TOKEN", token);
        if (!string.IsNullOrEmpty(project.TargetGuidByName("ReleaseForProfiling")) && !string.IsNullOrEmpty(project.GetBuildPropertyForConfig(targetGUID, "USYM_UPLOAD_AUTH_TOKEN")))
            project.SetBuildProperty(project.TargetGuidByName("ReleaseForProfiling"), "USYM_UPLOAD_AUTH_TOKEN", token);


        
            
            
        project.WriteToFile(pbxFilename);
    }
}

#endif