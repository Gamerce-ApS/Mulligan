using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class IOSPodPostBuild
{
    private const string PodPath = "/opt/homebrew/lib/ruby/gems/4.0.0/bin/pod";

    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
            return;

        UnityEngine.Debug.Log("[IOSPodPostBuild] Post build started");
        UnityEngine.Debug.Log("[IOSPodPostBuild] Build path: " + pathToBuiltProject);
        UnityEngine.Debug.Log("[IOSPodPostBuild] Pod path: " + PodPath);

        if (!Directory.Exists(pathToBuiltProject))
        {
            UnityEngine.Debug.LogError("[IOSPodPostBuild] Build folder does not exist: " + pathToBuiltProject);
            return;
        }

        if (!File.Exists(PodPath))
        {
            UnityEngine.Debug.LogError("[IOSPodPostBuild] CocoaPods not found at: " + PodPath);
            return;
        }

        string podfilePath = Path.Combine(pathToBuiltProject, "Podfile");
        if (!File.Exists(podfilePath))
        {
            UnityEngine.Debug.LogError("[IOSPodPostBuild] No Podfile found at: " + podfilePath);
            UnityEngine.Debug.LogError("[IOSPodPostBuild] CocoaPods cannot run without a Podfile.");
            return;
        }

        RunProcess("/bin/zsh", $"-lc 'cd \"{pathToBuiltProject}\" && \"{PodPath}\" install'", "pod install");

        string workspacePath = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcworkspace");
        if (Directory.Exists(workspacePath))
        {
            UnityEngine.Debug.Log("[IOSPodPostBuild] Workspace created: " + workspacePath);
        }
        else
        {
            UnityEngine.Debug.LogError("[IOSPodPostBuild] Workspace was not created: " + workspacePath);
        }

        FixXcodeProject(pathToBuiltProject);
    }

    private static void FixXcodeProject(string pathToBuiltProject)
    {
        string pbxProjectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);

        if (!File.Exists(pbxProjectPath))
        {
            UnityEngine.Debug.LogError("[IOSPodPostBuild] PBX project not found at: " + pbxProjectPath);
            return;
        }

        var pbxProject = new PBXProject();
        pbxProject.ReadFromFile(pbxProjectPath);

        string mainTargetGuid = pbxProject.GetUnityMainTargetGuid();
        string frameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();

        UnityEngine.Debug.Log("[IOSPodPostBuild] Fixing Xcode build settings");

        // Main app target can embed Swift libs if needed by pods/plugins
        pbxProject.SetBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

        // UnityFramework must not embed frameworks inside itself
        pbxProject.SetBuildProperty(frameworkTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");

        pbxProject.WriteToFile(pbxProjectPath);

        UnityEngine.Debug.Log("[IOSPodPostBuild] Set ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES = YES for main target");
        UnityEngine.Debug.Log("[IOSPodPostBuild] Set ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES = NO for UnityFramework target");
    }

    private static void RunProcess(string fileName, string arguments, string label)
    {
        var process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

        UnityEngine.Debug.Log($"[IOSPodPostBuild] Running {label}");
        UnityEngine.Debug.Log($"[IOSPodPostBuild] Command: {fileName} {arguments}");

        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        UnityEngine.Debug.Log($"[IOSPodPostBuild] {label} exit code: {process.ExitCode}");

        if (!string.IsNullOrWhiteSpace(output))
            UnityEngine.Debug.Log($"[IOSPodPostBuild] {label} stdout:\n{output}");

        if (!string.IsNullOrWhiteSpace(error))
            UnityEngine.Debug.LogWarning($"[IOSPodPostBuild] {label} stderr:\n{error}");

        if (process.ExitCode != 0)
            UnityEngine.Debug.LogError($"[IOSPodPostBuild] {label} failed");
    }
}