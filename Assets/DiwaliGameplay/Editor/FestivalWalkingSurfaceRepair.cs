#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace DiwaliGameplay {
public static class FestivalWalkingSurfaceRepair {
 const string Folder="Assets/DiwaliGameplay/Wayfinding/PathPolish/";
 [MenuItem("Diwali/Finish Today/Repair walking surface collision")]
 public static void Apply(){
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
  var root=GameObjectUtility.FindExact("Festival walking routes");
  var sample=UnityEngine.Object.FindObjectsByType<TeleportationArea>(FindObjectsSortMode.None).First(a=>a.teleportationProvider);
  int surfaces=0,removed=0;
  foreach(var filter in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.EndsWith(" walking surface",StringComparison.Ordinal)||f.name.Contains(" inlaid "))){
   var source=filter.sharedMesh;if(!source)continue;
   var vertices=source.vertices;var old=source.triangles;var triangles=new List<int>();var seen=new HashSet<string>();
   for(int i=0;i<old.Length;i+=3){
    int a=old[i],b=old[i+1],c=old[i+2];
    string key=string.Join(",",new[]{a,b,c}.OrderBy(v=>v));
    if(!seen.Add(key)){removed++;continue;}
    var normal=Vector3.Cross(filter.transform.TransformVector(vertices[b]-vertices[a]),filter.transform.TransformVector(vertices[c]-vertices[a]));
    if(normal.y<0){int swap=b;b=c;c=swap;}
    triangles.Add(a);triangles.Add(b);triangles.Add(c);
   }
   var mesh=UnityEngine.Object.Instantiate(source);mesh.name=filter.name+" stable surface";mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
   string path=Folder+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(saved){Undo.RecordObject(saved,"Repair path winding");EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}else{AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
   Undo.RecordObject(filter,"Use stable path mesh");filter.sharedMesh=saved;
   var renderer=filter.GetComponent<MeshRenderer>();Undo.RecordObject(renderer,"Prevent path self-shadow artifacts");renderer.shadowCastingMode=ShadowCastingMode.Off;
   if(!filter.name.EndsWith(" walking surface",StringComparison.Ordinal))continue;
   var collider=filter.GetComponent<MeshCollider>();if(!collider)collider=Undo.AddComponent<MeshCollider>(filter.gameObject);Undo.RecordObject(collider,"Match collision to visible paving");collider.sharedMesh=saved;collider.convex=false;collider.isTrigger=false;collider.contactOffset=.003f;
   var area=filter.GetComponent<TeleportationArea>();if(!area)area=Undo.AddComponent<TeleportationArea>(filter.gameObject);Undo.RecordObject(area,"Teleport onto visible paving");area.colliders.Clear();area.colliders.Add(collider);area.interactionLayers=sample.interactionLayers;area.teleportationProvider=sample.teleportationProvider;area.matchOrientation=MatchOrientation.WorldSpaceUp;surfaces++;
  }
  var ground=GameObjectUtility.FindExact("Diwali Festival Ground");
  foreach(var renderer in ground.GetComponentsInChildren<MeshRenderer>())if(renderer.bounds.size.y<.025f&&renderer.bounds.size.x>1&&renderer.bounds.size.z>1){Undo.RecordObject(renderer,"Prevent layered paving shadow acne");renderer.shadowCastingMode=ShadowCastingMode.Off;}
  Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);AssetDatabase.SaveAssets();
  Debug.Log("Walking surfaces repaired: "+surfaces+" collision/teleport surfaces; "+removed+" duplicate reverse faces removed.");
 }
}
}
#endif
