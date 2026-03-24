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

    // Gender options for auto-generating wrong item lines
    public enum NPCGender { Male, Female, Ambiguous }

    [Header("NPC Settings")]
    public string npcName = "NPC";
    public NPCGender gender = NPCGender.Ambiguous;
    public DialogueTopic[] topics;

    [Header("Goodbye Line")]
    public string curlyGoodbyeLine = "Take it easy.";
    public string npcGoodbyeLine = "Yeah. See you around.";

    [Header("Trade / Quest")]
    [Tooltip("The item name required to complete the trade. Leave blank for no trade.")]
    public string requiredItemName = "";

    [Tooltip("Line the NPC says when the trade completes successfully.")]
    [TextArea] public string tradeCompleteLine = "";

    [Tooltip("Item given to the player as a reward. Leave blank for no reward.")]
    public string rewardItemName = "";
    public Sprite rewardItemSprite;
    public Color rewardItemColor = Color.white;

    [Tooltip("Optional custom wrong item line. Leave blank to use the auto-generated gender line.")]
    [TextArea] public string wrongItemLine = "";

    // Set to true once the trade is completed so it can't be done again
    [HideInInspector] public bool tradeCompleted = false;

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
        List<string> options = new List<string>();
        List<System.Action> actions = new List<System.Action>();

        for (int i = 0; i < topics.Length; i++)
        {
            if (!topicUnlocked[i]) continue;
            int captured = i;
            options.Add(topics[i].topicLabel);
            actions.Add(() => SelectTopic(captured));
        }

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

        DialogueManager.instance.dialoguePanel.SetActive(false);
        DialogueManager.isInDialogue = true;

        yield return null;
        yield return new WaitForSeconds(0.2f);

        if (!string.IsNullOrEmpty(line.curlyLine))
        {
            DialogueLabel.curlyLabel.dialogueText.color = new Color(0f, 1f, 1f, 1f);
            DialogueLabel.curlyLabel.Say(line.curlyLine);
            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        if (!string.IsNullOrEmpty(line.npcLine))
        {
            DialogueLabel.ShowNPCLine(npcName, line.npcLine, transform.position);
            yield return new WaitUntil(() => !DialogueLabel.npcLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        topic.Advance();
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

    // Called from Interactable when the player uses an item on this NPC
    public void TryTradeItem(string usedItemName)
    {
        // No trade set up on this NPC — fall through to generic wrong item line
        if (string.IsNullOrEmpty(requiredItemName))
        {
            StartCoroutine(SayWrongItem());
            return;
        }

        // Trade already done
        if (tradeCompleted)
        {
            StartCoroutine(SayWrongItem());
            return;
        }

        // Wrong item
        if (usedItemName != requiredItemName)
        {
            StartCoroutine(SayWrongItem());
            return;
        }

        // Correct item — do the trade
        StartCoroutine(CompleteTrade());
    }

    IEnumerator CompleteTrade()
    {
        tradeCompleted = true;

        // Consume the required item from inventory
        InventoryManager.instance.RemoveItem(requiredItemName);

        // Play the trade complete line
        if (!string.IsNullOrEmpty(tradeCompleteLine))
        {
            DialogueLabel.ShowNPCLine(npcName, tradeCompleteLine, transform.position);
            yield return new WaitUntil(() => !DialogueLabel.npcLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        // Give reward item if one is set
        if (!string.IsNullOrEmpty(rewardItemName))
        {
            InventoryManager.instance.AddItem(rewardItemName, rewardItemSprite, rewardItemColor);
            DialogueLabel.curlyLabel.Say("Alright. Thanks.");
            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        }
    }

    IEnumerator SayWrongItem()
    {
        // Use custom wrong item line if set, otherwise auto-generate from gender
        string line = wrongItemLine;

        if (string.IsNullOrEmpty(line))
        {
            switch (gender)
            {
                case NPCGender.Male:
                    line = "He doesn't need that.";
                    break;
                case NPCGender.Female:
                    line = "She doesn't need that.";
                    break;
                case NPCGender.Ambiguous:
                    line = "They don't need that.";
                    break;
            }
        }

        DialogueLabel.curlyLabel.Say(line);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
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