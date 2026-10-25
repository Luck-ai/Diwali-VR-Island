#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class GeoIslandGlassSetup
{
    const string AutoAppliedKey = "GeoIsland.GlassApplied";
    static readonly string[] GlassItems = { "Cube.029" };

    // Applies the setup once, the first time the festival scene is open after this
    // script compiles. The scene is only marked dirty; Ctrl+S keeps the result.
    static GeoIslandGlassSetup()
    {
        EditorApplication.delayCall += AutoApply;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += AutoApply;
    }

    static void AutoApply()
    {
        if (EditorPrefs.GetBool(AutoAppliedKey) || EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || !GameObject.Find("Geo Island Integration")) return;
        try
        {
            Apply();
            EditorPrefs.SetBool(AutoAppliedKey, true);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Geo glass auto-setup skipped: " + e.Message);
        }
    }

    // Swaps every material slot on the listed objects for one clear glass material.
    [MenuItem("Tools/Geo Island/Make Glass")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var root = GameObject.Find("Geo Island Integration");
        if (!root) throw new InvalidOperationException("Geo island is missing.");
        var glass = GeoIslandMaterials.Transparent("Geo Glass", new Color(.85f, .95f, 1f, .18f), .95f);
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var name in GlassItems)
        {
            var renderer = renderers.Single(r => r.name == name);
            Undo.RecordObject(renderer, "Make glass");
            renderer.sharedMaterials = Enumerable.Repeat(glass, renderer.sharedMaterials.Length).ToArray();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            EditorUtility.SetDirty(renderer);
            EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
            Debug.Log("Geo glass: " + name + " is now see-through glass. Save the scene to keep it.");
        }
    }
}
#endif
