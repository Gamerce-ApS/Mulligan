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
        if (ActiveArtifacts.Count >= GameManager.Instance.TheHero.myHeroData.ArtifactSlots)
        {
            Debug.Log("Artifact slots are full.");
            return;
        }

        var all = CardContainer.Instance.GetUnlockedArtifacts();

        if (all == null || all.Count == 0)
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
        // ArtifactData selected = available[Random.Range(0, available.Count)];
        ArtifactData selected = PickArtifactByRarity(available);
        if (selected == null)
            return;

        DailyQuestManager.Instance.RollUnlockedRaceForArtifact(selected);
        ActiveArtifacts.Add(selected);
        SoundManager.TryPlay(SoundType.ArtifactObtained);

        // Update UI
        UIManager.Instance.UpdateArtifactSlotsUI();

        Debug.Log("Added artifact: " + selected.name);
    }
    public ArtifactData GetRandom()
    {

        var all = CardContainer.Instance.GetUnlockedArtifacts();
        if (all == null || all.Count == 0)
        {
            if (TutorialController.Instance.HasRunTutorial()== false && TutorialController.Instance.LastStepPlayed == "Step1_Gold")
                return CardContainer.Instance.ArtifactDataList.ToList().Find(c=> c.name == "+20 Dmg");

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
        // ArtifactData selected = available[Random.Range(0, available.Count)];
        ArtifactData selected = PickArtifactByRarity(available);
        DailyQuestManager.Instance.RollUnlockedRaceForArtifact(selected);

        if (TutorialController.Instance.HasRunTutorial()== false && TutorialController.Instance.LastStepPlayed == "Step1_Gold")
        {
             selected = available.Find(c=> c.name == "+20 Dmg");
             if (selected == null)
                selected = CardContainer.Instance.ArtifactDataList.ToList().Find(c=> c.name == "+20 Dmg");
        }

        return selected;
    }
    public void AddArtifact(ArtifactEffectType aType)
    {
        if (ActiveArtifacts.Count >= GameManager.Instance.TheHero.myHeroData.ArtifactSlots)
        {
            Debug.Log("Artifact slots are full.");
            return;
        }

        var all = CardContainer.Instance.GetUnlockedArtifacts();
        if (all == null || all.Count == 0)
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
                DailyQuestManager.Instance.RollUnlockedRaceForArtifact(artifact);
                ActiveArtifacts.Add(artifact);
                SoundManager.TryPlay(SoundType.ArtifactObtained);

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
        SoundManager.TryPlay(SoundType.ArtifactSold);
        UIManager.Instance.UpdateArtifactSlotsUI(); // updates visuals
        GameManager.Instance.AddGold(3);
        GameManager.Instance.TheHero.RefreshBar();


    }
    public void AddArtifact(ArtifactData artifact)
    {
        if (artifact == null)
            return;

        if (ActiveArtifacts.Count >= GameManager.Instance.TheHero.myHeroData.ArtifactSlots) return;

        DailyQuestManager.Instance.RollUnlockedRaceForArtifact(artifact);
        ActiveArtifacts.Add(artifact);
        SoundManager.TryPlay(SoundType.ArtifactObtained);
        UIManager.Instance.UpdateArtifactSlotsUI(); // updates visuals

        if(TutorialController.Instance.LastStepPlayed=="Step2_Shop3")
        {
            TutorialController.Instance.ShowNextStep();
        }
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
    private ArtifactData PickArtifactByRarity(List<ArtifactData> available)
{
    if (available == null || available.Count == 0)
        return null;

    RarityType rolled = CardContainer.Instance.GetRandomRarity();

    // Exact rarity first
    var pool = available
        .Where(a => (RarityType)a.rarity == rolled)
        .ToList();

    if (pool.Count > 0)
        return pool[Random.Range(0, pool.Count)];

    // Fallback downward
    for (int r = (int)rolled - 1; r >= 0; r--)
    {
        pool = available
            .Where(a => a.rarity == r)
            .ToList();

        if (pool.Count > 0)
            return pool[Random.Range(0, pool.Count)];
    }

    // Fallback upward
    for (int r = (int)rolled + 1; r <= 3; r++)
    {
        pool = available
            .Where(a => a.rarity == r)
            .ToList();

        if (pool.Count > 0)
            return pool[Random.Range(0, pool.Count)];
    }

    // Final fallback
    return available[Random.Range(0, available.Count)];
}

}
