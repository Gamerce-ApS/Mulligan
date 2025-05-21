using UnityEngine;

public static class CardLoader
{
    public static CardDataObject LoadAllCards(string containerName = "CardContainer")
    {
        CardDataObject container = Resources.Load<CardDataObject>(containerName);
        if (container == null)
        {
            Debug.LogError($"CardContainer '{containerName}' not found in Resources folder!");
            return null;
        }

        return container;
    }
}