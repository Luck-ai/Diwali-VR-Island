#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiwaliMaterialUpgrade.Editor
{
    public static class DiwaliMaterialUpgradePreview
    {
        [MenuItem("Diwali Island/Materials/Render Four PBR Game Previews")]
        public static void RenderAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Material previews run only in Edit Mode.");
            string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "AgentScripts/MaterialEnhancement/UnityPreviews");
            Directory.CreateDirectory(output);
            var audit = new StringBuilder("Game,Object,Center X,Center Y,Center Z,Width,Height,Depth\n");
            foreach (string game in new[] { "Diya", "Rangoli", "SweetShop", "LanternRouting" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DiwaliMaterialUpgrade/Prefabs/" + game + "_PBR.prefab");
                if (!prefab) throw new InvalidOperationException("Build the prefab first: " + game);
                for (int angle = 0; angle < 6; angle++)
                {
                    var preview = new PreviewRenderUtility();
                    try
                    {
                        var instance = UnityEngine.Object.Instantiate(prefab);
                        preview.AddSingleGO(instance);
                        var bounds = new Bounds(); bool first = true;
                        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                        {
                            if (angle == 0)
                            {
                                var b = renderer.bounds;
                                audit.AppendLine($"{game},{renderer.name},{b.center.x:F4},{b.center.y:F4},{b.center.z:F4},{b.size.x:F4},{b.size.y:F4},{b.size.z:F4}");
                            }
                            if (first) { bounds = renderer.bounds; first = false; }
                            else bounds.Encapsulate(renderer.bounds);
                        }
                        preview.cameraFieldOfView = 35;
                        preview.camera.clearFlags = CameraClearFlags.SolidColor;
                        preview.camera.backgroundColor = new Color(.045f, .055f, .070f, 1);
                        preview.camera.nearClipPlane = .1f;
                        preview.camera.farClipPlane = 100;
                        preview.ambientColor = new Color(.32f, .32f, .32f);
                        preview.lights[0].intensity = 1.5f;
                        preview.lights[0].transform.rotation = Quaternion.Euler(42, -28, 0);
                        preview.lights[1].intensity = .85f;
                        preview.lights[1].transform.rotation = Quaternion.Euler(330, 145, 0);
                        var directions = new[] { new Vector3(1, .56f, 1.2f), new Vector3(-1, .50f, -1.2f),
                            new Vector3(1, .15f, 0), new Vector3(-1, .15f, 0), new Vector3(0, .15f, 1), new Vector3(0, .15f, -1) };
                        var direction = directions[angle];
                        float extent = Mathf.Max(bounds.size.x, bounds.size.z, bounds.size.y);
                        preview.camera.transform.position = bounds.center + direction.normalized * extent * 1.90f;
                        preview.camera.transform.LookAt(bounds.center);
                        preview.BeginStaticPreview(new Rect(0, 0, 1200, 900));
                        preview.Render(true);
                        var texture = preview.EndStaticPreview();
                        File.WriteAllBytes(Path.Combine(output, game + "_" + angle + ".png"), texture.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                    finally { preview.Cleanup(); }
                }
            }
            File.WriteAllText(Path.Combine(output, "prefab-object-bounds.csv"), audit.ToString());
            File.WriteAllText(Path.Combine(output, "editor-state.txt"), $"Play={EditorApplication.isPlaying}; Scene={SceneManager.GetActiveScene().path}; Dirty={SceneManager.GetActiveScene().isDirty}");
            Debug.Log("Twenty-four isolated URP previews saved to " + output + "; the active scene was not changed.");
        }
    }
}
#endif
