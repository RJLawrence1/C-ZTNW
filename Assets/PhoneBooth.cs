using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhoneBooth : MonoBehaviour, IInteractable
{
    private bool isInUse = false;

    // Cached references
    private CurlyMovement curly;
    private ZoeyAI zoey;

    private string[] callOverLines = {
        "Zo. Let's go.",
        "Zoey. Come on.",
        "Hey. Booth. Now.",
        "Zoey, move it.",
        "Let's go, Zo. Come on."
    };

    private int callOverCount = 0;

    private string[] pickUpFails = {
        "Can't pick up a phone booth.",
        "Still can't.",
        "It's bolted down. I checked.",
        "We're not doing this again.",
        "I'm ignoring this now."
    };

    private string[] useItemFails = {
        "That's not going to do anything.",
        "Still no.",
        "I don't know what you're expecting.",
        "Tried it. Nothing.",
        "..."
    };

    private string[] talkToFails = {
        "It's a phone booth. You call people with it.",
        "Still a phone booth.",
        "I genuinely don't know what you want from me.",
        "Not talking to it.",
        "Done."
    };

    private int useZoeyCount = 0;
    private int pickUpCount = 0;
    private int useItemCount = 0;
    private int talkToCount = 0;

    private float resetTimer = 0f;
    private float resetTime = 5f;
    private bool hasInteracted = false;
    private bool isLockedOut = false;

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
            HotspotLabel.instance.Show("Phone Booth", transform.position);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(mousePos), Vector2.zero, Mathf.Infinity, iLayer);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                if (isLockedOut) return;
                if (DialogueManager.isInDialogue) return;
                if (DialogueLabel.curlyLabel.IsDisplaying()) return;
                curly.WalkToInteract(this);
            }
        }
    }

    void ResetCounts()
    {
        pickUpCount = 0;
        useItemCount = 0;
        talkToCount = 0;
        useZoeyCount = 0;
        resetTimer = 0f;
        hasInteracted = false;
        isLockedOut = false;
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

    public void OnInteract()
    {
        if (isInUse) return;
        if (VerbManager.instance == null) return;

        hasInteracted = true;
        resetTimer = 0f;

        switch (VerbManager.instance.currentVerb)
        {
            case VerbManager.Verb.LookAt:
                DialogueLabel.curlyLabel.Say("Phone booth.");
                break;
            case VerbManager.Verb.PickUp:
                DialogueLabel.curlyLabel.Say(GetFailLine(pickUpFails, ref pickUpCount));
                break;
            case VerbManager.Verb.UseItem:
                DialogueLabel.curlyLabel.Say(GetFailLine(useItemFails, ref useItemCount));
                break;
            case VerbManager.Verb.TalkTo:
                DialogueLabel.curlyLabel.Say(GetFailLine(talkToFails, ref talkToCount));
                break;
            case VerbManager.Verb.Interact:
                StartCoroutine(EnterBooth());
                break;
            case VerbManager.Verb.UseZoey:
                StartCoroutine(UseZoeySequence());
                break;
            default:
                DialogueLabel.curlyLabel.Say("Phone booth.");
                break;
        }
    }

    IEnumerator EnterBooth()
    {
        isInUse = true;

        // Curly calls Zoey over
        DialogueLabel.curlyLabel.Say(callOverLines[callOverCount % callOverLines.Length]);
        callOverCount++;
        yield return new WaitForSeconds(2f);

        // Zoey hustles to her spawn point next to the booth
        if (zoey != null)
        {
            Transform zoeySpawn = transform.Find("ZoeySpawn");
            Vector3 destination = zoeySpawn != null ? zoeySpawn.position : transform.position;
            zoey.HustleTo(destination);

            // Wait for her to arrive, with a 10 second timeout
            float timeout = 10f;
            while (!zoey.hasArrived && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        // Now open the phone UI
        PhoneBoothUI.instance.Show(this);
    }

    public void ExitBooth()
    {
        isInUse = false;
        if (zoey != null)
            zoey.StopAndStay();
    }

    IEnumerator UseZoeySequence()
    {
        if (useZoeyCount == 0)
        {
            DialogueLabel.curlyLabel.Say("Zo, take a look at this.");
            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
            yield return new WaitForSeconds(0.3f);
            DialogueLabel.zoeyLabel.Say("Already looked at it. It's a phone booth.");
        }
        else if (useZoeyCount == 1)
        {
            DialogueLabel.curlyLabel.Say("Yeah but does anything seem off about it?");
            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
            yield return new WaitForSeconds(0.3f);
            DialogueLabel.zoeyLabel.Say("It's in the middle of nowhere. Everything seems off about it.");
        }
        else if (useZoeyCount == 2)
        {
            DialogueLabel.curlyLabel.Say("...Fair point.");
        }
        else
        {
            DialogueLabel.zoeyLabel.Say("I'm taking a walk.");
            isLockedOut = true;
        }

        if (useZoeyCount < 3)
            useZoeyCount++;
    }
}