using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        public string curlyLine;
        public AudioClip curlyClip;   // Voice clip for Curly's line — leave blank for text only
        public string npcLine;
        public AudioClip npcClip;     // Voice clip for the NPC's line — leave blank for text only
    }

    public enum ReturnBehavior { ReturnToParent, ReturnToRoot, EndConversation }

    [System.Serializable]
    public class DialogueTopic
    {
        public string topicLabel;           // What shows in the choice panel
        public bool rotate = true;          // Rotate through lines or repeat the last one
        public bool oneShot = false;        // If true, disappears after being selected once
        public DialogueLine[] lines;        // One or more line pairs

        [Tooltip("Topics that appear after this one is selected. Leave empty to use return behavior.")]
        public DialogueTopic[] childTopics; // Sub-topics that appear after this plays

        public ReturnBehavior returnBehavior = ReturnBehavior.ReturnToParent;

        [HideInInspector] public int currentLine = 0;
        [HideInInspector] public bool hasBeenSelected = false;

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

        public bool HasChildren()
        {
            return childTopics != null && childTopics.Length > 0;
        }
    }

    // Gender options for auto-generating wrong item lines
    public enum NPCGender { Male, Female, Ambiguous }

    [Header("NPC Settings")]
    public string npcName = "NPC";
    public NPCGender gender = NPCGender.Ambiguous;

    [Tooltip("Root level topics — the first choices the player sees")]
    public DialogueTopic[] topics;

    [Header("Goodbye Line")]
    public string curlyGoodbyeLine = "Take it easy.";
    public AudioClip curlyGoodbyeClip;
    public string npcGoodbyeLine = "Yeah. See you around.";
    public AudioClip npcGoodbyeClip;

    [Header("Trade / Quest")]
    [Tooltip("The item name required to complete the trade. Leave blank for no trade.")]
    public string requiredItemName = "";

    [Tooltip("Line the NPC says when the trade completes successfully.")]
    [TextArea] public string tradeCompleteLine = "";
    public AudioClip tradeCompleteClip;

    [Tooltip("Item given to the player as a reward. Leave blank for no reward.")]
    public string rewardItemName = "";
    public Sprite rewardItemSprite;
    public Color rewardItemColor = Color.white;

    [Tooltip("Optional custom wrong item line. Leave blank to use the auto-generated gender line.")]
    [TextArea] public string wrongItemLine = "";
    public AudioClip wrongItemClip;

    // Set to true once the trade is completed so it can't be done again
    [HideInInspector] public bool tradeCompleted = false;

    private bool isTalking = false;

    // Stack of topic lists — lets us go back up the tree
    private Stack<DialogueTopic[]> topicStack = new Stack<DialogueTopic[]>();

    public void StartConversation()
    {
        if (isTalking) return;
        isTalking = true;
        topicStack.Clear();

        // Tell DialogueLabel where this NPC is so animations work
        DialogueLabel.currentNPCTransform = transform;

        ShowTopics(topics);
    }

    void ShowTopics(DialogueTopic[] currentTopics)
    {
        List<string> options = new List<string>();
        List<System.Action> actions = new List<System.Action>();

        foreach (DialogueTopic topic in currentTopics)
        {
            // Skip one-shot topics that have already been selected
            if (topic.oneShot && topic.hasBeenSelected) continue;

            DialogueTopic captured = topic;
            options.Add(topic.topicLabel);
            actions.Add(() => SelectTopic(captured, currentTopics));
        }

        // Add Back option if we're in a sub-branch
        if (topicStack.Count > 0)
        {
            options.Add("Back.");
            actions.Add(() => GoBack());
        }

        // Always add Goodbye at root level
        if (topicStack.Count == 0)
        {
            options.Add("Goodbye.");
            actions.Add(() => StartCoroutine(SayGoodbye()));
        }

        DialogueManager.instance.ShowDialogue(options.ToArray(), actions.ToArray());
    }

    void SelectTopic(DialogueTopic topic, DialogueTopic[] currentTopics)
    {
        topic.hasBeenSelected = true;
        StartCoroutine(PlayTopic(topic, currentTopics));
    }

    IEnumerator PlayTopic(DialogueTopic topic, DialogueTopic[] currentTopics)
    {
        DialogueLine line = topic.GetCurrentLine();
        if (line == null) yield break;

        DialogueManager.instance.dialoguePanel.SetActive(false);
        DialogueManager.isInDialogue = true;

        yield return null;
        yield return new WaitForSeconds(0.2f);

        if (!string.IsNullOrEmpty(line.curlyLine))
        {
            DialogueLabel.curlyLabel.dialogueText.color = new Color(0f, 1f, 1f, 1f);
            DialogueLabel.curlyLabel.Say(line.curlyLine, line.curlyClip);
            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        if (!string.IsNullOrEmpty(line.npcLine))
        {
            DialogueLabel.ShowNPCLine(npcName, line.npcLine, transform.position, line.npcClip);
            yield return new WaitUntil(() => !DialogueLabel.npcLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        topic.Advance();

        // If this topic has children, push current level and show children
        if (topic.HasChildren())
        {
            topicStack.Push(currentTopics);
            ShowTopics(topic.childTopics);
            yield break;
        }

        // No children — use return behavior
        switch (topic.returnBehavior)
        {
            case ReturnBehavior.ReturnToParent:
                if (topicStack.Count > 0)
                    ShowTopics(topicStack.Pop());
                else
                    ShowTopics(topics);
                break;

            case ReturnBehavior.ReturnToRoot:
                topicStack.Clear();
                ShowTopics(topics);
                break;

            case ReturnBehavior.EndConversation:
                StartCoroutine(SayGoodbye());
                break;
        }
    }

    void GoBack()
    {
        if (topicStack.Count > 0)
            ShowTopics(topicStack.Pop());
        else
            ShowTopics(topics);
    }

    IEnumerator SayGoodbye()
    {
        if (!string.IsNullOrEmpty(curlyGoodbyeLine))
        {
            DialogueLabel.curlyLabel.dialogueText.color = new Color(0f, 1f, 1f, 1f);
            DialogueLabel.curlyLabel.Say(curlyGoodbyeLine, curlyGoodbyeClip);
            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        if (!string.IsNullOrEmpty(npcGoodbyeLine))
        {
            DialogueLabel.ShowNPCLine(npcName, npcGoodbyeLine, transform.position, npcGoodbyeClip);
            yield return new WaitUntil(() => !DialogueLabel.npcLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        isTalking = false;
        topicStack.Clear();
        DialogueManager.instance.HideDialogue();
        DialogueLabel.currentNPCTransform = null;
    }

    // Called from Interactable when the player uses an item on this NPC
    public void TryTradeItem(string usedItemName)
    {
        if (string.IsNullOrEmpty(requiredItemName))
        {
            StartCoroutine(SayWrongItem());
            return;
        }

        if (tradeCompleted)
        {
            StartCoroutine(SayWrongItem());
            return;
        }

        if (usedItemName != requiredItemName)
        {
            StartCoroutine(SayWrongItem());
            return;
        }

        StartCoroutine(CompleteTrade());
    }

    IEnumerator CompleteTrade()
    {
        tradeCompleted = true;

        InventoryManager.instance.RemoveItem(requiredItemName);

        if (!string.IsNullOrEmpty(tradeCompleteLine))
        {
            DialogueLabel.ShowNPCLine(npcName, tradeCompleteLine, transform.position, tradeCompleteClip);
            yield return new WaitUntil(() => !DialogueLabel.npcLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        if (!string.IsNullOrEmpty(rewardItemName))
        {
            InventoryManager.instance.AddItem(rewardItemName, rewardItemSprite, rewardItemColor);
            DialogueLabel.curlyLabel.Say("Alright. Thanks.");
            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        }
    }

    IEnumerator SayWrongItem()
    {
        string line = wrongItemLine;

        if (string.IsNullOrEmpty(line))
        {
            switch (gender)
            {
                case NPCGender.Male: line = "He doesn't need that."; break;
                case NPCGender.Female: line = "She doesn't need that."; break;
                case NPCGender.Ambiguous: line = "They don't need that."; break;
            }
        }

        DialogueLabel.curlyLabel.Say(line, wrongItemClip);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
    }

    // Call from code to unlock a root topic
    public void UnlockTopic(int index)
    {
        if (index >= 0 && index < topics.Length)
            topics[index].hasBeenSelected = false;
    }

    // Call from code to lock a root topic
    public void LockTopic(int index)
    {
        if (index >= 0 && index < topics.Length)
            topics[index].hasBeenSelected = true;
    }
}