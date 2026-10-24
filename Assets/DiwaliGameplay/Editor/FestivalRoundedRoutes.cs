#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace DiwaliGameplay.Editor {
public static class FestivalRoundedRoutes {
 const string Folder="Assets/DiwaliGameplay/Wayfinding/PathPolish/";
 const string RingName="Rounded Rangoli promenade";
 const string LinkName="Rounded Rangoli approach links";
 const float HalfWidth=.96f, Height=.105f;
 struct Link { public Vector3 start,end; }
 [MenuItem("Diwali/Polish rounded Rangoli walking routes")]
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Rounded paths require Edit mode.");
  var root=GameObjectUtility.FindExact("Festival walking routes");
  var plaza=GameObjectUtility.FindExact("02 Rangoli and Ceremonial Lamps");
  var carpet=GameObjectUtility.FindExact("Flower rangoli - walkable inlaid carpet");
  if(!root||!plaza||!carpet)throw new InvalidOperationException("Authored festival paths and central Rangoli are required.");
  var paving=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Warm sandstone paving.mat");
  var trim=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Burgundy and aged brass inlay.mat");
  if(!paving||!trim)throw new InvalidOperationException("Apply FestivalPathwayPolish first to create the shared PBR palette.");
  foreach(string route in new[]{"East promenade","West promenade"})foreach(string suffix in new[]{" walking surface"," inlaid direction arrows"})
   if(!root.Find(route+suffix))throw new InvalidOperationException("Original central route is missing: "+route+suffix);
  Vector3 centre=GameObjectUtility.BoundsOf(carpet).center;centre.y=Height;
  var lamps=plaza.GetComponentsInChildren<Transform>(false).Where(t=>t.name=="Diya (Blender model)"&&Vector3.ProjectOnPlane(t.position-centre,Vector3.up).magnitude>6).ToArray();
  if(lamps.Length<20)throw new InvalidOperationException("Expected the active outer ceremonial diya circle; found "+lamps.Length);
  float obstructionRadius=0;
  foreach(var lamp in lamps){
   var b=GameObjectUtility.BoundsOf(lamp);
   foreach(var collider in lamp.GetComponentsInChildren<Collider>())if(collider.enabled&&!collider.isTrigger)b.Encapsulate(collider.bounds);
   foreach(float x in new[]{b.min.x,b.max.x})foreach(float z in new[]{b.min.z,b.max.z})obstructionRadius=Mathf.Max(obstructionRadius,Vector2.Distance(new Vector2(x,z),new Vector2(centre.x,centre.z)));
  }
  // Extra 2 cm accounts for the chord sag of the 96-sided inner boundary.
  float inner=obstructionRadius+.52f,radius=inner+HalfWidth,outer=inner+2*HalfWidth;
  var south=Endpoint(root,"Arrival walking surface",true);
  var north=Endpoint(root,"05 Fireworks walking surface",false);
  if((south-centre).magnitude<outer+.15f||(north-centre).magnitude<outer+.15f)
   throw new InvalidOperationException("The lamp-clear ring does not fit between existing approach junctions; inspect before rerouting branches.");
  var links=new[]{new Link{start=south,end=centre+(south-centre).normalized*radius},new Link{start=north,end=centre+(north-centre).normalized*radius}};
  Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Round the Rangoli promenade outside the fixed lamps");
  var surface=new Geometry(false);var border=new Geometry(true);
  const int steps=96;
  for(int i=0;i<steps;i++){
   float a=2*Mathf.PI*i/steps,b=2*Mathf.PI*(i+1)/steps;
   var u=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var v=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
   surface.Quad(centre+u*inner,centre+u*outer,centre+v*outer,centre+v*inner);
   border.Quad(centre+u*(inner-.01f),centre+u*(inner+.08f),centre+v*(inner+.08f),centre+v*(inner-.01f));
   // The outer keyline opens into each approach instead of crossing its walking lane.
   float middle=(a+b)*.5f;var test=centre+new Vector3(Mathf.Cos(middle),0,Mathf.Sin(middle))*outer;
   if(!links.Any(l=>InsideLink(test,l,1.6f)))border.Quad(centre+u*(outer-.08f),centre+u*(outer+.01f),centre+v*(outer+.01f),centre+v*(outer-.08f));
  }
  var linkSurface=new Geometry(false);var linkBorder=new Geometry(true);
  foreach(var link in links){
   var direction=(link.end-link.start).normalized;var side=Vector3.Cross(Vector3.up,direction);float length=Vector3.Distance(link.start,link.end);
   const int samples=32;
   for(int i=0;i<samples;i++){
    float t=(float)i/samples,q=(float)(i+1)/samples;
    var a=Vector3.Lerp(link.start,link.end,t);var b=Vector3.Lerp(link.start,link.end,q);
    // Rounded flare meets the annulus tangentially; the first part matches the branch width.
    float wa=HalfWidth+.55f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Max(0,length-1.8f),length,t*length));
    float wb=HalfWidth+.55f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Max(0,length-1.8f),length,q*length));
    linkSurface.Quad(a-side*wa,a+side*wa,b+side*wb,b-side*wb);
    float distance=Vector3.ProjectOnPlane((a+b)*.5f-centre,Vector3.up).magnitude;
    if(distance>outer+.04f)foreach(float sign in new[]{-1f,1f})linkBorder.Quad(a+side*(sign*(wa-.07f)),a+side*(sign*(wa+.02f)),b+side*(sign*(wb+.02f)),b+side*(sign*(wb-.07f)));
   }
  }
  Write(root,RingName+" walking surface",surface,paving);Write(root,RingName+" inlaid border",border,trim);
  Write(root,LinkName+" walking surface",linkSurface,paving);Write(root,LinkName+" inlaid border",linkBorder,trim);
  foreach(string route in new[]{"East promenade","West promenade"})foreach(string suffix in new[]{" walking surface"," inlaid direction arrows"}){
   var old=root.Find(route+suffix);if(!old)throw new InvalidOperationException("Original central route is missing: "+route+suffix);
   Undo.RecordObject(old.gameObject,"Retire angular central path visual");old.gameObject.SetActive(false);
  }
  // Brighten only the shared walking surface; the earthy grain and existing normal maps remain visible.
  Undo.RecordObject(paving,"Lift sandstone path contrast");paving.SetColor("_BaseColor",new Color(.87f,.72f,.53f,1));paving.SetColor("_Color",new Color(.87f,.72f,.53f,1));EditorUtility.SetDirty(paving);
  Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
  Debug.Log($"FestivalRoundedRoutes: {lamps.Length} fixed outer lamps; centre {centre:F3}; lamp AABB radius {obstructionRadius:F3}m; inner {inner:F3}m / centre {radius:F3}m / outer {outer:F3}m. Nominal lane 1.92m (minimum chord width >1.90m); minimum lamp-edge clearance >0.50m; 96 segments; existing branch endpoints retained; old angular paths inactive. Four replacement renderers, no new colliders/lights. Scene dirty, not saved.");
 }
 static bool InsideLink(Vector3 point,Link link,float width){var d=(link.end-link.start).normalized;float along=Vector3.Dot(point-link.start,d);return along>-.1f&&along<Vector3.Distance(link.start,link.end)+.1f&&Mathf.Abs(Vector3.Dot(point-link.start,Vector3.Cross(Vector3.up,d)))<width;}
 static Vector3 Endpoint(Transform root,string name,bool last){var target=root.Find(name);if(!target)throw new InvalidOperationException("Missing approach "+name);var original=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/DiwaliGameplay/Wayfinding/"+name+".asset");var v=original.vertices;return target.TransformPoint(last?(v[v.Length-1]+v[v.Length-2])*.5f:(v[0]+v[1])*.5f);}
 static void Write(Transform root,string name,Geometry geometry,Material material){
  var target=root.Find(name);if(!target){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));Undo.RegisterCreatedObjectUndo(go,"Create rounded route visual");target=go.transform;target.SetParent(root,false);}
  Undo.RecordObject(target.gameObject,"Activate rounded path visual");target.gameObject.SetActive(true);
  var mesh=geometry.Build(name,target);string path=Folder+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
  if(existing){Undo.RecordObject(existing,"Refresh rounded route mesh");EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
  var filter=target.GetComponent<MeshFilter>();var renderer=target.GetComponent<MeshRenderer>();Undo.RecordObject(filter,"Assign rounded route mesh");Undo.RecordObject(renderer,"Assign shared rounded route material");filter.sharedMesh=mesh;renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
 }
 sealed class Geometry {
  readonly bool trim;readonly List<Vector3> v=new List<Vector3>();readonly List<int> t=new List<int>();readonly List<Vector2> uv=new List<Vector2>();
  public Geometry(bool trim){this.trim=trim;}
  public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=v.Count;var vertices=new[]{a,b,c,d};foreach(var p in vertices)v.Add(p+(trim?Vector3.up*.006f:Vector3.zero));
   uv.AddRange(trim?new[]{new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(0,.5f)}:vertices.Select(p=>new Vector2(p.x/4.4f,p.z/4.4f)).ToArray());t.AddRange(new[]{n,n+2,n+1,n,n+3,n+2,n,n+1,n+2,n,n+2,n+3});}
  public Mesh Build(string name,Transform target){var mesh=new Mesh{name=name};mesh.SetVertices(v.Select(target.InverseTransformPoint).ToList());mesh.SetTriangles(t,0);mesh.SetUVs(0,uv);mesh.normals=Enumerable.Repeat(target.InverseTransformDirection(Vector3.up),v.Count).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;}
 }
}
}
#endif
