#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiwaliGameplay {
public static class VenueSpaceLayoutDiyaFireworks {
 public static void Apply() {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Apply venue layout in Edit mode.");
  var diya=UnityEngine.Object.FindFirstObjectByType<DiyaGame>();
  var fireworks=UnityEngine.Object.FindFirstObjectByType<FireworksGame>();
  if(!diya||!fireworks)throw new InvalidOperationException("Diya and Fireworks gameplay must be installed.");
  Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Organize Diya and Fireworks stations");
  CompactDiya(diya);
  CompactFountain(fireworks);
  EditorUtility.SetDirty(diya);EditorUtility.SetDirty(fireworks);
  EditorSceneManager.MarkSceneDirty(diya.gameObject.scene);
  Undo.CollapseUndoOperations(group);
  Debug.Log("Diya layout retained. Fireworks fountain support brought forward; staged props remain on authored side stairs. Source routes, hinges, axes, scales, and runtime menu placement retained.");
 }
 public static void ApplyPreparationBenchOnly() {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Move the Diya bench in Edit mode.");
  var game=UnityEngine.Object.FindFirstObjectByType<DiyaGame>();
  var venue=GameObjectUtility.FindExact("Diya - Approved Warm Palette");
  if(!game||!venue)throw new InvalidOperationException("Diya venue and gameplay must be installed.");
  var bench=GameObjectUtility.FindUnder(venue,"Cube.004");
  var chest=GameObjectUtility.FindUnder(venue,"Cube.001");
  var floor=GameObjectUtility.FindUnder(venue,"Plane.002");
  if(!bench||!chest||!floor)throw new InvalidOperationException("Diya preparation bench, chest, or floor is missing.");
  var bounds=MeshBounds(bench);var floorBounds=MeshBounds(floor);
  var destination=new Vector3(bounds.center.x,bounds.center.y,floorBounds.max.z-bounds.extents.z-.55f);
  var desiredRotation=venue.rotation*Quaternion.Euler(0,180,0);
  var rotation=desiredRotation*Quaternion.Inverse(bench.rotation);
  if(Vector3.Distance(bounds.center,destination)<.0001f&&Quaternion.Angle(rotation,Quaternion.identity)<.001f)return;
  var candidates=new[]{bench,game.pot.transform,game.key.transform,chest,game.lid,game.feather.transform,game.potRest,game.keySocket,game.featherRest};
  var roots=new List<Transform>();
  foreach(var candidate in candidates) {
   if(!candidate)throw new InvalidOperationException("A required preparation bench item or rest is missing.");
   bool nested=false;
   foreach(var other in candidates)if(other&&other!=candidate&&candidate.IsChildOf(other)){nested=true;break;}
   if(!nested&&!roots.Contains(candidate))roots.Add(candidate);
  }
  Undo.IncrementCurrentGroup();int undoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Move Diya preparation bench to opposite wall");
  Undo.RecordObject(game,"Move chest lock world anchors");
  var origin=bounds.center;
  foreach(var root in roots) {
   Undo.RecordObject(root,"Move supported bench contents");
   root.SetPositionAndRotation(destination+rotation*(root.position-origin),rotation*root.rotation);
   EditorUtility.SetDirty(root);
  }
  game.keyLockPoint=destination+rotation*(game.keyLockPoint-origin);
  game.keyTurnAxis=rotation*game.keyTurnAxis;
  EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
  Undo.CollapseUndoOperations(undoGroup);
  Debug.Log("Moved only the Diya preparation bench and its supported pot/key/chest assembly to the opposite wall. Drawer, carried diya, menus, floor, route, and display remain unchanged.");
 }
 static void CompactDiya(DiyaGame game) {
  // Diya layout is protected by the user's latest instruction.
  // ApplyPreparationBenchOnly moves its supported contents as one rigid assembly.
 }
 static void CompactFountain(FireworksGame game) {
  var venue=GameObjectUtility.FindExact("Fireworks - Native PBR");
  var group=GameObjectUtility.FindUnder(venue,"Accessible fountain station");
  var platform=GameObjectUtility.FindUnder(venue,"platform");
  var platformBounds=MeshBounds(platform);
  var rocket=game.rocketSocket.position;
  // The target board stays behind the rocket. The fountain has its own clear right-hand work area.
  float targetZ=rocket.z+1.65f;
  var delta=new Vector3(0,0,targetZ-platformBounds.center.z);
  Move(group,delta);
  if(!game.lift.IsChildOf(group))Move(game.lift,delta);
  Undo.RecordObject(game,"Fountain lift home");
  game.liftHome=game.lift.localPosition;
  // FireworksMenuPlacement anchors the controls beside the plunger in Play mode.
  // Fountain and spinner staging remains on the modeled stairs, whose measured top is 1.01 m.
 }
 static void Move(Transform target,Vector3 delta) {
  if(delta.sqrMagnitude<.0000001f)return;
  Undo.RecordObject(target,"Move venue station");target.position+=delta;EditorUtility.SetDirty(target);
 }
 static Bounds MeshBounds(Transform root) {
  bool found=false;var result=new Bounds(root.position,Vector3.zero);
  foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) {
   if(renderer.GetComponent<TextMesh>())continue;
   if(!found){result=renderer.bounds;found=true;}else result.Encapsulate(renderer.bounds);
  }
  if(!found)throw new InvalidOperationException("No model bounds for "+root.name);
  return result;
 }
}
}
#endif
