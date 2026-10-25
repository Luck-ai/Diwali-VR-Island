#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class GeoIslandPickups
{
    // Keeps an existing pickup as is; otherwise makes the object a pickup matching the other
    // Geo props (convex collider, kinematic until first grab via GeoIslandPhysics).
    public static XRGrabInteractable EnsureGrab(GameObject target, out bool wasGrabbable)
    {
        var grab = target.GetComponent<XRGrabInteractable>();
        wasGrabbable = grab;
        if (grab) return grab;
        var mesh = target.GetComponent<MeshFilter>();
        if (!mesh || !mesh.sharedMesh) throw new InvalidOperationException(target.name + " has no mesh to grab.");
        foreach (var old in target.GetComponents<Collider>()) Undo.DestroyObjectImmediate(old);
        var collider = Undo.AddComponent<MeshCollider>(target);
        collider.sharedMesh = mesh.sharedMesh;
        collider.convex = true;
        collider.contactOffset = .004f;
        var body = target.GetComponent<Rigidbody>();
        if (!body) body = Undo.AddComponent<Rigidbody>(target);
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        grab = Undo.AddComponent<XRGrabInteractable>(target);
        grab.throwOnDetach = false;
        grab.colliders.Clear();
        grab.colliders.Add(collider);
        EditorUtility.SetDirty(grab);
        return grab;
    }
}
#endif
