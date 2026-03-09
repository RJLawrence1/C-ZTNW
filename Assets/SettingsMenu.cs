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
    private const float BloopCooldownTime = 2f;

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
    }

    public void OpenSettings()
    {
        isOpen = true;
        settingsPanel.SetActive(true);
        optionsPanel.SetActive(false);
        quitPanel.SetActive(false);
        DialogueManager.isInDialogue = true;
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
    }

    void CloseOptions()
    {
        optionsPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    void OpenQuitConfirmation()
    {
        settingsPanel.SetActive(false);
        quitPanel.SetActive(true);
        if (quitMessage != null)
            quitMessage.text = "Hey! Make sure you save your game before you go!";

        // Play Curly's voice line if cooldown has expired
        if (quitVoiceLine != null && voiceAudioSource != null && quitLineCooldown <= 0f)
        {
            voiceAudioSource.PlayOneShot(quitVoiceLine);
            quitLineCooldown = QuitLineCooldownTime;
        }
    }

    void CloseQuitConfirmation()
    {
        quitPanel.SetActive(false);
        settingsPanel.SetActive(true);
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