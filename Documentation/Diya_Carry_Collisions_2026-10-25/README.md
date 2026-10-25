# Diya carry and player collision fixes

The chest controls use a fixed position about 2.7 m from the lock. Walking does not reposition the menu.

Walls and furniture already had solid colliders. XR locomotion used the CharacterController, but direct tracked-head movement could bypass that movement path. PlayerTrackedCollision now sweeps tracked horizontal motion through the same CharacterController. Explicit island travel and normal origin movement remain separate from tracked movement. The environment setup also includes the Geo island and added two missing static colliders. Static structures retain colliders; dynamic rigidbodies are not added to walls.

The carried diya now disables momentum, velocity amplification, and thumbstick distance manipulation only for the selected controller while the diya is held. Its grab transformer limits the target to 0.8 m from the controller and sweeps the diya collider against solid obstacles. Controller settings are restored on release. Other game objects keep their existing grab behaviour.

All 77 focused Play Mode checks passed. They exercise native north/south walls and the display table using both queued XR movement and tracked head movement; actual queued grip and walking carry; a held-object barrier; lighting at the starter; walking around the screen and over an authored route tile; flame transfer and return at the display; controller setting restoration; and chest menu clearance/stability. Lighting and delivery start from an explicit prepared/cleaned fixture.

Play_Mode.json and Checks.tsv contain the results. Verification.cs.txt records the Unity command used. Chest-controls.png shows the new spacing. These are desktop Unity checks; physical Quest testing has not been performed.

A separate slow-tracking check also passed: 2,000 movements of 0.5 mm accumulate through the collision guard and stop before the north wall, rather than slipping through below the movement threshold.
