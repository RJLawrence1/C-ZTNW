using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class Interactable : MonoBehaviour, IInteractable
{
    public string itemName = "Test Item";

    [Header("Allowed Verbs")]
    public bool canLookAt = true;
    public bool canPickUp = false;
    public bool canUseItem = false;
    public bool canTalkTo = false;
    public bool canInteract = false;
    public bool canUseZoey = false;

    [Header("Use Item Responses")]
    public ItemResponse[] itemResponses;

    [System.Serializable]
    public class ItemResponse
    {
        public string itemName;
        [TextArea] public string response;
        public bool consumesItem = false;
    }

    [Header("Pickup Lines")]
    public string pickUpExamineLine = "Hm, what's this?";
    public string pickUpLine = "Yeah this could definitely come in handy.";

    private int lookAtCount = 0;
    private int pickUpCount = 0;
    private int useItemCount = 0;
    private int talkToCount = 0;
    private int interactCount = 0;
    private int useZoeyCount = 0;

    private float resetTimer = 0f;
    private float resetTime = 5f;
    private bool hasInteracted = false;
    private bool isLockedOut = false;

    // Cached references — avoids FindObjectOfType on every click
    private CurlyMovement curly;
    private ZoeyAI zoey;

    private string[] lookAtFails = {
        "Nothing to see here.",
        "Still nothing to see here.",
        "I don't know what you're looking for but it's not there.",
        "Are you really doing this again?",
        "I'm ignoring you now."
    };

    private string[] pickUpFails = {
        "I can't pick that up.",
        "No really, I can't pick that up.",
        "I really cannot pick that up.",
        "I. Can't. Pick. That. Up.",
        "Why do I even bother sometimes..."
    };

    private string[] useItemFails = {
        "I can't use that.",
        "Nope, still can't use that.",
        "What exactly do you think is going to happen here?",
        "I have tried. It doesn't work. Move on.",
        "..."
    };

    private string[] talkToFails = {
        "I don't think it wants to talk.",
        "It's still not talking.",
        "It's an inanimate object.",
        "I am not talking to that.",
        "I'm done."
    };

    private string[] interactFails = {
        "Nothing happens.",
        "Nothing happened again.",
        "Shocking, still nothing.",
        "What are you expecting from me here?",
        "I give up."
    };

    private string[] useZoeyFails = {
        "I don't think that's a good idea.",
        "Yeah no, still not doing that.",
        "Curly, why do they keep asking me this?",
        "I have better things to do than this.",
        "I'm going for a walk."
    };

    void Start()
    {
        curly = FindObjectOfType<CurlyMovement>();
        zoey = FindObjectOfType<ZoeyAI>();
    }

    void Update()
    {
        if (PhoneBoothUI.isInPhoneBooth) return;

        if (hasInteracted)
        {
            resetTimer += Time.deltaTime;
            if (resetTimer >= resetTime)
                ResetCounts();
        }

        int iLayer = LayerMask.GetMask("Interactable");

        Vector2 hoverPos = Mouse.current.position.ReadValue();
        RaycastHit2D hoverHit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(hoverPos), Vector2.zero, Mathf.Infinity, iLayer);

        if (hoverHit.collider != null && hoverHit.collider.gameObject == gameObject)
            HotspotLabel.instance.Show(itemName, transform.position);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(mousePos), Vector2.zero, Mathf.Infinity, iLayer);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                if (isLockedOut) return;

                // Don't allow new interactions while dialogue panel is open or a line is playing
                if (DialogueManager.isInDialogue) return;
                if (DialogueLabel.curlyLabel.IsDisplaying()) return;

                if (VerbManager.instance.currentVerb == VerbManager.Verb.UseZoey)
                    StartCoroutine(ZoeySequence());
                else
                    curly.WalkToInteract(this);
            }
        }
    }

    void ResetCounts()
    {
        lookAtCount = 0;
        pickUpCount = 0;
        useItemCount = 0;
        talkToCount = 0;
        interactCount = 0;
        useZoeyCount = 0;
        resetTimer = 0f;
        hasInteracted = false;
        isLockedOut = false;
    }

    IEnumerator ZoeySequence()
    {
        DialogueLabel.curlyLabel.Say("Hey Zo, can you come check this out for me?");
        yield return new WaitForSeconds(2f);
        DialogueLabel.zoeyLabel.Say("Already on it.");
        yield return new WaitForSeconds(1f);
        zoey.WalkToInteract(this);
    }

    IEnumerator PickUpSequence()
    {
        DialogueLabel.curlyLabel.Say(pickUpExamineLine);
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say(pickUpLine);
        yield return new WaitForSeconds(3f);
        Sprite sprite = GetComponent<SpriteRenderer>() != null ? GetComponent<SpriteRenderer>().sprite : null;
        Color color = GetComponent<SpriteRenderer>() != null ? GetComponent<SpriteRenderer>().color : Color.white;
        InventoryManager.instance.AddItem(itemName, sprite, color);
        gameObject.SetActive(false);
    }

    string GetFailLine(string[] lines, ref int count)
    {
        string line = lines[Mathf.Min(count, lines.Length - 1)];
        if (count >= lines.Length - 1)
            isLockedOut = true;
        else
            count++;
        return line;
    }

    public void OnItemUsed(string itemName)
    {
        foreach (ItemResponse response in itemResponses)
        {
            if (response.itemName == itemName)
            {
                DialogueLabel.curlyLabel.Say(response.response);
                if (response.consumesItem)
                    InventoryManager.instance.RemoveItem(itemName);
                return;
            }
        }
        DialogueLabel.curlyLabel.Say("I don't think that'll work.");
    }

    public void OnInteract()
    {
        if (VerbManager.instance == null) return;

        hasInteracted = true;
        resetTimer = 0f;

        switch (VerbManager.instance.currentVerb)
        {
            case VerbManager.Verb.LookAt:
                if (canLookAt)
                    DialogueLabel.curlyLabel.Say("It's a " + itemName + ".");
                else
                    DialogueLabel.curlyLabel.Say(GetFailLine(lookAtFails, ref lookAtCount));
                break;
            case VerbManager.Verb.PickUp:
                if (canPickUp)
                    StartCoroutine(PickUpSequence());
                else
                    DialogueLabel.curlyLabel.Say(GetFailLine(pickUpFails, ref pickUpCount));
                break;
            case VerbManager.Verb.UseItem:
                if (canUseItem)
                    DialogueLabel.curlyLabel.Say("I use the " + itemName + ".");
                else
                    DialogueLabel.curlyLabel.Say(GetFailLine(useItemFails, ref useItemCount));
                break;
            case VerbManager.Verb.TalkTo:
                if (canTalkTo)
                {
                    NPCDialogue npcDialogue = GetComponent<NPCDialogue>();
                    if (npcDialogue != null)
                        npcDialogue.StartConversation();
                    else
                        DialogueLabel.curlyLabel.Say("Hey there.");
                }
                else
                    DialogueLabel.curlyLabel.Say(GetFailLine(talkToFails, ref talkToCount));
                break;
            case VerbManager.Verb.Interact:
                if (canInteract)
                    DialogueLabel.curlyLabel.Say("I interact with the " + itemName + ".");
                else
                    DialogueLabel.curlyLabel.Say(GetFailLine(interactFails, ref interactCount));
                break;
            case VerbManager.Verb.UseZoey:
                if (canUseZoey)
                    DialogueLabel.zoeyLabel.Say("Leave it to me!");
                else
                    DialogueLabel.zoeyLabel.Say(GetFailLine(useZoeyFails, ref useZoeyCount));
                break;
            default:
                DialogueLabel.curlyLabel.Say("Hm.");
                break;
        }
    }
}