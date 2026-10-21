using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Utilities;
using UnityEngine;

public static class PlayerRig
{
    static XROrigin cached;
    public static XROrigin Rig
    {
        get
        {
            if (!cached) cached = Object.FindFirstObjectByType<XROrigin>();
            return cached;
        }
    }
    public static Transform Origin => Rig ? Rig.transform : null;
    public static Camera Camera => Rig && Rig.Camera ? Rig.Camera : UnityEngine.Camera.main;
    public static Vector3 Feet => Camera ? new Vector3(Camera.transform.position.x,
        Rig ? Rig.GetCameraFloorWorldPosition().y : Origin.position.y, Camera.transform.position.z) : Vector3.zero;
    public static void MoveBy(Vector3 delta)
    {
        if (!Origin) return;
        var cc = Origin.GetComponent<CharacterController>();
        bool restore = cc && cc.enabled;
        if (restore) cc.enabled = false;
        Origin.position += delta;
        if (restore) cc.enabled = true;
    }
    public static void MoveFeetTo(Vector3 destination) { MoveBy(destination - Feet); }
}
