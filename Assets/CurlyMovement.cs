using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Pathfinding;
using System.Collections.Generic;

public class CurlyMovement : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float sprintSpeed = 6f;
    private float currentSpeed;
    public bool isSprinting = false;

    public float topY = 2f;
    public float bottomY = -2f;
    public float minScale = 0.5f;
    public float maxScale = 1f;
    public float verbBarWorldY = -4f;
    public float interactRange = 0.1f;

    public PolygonCollider2D walkableArea;

    private Seeker seeker;
    private Rigidbody2D rb;
    private List<Vector3> path = new List<Vector3>();
    private int pathIndex = 0;
    private bool isMoving = false;
    private IInteractable pendingInteractable = null;

    void Start()
    {
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Mouse.current.position.ReadValue();
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject.GetComponentInParent<Canvas>() != null)
                return true;
        }
        return false;
    }

    // Returns true if either character is currently speaking a line
    private bool IsDialoguePlaying()
    {
        bool curlyTalking = DialogueLabel.curlyLabel != null && DialogueLabel.curlyLabel.IsDisplaying();
        bool zoeyTalking = DialogueLabel.zoeyLabel != null && DialogueLabel.zoeyLabel.IsDisplaying();
        return curlyTalking || zoeyTalking;
    }

    public void MoveToPosition(Vector3 destination)
    {
        destination.z = 0f;
        seeker.StartPath(transform.position, destination, OnPathComplete);
    }

    public void WalkToInteract(IInteractable target)
    {
        pendingInteractable = target;
        MoveToPosition(target.transform.position);
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

    public void CancelMovement()
    {
        path.Clear();
        isMoving = false;
        pendingInteractable = null;
    }

    void Update()
    {
        if (SettingsMenu.isOpen) return;
        if (InventoryManager.instance.isOpen) return;
        if (PhoneBoothUI.isInPhoneBooth) return;
        if (DialogueManager.isInDialogue) return;
        if (IsDialoguePlaying()) return;
        if (SceneDoor.movementLocked) return;

        isSprinting = Keyboard.current.leftShiftKey.isPressed ||
                      Gamepad.current != null && Gamepad.current.leftTrigger.isPressed;

        currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if (Gamepad.current != null && Gamepad.current.leftStick != null && Gamepad.current.leftStick.ReadValue().magnitude > 0.2f)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            Vector3 delta = new Vector3(stick.x, stick.y, 0f) * currentSpeed * Time.deltaTime;
            Vector3 newPos = transform.position + delta;
            newPos.z = 0f;

            Collider2D wallHit = Physics2D.OverlapCircle(newPos, 0.2f, LayerMask.GetMask("Obstacle"));

            if (walkableArea.OverlapPoint(newPos) && newPos.y > verbBarWorldY && wallHit == null)
            {
                transform.position = newPos;
            }
            else
            {
                Vector3 slideX = transform.position + new Vector3(delta.x, 0f, 0f);
                slideX.z = 0f;
                Vector3 slideY = transform.position + new Vector3(0f, delta.y, 0f);
                slideY.z = 0f;

                Collider2D hitX = Physics2D.OverlapCircle(slideX, 0.2f, LayerMask.GetMask("Obstacle"));
                Collider2D hitY = Physics2D.OverlapCircle(slideY, 0.2f, LayerMask.GetMask("Obstacle"));

                if (walkableArea.OverlapPoint(slideX) && slideX.y > verbBarWorldY && hitX == null)
                    transform.position = slideX;
                else if (walkableArea.OverlapPoint(slideY) && slideY.y > verbBarWorldY && hitY == null)
                    transform.position = slideY;
            }

            isMoving = false;
        }
        else
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUI())
            {
                Vector2 mousePos2D = Mouse.current.position.ReadValue();
                int interactableLayer = LayerMask.GetMask("Interactable");
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(new Vector3(mousePos2D.x, mousePos2D.y, Camera.main.nearClipPlane)), Vector2.zero, Mathf.Infinity, interactableLayer);

                if (hit.collider != null && (hit.collider.GetComponent<Interactable>() != null || hit.collider.GetComponent<ZoeyInteractable>() != null || hit.collider.GetComponent<PhoneBooth>() != null))
                    return;

                Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos2D.x, mousePos2D.y, Camera.main.nearClipPlane));
                mousePos.z = 0f;

                if (walkableArea.OverlapPoint(mousePos) && mousePos.y > verbBarWorldY)
                {
                    pendingInteractable = null;
                    MoveToPosition(mousePos);
                }
            }

            if (isMoving && path.Count > 0)
            {
                Vector3 target = path[pathIndex];
                target.z = 0f;

                Vector3 delta = (target - transform.position).normalized * currentSpeed * Time.deltaTime;
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
                            new Vector3( delta.x,  delta.y, 0f).normalized * currentSpeed * Time.deltaTime,
                            new Vector3(-delta.x,  delta.y, 0f).normalized * currentSpeed * Time.deltaTime,
                            new Vector3( delta.x, -delta.y, 0f).normalized * currentSpeed * Time.deltaTime,
                            new Vector3(-delta.x, -delta.y, 0f).normalized * currentSpeed * Time.deltaTime,
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
                    }
                }
            }
        }

        float t = Mathf.InverseLerp(topY, bottomY, transform.position.y);
        float newScale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = new Vector3(newScale, newScale, 1f);
    }
}