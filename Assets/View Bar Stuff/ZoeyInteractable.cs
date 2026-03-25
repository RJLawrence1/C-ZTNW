using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ZoeyInteractable : MonoBehaviour, IInteractable
{
    private ZoeyAI zoeyAI;
    private CurlyMovement curly;

    [Header("Verb Response Clips")]
    public AudioClip lookAtClip;
    public AudioClip pickUpClip;
    public AudioClip useItemClip;
    public AudioClip interactClip;

    [Header("How You Holdin' Up Clips")]
    public AudioClip howsItGoing_Curly1;
    public AudioClip howsItGoing_Zoey1;
    public AudioClip howsItGoing_Curly2;
    public AudioClip howsItGoing_Zoey2;

    [Header("You Good Clips")]
    public AudioClip youGood_Curly1;
    public AudioClip youGood_Zoey1;
    public AudioClip youGood_Curly2;
    public AudioClip youGood_Zoey2;

    [Header("Should Get Moving Clips")]
    public AudioClip moving_Curly1;
    public AudioClip moving_Zoey1;
    public AudioClip moving_Curly2;
    public AudioClip moving_Zoey2;

    [Header("Never Mind Clips")]
    public AudioClip neverMind_Curly;
    public AudioClip neverMind_Zoey;

    [Header("Use Zoey On Zoey Clips")]
    public AudioClip useZoeyOnZoey_Curly1;
    public AudioClip useZoeyOnZoey_Curly2;
    public AudioClip useZoeyOnZoey_Zoey;
    public AudioClip useZoeyOnZoey_Curly3;

    void Start()
    {
        zoeyAI = GetComponent<ZoeyAI>();
        curly = FindObjectOfType<CurlyMovement>();
    }

    void Update()
    {
        if (PhoneBoothUI.isInPhoneBooth) return;

        int iLayer = LayerMask.GetMask("Interactable");

        Vector2 hoverPos = Mouse.current.position.ReadValue();
        RaycastHit2D hoverHit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(hoverPos), Vector2.zero, Mathf.Infinity, iLayer);

        if (hoverHit.collider != null && hoverHit.collider.gameObject == gameObject)
            HotspotLabel.instance.Show("Zoey", transform.position);

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

    IEnumerator HowsItGoing()
    {
        DialogueLabel.curlyLabel.Say("How you holdin' up, Zo?", howsItGoing_Curly1);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("Fine. Why, do I not look fine?", howsItGoing_Zoey1);
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("You look fine.", howsItGoing_Curly2);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("Then why'd you ask.", howsItGoing_Zoey2);
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        zoeyAI.isPaused = false;
    }

    IEnumerator YouDoingOkay()
    {
        DialogueLabel.curlyLabel.Say("You good?", youGood_Curly1);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("Yeah. You?", youGood_Zoey1);
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("Yeah.", youGood_Curly2);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("Cool.", youGood_Zoey2);
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        zoeyAI.isPaused = false;
    }

    IEnumerator ShouldGetMoving()
    {
        DialogueLabel.curlyLabel.Say("We should probably move.", moving_Curly1);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("I've been ready.", moving_Zoey1);
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("No you haven't.", moving_Curly2);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("...No I haven't.", moving_Zoey2);
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        zoeyAI.isPaused = false;
    }

    IEnumerator NeverMind()
    {
        DialogueLabel.curlyLabel.Say("Never mind.", neverMind_Curly);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("Okay.", neverMind_Zoey);
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        zoeyAI.isPaused = false;
    }

    IEnumerator UseZoeyOnZoey()
    {
        DialogueLabel.curlyLabel.Say("Hey Zo...", useZoeyOnZoey_Curly1);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("What would you do if someone asked you to check yourself out?", useZoeyOnZoey_Curly2);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("I'd tell them to mind their business.", useZoeyOnZoey_Zoey);
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("Fair.", useZoeyOnZoey_Curly3);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        zoeyAI.isPaused = false;
    }
}