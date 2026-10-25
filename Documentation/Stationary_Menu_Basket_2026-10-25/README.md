# Stationary Diya menu and basket rim fix

The Diya menu now uses fixed task positions, following the stationary placement used by the other venues. It does not translate or rotate in response to player movement. Positions are selected only when the task changes, remain within the courtyard, and avoid furniture. The existing Move menu button remains an explicit user action.

Packed sweets now fit within the basket floor footprint with an inset from its rim. Packing uses the basket axes so turning the basket does not rotate its layout outside the rim. The existing floating presentation and ability to grab sweets for swapping are retained.

Focused Play Mode verification covers all Diya menu stages while moving the player, both baskets, all three sweet types in repeated and mixed selections, and four basket rotations. The Diya interaction report separately verifies the queued controller and UI sequence. Physical Quest testing is not included.

Results: 428 focused placement/packing checks passed. The 69-check Diya interaction sequence also passed.
