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
    public TextMeshProUGUI zoeyDialogueText;
    public float displayTime = 3f;

    [Header("Curly Portrait")]
    public Sprite curlySprite;

    private Coroutine curlyClearRoutine;
    private Coroutine npcClearRoutine;
    private Coroutine zoeyClearRoutine;

    void Awake()
    {
        instance = this;
        screenPanel.SetActive(false);
    }

    public void Show(string npcName, Sprite npcSprite, Color npcNameColor)
    {
        if (curlyPortrait != null && curlySprite != null)
            curlyPortrait.sprite = curlySprite;

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
        if (curlyDialogueText != null) curlyDialogueText.text = "";
        if (npcDialogueText != null) npcDialogueText.text = "";
        if (zoeyDialogueText != null) zoeyDialogueText.text = "";
        screenPanel.SetActive(false);
    }

    // Returns true while any line is still showing — use this in WaitUntil checks
    public bool IsDisplaying()
    {
        bool curlyShowing = curlyDialogueText != null && curlyDialogueText.text != "";
        bool npcShowing = npcDialogueText != null && npcDialogueText.text != "";
        bool zoeyShowing = zoeyDialogueText != null && zoeyDialogueText.text != "";
        return curlyShowing || npcShowing || zoeyShowing;
    }

    public void SayCurly(string line, float duration)
    {
        if (curlyDialogueText == null) return;
        curlyDialogueText.text = line;
        if (curlyClearRoutine != null) StopCoroutine(curlyClearRoutine);
        curlyClearRoutine = StartCoroutine(ClearAfter(curlyDialogueText, duration));
    }

    public void SayNPC(string line, float duration)
    {
        if (npcDialogueText == null) return;
        npcDialogueText.text = line;
        if (npcClearRoutine != null) StopCoroutine(npcClearRoutine);
        npcClearRoutine = StartCoroutine(ClearAfter(npcDialogueText, duration));
    }

    public void SayZoey(string line, float duration)
    {
        if (zoeyDialogueText == null) return;
        zoeyDialogueText.text = line;
        if (zoeyClearRoutine != null) StopCoroutine(zoeyClearRoutine);
        zoeyClearRoutine = StartCoroutine(ClearAfter(zoeyDialogueText, duration));
    }

    IEnumerator ClearAfter(TextMeshProUGUI text, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (text != null) text.text = "";
    }
}