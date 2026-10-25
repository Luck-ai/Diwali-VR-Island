using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class GeoIslandPhysics : MonoBehaviour
{
    void Start()
    {
        foreach (var item in GetComponentsInChildren<XRGrabInteractable>(true))
        {
            var body = item.GetComponent<Rigidbody>();
            if (!body) continue;
            body.maxLinearVelocity = 3f;
            body.maxDepenetrationVelocity = 1f;
            body.maxAngularVelocity = 8f;
            body.solverIterations = 12;
            body.solverVelocityIterations = 8;
            body.linearDamping = .3f;
            body.angularDamping = 2f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.isKinematic = true;
            body.useGravity = false;
            item.selectExited.AddListener(ReleaseToPhysics);
        }
    }

    static void ReleaseToPhysics(SelectExitEventArgs args)
    {
        if (args.interactableObject.isSelected) return;
        var body = args.interactableObject.transform.GetComponent<Rigidbody>();
        if (!body) return;
        body.isKinematic = false;
        body.useGravity = true;
        // Throwable items (e.g. the rescue ring) keep the throw the grab hands them.
        if (args.interactableObject is XRGrabInteractable grab && grab.throwOnDetach) return;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    void OnDestroy()
    {
        foreach (var item in GetComponentsInChildren<XRGrabInteractable>(true))
            item.selectExited.RemoveListener(ReleaseToPhysics);
    }
}
