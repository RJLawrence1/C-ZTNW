using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager instance;

    // True while any cutscene is playing — checked by other systems to block input
    public static bool isPlaying = false;

    void Awake()
    {
        instance = this;
    }

    // Call this from anywhere to play a cutscene
    public void PlayCutscene(Cutscene cutscene)
    {
        if (isPlaying) return;
        StartCoroutine(RunCutscene(cutscene));
    }

    IEnumerator RunCutscene(Cutscene cutscene)
    {
        isPlaying = true;

        // Lock all input
        LockInput(true);

        foreach (CutsceneStep step in cutscene.steps)
        {
            yield return StartCoroutine(RunStep(step));
        }

        // Unlock all input
        LockInput(false);

        isPlaying = false;
    }

    IEnumerator RunStep(CutsceneStep step)
    {
        switch (step.type)
        {
            case CutsceneStep.StepType.Say:
                yield return StartCoroutine(SayStep(step));
                break;

            case CutsceneStep.StepType.Move:
                yield return StartCoroutine(MoveStep(step));
                break;

            case CutsceneStep.StepType.Wait:
                yield return new WaitForSeconds(step.waitTime);
                break;
        }
    }

    IEnumerator SayStep(CutsceneStep step)
    {
        // Disable skip for all cutscene lines
        if (DialogueLabel.curlyLabel != null) DialogueLabel.curlyLabel.skipEnabled = false;
        if (DialogueLabel.zoeyLabel != null) DialogueLabel.zoeyLabel.skipEnabled = false;

        switch (step.speaker)
        {
            case CutsceneStep.Speaker.Curly:
                DialogueLabel.curlyLabel.Say(step.line);
                yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
                break;

            case CutsceneStep.Speaker.Zoey:
                DialogueLabel.zoeyLabel.Say(step.line);
                yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
                break;

            case CutsceneStep.Speaker.NPC:
                // Uses the NPC label at a fixed world position set on the step
                DialogueLabel.ShowNPCLine(step.npcName, step.line, step.npcWorldPosition);
                yield return new WaitUntil(() => !DialogueLabel.npcLabel.IsDisplaying());
                break;
        }

        // Re-enable skip after the line
        if (DialogueLabel.curlyLabel != null) DialogueLabel.curlyLabel.skipEnabled = true;
        if (DialogueLabel.zoeyLabel != null) DialogueLabel.zoeyLabel.skipEnabled = true;

        // Small pause between lines
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator MoveStep(CutsceneStep step)
    {
        CurlyMovement curly = FindObjectOfType<CurlyMovement>();
        ZoeyAI zoey = FindObjectOfType<ZoeyAI>();

        switch (step.moveTarget)
        {
            case CutsceneStep.MoveTarget.Curly:
                if (curly != null)
                {
                    curly.WalkToPosition(step.moveDestination);
                    // Wait until Curly is close enough to the destination
                    yield return new WaitUntil(() =>
                        Vector3.Distance(curly.transform.position, step.moveDestination) < 0.2f);
                }
                break;

            case CutsceneStep.MoveTarget.Zoey:
                if (zoey != null)
                {
                    zoey.HustleTo(step.moveDestination);
                    yield return new WaitUntil(() => zoey.hasArrived);
                }
                break;

            case CutsceneStep.MoveTarget.Both:
                if (curly != null) curly.WalkToPosition(step.moveDestination);
                if (zoey != null) zoey.HustleTo(step.moveDestination);
                yield return new WaitUntil(() =>
                    (curly == null || Vector3.Distance(curly.transform.position, step.moveDestination) < 0.2f) &&
                    (zoey == null || zoey.hasArrived));
                break;
        }
    }

    void LockInput(bool locked)
    {
        // Lock Curly movement
        CurlyMovement curly = FindObjectOfType<CurlyMovement>();
        if (curly != null) curly.inputLocked = locked;

        // Lock Zoey AI
        ZoeyAI zoey = FindObjectOfType<ZoeyAI>();
        if (zoey != null) zoey.isPaused = locked;

        // Lock verb bar, inventory, phone booth
        if (locked)
        {
            DialogueManager.isInDialogue = true;
        }
        else
        {
            DialogueManager.isInDialogue = false;
        }
    }
}
