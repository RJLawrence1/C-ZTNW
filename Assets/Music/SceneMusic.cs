using UnityEngine;
using UnityEngine.Audio;

public class SceneMusic : MonoBehaviour
{
    [Tooltip("The music clip to play in this scene")]
    public AudioClip musicClip;

    [Tooltip("Unique tag for this track — scenes sharing the same tag will not restart the music")]
    public string trackTag = "";

    [Header("Ambience")]
    [Tooltip("The ambient sound clip to loop in this scene — leave blank for no ambience")]
    public AudioClip ambienceClip;

    [Range(0f, 1f)]
    public float ambienceVolume = 0.4f;

    [Tooltip("Drag your SFX AudioMixerGroup here so the SFX slider controls ambient volume")]
    public AudioMixerGroup sfxMixerGroup;

    private AudioSource ambienceSource;

    void Start()
    {
        // Music
        if (MusicManager.instance != null && musicClip != null)
            MusicManager.instance.PlayTrack(musicClip, trackTag);

        // Ambience
        if (ambienceClip != null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.clip = ambienceClip;
            ambienceSource.loop = true;
            ambienceSource.volume = ambienceVolume;
            ambienceSource.playOnAwake = false;
            if (sfxMixerGroup != null)
                ambienceSource.outputAudioMixerGroup = sfxMixerGroup;
            ambienceSource.Play();
        }
    }

    void OnDestroy()
    {
        if (ambienceSource != null)
            ambienceSource.Stop();
    }
}