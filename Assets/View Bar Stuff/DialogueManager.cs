using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;
    public static bool isInDialogue = false;

    public GameObject dialoguePanel;
    public Button option1Button;
    public Button option2Button;
    public Button option3Button;
    public Button option4Button;
    public Button option5Button;

    public TextMeshProUGUI option1Text;
    public TextMeshProUGUI option2Text;
    public TextMeshProUGUI option3Text;
    public TextMeshProUGUI option4Text;
    public TextMeshProUGUI option5Text;

    private Action[] optionActions = new Action[5];
    private int optionCount = 0;

    // Controller navigation
    private int highlightedIndex = 0;
    private float navCooldown = 0f;
    private const float NavCooldownTime = 0.2f;

    private Button[] buttons;
    private CurlyMovement curly;

    void Awake()
    {
        instance = this;
        dialoguePanel.SetActive(false);
    }

    void Start()
    {
        curly = FindObjectOfType<CurlyMovement>();
        buttons = new Button[] { option1Button, option2Button, option3Button, option4Button, option5Button };
    }

    void Update()
    {
        if (!dialoguePanel.activeSelf) return;

        if (navCooldown > 0f) navCooldown -= Time.unscaledDeltaTime;

        // Keyboard number shortcuts
        if (Keyboard.current.digit1Key.wasPressedThisFrame && optionCount >= 1) SelectOption(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame && optionCount >= 2) SelectOption(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame && optionCount >= 3) SelectOption(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame && optionCount >= 4) SelectOption(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame && optionCount >= 5) SelectOption(4);

        // Controller — D-pad up/down to navigate, A to confirm
        if (Gamepad.current != null)
        {
            if (navCooldown <= 0f)
            {
                bool up = Gamepad.current.dpad.up.isPressed || Gamepad.current.leftStick.y.ReadValue() > 0.5f;
                bool down = Gamepad.current.dpad.down.isPressed || Gamepad.current.leftStick.y.ReadValue() < -0.5f;

                if (up)
                {
                    highlightedIndex = (highlightedIndex - 1 + optionCount) % optionCount;
                    navCooldown = NavCooldownTime;
                    RefreshHighlight();
                }
                else if (down)
                {
                    highlightedIndex = (highlightedIndex + 1) % optionCount;
                    navCooldown = NavCooldownTime;
                    RefreshHighlight();
                }
            }

            if (Gamepad.current.buttonSouth.wasPressedThisFrame && optionCount > 0)
                SelectOption(highlightedIndex);
        }
    }

    void RefreshHighlight()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            TextMeshProUGUI tmp = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (tmp == null) continue;
            // Highlight selected option — bright white, others normal green
            tmp.color = (i == highlightedIndex && i < optionCount)
                ? Color.white
                : new Color(0f, 0.67f, 0f, 1f);
        }
    }

    public void ShowDialogue(string[] options, Action[] actions)
    {
        isInDialogue = true;
        optionCount = options.Length;
        optionActions = actions;
        highlightedIndex = 0;

        if (curly != null)
            curly.CancelMovement();

        SetupButton(option1Button, option1Text, optionCount >= 1 ? options[0] : "", 0, optionCount >= 1);
        SetupButton(option2Button, option2Text, optionCount >= 2 ? options[1] : "", 1, optionCount >= 2);
        SetupButton(option3Button, option3Text, optionCount >= 3 ? options[2] : "", 2, optionCount >= 3);
        SetupButton(option4Button, option4Text, optionCount >= 4 ? options[3] : "", 3, optionCount >= 4);
        SetupButton(option5Button, option5Text, optionCount >= 5 ? options[4] : "", 4, optionCount >= 5);

        dialoguePanel.SetActive(true);
        RefreshHighlight();
    }

    void SetupButton(Button button, TextMeshProUGUI text, string label, int index, bool active)
    {
        if (button == null) return;
        button.gameObject.SetActive(active);
        if (!active) return;

        text.text = (index + 1) + ". " + label;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectOption(index));
    }

    public void HideDialogue()
    {
        isInDialogue = false;
        dialoguePanel.SetActive(false);
    }

    void SelectOption(int index)
    {
        HideDialogue();
        optionActions[index]?.Invoke();
    }
}