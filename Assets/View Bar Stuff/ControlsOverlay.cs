using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class ControlsOverlay : MonoBehaviour
{
    public static ControlsOverlay instance;

    [Header("Panel")]
    public GameObject controlsPanel;
    public TextMeshProUGUI controlsText;
    public Button helpButton;

    private bool isOpen = false;

    // Controller hold B to open
    private float bHoldTimer = 0f;
    private const float BHoldThreshold = 0.5f;
    private bool bTriggered = false;

    void Awake()
    {
        instance = this;
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    void Start()
    {
        if (helpButton != null) helpButton.onClick.AddListener(Toggle);

        if (controlsText != null)
        {
            controlsText.text =
                "CONTROLS\n\n" +
                "KEYBOARD\n" +
                "1  —  Look At\n" +
                "2  —  Pick Up\n" +
                "3  —  Use Item\n" +
                "4  —  Talk To\n" +
                "5  —  Interact\n" +
                "6  —  Use Zoey\n" +
                "Tab  —  Cycle Verbs\n" +
                "E  —  Inventory\n" +
                "Shift  —  Sprint\n" +
                "F1  —  Controls\n" +
                "F3  —  FPS Counter\n" +
                "F5  —  Settings\n\n" +
                "CONTROLLER\n" +
                "LB / RB  —  Cycle Verbs\n" +
                "Right Stick  —  Cursor\n" +
                "Right Trigger  —  Click\n" +
                "Left Trigger  —  Sprint\n" +
                "Select  —  Inventory\n" +
                "Start  —  Settings\n" +
                "Hold B  —  Controls\n" +
                "B  —  Cancel\n\n" +
                "F1 or ESC to close";
        }
    }

    void Update()
    {
        // Keyboard toggle
        if (Keyboard.current.f1Key.wasPressedThisFrame)
            Toggle();

        if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();

        // Controller — hold B for 0.5s to open, tap B to close if already open
        if (Gamepad.current != null)
        {
            if (isOpen)
            {
                if (Gamepad.current.bButton.wasPressedThisFrame)
                    Close();
            }
            else if (!InventoryManager.instance.isOpen && !SettingsMenu.isOpen)
            {
                if (Gamepad.current.bButton.isPressed)
                {
                    bHoldTimer += Time.unscaledDeltaTime;
                    if (bHoldTimer >= BHoldThreshold && !bTriggered)
                    {
                        bTriggered = true;
                        Open();
                    }
                }
                else
                {
                    bHoldTimer = 0f;
                    bTriggered = false;
                }
            }
        }
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        isOpen = true;
        if (controlsPanel != null) controlsPanel.SetActive(true);
        DialogueManager.isInDialogue = true;
        Time.timeScale = 0f;
    }

    public void Close()
    {
        isOpen = false;
        bHoldTimer = 0f;
        bTriggered = false;
        if (controlsPanel != null) controlsPanel.SetActive(false);
        DialogueManager.isInDialogue = false;
        Time.timeScale = 1f;
    }
}