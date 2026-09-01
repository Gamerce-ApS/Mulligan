using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class AnimatedPortraitBuilderWindow : EditorWindow
{
    private Object selectedAsset;
    private string manifestPath = "";
    private string folderPath = "";
    private string outputPath = "";
    private PortraitManifest manifest;
    private readonly List<string> errors = new List<string>();
    private readonly List<string> warnings = new List<string>();
    private GameObject previewObject;
    private bool previewSource;

    [MenuItem("Tools/Mulligan Rush/Animated Portrait Builder")]
    public static void Open()
    {
        GetWindow<AnimatedPortraitBuilderWindow>("Portrait Builder");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Animated Portrait Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        selectedAsset = EditorGUILayout.ObjectField("Portrait JSON or Folder", selectedAsset, typeof(Object), false);
        if (EditorGUI.EndChangeCheck())
            ResolveSelectedAsset();

        EditorGUILayout.LabelField("Manifest", string.IsNullOrEmpty(manifestPath) ? "None" : manifestPath);
        EditorGUILayout.LabelField("Output Path", string.IsNullOrEmpty(outputPath) ? "None" : outputPath);

        if (manifest != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Character", manifest.characterName);
            EditorGUILayout.LabelField("Source Resolution", manifest.sourceWidth + " x " + manifest.sourceHeight);
            EditorGUILayout.LabelField("Detected Layers", manifest.layers != null ? manifest.layers.Count.ToString() : "0");
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate"))
            Validate();

        GUI.enabled = errors.Count == 0 && manifest != null;
        if (GUILayout.Button("Build Portrait"))
            BuildPortrait();

        if (GUILayout.Button("Rebuild Portrait"))
            BuildPortrait();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = File.Exists(outputPath);
        if (GUILayout.Button("Select Prefab"))
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);

        if (GUILayout.Button(previewSource ? "Preview Generated" : "Preview Source"))
            TogglePreviewSource();

        if (GUILayout.Button("Preview Idle"))
            PreviewIdle();

        if (GUILayout.Button("Stop Preview"))
            StopPreview();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        DrawValidationStatus();
        HandleDragAndDrop();
    }

    void OnDisable()
    {
        StopPreview();
    }

    private void ResolveSelectedAsset()
    {
        manifestPath = "";
        folderPath = "";
        outputPath = "";
        manifest = null;
        errors.Clear();
        warnings.Clear();

        if (selectedAsset == null)
            return;

        string path = AssetDatabase.GetAssetPath(selectedAsset);
        if (string.IsNullOrEmpty(path))
            return;

        if (Directory.Exists(path))
        {
            folderPath = path;
            manifestPath = Path.Combine(folderPath, "portrait.json").Replace("\\", "/");
        }
        else if (Path.GetFileName(path) == "portrait.json")
        {
            manifestPath = path;
            folderPath = Path.GetDirectoryName(path).Replace("\\", "/");
        }
        else
        {
            manifestPath = path;
            folderPath = Path.GetDirectoryName(path).Replace("\\", "/");
        }

        LoadManifest();
    }

    private void LoadManifest()
    {
        manifest = null;
        outputPath = "";

        if (File.Exists(manifestPath) == false)
            return;

        try
        {
            manifest = PortraitManifest.FromJson(File.ReadAllText(manifestPath));
            string characterName = string.IsNullOrEmpty(manifest.characterName)
                ? Path.GetFileName(folderPath)
                : manifest.characterName;
            outputPath = (folderPath + "/Generated/" + characterName + "_AnimatedPortrait.prefab").Replace("\\", "/");
        }
        catch (System.Exception e)
        {
            errors.Add("JSON parse failed: " + e.Message);
        }
    }

    private void Validate()
    {
        errors.Clear();
        warnings.Clear();

        if (string.IsNullOrEmpty(manifestPath) || File.Exists(manifestPath) == false)
        {
            errors.Add("portrait.json could not be found.");
            return;
        }

        LoadManifest();
        if (manifest == null)
        {
            errors.Add("portrait.json did not parse.");
            return;
        }

        if (manifest.sourceWidth <= 0 || manifest.sourceHeight <= 0)
            errors.Add("sourceWidth and sourceHeight must be greater than 0.");

        if (manifest.layers == null || manifest.layers.Count == 0)
            errors.Add("No layers found in portrait.json.");

        bool hasBody = false;
        bool hasHead = false;
        HashSet<string> layerNames = new HashSet<string>();

        for (int i = 0; manifest.layers != null && i < manifest.layers.Count; i++)
        {
            PortraitLayerDefinition layer = manifest.layers[i];
            if (layer == null)
                continue;

            if (string.IsNullOrEmpty(layer.name))
                errors.Add("Layer " + i + " has no name.");
            else if (layerNames.Add(layer.name) == false)
                errors.Add("Duplicate layer name: " + layer.name);

            if (layer.type == "body")
                hasBody = true;
            if (layer.type == "head")
                hasHead = true;

            if (string.IsNullOrEmpty(layer.file))
            {
                if (layer.required)
                    errors.Add("Required layer " + layer.name + " has no file.");
                continue;
            }

            string layerPath = GetLayerPath(layer);
            if (File.Exists(layerPath) == false)
            {
                if (layer.required)
                    errors.Add("Required layer file missing: " + layerPath);
                else
                    warnings.Add("Optional layer file missing: " + layerPath);
                continue;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(layerPath);
            if (texture == null)
            {
                errors.Add("Could not load texture: " + layerPath);
                continue;
            }

            if (texture.width != manifest.sourceWidth || texture.height != manifest.sourceHeight)
                errors.Add(layer.file + " is " + texture.width + "x" + texture.height + ", expected " + manifest.sourceWidth + "x" + manifest.sourceHeight + ".");

            ValidatePivot(layer.name, layer.pivot, "Layer pivot");
            if (layer.spring != null)
                ValidateSpring(layer);
        }

        if (hasBody == false)
            warnings.Add("No layer with type 'body' found.");
        if (hasHead == false)
            warnings.Add("No layer with type 'head' found.");

        for (int i = 0; manifest.recommendedLayerOrder != null && i < manifest.recommendedLayerOrder.Count; i++)
        {
            if (layerNames.Contains(manifest.recommendedLayerOrder[i]) == false)
                errors.Add("recommendedLayerOrder references missing layer: " + manifest.recommendedLayerOrder[i]);
        }

        for (int i = 0; manifest.layers != null && i < manifest.layers.Count; i++)
        {
            PortraitLayerDefinition layer = manifest.layers[i];
            if (layer != null && string.IsNullOrEmpty(layer.parent) == false && layerNames.Contains(layer.parent) == false)
                errors.Add(layer.name + " references missing parent: " + layer.parent);
        }

        if (manifest.pivots != null)
        {
            foreach (KeyValuePair<string, PortraitPivotDefinition> pivot in manifest.pivots)
            {
                if (layerNames.Contains(pivot.Key) == false)
                    warnings.Add("Pivot references unknown layer: " + pivot.Key);

                ValidatePivot(pivot.Key, pivot.Value, "Manifest pivot");
            }
        }

        if (manifest.animation != null && manifest.animation.blink != null && manifest.animation.blink.enabled)
        {
            bool hasBlink = false;
            for (int i = 0; manifest.layers != null && i < manifest.layers.Count; i++)
            {
                if (manifest.layers[i] != null && manifest.layers[i].type == "blinkOverlay")
                    hasBlink = true;
            }

            if (hasBlink == false)
                warnings.Add("Blinking is enabled, but no blinkOverlay layer exists.");
        }
    }

    private void BuildPortrait()
    {
        Validate();
        if (errors.Count > 0 || manifest == null)
            return;

        string generatedFolder = folderPath + "/Generated";
        if (AssetDatabase.IsValidFolder(generatedFolder) == false)
            AssetDatabase.CreateFolder(folderPath, "Generated");

        List<PortraitLayerDefinition> buildLayers = GetOrderedLayers();
        for (int i = 0; i < buildLayers.Count; i++)
            ApplySpriteImportSettings(GetLayerPath(buildLayers[i]));

        GameObject root = new GameObject(manifest.characterName + "_AnimatedPortrait", typeof(RectTransform), typeof(AnimatedPortrait));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        SetupRect(rootRect, new Vector2(manifest.sourceWidth, manifest.sourceHeight), Vector2.zero);

        AnimatedPortrait animatedPortrait = root.GetComponent<AnimatedPortrait>();
        animatedPortrait.ApplyManifest(manifest);

        Dictionary<string, RectTransform> pivotByLayer = new Dictionary<string, RectTransform>();
        Dictionary<string, Vector2> pivotPositionByLayer = new Dictionary<string, Vector2>();

        for (int i = 0; i < buildLayers.Count; i++)
        {
            PortraitLayerDefinition layer = buildLayers[i];
            string layerPath = GetLayerPath(layer);
            if (File.Exists(layerPath) == false)
                continue;

            RectTransform parent = GetParentRect(layer, rootRect, pivotByLayer);
            Vector2 parentPivotPosition = GetParentPivotPosition(layer, pivotPositionByLayer);
            RectTransform layerRoot;
            RectTransform visual;
            Vector2 layerPivotPosition = GetLayerPivotPosition(layer);

            if (RequiresPivot(layer))
            {
                layerRoot = CreateRect(layer.name + "Pivot", parent, Vector2.zero, layerPivotPosition - parentPivotPosition);
                visual = CreateImage(layer.name, layerRoot, layerPath, -layerPivotPosition);
                pivotByLayer[layer.name] = layerRoot;
                pivotPositionByLayer[layer.name] = layerPivotPosition;
            }
            else
            {
                Vector2 position = parent == rootRect ? Vector2.zero : -parentPivotPosition;
                visual = CreateImage(layer.name, parent, layerPath, position);
                layerRoot = visual;
            }

            if (layer.type == "body")
                animatedPortrait.Body = visual;
            else if (layer.type == "head")
                animatedPortrait.HeadPivot = layerRoot;
            else if (layer.type == "blinkOverlay")
            {
                visual.gameObject.SetActive(false);
                animatedPortrait.BlinkOverlays.Add(visual.gameObject);
            }
            else if (layer.type == "spring")
            {
                AnimatedPortraitSpringPart spring = layerRoot.gameObject.AddComponent<AnimatedPortraitSpringPart>();
                RectTransform followTarget = parent != rootRect ? parent : animatedPortrait.HeadPivot;
                spring.Init(layerRoot, followTarget, layer.spring);
                animatedPortrait.SpringParts.Add(spring);
            }
        }

        animatedPortrait.CaptureOriginalPoses();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, outputPath);
        DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
    }

    private RectTransform CreateImage(string name, RectTransform parent, string assetPath, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetupRect(rect, new Vector2(manifest.sourceWidth, manifest.sourceHeight), anchoredPosition);

        Image image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        image.raycastTarget = false;
        image.preserveAspect = false;

        return rect;
    }

    private RectTransform CreateRect(string name, RectTransform parent, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetupRect(rect, size, anchoredPosition);
        return rect;
    }

    private void SetupRect(RectTransform rect, Vector2 size, Vector2 anchoredPosition)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private List<PortraitLayerDefinition> GetOrderedLayers()
    {
        List<PortraitLayerDefinition> ordered = new List<PortraitLayerDefinition>();
        HashSet<string> added = new HashSet<string>();

        for (int i = 0; manifest.recommendedLayerOrder != null && i < manifest.recommendedLayerOrder.Count; i++)
        {
            PortraitLayerDefinition layer = manifest.GetLayer(manifest.recommendedLayerOrder[i]);
            if (layer != null && added.Add(layer.name))
                ordered.Add(layer);
        }

        for (int i = 0; manifest.layers != null && i < manifest.layers.Count; i++)
        {
            PortraitLayerDefinition layer = manifest.layers[i];
            if (layer != null && added.Add(layer.name))
                ordered.Add(layer);
        }

        return ordered;
    }

    private bool RequiresPivot(PortraitLayerDefinition layer)
    {
        return layer.type == "head" || layer.type == "spring";
    }

    private RectTransform GetParentRect(PortraitLayerDefinition layer, RectTransform root, Dictionary<string, RectTransform> pivotByLayer)
    {
        if (string.IsNullOrEmpty(layer.parent))
            return root;

        if (pivotByLayer.TryGetValue(layer.parent, out RectTransform parent))
            return parent;

        return root;
    }

    private Vector2 GetParentPivotPosition(PortraitLayerDefinition layer, Dictionary<string, Vector2> pivotPositionByLayer)
    {
        if (string.IsNullOrEmpty(layer.parent))
            return Vector2.zero;

        if (pivotPositionByLayer.TryGetValue(layer.parent, out Vector2 parentPosition))
            return parentPosition;

        return Vector2.zero;
    }

    private Vector2 GetLayerPivotPosition(PortraitLayerDefinition layer)
    {
        PortraitPivotDefinition pivot = manifest.GetPivot(layer.name);
        if (pivot == null)
            pivot = new PortraitPivotDefinition();

        return pivot.ToSourcePosition(manifest.sourceWidth, manifest.sourceHeight);
    }

    private void ValidatePivot(string name, PortraitPivotDefinition pivot, string label)
    {
        if (pivot == null)
            return;

        if (pivot.x < 0f || pivot.x > 1f || pivot.y < 0f || pivot.y > 1f)
            errors.Add(label + " for " + name + " must be between 0 and 1.");
    }

    private void ValidateSpring(PortraitLayerDefinition layer)
    {
        if (layer.spring.strength <= 0f)
            errors.Add(layer.name + " spring strength must be greater than 0.");
        if (layer.spring.damping < 0f)
            errors.Add(layer.name + " spring damping cannot be negative.");
        if (layer.spring.maxRotation < 0f)
            errors.Add(layer.name + " spring maxRotation cannot be negative.");
    }

    private string GetLayerPath(PortraitLayerDefinition layer)
    {
        return (folderPath + "/" + layer.file).Replace("\\", "/");
    }

    private void ApplySpriteImportSettings(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }
        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }
        if (importer.alphaIsTransparency == false)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }
        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private void DrawValidationStatus()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Validation Status", EditorStyles.boldLabel);

        if (errors.Count == 0 && warnings.Count == 0)
        {
            EditorGUILayout.HelpBox("No validation issues.", MessageType.Info);
            return;
        }

        for (int i = 0; i < errors.Count; i++)
            EditorGUILayout.HelpBox(errors[i], MessageType.Error);

        for (int i = 0; i < warnings.Count; i++)
            EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
    }

    private void HandleDragAndDrop()
    {
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0f, 42f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag portrait.json or character folder here");

        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
            return;

        if (dropArea.Contains(evt.mousePosition) == false)
            return;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            if (DragAndDrop.objectReferences.Length > 0)
            {
                selectedAsset = DragAndDrop.objectReferences[0];
                ResolveSelectedAsset();
            }
        }

        evt.Use();
    }

    private void PreviewIdle()
    {
        StopPreview();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
        if (prefab == null)
            return;

        previewObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (previewObject == null)
            return;

        AnimatedPortrait portrait = previewObject.GetComponent<AnimatedPortrait>();
        if (portrait != null)
            portrait.PlayIdle();

        Selection.activeObject = previewObject;
    }

    private void TogglePreviewSource()
    {
        StopPreview();
        previewSource = !previewSource;

        if (previewSource)
            PreviewSource();
        else
            PreviewGeneratedNeutral();
    }

    private void PreviewGeneratedNeutral()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
        if (prefab == null)
            return;

        previewObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (previewObject == null)
            return;

        AnimatedPortrait portrait = previewObject.GetComponent<AnimatedPortrait>();
        if (portrait != null)
        {
            portrait.StopIdle();
            portrait.ResetPose();
        }

        Selection.activeObject = previewObject;
    }

    private void PreviewSource()
    {
        if (manifest == null)
            Validate();

        string sourcePath = folderPath + "/source.png";
        if (File.Exists(sourcePath) == false)
        {
            warnings.Add("source.png not found for source preview.");
            return;
        }

        ApplySpriteImportSettings(sourcePath);
        previewObject = new GameObject(manifest.characterName + "_SourcePreview", typeof(RectTransform), typeof(Image));
        RectTransform rect = previewObject.GetComponent<RectTransform>();
        SetupRect(rect, new Vector2(manifest.sourceWidth, manifest.sourceHeight), Vector2.zero);
        Image image = previewObject.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath);
        image.raycastTarget = false;
        Selection.activeObject = previewObject;
    }

    private void StopPreview()
    {
        if (previewObject != null)
            DestroyImmediate(previewObject);

        previewObject = null;
    }
}
