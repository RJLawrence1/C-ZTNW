using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ZoeyInteractable : MonoBehaviour, IInteractable
{
    private ZoeyAI zoeyAI;

    // Cached reference — avoids FindObjectOfType on every click
    private CurlyMovement curly;

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
                DialogueLabel.curlyLabel.Say("It's Zoey.");
                break;
            case VerbManager.Verb.PickUp:
                DialogueLabel.curlyLabel.Say("I'm not even gonna try picking her up.");
                break;
            case VerbManager.Verb.UseItem:
                DialogueLabel.curlyLabel.Say("Don't think I can use her.");
                break;
            case VerbManager.Verb.TalkTo:
                OpenTalkToZoey();
                break;
            case VerbManager.Verb.Interact:
                DialogueLabel.curlyLabel.Say("Last time I did that she bit me.");
                break;
            case VerbManager.Verb.UseZoey:
                StartCoroutine(UseZoeyOnZoey());
                break;
        }
    }

    void OpenTalkToZoey()
    {
        string[] options = {
            "How's it going?",
            "You doing okay?",
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
        DialogueLabel.curlyLabel.Say("How's life, Zo?");
        yield return new WaitForSeconds(3f);
        DialogueLabel.zoeyLabel.Say("Could be worse. Could be you.");
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say("...Fair enough.");
        yield return new WaitForSeconds(3f);
        zoeyAI.isPaused = false;
    }

    IEnumerator YouDoingOkay()
    {
        DialogueLabel.curlyLabel.Say("You doin' alright?");
        yield return new WaitForSeconds(3f);
        DialogueLabel.zoeyLabel.Say("I was until you asked.");
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say("Noted.");
        yield return new WaitForSeconds(3f);
        zoeyAI.isPaused = false;
    }

    IEnumerator ShouldGetMoving()
    {
        DialogueLabel.curlyLabel.Say("We should probably get moving.");
        yield return new WaitForSeconds(3f);
        DialogueLabel.zoeyLabel.Say("I was ready ten minutes ago.");
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say("No you weren't.");
        yield return new WaitForSeconds(3f);
        DialogueLabel.zoeyLabel.Say("...No I wasn't.");
        yield return new WaitForSeconds(3f);
        zoeyAI.isPaused = false;
    }

    IEnumerator NeverMind()
    {
        DialogueLabel.curlyLabel.Say("Never mind.");
        yield return new WaitForSeconds(3f);
        DialogueLabel.zoeyLabel.Say("Typical.");
        yield return new WaitForSeconds(3f);
        zoeyAI.isPaused = false;
    }

    IEnumerator UseZoeyOnZoey()
    {
        DialogueLabel.curlyLabel.Say("Hey Zo... while you're at it,");
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say("maybe you could check ME out too?");
        yield return new WaitForSeconds(3f);
        DialogueLabel.zoeyLabel.Say("Not now, maybe later.");
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say("Alright then, later it is.");
        yield return new WaitForSeconds(3f);
        zoeyAI.isPaused = false;
    }
}