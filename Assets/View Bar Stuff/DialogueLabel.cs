using UnityEngine;
using TMPro;
using System.Collections;

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
    }

    void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                dialogueText.text = "";
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
    }

    public void SayAtPosition(string line, Vector3 worldPos)
    {
        staticWorldPos = worldPos;
        dialogueText.text = line;
        timer = displayTime;
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