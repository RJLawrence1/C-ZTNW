using UnityEngine;
using System.Collections.Generic;

public static class InventoryData
{
    public static List<string> names = new List<string>();
    public static List<Sprite> sprites = new List<Sprite>();
    public static List<Color> colors = new List<Color>();

    // Persistent sprite store — never cleared, survives all scene loads
    public static Dictionary<string, Sprite> spriteStore = new Dictionary<string, Sprite>();

    public static void Sync(List<string> n, List<Sprite> s, List<Color> c)
    {
        names = new List<string>(n);
        sprites = new List<Sprite>(s);
        colors = new List<Color>(c);

        // Store any non-null sprites permanently by name
        for (int i = 0; i < n.Count; i++)
            if (s[i] != null)
                spriteStore[n[i]] = s[i];
    }

    public static Sprite GetSprite(string itemName)
    {
        Debug.Log("GetSprite for: " + itemName + " | spriteStore has: " + spriteStore.Count + " | registry has: " + Interactable.spriteRegistry.Count);
        if (spriteStore.ContainsKey(itemName))
            return spriteStore[itemName];
        if (Interactable.spriteRegistry.ContainsKey(itemName))
            return Interactable.spriteRegistry[itemName];
        return null;
    }

    public static void Clear()
    {
        names.Clear();
        sprites.Clear();
        colors.Clear();
        // Note: spriteStore is intentionally NOT cleared
    }
}