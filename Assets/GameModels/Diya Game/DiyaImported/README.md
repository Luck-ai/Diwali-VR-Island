# Imported diya

Source: `/home/lucky/KMITL/3.1/AR/Project/Diya Game/diya.blend` (unchanged).

`Diya.prefab` is the reusable Unity model. The existing `Assets/DiwaliFestival/Prefabs/Diya.prefab` also contains it. The FBX is an exported snapshot, not a live link to the original Blender file.

- 132 lamps replaced in `Assets/Scenes/DiwaliFestivalGround.unity`.
- 50 lamps replaced in `Assets/Scenes/DiwaliIsland.unity`.
- Original placements and approximate footprints retained. All 274 festoon light bulbs preserved.
- Sphere/flame UV mapping and original image preserved. The source flame image is an opaque JPEG; saving it as PNG does not add transparency.
- Bowl uses material shading in Blender. UVs were generated only on the exported bowl copy to hold baked clay/oil color and oil normal textures. Oil transmission and thin-film shading are approximated with URP Lit.
- Bowl normals and export triangulation corrected for Unity; original vertex positions retained.
- Scene and original prefab backups: `Scene_Backups/Diya-20261016/`.

Re-export after editing the source with `AgentScripts/export_diya.py` in background Blender, then run `DiyaModelImport.Import` from `AgentScripts/DiyaModelImport.cs` using the Unity CLI. Existing scene instances share the prefab. Do not rerun `DiyaSceneReplacement.Apply`; it is the one-time migration and intentionally checks for the original lamp counts.

Verification images and scene audit: `../Scene_Previews/Diya-Import-20261016/`.
