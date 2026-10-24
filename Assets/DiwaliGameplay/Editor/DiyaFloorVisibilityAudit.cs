#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiwaliGameplay {
public static class DiyaFloorVisibilityAudit {
 sealed class Triangle {
  public Vector3 a, b, c, normal;
  public float area;
  public int submesh;
 }
 sealed class Surface {
  public MeshRenderer renderer;
  public List<Triangle> triangles;
 }
 static float[] V(Vector3 p) => new[] { p.x, p.y, p.z };
 static string PathOf(Transform t) { string p=t.name;while(t.parent){t=t.parent;p=t.name+"/"+p;}return p; }

 [MenuItem("Diwali/Finish Today/Audit Diya floor visibility")]
 public static void Apply() {
  var venue = GameObjectUtility.FindExact("Diya - Approved Warm Palette");
  var floor = venue ? GameObjectUtility.FindUnder(venue, "Plane.002") : null;
  var filter = floor ? floor.GetComponent<MeshFilter>() : null;
  var renderer = floor ? floor.GetComponent<MeshRenderer>() : null;
  if (!filter || !filter.sharedMesh || !renderer) throw new InvalidOperationException("Diya Plane.002 not found");
  var triangles = Read(filter);
  var bounds = renderer.bounds;
  var submeshes = Enumerable.Range(0,filter.sharedMesh.subMeshCount).Select(index => {
   var set=triangles.Where(t=>t.submesh==index).ToArray();
   var horizontal=set.Where(t=>Mathf.Abs(t.normal.y)>.9f).ToArray();
   var material=index<renderer.sharedMaterials.Length?renderer.sharedMaterials[index]:null;
   return new {index, triangles=set.Length, upwardTriangles=horizontal.Count(t=>t.normal.y>0), downwardTriangles=horizontal.Count(t=>t.normal.y<0),
    upwardArea=horizontal.Where(t=>t.normal.y>0).Sum(t=>t.area),downwardArea=horizontal.Where(t=>t.normal.y<0).Sum(t=>t.area),
    horizontalLevels=horizontal.Select(t=>Math.Round((t.a.y+t.b.y+t.c.y)/3,5)).Distinct().OrderBy(y=>y).ToArray(),
    material=material?AssetDatabase.GetAssetPath(material):null, cull=material&&material.HasProperty("_Cull")?material.GetFloat("_Cull"):-1,
    baseMap=material&&material.HasProperty("_BaseMap")?AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")):null};
  }).ToArray();
  var surfaces=new List<Surface>();
  foreach(var candidate in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) {
   if(!candidate.enabled||!candidate.gameObject.activeInHierarchy)continue;
   var b=candidate.bounds;
   if(b.max.x<bounds.min.x||b.min.x>bounds.max.x||b.max.z<bounds.min.z||b.min.z>bounds.max.z||b.min.y>bounds.max.y+.3f||b.max.y<bounds.min.y-.3f)continue;
   var mf=candidate.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;
   surfaces.Add(new Surface{renderer=candidate,triangles=candidate==renderer?triangles:Read(mf)});
  }
  var samples=new List<object>();
  for(int ix=1;ix<=5;ix++)for(int iz=1;iz<=5;iz++) {
   var point=new Vector3(Mathf.Lerp(bounds.min.x,bounds.max.x,ix/6f),bounds.max.y+3,Mathf.Lerp(bounds.min.z,bounds.max.z,iz/6f));
   var collisions=Physics.RaycastAll(point,Vector3.down,6,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Select(h=>new{path=PathOf(h.collider.transform),y=h.point.y,normal=V(h.normal)}).ToArray();
   var layers=new List<(string path,int submesh,float y,float normalY,string material)>();
   foreach(var surface in surfaces) {
    var rb=surface.renderer.bounds;
    if(point.x<rb.min.x||point.x>rb.max.x||point.z<rb.min.z||point.z>rb.max.z)continue;
    foreach(var t in surface.triangles) {
     if(Mathf.Abs(t.normal.y)<.6f||!AtXZ(t,point,out var y)||y<bounds.min.y-.3f||y>bounds.max.y+.3f)continue;
     var mats=surface.renderer.sharedMaterials;var mat=t.submesh<mats.Length?mats[t.submesh]:null;
     layers.Add((PathOf(surface.renderer.transform),t.submesh,y,t.normal.y,mat?mat.name:null));
    }
   }
   var renderLayers=layers.GroupBy(v=>new{v.path,v.submesh,y=Math.Round(v.y,5),n=Math.Round(v.normalY,3)}).Select(g=>g.First()).OrderByDescending(v=>v.y).Select(v=>new{v.path,v.submesh,v.y,v.normalY,v.material}).ToArray();
   samples.Add(new {xz=new[]{point.x,point.z},colliderHits=collisions,renderLayers});
  }
  var report=new {floor=PathOf(floor),position=V(floor.position),scale=V(floor.lossyScale),boundsMin=V(bounds.min),boundsMax=V(bounds.max),
   mesh=AssetDatabase.GetAssetPath(filter.sharedMesh),submeshes,candidateRenderers=surfaces.Select(s=>new{path=PathOf(s.renderer.transform),min=V(s.renderer.bounds.min),max=V(s.renderer.bounds.max)}).ToArray(),samples};
  var folder=System.IO.Path.GetFullPath("../Game_Plans/Finish_Today_2026-10-24");Directory.CreateDirectory(folder);
  var output=System.IO.Path.Combine(folder,"Diya_Floor_Visibility_Audit.json");
  File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
  Debug.Log("Diya floor visibility audit: "+output+"; "+string.Join("; ",submeshes.Select(s=>"slot "+s.index+" up="+s.upwardTriangles+" down="+s.downwardTriangles+" levels="+string.Join(",",s.horizontalLevels))));
 }

 static bool AtXZ(Triangle t,Vector3 p,out float y) {
  y=0;float denominator=(t.b.z-t.c.z)*(t.a.x-t.c.x)+(t.c.x-t.b.x)*(t.a.z-t.c.z);
  if(Mathf.Abs(denominator)<.0000001f)return false;
  float u=((t.b.z-t.c.z)*(p.x-t.c.x)+(t.c.x-t.b.x)*(p.z-t.c.z))/denominator;
  float v=((t.c.z-t.a.z)*(p.x-t.c.x)+(t.a.x-t.c.x)*(p.z-t.c.z))/denominator;
  float w=1-u-v;if(u<-.00001f||v<-.00001f||w<-.00001f)return false;
  y=u*t.a.y+v*t.b.y+w*t.c.y;return true;
 }
 static List<Triangle> Read(MeshFilter filter) {
  var result=new List<Triangle>();
  using(var array=MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh)) {
   var data=array[0];
   using(var vertices=new NativeArray<Vector3>(data.vertexCount,Allocator.Temp)) {
    data.GetVertices(vertices);
    for(int s=0;s<data.subMeshCount;s++) {
     var descriptor=data.GetSubMesh(s);if(descriptor.topology!=MeshTopology.Triangles)continue;
     for(int i=0;i+2<descriptor.indexCount;i+=3) {
      int Index(int offset)=>data.indexFormat==IndexFormat.UInt16?data.GetIndexData<ushort>()[descriptor.indexStart+offset]+descriptor.baseVertex:(int)data.GetIndexData<uint>()[descriptor.indexStart+offset]+descriptor.baseVertex;
      var a=filter.transform.TransformPoint(vertices[Index(i)]);var b=filter.transform.TransformPoint(vertices[Index(i+1)]);var c=filter.transform.TransformPoint(vertices[Index(i+2)]);
      var cross=Vector3.Cross(b-a,c-a);if(cross.sqrMagnitude<.0000000001f)continue;
      result.Add(new Triangle{a=a,b=b,c=c,normal=cross.normalized,area=cross.magnitude*.5f,submesh=s});
     }
    }
   }
  }
  return result;
 }
}
}
#endif
