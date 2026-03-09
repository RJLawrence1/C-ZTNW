using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    public GameObject inventoryPanel;
    public Image inventoryHighlight;
    public List<Image> slots;

    public List<Sprite> itemSprites = new List<Sprite>();
    public List<string> itemNames = new List<string>();
    public List<Color> itemColors = new List<Color>();

    public bool isOpen = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ClearSlots();
        inventoryHighlight.color = Color.clear;

        // Add click listeners to each slot
        for (int i = 0; i < slots.Count; i++)
        {
            int index = i; // capture for lambda
            EventTrigger trigger = slots[i].gameObject.GetComponent<EventTrigger>()
                                ?? slots[i].gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((_) => OnSlotClicked(index));
            trigger.triggers.Add(entry);
        }
    }

    void OnSlotClicked(int index)
    {
        // Ignore empty slots
        if (index >= itemNames.Count || itemNames[index] == null || itemNames[index] == "") return;

        // If this item is already selected, deselect it
        if (ItemCursor.selectedItemName == itemNames[index])
        {
            ItemCursor.instance.ClearSelection();
            return;
        }

        // Select the item and close inventory
        ItemCursor.instance.SelectItem(itemNames[index], itemSprites[index], itemColors[index]);
        isOpen = false;
        inventoryPanel.SetActive(false);
        inventoryHighlight.color = Color.clear;
    }

    void Update()
    {
        if (PhoneBoothUI.isInPhoneBooth) return;
        if (Keyboard.current.eKey.wasPressedThisFrame)
            ToggleInventory();

        if (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame)
            ToggleInventory();
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);
        inventoryHighlight.color = isOpen ? new Color(0f, 0.5f, 1f, 0.5f) : Color.clear;

        // Cancel item selection when closing inventory
        if (!isOpen && ItemCursor.hasSelectedItem)
            ItemCursor.instance.ClearSelection();
    }

    public void AddItem(string itemName, Sprite itemSprite, Color itemColor)
    {
        itemNames.Add(itemName);
        itemSprites.Add(itemSprite);
        itemColors.Add(itemColor);
        RefreshSlots();
    }

    public void RemoveItem(string itemName)
    {
        int index = itemNames.IndexOf(itemName);
        if (index >= 0)
        {
            itemNames.RemoveAt(index);
            itemSprites.RemoveAt(index);
            itemColors.RemoveAt(index);
            RefreshSlots();
        }
    }

    public void ClearAllItems()
    {
        itemNames.Clear();
        itemSprites.Clear();
        itemColors.Clear();
        ClearSlots();
    }

    void RefreshSlots()
    {
        ClearSlots();
        for (int i = 0; i < itemSprites.Count && i < slots.Count; i++)
        {
            slots[i].sprite = itemSprites[i];
            slots[i].color = itemColors[i];
        }
    }

    void ClearSlots()
    {
        foreach (Image slot in slots)
        {
            slot.sprite = null;
            slot.color = new Color(0f, 0.5f, 1f, 0.5f);
        }
    }
}