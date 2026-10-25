# Sweet shop: reach, checkout and loaded basket

This verification supersedes the earlier Sweet checkout placement checks. Those checked an empty carried basket and screenshots taken from inside the room, which missed the view from the real eastern entrance and the loaded re-grab.

The counter keeps its previous 37.69-degree orientation. The cabinet now faces the entrance beside the counter in an L arrangement. The working position is (-30, 1.6, 4.45). A permanent **BASKET / CHECKOUT — Leave basket here** sign identifies the counter recess. Packing and checkout use that same recess; there is no separate place to carry the filled basket.

The basket carry pose includes the selected sweets, keeps the bundle upright and below eye level, and applies after XR's render-time updates. It uses the physical interactor origin rather than a far attachment point. Both basket sizes are preserved. The measured minimum controller-to-rendered-bounds gap was 0.219 m; the minimum head gap was 0.713 m across the recorded cases.

Shelf return supports a nearby release or aiming at the sweet's own green shelf preview and releasing. The card rests angled toward the customer, clear of the counter lip, and retains its remote attachment distance when picked up. Payment feedback and **Share Gift** remain on the small monitor. The large menu hides during payment.

## Current evidence

`Play_Mode.json`: 76 passed, zero failed. The run used queued NearFar grip and trigger inputs with a 3 m cast limit. It collected a basket, picked and released all four sweets from their actual cabinet positions, swapped a packed sweet back to its original shelf and replaced it, re-grabbed the loaded basket, returned it to checkout, tapped the card and shared the gift. Stocking, swapping and payment kept the head fixed and controller origins within 0.7 m.

Twenty-four carry cases covered both baskets, left/right controller positions, raised and lowered hands, and eye heights of 1.15, 1.6 and 1.85 m. The locomotion transformer was temporarily disabled to hold test eye positions and restored afterward. These are desktop Play Mode checks, not physical Quest tests or a navigation test.

Inspected captures: `Packing-from-work-position.png`, `loaded basket-forward-view.png`, `loaded basket-basket-view.png`, and `Payment-clear-view.png`. A final geometry probe found zero overlaps between the counter's measured extent, the cabinet and shop walls. Play Mode was stopped after verification.
