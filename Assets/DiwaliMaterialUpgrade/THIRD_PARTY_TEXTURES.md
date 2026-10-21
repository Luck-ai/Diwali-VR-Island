# Supplied PBR textures

These six original map sets were provided by the user in PBR Materials-20261019T125559Z-1-001.
FreePBR.com identifies itself as creator in the supplied About these PBR files.txt files.
Their supplied notice permits use in video games and 3D work at no cost, asks for credit,
and prohibits redistribution of the source texture collection on other websites.
Credit in published game: Surface textures from FreePBR.com.

Sets: fiber-textured-wall1, twill-fabric, gold-scuffed, rectangle-polished-tile,
flat-temple-stonework, oakfloor. Exact source paths and processing are recorded in
pbr-provenance.json. No external downloads were needed for this pass.

Derived runtime maps are 1024px with mipmaps and ASTC 6x6 on Android. Albedo is sRGB;
normal, occlusion, metallic and smoothness are linear. Roughness is remapped to a restrained
material-specific range before inversion into URP smoothness. No displacement is applied.
