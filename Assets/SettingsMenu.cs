using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu instance;
    public static bool isOpen = false;

    [Header("Panel")]
    public GameObject settingsPanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button saveButton;
    public Button loadButton;
    public Button optionsButton;
    public Button quitButton;

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

    [Header("Version Text")]
    public TextMeshProUGUI versionText;

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
        // Set version text
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
        // F5 toggles settings
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            if (isOpen) CloseSettings();
            else OpenSettings();
        }

        // Escape also closes
        if (Keyboard.current.escapeKey.wasPressedThisFrame && isOpen)
            CloseSettings();

        // Controller — Start button opens/closes
        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
        {
            if (isOpen) CloseSettings();
            else OpenSettings();
        }
    }

    public void OpenSettings()
    {
        isOpen = true;
        settingsPanel.SetActive(true);
        optionsPanel.SetActive(false);
        quitPanel.SetActive(false);
        DialogueManager.isInDialogue = true; // Block input while open
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
        optionsPanel.SetActive(true);
    }

    void CloseOptions()
    {
        optionsPanel.SetActive(false);
    }

    void OpenQuitConfirmation()
    {
        quitPanel.SetActive(true);
        if (quitMessage != null)
            quitMessage.text = "Hey! Make sure you save your game!";
    }

    void CloseQuitConfirmation()
    {
        quitPanel.SetActive(false);
    }

    void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Volume callbacks
    void OnMusicChanged(float value)
    {
        musicVolume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    void OnSFXChanged(float value)
    {
        sfxVolume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    void OnVoiceChanged(float value)
    {
        voiceVolume = value;
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