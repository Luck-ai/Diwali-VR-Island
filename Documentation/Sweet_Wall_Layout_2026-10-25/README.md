# Sweet shop wall layout — 25 October 2026

Restored the user-marked arrangement: rack near the entrance and counter beside it along the left wall. Both face the open shop floor. The counter retains its original orientation; the baskets remain on their wall shelves.

The native counter recess now has two separate zones: amber **Fill basket** and green **Checkout**. Both have permanent surface pads and front labels. Hold the full basket, point at Checkout and release when its green preview appears. Aiming selects the correct adjacent zone; the generous settling margin remains. Card acceptance and Share Gift use the original small counter monitor, while the large activity menu stays out of the packing/payment view.

## Verification

- Fresh desktop Play Mode: **80 checks passed, 0 failed**. See `Play_Mode.json`.
- Actual queued NearFar grip/trigger input: start, open cabinet, switch opening, select Barfi/Modak/Gujiya, fill four slots, swap a selected sweet back to its source shelf, reselect it, carry the full basket, release into the separate Checkout zone, tap the card and Share Gift.
- Stocking, swapping, checkout and payment used a fixed head position. Controller origins stayed within 0.7 m of the head, with a 3 m ray limit. Initial collection of the shelf basket was tested near its shelf.
- Loaded and second empty basket: 24 carry poses across 1.15/1.60/1.85 m head heights, both sides and two hand elevations. The basket stayed upright, below eye level and clear of controller/head bounds.
- Counter support: all 18 footprint samples across the two basket target poses lie on the original inset mesh.
- The final copy/label-facing cleanup was compiled and inspected separately after the workflow run.

This verifies desktop controller simulation. Physical Quest reach/comfort still requires a headset check.

Earlier `Sweet_Reach_Carry_2026-10-25` and `Sweet_Checkout_2026-10-25` screenshots describe superseded layouts.
