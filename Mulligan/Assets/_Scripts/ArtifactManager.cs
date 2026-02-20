using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

public class ArtifactManager : Singleton<ArtifactManager>
{
    public List<ArtifactData> ActiveArtifacts = new List<ArtifactData>(5);
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void AddRandomArtifact()
    {
        if (ActiveArtifacts.Count >= 6)
        {
            Debug.Log("Artifact slots are full.");
            return;
        }

        var all = CardContainer.Instance.ArtifactDataList;

        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("No artifacts available to choose from.");
            return;
        }

        // Filter out already equipped ones
        List<ArtifactData> available = new List<ArtifactData>();
        foreach (var artifact in all)
        {
            if (!ActiveArtifacts.Contains(artifact))
            {
                available.Add(artifact);
            }
        }

        if (available.Count == 0)
        {
            Debug.Log("All artifacts are already equipped.");
            return;
        }

        // Pick random one
        ArtifactData selected = available[Random.Range(0, available.Count)];

        ActiveArtifacts.Add(selected);

        // Update UI
        UIManager.Instance.UpdateArtifactSlotsUI();

        Debug.Log("Added artifact: " + selected.name);
    }
    public ArtifactData GetRandom()
    {

        var all = CardContainer.Instance.ArtifactDataList;
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("No artifacts available to choose from.");
            return null;
        }

        // Filter out already equipped ones
        List<ArtifactData> available = new List<ArtifactData>();
        foreach (var artifact in all)
        {
            if (!ActiveArtifacts.Contains(artifact))
            {
                available.Add(artifact);
            }
        }

        if (available.Count == 0)
        {
            Debug.Log("All artifacts are already equipped.");
            return null;
        }

        // Pick random one
        ArtifactData selected = available[Random.Range(0, available.Count)];

        return selected;
    }
    public void AddArtifact(ArtifactEffectType aType)
    {
        if (ActiveArtifacts.Count >= 6)
        {
            Debug.Log("Artifact slots are full.");
            return;
        }

        var all = CardContainer.Instance.ArtifactDataList;
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("No artifacts available to choose from.");
            return;
        }

        // Filter out already equipped ones
        List<ArtifactData> available = new List<ArtifactData>();
        foreach (var artifact in all)
        {
            if (!ActiveArtifacts.Contains(artifact) && aType== artifact.effect)
            {
                ActiveArtifacts.Add(artifact);

                // Update UI
                UIManager.Instance.UpdateArtifactSlotsUI();

                Debug.Log("Added artifact: " + artifact.name);
                return;
            }
        }

        if (available.Count == 0)
        {
            Debug.Log("All artifacts are already equipped.");
            return;
        }




    }
    public void SellArtifact(Artifact aArtifact)
    {
        ActiveArtifacts.Remove(aArtifact.ArtifactData);
        Destroy(aArtifact.gameObject);
        UIManager.Instance.UpdateArtifactSlotsUI(); // updates visuals
        GameData.CurrentGold += 3;



    }
    public void AddArtifact(ArtifactData artifact)
    {
        if (ActiveArtifacts.Count >= 6) return;

        ActiveArtifacts.Add(artifact);
        UIManager.Instance.UpdateArtifactSlotsUI(); // updates visuals
    }

    public bool HasArtifact(ArtifactEffectType effectType)
    {
        return ActiveArtifacts.Exists(a => a.effect == effectType);
    }

    public int GetArtifactValue(ArtifactEffectType effectType)
    {
        int total = 0;
        foreach (var artifact in ActiveArtifacts)
        {
            if (artifact.effect == effectType)
                total += artifact.value;
        }
        return total;
    }



    [System.Serializable]
    public class ArtifactJsonData
    {
        public string name;
        public string description;
        public int value;
        public ArtifactEffectType effect;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }

    private string WrapArray(string rawJson)
    {
        return "{ \"items\": " + rawJson + " }";
    }

}
