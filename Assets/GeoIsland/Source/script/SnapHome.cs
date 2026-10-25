using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Remembers where a pickup starts (or uses a set target pose instead). Dropping it within
// snapDistance of that spot puts it there exactly and holds it until it is grabbed again.
[RequireComponent(typeof(XRGrabInteractable))]
public class SnapHome : MonoBehaviour
{
    public float snapDistance = .3f;
    public AudioClip snapClip; // optional
    [Tooltip("Snap to the pose below (relative to the parent) instead of the start pose.")]
    public bool useTargetPose;
    public Vector3 targetLocalPosition;
    public Vector3 targetLocalEuler;
    public UnityEvent onSnapped;
    XRGrabInteractable grab;
    Rigidbody body;
    Vector3 homePosition;
    Quaternion homeRotation;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        body = GetComponent<Rigidbody>();
        homePosition = transform.position;
        homeRotation = transform.rotation;
        if (useTargetPose && transform.parent)
        {
            homePosition = transform.parent.TransformPoint(targetLocalPosition);
            homeRotation = transform.parent.rotation * Quaternion.Euler(targetLocalEuler);
        }
    }

    void OnEnable()
    {
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (grab.isSelected || Vector3.Distance(transform.position, homePosition) > snapDistance) return;
        StartCoroutine(SnapNextFrame());
    }

    // GeoIslandPhysics turns gravity on in the same release event, so the snap waits a
    // frame to make sure it has the final say.
    IEnumerator SnapNextFrame()
    {
        yield return null;
        if (grab.isSelected) yield break;
        if (body)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
        }
        transform.SetPositionAndRotation(homePosition, homeRotation);
        if (snapClip) AudioSource.PlayClipAtPoint(snapClip, homePosition);
        onSnapped.Invoke();
    }
}
