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
                "B  —  Cancel\n\n" +
                "F1 or ESC to close";
        }
    }

    void Update()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame)
            Toggle();

        if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
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
        if (controlsPanel != null) controlsPanel.SetActive(false);
        DialogueManager.isInDialogue = false;
        Time.timeScale = 1f;
    }
}