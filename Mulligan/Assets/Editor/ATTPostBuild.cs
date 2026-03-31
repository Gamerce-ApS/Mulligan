using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public static class ATTPostBuild
{
    [PostProcessBuild(1000)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");

        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict rootDict = plist.root;

        rootDict.SetString(
            "NSUserTrackingUsageDescription",
            "This data is used to deliver better ads and improve your experience."
        );

        File.WriteAllText(plistPath, plist.WriteToString());
    }
}