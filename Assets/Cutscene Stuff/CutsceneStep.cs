using UnityEngine;

[System.Serializable]
public class CutsceneStep
{
    public enum StepType { Say, Move, Wait }
    public enum Speaker { Curly, Zoey, NPC }
    public enum MoveTarget { Curly, Zoey, Both }

    [Header("Step Type")]
    public StepType type;

    [Header("Say Settings")]
    // Only used when type is Say
    public Speaker speaker;
    [TextArea] public string line;

    // Only used when speaker is NPC
    public string npcName;
    public Vector3 npcWorldPosition;

    [Header("Move Settings")]
    // Only used when type is Move
    public MoveTarget moveTarget;
    public Vector3 moveDestination;

    [Header("Wait Settings")]
    // Only used when type is Wait
    public float waitTime = 1f;
}
