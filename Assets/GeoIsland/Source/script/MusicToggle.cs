using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicToggle : MonoBehaviour
{
    AudioSource source;
    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake=false;
    }
    public void Toggle()
    {
        if (source.isPlaying)
            source.Pause();
        else
            source.Play();
    }
    public void Restart()
    {
        source.Stop();
        source.Play();
    }
}
