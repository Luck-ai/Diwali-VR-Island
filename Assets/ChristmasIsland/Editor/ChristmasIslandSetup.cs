#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static class ChristmasIslandSetup
{
    public const string AssetRoot = "Assets/ChristmasIsland";
    const string ModelPath = AssetRoot + "/Models/ChristmasIsland.fbx";
    static readonly HashSet<int> FixedEvents = new HashSet<int> { 1, 2, 4, 5, 6, 9 };
    static TeleportationProvider provider;
    static InteractionLayerMask teleportLayers;
    static Material wood, red, cream;
    static TMP_FontAsset font;

    [MenuItem("Tools/Christmas Island/Setup Selected Island")]
    public static void SetupSelected()
    {
        if (!Selection.activeGameObject) throw new InvalidOperationException("Select the imported Christmas island first.");
        Configure(Selection.activeGameObject);
    }
    public static GameObject Install(Vector3 center)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before scene setup.");
        var existing = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(x => x.name == "Christmas Island Integration");
        if (existing) { existing.transform.position = center; Configure(existing); return existing; }
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (!asset) throw new FileNotFoundException(ModelPath);
        var container = new GameObject("Christmas Island Integration");
        container.transform.position = center;
        var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, container.transform);
        model.name = "ChristmasIsland";
        Configure(container);
        return container;
    }
    public static object Configure(GameObject root)
    {
        provider = UnityEngine.Object.FindFirstObjectByType<TeleportationProvider>();
        var sample = UnityEngine.Object.FindObjectsByType<TeleportationArea>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(x => !x.transform.IsChildOf(root.transform));
        teleportLayers = sample ? sample.interactionLayers : (InteractionLayerMask)(1 << 1);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular SDF.asset");
        Directory.CreateDirectory(AssetRoot + "/Materials");
        wood = Material("Bridge Cedar", new Color(.23f,.12f,.065f));
        red = Material("Christmas Red", new Color(.43f,.025f,.035f));
        cream = Material("Instruction Ivory", new Color(.96f,.92f,.79f));
        var all = root.GetComponentsInChildren<Transform>(true);
        var events = all.Where(t => t.parent && t.parent.name.EndsWith("_Interactables") &&
            System.Text.RegularExpressions.Regex.IsMatch(t.name, @"_Int\d+_")).ToArray();
        if (events.Length != 10) throw new InvalidOperationException("Expected 10 supplied event roots, found " + events.Length);
        foreach (var t in all)
        {
            if (t.name == "Sea") { t.gameObject.SetActive(false); continue; }
            var mf = t.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh || IsInsideEvent(t)) continue;
            if (t.name.Contains("_Prop_")) { ConfigureGrab(t.gameObject); continue; }
            var collider = t.GetComponent<MeshCollider>();
            if (!collider) collider = t.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mf.sharedMesh;
            bool moving = IsMoving(t);
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, moving ? 0 : StaticEditorFlags.BatchingStatic);
            if (t.name == "Island_Terrain" || t.name == "Plaza_Paths" || t.name.EndsWith("_Base") || t.name.EndsWith("_Walls"))
                ConfigureTeleport(t.gameObject, collider);
            if (t.name.EndsWith("_Door") && !IsInsideEvent(t))
            {
                ConfigureSimple(t.gameObject, new[] { collider });
                Get<ChristmasDoor>(t.gameObject);
            }
        }
        foreach (var t in events)
        {
            var item = Get<InteractableItem>(t.gameObject); item.ParseName();
            var colliders = ConfigureItemColliders(t.gameObject);
            if (FixedEvents.Contains(item.eventNumber)) ConfigureSimple(t.gameObject, colliders);
            else ConfigureGrab(t.gameObject, colliders);
            Get<XRItemHook>(t.gameObject);
            var action = Get<ChristmasItemAction>(t.gameObject);
            action.motionPart = item.eventNumber == 1 ? item.Part("Lid") : item.eventNumber == 3 ? item.Part("Fish") : item.eventNumber == 5 ? item.Part("Door") : null;
            action.secondaryPart = item.eventNumber == 1 ? item.Part("Surprise") : item.eventNumber == 5 ? item.Part("Cookies") : null;
            if (item.eventNumber == 2) action.track = all.First(x => x.name.EndsWith("_TrainTrack"));
            if (item.eventNumber == 3) action.target = all.First(x => x.name.EndsWith("_FishingHole_Target"));
            if (item.eventNumber == 7) action.target = all.First(x => x.name.EndsWith("_Snowman_Target"));
            if (item.eventNumber == 8) action.target = all.First(x => x.name.EndsWith("_Reindeer_Target"));
            if (item.eventNumber == 10) action.snow = ConfigureSnow(t);
            EditorUtility.SetDirty(action);
        }
        var ferris = all.First(x => x.name == "B3_FerrisWheel");
        var ferrisController = Get<FerrisWheelSpinner>(ferris.gameObject);
        ferrisController.wheel = all.First(x => x.name == "B3_FerrisWheel_Wheel"); ferrisController.spinning = false;
        var exit = ferris.Find("Ride Exit");
        if (!exit) { exit = new GameObject("Ride Exit").transform; exit.SetParent(ferris, false); exit.localPosition = new Vector3(0,.4f,4.5f); }
        foreach (var cabin in all.Where(t => t.name.Contains("_Cabin_")))
        {
            var c = cabin.GetComponent<MeshCollider>();
            ConfigureSimple(cabin.gameObject, new[] { c });
            Get<RideSeat>(cabin.gameObject).exitPoint = exit;
        }
        var carousel = all.First(x => x.name == "B7_Carousel");
        var spinner = Get<CarouselSpinner>(carousel.gameObject);
        spinner.rotor = all.First(x => x.name == "B7_Carousel_Rotor"); spinner.spinning = false;
        var rotorCollider = spinner.rotor.GetComponent<MeshCollider>();
        ConfigureTeleport(spinner.rotor.gameObject, rotorCollider);
        Get<RidePlatform>(spinner.rotor.gameObject);
        ConvertMaterials(root);
        ConfigureInstructions(root, all);
        EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();
        return Validate(root);
    }
    static bool IsInsideEvent(Transform t)
    {
        for (var p = t; p; p = p.parent) if (p.parent && p.parent.name.EndsWith("_Interactables")) return true;
        return false;
    }
    static bool IsMoving(Transform t)
    {
        for (var p = t; p; p = p.parent)
            if (p.name.EndsWith("_Wheel") || p.name.Contains("_Cabin_") || p.name.EndsWith("_Rotor") ||
                p.name.Contains("_Horse_") || p.name.Contains("_Reindeer_") || p.name.EndsWith("_Door") || p.name.EndsWith("_Target")) return true;
        return false;
    }
    static T Get<T>(GameObject go) where T : Component { var existing = go.GetComponent<T>(); return existing ? existing : go.AddComponent<T>(); }
    static Collider[] ConfigureItemColliders(GameObject go)
    {
        var list = new List<Collider>();
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            var old = mf.GetComponent<MeshCollider>(); if (old) UnityEngine.Object.DestroyImmediate(old);
            var box = Get<BoxCollider>(mf.gameObject);
            box.center = mf.sharedMesh.bounds.center;
            var size = mf.sharedMesh.bounds.size;
            box.size = new Vector3(Mathf.Max(.045f,size.x),Mathf.Max(.045f,size.y),Mathf.Max(.045f,size.z));
            list.Add(box);
        }
        return list.ToArray();
    }
    static void ConfigureSimple(GameObject go, IEnumerable<Collider> colliders)
    {
        var xr = Get<XRSimpleInteractable>(go);
        xr.colliders.Clear(); xr.colliders.AddRange(colliders.Where(x => x));
        xr.interactionLayers = (InteractionLayerMask)1;
        GameObjectUtility.SetStaticEditorFlags(go, 0);
    }
    static void ConfigureGrab(GameObject go, Collider[] colliders = null)
    {
        if (colliders == null) colliders = ConfigureItemColliders(go);
        var body = Get<Rigidbody>(go);
        // Props stay displayed until first grabbed, then regain gravity on release.
        body.isKinematic = true; body.useGravity = true;
        body.mass = .25f; body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        var xr = Get<XRGrabInteractable>(go);
        xr.movementType = XRBaseInteractable.MovementType.Kinematic;
        xr.throwOnDetach = true; xr.forceGravityOnDetach = true;
        xr.colliders.Clear(); xr.colliders.AddRange(colliders);
        xr.interactionLayers = (InteractionLayerMask)1;
        Get<ChristmasPropReset>(go);
        GameObjectUtility.SetStaticEditorFlags(go, 0);
    }
    static void ConfigureTeleport(GameObject go, Collider collider)
    {
        var area = Get<TeleportationArea>(go);
        area.colliders.Clear(); if (collider) area.colliders.Add(collider);
        area.teleportationProvider = provider; area.interactionLayers = teleportLayers;
        area.matchOrientation = MatchOrientation.WorldSpaceUp;
        area.filterSelectionByHitNormal = true; area.upNormalToleranceDegrees = 35;
    }
    static ParticleSystem ConfigureSnow(Transform globe)
    {
        var t = globe.Find("Interactive Snow");
        if (!t) { t = new GameObject("Interactive Snow").transform; t.SetParent(globe,false); t.localPosition = new Vector3(0,.25f,0); }
        var ps = Get<ParticleSystem>(t.gameObject);
        var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = 2; main.startLifetime = 1.2f;
        main.startSpeed = .08f; main.startSize = .012f; main.maxParticles = 80; main.gravityModifier = -.015f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = ps.emission; emission.rateOverTime = 30;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .13f;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var particleMaterial = Material("Snow Particles", Color.white, true);
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
        return ps;
    }
    static Material Material(string name, Color color, bool unlit = false)
    {
        var path = AssetRoot + "/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Lit"));
            mat.name = name; mat.color = color; AssetDatabase.CreateAsset(mat,path);
        }
        return mat;
    }
    static void ConvertMaterials(GameObject root)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i=0;i<mats.Length;i++)
            {
                var old = mats[i]; if (!old || AssetDatabase.GetAssetPath(old).StartsWith(AssetRoot + "/Materials/")) continue;
                string name = old.name.Replace(" (Instance)", "");
                var color = old.HasProperty("_BaseColor") ? old.GetColor("_BaseColor") : old.HasProperty("_Color") ? old.GetColor("_Color") : Color.white;
                mats[i] = Material(name, color);
                if (name.Contains("Gold") || name.Contains("Metal") || name.Contains("Brass")) mats[i].SetFloat("_Metallic", .6f);
                if (name.Contains("Glass") || name.Contains("Ice")) mats[i].SetFloat("_Smoothness", .8f);
            }
            r.sharedMaterials = mats;
        }
    }
    static void ConfigureInstructions(GameObject root, Transform[] all)
    {
        string[] text = {
            "SANTA'S WORKSHOP\nGrip gifts and toys.\nSelect the gift to open its lid.\nSelect the train to start / stop.",
            "FROZEN LAKE\nGrip the fishing rod.\nHold near the fishing hole.\nPress trigger to reel in a fish.",
            "FERRIS WHEEL\nSelect the lever to start / stop.\nSelect a cabin to board.\nSelect it again to exit safely.\nTeleport away to leave at any time.",
            "GINGERBREAD BAKERY\nGrip the baking props.\nSelect the oven to open its door\nand slide out the cookie tray.",
            "SNOW PARK\nSelect the swing to start / stop.\nGrip and throw the snowball\nat the snowman. The ball returns.",
            "REINDEER STABLE\nGrip a carrot and hold it\nnear the reindeer's nose to feed.\nThe carrot returns after feeding.",
            "CAROUSEL\nSelect the red button to start / stop.\nWalk or teleport onto the deck.\nWalk or teleport away to leave.",
            "CHRISTMAS MARKET\nGrip the snow globe.\nPress trigger to swirl the snow.\nGrip and examine the market gifts."
        };
        for (int i=1;i<=8;i++)
        {
            var venue = all.First(t => System.Text.RegularExpressions.Regex.IsMatch(t.name,"^B"+i+"_[^_]+$"));
            Sign(venue, "Activity Instructions", new Vector3(-2.8f,1.5f,7.5f), text[i-1], 2.9f);
        }
    }
    static void Sign(Transform parent, string name, Vector3 pos, string copy, float width)
    {
        var t = parent.Find(name);
        if (!t) { t = new GameObject(name).transform; t.SetParent(parent,false); }
        t.localPosition = pos; t.localRotation = Quaternion.Euler(0,180,0);
        var text = Get<TextMeshPro>(t.gameObject);
        t = text.transform;
        if (font) text.font = font;
        text.text = copy; text.fontSize = 2.2f; text.color = new Color(.12f,.035f,.025f);
        text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.Normal;
        text.rectTransform.sizeDelta = new Vector2(width,1.35f);
        var board = t.Find("Sign backing");
        if (!board)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); board = go.transform;
            board.name = "Sign backing"; board.SetParent(t,false);
        }
        board.localPosition = new Vector3(0,0,.045f); board.localScale = new Vector3(width+.15f,1.5f,.07f);
        board.GetComponent<Renderer>().sharedMaterial = cream;
    }
    public static string ApplySameLevelLandscapeClearance(Vector3 christmasCenter)
    {
        var landscape = GameObject.Find("Rolling earth landscape");
        if(!landscape) return "No generated decorative rolling landscape found";
        var mf = landscape.GetComponent<MeshFilter>();
        if(!mf || !mf.sharedMesh) return "No decorative landscape mesh";
        const string folder = AssetRoot + "/Geometry";
        Directory.CreateDirectory(folder);
        string originalPath=folder+"/OriginalDecorativeLandscape.asset";
        var original=AssetDatabase.LoadAssetAtPath<Mesh>(originalPath);
        if(!original)
        {
            original=UnityEngine.Object.Instantiate(mf.sharedMesh);
            original.name="Preserved original decorative background terrain";
            AssetDatabase.CreateAsset(original,originalPath);
        }
        string adaptedPath=folder+"/DecorativeLandscapeChristmasClearance.asset";
        var adapted=AssetDatabase.LoadAssetAtPath<Mesh>(adaptedPath);
        if(!adapted)
        {
            adapted=UnityEngine.Object.Instantiate(original);
            adapted.name="Decorative terrain with Christmas and path clearance";
            AssetDatabase.CreateAsset(adapted,adaptedPath);
        }
        var vertices=original.vertices;
        int changed=0;
        for(int i=0;i<vertices.Length;i++)
        {
            var world=landscape.transform.TransformPoint(vertices[i]);
            float radial=Vector2.Distance(new Vector2(world.x,world.z),new Vector2(christmasCenter.x,christmasCenter.z));
            float corridorZ=Mathf.Clamp(world.z,christmasCenter.z+24,-30);
            float corridor=Vector2.Distance(new Vector2(world.x,world.z),new Vector2(0,corridorZ));
            float weight=Mathf.Max(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(63,78,radial)),1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(8,18,corridor)));
            if(weight<=0 || world.y<=-.22f) continue;
            world.y=Mathf.Lerp(world.y,-.22f,weight);
            vertices[i]=landscape.transform.InverseTransformPoint(world);changed++;
        }
        adapted.vertices=vertices;adapted.RecalculateBounds();adapted.RecalculateNormals();
        mf.sharedMesh=adapted;EditorUtility.SetDirty(adapted);
        var collider=landscape.GetComponent<MeshCollider>();if(collider)collider.sharedMesh=adapted;
        EditorSceneManager.MarkSceneDirty(landscape.scene);AssetDatabase.SaveAssets();
        return "Adjusted "+changed+" background vertices; original mesh preserved at "+originalPath;
    }
    public static object BuildConnection(Vector3 diwaliStart, Vector3 christmasEnd)
    {
        provider = UnityEngine.Object.FindFirstObjectByType<TeleportationProvider>();
        var sample = UnityEngine.Object.FindFirstObjectByType<TeleportationArea>();
        teleportLayers = sample ? sample.interactionLayers : (InteractionLayerMask)(1 << 1);
        wood = Material("Bridge Cedar",new Color(.23f,.12f,.065f));
        red = Material("Christmas Red",new Color(.43f,.025f,.035f));
        var existing = GameObject.Find("Diwali Christmas Walkway");
        if (existing) UnityEngine.Object.DestroyImmediate(existing);
        var root = new GameObject("Diwali Christmas Walkway");
        var horizontal = christmasEnd - diwaliStart; horizontal.y = 0;
        var direction = horizontal.normalized;
        var right = Vector3.Cross(Vector3.up,direction).normalized;
        var points = new List<Vector3>{diwaliStart};
        if(christmasEnd.y > diwaliStart.y + 2f)
        {
            points.Add(diwaliStart + right * -16 + direction * 6 + Vector3.up * ((christmasEnd.y-diwaliStart.y)*.5f));
            points.Add(diwaliStart + right * -16 + direction * 23 + Vector3.up * (christmasEnd.y-diwaliStart.y));
            points.Add(diwaliStart + direction * 31 + Vector3.up * (christmasEnd.y-diwaliStart.y));
        }
        points.Add(christmasEnd);
        int segments = 0; float length = 0;
        for(int leg=0;leg<points.Count-1;leg++)
        {
            var from=points[leg];var to=points[leg+1];
            var delta=to-from;var forward=delta.normalized;
            var sideVector=Vector3.Cross(Vector3.up,forward).normalized;
            float legLength=delta.magnitude;length+=legLength;
            int count=Mathf.CeilToInt(legLength/2f);
            for(int i=0;i<count;i++)
            {
                var a=Vector3.Lerp(from,to,(float)i/count);
                var b=Vector3.Lerp(from,to,(float)(i+1)/count);
                var deck=Cube(root.transform,"Walkable Deck "+(++segments),(a+b)*.5f-Vector3.up*.12f,
                    new Vector3(3.6f,.24f,Vector3.Distance(a,b)+.03f),Quaternion.LookRotation(forward),wood);
                ConfigureTeleport(deck,deck.GetComponent<Collider>());
                for(int side=-1;side<=1;side+=2)
                {
                    Cube(root.transform,"Handrail",(a+b)*.5f+sideVector*side*1.82f+Vector3.up*1.05f,
                        new Vector3(.12f,.12f,Vector3.Distance(a,b)+.04f),Quaternion.LookRotation(forward),red);
                    Cube(root.transform,"Guardrail",(a+b)*.5f+sideVector*side*1.82f+Vector3.up*.55f,
                        new Vector3(.10f,.10f,Vector3.Distance(a,b)+.04f),Quaternion.LookRotation(forward),wood);
                    Cube(root.transform,"Railing Post",a+sideVector*side*1.82f+Vector3.up*.53f,
                        new Vector3(.14f,1.14f,.14f),Quaternion.identity,red);
                }
            }
            if(leg<points.Count-2)
            {
                var landing=Cube(root.transform,"Turn Landing "+leg,to-Vector3.up*.12f,new Vector3(4,.24f,4),Quaternion.identity,wood);
                ConfigureTeleport(landing,landing.GetComponent<Collider>());
            }
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        return new { segments, width=3.6f, length, start=diwaliStart.ToString(), end=christmasEnd.ToString(),waypoints=points.Select(x=>x.ToString()).ToArray() };
    }
    static GameObject Cube(Transform parent,string name,Vector3 position,Vector3 size,Quaternion rotation,Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,true);
        go.transform.SetPositionAndRotation(position,rotation); go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=material; return go;
    }
    public static object Validate(GameObject root)
    {
        var items=root.GetComponentsInChildren<InteractableItem>(true);
        var errors=new List<string>();
        foreach(var item in items)
        {
            if (!item.GetComponent<XRItemHook>()) errors.Add(item.name+": no XR hook");
            if (!item.GetComponent<ChristmasItemAction>()) errors.Add(item.name+": no action");
            var xr=item.GetComponent<XRBaseInteractable>();
            if (!xr || xr.colliders.Count==0) errors.Add(item.name+": no XR collider");
        }
        int missingScripts=0;
        foreach(var t in root.GetComponentsInChildren<Transform>(true)) missingScripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
        if(missingScripts>0) errors.Add("Missing scripts: "+missingScripts);
        return new { events=items.Length, actions=root.GetComponentsInChildren<ChristmasItemAction>(true).Length,
            grabbables=root.GetComponentsInChildren<XRGrabInteractable>(true).Length,
            seats=root.GetComponentsInChildren<RideSeat>(true).Length,
            teleportAreas=root.GetComponentsInChildren<TeleportationArea>(true).Length,
            provider=provider?provider.name:"MISSING", errors=errors.ToArray() };
    }
}
#endif
