using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RumbleManager : MonoBehaviour
{
    public static RumbleManager instance;

    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Call this from anywhere — e.g. RumbleManager.instance.Rumble();
    public void Rumble(float lowFreq = 0.2f, float highFreq = 0.15f, float duration = 0.15f)
    {
        if (Gamepad.current == null) return;
        StartCoroutine(DoRumble(lowFreq, highFreq, duration));
    }

    IEnumerator DoRumble(float lowFreq, float highFreq, float duration)
    {
        Gamepad.current.SetMotorSpeeds(lowFreq, highFreq);
        yield return new WaitForSeconds(duration);
        Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}
