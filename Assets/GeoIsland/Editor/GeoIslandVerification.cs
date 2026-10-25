#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class GeoIslandVerification
{
    public static bool IsRunning;
    static readonly List<object> rows = new List<object>();
    static int failures;
    static XROrigin rig;
    static Camera eye;
    static NearFarInteractor hand;
    static GameObject controller;
    static string ReportPath => Path.GetFullPath("../Game_Plans/GeoIslandVerification.json");
    static async Task Pause(int ms = 140) { int frame = Time.frameCount; await Task.Delay(ms); while (EditorApplication.isPlaying && Time.frameCount < frame + 2) await Task.Delay(20); }
    static void Check(string name, bool pass, string detail = "")
    {
        if (!pass) failures++;
        rows.Add(new { name, pass, detail });
        Save("running");
        if (!pass) Debug.LogWarning("Geo QA: " + name + " " + detail);
    }
    static void Save(string status)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, JsonConvert.SerializeObject(new { status, failures, scope = "Play Mode synthetic NearFar controller grip and queued XR UI presses; no physical headset claim.", results = rows }, Formatting.Indented));
    }
    static void Pose(Vector3 handPosition, Vector3 direction)
    {
        hand.transform.SetPositionAndRotation(handPosition, Quaternion.LookRotation(direction.normalized, Vector3.up));
        var eyeTarget = handPosition - direction.normalized * .3f + Vector3.up * .3f;
        rig.transform.position += eyeTarget - eye.transform.position;
        Physics.SyncTransforms();
    }
    static async Task TestButton(Button button)
    {
        var canvas = button.GetComponentInParent<Canvas>();
        var raycaster = canvas ? canvas.GetComponent<TrackedDeviceGraphicRaycaster>() : null;
        var rect = button.GetComponent<RectTransform>();
        var center = rect.TransformPoint(rect.rect.center);
        var origin = center - canvas.transform.forward * .85f;
        Pose(origin, center - origin);
        await Pause(200);
        Check("button available " + center, button.gameObject.activeInHierarchy && button.interactable, button.name);
        var ray = new TrackedDeviceEventData(EventSystem.current) { rayPoints = new List<Vector3> { origin, center + canvas.transform.forward * .05f }, layerMask = ~0 };
        var hits = new List<RaycastResult>();
        if (raycaster) raycaster.Raycast(ray, hits);
        Check("tracked ray " + center, hits.Any(h => h.gameObject == button.gameObject), string.Join(",", hits.Select(h => h.gameObject.name)));
        var target = button.onClick.GetPersistentEventCount() > 0 ? button.onClick.GetPersistentTarget(0) : null;
        var method = button.onClick.GetPersistentEventCount() > 0 ? button.onClick.GetPersistentMethodName(0) : "";
        var light = target as LightToggle;
        var spinner = target as Spinner;
        var music = target as MusicToggle;
        var beforeLight = light && light.targetLight ? light.targetLight.enabled : false;
        var beforeRotation = spinner ? spinner.transform.rotation : Quaternion.identity;
        var source = music ? music.GetComponent<AudioSource>() : null;
        var beforeMusic = source && source.isPlaying;
        int presses = 0;
        UnityEngine.Events.UnityAction listener = () => presses++;
        button.onClick.AddListener(listener);
        hand.uiPressInput.QueueManualState(true, 1);
        await Pause(160);
        hand.uiPressInput.QueueManualState(false, 0);
        await Pause(420);
        button.onClick.RemoveListener(listener);
        Check("queued XR button dispatch " + method, presses == 1, "clicks=" + presses + " button=" + button.name);
        if (light && light.targetLight) Check("light toggled " + light.targetLight.name, light.targetLight.enabled != beforeLight);
        if (spinner) Check("spinner turns " + spinner.name, Quaternion.Angle(beforeRotation, spinner.transform.rotation) > 2f);
        if (music && source) Check("music toggled locally", source.isPlaying != beforeMusic && source.spatialBlend > .9f && source.maxDistance <= 12.1f);
    }
    static async Task TestGrab(GameObject root, bool targeted = false)
    {
        var items = root.GetComponentsInChildren<XRGrabInteractable>(true)
            .Where(x => !targeted || x.name == "Cylinder.001" || x.name == "Cylinder" || x.name == "Cylinder.004" || x.name == "Cylinder.046" || x.name == "Sphere.017" || x.name == "Sphere.018" || x.name == "Sphere.019" || x.name == "Sphere.020" || x.name == "Sphere.021" || x.name == "Cube.105")
            .ToArray();
        int attempted = 0, selected = 0, unreachable = 0;
        foreach (var item in items)
        {
            var collider = item.GetComponents<Collider>().FirstOrDefault(x => x.enabled);
            var body = item.GetComponent<Rigidbody>();
            var renderer = item.GetComponent<Renderer>() ?? item.GetComponentInChildren<Renderer>();
            var id = item.name + "@" + item.transform.position.ToString("F2");
            Check("grab components " + id, collider && collider.enabled && body && renderer,
                "collider=" + (collider ? collider.GetType().Name : "none") + ", rb=" + (body ? "yes" : "no") + ", renderer=" + (renderer ? "yes" : "no"));
            if (!collider || !body || !renderer) continue;
            var size = renderer.bounds.size;
            Check("grab carry scale " + id, size.magnitude < 5f && item.transform.lossyScale.magnitude < 10f,
                "bounds=" + size + ", scale=" + item.transform.lossyScale);
            attempted++;
            var center = collider.bounds.center;
            bool found = false;
            foreach (var direction in new[] { Vector3.back, Vector3.right, Vector3.left, Vector3.forward, Vector3.up, Vector3.down })
            {
                var origin = center + direction * 1.05f;
                Pose(origin, center - origin);
                await Pause(65);
                var valid = new List<IXRInteractable>();
                hand.GetValidTargets(valid);
                if (!valid.Contains(item)) continue;
                found = true;
                hand.selectInput.QueueManualState(true, 1);
                await Pause(180);
                var held = item.interactorsSelecting.Count > 0;
                Check("queued NearFar grip " + id, held, "origin=" + origin);
                hand.selectInput.QueueManualState(false, 0);
                await Pause(110);
                Check("NearFar release " + id, item.interactorsSelecting.Count == 0);
                if (held) selected++;
                break;
            }
            if (!found) { unreachable++; Check("unreachable Geo prop " + id, false, "No valid 1.05 m synthetic NearFar direction"); }
        }
        Check("all reachable Geo props gripped", selected == attempted - unreachable,
            "attempted=" + attempted + ", selected=" + selected + ", unreachable=" + unreachable);
        Check("Geo props reachable count", selected > 0, "selected=" + selected);
        Check("no missing components in Geo hierarchy", root.GetComponentsInChildren<Transform>(true)
            .All(t => t.GetComponents<Component>().All(c => c != null)));
    }
    [MenuItem("Tools/Geo Island/Run Play Verification")]
    public static void Run() => RunCore(false);
    [MenuItem("Tools/Geo Island/Run Targeted Play Verification")]
    public static void RunTargeted() => RunCore(true);
    static async void RunCore(bool targeted)
    {
        if (IsRunning || !EditorApplication.isPlaying) return;
        IsRunning = true;
        rows.Clear(); failures = 0;
        Vector3 oldPosition = Vector3.zero; Quaternion oldRotation = Quaternion.identity;
        try
        {
            var root = GameObject.Find("Geo Island Integration");
            Check("Geo runtime root", root);
            if (!root) return;
            rig = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            eye = rig ? rig.Camera : null;
            Check("XR rig and camera", rig && eye);
            Check("XR UI input module", UnityEngine.Object.FindFirstObjectByType<XRUIInputModule>());
            if (!rig || !eye) return;
            oldPosition = rig.transform.position; oldRotation = rig.transform.rotation;
            controller = new GameObject("Geo verification queued controller");
            controller.AddComponent<Rigidbody>().isKinematic = true;
            controller.AddComponent<SphereCollider>().isTrigger = true;
            hand = controller.AddComponent<NearFarInteractor>();
            hand.interactionManager = UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
            hand.enableNearCasting = true; hand.enableUIInteraction = true;
            hand.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            hand.uiPressInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            var attach = hand.interactionAttachController as InteractionAttachController;
            attach.transformToFollow = controller.transform; attach.useMomentum = false;
            attach.useDistanceBasedVelocityScaling = false; attach.useManipulationInput = false;
            await Pause(300);
            Check("five world panels", root.GetComponentsInChildren<Canvas>(true).Length == 5);
            if (!targeted) foreach (var button in root.GetComponentsInChildren<Button>(true)) await TestButton(button);
            await TestGrab(root, targeted);
            await Pause(600);
            var bodies = root.GetComponentsInChildren<XRGrabInteractable>(true).Select(x => x.GetComponent<Rigidbody>()).Where(x => x).ToArray();
            Check("no prop sinks below island", bodies.All(x => x.position.y > -1f), string.Join(",", bodies.Where(x => x.position.y <= -1f).Select(x => x.name + "@" + x.position.y)));
        }
        catch (Exception e) { Check("Geo harness exception", false, e.ToString()); }
        finally
        {
            if (hand) { hand.uiPressInput.QueueManualState(false, 0); hand.selectInput.QueueManualState(false, 0); }
            if (rig) rig.transform.SetPositionAndRotation(oldPosition, oldRotation);
            if (controller) UnityEngine.Object.Destroy(controller);
            hand = null; controller = null; IsRunning = false;
            Save(failures == 0 ? "passed" : "failed");
            Debug.Log("Geo verification: " + failures + " failures. " + ReportPath);
        }
    }
}
#endif
