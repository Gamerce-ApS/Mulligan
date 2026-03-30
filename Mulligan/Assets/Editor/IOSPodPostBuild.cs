using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class IOSPodPostBuild
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
            return;

        Debug.Log("[IOSPodPostBuild] Post build started");
        Debug.Log("[IOSPodPostBuild] Build path: " + pathToBuiltProject);

        if (!Directory.Exists(pathToBuiltProject))
        {
            Debug.LogError("[IOSPodPostBuild] Build folder does not exist: " + pathToBuiltProject);
            return;
        }

        string podfilePath = Path.Combine(pathToBuiltProject, "Podfile");
        if (!File.Exists(podfilePath))
        {
            Debug.LogError("[IOSPodPostBuild] No Podfile found at: " + podfilePath);
            return;
        }

        string podCommand = FindPodCommand();

        if (string.IsNullOrEmpty(podCommand))
        {
            Debug.LogError("[IOSPodPostBuild] CocoaPods was not found on this machine.");
            Debug.LogError("[IOSPodPostBuild] Install CocoaPods on the build server or add it to PATH.");
            return;
        }

        Debug.Log("[IOSPodPostBuild] Using pod command: " + podCommand);

        PatchPodfile(podfilePath);

        RunProcess("/bin/zsh", $"-lc 'cd \"{pathToBuiltProject}\" && \"{podCommand}\" install'", "pod install");

        string workspacePath = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcworkspace");
        if (Directory.Exists(workspacePath))
            Debug.Log("[IOSPodPostBuild] Workspace created: " + workspacePath);
        else
            Debug.LogError("[IOSPodPostBuild] Workspace was not created: " + workspacePath);

        FixXcodeProject(pathToBuiltProject);
    }

    private static string FindPodCommand()
    {
        string[] possiblePaths =
        {
            "/opt/homebrew/bin/pod",
            "/usr/local/bin/pod",
            "/usr/bin/pod",
            "/opt/homebrew/lib/ruby/gems/4.0.0/bin/pod"
        };

        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        string whichResult = RunProcessAndGetOutput("/bin/zsh", "-lc 'which pod'");

        if (!string.IsNullOrWhiteSpace(whichResult))
        {
            string foundPath = whichResult.Trim();
            if (File.Exists(foundPath))
                return foundPath;
        }

        return "pod";
    }

    private static void PatchPodfile(string podfilePath)
    {
        string podfile = File.ReadAllText(podfilePath);

        const string marker = "# IOSPODPOSTBUILD_SWIFT_FIX";

        if (podfile.Contains(marker))
        {
            Debug.Log("[IOSPodPostBuild] Podfile already patched");
            return;
        }

        string postInstallBlock = @"

" + marker + @"
post_install do |installer|
  installer.pods_project.targets.each do |target|
    target.build_configurations.each do |config|
      config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] = '13.0'
    end
  end

  installer.aggregate_targets.each do |aggregate_target|
    user_project = aggregate_target.user_project

    user_project.native_targets.each do |target|
      target.build_configurations.each do |config|
        if target.name == 'UnityFramework'
          config.build_settings['ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES'] = 'NO'
        elsif target.name == 'Unity-iPhone'
          config.build_settings['ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES'] = 'YES'
        end
      end
    end

    user_project.save
  end
end
";

        podfile += postInstallBlock;
        File.WriteAllText(podfilePath, podfile);

        Debug.Log("[IOSPodPostBuild] Podfile patched with Swift embed fix");
    }

    private static void FixXcodeProject(string pathToBuiltProject)
    {
        string pbxProjectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);

        if (!File.Exists(pbxProjectPath))
        {
            Debug.LogError("[IOSPodPostBuild] PBX project not found at: " + pbxProjectPath);
            return;
        }

        var pbxProject = new PBXProject();
        pbxProject.ReadFromFile(pbxProjectPath);

        string mainTargetGuid = pbxProject.GetUnityMainTargetGuid();
        string frameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();

        Debug.Log("[IOSPodPostBuild] Fixing Xcode build settings");

        pbxProject.SetBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
        pbxProject.SetBuildProperty(frameworkTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");

        pbxProject.WriteToFile(pbxProjectPath);

        Debug.Log("[IOSPodPostBuild] Set ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES = YES for Unity-iPhone");
        Debug.Log("[IOSPodPostBuild] Set ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES = NO for UnityFramework");
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

        Debug.Log($"[IOSPodPostBuild] Running {label}");
        Debug.Log($"[IOSPodPostBuild] Command: {fileName} {arguments}");

        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        Debug.Log($"[IOSPodPostBuild] {label} exit code: {process.ExitCode}");

        if (!string.IsNullOrWhiteSpace(output))
            Debug.Log($"[IOSPodPostBuild] {label} stdout:\n{output}");

        if (!string.IsNullOrWhiteSpace(error))
            Debug.LogWarning($"[IOSPodPostBuild] {label} stderr:\n{error}");

        if (process.ExitCode != 0)
            Debug.LogError($"[IOSPodPostBuild] {label} failed");
    }

    private static string RunProcessAndGetOutput(string fileName, string arguments)
    {
        var process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output;
    }
}