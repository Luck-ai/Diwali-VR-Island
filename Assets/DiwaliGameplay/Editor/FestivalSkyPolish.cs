#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiwaliGameplay
{
    public static class FestivalSkyPolish
    {
        const string MaterialPath = "Assets/DiwaliFestival/Materials/Festival twilight final.mat";
        const string ShaderName = "DiwaliFestival/Festival Twilight Sky";

        [MenuItem("Diwali/Final Pass/Apply festival twilight sky")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before changing the festival sky.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/DiwaliFestivalGround.unity")
                throw new InvalidOperationException("Open DiwaliFestivalGround before applying the sky.");
            var shader = Shader.Find(ShaderName);
            if (!shader || !shader.isSupported)
                throw new InvalidOperationException("Festival twilight sky shader is missing or unsupported.");
            var previous = RenderSettings.skybox;
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply festival twilight sky");
            if (!material)
            {
                material = previous ? new Material(previous) : new Material(shader);
                material.name = "Festival twilight final";
                material.shader = shader;
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                Undo.RecordObject(material, "Tune festival twilight sky");
                material.shader = shader;
            }
            // Shared gradient properties survive cloning; only the final sky palette is deliberately replaced.
            material.SetColor("_ZenithColor", new Color(.045f, .065f, .16f));
            material.SetColor("_HorizonColor", new Color(.29f, .22f, .30f));
            material.SetColor("_LowerColor", new Color(.12f, .14f, .21f));
            material.SetColor("_StarColor", new Color(.62f, .70f, .88f));
            material.SetFloat("_StarStrength", .55f);
            material.SetFloat("_StarDensity", .002f);
            EditorUtility.SetDirty(material);
            // Register the scene RenderSettings object so Undo restores the previous sky assignment.
            Undo.RecordObject(Unsupported.GetRenderSettings(), "Assign festival twilight sky");
            RenderSettings.skybox = material;
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("Festival sky: " + (previous ? previous.name : "none") + " -> " + material.name
                + "; indigo zenith, dusk horizon, sparse static stars; one sky pass, no textures. Ambient/key/exposure unchanged.");
        }
    }
}
#endif
