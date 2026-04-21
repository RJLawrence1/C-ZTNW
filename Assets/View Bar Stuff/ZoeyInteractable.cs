using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ZoeyInteractable : MonoBehaviour, IInteractable
{
    private ZoeyAI zoeyAI;
    private CurlyMovement curly;

    // Reusable line struct — text, clip, and emotion all in one Inspector block
    [System.Serializable]
    public class ZoeyLine
    {
        [TextArea] public string line;
        public AudioClip clip;
        public DialogueScreen.Emotion emotion = DialogueScreen.Emotion.Normal;
    }

    [Header("Zoey Portrait")]
    public Sprite zoeyPortrait;
    public Color zoeyNameColor = new Color(1f, 0.5f, 0.8f);

    [Header("Verb Response Clips")]
    public AudioClip lookAtClip;
    public AudioClip pickUpClip;
    public AudioClip useItemClip;
    public AudioClip interactClip;

    [Header("How You Holdin' Up")]
    public ZoeyLine howsItGoing_Curly1;
    public ZoeyLine howsItGoing_Zoey1;
    public ZoeyLine howsItGoing_Curly2;
    public ZoeyLine howsItGoing_Zoey2;

    [Header("You Good")]
    public ZoeyLine youGood_Curly1;
    public ZoeyLine youGood_Zoey1;
    public ZoeyLine youGood_Curly2;
    public ZoeyLine youGood_Zoey2;

    [Header("Should Get Moving")]
    public ZoeyLine moving_Curly1;
    public ZoeyLine moving_Zoey1;
    public ZoeyLine moving_Curly2;
    public ZoeyLine moving_Zoey2;

    [Header("Never Mind")]
    public ZoeyLine neverMind_Curly;
    public ZoeyLine neverMind_Zoey;

    [Header("Use Zoey On Zoey")]
    public ZoeyLine useZoeyOnZoey_Curly1;
    public ZoeyLine useZoeyOnZoey_Curly2;
    public ZoeyLine useZoeyOnZoey_Zoey;
    public ZoeyLine useZoeyOnZoey_Curly3;

    const float fallbackDuration = 3f;

    void Start()
    {
        zoeyAI = GetComponent<ZoeyAI>();
        curly = FindObjectOfType<CurlyMovement>();
    }

    void Update()
    {
        if (PhoneBoothUI.isInPhoneBooth) return;

        int iLayer = LayerMask.GetMask("Interactable");

        // Only do mouse hover when not using controller — controller handles its own hotspot labels
        if (!ControllerCursor.usingController)
        {
            Vector2 hoverPos = Mouse.current.position.ReadValue();
            RaycastHit2D hoverHit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(hoverPos), Vector2.zero, Mathf.Infinity, iLayer);

            if (hoverHit.collider != null && hoverHit.collider.gameObject == gameObject)
                HotspotLabel.instance.Show("Zoey", transform.position);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(mousePos), Vector2.zero, Mathf.Infinity, iLayer);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                if (DialogueManager.isInDialogue) return;
                if (DialogueLabel.curlyLabel.IsDisplaying()) return;

                switch (VerbManager.instance.currentVerb)
                {
                    case VerbManager.Verb.TalkTo:
                    case VerbManager.Verb.UseZoey:
                        zoeyAI.StopAndStay();
                        zoeyAI.isPaused = true;
                        curly.WalkToInteract(this);
                        break;
                    default:
                        OnInteract();
                        break;
                }
            }
        }
    }

    public void OnInteract()
    {
        if (VerbManager.instance == null) return;

        switch (VerbManager.instance.currentVerb)
        {
            case VerbManager.Verb.LookAt:
                DialogueLabel.curlyLabel.Say("That's Zoey.", lookAtClip);
                break;
            case VerbManager.Verb.PickUp:
                DialogueLabel.curlyLabel.Say("Not happening.", pickUpClip);
                break;
            case VerbManager.Verb.UseItem:
                DialogueLabel.curlyLabel.Say("That's not what she's for.", useItemClip);
                break;
            case VerbManager.Verb.TalkTo:
                OpenTalkToZoey();
                break;
            case VerbManager.Verb.Interact:
                DialogueLabel.curlyLabel.Say("Last time I tried that she bit me.", interactClip);
                break;
            case VerbManager.Verb.UseZoey:
                StartCoroutine(UseZoeyOnZoey());
                break;
        }
    }

    void OpenTalkToZoey()
    {
        if (DialogueScreen.instance != null)
            DialogueScreen.instance.Show("Zoey", zoeyPortrait, zoeyNameColor);

        string[] options = {
            "How you holdin' up?",
            "You good?",
            "We should get moving.",
            "Never mind."
        };

        System.Action[] actions = {
            () => StartCoroutine(HowsItGoing()),
            () => StartCoroutine(YouDoingOkay()),
            () => StartCoroutine(ShouldGetMoving()),
            () => StartCoroutine(NeverMind())
        };

        DialogueManager.instance.ShowDialogue(options, actions);
    }

    void EndConversation()
    {
        if (DialogueScreen.instance != null)
            DialogueScreen.instance.Hide();

        zoeyAI.isPaused = false;
    }

    // Plays a Curly line on the dialogue screen
    IEnumerator SayCurly(ZoeyLine l)
    {
        if (string.IsNullOrEmpty(l.line)) yield break;
        float dur = l.clip != null ? l.clip.length : fallbackDuration;
        DialogueScreen.instance.SayCurly(l.line, dur, l.emotion);
        yield return new WaitUntil(() => !DialogueScreen.instance.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
    }

    // Plays a Zoey line on the dialogue screen
    IEnumerator SayZoey(ZoeyLine l)
    {
        if (string.IsNullOrEmpty(l.line)) yield break;
        float dur = l.clip != null ? l.clip.length : fallbackDuration;
        DialogueScreen.instance.SayZoey(l.line, dur, l.emotion);
        yield return new WaitUntil(() => !DialogueScreen.instance.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator HowsItGoing()
    {
        yield return StartCoroutine(SayCurly(howsItGoing_Curly1));
        yield return StartCoroutine(SayZoey(howsItGoing_Zoey1));
        yield return StartCoroutine(SayCurly(howsItGoing_Curly2));
        yield return StartCoroutine(SayZoey(howsItGoing_Zoey2));
        OpenTalkToZoey();
    }

    IEnumerator YouDoingOkay()
    {
        yield return StartCoroutine(SayCurly(youGood_Curly1));
        yield return StartCoroutine(SayZoey(youGood_Zoey1));
        yield return StartCoroutine(SayCurly(youGood_Curly2));
        yield return StartCoroutine(SayZoey(youGood_Zoey2));
        OpenTalkToZoey();
    }

    IEnumerator ShouldGetMoving()
    {
        yield return StartCoroutine(SayCurly(moving_Curly1));
        yield return StartCoroutine(SayZoey(moving_Zoey1));
        yield return StartCoroutine(SayCurly(moving_Curly2));
        yield return StartCoroutine(SayZoey(moving_Zoey2));
        OpenTalkToZoey();
    }

    IEnumerator NeverMind()
    {
        yield return StartCoroutine(SayCurly(neverMind_Curly));
        yield return StartCoroutine(SayZoey(neverMind_Zoey));
        EndConversation();
    }

    IEnumerator UseZoeyOnZoey()
    {
        // UseZoey stays as world-space labels — no dialogue screen
        DialogueLabel.curlyLabel.Say(useZoeyOnZoey_Curly1.line, useZoeyOnZoey_Curly1.clip);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say(useZoeyOnZoey_Curly2.line, useZoeyOnZoey_Curly2.clip);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say(useZoeyOnZoey_Zoey.line, useZoeyOnZoey_Zoey.clip);
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say(useZoeyOnZoey_Curly3.line, useZoeyOnZoey_Curly3.clip);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        zoeyAI.isPaused = false;
    }
}