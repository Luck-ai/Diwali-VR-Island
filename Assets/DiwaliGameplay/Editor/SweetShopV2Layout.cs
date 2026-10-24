#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiwaliGameplay {
public static class SweetShopV2Layout {
 const string Folder="Assets/DiwaliApprovedGames/SweetShopV2/";
 const string Stamp="SweetShop v2 authored shelves layout";
 [MenuItem("Diwali/Gameplay/Use sweetfull2 shop layout")]
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Apply in Edit Mode before gameplay caches its starting poses.");
  var venue=GameObjectUtility.FindExact("SweetShop - Approved Warm Palette");
  var game=UnityEngine.Object.FindFirstObjectByType<SweetGame>();
  if(!venue||!game)throw new InvalidOperationException("Sweet shop or its installed gameplay is missing.");
  if(venue.Find(Stamp)){Debug.Log("sweetfull2 layout already applied.");return;}
  AssetDatabase.Refresh();
  string modelPath=Folder+"SweetShopV2_ChangedWalls.fbx";
  var importer=AssetImporter.GetAtPath(modelPath) as ModelImporter;
  if(!importer)throw new InvalidOperationException("sweetfull2 wall export missing.");
  var original=AssetImporter.GetAtPath("Assets/DiwaliApprovedGames/Models/SweetShop_Approved.fbx") as ModelImporter;
  importer.globalScale=original.globalScale;importer.useFileScale=original.useFileScale;
  importer.bakeAxisConversion=original.bakeAxisConversion;importer.importNormals=ModelImporterNormals.Import;
  importer.importTangents=ModelImporterTangents.CalculateMikk;importer.isReadable=true;
  importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
  Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Replace shop with sweetfull2 and arrange work areas");
  foreach(string name in new[]{"Cube.013","Cube.014"}){
   var target=GameObjectUtility.FindUnder(venue,name);
   var source=model.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.name==name);
   var mf=target.GetComponent<MeshFilter>();var renderer=target.GetComponent<MeshRenderer>();var before=renderer.bounds;
   Undo.RecordObjects(new UnityEngine.Object[]{target,mf,renderer},"Import authored low basket shelves");
   target.localPosition=source.transform.localPosition;target.localRotation=source.transform.localRotation;target.localScale=source.transform.localScale;
   mf.sharedMesh=source.sharedMesh;var material=WallMaterial(name);
   renderer.sharedMaterials=Enumerable.Repeat(material,source.sharedMesh.subMeshCount).ToArray();
   var after=renderer.bounds;
   if(Mathf.Abs(before.min.y-after.min.y)>.01f||Mathf.Abs(before.max.y-after.max.y)>.01f||Mathf.Abs(before.min.z-after.min.z)>.01f||Mathf.Abs(before.max.z-after.max.z)>.01f||Mathf.Abs(before.max.x-after.max.x)>.01f)throw new InvalidOperationException("Wall floor or outer alignment changed unexpectedly: "+name+" "+before+" -> "+after);
   foreach(var old in target.GetComponents<Collider>()){if(old is MeshCollider mesh){Undo.RecordObject(mesh,"Update wall collision");mesh.sharedMesh=source.sharedMesh;mesh.convex=false;}else{Undo.RecordObject(old,"Replace approximate wall collision");old.enabled=false;}}
   if(!target.GetComponent<MeshCollider>()){var collider=Undo.AddComponent<MeshCollider>(target.gameObject);collider.sharedMesh=source.sharedMesh;}
   EditorUtility.SetDirty(mf);EditorUtility.SetDirty(renderer);
  }
  var temporary=venue.Find("Reachable basket shelf");if(temporary)Undo.DestroyObjectImmediate(temporary.gameObject);
  var cabinet=GameObjectUtility.FindUnder(venue,"Accessible cabinet group");
  var cabinetMesh=GameObjectUtility.FindUnder(venue,"Cube");
  var cabinetBounds=GameObjectUtility.BoundsOf(cabinetMesh);
  var cabinetDelta=new Vector3(-31f,cabinetBounds.center.y,.35f)-cabinetBounds.center;
  Move(cabinet,cabinet.position+cabinetDelta);
  foreach(var sweet in game.sweets)if(!sweet.transform.IsChildOf(cabinet))Move(sweet.transform,sweet.transform.position+cabinetDelta);
  var fill=GameObjectUtility.FindUnder(venue,"SweetShop warm courtyard fill");if(fill)Move(fill,fill.position+cabinetDelta);
  var counter=GameObjectUtility.FindUnder(venue,"Cube.012");var counterGroup=counter.parent;
  var counterBounds=GameObjectUtility.BoundsOf(counter);var pivot=counterBounds.center;
  var destination=new Vector3(-30.8f,pivot.y,6.45f);var turn=Quaternion.AngleAxis(180,Vector3.up);
  var counterParts=new[]{counterGroup,game.card.transform,game.rest,game.checkout,game.cardReader,game.collection}.Where(t=>t).Distinct().ToArray();
  foreach(var t in counterParts.Where(t=>!counterParts.Any(p=>p!=t&&t.IsChildOf(p)))){
   Undo.RecordObject(t,"Arrange checkout opposite the cabinet");t.SetPositionAndRotation(destination+turn*(t.position-pivot),turn*t.rotation);EditorUtility.SetDirty(t);
  }
  game.workspaceForward=Vector3.back;
  game.closedGlass=game.glass.Select(t=>t.localPosition).ToArray();
  // These anchors remain on the user's modeled inset and reader surfaces, moved with the counter above.
  game.worktopY=game.rest.position.y-(game.basket.transform.position.y-GameObjectUtility.BoundsOf(game.basket.transform).min.y)-.015f;
  game.panelPosition=game.rest.position+Vector3.left*1.25f+Vector3.back*.65f;
  game.panelPosition.y=game.worktopY+.4f;game.panelRotation=Quaternion.LookRotation(Vector3.forward,Vector3.up);
  var supports=new[]{new Vector2(-8.0f,-43.08176f),new Vector2(-7.71494f,-39.87902f)};
  for(int i=0;i<game.baskets.Length;i++){
   var b=game.baskets[i];var p=SourcePoint(venue,supports[Mathf.Min(i,supports.Length-1)]);
   float support=SupportY(venue,p);var bounds=GameObjectUtility.BoundsOf(b.transform);
   p.y=support+(b.transform.position.y-bounds.min.y)+.004f;Move(b.transform,p);
  }
  var marker=new GameObject(Stamp);Undo.RegisterCreatedObjectUndo(marker,"Record authored shop layout");marker.transform.SetParent(venue,false);
  EditorUtility.SetDirty(game);Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
  AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(undo);
  Debug.Log("sweetfull2 imported: authored low shelves, supported baskets, cabinet left, checkout right, clear central aisle. "+Describe(game,venue));
 }
 static void Move(Transform t,Vector3 p){Undo.RecordObject(t,"Arrange sweet shop");t.position=p;EditorUtility.SetDirty(t);}
 static Vector3 SourcePoint(Transform venue,Vector2 xy){const float k=.4214060280789118f;return venue.TransformPoint(new Vector3(-(xy.x+.29651355743408203f)*k,0,-(xy.y+41.253238677978516f)*k));}
 static float SupportY(Transform venue,Vector3 p){
  float height=float.NegativeInfinity;
  foreach(var name in new[]{"Cube.013","Cube.014"}){
   var t=GameObjectUtility.FindUnder(venue,name);var mesh=t.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var triangles=mesh.triangles;
   for(int i=0;i<triangles.Length;i+=3){var a=t.TransformPoint(vertices[triangles[i]]);var b=t.TransformPoint(vertices[triangles[i+1]]);var c=t.TransformPoint(vertices[triangles[i+2]]);
    float det=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(det)<1e-8f)continue;
    float u=((b.z-c.z)*(p.x-c.x)+(c.x-b.x)*(p.z-c.z))/det;float v=((c.z-a.z)*(p.x-c.x)+(a.x-c.x)*(p.z-c.z))/det;
    if(u<-.0001f||v<-.0001f||u+v>1.0001f)continue;float y=u*a.y+v*b.y+(1-u-v)*c.y;
    if(y>venue.position.y+.65f&&y<venue.position.y+1.65f)height=Mathf.Max(height,y);
   }
  }
  if(float.IsNegativeInfinity(height))throw new InvalidOperationException("No authored low shelf under basket at "+p);
  return height;
 }
 static Material WallMaterial(string name){
  string prefix=Folder+"SweetShopV2_"+name.Replace('.','_')+"_";
  foreach(var suffix in new[]{"BaseColor","Normal","MetallicSmoothness"}){
   var textureImporter=AssetImporter.GetAtPath(prefix+suffix+".png") as TextureImporter;
   textureImporter.textureType=suffix=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
   textureImporter.sRGBTexture=suffix=="BaseColor";textureImporter.mipmapEnabled=true;textureImporter.wrapMode=TextureWrapMode.Clamp;
   textureImporter.alphaSource=TextureImporterAlphaSource.FromInput;textureImporter.alphaIsTransparency=false;textureImporter.maxTextureSize=1024;
   var android=textureImporter.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;android.maxTextureSize=1024;android.format=TextureImporterFormat.ASTC_6x6;textureImporter.SetPlatformTextureSettings(android);
   textureImporter.SaveAndReimport();
  }
  string path=prefix+"Atlas.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
  material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(prefix+"BaseColor.png"));
  material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(prefix+"Normal.png"));material.SetFloat("_BumpScale",.55f);material.EnableKeyword("_NORMALMAP");
  material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(prefix+"MetallicSmoothness.png"));material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",.65f);material.EnableKeyword("_METALLICSPECGLOSSMAP");
  material.SetFloat("_Cull",2);EditorUtility.SetDirty(material);return material;
 }
 public static string Describe(SweetGame g,Transform venue){return "cabinet="+GameObjectUtility.BoundsOf(GameObjectUtility.FindUnder(venue,"Cube"))+" counter="+GameObjectUtility.BoundsOf(GameObjectUtility.FindUnder(venue,"Cube.012"))+" baskets="+string.Join(";",g.baskets.Select(b=>b.transform.position.ToString("F3")))+" rest="+g.rest.position.ToString("F3")+" checkout="+g.checkout.position.ToString("F3");}
}
}
#endif
