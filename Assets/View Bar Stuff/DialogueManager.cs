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

    private CurlyMovement curly;

    void Awake()
    {
        instance = this;
        dialoguePanel.SetActive(false);
    }

    void Start()
    {
        curly = FindObjectOfType<CurlyMovement>();
    }

    void Update()
    {
        if (!dialoguePanel.activeSelf) return;

        // Keyboard
        if (Keyboard.current.digit1Key.wasPressedThisFrame && optionCount >= 1) SelectOption(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame && optionCount >= 2) SelectOption(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame && optionCount >= 3) SelectOption(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame && optionCount >= 4) SelectOption(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame && optionCount >= 5) SelectOption(4);

        // Controller face buttons — 5th option on D-pad down
        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.wasPressedThisFrame && optionCount >= 1) SelectOption(0);
            if (Gamepad.current.buttonEast.wasPressedThisFrame && optionCount >= 2) SelectOption(1);
            if (Gamepad.current.buttonWest.wasPressedThisFrame && optionCount >= 3) SelectOption(2);
            if (Gamepad.current.buttonNorth.wasPressedThisFrame && optionCount >= 4) SelectOption(3);
            if (Gamepad.current.dpad.down.wasPressedThisFrame && optionCount >= 5) SelectOption(4);
        }
    }

    public void ShowDialogue(string[] options, Action[] actions)
    {
        isInDialogue = true;
        optionCount = options.Length;
        optionActions = actions;

        if (curly != null)
            curly.CancelMovement();

        SetupButton(option1Button, option1Text, optionCount >= 1 ? options[0] : "", 0, optionCount >= 1);
        SetupButton(option2Button, option2Text, optionCount >= 2 ? options[1] : "", 1, optionCount >= 2);
        SetupButton(option3Button, option3Text, optionCount >= 3 ? options[2] : "", 2, optionCount >= 3);
        SetupButton(option4Button, option4Text, optionCount >= 4 ? options[3] : "", 3, optionCount >= 4);
        SetupButton(option5Button, option5Text, optionCount >= 5 ? options[4] : "", 4, optionCount >= 5);

        dialoguePanel.SetActive(true);
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