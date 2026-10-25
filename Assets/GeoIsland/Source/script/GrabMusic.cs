using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Plays the clip once from the start when the object is grabbed or clicked (any XR
// interactable: a grab for pickups, a simple interactable for fixed objects). It never
// loops, and selecting again while it is still playing lets it finish instead of restarting.
[RequireComponent(typeof(AudioSource))]
public class GrabMusic : MonoBehaviour
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    XRBaseInteractable interactable;
    AudioSource source;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.Stop();
    }

    void OnEnable()
    {
        if (interactable) interactable.selectEntered.AddListener(OnSelect);
    }

    void OnDisable()
    {
        if (interactable) interactable.selectEntered.RemoveListener(OnSelect);
    }

    void OnSelect(SelectEnterEventArgs args)
    {
        if (!clip || source.isPlaying) return;
        source.clip = clip;
        source.volume = volume;
        source.loop = false;
        source.Play();
    }
}
