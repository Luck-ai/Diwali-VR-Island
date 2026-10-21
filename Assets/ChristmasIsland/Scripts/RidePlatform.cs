using UnityEngine;

[DefaultExecutionOrder(110)]
public class RidePlatform : MonoBehaviour
{
    public float rayLength = 3f;
    Matrix4x4 previousMatrix;
    bool riding;
    readonly RaycastHit[] hits = new RaycastHit[32];
    void OnDisable() { riding = false; }
    void LateUpdate()
    {
        if (!PlayerRig.Origin || !PlayerRig.Camera || RideSeat.current) { riding = false; return; }
        var feet = PlayerRig.Feet;
        int count = Physics.RaycastNonAlloc(PlayerRig.Camera.transform.position, Vector3.down, hits,
            rayLength, ~0, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        Collider floor = null;
        Vector3 floorPoint = Vector3.zero;
        for(int i=0;i<count;i++)
        {
            var hit = hits[i];
            if(hit.collider.transform.IsChildOf(PlayerRig.Origin) || hit.distance >= nearest) continue;
            nearest = hit.distance; floor = hit.collider; floorPoint = hit.point;
        }
        bool onDeck = floor && floor.transform.IsChildOf(transform) && Mathf.Abs(feet.y-floorPoint.y)<.5f;
        if (riding && onDeck)
        {
            var carried = transform.localToWorldMatrix.MultiplyPoint3x4(previousMatrix.inverse.MultiplyPoint3x4(feet));
            // Ignore a teleport or a deliberate step away from the deck.
            if ((carried - feet).sqrMagnitude < 2.25f) PlayerRig.MoveBy(carried - feet);
        }
        riding = onDeck;
        previousMatrix = transform.localToWorldMatrix;
    }
}
