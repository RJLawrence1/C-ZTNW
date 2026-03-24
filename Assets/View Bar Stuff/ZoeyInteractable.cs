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
                DialogueLabel.curlyLabel.Say("That's Zoey.");
                break;
            case VerbManager.Verb.PickUp:
                DialogueLabel.curlyLabel.Say("Not happening.");
                break;
            case VerbManager.Verb.UseItem:
                DialogueLabel.curlyLabel.Say("That's not what she's for.");
                break;
            case VerbManager.Verb.TalkTo:
                OpenTalkToZoey();
                break;
            case VerbManager.Verb.Interact:
                DialogueLabel.curlyLabel.Say("Last time I tried that she bit me.");
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
        DialogueLabel.curlyLabel.Say("How you holdin' up, Zo?");
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("Fine. Why, do I not look fine?");
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("You look fine.");
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("Then why'd you ask.");
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        zoeyAI.isPaused = false;
    }

    IEnumerator YouDoingOkay()
    {
        DialogueLabel.curlyLabel.Say("You good?");
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("Yeah. You?");
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("Yeah.");
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("Cool.");
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        zoeyAI.isPaused = false;
    }

    IEnumerator ShouldGetMoving()
    {
        DialogueLabel.curlyLabel.Say("We should probably move.");
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("I've been ready.");
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("No you haven't.");
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("...No I haven't.");
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        zoeyAI.isPaused = false;
    }

    IEnumerator NeverMind()
    {
        DialogueLabel.curlyLabel.Say("Never mind.");
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("Okay.");
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        zoeyAI.isPaused = false;
    }

    IEnumerator UseZoeyOnZoey()
    {
        DialogueLabel.curlyLabel.Say("Hey Zo...");
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("What would you do if someone asked you to check yourself out?");
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("I'd tell them to mind their business.");
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("Fair.");
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        zoeyAI.isPaused = false;
    }
}