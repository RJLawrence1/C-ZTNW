using UnityEngine;
using Pathfinding;
using System.Collections.Generic;

public class ZoeyAI : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public PolygonCollider2D walkableArea;
    public Transform curly;
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

    private Seeker seeker;
    private List<Vector3> path = new List<Vector3>();
    private int pathIndex = 0;
    private bool isMoving = false;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private IInteractable pendingInteractable = null;

    void Start()
    {
        seeker = GetComponent<Seeker>();
        StartWait();
    }

    void Update()
    {
        // Only paused when Curly is actively talking to Zoey (set by ZoeyInteractable)
        if (isPaused) return;
        if (SceneDoor.movementLocked) return;

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
        if (isMoving && path.Count > 0)
        {
            Vector3 target = path[pathIndex];
            target.z = 0f;

            // --- Separation from Curly ---
            Vector3 separation = Vector3.zero;
            if (curly != null)
            {
                Vector3 toZoey = transform.position - curly.position;
                toZoey.z = 0f;
                float dist = toZoey.magnitude;
            }

            // Combine path following with separation steering
            Vector3 moveDir = (target - transform.position).normalized;
            Vector3 combinedMove = (moveDir + separation).normalized;
            Vector3 delta = combinedMove * moveSpeed * Time.deltaTime;

            // --- Wall sliding ---
            // Try full move first, then fall back to X-only or Y-only
            // so she glides along walls instead of coming to a dead stop
            Vector3 newPos = transform.position + delta;
            newPos.z = 0f;

            if (walkableArea.OverlapPoint(newPos) && newPos.y > verbBarWorldY)
            {
                // Full move is fine
                transform.position = newPos;
            }
            else
            {
                // Try sliding along X axis only
                Vector3 slideX = transform.position + new Vector3(delta.x, 0f, 0f);
                slideX.z = 0f;

                // Try sliding along Y axis only
                Vector3 slideY = transform.position + new Vector3(0f, delta.y, 0f);
                slideY.z = 0f;

                if (walkableArea.OverlapPoint(slideX) && slideX.y > verbBarWorldY)
                    transform.position = slideX;
                else if (walkableArea.OverlapPoint(slideY) && slideY.y > verbBarWorldY)
                    transform.position = slideY;
                else
                {
                    // Try diagonal directions as a last resort for corners
                    Vector3[] fallbacks = {
                        new Vector3( delta.x,  delta.y, 0f).normalized * moveSpeed * Time.deltaTime,
                        new Vector3(-delta.x,  delta.y, 0f).normalized * moveSpeed * Time.deltaTime,
                        new Vector3( delta.x, -delta.y, 0f).normalized * moveSpeed * Time.deltaTime,
                        new Vector3(-delta.x, -delta.y, 0f).normalized * moveSpeed * Time.deltaTime,
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

                    // If truly stuck, nudge toward walkable area center then skip waypoint
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
                        if (pendingInteractable != null)
                        {
                            pendingInteractable.OnInteract();
                            pendingInteractable = null;
                        }
                        StartWait();
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
                    if (pendingInteractable != null)
                    {
                        pendingInteractable.OnInteract();
                        pendingInteractable = null;
                    }
                    StartWait();
                }
            }
        }
    }

    void HandleScaling()
    {
        float t = Mathf.InverseLerp(topY, bottomY, transform.position.y);
        float newScale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = new Vector3(newScale, newScale, 1f);
    }

    void PickNextDestination()
    {
        if (Vector3.Distance(transform.position, curly.position) > returnDistance)
        {
            MoveToPosition(curly.position);
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