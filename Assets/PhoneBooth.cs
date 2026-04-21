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
    [Header("Call Over Clips")]
    public AudioClip[] callOverClips = new AudioClip[5];

    private int callOverCount = 0;

    private string[] pickUpFails = {
        "Can't pick up a phone booth.",
        "Still can't.",
        "It's bolted down. I checked.",
        "We're not doing this again.",
        "I'm ignoring this now."
    };
    [Header("Pick Up Fail Clips")]
    public AudioClip[] pickUpFailClips = new AudioClip[5];

    private string[] useItemFails = {
        "That's not going to do anything.",
        "Still no.",
        "I don't know what you're expecting.",
        "Tried it. Nothing.",
        "..."
    };
    [Header("Use Item Fail Clips")]
    public AudioClip[] useItemFailClips = new AudioClip[5];

    private string[] talkToFails = {
        "It's a phone booth. You call people with it.",
        "Still a phone booth.",
        "I genuinely don't know what you want from me.",
        "Not talking to it.",
        "Done."
    };
    [Header("Talk To Fail Clips")]
    public AudioClip[] talkToFailClips = new AudioClip[5];

    [Header("Use Zoey Sequence Clips")]
    public AudioClip useZoey_Curly1;
    public AudioClip useZoey_Zoey1;
    public AudioClip useZoey_Curly2;
    public AudioClip useZoey_Zoey2;
    public AudioClip useZoey_Curly3;
    public AudioClip useZoey_ZoeyFinal;

    [Header("Look At Clip")]
    public AudioClip lookAtClip;

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

        int iLayer = LayerMask.GetMask("PhoneBooth");

        // Hover — use controller cursor position if using controller, otherwise mouse
        Vector2 hoverPos = ControllerCursor.usingController && ControllerCursor.instance != null
            ? ControllerCursor.instance.GetScreenPositionPublic()
            : Mouse.current.position.ReadValue();

        RaycastHit2D hoverHit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(hoverPos), Vector2.zero, Mathf.Infinity, iLayer);

        if (hoverHit.collider != null && hoverHit.collider.gameObject == gameObject)
            HotspotLabel.instance.Show("Phone Booth", transform.position);

        // Click — mouse left click OR controller right trigger via SimulateClick in ControllerCursor
        if (Mouse.current.leftButton.wasPressedThisFrame && !ControllerCursor.usingController)
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

    string GetFailLine(string[] lines, AudioClip[] clips, ref int count, out AudioClip clip)
    {
        int index = Mathf.Min(count, lines.Length - 1);
        string line = lines[index];
        clip = (clips != null && index < clips.Length) ? clips[index] : null;
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
                DialogueLabel.curlyLabel.Say("Phone booth.", lookAtClip);
                break;
            case VerbManager.Verb.PickUp:
                { AudioClip c; DialogueLabel.curlyLabel.Say(GetFailLine(pickUpFails, pickUpFailClips, ref pickUpCount, out c), c); }
                break;
            case VerbManager.Verb.UseItem:
                { AudioClip c; DialogueLabel.curlyLabel.Say(GetFailLine(useItemFails, useItemFailClips, ref useItemCount, out c), c); }
                break;
            case VerbManager.Verb.TalkTo:
                { AudioClip c; DialogueLabel.curlyLabel.Say(GetFailLine(talkToFails, talkToFailClips, ref talkToCount, out c), c); }
                break;
            case VerbManager.Verb.Interact:
                StartCoroutine(EnterBooth());
                break;
            case VerbManager.Verb.UseZoey:
                StartCoroutine(UseZoeySequence());
                break;
            default:
                DialogueLabel.curlyLabel.Say("Phone booth.", lookAtClip);
                break;
        }
    }

    IEnumerator EnterBooth()
    {
        isInUse = true;

        // Get spawn points
        Transform entryPoint = transform.Find("EntryPoint");
        Transform curlyInside = transform.Find("CurlyInside");
        Transform zoeyInside = transform.Find("ZoeyInside");
        Transform zoeyEntry = transform.Find("ZoeySpawn");

        // Curly walks to entry point — disable collider so he doesn't get shoved
        Collider2D curlyCol = curly.GetComponent<Collider2D>();
        if (curlyCol != null) curlyCol.enabled = false;

        // Make sure input is unlocked so Curly can walk to entry point
        curly.inputLocked = false;
        curly.CancelMovement();

        if (entryPoint != null)
        {
            curly.WalkToPosition(entryPoint.position);
            yield return new WaitUntil(() =>
                Vector3.Distance(curly.transform.position, entryPoint.position) < 0.2f);
        }

        // Lock Curly now that he's at the entry point
        curly.inputLocked = true;
        curly.CancelMovement();

        // Move Curly inside
        if (curlyInside != null)
            curly.transform.position = curlyInside.position;

        // Curly calls Zoey over
        int callIndex = callOverCount % callOverLines.Length;
        AudioClip callClip = (callOverClips != null && callIndex < callOverClips.Length) ? callOverClips[callIndex] : null;
        DialogueLabel.curlyLabel.Say(callOverLines[callIndex], callClip);
        callOverCount++;
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());

        // Zoey hustles to entry point — disable collider so she doesn't get shoved
        Collider2D zoeyCol = zoey != null ? zoey.GetComponent<Collider2D>() : null;
        if (zoeyCol != null) zoeyCol.enabled = false;

        if (zoey != null)
        {
            Vector3 zoeyDest = zoeyEntry != null ? zoeyEntry.position : entryPoint != null ? entryPoint.position : transform.position;
            zoey.HustleTo(zoeyDest);

            float timeout = 10f;
            while (!zoey.hasArrived && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        // Move Zoey inside
        if (zoeyInside != null && zoey != null)
            zoey.transform.position = zoeyInside.position;

        // Re-enable only Zoey's collider after a moment — Curly's stays off until exit
        yield return new WaitForSeconds(0.5f);
        if (zoeyCol != null) zoeyCol.enabled = true;

        // Freeze Zoey so she doesn't wander out while UI is open
        if (zoey != null) zoey.isPaused = true;

        // Open the phone UI
        if (RumbleManager.instance != null) RumbleManager.instance.Rumble(0.2f, 0.15f, 0.2f);
        PhoneBoothUI.instance.Show(this);
    }

    // Called on hangup and easter egg endings — steps both characters out of the booth
    public IEnumerator ExitBoothSequence()
    {
        isInUse = false;

        // Unfreeze both now that UI is closed
        if (zoey != null) zoey.isPaused = false;
        if (curly != null) curly.inputLocked = false;

        // How far left to step out — adjust to match your booth width
        float exitDistance = 1.5f;

        // Get booth collider to know when they're clear
        Collider2D boothCol = GetComponent<Collider2D>();

        // Zoey exits first
        if (zoey != null)
        {
            Collider2D zoeyCol = zoey.GetComponent<Collider2D>();
            Rigidbody2D zoeyRb = zoey.GetComponent<Rigidbody2D>();
            if (zoeyCol != null) zoeyCol.enabled = false;
            if (zoeyRb != null) zoeyRb.simulated = true;

            Vector3 zoeyExit = zoey.transform.position + Vector3.left * exitDistance;
            zoey.HustleTo(zoeyExit);

            float timeout = 5f;
            while (!zoey.hasArrived && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            zoey.StopAndStay();
            yield return new WaitForSeconds(0.2f);
            if (zoeyCol != null) zoeyCol.enabled = true;
        }

        yield return new WaitForSeconds(0.2f);

        // Curly exits
        if (curly != null)
        {
            Collider2D curlyCol = curly.GetComponent<Collider2D>();
            Rigidbody2D curlyRb = curly.GetComponent<Rigidbody2D>();
            if (curlyCol != null) curlyCol.enabled = false;
            if (curlyRb != null) curlyRb.simulated = true;
            curly.inputLocked = false;
            curly.CancelMovement();

            Vector3 curlyExit = curly.transform.position + Vector3.left * exitDistance;
            curly.WalkToPosition(curlyExit);

            yield return new WaitUntil(() =>
                Vector3.Distance(curly.transform.position, curlyExit) < 0.2f);

            yield return new WaitForSeconds(0.2f);
            if (curlyCol != null) curlyCol.enabled = true;
        }
    }

    public void ExitBooth()
    {
        isInUse = false;
        if (zoey != null)
        {
            zoey.isPaused = false;
            zoey.StopAndStay();
        }
        if (curly != null) curly.inputLocked = false;
    }

    IEnumerator UseZoeySequence()
    {
        if (useZoeyCount == 0)
        {
            DialogueLabel.curlyLabel.Say("Zo, take a look at this.", useZoey_Curly1);
            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
            yield return new WaitForSeconds(0.3f);
            DialogueLabel.zoeyLabel.Say("Already looked at it. It's a phone booth.", useZoey_Zoey1);
        }
        else if (useZoeyCount == 1)
        {
            DialogueLabel.curlyLabel.Say("Yeah but does anything seem off about it?", useZoey_Curly2);
            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
            yield return new WaitForSeconds(0.3f);
            DialogueLabel.zoeyLabel.Say("It's in the middle of nowhere. Everything seems off about it.", useZoey_Zoey2);
        }
        else if (useZoeyCount == 2)
        {
            DialogueLabel.curlyLabel.Say("...Fair point.", useZoey_Curly3);
        }
        else
        {
            DialogueLabel.zoeyLabel.Say("I'm taking a walk.", useZoey_ZoeyFinal);
            isLockedOut = true;
        }

        if (useZoeyCount < 3)
            useZoeyCount++;
    }
}