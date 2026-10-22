# Festival Realms

A VR festival experience built in Unity for Meta Quest 3S. Explore connected Diwali and Christmas islands, take part in festival activities, and bring the Diwali island to life through hands-on mini-games.

## The experience

The Diwali island contains five interactive venues. Each combines grabbing with scripted interactions and world-space controls.

| Venue | What you do |
| --- | --- |
| Rangoli Courtyard | Fit five distinct pieces into the puzzle holder to reveal their matching decorations in the pavilion, then customise the display. |
| Sweet Shop | Open the cabinet, collect four sweets in a basket, and use the payment card to check out. |
| Lantern Routing | Fit the connectors and power crystal, rotate the connectors, and test the route to light the lanterns. |
| Diya Courtyard | Discover the instructions and key, open the chest, clean and light a diya, then carry its flame to the main display. |
| Fireworks | Load the rocket, fountain and spinner; adjust their controls and press the plunger. Aim the rocket at the movable target and trigger a finale around the central decoration. |

Venue completion adds celebration to the wider island: glowing diyas and lanterns, an animated central rangoli, and fireworks. Travel panels outside the venues provide shortcuts, including travel to the Christmas island. A connecting pathway also joins the islands.

## Requirements

- **Unity 6000.3.19f1** (the project version).
- **Android Build Support**, including the SDK, NDK and OpenJDK modules supplied through Unity Hub.
- A **Meta Quest 3S** for testing the Android build with tracked controllers.
- Git to clone the project. Unity downloads packages and regenerates its local cache on first open.

Key packages are recorded in `Packages/manifest.json` and `Packages/packages-lock.json`: XR Interaction Toolkit 3.5.1, OpenXR 1.17.1, Meta OpenXR 2.5.1, Input System 1.19.0 and URP 17.3.0. The manifest also includes development/editor tooling, including Unity Skills from GitHub.

## Open the project

1. Clone this repository.
2. In Unity Hub, choose **Add project from disk** and select the repository folder—the folder containing `Assets`, `Packages` and `ProjectSettings`.
3. Open it with Unity 6000.3.19f1 and allow package resolution and asset imports to finish.
4. Open **`Assets/Scenes/DiwaliFestivalGround.unity`**. This scene contains the connected festival islands and is the enabled build scene.

The local checkout may still be named `final_project_vr`; this does not affect the project title or its content.

## Play and controls

Use the configured XR controller input actions to move, turn, select world-space UI and grab objects. Near valid placement targets, visual hints show where items can be released. The games use forgiving snap areas rather than requiring exact manual alignment.

- Read the current task on each venue panel.
- Choose **Begin** to start that venue.
- Use **How To Play** for its instructions.
- Grip objects to interact; release them near the indicated target when placement is required.
- Use the venue buttons or sliders to control moving parts.
- Choose **Restart** to reset the venue.

For Fireworks, the plunger is a physical interaction: grab its handle and push down. Its menu stays beside the plunger. Activated effects continue until the venue is restarted.

## Build for Quest 3S

1. Open **File → Build Profiles** and switch to **Android**.
2. Confirm `DiwaliFestivalGround` is enabled in the scene list.
3. Check the Android XR Plug-in Management / OpenXR settings and Meta Quest support before building.
4. Use **IL2CPP** with **ARM64** for the Quest build.
5. Enable developer mode on the headset, connect it with USB and accept the debugging prompt.
6. Choose **Build And Run**, or install the resulting APK onto the headset with Android Debug Bridge.

Quest uses an Android-based operating system, so Android is the correct build platform. The XR configuration supplies headset and controller support. APKs and generated build folders are excluded from Git.

## Testing status

Gameplay changes have been checked in Unity Play Mode using simulation and scripted checks, including placement, state transitions, menus, effects and restart behaviour. These checks do not establish physical headset comfort, controller reach or Android performance. Test the APK on the Quest 3S before treating it as a finished release.

## Project layout

| Path | Contents |
| --- | --- |
| `Assets/Scenes/` | Main festival scene and other scenes. |
| `Assets/DiwaliGameplay/` | Venue logic, grabbing and snapping, UI, effects, travel and editor utilities. |
| `Assets/DiwaliApprovedGames/` | Imported venue models, materials and import repairs. |
| `Assets/DiwaliFestival/` | Festival environment assets and rendering configuration. |
| `Assets/GameModels/` | Other imported game models. |
| `Assets/XR/` | XR settings and related assets. |
| `Packages/` | Unity package manifest and dependency lock file. |
| `ProjectSettings/` | Shared Unity project configuration. |

## Assets and attribution

This coursework project combines authored Blender models, imported PBR materials, Unity-generated environment details and Unity package/sample content. Their inclusion here does not grant a separate licence to third-party assets. Preserve any accompanying attribution and licence files when reusing or redistributing content.

Unity-generated caches (`Library`, `Temp`, `Logs`), local editor preferences, APKs and debug build output are intentionally excluded. Keep `.meta` files with their assets to preserve scene and prefab references.
