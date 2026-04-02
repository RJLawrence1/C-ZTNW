using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;

public class IntroSequence : MonoBehaviour
{
    [Header("Scene to load after intro")]
    public string nextScene = "MainGame";

    [Header("Dev Note")]
    public GameObject devNotePanel;
    public TextMeshProUGUI devNoteText;

    [Header("Boot Screen")]
    public GameObject bootPanel;
    public TextMeshProUGUI bootText;

    [Header("Boot Audio")]
    public AudioSource bootAudioSource;
    public AudioClip bootAudioClip;

    [Header("OS Load Screen")]
    public GameObject osPanel;
    public TextMeshProUGUI osText;
    public Image osLoadBar;

    [Header("Logo Spawn Path")]
    public SpriteShapeController spawnPath;

    [Header("Logo Screen")]
    public GameObject logoPanel;
    public Image starfield;
    public Image gupworksLogo;
    public float centerLogoSize = 350f;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip logoCue;

    [Header("Fade")]
    public Image fadeImage;

    [Header("Skip")]
    public TextMeshProUGUI skipText; // assign a TMP text at bottom center of Canvas
    public float skipHoldTime = 3f;

    [Header("Timing")]
    public float charDelay = 0.03f;
    public float lineDelay = 0.4f;
    public float logoScreenTime = 6f;
    public int logoCount = 14;

    private List<RectTransform> floatingLogos = new List<RectTransform>();
    private bool introSkipped = false;
    private float skipHoldTimer = 0f;

    void Start()
    {
        devNotePanel.SetActive(false);
        bootPanel.SetActive(false);
        osPanel.SetActive(false);
        logoPanel.SetActive(false);

        if (skipText != null) skipText.text = "";

        SetFade(1f);
        StartCoroutine(RunIntro());
    }

    void Update()
    {
        if (introSkipped) return;

        // Ignore skip during dev note — it has its own input handling
        if (devNotePanel != null && devNotePanel.activeSelf) return;

        if (Keyboard.current.spaceKey.isPressed)
        {
            skipHoldTimer += Time.deltaTime;

            // Show progress text
            if (skipText != null)
            {
                int dots = Mathf.FloorToInt((skipHoldTimer / skipHoldTime) * 3f) + 1;
                dots = Mathf.Clamp(dots, 1, 3);
                skipText.text = "Skipping intro" + new string('.', dots);
            }

            if (skipHoldTimer >= skipHoldTime)
                SkipIntro();
        }
        else
        {
            // Reset if released
            skipHoldTimer = 0f;
            if (skipText != null) skipText.text = "";
        }
    }

    void SkipIntro()
    {
        introSkipped = true;
        keepSpawning = false;
        StopAllCoroutines();

        if (skipText != null) skipText.text = "";
        if (bootAudioSource != null) bootAudioSource.Stop();
        if (musicSource != null) musicSource.Stop();

        SetFade(1f);
        SceneManager.LoadScene(nextScene);
    }

    IEnumerator RunIntro()
    {
        yield return StartCoroutine(ShowDevNote());
        if (introSkipped) yield break;

        if (bootAudioSource != null && bootAudioClip != null)
        {
            bootAudioSource.clip = bootAudioClip;
            bootAudioSource.Play();
        }

        logoPanel.SetActive(true);
        Color starColor = starfield.color;
        starColor.a = 1f;
        starfield.color = starColor;
        gupworksLogo.color = new Color(1f, 1f, 1f, 1f);
        StartCoroutine(FloatLogo());

        SetFade(1f);
        bootPanel.SetActive(true);
        bootText.text = "";
        SetFade(0f);

        yield return StartCoroutine(PlayBootSequence());
        if (introSkipped) yield break;

        bootPanel.SetActive(false);
        osPanel.SetActive(true);
        osText.text = "";
        osLoadBar.fillAmount = 0f;

        yield return StartCoroutine(PlayOSSequence());
        if (introSkipped) yield break;

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(FadeOut());
        osPanel.SetActive(false);

        StartCoroutine(FadeIn(0.5f));
        yield return new WaitForSeconds(0.5f);

        if (musicSource != null && logoCue != null)
        {
            musicSource.clip = logoCue;
            musicSource.Play();
        }

        yield return new WaitForSeconds(1.5f);
        yield return new WaitForSeconds(logoScreenTime);
        if (introSkipped) yield break;

        keepSpawning = false;
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(nextScene);
    }

