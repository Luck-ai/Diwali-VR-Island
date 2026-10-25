# Unity verification — 25 October 2026

The Geo room props and controls are restored to their authored positions. Untouched props remain anchored until picked up; releasing them enables gravity. Stage lights now aim at the stage and produce visible illumination. Music starts silent and supports play, pause and resume. The elevated room has working floor collision, teleport support and a tested stair approach.

Automatic gaze clicks are disabled in the saved player instance. Looking at another menu can no longer open it while the controller is interacting elsewhere.

| Area | Recorded result | Evidence |
| --- | --- | --- |
| Geo interactions | 873 checks passed; all 6 UI actions and 61 pickups; 12 tabletop drops | [Full interaction audit](GeoFullInteractionVerification_2026-10-25.json) |
| Geo untouched presentation | All 61 authored pickup poses preserved after 587 seconds; no missing scripts | [Fresh-session check](Geo_Fresh_Presentation_Verification.json) |
| Elevated room access | Actual CharacterController walk up the stairs, with production capsule and slope settings | [Stair check](Geo_Room_Stair_Verification.json) |
| Five Diwali games | 376 checks passed with queued XR input | [Game verification](FinalPassVerification.json) |
| Christmas interactions | 134 checks passed across the supplied event types, doors, both swings, rides and sampled loose props | [Christmas verification](Christmas_Interaction_Verification_2026-10-24.json) |
| Travel and collisions | 143 checks passed, including all eight destinations, clear arrival positions, venue menu approaches and an actual wall collision probe | [Travel verification](FinalTravelVerification.json) |

The [Geo action report](Geo_Island_Action_Report.md) lists all 67 source actions. Actual Unity captures show the [restored room](Geo_Room_Restored.png), [stage lights off](Geo_Stage_Lights_Off.png) and [stage lights on](Geo_Stage_Lights_On.png).

These are desktop Play Mode checks using simulated controllers. They do not establish physical Quest comfort, Android performance, every decorative prop interaction or every possible event combination. The Linux Editor reports an unavailable native UnityOpenXR library at startup; the recorded interaction checks ran through the desktop simulator. Test the rebuilt APK on Quest before treating device behavior as verified.

The later [path, grip and front-entry update](Movement_Grip_Fixes_2026-10-25/README.md) records the final targeted checks after these fixes.
