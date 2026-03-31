using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PhoneBoothUI : MonoBehaviour
{
    public static PhoneBoothUI instance;
    public static bool isInPhoneBooth = false;
    public static string currentEra = "1987";

    // Set to true before a time travel scene load — tells Start() to spawn at the booth
    public static bool arrivedViaTimeTravel = false;

    public GameObject phoneBoothPanel;
    public TextMeshProUGUI phoneDisplay;

    [Header("Buttons")]
    public Button btn1, btn2, btn3, btn4, btn5, btn6, btn7, btn8, btn9, btn0, btnStar, btnPound;
    public Button btnCall, btnClear, btnHangUp;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource dialBeepSource;
    public AudioClip pickupClip;
    public AudioClip predialingClip;
    public AudioClip hangupClip;
    public AudioClip dialTone;
    public AudioClip invalidNumberClip;
    public AudioClip answeringMachineClip;
    public AudioClip beepClip;
    public AudioClip busySignalClip;

    [Header("Dial Tone Clips (one per key)")]
    public AudioClip dialBeep0;
    public AudioClip dialBeep1;
    public AudioClip dialBeep2;
    public AudioClip dialBeep3;
    public AudioClip dialBeep4;
    public AudioClip dialBeep5;
    public AudioClip dialBeep6;
    public AudioClip dialBeep7;
    public AudioClip dialBeep8;
    public AudioClip dialBeep9;
    public AudioClip dialBeepStar;
    public AudioClip dialBeepPound;

    private string dialedNumber = "";
    private const string PREFIX = "1-650-";
    private PhoneBooth currentBooth;
    private bool isProcessing = false;

    private int sameEraCount = 0;
    private int callOverCount = 0;

    private string[] sameEraLines = {
        "...I'm already here.",
        "That's now. I'm standing in it.",
        "I don't think the machine is supposed to do that.",
        "Yeah, I'm not going anywhere.",
        "...Did I just try to travel to right now?"
    };
    [Header("Same Era Clips")]
    public AudioClip[] sameEraClips = new AudioClip[5];

    private string[] callOverLines = {
        "Zoey, get your ass over here.",
        "Zo, come on, we're going.",
        "Zoey! Booth. Now.",
        "Hey, Zo, move it!",
        "Zoey, quit wandering, we're leaving."
    };
    [Header("Call Over Clips")]
    public AudioClip[] callOverClips = new AudioClip[5];

    [Header("Easter Egg Clips")]
    public AudioClip teleportClip;
    public AudioClip arrivalClip;
    public AudioClip bttf1955_Clip;
    public AudioClip bttf1985_Curly1;
    public AudioClip bttf1985_Zoey;
    public AudioClip bttf1985_Curly2;
    public AudioClip bttf1985_Curly3;
    public AudioClip bttf2015_Clip;
    public AudioClip billTed_Clip;
    public AudioClip samMax_Curly1;
    public AudioClip samMax_Curly2;
    public AudioClip samMax_Zoey;
    public AudioClip samMax_Curly3;
    public AudioClip birthday_Clip;
    public AudioClip invalidNumber_Clip;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        phoneBoothPanel.SetActive(false);
        SetupButtons();
        UpdateDisplay();

        // Always reset on scene load
        isInPhoneBooth = false;

        // Set era from scene name if we didn't arrive via time travel
        if (!arrivedViaTimeTravel)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName.Contains("1957")) currentEra = "1957";
            else if (sceneName.Contains("1987")) currentEra = "1987";
            else if (sceneName.Contains("2017")) currentEra = "2017";
        }

        // Only teleport to booth spawn if we arrived via time travel
        if (arrivedViaTimeTravel)
        {
            arrivedViaTimeTravel = false;

            PhoneBooth booth = FindObjectOfType<PhoneBooth>();
            if (booth != null)
            {
                Transform curlySpawn = booth.transform.Find("CurlyInside");
                Transform zoeySpawn = booth.transform.Find("ZoeyInside");

                CurlyMovement curly = FindObjectOfType<CurlyMovement>();
                ZoeyAI zoey = FindObjectOfType<ZoeyAI>();

                if (curlySpawn != null && curly != null)
                {
                    curly.transform.position = curlySpawn.position;
                    Collider2D curlyCol = curly.GetComponent<Collider2D>();
                    if (curlyCol != null) curlyCol.enabled = false;
                    Rigidbody2D curlyRb = curly.GetComponent<Rigidbody2D>();
                    if (curlyRb != null) curlyRb.simulated = false;
                }

                if (zoeySpawn != null && zoey != null)
                {
                    zoey.transform.position = zoeySpawn.position;
                    Collider2D zoeyCol = zoey.GetComponent<Collider2D>();
                    if (zoeyCol != null) zoeyCol.enabled = false;
                    Rigidbody2D zoeyRb = zoey.GetComponent<Rigidbody2D>();
                    if (zoeyRb != null) zoeyRb.simulated = false;
                }

                // Lock both so ExitBoothSequence controls the order
                if (curly != null) curly.inputLocked = true;
                if (zoey != null) zoey.isPaused = true;

                // Play arrival sound before stepping out
                if (arrivalClip != null)
                    audioSource.PlayOneShot(arrivalClip);

                // Step them out of the booth, then make sure Zoey is fully released
                StartCoroutine(ExitAndReleaseZoey(booth, zoey));
            }
        }
    }

    IEnumerator ExitAndReleaseZoey(PhoneBooth booth, ZoeyAI zoey)
    {
        yield return StartCoroutine(booth.ExitBoothSequence());

        // Make absolutely sure Zoey is unpaused and wandering after the exit sequence
        if (zoey != null)
        {
            zoey.isPaused = false;
            zoey.StopAndStay();
        }
    }

    void Update()
    {
        if (!isInPhoneBooth) return;
        if (isProcessing) return;

        if (!Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) DialDigit("1");
            if (Keyboard.current.digit2Key.wasPressedThisFrame) DialDigit("2");
            if (Keyboard.current.digit3Key.wasPressedThisFrame) DialDigit("3");
            if (Keyboard.current.digit4Key.wasPressedThisFrame) DialDigit("4");
            if (Keyboard.current.digit5Key.wasPressedThisFrame) DialDigit("5");
            if (Keyboard.current.digit6Key.wasPressedThisFrame) DialDigit("6");
            if (Keyboard.current.digit7Key.wasPressedThisFrame) DialDigit("7");
            if (Keyboard.current.digit8Key.wasPressedThisFrame) DialDigit("8");
            if (Keyboard.current.digit9Key.wasPressedThisFrame) DialDigit("9");
            if (Keyboard.current.digit0Key.wasPressedThisFrame) DialDigit("0");
        }
        if (Keyboard.current.enterKey.wasPressedThisFrame) DialCall();
        if (Keyboard.current.backspaceKey.wasPressedThisFrame) ClearNumber();
        if (Keyboard.current.escapeKey.wasPressedThisFrame) HangUp();
    }

    void SetupButtons()
    {
        btn1.onClick.AddListener(() => DialDigit("1"));
        btn2.onClick.AddListener(() => DialDigit("2"));
        btn3.onClick.AddListener(() => DialDigit("3"));
        btn4.onClick.AddListener(() => DialDigit("4"));
        btn5.onClick.AddListener(() => DialDigit("5"));
        btn6.onClick.AddListener(() => DialDigit("6"));
        btn7.onClick.AddListener(() => DialDigit("7"));
        btn8.onClick.AddListener(() => DialDigit("8"));
        btn9.onClick.AddListener(() => DialDigit("9"));
        btn0.onClick.AddListener(() => DialDigit("0"));
        btnStar.onClick.AddListener(() => DialDigit("*"));
        btnPound.onClick.AddListener(() => DialDigit("#"));
        btnCall.onClick.AddListener(() => { StartCoroutine(FlashButton(btnCall)); DialCall(); });
        btnClear.onClick.AddListener(() => { StartCoroutine(FlashButton(btnClear)); ClearNumber(); });
        btnHangUp.onClick.AddListener(() => { StartCoroutine(FlashButton(btnHangUp)); HangUp(); });
    }

    public void Show(PhoneBooth booth)
    {
        isInPhoneBooth = true;
        currentBooth = booth;
        dialedNumber = "";
        UpdateDisplay();
        phoneBoothPanel.SetActive(true);

        // Duck background music while in the booth
        if (MusicManager.instance != null)
            MusicManager.instance.SetVolume(0.2f);

        StartCoroutine(PickupSequence());
    }

    IEnumerator PickupSequence()
    {
        if (pickupClip != null)
        {
            audioSource.loop = false;
            audioSource.PlayOneShot(pickupClip);
            yield return new WaitForSeconds(pickupClip.length);
        }

        if (dialTone != null)
        {
            audioSource.clip = dialTone;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    public void Hide(bool triggerExit = true)
    {
        isInPhoneBooth = false;
        isProcessing = false;
        audioSource.Stop();

        if (hangupClip != null)
            audioSource.PlayOneShot(hangupClip);

        // Restore background music volume
        if (MusicManager.instance != null)
            MusicManager.instance.SetVolume(1f);

        phoneBoothPanel.SetActive(false);

        if (triggerExit && currentBooth != null)
            StartCoroutine(currentBooth.ExitBoothSequence());
        else if (!triggerExit && currentBooth != null)
            currentBooth.ExitBooth();

        currentBooth = null;
    }

    void DialDigit(string digit)
    {
        if (digit != "*" && digit != "#" && dialedNumber.Length >= 8) return;

        if (digit != "*" && digit != "#")
            dialedNumber += digit;

        UpdateDisplay();

        Button btn = GetButtonForDigit(digit);
        if (btn != null)
            StartCoroutine(FlashButton(btn));

        AudioClip clip = GetDialClip(digit);
        if (clip != null && dialBeepSource != null)
            dialBeepSource.PlayOneShot(clip);
    }

    Button GetButtonForDigit(string digit)
    {
        switch (digit)
        {
            case "0": return btn0;
            case "1": return btn1;
            case "2": return btn2;
            case "3": return btn3;
            case "4": return btn4;
            case "5": return btn5;
            case "6": return btn6;
            case "7": return btn7;
            case "8": return btn8;
            case "9": return btn9;
            case "*": return btnStar;
            case "#": return btnPound;
            default: return null;
        }
    }

    IEnumerator FlashButton(Button button)
    {
        var eventData = new PointerEventData(EventSystem.current);

        ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerEnterHandler);
        ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerDownHandler);

        yield return new WaitForSeconds(0.1f);

        ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerExitHandler);
    }

    AudioClip GetDialClip(string digit)
    {
        switch (digit)
        {
            case "0": return dialBeep0;
            case "1": return dialBeep1;
            case "2": return dialBeep2;
            case "3": return dialBeep3;
            case "4": return dialBeep4;
            case "5": return dialBeep5;
            case "6": return dialBeep6;
            case "7": return dialBeep7;
            case "8": return dialBeep8;
            case "9": return dialBeep9;
            case "*": return dialBeepStar;
            case "#": return dialBeepPound;
            default: return null;
        }
    }

    void ClearNumber()
    {
        dialedNumber = "";
        UpdateDisplay();

        if (dialTone != null && (!audioSource.isPlaying || audioSource.clip != dialTone))
        {
            audioSource.clip = dialTone;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void UpdateDisplay()
    {
        if (dialedNumber.Length <= 4)
            phoneDisplay.text = PREFIX + dialedNumber;
        else
            phoneDisplay.text = PREFIX + dialedNumber.Substring(0, 4) + "-" + dialedNumber.Substring(4);
    }

    void HangUp()
    {
        if (isProcessing) return;
        dialedNumber = "";
        Hide();
    }

    void DialCall()
    {
        if (dialedNumber.Length < 8) return;
        string formatted = dialedNumber.Substring(0, 4) + "-" + dialedNumber.Substring(4);
        audioSource.Stop();
        StartCoroutine(DialAndConnect(formatted));
    }

    IEnumerator DialAndConnect(string number)
    {
        isProcessing = true;

        if (predialingClip != null)
        {
            audioSource.loop = false;
            audioSource.PlayOneShot(predialingClip);
            yield return new WaitForSeconds(predialingClip.length);
        }

        yield return StartCoroutine(ProcessCall(number));

        isProcessing = false;
    }

    IEnumerator ProcessCall(string number)
    {
        switch (number)
        {
            case "0824-1957":
                if (currentEra == "1957") yield return StartCoroutine(SameEra());
                else yield return StartCoroutine(TimeTravel("1957"));
                break;
            case "0824-1987":
                if (currentEra == "1987") yield return StartCoroutine(SameEra());
                else yield return StartCoroutine(TimeTravel("1987"));
                break;
            case "0824-2017":
                if (currentEra == "2017") yield return StartCoroutine(SameEra());
                else yield return StartCoroutine(TimeTravel("2017"));
                break;
            case "1026-1955":
                yield return StartCoroutine(BTTF1955());
                break;
            case "1026-1985":
                yield return StartCoroutine(BTTF1985());
                break;
            case "1026-2015":
                yield return StartCoroutine(BTTF2015());
                break;
            case "0217-1989":
                yield return StartCoroutine(BillTed1989());
                break;
            case "1101-1993":
                yield return StartCoroutine(SamMax1993());
                break;
            case "1018-2006":
                yield return StartCoroutine(BirthdayCall());
                break;
            default:
                yield return StartCoroutine(InvalidNumber());
                break;
        }
    }

    IEnumerator SameEra()
    {
        Hide(false);
        int index = sameEraCount % sameEraLines.Length;
        AudioClip clip = (sameEraClips != null && index < sameEraClips.Length) ? sameEraClips[index] : null;
        DialogueLabel.curlyLabel.Say(sameEraLines[index], clip);
        sameEraCount++;
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
    }

    IEnumerator TimeTravel(string era)
    {
        // Close the phone UI first
        Hide(false);

        // Call Zoey over
        ZoeyAI zoey = FindObjectOfType<ZoeyAI>();
        if (zoey != null && currentBooth != null)
        {
            // Find ZoeySpawn on the booth
            Transform zoeySpawn = currentBooth.transform.Find("ZoeySpawn");
            Vector3 destination = zoeySpawn != null ? zoeySpawn.position : currentBooth.transform.position;

            // Curly calls her over
            int callIndex = callOverCount % callOverLines.Length;
            AudioClip callClip = (callOverClips != null && callIndex < callOverClips.Length) ? callOverClips[callIndex] : null;
            DialogueLabel.curlyLabel.Say(callOverLines[callIndex], callClip);
            callOverCount++;
            yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());

            // Zoey hustles to the booth
            zoey.HustleTo(destination);

            // Wait until she arrives, with a timeout of 10 seconds just in case
            float timeout = 10f;
            while (!zoey.hasArrived && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        // Fade to black and hold
        if (SceneTransition.instance != null)
            yield return StartCoroutine(SceneTransition.instance.FadeOutAndHold());

        // Play teleport sound over the black screen
        if (teleportClip != null)
        {
            audioSource.loop = false;
            audioSource.PlayOneShot(teleportClip);
            yield return new WaitForSeconds(teleportClip.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        currentEra = era;
        arrivedViaTimeTravel = true;

        // Hold on black for arrival clip length so it plays before fade in
        if (arrivalClip != null)
            SceneTransition.holdTime = arrivalClip.length;

        switch (era)
        {
            case "1957":
                UnityEngine.SceneManagement.SceneManager.LoadScene("1957Test");
                break;
            case "1987":
                UnityEngine.SceneManagement.SceneManager.LoadScene("1987Test");
                break;
            case "2017":
                UnityEngine.SceneManagement.SceneManager.LoadScene("2017Test");
                break;
        }
    }

    IEnumerator BTTF1955()
    {
        Hide();
        DialogueLabel.curlyLabel.Say("Think, McFly, Think!", bttf1955_Clip);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
    }

    IEnumerator BTTF1985()
    {
        Hide();
        DialogueLabel.curlyLabel.Say("1.21 jigawatts...Great Scott.", bttf1985_Curly1);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("What the hell is a jigawatt anyway?", bttf1985_Zoey);
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("That's a really good question actually,", bttf1985_Curly2);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("I'll have to look into it.", bttf1985_Curly3);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
    }

    IEnumerator BTTF2015()
    {
        Hide();
        DialogueLabel.curlyLabel.Say("Roads? Where we're going we don't need roads.", bttf2015_Clip);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
    }

    IEnumerator BillTed1989()
    {
        Hide();
        DialogueLabel.curlyLabel.Say("Strange things are afoot in Bayfront.", billTed_Clip);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
    }

    IEnumerator SamMax1993()
    {
        Hide();
        DialogueLabel.curlyLabel.Say("You know, Max, I can't help but think that we have may have tampered with", samMax_Curly1);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("the fragile inner workings of this little spaceship we call Earth.", samMax_Curly2);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.zoeyLabel.Say("Who's Max?", samMax_Zoey);
        yield return new WaitUntil(() => !DialogueLabel.zoeyLabel.IsDisplaying());
        yield return new WaitForSeconds(0.3f);
        DialogueLabel.curlyLabel.Say("Good question.", samMax_Curly3);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
    }

    IEnumerator BirthdayCall()
    {
        if (answeringMachineClip != null)
        {
            audioSource.loop = false;
            audioSource.PlayOneShot(answeringMachineClip);
            yield return new WaitForSeconds(answeringMachineClip.length);
        }
        else
            yield return new WaitForSeconds(2f);

        if (beepClip != null)
        {
            audioSource.PlayOneShot(beepClip);
            yield return new WaitForSeconds(beepClip.length);
        }
        else
            yield return new WaitForSeconds(1f);

        if (busySignalClip != null)
        {
            audioSource.clip = busySignalClip;
            audioSource.loop = true;
            audioSource.Play();
            yield return new WaitForSeconds(5f);
            audioSource.Stop();
        }
        else
            yield return new WaitForSeconds(5f);

        Hide();
        DialogueLabel.curlyLabel.Say("...The hell...?", birthday_Clip);
        yield return new WaitUntil(() => !DialogueLabel.curlyLabel.IsDisplaying());
    }

    IEnumerator InvalidNumber()
    {
        if (invalidNumberClip != null)
        {
            audioSource.loop = false;
            audioSource.PlayOneShot(invalidNumberClip);
            yield return new WaitForSeconds(invalidNumberClip.length);
        }
        else
            yield return new WaitForSeconds(3f);

        if (Random.Range(0, 50) == 0)
            DialogueLabel.curlyLabel.Say("...fuck.", invalidNumber_Clip);
        else
            DialogueLabel.curlyLabel.Say("...", invalidNumber_Clip);

        dialedNumber = "";
        UpdateDisplay();

        if (dialTone != null)
        {
            audioSource.clip = dialTone;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
}