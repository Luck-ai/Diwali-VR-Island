#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[InitializeOnLoad]
public static class GeoIslandGuitarSound
{
    const string AutoAppliedKey = "GeoIsland.SoundSetupApplied";

    // Applies both sound setups once, the first time the festival scene is open after this
    // script compiles. The scene is only marked dirty; Ctrl+S keeps the result.
    static GeoIslandGuitarSound()
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
                ApplyEngine();
                EditorPrefs.SetBool(AutoAppliedKey, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Geo sound auto-setup skipped: " + e.Message);
            }
        }
        if (EditorPrefs.GetBool(AutoAppliedCube101Key)) return;
        try
        {
            ApplyCube101();
            EditorPrefs.SetBool(AutoAppliedCube101Key, true);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Geo Cube.101 sound auto-setup skipped: " + e.Message);
        }
    }

    const string AutoAppliedCube101Key = "GeoIsland.Cube101SoundApplied";

    // Cube.101 plays its clip once when touched (selected by hand or ray). A pickup stays a
    // pickup; anything else gets a click-only interactable so it does not start moving.
    [MenuItem("Tools/Geo Island/Add Cube.101 Touch Sound")]
    public static void ApplyCube101()
    {
        var root = GeoRoot();
        var item = root.GetComponentsInChildren<Transform>(true).Single(x => x.name == "Cube.101").gameObject;
        Undo.RegisterFullObjectHierarchyUndo(item, "Add Cube.101 touch sound");
        if (!item.GetComponent<XRBaseInteractable>())
        {
            if (!item.GetComponents<Collider>().Any(c => c.enabled))
            {
                var collider = Undo.AddComponent<MeshCollider>(item);
                collider.sharedMesh = item.GetComponent<MeshFilter>().sharedMesh;
                collider.convex = false;
            }
            Undo.AddComponent<XRSimpleInteractable>(item);
        }
        AddSound(item, "Assets/GeoIsland/Source/mp3/y2mate_HAnHMFc.mp3");
        Debug.Log("Geo Cube.101: plays y2mate_HAnHMFc once when touched. Save the scene to keep it.");
    }

    [MenuItem("Tools/Geo Island/Add Guitar Grab Sound")]
    public static void Apply()
    {
        var root = GeoRoot();
        var guitar = root.GetComponentsInChildren<XRGrabInteractable>(true).Single(x => x.name == "Cube.100").gameObject;
        Undo.RegisterFullObjectHierarchyUndo(guitar, "Add guitar grab sound");
        AddSound(guitar, "Assets/GeoIsland/Source/mp3/rokytshred.mp3");
        Debug.Log("Geo guitar: Cube.100 plays rokytshred once when grabbed. Save the scene to keep it.");
    }

    // The boat engine stays fixed in place; clicking it (ray or hand select) makes it roar.
    [MenuItem("Tools/Geo Island/Add Boat Engine Click Sound")]
    public static void ApplyEngine()
    {
        var root = GeoRoot();
        var engine = root.GetComponentsInChildren<Transform>(true).Single(x => x.name == "Cube.103").gameObject;
        if (engine.transform.parent.GetComponentInParent<Rigidbody>())
            throw new InvalidOperationException("Cube.103 sits under a movable object; it cannot be a fixed button.");
        Undo.RegisterFullObjectHierarchyUndo(engine, "Add boat engine click sound");

        var grab = engine.GetComponent<XRGrabInteractable>();
        if (grab) Undo.DestroyObjectImmediate(grab);
        var body = engine.GetComponent<Rigidbody>();
        if (body) Undo.DestroyObjectImmediate(body);
        if (!engine.GetComponents<Collider>().Any(c => c.enabled))
        {
            var collider = Undo.AddComponent<MeshCollider>(engine);
            collider.sharedMesh = engine.GetComponent<MeshFilter>().sharedMesh;
            collider.convex = false;
        }
        if (!engine.GetComponent<XRSimpleInteractable>()) Undo.AddComponent<XRSimpleInteractable>(engine);
        AddSound(engine, "Assets/GeoIsland/Source/mp3/car-engine.mp3");
        Debug.Log("Geo boat engine: Cube.103 is fixed and plays car-engine once when clicked. Save the scene to keep it.");
    }

    static GameObject GeoRoot()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var root = GameObject.Find("Geo Island Integration");
        if (!root) throw new InvalidOperationException("Geo island is missing.");
        return root;
    }

    static void AddSound(GameObject target, string clipPath)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        if (!clip) throw new InvalidOperationException("Clip is missing: " + clipPath);
        var source = target.GetComponent<AudioSource>();
        if (!source) source = Undo.AddComponent<AudioSource>(target);
        source.clip = clip;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.minDistance = 1.5f;
        source.maxDistance = 18f;
        source.dopplerLevel = 0f;
        var music = target.GetComponent<GrabMusic>();
        if (!music) music = Undo.AddComponent<GrabMusic>(target);
        music.clip = clip;
        EditorUtility.SetDirty(source);
        EditorUtility.SetDirty(music);
        EditorSceneManager.MarkSceneDirty(target.scene);
        Selection.activeGameObject = target;
    }
}
#endif
