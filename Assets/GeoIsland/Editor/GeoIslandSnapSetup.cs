#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[InitializeOnLoad]
public static class GeoIslandSnapSetup
{
    // Bump the suffix whenever SnapItems changes so the new items get set up too.
    const string AutoAppliedKey = "GeoIsland.SnapSetupApplied.2";

    // Applies the snap setup once, the first time the festival scene is open after this
    // script compiles. The scene is only marked dirty; Ctrl+S keeps the result.
    static GeoIslandSnapSetup()
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
            Debug.LogWarning("Geo snap auto-setup skipped: " + e.Message);
        }
    }

    static readonly string[] SnapItems = { "Cylinder.030", "Cylinder.009" };

    [MenuItem("Tools/Geo Island/Add Snap Home To Pickups")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var root = GameObject.Find("Geo Island Integration");
        if (!root) throw new InvalidOperationException("Geo island is missing.");
        var grabbables = root.GetComponentsInChildren<XRGrabInteractable>(true);
        foreach (var name in SnapItems)
        {
            var item = grabbables.Single(x => x.name == name).gameObject;
            Undo.RegisterFullObjectHierarchyUndo(item, "Add snap home");
            var snap = item.GetComponent<SnapHome>();
            if (!snap) snap = Undo.AddComponent<SnapHome>(item);
            EditorUtility.SetDirty(snap);
            EditorSceneManager.MarkSceneDirty(item.scene);
            Debug.Log("Geo snap: " + name + " snaps back to its start pose when dropped within " + snap.snapDistance + " m. Save the scene to keep it.");
        }
    }
}
#endif
