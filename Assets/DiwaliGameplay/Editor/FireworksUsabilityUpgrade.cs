#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DiwaliGameplay {
public static class FireworksUsabilityUpgrade {
 [MenuItem("Diwali Island/Gameplay/Place loose rocket on ground")]
 public static void Apply(){
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode before changing rocket staging.");
  var game=UnityEngine.Object.FindFirstObjectByType<FireworksGame>();
  var venue=GameObjectUtility.FindExact("Fireworks - Native PBR");
  if(!game||!venue)throw new InvalidOperationException("Existing Fireworks gameplay is required.");
  PlaceRocketOnGround(game,venue);
 }
 public static void PlaceRocketOnGround(FireworksGame game,Transform venue){
  var tray=game.transform.Find("Rocket staging tray — reused platform");
  var position=game.rocketStagingPosition!=Vector3.zero?game.rocketStagingPosition:game.rocketSocket.position-venue.right*1.15f;
  if(tray){var bounds=GameObjectUtility.BoundsOf(tray);position.x=bounds.center.x;position.z=bounds.center.z;Undo.DestroyObjectImmediate(tray.gameObject);}
  Physics.SyncTransforms();
  float floorY=venue.position.y+.025f;
  var hits=Physics.RaycastAll(new Vector3(position.x,venue.position.y+1,position.z),Vector3.down,2f,~0,QueryTriggerInteraction.Ignore);
  foreach(var hit in hits)if(hit.collider.transform.IsChildOf(venue)&&hit.normal.y>.65f&&hit.point.y>floorY)floorY=hit.point.y;
  var rocketBounds=GameObjectUtility.BoundsOf(game.rocket.transform);
  position.y=floorY+.015f+game.rocket.transform.position.y-rocketBounds.min.y;
  Undo.RecordObject(game.rocket.transform,"Rest loose rocket on venue floor");
  Undo.RecordObject(game,"Save ground rocket staging pose");
  game.rocketStagingPosition=position;game.rocket.transform.position=position;
  EditorUtility.SetDirty(game.rocket.transform);EditorUtility.SetDirty(game);
  EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
  Debug.Log("Loose rocket rests on the ground; the added staging stand was removed. Authored launcher preserved.");
 }
}
}
#endif
