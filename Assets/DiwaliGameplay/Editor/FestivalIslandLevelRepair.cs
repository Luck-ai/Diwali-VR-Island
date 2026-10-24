#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiwaliGameplay {
public static class FestivalIslandLevelRepair {
 [MenuItem("Diwali/Final Pass/Align islands and lower sea")]
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.path!="Assets/Scenes/DiwaliFestivalGround.unity")throw new InvalidOperationException("Open the festival scene first.");
  var diwali=GameObjectUtility.FindExact("Diwali Festival Ground");var christmas=GameObjectUtility.FindExact("Christmas Island Integration");
  var bridge=GameObjectUtility.FindExact("Diwali Christmas Walkway");var terrain=GameObjectUtility.FindExact("Island_Terrain");var rig=GameObjectUtility.FindExact("XR Origin Hands (XR Rig)");
  if(!diwali||!christmas||!bridge||!terrain||!rig)throw new InvalidOperationException("Island or player roots are missing.");
  Directory.CreateDirectory("Scene_Backups/WaterElevation");
  EditorSceneManager.SaveScene(scene,"Scene_Backups/WaterElevation/before-level-repair-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ")+".unity",true);
  SetY(diwali,0);SetY(christmas,0);SetY(bridge,0);
  Undo.RecordObject(terrain,"Restore Christmas terrain to its props");var p=terrain.localPosition;p.y=0;terrain.localPosition=p;PrefabUtility.RecordPrefabInstancePropertyModifications(terrain);
  SetY(rig,-.02f);
  Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);
  FestivalWaterSurroundings.Apply();
 }
 static void SetY(Transform target,float y){Undo.RecordObject(target,"Align festival islands and gameplay");var p=target.position;p.y=y;target.position=p;PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
}
}
#endif
