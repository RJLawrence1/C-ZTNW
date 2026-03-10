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

        // Follow controller cursor or mouse depending on active input mode
        if (ControllerCursor.usingController && ControllerCursor.instance != null)
            cursorImage.transform.position = ControllerCursor.instance.GetScreenPositionPublic();
        else
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
        // Only hide hardware cursor if using mouse — controller already hides it
        if (!ControllerCursor.usingController)
            Cursor.visible = false;
    }

    public void ClearSelection()
    {
        selectedItemName = "";
        cursorImage.enabled = false;
        if (!ControllerCursor.usingController)
            Cursor.visible = true;
    }
}