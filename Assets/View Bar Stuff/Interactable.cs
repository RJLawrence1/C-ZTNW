using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class Interactable : MonoBehaviour, IInteractable
{
    public string itemName = "Test Item";

    // Tracks all picked-up item names across scene transitions
    public static System.Collections.Generic.HashSet<string> pickedUpItems
        = new System.Collections.Generic.HashSet<string>();

    // Registry of item name -> sprite, so inventory can look up sprites after scene loads
    public static System.Collections.Generic.Dictionary<string, Sprite> spriteRegistry
        = new System.Collections.Generic.Dictionary<string, Sprite>();

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
    public string pickUpExamineLine = "Hm.";
    public string pickUpLine = "Yeah. That's coming with me.";

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
        "Nothing.",
        "Still nothing.",
        "I've looked at it. It's not interesting.",
        "You're really committed to this.",
        "I'm not looking anymore."
    };

    private string[] pickUpFails = {
        "Not taking that.",
        "Can't. Won't.",
        "I've tried. It's a no.",
        "This isn't a conversation I'm going to win, is it.",
        "Fine. We're leaving it."
    };

    private string[] useItemFails = {
        "That's not the move.",
        "Still not the move.",
        "I know what you're thinking. You're wrong.",
        "Tried it. Nothing.",
        "..."
    };

    private string[] talkToFails = {
        "It's not talking.",
        "Hasn't changed.",
        "It's not alive.",
        "I'm not doing this anymore.",
        "We're done."
    };

    private string[] interactFails = {
        "Nothing.",
        "Same as before.",
        "I don't know what the expectation is here.",
        "It's just sitting there.",
        "Alright. I'm out."
    };

    private string[] useZoeyFails = {
        "Nope.",
        "Still nope.",
        "Curly, tell them to stop.",
        "I don't know what they think is gonna happen.",
        "I'm taking a walk. Don't follow me."
    };

    void Awake()
    {
        // Register sprite early so InventoryManager.Start can find it
        Sprite sprite = GetComponent<SpriteRenderer>() != null ? GetComponent<SpriteRenderer>().sprite : null;
        if (sprite != null)
            spriteRegistry[itemName] = sprite;
    }

    void Start()
    {
        curly = FindObjectOfType<CurlyMovement>();
        zoey = FindObjectOfType<ZoeyAI>();

        // If this item was already picked up in a previous scene, hide it immediately
        if (pickedUpItems.Contains(itemName))
            gameObject.SetActive(false);
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
        DialogueLabel.curlyLabel.Say("Zo. Come take a look at this.");
        yield return new WaitForSeconds(2f);
        DialogueLabel.zoeyLabel.Say("On it.");
        yield return new WaitForSeconds(1f);
        zoey.WalkToInteract(this);
    }

    IEnumerator PickUpSequence()
    {
        DialogueLabel.curlyLabel.skipEnabled = false;

        DialogueLabel.curlyLabel.Say(pickUpExamineLine);
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say(pickUpLine);
        yield return new WaitForSeconds(3f);

        DialogueLabel.curlyLabel.skipEnabled = true;

        Sprite sprite = GetComponent<SpriteRenderer>() != null ? GetComponent<SpriteRenderer>().sprite : null;
        Color color = GetComponent<SpriteRenderer>() != null ? GetComponent<SpriteRenderer>().color : Color.white;
        InventoryManager.instance.AddItem(itemName, sprite, color);
        Debug.Log("InventoryData count after pickup: " + InventoryData.names.Count);
        pickedUpItems.Add(itemName);
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

    public void OnItemUsed(string usedItemName)
    {
        // If this interactable has an NPCDialogue, route to the trade system first
        NPCDialogue npcDialogue = GetComponent<NPCDialogue>();
        if (npcDialogue != null)
        {
            npcDialogue.TryTradeItem(usedItemName);
            return;
        }

        // Otherwise use the standard item response list
        foreach (ItemResponse response in itemResponses)
        {
            if (response.itemName == usedItemName)
            {
                DialogueLabel.curlyLabel.Say(response.response);
                if (response.consumesItem)
                    InventoryManager.instance.RemoveItem(usedItemName);
                return;
            }
        }
        DialogueLabel.curlyLabel.Say("That's not going to do anything.");
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
                    DialogueLabel.curlyLabel.Say("That's a " + itemName + ".");
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
                {
                    // Check if a specific item is selected via the cursor
                    if (ItemCursor.hasSelectedItem)
                        OnItemUsed(ItemCursor.selectedItemName);
                    else
                        DialogueLabel.curlyLabel.Say("Use what.");
                }
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
                        DialogueLabel.curlyLabel.Say("Hey.");
                }
                else
                    DialogueLabel.curlyLabel.Say(GetFailLine(talkToFails, ref talkToCount));
                break;
            case VerbManager.Verb.Interact:
                if (canInteract)
                    DialogueLabel.curlyLabel.Say("Alright.");
                else
                    DialogueLabel.curlyLabel.Say(GetFailLine(interactFails, ref interactCount));
                break;
            case VerbManager.Verb.UseZoey:
                if (canUseZoey)
                    DialogueLabel.zoeyLabel.Say("I'm already there.");
                else
                    DialogueLabel.zoeyLabel.Say(GetFailLine(useZoeyFails, ref useZoeyCount));
                break;
            default:
                DialogueLabel.curlyLabel.Say("Hm.");
                break;
        }
    }
}