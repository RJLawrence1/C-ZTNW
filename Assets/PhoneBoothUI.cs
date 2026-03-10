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

    public GameObject phoneBoothPanel;
    public TextMeshProUGUI phoneDisplay;

    [Header("Buttons")]
    public Button btn1, btn2, btn3, btn4, btn5, btn6, btn7, btn8, btn9, btn0, btnStar, btnPound;
    public Button btnCall, btnClear, btnHangUp;

    [Header("Audio")]
    public AudioSource audioSource;
    // A second dedicated AudioSource just for dial beeps so we can pitch-shift it
    // without affecting the main audio (dial tone, pickup, hangup sounds etc.)
    // Add a second AudioSource component on the same GameObject and drag it here.
    public AudioSource dialBeepSource;
    public AudioClip pickupClip;
    public AudioClip predialingClip; // Plays once before the looping dial tone starts
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
    private bool isProcessing = false; // Locks all buttons while a call is playing out

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        phoneBoothPanel.SetActive(false);
        SetupButtons();
        UpdateDisplay();
    }

    void Update()
    {
        if (!isInPhoneBooth) return;
        if (isProcessing) return;

        // Don't process digit keys on the same frame as Enter to avoid re-dialing the last digit
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

    public void Hide()
    {
        isInPhoneBooth = false;
        isProcessing = false;
        audioSource.Stop();

        if (hangupClip != null)
            audioSource.PlayOneShot(hangupClip);

        phoneBoothPanel.SetActive(false);
        if (currentBooth != null)
            currentBooth.ExitBooth();
        currentBooth = null;
    }

    void DialDigit(string digit)
    {
        // * and # don't count toward the 8-digit limit
        if (digit != "*" && digit != "#" && dialedNumber.Length >= 8) return;

        if (digit != "*" && digit != "#")
            dialedNumber += digit;

        UpdateDisplay();

        // Flash the corresponding button visually so keyboard/controller feel like a real press
        Button btn = GetButtonForDigit(digit);
        if (btn != null)
            StartCoroutine(FlashButton(btn));

        // Pick the right clip for this key and play it through the dedicated beep source
        AudioClip clip = GetDialClip(digit);
        if (clip != null && dialBeepSource != null)
            dialBeepSource.PlayOneShot(clip);
    }

    // Returns the Button component that matches the given digit string
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

    // Briefly triggers the button's pressed visual state so it looks clicked
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

        // Only restart the dial tone if it isn't already playing —
        // avoids the popping noise caused by stopping and restarting mid-playback
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
        // Don't allow hanging up while a call is playing out
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

        // Play the predialing clip (e.g. connecting/ringing sound) before the call response
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
                yield return StartCoroutine(TimeTravel("1957"));
                break;
            case "0824-1987":
                yield return StartCoroutine(TimeTravel("1987"));
                break;
            case "0824-2017":
                yield return StartCoroutine(TimeTravel("2017"));
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

    IEnumerator TimeTravel(string era)
    {
        Hide();
        yield return new WaitForSeconds(0.5f); // Small delay so Hide() can finish cleanly

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
        DialogueLabel.curlyLabel.Say("Think, McFly, Think!");
        yield return new WaitForSeconds(3f);
    }

    IEnumerator BTTF1985()
    {
        Hide();
        DialogueLabel.curlyLabel.Say("1.21 jigawatts...Great Scott.");
        yield return new WaitForSeconds(3f);
        DialogueLabel.zoeyLabel.Say("What the hell is a jigawatt anyway?");
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say("That's a really good question actually,");
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say("I'll have to look into it.");
        yield return new WaitForSeconds(3f);
    }

    IEnumerator BTTF2015()
    {
        Hide();
        DialogueLabel.curlyLabel.Say("Roads? Where we're going we don't need roads.");
        yield return new WaitForSeconds(3f);
    }

    IEnumerator BillTed1989()
    {
        Hide();
        DialogueLabel.curlyLabel.Say("Strange things are afoot in Bayfront.");
        yield return new WaitForSeconds(3f);
    }

    IEnumerator SamMax1993()
    {
        Hide();
        DialogueLabel.curlyLabel.Say("You know, Max, I can't help but think that we have may have tampered with");
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say("the fragile inner workings of this little spaceship we call Earth.");
        yield return new WaitForSeconds(3f);
        DialogueLabel.zoeyLabel.Say("Who's Max?");
        yield return new WaitForSeconds(3f);
        DialogueLabel.curlyLabel.Say("Good question.");
        yield return new WaitForSeconds(3f);
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
        DialogueLabel.curlyLabel.Say("...The hell...?");
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
            DialogueLabel.curlyLabel.Say("...fuck.");
        else
            DialogueLabel.curlyLabel.Say("...");

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