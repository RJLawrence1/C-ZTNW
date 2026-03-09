using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VerbManager : MonoBehaviour
{
    public static VerbManager instance;

    public enum Verb { None, LookAt, PickUp, UseItem, TalkTo, Interact, UseZoey }
    public Verb currentVerb = Verb.None;

    [Header("Verb Highlights")]
    public Image lookAtHighlight;
    public Image pickUpHighlight;
    public Image useItemHighlight;
    public Image talkToHighlight;
    public Image interactHighlight;
    public Image useZoeyHighlight;

    private Verb[] verbOrder = {
        Verb.LookAt,
        Verb.PickUp,
        Verb.UseItem,
        Verb.TalkTo,
        Verb.Interact,
        Verb.UseZoey
    };

    private int currentVerbIndex = 0;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        UpdateHighlights();
    }

    // Returns true if either character is currently speaking a line
    private bool IsDialoguePlaying()
    {
        bool curlyTalking = DialogueLabel.curlyLabel != null && DialogueLabel.curlyLabel.IsDisplaying();
        bool zoeyTalking = DialogueLabel.zoeyLabel != null && DialogueLabel.zoeyLabel.IsDisplaying();
        return curlyTalking || zoeyTalking;
    }

    void Update()
    {
        if (PhoneBoothUI.isInPhoneBooth) return;
        if (DialogueManager.isInDialogue) return;

        // Don't allow verb switching while a spoken line is playing
        if (IsDialoguePlaying()) return;

        // Keyboard number shortcuts
        if (Keyboard.current.digit1Key.wasPressedThisFrame) SetVerb(Verb.LookAt);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SetVerb(Verb.PickUp);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SetVerb(Verb.UseItem);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) SetVerb(Verb.TalkTo);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) SetVerb(Verb.Interact);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) SetVerb(Verb.UseZoey);

        // Tab to cycle forward, Shift+Tab to cycle back
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (Keyboard.current.leftShiftKey.isPressed)
            {
                currentVerbIndex--;
                if (currentVerbIndex < 0) currentVerbIndex = verbOrder.Length - 1;
            }
            else
            {
                currentVerbIndex++;
                if (currentVerbIndex >= verbOrder.Length) currentVerbIndex = 0;
            }
            SetVerb(verbOrder[currentVerbIndex]);
        }

        // Controller
        if (Gamepad.current != null)
        {
            if (Gamepad.current.leftShoulder.wasPressedThisFrame)
            {
                currentVerbIndex--;
                if (currentVerbIndex < 0) currentVerbIndex = verbOrder.Length - 1;
                SetVerb(verbOrder[currentVerbIndex]);
            }

            if (Gamepad.current.rightShoulder.wasPressedThisFrame)
            {
                currentVerbIndex++;
                if (currentVerbIndex >= verbOrder.Length) currentVerbIndex = 0;
                SetVerb(verbOrder[currentVerbIndex]);
            }
        }
    }

    public void SetVerb(Verb verb)
    {
        currentVerb = verb;
        for (int i = 0; i < verbOrder.Length; i++)
        {
            if (verbOrder[i] == verb)
            {
                currentVerbIndex = i;
                break;
            }
        }
        UpdateHighlights();
    }

    void UpdateHighlights()
    {
        Color highlight = new Color(0f, 0.5f, 1f, 0.5f);

        lookAtHighlight.color = currentVerb == Verb.LookAt ? highlight : Color.clear;
        pickUpHighlight.color = currentVerb == Verb.PickUp ? highlight : Color.clear;
        useItemHighlight.color = currentVerb == Verb.UseItem ? highlight : Color.clear;
        talkToHighlight.color = currentVerb == Verb.TalkTo ? highlight : Color.clear;
        interactHighlight.color = currentVerb == Verb.Interact ? highlight : Color.clear;
        useZoeyHighlight.color = currentVerb == Verb.UseZoey ? highlight : Color.clear;
    }

    public void SetLookAt() { SetVerb(Verb.LookAt); }
    public void SetPickUp() { SetVerb(Verb.PickUp); }
    public void SetUseItem() { SetVerb(Verb.UseItem); }
    public void SetTalkTo() { SetVerb(Verb.TalkTo); }
    public void SetInteract() { SetVerb(Verb.Interact); }
    public void SetUseZoey() { SetVerb(Verb.UseZoey); }
}