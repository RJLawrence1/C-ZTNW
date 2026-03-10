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

    private CurlyMovement curly;

    // Input mode — switches automatically based on which device was used last
    public static bool usingController = false;
    private Vector2 lastMousePosition;

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

    private bool IsDialoguePlaying()
    {
        bool curlyTalking = DialogueLabel.curlyLabel != null && DialogueLabel.curlyLabel.IsDisplaying();
        bool zoeyTalking = DialogueLabel.zoeyLabel != null && DialogueLabel.zoeyLabel.IsDisplaying();
        return curlyTalking || zoeyTalking;
    }

    private int selectedSlotIndex = 0;
    private int controllerDragIndex = -1;   // Slot being "held" for combining

    void Update()
    {
        if (DialogueManager.isInDialogue) return;

        var gamepad = Gamepad.current;

        // Detect input mode switches
        Vector2 mousePos = Mouse.current.position.ReadValue();
        bool mouseMoved = Vector2.Distance(mousePos, lastMousePosition) > 2f;

        if (mouseMoved)
        {
            usingController = false;
            lastMousePosition = mousePos;
            cursorImage.enabled = false;
        }
        else if (gamepad != null)
        {
            Vector2 rightStick = new Vector2(
                gamepad.rightStick.x.ReadValue(),
                gamepad.rightStick.y.ReadValue());

            bool anyControllerInput = rightStick.magnitude > 0.1f
                || gamepad.leftStick.ReadValue().magnitude > 0.1f
                || gamepad.rightTrigger.isPressed
                || gamepad.leftTrigger.isPressed
                || gamepad.aButton.isPressed
                || gamepad.bButton.isPressed
                || gamepad.dpad.left.isPressed
                || gamepad.dpad.right.isPressed;

            if (anyControllerInput)
                usingController = true;
        }

        // Hide hardware mouse cursor when using controller, show when using mouse
        Cursor.visible = !usingController;

        if (!usingController)
        {
            cursorImage.enabled = false;
            return;
        }

        cursorImage.color = PhoneBoothUI.isInPhoneBooth ? Color.black : Color.white;
        if (gamepad == null) return;

        // B button cancels item selection (world use)
        if (gamepad.bButton.wasPressedThisFrame && ItemCursor.hasSelectedItem)
        {
            ItemCursor.instance.ClearSelection();
            return;
        }

        float x = gamepad.rightStick.x.ReadValue();
        float y = gamepad.rightStick.y.ReadValue();
        Vector2 rightStickMove = new Vector2(x, y);

        if (rightStickMove.magnitude > 0.01f)
        {
            cursorImage.enabled = true;
            hideTimer = 0f;

            cursorPosition += rightStickMove * cursorSpeed * Time.deltaTime;

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

        // If inventory is open, hand off to inventory controller
        if (InventoryManager.instance.isOpen)
        {
            HandleInventoryController(gamepad);
            return;
        }

        // Hotspot detection
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
            if (IsDialoguePlaying()) return;
            SimulateClick(GetScreenPosition());
        }
    }

    // ── Inventory Controller ─────────────────────────────────────

    // Returns which slot index the controller cursor is currently hovering over, or -1
    int GetSlotUnderControllerCursor()
    {
        var inv = InventoryManager.instance;
        Vector2 screenPos = GetScreenPosition();
        for (int i = 0; i < inv.slots.Count; i++)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                inv.slots[i].rectTransform, screenPos, null))
                return i;
        }
        return -1;
    }
    bool IsCursorOverInventoryPanel()
    {
        var inv = InventoryManager.instance;
        if (inv.inventoryPanel == null) return false;
        RectTransform panelRect = inv.inventoryPanel.GetComponent<RectTransform>();
        if (panelRect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, GetScreenPosition(), null);
    }

    void HandleInventoryController(Gamepad gamepad)
    {
        var inv = InventoryManager.instance;
        int itemCount = inv.itemNames.Count;

        // D-pad navigates slots
        if (gamepad.dpad.left.wasPressedThisFrame)
        {
            selectedSlotIndex = Mathf.Max(0, selectedSlotIndex - 1);
            HighlightSlot(selectedSlotIndex);
        }
        else if (gamepad.dpad.right.wasPressedThisFrame)
        {
            selectedSlotIndex = Mathf.Min(Mathf.Max(0, itemCount - 1), selectedSlotIndex + 1);
            HighlightSlot(selectedSlotIndex);
        }

        // Right trigger
        if (gamepad.rightTrigger.wasPressedThisFrame)
        {
            int cursorSlot = GetSlotUnderControllerCursor();

            if (controllerDragIndex < 0)
            {
                // First press — grab whatever slot the cursor is over
                if (cursorSlot < 0 || cursorSlot >= itemCount) return;

                controllerDragIndex = cursorSlot;
                if (controllerDragIndex < inv.slots.Count)
                {
                    Color c = inv.slots[controllerDragIndex].color;
                    inv.slots[controllerDragIndex].color = new Color(c.r, c.g, c.b, 0.4f);
                }
            }
            else
            {
                // Second press
                if (IsCursorOverInventoryPanel())
                {
                    // Over panel — combine with whatever slot cursor is on
                    if (cursorSlot >= 0 && cursorSlot != controllerDragIndex && cursorSlot < itemCount)
                        inv.TryCombineFromController(controllerDragIndex, cursorSlot);
                }
                else
                {
                    // Off panel — select for world use
                    if (controllerDragIndex < itemCount)
                    {
                        if (VerbManager.instance != null)
                            VerbManager.instance.SetVerb(VerbManager.Verb.UseItem);
                        ItemCursor.instance.SelectItem(
                            inv.itemNames[controllerDragIndex],
                            inv.itemSprites[controllerDragIndex],
                            inv.itemColors[controllerDragIndex]
                        );
                        inv.isOpen = false;
                        inv.inventoryPanel.SetActive(false);
                        inv.inventoryHighlight.color = Color.clear;
                    }
                }

                controllerDragIndex = -1;
                HighlightSlot(selectedSlotIndex);
            }
        }

        // A button — select item for world use (only if not mid-combine)
        if (gamepad.aButton.wasPressedThisFrame && controllerDragIndex < 0)
        {
            if (selectedSlotIndex < itemCount)
            {
                if (VerbManager.instance != null)
                    VerbManager.instance.SetVerb(VerbManager.Verb.UseItem);
                ItemCursor.instance.SelectItem(
                    inv.itemNames[selectedSlotIndex],
                    inv.itemSprites[selectedSlotIndex],
                    inv.itemColors[selectedSlotIndex]
                );
                inv.isOpen = false;
                inv.inventoryPanel.SetActive(false);
                inv.inventoryHighlight.color = Color.clear;
            }
        }

        // B button — cancel combine grab OR close inventory
        if (gamepad.bButton.wasPressedThisFrame)
        {
            if (controllerDragIndex >= 0)
            {
                controllerDragIndex = -1;
                HighlightSlot(selectedSlotIndex);
            }
            else
            {
                inv.ToggleInventory();
            }
        }
    }

    void HighlightSlot(int index)
    {
        var inv = InventoryManager.instance;
        for (int i = 0; i < inv.slots.Count; i++)
        {
            Color c = inv.slots[i].color;
            inv.slots[i].color = (i == index && i < inv.itemNames.Count)
                ? new Color(c.r, c.g, c.b, 1f)
                : new Color(c.r, c.g, c.b, 0.5f);
        }
    }

    public Vector2 GetScreenPositionPublic() => GetScreenPosition();

    Vector2 GetScreenPosition()
    {
        return new Vector2(
            Screen.width / 2f + cursorPosition.x,
            Screen.height / 2f + cursorPosition.y
        );
    }

    void SimulateClick(Vector2 screenPosition)
    {
        // UI first
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
                if (button != null) { button.onClick.Invoke(); return; }
            }
            return;
        }

        // World interactables
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