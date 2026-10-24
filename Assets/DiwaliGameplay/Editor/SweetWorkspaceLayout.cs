#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiwaliGameplay {
public static class SweetWorkspaceLayout {
 static string Folder=>Path.GetFullPath("../Game_Plans/Finish_Today_2026-10-24");
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 static object B(Transform t){var b=GameObjectUtility.BoundsOf(t);return new{name=t.name,position=V(t.position),rotation=V(t.eulerAngles),center=V(b.center),size=V(b.size),min=V(b.min),max=V(b.max)};}
 [MenuItem("Diwali/Gameplay/Inspect Sweet workspace")]
 public static void Inspect(){
  var g=UnityEngine.Object.FindFirstObjectByType<SweetGame>();var venue=GameObjectUtility.FindExact("SweetShop - Approved Warm Palette");
  Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"Sweet_Workspace.json"),JsonConvert.SerializeObject(new{time=DateTime.UtcNow,playing=EditorApplication.isPlaying,parts=new[]{"Cube","Cube.012","Cube.013","Cube.014","Plane.001","Plane.002"}.Select(n=>B(GameObjectUtility.FindUnder(venue,n))),rest=B(g.rest),checkout=B(g.checkout),reader=B(g.cardReader),card=B(g.card.transform),baskets=g.baskets.Select(b=>B(b.transform)),workspaceForward=V(g.workspaceForward)},Formatting.Indented));
 }
 [MenuItem("Diwali/Gameplay/Arrange Sweet forward workspace")]
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Arrange the saved shop outside Play Mode.");
  var g=UnityEngine.Object.FindFirstObjectByType<SweetGame>();var venue=GameObjectUtility.FindExact("SweetShop - Approved Warm Palette");
  if(venue.Find("Sweet forward workspace")){Inspect();return;}
  Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Arrange sweet shop in a shared forward workspace");
  var cabinet=GameObjectUtility.FindUnder(venue,"Accessible cabinet group");var mesh=GameObjectUtility.FindUnder(venue,"Cube");
  var cabinetBounds=GameObjectUtility.BoundsOf(mesh);
  var delta=new Vector3(-28.9f,cabinetBounds.center.y,1.2f)-cabinetBounds.center;
  Move(cabinet,delta,Quaternion.identity,Vector3.zero);
  foreach(var sweet in g.sweets)if(!sweet.transform.IsChildOf(cabinet))Move(sweet.transform,delta,Quaternion.identity,Vector3.zero);
  var fill=GameObjectUtility.FindUnder(venue,"SweetShop warm courtyard fill");if(fill)Move(fill,delta,Quaternion.identity,Vector3.zero);
  var counter=GameObjectUtility.FindUnder(venue,"Cube.012");var counterGroup=GameObjectUtility.FindUnder(venue,"Accessible checkout group");
  var cb=GameObjectUtility.BoundsOf(counter);var pivot=cb.center;var turn=Quaternion.AngleAxis(180,Vector3.up);
  var dest=new Vector3(-32.45f,pivot.y,1.2f);
  var parts=new[]{counterGroup,g.card.transform,g.rest,g.checkout,g.cardReader,g.collection}.Where(t=>t).Distinct().ToArray();
  foreach(var t in parts.Where(t=>!parts.Any(p=>p!=t&&t.IsChildOf(p))))Move(t,dest-pivot,turn,pivot);
  g.workspaceForward=Vector3.forward;g.closedGlass=g.glass.Select(t=>t.localPosition).ToArray();
  g.panelPosition=g.rest.position+Vector3.right*.9f+Vector3.forward*.3f;g.panelPosition.y=g.worktopY+.45f;g.panelRotation=Quaternion.LookRotation(Vector3.back,Vector3.up);
  var stamp=new GameObject("Sweet forward workspace");Undo.RegisterCreatedObjectUndo(stamp,"Record forward workspace");stamp.transform.SetParent(venue,false);
  EditorUtility.SetDirty(g);Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(g.gameObject.scene);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(undo);Inspect();
 }
 static void Move(Transform t,Vector3 delta,Quaternion turn,Vector3 pivot){Undo.RecordObject(t,"Move sweet work area");t.SetPositionAndRotation(pivot+delta+turn*(t.position-pivot),turn*t.rotation);EditorUtility.SetDirty(t);}
 [MenuItem("Diwali/Gameplay/Capture Sweet forward workspace")]
 public static void Capture(){
  var g=UnityEngine.Object.FindFirstObjectByType<SweetGame>();Directory.CreateDirectory(Folder);
  File.Copy(ChristmasIslandQA.Render("Sweet-forward-workspace",new Vector3(-30.4f,1.65f,4.4f),new Vector3(-30.4f,1.05f,1.2f),88),Path.Combine(Folder,"Sweet-Forward-Workspace.png"),true);
 }
}
}
#endif
