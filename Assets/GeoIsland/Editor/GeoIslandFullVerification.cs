#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class GeoIslandFullVerification
{
    public static bool IsRunning;
    public static string ReportPath => Path.GetFullPath("../Game_Plans/GeoFullInteractionVerification_2026-10-25.json");
    public static string TableReportPath => Path.GetFullPath("../Game_Plans/GeoTableVerification_2026-10-25.json");
    static string activeReportPath;
    static readonly List<object> rows = new List<object>(), actions = new List<object>(), inventory = new List<object>();
    static int failures;
    static XROrigin rig;
    static Camera eye;
    static NearFarInteractor hand;
    static GameObject controller, root;
    static DateTime started;
    static string lastApproachDetail;
    static JArray authoredPoses;
    static async Task Pause(int ms = 120)
    {
        int frame = Time.frameCount;
        await Task.Delay(ms);
        while (EditorApplication.isPlaying && Time.frameCount < frame + 2) await Task.Delay(20);
        if (!EditorApplication.isPlaying) throw new OperationCanceledException("Play Mode stopped during Geo audit.");
    }
    static void Save(string status)
    {
        var path = activeReportPath ?? ReportPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonConvert.SerializeObject(new {
            status, failures, started, elapsedSeconds = (DateTime.UtcNow - started).TotalSeconds,
            scope = "Desktop Play Mode, queued NearFar grip/UI input, floor-supported standing fixtures. Objects are released and allowed to settle before checking; fixture restoration happens only afterwards. This does not establish physical headset comfort or Android performance.",
            actions, inventory, results = rows }, Formatting.Indented));
    }
    static void Check(string name, bool pass, string detail = "")
    {
        if (!pass) failures++;
        rows.Add(new { name, pass, detail }); Save("running");
        if (!pass) Debug.LogWarning("Geo full QA: " + name + " " + detail);
    }
    static string PathOf(Transform t)
    {
        var path = t.name;
        while (t.parent && t != root.transform) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
    static bool External(Collider c) => c && c.enabled && !c.isTrigger && !c.transform.IsChildOf(rig.transform) &&
        (!controller || !c.transform.IsChildOf(controller.transform));
    static bool FloorAt(Vector3 point, out RaycastHit floor)
    {
        floor = Physics.SphereCastAll(new Vector3(point.x, 7f, point.z), .075f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore)
            .Where(h => External(h.collider) && !h.collider.attachedRigidbody && h.normal.y > .75f && h.point.y >= -.1f &&
                h.collider.bounds.size.x > 2f && h.collider.bounds.size.z > 2f &&
                (h.point.y < .9f || h.collider.name == "Cube.086" || h.collider.name == "Plane.004" || h.collider.name == "Cube.007"))
            .OrderByDescending(h => h.point.y).FirstOrDefault();
        return floor.collider;
    }
    static bool StandAt(Vector3 point, Vector3 target, out string detail, float handHeight = 1.30f)
    {
        if (!FloorAt(point, out var floor)) { detail = "no floor"; return false; }
        var head = new Vector3(point.x, floor.point.y + 1.65f, point.z);
        var blockers = Physics.OverlapCapsule(head - Vector3.up * .1f, head - Vector3.up * 1.45f, .18f, ~0,
            QueryTriggerInteraction.Ignore).Where(External).ToArray();
        if (blockers.Length > 0) { detail = string.Join(",", blockers.Select(c => c.name)); return false; }
        rig.transform.position += head - eye.transform.position;
        var from = Vector3.ProjectOnPlane(eye.transform.forward, Vector3.up);
        var to = Vector3.ProjectOnPlane(target - eye.transform.position, Vector3.up);
        if (from.sqrMagnitude > .001f && to.sqrMagnitude > .001f)
            rig.transform.RotateAround(eye.transform.position, Vector3.up, Vector3.SignedAngle(from, to, Vector3.up));
        var forward = Vector3.ProjectOnPlane(target - head, Vector3.up).normalized;
        var origin = new Vector3(head.x, floor.point.y + handHeight, head.z) + forward * .25f;
        var delta = target - origin;
        if (delta.magnitude > 3f) { detail = "ray beyond 3 m: " + delta.magnitude; return false; }
        hand.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(delta.normalized)); Physics.SyncTransforms();
        detail = "floor=" + floor.collider.name + ", hand height=" + (origin.y - floor.point.y).ToString("F2") +
            ", ray=" + delta.magnitude.ToString("F2") + ", head=" + head.ToString("F2");
        return true;
    }
    static async Task<bool> Approach(Vector3 target, IXRInteractable item)
    {
        foreach (float handHeight in new[] { 1.30f, 1.0f, .75f })
        foreach (float distance in new[] { 1.05f, 1.55f, 2.1f })
        foreach (float angle in new[] { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f })
        {
            var direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.left;
            if (!StandAt(target + direction * distance, target, out var detail, handHeight)) continue;
            await Pause(65);
            var valid = new List<IXRInteractable>(); hand.GetValidTargets(valid);
            if (valid.Contains(item)) { lastApproachDetail = detail; return true; }
        }
        return false;
    }
    static async Task<bool> Click(Button button)
    {
        var rect = button.GetComponent<RectTransform>(); var center = rect.TransformPoint(rect.rect.center);
        var canvas = button.GetComponentInParent<Canvas>(); bool staged = false;
        foreach (float distance in new[] { 1.05f, 1.55f, 2.1f })
        {
            if (!StandAt(center - canvas.transform.forward * distance, center, out var detail)) continue;
            var delta = center - hand.transform.position;
            var blockers = Physics.RaycastAll(hand.transform.position, delta.normalized, delta.magnitude - .025f, ~0,
                QueryTriggerInteraction.Ignore).Where(h => External(h.collider)).ToArray();
            if (blockers.Length != 0) continue;
            Check("standing UI reach " + button.name, true, detail); staged = true; break;
        }
        Check("button clear approach " + button.name, staged, PathOf(button.transform));
        if (!staged) return false;
        await Pause(150); int clicks = 0; UnityEngine.Events.UnityAction listener = () => clicks++;
        button.onClick.AddListener(listener); hand.uiPressInput.QueueManualState(true, 1); await Pause(130);
        hand.uiPressInput.QueueManualState(false, 0); await Pause(220); button.onClick.RemoveListener(listener);
        Check("queued UI dispatch " + button.name, clicks == 1, "clicks=" + clicks); return clicks == 1;
    }
    static Color Emission(LightToggle light) => light.glowRenderer ? light.glowRenderer.materials[light.glowMaterialIndex].GetColor("_EmissionColor") : Color.black;
    static Color32[] CaptureStage(string filename)
    {
        var go = new GameObject("Temporary Geo stage evidence camera");
        var camera = go.AddComponent<Camera>(); camera.enabled = false;
        camera.transform.SetPositionAndRotation(new Vector3(82f, 6f, -66f),
            Quaternion.LookRotation(new Vector3(73.3f, 3.2f, -55.6f) - new Vector3(82f, 6f, -66f)));
        camera.fieldOfView = 60f; camera.nearClipPlane = .1f; camera.farClipPlane = 80f;
        var target = RenderTexture.GetTemporary(960, 540, 24, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        var texture = new Texture2D(960, 540, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.GetFullPath("../Game_Plans/" + filename), texture.EncodeToPNG());
            return texture.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(go);
        }
    }
    static async Task TestButton(Button button, int number)
    {
        int before = failures;
        string label = string.Join(" ", button.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t => t.text));
        var targets = Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Select(i => button.onClick.GetPersistentTarget(i)).ToArray();
        Check("button active " + label, button.gameObject.activeInHierarchy && button.interactable && targets.Length > 0 && targets.All(t => t));
        var lights = targets.OfType<LightToggle>().ToArray(); var spinner = targets.OfType<Spinner>().FirstOrDefault();
        var music = targets.OfType<MusicToggle>().FirstOrDefault(); var source = music ? music.GetComponent<AudioSource>() : null;
        var oldLight = lights.Select(l => l.targetLight && l.targetLight.enabled).ToArray(); var oldGlow = lights.Select(Emission).ToArray();
        var oldMusic = source && source.isPlaying;
        bool stage = lights.Length == 3;
        var dark = stage ? CaptureStage("Geo_Stage_Lights_Off.png") : null;
        await Click(button);
        for (int i = 0; i < lights.Length; i++)
        {
            Check(label + " actual light ON " + i, lights[i].targetLight && lights[i].targetLight.enabled != oldLight[i]);
            if (lights[i].glowRenderer) Check(label + " visible emission ON " + i, Emission(lights[i]) != oldGlow[i]);
        }
        if (spinner)
        {
            var position = spinner.transform.position; var rotation = spinner.transform.rotation;
            var shaft = spinner.transform.TransformDirection(spinner.rotationAxis).normalized;
            await Pause(300); var delta = spinner.transform.rotation * Quaternion.Inverse(rotation); delta.ToAngleAxis(out float degrees, out var axis);
            Check(label + " rotates around configured shaft", spinner.IsSpinning && degrees > 3f && Mathf.Abs(Vector3.Dot(axis.normalized, shaft)) > .995f,
                "degrees=" + degrees.ToString("F1") + ", axis=" + axis + ", shaft=" + shaft);
            Check(label + " fixed pivot", Vector3.Distance(position, spinner.transform.position) < .015f);
            if (spinner.name == "Cylinder.005") Check("fan shaft remains vertical", Mathf.Abs(Vector3.Dot(shaft, Vector3.up)) > .999f);
        }
        if (source) Check("music plays within Geo", source.isPlaying != oldMusic && source.clip && source.spatialBlend > .99f && source.maxDistance <= 18.1f);
        if (stage)
        {
            await Pause(180);
            var lit = CaptureStage("Geo_Stage_Lights_On.png");
            double increase = 0; int clearPixels = 0;
            for (int i = 0; i < lit.Length; i++)
            {
                double delta = ((lit[i].r - dark[i].r) * .2126 + (lit[i].g - dark[i].g) * .7152 + (lit[i].b - dark[i].b) * .0722) / 255.0;
                increase += delta;
                if (delta > .08) clearPixels++;
            }
            increase /= lit.Length;
            Check("stage lighting visibly brightens the rendered stage", increase > .006 && clearPixels > lit.Length * .01,
                "mean luminance increase=" + increase.ToString("F4") + ", clearly brighter pixels=" + clearPixels);
        }
        await Click(button);
        for (int i = 0; i < lights.Length; i++)
        {
            Check(label + " actual light OFF " + i, lights[i].targetLight && lights[i].targetLight.enabled == oldLight[i]);
            if (lights[i].glowRenderer) Check(label + " visible emission OFF " + i, Emission(lights[i]) == oldGlow[i]);
        }
        if (spinner) { var rotation = spinner.transform.rotation; await Pause(250); Check(label + " stops", !spinner.IsSpinning && Quaternion.Angle(rotation, spinner.transform.rotation) < .2f); }
        if (source) Check("music pauses", source.isPlaying == oldMusic);
        actions.Add(new { id = "GEO-UI-" + number.ToString("00"), type = "UI toggle", name = label, path = PathOf(button.transform),
            targets = targets.Select(t => t.name).ToArray(), passed = failures == before });
    }
    static Bounds VisualBounds(XRGrabInteractable item)
    {
        var renderers = item.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled).ToArray(); var bounds = renderers[0].bounds;
        foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds); return bounds;
    }
    static void Restore(XRGrabInteractable item, Transform parent, Vector3 position, Quaternion rotation)
    {
        item.transform.SetParent(parent, true); item.transform.SetPositionAndRotation(position, rotation);
        var body = item.GetComponent<Rigidbody>(); body.position = position; body.rotation = rotation;
        if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        body.isKinematic = true; body.useGravity = false; body.Sleep(); Physics.SyncTransforms();
    }
    static async Task Settle(XRGrabInteractable item, float surfaceY, string name)
    {
        var body = item.GetComponent<Rigidbody>(); await Pause(650);
        for (int i = 0; i < 10 && body.linearVelocity.magnitude > .12f; i++) await Pause(100);
        var gap = VisualBounds(item).min.y - surfaceY;
        Check(name + " gravity after release", !body.isKinematic && body.useGravity && body.detectCollisions);
        Check(name + " rests on visible surface", gap >= -.065f && gap < .10f,
            "visual bottom=" + VisualBounds(item).min.y.ToString("F3") + ", support=" + surfaceY.ToString("F3") + ", gap=" + gap.ToString("F3"));
        Check(name + " settles", body.linearVelocity.magnitude < .15f, "speed=" + body.linearVelocity.magnitude.ToString("F3"));
    }
    static async Task TestPickup(XRGrabInteractable item, int number, Collider[] tables)
    {
        int before = failures; var body = item.GetComponent<Rigidbody>(); var parent = item.transform.parent;
        var position = item.transform.position; var rotation = item.transform.rotation; var scale = item.transform.lossyScale;
        var path = PathOf(item.transform); var bounds = VisualBounds(item); var collider = item.colliders.FirstOrDefault(c => c && c.enabled && !c.isTrigger);
        inventory.Add(new { id = "GEO-GRAB-" + number.ToString("00"), name = item.name, path, home = position.ToString("F3"),
            visualSize = bounds.size.ToString("F3"), collider = collider ? collider.GetType().Name : "missing", scale = scale.ToString("F4") });
        string landing = "none";
        try
        {
            var original = authoredPoses?.FirstOrDefault(p => (string)p["name"] == item.name);
            if (original != null)
            {
                var p = original["position"].ToObject<float[]>(); var q = original["rotation"].ToObject<float[]>();
                Check("untouched authored pose " + item.name,
                    Vector3.Distance(item.transform.position, new Vector3(p[0], p[1], p[2])) < .005f &&
                    (item.GetComponent<Spinner>() || Quaternion.Angle(item.transform.rotation, new Quaternion(q[0], q[1], q[2], q[3])) < .1f));
            }
            Check("pickup collider " + item.name, body && collider && body.detectCollisions && body.collisionDetectionMode == CollisionDetectionMode.ContinuousSpeculative);
            bool staged = await Approach(collider.bounds.center, item); Check("standing pickup approach " + item.name, staged, path + "; " + lastApproachDetail); if (!staged) return;
            hand.selectInput.QueueManualState(true, 1); await Pause(200); bool selected = item.interactorsSelecting.Contains(hand);
            Check("queued grip " + item.name, selected); Check("held model scale " + item.name, (item.transform.lossyScale - scale).magnitude < .002f); if (!selected) return;
            var startHand = hand.transform.position;
            var startCenter = VisualBounds(item).center;
            hand.transform.position += Vector3.up * .12f;
            await Pause(220);
            Check("carry tracks controller " + item.name, Vector3.Distance(VisualBounds(item).center, startCenter + Vector3.up * .12f) < .09f);
            hand.transform.position = startHand;
            await Pause(220);
            hand.transform.rotation = rotation * Quaternion.Inverse(item.transform.rotation) * hand.transform.rotation;
            await Pause(160);
            for (int i = 0; i < 3; i++)
            {
                hand.transform.position += bounds.center + Vector3.up * .04f - VisualBounds(item).center;
                await Pause(90);
            }
            Check("placement over original support " + item.name,
                Vector3.Distance(VisualBounds(item).center, bounds.center + Vector3.up * .04f) < .06f);
            landing = "original support";
            hand.selectInput.QueueManualState(false, 0); await Pause(150); Check("queued release " + item.name, !item.isSelected);
            await Pause(750);
            for (int i = 0; i < 25 && body.linearVelocity.magnitude > .12f; i++) await Pause(100);
            Check("released gravity " + item.name, !body.isKinematic && body.useGravity && body.detectCollisions);
            Check("placed back without falling through " + item.name, VisualBounds(item).min.y > bounds.min.y - .35f && body.position.y > -.2f,
                "before bottom=" + bounds.min.y.ToString("F3") + ", after=" + VisualBounds(item).min.y.ToString("F3"));
            Check("placed back settles " + item.name, body.linearVelocity.magnitude < .15f, "speed=" + body.linearVelocity.magnitude.ToString("F3"));
            Check("released model scale " + item.name, (item.transform.lossyScale - scale).magnitude < .002f);
        }
        finally
        {
            hand.selectInput.QueueManualState(false, 0); await Pause(80); Restore(item, parent, position, rotation);
            actions.Add(new { id = "GEO-GRAB-" + number.ToString("00"), type = "Grab and place", name = item.name, path, placementSurface = landing, passed = failures == before });
        }
    }
    static async Task TestTables(Collider[] tables, XRGrabInteractable small)
    {
        var parent = small.transform.parent; var position = small.transform.position; var rotation = small.transform.rotation;
        var others = root.GetComponentsInChildren<XRGrabInteractable>(true).Where(x => x != small).SelectMany(x => x.colliders).Where(c => c).ToArray();
        var own = small.colliders.Where(c => c).ToArray();
        foreach (var a in own) foreach (var b in others) Physics.IgnoreCollision(a, b, true);
        foreach (var table in tables) foreach (float height in new[] { .15f, .75f })
        {
            small.transform.rotation = Quaternion.AngleAxis(90f, Vector3.up) * rotation;
            var body = small.GetComponent<Rigidbody>(); body.rotation = small.transform.rotation;
            body.isKinematic = false; body.useGravity = true;
            Physics.SyncTransforms();
            var bounds = VisualBounds(small);
            var top = table.bounds.max.y + .003f;
            var margin = new Vector2(table.bounds.extents.x - bounds.extents.x - .012f,
                table.bounds.extents.z - bounds.extents.z - .012f);
            Vector3 drop = Vector3.zero; bool clear = false;
            foreach (float x in new[] { 0f, 1f, -1f, .5f, -.5f })
            {
                foreach (float z in new[] { 0f, 1f, -1f, .5f, -.5f })
                {
                    var point = new Vector3(table.bounds.center.x + x * margin.x, top + bounds.extents.y + .012f,
                        table.bounds.center.z + z * margin.y);
                    var blocked = Physics.OverlapBox(point, bounds.extents + Vector3.one * .003f, Quaternion.identity, ~0,
                        QueryTriggerInteraction.Ignore).Any(c => !c.attachedRigidbody && c != table &&
                        c.bounds.max.y > top + .006f);
                    if (blocked || margin.x <= 0f || margin.y <= 0f) continue;
                    drop = point; clear = true; break;
                }
                if (clear) break;
            }
            Check("unobstructed tabletop drop location " + table.name, clear,
                "Fixed decorations excluded; location=" + drop.ToString("F3"));
            if (!clear) continue;
            var desired = drop + Vector3.up * (height - .012f);
            small.transform.position += desired - bounds.center;
            body.position = small.transform.position; body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms(); body.WakeUp();
            Check("drop starts over " + table.name, Vector3.Distance(VisualBounds(small).center, desired) < .015f,
                "desired=" + desired + ", actual=" + VisualBounds(small).center);
            await Settle(small, table.bounds.max.y + .003f, "drop " + small.name + " on " + table.name + " from " + height.ToString("F2") + "m");
            Restore(small, parent, position, rotation);
        }
        foreach (var a in own) foreach (var b in others) Physics.IgnoreCollision(a, b, false);
    }
    [MenuItem("Tools/Geo Island/Run Full Interaction Audit")]
    public static async void Run()
    {
        if (IsRunning || !EditorApplication.isPlaying) return; IsRunning = true;
        activeReportPath = ReportPath;
        rows.Clear(); actions.Clear(); inventory.Clear(); failures = 0; started = DateTime.UtcNow;
        Vector3 oldPosition = Vector3.zero; Quaternion oldRotation = Quaternion.identity;
        try
        {
            root = GameObject.Find("Geo Island Integration"); rig = UnityEngine.Object.FindFirstObjectByType<XROrigin>(); eye = rig ? rig.Camera : null;
            Check("Geo root and XR camera", root && rig && eye); if (!root || !rig || !eye) return;
            oldPosition = rig.transform.position; oldRotation = rig.transform.rotation;
            controller = new GameObject("Geo audit queued controller"); controller.AddComponent<Rigidbody>().isKinematic = true;
            var trigger = controller.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.radius = .025f;
            hand = controller.AddComponent<NearFarInteractor>(); hand.interactionManager = UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
            hand.enableUIInteraction = true; hand.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            hand.uiPressInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            var attach = hand.interactionAttachController as InteractionAttachController; attach.transformToFollow = controller.transform;
            attach.useMomentum = false; attach.useDistanceBasedVelocityScaling = false; attach.useManipulationInput = false;
            await Pause(1500);
            var items = root.GetComponentsInChildren<XRGrabInteractable>(true).OrderBy(x => x.name, StringComparer.Ordinal).ToArray();
            authoredPoses = JArray.Parse(File.ReadAllText(Path.GetFullPath("../Game_Plans/GeoAuthoredPresentationBaseline.json")));
            var buttons = root.GetComponentsInChildren<Button>(true);
            var tables = root.GetComponentsInChildren<BoxCollider>(true).Where(c => c.name.StartsWith("Tabletop support - ")).ToArray();
            Check("all 61 source pickups present", items.Length == 61, "count=" + items.Length);
            Check("all six source buttons present", buttons.Length == 6, "count=" + buttons.Length);
            Check("all six tabletop supports present", tables.Length == 6, "count=" + tables.Length);
            Check("table colliders match tabletop dimensions", tables.All(t => t.bounds.size.y > .15f && t.bounds.size.y < .17f && t.bounds.size.z < 4f));
            Check("all untouched pickups retain their authored pose until grabbed", items.All(i => i.GetComponent<Rigidbody>().isKinematic && !i.GetComponent<Rigidbody>().useGravity));
            for (int i = 0; i < buttons.Length; i++) await TestButton(buttons[i], i + 1);
            for (int i = 0; i < items.Length; i++) await TestPickup(items[i], i + 1, tables);
            await TestTables(tables, items.Single(x => x.name == "Cylinder.020")); await Pause(1000);
            Check("no pickup falls below island", items.All(x => x.transform.position.y > -.2f), string.Join(",", items.Where(x => x.transform.position.y <= -.2f).Select(x => x.name)));
            Check("no missing Geo scripts", root.GetComponentsInChildren<Transform>(true).All(t => t.GetComponents<Component>().All(c => c)));
            Check("all 67 source actions exercised", actions.Count == 67, "count=" + actions.Count);
        }
        catch (Exception e) { Check("Geo audit exception", false, e.ToString()); }
        finally
        {
            if (hand) { hand.uiPressInput.QueueManualState(false, 0); hand.selectInput.QueueManualState(false, 0); }
            if (rig) rig.transform.SetPositionAndRotation(oldPosition, oldRotation); if (controller) UnityEngine.Object.Destroy(controller);
            hand = null; controller = null; IsRunning = false; Save(failures == 0 ? "passed" : "failed");
            Debug.Log("Geo full verification: " + failures + " failures. " + ReportPath);
        }
    }

    [MenuItem("Tools/Geo Island/Run Table Drop Audit")]
    public static async void RunTableAudit()
    {
        if (IsRunning || !EditorApplication.isPlaying) return;
        IsRunning = true; activeReportPath = TableReportPath;
        rows.Clear(); actions.Clear(); inventory.Clear(); failures = 0; started = DateTime.UtcNow;
        try
        {
            root = GameObject.Find("Geo Island Integration");
            var tables = root.GetComponentsInChildren<BoxCollider>(true).Where(c => c.name.StartsWith("Tabletop support - ")).ToArray();
            var small = root.GetComponentsInChildren<XRGrabInteractable>().Single(x => x.name == "Cylinder.020");
            await Pause(1500);
            await TestTables(tables, small);
        }
        catch (Exception e) { Check("table audit exception", false, e.ToString()); }
        finally
        {
            IsRunning = false; Save(failures == 0 ? "passed" : "failed");
            Debug.Log("Geo table verification: " + failures + " failures. " + TableReportPath);
        }
    }
}
#endif
