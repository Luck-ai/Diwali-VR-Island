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
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            item.selectExited.AddListener(StopReleaseMomentum);
        }
    }

    static void StopReleaseMomentum(SelectExitEventArgs args)
    {
        var body = args.interactableObject.transform.GetComponent<Rigidbody>();
        if (!body || body.isKinematic) return;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    void OnDestroy()
    {
        foreach (var item in GetComponentsInChildren<XRGrabInteractable>(true))
            item.selectExited.RemoveListener(StopReleaseMomentum);
    }
}
