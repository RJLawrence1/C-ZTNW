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
    public AudioClip bootAudioClip; // 17 second boot sound

    [Header("OS Load Screen")]
    public GameObject osPanel;
    public TextMeshProUGUI osText;
    public Image osLoadBar;

    [Header("Logo Spawn Path")]
    public SpriteShapeController spawnPath;

    [Header("Logo Screen")]
    public GameObject logoPanel;
    public Image starfield;
    public Image gupworksLogo;          // The big centered logo — stays put
    public float centerLogoSize = 350f; // Size of the center logo

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip logoCue;

    [Header("Fade")]
    public Image fadeImage;

    [Header("Timing")]
    public float charDelay = 0.03f;     // Speed of text typewriter effect
    public float lineDelay = 0.4f;      // Pause between lines
    public float logoScreenTime = 6f;   // How long the logo screen plays
    public int logoCount = 14;          // How many logos float around (12-16)

    private List<RectTransform> floatingLogos = new List<RectTransform>();

    void Start()
    {
        // Hide everything at start
        devNotePanel.SetActive(false);
        bootPanel.SetActive(false);
        osPanel.SetActive(false);
        logoPanel.SetActive(false);

        // Start fully faded to black
        SetFade(1f);

        StartCoroutine(RunIntro());
    }

    IEnumerator ShowDevNote()
    {
        devNotePanel.SetActive(true);
        devNoteText.color = new Color(1f, 1f, 1f, 1f);
        devNoteText.text = "";

        // Fade in from black first
        yield return StartCoroutine(FadeIn());

        // Small delay so any keypress from starting Play mode doesn't instantly skip
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
            if (line == null)
            {
                devNoteText.text += "\n";
                // During paragraph pause, check for skip
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

        // If skipped mid-way, dump the rest of the text instantly
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

            // Wait for one more keypress to dismiss after showing full text
            yield return new WaitForSeconds(0.3f);
            yield return new WaitUntil(() => Input.anyKeyDown || Mouse.current.leftButton.wasPressedThisFrame);
        }
        else
        {
            // Fully typed — hold then wait for dismiss
            yield return new WaitForSeconds(1f);
            yield return new WaitUntil(() => Input.anyKeyDown || Mouse.current.leftButton.wasPressedThisFrame);
        }

        // Fade out
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

    IEnumerator RunIntro()
    {
        // Dev note first — fade in, wait for keypress, fade out
        yield return StartCoroutine(ShowDevNote());

        // Start boot audio immediately — plays across both boot and OS screens
        if (bootAudioSource != null && bootAudioClip != null)
        {
            bootAudioSource.clip = bootAudioClip;
            bootAudioSource.Play();
        }

        // Activate logo panel in background BEFORE boot screen — logos start
        // drifting immediately and will be well spread out by the time we reveal them
        logoPanel.SetActive(true);
        Color starColor = starfield.color;
        starColor.a = 1f;
        starfield.color = starColor;
        gupworksLogo.color = new Color(1f, 1f, 1f, 1f);
        StartCoroutine(FloatLogo());

        // Start fully black then instantly cut to boot screen — no fade
        SetFade(1f);
        bootPanel.SetActive(true);
        bootText.text = "";
        SetFade(0f);

        // Play BIOS POST text
        yield return StartCoroutine(PlayBootSequence());

        // Hard cut to OS panel — no fade, audio keeps playing
        bootPanel.SetActive(false);
        osPanel.SetActive(true);
        osText.text = "";
        osLoadBar.fillAmount = 0f;

        yield return StartCoroutine(PlayOSSequence());

        yield return new WaitForSeconds(0.5f);

        // Fade to black — logos have been drifting this whole time behind the scenes
        yield return StartCoroutine(FadeOut());
        osPanel.SetActive(false);

        // Start fade in and music cue together — music kicks in 0.5s into the fade
        StartCoroutine(FadeIn(0.5f));
        yield return new WaitForSeconds(0.5f);

        if (musicSource != null && logoCue != null)
        {
            musicSource.clip = logoCue;
            musicSource.Play();
        }

        // Wait for fade to finish (fade takes ~2s total, we already waited 0.5s)
        yield return new WaitForSeconds(1.5f);

        // Wait for the logo screen to finish
        yield return new WaitForSeconds(logoScreenTime);

        // Stop spawning new logos and fade out
        keepSpawning = false;
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(nextScene);
    }

    IEnumerator PlayBootSequence()
    {
        bootText.color = new Color(1f, 1f, 1f, 1f);
        bootText.text = "";

        // Wait 4 seconds — machine click and whirr plays first
        yield return new WaitForSeconds(4f);

        // Block 1 — BIOS header and CPU/RAM (appears at 4s mark)
        bootText.text =
            "GupWorks Interactive BIOS v1.9.87, An Energy Star Ally\n" +
            "Copyright (C) 1994 GupWorks Interactive. All rights reserved. Probably.\n" +
            "\n" +
            "(GW-1994) GupTech Pentium-G PCIset(TM)\n" +
            "\n" +
            "PENTIUM-G CPU at 66MHz\n";

        // Animated RAM count — runs from ~4s to ~6s
        yield return StartCoroutine(CountRAM());

        yield return new WaitForSeconds(0.3f);

        // Block 2 — PnP and IDE detection
        bootText.text +=
            "\n" +
            "GupWorks Plug and Play BIOS Extension  v1.0A\n" +
            "Copyright (C) 1994, GupWorks Interactive\n" +
            "  Detecting IDE Primary Master   ... CURLY-HDD\n" +
            "  Detecting IDE Primary Slave    ... GUP-CDROM-4X\n" +
            "  Detecting IDE Secondary Master ... None\n" +
            "  Detecting IDE Secondary Slave  ... None\n";

        yield return new WaitForSeconds(0.5f);

        // Wait until 13s mark — transition to OS panel
        // We're roughly at 7-8s after BIOS + IDE blocks, wait ~5 more seconds
        yield return new WaitForSeconds(5f);

        // Block 3 — two column hardware table (appears at ~13s mark)
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

        // DMI dots and final line — finishes right around 17s
        yield return StartCoroutine(DMIVerify());

        bootText.text += "Starting GupWorks OS v1.9.87...\n";
        yield return new WaitForSeconds(0.3f);

        bootText.text += "\n\nPress <b>DEL</b> to enter SETUP\n";
        bootText.text += "12/10/94-GW1994,GUP8669-1A94GW2BC-00";

        // Hold until audio finishes (total ~17s)
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
        // OS panel only has ~4 seconds before logo — keep it snappy
        osText.color = new Color(0f, 1f, 0.3f, 1f); // green text for OS
        osText.text =
            "GupWorks OS v1.9.87\n" +
            "Copyright (C) 1994 GupWorks Interactive\n" +
            "\n";

        yield return new WaitForSeconds(0.4f);

        osText.text +=
            "Loading CURLY_HENDERSON.EXE...  OK\n" +
            "Loading ZOEY.DLL...             OK\n" +
            "Loading PHONE_BOOTH.SYS...      OK\n" +
            "Loading TIME_TRAVEL.EXE...      OK\n" +
            "Initialising BAYFRONT...        OK\n";

        yield return new WaitForSeconds(0.4f);

        osText.text += "\nAll systems nominal. Probably.\n";

        yield return new WaitForSeconds(0.5f);

        // Animate load bar quickly
        float t = 0f;
        while (t < 1f)
        {
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
        // Set up the big centered logo — fully visible, fadeImage covers it
        RectTransform centerRT = gupworksLogo.rectTransform;
        centerRT.anchoredPosition = Vector2.zero;
        centerRT.sizeDelta = new Vector2(centerLogoSize, centerLogoSize);
        gupworksLogo.preserveAspect = true;
        gupworksLogo.color = new Color(1f, 1f, 1f, 1f);

        // Spawn initial batch — half mid-screen already in motion, half coming from top-left
        for (int i = 0; i < logoCount; i++)
        {
            bool midScreen = i < logoCount / 2;
            SpawnFloatingLogo(false, midScreen);
        }

        // Make sure center logo is on top
        gupworksLogo.transform.SetAsLastSibling();

        // Keep spawning new logos continuously
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
            // Fallback — left edge or top edge
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

        // Pick a random segment
        int i = Random.Range(0, pointCount - 1);
        Vector3 a = spawnPath.transform.TransformPoint(spline.GetPosition(i));
        Vector3 b = spawnPath.transform.TransformPoint(spline.GetPosition(i + 1));

        // Random point along that segment
        Vector3 worldPos = Vector3.Lerp(a, b, Random.value);

        // Convert world position to canvas local position
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
        // Fade in to half alpha if this is a continuously spawned logo
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

        // Ensure alpha is exactly 0.5 after fade (or from spawn)
        img.color = new Color(1f, 1f, 1f, 0.5f);

        // Always drift toward bottom-right
        Vector2 drift = new Vector2(speed, -speed);

        while (true)
        {
            if (rt == null) yield break;
            rt.anchoredPosition += drift * Time.deltaTime;
            yield return null;
        }
    }

    // ---- Fade helpers ----

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