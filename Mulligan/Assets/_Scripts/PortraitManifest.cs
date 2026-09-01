using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class PortraitManifest
{
    public int version = 1;
    public string characterName;
    public int sourceWidth;
    public int sourceHeight;
    public List<PortraitLayerDefinition> layers = new List<PortraitLayerDefinition>();
    public List<string> recommendedLayerOrder = new List<string>();
    public Dictionary<string, PortraitPivotDefinition> pivots = new Dictionary<string, PortraitPivotDefinition>();
    public PortraitAnimationDefinition animation = new PortraitAnimationDefinition();
    public List<string> notes = new List<string>();

    public static PortraitManifest FromJson(string json)
    {
        return JsonConvert.DeserializeObject<PortraitManifest>(json);
    }

    public PortraitLayerDefinition GetLayer(string layerName)
    {
        if (layers == null)
            return null;

        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i] != null && layers[i].name == layerName)
                return layers[i];
        }

        return null;
    }

    public PortraitPivotDefinition GetPivot(string layerName)
    {
        if (string.IsNullOrEmpty(layerName))
            return null;

        if (pivots != null && pivots.TryGetValue(layerName, out PortraitPivotDefinition pivot))
            return pivot;

        PortraitLayerDefinition layer = GetLayer(layerName);
        return layer != null ? layer.pivot : null;
    }
}

[System.Serializable]
public class PortraitLayerDefinition
{
    public string name;
    public string file;
    public string type;
    public bool required;
    public string parent;
    public PortraitPivotDefinition pivot;
    public PortraitSpringDefinition spring;
}

[System.Serializable]
public class PortraitPivotDefinition
{
    public float x = 0.5f;
    public float y = 0.5f;
    public string description;

    public Vector2 ToSourcePosition(float sourceWidth, float sourceHeight)
    {
        return new Vector2((x - 0.5f) * sourceWidth, (y - 0.5f) * sourceHeight);
    }
}

[System.Serializable]
public class PortraitSpringDefinition
{
    public float strength = 12f;
    public float damping = 6f;
    public float maxRotation = 2f;
}

[System.Serializable]
public class PortraitAnimationDefinition
{
    public PortraitBreathingDefinition breathing = new PortraitBreathingDefinition();
    public PortraitHeadAnimationDefinition head = new PortraitHeadAnimationDefinition();
    public PortraitBlinkDefinition blink = new PortraitBlinkDefinition();
}

[System.Serializable]
public class PortraitBreathingDefinition
{
    public bool enabled = true;
    public float amount = 0.004f;
    public float speed = 1f;
}

[System.Serializable]
public class PortraitHeadAnimationDefinition
{
    public bool enabled = true;
    public float positionAmount = 2f;
    public float rotationAmount = 0.6f;
    public float speed = 0.15f;
}

[System.Serializable]
public class PortraitBlinkDefinition
{
    public bool enabled = true;
    public float minInterval = 3f;
    public float maxInterval = 7f;
    public float closedDuration = 0.1f;
    public float doubleBlinkChance = 0.18f;
}
