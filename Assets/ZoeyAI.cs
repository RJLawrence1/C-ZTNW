using UnityEngine;
using Pathfinding;
using System.Collections.Generic;

public class ZoeyAI : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public float hustleSpeed = 6f;
    public PolygonCollider2D walkableArea;
    public Transform curly;
    public Transform zoeyReturnPoint;
    public float waitTimeMin = 2f;
    public float waitTimeMax = 5f;
    public float returnDistance = 5f;
    public float verbBarWorldY = -4f;
    public float interactRange = 1.5f;
    public bool isPaused = false;

    public float topY = 2f;
    public float bottomY = -2f;
    public float minScale = 0.5f;
    public float maxScale = 1f;

    // Set to true when she arrives at a booth hustle destination
    public bool hasArrived = false;

    private float currentMoveSpeed;
    private bool isHustling = false;
    private bool isReturningToCurly = false;

    private float returnRecalcTimer = 0f;
    private float returnRecalcInterval = 0.5f;

    private Seeker seeker;
    private List<Vector3> path = new List<Vector3>();
    private int pathIndex = 0;
    private bool isMoving = false;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private IInteractable pendingInteractable = null;

    // Animation
    private CharacterAnimator characterAnimator;
    private Vector3 lastPosition;

    void Start()
    {
        seeker = GetComponent<Seeker>();
        currentMoveSpeed = moveSpeed;
        characterAnimator = GetComponent<CharacterAnimator>();
        lastPosition = transform.position;
        StartWait();

        // Set correct scale immediately on spawn
        float t = Mathf.InverseLerp(topY, bottomY, transform.position.y);
        float initialScale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = new Vector3(initialScale, initialScale, 1f);
    }

    void Update()
    {
        if (isPaused) return;
        if (SettingsMenu.isOpen) return;
        if (SceneDoor.movementLocked) return;
        if (PhoneBoothUI.isInPhoneBooth) return;

        HandleMovement();
        HandleScaling();

        if (isWaiting && pendingInteractable == null)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                PickNextDestination();
            }
        }
    }

    public void StopAndStay()
    {
        isMoving = false;
        isWaiting = false;
        isHustling = false;
        isReturningToCurly = false;
        currentMoveSpeed = moveSpeed;
        path.Clear();
        pendingInteractable = null;
        StartWait();
    }

    public void WalkToInteract(IInteractable target)
    {
        pendingInteractable = target;
        isWaiting = false;
        MoveToPosition(target.transform.position);
    }

    // Sends Zoey sprinting to a position — sets hasArrived = true when she gets there (unless returning to Curly)
    public void HustleTo(Vector3 destination)
    {
        hasArrived = false;
        isHustling = true;
        currentMoveSpeed = hustleSpeed;
        isWaiting = false;
        pendingInteractable = null;
        MoveToPosition(destination);
    }

    public void MoveToPosition(Vector3 destination)
    {
        if (!walkableArea.OverlapPoint(destination) || destination.y <= verbBarWorldY)
            destination = GetNearestWalkablePoint(destination);

        destination.z = 0f;
        seeker.StartPath(transform.position, destination, OnPathComplete);
    }

    Vector3 GetNearestWalkablePoint(Vector3 from)
    {
        Bounds bounds = walkableArea.bounds;
        Vector3 best = transform.position;
        float bestDist = float.MaxValue;

        for (int i = 0; i < 30; i++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                0f
            );

            if (walkableArea.OverlapPoint(candidate) && candidate.y > verbBarWorldY)
            {
                float dist = Vector3.Distance(candidate, from);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = candidate;
                }
            }
        }

        return best;
    }

    void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p.vectorPath;
            pathIndex = 0;
            isMoving = true;
        }
    }

    void HandleMovement()
    {
        // If returning to Curly, keep recalculating path so she follows him as he moves
        if (isReturningToCurly)
        {
            returnRecalcTimer += Time.deltaTime;
            if (returnRecalcTimer >= returnRecalcInterval)
            {
                returnRecalcTimer = 0f;
                Transform returnTarget = zoeyReturnPoint != null ? zoeyReturnPoint : curly;
                MoveToPosition(returnTarget.position);
            }
        }

        if (isMoving && path.Count > 0)
        {
            Vector3 target = path[pathIndex];
            target.z = 0f;

            Vector3 separation = Vector3.zero;
            if (curly != null)
            {
                Vector3 toZoey = transform.position - curly.position;
                toZoey.z = 0f;
                float dist = toZoey.magnitude;
            }

            Vector3 moveDir = (target - transform.position).normalized;
            Vector3 combinedMove = (moveDir + separation).normalized;
            Vector3 delta = combinedMove * currentMoveSpeed * Time.deltaTime;

            Vector3 newPos = transform.position + delta;
            newPos.z = 0f;

            if (walkableArea.OverlapPoint(newPos) && newPos.y > verbBarWorldY)
            {
                transform.position = newPos;
            }
            else
            {
                Vector3 slideX = transform.position + new Vector3(delta.x, 0f, 0f);
                slideX.z = 0f;
                Vector3 slideY = transform.position + new Vector3(0f, delta.y, 0f);
                slideY.z = 0f;

                if (walkableArea.OverlapPoint(slideX) && slideX.y > verbBarWorldY)
                    transform.position = slideX;
                else if (walkableArea.OverlapPoint(slideY) && slideY.y > verbBarWorldY)
                    transform.position = slideY;
                else
                {
                    Vector3[] fallbacks = {
                        new Vector3( delta.x,  delta.y, 0f).normalized * currentMoveSpeed * Time.deltaTime,
                        new Vector3(-delta.x,  delta.y, 0f).normalized * currentMoveSpeed * Time.deltaTime,
                        new Vector3( delta.x, -delta.y, 0f).normalized * currentMoveSpeed * Time.deltaTime,
                        new Vector3(-delta.x, -delta.y, 0f).normalized * currentMoveSpeed * Time.deltaTime,
                    };

                    bool moved = false;
                    foreach (Vector3 fallback in fallbacks)
                    {
                        Vector3 fallbackPos = transform.position + fallback;
                        fallbackPos.z = 0f;
                        if (walkableArea.OverlapPoint(fallbackPos) && fallbackPos.y > verbBarWorldY)
                        {
                            transform.position = fallbackPos;
                            moved = true;
                            break;
                        }
                    }

                    if (!moved)
                    {
                        Vector3 center = walkableArea.bounds.center;
                        center.z = 0f;
                        transform.position += (center - transform.position).normalized * 0.02f;
                    }

                    pathIndex++;
                    if (pathIndex >= path.Count)
                    {
                        isMoving = false;
                        OnReachedDestination();
                    }
                    return;
                }
            }

            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                pathIndex++;
                if (pathIndex >= path.Count)
                {
                    isMoving = false;
                    OnReachedDestination();
                }
            }
        }
    }

    void OnReachedDestination()
    {
        if (pendingInteractable != null)
        {
            pendingInteractable.OnInteract();
            pendingInteractable = null;
        }

        if (isHustling)
        {
            isHustling = false;
            currentMoveSpeed = moveSpeed;

            if (isReturningToCurly)
            {
                // Came back to Curly — resume wandering, don't set hasArrived
                isReturningToCurly = false;
                returnRecalcTimer = 0f;
            }
            else
            {
                // Arrived at booth hustle destination
                hasArrived = true;
            }

            StartWait();
            return;
        }

        StartWait();
    }

    void HandleScaling()
    {
        float t = Mathf.InverseLerp(topY, bottomY, transform.position.y);
        float newScale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = new Vector3(newScale, newScale, 1f);

        // Feed movement delta to animator
        if (characterAnimator != null)
        {
            Vector2 delta = (transform.position - lastPosition);
            characterAnimator.SetMoving(delta, isHustling);
        }
        lastPosition = transform.position;
    }

    void PickNextDestination()
    {
        Transform returnTarget = zoeyReturnPoint != null ? zoeyReturnPoint : curly;

        if (Vector3.Distance(transform.position, returnTarget.position) > returnDistance)
        {
            // Hustle back to Curly
            isReturningToCurly = true;
            returnRecalcTimer = 0f;
            HustleTo(returnTarget.position);
            return;
        }

        MoveToPosition(GetRandomWalkablePoint());
    }

    Vector3 GetRandomWalkablePoint()
    {
        Bounds bounds = walkableArea.bounds;

        for (int i = 0; i < 20; i++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                0f
            );

            if (walkableArea.OverlapPoint(candidate) && candidate.y > verbBarWorldY)
                return candidate;
        }

        return transform.position;
    }

    void StartWait()
    {
        isWaiting = true;
        waitTimer = Random.Range(waitTimeMin, waitTimeMax);
    }
}