using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemCursor : MonoBehaviour
{
    public static ItemCursor instance;

    [Header("Cursor Image")]
    public Image cursorImage;  // UI Image that follows the mouse

    public static string selectedItemName = "";
    public static bool hasSelectedItem => selectedItemName != "";

    void Awake()
    {
        instance = this;
        cursorImage.enabled = false;
    }

    void Update()
    {
        if (!hasSelectedItem) return;

        // Follow mouse position
        cursorImage.transform.position = Mouse.current.position.ReadValue();

        // Right click or B button cancels
        if (Mouse.current.rightButton.wasPressedThisFrame ||
            (Gamepad.current != null && Gamepad.current.bButton.wasPressedThisFrame))
        {
            ClearSelection();
        }
    }

    public void SelectItem(string itemName, Sprite sprite, Color color)
    {
        selectedItemName = itemName;
        cursorImage.sprite = sprite;
        cursorImage.color = color;
        cursorImage.enabled = true;
        Cursor.visible = false;
    }

    public void ClearSelection()
    {
        selectedItemName = "";
        cursorImage.enabled = false;
        Cursor.visible = true;
    }
}