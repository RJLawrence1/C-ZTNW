using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class DialogueLabel : MonoBehaviour
{
    public static DialogueLabel curlyLabel;
    public static DialogueLabel zoeyLabel;

    // Shared NPC label — reused for all NPC lines
    public static DialogueLabel npcLabel;

    public TextMeshPro dialogueText;
    public float displayTime = 3f;
    public Color dialogueColor = Color.cyan;
    public bool isZoey = false;
    public bool isNPC = false;
    public Vector3 offset = new Vector3(0f, 0.5f, 0f);

    private float timer = 0f;
    private Transform followTarget;
    private Vector3 staticWorldPos; // For NPC labels that follow a world position

    // Tracks which lines have been seen before — persists across the whole session
    private static HashSet<string> seenLines = new HashSet<string>();

    // True while the current line is locked (first time playing — no skipping allowed)
    private bool isLocked = false;

    // Set to false before Say() to prevent skipping entirely — e.g. during pickup sequences
    public bool skipEnabled = true;

    // Set this before a conversation so dialogue labels know where the NPC is
    public static Transform currentNPCTransform = null;

    // Cached animator references
    private static CharacterAnimator curlyAnimator = null;
    private static CharacterAnimator zoeyAnimator = null;

    void Awake()
    {
        if (isNPC)
        {
            npcLabel = this;
            Debug.Log("Registered as NPC: " + gameObject.name);
        }
        else if (isZoey)
        {
            zoeyLabel = this;
            Debug.Log("Registered as Zoey: " + gameObject.name);
        }
        else
        {
            curlyLabel = this;
            Debug.Log("Registered as Curly: " + gameObject.name);
        }
    }

    void Start()
    {
        if (dialogueText == null)
            dialogueText = GetComponentInChildren<TextMeshPro>();

        dialogueText.color = dialogueColor;
        dialogueText.text = "";
        followTarget = isNPC ? null : transform.parent;

        // Cache animator references
        if (!isNPC && !isZoey && followTarget != null)
            curlyAnimator = followTarget.GetComponent<CharacterAnimator>();
        else if (isZoey && followTarget != null)
            zoeyAnimator = followTarget.GetComponent<CharacterAnimator>();
    }

    void Update()
    {
        if (timer > 0f)
        {
            // Check for skip input — only allowed if skip is enabled and line has been seen before
            if (skipEnabled && !isLocked && IsSkipPressed())
            {
                Skip();
                return;
            }

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                isLocked = false;
                dialogueText.text = "";
                OnLineEnd();
            }
        }

        transform.rotation = Camera.main.transform.rotation;

        if (followTarget != null)
        {
            Vector3 desiredPos = followTarget.position + offset;
            transform.position = ClampToScreen(desiredPos);
        }
        else if (isNPC && timer > 0f)
        {
            transform.position = ClampToScreen(staticWorldPos + offset);
        }
    }

    // Called when a line finishes — return characters to idle
    void OnLineEnd()
    {
        if (!isNPC && !isZoey && curlyAnimator != null)
            curlyAnimator.SetIdle();
        else if (isZoey && zoeyAnimator != null)
            zoeyAnimator.SetIdle();
        else if (isNPC)
        {
            // NPC animator is on the currentNPCTransform
            if (currentNPCTransform != null)
            {
                CharacterAnimator npcAnim = currentNPCTransform.GetComponent<CharacterAnimator>();
                if (npcAnim != null) npcAnim.SetIdle();
            }
        }
    }

    // Returns true if the player pressed the skip button this frame
    bool IsSkipPressed()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            return true;
        return false;
    }

    // Immediately ends the current line
    void Skip()
    {
        timer = 0f;
        isLocked = false;
        dialogueText.text = "";
        OnLineEnd();
    }

    Vector3 ClampToScreen(Vector3 worldPos)
    {
        float camHeight = Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;
        Vector3 camPos = Camera.main.transform.position;

        float padding = 0.5f;

        worldPos.x = Mathf.Clamp(worldPos.x, camPos.x - camWidth + padding, camPos.x + camWidth - padding);
        worldPos.y = Mathf.Clamp(worldPos.y, camPos.y - camHeight + padding, camPos.y + camHeight - padding);

        return worldPos;
    }

    public void Say(string line)
    {
        dialogueText.text = line;
        timer = displayTime;

        // First time this line plays — lock it
        if (!seenLines.Contains(line))
        {
            isLocked = true;
            seenLines.Add(line);
        }
        else
        {
            isLocked = false;
        }

        // Trigger talk animation
        if (!isNPC && !isZoey && curlyAnimator != null && currentNPCTransform != null)
        {
            // Curly talks — face NPC, NPC faces Curly
            curlyAnimator.SetTalking(currentNPCTransform.position);
            CharacterAnimator npcAnim = currentNPCTransform.GetComponent<CharacterAnimator>();
            if (npcAnim != null) npcAnim.FaceToward(followTarget.position);
        }
        else if (isZoey && zoeyAnimator != null && currentNPCTransform != null)
        {
            // Zoey talks — face NPC, NPC faces Zoey
            zoeyAnimator.SetTalking(currentNPCTransform.position);
            CharacterAnimator npcAnim = currentNPCTransform.GetComponent<CharacterAnimator>();
            if (npcAnim != null) npcAnim.FaceToward(followTarget.position);
        }
    }

    public void SayAtPosition(string line, Vector3 worldPos)
    {
        staticWorldPos = worldPos;
        dialogueText.text = line;
        timer = displayTime;

        // First time this line plays — lock it
        if (!seenLines.Contains(line))
        {
            isLocked = true;
            seenLines.Add(line);
        }
        else
        {
            isLocked = false;
        }

        // NPC talks — NPC faces Curly, Curly faces NPC
        if (isNPC && currentNPCTransform != null)
        {
            CharacterAnimator npcAnim = currentNPCTransform.GetComponent<CharacterAnimator>();
            if (npcAnim != null && curlyAnimator != null)
            {
                npcAnim.SetTalking(curlyAnimator.transform.position);
                curlyAnimator.FaceToward(currentNPCTransform.position);
            }
        }
    }

    // Static helper — shows an NPC line above a world position
    public static void ShowNPCLine(string npcName, string line, Vector3 worldPos)
    {
        if (npcLabel == null)
        {
            Debug.LogWarning("NPCLabel not found — make sure a DialogueLabel with Is NPC checked exists in the scene.");
            return;
        }
        npcLabel.dialogueText.color = npcLabel.dialogueColor;
        npcLabel.SayAtPosition(line, worldPos);
    }

    public bool IsDisplaying()
    {
        return timer > 0f;
    }
}