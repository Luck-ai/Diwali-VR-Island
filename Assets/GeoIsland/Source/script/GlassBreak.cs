using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// A glass case that locks the pickups inside it. Once the breaker has been picked up and
// touches the glass, the break sound plays, the glass disappears and the pickups unlock.
public class GlassBreak : MonoBehaviour
{
    public XRGrabInteractable breaker;
    public XRGrabInteractable[] lockedInside;
    public AudioClip breakClip;
    public GameObject hint; // optional sign above the glass, hidden once it breaks
    public float touchMargin = .02f;
    Renderer glass, breakerRenderer;
    bool breakerUsed;

    void Awake()
    {
        glass = GetComponent<Renderer>();
        if (breaker) breakerRenderer = breaker.GetComponentInChildren<Renderer>();
        foreach (var item in lockedInside)
            if (item) item.enabled = false;
    }

    void Update()
    {
        if (!breaker || !glass || !breakerRenderer) return;
        // Only a breaker someone has handled counts, so it cannot break the glass just by
        // starting next to it.
        if (breaker.isSelected) breakerUsed = true;
        if (!breakerUsed) return;
        var reach = glass.bounds;
        reach.Expand(touchMargin * 2f);
        if (reach.Intersects(breakerRenderer.bounds)) Break();
    }

    void Break()
    {
        foreach (var item in lockedInside)
            if (item) item.enabled = true;
        if (breakClip) AudioSource.PlayClipAtPoint(breakClip, glass.bounds.center);
        if (hint) hint.SetActive(false);
        gameObject.SetActive(false);
    }
}
