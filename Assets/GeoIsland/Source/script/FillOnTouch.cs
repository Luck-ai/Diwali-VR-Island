using UnityEngine;

// Fills a container once it touches the source (e.g. a mug against the keg): the hidden
// liquid grows up from the bottom and the fill sound plays once. It stays full after that.
public class FillOnTouch : MonoBehaviour
{
    public Renderer source;
    public Transform liquid; // pivot at the bottom of the liquid, so scaling Y fills upward
    public AudioClip fillClip;
    public float touchMargin = .02f;
    public float fillSeconds = .6f;
    Renderer container;
    Vector3 fullScale;
    float fillTime = -1f;

    void Awake()
    {
        container = GetComponent<Renderer>();
        fullScale = liquid.localScale;
        liquid.gameObject.SetActive(false);
    }

    void Update()
    {
        if (fillTime < 0f)
        {
            if (!source || !container) return;
            var reach = source.bounds;
            reach.Expand(touchMargin * 2f);
            if (!reach.Intersects(container.bounds)) return;
            fillTime = 0f;
            liquid.localScale = new Vector3(fullScale.x, 0f, fullScale.z);
            liquid.gameObject.SetActive(true);
            if (fillClip) AudioSource.PlayClipAtPoint(fillClip, transform.position);
        }
        if (fillTime >= fillSeconds) return;
        fillTime = Mathf.Min(fillTime + Time.deltaTime, fillSeconds);
        var t = fillSeconds > 0f ? fillTime / fillSeconds : 1f;
        liquid.localScale = new Vector3(fullScale.x, fullScale.y * t, fullScale.z);
    }
}
