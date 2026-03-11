using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemSpriteDatabase", menuName = "GupWorks/Item Sprite Database")]
public class ItemSpriteDatabase : ScriptableObject
{
    [System.Serializable]
    public class ItemSpriteEntry
    {
        public string itemName;
        public Sprite sprite;
    }

    public List<ItemSpriteEntry> entries = new List<ItemSpriteEntry>();

    // Look up a sprite by item name — returns null if not found
    public Sprite GetSprite(string itemName)
    {
        foreach (ItemSpriteEntry entry in entries)
        {
            if (entry.itemName == itemName)
                return entry.sprite;
        }
        return null;
    }
}