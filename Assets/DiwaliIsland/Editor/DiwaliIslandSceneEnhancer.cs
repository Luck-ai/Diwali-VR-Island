using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DiwaliIsland.Editor
{
    public static class DiwaliIslandSceneEnhancer
    {
        const string RootPath = "Assets/DiwaliIsland";
        static Transform detail;

        [MenuItem("Diwali Island/Enhance Island Environment")]
        public static void Enhance()
        {
            var island = GameObject.Find("Diwali Island");
            if (island == null || SceneManager.GetActiveScene().path != "Assets/Scenes/DiwaliIsland.unity")
                throw new InvalidOperationException("Open the DiwaliIsland scene first.");
            if (island.transform.Find("09 Island Enhancements") != null)
                throw new InvalidOperationException("Enhancements already exist; edit their hierarchy directly.");

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Enhance Diwali island environment");
            int group = Undo.GetCurrentGroup();
            detail = Group("09 Island Enhancements", island.transform);
            Undo.RegisterCreatedObjectUndo(detail.gameObject, "Enhance Diwali island environment");
            WaterAndShore();
            SeatingAndPlanting();
            VenueDetails(island.transform.Find("03 Five Venues"));
            Atmosphere();
            PostProcessing(island);
            foreach (var renderer in detail.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.GetComponent<TextMeshPro>() != null) continue;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = island;
            Debug.Log("DIWALI_ENHANCED: shoreline, water, seating, planting, venue trim and subtle URP post-processing. No Blender models included.");
        }

        static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static Material Material(string name) => AssetDatabase.LoadAssetAtPath<Material>(RootPath + "/Materials/" + name + ".mat");

        static Material NewMaterial(string name, string shader, Color color)
        {
            var material = new Material(Shader.Find(shader)) { name = name };
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, RootPath + "/Materials/" + name + ".mat");
            return material;
        }

        static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 size, string material, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Material(material);
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, string material, bool collider = false)
            => Shape(name, PrimitiveType.Cube, parent, position, size, material, collider);

        static void Annulus(string name, float inner, float outer, float height, string material, bool broken)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int count = 128;
            for (int i = 0; i < count; i++)
            {
                if (broken && (i / 7) % 3 == 0) continue;
                float a = i * Mathf.PI * 2 / count, b = (i + 1) * Mathf.PI * 2 / count;
                float wa = Mathf.Sin(a * 3) * 0.6f + Mathf.Sin(a * 7) * 0.2f;
                float wb = Mathf.Sin(b * 3) * 0.6f + Mathf.Sin(b * 7) * 0.2f;
                int start = vertices.Count;
                vertices.Add(new Vector3(Mathf.Cos(a) * (inner + wa), height, Mathf.Sin(a) * (inner + wa)));
                vertices.Add(new Vector3(Mathf.Cos(a) * (outer + wa), height, Mathf.Sin(a) * (outer + wa)));
                vertices.Add(new Vector3(Mathf.Cos(b) * (inner + wb), height, Mathf.Sin(b) * (inner + wb)));
                vertices.Add(new Vector3(Mathf.Cos(b) * (outer + wb), height, Mathf.Sin(b) * (outer + wb)));
                triangles.AddRange(new[] { start, start + 2, start + 1, start + 1, start + 2, start + 3 });
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, RootPath + "/Geometry/Enhancement-" + name.Replace(" ", "-") + ".asset");
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(detail, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<Renderer>().sharedMaterial = Material(material);
        }

        static void WaterAndShore()
        {
            NewMaterial("ShallowWater", "Universal Render Pipeline/Unlit", new Color(0.10f, 0.31f, 0.35f));
            NewMaterial("SoftSeaFoam", "Universal Render Pipeline/Unlit", new Color(0.43f, 0.59f, 0.57f));
            var water = new Material(Shader.Find("DiwaliIsland/FestivalWater")) { name = "Festival Water" };
            water.SetColor("_DeepColor", new Color(0.025f, 0.095f, 0.15f));
            water.SetColor("_RippleColor", new Color(0.10f, 0.27f, 0.33f));
            AssetDatabase.CreateAsset(water, RootPath + "/Materials/FestivalWater.mat");
            var sea = GameObject.Find("Diwali Island").GetComponentsInChildren<MeshRenderer>().First(r => r.name == "Quiet sea - no collision");
            Undo.RegisterCompleteObjectUndo(sea, "Enhance water");
            sea.sharedMaterial = water;
            Annulus("Shallow coastal band", 25.6f, 32.4f, -1.105f, "ShallowWater", false);
            Annulus("Inner foam arc", 27.5f, 27.64f, -1.085f, "SoftSeaFoam", true);
            Annulus("Outer foam arc", 30.2f, 30.27f, -1.08f, "SoftSeaFoam", true);
            Annulus("Grass and sand edge", 23.38f, 23.54f, 0.026f, "Sand", false);
        }

        static void Bench(Vector3 position, float yaw)
        {
            var p = Group("Plaza timber bench", detail);
            p.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            for (int i = 0; i < 3; i++)
                Box("Bench seat slat", p, new Vector3(0, 0.48f, -0.18f + i * 0.18f), new Vector3(2.1f, 0.08f, 0.15f), "WoodLight", true);
            foreach (float x in new[] { -0.82f, 0.82f })
            {
                Box("Bench leg", p, new Vector3(x, 0.23f, 0), new Vector3(0.13f, 0.46f, 0.45f), "Wood");
                Box("Bench back upright", p, new Vector3(x, 0.72f, 0.29f), new Vector3(0.10f, 0.69f, 0.10f), "Wood");
            }
            for (int i = 0; i < 2; i++)
                Box("Bench back slat", p, new Vector3(0, 0.78f + i * 0.20f, 0.29f), new Vector3(2.1f, 0.15f, 0.08f), "WoodLight", true);
        }

        static void SeatingAndPlanting()
        {
            Bench(new Vector3(-6.9f, 0.025f, -0.5f), 90);
            Bench(new Vector3(7.0f, 0.025f, -5.7f), -40);
            foreach (var position in new[] { new Vector3(-21,0,-10), new Vector3(-20,0,14), new Vector3(18,0,18), new Vector3(20,0,-10), new Vector3(-2,0,23) })
            {
                var patch = Group("Coastal flower bed", detail);
                patch.position = position;
                Shape("Flower bed border", PrimitiveType.Cylinder, patch, new Vector3(0,0.07f,0), new Vector3(2.3f,0.06f,1.5f), "Terracotta");
                Shape("Flower bed foliage", PrimitiveType.Sphere, patch, new Vector3(0,0.22f,0), new Vector3(2.1f,0.34f,1.35f), "Leaves");
                for (int i = 0; i < 7; i++)
                {
                    float a = i * 2.399f;
                    var pos = new Vector3(Mathf.Cos(a)*0.78f,0.38f,Mathf.Sin(a)*0.43f);
                    Shape("Marigold flower cluster", PrimitiveType.Sphere, patch, pos, Vector3.one*0.25f, i%2==0 ? "Orange" : "Gold");
                }
            }
        }

        static void VenueDetails(Transform venues)
        {
            foreach (Transform venue in venues)
            {
                var trim = Group(venue.name + " architectural details", detail);
                trim.SetPositionAndRotation(venue.position, venue.rotation);
                if (venue.name.Contains("SWEET"))
                {
                    for (int i = 0; i < 14; i++)
                        Box("Sweet shop striped valance", trim, new Vector3(-3.25f+i*0.50f,2.2f,-3.21f), new Vector3(0.48f,0.20f,0.12f), i%2==0 ? "Cream" : "Terracotta");
                    for (int i = 0; i < 2; i++)
                    {
                        var window = Group("Warm sweet shop side window", trim);
                        window.localPosition = new Vector3(-3.82f,1.85f,-1.65f+i*3.0f);
                        Box("Window brass frame",window,Vector3.zero,new Vector3(0.10f,0.95f,1.30f),"Gold");
                        Box("Window dark inset",window,new Vector3(-0.065f,0,0),new Vector3(0.04f,0.78f,1.13f),"Navy");
                        for(int j=0;j<3;j++)Box("Window warm pane",window,new Vector3(-0.09f,0,-0.36f+j*0.36f),new Vector3(0.03f,0.61f,0.28f),"Glow");
                    }
                }
                if (venue.name.Contains("LANTERN"))
                {
                    foreach(float x in new[]{-3.75f,3.75f}) for(int j=0;j<5;j++)
                        Shape("Pergola climbing foliage",PrimitiveType.Sphere,trim,new Vector3(x,0.50f+j*0.42f,-3.20f),new Vector3(0.43f,0.52f,0.45f),"Leaves");
                    Box("Garden entry threshold",trim,new Vector3(0,0.06f,-3.66f),new Vector3(5.8f,0.05f,0.4f),"Teal");
                }
                if (venue.name.Contains("DIYA"))
                {
                    for(int side=-1;side<=1;side+=2) for(int j=0;j<9;j++)
                        Shape("Courtyard marigold column",PrimitiveType.Sphere,trim,new Vector3(side*3.1f,0.38f+j*0.25f,-2.55f),new Vector3(0.20f,0.21f,0.20f),j%2==0?"Orange":"Gold");
                    Box("Courtyard entry step",trim,new Vector3(0,0.055f,-3.50f),new Vector3(6f,0.045f,0.42f),"Cream");
                }
                if (venue.name.Contains("PIER"))
                {
                    Box("Pier navy fascia",trim,new Vector3(0,2.75f,-3.47f),new Vector3(7.55f,0.23f,0.17f),"Navy");
                    for(int i=0;i<15;i++)Box("Arcade gold fascia stripe",trim,new Vector3(-3.5f+i*0.5f,2.75f,-3.57f),new Vector3(0.16f,0.23f,0.04f),"Gold");
                    foreach(float x in new[]{-3.4f,3.4f})
                    {
                        Shape("Dock corner bollard",PrimitiveType.Cylinder,trim,new Vector3(x,0.38f,-3.1f),new Vector3(0.28f,0.36f,0.28f),"Wood",true);
                        Shape("Bollard gold cap",PrimitiveType.Cylinder,trim,new Vector3(x,0.76f,-3.1f),new Vector3(0.36f,0.03f,0.36f),"Gold");
                    }
                }
            }
        }

        static void Atmosphere()
        {
            var sky = Material("DiwaliDuskSky");
            Undo.RegisterCompleteObjectUndo(sky, "Enhance dusk sky");
            sky.SetColor("_ZenithColor",new Color(0.035f,0.045f,0.12f));
            sky.SetColor("_HorizonColor",new Color(0.46f,0.25f,0.36f));
            sky.SetColor("_LowerColor",new Color(0.055f,0.14f,0.20f));
            EditorUtility.SetDirty(sky);
            RenderSettings.fogColor=new Color(0.22f,0.27f,0.37f);
            RenderSettings.fogStartDistance=85;
            RenderSettings.fogEndDistance=190;
            RenderSettings.ambientSkyColor=new Color(0.47f,0.48f,0.64f);
            RenderSettings.ambientEquatorColor=new Color(0.45f,0.38f,0.48f);
            RenderSettings.ambientGroundColor=new Color(0.25f,0.27f,0.29f);
            var sun=RenderSettings.sun;
            Undo.RegisterCompleteObjectUndo(sun,"Enhance warm dusk light");
            sun.intensity=1.45f;
            sun.color=new Color(1f,0.83f,0.70f);
        }

        static void PostProcessing(GameObject island)
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RootPath+"/DiwaliIslandPipeline.asset");
            Undo.RegisterCompleteObjectUndo(pipeline,"Enable island HDR");
            pipeline.supportsHDR=true;
            var pipelineData=new SerializedObject(pipeline);
            var renderers=pipelineData.FindProperty("m_RendererDataList");
            var source=(ScriptableRendererData)renderers.GetArrayElementAtIndex(0).objectReferenceValue;
            var renderer=UnityEngine.Object.Instantiate(source);
            renderer.name="Diwali Island Renderer";
            AssetDatabase.CreateAsset(renderer,RootPath+"/DiwaliIslandRenderer.asset");
            var rendererData=new SerializedObject(renderer);
            rendererData.FindProperty("postProcessData").objectReferenceValue=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            rendererData.ApplyModifiedPropertiesWithoutUndo();
            renderers.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
            pipelineData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);

            var profile=ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name="Diwali Island Post Processing";
            AssetDatabase.CreateAsset(profile,RootPath+"/DiwaliIslandPostProcessing.asset");
            var bloom=profile.Add<Bloom>();
            bloom.threshold.Override(1.1f);bloom.intensity.Override(0.28f);bloom.scatter.Override(0.55f);
            bloom.downscale.Override(BloomDownscaleMode.Quarter);bloom.maxIterations.Override(3);
            var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.Neutral);
            var color=profile.Add<ColorAdjustments>();
            color.postExposure.Override(0.15f);color.saturation.Override(12);color.contrast.Override(8);
            foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
            EditorUtility.SetDirty(profile);
            var volume=Group("Island subtle bloom and color",detail).gameObject.AddComponent<Volume>();
            volume.isGlobal=true;volume.priority=10;volume.sharedProfile=profile;
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var data=camera.GetUniversalAdditionalCameraData();
                Undo.RegisterCompleteObjectUndo(data,"Enable island camera post-processing");
                Undo.RegisterCompleteObjectUndo(camera,"Enable island camera HDR");
                data.renderPostProcessing=true;data.volumeLayerMask|=1;camera.allowHDR=true;
                EditorUtility.SetDirty(data);
            }
        }
    }
}
