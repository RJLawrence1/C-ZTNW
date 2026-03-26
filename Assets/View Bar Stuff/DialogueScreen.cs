using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueScreen : MonoBehaviour
{
    public static DialogueScreen instance;

    [Header("UI References")]
    public GameObject screenPanel;
    public Image curlyPortrait;
    public Image npcPortrait;
    public TextMeshProUGUI npcNameText;

    [Header("Dialogue Text")]
    public TextMeshProUGUI curlyDialogueText;
    public TextMeshProUGUI npcDialogueText;
    public float displayTime = 3f;

    [Header("Curly Portrait")]
    public Sprite curlySprite;

    private Coroutine curlyClearRoutine;
    private Coroutine npcClearRoutine;

    void Awake()
    {
        instance = this;
        screenPanel.SetActive(false);
    }

    // Call this when a conversation starts
    public void Show(string npcName, Sprite npcSprite, Color npcNameColor)
    {
        // Set Curly's portrait
        if (curlyPortrait != null && curlySprite != null)
            curlyPortrait.sprite = curlySprite;

        // Set NPC portrait
        if (npcPortrait != null)
        {
            npcPortrait.sprite = npcSprite;
            npcPortrait.color = npcSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }

        // Set NPC name
        if (npcNameText != null)
        {
            npcNameText.text = npcName;
            npcNameText.color = npcNameColor;
        }

        // Clear any leftover text
        if (curlyDialogueText != null) curlyDialogueText.text = "";
        if (npcDialogueText != null) npcDialogueText.text = "";

        screenPanel.SetActive(true);
    }

    // Call this when conversation ends
    public void Hide()
    {
        if (curlyDialogueText != null) curlyDialogueText.text = "";
        if (npcDialogueText != null) npcDialogueText.text = "";
        screenPanel.SetActive(false);
    }

    // Show a line above Curly's portrait — duration driven by clip or displayTime
    public void SayCurly(string line, float duration)
    {
        if (curlyDialogueText == null) return;
        curlyDialogueText.text = line;
        if (curlyClearRoutine != null) StopCoroutine(curlyClearRoutine);
        curlyClearRoutine = StartCoroutine(ClearAfter(curlyDialogueText, duration));
    }

    // Show a line above the NPC portrait — duration driven by clip or displayTime
    public void SayNPC(string line, float duration)
    {
        if (npcDialogueText == null) return;
        npcDialogueText.text = line;
        if (npcClearRoutine != null) StopCoroutine(npcClearRoutine);
        npcClearRoutine = StartCoroutine(ClearAfter(npcDialogueText, duration));
    }

    IEnumerator ClearAfter(TextMeshProUGUI text, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (text != null) text.text = "";
    }
}