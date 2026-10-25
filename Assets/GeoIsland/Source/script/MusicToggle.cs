using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicToggle : MonoBehaviour
{
    AudioSource source;
    bool paused;
    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake=false;
        source.loop = true;
        source.Stop();
    }
    public void Toggle()
    {
        if (source.isPlaying)
        {
            source.Pause();
            paused = true;
        }
        else if (paused)
        {
            source.UnPause();
            paused = false;
        }
        else
            source.Play();
    }
    public void Restart()
    {
        source.Stop();
        source.Play();
        paused = false;
    }
}
