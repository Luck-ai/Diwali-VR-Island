using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(InteractableItem))]
public sealed class ChristmasItemAction : MonoBehaviour
{
    public Transform target;
    public Transform motionPart;
    public Transform secondaryPart;
    public Transform track;
    public ParticleSystem snow;
    public int completedCount;
    public int collisionCount;
    public string lastCollision;
    public bool active;
    InteractableItem item;
    XRGrabInteractable grab;
    Vector3 initialPosition, partPosition, secondPosition;
    Quaternion initialRotation, partRotation, targetRotation;
    float phase, resetAt;
    bool initialized;

    void Awake() { Initialize(); }
    void Initialize()
    {
        if (initialized) return;
        initialized = true;
        item = GetComponent<InteractableItem>(); item.ParseName();
        grab = GetComponent<XRGrabInteractable>();
        initialPosition = transform.localPosition; initialRotation = transform.localRotation;
        if (motionPart) { partPosition = motionPart.localPosition; partRotation = motionPart.localRotation; }
        if (secondaryPart) secondPosition = secondaryPart.localPosition;
        if (target) targetRotation = target.localRotation;
        if (item.eventNumber == 3 && motionPart) motionPart.gameObject.SetActive(false);
    }
    public void Perform()
    {
        Initialize();
        switch (item.eventNumber)
        {
            case 1: active = !active; completedCount++; break;
            case 2: active = !active; completedCount++; break;
            case 3:
                if (target && Vector3.Distance(transform.position, target.position) < 4.5f)
                { active = !active; completedCount++; if (motionPart) motionPart.gameObject.SetActive(true); }
                break;
            case 4: GetComponentInParent<FerrisWheelSpinner>()?.ToggleSpinning(); completedCount++; break;
            case 5: active = !active; completedCount++; break;
            case 6: active = !active; completedCount++; break;
            case 7:
                // Throwing is handled by the XR grab release; trigger recalls the ball.
                if (!grab || !grab.isSelected) Recall();
                break;
            case 8: TryFeed(); break;
            case 9: GetComponentInParent<CarouselSpinner>()?.ToggleSpinning(); completedCount++; break;
            case 10:
                if (snow) snow.Play();
                completedCount++;
                break;
        }
    }
    void Update()
    {
        Initialize();
        float step = Time.deltaTime * 4f;
        switch (item.eventNumber)
        {
            case 1:
                if (motionPart) motionPart.localRotation = Quaternion.Slerp(motionPart.localRotation,
                    partRotation * Quaternion.Euler(active ? -110f : 0f, 0, 0), step);
                if (secondaryPart) secondaryPart.localPosition = Vector3.Lerp(secondaryPart.localPosition,
                    secondPosition + Vector3.up * (active ? 0.52f : 0), step);
                break;
            case 2:
                if (active && track)
                {
                    phase += Time.deltaTime * 0.7f;
                    var center = transform.parent.InverseTransformPoint(track.position);
                    transform.localPosition = center + new Vector3(Mathf.Cos(phase), 0.045f, Mathf.Sin(phase)) * 0.92f;
                    transform.localRotation = Quaternion.Euler(0, -phase * Mathf.Rad2Deg, 0);
                }
                break;
            case 3:
                if (motionPart && motionPart.gameObject.activeSelf)
                    motionPart.localPosition = Vector3.Lerp(motionPart.localPosition,
                        partPosition + Vector3.up * (active ? 0.8f : 0), step);
                break;
            case 5:
                if (motionPart) motionPart.localRotation = Quaternion.Slerp(motionPart.localRotation,
                    partRotation * Quaternion.Euler(active ? 85f : 0f, 0, 0), step);
                if (secondaryPart) secondaryPart.localPosition = Vector3.Lerp(secondaryPart.localPosition,
                    secondPosition + Vector3.forward * (active ? 0.32f : 0), step);
                var glow = item.Part("Glow");
                if (glow) glow.gameObject.SetActive(!active);
                break;
            case 6:
                phase += Time.deltaTime * 1.6f;
                transform.localRotation = Quaternion.Slerp(transform.localRotation,
                    initialRotation * Quaternion.Euler(active ? Mathf.Sin(phase) * 28f : 0, 0, 0), step);
                break;
            case 8:
                if (!active && grab && grab.isSelected) TryFeed();
                if (active && target) target.localRotation = targetRotation * Quaternion.Euler(Mathf.Sin(Time.time * 7f) * 7f, 0, 0);
                break;
        }
        if (resetAt > 0 && Time.time >= resetAt && (!grab || !grab.isSelected))
        { resetAt = 0; Recall(); }
        if (grab && !grab.isSelected && transform.position.y < -8f) Recall();
    }
    void TryFeed()
    {
        if (active || !target) return;
        var renderer = target.GetComponent<Renderer>();
        float distance = renderer ? Vector3.Distance(transform.position, renderer.bounds.ClosestPoint(transform.position)) : Vector3.Distance(transform.position, target.position);
        if (distance > 0.65f) return;
        active = true; completedCount++; resetAt = Time.time + 3;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
    }
    void OnCollisionEnter(Collision collision)
    {
        collisionCount++; lastCollision = collision.collider.name;
        if (!item || item.eventNumber != 7 || !target || (grab && grab.isSelected)) return;
        if (collision.collider.transform != target && !collision.collider.transform.IsChildOf(target)) return;
        if (resetAt > Time.time) return;
        completedCount++; resetAt = Time.time + 1;
        StopAllCoroutines(); StartCoroutine(TargetReact());
    }
    IEnumerator TargetReact()
    {
        var basis = target.localRotation;
        float elapsed = 0;
        while (elapsed < 0.7f)
        {
            elapsed += Time.deltaTime;
            target.localRotation = basis * Quaternion.Euler(0, 0, Mathf.Sin(elapsed * 16) * 9 * (1 - elapsed / 0.7f));
            yield return null;
        }
        target.localRotation = basis;
    }
    public void Recall()
    {
        Initialize();
        if (grab && grab.isSelected) return;
        var body = GetComponent<Rigidbody>();
        if (body && !body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        transform.localPosition = initialPosition; transform.localRotation = initialRotation;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
        active = false;
        if (target && item.eventNumber == 8) target.localRotation = targetRotation;
    }
}
