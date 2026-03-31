using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueScreen : MonoBehaviour
{
    public static DialogueScreen instance;

    public enum Emotion
    {
        Normal,
        Excited,
        Sad,
        Angry,
        Curious,
        Scared,
        Disgusted,
        Smug,
        Confused,
        Surprised,
        Thinking
    }

    // Holds all emotion frames for one NPC — assigned on NPCDialogue, passed into Show()
    [System.Serializable]
    public class NPCPortraitData
    {
        public Sprite[] normalFrames;
        public Sprite[] excitedFrames;
        public Sprite[] sadFrames;
        public Sprite[] angryFrames;
        public Sprite[] curiousFrames;
        public Sprite[] scaredFrames;
        public Sprite[] disgustedFrames;
        public Sprite[] smugFrames;
        public Sprite[] confusedFrames;
        public Sprite[] surprisedFrames;
        public Sprite[] thinkingFrames;
    }

    [Header("UI References")]
    public GameObject screenPanel;
    public Image curlyPortrait;
    public Image npcPortrait;
    public TextMeshProUGUI npcNameText;

    [Header("Dialogue Text")]
    public TextMeshProUGUI curlyDialogueText;
    public TextMeshProUGUI npcDialogueText;
    public TextMeshProUGUI zoeyDialogueText;
    public float displayTime = 3f;

    [Header("Curly Portraits")]
    public Sprite curlyIdleSprite;
    public Sprite[] curlyNormalFrames;
    public Sprite[] curlyExcitedFrames;
    public Sprite[] curlySadFrames;
    public Sprite[] curlyAngryFrames;
    public Sprite[] curlyCuriousFrames;
    public Sprite[] curlyScaredFrames;
    public Sprite[] curlyDisgustedFrames;
    public Sprite[] curlySmugFrames;
    public Sprite[] curlyConfusedFrames;
    public Sprite[] curlySurprisedFrames;
    public Sprite[] curlyThinkingFrames;

    [Header("Zoey Portraits")]
    public Sprite zoeyIdleSprite;
    public Sprite[] zoeyNormalFrames;
    public Sprite[] zoeyExcitedFrames;
    public Sprite[] zoeySadFrames;
    public Sprite[] zoeyAngryFrames;
    public Sprite[] zoeyCuriousFrames;
    public Sprite[] zoeyScaredFrames;
    public Sprite[] zoeyDisgustedFrames;
    public Sprite[] zoeySmugFrames;
    public Sprite[] zoeyConfusedFrames;
    public Sprite[] zoeySurprisedFrames;
    public Sprite[] zoeyThinkingFrames;

    [Header("Talking Animation")]
    public float talkingFrameRate = 0.1f;

    // Runtime — set when Show() is called
    private Sprite npcIdleSprite;
    private NPCPortraitData currentNPCPortraitData;

    private Coroutine curlyClearRoutine;
    private Coroutine npcClearRoutine;
    private Coroutine zoeyClearRoutine;

    private Coroutine curlyTalkRoutine;
    private Coroutine npcTalkRoutine;
    private Coroutine zoeyTalkRoutine;

    void Awake()
    {
        instance = this;
        screenPanel.SetActive(false);
    }

    // Called by NPCDialogue.StartConversation() — passes in that NPC's portrait data
    public void Show(string npcName, Sprite npcSprite, Color npcNameColor, NPCPortraitData portraitData = null)
    {
        npcIdleSprite = npcSprite;
        currentNPCPortraitData = portraitData;

        if (curlyPortrait != null && curlyIdleSprite != null)
            curlyPortrait.sprite = curlyIdleSprite;

        if (npcPortrait != null)
        {
            npcPortrait.sprite = npcSprite;
            npcPortrait.color = npcSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }

        if (npcNameText != null)
        {
            npcNameText.text = npcName;
            npcNameText.color = npcNameColor;
        }

        if (curlyDialogueText != null) curlyDialogueText.text = "";
        if (npcDialogueText != null) npcDialogueText.text = "";
        if (zoeyDialogueText != null) zoeyDialogueText.text = "";

        screenPanel.SetActive(true);
    }

    public void Hide()
    {
        StopAllTalkingAnimations();

        if (curlyDialogueText != null) curlyDialogueText.text = "";
        if (npcDialogueText != null) npcDialogueText.text = "";
        if (zoeyDialogueText != null) zoeyDialogueText.text = "";

        screenPanel.SetActive(false);
    }

    public bool IsDisplaying()
    {
        bool curlyShowing = curlyDialogueText != null && curlyDialogueText.text != "";
        bool npcShowing = npcDialogueText != null && npcDialogueText.text != "";
        bool zoeyShowing = zoeyDialogueText != null && zoeyDialogueText.text != "";
        return curlyShowing || npcShowing || zoeyShowing;
    }

    public void SayCurly(string line, float duration, Emotion emotion = Emotion.Normal)
    {
        if (curlyDialogueText == null) return;
        curlyDialogueText.text = line;

        if (curlyClearRoutine != null) StopCoroutine(curlyClearRoutine);
        curlyClearRoutine = StartCoroutine(ClearAfter(curlyDialogueText, duration));

        if (curlyTalkRoutine != null) StopCoroutine(curlyTalkRoutine);
        curlyTalkRoutine = StartCoroutine(TalkingAnimation(curlyPortrait, GetCurlyFrames(emotion), curlyIdleSprite, duration));
    }

    public void SayNPC(string line, float duration, Emotion emotion = Emotion.Normal)
    {
        if (npcDialogueText == null) return;
        npcDialogueText.text = line;

        if (npcClearRoutine != null) StopCoroutine(npcClearRoutine);
        npcClearRoutine = StartCoroutine(ClearAfter(npcDialogueText, duration));

        if (npcTalkRoutine != null) StopCoroutine(npcTalkRoutine);
        npcTalkRoutine = StartCoroutine(TalkingAnimation(npcPortrait, GetNPCFrames(emotion), npcIdleSprite, duration));
    }

    public void SayZoey(string line, float duration, Emotion emotion = Emotion.Normal)
    {
        if (zoeyDialogueText == null) return;
        zoeyDialogueText.text = line;

        if (zoeyClearRoutine != null) StopCoroutine(zoeyClearRoutine);
        zoeyClearRoutine = StartCoroutine(ClearAfter(zoeyDialogueText, duration));

        if (zoeyTalkRoutine != null) StopCoroutine(zoeyTalkRoutine);
        zoeyTalkRoutine = StartCoroutine(TalkingAnimation(npcPortrait, GetZoeyFrames(emotion), zoeyIdleSprite, duration));
    }

    Sprite[] GetCurlyFrames(Emotion emotion)
    {
        switch (emotion)
        {
            case Emotion.Excited: return curlyExcitedFrames;
            case Emotion.Sad: return curlySadFrames;
            case Emotion.Angry: return curlyAngryFrames;
            case Emotion.Curious: return curlyCuriousFrames;
            case Emotion.Scared: return curlyScaredFrames;
            case Emotion.Disgusted: return curlyDisgustedFrames;
            case Emotion.Smug: return curlySmugFrames;
            case Emotion.Confused: return curlyConfusedFrames;
            case Emotion.Surprised: return curlySurprisedFrames;
            case Emotion.Thinking: return curlyThinkingFrames;
            default: return curlyNormalFrames;
        }
    }

    Sprite[] GetNPCFrames(Emotion emotion)
    {
        // Uses the current NPC's portrait data — falls back gracefully if none assigned
        if (currentNPCPortraitData == null) return null;

        switch (emotion)
        {
            case Emotion.Excited: return currentNPCPortraitData.excitedFrames;
            case Emotion.Sad: return currentNPCPortraitData.sadFrames;
            case Emotion.Angry: return currentNPCPortraitData.angryFrames;
            case Emotion.Curious: return currentNPCPortraitData.curiousFrames;
            case Emotion.Scared: return currentNPCPortraitData.scaredFrames;
            case Emotion.Disgusted: return currentNPCPortraitData.disgustedFrames;
            case Emotion.Smug: return currentNPCPortraitData.smugFrames;
            case Emotion.Confused: return currentNPCPortraitData.confusedFrames;
            case Emotion.Surprised: return currentNPCPortraitData.surprisedFrames;
            case Emotion.Thinking: return currentNPCPortraitData.thinkingFrames;
            default: return currentNPCPortraitData.normalFrames;
        }
    }

    Sprite[] GetZoeyFrames(Emotion emotion)
    {
        switch (emotion)
        {
            case Emotion.Excited: return zoeyExcitedFrames;
            case Emotion.Sad: return zoeySadFrames;
            case Emotion.Angry: return zoeyAngryFrames;
            case Emotion.Curious: return zoeyCuriousFrames;
            case Emotion.Scared: return zoeyScaredFrames;
            case Emotion.Disgusted: return zoeyDisgustedFrames;
            case Emotion.Smug: return zoeySmugFrames;
            case Emotion.Confused: return zoeyConfusedFrames;
            case Emotion.Surprised: return zoeySurprisedFrames;
            case Emotion.Thinking: return zoeyThinkingFrames;
            default: return zoeyNormalFrames;
        }
    }

    IEnumerator TalkingAnimation(Image portrait, Sprite[] frames, Sprite idleSprite, float duration)
    {
        if (frames == null || frames.Length == 0)
            yield break;

        float elapsed = 0f;
        int frameIndex = 0;

        while (elapsed < duration)
        {
            if (portrait != null && frames[frameIndex] != null)
                portrait.sprite = frames[frameIndex];

            frameIndex = (frameIndex + 1) % frames.Length;
            yield return new WaitForSeconds(talkingFrameRate);
            elapsed += talkingFrameRate;
        }

        if (portrait != null && idleSprite != null)
            portrait.sprite = idleSprite;
    }

    void StopAllTalkingAnimations()
    {
        if (curlyTalkRoutine != null) { StopCoroutine(curlyTalkRoutine); curlyTalkRoutine = null; }
        if (npcTalkRoutine != null) { StopCoroutine(npcTalkRoutine); npcTalkRoutine = null; }
        if (zoeyTalkRoutine != null) { StopCoroutine(zoeyTalkRoutine); zoeyTalkRoutine = null; }

        if (curlyPortrait != null && curlyIdleSprite != null) curlyPortrait.sprite = curlyIdleSprite;
        if (npcPortrait != null && npcIdleSprite != null) npcPortrait.sprite = npcIdleSprite;
    }

    IEnumerator ClearAfter(TextMeshProUGUI text, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (text != null) text.text = "";
    }
}