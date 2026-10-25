#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GeoIslandDisplayCaseSetup
{
    const string AutoAppliedKey = "GeoIsland.DisplayCaseApplied";
    const string ClipPath = "Assets/GeoIsland/Source/mp3/glass-breaking-sound-effect_wLZSIYn.mp3";
    static readonly string[] LockedItems = { "Cylinder.015", "Cylinder.014" };

    // Applies the setup once, the first time the festival scene is open after this
    // script compiles. The scene is only marked dirty; Ctrl+S keeps the result.
    static GeoIslandDisplayCaseSetup()
    {
        EditorApplication.delayCall += AutoApply;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += AutoApply;
    }

    static void AutoApply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            !GameObject.Find("Geo Island Integration")) return;
        if (!EditorPrefs.GetBool(AutoAppliedKey))
        {
            try
            {
                Apply();
                EditorPrefs.SetBool(AutoAppliedKey, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Geo display case auto-setup skipped: " + e.Message);
                return;
            }
        }
        if (EditorPrefs.GetBool(HintAppliedKey)) return;
        try
        {
            ApplyHint();
            EditorPrefs.SetBool(HintAppliedKey, true);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Geo display case hint auto-setup skipped: " + e.Message);
        }
    }

    const string HintAppliedKey = "GeoIsland.DisplayCaseHintApplied";

    // A black hint sign floating above the glass, hidden by GlassBreak once it shatters.
    [MenuItem("Tools/Geo Island/Add Glass Display Case Hint")]
    public static void ApplyHint()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var root = GameObject.Find("Geo Island Integration");
        if (!root) throw new InvalidOperationException("Geo island is missing.");
        var glass = root.GetComponentsInChildren<GlassBreak>(true).Single();
        var sign = root.transform.Find("Glass case hint");
        if (!sign)
        {
            sign = new GameObject("Glass case hint", typeof(RectTransform), typeof(TextMeshPro), typeof(FaceCamera)).transform;
            Undo.RegisterCreatedObjectUndo(sign.gameObject, "Add glass case hint");
            sign.SetParent(root.transform, false);
        }
        var bounds = glass.GetComponent<Renderer>().bounds;
        sign.position = new Vector3(bounds.center.x, bounds.max.y + .3f, bounds.center.z);
        sign.localScale = Vector3.one / root.transform.lossyScale.x;
        var text = sign.GetComponent<TextMeshPro>();
        text.text = "maybe i can use something under the house to break this";
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.enableAutoSizing = true;
        text.fontSizeMin = .3f;
        text.fontSizeMax = 1.5f;
        text.rectTransform.sizeDelta = new Vector2(1.6f, .5f);
        Undo.RecordObject(glass, "Add glass case hint");
        glass.hint = sign.gameObject;
        EditorUtility.SetDirty(text);
        EditorUtility.SetDirty(glass);
        EditorSceneManager.MarkSceneDirty(glass.gameObject.scene);
        Selection.activeGameObject = sign.gameObject;
        Debug.Log("Geo display case: hint text added above Cube.029. Save the scene to keep it.");
    }

    // Cube.029 (glass) locks Cylinder.015 and Cylinder.014 until Cylinder.052 smashes it.
    // The locked items snap back to where they now stand when dropped near it.
    [MenuItem("Tools/Geo Island/Add Glass Display Case")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var root = GameObject.Find("Geo Island Integration");
        if (!root) throw new InvalidOperationException("Geo island is missing.");
        var all = root.GetComponentsInChildren<Transform>(true);
        GameObject Find(string name) => all.Single(t => t.name == name).gameObject;
        var glass = Find("Cube.029");
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        if (!clip) throw new InvalidOperationException("Clip is missing: " + ClipPath);

        var locked = LockedItems.Select(name =>
        {
            var item = Find(name);
            Undo.RegisterFullObjectHierarchyUndo(item, "Add glass display case");
            var grab = GeoIslandPickups.EnsureGrab(item, out bool was);
            Debug.Log("Geo display case: " + name + (was ? " was already VR grabbable." : " was NOT grabbable; it is now."));
            var snap = item.GetComponent<SnapHome>();
            if (!snap) snap = Undo.AddComponent<SnapHome>(item);
            EditorUtility.SetDirty(snap);
            return grab;
        }).ToArray();

        var hammer = Find("Cylinder.052");
        Undo.RegisterFullObjectHierarchyUndo(hammer, "Add glass display case");
        var breaker = GeoIslandPickups.EnsureGrab(hammer, out bool hammerWas);
        Debug.Log("Geo display case: Cylinder.052" + (hammerWas ? " was already VR grabbable." : " was NOT grabbable; it is now."));

        Undo.RegisterFullObjectHierarchyUndo(glass, "Add glass display case");
        var glassBreak = glass.GetComponent<GlassBreak>();
        if (!glassBreak) glassBreak = Undo.AddComponent<GlassBreak>(glass);
        glassBreak.breaker = breaker;
        glassBreak.lockedInside = locked;
        glassBreak.breakClip = clip;
        EditorUtility.SetDirty(glassBreak);
        EditorSceneManager.MarkSceneDirty(glass.scene);
        Selection.activeGameObject = glass;
        Debug.Log("Geo display case: Cube.029 locks Cylinder.015 and Cylinder.014 until Cylinder.052 breaks it. Save the scene to keep it.");
    }
}
#endif
