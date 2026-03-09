using UnityEngine;
using TMPro;

public class HotspotLabel : MonoBehaviour
{
    public static HotspotLabel instance;
    public TextMeshProUGUI labelText;
    public GameObject labelCanvas;

    private bool shownThisFrame = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        labelCanvas.SetActive(false);
    }

    void LateUpdate()
    {
        if (!shownThisFrame)
            labelCanvas.SetActive(false);

        shownThisFrame = false;
    }

    public void Show(string name, Vector3 worldPosition)
    {
        // Don't show hotspot labels while the phone booth UI is open
        if (PhoneBoothUI.isInPhoneBooth) return;

        if (DialogueLabel.curlyLabel != null && DialogueLabel.curlyLabel.IsDisplaying()) return;
        if (DialogueLabel.zoeyLabel != null && DialogueLabel.zoeyLabel.IsDisplaying()) return;

        labelCanvas.SetActive(true);
        labelText.text = name;
        labelCanvas.transform.position = new Vector3(worldPosition.x, worldPosition.y + 1f, 0f);
        shownThisFrame = true;
    }
}