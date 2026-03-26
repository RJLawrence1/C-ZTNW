using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition instance;
    public Image fadeImage;
    public float fadeSpeed = 1.5f;

    private static string targetDoorTag = "";

    // Set this before a scene load to hold on black before fading in
    public static float holdTime = 0f;

    void Awake()
    {
        // If an instance already exists from a previous scene, destroy this
        // duplicate and keep the original — otherwise set this as the instance
        // and make sure it survives scene loads
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Called automatically by Unity whenever a new scene finishes loading
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(FadeInAndSpawn());
    }

    public void GoToScene(string sceneName, string doorTag)
    {
        targetDoorTag = doorTag;
        StartCoroutine(FadeAndLoad(sceneName));
    }

    IEnumerator FadeAndLoad(string sceneName)
    {
        // Fade music out alongside the screen fade
        if (MusicManager.instance != null)
            MusicManager.instance.FadeOut();

        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeInAndSpawn()
    {
        // Unlock movement as soon as the new scene loads
        SceneDoor.movementLocked = false;

        // Hold on black if requested — used for time travel arrival sound
        if (holdTime > 0f)
        {
            yield return new WaitForSeconds(holdTime);
            holdTime = 0f;
        }

        if (targetDoorTag != "")
        {
            SceneDoor[] doors = FindObjectsOfType<SceneDoor>();
            foreach (SceneDoor door in doors)
            {
                if (door.spawnDoorTag == targetDoorTag)
                {
                    Vector3 spawnPos = door.transform.position;

                    CurlyMovement curly = FindObjectOfType<CurlyMovement>();
                    ZoeyAI zoey = FindObjectOfType<ZoeyAI>();

                    if (curly != null)
                    {
                        curly.transform.position = spawnPos;
                        curly.CancelMovement();
                    }

                    if (zoey != null)
                        zoey.transform.position = spawnPos - new Vector3(0.5f, 0.3f, 0f);

                    door.StartCooldown();
                    targetDoorTag = "";
                    break;
                }
            }
        }

        yield return StartCoroutine(FadeIn());
    }

    // Fades to black and stays black — used for time travel so a sound can play over it
    public IEnumerator FadeOutAndHold()
    {
        if (MusicManager.instance != null)
            MusicManager.instance.FadeOut();

        yield return StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        Color c = fadeImage.color;
        while (c.a < 1f)
        {
            c.a += Time.deltaTime * fadeSpeed;
            fadeImage.color = c;
            yield return null;
        }
    }

    IEnumerator FadeIn()
    {
        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;
        while (c.a > 0f)
        {
            c.a -= Time.deltaTime * fadeSpeed;
            fadeImage.color = c;
            yield return null;
        }
    }
}