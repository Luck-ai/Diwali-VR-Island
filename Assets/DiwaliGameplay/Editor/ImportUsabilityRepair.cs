#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace DiwaliGameplay {
public static class ImportUsabilityRepair {
 const string Dir="Assets/DiwaliApprovedGames/ImportRepairs";
 static void DoubleSided(Transform part){if(!part)return;foreach(var renderer in part.GetComponentsInChildren<MeshRenderer>(true))foreach(var material in renderer.sharedMaterials){if(!material)continue;if(material.HasProperty("_Cull"))material.SetFloat("_Cull",0);material.doubleSidedGI=true;EditorUtility.SetDirty(material);}}
 [MenuItem("Diwali/Gameplay/Use standalone Routing green glass")]
 public static void GreenRouting(){if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");
  string path=Dir+"/Routing-GreenGlass.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
  material.name="Routing - standalone green crystal glass";material.SetColor("_BaseColor",new Color(.025f,.5f,.008f,.58f));material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.9f);material.SetFloat("_Cull",0);material.SetFloat("_Surface",1);material.SetFloat("_ZWrite",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
  var game=Object.FindFirstObjectByType<RoutingGame>();foreach(var piece in game.pieces.Skip(1))foreach(var r in piece.GetComponentsInChildren<MeshRenderer>()){r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();EditorUtility.SetDirty(r);}EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
 }
 [MenuItem("Diwali/Gameplay/Refresh latest Routing connector materials")]
 public static void RefreshRouting(){if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");
  string folder=Dir+"/RoutingRefresh";var data=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(folder+"/materials.json"));
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/RoutingLatest.fbx");var venue=GameObjectUtility.FindExact("LanternRouting - Approved Warm Palette");
  foreach(var source in model.GetComponentsInChildren<MeshFilter>()){
   var target=GameObjectUtility.FindUnder(venue,source.name);if(!target)throw new System.InvalidOperationException("Routing part missing "+source.name);
   var mats=data["materials"].Where(x=>(string)x["object"]==source.name).Select(row=>{
    string path=folder+"/"+(string)row["name"]+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
    foreach(var pair in new[]{new[]{"baseColor","_BaseMap"},new[]{"normal","_BumpMap"},new[]{"metallicSmoothness","_MetallicGlossMap"}}){string texPath=folder+"/"+(string)row[pair[0]];var imp=AssetImporter.GetAtPath(texPath) as TextureImporter;if(imp){imp.textureType=pair[0]=="normal"?TextureImporterType.NormalMap:TextureImporterType.Default;imp.sRGBTexture=pair[0]=="baseColor";imp.SaveAndReimport();}m.SetTexture(pair[1],AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));}
    m.SetColor("_BaseColor",Color.white);m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",.55f);m.SetFloat("_Cull",0);m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_METALLICSPECGLOSSMAP");EditorUtility.SetDirty(m);return m;
   }).ToArray();target.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;target.GetComponent<MeshRenderer>().sharedMaterials=mats;EditorUtility.SetDirty(target.GetComponent<MeshFilter>());EditorUtility.SetDirty(target.GetComponent<MeshRenderer>());
  }AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
 }
 [MenuItem("Diwali/Gameplay/Repair imported surfaces and feather")]
 public static void Apply(){if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");
 var sweet=Object.FindFirstObjectByType<SweetGame>();if(sweet)foreach(var basket in sweet.baskets)DoubleSided(basket.transform);
 var venue=GameObjectUtility.FindExact("Diya - Approved Warm Palette");if(venue){foreach(var name in new[]{"Sphere","Sphere.001","Sphere.002","Cylinder.003"})DoubleSided(GameObjectUtility.FindUnder(venue,name));
 var feather=GameObjectUtility.FindUnder(venue,"Cylinder.003");if(feather&&!GameObjectUtility.FindUnder(feather,"Original feather strands")){
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/FeatherHair.fbx");if(!model)throw new System.InvalidOperationException("Feather strand FBX missing");var source=model.GetComponentsInChildren<MeshFilter>().First();
  var go=new GameObject("Original feather strands");go.transform.SetParent(feather,false);var filter=go.AddComponent<MeshFilter>();var renderer=go.AddComponent<MeshRenderer>();
  var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(source.sharedMesh)) as ModelImporter;if(importer&&!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();source=model.GetComponentsInChildren<MeshFilter>().First();}
  var mesh=Object.Instantiate(source.sharedMesh);mesh.name="Converted source feather strands";
  var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=source.transform.localToWorldMatrix.MultiplyPoint3x4(vertices[i]);
  // The approved FBX stores each object's source pivot in the transform and its vertices relative to that pivot.
  var approved=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DiwaliApprovedGames/Models/Diya_Approved.fbx");var original=approved.GetComponentsInChildren<MeshFilter>().First(x=>x.name=="Cylinder.003");
  var inverse=original.transform.worldToLocalMatrix;for(int i=0;i<vertices.Length;i++)vertices[i]=inverse.MultiplyPoint3x4(vertices[i]);mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();
  string meshPath=Dir+"/FeatherStrands.asset";AssetDatabase.DeleteAsset(meshPath);AssetDatabase.CreateAsset(mesh,meshPath);filter.sharedMesh=mesh;
  var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name="Original feather strands - green satin";material.SetColor("_BaseColor",new Color(.12f,.36f,.16f));material.SetFloat("_Smoothness",.28f);material.SetFloat("_Cull",0);AssetDatabase.CreateAsset(material,Dir+"/FeatherStrands.mat");renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
 }
 }
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
 }
}
}
#endif
