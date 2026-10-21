#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FireworksImport
{
    sealed class FireworksTextureDefaults : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(assetPath.StartsWith("Assets/DiwaliFireworks/Textures/",StringComparison.Ordinal))
                FireworksInstaller.ApplyTextureSettings((TextureImporter)assetImporter);
        }
    }
    public static class FireworksInstaller
    {
        const string Root = "Assets/DiwaliFireworks";
        const string Reports = "AgentScripts/FireworksImport";
        const string ScenePath = "Assets/Scenes/DiwaliFestivalGround.unity";
        const string PlotRoot = "Diwali Festival Ground/06 Five Empty Venue Plots";
        static readonly string[] Games = { "Rangoli", "SweetShop", "LanternRouting", "Diya" };
        static readonly float[] Yaws = { 180, 180, 0, -90 };
        static readonly Vector3[] Positions = { new Vector3(-18,0,9), new Vector3(-18,0,-10), new Vector3(18,0,9), new Vector3(18,0,-10) };

        [Serializable] public class Manifest { public Game[] games; }
        [Serializable] public class Game
        {
            public string game, source, sourceSha256, model;
            public Surface[] materials;
            public Part[] objects;
            public Normalization normalization;
        }
        [Serializable] public class Normalization { public string floorObjectName; public float scale; }
        [Serializable] public class Part { public string name; public string[] materials; }
        [Serializable] public class Surface
        {
            public string name, sourceMaterial, baseColor, normal, metallicSmoothness, emission, alphaMode, canonicalMaterial;
            public float[] baseColorTint, emissionColorLinear;
            public float metallic = 1, smoothness = 1, normalScale = 1, alpha = 1, alphaMultiplier = 1, alphaCutoff = .5f, emissionStrength = 1;
            public bool twoSided;
        }
        static void Write(string file, object data)
        {
            Directory.CreateDirectory(Reports);
            File.WriteAllText(Reports + "/" + file, Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
        }
        static float[] Vec(Vector3 value) => new[]{value.x,value.y,value.z};
        static string Hash(string value)
        {
            using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
        }
        static string HierarchyFingerprint(Transform root)
        {
            if (!root) return "absent";
            return Hash(string.Join("\n", root.GetComponentsInChildren<Transform>(true).Select(t =>
                t.name + "|" + t.localPosition.ToString("R") + "|" + t.localRotation.ToString("R") + "|" + t.localScale.ToString("R") + "|" + t.gameObject.activeSelf + "|" +
                string.Join(",",t.GetComponents<Component>().Select(c=>c ? c.GetType().FullName : "MISSING")))));
        }
        static Transform Plots()
        {
            var go=GameObject.Find(PlotRoot);
            if (!go) throw new InvalidOperationException("Festival plot root was not found.");
            return go.transform;
        }
        static void RequireEdit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("This operation requires Edit Mode.");
        }
        static Bounds BoundsOf(GameObject root)
        {
            var renderers=root.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0) throw new InvalidDataException("No renderers in " + root.name);
            var bounds=renderers[0].bounds;
            foreach(var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            return bounds;
        }
        [MenuItem("Diwali Island/Approved Palette/Inspect Scene")]
        public static void Inspect()
        {
            RequireEdit();
            Write("live-scene-inspection.json",new {
                scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,
                plots=Plots().Cast<Transform>().Select(p=>new {name=p.name,position=Vec(p.position),children=p.childCount,
                    anchorChildren=p.Find("PLACE YOUR BLENDER VENUE HERE")?.childCount}).ToArray(),
                roots=SceneManager.GetActiveScene().GetRootGameObjects().Select(r=>new {name=r.name,objects=r.GetComponentsInChildren<Transform>(true).Length}).ToArray(),
                colorSpace=QualitySettings.activeColorSpace.ToString()
            });
        }
        static Manifest LoadManifest()
        {
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/game-import-manifest.json"));
            if(manifest?.games?.Length!=1) throw new InvalidDataException("Fireworks export is required.");
            return manifest;
        }
        static string AssetPath(string relative) => string.IsNullOrEmpty(relative) ? null : Root + "/" + relative;
        static Texture2D Texture(string relative)
        {
            if(string.IsNullOrEmpty(relative)) return null;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(AssetPath(relative));
            if(!texture) throw new InvalidDataException("Missing texture " + relative);
            return texture;
        }
        static void ConfigureTexture(string relative)
        {
            if(string.IsNullOrEmpty(relative)) return;
            var importer=AssetImporter.GetAtPath(AssetPath(relative)) as TextureImporter;
            if(!importer) throw new InvalidDataException("Texture importer missing: " + relative);
            string before=EditorJsonUtility.ToJson(importer);
            ApplyTextureSettings(importer);
            if(before!=EditorJsonUtility.ToJson(importer)) importer.SaveAndReimport();
        }
        internal static void ApplyTextureSettings(TextureImporter importer)
        {
            bool normal=importer.assetPath.EndsWith("_Normal.png",StringComparison.OrdinalIgnoreCase);
            bool mask=importer.assetPath.EndsWith("_MetallicSmoothness.png",StringComparison.OrdinalIgnoreCase);
            importer.textureType=normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture=!normal && !mask;
            importer.convertToNormalmap=false;
            importer.flipGreenChannel=false;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency=!normal && !mask;
            importer.mipmapEnabled=true;
            importer.streamingMipmaps=true;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Trilinear;
            importer.anisoLevel=4;
            importer.maxTextureSize=2048;
            importer.isReadable=false;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            var android=importer.GetPlatformTextureSettings("Android");
            android.name="Android";android.overridden=true;android.maxTextureSize=2048;
            android.format=TextureImporterFormat.ASTC_6x6;android.compressionQuality=80;
            importer.SetPlatformTextureSettings(android);
        }
        static Color ColorOf(float[] rgba, Color fallback)
        {
            if(rgba==null || rgba.Length<3) return fallback;
            return new Color(rgba[0],rgba[1],rgba[2],rgba.Length>3?rgba[3]:1);
        }
        static Material BuildSurface(Surface surface, Shader shader)
        {
            string path=Root+"/Materials/"+(string.IsNullOrEmpty(surface.canonicalMaterial)?surface.name:surface.canonicalMaterial)+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) { material=new Material(shader); AssetDatabase.CreateAsset(material,path); }
            material.shader=shader;material.shaderKeywords=Array.Empty<string>();
            var tint=ColorOf(surface.baseColorTint,Color.white).gamma;
            tint.a=surface.alphaMultiplier;
            material.SetColor("_BaseColor",tint);
            material.SetTexture("_BaseMap",Texture(surface.baseColor));
            material.SetTexture("_BumpMap",Texture(surface.normal));
            material.SetFloat("_BumpScale",surface.normalScale);
            if(!string.IsNullOrEmpty(surface.normal)) material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",Texture(surface.metallicSmoothness));
            material.SetFloat("_Metallic",surface.metallic);
            material.SetFloat("_Smoothness",surface.smoothness);
            material.SetFloat("_SmoothnessTextureChannel",0);
            if(!string.IsNullOrEmpty(surface.metallicSmoothness)) material.EnableKeyword("_METALLICSPECGLOSSMAP");
            bool blend=surface.alphaMode=="Blend",clip=surface.alphaMode=="Clip";
            material.SetFloat("_Surface",blend?1:0);material.SetFloat("_Blend",0);
            material.SetFloat("_SrcBlend",(float)(blend?BlendMode.SrcAlpha:BlendMode.One));
            material.SetFloat("_DstBlend",(float)(blend?BlendMode.OneMinusSrcAlpha:BlendMode.Zero));
            material.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha",(float)(blend?BlendMode.OneMinusSrcAlpha:BlendMode.Zero));
            material.SetFloat("_ZWrite",blend?0:1);
            material.SetFloat("_Cull",(float)(surface.twoSided||blend?CullMode.Off:CullMode.Back));
            material.SetFloat("_AlphaClip",clip?1:0);material.SetFloat("_Cutoff",surface.alphaCutoff);
            material.SetOverrideTag("RenderType",blend?"Transparent":clip?"TransparentCutout":"Opaque");
            material.renderQueue=blend?(int)RenderQueue.Transparent:clip?(int)RenderQueue.AlphaTest:(int)RenderQueue.Geometry;
            if(blend) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if(clip) material.EnableKeyword("_ALPHATEST_ON");
            material.SetShaderPassEnabled("ShadowCaster",!blend);
            material.SetShaderPassEnabled("DepthOnly",!blend);
            material.SetShaderPassEnabled("DepthNormals",!blend);
            material.SetTexture("_EmissionMap",Texture(surface.emission));
            var emission=ColorOf(surface.emissionColorLinear,Color.black);
            material.SetColor("_EmissionColor",emission);
            bool emits=emission.maxColorComponent>0;
            if(emits) material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags=emits?MaterialGlobalIlluminationFlags.BakedEmissive:MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(material);
            return material;
        }
        static void BuildGame(string key)
        {
            RequireEdit();
            var game=LoadManifest().games.Single(g=>g.game==key);
            Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Prefabs");
            AssetDatabase.Refresh();
            foreach(var path in game.materials.SelectMany(m=>new[]{m.baseColor,m.normal,m.metallicSmoothness,m.emission}).Where(p=>!string.IsNullOrEmpty(p)).Distinct()) ConfigureTexture(path);
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(!shader) throw new InvalidOperationException("URP Lit unavailable.");
            var materials=game.materials.ToDictionary(s=>s.name,s=>BuildSurface(s,shader));
            var importer=AssetImporter.GetAtPath(AssetPath(game.model)) as ModelImporter;
            if(!importer) throw new InvalidDataException("Missing FBX: "+game.model);
            importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
            importer.isReadable=false;importer.meshCompression=ModelImporterMeshCompression.Off;
            importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var previewScene=EditorSceneManager.NewPreviewScene();
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath(game.model)),previewScene);
            try
            {
                instance.name=key+" - Native PBR";
                var renderers=instance.GetComponentsInChildren<Renderer>(true);
                foreach(var renderer in renderers)
                {
                    var slots=renderer.sharedMaterials;
                    for(int i=0;i<slots.Length;i++)
                    {
                        string materialName=slots[i]?slots[i].name.Replace(" (Instance)",""):"";
                        if(!materials.TryGetValue(materialName,out var replacement)) throw new InvalidDataException(key+" unmatched material "+materialName+" on "+renderer.name);
                        slots[i]=replacement;
                    }
                    renderer.sharedMaterials=slots;
                }
                var bounds=BoundsOf(instance);
                if(Mathf.Max(bounds.size.x,bounds.size.z)>9.8f) throw new InvalidDataException(key+" exceeds plot bounds: "+bounds.size);
                if(renderers.Length!=game.objects.Length) throw new InvalidDataException(key+" object count changed during import.");
                PrefabUtility.SaveAsPrefabAsset(instance,Root+"/Prefabs/"+key+"_Approved.prefab");
                Write(key+"-unity-import.json",new {game=key,source=game.source,sourceSha256=game.sourceSha256,
                    renderers=renderers.Length,slots=renderers.Sum(r=>r.sharedMaterials.Length),materials=materials.Count,
                    bounds=new{min=Vec(bounds.min),max=Vec(bounds.max),size=Vec(bounds.size)},
                    floor=game.normalization.floorObjectName,
                    parts=renderers.Select(r=>new {name=r.name,center=Vec(r.bounds.center),size=Vec(r.bounds.size),materials=r.sharedMaterials.Select(m=>m.name).ToArray()}).ToArray()});
            }
            finally { UnityEngine.Object.DestroyImmediate(instance);EditorSceneManager.ClosePreviewScene(previewScene); }
            AssetDatabase.SaveAssets();
            Debug.Log("Approved palette prefab built: "+key);
        }

        [MenuItem("Diwali Island/Fireworks/Build")]
        public static void Build()=>BuildGame("Fireworks");
        [MenuItem("Diwali Island/Fireworks/Place")]
        public static void Place(){
            RequireEdit();var scene=SceneManager.GetActiveScene();
            if(scene.path!=ScenePath || scene.isDirty)throw new InvalidOperationException("Expected clean festival scene");
            var plots=Plots().Cast<Transform>().ToArray();
            var plot=plots.Single(p=>p.name.StartsWith("05 "));var anchor=plot.Find("PLACE YOUR BLENDER VENUE HERE");
            if(anchor.childCount!=0)throw new InvalidOperationException("Top plot occupied");
            var protectedPlots=plots.Where(p=>p!=plot).ToDictionary(p=>p,p=>HierarchyFingerprint(p));
            var external=scene.GetRootGameObjects().Where(g=>g.name!="Diwali Festival Ground").ToDictionary(g=>g, g=>HierarchyFingerprint(g.transform));
            string backup="Scene_Backups/FireworksImport/DiwaliFestivalGround.before-fireworks-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ")+".unity";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if(!EditorSceneManager.SaveScene(scene,backup,true))throw new IOException("Backup failed");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Fireworks_Approved.prefab");
            if(!prefab)throw new InvalidDataException("Missing fireworks prefab");
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Import fireworks venue on fifth plot");
            try{
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,anchor);Undo.RegisterCreatedObjectUndo(instance,"Place fireworks venue");
                instance.name="Fireworks - Native PBR";
                // Fit measured source floor within the usable plot, leaving its edge markers visible.
                instance.transform.localScale=Vector3.one*1.05f;
                instance.transform.localPosition=new Vector3(0,.03f,0);
                instance.transform.localRotation=Quaternion.Euler(0,-90,0);
                var bounds=BoundsOf(instance);var floor=plot.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Empty venue floor 5").bounds;
                if(bounds.min.x<floor.min.x || bounds.max.x>floor.max.x || bounds.min.z<floor.min.z || bounds.max.z>floor.max.z)throw new InvalidDataException("Fireworks outside usable plot");
                var renderers=instance.GetComponentsInChildren<Renderer>(true);
                if(renderers.Length!=13 || renderers.Any(r=>r.sharedMaterials.Any(m=>!m || !m.shader || m.shader.name.Contains("InternalError"))))throw new InvalidDataException("Fireworks renderer/material validation failed");
                bool protectedUnchanged=protectedPlots.All(kv=>HierarchyFingerprint(kv.Key)==kv.Value)&&external.All(kv=>HierarchyFingerprint(kv.Key.transform)==kv.Value);
                if(!protectedUnchanged)throw new InvalidOperationException("Protected objects changed");
                Undo.RecordObject(plot.gameObject,"Name occupied fireworks plot");plot.name="05 FIREWORKS PAVILION - 10m x 10m";
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Undo.CollapseUndoOperations(group);
                Write("placement-report.json",new{scene=scene.path,saved=!scene.isDirty,backup,protectedUnchanged,rootLocalScale=Vec(instance.transform.localScale),rootWorldScale=Vec(instance.transform.lossyScale),position=Vec(instance.transform.position),yaw=instance.transform.eulerAngles.y,renderers=renderers.Length,missingMaterials=0,bounds=new{min=Vec(bounds.min),max=Vec(bounds.max)},plotBounds=new{min=Vec(floor.min),max=Vec(floor.max)}});
            }catch{Undo.RevertAllDownToGroup(group);throw;}
        }
        [MenuItem("Diwali Island/Fireworks/Render")]
        public static void Render(){
            var root=GameObject.Find("Fireworks - Native PBR");if(!root)throw new InvalidOperationException("Fireworks missing");
            var b=BoundsOf(root);string folder=Path.GetFullPath("../Scene_Previews/Fireworks-Imported");Directory.CreateDirectory(folder);
            var shots=new[]{new{name="Fireworks-Front",eye=b.center+new Vector3(0,10,-17),target=b.center},new{name="Fireworks-Overview",eye=b.center+new Vector3(13,19,-15),target=b.center},new{name="Festival-All-Five",eye=new Vector3(0,85,-30),target=new Vector3(0,0,27)}};
            foreach(var s in shots){
                var go=new GameObject("Temporary fireworks review camera");go.hideFlags=HideFlags.HideAndDontSave;var cam=go.AddComponent<Camera>();cam.GetUniversalAdditionalCameraData().allowXRRendering=false;
                cam.fieldOfView=55;cam.farClipPlane=500;cam.nearClipPlane=.1f;cam.allowHDR=true;cam.clearFlags=CameraClearFlags.Skybox;go.transform.position=s.eye;go.transform.LookAt(s.target);
                var rt=new RenderTexture(1600,1100,24);var old=RenderTexture.active;Texture2D pixels=null;
                try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;pixels=new Texture2D(1600,1100,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,1100),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,s.name+".png"),pixels.EncodeToPNG());}
                finally{cam.targetTexture=null;RenderTexture.active=old;if(pixels)UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
            }
            Write("render-report.json",new{folder,images=shots.Select(s=>s.name+".png").ToArray(),dirty=SceneManager.GetActiveScene().isDirty});
        }
    }
}
#endif
