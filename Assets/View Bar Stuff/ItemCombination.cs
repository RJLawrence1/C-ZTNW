using UnityEngine;

// Attach this to any GameObject that represents a combinable item.
// Set the item's own name in itemName, the item it combines WITH in combinesWith,
// and the result item's name/sprite/color in the result fields.
//
// Only ONE of the two items needs this script — if Yellow Square has it pointing
// at Blue Square, that's enough. You don't need it on Blue Square too.

[CreateAssetMenu(fileName = "NewCombination", menuName = "Inventory/Item Combination")]
public class ItemCombination : ScriptableObject
{
    [Header("Combination")]
    public string itemA;            // First item name
    public string itemB;            // Second item name (the one it combines with)

    [Header("Result")]
    public string resultName;       // Name of the combined item
    public Sprite resultSprite;     // Sprite of the combined item
    public Color resultColor = Color.white; // Color of the combined item
}