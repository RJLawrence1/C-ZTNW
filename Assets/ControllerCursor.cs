using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class ControllerCursor : MonoBehaviour
{
    public static ControllerCursor instance;
    public float cursorSpeed = 800f;
    private Image cursorImage;
    private RectTransform rectTransform;
    private Vector2 cursorPosition;
    private float hideTimer = 0f;
    private float hideTime = 2f;

    // Cached references — avoids FindObjectOfType on every click
    private CurlyMovement curly;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        cursorImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        cursorImage.enabled = false;
        CenterCursor();

        curly = FindObjectOfType<CurlyMovement>();
    }

    void CenterCursor()
    {
        cursorPosition = Vector2.zero;
        rectTransform.anchoredPosition = cursorPosition;
    }

    // Returns true if either character is currently speaking a line
    private bool IsDialoguePlaying()
    {
        bool curlyTalking = DialogueLabel.curlyLabel != null && DialogueLabel.curlyLabel.IsDisplaying();
        bool zoeyTalking = DialogueLabel.zoeyLabel != null && DialogueLabel.zoeyLabel.IsDisplaying();
        return curlyTalking || zoeyTalking;
    }

    private int selectedSlotIndex = 0;

    void Update()
    {
        Debug.Log("isInDialogue: " + DialogueManager.isInDialogue + " | gamepad: " + (Gamepad.current != null));
        if (DialogueManager.isInDialogue) return;
        cursorImage.color = PhoneBoothUI.isInPhoneBooth ? Color.black : Color.white;

        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        // B button cancels item selection
        if (gamepad.bButton.wasPressedThisFrame && ItemCursor.hasSelectedItem)
        {
            ItemCursor.instance.ClearSelection();
            return;
        }

        float x = gamepad.rightStick.x.ReadValue();
        float y = gamepad.rightStick.y.ReadValue();

        Vector2 rightStick = new Vector2(x, y);

        if (rightStick.magnitude > 0.01f)
        {
            cursorImage.enabled = true;
            hideTimer = 0f;

            cursorPosition += rightStick * cursorSpeed * Time.deltaTime;

            Vector2 halfScreen = new Vector2(Screen.width / 2f, Screen.height / 2f);
            cursorPosition.x = Mathf.Clamp(cursorPosition.x, -halfScreen.x, halfScreen.x);
            cursorPosition.y = Mathf.Clamp(cursorPosition.y, -halfScreen.y, halfScreen.y);

            rectTransform.anchoredPosition = cursorPosition;
        }
        else if (cursorImage.enabled)
        {
            hideTimer += Time.deltaTime;
            if (hideTimer >= hideTime)
            {
                cursorImage.enabled = false;
                CenterCursor();
            }
        }

        // If inventory is open, handle slot interaction and skip world clicks
        if (InventoryManager.instance.isOpen)
        {
            HandleInventoryController(gamepad);
            return;
        }

        // Hotspot detection — skip when in phone booth or dialogue is playing
        if (cursorImage.enabled && !PhoneBoothUI.isInPhoneBooth && !IsDialoguePlaying())
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(GetScreenPosition());
            int interactableLayer = LayerMask.GetMask("Interactable");
            RaycastHit2D hoverHit = Physics2D.Raycast(worldPos, Vector2.zero, Mathf.Infinity, interactableLayer);

            if (hoverHit.collider != null)
            {
                Interactable interactable = hoverHit.collider.GetComponent<Interactable>();
                ZoeyInteractable zoeyInteractable = hoverHit.collider.GetComponent<ZoeyInteractable>();
                PhoneBooth phoneBooth = hoverHit.collider.GetComponent<PhoneBooth>();

                if (interactable != null)
                    HotspotLabel.instance.Show(interactable.itemName, hoverHit.collider.transform.position);
                else if (zoeyInteractable != null)
                    HotspotLabel.instance.Show("Zoey", hoverHit.collider.transform.position);
                else if (phoneBooth != null)
                    HotspotLabel.instance.Show("Phone Booth", hoverHit.collider.transform.position);
            }
        }

        if (gamepad.rightTrigger.wasPressedThisFrame && cursorImage.enabled)
        {
            // Don't allow clicks while a spoken line is playing
            if (IsDialoguePlaying()) return;

            SimulateClick(GetScreenPosition());
        }
    }

    void HandleInventoryController(Gamepad gamepad)
    {
        var inv = InventoryManager.instance;
        int itemCount = inv.itemNames.Count;
        if (itemCount == 0) return;

        // D-pad left/right navigates slots
        if (gamepad.dpad.left.wasPressedThisFrame)
        {
            selectedSlotIndex = Mathf.Max(0, selectedSlotIndex - 1);
            HighlightSlot(selectedSlotIndex);
        }
        else if (gamepad.dpad.right.wasPressedThisFrame)
        {
            selectedSlotIndex = Mathf.Min(itemCount - 1, selectedSlotIndex + 1);
            HighlightSlot(selectedSlotIndex);
        }

        // Right trigger or A button selects the highlighted slot
        if (gamepad.rightTrigger.wasPressedThisFrame || gamepad.aButton.wasPressedThisFrame)
        {
            if (selectedSlotIndex < itemCount)
            {
                ItemCursor.instance.SelectItem(
                    inv.itemNames[selectedSlotIndex],
                    inv.itemSprites[selectedSlotIndex],
                    inv.itemColors[selectedSlotIndex]
                );
                // Close inventory
                inv.isOpen = false;
                inv.inventoryPanel.SetActive(false);
                inv.inventoryHighlight.color = Color.clear;
            }
        }

        // B button closes inventory
        if (gamepad.bButton.wasPressedThisFrame)
            inv.ToggleInventory();
    }

    void HighlightSlot(int index)
    {
        var inv = InventoryManager.instance;
        for (int i = 0; i < inv.slots.Count; i++)
        {
            Color c = inv.slots[i].color;
            // Brighten selected slot, dim others
            inv.slots[i].color = (i == index && i < inv.itemNames.Count)
                ? new Color(c.r, c.g, c.b, 1f)
                : new Color(c.r, c.g, c.b, 0.5f);
        }
    }

    Vector2 GetScreenPosition()
    {
        return new Vector2(
            Screen.width / 2f + cursorPosition.x,
            Screen.height / 2f + cursorPosition.y
        );
    }

    void SimulateClick(Vector2 screenPosition)
    {
        // Check UI first
        if ((DialogueManager.instance != null && DialogueManager.instance.dialoguePanel.activeSelf)
            || PhoneBoothUI.isInPhoneBooth)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = screenPosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (RaycastResult result in results)
            {
                Button button = result.gameObject.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.Invoke();
                    return;
                }
            }
            return;
        }

        // Check world interactables
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPosition);
        int interactableLayer = LayerMask.GetMask("Interactable");
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, Mathf.Infinity, interactableLayer);

        if (hit.collider != null)
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            ZoeyInteractable zoeyInteractable = hit.collider.GetComponent<ZoeyInteractable>();
            PhoneBooth phoneBooth = hit.collider.GetComponent<PhoneBooth>();

            if (interactable != null)
            {
                // If item is selected, use it on the interactable
                if (ItemCursor.hasSelectedItem)
                {
                    interactable.OnItemUsed(ItemCursor.selectedItemName);
                    ItemCursor.instance.ClearSelection();
                }
                else
                    curly.WalkToInteract(interactable);
            }
            else if (zoeyInteractable != null)
            {
                if (VerbManager.instance.currentVerb == VerbManager.Verb.TalkTo ||
                    VerbManager.instance.currentVerb == VerbManager.Verb.UseZoey)
                    curly.WalkToInteract(zoeyInteractable);
                else
                    zoeyInteractable.OnInteract();
            }
            else if (phoneBooth != null)
                curly.WalkToInteract(phoneBooth);
        }
        else
        {
            Vector3 worldPos3 = Camera.main.ScreenToWorldPoint(screenPosition);
            worldPos3.z = 0f;
            if (curly.walkableArea.OverlapPoint(worldPos3))
                curly.MoveToPosition(worldPos3);
        }
    }
}