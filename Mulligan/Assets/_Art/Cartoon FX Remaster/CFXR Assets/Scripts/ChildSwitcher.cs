using UnityEngine;

public class ChildSwitcher : MonoBehaviour
{
    private int currentIndex = 0;
    private int childCount;

    void Start()
    {
        childCount = transform.childCount;
        
        if (childCount > 0)
        {
            UpdateChildren();
        }
        else
        {
            Debug.LogWarning("No children found on " + gameObject.name);
        }
    }

    void Update()
    {
        if (childCount == 0) return;

        // Detect Right Arrow
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentIndex++;
            if (currentIndex >= childCount) currentIndex = 0; // Loop to start
            UpdateChildren();
        }

        // Detect Left Arrow
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = childCount - 1; // Loop to end
            UpdateChildren();
        }
    }

    void UpdateChildren()
    {
        for (int i = 0; i < childCount; i++)
        {
            // Only the child matching our current index stays active
            transform.GetChild(i).gameObject.SetActive(i == currentIndex);
        }
        
        // Bonus: Add a small sound or popup when switching
        // PopupTextManager.Instance.ShowCombo(transform, "NEXT UPGRADE");
    }
}