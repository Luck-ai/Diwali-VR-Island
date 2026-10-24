#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DiwaliGameplay
{
    public static class IslandBlenderSnapshot
    {
        static readonly string Output = Path.GetFullPath("../Submission_Diwali/Update_2026-10-24/UnityExport");

        [MenuItem("Diwali/Export/Current islands for Blender")]
        public static void Export()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Export the saved Edit mode scene, outside Play mode.");
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path) || scene.isDirty)
                throw new InvalidOperationException("Save the current scene before exporting its Blender snapshot.");
            Directory.CreateDirectory(Output);
            var meshFiles = new Dictionary<Mesh, string>();
            var materials = new Dictionary<string, object>();
            var items = new List<object>();
            var warnings = new List<string>();
            var categories = new Dictionary<string, int>();
            var venueRoots = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(c => c && new[] { "RangoliGame", "SweetGame", "RoutingGame", "DiyaGame", "FireworksGame" }.Contains(c.GetType().Name))
                .ToArray();
            int vertices = 0, triangles = 0, excluded = 0;
            var renderers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true))
                .OrderBy(r => Hierarchy(r.transform), StringComparer.Ordinal).ToArray();
            foreach (var renderer in renderers)
            {
                var filter = renderer.GetComponent<MeshFilter>();
                string hierarchy = Hierarchy(renderer.transform);
                if (!renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy || !filter || !filter.sharedMesh || Excluded(renderer, hierarchy))
                { excluded++; continue; }
                var mesh = filter.sharedMesh;
                string meshFile;
                if (!meshFiles.TryGetValue(mesh, out meshFile))
                {
                    meshFile = "mesh-" + meshFiles.Count + ".json";
                    try
                    {
                        var payload = ReadMesh(mesh, out int vertexCount, out int triangleCount);
                        File.WriteAllText(Path.Combine(Output, meshFile), JsonConvert.SerializeObject(payload));
                        vertices += vertexCount; triangles += triangleCount;
                        meshFiles.Add(mesh, meshFile);
                    }
                    catch (Exception e)
                    { warnings.Add(hierarchy + ": mesh export failed: " + e.Message); continue; }
                }
                var materialIds = new List<string>();
                foreach (var material in renderer.sharedMaterials)
                {
                    if (!material) { materialIds.Add(null); continue; }
                    string id = "material-" + material.GetInstanceID();
                    if (!materials.ContainsKey(id)) materials.Add(id, ReadMaterial(material));
                    materialIds.Add(id);
                }
                string source = AssetDatabase.GetAssetPath(mesh);
                string category = Category(hierarchy, source);
                string venue = Venue(renderer.transform, venueRoots, category);
                var matrix = renderer.localToWorldMatrix;
                var values = new float[16];
                for (int row = 0; row < 4; row++) for (int col = 0; col < 4; col++) values[row * 4 + col] = matrix[row, col];
                items.Add(new { name = renderer.name, path = hierarchy, mesh = meshFile, materials = materialIds, matrix = values, category, venue, sourceAsset = source });
                if (!categories.ContainsKey(category)) categories[category] = 0;
                categories[category]++;
            }
            var diagnostics = new { objects = items.Count, uniqueMeshes = meshFiles.Count, uniqueMaterials = materials.Count, uniqueVertices = vertices, uniqueTriangles = triangles, excludedRenderers = excluded, categories, warnings };
            var manifest = new { schemaVersion = 1, scene = scene.path, savedEditMode = true, coordinateSystem = "Unity left handed, Y up; matrices row-major; local mesh vertices", exportedUtc = DateTime.UtcNow.ToString("o"), items, materials, diagnostics };
            File.WriteAllText(Path.Combine(Output, "manifest.json"), JsonConvert.SerializeObject(manifest, Formatting.Indented));
            Debug.Log("Blender snapshot: " + JsonConvert.SerializeObject(diagnostics) + " Output: " + Output);
        }

        static object ReadMesh(Mesh mesh, out int vertexCount, out int triangleCount)
        {
            using (var dataArray = MeshUtility.AcquireReadOnlyMeshData(mesh))
            {
                var data = dataArray[0];
                vertexCount = data.vertexCount;
                triangleCount = 0;
                var positions = new float[vertexCount][];
                using (var values = new NativeArray<Vector3>(vertexCount, Allocator.Temp))
                {
                    data.GetVertices(values);
                    for (int i = 0; i < vertexCount; i++) positions[i] = V3(values[i]);
                }
                var normals = new float[0][];
                if (data.HasVertexAttribute(VertexAttribute.Normal))
                {
                    normals = new float[vertexCount][];
                    using (var values = new NativeArray<Vector3>(vertexCount, Allocator.Temp))
                    {
                        data.GetNormals(values);
                        for (int i = 0; i < vertexCount; i++) normals[i] = V3(values[i]);
                    }
                }
                var uv = new float[0][];
                if (data.HasVertexAttribute(VertexAttribute.TexCoord0))
                {
                    uv = new float[vertexCount][];
                    using (var values = new NativeArray<Vector2>(vertexCount, Allocator.Temp))
                    {
                        data.GetUVs(0, values);
                        for (int i = 0; i < vertexCount; i++) uv[i] = V2(values[i]);
                    }
                }
                var submeshes = new List<int[]>();
                for (int s = 0; s < data.subMeshCount; s++)
                {
                    var descriptor = data.GetSubMesh(s);
                    if (descriptor.topology != MeshTopology.Triangles) { submeshes.Add(new int[0]); continue; }
                    using (var indices = new NativeArray<int>(descriptor.indexCount, Allocator.Temp))
                    {
                        data.GetIndices(indices, s, true);
                        submeshes.Add(indices.ToArray());
                        triangleCount += descriptor.indexCount / 3;
                    }
                }
                return new { name = mesh.name, sourceAsset = AssetDatabase.GetAssetPath(mesh), positions, normals, uv, submeshes };
            }
        }

        static object ReadMaterial(Material material)
        {
            var colors = new Dictionary<string, float[]>();
            var floats = new Dictionary<string, float>();
            var textures = new Dictionary<string, object>();
            var shader = material.shader;
            if (shader) for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                string property = ShaderUtil.GetPropertyName(shader, i);
                switch (ShaderUtil.GetPropertyType(shader, i))
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        var c = material.GetColor(property); colors[property] = new[] { c.r, c.g, c.b, c.a }; break;
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        floats[property] = material.GetFloat(property); break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        var texture = material.GetTexture(property);
                        string asset = texture ? AssetDatabase.GetAssetPath(texture) : "";
                        textures[property] = new { path = string.IsNullOrEmpty(asset) ? "" : Path.GetFullPath(asset), asset, name = texture ? texture.name : "", scale = V2(material.GetTextureScale(property)), offset = V2(material.GetTextureOffset(property)) }; break;
                }
            }
            return new { name = material.name, shader = shader ? shader.name : "", asset = AssetDatabase.GetAssetPath(material), colors, floats, textures, renderQueue = material.renderQueue };
        }

        static string Hierarchy(Transform t) => t.parent ? Hierarchy(t.parent) + "/" + t.name : t.name;
        static float[] V3(Vector3 v) => new[] { v.x, v.y, v.z };
        static float[] V2(Vector2 v) => new[] { v.x, v.y };
        static bool Excluded(MeshRenderer renderer, string hierarchy)
        {
            string p = hierarchy.ToLowerInvariant();
            return renderer.GetComponentInParent<Canvas>(true) || p.Contains("xr origin") || p.Contains("xr rig") || p.Contains("controller") || p.Contains("runtimeui") || p.Contains("runtime ui") || p.Contains("playerenvironment") || p.Contains("player environment") || p.Contains("camera offset") || p.Contains("hand visual");
        }
        static string Category(string hierarchy, string asset)
        {
            string p = hierarchy.ToLowerInvariant(), a = asset.ToLowerInvariant();
            if (p.Contains("ocean") || p.Contains("surrounding water")) return "Water";
            if (p.Contains("bridge")) return "Bridge";
            if (p.Contains("christmas") || a.Contains("christmas") || p.Contains("island_terrain")) return "Christmas";
            if (a.Contains("diwaliapprovedgames") || p.Contains("approved warm palette") || p.Contains("native pbr")) return "User_Models";
            return "AI_Environment";
        }
        static string Venue(Transform target, MonoBehaviour[] roots, string category)
        {
            if (category == "Water" || category == "Christmas" || category == "Bridge") return "";
            for (var t = target; t; t = t.parent)
                foreach (var root in roots) if (t == root.transform) return root.GetType().Name;
            var nearest = roots.OrderBy(r => (r.transform.position - target.position).sqrMagnitude).FirstOrDefault();
            return nearest && (nearest.transform.position - target.position).sqrMagnitude < 2500 ? nearest.GetType().Name : "";
        }
    }
}
#endif
