using UnityEngine;
using UnityEngine.UI;

// Attach this script to any persistent GameObject in your scene (e.g. your GameManager or UI root).
// Assign all the sprite slots in the Inspector, and drag in your controller cursor Image too.

public class CursorManager : MonoBehaviour
{
    public static CursorManager instance;

    [Header("Mouse Cursor Textures")]
    // These must be Texture2D, not Sprite — import each as Texture2D in Unity,
    // OR see the note below about converting from Sprite automatically.
    // Easiest: in Unity, set Texture Type to "Cursor" for each cursor image.
    public Texture2D cursorDefault;
    public Texture2D cursorLookAt;
    public Texture2D cursorPickUp;
    public Texture2D cursorUseItem;
    public Texture2D cursorTalkTo;
    public Texture2D cursorInteract;
    public Texture2D cursorUseZoey;

    [Header("Controller Cursor (UI Image)")]
    // Drag in the Image component that acts as your on-screen controller cursor.
    public Image controllerCursorImage;

    [Header("Controller Cursor Sprites")]
    // These are regular Sprites — same artwork, but used for the UI Image.
    public Sprite spriteDefault;
    public Sprite spriteLookAt;
    public Sprite spritePickUp;
    public Sprite spriteUseItem;
    public Sprite spriteTalkTo;
    public Sprite spriteInteract;
    public Sprite spriteUseZoey;

    // The hotspot is the "active point" of the cursor — e.g. the tip of an arrow.
    // Vector2.zero means the top-left corner of the texture is the click point,
    // which is correct for a standard arrow. Adjust if your cursor art needs it.
    private Vector2 hotspot = Vector2.zero;

    private VerbManager.Verb lastVerb = VerbManager.Verb.None;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // Set the default cursor right away when the scene loads
        ApplyCursor(VerbManager.Verb.None);
    }

    void Update()
    {
        if (VerbManager.instance == null) return;

        // Only update when the verb actually changes — no need to set it every frame
        if (VerbManager.instance.currentVerb != lastVerb)
        {
            lastVerb = VerbManager.instance.currentVerb;
            ApplyCursor(lastVerb);
        }
    }

    void ApplyCursor(VerbManager.Verb verb)
    {
        Texture2D mouseTex = cursorDefault;
        Sprite controllerSp = spriteDefault;

        // Match each verb to its cursor pair
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
                // VerbManager.Verb.None falls through to the defaults set above
        }

        // Apply to hardware mouse cursor
        Cursor.SetCursor(mouseTex, hotspot, CursorMode.Auto);

        // Apply to controller cursor UI Image
        if (controllerCursorImage != null && controllerSp != null)
            controllerCursorImage.sprite = controllerSp;
    }

    // Call this from anywhere if you need to force a cursor reset
    // e.g. when opening the phone booth UI
    public void ResetToDefault()
    {
        Cursor.SetCursor(cursorDefault, hotspot, CursorMode.Auto);
        if (controllerCursorImage != null && spriteDefault != null)
            controllerCursorImage.sprite = spriteDefault;
    }
}