using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class ChristmasPropReset : MonoBehaviour
{
    Vector3 position;
    Quaternion rotation;
    Transform originalParent;
    XRGrabInteractable grab;
    float droppedAt;
    void Awake()
    {
        originalParent = transform.parent;
        position = transform.localPosition; rotation = transform.localRotation;
        grab = GetComponent<XRGrabInteractable>();
    }
    void OnEnable() { if (!grab) grab = GetComponent<XRGrabInteractable>(); if(grab) grab.selectExited.AddListener(Released); }
    void OnDisable() { if(grab) grab.selectExited.RemoveListener(Released); }
    void Released(SelectExitEventArgs args) { var body=GetComponent<Rigidbody>(); if(body) body.isKinematic=false; droppedAt=Time.time; }
    void Update()
    {
        if (grab && grab.isSelected) { droppedAt = Time.time; return; }
        if (transform.position.y > -6 && Time.time - droppedAt < 45) return;
        var body = GetComponent<Rigidbody>();
        if (body && !body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        transform.SetParent(originalParent, false);
        transform.localPosition = position; transform.localRotation = rotation;
        droppedAt = Time.time;
    }
}
