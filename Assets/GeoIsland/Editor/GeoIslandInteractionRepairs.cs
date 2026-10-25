#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class GeoIslandInteractionRepairs
{
    [MenuItem("Tools/Geo Island/Repair Fan And Table Physics")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before repairing Geo.");
        var root = GameObject.Find("Geo Island Integration");
        if (!root) throw new InvalidOperationException("Geo island is missing.");
        Undo.RegisterFullObjectHierarchyUndo(root, "Repair Geo interactions");
        if (!root.GetComponent<GeoIslandPhysics>()) root.AddComponent<GeoIslandPhysics>();

        var fan = root.GetComponentsInChildren<Spinner>(true).Single(s => s.name == "Cylinder.005");
        fan.rotationAxis = Vector3.forward;
        EditorUtility.SetDirty(fan);

        int added = 0;
        foreach (var mesh in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer = mesh.GetComponent<MeshRenderer>();
            if (!mesh.sharedMesh || !renderer || !renderer.enabled ||
                mesh.GetComponentInParent<Rigidbody>() || mesh.GetComponentInParent<Spinner>()) continue;
            if (mesh.GetComponents<Collider>().Any(c => c.enabled && !c.isTrigger)) continue;
            var collider = mesh.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh.sharedMesh;
            collider.convex = false;
            added++;
        }

        foreach (var old in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Tabletop collision backing").ToArray())
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        var supports = root.transform.Find("Geo tabletop supports");
        if (!supports)
        {
            supports = new GameObject("Geo tabletop supports").transform;
            supports.SetParent(root.transform, false);
        }
        supports.SetPositionAndRotation(root.transform.position, Quaternion.identity);
        supports.localScale = Vector3.one / root.transform.lossyScale.x;
        var tableNames = new[] { "Cube.003", "Cube.004", "Cube.045", "Cube.055", "Cube.069", "Cube.077" };
        foreach (var old in supports.Cast<Transform>().Where(t => !tableNames.Any(n => t.name == "Tabletop support - " + n)).ToArray())
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        int tables = 0;
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var bounds = renderer.bounds;
            if (!tableNames.Contains(renderer.name) || !renderer.enabled || renderer.GetComponentInParent<Rigidbody>() ||
                bounds.center.y < .5f || bounds.center.y > 1.6f || bounds.size.y > .12f ||
                bounds.size.x < .3f || bounds.size.z < .3f) continue;
            var support = supports.Find("Tabletop support - " + renderer.name);
            if (!support)
            {
                support = new GameObject("Tabletop support - " + renderer.name, typeof(BoxCollider)).transform;
                support.SetParent(supports, false);
            }
            support.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.max.y - .083f, bounds.center.z), Quaternion.identity);
            support.localScale = Vector3.one;
            var collider = support.GetComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(bounds.size.x, .16f, bounds.size.z);
            tables++;
        }

        foreach (var item in root.GetComponentsInChildren<XRGrabInteractable>(true))
        {
            var mesh = item.GetComponent<MeshFilter>();
            if (mesh && mesh.sharedMesh)
            {
                bool curved = item.name.StartsWith("Cylinder") || item.name.StartsWith("Sphere");
                foreach (var previous in item.GetComponents<Collider>())
                    UnityEngine.Object.DestroyImmediate(previous);
                item.colliders.Clear();
                Collider shape;
                if (curved)
                {
                    var convex = item.gameObject.AddComponent<MeshCollider>();
                    convex.sharedMesh = mesh.sharedMesh;
                    convex.convex = true;
                    shape = convex;
                }
                else
                {
                    var box = item.gameObject.AddComponent<BoxCollider>();
                    box.center = mesh.sharedMesh.bounds.center;
                    var size = mesh.sharedMesh.bounds.size;
                    var scale = item.transform.lossyScale;
                    box.size = new Vector3(Mathf.Max(size.x, .02f / Mathf.Abs(scale.x)),
                        Mathf.Max(size.y, .02f / Mathf.Abs(scale.y)), Mathf.Max(size.z, .02f / Mathf.Abs(scale.z)));
                    shape = box;
                }
                shape.contactOffset = .004f;
                item.colliders.Add(shape);
            }
            var body = item.GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = true;
            body.detectCollisions = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.interpolation = RigidbodyInterpolation.None;
            body.maxDepenetrationVelocity = 1f;
            body.maxLinearVelocity = 3f;
            body.solverIterations = 12;
            body.solverVelocityIterations = 8;
            body.ResetCenterOfMass();
            body.ResetInertiaTensor();
            item.throwOnDetach = false;
            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(item);
        }

        // These source props intersected fixed display geometry at their authored positions.
        foreach (var name in new[] { "Cube.059", "Cylinder.001" })
        {
            var item = root.GetComponentsInChildren<XRGrabInteractable>().Single(x => x.name == name);
            if (item.transform.position.x < 106.2f) item.transform.position += Vector3.right * .16f;
        }
        var crate = root.GetComponentsInChildren<XRGrabInteractable>().Single(x => x.name == "Cube.098");
        var crateBounds = crate.GetComponent<Renderer>().bounds;
        if (crateBounds.min.y < .56f) crate.transform.position += Vector3.up * (.565f - crateBounds.min.y);
        var stageProp = root.GetComponentsInChildren<XRGrabInteractable>().Single(x => x.name == "Sphere.017");
        if (stageProp.GetComponent<Renderer>().bounds.min.y < 2.56f)
        {
            var bounds = stageProp.GetComponent<Renderer>().bounds;
            stageProp.transform.position += new Vector3(1.1f, 2.565f - bounds.min.y, 0f);
        }

        var pickupCounter = root.GetComponentsInChildren<MeshRenderer>().Single(r => r.name == "Cube.069").bounds;
        var shelfItems = new[] { "Cube.022", "Cylinder.007", "Cylinder.011" };
        for (int i = 0; i < shelfItems.Length; i++)
        {
            var item = root.GetComponentsInChildren<XRGrabInteractable>().Single(x => x.name == shelfItems[i]);
            if (item.transform.position.y < 2f) continue;
            var bounds = item.GetComponent<Renderer>().bounds;
            var center = new Vector3(pickupCounter.center.x + .16f, pickupCounter.max.y + bounds.extents.y + .015f,
                pickupCounter.center.z + (i - 1) * .65f);
            item.transform.position += center - bounds.center;
            EditorUtility.SetDirty(item.transform);
        }

        var fanPanel = fan.GetComponentsInParent<Transform>().Last().GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .Single(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentTarget(i) == fan))
            .GetComponentInParent<Canvas>().transform;
        fanPanel.position = new Vector3(fanPanel.position.x, 1.5f, fanPanel.position.z);

        var buttons = root.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        var shopBulb = root.GetComponentsInChildren<LightToggle>(true).Single(l => l.name == "Cube");
        var shopSupply = root.GetComponentsInChildren<LightToggle>(true).Single(l => l.name == "gg");
        shopBulb.targetLight = shopSupply.targetLight;
        var stageLights = root.GetComponentsInChildren<LightToggle>(true).Where(l => l.name == "Cylinder.031").ToArray();
        var stageButton = buttons.FirstOrDefault(b => b.onClick.GetPersistentTarget(0) is LightToggle l && l.name == "Cylinder.031") ??
            buttons.Where(b => b.onClick.GetPersistentTarget(0) is LightToggle l && l.name == "Sphere")
                .OrderByDescending(b => Vector3.Distance(b.transform.position, fan.transform.position)).First();
        while (stageButton.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(stageButton.onClick, 0);
        foreach (var light in stageLights) UnityEventTools.AddPersistentListener(stageButton.onClick, light.Toggle);
        foreach (var button in buttons)
        {
            var target = button.onClick.GetPersistentTarget(0);
            string label = button == stageButton ? "Stage lights" : target == fan ? "Fan" :
                target is Spinner ? "Spin display" : target is MusicToggle ? "Play / pause music" :
                target == shopBulb ? "Shop light" : "Ceiling light";
            foreach (var text in button.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.text = label;
            EditorUtility.SetDirty(button);
        }
        EditorUtility.SetDirty(shopBulb);
        foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
        {
            source.spatialBlend = 1f;
            source.minDistance = 1.5f;
            source.maxDistance = 18f;
            source.dopplerLevel = 0f;
            EditorUtility.SetDirty(source);
        }

        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        Debug.Log($"Geo interaction repairs: {added} missing static colliders, {tables} supported tabletop surfaces, fan local-Z axis, 61 dynamic pickups.");
    }
}
#endif
