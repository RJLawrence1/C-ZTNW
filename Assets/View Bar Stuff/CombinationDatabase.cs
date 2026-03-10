using UnityEngine;

// Place this on a persistent GameObject in your scene (e.g. GameManager).
// Drag all your ItemCombination ScriptableObjects into the combinations list.
// CombinationDatabase.instance.TryCombine() handles all the logic.

public class CombinationDatabase : MonoBehaviour
{
    public static CombinationDatabase instance;

    [Header("All Valid Combinations")]
    public ItemCombination[] combinations;

    void Awake()
    {
        instance = this;
    }

    // Returns the matching combination if itemA + itemB is valid, null otherwise.
    // Order doesn't matter — Yellow+Blue and Blue+Yellow both work.
    public ItemCombination TryCombine(string itemA, string itemB)
    {
        foreach (ItemCombination combo in combinations)
        {
            bool match = (combo.itemA == itemA && combo.itemB == itemB)
                      || (combo.itemA == itemB && combo.itemB == itemA);
            if (match)
                return combo;
        }
        return null;
    }
}