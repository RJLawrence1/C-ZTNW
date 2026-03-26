using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    [Header("UI")]
    public TextMeshProUGUI notificationText;

    [Header("Fallback Sprite")]
    // Drag your white square sprite here — used when a sprite can't be found on load
    public Sprite defaultItemSprite;

    [Header("Save / Load Voice Clips")]
    public AudioClip saveEasterEggClip;
    public AudioClip loadDialogue_Curly1;
    public AudioClip loadDialogue_Curly2;
    public AudioClip loadDialogue_Zoey;
    public AudioClip loadDialogue_Curly3;

    // Cached references
    private CurlyMovement curly;
    private ZoeyAI zoey;

    // Tracks whether we just loaded a save and are waiting for the scene to finish loading
    private static bool pendingLoad = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        curly = FindObjectOfType<CurlyMovement>();
        zoey = FindObjectOfType<ZoeyAI>();

        if (notificationText != null)
            notificationText.color = new Color(notificationText.color.r, notificationText.color.g, notificationText.color.b, 0f);

        // Load played cutscenes from PlayerPrefs so they persist across sessions
        int playedCutsceneCount = PlayerPrefs.GetInt("PlayedCutsceneCount", 0);
        for (int i = 0; i < playedCutsceneCount; i++)
            CutsceneTrigger.playedCutscenes.Add(PlayerPrefs.GetString("PlayedCutscene" + i));
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        curly = FindObjectOfType<CurlyMovement>();
        zoey = FindObjectOfType<ZoeyAI>();

        if (pendingLoad)
        {
            pendingLoad = false;
            StartCoroutine(RestoreAfterLoad());
        }
    }

    void Update()
    {
        if (Keyboard.current.f6Key.wasPressedThisFrame)
            SaveGame();
        if (Keyboard.current.f9Key.wasPressedThisFrame)
            LoadGame();
    }

    public void SaveGame()
    {
        // Save current scene and era
        PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetString("CurrentEra", PhoneBoothUI.currentEra);

        if (curly != null)
        {
            PlayerPrefs.SetFloat("CurlyX", curly.transform.position.x);
            PlayerPrefs.SetFloat("CurlyY", curly.transform.position.y);
        }

        if (zoey != null)
        {
            PlayerPrefs.SetFloat("ZoeyX", zoey.transform.position.x);
            PlayerPrefs.SetFloat("ZoeyY", zoey.transform.position.y);
        }

        List<string> itemNames = InventoryManager.instance.itemNames;
        List<Color> itemColors = InventoryManager.instance.itemColors;

        PlayerPrefs.SetInt("InventoryCount", itemNames.Count);
        for (int i = 0; i < itemNames.Count; i++)
        {
            PlayerPrefs.SetString("ItemName" + i, itemNames[i]);
            PlayerPrefs.SetFloat("ItemColorR" + i, itemColors[i].r);
            PlayerPrefs.SetFloat("ItemColorG" + i, itemColors[i].g);
            PlayerPrefs.SetFloat("ItemColorB" + i, itemColors[i].b);
        }

        // Save picked up item names so they don't respawn on load
        var pickedUp = new List<string>(Interactable.pickedUpItems);
        PlayerPrefs.SetInt("PickedUpCount", pickedUp.Count);
        for (int i = 0; i < pickedUp.Count; i++)
            PlayerPrefs.SetString("PickedUp" + i, pickedUp[i]);

        Interactable[] allInteractables = FindObjectsOfType<Interactable>(true);
        PlayerPrefs.SetInt("InteractableCount", allInteractables.Length);
        for (int i = 0; i < allInteractables.Length; i++)
        {
            PlayerPrefs.SetString("InteractableName" + i, allInteractables[i].itemName);
            PlayerPrefs.SetInt("InteractableActive" + i, allInteractables[i].gameObject.activeSelf ? 1 : 0);
        }

        // Save played cutscenes so they don't replay on load
        var playedCutscenes = new List<string>(CutsceneTrigger.playedCutscenes);
        PlayerPrefs.SetInt("PlayedCutsceneCount", playedCutscenes.Count);
        for (int i = 0; i < playedCutscenes.Count; i++)
            PlayerPrefs.SetString("PlayedCutscene" + i, playedCutscenes[i]);

        // Save all NPC dialogue states
        NPCDialogue[] allNPCs = FindObjectsOfType<NPCDialogue>(true);
        PlayerPrefs.SetInt("NPCCount", allNPCs.Length);
        for (int i = 0; i < allNPCs.Length; i++)
            allNPCs[i].SaveState();

        PlayerPrefs.Save();

        if (Random.Range(0, 100) == 0)
            DialogueLabel.curlyLabel.Say("...The hell?", saveEasterEggClip);

        ShowNotification("Game Saved");
        Debug.Log("Game saved in scene: " + SceneManager.GetActiveScene().name + " | Era: " + PhoneBoothUI.currentEra);
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey("SavedScene")) return;

        string savedScene = PlayerPrefs.GetString("SavedScene");
        string currentScene = SceneManager.GetActiveScene().name;

        ShowNotification("Game Loaded");

        if (savedScene == currentScene)
        {
            StartCoroutine(RestoreAfterLoad());
        }
        else
        {
            pendingLoad = true;
            SceneManager.LoadScene(savedScene);
        }
    }

    IEnumerator RestoreAfterLoad()
    {
        // Wait one frame to make sure all Start() methods have run in the new scene
        yield return null;

        curly = FindObjectOfType<CurlyMovement>();
        zoey = FindObjectOfType<ZoeyAI>();

        // Restore era
        PhoneBoothUI.currentEra = PlayerPrefs.GetString("CurrentEra", "1987");

        if (curly != null)
        {
            float x = PlayerPrefs.GetFloat("CurlyX");
            float y = PlayerPrefs.GetFloat("CurlyY");
            curly.transform.position = new Vector3(x, y, 0f);
            curly.CancelMovement();
        }

        if (zoey != null)
        {
            float zx = PlayerPrefs.GetFloat("ZoeyX");
            float zy = PlayerPrefs.GetFloat("ZoeyY");
            zoey.transform.position = new Vector3(zx, zy, 0f);
            zoey.StopAndStay();
        }

        InventoryManager.instance.ClearAllItems();
        int count = PlayerPrefs.GetInt("InventoryCount");
        for (int i = 0; i < count; i++)
        {
            string itemName = PlayerPrefs.GetString("ItemName" + i);
            Color color = new Color(
                PlayerPrefs.GetFloat("ItemColorR" + i),
                PlayerPrefs.GetFloat("ItemColorG" + i),
                PlayerPrefs.GetFloat("ItemColorB" + i)
            );

            // Try to find the sprite from the store, fall back to default white square
            Sprite sprite = InventoryData.GetSprite(itemName);
            if (sprite == null) sprite = defaultItemSprite;

            InventoryManager.instance.AddItem(itemName, sprite, color);
        }

        // Restore picked up items so they don't respawn
        Interactable.pickedUpItems.Clear();
        int pickedUpCount = PlayerPrefs.GetInt("PickedUpCount");
        for (int i = 0; i < pickedUpCount; i++)
            Interactable.pickedUpItems.Add(PlayerPrefs.GetString("PickedUp" + i));

        Interactable[] allInteractables = FindObjectsOfType<Interactable>(true);
        int interactableCount = PlayerPrefs.GetInt("InteractableCount");
        for (int i = 0; i < interactableCount; i++)
        {
            string savedName = PlayerPrefs.GetString("InteractableName" + i);
            int active = PlayerPrefs.GetInt("InteractableActive" + i);
            foreach (Interactable interactable in allInteractables)
            {
                if (interactable.itemName == savedName)
                    interactable.gameObject.SetActive(active == 1);
            }
        }

        // Restore played cutscenes so they don't replay
        CutsceneTrigger.playedCutscenes.Clear();
        int playedCutsceneCount = PlayerPrefs.GetInt("PlayedCutsceneCount", 0);
        for (int i = 0; i < playedCutsceneCount; i++)
            CutsceneTrigger.playedCutscenes.Add(PlayerPrefs.GetString("PlayedCutscene" + i));

        // Restore all NPC dialogue states
        NPCDialogue[] allNPCs = FindObjectsOfType<NPCDialogue>(true);
        foreach (NPCDialogue npc in allNPCs)
            npc.LoadState();

        if (Random.Range(0, 100) == 0)
            StartCoroutine(LoadDialogue());

        Debug.Log("Game loaded in scene: " + SceneManager.GetActiveScene().name + " | Era: " + PhoneBoothUI.currentEra);
    }

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