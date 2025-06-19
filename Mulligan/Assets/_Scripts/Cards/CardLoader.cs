using UnityEngine;

public static class CardLoader
{
    public static CardDataObject container=null;
    public static CardDataObject LoadAllCards(string containerName = "CardContainer")
    {
        if (container != null)
            return container;
        if (PlayerPrefs.HasKey("GameData"))
        {
            CardLoader.container = new CardDataObject(JsonUtility.FromJson<CardDataExportWrapper>(PlayerPrefs.GetString("GameData")));
        }
        else
        {
            CardLoader.container = Resources.Load<CardDataObject>(containerName);
        }
        if (container == null)
        {
            Debug.LogError($"CardContainer '{containerName}' not found in Resources folder!");
            return null;
        }

        return container;
    }
}