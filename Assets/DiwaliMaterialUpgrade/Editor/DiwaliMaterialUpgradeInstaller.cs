#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiwaliMaterialUpgrade.Editor
{
    public static class DiwaliMaterialUpgradeInstaller
    {
        const string Root = "Assets/DiwaliMaterialUpgrade";
        [Serializable] public class Manifest { public Game[] games; }
        [Serializable] public class Game { public string game, model; public Surface[] materials; }
        [Serializable] public class Surface
        {
            public string name, kind, sourceMaterial, baseColor, normal, emission;
            public float[] tint;
            public float metallic, smoothness = .5f, normalScale = .2f, alpha = 1, transmission;
            public bool emissive, twoSided, flame;
        }

        [MenuItem("Diwali Island/Materials/Build Four PBR Game Prefabs")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before building the material prefabs.");
            string manifestPath = Root + "/game-import-manifest.json";
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest?.games == null || manifest.games.Length != 4)
                throw new InvalidDataException("The full four-game export manifest is required.");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            ConfigureTextures();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit shader is unavailable.");
            var report = new StringBuilder("Game,Renderers,Material slots,Width,Height,Depth\n");
            foreach (var game in manifest.games)
            {
                string modelPath = Root + "/" + game.model;
                var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (importer == null) throw new InvalidDataException("Missing model: " + modelPath);
                importer.importCameras = false;
                importer.importLights = false;
                importer.importAnimation = false;
                importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.SaveAndReimport();
                var materials = new Dictionary<string, Material>();
                foreach (var surface in game.materials)
                    materials[surface.name] = BuildSurface(surface, shader);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (!model) throw new InvalidDataException("Model did not import: " + modelPath);
                var instance = UnityEngine.Object.Instantiate(model);
                try
                {
                    instance.name = game.game + " - Completed PBR Game";
                    var renderers = instance.GetComponentsInChildren<Renderer>(true);
                    int slots = 0;
                    var bounds = new Bounds(); bool first = true;
                    foreach (var renderer in renderers)
                    {
                        var assigned = renderer.sharedMaterials;
                        for (int i = 0; i < assigned.Length; i++)
                        {
                            string key = assigned[i] ? assigned[i].name.Replace(" (Instance)", "") : "";
                            if (!materials.TryGetValue(key, out var replacement))
                                throw new InvalidDataException(game.game + ": unmatched material slot " + key + " on " + renderer.name);
                            assigned[i] = replacement;
                            slots++;
                        }
                        renderer.sharedMaterials = assigned;
                        if (first) { bounds = renderer.bounds; first = false; }
                        else bounds.Encapsulate(renderer.bounds);
                    }
                    PrefabUtility.SaveAsPrefabAsset(instance, Root + "/Prefabs/" + game.game + "_PBR.prefab");
                    report.AppendLine($"{game.game},{renderers.Length},{slots},{bounds.size.x:F3},{bounds.size.y:F3},{bounds.size.z:F3}");
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "AgentScripts/MaterialEnhancement/unity-import-report.csv"), report.ToString());
            AssetDatabase.SaveAssets();
            Debug.Log("Four completed PBR game prefabs built. No scene was changed.\n" + report);
        }

        static void ConfigureTextures()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Textures" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool normal = path.EndsWith("_Normal.png", StringComparison.Ordinal);
                bool color = path.EndsWith("_BaseColor.png", StringComparison.Ordinal) || path.EndsWith("_Emission.png", StringComparison.Ordinal);
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = color;
                importer.flipGreenChannel = false;
                importer.convertToNormalmap = false;
                importer.mipmapEnabled = true;
                importer.streamingMipmaps = true;
                importer.wrapMode = path.Contains("/Baked/") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = normal ? 2 : 4;
                importer.maxTextureSize = 1024;
                importer.isReadable = false;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                var android = importer.GetPlatformTextureSettings("Android");
                android.name = "Android";
                android.overridden = true;
                android.maxTextureSize = 1024;
                android.format = TextureImporterFormat.ASTC_6x6;
                android.compressionQuality = 80;
                importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();
            }
        }

        static Texture2D Texture(string path) => string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/" + path);
        static void Float(Material m, string key, float value) { if (m.HasProperty(key)) m.SetFloat(key, value); }
        static void Map(Material m, string key, Texture2D value) { if (m.HasProperty(key)) m.SetTexture(key, value); }

        static Material BuildSurface(Surface s, Shader shader)
        {
            // Identical surface recipes share an asset across repeated props.
            string recipe = s.kind + "/" + s.baseColor + "/" + s.normal + "/" + s.emission + "/" + string.Join(",", s.tint ?? Array.Empty<float>())
                + $"/{s.metallic:R}/{s.smoothness:R}/{s.normalScale:R}/{s.alpha:R}/{s.transmission:R}/{s.emissive}/{s.twoSided}/{s.flame}";
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(recipe))).Replace("-", "").Substring(0, 16);
            string path = Root + "/Materials/" + s.kind + "_" + hash + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            mat.shaderKeywords = Array.Empty<string>();
            bool baked = s.kind == "Baked";
            var color = s.tint != null && s.tint.Length == 4 ? new Color(s.tint[0], s.tint[1], s.tint[2], s.tint[3]) : Color.white;
            float alpha = s.alpha > 0 ? s.alpha : 1;
            if (s.transmission > .05f) alpha = Mathf.Min(alpha, s.transmission >= .5f ? .48f : .78f);
            color.a = alpha;
            mat.SetColor("_BaseColor", color);
            Map(mat, "_BaseMap", Texture(baked ? s.baseColor : "Textures/" + s.kind + "_BaseColor.png"));
            var normal = Texture(baked ? s.normal : "Textures/" + s.kind + "_Normal.png");
            Map(mat, "_BumpMap", normal);
            Float(mat, "_BumpScale", s.normalScale);
            if (normal && s.normalScale > 0) mat.EnableKeyword("_NORMALMAP");
            Float(mat, "_Metallic", s.kind == "Gold" ? 1 : s.metallic);
            Float(mat, "_Smoothness", baked ? Mathf.Clamp(s.smoothness, .08f, .96f) : 1);
            Map(mat, "_MetallicGlossMap", baked ? null : Texture("Textures/" + s.kind + "_MetallicSmoothness.png"));
            if (!baked) mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            Map(mat, "_OcclusionMap", baked ? null : Texture("Textures/" + s.kind + "_Occlusion.png"));
            Float(mat, "_OcclusionStrength", .22f);
            if (!baked) mat.EnableKeyword("_OCCLUSIONMAP");
            Float(mat, "_SmoothnessTextureChannel", 0);
            bool transparent = alpha < .99f;
            Float(mat, "_Surface", transparent ? 1 : 0);
            Float(mat, "_Blend", 0);
            Float(mat, "_SrcBlend", (float)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
            Float(mat, "_DstBlend", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            Float(mat, "_SrcBlendAlpha", (float)BlendMode.One);
            Float(mat, "_DstBlendAlpha", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            Float(mat, "_ZWrite", transparent ? 0 : 1);
            Float(mat, "_Cull", (float)(s.twoSided || transparent || s.emissive ? CullMode.Off : CullMode.Back));
            Float(mat, "_AlphaClip", 0);
            mat.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
            mat.renderQueue = transparent ? (int)RenderQueue.Transparent : (int)RenderQueue.Geometry;
            if (transparent) mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetShaderPassEnabled("ShadowCaster", !transparent);
            mat.SetShaderPassEnabled("DepthOnly", !transparent);
            mat.SetShaderPassEnabled("DepthNormals", !transparent);
            var emission = Texture(s.emission);
            Map(mat, "_EmissionMap", emission);
            mat.SetColor("_EmissionColor", s.emissive ? Color.white * 1.4f : Color.black);
            if (s.flame)
            {
                // The source UVs crop its opaque JPEG onto flame-shaped geometry.
                // Use the preserved gradient without the original strength-15 clipping.
                Map(mat, "_BumpMap", null);
                Map(mat, "_EmissionMap", Texture(s.baseColor));
                mat.DisableKeyword("_NORMALMAP");
                mat.SetColor("_BaseColor", Color.white);
                mat.SetColor("_EmissionColor", new Color(1f, .86f, .68f, 1f) * 2.2f);
            }
            if (s.emissive) mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = s.emissive ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
#endif
