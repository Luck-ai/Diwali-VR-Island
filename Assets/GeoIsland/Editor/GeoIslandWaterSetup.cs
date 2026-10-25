#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class GeoIslandWaterSetup
{
    const string AutoAppliedKey = "GeoIsland.WaterSetupApplied";
    const string ClipPath = "Assets/GeoIsland/Source/mp3/minecraft-water-splash-sound-effect.mp3";

    // Applies the setup once, the first time the festival scene is open after this
    // script compiles. The scene is only marked dirty; Ctrl+S keeps the result.
    static GeoIslandWaterSetup()
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
            Debug.LogWarning("Geo water auto-setup skipped: " + e.Message);
        }
    }

    // Cylinder.010 (mug) gets a hidden transparent water column that fills when it touches
    // Cylinder.008 (keg). Re-running rebuilds the water from the mug's mesh.
    [MenuItem("Tools/Geo Island/Add Mug Water Fill")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var root = GameObject.Find("Geo Island Integration");
        if (!root) throw new InvalidOperationException("Geo island is missing.");
        var all = root.GetComponentsInChildren<Transform>(true);
        var mug = all.Single(t => t.name == "Cylinder.010");
        var keg = all.Single(t => t.name == "Cylinder.008").GetComponent<Renderer>();
        var filter = mug.GetComponent<MeshFilter>();
        var mesh = filter ? filter.sharedMesh : null;
        if (!mesh || !keg) throw new InvalidOperationException("Mug mesh or keg renderer is missing.");
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        if (!clip) throw new InvalidOperationException("Clip is missing: " + ClipPath);
        Undo.RegisterFullObjectHierarchyUndo(mug.gameObject, "Add mug water fill");

        // Work in the mug's mesh space: find which local axis points up in the scene,
        // then size the water from the bottom ring of vertices so a handle is ignored.
        var worldUp = mug.InverseTransformDirection(Vector3.up);
        int up = Enumerable.Range(0, 3).OrderByDescending(i => Mathf.Abs(worldUp[i])).First();
        float sign = Mathf.Sign(worldUp[up]);
        var verts = mesh.vertices;
        var heights = verts.Select(v => v[up] * sign).ToArray();
        float bottom = heights.Min(), height = heights.Max() - bottom;
        var ring = verts.Where((v, i) => heights[i] < bottom + height * .2f).ToArray();
        int a = (up + 1) % 3, b = (up + 2) % 3;
        float minA = ring.Min(v => v[a]), maxA = ring.Max(v => v[a]);
        float minB = ring.Min(v => v[b]), maxB = ring.Max(v => v[b]);

        var old = mug.Find("Water fill");
        if (old) Undo.DestroyObjectImmediate(old.gameObject);
        var pivot = new GameObject("Water fill").transform;
        Undo.RegisterCreatedObjectUndo(pivot.gameObject, "Add mug water fill");
        pivot.SetParent(mug, false);
        var axis = Vector3.zero;
        axis[up] = sign;
        var position = Vector3.zero;
        position[up] = (bottom + height * .06f) * sign;
        position[a] = (minA + maxA) * .5f;
        position[b] = (minB + maxB) * .5f;
        pivot.localPosition = position;
        pivot.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
        pivot.localScale = Vector3.one;

        var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
        water.name = "Water";
        UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
        water.SetParent(pivot, false);
        float fill = height * .72f;
        int pivotX = MeshAxis(pivot.localRotation * Vector3.right), pivotZ = MeshAxis(pivot.localRotation * Vector3.forward);
        float Width(int i) => (i == a ? maxA - minA : maxB - minB) * .82f;
        water.localPosition = new Vector3(0f, fill * .5f, 0f);
        water.localRotation = Quaternion.identity;
        water.localScale = new Vector3(Width(pivotX), fill * .5f, Width(pivotZ));
        var renderer = water.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = GeoIslandMaterials.Transparent("Geo Water", new Color(.35f, .65f, 1f, .45f), .9f);
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        var filler = mug.GetComponent<FillOnTouch>();
        if (!filler) filler = Undo.AddComponent<FillOnTouch>(mug.gameObject);
        filler.source = keg;
        filler.liquid = pivot;
        filler.fillClip = clip;
        EditorUtility.SetDirty(filler);
        EditorSceneManager.MarkSceneDirty(mug.gameObject.scene);
        Selection.activeGameObject = pivot.gameObject;
        Debug.Log("Geo water: Cylinder.010 fills with water when it touches Cylinder.008. Save the scene to keep it.");
    }

    static int MeshAxis(Vector3 direction)
    {
        return Enumerable.Range(0, 3).OrderByDescending(i => Mathf.Abs(direction[i])).First();
    }
}
#endif
