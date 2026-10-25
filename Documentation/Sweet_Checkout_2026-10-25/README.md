# Sweet shop checkout repair

Superseded by [the reach and loaded-carry verification](../Sweet_Reach_Carry_2026-10-25/README.md). The following describes the earlier layout; it did not test re-grabbing a loaded basket or stationary stocking from the real entrance side.

The counter retains its previous 37.69-degree orientation. It sits beside the cabinet; the cabinet moved 0.5 m forward and 0.25 m sideways. Basket packing and checkout share the authored recessed tray. Snap anchors and the card now follow the counter transform without enlarging the basket.

Selected sweets sit 14 cm above the basket interior floor, reduced from 30 cm. They remain available for swapping. Shelf return behavior is preserved.

Workflow: make a basket, open the cabinet, place a basket in the recess, choose four sweets, select **Checkout** on the small monitor, tap the card on its outlined reader, then select **Share Gift** on the monitor. The large side menu hides at checkout and stays hidden after the card is accepted. **New Gift** restores the activity.

Sharing adds a persistent copy of the chosen basket and sweets beside the central rangoli, outside the raised lamp decoration. The display has no grabbed items, rigidbodies or colliders. Brief sharing sparkles end after four seconds; the gift remains.

## Verification

`Play_Mode.json`: 44 focused checks passed in Unity desktop Play Mode. Actual queued NearFar grips, XR UI trigger dispatch, a loose basket release, card touch, monitor checkout/share/reset, menu clearance from three viewpoints, and the central reward were exercised. Four previously verified sweet placements were prepared through `AddSweet`; this run did not repeat the full stocking workflow or use a physical Quest headset.

Visuals were inspected after the final placement changes: `Packing.png`, `Card-accepted.png`, and `Central-gift.png`. A separate geometric check confirmed five basket support points on the authored tray and no counter overlap with the cabinet or walls.
