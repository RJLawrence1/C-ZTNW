using UnityEngine;
using System.Collections.Generic;

public class CutsceneTrigger : MonoBehaviour
{
    public enum TriggerType { OnZoneEnter, OnStart, Manual }

    [Header("Trigger Settings")]
    public TriggerType triggerType = TriggerType.OnZoneEnter;
    public Cutscene cutscene;

    [Tooltip("If true, this cutscene will only fire once and never again.")]
    public bool oneShot = true;

    // Persists across scene loads for the whole session
    public static HashSet<string> playedCutscenes = new HashSet<string>();

    void Start()
    {
        if (triggerType == TriggerType.OnStart)
            TryPlay();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerType != TriggerType.OnZoneEnter) return;
        if (!other.CompareTag("Player")) return;
        TryPlay();
    }

    // Call this from code or an UnityEvent to fire manually
    public void TryPlay()
    {
        if (cutscene == null) return;
        if (oneShot && playedCutscenes.Contains(cutscene.name)) return;
        if (CutsceneManager.isPlaying) return;

        if (oneShot)
            playedCutscenes.Add(cutscene.name);

        CutsceneManager.instance.PlayCutscene(cutscene);
    }

    // Reset so it can play again — useful for repeatable cutscenes
    public void Reset()
    {
        if (cutscene != null)
            playedCutscenes.Remove(cutscene.name);
    }
}