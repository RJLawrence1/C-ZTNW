using UnityEngine;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    public static CursorManager instance;

    [Header("Mouse Cursor Textures")]
    public Texture2D cursorDefault;
    public Texture2D cursorLookAt;
    public Texture2D cursorPickUp;
    public Texture2D cursorUseItem;
    public Texture2D cursorTalkTo;
    public Texture2D cursorInteract;
    public Texture2D cursorUseZoey;

    [Header("Controller Cursor (UI Image)")]
    public Image controllerCursorImage;

    [Header("Controller Cursor Sprites")]
    public Sprite spriteDefault;
    public Sprite spriteLookAt;
    public Sprite spritePickUp;
    public Sprite spriteUseItem;
    public Sprite spriteTalkTo;
    public Sprite spriteInteract;
    public Sprite spriteUseZoey;

    private Vector2 hotspot = Vector2.zero;
    private VerbManager.Verb lastVerb = VerbManager.Verb.None;

    // True while the cursor is hovering over an interactable
    private bool isHovering = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ApplyCursor(VerbManager.Verb.None);
    }

    void Update()
    {
        if (VerbManager.instance == null) return;

        // Only update when verb changes AND not hovering — hover handles its own cursor
        if (!isHovering && VerbManager.instance.currentVerb != lastVerb)
        {
            lastVerb = VerbManager.instance.currentVerb;
            ApplyCursor(lastVerb);
        }
    }

    // Called by Interactable when the cursor enters a hotspot
    public void SetHoverCursor()
    {
        isHovering = true;
        lastVerb = VerbManager.instance != null ? VerbManager.instance.currentVerb : VerbManager.Verb.None;
        ApplyCursor(lastVerb);
    }

    // Called by Interactable when the cursor leaves a hotspot
    public void ResetHoverCursor()
    {
        isHovering = false;
        ApplyCursor(VerbManager.Verb.None);
    }

    void ApplyCursor(VerbManager.Verb verb)
    {
        Texture2D mouseTex = cursorDefault;
        Sprite controllerSp = spriteDefault;

        switch (verb)
        {
            case VerbManager.Verb.LookAt:
                mouseTex = cursorLookAt;
                controllerSp = spriteLookAt;
                break;
            case VerbManager.Verb.PickUp:
                mouseTex = cursorPickUp;
                controllerSp = spritePickUp;
                break;
            case VerbManager.Verb.UseItem:
                mouseTex = cursorUseItem;
                controllerSp = spriteUseItem;
                break;
            case VerbManager.Verb.TalkTo:
                mouseTex = cursorTalkTo;
                controllerSp = spriteTalkTo;
                break;
            case VerbManager.Verb.Interact:
                mouseTex = cursorInteract;
                controllerSp = spriteInteract;
                break;
            case VerbManager.Verb.UseZoey:
                mouseTex = cursorUseZoey;
                controllerSp = spriteUseZoey;
                break;
        }

        Cursor.SetCursor(mouseTex, hotspot, CursorMode.Auto);

        if (controllerCursorImage != null && controllerSp != null)
            controllerCursorImage.sprite = controllerSp;
    }

    public void ResetToDefault()
    {
        isHovering = false;
        Cursor.SetCursor(cursorDefault, hotspot, CursorMode.Auto);
        if (controllerCursorImage != null && spriteDefault != null)
            controllerCursorImage.sprite = spriteDefault;
    }
}