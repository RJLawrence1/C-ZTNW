using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        public string curlyLine;
        public AudioClip curlyClip;
        public DialogueScreen.Emotion curlyEmotion = DialogueScreen.Emotion.Normal;

        public string npcLine;
        public AudioClip npcClip;
        public DialogueScreen.Emotion npcEmotion = DialogueScreen.Emotion.Normal;
    }

    public enum ReturnBehavior { ReturnToParent, ReturnToRoot, EndConversation }

    [System.Serializable]
    public class DialogueTopic
    {
        public string topicLabel;
        public bool rotate = true;
        public bool oneShot = false;
        public DialogueLine[] lines;

        [Tooltip("Topics that appear after this one is selected. Leave empty to use return behavior.")]
        public DialogueTopic[] childTopics;

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

    public enum NPCGender { Male, Female, Ambiguous }

    [Header("NPC Settings")]
    public string npcName = "NPC";
    public NPCGender gender = NPCGender.Ambiguous;
    public Sprite portrait;
    public Color nameColor = Color.red;

    [Header("NPC Portrait Frames")]
    public DialogueScreen.NPCPortraitData portraitData;

    [Tooltip("Root level topics — the first choices the player sees")]
    public DialogueTopic[] topics;

    [Header("Goodbye Line")]
    public string curlyGoodbyeLine = "Take it easy.";
    public AudioClip curlyGoodbyeClip;
    public DialogueScreen.Emotion curlyGoodbyeEmotion = DialogueScreen.Emotion.Normal;
    public string npcGoodbyeLine = "Yeah. See you around.";
    public AudioClip npcGoodbyeClip;
    public DialogueScreen.Emotion npcGoodbyeEmotion = DialogueScreen.Emotion.Normal;

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

    [HideInInspector] public bool tradeCompleted = false;

    private bool isTalking = false;
    private Stack<DialogueTopic[]> topicStack = new Stack<DialogueTopic[]>();

    public void StartConversation()
    {
        if (isTalking) return;
        isTalking = true;
        topicStack.Clear();

        DialogueLabel.currentNPCTransform = transform;

        // Pass this NPC's portrait data so DialogueScreen uses the right frames
        if (DialogueScreen.instance != null)
            DialogueScreen.instance.Show(npcName, portrait, nameColor, portraitData);

        ShowTopics(topics);
    }

    void ShowTopics(DialogueTopic[] currentTopics)
    {
        List<string> options = new List<string>();
        List<System.Action> actions = new List<System.Action>();

        foreach (DialogueTopic topic in currentTopics)
        {
            if (topic.oneShot && topic.hasBeenSelected) continue;

            DialogueTopic captured = topic;
            options.Add(topic.topicLabel);
            actions.Add(() => SelectTopic(captured, currentTopics));
        }

        if (topicStack.Count > 0)
        {
            options.Add("Back.");
            actions.Add(() => GoBack());
        }

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

            if (DialogueScreen.instance != null && DialogueScreen.instance.screenPanel.activeSelf)
            {
                float dur = line.curlyClip != null ? line.curlyClip.length : DialogueScreen.instance.displayTime;
                DialogueScreen.instance.SayCurly(line.curlyLine, dur, line.curlyEmotion);
            }

            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        if (!string.IsNullOrEmpty(line.npcLine))
        {
            DialogueLabel.ShowNPCLine(npcName, line.npcLine, transform.position, line.npcClip);

            if (DialogueScreen.instance != null && DialogueScreen.instance.screenPanel.activeSelf)
            {
                float dur = line.npcClip != null ? line.npcClip.length : DialogueScreen.instance.displayTime;
                DialogueScreen.instance.SayNPC(line.npcLine, dur, line.npcEmotion);
            }

            yield return new WaitUntil(() => !DialogueLabel.npcLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        topic.Advance();

        if (topic.HasChildren())
        {
            topicStack.Push(currentTopics);
            ShowTopics(topic.childTopics);
            yield break;
        }

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

            if (DialogueScreen.instance != null && DialogueScreen.instance.screenPanel.activeSelf)
            {
                float dur = curlyGoodbyeClip != null ? curlyGoodbyeClip.length : DialogueScreen.instance.displayTime;
                DialogueScreen.instance.SayCurly(curlyGoodbyeLine, dur, curlyGoodbyeEmotion);
            }

            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        if (!string.IsNullOrEmpty(npcGoodbyeLine))
        {
            DialogueLabel.ShowNPCLine(npcName, npcGoodbyeLine, transform.position, npcGoodbyeClip);

            if (DialogueScreen.instance != null && DialogueScreen.instance.screenPanel.activeSelf)
            {
                float dur = npcGoodbyeClip != null ? npcGoodbyeClip.length : DialogueScreen.instance.displayTime;
                DialogueScreen.instance.SayNPC(npcGoodbyeLine, dur, npcGoodbyeEmotion);
            }

            yield return new WaitUntil(() => !DialogueLabel.npcLabel.IsDisplaying());
        }

        yield return new WaitForSeconds(0.3f);

        isTalking = false;
        topicStack.Clear();
        DialogueManager.instance.HideDialogue();
        DialogueLabel.currentNPCTransform = null;

        if (DialogueScreen.instance != null)
            DialogueScreen.instance.Hide();
    }

    public void TryTradeItem(string usedItemName)
    {
        if (string.IsNullOrEmpty(requiredItemName)) { StartCoroutine(SayWrongItem()); return; }
        if (tradeCompleted) { StartCoroutine(SayWrongItem()); return; }
        if (usedItemName != requiredItemName) { StartCoroutine(SayWrongItem()); return; }
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

    public void UnlockTopic(int index)
    {
        if (index >= 0 && index < topics.Length)
            topics[index].hasBeenSelected = false;
    }

    public void LockTopic(int index)
    {
        if (index >= 0 && index < topics.Length)
            topics[index].hasBeenSelected = true;
    }

    public void SaveState()
    {
        string key = "NPC_" + npcName;
        PlayerPrefs.SetInt(key + "_tradeCompleted", tradeCompleted ? 1 : 0);
        SaveTopics(topics, key + "_root");
    }

    void SaveTopics(DialogueTopic[] topicList, string prefix)
    {
        if (topicList == null) return;
        PlayerPrefs.SetInt(prefix + "_count", topicList.Length);
        for (int i = 0; i < topicList.Length; i++)
        {
            string t = prefix + "_" + i;
            PlayerPrefs.SetInt(t + "_selected", topicList[i].hasBeenSelected ? 1 : 0);
            PlayerPrefs.SetInt(t + "_line", topicList[i].currentLine);
            if (topicList[i].HasChildren())
                SaveTopics(topicList[i].childTopics, t + "_children");
        }
    }

    public void LoadState()
    {
        string key = "NPC_" + npcName;
        if (!PlayerPrefs.HasKey(key + "_tradeCompleted")) return;
        tradeCompleted = PlayerPrefs.GetInt(key + "_tradeCompleted") == 1;
        LoadTopics(topics, key + "_root");
    }

    void LoadTopics(DialogueTopic[] topicList, string prefix)
    {
        if (topicList == null) return;
        int count = PlayerPrefs.GetInt(prefix + "_count", 0);
        for (int i = 0; i < Mathf.Min(topicList.Length, count); i++)
        {
            string t = prefix + "_" + i;
            topicList[i].hasBeenSelected = PlayerPrefs.GetInt(t + "_selected", 0) == 1;
            topicList[i].currentLine = PlayerPrefs.GetInt(t + "_line", 0);
            if (topicList[i].HasChildren())
                LoadTopics(topicList[i].childTopics, t + "_children");
        }
    }
}