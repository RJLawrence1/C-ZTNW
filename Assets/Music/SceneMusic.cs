using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [Tooltip("The music clip to play in this scene")]
    public AudioClip musicClip;

    [Tooltip("Unique tag for this track — scenes sharing the same tag will not restart the music")]
    public string trackTag = "";

    void Start()
    {
        if (MusicManager.instance == null) return;
        if (musicClip == null) return;

        MusicManager.instance.PlayTrack(musicClip, trackTag);
    }
}
