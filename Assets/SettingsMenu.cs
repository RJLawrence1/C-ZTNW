using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu instance;
    public static bool isOpen = false;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Panel")]
    public GameObject settingsPanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button saveButton;
    public Button loadButton;
    public Button optionsButton;
    public Button quitButton;
    public TextMeshProUGUI versionText;

    [Header("Options Panel")]
    public GameObject optionsPanel;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider voiceSlider;
    public Toggle familyFriendlyToggle;
    public Button optionsBackButton;

    [Header("Quit Confirmation Panel")]
    public GameObject quitPanel;
    public TextMeshProUGUI quitMessage;
    public Button quitYesButton;
    public Button quitNoButton;

    [Header("Audio")]
    public AudioSource uiAudioSource;       // AudioSource routed to SFX mixer group
    public AudioSource voiceAudioSource;    // AudioSource routed to Voice mixer group
    public AudioClip bloopClip;             // Plays when SFX slider moves
    public AudioClip quitVoiceLine;         // Curly's "you should save" voice line

    private float quitLineCooldown = 0f;
    private const float QuitLineCooldownTime = 5f;
    private float bloopCooldown = 0f;
    private const float BloopCooldownTime = 0.5f;

    // Controller navigation
    private int selectedIndex = 0;
    private float stickCooldown = 0f;
    private const float StickCooldownTime = 0.2f;

    private enum MenuPanel { Main, Options, Quit }
    private MenuPanel activePanel = MenuPanel.Main;

    // Game settings — static so any script can read them
    public static float musicVolume = 1f;
    public static float sfxVolume = 1f;
    public static float voiceVolume = 1f;
    public static bool familyFriendly = false;

    void Awake()
    {
        instance = this;
        settingsPanel.SetActive(false);
        optionsPanel.SetActive(false);
        quitPanel.SetActive(false);
    }

    void Start()
    {
        if (versionText != null)
            versionText.text = "Curly & Zoey: TNW  v0.1";

        // Load saved settings
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        voiceVolume = PlayerPrefs.GetFloat("VoiceVolume", 1f);
        familyFriendly = PlayerPrefs.GetInt("FamilyFriendly", 0) == 1;

        // Apply to sliders and toggle
        if (musicSlider != null) musicSlider.value = musicVolume;
        if (sfxSlider != null) sfxSlider.value = sfxVolume;
        if (voiceSlider != null) voiceSlider.value = voiceVolume;
        if (familyFriendlyToggle != null) familyFriendlyToggle.isOn = familyFriendly;

        // Apply saved volumes to mixer
        ApplyVolumes();

        // Hook up buttons
        resumeButton.onClick.AddListener(CloseSettings);
        saveButton.onClick.AddListener(OnSave);
        loadButton.onClick.AddListener(OnLoad);
        optionsButton.onClick.AddListener(OpenOptions);
        quitButton.onClick.AddListener(OpenQuitConfirmation);
        optionsBackButton.onClick.AddListener(CloseOptions);
        quitYesButton.onClick.AddListener(QuitGame);
        quitNoButton.onClick.AddListener(CloseQuitConfirmation);

        // Hook up sliders
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        if (voiceSlider != null) voiceSlider.onValueChanged.AddListener(OnVoiceChanged);
        if (familyFriendlyToggle != null) familyFriendlyToggle.onValueChanged.AddListener(OnFamilyFriendlyChanged);
    }

    void Update()
    {
        if (quitLineCooldown > 0f)
            quitLineCooldown -= Time.unscaledDeltaTime;
        if (bloopCooldown > 0f)
            bloopCooldown -= Time.unscaledDeltaTime;

        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            if (isOpen) CloseSettings();
            else OpenSettings();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame && isOpen)
            CloseSettings();

        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
        {
            if (isOpen) CloseSettings();
            else OpenSettings();
        }

        // Controller navigation inside menu
        if (isOpen && Gamepad.current != null)
            HandleControllerNavigation(Gamepad.current);
    }

    void HandleControllerNavigation(Gamepad gamepad)
    {
        if (stickCooldown > 0f)
        {
            stickCooldown -= Time.unscaledDeltaTime;
            return;
        }

        float vertical = gamepad.leftStick.y.ReadValue();
        if (Mathf.Abs(vertical) < 0.3f) vertical = 0f;
        if (gamepad.dpad.up.isPressed) vertical = 1f;
        if (gamepad.dpad.down.isPressed) vertical = -1f;

        float horizontal = gamepad.leftStick.x.ReadValue();
        if (Mathf.Abs(horizontal) < 0.3f) horizontal = 0f;
        if (gamepad.dpad.left.isPressed) horizontal = -1f;
        if (gamepad.dpad.right.isPressed) horizontal = 1f;

        int itemCount = GetCurrentPanelItemCount();

        if (vertical > 0f)
        {
            selectedIndex = (selectedIndex - 1 + itemCount) % itemCount;
            stickCooldown = StickCooldownTime;
            RefreshHighlights();
        }
        else if (vertical < 0f)
        {
            selectedIndex = (selectedIndex + 1) % itemCount;
            stickCooldown = StickCooldownTime;
            RefreshHighlights();
        }

        // Left/right adjusts sliders when in options panel
        if (activePanel == MenuPanel.Options && horizontal != 0f)
        {
            AdjustCurrentSlider(horizontal);
            stickCooldown = StickCooldownTime;
        }

        // A button — confirm/press current item
        if (gamepad.aButton.wasPressedThisFrame)
            ActivateCurrentItem();

        // B button — back
        if (gamepad.bButton.wasPressedThisFrame)
        {
            if (activePanel == MenuPanel.Options) CloseOptions();
            else if (activePanel == MenuPanel.Quit) CloseQuitConfirmation();
            else CloseSettings();
        }
    }

    int GetCurrentPanelItemCount()
    {
        switch (activePanel)
        {
            case MenuPanel.Main: return 5; // Resume, Save, Load, Options, Quit
            case MenuPanel.Options: return 4; // Music, SFX, Voice, Uncensored, Back
            case MenuPanel.Quit: return 2; // Yes, No
        }
        return 0;
    }

    void ActivateCurrentItem()
    {
        switch (activePanel)
        {
            case MenuPanel.Main:
                switch (selectedIndex)
                {
                    case 0: CloseSettings(); break;
                    case 1: OnSave(); break;
                    case 2: OnLoad(); break;
                    case 3: OpenOptions(); break;
                    case 4: OpenQuitConfirmation(); break;
                }
                break;

            case MenuPanel.Options:
                switch (selectedIndex)
                {
                    case 0: AdjustCurrentSlider(0.1f); break; // Music — nudge right
                    case 1: AdjustCurrentSlider(0.1f); break; // SFX
                    case 2: AdjustCurrentSlider(0.1f); break; // Voice
                    case 3: // Toggle
                        if (familyFriendlyToggle != null)
                            familyFriendlyToggle.isOn = !familyFriendlyToggle.isOn;
                        break;
                    case 4: CloseOptions(); break;
                }
                break;

            case MenuPanel.Quit:
                switch (selectedIndex)
                {
                    case 0: QuitGame(); break;
                    case 1: CloseQuitConfirmation(); break;
                }
                break;
        }
    }

    void AdjustCurrentSlider(float direction)
    {
        if (activePanel != MenuPanel.Options) return;
        float step = 0.05f;
        switch (selectedIndex)
        {
            case 0: if (musicSlider != null) musicSlider.value = Mathf.Clamp01(musicSlider.value + direction * step); break;
            case 1: if (sfxSlider != null) sfxSlider.value = Mathf.Clamp01(sfxSlider.value + direction * step); break;
            case 2: if (voiceSlider != null) voiceSlider.value = Mathf.Clamp01(voiceSlider.value + direction * step); break;
        }
    }

    void RefreshHighlights()
    {
        // Main panel buttons
        SetButtonHighlight(resumeButton, activePanel == MenuPanel.Main && selectedIndex == 0);
        SetButtonHighlight(saveButton, activePanel == MenuPanel.Main && selectedIndex == 1);
        SetButtonHighlight(loadButton, activePanel == MenuPanel.Main && selectedIndex == 2);
        SetButtonHighlight(optionsButton, activePanel == MenuPanel.Main && selectedIndex == 3);
        SetButtonHighlight(quitButton, activePanel == MenuPanel.Main && selectedIndex == 4);

        // Options panel
        SetSliderHighlight(musicSlider, activePanel == MenuPanel.Options && selectedIndex == 0);
        SetSliderHighlight(sfxSlider, activePanel == MenuPanel.Options && selectedIndex == 1);
        SetSliderHighlight(voiceSlider, activePanel == MenuPanel.Options && selectedIndex == 2);
        SetToggleHighlight(familyFriendlyToggle, activePanel == MenuPanel.Options && selectedIndex == 3);
        SetButtonHighlight(optionsBackButton, activePanel == MenuPanel.Options && selectedIndex == 4);

        // Quit panel
        SetButtonHighlight(quitYesButton, activePanel == MenuPanel.Quit && selectedIndex == 0);
        SetButtonHighlight(quitNoButton, activePanel == MenuPanel.Quit && selectedIndex == 1);
    }

    void SetButtonHighlight(Button btn, bool highlighted)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = highlighted ? new Color(0f, 0.4f, 0.4f, 1f) : new Color(0f, 0f, 0.67f, 1f);
        Outline outline = btn.GetComponent<Outline>();
        if (outline != null) outline.effectColor = highlighted ? Color.white : new Color(0f, 1f, 1f, 1f);
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.color = highlighted ? Color.white : new Color(0f, 0.67f, 0f, 1f);
    }

    void SetSliderHighlight(Slider slider, bool highlighted)
    {
        if (slider == null) return;
        Transform bgT = slider.transform.Find("Background");
        if (bgT != null)
        {
            Outline o = bgT.GetComponent<Outline>();
            if (o != null) o.effectColor = highlighted ? Color.white : new Color(0f, 1f, 1f, 1f);
        }
        TextMeshProUGUI tmp = slider.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.color = highlighted ? Color.white : new Color(0f, 0.67f, 0f, 1f);
    }

    void SetToggleHighlight(Toggle toggle, bool highlighted)
    {
        if (toggle == null) return;
        Image bgImg = toggle.targetGraphic as Image;
        if (bgImg != null)
        {
            Outline o = bgImg.GetComponent<Outline>();
            if (o != null) o.effectColor = highlighted ? Color.white : new Color(0f, 1f, 1f, 1f);
        }
        TextMeshProUGUI tmp = toggle.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.color = highlighted ? Color.white : new Color(0f, 0.67f, 0f, 1f);
    }

    public void OpenSettings()
    {
        isOpen = true;
        settingsPanel.SetActive(true);
        optionsPanel.SetActive(false);
        quitPanel.SetActive(false);
        DialogueManager.isInDialogue = true;
        activePanel = MenuPanel.Main;
        selectedIndex = 0;
        RefreshHighlights();
    }

    public void CloseSettings()
    {
        isOpen = false;
        settingsPanel.SetActive(false);
        optionsPanel.SetActive(false);
        quitPanel.SetActive(false);
        DialogueManager.isInDialogue = false;
    }

    void OnSave()
    {
        CloseSettings();
        SaveManager.instance.SaveGame();
    }

    void OnLoad()
    {
        CloseSettings();
        SaveManager.instance.LoadGame();
    }

    void OpenOptions()
    {
        settingsPanel.SetActive(false);
        optionsPanel.SetActive(true);
        activePanel = MenuPanel.Options;
        selectedIndex = 0;
        RefreshHighlights();
    }

    void CloseOptions()
    {
        optionsPanel.SetActive(false);
        settingsPanel.SetActive(true);
        activePanel = MenuPanel.Main;
        selectedIndex = 3; // Land back on Options button
        RefreshHighlights();
    }

    void OpenQuitConfirmation()
    {
        settingsPanel.SetActive(false);
        quitPanel.SetActive(true);
        if (quitMessage != null)
            quitMessage.text = "Hey! Make sure you save your game before you go!";

        if (quitVoiceLine != null && voiceAudioSource != null && quitLineCooldown <= 0f)
        {
            voiceAudioSource.PlayOneShot(quitVoiceLine);
            quitLineCooldown = QuitLineCooldownTime;
        }

        activePanel = MenuPanel.Quit;
        selectedIndex = 1; // Default to No for safety
        RefreshHighlights();
    }

    void CloseQuitConfirmation()
    {
        quitPanel.SetActive(false);
        settingsPanel.SetActive(true);
        activePanel = MenuPanel.Main;
        selectedIndex = 4; // Land back on Quit button
        RefreshHighlights();
    }

    void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void ApplyVolumes()
    {
        if (audioMixer == null) return;
        audioMixer.SetFloat("MusicVolume", musicVolume > 0 ? Mathf.Log10(musicVolume) * 20 : -80f);
        audioMixer.SetFloat("SFXVolume", sfxVolume > 0 ? Mathf.Log10(sfxVolume) * 20 : -80f);
        audioMixer.SetFloat("VoiceVolume", voiceVolume > 0 ? Mathf.Log10(voiceVolume) * 20 : -80f);
    }

    void OnMusicChanged(float value)
    {
        musicVolume = value;
        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", value > 0 ? Mathf.Log10(value) * 20 : -80f);
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    void OnSFXChanged(float value)
    {
        sfxVolume = value;
        if (audioMixer != null)
            audioMixer.SetFloat("SFXVolume", value > 0 ? Mathf.Log10(value) * 20 : -80f);
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();

        // Play bloop so the player can hear the new volume level immediately
        if (bloopClip != null && uiAudioSource != null && bloopCooldown <= 0f)
        {
            uiAudioSource.PlayOneShot(bloopClip);
            bloopCooldown = BloopCooldownTime;
        }
    }

    void OnVoiceChanged(float value)
    {
        voiceVolume = value;
        if (audioMixer != null)
            audioMixer.SetFloat("VoiceVolume", value > 0 ? Mathf.Log10(value) * 20 : -80f);
        PlayerPrefs.SetFloat("VoiceVolume", value);
        PlayerPrefs.Save();
    }

    void OnFamilyFriendlyChanged(bool value)
    {
        familyFriendly = value;
        PlayerPrefs.SetInt("FamilyFriendly", value ? 1 : 0);
        PlayerPrefs.Save();
    }
}