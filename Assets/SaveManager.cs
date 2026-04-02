using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    [Header("UI")]
    public TextMeshProUGUI notificationText;

    [Header("Save Slot Panel")]
    public GameObject saveSlotPanel;
    public Button slotAutoButton;
    public TextMeshProUGUI slotAutoLabel;
    public TextMeshProUGUI slotAutoInfo;
    public Button slot1Button;
    public TextMeshProUGUI slot1Label;
    public TextMeshProUGUI slot1Info;
    public Button slot2Button;
    public TextMeshProUGUI slot2Label;
    public TextMeshProUGUI slot2Info;
    public Button slot3Button;
    public TextMeshProUGUI slot3Label;
    public TextMeshProUGUI slot3Info;
    public Button backButton;
    public Button deleteButton;

    // Colors for delete mode
    private static readonly Color DeleteHighlight = new Color(0.8f, 0f, 0f, 1f);
    private static readonly Color NormalSlotColor = new Color(0f, 0f, 0.67f, 1f);

    [Header("Fallback Sprite")]
    public Sprite defaultItemSprite;

    [Header("Save / Load Voice Clips")]
    public AudioClip saveEasterEggClip;
    public AudioClip loadDialogue_Curly1;
    public AudioClip loadDialogue_Curly2;
    public AudioClip loadDialogue_Zoey;
    public AudioClip loadDialogue_Curly3;

    // Slot 0 = auto save, slots 1-3 = manual
    private const int AUTO_SLOT = 0;
    private const int SLOT_COUNT = 4;

    private bool isSaveMode = false;
    private bool isDeleteMode = false;
    private static int pendingLoadSlot = -1;
    private static bool pendingAutoSave = false;

    private CurlyMovement curly;
    private ZoeyAI zoey;

    public static void QueueAutoSave() { pendingAutoSave = true; }

    void Awake()
    {
        instance = this;
        if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
    }

    void Start()
    {
        curly = FindObjectOfType<CurlyMovement>();
        zoey = FindObjectOfType<ZoeyAI>();

        if (notificationText != null)
            notificationText.color = new Color(notificationText.color.r, notificationText.color.g, notificationText.color.b, 0f);

        // Hook up buttons
        if (slot1Button != null) slot1Button.onClick.AddListener(() => OnSlotClicked(1));
        if (slot2Button != null) slot2Button.onClick.AddListener(() => OnSlotClicked(2));
        if (slot3Button != null) slot3Button.onClick.AddListener(() => OnSlotClicked(3));
        if (slotAutoButton != null) slotAutoButton.onClick.AddListener(() => OnSlotClicked(0));
        if (backButton != null) backButton.onClick.AddListener(CloseSlotPanel);
        if (deleteButton != null) deleteButton.onClick.AddListener(ToggleDeleteMode);

        int playedCutsceneCount = PlayerPrefs.GetInt("PlayedCutsceneCount", 0);
        for (int i = 0; i < playedCutsceneCount; i++)
            CutsceneTrigger.playedCutscenes.Add(PlayerPrefs.GetString("PlayedCutscene" + i));
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        curly = FindObjectOfType<CurlyMovement>();
        zoey = FindObjectOfType<ZoeyAI>();

        if (pendingLoadSlot >= 0)
        {
            int slot = pendingLoadSlot;
            pendingLoadSlot = -1;
            StartCoroutine(RestoreAfterLoad(slot));
        }
        else if (pendingAutoSave)
        {
            pendingAutoSave = false;
            StartCoroutine(DelayedAutoSave());
        }
    }

    IEnumerator DelayedAutoSave()
    {
        yield return new WaitUntil(() => !SceneTransition.isFading);
        yield return new WaitForSeconds(2f);
        SaveToSlot(AUTO_SLOT);
    }

    // ── Open/Close Panel ────────────────────────────────────────

    public void OpenSavePanel()
    {
        isSaveMode = true;
        OpenSlotPanel();
    }

    public void OpenLoadPanel()
    {
        isSaveMode = false;
        OpenSlotPanel();
    }

    void OpenSlotPanel()
    {
        RefreshSlotDisplay();

        // In save mode, auto slot is greyed out and unclickable
        if (slotAutoButton != null)
        {
            slotAutoButton.interactable = !isSaveMode;
            Color c = slotAutoLabel != null ? slotAutoLabel.color : Color.white;
            if (slotAutoLabel != null) slotAutoLabel.color = isSaveMode ? new Color(c.r, c.g, c.b, 0.4f) : new Color(c.r, c.g, c.b, 1f);
        }

        if (saveSlotPanel != null) saveSlotPanel.SetActive(true);
        DialogueManager.isInDialogue = true;
    }

    public void CloseSlotPanel()
    {
        isDeleteMode = false;
        RefreshSlotColors();
        if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
        DialogueManager.isInDialogue = false;
        if (SettingsMenu.instance != null) SettingsMenu.instance.OpenSettings();
    }

    void RefreshSlotDisplay()
    {
        RefreshSlot(0, slotAutoLabel, slotAutoInfo, "AUTO SAVE");
        RefreshSlot(1, slot1Label, slot1Info, "SLOT 1");
        RefreshSlot(2, slot2Label, slot2Info, "SLOT 2");
        RefreshSlot(3, slot3Label, slot3Info, "SLOT 3");
    }

    void RefreshSlot(int slot, TextMeshProUGUI label, TextMeshProUGUI info, string labelText)
    {
        if (label != null) label.text = labelText;
        if (info == null) return;

        string prefix = "Slot" + slot + "_";
        if (!PlayerPrefs.HasKey(prefix + "Scene"))
        {
            info.text = "";
            return;
        }

        string scene = PlayerPrefs.GetString(prefix + "Scene");
        string era = PlayerPrefs.GetString(prefix + "Era");
        string timestamp = PlayerPrefs.GetString(prefix + "Time");
        info.text = scene + " | " + era + "\n" + timestamp;
    }

    void ToggleDeleteMode()
    {
        isDeleteMode = !isDeleteMode;

        // Tint delete button red when active
        if (deleteButton != null)
        {
            Image img = deleteButton.GetComponent<Image>();
            if (img != null) img.color = isDeleteMode ? DeleteHighlight : NormalSlotColor;
        }

        RefreshSlotColors();
    }

    void RefreshSlotColors()
    {
        SetSlotColor(slotAutoButton, isDeleteMode);
        SetSlotColor(slot1Button, isDeleteMode);
        SetSlotColor(slot2Button, isDeleteMode);
        SetSlotColor(slot3Button, isDeleteMode);
    }

    void SetSlotColor(Button btn, bool danger)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = danger ? DeleteHighlight : NormalSlotColor;
    }

    void OnSlotClicked(int slot)
    {
        if (isDeleteMode)
        {
            DeleteSlot(slot);
            isDeleteMode = false;
            RefreshSlotColors();
            if (deleteButton != null)
            {
                Image img = deleteButton.GetComponent<Image>();
                if (img != null) img.color = NormalSlotColor;
            }
        }
        else if (isSaveMode)
            SaveToSlot(slot);
        else
            LoadFromSlot(slot);
    }

    void DeleteSlot(int slot)
    {
        string prefix = "Slot" + slot + "_";
        PlayerPrefs.DeleteKey(prefix + "Scene");
        PlayerPrefs.DeleteKey(prefix + "Era");
        PlayerPrefs.DeleteKey(prefix + "Time");
        PlayerPrefs.DeleteKey(prefix + "CurlyX");
        PlayerPrefs.DeleteKey(prefix + "CurlyY");
        PlayerPrefs.DeleteKey(prefix + "ZoeyX");
        PlayerPrefs.DeleteKey(prefix + "ZoeyY");

        int invCount = PlayerPrefs.GetInt(prefix + "InventoryCount", 0);
        for (int i = 0; i < invCount; i++)
        {
            PlayerPrefs.DeleteKey(prefix + "ItemName" + i);
            PlayerPrefs.DeleteKey(prefix + "ItemColorR" + i);
            PlayerPrefs.DeleteKey(prefix + "ItemColorG" + i);
            PlayerPrefs.DeleteKey(prefix + "ItemColorB" + i);
        }
        PlayerPrefs.DeleteKey(prefix + "InventoryCount");

        int pickedCount = PlayerPrefs.GetInt(prefix + "PickedUpCount", 0);
        for (int i = 0; i < pickedCount; i++)
            PlayerPrefs.DeleteKey(prefix + "PickedUp" + i);
        PlayerPrefs.DeleteKey(prefix + "PickedUpCount");

        int interactCount = PlayerPrefs.GetInt(prefix + "InteractableCount", 0);
        for (int i = 0; i < interactCount; i++)
        {
            PlayerPrefs.DeleteKey(prefix + "InteractableName" + i);
            PlayerPrefs.DeleteKey(prefix + "InteractableActive" + i);
        }
        PlayerPrefs.DeleteKey(prefix + "InteractableCount");

        int cutsceneCount = PlayerPrefs.GetInt(prefix + "PlayedCutsceneCount", 0);
        for (int i = 0; i < cutsceneCount; i++)
            PlayerPrefs.DeleteKey(prefix + "PlayedCutscene" + i);
        PlayerPrefs.DeleteKey(prefix + "PlayedCutsceneCount");

        PlayerPrefs.Save();
        RefreshSlotDisplay();
        ShowNotification("Save Deleted");
    }

    // ── Save ─────────────────────────────────────────────────────

    public void SaveToSlot(int slot)
    {
        string prefix = "Slot" + slot + "_";
        bool isAutoSave = slot == AUTO_SLOT;

        PlayerPrefs.SetString(prefix + "Scene", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetString(prefix + "Era", PhoneBoothUI.currentEra);
        PlayerPrefs.SetString(prefix + "Time", DateTime.Now.ToString("MM/dd/yyyy HH:mm"));

        if (curly != null)
        {
            PlayerPrefs.SetFloat(prefix + "CurlyX", curly.transform.position.x);
            PlayerPrefs.SetFloat(prefix + "CurlyY", curly.transform.position.y);
        }

        if (zoey != null)
        {
            PlayerPrefs.SetFloat(prefix + "ZoeyX", zoey.transform.position.x);
            PlayerPrefs.SetFloat(prefix + "ZoeyY", zoey.transform.position.y);
        }

        List<string> itemNames = InventoryManager.instance.itemNames;
        List<Color> itemColors = InventoryManager.instance.itemColors;

        PlayerPrefs.SetInt(prefix + "InventoryCount", itemNames.Count);
        for (int i = 0; i < itemNames.Count; i++)
        {
            PlayerPrefs.SetString(prefix + "ItemName" + i, itemNames[i]);
            PlayerPrefs.SetFloat(prefix + "ItemColorR" + i, itemColors[i].r);
            PlayerPrefs.SetFloat(prefix + "ItemColorG" + i, itemColors[i].g);
            PlayerPrefs.SetFloat(prefix + "ItemColorB" + i, itemColors[i].b);
        }

        var pickedUp = new List<string>(Interactable.pickedUpItems);
        PlayerPrefs.SetInt(prefix + "PickedUpCount", pickedUp.Count);
        for (int i = 0; i < pickedUp.Count; i++)
            PlayerPrefs.SetString(prefix + "PickedUp" + i, pickedUp[i]);

        Interactable[] allInteractables = FindObjectsOfType<Interactable>(true);
        PlayerPrefs.SetInt(prefix + "InteractableCount", allInteractables.Length);
        for (int i = 0; i < allInteractables.Length; i++)
        {
            PlayerPrefs.SetString(prefix + "InteractableName" + i, allInteractables[i].itemName);
            PlayerPrefs.SetInt(prefix + "InteractableActive" + i, allInteractables[i].gameObject.activeSelf ? 1 : 0);
        }

        var playedCutscenes = new List<string>(CutsceneTrigger.playedCutscenes);
        PlayerPrefs.SetInt(prefix + "PlayedCutsceneCount", playedCutscenes.Count);
        for (int i = 0; i < playedCutscenes.Count; i++)
            PlayerPrefs.SetString(prefix + "PlayedCutscene" + i, playedCutscenes[i]);

        NPCDialogue[] allNPCs = FindObjectsOfType<NPCDialogue>(true);
        for (int i = 0; i < allNPCs.Length; i++)
            allNPCs[i].SaveState();

        PlayerPrefs.Save();

        if (!isAutoSave && UnityEngine.Random.Range(0, 100) == 0)
            DialogueLabel.curlyLabel.Say("...The hell?", saveEasterEggClip);

        if (!isAutoSave)
        {
            CloseSlotPanel();
            ShowNotification("Game Saved");
        }
    }

    // ── Load ─────────────────────────────────────────────────────

    public void LoadFromSlot(int slot)
    {
        string prefix = "Slot" + slot + "_";
        if (!PlayerPrefs.HasKey(prefix + "Scene")) return;

        string savedScene = PlayerPrefs.GetString(prefix + "Scene");
        string currentScene = SceneManager.GetActiveScene().name;

        CloseSlotPanel();
        ShowNotification("Game Loaded");

        if (savedScene == currentScene)
        {
            StartCoroutine(RestoreAfterLoad(slot));
        }
        else
        {
            pendingLoadSlot = slot;
            SceneManager.LoadScene(savedScene);
        }
    }

    IEnumerator RestoreAfterLoad(int slot)
    {
        yield return null;

        string prefix = "Slot" + slot + "_";

        curly = FindObjectOfType<CurlyMovement>();
        zoey = FindObjectOfType<ZoeyAI>();

        PhoneBoothUI.currentEra = PlayerPrefs.GetString(prefix + "Era", "1987");

        if (curly != null)
        {
            curly.transform.position = new Vector3(
                PlayerPrefs.GetFloat(prefix + "CurlyX"),
                PlayerPrefs.GetFloat(prefix + "CurlyY"), 0f);
            curly.CancelMovement();
        }

        if (zoey != null)
        {
            zoey.transform.position = new Vector3(
                PlayerPrefs.GetFloat(prefix + "ZoeyX"),
                PlayerPrefs.GetFloat(prefix + "ZoeyY"), 0f);
            zoey.StopAndStay();
        }

        InventoryManager.instance.ClearAllItems();
        int count = PlayerPrefs.GetInt(prefix + "InventoryCount");
        for (int i = 0; i < count; i++)
        {
            string itemName = PlayerPrefs.GetString(prefix + "ItemName" + i);
            Color color = new Color(
                PlayerPrefs.GetFloat(prefix + "ItemColorR" + i),
                PlayerPrefs.GetFloat(prefix + "ItemColorG" + i),
                PlayerPrefs.GetFloat(prefix + "ItemColorB" + i));
            Sprite sprite = InventoryData.GetSprite(itemName) ?? defaultItemSprite;
            InventoryManager.instance.AddItem(itemName, sprite, color);
        }

        Interactable.pickedUpItems.Clear();
        int pickedUpCount = PlayerPrefs.GetInt(prefix + "PickedUpCount");
        for (int i = 0; i < pickedUpCount; i++)
            Interactable.pickedUpItems.Add(PlayerPrefs.GetString(prefix + "PickedUp" + i));

        Interactable[] allInteractables = FindObjectsOfType<Interactable>(true);
        int interactableCount = PlayerPrefs.GetInt(prefix + "InteractableCount");
        for (int i = 0; i < interactableCount; i++)
        {
            string savedName = PlayerPrefs.GetString(prefix + "InteractableName" + i);
            int active = PlayerPrefs.GetInt(prefix + "InteractableActive" + i);
            foreach (Interactable interactable in allInteractables)
                if (interactable.itemName == savedName)
                    interactable.gameObject.SetActive(active == 1);
        }

        CutsceneTrigger.playedCutscenes.Clear();
        int playedCutsceneCount = PlayerPrefs.GetInt(prefix + "PlayedCutsceneCount", 0);
        for (int i = 0; i < playedCutsceneCount; i++)
            CutsceneTrigger.playedCutscenes.Add(PlayerPrefs.GetString(prefix + "PlayedCutscene" + i));

        NPCDialogue[] allNPCs = FindObjectsOfType<NPCDialogue>(true);
        foreach (NPCDialogue npc in allNPCs)
            npc.LoadState();

        if (UnityEngine.Random.Range(0, 100) == 0)
            StartCoroutine(LoadDialogue());
    }

    // ── Notifications ────────────────────────────────────────────

    void ShowNotification(string message)
    {
        if (notificationText == null) return;
        StopCoroutine("FadeNotification");
        StartCoroutine(FadeNotification(message));
    }

    IEnumerator FadeNotification(string message)
    {
        notificationText.text = message;
        Color c = notificationText.color;
        c.a = 0f;
        notificationText.color = c;

        while (c.a < 1f)
        {
            c.a += Time.deltaTime * 3f;
            notificationText.color = c;
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);

        while (c.a > 0f)
        {
            c.a -= Time.deltaTime * 2f;
            notificationText.color = c;
            yield return null;
        }
    }

    IEnumerator LoadDialogue()
    {
        DialogueLabel.curlyLabel.Say("I just had the strangest feeling...", loadDialogue_Curly1);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("like I was dropped into this exact spot from somewhere else entirely.", loadDialogue_Curly2);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("What makes you think that?", loadDialogue_Zoey);
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("No reason. Forget I said anything.", loadDialogue_Curly3);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
    }
}