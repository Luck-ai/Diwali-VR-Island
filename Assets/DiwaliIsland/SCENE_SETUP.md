# Diwali Island scene setup

Open `Assets/Scenes/DiwaliIsland.unity`. The scene is a simple, editable Unity environment prepared around the agreed five activities. No user Blender models were imported, exported, modified, or placed.

## Included

- An island, water, coastal rocks, distant islets, palms, a central mandala plaza, and five paved paths.
- Five proper structures: an octagonal Rangoli pavilion, a gabled sweet shop, an arched lantern garden, a domed Diya courtyard, and a timber games pier.
- Venue floors, counters, shelves, tables, posts, roof trim, garlands, planters, architectural lamps, and readable signs.
- The existing XR rig and locomotion, 88 teleport surfaces, one explicit XR Interaction Manager, and six travel destinations.
- A world-space travel directory and a left-controller travel menu. The left controller secondary button toggles the menu; point and select a destination using the existing XR UI controls.
- 32 empty `PLACE ASSET - ...` anchors with descriptions and suggested dimensions. Select an anchor to see its placement bounds in Scene View.
- Dusk lighting, a custom gradient sky shader, an island-owned URP profile and renderer, a subtle bloom/color Volume, and two disabled preview cameras.
- Shallow coastal water, foam arcs, gentle shader ripples, benches, flower beds, sweet shop window details and striped trim, lantern garden climbing foliage, courtyard flower columns, and pier fascia/bollards.

## Scope and preservation

The new scene was copied from the current open SampleScene. Original template environment roots are inactive in this copy; the existing XR rig, input setup, and EventSystem are retained. `Assets/Scenes/SampleScene.unity` was preserved, with a disk backup at `Scene_Backups/SampleScene.before-diwali-20261012.unity`.

Activity models and game logic are not included. The anchors reserve space for the Rangoli matching game, Diya treasure search, sweet memory game, lantern routing game, and pier aiming game. Grabbing, controller-controlled covers/lids/drawers/rotation, scoring, and validation belong to that later integration. The existing template interactables are inactive.

The generated geometry uses Unity primitives and custom Unity meshes. It is environment scaffolding, not a replacement for your original Blender coursework models. There are no generated gameplay prop prefabs.

The `DiwaliIslandRendering` component uses `DiwaliIslandPipeline.asset` during Play Mode and restores the previous quality pipeline when disabled. Editor quality settings keep the original performance profile. The island profile uses a 45 m shadow distance, a 1024 px main light shadow map, and HDR. Its renderer is a separate copy with URP PostProcessData assigned. The original URP and renderer assets are unchanged. Preview capture temporarily selects the island profile and extends shadow distance, then restores the editor settings.

The post-processing profile uses Neutral tonemapping, exposure +0.15, saturation +12, contrast +8, and bloom intensity 0.28 with quarter-resolution filtering and three iterations. Motion blur, depth of field, lens distortion, and vignette are absent. Water ripples are visual shader animation with no fluid physics or collider.

## Files and editing

- `Assets/DiwaliIsland/Materials`: palette materials and sky material.
- `Assets/DiwaliIsland/Geometry`: saved custom mesh assets.
- `Assets/DiwaliIsland/Scripts/DiwaliIslandNavigation.cs`: controller menu and travel requests.
- `Assets/DiwaliIsland/Scripts/DiwaliAssetAnchor.cs`: empty model placement guides.
- `Assets/DiwaliIsland/Scripts/DiwaliIslandRendering.cs`: scene-owned rendering profile lifecycle.
- `Assets/DiwaliIsland/Shaders/DuskSky.shader`: simple dusk sky.
- `Assets/DiwaliIsland/Editor/DiwaliIslandSceneBuilder.cs`: environment source and preview capture menu.
- `Assets/DiwaliIsland/Editor/DiwaliIslandSceneEnhancer.cs`: environment additions and URP setup.
- `Assets/DiwaliIsland/Editor/DiwaliIslandAssetInventory.cs`: exports the live scene and generated asset inventory.

Edit the generated hierarchy directly. The build and enhancement menus refuse to replace existing work. The preview and inventory menus can be run repeatedly: **Diwali Island > Save Overview And Welcome Previews** and **Diwali Island > Export Generated Asset Inventory**.

Saved reference images and navigation verification are in the workspace's `Scene_Previews` folder. `GENERATED_ASSETS.md`, `GENERATED_ASSET_FILES.csv`, and `GENERATED_SCENE_OBJECTS.csv` provide the asset and object inventory.

## Verification

The scene compiles and renders in the current Unity editor. The scene audit verifies five venues, 88 teleport surfaces with valid providers and matching interaction layers, 12 persistent travel-button listeners, one active XR Origin, one active gameplay camera, one Interaction Manager, and one EventSystem.

All six destinations passed the final Play Mode travel checks with zero measured camera-floor error. Unity's frame counter advanced during the test. HDR, camera post-processing, Volume layer/overrides, renderer PostProcessData and both owned shaders passed the rendering checks.

Native headset verification is unavailable in this editor session: the existing OpenXR loader raises `DllNotFoundException: UnityOpenXR`. Controller bindings, hand tracking, and comfort therefore still need a headset check. The editor travel test is recorded separately from that limitation.
