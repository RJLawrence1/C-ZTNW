using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class GamepadNotification : MonoBehaviour
{
    public TextMeshProUGUI notificationText;
    public float displayTime = 3f;
    public float fadeTime = 0.5f;

    private Coroutine activeCoroutine;
    private bool wasConnected = false;

    void Start()
    {
        notificationText.alpha = 0f;
        wasConnected = Gamepad.current != null;

        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!(device is Gamepad)) return;

        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
            ShowMessage("Gamepad connected.");
        else if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            ShowMessage("Gamepad disconnected.");
    }

    void ShowMessage(string message)
    {
        notificationText.text = message;

        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        activeCoroutine = StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        // Fade in
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            notificationText.alpha = Mathf.Clamp01(t / fadeTime);
            yield return null;
        }
        notificationText.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(displayTime);

        // Fade out
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            notificationText.alpha = Mathf.Clamp01(1f - (t / fadeTime));
            yield return null;
        }
        notificationText.alpha = 0f;
    }
}