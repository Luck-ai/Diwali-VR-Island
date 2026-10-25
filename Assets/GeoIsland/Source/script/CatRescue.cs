using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// The drowning cat: meows for help on a timer under a "HELP ME" sign. When the ring is
// dropped or thrown onto it, the cat jumps to its rescue spot, the sign disappears, and it
// spins to the happy song for a while before settling.
[RequireComponent(typeof(AudioSource))]
public class CatRescue : MonoBehaviour
{
    public XRGrabInteractable ring;
    public GameObject helpText;
    public AudioClip meowClip;
    public float meowInterval = 2f;
    public AudioClip happyClip;
    public float happySeconds = 10f;
    public float spinSpeed = 180f;
    public Vector3 spinAxis = Vector3.forward;
    public bool keepSpinningAfterSong;
    public float rescueRadius = .6f;
    public Vector3 rescueLocalPosition = new Vector3(111.3f, 2.7f, 64.1f);
    public Vector3 rescueLocalEuler = new Vector3(0f, -122.094f, 0f);

    AudioSource source;
    Renderer catRenderer, ringRenderer;
    bool ringReleased, rescued;
    float meowTimer, happyTimer;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        catRenderer = GetComponent<Renderer>();
        if (ring) ringRenderer = ring.GetComponentInChildren<Renderer>();
    }

    void OnEnable()
    {
        if (ring) ring.selectExited.AddListener(OnRingReleased);
    }

    void OnDisable()
    {
        if (ring) ring.selectExited.RemoveListener(OnRingReleased);
    }

    // Only a ring that has left a hand counts, so it cannot rescue the cat by just
    // starting near it.
    void OnRingReleased(SelectExitEventArgs args) => ringReleased = true;

    void Update()
    {
        if (!rescued)
        {
            meowTimer -= Time.deltaTime;
            if (meowTimer <= 0f && meowClip)
            {
                meowTimer = meowInterval;
                source.clip = meowClip;
                source.Play();
            }
            if (RingAtCat()) Rescue();
            return;
        }
        if (happyTimer <= 0f && !keepSpinningAfterSong) return;
        transform.Rotate(spinAxis.normalized, spinSpeed * Time.deltaTime, Space.Self);
        if (happyTimer <= 0f) return;
        happyTimer -= Time.deltaTime;
        if (happyTimer > 0f) return;
        source.Stop();
        if (!keepSpinningAfterSong) transform.localEulerAngles = rescueLocalEuler;
    }

    bool RingAtCat()
    {
        if (!ring || !ringReleased || ring.isSelected || !catRenderer || !ringRenderer) return false;
        var reach = catRenderer.bounds;
        reach.Expand(rescueRadius * 2f);
        return reach.Intersects(ringRenderer.bounds);
    }

    void Rescue()
    {
        rescued = true;
        var body = GetComponent<Rigidbody>();
        if (body)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
        transform.localPosition = rescueLocalPosition;
        transform.localEulerAngles = rescueLocalEuler;
        if (helpText) helpText.SetActive(false);
        source.Stop();
        happyTimer = happySeconds;
        if (!happyClip) return;
        source.clip = happyClip;
        source.loop = true;
        source.Play();
    }
}
