#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GeoIslandOfferingSetup
{
    const string AutoAppliedKey = "GeoIsland.OfferingApplied";
    const string ClipPath = "Assets/GeoIsland/Source/mp3/holy_U9pLRUi.mp3";
    // Where Cylinder.039 belongs, relative to its parent (from the user's Inspector).
    static readonly Vector3 TargetLocalPosition = new Vector3(39.61733f, 7.247449f, 41.82384f);
    static readonly Vector3 TargetLocalEuler = new Vector3(-90f, 0f, -180f);

    // Applies the setup once, the first time the festival scene is open after this
    // script compiles. The scene is only marked dirty; Ctrl+S keeps the result.
    static GeoIslandOfferingSetup()
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
            Debug.LogWarning("Geo offering auto-setup skipped: " + e.Message);
        }
    }

    // Placing Cylinder.039 on its spot plays the holy sound and makes Cube.005 glow yellow for 2 s.
    [MenuItem("Tools/Geo Island/Add Cylinder.039 Offering Snap")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var root = GameObject.Find("Geo Island Integration");
        if (!root) throw new InvalidOperationException("Geo island is missing.");
        var all = root.GetComponentsInChildren<Transform>(true);
        var item = all.Single(t => t.name == "Cylinder.039").gameObject;
        var shrine = all.Single(t => t.name == "Cube.005").gameObject;
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        if (!clip) throw new InvalidOperationException("Clip is missing: " + ClipPath);
        Undo.RegisterFullObjectHierarchyUndo(item, "Add offering snap");
        Undo.RegisterFullObjectHierarchyUndo(shrine, "Add offering snap");

        GeoIslandPickups.EnsureGrab(item, out bool was);
        Debug.Log("Geo offering: Cylinder.039" + (was ? " was already VR grabbable." : " was NOT grabbable; it is now."));

        var renderer = shrine.GetComponent<Renderer>();
        var lightObject = shrine.transform.Find("Yellow glow light");
        if (!lightObject)
        {
            lightObject = new GameObject("Yellow glow light", typeof(Light)).transform;
            Undo.RegisterCreatedObjectUndo(lightObject.gameObject, "Add offering snap");
            lightObject.SetParent(shrine.transform, false);
        }
        var bounds = renderer.bounds;
        lightObject.position = new Vector3(bounds.center.x, bounds.max.y + .2f, bounds.center.z);
        var light = lightObject.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, .85f, .2f);
        light.intensity = 15f;
        light.range = Mathf.Max(3f, bounds.size.magnitude * 1.5f);
        light.shadows = LightShadows.None;
        light.enabled = false;
        EditorUtility.SetDirty(light);

        var glow = shrine.GetComponent<GlowPulse>();
        if (!glow) glow = Undo.AddComponent<GlowPulse>(shrine);
        glow.glowLight = light;
        glow.glowRenderer = renderer;
        glow.seconds = 2f;
        EditorUtility.SetDirty(glow);

        var snap = item.GetComponent<SnapHome>();
        if (!snap) snap = Undo.AddComponent<SnapHome>(item);
        snap.useTargetPose = true;
        snap.targetLocalPosition = TargetLocalPosition;
        snap.targetLocalEuler = TargetLocalEuler;
        snap.snapClip = clip;
        while (snap.onSnapped.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(snap.onSnapped, 0);
        UnityEventTools.AddPersistentListener(snap.onSnapped, glow.Pulse);
        EditorUtility.SetDirty(snap);

        EditorSceneManager.MarkSceneDirty(item.scene);
        Selection.activeGameObject = item;
        var target = item.transform.parent ? item.transform.parent.TransformPoint(TargetLocalPosition) : TargetLocalPosition;
        Debug.Log("Geo offering: Cylinder.039 snaps to " + target + " (world), " +
            Vector3.Distance(item.transform.position, target).ToString("F2") + " m from where it is now. Save the scene to keep it.");
    }
}
#endif
