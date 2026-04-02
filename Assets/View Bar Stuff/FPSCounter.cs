using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;

    private float deltaTime = 0f;
    private bool isVisible = false;

    void Awake()
    {
        // Persist across all scene loads
        DontDestroyOnLoad(gameObject);
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        // Toggle with F3
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
        {
            isVisible = !isVisible;
            if (fpsText != null) fpsText.gameObject.SetActive(isVisible);
        }

        if (!isVisible) return;

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1f / deltaTime;

        if (fpsText != null)
            fpsText.text = Mathf.Ceil(fps).ToString() + " FPS";
    }
}