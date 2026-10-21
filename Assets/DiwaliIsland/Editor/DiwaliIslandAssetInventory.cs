using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DiwaliIsland.Editor
{
    public static class DiwaliIslandAssetInventory
    {
        static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

        [MenuItem("Diwali Island/Export Generated Asset Inventory")]
        public static void Export()
        {
            var root = GameObject.Find("Diwali Island");
            if (root == null) throw new InvalidOperationException("Open the island scene first.");
            string folder = Directory.GetParent(Application.dataPath).Parent.FullName;
            var rows = new StringBuilder("Hierarchy path,Name,Components,Mesh file,Material files,Active in hierarchy\n");
            var objects = root.GetComponentsInChildren<Transform>(true).ToList();
            var menu = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == "Controller Travel Menu");
            if (menu != null) objects.AddRange(menu.GetComponentsInChildren<Transform>(true));
            var manager = GameObject.Find("XR Interaction Manager");
            if (manager != null && manager.transform.parent == null) objects.Add(manager.transform);
            foreach (var transform in objects)
            {
                string path = transform.name;
                for (var parent = transform.parent; parent != null; parent = parent.parent)
                    path = parent.name + "/" + path;
                var mesh = transform.GetComponent<MeshFilter>();
                var renderer = transform.GetComponent<Renderer>();
                string components = string.Join(" | ", transform.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name));
                string materials = renderer == null ? "" : string.Join(" | ", renderer.sharedMaterials.Where(m => m != null).Select(AssetDatabase.GetAssetPath));
                rows.AppendLine(string.Join(",", new[]
                {
                    Quote(path), Quote(transform.name), Quote(components),
                    Quote(mesh == null ? "" : AssetDatabase.GetAssetPath(mesh.sharedMesh)),
                    Quote(materials), Quote(transform.gameObject.activeInHierarchy.ToString())
                }));
            }
            File.WriteAllText(Path.Combine(folder, "GENERATED_SCENE_OBJECTS.csv"), rows.ToString());
            var files = new StringBuilder("Asset path,Asset type\n");
            var assets = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/DiwaliIsland/") && !AssetDatabase.IsValidFolder(p))
                .Concat(new[] { "Assets/Scenes/DiwaliIsland.unity" }).OrderBy(p => p).ToArray();
            foreach (string path in assets)
                files.AppendLine(Quote(path) + "," + Quote(AssetDatabase.GetMainAssetTypeAtPath(path).Name));
            File.WriteAllText(Path.Combine(folder, "GENERATED_ASSET_FILES.csv"), files.ToString());
            Debug.Log($"Inventory exported: {assets.Length} asset files and {objects.Count} scene objects.");
        }
    }
}
