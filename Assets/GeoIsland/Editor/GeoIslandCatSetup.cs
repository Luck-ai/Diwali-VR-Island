#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[InitializeOnLoad]
public static class GeoIslandCatSetup
{
    const string AutoAppliedKey = "GeoIsland.CatRescueApplied";
    const string Mp3 = "Assets/GeoIsland/Source/mp3/";

    // Applies the setup once, the first time the festival scene is open after this
    // script compiles. The scene is only marked dirty; Ctrl+S keeps the result.
    static GeoIslandCatSetup()
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
            Debug.LogWarning("Geo cat auto-setup skipped: " + e.Message);
        }
    }

    // Cube.105 (cat) drowns where it stands in the scene, meowing under a HELP ME sign.
    // Torus becomes a throwable ring that rescues it.
    [MenuItem("Tools/Geo Island/Add Cat Rescue")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var root = GameObject.Find("Geo Island Integration");
        if (!root) throw new InvalidOperationException("Geo island is missing.");
        var all = root.GetComponentsInChildren<Transform>(true);
        var cat = all.Single(t => t.name == "Cube.105");
        var torus = all.Single(t => t.name == "Torus");
        var meow = Clip("meowrgh.mp3");
        var happy = Clip("happy-happy-happy-song.mp3");
        Undo.RegisterFullObjectHierarchyUndo(cat.gameObject, "Add cat rescue");
        Undo.RegisterFullObjectHierarchyUndo(torus.gameObject, "Add cat rescue");

        var ring = GeoIslandPickups.EnsureGrab(torus.gameObject, out _);
        ring.throwOnDetach = true;
        EditorUtility.SetDirty(ring);

        var source = cat.GetComponent<AudioSource>();
        if (!source) source = Undo.AddComponent<AudioSource>(cat.gameObject);
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.minDistance = 1.5f;
        source.maxDistance = 25f;
        source.dopplerLevel = 0f;
        EditorUtility.SetDirty(source);

        var rescue = cat.GetComponent<CatRescue>();
        if (!rescue) rescue = Undo.AddComponent<CatRescue>(cat.gameObject);
        rescue.ring = ring;
        rescue.helpText = HelpText(cat).gameObject;
        rescue.meowClip = meow;
        rescue.happyClip = happy;
        EditorUtility.SetDirty(rescue);

        EditorSceneManager.MarkSceneDirty(cat.gameObject.scene);
        Selection.activeGameObject = cat.gameObject;
        Debug.Log("Geo cat: Cube.105 calls for help until Torus lands on it, then jumps to " + rescue.rescueLocalPosition + ". Save the scene to keep it.");
    }

    static AudioClip Clip(string file)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Mp3 + file);
        if (!clip) throw new InvalidOperationException("Clip is missing: " + Mp3 + file);
        return clip;
    }

    static TextMeshPro HelpText(Transform cat)
    {
        var sign = cat.Find("Help text");
        if (!sign)
        {
            sign = new GameObject("Help text", typeof(RectTransform), typeof(TextMeshPro), typeof(FaceCamera)).transform;
            Undo.RegisterCreatedObjectUndo(sign.gameObject, "Add cat help text");
            sign.SetParent(cat, false);
        }
        var bounds = cat.GetComponent<Renderer>().bounds;
        sign.position = new Vector3(bounds.center.x, bounds.max.y + .35f, bounds.center.z);
        sign.localScale = Vector3.one / cat.lossyScale.x;
        var text = sign.GetComponent<TextMeshPro>();
        text.text = "HELP ME";
        text.fontSize = 3f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, .2f, .2f);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(3f, .6f);
        EditorUtility.SetDirty(text);
        return text;
    }
}
#endif
