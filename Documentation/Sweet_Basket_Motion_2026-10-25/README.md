# Basket movement fix — 25 October 2026

The previous carry code limited the basket to a height below the player's head and used only the controller position. Raising the hand above that limit or aiming the wrist therefore left the basket fixed in place. Earlier clearance checks did not assert that these motions actually moved the basket.

Basket carrying now follows the XR interaction attachment point, including controller translation, wrist aiming and far-grab distance adjustment. The basket remains upright. Horizontal controller/head clearance replaces the fixed vertical ceiling. A floor limit keeps the mesh above the ground, and release still settles it into Fill Basket or Checkout. Both shelf baskets use the same behavior. The wall layout is unchanged.

## Verification

Fresh desktop Play Mode: **90 checks passed, 0 failed** (`Play_Mode.json`). Inputs are queued NearFar grip/trigger actions; no physical Quest test was performed.

New movement assertions cover both the loaded and empty basket: lift the hand, move sideways, aim the wrist upward, aim sideways, and adjust the far attachment distance. These assert a measurable basket displacement, in addition to selection/clearance.

The existing workflow also passed: initial shelf pickup, filling four sweets, returning and reselecting a sweet, moving a full basket to the separate Checkout target, card acceptance and Share Gift. Carry clearance was checked at three head heights with both controller sides and two hand elevations. Stocking through payment used one standing position and controller origins within 0.7 m of the head.

- loaded basket follows vertical hand movement: lift=0.402
- loaded basket follows sideways hand movement: side=0.384
- loaded basket follows upward wrist aiming: lift=0.494
- loaded basket follows sideways wrist aiming: side=0.652
- loaded basket follows far grab distance adjustment: movement=0.286
- second empty basket follows vertical hand movement: lift=0.436
- second empty basket follows sideways hand movement: side=0.378
- second empty basket follows upward wrist aiming: lift=0.499
- second empty basket follows sideways wrist aiming: side=0.631
- second empty basket follows far grab distance adjustment: movement=0.321
