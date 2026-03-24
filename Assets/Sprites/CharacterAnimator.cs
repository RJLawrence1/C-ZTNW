using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    public enum AnimState { Idle, Walk, Talk }

    [Header("Spritesheets")]
    [Tooltip("8 directions x 6 frames — row order: Down, DownRight, Right, UpRight, Up, UpLeft, Left, DownLeft")]
    public Sprite[] walkFrames;   // 48 frames total

    [Tooltip("8 directions x 2 frames")]
    public Sprite[] idleFrames;   // 16 frames total

    [Tooltip("8 directions x 4 frames")]
    public Sprite[] talkFrames;   // 32 frames total

    [Header("Animation Settings")]
    public float walkFPS = 8f;
    public float idleFPS = 4f;
    public float talkFPS = 6f;

    // Frame counts per direction
    private const int WALK_FRAMES = 6;
    private const int IDLE_FRAMES = 2;
    private const int TALK_FRAMES = 4;
    private const int DIRECTIONS = 8;

    private SpriteRenderer spriteRenderer;
    private AnimState currentState = AnimState.Idle;
    private int currentDirection = 0; // 0=Down, 1=DownRight, 2=Right, 3=UpRight, 4=Up, 5=UpLeft, 6=Left, 7=DownLeft
    private float frameTimer = 0f;
    private int currentFrame = 0;

    // Used to face toward a target during talk/idle
    private Transform faceTarget = null;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        frameTimer += Time.deltaTime;
        float fps = GetCurrentFPS();

        if (frameTimer >= 1f / fps)
        {
            frameTimer = 0f;
            AdvanceFrame();
        }

        ApplySprite();
    }

    // Called every frame by CurlyMovement or ZoeyAI with the current movement delta
    public void SetMoving(Vector2 moveDelta)
    {
        if (moveDelta.magnitude > 0.01f)
        {
            SetState(AnimState.Walk);
            currentDirection = VectorToDirection(moveDelta);
        }
        else
        {
            if (currentState == AnimState.Walk)
                SetState(AnimState.Idle);
        }
    }

    // Call this to start talk animation facing a world position
    public void SetTalking(Vector3 targetWorldPos)
    {
        Vector2 dir = (targetWorldPos - transform.position);
        currentDirection = VectorToDirection(dir);
        SetState(AnimState.Talk);
    }

    // Call this to stop talking and return to idle
    public void SetIdle()
    {
        SetState(AnimState.Idle);
        faceTarget = null;
    }

    // Call this to face toward a world position without changing state
    public void FaceToward(Vector3 worldPos)
    {
        Vector2 dir = (worldPos - transform.position);
        currentDirection = VectorToDirection(dir);
    }

    void SetState(AnimState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        currentFrame = 0;
        frameTimer = 0f;
    }

    void AdvanceFrame()
    {
        int frameCount = GetFrameCount();
        currentFrame = (currentFrame + 1) % frameCount;
    }

    void ApplySprite()
    {
        Sprite[] frames = GetCurrentFrames();
        if (frames == null || frames.Length == 0) return;

        int frameCount = GetFrameCount();
        int index = currentDirection * frameCount + currentFrame;

        if (index >= 0 && index < frames.Length)
            spriteRenderer.sprite = frames[index];
    }

    Sprite[] GetCurrentFrames()
    {
        switch (currentState)
        {
            case AnimState.Walk: return walkFrames;
            case AnimState.Idle: return idleFrames;
            case AnimState.Talk: return talkFrames;
            default: return idleFrames;
        }
    }

    int GetFrameCount()
    {
        switch (currentState)
        {
            case AnimState.Walk: return WALK_FRAMES;
            case AnimState.Idle: return IDLE_FRAMES;
            case AnimState.Talk: return TALK_FRAMES;
            default: return IDLE_FRAMES;
        }
    }

    float GetCurrentFPS()
    {
        switch (currentState)
        {
            case AnimState.Walk: return walkFPS;
            case AnimState.Idle: return idleFPS;
            case AnimState.Talk: return talkFPS;
            default: return idleFPS;
        }
    }

    // Converts a movement vector to one of 8 direction indices
    // 0=Down, 1=DownRight, 2=Right, 3=UpRight, 4=Up, 5=UpLeft, 6=Left, 7=DownLeft
    int VectorToDirection(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Normalize to 0-360
        if (angle < 0) angle += 360f;

        // 8 slices of 45 degrees each, offset so Down is centered at 270
        // Right=0, UpRight=45, Up=90, UpLeft=135, Left=180, DownLeft=225, Down=270, DownRight=315

        if (angle >= 337.5f || angle < 22.5f) return 2; // Right
        if (angle >= 22.5f && angle < 67.5f) return 3; // UpRight
        if (angle >= 67.5f && angle < 112.5f) return 4; // Up
        if (angle >= 112.5f && angle < 157.5f) return 5; // UpLeft
        if (angle >= 157.5f && angle < 202.5f) return 6; // Left
        if (angle >= 202.5f && angle < 247.5f) return 7; // DownLeft
        if (angle >= 247.5f && angle < 292.5f) return 0; // Down
        if (angle >= 292.5f && angle < 337.5f) return 1; // DownRight

        return 0;
    }
}