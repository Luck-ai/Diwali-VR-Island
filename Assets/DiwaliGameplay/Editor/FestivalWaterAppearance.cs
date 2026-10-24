#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiwaliGameplay
{
    public static class FestivalWaterAppearance
    {
        const string NormalPath = "Assets/DiwaliFestival/WaterDetails/CalmOceanNormal.png";
        const string MaterialPath = "Assets/DiwaliFestival/Materials/Festival surrounding water.mat";

        [MenuItem("Diwali/Final Pass/Refine ocean water appearance")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before refining ocean water.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var shader = Shader.Find("DiwaliFestival/Festival Water");
            if (!material || !shader || !shader.isSupported)
                throw new InvalidOperationException("Existing ocean material and supported water shader are required.");
            if (ShaderUtil.GetShaderMessages(shader).Any(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))
                throw new InvalidOperationException("Resolve water shader compilation errors first.");

            AssetDatabase.ImportAsset(NormalPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(NormalPath) as TextureImporter;
            if (!importer)
                throw new InvalidOperationException("Ocean normal texture is missing.");
            importer.textureType = TextureImporterType.NormalMap;
            importer.convertToNormalmap = false;
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            Undo.RecordObject(material, "Refine ocean water appearance");
            material.shader = shader;
            material.SetTexture("_WaveNormal", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
            material.SetColor("_DeepColor", new Color(.045f, .17f, .25f));
            material.SetColor("_ShallowColor", new Color(.09f, .32f, .38f));
            material.SetFloat("_WaveScale", .85f);
            material.SetFloat("_WaveSpeed", .35f);
            material.SetFloat("_NormalStrength", .46f);
            material.SetFloat("_Smoothness", .91f);
            material.SetFloat("_ReflectionStrength", 1.15f);
            material.SetFloat("_Opacity", .96f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            // Refresh the cached sky reflection; no realtime probe or capture camera is added.
            Undo.RecordObject(Unsupported.GetRenderSettings(), "Refresh ocean sky reflection");
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 128;
            DynamicGI.UpdateEnvironment();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Ocean appearance refined. Sea transforms and sand backup were preserved; save the scene after reviewing the refreshed sky reflection.");
        }
    }
}
#endif
