using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    [Header("Audio Sources")]
    // Two sources for crossfading — one fades out while the other fades in
    public AudioSource sourceA;
    public AudioSource sourceB;

    [Header("Settings")]
    public float fadeTime = 1.5f;

    // Which source is currently active
    private bool isSourceA = true;
    private string currentTrackTag = "";
    private AudioClip currentClip = null;

    void Awake()
    {
        // Persist across all scene loads
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        sourceA.loop = true;
        sourceB.loop = true;
        sourceA.volume = 0f;
        sourceB.volume = 0f;
    }

    // Call this from SceneMusic on each scene to set the track
    // If the tag matches what's already playing, does nothing
    public void PlayTrack(AudioClip clip, string trackTag)
    {
        // Same tag AND same clip — let it keep playing uninterrupted
        if (trackTag == currentTrackTag && clip == currentClip) return;

        currentTrackTag = trackTag;
        currentClip = clip;
        StartCoroutine(CrossFade(clip));
    }

    // Smoothly duck or restore music volume
    public void SetVolume(float targetVolume)
    {
        StopCoroutine("LerpVolume");
        StartCoroutine(LerpVolume(targetVolume));
    }

    IEnumerator LerpVolume(float targetVolume)
    {
        AudioSource active = isSourceA ? sourceB : sourceA;
        float startVolume = active.volume;
        float timer = 0f;
        float duration = 0.5f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            active.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }

        active.volume = targetVolume;
    }

    // Call this to fade out music — used during scene transitions and time travel
    public void FadeOut()
    {
        StartCoroutine(FadeOutCurrent());
    }

    IEnumerator CrossFade(AudioClip newClip)
    {
        AudioSource incoming = isSourceA ? sourceA : sourceB;
        AudioSource outgoing = isSourceA ? sourceB : sourceA;

        incoming.clip = newClip;
        incoming.volume = 0f;
        incoming.Play();

        float timer = 0f;
        float outgoingStart = outgoing.volume;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeTime;
            incoming.volume = t;
            outgoing.volume = Mathf.Lerp(outgoingStart, 0f, t);
            yield return null;
        }

        incoming.volume = 1f;
        outgoing.volume = 0f;
        outgoing.Stop();

        isSourceA = !isSourceA;
    }

    IEnumerator FadeOutCurrent()
    {
        AudioSource active = isSourceA ? sourceB : sourceA;

        float timer = 0f;
        float startVolume = active.volume;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            active.volume = Mathf.Lerp(startVolume, 0f, timer / fadeTime);
            yield return null;
        }

        active.volume = 0f;
        active.Stop();
        currentTrackTag = "";
        currentClip = null;
    }
}