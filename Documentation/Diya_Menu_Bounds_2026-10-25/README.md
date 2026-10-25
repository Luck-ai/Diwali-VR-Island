# Diya menu containment fix — 25 October 2026

The old head-clearance code repeatedly added a sideways offset. A player approaching the bench could push the controls through the north courtyard wall. The menu was observed at Z11.09, beyond the floor edge at Z10.86.

The menu now uses the authored courtyard floor bounds with an 0.85 m inset. Each stage searches for a clear position, reserving the full rotating panel footprint and avoiding furniture. Positions stay at least 1.725 m from the player; reachable candidates within 2.8 m are preferred. Stage transitions invalidate the previous search, and a stationary player no longer causes accumulated drift. A bounded fallback handles obstructed positions.

Verification evidence is saved beside this file. Layout fixtures test all 11 stages at nine player positions, plus stationary stability. The interaction report exercises actual queued XR grip and UI input in Play Mode. These are desktop Unity checks; physical Quest comfort has not been verified.

Results: **110/110 layout checks passed** and **69/69 Diya interaction checks passed**, with zero failures. The interaction pass includes the starter-flame button from inside the courtyard and successful final flame delivery.