    IEnumerator ShowDevNote()
    {
        devNotePanel.SetActive(true);
        devNoteText.color = new Color(1f, 1f, 1f, 1f);
        devNoteText.text = "";

        yield return StartCoroutine(FadeIn());
        yield return new WaitForSeconds(0.3f);

        string[] lines = {
            "DEVELOPER NOTE:",
            null,
            "Only the character voices in this game were generated using AI tools,",
            "except for Curly, who is voiced by me, because I have no budget,",
            "no cast, and no friends willing to yell into a microphone for me.",
            null,
            "Everything else is 100% handmade in Unity by a college kid (me)",
            "working out of a dorm room, powered by caffeine, willpower,",
            "college classes, and classmates who genuinely wanted",
            "to see this project go farther than common sense suggested.",
            null,
            "I built the basic systems for this game from the ground up in four days,",
            "which is either impressive or deeply concerning depending on who you ask.",
            null,
            "I made this game because I believe point-and-click adventures",
            "are a lost art, and someone had to try bringing them back.",
            "Even if my entire experience amounts to 2.5 hours",
            "of Sam & Max Hit the Road.",
            null,
            "Thanks for giving this game a chance, whoever you are.",
            "I'm just being funny and honest when I say:",
            "I really did put my heart into this. Please enjoy it.",
            null,
            "— Robert Lawrence",
            "   Fictional CEO, GupWorks Interactive",
        };

        bool skipped = false;

        foreach (string line in lines)
        {
            if (introSkipped) yield break;

            if (line == null)
            {
                devNoteText.text += "\n";
                float pauseTimer = 0f;
                while (pauseTimer < 1f)
                {
                    if (Input.anyKeyDown || Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        skipped = true;
                        break;
                    }
                    pauseTimer += Time.deltaTime;
                    yield return null;
                }
                if (skipped) break;
            }
            else
            {
                foreach (char c in line)
                {
                    if (introSkipped) yield break;
                    if (Input.anyKeyDown || Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        skipped = true;
                        break;
                    }
                    devNoteText.text += c;
                    yield return new WaitForSeconds(charDelay);
                }
                devNoteText.text += "\n";
                if (skipped) break;
            }
        }

        if (skipped)
        {
            devNoteText.text =
                "DEVELOPER NOTE:\n\n" +
                "Only the character voices in this game were generated using AI tools,\n" +
                "except for Curly, who is voiced by me, because I have no budget,\n" +
                "no cast, and no friends willing to yell into a microphone for me.\n\n" +
                "Everything else is 100% handmade in Unity by a college kid (me)\n" +
                "working out of a dorm room, powered by caffeine, willpower,\n" +
                "college classes, and classmates who genuinely wanted\n" +
                "to see this project go farther than common sense suggested.\n\n" +
                "I built the basic systems for this game from the ground up in four days,\n" +
                "which is either impressive or deeply concerning depending on who you ask.\n\n" +
                "I made this game because I believe point-and-click adventures\n" +
                "are a lost art, and someone had to try bringing them back.\n" +
                "Even if my entire experience amounts to 2.5 hours\n" +
                "of Sam & Max Hit the Road.\n\n" +
                "Thanks for giving this game a chance, whoever you are.\n" +
                "I'm just being funny and honest when I say:\n" +
                "I really did put my heart into this. Please enjoy it.\n\n" +
                "— Robert Lawrence\n" +
                "   Fictional CEO, GupWorks Interactive";

            yield return new WaitForSeconds(0.3f);
            yield return new WaitUntil(() => introSkipped || Input.anyKeyDown || Mouse.current.leftButton.wasPressedThisFrame);
        }
        else
        {
            yield return new WaitForSeconds(1f);
            yield return new WaitUntil(() => introSkipped || Input.anyKeyDown || Mouse.current.leftButton.wasPressedThisFrame);
        }

        if (introSkipped) yield break;

        yield return StartCoroutine(FadeImageAlpha(devNoteText, 1f, 0f));
        devNotePanel.SetActive(false);
    }

