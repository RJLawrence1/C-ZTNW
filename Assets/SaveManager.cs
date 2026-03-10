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
    // Drag a TextMeshProUGUI element from your Canvas here.
    // It will briefly show "Game Saved" or "Game Loaded" then fade out.
    public TextMeshProUGUI notificationText;

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

        // Hide the notification text at startup
        if (notificationText != null)
            notificationText.color = new Color(notificationText.color.r, notificationText.color.g, notificationText.color.b, 0f);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // After a load, re-cache references and restore state once the new scene is ready
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
        // Save current scene name so we can return to it on load
        PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);

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

        PlayerPrefs.Save();

        if (Random.Range(0, 100) == 0)
            DialogueLabel.curlyLabel.Say("...The hell?");

        ShowNotification("Game Saved");
        Debug.Log("Game saved in scene: " + SceneManager.GetActiveScene().name);
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey("SavedScene")) return;

        string savedScene = PlayerPrefs.GetString("SavedScene");
        string currentScene = SceneManager.GetActiveScene().name;

        ShowNotification("Game Loaded");

        if (savedScene == currentScene)
        {
            // Same scene — restore directly without reloading
            StartCoroutine(RestoreAfterLoad());
        }
        else
        {
            // Different scene — load it, then restore once it's ready
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
            InventoryManager.instance.AddItem(itemName, null, color);
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

        if (Random.Range(0, 100) == 0)
            StartCoroutine(LoadDialogue());

        Debug.Log("Game loaded in scene: " + SceneManager.GetActiveScene().name);
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

        // Fade in quickly
        Color c = notificationText.color;
        c.a = 0f;
        notificationText.color = c;

        while (c.a < 1f)
        {
            c.a += Time.deltaTime * 3f;
            notificationText.color = c;
            yield return null;
        }

        // Hold for 1.5 seconds
        yield return new WaitForSeconds(1.5f);

        // Fade out slowly
        while (c.a > 0f)
        {
            c.a -= Time.deltaTime * 2f;
            notificationText.color = c;
            yield return null;
        }
    }

    IEnumerator LoadDialogue()
    {
        DialogueLabel.curlyLabel.Say("I just had the strangest feeling...");
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say("like I was dropped into this exact spot from somewhere else entirely.");
        yield return new WaitForSeconds(3f);
        DialogueLabel.zoeyLabel.Say("What makes you think that?");
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say("No reason. Forget I said anything.");
    }
}