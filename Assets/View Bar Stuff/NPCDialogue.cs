using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        public string curlyLine;   // What Curly says
        public string npcLine;     // What the NPC responds
    }

    [System.Serializable]
    public class DialogueTopic
    {
        public string topicLabel;          // What shows in the choice panel e.g. "How's it going?"
        public bool rotate = true;         // Rotate through lines or just repeat the last one
        public DialogueLine[] lines;       // One or more line pairs for this topic

        [HideInInspector] public int currentLine = 0;

        public DialogueLine GetCurrentLine()
        {
            if (lines == null || lines.Length == 0) return null;
            int index = Mathf.Min(currentLine, lines.Length - 1);
            return lines[index];
        }

        public void Advance()
        {
            if (rotate && currentLine < lines.Length - 1)
                currentLine++;
        }
    }

    [Header("NPC Settings")]
    public string npcName = "NPC";
    public DialogueTopic[] topics;

    [Header("Goodbye Line")]
    public string curlyGoodbyeLine = "See you around.";
    public string npcGoodbyeLine = "Take care.";

    // Which topics are currently visible — lets you unlock topics mid-game
    public bool[] topicUnlocked;

    private bool isTalking = false;

    void Start()
    {
        // Default all topics to unlocked
        if (topicUnlocked == null || topicUnlocked.Length != topics.Length)
        {
            topicUnlocked = new bool[topics.Length];
            for (int i = 0; i < topicUnlocked.Length; i++)
                topicUnlocked[i] = true;
        }
    }

    public void StartConversation()
    {
        if (isTalking) return;
        isTalking = true;
        ShowTopics();
    }

    void ShowTopics()
    {
        // Build list of active topics
        List<string> options = new List<string>();
        List<System.Action> actions = new List<System.Action>();

        for (int i = 0; i < topics.Length; i++)
        {
            if (!topicUnlocked[i]) continue;
            int captured = i;
            options.Add(topics[i].topicLabel);
            actions.Add(() => SelectTopic(captured));
        }

        // Always add Goodbye last
        options.Add("Goodbye.");
        actions.Add(() => StartCoroutine(SayGoodbye()));

        DialogueManager.instance.ShowDialogue(options.ToArray(), actions.ToArray());
    }

    void SelectTopic(int index)
    {
        StartCoroutine(PlayTopic(index));
    }

    IEnumerator PlayTopic(int index)
    {
        DialogueTopic topic = topics[index];
        DialogueLine line = topic.GetCurrentLine();
        if (line == null) yield break;

        // Hide the dialogue panel visually but keep isInDialogue true while lines play
        DialogueManager.instance.dialoguePanel.SetActive(false);
        DialogueManager.isInDialogue = true;

        // Wait a frame
        yield return null;
        yield return new WaitForSeconds(0.2f);

        // Curly speaks first
        if (!string.IsNullOrEmpty(line.curlyLine))
        {
            Debug.Log("About to say: " + line.curlyLine + " | isInDialogue: " + DialogueManager.isInDialogue + " | curlyLabel null: " + (DialogueLabel.curlyLabel == null));
            DialogueLabel.curlyLabel.dialogueText.color = new Color(0f, 1f, 1f, 1f);
            DialogueLabel.curlyLabel.Say(line.curlyLine);
            Debug.Log("Curly text: " + DialogueLabel.curlyLabel.dialogueText.text + " | displaying: " + DialogueLabel.curlyLabel.IsDisplaying());
            Debug.Log("curlyLabel GameObject: " + DialogueLabel.curlyLabel.gameObject.name);
            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        }

        // Small pause between lines
        yield return new WaitForSeconds(0.3f);

        // NPC responds
        if (!string.IsNullOrEmpty(line.npcLine))
        {
            DialogueLabel.ShowNPCLine(npcName, line.npcLine, transform.position);
            yield return new WaitUntil(() => !DialogueLabel.npcLabel.IsDisplaying());
        }

        // Small pause before showing topics again
        yield return new WaitForSeconds(0.3f);

        // Advance to next line in rotation
        topic.Advance();

        // Show topics again
        ShowTopics();
    }

    IEnumerator SayGoodbye()
    {
        if (!string.IsNullOrEmpty(curlyGoodbyeLine))
        {
            DialogueLabel.curlyLabel.dialogueText.color = new Color(0f, 1f, 1f, 1f);
            DialogueLabel.curlyLabel.Say(curlyGoodbyeLine);
            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        if (!string.IsNullOrEmpty(npcGoodbyeLine))
        {
            DialogueLabel.ShowNPCLine(npcName, npcGoodbyeLine, transform.position);
            yield return new WaitUntil(() => !DialogueLabel.npcLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        isTalking = false;
        DialogueManager.instance.HideDialogue();
    }

    // Call this from code to unlock a topic mid-game
    public void UnlockTopic(int index)
    {
        if (index >= 0 && index < topicUnlocked.Length)
            topicUnlocked[index] = true;
    }

    // Call this to lock a topic — e.g. after it's been resolved
    public void LockTopic(int index)
    {
        if (index >= 0 && index < topicUnlocked.Length)
            topicUnlocked[index] = false;
    }
}