    IEnumerator FadeImageAlpha(TextMeshProUGUI tmp, float from, float to)
    {
        float duration = 1f;
        float elapsed = 0f;
        Color c = tmp.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            tmp.color = c;
            yield return null;
        }
        c.a = to;
        tmp.color = c;
    }

    IEnumerator PlayBootSequence()
    {
        bootText.color = new Color(1f, 1f, 1f, 1f);
        bootText.text = "";

        yield return new WaitForSeconds(4f);
        if (introSkipped) yield break;

        bootText.text =
            "GupWorks Interactive BIOS v1.9.87, An Energy Star Ally\n" +
            "Copyright (C) 1994 GupWorks Interactive. All rights reserved. Probably.\n" +
            "\n" +
            "(GW-1994) GupTech Pentium-G PCIset(TM)\n" +
            "\n" +
            "PENTIUM-G CPU at 66MHz\n";

        yield return StartCoroutine(CountRAM());
        if (introSkipped) yield break;
        yield return new WaitForSeconds(0.3f);

        bootText.text +=
            "\n" +
            "GupWorks Plug and Play BIOS Extension  v1.0A\n" +
            "Copyright (C) 1994, GupWorks Interactive\n" +
            "  Detecting IDE Primary Master   ... CURLY-HDD\n" +
            "  Detecting IDE Primary Slave    ... GUP-CDROM-4X\n" +
            "  Detecting IDE Secondary Master ... None\n" +
            "  Detecting IDE Secondary Slave  ... None\n";

        yield return new WaitForSeconds(0.5f);
        if (introSkipped) yield break;
        yield return new WaitForSeconds(5f);
        if (introSkipped) yield break;

        bootText.text +=
            "\n" +
            "+-----------------------+---------------------------+\n" +
            "| CPU Type  : PENTIUM-G | Base Memory   :    640K   |\n" +
            "| Co-Proc   : Installed | Extended Mem  :  31744K   |\n" +
            "| CPU Clock : 66MHz     | Cache Memory  :    256K   |\n" +
            "|                       |                           |\n" +
            "| Drive A   : 3.5\" 1.44MB   Display : GupVGA 256K  |\n" +
            "| Pri.Master: CURLY-HDD     Sound   : GupSound Pro  |\n" +
            "| Pri.Slave : GUP-CDROM-4X  IRQ     : 14            |\n" +
            "+-----------------------+---------------------------+\n" +
            "\n" +
            "PCI device listing.....\n" +
            "Bus No.  Device No.  Vendor ID  Device ID  Device Class      IRQ\n" +
            "  0         7          1994       0001      IDE Controller     14\n" +
            "  0        17          1994       0002      Multimedia Device  11\n" +
            "\n";

        yield return StartCoroutine(DMIVerify());
        if (introSkipped) yield break;

        bootText.text += "Starting GupWorks OS v1.9.87...\n";
        yield return new WaitForSeconds(0.3f);

        bootText.text += "\n\nPress <b>DEL</b> to enter SETUP\n";
        bootText.text += "12/10/94-GW1994,GUP8669-1A94GW2BC-00";

        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator CountRAM()
    {
        int target = 32768;
        int current = 0;
        int step = 512;

        bootText.text += "Memory Test : ";

        while (current < target)
        {
            if (introSkipped) yield break;
            current = Mathf.Min(current + step, target);
            string[] lines = bootText.text.Split('\n');
            lines[lines.Length - 1] = "Memory Test : " + current + "K OK";
            bootText.text = string.Join("\n", lines);
            yield return new WaitForSeconds(0.015f);
        }

        bootText.text += "\n";
        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator DMIVerify()
    {
        string base_line = "Verifying DMI Pool Data ";
        bootText.text += base_line;
        int dots = 0;
        while (dots < 8)
        {
            if (introSkipped) yield break;
            string[] lines = bootText.text.Split('\n');
            lines[lines.Length - 1] = base_line + new string('.', dots + 1);
            bootText.text = string.Join("\n", lines);
            dots++;
            yield return new WaitForSeconds(0.15f);
        }
        bootText.text += "\n";
    }

    IEnumerator PlayOSSequence()
    {
        osText.color = new Color(0f, 1f, 0.3f, 1f);
        osText.text =
            "GupWorks OS v1.9.87\n" +
            "Copyright (C) 1994 GupWorks Interactive\n" +
            "\n";

        yield return new WaitForSeconds(0.4f);
        if (introSkipped) yield break;

        osText.text +=
            "Loading CURLY_HENDERSON.EXE...  OK\n" +
            "Loading ZOEY.DLL...             OK\n" +
            "Loading PHONE_BOOTH.SYS...      OK\n" +
            "Loading TIME_TRAVEL.EXE...      OK\n" +
            "Initialising BAYFRONT...        OK\n";

        yield return new WaitForSeconds(0.4f);
        if (introSkipped) yield break;

        osText.text += "\nAll systems nominal. Probably.\n";

        yield return new WaitForSeconds(0.5f);
        if (introSkipped) yield break;

        float t = 0f;
        while (t < 1f)
        {
            if (introSkipped) yield break;
            t += Time.deltaTime * 1.5f;
            osLoadBar.fillAmount = Mathf.Clamp01(t);
            yield return null;
        }

        osText.text += "\nStarting game...\n";
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator TypeLine(TextMeshProUGUI textElement, string line, bool newline)
    {
        string current = textElement.text;
        foreach (char c in line)
        {
            current += c;
            textElement.text = current;
            yield return new WaitForSeconds(charDelay);
        }
        if (newline)
            textElement.text += "\n";
    }

    IEnumerator FloatLogo()
    {
        RectTransform centerRT = gupworksLogo.rectTransform;
        centerRT.anchoredPosition = Vector2.zero;
        centerRT.sizeDelta = new Vector2(centerLogoSize, centerLogoSize);
        gupworksLogo.preserveAspect = true;
        gupworksLogo.color = new Color(1f, 1f, 1f, 1f);

        for (int i = 0; i < logoCount; i++)
        {
            bool midScreen = i < logoCount / 2;
            SpawnFloatingLogo(false, midScreen);
        }

        gupworksLogo.transform.SetAsLastSibling();
        StartCoroutine(ContinuousSpawn());
        yield return null;
    }

    void SpawnFloatingLogo(bool fadeIn, bool midScreen = false)
    {
        GameObject copy = new GameObject("LogoCopy");
        copy.transform.SetParent(logoPanel.transform, false);

        Image img = copy.AddComponent<Image>();
        img.sprite = gupworksLogo.sprite;
        img.preserveAspect = true;
        img.color = new Color(1f, 1f, 1f, 0.5f);

        float size = Random.Range(60f, 280f);
        RectTransform rt = copy.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);

        if (midScreen)
        {
            rt.anchoredPosition = new Vector2(
                Random.Range(-900f, 900f),
                Random.Range(-500f, 500f)
            );
        }
        else if (spawnPath != null)
        {
            rt.anchoredPosition = GetRandomPointOnSpline();
        }
        else
        {
            if (Random.value > 0.5f)
            {
                rt.anchoredPosition = new Vector2(
                    Random.Range(-1600f, -900f),
                    Random.Range(-600f, 600f)
                );
            }
            else
            {
                rt.anchoredPosition = new Vector2(
                    Random.Range(-900f, 900f),
                    Random.Range(600f, 1100f)
                );
            }
        }

        floatingLogos.Add(rt);

        float speed = Mathf.Lerp(160f, 50f, (size - 60f) / (280f - 60f));
        StartCoroutine(DriftLogo(rt, img, speed, fadeIn));
    }

    Vector2 GetRandomPointOnSpline()
    {
        Spline spline = spawnPath.spline;
        int pointCount = spline.GetPointCount();
        if (pointCount < 2) return Vector2.zero;

        int i = Random.Range(0, pointCount - 1);
        Vector3 a = spawnPath.transform.TransformPoint(spline.GetPosition(i));
        Vector3 b = spawnPath.transform.TransformPoint(spline.GetPosition(i + 1));

        Vector3 worldPos = Vector3.Lerp(a, b, Random.value);

        RectTransform canvasRT = logoPanel.transform.parent.GetComponent<RectTransform>();
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPos, null, out Vector2 localPos);
        return localPos;
    }

    bool IsTooCloseToExisting(Vector2 pos, float size)
    {
        float minDist = size * 1.2f;
        foreach (RectTransform rt in floatingLogos)
        {
            if (rt == null) continue;
            if (Vector2.Distance(pos, rt.anchoredPosition) < minDist)
                return true;
        }
        return false;
    }

    private bool keepSpawning = false;

    IEnumerator ContinuousSpawn()
    {
        keepSpawning = true;
        while (keepSpawning)
        {
            yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));
            if (!keepSpawning) yield break;
            SpawnFloatingLogo(true);
            gupworksLogo.transform.SetAsLastSibling();
        }
    }

    IEnumerator DriftLogo(RectTransform rt, Image img, float speed, bool fadeIn)
    {
        if (fadeIn)
        {
            float elapsed = 0f;
            float fadeDuration = 0.5f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                img.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 0.5f, elapsed / fadeDuration));
                yield return null;
            }
        }

        img.color = new Color(1f, 1f, 1f, 0.5f);
        Vector2 drift = new Vector2(speed, -speed);

        while (true)
        {
            if (rt == null) yield break;
            rt.anchoredPosition += drift * Time.deltaTime;
            yield return null;
        }
    }

    void SetFade(float alpha)
    {
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }

    IEnumerator FadeIn(float speed = 2f)
    {
        Color c = fadeImage.color;
        while (c.a > 0f)
        {
            c.a -= Time.deltaTime * speed;
            fadeImage.color = c;
            yield return null;
        }
        c.a = 0f;
        fadeImage.color = c;
    }

    IEnumerator FadeOut()
    {
        Color c = fadeImage.color;
        while (c.a < 1f)
        {
            c.a += Time.deltaTime * 2f;
            fadeImage.color = c;
            yield return null;
        }
        c.a = 1f;
        fadeImage.color = c;
    }

    IEnumerator FadeImageAlpha(Image img, float from, float to, float duration)
    {
        float elapsed = 0f;
        Color c = img.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            img.color = c;
            yield return null;
        }
        c.a = to;
        img.color = c;
    }
}