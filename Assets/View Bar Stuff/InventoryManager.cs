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

    // Drag state
    private int draggingIndex = -1;
    private Image dragGhost = null;
    private RectTransform dragGhostRect;
    private Canvas rootCanvas;

    // Click-to-combine state
    private int selectedForCombine = -1;

    [Header("Combine Clips")]
    public AudioClip combineSuccessClip;
    public AudioClip combineFailClip;

    void Awake()
    {
        instance = this;
        rootCanvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        ClearSlots();
        inventoryHighlight.color = Color.clear;
        SetupSlotEvents();

        // Restore inventory from InventoryData (persists across scenes)
        List<string> restoredNames = new List<string>(InventoryData.names);
        List<Color> restoredColors = new List<Color>(InventoryData.colors);

        for (int i = 0; i < restoredNames.Count; i++)
        {
            string name = restoredNames[i];
            Sprite sprite = InventoryData.GetSprite(name);
            AddItem(name, sprite, restoredColors[i]);
        }
    }

    void SetupSlotEvents()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            int index = i;
            EventTrigger trigger = slots[i].gameObject.GetComponent<EventTrigger>()
                                ?? slots[i].gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            var beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDrag.callback.AddListener((_) => OnBeginDrag(index));
            trigger.triggers.Add(beginDrag);

            var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            drag.callback.AddListener((data) => OnDrag((PointerEventData)data));
            trigger.triggers.Add(drag);

            var endDrag = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endDrag.callback.AddListener((data) => OnEndDrag((PointerEventData)data));
            trigger.triggers.Add(endDrag);

            var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            click.callback.AddListener((_) => OnSlotClicked(index));
            trigger.triggers.Add(click);
        }
    }

    // ── Drag & Drop ─────────────────────────────────────────────

    void OnBeginDrag(int index)
    {
        if (index >= itemNames.Count || itemNames[index] == null || itemNames[index] == "") return;

        draggingIndex = index;

        GameObject ghost = new GameObject("DragGhost");
        ghost.transform.SetParent(rootCanvas.transform, false);
        ghost.transform.SetAsLastSibling();

        dragGhost = ghost.AddComponent<Image>();
        dragGhost.sprite = itemSprites[index];
        dragGhost.color = itemColors[index];
        dragGhost.raycastTarget = false;

        dragGhostRect = ghost.GetComponent<RectTransform>();
        dragGhostRect.sizeDelta = new Vector2(50f, 50f);
        dragGhostRect.anchorMin = dragGhostRect.anchorMax = new Vector2(0.5f, 0.5f);

        UpdateGhostPosition(Mouse.current.position.ReadValue());
    }

    void OnDrag(PointerEventData data)
    {
        if (dragGhost != null)
            UpdateGhostPosition(data.position);
    }

    void OnEndDrag(PointerEventData data)
    {
        if (draggingIndex < 0) { CleanupDrag(); return; }

        int targetIndex = GetSlotUnderMouse(data.position);

        if (targetIndex >= 0)
        {
            // Dropped on another slot — try combine
            if (targetIndex != draggingIndex
                && targetIndex < itemNames.Count
                && itemNames[targetIndex] != null && itemNames[targetIndex] != "")
            {
                TryCombineSlots(draggingIndex, targetIndex);
            }
        }
        else
        {
            // Dropped outside panel — switch to UseItem, select for world use, close inventory
            if (VerbManager.instance != null)
                VerbManager.instance.SetVerb(VerbManager.Verb.UseItem);

            ItemCursor.instance.SelectItem(
                itemNames[draggingIndex],
                itemSprites[draggingIndex],
                itemColors[draggingIndex]
            );
            selectedForCombine = -1;
            RefreshSlotHighlights();
            isOpen = false;
            inventoryPanel.SetActive(false);
            inventoryHighlight.color = Color.clear;
        }

        CleanupDrag();
    }

    int GetSlotUnderMouse(Vector2 screenPos)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                slots[i].rectTransform, screenPos, Camera.main))
                return i;
        }
        return -1;
    }

    void UpdateGhostPosition(Vector2 screenPos)
    {
        if (dragGhostRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            screenPos, rootCanvas.worldCamera,
            out Vector2 localPoint);
        dragGhostRect.localPosition = localPoint;
    }

    void CleanupDrag()
    {
        draggingIndex = -1;
        if (dragGhost != null)
        {
            Destroy(dragGhost.gameObject);
            dragGhost = null;
        }
    }

    // ── Click to Combine ─────────────────────────────────────────

    void OnSlotClicked(int index)
    {
        if (draggingIndex >= 0) return; // Don't fire at end of a drag
        if (index >= itemNames.Count || itemNames[index] == null || itemNames[index] == "") return;

        if (selectedForCombine < 0)
        {
            // Nothing selected — select this slot
            selectedForCombine = index;
            RefreshSlotHighlights();
        }
        else if (selectedForCombine == index)
        {
            // Clicked same slot — deselect
            selectedForCombine = -1;
            RefreshSlotHighlights();
        }
        else
        {
            // Second slot — try combine
            TryCombineSlots(selectedForCombine, index);
            selectedForCombine = -1;
            RefreshSlotHighlights();
        }
    }

    void RefreshSlotHighlights()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i >= itemNames.Count || itemNames[i] == null || itemNames[i] == "")
            {
                slots[i].color = new Color(0f, 0.5f, 1f, 0.5f);
                continue;
            }
            slots[i].color = (i == selectedForCombine)
                ? new Color(itemColors[i].r, itemColors[i].g, itemColors[i].b, 1f)
                : new Color(itemColors[i].r, itemColors[i].g, itemColors[i].b, 0.7f);
        }
    }

    // ── Combining ───────────────────────────────────────────────

    void TryCombineSlots(int indexA, int indexB)
    {
        if (CombinationDatabase.instance == null) return;

        string nameA = itemNames[indexA];
        string nameB = itemNames[indexB];

        ItemCombination combo = CombinationDatabase.instance.TryCombine(nameA, nameB);
        if (combo != null)
        {
            int first = Mathf.Min(indexA, indexB);
            int second = Mathf.Max(indexA, indexB);

            itemNames.RemoveAt(second); itemSprites.RemoveAt(second); itemColors.RemoveAt(second);
            itemNames.RemoveAt(first); itemSprites.RemoveAt(first); itemColors.RemoveAt(first);

            AddItem(combo.resultName, combo.resultSprite, combo.resultColor);
            DialogueLabel.curlyLabel.Say("Yeah. That works.", combineSuccessClip);
        }
        else
        {
            DialogueLabel.curlyLabel.Say("Those don't go together.", combineFailClip);
        }
    }

    // Called by ControllerCursor for controller combining
    public void TryCombineFromController(int indexA, int indexB)
    {
        TryCombineSlots(indexA, indexB);
    }

    // ── Toggle / Update ─────────────────────────────────────────

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

        if (!isOpen)
        {
            selectedForCombine = -1;
            if (ItemCursor.hasSelectedItem)
                ItemCursor.instance.ClearSelection();
        }
    }

    // ── Item Management ─────────────────────────────────────────

    public void AddItem(string itemName, Sprite itemSprite, Color itemColor)
    {
        itemNames.Add(itemName);
        itemSprites.Add(itemSprite);
        itemColors.Add(itemColor);
        InventoryData.Sync(itemNames, itemSprites, itemColors);
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
            InventoryData.Sync(itemNames, itemSprites, itemColors);
            RefreshSlots();
        }
    }

    public void ClearAllItems()
    {
        itemNames.Clear();
        itemSprites.Clear();
        itemColors.Clear();
        InventoryData.Clear();
        ClearSlots();
    }

    void RefreshSlots()
    {
        ClearSlots();
        for (int i = 0; i < itemNames.Count && i < slots.Count; i++)
        {
            if (itemSprites[i] != null)
            {
                slots[i].sprite = itemSprites[i];
                slots[i].color = itemColors[i];
            }
            else
            {
                // No sprite — just show the color so the slot isn't invisible
                slots[i].sprite = null;
                slots[i].color = itemColors[i];
            }
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