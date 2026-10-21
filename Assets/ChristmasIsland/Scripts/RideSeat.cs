using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
[DefaultExecutionOrder(100)]
public class RideSeat : MonoBehaviour
{
    public Vector3 seatOffset = new Vector3(0f, -2.1f, 0f);
    public Transform exitPoint;
    public static RideSeat current;
    XRSimpleInteractable xr;
    Vector3 previousSeat, previousFeet;
    void Awake() { xr = GetComponent<XRSimpleInteractable>(); }
    void OnEnable() { if (!xr) xr = GetComponent<XRSimpleInteractable>(); xr.selectEntered.AddListener(OnSelect); }
    void OnDisable() { if (xr) xr.selectEntered.RemoveListener(OnSelect); if (current == this) GetOut(); }
    void OnSelect(SelectEnterEventArgs args) { if (current == this) ExitToPlatform(); else Board(); }
    public void Board()
    {
        if (!PlayerRig.Origin) return;
        current = this;
        previousSeat = transform.TransformPoint(seatOffset);
        PlayerRig.MoveFeetTo(previousSeat);
        previousFeet = PlayerRig.Feet;
    }
    public static void GetOut() { current = null; }
    public void ExitToPlatform()
    {
        GetOut();
        if (exitPoint) PlayerRig.MoveFeetTo(exitPoint.position);
    }
    void LateUpdate()
    {
        if (current != this || !PlayerRig.Origin) return;
        if ((PlayerRig.Feet - previousFeet).sqrMagnitude > 1.3f) { GetOut(); return; }
        var point = transform.TransformPoint(seatOffset);
        PlayerRig.MoveBy(point - previousSeat);
        previousSeat = point;
        previousFeet = PlayerRig.Feet;
    }
}
