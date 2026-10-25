#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class GeoIslandSetup
{
    const string SourceScene = "Assets/GeoIsland/Source/GeoSource.unity";
    const string RootName = "Geo Island Integration";

    [MenuItem("Tools/Geo Island/Integrate Source Into Festival")]
    public static void Integrate()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before Geo integration.");
        var target = SceneManager.GetActiveScene();
        if (!target.IsValid() || target.path != "Assets/Scenes/DiwaliFestivalGround.unity")
            throw new InvalidOperationException("Open DiwaliFestivalGround before Geo integration.");
        if (target.GetRootGameObjects().Any(x => x.name == RootName))
            throw new InvalidOperationException("Geo Island Integration already exists; inspect it before re-running.");
        var source = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Additive);
        var imported = source.GetRootGameObjects().Single(x => x.name == "GameObject" && x.GetComponentsInChildren<XRGrabInteractable>(true).Length > 30);
        SceneManager.MoveGameObjectToScene(imported, target);
        imported.name = RootName;
        EditorSceneManager.CloseScene(source, false);
        SceneManager.SetActiveScene(target);
        var focus = FocusOf(imported);
        var offset = new Vector3(100f - focus.x, 1.15f - focus.y, -100f - focus.z);
        imported.transform.position += offset;
        var collidersAdded = AddStaticColliders(imported);
        var uiRaycasters = ConfigureUi(imported);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(target);
        EditorSceneManager.SaveScene(target);
        var bounds = BoundsOf(imported);
        Debug.Log($"GEO_IMPORT_COMPLETE root={imported.name} focus={FocusOf(imported)} bounds={bounds} grabbables={imported.GetComponentsInChildren<XRGrabInteractable>(true).Length} staticCollidersAdded={collidersAdded} uiRaycasters={uiRaycasters}");
    }

    static Vector3 FocusOf(GameObject root)
    {
        var points = root.GetComponentsInChildren<XRGrabInteractable>(true)
            .Select(x => x.GetComponentInChildren<Renderer>() ?? x.GetComponent<Renderer>())
            .Where(x => x != null).Select(x => x.bounds.center).ToArray();
        if (points.Length < 10) throw new InvalidOperationException("Geo source interactive props are missing.");
        float Median(Func<Vector3, float> axis) => points.Select(axis).OrderBy(x => x).ElementAt(points.Length / 2);
        return new Vector3(Median(x => x.x), Median(x => x.y), Median(x => x.z));
    }

    static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
        if (renderers.Length == 0) throw new InvalidOperationException("Geo source has no renderers.");
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    static int AddStaticColliders(GameObject root)
    {
        int added = 0;
        foreach (var mesh in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!mesh.sharedMesh || !mesh.GetComponent<MeshRenderer>() || mesh.GetComponent<Collider>() ||
                mesh.GetComponentInParent<Rigidbody>() || mesh.GetComponentInParent<XRGrabInteractable>()) continue;
            var collider = mesh.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh.sharedMesh;
            collider.convex = false;
            collider.isTrigger = false;
            added++;
        }
        return added;
    }

    static int ConfigureUi(GameObject root)
    {
        int count = 0;
        foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            if (canvas.renderMode != RenderMode.WorldSpace) continue;
            var legacy = canvas.GetComponent<GraphicRaycaster>();
            if (legacy) UnityEngine.Object.DestroyImmediate(legacy);
            var raycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (!raycaster) raycaster = canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            raycaster.checkFor3DOcclusion = true;
            canvas.worldCamera = Camera.main;
            count++;
        }
        return count;
    }
}
#endif
