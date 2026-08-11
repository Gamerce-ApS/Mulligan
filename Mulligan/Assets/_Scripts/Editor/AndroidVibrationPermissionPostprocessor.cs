#if UNITY_ANDROID
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;

public class AndroidVibrationPermissionPostprocessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder
    {
        get { return 0; }
    }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = GetManifestPath(path);
        if (!File.Exists(manifestPath))
            return;

        XDocument document = XDocument.Load(manifestPath);
        XElement manifest = document.Root;
        if (manifest == null)
            return;

        XNamespace android = "http://schemas.android.com/apk/res/android";
        bool hasVibrationPermission = manifest.Elements("uses-permission")
            .Any(permission => (string)permission.Attribute(android + "name") == "android.permission.VIBRATE");

        if (hasVibrationPermission)
            return;

        manifest.AddFirst(new XElement("uses-permission", new XAttribute(android + "name", "android.permission.VIBRATE")));
        document.Save(manifestPath);
    }

    private string GetManifestPath(string path)
    {
        string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
        if (File.Exists(manifestPath))
            return manifestPath;

        return Path.Combine(path, "unityLibrary/src/main/AndroidManifest.xml");
    }
}
#endif
