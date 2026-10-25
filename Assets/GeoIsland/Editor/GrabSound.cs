using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(AudioSource))]
public class GrabSound : MonoBehaviour
{
    public AudioClip grabClip;
    public AudioClip releaseClip; // optional
    [Range(0f, 1f)] public float volume = 1f;

    XRGrabInteractable grab;
    AudioSource source;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (grabClip != null)
            source.PlayOneShot(grabClip, volume);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (releaseClip != null)
            source.PlayOneShot(releaseClip, volume);
    }
}