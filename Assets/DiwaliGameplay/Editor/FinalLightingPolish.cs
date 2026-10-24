#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiwaliGameplay
{
    public static class FinalLightingPolish
    {
        const string ScenePath = "Assets/Scenes/DiwaliFestivalGround.unity";
        const string UndoLabel = "Polish existing festival activity fills";

        [MenuItem("Diwali/Final Pass/Apply targeted lighting polish")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before applying lighting polish.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException("Open DiwaliFestivalGround before applying lighting polish.");

            var lights = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Light>(true)).ToArray();
            var sweet = Find(lights, "SweetShop warm courtyard fill");
            var diya = Find(lights, "Diya warm courtyard fill");
            var routing = Find(lights, "LanternRouting warm courtyard fill");
            var rangoli = Find(lights, "Rangoli authored display warm fill");
            var cabinet = sweet.transform.parent.GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == "Accessible cabinet group");
            var cabinetRenderers = cabinet.GetComponentsInChildren<MeshRenderer>(true);
            if (cabinetRenderers.Length == 0)
                throw new InvalidOperationException("The sweet cabinet has no mesh bounds.");
            var bounds = cabinetRenderers[0].bounds;
            foreach (var renderer in cabinetRenderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var sweetPosition = bounds.center + Vector3.up * 2.1f - sweet.transform.parent.forward * .65f;

            Debug.Log("Final lighting BEFORE: " + Describe(lights));
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            // Reuse the four authored fills; the key, lamp pools and gameplay flame lights keep their roles.
            Tune(sweet, new Color(1f, .92f, .81f), 2.2f, 10f);
            Undo.RecordObject(sweet.transform, UndoLabel);
            sweet.transform.position = sweetPosition;
            PrefabUtility.RecordPrefabInstancePropertyModifications(sweet.transform);
            Tune(diya, new Color(1f, .91f, .78f), 2.5f, 12f);
            Tune(routing, new Color(1f, .91f, .78f), 2.5f, 12f);
            Tune(rangoli, new Color(1f, .90f, .74f), 2.4f, 6f);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("Final lighting AFTER: " + Describe(lights));
        }

        static Light Find(Light[] lights, string name)
        {
            var matches = lights.Where(light => light.name == name && light.gameObject.activeInHierarchy).ToArray();
            if (matches.Length != 1 || !matches[0].enabled || matches[0].type != LightType.Point)
                throw new InvalidOperationException("Expected one active point fill: " + name);
            return matches[0];
        }

        static void Tune(Light light, Color color, float intensity, float range)
        {
            Undo.RecordObject(light, UndoLabel);
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            EditorUtility.SetDirty(light);
            PrefabUtility.RecordPrefabInstancePropertyModifications(light);
        }

        static string Describe(Light[] lights)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                scene = SceneManager.GetActiveScene().path,
                totalLights = lights.Length,
                activeLights = lights.Count(light => light.isActiveAndEnabled),
                activeShadowLights = lights.Count(light => light.isActiveAndEnabled && light.shadows != LightShadows.None),
                ambientMode = RenderSettings.ambientMode.ToString(),
                ambientIntensity = RenderSettings.ambientIntensity,
                reflectionIntensity = RenderSettings.reflectionIntensity,
                lights = lights.OrderBy(light => light.name).Select(light => new
                {
                    light.name,
                    parent = light.transform.parent ? light.transform.parent.name : "",
                    active = light.gameObject.activeInHierarchy,
                    light.enabled,
                    type = light.type.ToString(),
                    light.intensity,
                    light.range,
                    color = new[] { light.color.r, light.color.g, light.color.b },
                    position = new[] { light.transform.position.x, light.transform.position.y, light.transform.position.z },
                    shadows = light.shadows.ToString()
                })
            });
        }
    }
}
#endif
