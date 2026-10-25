#if UNITY_EDITOR
using System;
using System.Linq;
using DiwaliGameplay;
using DiwaliIsland;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static class GeoIslandTravelSetup
{
    [MenuItem("Tools/Geo Island/Connect Travel And Walkway")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = SceneManager.GetActiveScene();
        var island = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Geo Island Integration");
        if (!island) throw new InvalidOperationException("Integrate the Geo source before building its travel route.");
        var nav = UnityEngine.Object.FindFirstObjectByType<DiwaliIslandNavigation>();
        if (!nav || nav.destinations == null || nav.destinations.Length < 7)
            throw new InvalidOperationException("Festival navigation is missing.");
        var root = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Geo Island Travel") ?? new GameObject("Geo Island Travel");
        var arrival = root.transform.Find("Geo Island");
        if (!arrival)
        {
            arrival = new GameObject("Geo Island").transform;
            arrival.SetParent(root.transform, false);
        }
        var ground = GameObject.Find("Geo ground base");
        if (!ground) throw new InvalidOperationException("Geo island ground is missing.");
        FaceFrontRoadTowardBridge(island.transform, ground.transform.position);
        var offset = new Vector3(ground.transform.position.x - 89f, 0f, ground.transform.position.z + 92f);
        var approachAnchor = island.transform.Find("Geo entrance approach");
        var arrivalPosition = approachAnchor ? approachAnchor.position : new Vector3(68f, .08f, -100f) + offset;
        arrival.SetPositionAndRotation(arrivalPosition, Quaternion.LookRotation(Vector3.right));
        if (!nav.destinations.Contains(arrival)) nav.destinations = nav.destinations.Concat(new[] { arrival }).ToArray();
        EditorUtility.SetDirty(nav);

        var existingBridge = scene.GetRootGameObjects().First(x => x.name == "Diwali Christmas Walkway");
        var sample = existingBridge.GetComponentsInChildren<TeleportationArea>(true).FirstOrDefault();
        if (!sample) throw new InvalidOperationException("The existing walkway has no teleportation area to match.");
        var deckMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/ChristmasIsland/Materials/Bridge Cedar.mat");
        RebuildYWalkway(existingBridge.transform, root.transform, deckMaterial, sample,
            new Vector3(arrival.position.x + .25f, arrival.position.y - .14f, arrival.position.z));
        var kioskRoot = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Venue travel kiosks")?.transform ?? root.transform;
        var kiosk = kioskRoot.Find("Travel outside - Geo Island");
        if (!kiosk)
        {
            kiosk = new GameObject("Travel outside - Geo Island").transform;
            kiosk.SetParent(kioskRoot, false);
        }
        var kioskAnchor = island.transform.Find("Geo entrance kiosk");
        var kioskPosition = kioskAnchor ? kioskAnchor.position : new Vector3(70.5f, 1.43f, -100f) + offset;
        kiosk.SetPositionAndRotation(kioskPosition, Quaternion.LookRotation(Vector3.right));
        var component = kiosk.GetComponent<VenueTravelKiosk>() ?? kiosk.gameObject.AddComponent<VenueTravelKiosk>();
        component.navigation = nav;
        component.venueTitle = "Geo Island";
        component.islandsOnly = true;
        ControllerMenu(nav);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("GEO_TRAVEL_COMPLETE destinations=" + nav.destinations.Length + " arrival=" + arrival.position + " kiosk=" + kiosk.position);
    }

    static void FaceFrontRoadTowardBridge(Transform island, Vector3 islandCenter)
    {
        Undo.RecordObject(island, "Turn Geo front road toward the Y bridge");
        float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(island.right, Vector3.up), Vector3.left, Vector3.up);
        island.RotateAround(islandCenter, Vector3.up, angle);
        Physics.SyncTransforms();
        var road = island.GetComponentsInChildren<MeshRenderer>().Single(r => r.name == "Cube.113");
        var bounds = road.bounds;
        var entrance = new Vector3(bounds.min.x + 1.25f, bounds.max.y + .08f, bounds.center.z);
        var approach = island.Find("Geo entrance approach");
        var kiosk = island.Find("Geo entrance kiosk");
        if (!approach || !kiosk) throw new InvalidOperationException("Geo entrance anchors are missing.");
        Undo.RecordObject(approach, "Use authored Geo front road entrance");
        Undo.RecordObject(kiosk, "Place Geo travel beside the front road");
        approach.SetPositionAndRotation(entrance, Quaternion.LookRotation(Vector3.right));
        kiosk.SetPositionAndRotation(entrance + new Vector3(1.5f, 1.35f, 2.6f), Quaternion.LookRotation(Vector3.right));
    }

    static void RebuildYWalkway(Transform bridge, Transform geoTravel, Material material, TeleportationArea sample, Vector3 geoLanding)
    {
        // The fork is over open water. Its eastern leg stays outside the Christmas fence
        // before reaching the clear west side of Geo Island.
        var fork = new Vector3(20f, 0f, -54f);
        Segment(bridge, "Y stem - Diwali", new Vector3(0f, 0f, -30f), fork, material, sample, 0f, 5f);
        Segment(bridge, "Y branch - Christmas", fork, new Vector3(0f, 0f, -81f), material, sample, 5f, 0f);
        Segment(bridge, "Y branch - Geo", fork, geoLanding, material, sample, 5f, 0f);
        Cube(bridge, "Y fork deck", new Vector3(fork.x, -.06f, fork.z), new Vector3(10f, .24f, 10f), material, sample);
        var hubSafety = bridge.Find("Y fork safety floor");
        if (!hubSafety)
        {
            hubSafety = new GameObject("Y fork safety floor", typeof(BoxCollider), typeof(TeleportationArea)).transform;
            hubSafety.SetParent(bridge, false);
        }
        hubSafety.localPosition = new Vector3(fork.x, -.07f, fork.z);
        hubSafety.localRotation = Quaternion.identity;
        var hubCollider = hubSafety.GetComponent<BoxCollider>();
        hubCollider.size = new Vector3(13.6f, .2f, 13.6f);
        Configure(hubSafety.GetComponent<TeleportationArea>(), hubCollider, sample);

        foreach (Transform child in bridge.Cast<Transform>().ToArray())
            if (!child.name.StartsWith("Y ")) UnityEngine.Object.DestroyImmediate(child.gameObject);
        foreach (var name in new[] { "East walk", "South walk" })
        {
            var old = geoTravel.Find(name);
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
    }

    static void Segment(Transform parent, string name, Vector3 start, Vector3 end, Material deckMaterial,
        TeleportationArea sample, float railInsetStart, float railInsetEnd)
    {
        var segment = parent.Find(name);
        if (!segment)
        {
            segment = new GameObject(name).transform;
            segment.SetParent(parent, false);
        }
        var center = (start + end) * .5f;
        var direction = (end - start).normalized;
        var length = Vector3.Distance(start, end);
        segment.SetPositionAndRotation(center, Quaternion.LookRotation(direction));
        Cube(segment, "Walkable Deck", new Vector3(0, -.06f, 0), new Vector3(3.6f, .24f, length + .3f), deckMaterial, sample);
        var safe = segment.Find("Safety floor - walkable margin");
        if (!safe)
        {
            safe = new GameObject("Safety floor - walkable margin", typeof(BoxCollider), typeof(TeleportationArea)).transform;
            safe.SetParent(segment, false);
        }
        safe.localPosition = new Vector3(0, -.07f, 0);
        safe.localRotation = Quaternion.identity;
        var collider = safe.GetComponent<BoxCollider>();
        collider.size = new Vector3(13.6f, .2f, length + 4f);
        var area = safe.GetComponent<TeleportationArea>();
        Configure(area, collider, sample);
        var railLength = length - railInsetStart - railInsetEnd;
        var railOffset = (railInsetStart - railInsetEnd) * .5f;
        for (int side = -1; side <= 1; side += 2)
        {
            Cube(segment, side < 0 ? "Left low rail" : "Right low rail",
                new Vector3(side * 1.95f, .49f, railOffset), new Vector3(.09f, .09f, railLength), deckMaterial, null, false);
        }
    }

    static void Cube(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material, TeleportationArea sample, bool solid = true)
    {
        var item = parent.Find(name);
        if (!item)
        {
            item = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            item.name = name;
            item.SetParent(parent, false);
        }
        item.localPosition = localPosition;
        item.localRotation = Quaternion.identity;
        item.localScale = size;
        if (material) item.GetComponent<Renderer>().sharedMaterial = material;
        var collider = item.GetComponent<BoxCollider>();
        if (!solid && collider) UnityEngine.Object.DestroyImmediate(collider);
        if (solid && sample)
        {
            var area = item.GetComponent<TeleportationArea>() ?? item.gameObject.AddComponent<TeleportationArea>();
            Configure(area, collider, sample);
        }
    }

    static void Configure(TeleportationArea area, Collider collider, TeleportationArea sample)
    {
        area.colliders.Clear();
        area.colliders.Add(collider);
        area.interactionLayers = sample.interactionLayers;
        area.teleportationProvider = sample.teleportationProvider;
        area.matchOrientation = MatchOrientation.WorldSpaceUp;
    }

    static void ControllerMenu(DiwaliIslandNavigation nav)
    {
        var menu = nav.controllerMenu;
        if (!menu) return;
        var source = menu.transform.Find("Visit 6")?.GetComponent<Button>();
        if (!source) return;
        var button = menu.transform.Find("Visit 7")?.GetComponent<Button>();
        if (!button)
        {
            button = UnityEngine.Object.Instantiate(source, menu.transform);
            button.name = "Visit 7";
            while (button.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(button.onClick, 0);
            UnityEventTools.AddIntPersistentListener(button.onClick, nav.VisitVenue, 7);
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true)) text.text = "Geo Island";
            foreach (var text in button.GetComponentsInChildren<Text>(true)) text.text = "Geo Island";
        }
        var menuRect = menu.GetComponent<RectTransform>();
        menuRect.sizeDelta = new Vector2(menuRect.sizeDelta.x, 900);
        for (int i = 0; i < 8; i++)
        {
            var item = menu.transform.Find("Visit " + i)?.GetComponent<RectTransform>();
            if (item) item.anchoredPosition = new Vector2(0, 155 - 72 * i);
        }
        EditorUtility.SetDirty(menu);
        EditorUtility.SetDirty(button);
    }
}
#endif
