using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class ChristmasPropReset : MonoBehaviour
{
    Vector3 position;
    Quaternion rotation;
    Transform originalParent;
    XRGrabInteractable grab;
    Rigidbody body;
    float droppedAt;
    void Awake()
    {
        originalParent = transform.parent;
        position = transform.localPosition; rotation = transform.localRotation;
        grab = GetComponent<XRGrabInteractable>();
        body = GetComponent<Rigidbody>();
        FreezeAtHome();
    }
    void OnEnable()
    {
        if (!grab) grab = GetComponent<XRGrabInteractable>();
        if (grab) { grab.selectEntered.AddListener(Grabbed); grab.selectExited.AddListener(Released); }
    }
    void OnDisable() { if (grab) { grab.selectEntered.RemoveListener(Grabbed); grab.selectExited.RemoveListener(Released); } }
    void Grabbed(SelectEnterEventArgs args) { if (body) body.constraints = RigidbodyConstraints.None; }
    void Released(SelectExitEventArgs args) { droppedAt = Time.time; }
    public void FreezeAtHome()
    {
        if (!body) body = GetComponent<Rigidbody>();
        if (!body) return;
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.constraints = RigidbodyConstraints.FreezeAll;
    }
    void Update()
    {
        if (grab && grab.isSelected) { droppedAt = Time.time; return; }
        if (transform.position.y > -6 && Time.time - droppedAt < 45) return;
        transform.SetParent(originalParent, false);
        transform.localPosition = position; transform.localRotation = rotation;
        FreezeAtHome();
        droppedAt = Time.time;
    }
}
