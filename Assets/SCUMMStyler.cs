using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach to your SettingsMenu GameObject.
// Drag in all UI elements in the Inspector.
// Styles everything to match classic 80s SCUMM (Maniac Mansion / Zak McKracken era).

public class SCUMMStyler : MonoBehaviour
{
    [Header("Buttons to Style")]
    public Button[] buttons;

    [Header("Sliders to Style")]
    public Slider[] sliders;

    [Header("Toggles to Style")]
    public Toggle[] toggles;

    [Header("Panel Backgrounds")]
    public Image[] panels;

    // ── EGA / 80s SCUMM Palette ─────────────────────────────────
    // Pure black background
    static readonly Color Black = new Color(0f, 0f, 0f, 1f);
    // Classic EGA dark blue (that Maniac Mansion panel color)
    static readonly Color EGADarkBlue = new Color(0f, 0f, 0.67f, 1f);
    // EGA bright cyan — the highlight/border color
    static readonly Color EGACyan = new Color(0f, 1f, 1f, 1f);
    // EGA bright green — matches your existing text
    static readonly Color EGAGreen = new Color(0f, 0.67f, 0f, 1f);
    // EGA bright white
    static readonly Color EGAWhite = new Color(1f, 1f, 1f, 1f);
    // EGA dark grey for pressed state
    static readonly Color EGADarkGrey = new Color(0.33f, 0.33f, 0.33f, 1f);
    // ────────────────────────────────────────────────────────────

    void Awake()
    {
        ApplyStyle();
    }

    public void ApplyStyle()
    {
        foreach (Button btn in buttons)
            StyleButton(btn);

        foreach (Slider slider in sliders)
            StyleSlider(slider);

        foreach (Toggle toggle in toggles)
            StyleToggle(toggle);

        foreach (Image panel in panels)
            StylePanel(panel);
    }

    // ── Button ──────────────────────────────────────────────────
    // Solid dark blue box, cyan hard border, green text
    void StyleButton(Button btn)
    {
        if (btn == null) return;

        Image bg = btn.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = EGADarkBlue;
            bg.sprite = null;
            bg.type = Image.Type.Simple;
        }

        // Hard pixel outline — no softness
        Outline outline = btn.GetComponent<Outline>() ?? btn.gameObject.AddComponent<Outline>();
        outline.effectColor = EGACyan;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = false;

        // Color transition — instant snap, no smooth fade
        ColorBlock cb = btn.colors;
        cb.normalColor = EGADarkBlue;
        cb.highlightedColor = EGACyan;
        cb.pressedColor = Black;
        cb.selectedColor = EGACyan;
        cb.disabledColor = EGADarkGrey;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0f;
        btn.colors = cb;

        // Text
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.color = EGAGreen;
            tmp.fontStyle = FontStyles.Bold;
        }
    }

    // ── Slider ──────────────────────────────────────────────────
    // Dark track, cyan fill, square white handle
    void StyleSlider(Slider slider)
    {
        if (slider == null) return;

        // Background track
        Transform bgT = slider.transform.Find("Background");
        if (bgT != null)
        {
            Image bgImg = bgT.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = Black;
                bgImg.sprite = null;

                Outline o = bgImg.GetComponent<Outline>() ?? bgImg.gameObject.AddComponent<Outline>();
                o.effectColor = EGACyan;
                o.effectDistance = new Vector2(2f, -2f);
                o.useGraphicAlpha = false;
            }

            RectTransform bgRT = bgT.GetComponent<RectTransform>();
            if (bgRT != null)
                bgRT.sizeDelta = new Vector2(bgRT.sizeDelta.x, 8f);
        }

        // Fill
        if (slider.fillRect != null)
        {
            Image fillImg = slider.fillRect.GetComponent<Image>();
            if (fillImg != null)
            {
                fillImg.color = EGACyan;
                fillImg.sprite = null;
            }
        }

        // Handle — chunky square
        if (slider.handleRect != null)
        {
            Image handleImg = slider.handleRect.GetComponent<Image>();
            if (handleImg != null)
            {
                handleImg.color = EGAWhite;
                handleImg.sprite = null;

                RectTransform rt = slider.handleRect;
                rt.sizeDelta = new Vector2(16f, 24f);

                Outline ho = handleImg.GetComponent<Outline>() ?? handleImg.gameObject.AddComponent<Outline>();
                ho.effectColor = EGACyan;
                ho.effectDistance = new Vector2(2f, -2f);
                ho.useGraphicAlpha = false;
            }
        }

        // Label
        TextMeshProUGUI tmp = slider.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.color = EGAGreen;
    }

    // ── Toggle ──────────────────────────────────────────────────
    // Classic [ ] / [X] checkbox
    void StyleToggle(Toggle toggle)
    {
        if (toggle == null) return;

        Image bgImg = toggle.targetGraphic as Image;
        if (bgImg != null)
        {
            bgImg.color = Black;
            bgImg.sprite = null;

            RectTransform rt = bgImg.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(20f, 20f);

            Outline o = bgImg.GetComponent<Outline>() ?? bgImg.gameObject.AddComponent<Outline>();
            o.effectColor = EGACyan;
            o.effectDistance = new Vector2(2f, -2f);
            o.useGraphicAlpha = false;
        }

        // Checkmark — solid cyan block when on
        if (toggle.graphic != null)
        {
            Image checkImg = toggle.graphic as Image;
            if (checkImg != null)
            {
                checkImg.color = EGACyan;
                checkImg.sprite = null;
            }
        }

        // Label
        TextMeshProUGUI tmp = toggle.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.color = EGAGreen;
    }

    // ── Panel ───────────────────────────────────────────────────
    // Black bg, cyan border
    void StylePanel(Image panel)
    {
        if (panel == null) return;

        panel.color = Black;
        panel.sprite = null;

        Outline o = panel.GetComponent<Outline>() ?? panel.gameObject.AddComponent<Outline>();
        o.effectColor = EGACyan;
        o.effectDistance = new Vector2(4f, -4f);
        o.useGraphicAlpha = false;
    }
}